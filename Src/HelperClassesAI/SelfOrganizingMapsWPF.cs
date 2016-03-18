using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;

namespace Game.HelperClassesAI
{
    public static class SelfOrganizingMapsWPF
    {
        #region Class: BlobResult

        public class BlobEvents
        {
            public BlobEvents(Action<Polygon, SOMNode, ISOMInput[], MouseEventArgs> mouseMove, Action mouseLeave, Action<Polygon, SOMNode, ISOMInput[], MouseEventArgs> click)
            {
                this.MouseMove = mouseMove;
                this.MouseLeave = mouseLeave;
                this.Click = click;
            }

            public readonly Action<Polygon, SOMNode, ISOMInput[], MouseEventArgs> MouseMove;
            public readonly Action MouseLeave;

            public readonly Action<Polygon, SOMNode, ISOMInput[], MouseEventArgs> Click;
        }

        #endregion

        public static void ShowResults2D_Blobs(Border border, SOMResult result, Func<SOMNode, Color> getNodeColor, BlobEvents events = null)
        {
            #region validate
#if DEBUG
            if (!result.Nodes.All(o => o.Position.Length == 2))
            {
                throw new ArgumentException("Node positions need to be 2D");
            }
#endif
            #endregion

            Point[] points = result.Nodes.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            VoronoiResult2D voronoi = Math2D.GetVoronoi(points, true);
            voronoi = Math2D.CapVoronoiCircle(voronoi);

            Color[] colors = result.Nodes.
                Select(o => getNodeColor(o)).
                ToArray();

            //ISOMInput[][] inputsByNode = UtilityCore.ConvertJaggedArray<ISOMInput>(result.InputsByNode);

            Vector size = new Vector(border.ActualWidth - border.Padding.Left - border.Padding.Right, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            Canvas canvas = DrawVoronoiBlobs(voronoi, colors, result.Nodes, result.InputsByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor(), events);

            border.Child = canvas;
        }

        /// <summary>
        /// This divides the node's weights into thirds, and maps to RGB
        /// </summary>
        public static Color GetNodeColor(SOMNode node)
        {
            var raw = GetNodeColor_RGB(node);

            // Scale to bytes.  This assumes that the weights are normalized between -1 and 1
            //TODO: Handle negatives
            Color orig = Color.FromRgb(
                Convert.ToByte(UtilityCore.GetScaledValue_Capped(0, 255, 0, 1, Math.Abs(raw.Item1))),
                Convert.ToByte(UtilityCore.GetScaledValue_Capped(0, 255, 0, 1, Math.Abs(raw.Item2))),
                Convert.ToByte(UtilityCore.GetScaledValue_Capped(0, 255, 0, 1, Math.Abs(raw.Item3))));

            // Bring up the saturation
            ColorHSV hsv = UtilityWPF.RGBtoHSV(orig);
            return UtilityWPF.HSVtoRGB(hsv.H, Math.Max(hsv.S, 40), hsv.V);
        }

        #region Tooltip Builder

        public static Tuple<Rect, Vector> GetMouseCursorRect(double marginPercent = 0)
        {
            // Get system size
            double width = SystemParameters.CursorWidth;
            double height = SystemParameters.CursorHeight;

            // Offset (so the center will be center of the cursor, and not topleft)
            //NOTE: Doing this before margin is applied
            Vector offset = new Vector(width / 2d, height / 2d);

            // Apply margin
            width *= 1 + marginPercent;
            height *= 1 + marginPercent;

            // Build return
            double halfWidth = width / 2d;
            double halfHeight = height / 2d;
            Rect rect = new Rect(-halfWidth, -halfHeight, width, height);

            return Tuple.Create(rect, offset);
        }

        /// <summary>
        /// This looks long direction until a spot large enough to hold size is available
        /// </summary>
        /// <param name="size">The size of the rectangle to return</param>
        /// <param name="start">The point that the rectangle would like to be centered over</param>
        /// <param name="direction">
        /// The direction to slide the rectangle until an empty space is found
        /// NOTE: Set dir.Y to be negative to match screen coords
        /// </param>
        public static Rect GetFreeSpot(Size size, Point desiredPos, Vector direction, List<Rect> existing)
        {
            double halfWidth = size.Width / 2d;
            double halfHeight = size.Height / 2d;

            if (existing.Count == 0)
            {
                // There is nothing blocking this, center the rectangle over the position
                return new Rect(desiredPos.X - halfWidth, desiredPos.Y - halfHeight, size.Width, size.Height);
            }

            // Direction unit
            Vector dirUnit = direction.ToUnit();
            if (Math2D.IsInvalid(dirUnit))
            {
                dirUnit = Math3D.GetRandomVector_Circular_Shell(1).ToVector2D();
            }

            // Calculate step distance (5% of the average of all the sizes)
            double stepDist = UtilityCore.Iterate<Size>(size, existing.Select(o => o.Size)).
                SelectMany(o => new[] { o.Width, o.Height }).
                Average();

            stepDist *= .05;

            // Keep walking along direction until the rectangle doesn't intersect any existing rectangles
            Point point = new Point();
            Rect rect = new Rect();

            for (int cntr = 0; cntr < 5000; cntr++)
            {
                point = desiredPos + (dirUnit * (stepDist * cntr));
                rect = new Rect(point.X - halfWidth, point.Y - halfHeight, size.Width, size.Height);

                if (!existing.Any(o => o.IntersectsWith(rect)))
                {
                    break;
                }
            }

            return rect;
        }

        public static OutlinedTextBlock GetOutlineText(string text, double fontSize, double strokeThickness, bool invert = false)
        {
            return new OutlinedTextBlock()
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = FontWeight.FromOpenTypeWeight(900),
                StrokeThickness = strokeThickness,
                Fill = invert ? Brushes.Black : Brushes.White,
                Stroke = invert ? Brushes.White : Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        #endregion

        #region Event Listeners

        private static void Polygon2D_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                Polygon senderCast = sender as Polygon;
                if (senderCast == null)
                {
                    return;
                }

                var tag = senderCast.Tag as Tuple<BlobEvents, SOMNode, ISOMInput[]>;
                if (tag == null)
                {
                    return;
                }

                tag.Item1.MouseMove(senderCast, tag.Item2, tag.Item3, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Polygon2D_MouseMove", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static void Polygon2D_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                Polygon senderCast = sender as Polygon;
                if (senderCast == null)
                {
                    return;
                }

                var tag = senderCast.Tag as Tuple<BlobEvents, SOMNode, ISOMInput[]>;
                if (tag == null)
                {
                    return;
                }

                tag.Item1.MouseLeave();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Polygon2D_MouseLeave", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static void Polygon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if(e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                Polygon senderCast = sender as Polygon;
                if (senderCast == null)
                {
                    return;
                }

                var tag = senderCast.Tag as Tuple<BlobEvents, SOMNode, ISOMInput[]>;
                if (tag == null)
                {
                    return;
                }

                tag.Item1.Click(senderCast, tag.Item2, tag.Item3, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Polygon2D_MouseMove", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static Canvas DrawVoronoiBlobs(VoronoiResult2D voronoi, Color[] colors, SOMNode[] nodes, ISOMInput[][] inputsByNode, int imageWidth, int imageHeight, BlobEvents events)
        {
            const double MARGINPERCENT = 1.05;

            // Analyze size ratios
            double[] areas = AnalyzeVoronoiCellSizes(voronoi, inputsByNode);

            #region transform

            var aabb = Math2D.GetAABB(voronoi.EdgePoints);
            aabb = Tuple.Create((aabb.Item1.ToVector() * MARGINPERCENT).ToPoint(), (aabb.Item2.ToVector() * MARGINPERCENT).ToPoint());

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(-aabb.Item1.X, -aabb.Item1.Y));
            transform.Children.Add(new ScaleTransform(imageWidth / (aabb.Item2.X - aabb.Item1.X), imageHeight / (aabb.Item2.Y - aabb.Item1.Y)));

            #endregion

            Canvas retVal = new Canvas();
            retVal.Effect = new DropShadowEffect()
            {
                Color = Colors.Gray,
                Opacity = .2,
                BlurRadius = 5,
                Direction = 0,
                ShadowDepth = 0,
            };

            for (int cntr = 0; cntr < voronoi.ControlPoints.Length; cntr++)
            {
                #region polygon

                Polygon polygon = new Polygon();

                if (voronoi.EdgesByControlPoint[cntr].Length < 3)
                {
                    throw new ApplicationException("Expected at least three edge points");
                }

                Edge2D[] edges = voronoi.EdgesByControlPoint[cntr].Select(o => voronoi.Edges[o]).ToArray();
                Point[] edgePoints = Edge2D.GetPolygon(edges, 1d);

                // Resize to match the desired area
                edgePoints = ResizeConvexPolygon(edgePoints, areas[cntr]);

                // Convert into a smooth blob
                BezierSegment3D[] bezier = BezierUtil.GetBezierSegments(edgePoints.Select(o => o.ToPoint3D()).ToArray(), .25, true);
                edgePoints = BezierUtil.GetPath(75, bezier).
                    Select(o => o.ToPoint2D()).
                    ToArray();

                // Transform to canvas coords
                edgePoints = edgePoints.
                    Select(o => transform.Transform(o)).
                    ToArray();

                foreach (Point point in edgePoints)
                {
                    polygon.Points.Add(point);
                }

                polygon.Fill = new SolidColorBrush(colors[cntr]);
                polygon.Stroke = null; // new SolidColorBrush(UtilityWPF.OppositeColor(colors[cntr], false));
                polygon.StrokeThickness = 1;

                polygon.Tag = Tuple.Create(events, nodes[cntr], inputsByNode[cntr]);

                if (events != null)
                {
                    if (events.MouseMove != null && events.MouseLeave != null)
                    {
                        polygon.MouseMove += Polygon2D_MouseMove;
                        polygon.MouseLeave += Polygon2D_MouseLeave;
                    }

                    if(events.Click != null)
                    {
                        polygon.MouseUp += Polygon_MouseUp;
                    }
                }

                retVal.Children.Add(polygon);

                #endregion
            }

            return retVal;
        }

        private static double[] AnalyzeVoronoiCellSizes(VoronoiResult2D voronoi, ISOMInput[][] imagesByNode)
        {
            // Calculate area, density of each node
            var sizes = Enumerable.Range(0, voronoi.ControlPoints.Length).
                Select(o =>
                {
                    double area = Math2D.GetAreaPolygon(voronoi.GetPolygon(o, 1));        // there are no rays
                    double imageCount = imagesByNode[o].Length.ToDouble();

                    return new
                    {
                        ImagesCount = imageCount,
                        Area = area,
                        Density = imageCount / area,
                    };
                }).
                ToArray();

            // Don't let any node have an area smaller than this
            double minArea = sizes.Min(o => o.Area) * .2;

            // Find the node with the largest density.  This is the density to use when drawing all cells
            var largestDensity = sizes.
                OrderByDescending(o => o.Density).
                First();

            return sizes.Select(o =>
            {
                // Figure out how much area it would take using the highest density
                double area = o.ImagesCount / largestDensity.Density;
                if (area < minArea)
                {
                    area = minArea;
                }

                return area;
            }).
            ToArray();
        }

        private static Point[] ResizeConvexPolygon(Point[] polygon, double newArea)
        {
            Point center = Math2D.GetCenter(polygon);

            // Create a delagate that returns the area of the polygon based on the percent size
            Func<double, double> getOutput = new Func<double, double>(o =>
            {
                Point[] polyPoints = GetPolygon(center, polygon, o);
                return Math2D.GetAreaPolygon(polyPoints);
            });

            // Find a percent that returns the desired area
            double percent = Math1D.GetInputForDesiredOutput_PosInput_PosCorrelation(newArea, newArea * .01, getOutput);

            // Return the sized polygon
            return GetPolygon(center, polygon, percent);
        }

        private static Point[] GetPolygon(Point center, Point[] polygon, double percent)
        {
            return polygon.
                Select(o =>
                {
                    Vector displace = o - center;
                    displace *= percent;
                    return center + displace;
                }).
                ToArray();
        }

        /// <summary>
        /// This gets the raw values that will be used for RGB (could return negative)
        /// </summary>
        private static Tuple<double, double, double> GetNodeColor_RGB(SOMNode node)
        {
            if (node.Weights.Length == 0)
            {
                return Tuple.Create(0d, 0d, 0d);
            }
            else if (node.Weights.Length == 3)
            {
                return Tuple.Create(node.Weights[0], node.Weights[1], node.Weights[2]);
            }
            else if (node.Weights.Length < 3)
            {
                double left = node.Weights[0];
                double right = node.Weights[node.Weights.Length - 1];

                return Tuple.Create(left, Math1D.Avg(left, right), right);
            }

            int div = node.Weights.Length / 3;
            int rem = node.Weights.Length % 3;

            // Start them all off with the same amount
            int[] widths = Enumerable.Range(0, 3).
                Select(o => div).
                ToArray();

            // Randomly distribute the remainder
            foreach (int index in UtilityCore.RandomRange(0, 3, rem))
            {
                widths[index]++;
            }

            double r = node.Weights.
                Take(widths[0]).
                Average();

            double g = node.Weights.
                Skip(widths[0]).
                Take(widths[1]).
                Average();

            double b = node.Weights.
                Skip(widths[0] + widths[1]).
                Average();

            return Tuple.Create(r, g, b);
        }

        #endregion
    }
}
