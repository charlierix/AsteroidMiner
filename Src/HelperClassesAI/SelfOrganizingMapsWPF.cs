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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;

namespace Game.HelperClassesAI
{
    public static class SelfOrganizingMapsWPF
    {
        #region Class: BlobEvents

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
        #region Class: DrawTileArgs

        public class DrawTileArgs
        {
            public DrawTileArgs(ISOMInput tile, int tileWidth, int tileHeight, byte[] bitmapPixelBytes, int imageX, int imageY, int stride, int pixelWidth)
            {
                this.Tile = tile;
                this.TileWidth = tileWidth;
                this.TileHeight = tileHeight;
                this.BitmapPixelBytes = bitmapPixelBytes;
                this.ImageX = imageX;
                this.ImageY = imageY;
                this.Stride = stride;
                this.PixelWidth = pixelWidth;
            }

            public readonly ISOMInput Tile;

            public readonly int TileWidth;
            public readonly int TileHeight;

            /// <summary>
            /// Each pixel is 4 bytes: BGRA
            /// </summary>
            public readonly byte[] BitmapPixelBytes;

            public readonly int ImageX;
            public readonly int ImageY;

            //See UtilityWPF.GetBitmap for example usage
            public readonly int Stride;
            public readonly int PixelWidth;
        }

        #endregion

        /// <summary>
        /// This creates solid colored blobs with areas proportional to the number of items contained.  When the user
        /// mouses over a blob, the caller can show examples of the items as tooltips
        /// </summary>
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
            Canvas canvas = DrawVoronoi_Blobs(voronoi, colors, result.Nodes, result.InputsByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor(), events);

            border.Child = canvas;
        }

        /// <summary>
        /// This divides the border up into a voronoi, then each node is tiled with examples
        /// </summary>
        public static void ShowResults2D_Tiled(Border border, SOMResult result, int tileWidth, int tileHeight, Action<DrawTileArgs> drawTile, BlobEvents events = null)
        {

            //TODO: Take a func that will render the input onto a writable bitmap, or something dynamic but efficient?
            // or take these in?
            //int tileWidth, int tileHeight



            Point[] points = result.Nodes.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            Vector size = new Vector(border.ActualWidth - border.Padding.Left - border.Padding.Right, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);

            VoronoiResult2D voronoi = Math2D.GetVoronoi(points, true);
            voronoi = Math2D.CapVoronoiCircle(voronoi);
            //voronoi = Math2D.CapVoronoiRectangle(voronoi, aspectRatio: 1d);       //TODO: Implement this

            Canvas canvas = DrawVoronoi_Tiled(voronoi, result.Nodes, result.InputsByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor(), tileWidth, tileHeight, drawTile, events);

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
                if (e.ChangedButton != MouseButton.Left)
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

        private static Canvas DrawVoronoi_Blobs(VoronoiResult2D voronoi, Color[] colors, SOMNode[] nodes, ISOMInput[][] inputsByNode, int imageWidth, int imageHeight, BlobEvents events)
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

                    if (events.Click != null)
                    {
                        polygon.MouseUp += Polygon_MouseUp;
                    }
                }

                retVal.Children.Add(polygon);

