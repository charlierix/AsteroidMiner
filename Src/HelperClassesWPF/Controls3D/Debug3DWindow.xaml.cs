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
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF.Controls3D
{
    public partial class Debug3DWindow : Window
    {
        #region Declaration Section

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

        public void AddDot(Point3D position, double radius, Color color, bool isShiny = true)
        {
            Material material = GetMaterial(isShiny, color);

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, radius);
            geometry.Transform = new TranslateTransform3D(position.ToVector());

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;
            this.Visuals3D.Add(visual);
        }
        public void AddDots(IEnumerable<Point3D> positions, double radius, Color color, bool isShiny = true)
        {
            Model3DGroup geometries = new Model3DGroup();

            Material material = GetMaterial(isShiny, color);

            foreach (Point3D pos in positions)
            {
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, radius);
                geometry.Transform = new TranslateTransform3D(pos.ToVector());

                geometries.Children.Add(geometry);
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;
            this.Visuals3D.Add(visual);
        }
        public void AddDots(IEnumerable<Tuple<Point3D, double, Color, bool>> definitions)
        {
            Model3DGroup geometries = new Model3DGroup();

            foreach (var def in definitions)
            {
                Material material = GetMaterial(def.Item4, def.Item3);

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, def.Item2);
                geometry.Transform = new TranslateTransform3D(def.Item1.ToVector());

                geometries.Children.Add(geometry);
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;
            this.Visuals3D.Add(visual);
        }

        public void AddLine(Point3D point1, Point3D point2, double thickness, Color color)
        {
            BillboardLine3DSet visual = new BillboardLine3DSet();
            visual.Color = color;
            visual.BeginAddingLines();

            visual.AddLine(point1, point2, thickness);

            visual.EndAddingLines();

            this.Visuals3D.Add(visual);
        }
        public void AddLines(IEnumerable<Tuple<Point3D, Point3D>> lines, double thickness, Color color)
        {
            BillboardLine3DSet visual = new BillboardLine3DSet();
            visual.Color = color;
            visual.BeginAddingLines();

            foreach (var line in lines)
            {
                visual.AddLine(line.Item1, line.Item2, thickness);
            }

            visual.EndAddingLines();

            this.Visuals3D.Add(visual);
        }

        public void AddMessage(string text, bool isBottom = true, string color = "FFFFFF")
        {
            TextBlock textBlock = new TextBlock()
            {
                Text = text,
                Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex(color)),
            };

            if (isBottom)
            {
                this.Messages_Bottom.Add(textBlock);
            }
            else
            {
                this.Messages_Top.Add(textBlock);
            }
        }

        public void SetCamera(Point3D position, Vector3D lookDirection, Vector3D upDirection)
        {
            _wasSetCameraCalled = true;

            _camera.Position = position;
            _camera.LookDirection = lookDirection;
            _camera.UpDirection = upDirection;
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

        private static Material GetMaterial(bool isShiny, Color color)
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
}
