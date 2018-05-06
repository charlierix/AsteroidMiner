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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;

namespace Game.Newt.Testers
{
    public partial class EvenDistributionOther : Window
    {
        #region enum: BoundryShape

        private enum BoundryShape
        {
            Cube,
            Cone,
            /// <summary>
            /// There is no external shape constraint.  This is only useful when there are links binding the points
            /// </summary>
            Open_Links,
        }

        #endregion
        #region class: ConeBoundries

        private class ConeBoundries
        {
            public ConeBoundries(double heightMin, double heightMax, double angle, Vector3D axisUnit, Vector3D upUnit, double dot)
            {
                this.HeightMin = heightMin;
                this.HeightMax = heightMax;
                this.Angle = angle;
                this.AxisUnit = axisUnit;
                this.UpUnit = upUnit;
                this.Dot = dot;
            }

            public readonly double HeightMin;
            public readonly double HeightMax;

            public readonly double Angle;

            public readonly Vector3D AxisUnit;
            public readonly Vector3D UpUnit;

            public readonly double Dot;
        }

        #endregion
        #region class: Dot

        private class Dot
        {
            #region Declaration Section

            private const string DOTCOLOR = "808080";
            private const string DOTCOLOR_STATIC = "B84D4D";
            private const double DOTRADIUS = .2;

            private TranslateTransform3D _transform = null;

            #endregion

            #region Constructor

            public Dot(bool isStatic, Point3D position, double repulseMult = 1d)
            {
                this.IsStatic = isStatic;
                _position = position;
                this.RepulseMult = repulseMult;

                ModelVisual3D model = BuildDot(isStatic, repulseMult);
                this.Visual = model;

                _transform = new TranslateTransform3D(position.ToVector());
                model.Transform = _transform;
            }

            #endregion

            #region Public Properties

            public readonly bool IsStatic;

            private Point3D _position;
            public Point3D Position
            {
                get
                {
                    return _position;
                }
                set
                {
                    _position = value;
                    _transform.OffsetX = _position.X;
                    _transform.OffsetY = _position.Y;
                    _transform.OffsetZ = _position.Z;
                }
            }

            public readonly double RepulseMult;

            public Visual3D Visual
            {
                get;
                private set;
            }

            #endregion

            #region Private Methods

            private static ModelVisual3D BuildDot(bool isStatic, double repulseMult = 1d)
            {
                Color color = UtilityWPF.ColorFromHex(isStatic ? DOTCOLOR_STATIC : DOTCOLOR);

                double radius = DOTRADIUS * repulseMult;
                if (repulseMult > 1)
                {
                    radius = DOTRADIUS * UtilityCore.GetScaledValue(1, 3, 1, 10, repulseMult);
                }

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, radius, radius, radius);

                // Model Visual
                ModelVisual3D retVal = new ModelVisual3D();
                retVal.Content = geometry;

                // Exit Function
                return retVal;
            }

            #endregion
        }

        #endregion
        #region class: Link

        private class Link
        {
            public Tuple<int, int> Index { get; set; }

            public BillboardLine3D Line { get; set; }
            public ModelVisual3D Visual { get; set; }
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<Dot> _dots = new List<Dot>();
        private List<Link> _links = new List<Link>();

        private readonly Effect _errorEffect;

        private readonly DispatcherTimer _timer;

        private BoundryShape _shape;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public EvenDistributionOther()
        {
            InitializeComponent();

            _errorEffect = new DropShadowEffect()
            {
                Color = UtilityWPF.ColorFromHex("FF0000"),
                BlurRadius = 4,
                Direction = 0,
                Opacity = .5,
                ShadowDepth = 0,
            };

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(10);
            _timer.Tick += Timer_Tick;

            cboShape.Items.Add(BoundryShape.Cube);
            cboShape.Items.Add(BoundryShape.Cone);
            cboShape.Items.Add(BoundryShape.Open_Links);

            _initialized = true;

            cboShape.SelectedItem = BoundryShape.Cube;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Camera Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cboShape_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized || cboShape.SelectedItem == null)
                {
                    return;
                }

                var cubeProps = new UIElement[] { lblSizeX, txtSizeX, lblSizeY, txtSizeY, lblSizeZ, txtSizeZ, chkSeparateLinkSets };
                var openProps = UtilityCore.Iterate<UIElement>(cubeProps, lblLinkScale, trkLinkScale).ToArray();
                var coneProps = new UIElement[] { lblHeightMin, txtHeightMin, lblHeightMax, txtHeightMax, lblAngle, txtAngle };

                // Switch visibilities
                BoundryShape selected = (BoundryShape)cboShape.SelectedItem;     // null check at the top of the method
                switch (selected)
                {
                    case BoundryShape.Cube:
                        foreach (var control in coneProps) control.Visibility = Visibility.Collapsed;
                        foreach (var control in openProps) control.Visibility = Visibility.Collapsed;
                        foreach (var control in cubeProps) control.Visibility = Visibility.Visible;
                        break;

                    case BoundryShape.Open_Links:
                        foreach (var control in cubeProps) control.Visibility = Visibility.Collapsed;
                        foreach (var control in coneProps) control.Visibility = Visibility.Collapsed;
                        foreach (var control in openProps) control.Visibility = Visibility.Visible;
                        break;

                    case BoundryShape.Cone:
                        foreach (var control in openProps) control.Visibility = Visibility.Collapsed;
                        foreach (var control in cubeProps) control.Visibility = Visibility.Collapsed;
                        foreach (var control in coneProps) control.Visibility = Visibility.Visible;
                        break;

                    default:
                        throw new ApplicationException("Unknown Shape: " + selected.ToString());
                }

                _shape = selected;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Double_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                TextBox cast = sender as TextBox;
                if (cast == null)
                {
                    return;
                }

