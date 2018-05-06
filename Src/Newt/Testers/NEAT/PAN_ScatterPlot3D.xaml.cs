using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.NEAT
{
    /// <summary>
    /// Draws dots inside of a cube
    /// </summary>
    /// <remarks>
    /// This is a one off harcoded control - quick and dirty
    /// 
    /// It was copied from Debug3DWindow, and hardcoded to show the Pick a Number test data
    /// </remarks>
    public partial class PAN_ScatterPlot3D : UserControl
    {
        #region Declaration Section

        private readonly TrackBallRoam _trackball;

        private readonly int _viewportOffset_Init;
        private int _viewportOffset_Labels = -1;

        #endregion

        #region Constructor

        public PAN_ScatterPlot3D()
        {
            InitializeComponent();

            _trackball = new TrackBallRoam(_camera);
            //_trackball.KeyPanScale = 15d;
            _trackball.EventSource = grdViewPort;       //NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
            _trackball.ShouldHitTestOnOrbit = false;

            _viewportOffset_Init = _viewport.Children.Count;
            _viewportOffset_Labels = _viewportOffset_Init;
        }

        #endregion

        #region Public Properties

        private Color _trueColor = Colors.DodgerBlue;
        public Color TrueColor
        {
            get
            {
                return _trueColor;
            }
            set
            {
                _trueColor = value;
            }
        }

        private Color _falseColor = UtilityWPF.ColorFromHex("FEE");
        public Color FalseColor
        {
            get
            {
                return _falseColor;
            }
            set
            {
                _falseColor = value;
            }
        }

        #endregion

        #region Public Methods

        public void ResetLabels(int[] trueValues)
        {
            if (trueValues == null)
            {
                trueValues = new int[0];
            }

            if (trueValues.Any(o => o > 7))
            {
                throw new ArgumentOutOfRangeException("");
            }

            while (_viewport.Children.Count > _viewportOffset_Init)
            {
                _viewport.Children.RemoveAt(_viewport.Children.Count - 1);
            }

            VectorND center = new VectorND(new[] { .5, .5, .5 });

            // Add the numbers
            for (int cntr = 0; cntr < 8; cntr++)
            {
                VectorND position = new VectorND(UtilityCore.ConvertToBase2(cntr, 3).Select(o => o ? 1d : 0d).ToArray());
                position = position - center;
                position = center + (position * 1.2);

                Color color = trueValues.Contains(cntr) ?
                    TrueColor :
                    FalseColor;

                AddText3D(cntr.ToString(), position.ToPoint3D(), position.ToVector3D().ToUnit(false), .1, color, false);
            }

            _viewportOffset_Labels = _viewport.Children.Count;
        }

        public void ClearFrame()
        {
            while (_viewport.Children.Count - 1 >= _viewportOffset_Labels)
            {
                _viewport.Children.RemoveAt(_viewportOffset_Init);
            }
        }

        public void AddDots(IEnumerable<Tuple<Point3D, Color>> dots, double radius = .05, bool isShiny = true, bool isHiRes = false)
        {
            Model3DGroup geometries = new Model3DGroup();

            foreach (Tuple<Point3D, Color> dot in dots)
            {
                Material material = GetMaterial(isShiny, dot.Item2);

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, isHiRes ? 3 : 1, true);
                geometry.Transform = new TranslateTransform3D(dot.Item1.ToVector());

                geometries.Children.Add(geometry);
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;
            _viewport.Children.Insert(_viewportOffset_Init, visual);        // put this between the lights and the labels
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
            Material faceMaterial = GetMaterial(isShiny, color);
            Material edgeMaterial = GetMaterial(false, UtilityWPF.AlphaBlend(color, UtilityWPF.OppositeColor_BW(color), .75));

            ModelVisual3D visual = new ModelVisual3D();

            visual.Content = UtilityWPF.GetText3D(
                text,
                font ?? this.FontFamily,
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

            _viewport.Children.Add(visual);
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
    }
}
