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
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;

namespace Game.Newt.v2.Arcanorum.Views
{
    /// <summary>
    /// This is meant to go in front of the game screen (as an overlay)
    /// </summary>
    public partial class BackdropPanel : UserControl
    {
        #region Events

        /// <summary>
        /// This gets raised when the user wants to close the panel
        /// </summary>
        public event EventHandler ClosePanel = null;

        #endregion

        #region Declaration Section

        // Some stable settings for different angles
        //const double THETA = 25.5;
        //const double HEIGHT = 13.9;
        //const double RADIUS = 28.8;
        //const double TRANSLATE = 11;

        //const double THETA = 12;
        //const double HEIGHT = 27.6;
        //const double RADIUS = 121;
        //const double TRANSLATE = 66.3;

        const double THETA = 25.5;
        const double HEIGHT = 13.9;
        const double RADIUS = 28.8;
        const double TRANSLATE = 11;
        const int NUMSEGMENTS = 10;
        //const double PIXELMULT = .6;
        const double PIXELMULT = .75;

        private ContentPresenter pnlLeft = null;
        private ItemDetailPanel pnlDetail = null;

        private Grid grd3D = null;
        private Viewport2DVisual3D _visual2D3D = null;

        private SpriteGraphic_Shells _graphic = null;
        private ModelVisual3D _backVisual = null;

        private ScaleTransform3D _detailScale = null;
        private QuaternionRotation3D _detailRotationInitial = null;
        private AxisAngleRotation3D _detailRotationAnimate = null;
        private TranslateTransform3D _detailTranslate = null;
        private ModelVisual3D _detailVisual = null;

        private AnimateRotation _detailAnimate = null;

        private DispatcherTimer _timer = null;
        private DateTime _lastTick = DateTime.UtcNow;

        private double _cameraLength = 0;

        private readonly double _cameraZ;

        //private BackdropQuick _quick = null;

        private static Lazy<ItemOptionsArco> _itemOptions = new Lazy<ItemOptionsArco>(() => new ItemOptionsArco());

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public BackdropPanel()
        {
            InitializeComponent();

            pnlLeft = new ContentPresenter();
            pnlDetail = new ItemDetailPanel();
            pnlDetail.ItemChanged += pnlDetail_ItemChanged;

            _cameraZ = _camera.Position.Z;
            _cameraLength = _cameraZ; //_camera.Position.ToVector().Length;     // the camera is at (0,0,Z), so length is just Z

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += Timer_Tick;
        }

        #endregion

        #region Public Properties

        private UIElement _leftPanel = null;
        public UIElement LeftPanel
        {
            get
            {
                return _leftPanel;
            }
            set
            {
                _leftPanel = value;

                pnlLeft.Content = _leftPanel;
            }
        }

        public ItemDetailPanel DetailPanel
        {
            get
            {
                return pnlDetail;
            }
        }

        private Key[] _closePanelKeys = new Key[] { Key.Escape, Key.Enter };
        /// <summary>
        /// If the use presses any of these keys, the ClosePanel event will fire
        /// </summary>
        public Key[] ClosePanelKeys
        {
            get
            {
                return _closePanelKeys;
            }
            set
            {
                _closePanelKeys = value;
            }
        }

        private BotShellColorsDNA _backdropColors = null;
        public BotShellColorsDNA BackdropColors
        {
            get
            {
                return _backdropColors;
            }
            set
            {
                _backdropColors = value;

                if (_backdropColors == null)
                {
                    _graphic = null;
                    if (_backVisual != null)
                    {
                        _backVisual.Content = GetBlankModel();
                    }
                }
                else
                {
                    _graphic = new SpriteGraphic_Shells(_backdropColors, 5, _cameraLength * 10, _itemOptions.Value);

                    if (_backVisual != null)
                    {
                        _backVisual.Content = _graphic.Model;
                    }
                }
            }
        }

        private bool _is3DPanel = true;
        public bool Is3DPanel
        {
            get
            {
                return _is3DPanel;
            }
            set
            {
                if (_is3DPanel == value)
                {
                    return;
                }

                _is3DPanel = value;

                Update2D3DPanels();
            }
        }

