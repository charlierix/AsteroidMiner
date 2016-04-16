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
    public partial class EvenDistributionCube : Window
    {
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
        #region Class: ShortPair

        private class ShortPair
        {
            public ShortPair(int index1, int index2, double length, double lengthRatio, double avgMult, Vector3D link)
            {
                this.Index1 = index1;
                this.Index2 = index2;
                this.Length = length;
                this.LengthRatio = lengthRatio;
                this.AvgMult = avgMult;
                this.Link = link;
            }

            public readonly int Index1;
            public readonly int Index2;
            public readonly double Length;
            public readonly double LengthRatio;
            public readonly double AvgMult;
            public readonly Vector3D Link;
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

        private bool _initialized = false;

        #endregion

        #region Constructor

        public EvenDistributionCube()
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

            _initialized = true;
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
                Step();
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

                Step();
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
                Vector3D? cubeSize = GetCubeSize();
                if (cubeSize == null)
                {
                    MessageBox.Show("Invalid cube size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var aabb = GetCubeAABB(cubeSize.Value);

                foreach (Dot dot in _dots)
                {
                    if (!dot.IsStatic)
                    {
                        dot.Position = Math3D.GetRandomVector(aabb.Item1.ToVector(), aabb.Item2.ToVector()).ToPoint();
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
                if (_dots.Count == 0)
                {
                    return;
                }

                Vector3D? size = GetCubeSize();
                if (size == null)
                {
                    return;
                }

                Tuple<Point3D, Point3D> aabb3D = GetCubeAABB(size.Value);

                Tuple<double[], double[]> aabb = Tuple.Create(
                    new[] { aabb3D.Item1.X, aabb3D.Item1.Y, aabb3D.Item1.Z },
                    new[] { aabb3D.Item2.X, aabb3D.Item2.Y, aabb3D.Item2.Z });

                double[][] movablePoints = _dots.
                    Where(o => !o.IsStatic).
                    Select(o => new[] { o.Position.X, o.Position.Y, o.Position.Z }).
                    ToArray();

                double[] movableRepulse = _dots.
                    Where(o => !o.IsStatic).
                    Select(o => o.RepulseMult).
                    ToArray();

                double[][] staticPoints = _dots.
                    Where(o => o.IsStatic).
                    Select(o => new[] { o.Position.X, o.Position.Y, o.Position.Z }).
                    ToArray();

                double[] staticRepulse = _dots.
                    Where(o => o.IsStatic).
                    Select(o => o.RepulseMult).
                    ToArray();

                double[][] newMovable = MathND.GetRandomVectors_Cube_EventDist(movablePoints, aabb, movableRepulse, staticPoints, staticRepulse);

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
            Vector3D? cubeSize = GetCubeSize();
            if (cubeSize == null)
            {
                MessageBox.Show("Invalid cube size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int count;
            if (!int.TryParse(txtNumDots.Text, out count))
            {
                MessageBox.Show("Invalid number of points", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var aabb = GetCubeAABB(cubeSize.Value);

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

                Dot dot = new Dot(isStatic, Math3D.GetRandomVector(aabb.Item1.ToVector(), aabb.Item2.ToVector()).ToPoint(), mult);

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

        private void Step()
        {
            if (_dots.Count == 0)
            {
                return;
            }

            // Percent
            double percent;
            if (!double.TryParse(txtMovePercent.Text, out percent))
            {
                return;
            }
            percent /= 100d;

            // Bounds
            Vector3D? size = GetCubeSize();
            if (size == null)
            {
                return;
            }
            var aabb = GetCubeAABB(size.Value);

            // Do it
            MoveStep_3(_dots, percent, aabb);
            CapToCube(_dots, aabb);     //TODO: If the boundries don't change, there is no need to cap them all each frame.  Just do an initial, then only cap those that move in a particular frame

            // Stats
            UpdateReport();
        }

        private static void MoveStep_1(IList<Dot> dots, double percent)
        {
            // Find shortest pair lengths
            Tuple<int, int, double, Vector3D> absShortest = null;
            var subShortest = new List<Tuple<int, int, double, Vector3D>>();

            for (int outer = 0; outer < dots.Count - 1; outer++)
            {
                Tuple<int, int, double, Vector3D> currentShortest = null;

                for (int inner = outer + 1; inner < dots.Count; inner++)
                {
                    if (dots[outer].IsStatic && dots[inner].IsStatic)
                    {
                        continue;
                    }

                    Vector3D link = dots[inner].Position - dots[outer].Position;
                    double lenSqr = (link).LengthSquared;

                    if (currentShortest == null || lenSqr < currentShortest.Item3)
                    {
                        currentShortest = Tuple.Create(outer, inner, lenSqr, link);
                    }
                }

                if (currentShortest == null)
                {
                    continue;
                }

                currentShortest = Tuple.Create(currentShortest.Item1, currentShortest.Item2, Math.Sqrt(currentShortest.Item3), currentShortest.Item4);
                subShortest.Add(currentShortest);

                if (absShortest == null || currentShortest.Item3 < absShortest.Item3)
                {
                    absShortest = currentShortest;
                }
            }

            if (subShortest.Count == 0)
            {
                return;
            }

            // Move the shortest pair away from each other (based on how far they are away from the avg)
            double avg = subShortest.Average(o => o.Item3);
            double distToMove = avg - absShortest.Item3;     // the shortest will always be less than average

            Vector3D displace = absShortest.Item4.ToUnit() * (distToMove * percent);

            if (!dots[absShortest.Item1].IsStatic)
                dots[absShortest.Item1].Position -= displace;

            if (!dots[absShortest.Item2].IsStatic)
                dots[absShortest.Item2].Position += displace;
        }
        private static void MoveStep_2(IList<Dot> dots, double percent, Tuple<Point3D, Point3D> aabb)
        {
            // Find shortest pair lengths
            Tuple<int, int, double, Vector3D>[] shortPairs = MoveStep_GetShortest(dots);

            if (shortPairs.Length == 0)
            {
                return;
            }

            // Move the shortest pair away from each other (based on how far they are away from the avg)
            double avg = shortPairs.Average(o => o.Item3);

            double distToMoveMax = avg - shortPairs[0].Item3;

            for (int cntr = 0; cntr < shortPairs.Length; cntr++)
            {
                if (shortPairs[cntr].Item3 >= avg)
                {
                    break;      // they are sorted, so the rest of the list will also be greater
                }

                // Figure out how far they should move
                double actualPercent, distToMove;
                if (cntr == 0)
                {
                    actualPercent = percent;
                    distToMove = distToMoveMax;
                }
                else
                {
                    distToMove = avg - shortPairs[cntr].Item3;
                    actualPercent = (distToMove / distToMoveMax) * percent;
                }

                distToMove /= 2;        // displace is for one of them, so cut it in half

                Vector3D displace = shortPairs[cntr].Item4.ToUnit() * (distToMove * actualPercent);

                // Move dots
                if (!dots[shortPairs[cntr].Item1].IsStatic)
                {
                    dots[shortPairs[cntr].Item1].Position -= displace;
                    CapToCube(dots[shortPairs[cntr].Item1], aabb);
                }

                if (!dots[shortPairs[cntr].Item2].IsStatic)
                {
                    dots[shortPairs[cntr].Item2].Position += displace;
                    CapToCube(dots[shortPairs[cntr].Item2], aabb);
                }
            }
        }
        private static void MoveStep_3(IList<Dot> dots, double percent, Tuple<Point3D, Point3D> aabb)
        {
            // Find shortest pair lengths
            ShortPair[] shortPairs = MoveStep_GetShortest3(dots);
            if (shortPairs.Length == 0)
            {
                return;
            }

            // Move the shortest pair away from each other (based on how far they are away from the avg)
            double avg = shortPairs.Average(o => o.LengthRatio);

            double distToMoveMax = avg - shortPairs[0].LengthRatio;
            if (distToMoveMax.IsNearZero())
            {
                // Found equilbrium
                return;
            }

            for (int cntr = 0; cntr < shortPairs.Length; cntr++)
            {
                if (shortPairs[cntr].LengthRatio >= avg)
                {
                    break;      // they are sorted, so the rest of the list will also be greater
                }

                // Figure out how far they should move
                double actualPercent, distToMoveRatio;
                if (cntr == 0)
                {
                    actualPercent = percent;
                    distToMoveRatio = distToMoveMax;
                }
                else
                {
                    distToMoveRatio = avg - shortPairs[cntr].LengthRatio;
                    actualPercent = (distToMoveRatio / distToMoveMax) * percent;        // don't use the full percent.  Reduce it based on the ratio of this distance with the max distance
                }

                double moveDist = distToMoveRatio * actualPercent * shortPairs[cntr].AvgMult;

                Vector3D displaceUnit;
                if (shortPairs[cntr].Length.IsNearZero())
                {
                    displaceUnit = Math3D.GetRandomVector_Spherical(1);
                }
                else
                {
                    displaceUnit = shortPairs[cntr].Link.ToUnit(false);
                }

                // Can't move evenly.  Divide it up based on the ratio of multipliers
                double sumMult = dots[shortPairs[cntr].Index1].RepulseMult + dots[shortPairs[cntr].Index2].RepulseMult;
                double percent2 = dots[shortPairs[cntr].Index1].RepulseMult / sumMult;      // flipping 1 and 2, because if 1 is bigger, 2 needs to move more
                double percent1 = 1 - percent2;

                // Move dots
                Dot dot = dots[shortPairs[cntr].Index1];
                if (!dot.IsStatic)
                {
                    Vector3D displace = displaceUnit * (moveDist * percent1);
                    dot.Position -= displace;
                    CapToCube(dot, aabb);
                }

                dot = dots[shortPairs[cntr].Index2];
                if (!dot.IsStatic)
                {
                    Vector3D displace = displaceUnit * (moveDist * percent2);
                    dot.Position += displace;
                    CapToCube(dot, aabb);
                }
            }
        }

        private static Tuple<int, int, double, Vector3D>[] MoveStep_GetShortest(IList<Dot> dots)
        {
            var retVal = new List<Tuple<int, int, double, Vector3D>>();

            for (int outer = 0; outer < dots.Count - 1; outer++)
            {
                Tuple<int, int, double, Vector3D> currentShortest = null;

                for (int inner = outer + 1; inner < dots.Count; inner++)
                {
                    if (dots[outer].IsStatic && dots[inner].IsStatic)
                    {
                        continue;
                    }

                    Vector3D link = dots[inner].Position - dots[outer].Position;
                    double lenSqr = (link).LengthSquared;

                    if (currentShortest == null || lenSqr < currentShortest.Item3)
                    {
                        currentShortest = Tuple.Create(outer, inner, lenSqr, link);
                    }
                }

                if (currentShortest == null)
                {
                    continue;
                }

                currentShortest = Tuple.Create(currentShortest.Item1, currentShortest.Item2, Math.Sqrt(currentShortest.Item3), currentShortest.Item4);
                retVal.Add(currentShortest);
            }

            return retVal.
                OrderBy(o => o.Item3).
                ToArray();
        }
        private static ShortPair[] MoveStep_GetShortest3(IList<Dot> dots)
        {
            List<ShortPair> retVal = new List<ShortPair>();

            for (int outer = 0; outer < dots.Count - 1; outer++)
            {
                ShortPair currentShortest = null;

                for (int inner = outer + 1; inner < dots.Count; inner++)
                {
                    if (dots[outer].IsStatic && dots[inner].IsStatic)
                    {
                        continue;
                    }

                    Vector3D link = dots[inner].Position - dots[outer].Position;
                    double length = link.Length;
                    double avgMult = (dots[inner].RepulseMult + dots[outer].RepulseMult) / 2d;
                    double ratio = length / avgMult;

                    if (currentShortest == null || ratio < currentShortest.LengthRatio)
                    {
                        currentShortest = new ShortPair(outer, inner, length, ratio, avgMult, link);
                    }
                }

                if (currentShortest == null)
                {
                    continue;
                }

                retVal.Add(currentShortest);
            }

            return retVal.
                OrderBy(o => o.LengthRatio).
                ToArray();
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

        #endregion
    }
}
