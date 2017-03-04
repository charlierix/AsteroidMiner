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

namespace Game.Newt.Testers
{
    public partial class EvenDistributionOther : Window
    {
        #region Enum: BoundryShape

        private enum BoundryShape
        {
            Cube,
            Cone,
        }

        #endregion
        #region Class: ConeBoundries

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
        #region Class: Dot

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

        #region Declaration Section

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<Dot> _dots = new List<Dot>();

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

                var cubeProps = new UIElement[] { lblSizeX, txtSizeX, lblSizeY, txtSizeY, lblSizeZ, txtSizeZ };
                var coneProps = new UIElement[] { lblHeightMin, txtHeightMin, lblHeightMax, txtHeightMax, lblAngle, txtAngle };

                // Switch visibilities
                BoundryShape selected = (BoundryShape)cboShape.SelectedItem;     // null check at the top of the method
                switch (selected)
                {
                    case BoundryShape.Cube:
                        foreach (var cone in coneProps) cone.Visibility = Visibility.Collapsed;
                        foreach (var cube in cubeProps) cube.Visibility = Visibility.Visible;
                        break;

                    case BoundryShape.Cone:
                        foreach (var cube in cubeProps) cube.Visibility = Visibility.Collapsed;
                        foreach (var cone in coneProps) cone.Visibility = Visibility.Visible;
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
                Step(1000);
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

        private void ClearDebugVisuals()
        {

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
                    return Math3D.GetRandomVector(cubeAABB.Item1.ToVector(), cubeAABB.Item2.ToVector()).ToPoint();

                case BoundryShape.Cone:
                    Vector3D direction = Math3D.GetRandomVector_Cone(cone.AxisUnit, cone.Angle);
                    return (direction.ToUnit(false) * StaticRandom.NextDouble(cone.HeightMin, cone.HeightMax)).ToPoint();

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

            switch (_shape)
            {
                case BoundryShape.Cube:
                    Step_Cube(iterations);
                    break;

                case BoundryShape.Cone:
                    Step_Cone(iterations);
                    break;

                default:
                    break;
            }

            // Stats
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

            Tuple<VectorND, VectorND> aabb = Tuple.Create(
                cubeAABB.Item1.ToVectorND(),
                cubeAABB.Item2.ToVectorND());

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

                    Vector3D direction = Math3D.GetRandomVector_Cone(cone.AxisUnit, cone.Angle);
                    dot.Position = (direction.ToUnit(false) * StaticRandom.NextDouble(cone.HeightMin, cone.HeightMax)).ToPoint();
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

        #region OLD

        // These only deal with cube

        //private void Step()
        //{
        //    if (_dots.Count == 0)
        //    {
        //        return;
        //    }

        //    // Percent
        //    double percent;
        //    if (!double.TryParse(txtMovePercent.Text, out percent))
        //    {
        //        return;
        //    }
        //    percent /= 100d;

        //    // Bounds
        //    Tuple<Point3D, Point3D> cubeAABB;
        //    ConeBoundries cone;
        //    if (!GetShapeBoundries(out cubeAABB, out cone))
        //    {
        //        return;
        //    }

        //    // Do it
        //    MoveStep_3(_dots, percent, cubeAABB);
        //    CapToCube(_dots, cubeAABB);     //TODO: If the boundries don't change, there is no need to cap them all each frame.  Just do an initial, then only cap those that move in a particular frame

        //    // Stats
        //    UpdateReport();
        //}

        //private static void MoveStep_1(IList<Dot> dots, double percent)
        //{
        //    // Find shortest pair lengths
        //    Tuple<int, int, double, Vector3D> absShortest = null;
        //    var subShortest = new List<Tuple<int, int, double, Vector3D>>();

        //    for (int outer = 0; outer < dots.Count - 1; outer++)
        //    {
        //        Tuple<int, int, double, Vector3D> currentShortest = null;

        //        for (int inner = outer + 1; inner < dots.Count; inner++)
        //        {
        //            if (dots[outer].IsStatic && dots[inner].IsStatic)
        //            {
        //                continue;
        //            }

        //            Vector3D link = dots[inner].Position - dots[outer].Position;
        //            double lenSqr = (link).LengthSquared;

        //            if (currentShortest == null || lenSqr < currentShortest.Item3)
        //            {
        //                currentShortest = Tuple.Create(outer, inner, lenSqr, link);
        //            }
        //        }

        //        if (currentShortest == null)
        //        {
        //            continue;
        //        }

        //        currentShortest = Tuple.Create(currentShortest.Item1, currentShortest.Item2, Math.Sqrt(currentShortest.Item3), currentShortest.Item4);
        //        subShortest.Add(currentShortest);

        //        if (absShortest == null || currentShortest.Item3 < absShortest.Item3)
        //        {
        //            absShortest = currentShortest;
        //        }
        //    }

        //    if (subShortest.Count == 0)
        //    {
        //        return;
        //    }

        //    // Move the shortest pair away from each other (based on how far they are away from the avg)
        //    double avg = subShortest.Average(o => o.Item3);
        //    double distToMove = avg - absShortest.Item3;     // the shortest will always be less than average

        //    Vector3D displace = absShortest.Item4.ToUnit() * (distToMove * percent);

        //    if (!dots[absShortest.Item1].IsStatic)
        //        dots[absShortest.Item1].Position -= displace;

        //    if (!dots[absShortest.Item2].IsStatic)
        //        dots[absShortest.Item2].Position += displace;
        //}
        //private static void MoveStep_2(IList<Dot> dots, double percent, Tuple<Point3D, Point3D> aabb)
        //{
        //    // Find shortest pair lengths
        //    Tuple<int, int, double, Vector3D>[] shortPairs = MoveStep_GetShortest(dots);

        //    if (shortPairs.Length == 0)
        //    {
        //        return;
        //    }

        //    // Move the shortest pair away from each other (based on how far they are away from the avg)
        //    double avg = shortPairs.Average(o => o.Item3);

        //    double distToMoveMax = avg - shortPairs[0].Item3;

        //    for (int cntr = 0; cntr < shortPairs.Length; cntr++)
        //    {
        //        if (shortPairs[cntr].Item3 >= avg)
        //        {
        //            break;      // they are sorted, so the rest of the list will also be greater
        //        }

        //        // Figure out how far they should move
        //        double actualPercent, distToMove;
        //        if (cntr == 0)
        //        {
        //            actualPercent = percent;
        //            distToMove = distToMoveMax;
        //        }
        //        else
        //        {
        //            distToMove = avg - shortPairs[cntr].Item3;
        //            actualPercent = (distToMove / distToMoveMax) * percent;
        //        }

        //        distToMove /= 2;        // displace is for one of them, so cut it in half

        //        Vector3D displace = shortPairs[cntr].Item4.ToUnit() * (distToMove * actualPercent);

        //        // Move dots
        //        if (!dots[shortPairs[cntr].Item1].IsStatic)
        //        {
        //            dots[shortPairs[cntr].Item1].Position -= displace;
        //            CapToCube(dots[shortPairs[cntr].Item1], aabb);
        //        }

        //        if (!dots[shortPairs[cntr].Item2].IsStatic)
        //        {
        //            dots[shortPairs[cntr].Item2].Position += displace;
        //            CapToCube(dots[shortPairs[cntr].Item2], aabb);
        //        }
        //    }
        //}
        //private static void MoveStep_3(IList<Dot> dots, double percent, Tuple<Point3D, Point3D> aabb)
        //{
        //    // Find shortest pair lengths
        //    ShortPair[] shortPairs = MoveStep_GetShortest3(dots);
        //    if (shortPairs.Length == 0)
        //    {
        //        return;
        //    }

        //    // Move the shortest pair away from each other (based on how far they are away from the avg)
        //    double avg = shortPairs.Average(o => o.LengthRatio);

        //    double distToMoveMax = avg - shortPairs[0].LengthRatio;
        //    if (distToMoveMax.IsNearZero())
        //    {
        //        // Found equilbrium
        //        return;
        //    }

        //    for (int cntr = 0; cntr < shortPairs.Length; cntr++)
        //    {
        //        if (shortPairs[cntr].LengthRatio >= avg)
        //        {
        //            break;      // they are sorted, so the rest of the list will also be greater
        //        }

        //        // Figure out how far they should move
        //        double actualPercent, distToMoveRatio;
        //        if (cntr == 0)
        //        {
        //            actualPercent = percent;
        //            distToMoveRatio = distToMoveMax;
        //        }
        //        else
        //        {
        //            distToMoveRatio = avg - shortPairs[cntr].LengthRatio;
        //            actualPercent = (distToMoveRatio / distToMoveMax) * percent;        // don't use the full percent.  Reduce it based on the ratio of this distance with the max distance
        //        }

        //        double moveDist = distToMoveRatio * actualPercent * shortPairs[cntr].AvgMult;

        //        Vector3D displaceUnit;
        //        if (shortPairs[cntr].Length.IsNearZero())
        //        {
        //            displaceUnit = Math3D.GetRandomVector_Spherical(1);
        //        }
        //        else
        //        {
        //            displaceUnit = shortPairs[cntr].Link.ToUnit(false);
        //        }

        //        // Can't move evenly.  Divide it up based on the ratio of multipliers
        //        double sumMult = dots[shortPairs[cntr].Index1].RepulseMult + dots[shortPairs[cntr].Index2].RepulseMult;
        //        double percent2 = dots[shortPairs[cntr].Index1].RepulseMult / sumMult;      // flipping 1 and 2, because if 1 is bigger, 2 needs to move more
        //        double percent1 = 1 - percent2;

        //        // Move dots
        //        Dot dot = dots[shortPairs[cntr].Index1];
        //        if (!dot.IsStatic)
        //        {
        //            Vector3D displace = displaceUnit * (moveDist * percent1);
        //            dot.Position -= displace;
        //            CapToCube(dot, aabb);
        //        }

        //        dot = dots[shortPairs[cntr].Index2];
        //        if (!dot.IsStatic)
        //        {
        //            Vector3D displace = displaceUnit * (moveDist * percent2);
        //            dot.Position += displace;
        //            CapToCube(dot, aabb);
        //        }
        //    }
        //}

        //private static Tuple<int, int, double, Vector3D>[] MoveStep_GetShortest(IList<Dot> dots)
        //{
        //    var retVal = new List<Tuple<int, int, double, Vector3D>>();

        //    for (int outer = 0; outer < dots.Count - 1; outer++)
        //    {
        //        Tuple<int, int, double, Vector3D> currentShortest = null;

        //        for (int inner = outer + 1; inner < dots.Count; inner++)
        //        {
        //            if (dots[outer].IsStatic && dots[inner].IsStatic)
        //            {
        //                continue;
        //            }

        //            Vector3D link = dots[inner].Position - dots[outer].Position;
        //            double lenSqr = (link).LengthSquared;

        //            if (currentShortest == null || lenSqr < currentShortest.Item3)
        //            {
        //                currentShortest = Tuple.Create(outer, inner, lenSqr, link);
        //            }
        //        }

        //        if (currentShortest == null)
        //        {
        //            continue;
        //        }

        //        currentShortest = Tuple.Create(currentShortest.Item1, currentShortest.Item2, Math.Sqrt(currentShortest.Item3), currentShortest.Item4);
        //        retVal.Add(currentShortest);
        //    }

        //    return retVal.
        //        OrderBy(o => o.Item3).
        //        ToArray();
        //}
        //private static ShortPair[] MoveStep_GetShortest3(IList<Dot> dots)
        //{
        //    List<ShortPair> retVal = new List<ShortPair>();

        //    for (int outer = 0; outer < dots.Count - 1; outer++)
        //    {
        //        ShortPair currentShortest = null;

        //        for (int inner = outer + 1; inner < dots.Count; inner++)
        //        {
        //            if (dots[outer].IsStatic && dots[inner].IsStatic)
        //            {
        //                continue;
        //            }

        //            Vector3D link = dots[inner].Position - dots[outer].Position;
        //            double length = link.Length;
        //            double avgMult = (dots[inner].RepulseMult + dots[outer].RepulseMult) / 2d;
        //            double ratio = length / avgMult;

        //            if (currentShortest == null || ratio < currentShortest.LengthRatio)
        //            {
        //                currentShortest = new ShortPair(outer, inner, length, ratio, avgMult, link);
        //            }
        //        }

        //        if (currentShortest == null)
        //        {
        //            continue;
        //        }

        //        retVal.Add(currentShortest);
        //    }

        //    return retVal.
        //        OrderBy(o => o.LengthRatio).
        //        ToArray();
        //}

        #endregion

        #endregion
    }
}