                double parsed;
                if (double.TryParse(cast.Text, out parsed))
                {
                    cast.Effect = null;
                }
                else
                {
                    cast.Effect = _errorEffect;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Int_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                TextBox cast = sender as TextBox;
                if (cast == null)
                {
                    return;
                }

                int parsed;
                if (int.TryParse(cast.Text, out parsed))
                {
                    cast.Effect = null;
                }
                else
                {
                    cast.Effect = _errorEffect;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddDots(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddStaticDots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddDots(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddLinks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddLinks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Step(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkContinuous_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                if (chkContinuous.IsChecked.Value)
                {
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!chkContinuous.IsChecked.Value)
                {
                    _timer.Stop();
                    return;
                }

                Step(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RandomizeMovable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Point3D, Point3D> cubeAABB;
                ConeBoundries cone;
                if (!GetShapeBoundries(out cubeAABB, out cone))
                {
                    MessageBox.Show("Invalid cube/cone definition", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (Dot dot in _dots)
                {
                    if (!dot.IsStatic)
                    {
                        dot.Position = GetRandomPosition(_shape, cubeAABB, cone);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BatchMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Step(200);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// This algorithm was built for a database table visualization.  This button is test case from a particular db that caused the dots to
        /// go unstable.  The final fix was to cap the forces
        /// </summary>
        private void RMSTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearDots_Click(this, null);

                cboShape.SelectedValue = BoundryShape.Open_Links;

                // Dots
                //_dots.Add(new Dot(false, new Point3D(0, 0, 0)));
                _dots.Add(new Dot(false, new Point3D(28.1382135712254, 99.3373016823722, 99.6983332558062)));
                _dots.Add(new Dot(false, new Point3D(40.9378037512944, -54.7950695989631, 16.7918111741505)));
                _dots.Add(new Dot(false, new Point3D(-6.00426844600787, 19.6806911005083, -80.2413779218874)));
                _dots.Add(new Dot(false, new Point3D(96.3785327954118, 91.2495168816529, -10.5310818695142)));
                _dots.Add(new Dot(false, new Point3D(36.4407546522285, -41.9503007745139, -36.9630613070741)));
                _dots.Add(new Dot(false, new Point3D(56.8018845081338, 19.2195065874697, 81.228697198084)));
                _dots.Add(new Dot(false, new Point3D(47.456881845117, -47.9834844116045, 33.0914251194761)));
                _dots.Add(new Dot(false, new Point3D(-45.8828376354104, 58.8653441326997, -26.1631502426058)));
                _dots.Add(new Dot(false, new Point3D(-91.8877908922209, -72.8348870635195, -43.0900225150818)));
                _dots.Add(new Dot(false, new Point3D(9.23945275565583, -40.8932167761462, 67.630518771536)));
                _dots.Add(new Dot(false, new Point3D(-46.7030362909208, -16.1544773337219, 10.0979127502525)));
                _dots.Add(new Dot(false, new Point3D(-86.5745079641112, 9.5707562330974, 4.64237244084589)));
                _dots.Add(new Dot(false, new Point3D(-24.5299446976417, 62.6137059007835, -44.7178657840555)));
                _dots.Add(new Dot(false, new Point3D(70.895431084044, -51.1306804377263, 62.5359908503182)));
                _dots.Add(new Dot(false, new Point3D(-99.1975096050638, -30.9270098483781, -32.9263331056229)));
                _dots.Add(new Dot(false, new Point3D(-78.876582243888, -72.5648381619551, 39.273297199641)));
                _dots.Add(new Dot(false, new Point3D(4.78777908943025, -36.0594595484712, 30.7405863566048)));
                _dots.Add(new Dot(false, new Point3D(-41.3635533961298, -99.1487280461699, -69.9452902981757)));
                _dots.Add(new Dot(false, new Point3D(88.5000952465926, -25.9789487933642, -59.5280424503274)));
                _dots.Add(new Dot(false, new Point3D(25.861483498412, 32.8255946435154, -81.9601825354435)));
                _dots.Add(new Dot(false, new Point3D(-40.1181663107677, 84.7562787983363, -39.4260921233455)));
                _dots.Add(new Dot(false, new Point3D(-47.8718966934233, 43.0815690863326, 7.40399421537481)));
                _dots.Add(new Dot(false, new Point3D(79.3710053802333, 23.0152626163397, 48.4789429923887)));
                _dots.Add(new Dot(false, new Point3D(58.3945662520801, -18.6681707942245, 56.6058006866862)));
                _dots.Add(new Dot(false, new Point3D(25.9465629821394, 76.561450761073, -96.8528039738782)));
                _dots.Add(new Dot(false, new Point3D(70.5554342691579, -46.6853280303466, -10.2076460189222)));
                _dots.Add(new Dot(false, new Point3D(-93.2368171369828, 86.988791351667, 99.7299510984355)));
                _dots.Add(new Dot(false, new Point3D(17.6366802852772, -95.5483263337744, 95.1662427723251)));
                _dots.Add(new Dot(false, new Point3D(-63.1100675850688, 94.660517105209, -17.0057492875521)));
                _dots.Add(new Dot(false, new Point3D(-19.9567969515719, -75.0746032107037, -64.4502949735384)));
                _dots.Add(new Dot(false, new Point3D(-35.8295851088267, 49.6085718039463, -70.2118887427318)));
                _dots.Add(new Dot(false, new Point3D(-62.757683248612, 11.0135973948117, -35.8869592360626)));
                _dots.Add(new Dot(false, new Point3D(1.96208297366374, 48.6743870883595, 25.9914210652893)));
                _dots.Add(new Dot(false, new Point3D(59.6696726790022, -58.2475876241213, 4.41989922170522)));
                _dots.Add(new Dot(false, new Point3D(90.7943542072523, 46.3932128373502, 82.6087112457532)));
                _dots.Add(new Dot(false, new Point3D(74.1347856699185, 32.6898836217308, -75.7101788072429)));
                _dots.Add(new Dot(false, new Point3D(-73.0924863242975, -82.0553390225653, -79.2936207630176)));
                _dots.Add(new Dot(false, new Point3D(50.6796035685947, 19.0983006353948, 45.8368032918483)));
                _dots.Add(new Dot(false, new Point3D(-81.6901336338791, 42.2451534039551, 80.3046051321107)));
                _dots.Add(new Dot(false, new Point3D(-34.5923348956706, -84.7618291083546, 48.4210519811236)));
                _dots.Add(new Dot(false, new Point3D(-75.5902564970731, -0.672197668194869, -1.91013417295652)));
                _dots.Add(new Dot(false, new Point3D(12.9292379659271, -5.77584863909327, 31.7232574018292)));
                _dots.Add(new Dot(false, new Point3D(26.817689429418, -11.2957537692486, -34.4521466337387)));
                _dots.Add(new Dot(false, new Point3D(39.0341552621844, -31.4066487045058, 4.64028488129391)));
                _dots.Add(new Dot(false, new Point3D(63.8009329157886, -52.9064898159851, 45.2363789757883)));
                _dots.Add(new Dot(false, new Point3D(-4.68994812326969, 26.8423260780248, -41.9415391711246)));
                _dots.Add(new Dot(false, new Point3D(-87.4424684734282, -37.2448532549873, -38.0293665165218)));
                _dots.Add(new Dot(false, new Point3D(-41.2955704803092, -46.8643106272744, -93.0192641881384)));
                _dots.Add(new Dot(false, new Point3D(-85.1566742105208, 13.4908113225786, -69.4897288314485)));
                _dots.Add(new Dot(false, new Point3D(-16.0486920345801, -81.0675496147329, 68.7684439908566)));
                _dots.Add(new Dot(false, new Point3D(-16.1915643681733, -63.4455821306657, 33.4362161967141)));
                _dots.Add(new Dot(false, new Point3D(77.5703690841656, 35.2599291760753, 42.4246100440736)));
                _dots.Add(new Dot(false, new Point3D(-93.6699666053382, -22.1348837586748, -47.8309385235565)));
                _dots.Add(new Dot(false, new Point3D(-49.114546156076, -52.6829037594995, -56.0143626090206)));
                _dots.Add(new Dot(false, new Point3D(58.7419678264959, -12.1266415864819, 49.3513096819405)));
                _dots.Add(new Dot(false, new Point3D(16.0660943556885, 86.8786706528061, -27.9952095486201)));
                _dots.Add(new Dot(false, new Point3D(-99.39957568394, 22.9998144893906, -84.5971726740697)));
                _dots.Add(new Dot(false, new Point3D(22.2461443032353, -47.1498664222424, 52.4830241466328)));
                _dots.Add(new Dot(false, new Point3D(-13.5495815023545, 65.7053139832361, -53.8078870409205)));
                _dots.Add(new Dot(false, new Point3D(-8.8908699848181, -1.91408782355212, 80.7333400383281)));
                _dots.Add(new Dot(false, new Point3D(1.2129862332777, -57.1336185360018, -30.2282041545157)));
                _dots.Add(new Dot(false, new Point3D(-3.22059062459533, -44.7742803696423, -67.9610665738401)));
                _dots.Add(new Dot(false, new Point3D(71.2040686845799, 86.230563831623, 11.8335810079396)));
                _dots.Add(new Dot(false, new Point3D(-97.1882310682853, -11.0199815179314, -51.0227901633004)));
                _dots.Add(new Dot(false, new Point3D(-94.110600647568, 61.6720776826479, -84.5619494954878)));
                _dots.Add(new Dot(false, new Point3D(-82.0150039075012, -0.0375383068050894, 65.2623309592075)));
                _dots.Add(new Dot(false, new Point3D(-42.3705738700789, -1.22276856620925, 26.6121406697725)));
                _dots.Add(new Dot(false, new Point3D(58.5054807171717, -16.6491163506401, -4.0673641041235)));
                _dots.Add(new Dot(false, new Point3D(53.3656166649263, 61.5622913285914, 83.7042842915767)));
                _dots.Add(new Dot(false, new Point3D(80.9531920500813, -8.88004941347988, 69.5546151928392)));
                _dots.Add(new Dot(false, new Point3D(-3.76750291500591, 15.2209033794798, 79.7792040648773)));
                _dots.Add(new Dot(false, new Point3D(-28.5642785618847, 49.6724676106463, -95.5492852234977)));
                _dots.Add(new Dot(false, new Point3D(74.2138415454951, -38.0374415489088, -67.3523612168396)));
                _dots.Add(new Dot(false, new Point3D(17.3123762557806, 44.8620256711087, -99.3518931788168)));
                _dots.Add(new Dot(false, new Point3D(60.1712094434403, 97.7886553843453, -65.980203992678)));
                _dots.Add(new Dot(false, new Point3D(66.4256174892306, 16.3567449508033, -8.82194410489031)));
                _dots.Add(new Dot(false, new Point3D(37.0449736421206, -31.5345775948533, -34.2571477099588)));
                _dots.Add(new Dot(false, new Point3D(-19.070218000128, -66.5202961147392, 99.3086807426571)));
                _dots.Add(new Dot(false, new Point3D(-45.8788006314443, 42.707505516106, 59.5154978146383)));
                _dots.Add(new Dot(false, new Point3D(73.8391599496077, 43.4137927104783, -6.33657169823375)));
                _dots.Add(new Dot(false, new Point3D(-51.6653508654169, 90.2508766344985, -4.88938675489715)));
                _dots.Add(new Dot(false, new Point3D(42.2789658151003, 6.5792718467206, 73.7591151025887)));
                _dots.Add(new Dot(false, new Point3D(-30.8019942281777, 34.4536779143166, -88.0003899279984)));
                _dots.Add(new Dot(false, new Point3D(-89.0126642719901, -56.2288454529964, -62.0000967578963)));
                _dots.Add(new Dot(false, new Point3D(32.6146921760471, 40.3170498741405, 53.9152057626821)));
                _dots.Add(new Dot(false, new Point3D(25.9640338485893, 98.3342712737314, -14.4377717349854)));
                _dots.Add(new Dot(false, new Point3D(-38.0871601114455, 86.9399991756957, -54.7944536222119)));
                _dots.Add(new Dot(false, new Point3D(-7.47377160353297, -56.0917815920393, -77.3454718186266)));
                _dots.Add(new Dot(false, new Point3D(3.81176290279801, -84.6972849148779, -18.258800505781)));
                _dots.Add(new Dot(false, new Point3D(80.4705233222202, -82.6854779304403, -93.0350379054598)));
                _dots.Add(new Dot(false, new Point3D(-55.064783038136, -99.6253184041126, 18.5487657406129)));
                _dots.Add(new Dot(false, new Point3D(38.9842104813942, -31.0222728788025, 54.6111490366101)));
                _dots.Add(new Dot(false, new Point3D(5.53749357608031, -82.10775637166, -8.79061646237533)));
                _dots.Add(new Dot(false, new Point3D(-39.7393190952667, -2.77238828259165, 81.9030670364867)));
                _dots.Add(new Dot(false, new Point3D(-20.8215541768919, 26.0576379141107, -75.3057321418569)));
                _dots.Add(new Dot(false, new Point3D(-72.2570509520625, 48.3150898238249, -6.8373459888796)));
                _dots.Add(new Dot(false, new Point3D(-54.606525020025, 28.1571655199664, 44.3732342423747)));
                _dots.Add(new Dot(false, new Point3D(-26.0467304503763, 11.9263200610533, 56.4737935347826)));
                _dots.Add(new Dot(false, new Point3D(-51.5421180760218, 55.8084207381161, 46.3426582265378)));
                _dots.Add(new Dot(false, new Point3D(-27.5439149362705, -61.5327970876977, -8.72344323840153)));
                _dots.Add(new Dot(false, new Point3D(-7.98208439163029, -11.2725175503979, 17.1391558447569)));
                _dots.Add(new Dot(false, new Point3D(-94.9653520225386, 66.0521187661458, -56.6035270488837)));
                _dots.Add(new Dot(false, new Point3D(19.4511375014908, 93.6304816946529, -28.6606772470571)));
                _dots.Add(new Dot(false, new Point3D(99.304056726072, -79.573459727491, 80.4420276453914)));
                _dots.Add(new Dot(false, new Point3D(94.3528447273899, -98.3478410161789, -10.2876125417126)));
                _dots.Add(new Dot(false, new Point3D(-36.6975206586986, -86.652217426641, 17.85058049385)));
                _dots.Add(new Dot(false, new Point3D(97.9602603232303, -23.9311861451395, -33.0123747387027)));
                _dots.Add(new Dot(false, new Point3D(88.5785454830986, 35.0770483422452, -10.8426434504067)));
                _dots.Add(new Dot(false, new Point3D(-37.4082721478344, 70.9819474122403, -11.5516384651659)));
                _dots.Add(new Dot(false, new Point3D(62.0749722058303, -9.47367144258399, 13.1693063830814)));
                _dots.Add(new Dot(false, new Point3D(-91.7315091899277, -66.9185914876492, 79.4250407160376)));
                _dots.Add(new Dot(false, new Point3D(99.9328267760262, 68.2427652963636, -91.4998707321938)));
                _dots.Add(new Dot(false, new Point3D(-35.2360888082702, -25.8562021543534, 60.0055191479649)));
                _dots.Add(new Dot(false, new Point3D(81.2977949070268, 8.29181154644667, 54.6846081291719)));
                _dots.Add(new Dot(false, new Point3D(-78.1766687418226, -53.910581746097, 7.73062524745735)));
                _dots.Add(new Dot(false, new Point3D(63.9312065969832, -20.3995751777662, 10.2741610772322)));
                _dots.Add(new Dot(false, new Point3D(-33.2385939235047, 85.1554025826768, 42.4606381647571)));
                _dots.Add(new Dot(false, new Point3D(-71.5079222673122, -25.5041752595008, 62.3983890574418)));
                _dots.Add(new Dot(false, new Point3D(-75.7110684996988, 3.4393701252711, 53.6504341073569)));
                _dots.Add(new Dot(false, new Point3D(-72.0182007048364, 42.4429201252958, 95.0701713539056)));
                _dots.Add(new Dot(false, new Point3D(54.9481114162822, 57.3761652956606, 3.10415313723691)));

                foreach (Dot dot in _dots)
                {
                    _viewport.Children.Add(dot.Visual);
                }


                trkLinkScale.Value = 3;

                double mult = 20 / 3;

                foreach(Dot dot in _dots)
                {
                    dot.Position = (dot.Position.ToVector() / mult).ToPoint();
                }


                // Links
                Tuple<int, int>[] links = new Tuple<int, int>[]
                {
                    //Tuple.Create(),
Tuple.Create(0, 2),
Tuple.Create(0, 5),
Tuple.Create(0, 3),
Tuple.Create(0, 4),
Tuple.Create(0, 1),
Tuple.Create(1, 8),
Tuple.Create(1, 10),
Tuple.Create(1, 5),
Tuple.Create(1, 6),
Tuple.Create(1, 7),
Tuple.Create(1, 9),
Tuple.Create(2, 11),
Tuple.Create(12, 3),
Tuple.Create(3, 5),
Tuple.Create(13, 4),
Tuple.Create(6, 5),
Tuple.Create(7, 5),
Tuple.Create(14, 5),
Tuple.Create(9, 5),
Tuple.Create(15, 5),
Tuple.Create(12, 5),
Tuple.Create(40, 5),
Tuple.Create(16, 5),
Tuple.Create(17, 5),
Tuple.Create(18, 5),
Tuple.Create(19, 5),
Tuple.Create(20, 5),
Tuple.Create(21, 5),
Tuple.Create(22, 5),
Tuple.Create(23, 5),
Tuple.Create(24, 5),
Tuple.Create(25, 5),
Tuple.Create(26, 5),
Tuple.Create(27, 5),
Tuple.Create(39, 5),
Tuple.Create(28, 5),
Tuple.Create(29, 5),
Tuple.Create(30, 5),
Tuple.Create(31, 5),
Tuple.Create(32, 5),
Tuple.Create(33, 5),
Tuple.Create(5, 36),
Tuple.Create(5, 35),
Tuple.Create(5, 38),
Tuple.Create(5, 37),
Tuple.Create(5, 34),
Tuple.Create(6, 8),
Tuple.Create(6, 41),
Tuple.Create(6, 10),
Tuple.Create(6, 17),
Tuple.Create(6, 14),
Tuple.Create(7, 8),
Tuple.Create(7, 41),
Tuple.Create(7, 10),
Tuple.Create(9, 41),
Tuple.Create(11, 41),
Tuple.Create(12, 22),
Tuple.Create(12, 17),
Tuple.Create(12, 30),
Tuple.Create(15, 13),
Tuple.Create(51, 13),
Tuple.Create(42, 13),
Tuple.Create(13, 43),
Tuple.Create(13, 44),
Tuple.Create(13, 45),
Tuple.Create(13, 46),
Tuple.Create(13, 30),
Tuple.Create(13, 48),
Tuple.Create(13, 47),
Tuple.Create(13, 32),
Tuple.Create(13, 31),
Tuple.Create(13, 49),
Tuple.Create(13, 53),
Tuple.Create(13, 50),
Tuple.Create(13, 52),
Tuple.Create(14, 17),
Tuple.Create(15, 54),
Tuple.Create(16, 41),
Tuple.Create(16, 31),
Tuple.Create(58, 17),
Tuple.Create(17, 55),
Tuple.Create(17, 18),
Tuple.Create(17, 20),
Tuple.Create(17, 45),
Tuple.Create(17, 57),
Tuple.Create(17, 56),
Tuple.Create(17, 27),
Tuple.Create(17, 21),
Tuple.Create(59, 18),
Tuple.Create(59, 19),
Tuple.Create(19, 20),
Tuple.Create(21, 22),
Tuple.Create(21, 30),
Tuple.Create(60, 22),
Tuple.Create(22, 61),
Tuple.Create(22, 23),
Tuple.Create(22, 24),
Tuple.Create(22, 62),
Tuple.Create(22, 27),
Tuple.Create(64, 24),
Tuple.Create(63, 24),
Tuple.Create(65, 25),
Tuple.Create(43, 26),
Tuple.Create(27, 30),
Tuple.Create(65, 28),
Tuple.Create(43, 28),
Tuple.Create(66, 29),
Tuple.Create(67, 29),
Tuple.Create(44, 29),
Tuple.Create(68, 29),
Tuple.Create(29, 69),
Tuple.Create(43, 30),
Tuple.Create(30, 70),
Tuple.Create(30, 31),
Tuple.Create(40, 31),
Tuple.Create(53, 31),
Tuple.Create(31, 71),
Tuple.Create(31, 73),
Tuple.Create(31, 57),
Tuple.Create(31, 33),
Tuple.Create(31, 72),
Tuple.Create(32, 71),
Tuple.Create(41, 33),
Tuple.Create(41, 35),
Tuple.Create(41, 36),
Tuple.Create(43, 36),
Tuple.Create(36, 74),
Tuple.Create(75, 37),
Tuple.Create(76, 37),
Tuple.Create(65, 39),
Tuple.Create(77, 39),
Tuple.Create(43, 39),
Tuple.Create(40, 41),
Tuple.Create(78, 41),
Tuple.Create(79, 41),
Tuple.Create(80, 41),
Tuple.Create(81, 41),
Tuple.Create(41, 82),
Tuple.Create(41, 83),
Tuple.Create(41, 66),
Tuple.Create(41, 48),
Tuple.Create(41, 47),
Tuple.Create(41, 50),
Tuple.Create(65, 43),
Tuple.Create(43, 84),
Tuple.Create(43, 85),
Tuple.Create(54, 45),
Tuple.Create(54, 86),
Tuple.Create(54, 81),
Tuple.Create(54, 87),
Tuple.Create(54, 56),
Tuple.Create(54, 57),
Tuple.Create(58, 88),
Tuple.Create(58, 89),
Tuple.Create(59, 90),
Tuple.Create(59, 86),
Tuple.Create(59, 91),
Tuple.Create(92, 60),
Tuple.Create(60, 61),
Tuple.Create(93, 62),
Tuple.Create(94, 63),
Tuple.Create(95, 63),
Tuple.Create(63, 96),
Tuple.Create(94, 64),
Tuple.Create(64, 97),
Tuple.Create(64, 96),
Tuple.Create(65, 84),
Tuple.Create(65, 98),
Tuple.Create(65, 99),
Tuple.Create(66, 83),
Tuple.Create(66, 100),
Tuple.Create(66, 87),
Tuple.Create(71, 101),
Tuple.Create(73, 102),
Tuple.Create(75, 103),
Tuple.Create(79, 89),
Tuple.Create(89, 80),
Tuple.Create(98, 84),
Tuple.Create(104, 89),
Tuple.Create(89, 105),
Tuple.Create(89, 106),
Tuple.Create(91, 107),
Tuple.Create(94, 92),
Tuple.Create(108, 93),
Tuple.Create(109, 93),
Tuple.Create(110, 93),
Tuple.Create(111, 93),
Tuple.Create(112, 93),
Tuple.Create(94, 113),
Tuple.Create(95, 114),
Tuple.Create(115, 96),
Tuple.Create(97, 114),
Tuple.Create(116, 99),
Tuple.Create(117, 101),
Tuple.Create(118, 102),
Tuple.Create(108, 109),
Tuple.Create(113, 119),
Tuple.Create(113, 120),
                };




                foreach (Tuple<int, int> link in links)
                {
                    BillboardLine3D line = new BillboardLine3D()
                    {
                        Color = UtilityWPF.ColorFromHex("808080"),
                        Thickness = .05,
                        FromPoint = _dots[link.Item1].Position,
                        ToPoint = _dots[link.Item2].Position,
                    };

                    ModelVisual3D visual = new ModelVisual3D()
                    {
                        Content = line.Model,
                    };

                    _viewport.Children.Add(visual);

                    _links.Add(new Link()
                    {
                        Index = link,
                        Line = line,
                        Visual = visual,
                    });
                }

                UpdateLinks();
                UpdateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearDebugVisuals();

                // Links
                foreach (Link link in _links)
                {
                    _viewport.Children.Remove(link.Visual);
                }

                _links.Clear();

                // Dots
                foreach (Dot dot in _dots)
                {
                    _viewport.Children.Remove(dot.Visual);
                }

                _dots.Clear();

                UpdateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void AddDots(bool isStatic)
        {
            int count;
            if (!int.TryParse(txtNumDots.Text, out count))
            {
                MessageBox.Show("Invalid number of points", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Tuple<Point3D, Point3D> cubeAABB;
            ConeBoundries cone;
            if (!GetShapeBoundries(out cubeAABB, out cone))
            {
                MessageBox.Show("Invalid cube/cone definition", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Random rand = StaticRandom.GetRandomForThread();

            bool randomMult = chkRandomWeights.IsChecked.Value;
            double constantMult = -1;
            if (!randomMult && !double.TryParse(txtWeight.Text, out constantMult))
            {
                MessageBox.Show("Invalid weight", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ClearDebugVisuals();

            for (int cntr = 0; cntr < count; cntr++)
            {
                double mult = constantMult;
                if (randomMult)
                {
                    if (rand.NextBool())
                    {
                        mult = rand.NextDouble(.3, 1);
                    }
                    else
                    {
                        mult = 1 + (rand.NextPow(5, 9));
                    }
                }

                Point3D position = GetRandomPosition(_shape, cubeAABB, cone);
                Dot dot = new Dot(isStatic, position, mult);

                _dots.Add(dot);
                _viewport.Children.Add(dot.Visual);
            }

            UpdateReport();
        }
        private void AddLinks()
        {
            int count;
            if (!int.TryParse(txtNumDots.Text, out count))
            {
                MessageBox.Show("Invalid number of points", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Random rand = StaticRandom.GetRandomForThread();

            ClearDebugVisuals();

            //NOTE: This is really inefficient when there are a lot of dots, but it's easy to write
            List<Tuple<int, int>> possible = UtilityCore.GetPairs(_dots.Count).ToList();

            possible.RemoveWhere(o => _links.Any(p => p.Index.Item1 == o.Item1 && p.Index.Item2 == o.Item2));

            count = Math.Min(count, possible.Count);

            foreach (int index in UtilityCore.RandomRange(0, possible.Count, count))
            {
                BillboardLine3D line = new BillboardLine3D()
                {
                    Color = UtilityWPF.ColorFromHex("808080"),
                    Thickness = .05,
                    FromPoint = _dots[possible[index].Item1].Position,
                    ToPoint = _dots[possible[index].Item2].Position,
                };

                ModelVisual3D visual = new ModelVisual3D()
                {
                    Content = line.Model,
                };

                _viewport.Children.Add(visual);

                _links.Add(new Link()
                {
                    Index = possible[index],
                    Line = line,
                    Visual = visual,
                });
            }

            UpdateLinks();
            UpdateReport();
        }

        private void ClearDebugVisuals()
        {

        }

        private void UpdateLinks()
        {
            foreach (Link link in _links)
            {
                link.Line.FromPoint = _dots[link.Index.Item1].Position;
                link.Line.ToPoint = _dots[link.Index.Item2].Position;
            }
        }

        private void UpdateReport()
        {
            // Total Dots
            lblTotalDots.Text = _dots.Count.ToString();

            //TODO: Report distances

        }

        private bool GetShapeBoundries(out Tuple<Point3D, Point3D> cubeAABB, out ConeBoundries cone)
        {
            cubeAABB = null;
            cone = null;

            switch (_shape)
            {
                case BoundryShape.Cube:
                case BoundryShape.Open_Links:
                    #region cube

                    Vector3D? cubeSize = GetCubeSize();
                    if (cubeSize == null)
                    {
                        return false;
                    }

                    cubeAABB = GetCubeAABB(cubeSize.Value);

                    #endregion
                    break;

                case BoundryShape.Cone:
                    #region cone

                    cone = GetConeSize(new Vector3D(0, 0, 1), new Vector3D(0, 1, 0));
                    if (cone == null)
                    {
                        return false;
                    }

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown BoundryShape: " + _shape.ToString());
            }

            return true;
        }

        private Vector3D? GetCubeSize()
        {
            bool hadError = false;
            double x, y, z;

            // X
            if (double.TryParse(txtSizeX.Text, out x))
            {
                txtSizeX.Effect = null;
            }
            else
            {
                txtSizeX.Effect = _errorEffect;
            }

            // Y
            if (double.TryParse(txtSizeY.Text, out y))
            {
                txtSizeY.Effect = null;
            }
            else
            {
                txtSizeY.Effect = _errorEffect;
            }

            // Z
            if (double.TryParse(txtSizeZ.Text, out z))
            {
                txtSizeZ.Effect = null;
            }
            else
            {
                txtSizeZ.Effect = _errorEffect;
            }

            if (hadError)
            {
                return null;
            }
            else
            {
                return new Vector3D(x, y, z);
            }
        }
        private static Tuple<Point3D, Point3D> GetCubeAABB(Vector3D size)
        {
            double halfX = size.X / 2;
            double halfY = size.Y / 2;
            double halfZ = size.Z / 2;

            return Tuple.Create(new Point3D(-halfX, -halfY, -halfZ), new Point3D(halfX, halfY, halfZ));
        }

        private ConeBoundries GetConeSize(Vector3D axis, Vector3D up)
        {
            double heightMin = 0;
            if (!double.TryParse(txtHeightMin.Text, out heightMin))
            {
                return null;
            }

            double heightMax = 0;
            if (!double.TryParse(txtHeightMax.Text, out heightMax))
            {
                return null;
            }

            double angle = 0;
            if (!double.TryParse(txtAngle.Text, out angle))
            {
                return null;
            }

            // The user thinks in total angle, but the angle to actually rotate off axis is half of that
            angle /= 2;

            // Figure out the max dot product based on the angle
            axis = axis.ToUnit();
            up = up.ToUnit();
            Vector3D rotated = axis.GetRotatedVector(up, angle);

            double dot = Vector3D.DotProduct(axis, rotated);

            return new ConeBoundries(heightMin, heightMax, angle, axis, up, dot);
        }

        private static Point3D GetRandomPosition(BoundryShape shape, Tuple<Point3D, Point3D> cubeAABB, ConeBoundries cone)
        {
            switch (shape)
            {
                case BoundryShape.Cube:
                case BoundryShape.Open_Links:
                    return Math3D.GetRandomVector(cubeAABB.Item1.ToVector(), cubeAABB.Item2.ToVector()).ToPoint();

                case BoundryShape.Cone:
                    return Math3D.GetRandomVector_Cone(cone.AxisUnit, 0, cone.Angle, cone.HeightMin, cone.HeightMax).ToPoint();

                default:
                    throw new ApplicationException("Unknown BoundryShape: " + shape.ToString());
            }
        }

        private void Step(int iterations)
        {
            if (_dots.Count == 0)
            {
                return;
            }

            if (_links.Count > 0)
            {
                switch (_shape)
                {
                    case BoundryShape.Cube:
                        if (chkSeparateLinkSets.IsChecked.Value)
                        {
                            Step_Links_Cube_Multi(iterations);
                        }
                        else
                        {
                            Step_Links_Cube_Single(iterations);
                        }
                        break;

                    case BoundryShape.Open_Links:
                        Step_Links_Open(iterations);
                        break;

                    default:        // can't handle cone with links, so do nothing
                        break;
                }
            }
            else
            {
                switch (_shape)
                {
                    case BoundryShape.Cube:
                        Step_Cube(iterations);
                        break;

                    case BoundryShape.Cone:
                        Step_Cone(iterations);
                        break;

                    default:        // open without links should do nothing
                        break;
                }
            }

            UpdateLinks();
            UpdateReport();
        }

        private void Step_Cube(int iterations)
        {
            // Get boundry
            Tuple<Point3D, Point3D> cubeAABB;
            ConeBoundries cone;
            if (!GetShapeBoundries(out cubeAABB, out cone))
            {
                return;
            }

            Tuple<VectorND, VectorND> aabb = Tuple.Create(
                cubeAABB.Item1.ToVectorND(),
                cubeAABB.Item2.ToVectorND());

            // Convert points
            VectorND[] movablePoints = _dots.
                Where(o => !o.IsStatic).
                Select(o => o.Position.ToVectorND()).
                ToArray();

            double[] movableRepulse = _dots.
                Where(o => !o.IsStatic).
                Select(o => o.RepulseMult).
                ToArray();

            VectorND[] staticPoints = _dots.
                Where(o => o.IsStatic).
                Select(o => o.Position.ToVectorND()).
                ToArray();

            double[] staticRepulse = _dots.
                Where(o => o.IsStatic).
                Select(o => o.RepulseMult).
                ToArray();

            VectorND[] newMovable = MathND.GetRandomVectors_Cube_EventDist(movablePoints, aabb, movableRepulse, staticPoints, staticRepulse, stopIterationCount: iterations);

            // Update dots
            int index = -1;
            foreach (Dot dot in _dots)
            {
                if (dot.IsStatic)
                {
                    continue;
                }

                index++;

                dot.Position = new Point3D(newMovable[index][0], newMovable[index][1], newMovable[index][2]);
            }
        }
        private void Step_Cone(int iterations)
        {
            // Get boundry
            ConeBoundries cone = GetConeSize(new Vector3D(0, 0, 1), new Vector3D(0, 1, 0));
            if (cone == null)
            {
                return;
            }

            // Convert points
            Vector3D[] movablePoints = _dots.
                Where(o => !o.IsStatic).
                Select(o => o.Position.ToVector()).
                ToArray();

            double[] movableRepulse = _dots.
                Where(o => !o.IsStatic).
                Select(o => o.RepulseMult).
                ToArray();

            Vector3D[] staticPoints = _dots.
                Where(o => o.IsStatic).
                Select(o => o.Position.ToVector()).
                ToArray();

            double[] staticRepulse = _dots.
                Where(o => o.IsStatic).
                Select(o => o.RepulseMult).
                ToArray();

            Vector3D[] newMovable = Math3D.GetRandomVectors_Cone_EvenDist(movablePoints, cone.AxisUnit, cone.Angle, cone.HeightMin, cone.HeightMax, movableRepulse, staticPoints, staticRepulse, stopIterationCount: iterations);

            // Update dots
            int index = -1;
            foreach (Dot dot in _dots)
            {
                if (dot.IsStatic)
                {
                    continue;
                }

                index++;

                dot.Position = newMovable[index].ToPoint();
            }
        }
        private void Step_Links_Cube_Single(int iterations)
        {
            // Get boundry
            Tuple<Point3D, Point3D> cubeAABB;
            ConeBoundries cone;
            if (!GetShapeBoundries(out cubeAABB, out cone))
            {
                return;
            }

            Tuple<VectorND, VectorND> aabb = Tuple.Create(
                cubeAABB.Item1.ToVectorND(),
                cubeAABB.Item2.ToVectorND());

            // Convert points
            VectorND[] movablePoints = _dots.
                //Where(o => !o.IsStatic).      // the algorithm can't handle static points, and the links are index based.  So if static is needed, the links need more than just index (because where will change index)
                Select(o => o.Position.ToVectorND()).
                ToArray();

            Tuple<int, int>[] links = _links.
                Select(o => o.Index).
                ToArray();

            VectorND[] newMovable = MathND.GetRandomVectors_Cube_EventDist(movablePoints, links, aabb, stopIterationCount: iterations);

            // Update dots
            int index = -1;
            foreach (Dot dot in _dots)
            {
                //if (dot.IsStatic)
                //{
                //    continue;
                //}

                index++;

                dot.Position = new Point3D(newMovable[index][0], newMovable[index][1], newMovable[index][2]);
            }
        }
        private void Step_Links_Cube_Multi(int iterations)
        {
            // Get boundry
            Tuple<Point3D, Point3D> cubeAABB;
            ConeBoundries cone;
            if (!GetShapeBoundries(out cubeAABB, out cone))
            {
                return;
            }

            // Divide into sets
            var wrappers = UtilityCore.GetWrappers(_dots.ToArray(), _links.Select(o => Tuple.Create(o.Index.Item1, o.Index.Item2, o)).ToArray());
            var sets = UtilityCore.SeparateUnlinkedSets(wrappers.Item1, wrappers.Item2, 3);


            //TODO: Leave AABB alone, and instead add an offset vector to each dot.  This way, dots won't initially be smashed against a wall


            // Adjust AABBs
            double size = cubeAABB.Item2.X - cubeAABB.Item1.X;
            double margin = size / 3;

            double totalSize = (sets.Length * size) + ((sets.Length - 1) * margin);

            // Process each set
            for (int cntr = 0; cntr < sets.Length; cntr++)
            {
                Vector3D offset = new Vector3D(
                    (-totalSize / 2) + (cntr * size) + (cntr * margin) + (size / 2),
                    0, 0);

                Tuple<VectorND, VectorND> aabb = Tuple.Create(
                    (cubeAABB.Item1 + offset).ToVectorND(),
                    (cubeAABB.Item2 + offset).ToVectorND());

                // Convert points
                VectorND[] movablePoints = sets[cntr].Item1.
                    //Where(o => !o.Item.IsStatic).      // the algorithm can't handle static points, and the links are index based.  So if static is needed, the links need more than just index (because where will change index)
                    Select(o => o.Item.Position.ToVectorND()).
                    ToArray();

                Tuple<int, int>[] links = sets[cntr].Item2.
                    //Select(o => o.Link.Index).      // can't copy the indeces directly.  They refer to the large list
                    Select(o => Tuple.Create(
                        sets[cntr].Item1.IndexOf(o.Item1, (p1, p2) => p1.Index == p2.Index),
                        sets[cntr].Item1.IndexOf(o.Item2, (p1, p2) => p1.Index == p2.Index)
                        )).
                    ToArray();

                VectorND[] newMovable = MathND.GetRandomVectors_Cube_EventDist(movablePoints, links, aabb, stopIterationCount: iterations);

                // Update dots
                int index = -1;
                foreach (var dot in sets[cntr].Item1)
                {
                    //if (dot.Item.IsStatic)
                    //{
                    //    continue;
                    //}

                    index++;

                    dot.Item.Position = new Point3D(newMovable[index][0], newMovable[index][1], newMovable[index][2]);
                }
            }
        }
        private void Step_Links_Open(int iterations)
        {
            // Divide into sets
            var wrappers = UtilityCore.GetWrappers(_dots.ToArray(), _links.Select(o => Tuple.Create(o.Index.Item1, o.Index.Item2, o)).ToArray());
            var sets = UtilityCore.SeparateUnlinkedSets(wrappers.Item1, wrappers.Item2);



            //TODO: Instead of just one set, do them all.  When showing the final output, run the center through another even distribute, using the
            //radius of the set as a repulse multiplier


            // Find the largest set
            var largestSet = sets.
                Where(o => o.Item2.Length > 0).
                OrderByDescending(o => o.Item2.Length).
                FirstOrDefault();

            if (largestSet == null)
            {
                return;
            }



            // Convert points
            VectorND[] movablePoints = largestSet.Item1.
                //Where(o => !o.Item.IsStatic).      // the algorithm can't handle static points, and the links are index based.  So if static is needed, the links need more than just index (because where will change index)
                Select(o => o.Item.Position.ToVectorND()).
                ToArray();

            Tuple<int, int>[] links = largestSet.Item2.
                //Select(o => o.Link.Index).      // can't copy the indeces directly.  They refer to the large list
                Select(o => Tuple.Create(
                    largestSet.Item1.IndexOf(o.Item1, (p1, p2) => p1.Index == p2.Index),
                    largestSet.Item1.IndexOf(o.Item2, (p1, p2) => p1.Index == p2.Index)
                    )).
                ToArray();

            VectorND[] newMovable = MathND.GetRandomVectors_Open_EventDist(movablePoints, links, linkDistance: trkLinkScale.Value, stopIterationCount: iterations);

            // Update dots
            int index = -1;
            foreach (var dot in largestSet.Item1)
            {
                //if (dot.Item.IsStatic)
                //{
                //    continue;
                //}

                index++;

                dot.Item.Position = new Point3D(newMovable[index][0], newMovable[index][1], newMovable[index][2]);
            }




            // Get the rest out of the way
            for (int cntr = 0; cntr < _dots.Count; cntr++)
            {
                if (!largestSet.Item1.Any(o => o.Index == cntr))
                {
                    _dots[cntr].Position = new Point3D(20, 20, 20) + Math3D.GetRandomVector(.1);
                }
            }
        }

        private static void CapToCube(IEnumerable<Dot> dots, Tuple<Point3D, Point3D> aabb)
        {
            foreach (Dot dot in dots)
            {
                CapToCube(dot, aabb);
            }
        }
        private static void CapToCube(Dot dot, Tuple<Point3D, Point3D> aabb)
        {
            if (dot.IsStatic)
            {
                return;
            }

            bool hadChange = false;

            Point3D point = dot.Position;

            // Min
            if (point.X < aabb.Item1.X)
            {
                point.X = aabb.Item1.X;
                hadChange = true;
            }
            if (point.Y < aabb.Item1.Y)
            {
                point.Y = aabb.Item1.Y;
                hadChange = true;
            }
            if (point.Z < aabb.Item1.Z)
            {
                point.Z = aabb.Item1.Z;
                hadChange = true;
            }

            // Max
            if (point.X > aabb.Item2.X)
            {
                point.X = aabb.Item2.X;
                hadChange = true;
            }
            if (point.Y > aabb.Item2.Y)
            {
                point.Y = aabb.Item2.Y;
                hadChange = true;
            }
            if (point.Z > aabb.Item2.Z)
            {
                point.Z = aabb.Item2.Z;
                hadChange = true;
            }

            if (hadChange)
            {
                dot.Position = point;
            }
        }

        private static void CapToCone(IEnumerable<Dot> dots, ConeBoundries cone)
        {
            foreach (Dot dot in dots)
            {
                CapToCone(dot, cone);
            }
        }
        private static void CapToCone(Dot dot, ConeBoundries cone)
        {
            if (dot.IsStatic)
            {
                return;
            }

            bool hadChange = false;

            Vector3D point = dot.Position.ToVector();

            double heightSquared = point.LengthSquared;

            // Handle zero length when not allowed
            if (heightSquared.IsNearZero())
            {
                if (cone.HeightMin > 0)
                {
                    //GetRandomPointInCone(axisUnit, angle, heightMin, heightMax);
                    dot.Position = Math3D.GetRandomVector_Cone(cone.AxisUnit, 0, cone.Angle, cone.HeightMin, cone.HeightMax).ToPoint();
                }

                return;
            }

            // Cap Angle
            Vector3D posUnit = point.ToUnit(false);

            if (Vector3D.DotProduct(posUnit, cone.AxisUnit) < cone.Dot)
            {
                Vector3D cross = Vector3D.CrossProduct(cone.AxisUnit, posUnit);
                posUnit = cone.AxisUnit.GetRotatedVector(cross, cone.Angle);
                hadChange = true;
            }

            // Cap Height
            if (heightSquared < cone.HeightMin * cone.HeightMin)
            {
                heightSquared = cone.HeightMin * cone.HeightMin;
                hadChange = true;
            }
            else if (heightSquared > cone.HeightMax * cone.HeightMax)
            {
                heightSquared = cone.HeightMax * cone.HeightMax;
                hadChange = true;
            }

            // Update Position
            if (hadChange)
            {
                dot.Position = (posUnit * Math.Sqrt(heightSquared)).ToPoint();
            }
        }

        #endregion
    }
}
