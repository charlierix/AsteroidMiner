using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    #region Class: FlagVisual

    public class FlagVisual : FrameworkElement
    {
        #region Declaration Section

        private readonly DrawingVisual _visual;

        #endregion

        #region Constructor

        public FlagVisual(double width, double height, FlagProps props)
        {
            this.FlagProps = props;

            _visual = new DrawingVisual();
            using (DrawingContext dc = _visual.RenderOpen())
            {
                Size size = new Size(width, height);

                DrawBackground(dc, size, props);

                if (props.Overlay1 != null)
                {
                    DrawOverlay(dc, size, props.Overlay1);
                }

                if (props.Overlay2 != null)
                {
                    DrawOverlay(dc, size, props.Overlay2);
                }
            }
        }

        #endregion

        #region Overrides

        protected override Visual GetVisualChild(int index)
        {
            return _visual;
        }
        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        #endregion

        public readonly FlagProps FlagProps;

        #region Private Methods

        private static void DrawBackground(DrawingContext dc, Size size, FlagProps props)
        {
            double halfWidth = size.Width / 2d;
            double halfHeight = size.Height / 2d;
            Geometry geometry;

            switch (props.BackType)
            {
                case FlagBackType.Solid:
                    #region Solid

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(size));

                    #endregion
                    break;

                case FlagBackType.Horizontal_Two:
                    #region Horizontal_Two

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(0, 0, size.Width, halfHeight));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(0, halfHeight, size.Width, halfHeight));

                    #endregion
                    break;

                case FlagBackType.Horizontal_Three:
                    #region Horizontal_Three

                    double thirdHeight = size.Height / 3d;

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(0, 0, size.Width, thirdHeight));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(0, thirdHeight, size.Width, thirdHeight));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back3)), null, new Rect(0, thirdHeight * 2, size.Width, thirdHeight));

                    #endregion
                    break;

                case FlagBackType.Horizontal_Three_Thin:
                    #region Horizontal_Three_Thin

                    double midHeight1 = size.Height * .17;
                    double topBottHeight1 = (size.Height - midHeight1) / 2;

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(0, 0, size.Width, topBottHeight1));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(0, topBottHeight1, size.Width, midHeight1));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back3)), null, new Rect(0, topBottHeight1 + midHeight1, size.Width, topBottHeight1));

                    #endregion
                    break;

                case FlagBackType.Horizontal_Three_Thick:
                    #region Horizontal_Three_Thick

                    double midHeight2 = size.Height * .55;
                    double topBottHeight2 = (size.Height - midHeight2) / 2;

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(0, 0, size.Width, topBottHeight2));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(0, topBottHeight2, size.Width, midHeight2));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back3)), null, new Rect(0, topBottHeight2 + midHeight2, size.Width, topBottHeight2));

                    #endregion
                    break;

                case FlagBackType.Vertical_Two:
                    #region Vertical_Two

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(0, 0, halfWidth, size.Height));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(halfWidth, 0, halfWidth, size.Height));

                    #endregion
                    break;

                case FlagBackType.Vertical_Three:
                    #region Vertical_Three

                    double thirdWidth = size.Width / 3d;

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(0, 0, thirdWidth, size.Height));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(thirdWidth, 0, thirdWidth, size.Height));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back3)), null, new Rect(thirdWidth * 2, 0, thirdWidth, size.Height));

                    #endregion
                    break;

                case FlagBackType.Diagonal_Down:
                    #region Diagonal_Down

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(size.Width, size.Height),
                            new Point(0, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, geometry);

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(size.Width, 0),
                            new Point(size.Width, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, geometry);

                    #endregion
                    break;

                case FlagBackType.Diagonal_Up:
                    #region Diagonal_Up

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(size.Width, 0),
                            new Point(0, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, geometry);

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(size.Width, size.Height),
                            new Point(0, size.Height),
                            new Point(size.Width, 0),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, geometry);

                    #endregion
                    break;

                case FlagBackType.FourSquare:
                    #region FourSquare

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(0, 0, halfWidth, halfHeight));       // top left
                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, new Rect(halfWidth, halfHeight, halfWidth, halfHeight));      // bottom right

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(halfWidth, 0, halfWidth, halfHeight));       // top right
                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, new Rect(0, halfHeight, halfWidth, halfHeight));      // bottom left

                    #endregion
                    break;

                case FlagBackType.FourTriangle:
                    #region FourTriangle

                    // Top
                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(size.Width, 0),
                            new Point(halfWidth, halfHeight),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, geometry);

                    // Bottom
                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, size.Height),
                            new Point(size.Width, size.Height),
                            new Point(halfWidth, halfHeight),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back1)), null, geometry);

                    // Left
                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(halfWidth, halfHeight),
                            new Point(0, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, geometry);

                    // Right
                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(size.Width, 0),
                            new Point(size.Width, size.Height),
                            new Point(halfWidth, halfHeight),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(props.Back2)), null, geometry);

                    // It's nice not needing to worry about the normal :)

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown FlagBackType: " + props.BackType.ToString());
            }
        }

        private static void DrawOverlay(DrawingContext dc, Size size, FlagOverlay overlay)
        {
            double halfWidth = size.Width / 2d;
            double halfHeight = size.Height / 2d;
            double halfStripe = (Math.Min(size.Width, size.Height) * .15) / 2d;
            double smallCircleRadius = Math.Min(size.Width, size.Height) / 5;
            double smallCircleX = size.Width / 4;

            Geometry geometry;

            switch (overlay.Type)
            {
                case FlagOverlayType.Border:
                    #region Border

                    double borderSize = Math.Min(size.Width, size.Height) * .075;

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, 0, size.Width, borderSize));
                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, size.Height - borderSize, size.Width, borderSize));

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, borderSize, borderSize, size.Height - (borderSize * 2)));
                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(size.Width - borderSize, borderSize, borderSize, size.Height - (borderSize * 2)));

                    #endregion
                    break;

                case FlagOverlayType.Horizontal_One:
                    #region Horizontal_One

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, halfHeight - halfStripe, size.Width, halfStripe * 2));

                    #endregion
                    break;

                case FlagOverlayType.Horizontal_Two:
                    #region Horizontal_Two

                    double startY = size.Height - (halfStripe * 4);     // remaining height
                    startY /= 3;        // remaining split in 3
                    startY += halfStripe;

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, startY - halfStripe, size.Width, halfStripe * 2));
                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, size.Height - startY - halfStripe, size.Width, halfStripe * 2));

                    #endregion
                    break;

                case FlagOverlayType.Vertical_One:
                    #region Vertical

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(halfWidth - halfStripe, 0, halfStripe * 2, size.Height));

                    #endregion
                    break;

                case FlagOverlayType.Vertical_Two:
                    #region Vertical

                    double startX = size.Width - (halfStripe * 4);     // remaining width
                    startX /= 3;        // remaining split in 3
                    startX += halfStripe;

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(startX - halfStripe, 0, halfStripe * 2, size.Height));
                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(size.Width - startX - halfStripe, 0, halfStripe * 2, size.Height));

                    #endregion
                    break;

                case FlagOverlayType.Cross:
                    #region Cross

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, halfHeight - halfStripe, size.Width, halfStripe * 2));
                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(halfWidth - halfStripe, 0, halfStripe * 2, size.Height));

                    #endregion
                    break;

                case FlagOverlayType.Diagonal_Down:
                    #region Diagonal_Down

                    Vector intersects1 = GetStipeIntersect(size, halfStripe);

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(intersects1.X, 0),
                            new Point(size.Width, size.Height - intersects1.Y),
                            new Point(size.Width, size.Height),
                            new Point(size.Width - intersects1.X, size.Height),
                            new Point(0, intersects1.Y),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.Diagonal_Up:
                    #region Diagonal_Up

                    Vector intersects2 = GetStipeIntersect(size, halfStripe);

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, size.Height),
                            new Point(intersects2.X, size.Height),
                            new Point(size.Width, intersects2.Y),
                            new Point(size.Width, 0),
                            new Point(size.Width - intersects2.X, 0),
                            new Point(0, size.Height - intersects2.Y),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.X:
                    #region X

                    Vector intersects3 = GetStipeIntersect(size, halfStripe);

                    geometry = GetClosedGeometry(new[] 
                        {
                            // TL
                            new Point(0, intersects3.Y),
                            new Point(0, 0),
                            new Point(intersects3.X, 0),

                            new Point(halfWidth, halfHeight - intersects3.Y),

                            // TR
                            new Point(size.Width - intersects3.X, 0),
                            new Point(size.Width, 0),
                            new Point(size.Width, intersects3.Y),

                            new Point(halfWidth + intersects3.X, halfHeight),

                            // BR
                            new Point(size.Width, size.Height - intersects3.Y),
                            new Point(size.Width, size.Height),
                            new Point(size.Width - intersects3.X, size.Height),

                            new Point(halfWidth, halfHeight + intersects3.Y),

                            // BL
                            new Point(intersects3.X, size.Height),
                            new Point(0, size.Height),
                            new Point(0, size.Height - intersects3.Y),

                            new Point(halfWidth - intersects3.X, halfHeight),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.Triangle_Left:
                    #region Triangle_Left

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(halfWidth, halfHeight),
                            new Point(0, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.Triangle_Top:
                    #region Triangle_Top

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, 0),
                            new Point(halfWidth, halfHeight),
                            new Point(size.Width, 0),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.Triangle_Right:
                    #region Triangle_Right

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(size.Width, 0),
                            new Point(halfWidth, halfHeight),
                            new Point(size.Width, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.Triangle_Bottom:
                    #region Triangle_Bottom

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, size.Height),
                            new Point(halfWidth, halfHeight),
                            new Point(size.Width, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.Circle_Large:
                    #region Circle_Large

                    double radius1 = Math.Min(size.Width, size.Height) / 3;

                    dc.DrawEllipse(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Point(halfWidth, halfHeight), radius1, radius1);

                    #endregion
                    break;

                case FlagOverlayType.Circle_Small_Left:
                    #region Circle_Small_Left

                    dc.DrawEllipse(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Point(smallCircleX, halfHeight), smallCircleRadius, smallCircleRadius);

                    #endregion
                    break;

                case FlagOverlayType.Circle_Small_Right:
                    #region Circle_Small_Right

                    dc.DrawEllipse(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Point(size.Width - smallCircleX, halfHeight), smallCircleRadius, smallCircleRadius);

                    #endregion
                    break;

                case FlagOverlayType.Circle_Small_Two:
                    #region Circle_Small_Two

                    dc.DrawEllipse(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Point(smallCircleX, halfHeight), smallCircleRadius, smallCircleRadius);
                    dc.DrawEllipse(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Point(size.Width - smallCircleX, halfHeight), smallCircleRadius, smallCircleRadius);

                    #endregion
                    break;

                case FlagOverlayType.Diamond:
                    #region Diamond

                    geometry = GetClosedGeometry(new[] 
                        {
                            new Point(0, halfHeight),
                            new Point(halfWidth, 0),
                            new Point(size.Width, halfHeight),
                            new Point(halfWidth, size.Height),
                        });

                    dc.DrawGeometry(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, geometry);

                    #endregion
                    break;

                case FlagOverlayType.CornerRect:
                    #region CornerRect

                    dc.DrawRectangle(new SolidColorBrush(UtilityWPF.ColorFromHex(overlay.Color)), null, new Rect(0, 0, halfWidth, halfHeight));

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown FlagOverlayType: " + overlay.Type.ToString());
            }
        }

        private static Vector GetStipeIntersect(Size size, double halfStripe)
        {
            // Get the angle from corner to corner
            // tan(theta) = rise / run
            double radians = Math.Atan(size.Height / size.Width);
            double compRadians = (Math.PI / 2) - radians;

            // sin(theta) = rise / hyp
            double x = halfStripe / Math.Sin(radians);
            double y = halfStripe / Math.Sin(compRadians);

            if (size.Width > size.Height)
            {
                return new Vector(x, y);
            }
            else
            {
                return new Vector(y, x);
            }
        }

        /// <summary>
        /// This creates a filled geometry.  This will connect the last point with the first
        /// </summary>
        private static Geometry GetClosedGeometry(IEnumerable<Point> points)
        {
            //https://msdn.microsoft.com/en-us/library/ms742199(v=vs.110).aspx

            StreamGeometry retVal = new StreamGeometry();

            //NOTE: The figure is set to closed, so the line from last to first is implied
            retVal.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext context = retVal.Open())
            {
                bool isFirst = true;
                foreach (Point point in points)
                {
                    if (isFirst)
                    {
                        context.BeginFigure(point, true, true);
                        isFirst = false;
                    }
                    else
                    {
                        context.LineTo(point, true, false);
                    }
                }
            }

            retVal.Freeze();

            return retVal;
        }

        #endregion
    }

    #endregion

    #region Class: FlagProps

    public class FlagProps
    {
        public FlagBackType BackType { get; set; }

        // Colors in hex format
        public string Back1 { get; set; }
        public string Back2 { get; set; }
        public string Back3 { get; set; }

        public FlagOverlay Overlay1 { get; set; }
        public FlagOverlay Overlay2 { get; set; }
    }

    #endregion
    #region Class: FlagOverlay

    public class FlagOverlay
    {
        public FlagOverlayType Type { get; set; }
        public string Color { get; set; }
    }

    #endregion

    #region Enum: FlagBackType

    public enum FlagBackType
    {
        Solid,      // one color
        Horizontal_Two,     // two
        Horizontal_Three,       // three
        Horizontal_Three_Thin,       // three (center stripe is thin)
        Horizontal_Three_Thick,       // three (center stripe is thick)
        Vertical_Two,     // two
        Vertical_Three,       // three
        Diagonal_Down,      // two
        Diagonal_Up,        // two
        FourSquare,     // two
        FourTriangle,       // two
    }

    #endregion
    #region Enum: FlagOverlayType

    public enum FlagOverlayType
    {
        Border,
        Horizontal_One,
        Horizontal_Two,
        Vertical_One,
        Vertical_Two,
        Cross,
        Diagonal_Down,
        Diagonal_Up,
        X,
        Triangle_Left,
        Triangle_Top,
        Triangle_Right,
        Triangle_Bottom,
        Circle_Large,
        Circle_Small_Left,
        Circle_Small_Right,
        Circle_Small_Two,
        Diamond,
        CornerRect,
    }

    #endregion

    #region Class: FlagGenerator

    public static class FlagGenerator
    {
        #region Enum: FlagColorCategory

        public enum FlagColorCategory
        {
            White,
            Black,
            Bold,
            Pastel,
            Gray,
            Dark,
            Other,      // basically variants of gray
        }

        #endregion
        #region Class: FlagColorCategoriesCache

        public class FlagColorCategoriesCache
        {
            #region Declaration Section

            private readonly int _maxFlagColors;

            private readonly FlagColorCategory[] _ignoreHues = new[] { FlagColorCategory.Black, FlagColorCategory.Gray, FlagColorCategory.White };

            #endregion

            #region Constructor

            public FlagColorCategoriesCache()
            {
                // X is saturation
                // Y is value
                this.Categories = new[]
                {
                    Tuple.Create(new Rect(0, 0, 100, 15), FlagColorCategory.Black),     //if (color.V < 15) return ColorCategory.Black;
                    Tuple.Create(new Rect(0, 96, 4, 100-96), FlagColorCategory.White),       //else if (color.V > 96 && color.S < 4) return ColorCategory.White;
                    Tuple.Create(new Rect(25, 77, 50-25, 91-77), FlagColorCategory.Pastel),     //else if (color.V > 77 && color.V < 91 && color.S > 25 && color.S < 50) return ColorCategory.Pastel;
                    Tuple.Create(new Rect(60, 60, 100-60, 100-60), FlagColorCategory.Bold),       //else if (color.V > 60 && color.S > 60) return ColorCategory.Bold;
                    Tuple.Create(new Rect(66, 15, 100-66, 40-15), FlagColorCategory.Dark),       //else if (color.V < 40 && color.S > 66) return ColorCategory.Dark;
                    Tuple.Create(new Rect(0, 40, 10, 70-40), FlagColorCategory.Gray),       //else if (color.V > 40 && color.V < 70 && color.S < 10) return ColorCategory.Gray;
                    Tuple.Create(new Rect(0, 0, 100, 100), FlagColorCategory.Other),       //else return ColorCategory.Other;
                };

                this.MaxPerFlag = BuildMaxPerFlag(new[]
                {
                    Tuple.Create(new[] { FlagColorCategory.Black, FlagColorCategory.White }, 2),
                    Tuple.Create(new[] { FlagColorCategory.Bold }, 3),
                    Tuple.Create(new[] { FlagColorCategory.Pastel/*, FlagColorCategory.Gray*/ }, 1),        // gray doesn't look very good as a flag
                });

                _maxFlagColors = this.MaxPerFlag.Sum(o => o.Item2);
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// This categorizes colors
            /// rect.X is saturation
            /// rect.Y is value
            /// </summary>
            public readonly Tuple<Rect, FlagColorCategory>[] Categories;

            /// <summary>
            /// This tells the maximum of each color category that can be in a flag
            /// </summary>
            /// <remarks>
            /// Item1=Color categories that make up this group
            /// Item2=The max amount of times this category can be in a flag
            /// Item3,4=from/to percent (cached helper values that so that a random number from 0 to 1 can be chosen, then look for the group where: from LTE % LT to)
            /// </remarks>
            public readonly Tuple<FlagColorCategory[], int, double, double>[] MaxPerFlag;

            #endregion

            #region Public Methods

            public FlagColorCategory Categorize(ColorHSV color)
            {
                Point colorPoint = new Point(color.S, color.V);

                //NOTE: The rectangle could overlap, but the array is built in order with the intention that the first to match wins
                foreach (var category in this.Categories)
                {
                    if (category.Item1.Contains(colorPoint))
                    {
                        return category.Item2;
                    }
                }

                throw new ApplicationException("Should have found one");
            }
            public ColorHSV GetRandomColor(FlagColorCategory category, double? hue = null)
            {
                Random rand = StaticRandom.GetRandomForThread();

                double hueActual = hue ?? rand.NextDouble(360);

                ColorHSV retVal;

                if (category == FlagColorCategory.Other)
                {
                    // This category is under other category rectangles, so do a brute force repeat until match
                    FlagColorCategory cat;
                    do
                    {
                        retVal = new ColorHSV(hueActual, rand.NextDouble(100), rand.NextDouble(100));
                        cat = Categorize(retVal);
                    } while (cat != category);
                }
                else
                {
                    // The color range is stored as a rectangle (X is saturation, Y is value)
                    Rect catRect = this.Categories.First(o => o.Item2 == category).Item1;
                    retVal = new ColorHSV(hueActual, rand.NextDouble(catRect.Left, catRect.Right), rand.NextDouble(catRect.Top, catRect.Bottom));
                }

                return retVal;
            }

            public ColorHSV[] GetRandomFlagColors(int count, IEnumerable<ColorHSV> existing = null)
            {
                if (count > _maxFlagColors)
                {
                    throw new ArgumentOutOfRangeException(string.Format("Can't create that many colors: {0}.  Max allowed: {1}", count.ToString(), _maxFlagColors.ToString()));
                }

                ColorHSV[] retVal = new ColorHSV[count];
                var categorized = new List<Tuple<ColorHSV, FlagColorCategory>>();

                GetRandomFlagColors_LoadExisting(existing, retVal, categorized);

                int index = categorized.Count;
                for (int cntr = 0; cntr < count; cntr++)
                {
                    var color = GetRandomFlagColors_Next(categorized);

                    retVal[cntr] = color.Item1;
                    categorized.Add(color);
                }

                return retVal;
            }

            #endregion

            #region Private Methods

            private void GetRandomFlagColors_LoadExisting(IEnumerable<ColorHSV> existing, ColorHSV[] retVal, List<Tuple<ColorHSV, FlagColorCategory>> categorized)
            {
                if (existing == null)
                {
                    return;
                }

                int index = 0;

                foreach (ColorHSV color in existing)
                {
                    if (index >= retVal.Length)
                    {
                        break;
                    }

                    retVal[index] = color;
                    categorized.Add(Tuple.Create(color, Categorize(color)));

                    index++;
                }
            }

            private Tuple<ColorHSV, FlagColorCategory> GetRandomFlagColors_Next(List<Tuple<ColorHSV, FlagColorCategory>> existing)
            {
                // Figure out which category group to use
                var group = GetRandomFlagColors_Group(existing);

                // Pick a category out of this group
                FlagColorCategory category = GetRandomFlagColors_Category(this.MaxPerFlag[group.Item1].Item1, group.Item2);

                // Get a color from this category
                ColorHSV color = GetRandomFlagColors_Color(category, existing);

                return Tuple.Create(color, category);
            }

            private Tuple<int, Tuple<ColorHSV, FlagColorCategory>[]> GetRandomFlagColors_Group(List<Tuple<ColorHSV, FlagColorCategory>> existing)
            {
                int groupIndex = -1;
                Tuple<ColorHSV, FlagColorCategory>[] existingOfGroup = null;

                Random rand = StaticRandom.GetRandomForThread();

                while (true)
                {
                    // Pick a random group
                    double percent = rand.NextDouble();

                    for (int cntr = 0; cntr < this.MaxPerFlag.Length; cntr++)
                    {
                        if (percent >= this.MaxPerFlag[cntr].Item3 && percent < this.MaxPerFlag[cntr].Item4)
                        {
                            groupIndex = cntr;
                            break;
                        }
                    }

                    if (groupIndex < 0)
                    {
                        throw new ApplicationException("Couldn't find a category group");
                    }

                    // See if there are open slots in this group
                    existingOfGroup = existing.
                        Where(o => this.MaxPerFlag[groupIndex].Item1.Contains(o.Item2)).
                        ToArray();

                    if (existingOfGroup.Length < this.MaxPerFlag[groupIndex].Item2)
                    {
                        break;
                    }
                }

                return Tuple.Create(groupIndex, existingOfGroup);
            }

            private static FlagColorCategory GetRandomFlagColors_Category(FlagColorCategory[] chooseFrom, Tuple<ColorHSV, FlagColorCategory>[] existing)
            {
                if (chooseFrom.Length == 1)
                {
                    return chooseFrom[0];
                }

                Random rand = StaticRandom.GetRandomForThread();
                int infiniteLoopDetector = 0;

                // There are more than one to choose from.  Pick one that hasn't been picked before
                // For example { Black, White }, and White is used.  Need to return Black
                int index = -1;
                while (true)
                {
                    index = rand.Next(chooseFrom.Length);

                    if (!existing.Any(o => o.Item2 == chooseFrom[index]))
                    {
                        return chooseFrom[index];
                    }

                    infiniteLoopDetector++;
                    if (infiniteLoopDetector > 1000)
                    {
                        throw new ApplicationException("Infinite loop detected");
                    }
                }
            }

            private ColorHSV GetRandomFlagColors_Color(FlagColorCategory category, List<Tuple<ColorHSV, FlagColorCategory>> existing)
            {
                if (_ignoreHues.Contains(category))
                {
                    return GetRandomColor(category);        // don't worry about hue
                }

                double[] existingHues = existing.
                    Where(o => !_ignoreHues.Contains(o.Item2)).
                    Select(o => o.Item1.H).
                    ToArray();


                //TODO: Use the existing hues to try to influence the next one:
                //      only allow 30 degree increments
                //      give a higher chance to the slots farthest away from existing


                double hue = GetRandomFlagColors_Hue(existingHues);

                return GetRandomColor(category, hue);
            }

            private static double GetRandomFlagColors_Hue(double[] existing)
            {
                Random rand = StaticRandom.GetRandomForThread();

                double retVal = 0;

                while (true)
                {
                    retVal = rand.NextDouble(360);

                    if (!existing.Any(o => UtilityWPF.GetHueDistance(o, retVal) < 60))
                    {
                        break;
                    }
                }

                return retVal;
            }

            /// <summary>
            /// This tacks on from-to percent
            /// </summary>
            private static Tuple<FlagColorCategory[], int, double, double>[] BuildMaxPerFlag(Tuple<FlagColorCategory[], int>[] groups)
            {
                double sumEnumValues = groups.Sum(o => o.Item1.Length);

                double sumPercent = 0;
                var retVal = new Tuple<FlagColorCategory[], int, double, double>[groups.Length];
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    double percent = groups[cntr].Item1.Length / sumEnumValues;

                    retVal[cntr] = Tuple.Create(groups[cntr].Item1, groups[cntr].Item2, sumPercent, sumPercent + percent);

                    sumPercent += percent;
                }

                return retVal;
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// These are similar enough to each other that they shouldn't be combined
        /// NOTE: FlagBackType may be empty
        /// </summary>
        /// <remarks>
        /// TODO: May want to add vertical stripes, but the flag is wide enough that even the busiest combo isn't terrible (busy, but not terrible)
        /// </remarks>
        private static Lazy<Tuple<FlagOverlayType[], FlagBackType[]>[]> _similarOptions = new Lazy<Tuple<FlagOverlayType[], FlagBackType[]>[]>(() =>
            new[]
            {
                Tuple.Create(new[] { FlagOverlayType.Horizontal_One, FlagOverlayType.Horizontal_Two }, new FlagBackType[0]),
                Tuple.Create(new[] { FlagOverlayType.Horizontal_One, FlagOverlayType.Cross }, new[] { FlagBackType.Horizontal_Three, FlagBackType.Horizontal_Three_Thin }),
                Tuple.Create(new[] { FlagOverlayType.Horizontal_Two }, new[] { FlagBackType.Horizontal_Three_Thin, FlagBackType.Horizontal_Three_Thick, FlagBackType.Vertical_Three }),
                Tuple.Create(new[] { FlagOverlayType.Vertical_Two }, new[] { FlagBackType.Horizontal_Three, FlagBackType.Horizontal_Three_Thin, FlagBackType.Horizontal_Three_Thick, FlagBackType.FourSquare }),

                Tuple.Create(new[] { FlagOverlayType.Diagonal_Down, FlagOverlayType.X }, new FlagBackType[0]),
                Tuple.Create(new[] { FlagOverlayType.Diagonal_Up, FlagOverlayType.X }, new FlagBackType[0]),
            });

        /// <summary>
        /// These are overlays that can't be placed on top of others
        /// (Item1 can't be on top of Item2)
        /// </summary>
        private static Lazy<Tuple<FlagOverlayType[], FlagOverlayType[]>[]> _notAbove = new Lazy<Tuple<FlagOverlayType[], FlagOverlayType[]>[]>(() =>
            new[] 
            {
                Tuple.Create(new[] { FlagOverlayType.Horizontal_One, FlagOverlayType.Horizontal_Two, FlagOverlayType.Vertical_One, FlagOverlayType.Vertical_Two, FlagOverlayType.Cross, FlagOverlayType.Diagonal_Down, FlagOverlayType.Diagonal_Up, FlagOverlayType.X, FlagOverlayType.Diamond }, new[] { FlagOverlayType.Border }),

                Tuple.Create(new[] { FlagOverlayType.Triangle_Left, FlagOverlayType.Triangle_Top, FlagOverlayType.Triangle_Right, FlagOverlayType.Triangle_Bottom }, new[] { FlagOverlayType.Diagonal_Down, FlagOverlayType.Diagonal_Up, FlagOverlayType.X }),

                Tuple.Create(new[] { FlagOverlayType.CornerRect }, new[] { FlagOverlayType.Horizontal_One, FlagOverlayType.Vertical_One, FlagOverlayType.Cross, FlagOverlayType.Circle_Large, FlagOverlayType.Circle_Small_Left, FlagOverlayType.Circle_Small_Two }),

                Tuple.Create(new[] { FlagOverlayType.Horizontal_One, FlagOverlayType.Horizontal_Two, FlagOverlayType.Vertical_Two, FlagOverlayType.Cross, FlagOverlayType.Diagonal_Down, FlagOverlayType.Diagonal_Up, FlagOverlayType.X }, new[] { FlagOverlayType.Circle_Large, FlagOverlayType.Circle_Small_Left, FlagOverlayType.Circle_Small_Right, FlagOverlayType.Circle_Small_Two }),
                Tuple.Create(new[] { FlagOverlayType.Vertical_One }, new[] { FlagOverlayType.Circle_Large }),

                Tuple.Create(new[] { FlagOverlayType.Diamond }, new[] { FlagOverlayType.Horizontal_One, FlagOverlayType.Vertical_One, FlagOverlayType.Cross }),

                Tuple.Create(new[] { FlagOverlayType.Vertical_Two }, new[] { FlagOverlayType.Horizontal_One }),
                Tuple.Create(new[] { FlagOverlayType.Vertical_One, FlagOverlayType.Vertical_Two, FlagOverlayType.Diagonal_Down, FlagOverlayType.Diagonal_Up, FlagOverlayType.X }, new[] { FlagOverlayType.Horizontal_Two }),
                Tuple.Create(new[] { FlagOverlayType.Horizontal_Two }, new[] { FlagOverlayType.Vertical_One }),
                Tuple.Create(new[] { FlagOverlayType.Horizontal_One, FlagOverlayType.Horizontal_Two, FlagOverlayType.Diagonal_Down, FlagOverlayType.Diagonal_Up, FlagOverlayType.X }, new[] { FlagOverlayType.Vertical_Two }),

                //Tuple.Create(new[] { FlagOverlayType., FlagOverlayType. }, new[] { FlagOverlayType., FlagOverlayType. }),
            });

        private static Lazy<FlagColorCategoriesCache> _colorCategories = new Lazy<FlagColorCategoriesCache>(() => new FlagColorCategoriesCache());

        #endregion

        public static FlagProps GetRandomFlag()
        {
            var enums = GetRandomEnums();
            var colors = GetRandomColors();

            FlagProps retVal = new FlagProps()
            {
                BackType = enums.Item1,
                Back1 = colors[0].ToRGB().ToHex(false, false),
                Back2 = colors[1].ToRGB().ToHex(false, false),
                Back3 = colors[2].ToRGB().ToHex(false, false),
            };

            if (enums.Item2 != null)
            {
                retVal.Overlay1 = new FlagOverlay()
                {
                    Type = enums.Item2.Value,
                    Color = colors[3].ToRGB().ToHex(false, false),
                };
            }

            if (enums.Item3 != null)
            {
                retVal.Overlay2 = new FlagOverlay()
                {
                    Type = enums.Item3.Value,
                    Color = colors[4].ToRGB().ToHex(false, false),
                };
            }

            return retVal;
        }

        public static Tuple<FlagBackType, FlagOverlayType?, FlagOverlayType?> GetRandomEnums()
        {
            Random rand = StaticRandom.GetRandomForThread();

            double overlayProb = rand.NextDouble();

            // Choose a background
            FlagBackType backType = UtilityCore.GetRandomEnum<FlagBackType>();

            FlagOverlayType[] backTypeFilter = _similarOptions.Value.
                    Where(o => o.Item2.Any(p => p == backType)).
                    SelectMany(o => o.Item1).
                    ToArray();

            // Choose overlay one
            FlagOverlayType? overlayType1 = null;
            if (overlayProb > .33)
            {
                overlayType1 = UtilityCore.GetRandomEnum<FlagOverlayType>(backTypeFilter);
            }

            // Choose overlay two
            FlagOverlayType? overlayType2 = null;
            if (overlayProb > .66)
            {
                var test = _notAbove.Value.
                    Where(o => o.Item2.Any(p => p == overlayType1.Value)).
                    SelectMany(o => o.Item1).
                    ToArray();


                IEnumerable<FlagOverlayType> filter2 = UtilityCore.Iterate(
                    new[] { overlayType1.Value },
                    backTypeFilter,
                    _similarOptions.Value.Where(o => o.Item1.Any(p => p == overlayType1.Value)).SelectMany(o => o.Item1),
                    _notAbove.Value.Where(o => o.Item2.Any(p => p == overlayType1.Value)).SelectMany(o => o.Item1)
                    );

                overlayType2 = UtilityCore.GetRandomEnum<FlagOverlayType>(filter2);
            }

            return Tuple.Create(backType, overlayType1, overlayType2);
        }

        /// <summary>
        /// This always returns 5 colors (the max amount that a flag can have)
        /// </summary>
        public static ColorHSV[] GetRandomColors()
        {
            return _colorCategories.Value.GetRandomFlagColors(5);
        }
    }

    #endregion
}