                #endregion
            }

            return retVal;
        }

        private static Canvas DrawVoronoi_Tiled(VoronoiResult2D voronoi, SOMNode[] nodes, ISOMInput[][] images, int imageWidth, int imageHeight, int tileWidth, int tileHeight, Action<DrawTileArgs> drawTile, BlobEvents events)
        {
            #region transform

            var aabb = Math2D.GetAABB(voronoi.EdgePoints);

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(-aabb.Item1.X, -aabb.Item1.Y));
            transform.Children.Add(new ScaleTransform(imageWidth / (aabb.Item2.X - aabb.Item1.X), imageHeight / (aabb.Item2.Y - aabb.Item1.Y)));

            #endregion

            Canvas retVal = new Canvas();

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

                edgePoints = edgePoints.
                    Select(o => transform.Transform(o)).
                    ToArray();

                foreach (Point point in edgePoints)
                {
                    polygon.Points.Add(point);
                }

                polygon.Fill = GetTiledSamples(edgePoints, images[cntr], nodes[cntr], tileWidth, tileHeight, drawTile);
                polygon.Stroke = new SolidColorBrush(UtilityWPF.GetRandomColor(64, 192));
                polygon.StrokeThickness = 2;

                polygon.Tag = Tuple.Create(nodes[cntr], images[cntr]);

                if (events != null)
                {
                    if (events.MouseMove != null && events.MouseLeave != null)
                    {
                        polygon.MouseMove += Polygon2D_MouseMove;
                        polygon.MouseLeave += Polygon2D_MouseLeave;
                    }

                    if (events.Click != null)
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

        private static Brush GetTiledSamples_ONE(Point[] edgePoints, ISOMInput[] samples)
        {
            var aabb = Math2D.GetAABB(edgePoints);

            var colors = Enumerable.Range(0, 256).
                Select(o => UtilityWPF.GetRandomColor(64, 192)).
                ToArray();

            //BitmapSource bitmap = UtilityWPF.GetBitmap_Aliased(colors, 16, 16, 300, 300);
            BitmapSource bitmap = UtilityWPF.GetBitmap_Aliased(colors, 16, 16, (aabb.Item2.X - aabb.Item1.X).ToInt_Round(), (aabb.Item2.Y - aabb.Item1.Y).ToInt_Round());

            return new ImageBrush(bitmap)
            {
                Stretch = Stretch.None,
            };
        }
        private static Brush GetTiledSamples(Point[] edgePoints, ISOMInput[] samples, SOMNode node, int tileWidth, int tileHeight, Action<DrawTileArgs> drawTile)
        {
            var dimensions = GetTileImagePositions(samples.Length);

            // The image tiles will be drawn spiraling out from the center.  Order the list so that tiles closest to the node are first (so that
            // they are drawn closer to the center of the spiral)
            ISOMInput[] orderedSamples = samples.
                OrderBy(o => MathND.GetDistanceSquared(o.Weights, node.Weights)).
                ToArray();

            //int tilehalf_left = tileWidth / 2;
            //int tilehalf_top = tileHeight / 2;
            //int tilehalf_right = tileWidth - tilehalf_left;
            //int tilehalf_bot = tileHeight - tilehalf_top;

            int imageWidth = (dimensions.Item2.X - dimensions.Item1.X + 1) * tileWidth;
            int imageHeight = (dimensions.Item2.Y - dimensions.Item1.Y + 1) * tileHeight;

            //int offsetX = (Math.Abs(dimensions.Item1.X) * tileWidth) + tilehalf_left;
            //int offsetY = (Math.Abs(dimensions.Item1.Y) * tileHeight) + tilehalf_top;


            //TODO: Get the AABB of edgePoints.  If the bitmap will be bigger than the aabb, then draw just enough to totally fill the polygon



            //NOTE: Copied from UtilityWPF.GetBitmap

            WriteableBitmap bitmap = new WriteableBitmap(imageWidth, imageHeight, UtilityWPF.DPI, UtilityWPF.DPI, PixelFormats.Pbgra32, null);      // may want Bgra32 if performance is an issue

            int pixelWidth = bitmap.Format.BitsPerPixel / 8;
            int stride = bitmap.PixelWidth * pixelWidth;      // this is the length of one row of pixels

            byte[] pixels = new byte[bitmap.PixelHeight * stride];

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                for (int cntr = 0; cntr < orderedSamples.Length; cntr++)
                {
                    int x = (dimensions.Item3[cntr].X - dimensions.Item1.X) * tileWidth;
                    int y = (dimensions.Item3[cntr].Y - dimensions.Item1.Y) * tileWidth;

                    DrawTileArgs args = new DrawTileArgs(orderedSamples[cntr], tileWidth, tileHeight, pixels, x, y, stride, pixelWidth);
                    drawTile(args);
                }




                #region DISCARD

                //int index = 0;
                //for (int y = 0; y < colorsHeight; y++)
                //{
                //    for (int x = 0; x < colorsWidth; x++)
                //    {
                //        int gray = (grayColors[index] * grayValueScale).ToInt_Round();
                //        if (gray < 0) gray = 0;
                //        if (gray > 255) gray = 255;
                //        byte grayByte = Convert.ToByte(gray);
                //        Color color = Color.FromRgb(grayByte, grayByte, grayByte);
                //        ctx.DrawRectangle(new SolidColorBrush(color), null, new Rect(x * scaleX, y * scaleY, scaleX, scaleY));
                //        index++;
                //    }
                //}

                #endregion
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixels, stride, 0);



            return new ImageBrush(bitmap)
            {
                Stretch = Stretch.None,
            };
        }

        /// <summary>
        /// This returns the coordinates of tiles.  The tiles spiral outward: right,up,left,down,right,up...
        /// </summary>
        /// <returns>
        /// Item1=aabb min
        /// Item2=aabb max
        /// Item3=positions
        /// </returns>
        private static Tuple<VectorInt, VectorInt, VectorInt[]> GetTileImagePositions(int count)
        {
            int x = 0;
            int y = 0;

            int minX = 0;
            int minY = 0;
            int maxX = 0;
            int maxY = 0;

            int dirX = 1;
            int dirY = 0;

            VectorInt[] positions = new VectorInt[count];

            for (int cntr = 0; cntr < count; cntr++)
            {
                positions[cntr] = new VectorInt(x, y);

                if (cntr >= count - 1)
                {
                    // The logic below preps for the next step, but if this is the last one, leave mins and maxes alone
                    break;
                }

                x += dirX;
                y += dirY;

                if (x > maxX)
                {
                    maxX = x;
                    dirX = 0;
                    dirY = -1;
                }
                else if (x < minX)
                {
                    minX = x;
                    dirX = 0;
                    dirY = 1;
                }
                else if (y > maxY)
                {
                    maxY = y;
                    dirY = 0;
                    dirX = 1;
                }
                else if (y < minY)
                {
                    minY = y;
                    dirY = 0;
                    dirX = -1;
                }
            }

            return Tuple.Create(new VectorInt(minX, minY), new VectorInt(maxX, maxY), positions);
        }

        #endregion
    }
}
