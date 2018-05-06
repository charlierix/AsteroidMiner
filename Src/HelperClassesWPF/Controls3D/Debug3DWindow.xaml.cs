using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xaml;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF.Controls3D
{
    /// <summary>
    /// This is meant to be used when diagnosing an error.  Make a copy of your buggy function, instantiate a debug window
    /// from within the copy, draw the scene to try to help visualize the problem
    /// </summary>
    /// <remarks>
    /// All of the drawing functions were split into Add and a static Get.  The gets are meant to be able to be used elsewhere if
    /// you need to draw a scene in some other window
    /// 
    /// Note that these methods may not be optimal, the priority is having simple to use functions.  Having lots of Visual3D
    /// instances is really bad for performance.  It's better to have a model group with a single visual
    /// </remarks>
    public partial class Debug3DWindow : Window
    {
        #region Declaration Section

        public const string AXISCOLOR_X = "FF6060";
        public const string AXISCOLOR_Y = "00C000";
        public const string AXISCOLOR_Z = "6060FF";

        private TrackBallRoam _trackball = null;

        private bool _wasSetCameraCalled = false;

        private readonly int _viewportOffset;

        #endregion

        #region Constructor

        public Debug3DWindow()
        {
            this.Messages_Top = new ObservableCollection<UIElement>();       // these appear to need to be created before initializecomponent
            this.Messages_Bottom = new ObservableCollection<UIElement>();

            this.Visuals3D = new ObservableCollection<Visual3D>();
            this.Visuals3D.CollectionChanged += Visuals3D_CollectionChanged;        // can't bind viewport directly to this, because the camera and lights need to be left alone

            InitializeComponent();

            this.DataContext = this;

            _viewportOffset = _viewport.Children.Count;
        }

        #endregion

        #region Public Properties

        //public ObservableCollection<Visual3D> Visuals3D { get; private set; }
        public ObservableCollection<Visual3D> Visuals3D { get; private set; }

        // These can be used to show debug messages at the top or bottom of the window
        public ObservableCollection<UIElement> Messages_Top { get; private set; }       // I think these MUST be properties for binding to see them (not public variables)
        public ObservableCollection<UIElement> Messages_Bottom { get; private set; }

        // These can be used to change the color of the lights
        public Color LightColor_Ambient
        {
            get { return (Color)GetValue(LightColor_AmbientProperty); }
            set { SetValue(LightColor_AmbientProperty, value); }
        }
        public static readonly DependencyProperty LightColor_AmbientProperty = DependencyProperty.Register("LightColor_Ambient", typeof(Color), typeof(Debug3DWindow), new PropertyMetadata(Colors.DimGray));

        public Color LightColor_Primary
        {
            get { return (Color)GetValue(LightColor_PrimaryProperty); }
            set { SetValue(LightColor_PrimaryProperty, value); }
        }
        public static readonly DependencyProperty LightColor_PrimaryProperty = DependencyProperty.Register("LightColor_Primary", typeof(Color), typeof(Debug3DWindow), new PropertyMetadata(Colors.White));

        public Color LightColor_Secondary
        {
            get { return (Color)GetValue(LightColor_SecondaryProperty); }
            set { SetValue(LightColor_SecondaryProperty, value); }
        }
        public static readonly DependencyProperty LightColor_SecondaryProperty = DependencyProperty.Register("LightColor_Secondary", typeof(Color), typeof(Debug3DWindow), new PropertyMetadata(Colors.Silver));

        #endregion

        #region Public Methods

        public void AddAxisLines(double length, double thickness)
        {
            Visuals3D.AddRange(
                GetAxisLines(length, thickness));
        }
        public static Visual3D[] GetAxisLines(double length, double thickness)
        {
            return new[]
            {
                GetLine(new Point3D(0, 0, 0), new Point3D(length, 0, 0), thickness, UtilityWPF.ColorFromHex(AXISCOLOR_X)),
                GetLine(new Point3D(0, 0, 0), new Point3D(0, length, 0), thickness, UtilityWPF.ColorFromHex(AXISCOLOR_Y)),
                GetLine(new Point3D(0, 0, 0), new Point3D(0, 0, length), thickness, UtilityWPF.ColorFromHex(AXISCOLOR_Z)),
            };
        }

        public void AddDot(Point3D position, double radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            Visuals3D.Add(
                GetDot(position, radius, color, isShiny, isHiRes));
        }
        public void AddDot(Vector3D position, double radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            AddDot(position.ToPoint(), radius, color, isShiny, isHiRes);
        }
        public static Visual3D GetDot(Point3D position, double radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            Material material = GetMaterial(isShiny, color);

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, isHiRes ? 3 : 1, true);
            geometry.Transform = new TranslateTransform3D(position.ToVector());

            return new ModelVisual3D
            {
                Content = geometry
            };
        }

        public void AddDots(IEnumerable<Point3D> positions, double radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            Visuals3D.Add(
                GetDots(positions, radius, color, isShiny, isHiRes));
        }
        public void AddDots(IEnumerable<Vector3D> positions, double radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            AddDots(positions.Select(o => o.ToPoint()), radius, color, isShiny, isHiRes);
        }
        public static Visual3D GetDots(IEnumerable<Point3D> positions, double radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            Model3DGroup geometries = new Model3DGroup();

            Material material = GetMaterial(isShiny, color);

            foreach (Point3D pos in positions)
            {
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, isHiRes ? 3 : 1, true);
                geometry.Transform = new TranslateTransform3D(pos.ToVector());

                geometries.Children.Add(geometry);
            }

            return new ModelVisual3D
            {
                Content = geometries
            };
        }

        public void AddDots(IEnumerable<(Point3D position, double radius, Color color, bool isShiny, bool isHiRes)> definitions)
        {
            Visuals3D.Add(
                GetDots(definitions));
        }
        public static Visual3D GetDots(IEnumerable<(Point3D position, double radius, Color color, bool isShiny, bool isHiRes)> definitions)
        {
            Model3DGroup geometries = new Model3DGroup();

            foreach (var def in definitions)
            {
                Material material = GetMaterial(def.isShiny, def.color);

                geometries.Children.Add(new GeometryModel3D
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = UtilityWPF.GetSphere_Ico(def.radius, def.isHiRes ? 3 : 1, true),
                    Transform = new TranslateTransform3D(def.position.ToVector()),
                });
            }

            return new ModelVisual3D()
            {
                Content = geometries,
            };
        }

        public void AddEllipse(Point3D position, Vector3D radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            Visuals3D.Add(
                GetEllipse(position, radius, color, isShiny, isHiRes));
        }
        public static Visual3D GetEllipse(Point3D position, Vector3D radius, Color color, bool isShiny = true, bool isHiRes = false)
        {
            Material material = GetMaterial(isShiny, color);

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetSphere_Ico(1, isHiRes ? 3 : 1, true);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(radius));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            geometry.Transform = transform;

            return new ModelVisual3D
            {
                Content = geometry
            };
        }

        public void AddLine(Point3D point1, Point3D point2, double thickness, Color color)
        {
            Visuals3D.Add(
                GetLine(point1, point2, thickness, color));
        }
        public void AddLine(Vector3D point1, Vector3D point2, double thickness, Color color)
        {
            AddLine(point1.ToPoint(), point2.ToPoint(), thickness, color);
        }
        public static Visual3D GetLine(Point3D point1, Point3D point2, double thickness, Color color)
        {
            BillboardLine3DSet visual = new BillboardLine3DSet();
            visual.Color = color;
            visual.BeginAddingLines();

            visual.AddLine(point1, point2, thickness);

            visual.EndAddingLines();

            return visual;
        }

        public void AddLines(IEnumerable<(Point3D point1, Point3D point2)> lines, double thickness, Color color)
        {
            Visuals3D.Add(
                GetLines(lines, thickness, color));
        }
        public static Visual3D GetLines(IEnumerable<(Point3D point1, Point3D point2)> lines, double thickness, Color color)
        {
            BillboardLine3DSet visual = new BillboardLine3DSet();
            visual.Color = color;
            visual.BeginAddingLines();

            foreach (var line in lines)
            {
                visual.AddLine(line.point1, line.point2, thickness);
            }

            visual.EndAddingLines();

            return visual;
        }

        public void AddPlane(ITriangle plane, double size, Color color, Color? reflectiveColor = null, int numCells = 12, Point3D? center = null)
        {
            Visuals3D.Add(
                GetPlane(plane, size, color, reflectiveColor, numCells, center));
        }
        public static Visual3D GetPlane(ITriangle plane, double size, Color color, Color? reflectiveColor = null, int numCells = 12, Point3D? center = null)
        {
            return new ModelVisual3D()
            {
                Content = UtilityWPF.GetPlane(plane, size, color, reflectiveColor, numCells, center),
            };
        }

        public void AddCircle(Point3D center, double radius, double thickness, Color color, ITriangle plane = null, bool isShiny = true)
        {
            Visuals3D.Add(
                GetCircle(center, radius, thickness, color, plane, isShiny));
        }
        public static Visual3D GetCircle(Point3D center, double radius, double thickness, Color color, ITriangle plane = null, bool isShiny = true)
        {
            Material material = GetMaterial(isShiny, color);

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetTorus(30, 7, thickness / 2, radius);

            Transform3DGroup transform = new Transform3DGroup();

            if (plane == null)
            {
                transform.Children.Add(new TranslateTransform3D(center.ToVector()));
            }
            else
            {
                var transform2D = Math2D.GetTransformTo2D(plane);

                // Transform the center point down to 2D
                Point3D center2D = transform2D.Item1.Transform(center);

                // Add a translate along the 2D plane
                transform.Children.Add(new TranslateTransform3D(center2D.ToVector()));

                // Now that it's positioned correctly in 2D, transform the whole thing into 3D (to line up with the 3D plane that was passed in)
                transform.Children.Add(transform2D.Item2);
            }

            geometry.Transform = transform;

            return new ModelVisual3D
            {
                Content = geometry
            };
        }

        public void AddHull(ITriangleIndexed[] hull, Color? faceColor = null, Color? edgeColor = null, double? edgeThickness = null, bool isShinyFaces = true, bool isIndependentFaces = true)
        {
            Visuals3D.AddRange(
                GetHull(hull, faceColor, edgeColor, edgeThickness, isShinyFaces, isIndependentFaces));
        }
        public static Visual3D[] GetHull(ITriangleIndexed[] hull, Color? faceColor = null, Color? edgeColor = null, double? edgeThickness = null, bool isShinyFaces = true, bool isIndependentFaces = true)
        {
            List<Visual3D> retVal = new List<Visual3D>();

            // Lines
            if (edgeColor != null && edgeThickness != null)
            {
                var hullLines = TriangleIndexed.GetUniqueLines(hull);
                var hullLinePoints = hullLines.
                    Select(o => (hull[0].AllPoints[o.Item1], hull[0].AllPoints[o.Item2]));

                retVal.Add(GetLines(hullLinePoints, edgeThickness.Value, edgeColor.Value));
            }

            // Hull
            if (faceColor != null)
            {
                Material material = GetMaterial(isShinyFaces, faceColor.Value);

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;

                if (isIndependentFaces)
                {
                    geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(hull);
                }
                else
                {
                    geometry.Geometry = UtilityWPF.GetMeshFromTriangles(hull);
                }

                retVal.Add(new ModelVisual3D
                {
                    Content = geometry
                });
            }

            return retVal.ToArray();
        }

        public void AddTriangle(Point3D point1, Point3D point2, Point3D point3, Color color, bool isShiny = true)
        {
            Visuals3D.Add(
                GetTriangle(point1, point2, point3, color, isShiny));
        }
        public static Visual3D GetTriangle(Point3D point1, Point3D point2, Point3D point3, Color color, bool isShiny = true)
        {
            List<Visual3D> retVal = new List<Visual3D>();

            Material material = GetMaterial(isShiny, color);

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            MeshGeometry3D meshGeometry = new MeshGeometry3D();

            meshGeometry.Positions.Add(point1);
            meshGeometry.Positions.Add(point2);
            meshGeometry.Positions.Add(point3);

            meshGeometry.TriangleIndices.Add(0);
            meshGeometry.TriangleIndices.Add(1);
            meshGeometry.TriangleIndices.Add(2);

            geometry.Geometry = meshGeometry;

            return new ModelVisual3D
            {
                Content = geometry
            };
        }

        public void AddSquare(Point center, double sizeX, double sizeY, Color color, bool isShiny = true, double z = 0)
        {
            double halfSizeX = sizeX / 2;
            double halfSizeY = sizeY / 2;

            Point min = new Point(center.X - halfSizeX, center.Y - halfSizeY);
            Point max = new Point(center.X + halfSizeX, center.Y + halfSizeY);

            AddSquare(min, max, color, isShiny, z);
        }
        public void AddSquare(Point min, Point max, Color color, bool isShiny = true, double z = 0)
        {
            Visuals3D.Add(
                GetSquare(min, max, color, isShiny, z));
        }
        public void AddSquare(Rect rect, Color color, bool isShiny = true, double z = 0)
        {
            AddSquare(rect.TopLeft, rect.BottomRight, color, isShiny, z);
        }
        public static Visual3D GetSquare(Point min, Point max, Color color, bool isShiny = true, double z = 0)
        {
            Material material = GetMaterial(isShiny, color);

            return new ModelVisual3D
            {
                Content = new GeometryModel3D
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = UtilityWPF.GetSquare2D(min, max, z),
                },
            };
        }

        public void AddMesh(MeshGeometry3D mesh, Color color, bool isShinyFaces = true)
        {
            this.Visuals3D.Add(
                GetMesh(mesh, color, isShinyFaces));
        }
        public static Visual3D GetMesh(MeshGeometry3D mesh, Color color, bool isShinyFaces = true)
        {
            Material material = GetMaterial(isShinyFaces, color);

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = mesh;

            return new ModelVisual3D
            {
                Content = geometry
            };
        }

        public void AddModel(Model3D model)
        {
            this.Visuals3D.Add(
                GetModel(model));
        }
        public static Visual3D GetModel(Model3D model)
        {
            return new ModelVisual3D
            {
                Content = model
            };
        }

        /// <summary>
        /// This adds text into the 3D scene
        /// NOTE: This is affected by WPF's "feature" of semi transparent visuals only showing visuals that were added before
        /// </summary>
        /// <remarks>
        /// Think of the text sitting in a 2D rectangle. position=center, normal=vector sticking out of rect, textDirection=vector along x
        /// </remarks>
        /// <param name="position">The center point of the text</param>
        /// <param name="normal">The direction of the vector that points straight out of the plane of the text (default is 0,0,1)</param>
        /// <param name="textDirection">The direction of the vector that points along the text (default is 1,0,0)</param>
        public void AddText3D(string text, Point3D position, Vector3D normal, double height, Color color, bool isShiny, Vector3D? textDirection = null, double? depth = null, FontFamily font = null, FontStyle? style = null, FontWeight? weight = null, FontStretch? stretch = null)
        {
            this.Visuals3D.Add(
                GetText3D(text, position, normal, height, color, isShiny, textDirection, depth, font ?? this.FontFamily, style, weight, stretch));
        }
        public static Visual3D GetText3D(string text, Point3D position, Vector3D normal, double height, Color color, bool isShiny, Vector3D? textDirection = null, double? depth = null, FontFamily font = null, FontStyle? style = null, FontWeight? weight = null, FontStretch? stretch = null)
        {
            Material faceMaterial = GetMaterial(isShiny, color);
            Material edgeMaterial = GetMaterial(false, UtilityWPF.AlphaBlend(color, UtilityWPF.OppositeColor_BW(color), .75));

            ModelVisual3D visual = new ModelVisual3D();

            visual.Content = UtilityWPF.GetText3D(
                text,
                font ?? UtilityWPF.GetFont("Arial"),
                faceMaterial,
                edgeMaterial,
                height,
                depth ?? height / 15d,
                style,
                weight,
                stretch);

            Transform3DGroup transform = new Transform3DGroup();

            Quaternion quat;
            if (textDirection == null)
            {
                quat = Math3D.GetRotation(new Vector3D(0, 0, 1), normal);
            }
            else
            {
                quat = Math3D.GetRotation(new DoubleVector(new Vector3D(0, 0, 1), new Vector3D(1, 0, 0)), new DoubleVector(normal, textDirection.Value));
            }
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quat)));

            transform.Children.Add(new TranslateTransform3D(position.ToVector()));
            visual.Transform = transform;

            return visual;
        }

        public static GraphResult GetCountGraph(Func<double> func, string title = null)
        {
            double[] raw = Enumerable.Range(0, 100000).
                Select(o => func()).
                ToArray();

            double min = raw.Min();
            double max = raw.Max();
            int numSteps = 100;
            double rangeStep = (max - min) / numSteps;

            (double from, double to)[] ranges = Enumerable.Range(0, numSteps).
                Select(o => (min + (o * rangeStep), min + (o * rangeStep + rangeStep))).
                ToArray();

            int[] counts = new int[numSteps];

            //var testIndices = raw.
            //    Select(o => (value: o, index: ((o - min) / rangeStep).ToInt_Floor())).
            //    OrderBy(o => o.index).
            //    ToArray();

            int errorCount = 0;

            foreach (double rawValue in raw)
            {
                // find the bucket
                int index = ((rawValue - min) / rangeStep).ToInt_Floor();

                if (index >= numSteps)
                {
                    if (rawValue.IsNearValue(max))       // this is pretty rare, but does happen around once every 15,000 times (and least when func is rand.nextdouble())
                    {
                        index = numSteps - 1;
                    }
                    else
                    {
                        errorCount++;
                        continue;
                    }
                }

                counts[index]++;
            }

            if (errorCount > numSteps * .001)
            {
                throw new ApplicationException("There is a bug with calculating the index");
            }

            // need these as doubles to avoid integer division
            double maxCounts = counts.Max();
            double numStepsDbl = numSteps;

            var normalized = Enumerable.Range(0, numSteps).
                Select(o => new Point(o / numStepsDbl, counts[o] / maxCounts)).
                ToArray();

            return new GraphResult()
            {
                Title = title,
                Min = min,
                Max = max,
                Ranges = ranges,
                Counts = counts,
                NormalizedCounts = normalized,
            };
        }
        public void AddGraph(GraphResult graph, Point3D center, double size)
        {
            double halfSize = size / 2;

            Rect3D rect = new Rect3D(center.X - halfSize, center.Y - halfSize, center.Z, size, size, 0);

            AddGraph(graph, rect);
        }
        public void AddGraph(GraphResult graph, Rect3D location)
        {
            double thickness = Math1D.Max(location.SizeX, location.SizeY, location.SizeZ) / 500;

            AddLines(
                new[]
                {
                    // horizontal
                    (new Point3D(location.X, location.Y + (location.SizeY * .25), location.Z), new Point3D(location.X + location.SizeX, location.Y + (location.SizeY * .25), location.Z)),
                    (new Point3D(location.X, location.Y + (location.SizeY * .5), location.Z), new Point3D(location.X + location.SizeX, location.Y + (location.SizeY * .5), location.Z)),
                    (new Point3D(location.X, location.Y + (location.SizeY * .75), location.Z), new Point3D(location.X + location.SizeX, location.Y + (location.SizeY * .75), location.Z)),
                    (new Point3D(location.X, location.Y + location.SizeY, location.Z), new Point3D(location.X + location.SizeX, location.Y + location.SizeY, location.Z)),

                    // vertical
                    (new Point3D(location.X + (location.SizeX * .25), location.Y, location.Z), new Point3D(location.X  + (location.SizeX * .25), location.Y + location.SizeY, location.Z)),
                    (new Point3D(location.X + (location.SizeX * .5), location.Y, location.Z), new Point3D(location.X  + (location.SizeX * .5), location.Y + location.SizeY, location.Z)),
                    (new Point3D(location.X + (location.SizeX * .75), location.Y, location.Z), new Point3D(location.X  + (location.SizeX * .75), location.Y + location.SizeY, location.Z)),
                    (new Point3D(location.X + location.SizeX, location.Y, location.Z), new Point3D(location.X  + location.SizeX, location.Y + location.SizeY, location.Z)),

                    // diagonal
                    (new Point3D(location.X, location.Y, location.Z), new Point3D(location.X + location.SizeX, location.Y + location.SizeY, location.Z)),
                },
                thickness,
                Colors.Gray);

            AddLines(
                new[]
                {
                    (new Point3D(location.X, location.Y, location.Z), new Point3D(location.X, location.Y + location.SizeY, location.Z)),
                    (new Point3D(location.X, location.Y, location.Z), new Point3D(location.X + location.SizeX, location.Y, location.Z))
                },
                thickness,
                Colors.Black);

            Point3D[] pointsWorld = graph.NormalizedCounts.
                Select(o => new Point3D
                (
                    location.X + (location.SizeX * o.X),
                    location.Y + (location.SizeY * o.Y),
                    location.Z
                )).
                ToArray();

            AddLines(
                Enumerable.Range(0, graph.NormalizedCounts.Length - 1).
                    Select(o => (pointsWorld[o], pointsWorld[o + 1])),
                thickness,
                Colors.White);

            if (!string.IsNullOrWhiteSpace(graph.Title))
            {
                AddText3D(graph.Title, new Point3D(location.CenterX(), location.CenterY(), location.CenterZ()), new Vector3D(0, 0, 1), location.SizeY / 20, Colors.Black, false);
            }
        }
        public void AddGraphs(GraphResult[] graphs, Point3D center, double graphSize)
        {
            if (graphs == null || graphs.Length == 0)
            {
                return;
            }

            int rows = Math.Sqrt(graphs.Length).ToInt_Floor();

            int columns = graphs.Length / rows;
            if (graphs.Length % rows != 0)
            {
                columns++;
            }

            var cells = Math2D.GetCells_InvertY(graphSize * 1.1111111, columns, rows).
                ToArray();

            for (int cntr = 0; cntr < graphs.Length; cntr++)
            {
                Rect cellRect = cells[cntr].rect.ChangeSize(.9);
                cellRect.Location += center.ToVector2D();

                AddGraph(graphs[cntr], cellRect.ToRect3D(center.Z));
            }
        }

        /// <summary>
        /// This is the same as AddMessage.  I constantly find myself trying to type AddText, but the properties are
        /// called Message
        /// </summary>
        public void AddText(string text, bool isBottom = true, string color = null)
        {
            AddMessage(text, isBottom, color);
        }
        public void AddMessage(string text, bool isBottom = true, string color = null)
        {
            #region fore color

            Color foreColor;

            if (color == null)
            {
                if (this.Background is SolidColorBrush)
                {
                    // Ignore color portion, just go black or white
                    foreColor = UtilityWPF.OppositeColor_BW(((SolidColorBrush)this.Background).Color);
                }
                else
                {
                    foreColor = Colors.Black;
                }
            }
            else
            {
                foreColor = UtilityWPF.ColorFromHex(color);
            }

            #endregion

            TextBlock textblock = new TextBlock()
            {
                Text = text,
                Foreground = new SolidColorBrush(foreColor),
            };

            if (isBottom)
            {
                this.Messages_Bottom.Add(textblock);
            }
            else
            {
                this.Messages_Top.Add(textblock);
            }
        }

        public void AddSnapshot(object snapshot, string description = null)
        {
            // Doing this now in case they change the state of the snapshot object between this call and the button getting clicked
            string serialized = XamlServices.Save(snapshot);

            Button button = new Button()
            {
                Margin = new Thickness(2),
                Content = description ?? $"snapshot {panelSnapshots.Children.Count + 1}",
            };

            button.Click += (s, e) =>
            {
                Clipboard.SetText(serialized);
                MessageBox.Show($"Serialization of {snapshot.GetType()} saved to clipboard", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            };

            panelSnapshots.Children.Add(button);
        }

        public void SetCamera(Point3D position, Vector3D lookDirection, Vector3D upDirection)
        {
            _wasSetCameraCalled = true;

            _camera.Position = position;
            _camera.LookDirection = lookDirection;
            _camera.UpDirection = upDirection;
        }

        public static Material GetMaterial(bool isShiny, Color color)
        {
            // This was copied from BillboardLine3D (then modified a bit)

            if (isShiny)
            {
                MaterialGroup retVal = new MaterialGroup();
                retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40989898")), 2));

                return retVal;
            }
            else
            {
                return UtilityWPF.GetUnlitMaterial(color);
            }
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _trackball = new TrackBallRoam(_camera);
                //_trackball.KeyPanScale = 15d;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = false;

                //TODO: May want a public bool property telling whether to auto set this.  Also do this during Visuals3D change event
                if (!_wasSetCameraCalled && this.Visuals3D.Count > 0)
                {
                    AutoSetCamera();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Debug3DWindow)
                    {
                        window.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// This listener syncs Visuals3D to _viewport.  _viewport contains lights and camera, so there is an offset (that's why it's not
        /// directly bound to viewport)
        /// </summary>
        /// <remarks>
        /// http://blog.stephencleary.com/2009/07/interpreting-notifycollectionchangedeve.html
        /// </remarks>
        private void Visuals3D_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                #region DEBUG

                //string report = string.Format("{0}, new start={1}, new count={2}, old start={3}, old count={4} | new={5} | old={6}",
                //    e.Action,
                //    e.NewStartingIndex,
                //    e.NewItems == null ? "<null>" : e.NewItems.Count.ToString(),
                //    e.OldStartingIndex,
                //    e.OldItems == null ? "<null>" : e.OldItems.Count.ToString(),
                //    e.NewItems == null ? "<null>" : e.NewItems.AsEnumerabIe().Select(o => o.ToString()).ToJoin(";"),
                //    e.OldItems == null ? "<null>" : e.OldItems.AsEnumerabIe().Select(o => o.ToString()).ToJoin(";")
                //    );

                //AddMessage(report);

                #endregion

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        AddVisuals(e.NewStartingIndex, e.NewItems);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        RemoveVisuals(e.OldStartingIndex, e.OldItems);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        #region Reset

                        // They did a clear.  Remove everything except the camera and lights
                        while (_viewport.Children.Count - 1 >= _viewportOffset)
                        {
                            _viewport.Children.RemoveAt(_viewport.Children.Count - 1);
                        }

                        #endregion
                        break;

                    case NotifyCollectionChangedAction.Move:
                        #region Move

                        // If Action is NotifyCollectionChangedAction.Move, then NewItems and OldItems are logically equivalent (i.e., they are
                        // SequenceEqual, even if they are different instances), and they contain the items that moved. In addition, OldStartingIndex
                        // contains the index where the items were moved from, and NewStartingIndex contains the index where the items were
                        // moved to. A Move operation is logically treated as a Remove followed by an Add, so NewStartingIndex is interpreted
                        // as though the items had already been removed.

                        RemoveVisuals(e.OldStartingIndex, e.OldItems);
                        AddVisuals(e.NewStartingIndex, e.NewItems);

                        #endregion
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        #region Replace

                        //If Action is NotifyCollectionChangedAction.Replace, then OldItems contains the replaced items and NewItems contains
                        // the replacement items. In addition, NewStartingIndex and OldStartingIndex are equal, and if they are not -1, then they
                        // contain the index where the items were replaced.

                        if (e.OldStartingIndex != e.NewStartingIndex)
                        {
                            throw new ArgumentException(string.Format("e.OldStartingIndex and e.NewStartingIndex should be equal for replace: old={0}, new={1}", e.OldStartingIndex, e.NewStartingIndex));
                        }
                        else if (e.OldItems == null)
                        {
                            throw new ArgumentException("e.OldItems should never be null for replace");
                        }
                        else if (e.NewItems == null)
                        {
                            throw new ArgumentException("e.NewItems should never be null for replace");
                        }

                        RemoveVisuals(e.OldStartingIndex, e.OldItems);
                        AddVisuals(e.NewStartingIndex, e.NewItems);

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown NotifyCollectionChangedAction: " + e.Action.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void AddVisuals(int newStartingIndex, IList newItems)
        {
            if (newItems == null)
            {
                throw new ArgumentException("e.NewItems should never be null for add");
            }

            // if NewStartingIndex is not -1, then it contains the index where the new items were added
            int index = newStartingIndex < 0 ?
                _viewport.Children.Count :      // add
                newStartingIndex + _viewportOffset;       // insert (always after camera and lights)

            foreach (Visual3D item in newItems)
            {
                _viewport.Children.Insert(index, item);
                index++;
            }
        }
        private void RemoveVisuals(int oldStartingIndex, IList oldItems)
        {
            if (oldItems == null)
            {
                throw new ArgumentException("e.OldItems should never be null for remove");
            }

            // If Action is NotifyCollectionChangedAction.Remove, then OldItems contains the items that were removed. In addition, if OldStartingIndex is
            // not -1, then it contains the index where the old items were removed
            if (oldStartingIndex >= 0)
            {
                if (_viewport.Children.Count <= _viewportOffset + oldItems.Count)
                {
                    throw new ApplicationException("Trying to remove more item than exists in the viewport (observable collection and viewport fell out of sync)");
                }

                for (int cntr = 0; cntr < oldItems.Count; cntr++)
                {
                    _viewport.Children.RemoveAt(_viewportOffset + oldStartingIndex);
                }
            }
            else
            {
                foreach (Visual3D item in oldItems)
                {
                    _viewport.Children.Remove(item);
                }
            }
        }

        private void AutoSetCamera()
        {
            Point3D[] points = TryGetVisualPoints(this.Visuals3D);

            Tuple<Point3D, Vector3D, Vector3D> cameraPos = GetCameraPosition(points);      // this could return null
            if (cameraPos == null)
            {
                cameraPos = Tuple.Create(new Point3D(0, 0, 7), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
            }

            _camera.Position = cameraPos.Item1;
            _camera.LookDirection = cameraPos.Item2;
            _camera.UpDirection = cameraPos.Item3;

            double distance = _camera.Position.ToVector().Length;
            double scale = distance * .0214;

            _trackball.PanScale = scale / 10;
            _trackball.ZoomScale = scale;
            _trackball.MouseWheelScale = distance * .0007;
        }

        private static Tuple<Point3D, Vector3D, Vector3D> GetCameraPosition(Point3D[] points)
        {
            if (points == null || points.Length == 0)
            {
                return null;
            }
            else if (points.Length == 1)
            {
                return Tuple.Create(new Point3D(points[0].X, points[0].Y, points[0].Z + 7), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
            }

            Point3D center = Math3D.GetCenter(points);

            double[] distances = points.
                Select(o => (o - center).Length).
                ToArray();

            //TODO: Use this instead
            //Math1D.Get_Average_StandardDeviation(distances);

            double avgDist = distances.Average();
            double maxDist = distances.Max();

            double threeQuarters = UtilityCore.GetScaledValue(avgDist, maxDist, 0, 1, .75);

            double cameraDist = threeQuarters * 2.5;

            // Set camera to look at center, at a distance of X times average
            return Tuple.Create(new Point3D(center.X, center.Y, center.Z + cameraDist), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
        }

        private static Point3D[] TryGetVisualPoints(IEnumerable<Visual3D> visuals)
        {
            IEnumerable<Point3D> retVal = new Point3D[0];

            foreach (Visual3D visual in visuals)
            {
                Point3D[] points = null;
                try
                {
                    if (visual is ModelVisual3D)
                    {
                        ModelVisual3D visualCast = (ModelVisual3D)visual;
                        points = UtilityWPF.GetPointsFromMesh(visualCast.Content);        // this throws an exception if it doesn't know what kind of model it is
                    }
                }
                catch (Exception)
                {
                    points = null;
                }

                if (points != null)
                {
                    retVal = retVal.Concat(points);
                }
            }

            return retVal.ToArray();
        }

        #endregion
    }

    #region class: GraphResult

    public class GraphResult
    {
        public string Title { get; set; }

        public double Min { get; set; }
        public double Max { get; set; }

        public (double from, double to)[] Ranges { get; set; }

        public int[] Counts { get; set; }

        /// <summary>
        /// This runs the range from 0 to 1, and the counts from 0 to 1
        /// </summary>
        public Point[] NormalizedCounts { get; set; }
    }

    #endregion
}
