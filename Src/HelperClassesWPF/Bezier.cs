using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    public static class BezierUtil
    {
        // Get points along the curve
        public static Point3D[] GetPoints(int count, Point3D from, Point3D control, Point3D to)
        {
            return GetPoints(count, new[] { from, control, to });
        }
        public static Point3D[] GetPoints(int count, Point3D from, Point3D fromControl, Point3D toControl, Point3D to)
        {
            return GetPoints(count, new[] { from, fromControl, toControl, to });
        }
        public static Point3D[] GetPoints(int count, Point3D from, Point3D[] controls, Point3D to)
        {
            return GetPoints(count, UtilityCore.Iterate<Point3D>(from, controls, to).ToArray());
        }
        public static Point3D[] GetPoints(int count, Point3D[] controlPoints)
        {
            #region asserts
#if DEBUG
            if (controlPoints.Length < 2)
            {
                throw new ArgumentException("There must be at least two points passed in: " + controlPoints.Length.ToString());
            }
#endif
            #endregion

            double countD = count - 1;

            Point3D[] retVal = new Point3D[count];

            retVal[0] = controlPoints[0];
            retVal[count - 1] = controlPoints[controlPoints.Length - 1];

            for (int cntr = 1; cntr < count - 1; cntr++)
            {
                retVal[cntr] = GetPoint(cntr / countD, controlPoints);
            }

            return retVal;
        }
        public static Point3D[] GetPoints(int count, BezierSegment3D segment)
        {
            return GetPoints(count, UtilityCore.Iterate<Point3D>(segment.EndPoint0, segment.ControlPoints, segment.EndPoint1).ToArray());
        }

        /// <summary>
        /// Get a single point along the curve
        /// </summary>
        public static Point3D GetPoint(double percent, BezierSegment3D segment)
        {
            return GetPoint(percent, UtilityCore.Iterate<Point3D>(segment.EndPoint0, segment.ControlPoints, segment.EndPoint1).ToArray());
        }
        /// <summary>
        /// Get a single point along the curve
        /// </summary>
        /// <returns>
        /// Got this here:
        /// http://www.cubic.org/docs/bezier.htm
        /// </returns>
        public static Point3D GetPoint(double percent, Point3D[] controlPoints)
        {
            #region asserts
#if DEBUG
            if (controlPoints.Length < 2)
            {
                throw new ArgumentException("There must be at least two points passed in: " + controlPoints.Length.ToString());
            }
#endif
            #endregion

            Point3D[] prev = controlPoints;
            Point3D[] current = null;

            for (int outer = controlPoints.Length - 1; outer > 0; outer--)
            {
                current = new Point3D[outer];

                for (int inner = 0; inner < outer; inner++)
                {
                    current[inner] = Math3D.LERP(prev[inner], prev[inner + 1], percent);
                }

                prev = current;
            }

            return current[0];      // by the time execution gets here, the array only has one element
        }
        public static Point3D GetPoint(double percent, BezierSegment3D[] bezier)
        {
            //TODO: If the bezier is closed, make it circular
            if (percent < 0) return bezier[0].EndPoint0;

            double totalLength = bezier.Sum(o => o.Length_quick);

            double fromPercent = 0d;
            for(int cntr = 0; cntr < bezier.Length; cntr++)
            {
                double toPercent = fromPercent + (bezier[cntr].Length_quick / totalLength);

                if(percent >= fromPercent && percent <= toPercent)
                {
                    double localPercent = ((percent - fromPercent) * totalLength) / bezier[cntr].Length_quick;

                    return GetPoint(localPercent, bezier[cntr]);
                }

                fromPercent = toPercent;
            }

            return bezier[bezier.Length - 1].EndPoint1;
        }

        /// <summary>
        /// This returns points across several segment definitions.  count is the total number of sample points to return
        /// </summary>
        /// <remarks>
        /// This assumes that the segments are linked together into a single path
        /// 
        /// If the first and last point of segments are the same, then this will only return that shared point once (but the point count
        /// will still be how many were requested
        /// </remarks>
        public static Point3D[] GetPath(int count, BezierSegment3D[] segments)
        {
            // Get the total length of the curve
            double totalLength = 0;
            double[] cumulativeLengths = new double[segments.Length + 1];
            for (int cntr = 1; cntr < segments.Length + 1; cntr++)
            {
                totalLength += segments[cntr - 1].Length_quick;
                cumulativeLengths[cntr] = cumulativeLengths[cntr - 1] + segments[cntr - 1].Length_quick;
            }

            double countD = count - 1;

            Point3D[] retVal = new Point3D[count];

            retVal[0] = segments[0].EndPoint0;
            retVal[count - 1] = segments[segments.Length - 1].EndPoint1;        //NOTE: If the segment is a closed curve, this is the same point as retVal[0].  May want a boolean that tells whether the last point should be replicated

            int index = 0;

            for (int cntr = 1; cntr < count - 1; cntr++)
            {
                // Get the location along the entire path
                double totalPercent = cntr / countD;
                double portionTotalLength = totalLength * totalPercent;

                // Advance to the appropriate segment
                while (cumulativeLengths[index + 1] < portionTotalLength)
                {
                    index++;
                }

                // Get the percent of the current segment
                double localLength = portionTotalLength - cumulativeLengths[index];
                double localPercent = localLength / segments[index].Length_quick;

                // Calculate the bezier point
                retVal[cntr] = GetPoint(localPercent, segments[index].Combined);
            }

            return retVal;
        }
        /// <summary>
        /// This returns points across sets of segment definition.  Each set is run through the other path overload.  So the endpoints
        /// of each set are guaranteed to be included in the return points (deduped)
        /// </summary>
        /// <param name="countPerPath">This is how many points per set (the total number of points will be countPerPath * segmentSets.Length)</param>
        public static Point3D[] GetPath(int countPerPath, BezierSegment3D[][] segmentSets)
        {
            //TODO: Make an overload that takes in total count instead of per path

            // Get the points for each set of beziers
            List<Point3D[]> perPathPoints = new List<Point3D[]>();

            foreach (BezierSegment3D[] segments in segmentSets)
            {
                if (segments.Length == 1)
                {
                    perPathPoints.Add(GetPoints(countPerPath, segments[0]));
                }
                else
                {
                    perPathPoints.Add(GetPath(countPerPath, segments));
                }
            }

            // Dedupe them
            List<Point3D> retVal = new List<Point3D>();

            retVal.AddRange(perPathPoints[0]);

            for (int cntr = 1; cntr < perPathPoints.Count; cntr++)
            {
                if (Math3D.IsNearValue(retVal[retVal.Count - 1], perPathPoints[cntr][0]))
                {
                    // First point dupes with the last
                    retVal.AddRange(perPathPoints[cntr].Skip(1));
                }
                else
                {
                    retVal.AddRange(perPathPoints[cntr]);
                }
            }

            if (Math3D.IsNearValue(retVal[0], retVal[retVal.Count - 1]))
            {
                retVal.RemoveAt(retVal.Count - 1);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This is a helper method that creates a bezier definition that runs through a set of points
        /// </summary>
        /// <param name="ends">These are the end points that the beziers run through</param>
        /// <param name="along">This is how far out the control points should be pulled from the end points (it is a percent of that line segment's length)</param>
        /// <param name="isClosed">
        /// True: The assumption is that ends[0] and ends[len-1] aren't the same point.  This will add an extra segment to create a closed curve.
        /// False: This method compares ends[0] and ends[len-1].  If they are the same point, it makes a closed curve.  If they are different, it makes an open curve.
        /// </param>
        public static BezierSegment3D[] GetBezierSegments(Point3D[] ends, double along = .25, bool isClosed = false)
        {
            if (isClosed)
            {
                return GetBezierSegments_Closed(ends, along);
            }

            if (ends.Length > 2 && Math3D.IsNearValue(ends[0], ends[ends.Length - 1]))
            {
                Point3D[] endsClosed = new Point3D[ends.Length - 1];
                Array.Copy(ends, endsClosed, ends.Length - 1);
                return GetBezierSegments_Closed(endsClosed, along);       // remove the last point, which is redundant
            }
            else
            {
                return GetBezierSegments_Open(ends, along);
            }
        }

        public static Tuple<Point, Point> GetControlPoints_Middle(Point end1, Point end2, Point end3, double percentAlong12 = .25, double percentAlong23 = .25)
        {
            // Just use the 3D overload
            var retVal = GetControlPoints_Middle(end1.ToPoint3D(), end2.ToPoint3D(), end3.ToPoint3D(), percentAlong12, percentAlong23);

            // Convert the response back to 2D
            return Tuple.Create(retVal.Item1.ToPoint2D(), retVal.Item2.ToPoint2D());
        }
        /// <summary>
        /// This is a helper method to generate control points
        /// </summary>
        /// <remarks>
        /// A bezier curve will always go through the end points.  It will use the control points to pull it off the direct
        /// line segment.
        /// 
        /// When two bezier segments are linked, the curve will be smooth if the two control points for the shared
        /// end point are in a line.
        /// 
        /// This method takes the three end points, and returns the two control points for the middle end point (end2)
        /// 
        /// The returned control points will be colinear with end2
        /// </remarks>
        /// <param name="percentAlong12">This is the percent of the 1-2 segment's length</param>
        /// <param name="percentAlong23">This is the percent of the 2-3 segment's length</param>
        /// <returns>
        /// Item1=control point for end2 for the 1-2 bezier segment (this is the last point in this.ControlPoints)
        /// Item2=control point for end2 for the 2-3 bezier segment (this is the first point in this.ControlPoints)
        /// </returns>
        public static Tuple<Point3D, Point3D> GetControlPoints_Middle(Point3D end1, Point3D end2, Point3D end3, double percentAlong12 = .25, double percentAlong23 = .25)
        {
            Vector3D dir21 = end1 - end2;
            Vector3D dir23 = end3 - end2;

            Vector3D? controlLine = GetControlPoints_Middle_ControlLine(dir21, dir23);
            if (controlLine == null)
            {
                // The directions are either on top of each other, or pointing directly away from each other, or
                // some of the end points are the same.
                //
                // Just return control points that are the same as the middle point.  This could be improved in the
                // future if certain cases look bad
                return Tuple.Create(end2, end2);
            }

            Vector3D controlLineUnit;
            if (Vector3D.DotProduct(dir21, controlLine.Value) > 0)
            {
                // Control line is toward end 1
                controlLineUnit = controlLine.Value.ToUnit();
            }
            else
            {
                // Control line is toward end 3
                controlLineUnit = (-controlLine.Value).ToUnit();
            }

            Point3D control21 = end2 + (controlLineUnit * (dir21.Length * percentAlong12));
            Point3D control23 = end2 - (controlLineUnit * (dir23.Length * percentAlong23));

            return Tuple.Create(control21, control23);
        }

        /// <summary>
        /// This creates a control point for end1.  It is along a line that is some angle from dir12.  The distance along
        /// that rotated direction is (end2-end1)*percentAlong12
        /// </summary>
        /// <param name="otherPoint">This is a third point that is coplanar to end1 and end2.  It is just used to figure out the rotation axis (axis will be orthogonal to the plane defined by the three points)</param>
        /// <param name="isAwayFromOther">Whether the control line should rotate away from otherPoint or toward it</param>
        /// <param name="angle">Angle in degrees</param>
        /// <param name="percentAlong12">This is the percent of the 1-2 segment's length</param>
        public static Point3D GetControlPoint_End(Point3D end1, Point3D end2, Point3D otherPoint, bool isAwayFromOther = true, double angle = 30, double percentAlong12 = .25)
        {
            // Figure out the axis
            Vector3D axis;
            if (isAwayFromOther)
            {
                axis = Vector3D.CrossProduct(otherPoint - end1, end2 - end1);
            }
            else
            {
                axis = Vector3D.CrossProduct(end2 - end1, otherPoint - end1);
            }

            // Call the other overload
            return GetControlPoint_End(end1, end2, axis, angle, percentAlong12);
        }
        public static Point3D GetControlPoint_End(Point3D end1, Point3D end2, Vector3D axis, double angle = 20, double along12 = .25)
        {
            Vector3D dir12 = end2 - end1;

            Vector3D controlLine = dir12.GetRotatedVector(axis, angle).ToUnit();
            controlLine = controlLine * (dir12.Length * along12);

            return end1 + controlLine;
        }

        #region Private Methods

        private static BezierSegment3D[] GetBezierSegments_Closed(Point3D[] ends, double along = .25)
        {
            //NOTE: The difference between closed and open is closed has one more segment that loops back to zero (and a control point for point zero)

            // Precalculate the control points
            Tuple<Point3D, Point3D>[] controls = new Tuple<Point3D, Point3D>[ends.Length - 1];

            for (int cntr = 1; cntr < ends.Length; cntr++)
            {
                int lastIndex = cntr == ends.Length - 1 ? 0 : cntr + 1;

                Tuple<double, double> adjustedAlong = GetAdjustedRatios(ends[cntr - 1], ends[cntr], ends[lastIndex], along);

                controls[cntr - 1] = GetControlPoints_Middle(ends[cntr - 1], ends[cntr], ends[lastIndex], adjustedAlong.Item1, adjustedAlong.Item2);
            }

            Tuple<double, double> adjustedAlong2 = GetAdjustedRatios(ends[ends.Length - 1], ends[0], ends[1], along);
            var extraControl = GetControlPoints_Middle(ends[ends.Length - 1], ends[0], ends[1], adjustedAlong2.Item1, adjustedAlong2.Item2);      // loop back

            // Build the return segments
            BezierSegment3D[] retVal = new BezierSegment3D[ends.Length];

            for (int cntr = 0; cntr < ends.Length; cntr++)
            {
                Point3D? ctrl0 = cntr == 0 ? extraControl.Item2 : controls[cntr - 1].Item2;
                Point3D? ctrl1 = cntr == ends.Length - 1 ? extraControl.Item1 : controls[cntr].Item1;

                int lastIndex = cntr == ends.Length - 1 ? 0 : cntr + 1;

                retVal[cntr] = new BezierSegment3D(cntr, lastIndex, UtilityCore.Iterate<Point3D>(ctrl0, ctrl1).ToArray(), ends);
            }

            return retVal;
        }
        private static BezierSegment3D[] GetBezierSegments_Open(Point3D[] ends, double along = .25)
        {
            // Precalculate the control points
            Tuple<Point3D, Point3D>[] controls = new Tuple<Point3D, Point3D>[ends.Length - 2];

            for (int cntr = 1; cntr < ends.Length - 1; cntr++)
            {
                Tuple<double, double> adjustedAlong = GetAdjustedRatios(ends[cntr - 1], ends[cntr], ends[cntr + 1], along);

                controls[cntr - 1] = GetControlPoints_Middle(ends[cntr - 1], ends[cntr], ends[cntr + 1], adjustedAlong.Item1, adjustedAlong.Item2);
            }

            // Build the return segments
            BezierSegment3D[] retVal = new BezierSegment3D[ends.Length - 1];

            for (int cntr = 0; cntr < ends.Length - 1; cntr++)
            {
                Point3D? ctrl0 = cntr == 0 ? (Point3D?)null : controls[cntr - 1].Item2;
                Point3D? ctrl1 = cntr == ends.Length - 2 ? (Point3D?)null : controls[cntr].Item1;

                retVal[cntr] = new BezierSegment3D(cntr, cntr + 1, UtilityCore.Iterate<Point3D>(ctrl0, ctrl1).ToArray(), ends);
            }

            return retVal;
        }

        private static Vector3D? GetControlPoints_Middle_ControlLine(Vector3D dir21, Vector3D dir23)
        {
            // Get the angle between the two directions
            double angle = Vector3D.AngleBetween(dir21, dir23);
            if (Double.IsNaN(angle))
            {
                return null;
            }

            Vector3D axis = Vector3D.CrossProduct(dir21, dir23);
            if (axis.IsNearZero())
            {
                return null;
            }

            // Get the vector directly between the two directions
            Vector3D between = dir21.GetRotatedVector(axis, angle / 2d);

            // Now get the vector that is orthogonal to that between vector.  This is the line that
            // the control points will be along
            return Vector3D.CrossProduct(between, axis);        // length doesn't really matter for this.  It could also point in the exact opposite direction, and that wouldn't matter
        }

        private static Tuple<double, double> GetAdjustedRatios(Point3D p1, Point3D p2, Point3D p3, double along)
        {
            double length12 = (p2 - p1).Length;
            double length23 = (p3 - p2).Length;

            // The shorter segment gets the full amount, and the longer segment gets an adjusted amount

            if (length12.IsNearValue(length23))
            {
                return Tuple.Create(along, along);
            }
            else if (length12 < length23)
            {
                return Tuple.Create(along, along * (length12 / length23));
            }
            else
            {
                return Tuple.Create(along * (length23 / length12), along);
            }
        }

        #endregion

        #region OLD 2D

        //            public static Point[] GetBezierSegment(int count, Point[] controlPoints)
        //            {
        //                #region asserts
        //#if DEBUG
        //                if (controlPoints.Length < 2)
        //                {
        //                    throw new ArgumentException("There must be at least two points passed in: " + controlPoints.Length.ToString());
        //                }
        //#endif
        //                #endregion

        //                double countD = count - 1;

        //                Point[] retVal = new Point[count];

        //                retVal[0] = controlPoints[0];
        //                retVal[count - 1] = controlPoints[controlPoints.Length - 1];

        //                for (int cntr = 1; cntr < count - 1; cntr++)
        //                {
        //                    retVal[cntr] = GetBezier_SinglePoint(controlPoints, cntr / countD);
        //                }

        //                return retVal;
        //            }
        //            private static Point GetBezier_SinglePoint(Point[] controlPoints, double t)
        //            {
        //                #region asserts
        //#if DEBUG
        //                if (controlPoints.Length < 2)
        //                {
        //                    throw new ArgumentException("There must be at least two points passed in: " + controlPoints.Length.ToString());
        //                }
        //#endif
        //                #endregion

        //                Point[] prev = controlPoints;
        //                Point[] current = null;     // current will always be one smaller than prev (because it's a point along a line from two segments in prev)

        //                for (int outer = controlPoints.Length - 1; outer > 0; outer--)
        //                {
        //                    current = new Point[outer];

        //                    for (int inner = 0; inner < outer; inner++)
        //                    {
        //                        current[inner] = LERP(prev[inner], prev[inner + 1], t);
        //                    }

        //                    // Prep for the next iteration
        //                    prev = current;
        //                }

        //                return current[0];      // by the time execution gets here, the array only has one element
        //            }

        #endregion
    }

    //TODO: Make 1D and ND versions
    #region Class: BezierSegment3D

    public class BezierSegment3D       // wpf already has a BezierSegment
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public BezierSegment3D(int endIndex0, int endIndex1, Point3D[] controlPoints, Point3D[] allEndPoints)
        {
            this.EndIndex0 = endIndex0;
            this.EndIndex1 = endIndex1;
            this.ControlPoints = controlPoints;
            this.AllEndPoints = allEndPoints;

            this.Combined = UtilityCore.Iterate<Point3D>(this.EndPoint0, this.ControlPoints, this.EndPoint1).ToArray();
        }

        #endregion

        public readonly Point3D[] AllEndPoints;

        public readonly int EndIndex0;
        public readonly int EndIndex1;

        public Point3D EndPoint0
        {
            get
            {
                return this.AllEndPoints[this.EndIndex0];
            }
        }
        public Point3D EndPoint1
        {
            get
            {
                return this.AllEndPoints[this.EndIndex1];
            }
        }

        public readonly Point3D[] ControlPoints;

        /// <summary>
        /// This is { EndPoint0, ControlPoints, EndPoint1 }
        /// </summary>
        public readonly Point3D[] Combined;

        private double? _length_quick = null;
        /// <summary>
        /// This is a rough approximation of the length of the bezier.  It will likely be shorter than the actual length
        /// </summary>
        /// <remarks>
        /// Some suggestions on how to do it right:
        /// http://math.stackexchange.com/questions/12186/arc-length-of-b%C3%A9zier-curves
        /// http://www.carlosicaza.com/2012/08/12/an-more-efficient-way-of-calculating-the-length-of-a-bezier-curve-part-ii/
        /// </remarks>
        public double Length_quick
        {
            get
            {
                lock (_lock)
                {
                    if (_length_quick == null)
                    {
                        if(this.ControlPoints == null || this.ControlPoints.Length == 0)
                        {
                            _length_quick = (this.EndPoint1 - this.EndPoint0).Length;
                        }
                        else
                        {
                            double length = 0;

                            length += (this.ControlPoints[0] - this.EndPoint0).LengthSquared;
                            length += (this.ControlPoints[this.ControlPoints.Length - 1] - this.EndPoint1).LengthSquared;

                            for(int cntr = 0; cntr < this.ControlPoints.Length - 1; cntr++)
                            {
                                length += (this.ControlPoints[cntr] - this.ControlPoints[cntr + 1]).LengthSquared;
                            }

                            _length_quick = Math.Sqrt(length);
                        }
                    }

                    return _length_quick.Value;
                }
            }
        }

        #region Public Methods

        public BezierSegment3D ToReverse()
        {
            Point3D[] controlPoints = null;
            if (this.ControlPoints != null)
            {
                controlPoints = this.ControlPoints.Reverse().ToArray();
            }

            return new BezierSegment3D(this.EndIndex1, this.EndIndex0, controlPoints, this.AllEndPoints);      // no need to reverse AllEndPoints, just the indexes
        }

        #endregion
    }

    #endregion

    #region Class: BezierMesh

    //TODO: This needs to use BezierSegment1D.  The x and y in the bezier objects are ignored, and is just wasted calculations

    /// <summary>
    /// This takes in a grid of points, and will then apply beziers to estimate locations between those control points
    /// </summary>
    public class BezierMesh
    {
        #region Constructor

        public BezierMesh(double[] axisX, double[] axisY, double[] valuesZ)
        {
            var result = ConvertToBezier(axisX, axisY, valuesZ);

            this.AxisX = result.Item1;
            this.Horizontal = result.Item2;

            this.AxisY = result.Item3;
            this.Vertical = result.Item4;
        }

        #endregion

        #region Public Properties

        public readonly double[] AxisX;
        public readonly double[] AxisY;

        public readonly BezierSegment3D[][] Horizontal;
        public readonly BezierSegment3D[][] Vertical;

        #endregion

        #region Public Methods

        public double EstimateValue(double x, double y)
        {
            var segX = FindSegment(x, this.AxisX);
            var segY = FindSegment(y, this.AxisY);

            if (new[] { segX.Item1, segX.Item2, segY.Item1, segY.Item2 }.Any(o => o < 0))
            {
                // Probably want to just assume linear.  Use the slope and length of the nearest segments
                throw new ApplicationException("TODO: Handle out of bounds requests");
            }

            Point3D lerp = BicubicInterpolation(
                this.Horizontal[segY.Item1][segX.Item1],
                this.Horizontal[segY.Item2][segX.Item1],
                this.Vertical[segX.Item1][segY.Item1],
                this.Vertical[segX.Item2][segY.Item1],
                segX.Item3,
                segY.Item3);

            return lerp.Z;
        }

        #endregion

        #region Private Methods

        private static Tuple<double[], BezierSegment3D[][], double[], BezierSegment3D[][]> ConvertToBezier(double[] axisX, double[] axisY, double[] valuesZ)
        {
            #region validate

            // X length
            if (axisX == null || axisX.Length < 2)
            {
                throw new ArgumentException(string.Format("axisX must have at least 2 items: len={0}", axisX == null ? "null" : axisX.Length.ToString()));
            }

            // Y length
            if (axisY == null || axisY.Length < 2)
            {
                throw new ArgumentException(string.Format("axisY must have at least 2 items: len={0}", axisY == null ? "null" : axisY.Length.ToString()));
            }

            // Z area
            if (valuesZ == null || valuesZ.Length != axisX.Length * axisY.Length)
            {
                throw new ArgumentException(string.Format("valuesZ is invalid length: values={0}, axis1={1}, axis2={2}", valuesZ == null ? "null" : valuesZ.Length.ToString(), axisX.Length, axisY.Length));
            }

            // X equality
            if (Enumerable.Range(0, axisX.Length - 1).Any(o => axisX[o].IsNearValue(axisX[o + 1])))
            {
                throw new ArgumentException("Values can't be the same in x axis");
            }

            // Y equality
            if (Enumerable.Range(0, axisY.Length - 1).Any(o => axisY[o].IsNearValue(axisY[o + 1])))
            {
                throw new ArgumentException("Values can't be the same in y axis");
            }

            #endregion

            bool isAccendingX = axisX[1] > axisX[0];
            bool isAccendingY = axisY[1] > axisY[0];

            #region validate

            // X ascending
            if (Enumerable.Range(0, axisX.Length - 1).Any(o => isAccendingX ? axisX[o + 1] < axisX[o] : axisX[o + 1] > axisX[o]))
            {
                throw new ArgumentException("The values in axisX must all ascend or descend");
            }

            // Y ascending
            if (Enumerable.Range(0, axisY.Length - 1).Any(o => isAccendingY ? axisY[o + 1] < axisY[o] : axisY[o + 1] > axisY[o]))
            {
                throw new ArgumentException("The values in axisX must all ascend or descend");
            }

            #endregion

            #region ensure ascending X

            if (!isAccendingX)
            {
                axisX = axisX.Reverse().ToArray();

                double[] newZ = new double[valuesZ.Length];

                for (int oldX = 0; oldX < axisX.Length; oldX++)
                {
                    int newX = axisX.Length - 1 - oldX;

                    for (int y = 0; y < axisY.Length; y++)
                    {
                        int yIndex = y * axisX.Length;
                        newZ[yIndex + newX] = valuesZ[yIndex + oldX];
                    }
                }

                valuesZ = newZ;
            }

            #endregion
            #region ensure ascending Y

            if (!isAccendingY)
            {
                axisY = axisY.Reverse().ToArray();

                double[] newZ = new double[valuesZ.Length];

                for (int oldY = 0; oldY < axisY.Length; oldY++)
                {
                    int newY = axisY.Length - 1 - oldY;

                    int yIndexOld = oldY * axisX.Length;
                    int yIndexNew = newY * axisX.Length;

                    for (int x = 0; x < axisX.Length; x++)
                    {
                        newZ[yIndexNew + x] = valuesZ[yIndexOld + x];
                    }
                }

                valuesZ = newZ;
            }

            #endregion

            BezierSegment3D[][] horizontal = new BezierSegment3D[axisY.Length][];     // there is a horizontal set for each y
            BezierSegment3D[][] vertical = new BezierSegment3D[axisX.Length][];

            //TODO: Make an option of this that figures out the percent to use:
            //if you pass in .25, that is the max.
            //  if both segments are equal length then it will be .25 for each
            //  if they are different lengths, the smaller will use .25.  the larger will use the ratio of lengths*.25
            //BezierSegmentDef.GetBezierSegments();

            #region horizontal

            for (int y = 0; y < axisY.Length; y++)
            {
                int yIndex = y * axisX.Length;

                Point3D[] ends = Enumerable.Range(0, axisX.Length).
                    Select(x => new Point3D(axisX[x], axisY[y], valuesZ[yIndex + x])).
                    ToArray();

                horizontal[y] = BezierUtil.GetBezierSegments(ends);
            }

            #endregion
            #region vertical

            for (int x = 0; x < axisX.Length; x++)
            {
                Point3D[] ends = Enumerable.Range(0, axisY.Length).
                    Select(y => new Point3D(axisX[x], axisY[y], valuesZ[(y * axisX.Length) + x])).
                    ToArray();

                vertical[x] = BezierUtil.GetBezierSegments(ends);
            }

            #endregion

            return Tuple.Create(axisX, horizontal, axisY, vertical);
        }

        private static Tuple<int, int, double> FindSegment(double value, double[] axis)
        {
            // Find the two points that straddle the value
            for (int cntr = 0; cntr < axis.Length - 1; cntr++)
            {
                if (value >= axis[cntr] && value <= axis[cntr + 1])
                {
                    double percent = (value - axis[cntr]) / (axis[cntr + 1] - axis[cntr]);
                    return Tuple.Create(cntr, cntr + 1, percent);
                }
            }

            // Out of bounds
            if (value < axis[0])
            {
                return Tuple.Create(-1, 0, 0d);
            }
            else //if (value > axis[axis.Length - 1])
            {
                return Tuple.Create(axis.Length - 1, -1, 0d);
            }
        }

        #endregion

        //TODO: Put this in Math3D
        public static Point3D BicubicInterpolation(BezierSegment3D top, BezierSegment3D bottom, BezierSegment3D left, BezierSegment3D right, double percentX, double percentY)
        {
            Point3D valueTop = BezierUtil.GetPoint(percentX, top);
            Point3D valueBottom = BezierUtil.GetPoint(percentX, bottom);

            Point3D valueLeft = BezierUtil.GetPoint(percentY, left);
            Point3D valueRight = BezierUtil.GetPoint(percentY, right);

            var points = new[]
                {
                    Tuple.Create(valueTop, 1 - percentY),
                    Tuple.Create(valueBottom, percentY),
                    Tuple.Create(valueLeft, 1 - percentX),
                    Tuple.Create(valueRight, percentX),
                };

            return Math3D.GetCenter(points);
        }
    }

    #endregion
}