        #endregion

        #region Public Methods

        public void ClearPanels()
        {
            this.LeftPanel = null;
            this.DetailPanel.Item = null;
        }

        #endregion
        #region Protected Methods

        protected virtual void OnClosePanel()
        {
            if (this.ClosePanel != null)
            {
                this.ClosePanel(this, new EventArgs());
            }
        }

        #endregion

        #region Event Listeners

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //_quick = new BackdropQuick();
                //_quick.Show();
                //_quick.ValueChanged += _quick_ValueChanged;


                //NOTE: This MUST be set in loaded, or keyboard events get very flaky (even if the usercontrol has focus, something
                //inside of it must also have focus)
                lblFocusable.Focus();

                // Setting these up now so that the order stays consistent (so that semitransparency will work)

                #region Back visual

                _backVisual = new ModelVisual3D();
                if (_graphic == null)
                {
                    _backVisual.Content = GetBlankModel();
                }
                else
                {
                    _backVisual.Content = _graphic.Model;
                }

                _viewport.Children.Add(_backVisual);

                #endregion

                #region 2D panel

                DiffuseMaterial diffuse = new DiffuseMaterial(Brushes.White);
                Viewport2DVisual3D.SetIsVisualHostMaterial(diffuse, true);

                grd3D = new Grid();

