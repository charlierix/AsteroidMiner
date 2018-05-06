using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    // Used XDraw to make these (copied the export into c#)
    // https://xdraw.codeplex.com/
    public static class PartCategoryIcons
    {
        public static UIElement GetIcon(string tabName, string category, Brush brushPrimary, Brush brushSecondary, double size = 24)
        {
            if (tabName != PartToolItemBase.TAB_SHIPPART)
            {
                throw new ApplicationException("Unknown tab name: " + tabName);
            }

            DrawingImage image;

            switch (category)
            {
                case PartToolItemBase.CATEGORY_CONTAINER:
                    image = GetIcon_Container(brushPrimary, brushSecondary);
                    break;

                case PartToolItemBase.CATEGORY_PROPULSION:
                    image = GetIcon_Propulsion(brushPrimary, brushSecondary);
                    break;

                case PartToolItemBase.CATEGORY_SENSOR:
                    image = GetIcon_Sensor(brushPrimary, brushSecondary);
                    break;

                case PartToolItemBase.CATEGORY_WEAPON:
                    image = GetIcon_Weapon(brushPrimary, brushSecondary);
                    break;

                case PartToolItemBase.CATEGORY_CONVERTERS:
                    image = GetIcon_Converter(brushPrimary, brushSecondary);
                    break;

                case PartToolItemBase.CATEGORY_EQUIPMENT:
                    image = GetIcon_Equipment(brushPrimary, brushSecondary);
                    break;

                case PartToolItemBase.CATEGORY_BRAIN:
                    image = GetIcon_Brain(brushPrimary, brushSecondary);
                    break;

                case PartToolItemBase.CATEGORY_SHIELD:
                    image = GetIcon_Shield(brushPrimary, brushSecondary, true);
                    break;

                case PartToolItemBase.CATEGORY_STRUCTURAL:
                    // spider web (8 sided)
                    throw new ApplicationException("Finish structural");

                default:
                    throw new ApplicationException("Unknown part category: " + category);
            }

            return new Image()
            {
                Source = image,
                Width = size,
                Height = size,
            };
        }

        #region Private Methods

        //https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/geometry-overview

        private static DrawingImage GetIcon_Container(Brush brushPrimary, Brush brushSecondary)
        {
            DrawingGroup group = new DrawingGroup();

            GeometryDrawing drawing = null;

            #region box

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = new RectangleGeometry()
                {
                    Rect = new Rect(10, 30, 60, 60),
                    RadiusX = 0,
                    RadiusY = 0,
                },
            };

            group.Children.Add(drawing);

            #endregion

            #region back

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 10,30 L 34,10 L 90,10 L 90,66 L 70,90"),
            };

            group.Children.Add(drawing);

            #endregion

            #region middle line

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 70,30 L 90,10"),
            };

            group.Children.Add(drawing);

            #endregion

            DrawingImage retVal = new DrawingImage(group);

            return retVal;
        }
        private static DrawingImage GetIcon_Propulsion(Brush brushPrimary, Brush brushSecondary)
        {
            DrawingGroup group = new DrawingGroup();

            GeometryDrawing drawing = null;

            #region thruster body

            drawing = new GeometryDrawing()
            {
                //Brush = brushSecondary,
                Pen = new Pen()
                {
                    Thickness = 4,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 83,17 C 73,7 73,7 58,12 L 48,22 L 43,37 L 63,57 L 78,52 L 88,42 C 93,27 93,27 83,17"),
            };

            group.Children.Add(drawing);

            #endregion

            #region thruster bevel

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 3,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 49,22 L 78.02458368,50.85681152"),
            };

            group.Children.Add(drawing);

            #endregion

            #region flame

            drawing = new GeometryDrawing()
            {
                Brush = brushSecondary,
                Geometry = Geometry.Parse("M 42,43 A 50.465865728,58.6263035904 0 0,0 16.767067136,72.3131517952 A 28.0246616064,50.3584915456 0 0,1 7.13560297472,93.197430272 A 50.197430272,26.7898585088 0 0,1 27.8534514688,84.0974683136 A 59.2437051392,50.80409440256 0 0,0 57.4753040384,58.69542111232 L 52.57233043456,53.51059027968 A 28.26759569408,16.11820695552 0 0,1 38.43853258752,61.56969375744 A 16.1027719168,28.36221919232 0 0,1 46.48991854592,47.38858416128 L 42.01157627904,42.96090908672"),
            };

            group.Children.Add(drawing);

            #endregion

            DrawingImage retVal = new DrawingImage();
            retVal.Drawing = group;

            return retVal;
        }
        private static DrawingImage GetIcon_Sensor(Brush brushPrimary, Brush brushSecondary)
        {
            DrawingGroup group = new DrawingGroup();

            GeometryDrawing drawing = null;

            #region rect

            drawing = new GeometryDrawing()
            {
                Brush = brushPrimary,
                Geometry = new RectangleGeometry()
                {
                    Rect = new Rect(10, 70, 20, 20),
                    RadiusX = 2,
                    RadiusY = 2,
                },
            };

            group.Children.Add(drawing);

            #endregion

            #region arc pen

            Pen pen = new Pen()
            {
                Thickness = 5,
                DashCap = PenLineCap.Flat,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Miter,
                MiterLimit = 10,
                Brush = brushSecondary,
            };

            #endregion

            #region arc 1

            drawing = new GeometryDrawing()
            {
                Pen = pen,
                Geometry = Geometry.Parse("M 14,57 Q 48,54 44,86"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc 2

            drawing = new GeometryDrawing()
            {
                Pen = pen,
                Geometry = Geometry.Parse("M 14,42 Q 64,37 59,86"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc 3

            drawing = new GeometryDrawing()
            {
                Pen = pen,
                Geometry = Geometry.Parse("M 14,27 Q 80,23 75,86"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc 4

            drawing = new GeometryDrawing()
            {
                Pen = pen,
                Geometry = Geometry.Parse("M 14,12 Q 95,7 90,86"),
            };

            group.Children.Add(drawing);

            #endregion

            DrawingImage retVal = new DrawingImage();
            retVal.Drawing = group;

            return retVal;
        }
        private static DrawingImage GetIcon_Weapon(Brush brushPrimary, Brush brushSecondary)
        {
            DrawingGroup group = new DrawingGroup();

            GeometryDrawing drawing = null;

            #region handle

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 17,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushSecondary,
                },
                Geometry = Geometry.Parse("M 35,65 L 13,87"),
            };

            group.Children.Add(drawing);

            #endregion

            #region guard

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 8,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 20,50 L 50,80"),
            };

            group.Children.Add(drawing);

            #endregion

            #region blade

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 32,57 L 60,25 A 60,30 0 0,1 90,10 A 28,60 0 0,1 76,40 L 43,69"),
            };

            group.Children.Add(drawing);

            #endregion

            DrawingImage retVal = new DrawingImage();
            retVal.Drawing = group;

            return retVal;
        }
        private static DrawingImage GetIcon_Converter(Brush brushPrimary, Brush brushSecondary)
        {
            GeometryDrawing drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 32,68 L 25,75 Q 13,72 10,60 L 45,25 L 37,17 L 68,17 L 68,48 L 60,40 L 40,60 L 32,52 L 32,83 L 63,83 L 55,75 L 90,40 Q 87,28 75,25 L 68,32"),
            };

            DrawingImage retVal = new DrawingImage();
            retVal.Drawing = drawing;

            return retVal;
        }
        private static DrawingImage GetIcon_Equipment(Brush brushPrimary, Brush brushSecondary)
        {
            DrawingGroup group = new DrawingGroup();

            GeometryDrawing drawing = null;

            #region circle

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = new EllipseGeometry()
                {
                    Center = new Point(50, 50),
                    RadiusX = 40,
                    RadiusY = 40,
                },
            };

            group.Children.Add(drawing);

            #endregion

            #region center dot

            drawing = new GeometryDrawing()
            {
                Brush = brushPrimary,
                Geometry = new EllipseGeometry()
                {
                    Center = new Point(50, 50),
                    RadiusX = 4,
                    RadiusY = 4,
                },
            };

            group.Children.Add(drawing);

            #endregion

            #region spokes

            Pen pen = new Pen()
            {
                Thickness = 10,
                DashCap = PenLineCap.Flat,
                StartLineCap = PenLineCap.Triangle,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Miter,
                MiterLimit = 10,
                Brush = brushSecondary,
            };


            Point start = new Point(62.9735178272456, 42.420594688);
            Point end = new Point(73.7538857402056, 36.26268532736);

            RotateTransform rotate = new RotateTransform(0, 50, 50);

            for (int cntr = 0; cntr < 6; cntr++)
            {
                rotate.Angle = 60 * cntr;

                drawing = new GeometryDrawing()
                {
                    Pen = pen,
                    Geometry = new LineGeometry(rotate.Transform(start), rotate.Transform(end)),
                };

                group.Children.Add(drawing);
            }

            #endregion

            DrawingImage retVal = new DrawingImage();
            retVal.Drawing = group;

            return retVal;
        }
        private static DrawingImage GetIcon_Brain(Brush brushPrimary, Brush brushSecondary)
        {
            DrawingGroup group = new DrawingGroup();

            GeometryDrawing drawing = null;

            #region left lobe

            drawing = new GeometryDrawing()
            {
                //Brush = brushSecondary,

                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 51,85 L 51,15 Q 39,8 32,22 Q 13,32 22,50 Q 16,67 29,74 Q 30,86 41,87 Q 47,90 51,85"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 22,50 Q 26,37 37,41"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushSecondary,
                },
                Geometry = Geometry.Parse("M 31,59 Q 41,56 43,69"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushSecondary,
                },
                Geometry = Geometry.Parse("M 33,31 Q 42,32 42,25"),
            };

            group.Children.Add(drawing);

            #endregion

            #region right lobe

            drawing = new GeometryDrawing()
            {
                //Brush = brushSecondary,

                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 51,85 L 51,15 Q 59,7 70,20 Q 83,25 83,45 Q 86,60 79,65 Q 79,80 67,82 Q 55,93 51,85"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 83,45 Q 76,58 67,53"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushSecondary,
                },
                Geometry = Geometry.Parse("M 69,44 Q 58,40 61,30"),
            };

            group.Children.Add(drawing);

            #endregion
            #region arc

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushSecondary,
                },
                Geometry = Geometry.Parse("M 61,69 Q 67,71 70,65"),
            };

            group.Children.Add(drawing);

            #endregion

            DrawingImage retVal = new DrawingImage();
            retVal.Drawing = group;

            return retVal;
        }
        private static DrawingImage GetIcon_Shield(Brush brushPrimary, Brush brushSecondary, bool? isDiagonalDown = null)
        {
            //TODO: Use static random if null was passed in
            bool isDown = isDiagonalDown ?? true;

            DrawingGroup group = new DrawingGroup();

            GeometryDrawing drawing = null;

            #region stripe

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 12,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Flat,
                    LineJoin = PenLineJoin.Miter,
                    MiterLimit = 10,
                    Brush = brushSecondary,
                },

                Geometry = Geometry.Parse(isDown ? "M 25,28 L 70,67" : "M 75,27 L 30,67"),
            };

            group.Children.Add(drawing);

            #endregion

            #region rim

            drawing = new GeometryDrawing()
            {
                Pen = new Pen()
                {
                    Thickness = 5,
                    DashCap = PenLineCap.Flat,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                    Brush = brushPrimary,
                },
                Geometry = Geometry.Parse("M 20,25 Q 50,5 80,25 C 78,60 80,60 50,90 C 20,60 23,60 20,25"),
            };

            group.Children.Add(drawing);

            #endregion

            DrawingImage retVal = new DrawingImage();
            retVal.Drawing = group;

            return retVal;
        }

        #endregion
    }
}