                grd3D.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                grd3D.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(.5, GridUnitType.Star) });
                grd3D.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(pnlLeft, 0);
                Grid.SetColumn(pnlDetail, 2);

                grd3D.Children.Add(pnlLeft);
                grd3D.Children.Add(pnlDetail);

                _visual2D3D = new Viewport2DVisual3D();
                _visual2D3D.Material = diffuse;
                //_visual2D3D.Geometry = GetGeometry(_quick.CylinderNumSegments, _quick.CylinderThetaOffset, _quick.CylinderHeight, _quick.CylinderRadius, _quick.CylinderTranslate);
                _visual2D3D.Geometry = GetGeometry(NUMSEGMENTS, THETA, HEIGHT, RADIUS, TRANSLATE);
                _visual2D3D.Visual = grd3D;
                _visual2D3D.Transform = new TranslateTransform3D(0, 0, _cameraLength * .3);

                _viewport.IsHitTestVisible = true;

                _viewport.Children.Add(_visual2D3D);

                #endregion

                #region Detail Visual

                _detailVisual = new ModelVisual3D();
                _detailVisual.Content = GetBlankModel();

                Transform3DGroup transform = new Transform3DGroup();

                _detailRotationInitial = new QuaternionRotation3D();
                transform.Children.Add(new RotateTransform3D(_detailRotationInitial));

                _detailRotationAnimate = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
                transform.Children.Add(new RotateTransform3D(_detailRotationAnimate));

                _detailTranslate = new TranslateTransform3D(0, 0, 0);       // this one is so the model can be centered on zero
                transform.Children.Add(_detailTranslate);

                _detailScale = new ScaleTransform3D(1, 1, 1);
                transform.Children.Add(_detailScale);

                transform.Children.Add(new TranslateTransform3D(_cameraLength * .1, 0, 0));     // this one is so the visual is shifted into the final place on screen

                _detailVisual.Transform = transform;

                _viewport.Children.Add(_detailVisual);

                UpdateDetailVisual();

                #endregion

                _detailAnimate = AnimateRotation.Create_Constant(_detailRotationAnimate, 30);

                chkIs3D.IsChecked = _is3DPanel;

                _isInitialized = true;

                Update2D3DPanels();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkIs3D_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Is3DPanel = chkIs3D.IsChecked.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_closePanelKeys != null && _closePanelKeys.Contains(e.Key))
                {
                    e.Handled = true;
                    OnClosePanel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <remarks>
        /// This quits firing when they are doing a drag drop.  The solution is not trivial:
        /// http://social.msdn.microsoft.com/Forums/vstudio/en-US/1053aaa4-d8b6-48d7-8d53-2af98e60d542/dodragdrop-disables-mousemove-events?forum=wpf
        /// 
        /// I tried calling GetCursorPos API and use a Timer to compare the mouse position, when the position changes, that mean mouse is moving.
        /// </remarks>
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            const double SCALE = .18;

            //TODO: Only do this when in 3D mode

            try
            {
                if (_camera == null)
                {
                    return;
                }

                double width = this.ActualWidth;
                double halfWidth = width / 2d;
                double height = this.ActualHeight;
                double halfHeight = height / 2d;

                Point point = e.GetPosition(this);
                Vector percent = new Vector((point.X - halfWidth) / halfWidth, (point.Y - halfHeight) / halfHeight);

                _camera.Position = new Point3D(-percent.X * SCALE, percent.Y * SCALE, _cameraZ);
                _camera.LookDirection = _camera.Position.ToVector() * -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnClosePanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_timer != null)
                {
                    _lastTick = DateTime.UtcNow;
                    _timer.IsEnabled = this.IsVisible;
                }

                if (this.IsVisible)
                {
                    Resize3DPanels();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_graphic == null)
                {
                    return;
                }

                DateTime newTime = DateTime.UtcNow;
                double elapsedTime = (newTime - _lastTick).TotalSeconds;
                _lastTick = newTime;

                _graphic.Update(elapsedTime);
                _detailAnimate.Tick(elapsedTime);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                Resize3DPanels();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void pnlDetail_ItemChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateDetailVisual();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "BackdropPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _quick_ValueChanged(object sender, EventArgs e)
        {
            Resize3DPanels();
        }

        #endregion

        #region Private Methods

        private void Update2D3DPanels()
        {
            if (!_isInitialized)
            {
                return;
            }

            grd2D.Children.Clear();
            grd3D.Children.Clear();
            grd2D.Visibility = Visibility.Collapsed;

            _viewport.Children.Remove(_visual2D3D);
            _viewport.Children.Remove(_detailVisual);

            if (_is3DPanel)
            {
                grd3D.Children.Add(pnlLeft);
                grd3D.Children.Add(pnlDetail);

                _viewport.Children.Add(_visual2D3D);

                Resize3DPanels();
            }
            else
            {
                grd2D.Children.Add(pnlLeft);
                grd2D.Children.Add(pnlDetail);

                grd2D.Visibility = Visibility.Visible;
            }

            _viewport.Children.Add(_detailVisual);
        }
        private void Resize3DPanels()
        {
            if (!_isInitialized || !_is3DPanel || _visual2D3D == null)
            {
                return;
            }

            //grd3D.Width = this.ActualWidth * _quick.PixelMultiplier;
            //grd3D.Height = this.ActualHeight * _quick.PixelMultiplier;
            grd3D.Width = this.ActualWidth * PIXELMULT;
            grd3D.Height = this.ActualHeight * PIXELMULT;

            //_visual2D3D.Geometry = GetGeometry(_quick.CylinderNumSegments, _quick.CylinderThetaOffset, _quick.CylinderHeight, _quick.CylinderRadius, _quick.CylinderTranslate);
            _visual2D3D.Geometry = GetGeometry(NUMSEGMENTS, THETA, HEIGHT, RADIUS, TRANSLATE);
        }

        private static Geometry3D GetGeometry_ORIG()
        {
            MeshGeometry3D retVal = UtilityWPF.GetSquare2D(10);
            retVal.TextureCoordinates.Add(new Point(0, 0));
            retVal.TextureCoordinates.Add(new Point(0, 1));
            retVal.TextureCoordinates.Add(new Point(1, 1));
            retVal.TextureCoordinates.Add(new Point(1, 0));

            return retVal;
        }
        /// <summary>
        /// This defines the back side of a cylinder
        /// </summary>
        private static Geometry3D GetGeometry(int numSegments, double thetaOffset, double height, double radius, double translate)
        {
            double thetaStart = 270 - thetaOffset;
            double thetaStop = 270 + thetaOffset;

            MeshGeometry3D retVal = new MeshGeometry3D();

            #region Initial calculations

            // The rest of this method has height along Z, but the final needs to go along Y
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90d)));
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 180d)));
            transform.Children.Add(new TranslateTransform3D(0, 0, translate));

            double halfHeight = height / 2d;

            Point[] points = new Point[numSegments];

            double deltaTheta = Math1D.DegreesToRadians((thetaStop - thetaStart) / (numSegments - 1));        //NOTE: This will fail if theta start/stop goes past 0/360
            double theta = Math1D.DegreesToRadians(thetaStart);

            for (int cntr = 0; cntr < numSegments; cntr++)
            {
                points[cntr] = new Point(Math.Cos(theta) * radius, Math.Sin(theta) * radius);
                theta += deltaTheta;
            }

            #endregion

            for (int cntr = 0; cntr < numSegments; cntr++)
            {
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X, points[cntr].Y, -halfHeight)));
                retVal.Positions.Add(transform.Transform(new Point3D(points[cntr].X, points[cntr].Y, halfHeight)));

                retVal.Normals.Add(transform.Transform(new Vector3D(-points[cntr].X, -points[cntr].Y, 0d)));		// the normals point straight in from the side
                retVal.Normals.Add(transform.Transform(new Vector3D(-points[cntr].X, -points[cntr].Y, 0d)));

                double coord = Convert.ToDouble(cntr) / Convert.ToDouble(numSegments - 1);
                retVal.TextureCoordinates.Add(new Point(coord, 0));
                retVal.TextureCoordinates.Add(new Point(coord, 1));
            }

            for (int cntr = 0; cntr < numSegments - 1; cntr++)
            {
                // 0,2,3
                retVal.TriangleIndices.Add((cntr * 2) + 0);
                retVal.TriangleIndices.Add((cntr * 2) + 3);
                retVal.TriangleIndices.Add((cntr * 2) + 2);

                // 0,3,1
                retVal.TriangleIndices.Add((cntr * 2) + 0);
                retVal.TriangleIndices.Add((cntr * 2) + 1);
                retVal.TriangleIndices.Add((cntr * 2) + 3);
            }

            return retVal;
        }

        private void UpdateDetailVisual()
        {
            const double IDEALRATIO = .4d;

            if (_detailVisual == null)
            {
                return;
            }

            if (pnlDetail.Item != null && pnlDetail.Model != null)
            {
                _detailVisual.Content = UtilityCore.Clone(pnlDetail.Model);
                _detailRotationInitial.Quaternion = pnlDetail.ModelRotate;

                Rect3D bounds = _detailVisual.Content.Bounds;

                // Center it so that it's at 0,0,0 in model coords
                _detailTranslate.OffsetX = -(bounds.X + (bounds.SizeX / 2d));
                _detailTranslate.OffsetY = -(bounds.Y + (bounds.SizeY / 2d));
                _detailTranslate.OffsetZ = -(bounds.Z + (bounds.SizeZ / 2d));

                // Scale it to the ideal size on screen
                double size = bounds.DiagonalLength();

                double scale = IDEALRATIO / (size / _cameraLength);

                _detailScale.ScaleX = scale;
                _detailScale.ScaleY = scale;
                _detailScale.ScaleZ = scale;
            }
            else
            {
                _detailVisual.Content = GetBlankModel();
            }
        }

        private static Model3D GetBlankModel()
        {
            GeometryModel3D retVal = new GeometryModel3D();

            DiffuseMaterial material = new DiffuseMaterial(Brushes.Transparent);

            retVal.Material = material;
            retVal.BackMaterial = material;

            retVal.Geometry = UtilityWPF.GetSquare2D(1);

            return retVal;
        }
        private static Model3D GetBlankModel_DOT()
        {
            GeometryModel3D retVal = new GeometryModel3D();

            DiffuseMaterial material = new DiffuseMaterial(Brushes.Red);

            retVal.Material = material;
            retVal.BackMaterial = material;

            retVal.Geometry = UtilityWPF.GetSphere_LatLon(8, 1);

            return retVal;
        }

        #endregion
    }
}
