using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers
{
    public partial class NonlinearRandom : Window
    {
        #region Declaration Section

        /// <summary>
        /// This is the folder that all save will go
        /// This will be a subfolder of AsteroidMiner
        /// </summary>
        private const string FOLDER = "NonlinearRandom";
        private const string BELL3 = "Bell Control Points.xml";

        private readonly Effect _errorEffect;
        private readonly Brush _brushLightBlue = new SolidColorBrush(UtilityWPF.ColorFromHex("60455F99"));
        private readonly Brush _brushVeryLight = new SolidColorBrush(UtilityWPF.ColorFromHex("308C8681"));
        private readonly Brush _brushDark = new SolidColorBrush(UtilityWPF.ColorFromHex("802F3540"));
        private readonly Brush _brushSample = new SolidColorBrush(UtilityWPF.ColorFromHex("6D717A"));
        private readonly Brush _brushIdeal = new SolidColorBrush(UtilityWPF.ColorFromHex("8C8681"));

        private bool _isProgramaticallyChanging = false;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public NonlinearRandom()
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

            txtBell2.Text =
@"0 0
.1 .15
.9 .15
1 1";

            txtBell3.Text =
@".4 .3
.6 .8";

            try
            {
                RebuildBell3Combo();
            }
            catch (Exception) { }

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void trkPowLTOne_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChanging)
                {
                    return;
                }

                _isProgramaticallyChanging = true;

                double value = Math.Round(trkPowLTOne.Value, 2);

                txtPow.Text = value.ToString();

                _isProgramaticallyChanging = false;

                DrawPower();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkPowGTOne_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChanging)
                {
                    return;
                }

                _isProgramaticallyChanging = true;

                double value = GetGT1(trkPowGTOne.Value);
                value = Math.Round(value, 2);

                txtPow.Text = value.ToString();

                _isProgramaticallyChanging = false;

                DrawPower();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtPow_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChanging)
                {
                    return;
                }

                double value;
                if (!double.TryParse(txtPow.Text, out value))
                {
                    txtPow.Effect = _errorEffect;
                    return;
                }

                _isProgramaticallyChanging = true;

                if (value.IsNearValue(1))
                {
                    trkPowLTOne.Value = 1;
                    trkPowGTOne.Value = 0;
                }
                else if (value < 1)
                {
                    trkPowLTOne.Value = value;
                    trkPowGTOne.Value = 0;
                }
                else
                {
                    trkPowLTOne.Value = 1;
                    trkPowGTOne.Value = SetGT1(value);
                }

                _isProgramaticallyChanging = false;

                DrawPower();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Pow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DrawPower();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BellFail_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                DrawBellFail();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BellFail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DrawBellFail();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BellFailDebug_Click(object sender, RoutedEventArgs e)
        {
            const int COUNT = 25;


            // The problem that this method illustrates is that rand.nextdouble() is thought of as x, but it's really % along arc length of the curve
            // And because there are two curves, it's some hacked up hybrid
            //
            // The original intent was to get the y coord at some fixed x.  But there's no one single calculation that would get that.  Samples would
            // need to be taken to figure out which percent along curve to use to get the desired x, and corresponding y



            try
            {
                double midX = trkBellFailPeak.Value;

                BezierSegment3D[] bezier = BezierUtil.GetBezierSegments(new[] { new Point3D(0, 0, 0), new Point3D(midX, 1, 0), new Point3D(1, 0, 0) }, trkBellFailPinch.Value);

                // Add a control point so the curve will be attracted to the x axis at the two end points
                double run;
                if (trkBellFailLeftZero.Value > 0)
                {
                    run = (bezier[0].EndPoint1.X - bezier[0].EndPoint0.X) * trkBellFailLeftZero.Value;
                    bezier[0] = new BezierSegment3D(bezier[0].EndIndex0, bezier[0].EndIndex1, new[] { new Point3D(run, 0, 0), bezier[0].ControlPoints[0] }, bezier[0].AllEndPoints);
                }

                if (trkBellFailRightZero.Value > 0)
                {
                    run = (bezier[1].EndPoint1.X - bezier[1].EndPoint0.X) * trkBellFailRightZero.Value;
                    bezier[1] = new BezierSegment3D(bezier[1].EndIndex0, bezier[1].EndIndex1, new[] { bezier[1].ControlPoints[0], new Point3D(bezier[1].EndPoint1.X - run, 0, 0), }, bezier[1].AllEndPoints);
                }




                //bezier[0] = new BezierSegment3D(bezier[0].EndIndex0, bezier[0].EndIndex1, new Point3D[0], bezier[0].AllEndPoints);
                //bezier[1] = new BezierSegment3D(bezier[1].EndIndex0, bezier[1].EndIndex1, new Point3D[0], bezier[1].AllEndPoints);




                var samples = Enumerable.Range(0, COUNT).
                    Select(o =>
                    {
                        double percentOrig = o.ToDouble() / COUNT.ToDouble();

                        int index;
                        double percent;
                        if (percentOrig < midX)
                        {
                            index = 0;
                            percent = percentOrig / midX;
                        }
                        else
                        {
                            index = 1;
                            percent = (percentOrig - midX) / (1d - midX);
                        }

                        Point3D point = BezierUtil.GetPoint(percent, bezier[index]);

                        //if (retVal < 0) retVal = 0;
                        //else if (retVal > 1) retVal = 1;

                        return new
                        {
                            PercentOrig = percentOrig,
                            PercentSub = percent,
                            Index = index,
                            Point = point,
                        };
                    }).
                    ToArray();



                Clear();

                Rect bounds = GetBounds();

                // Grid
                AddLine(bounds.BottomLeft, bounds.TopRight, _brushVeryLight);      // diagonal

                AddLine(bounds.TopLeft, bounds.TopRight, _brushVeryLight);      // 1
                double y = bounds.Bottom - (bounds.Height * .75);
                AddLine(new Point(bounds.Left, y), new Point(bounds.Right, y), _brushVeryLight);      // .75
                y = bounds.Bottom - (bounds.Height * .5);
                AddLine(new Point(bounds.Left, y), new Point(bounds.Right, y), _brushVeryLight);      // .5
                y = bounds.Bottom - (bounds.Height * .25);
                AddLine(new Point(bounds.Left, y), new Point(bounds.Right, y), _brushVeryLight);      // .25

                AddLine(new[] { bounds.TopLeft, bounds.BottomLeft, bounds.BottomRight }, _brushDark);     // axiis

                // Samples
                foreach (var sample in samples)
                {
                    AddLine(new Point(bounds.Left + (sample.PercentOrig * bounds.Width), bounds.Bottom), new Point(bounds.Left + (sample.Point.X * bounds.Width), bounds.Bottom - (sample.Point.Y * bounds.Height)), Brushes.Black);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBell2_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                DrawBell2();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkBell2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                DrawBell2();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Bell2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DrawBell2();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkBellArms_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                DrawBellArms();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BellArms_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DrawBellArms();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBell3_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                DrawBell3();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveBell3Map_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point[] points = GetBellPoints(txtBell3.Text);
                if (points == null)
                {
                    MessageBox.Show("Invalid control points", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //C:\Users\<username>\AppData\Roaming\Asteroid Miner\NonlinearRandom\
                string foldername = UtilityCore.GetOptionsFolder();
                foldername = System.IO.Path.Combine(foldername, FOLDER);
                Directory.CreateDirectory(foldername);

                string filename = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff - ");
                filename += BELL3;
                filename = System.IO.Path.Combine(foldername, filename);

                UtilityCore.SerializeToFile(filename, points);

                RebuildBell3Combo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void cboBell3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChanging)
                {
                    return;
                }

                TextBlock textblock = cboBell3.SelectedItem as TextBlock;
                if (textblock == null)
                {
                    return;
                }

                string tag = textblock.Tag as string;
                if (string.IsNullOrEmpty(tag))
                {
                    return;
                }

                txtBell3.Text = tag;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Bell3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DrawBell3();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private const double BASE = 6;
        private static double GetGT1(double sliderValue)
        {
            // sliderValue should be between 0 and 1.5
            return Math.Pow(BASE, sliderValue);
        }
        private static double SetGT1(double actualValue)
        {
            // actualValue should be between BASE^1 and BASE^1.5
            return Math.Log(actualValue, BASE);
        }

        private void DrawPower()
        {
            double power;
            if (!double.TryParse(txtPow.Text, out power))
            {
                txtPow.Effect = _errorEffect;
                return;
            }

            txtPow.Effect = null;

            Random rand = StaticRandom.GetRandomForThread();

            IEnumerable<double> samples = Enumerable.Range(0, 200000).
                Select(o => rand.NextPow(power));

            IEnumerable<Point> idealLine = Enumerable.Range(0, 100).
                Select(o =>
                    {
                        double x = o / 100d;
                        return new Point(x, Math.Pow(x, power));
                    });

            DrawGraph(samples, idealLine);
        }
        private void DrawBellFail()
        {
            double midX = trkBellFailPeak.Value;

            BezierSegment3D[] bezier = BezierUtil.GetBezierSegments(new[] { new Point3D(0, 0, 0), new Point3D(midX, 1, 0), new Point3D(1, 0, 0) }, trkBellFailPinch.Value);

            // Add a control point so the curve will be attracted to the x axis at the two end points
            double run;
            if (trkBellFailLeftZero.Value > 0)
            {
                run = (bezier[0].EndPoint1.X - bezier[0].EndPoint0.X) * trkBellFailLeftZero.Value;
                bezier[0] = new BezierSegment3D(bezier[0].EndIndex0, bezier[0].EndIndex1, new[] { new Point3D(run, 0, 0), bezier[0].ControlPoints[0] }, bezier[0].AllEndPoints);
            }

            if (trkBellFailRightZero.Value > 0)
            {
                run = (bezier[1].EndPoint1.X - bezier[1].EndPoint0.X) * trkBellFailRightZero.Value;
                bezier[1] = new BezierSegment3D(bezier[1].EndIndex0, bezier[1].EndIndex1, new[] { bezier[1].ControlPoints[0], new Point3D(bezier[1].EndPoint1.X - run, 0, 0), }, bezier[1].AllEndPoints);
            }

            Random rand = StaticRandom.GetRandomForThread();

            //TODO: There is still a bug with calculating percent, or something
            //It might be treating X as a percent.  Maybe need to get the length of the lines, and take the percent of those???? - should be the same though

            IEnumerable<double> samples = Enumerable.Range(0, 50000).
                Select(o =>
                    {
                        double percent = rand.NextDouble();

                        int index;
                        if (percent < midX)
                        {
                            index = 0;
                            percent = percent / midX;
                        }
                        else
                        {
                            index = 1;
                            percent = (percent - midX) / (1d - midX);
                        }

                        double retVal = BezierUtil.GetPoint(percent, bezier[index]).Y;

                        if (retVal < 0) retVal = 0;
                        else if (retVal > 1) retVal = 1;

                        return retVal;
                    });

            IEnumerable<Point> idealLine = BezierUtil.GetPath(100, bezier).
                Select(o => o.ToPoint2D());

            DrawGraph(samples, idealLine);
        }
        private void DrawBell2()
        {
            Point[] points = GetBellPoints(txtBell2.Text);
            if (points == null)
            {
                txtBell2.Effect = _errorEffect;
                return;
            }

            txtBell2.Effect = null;

            BezierSegment3D[] bezier = BezierUtil.GetBezierSegments(points.Select(o => o.ToPoint3D()).ToArray(), trkBell2.Value);



            for (int cntr = 0; cntr < bezier.Length; cntr++)
            {
                bezier[cntr] = new BezierSegment3D(bezier[cntr].EndIndex0, bezier[cntr].EndIndex1, new Point3D[0], bezier[cntr].AllEndPoints);
            }



            Random rand = StaticRandom.GetRandomForThread();

            IEnumerable<double> samples = Enumerable.Range(0, 50000).
                Select(o => BezierUtil.GetPoint(rand.NextDouble(), bezier).Y);

            IEnumerable<Point> idealLine = BezierUtil.GetPath(100, bezier).
                Select(o => o.ToPoint2D());

            DrawGraph(samples, idealLine);



            var samples2 = Enumerable.Range(0, 1000).
                Select(o =>
                    {
                        double randPercent = rand.NextDouble();
                        Point3D point = BezierUtil.GetPoint(randPercent, bezier);
                        return Tuple.Create(randPercent, point.X, point.Y, point.Z);
                    }).
                OrderBy(o => o.Item1).
                ToArray();


            if (!samples2.All(o => o.Item4.IsNearZero()))
            {
                int three = 2;
            }

            if (!samples2.All(o => o.Item2.IsNearValue(o.Item3)))
            {
                int four = 7;
            }


            var samples2a = samples2.
                Select(o => Tuple.Create(o.Item1, o.Item2, o.Item1 - o.Item2)).
                ToArray();



            //IEnumerable<Point> testLine = Enumerable.Range(0, 150).
            //    Select(o => BezierUtil.GetPoint(o / 150d, bezier).ToPoint2D());

            //Rect bounds = GetBounds();

            //IEnumerable<Point> testLineFinal = idealLine
            //    .Select(o => new Point(bounds.Left + (o.X * bounds.Width), bounds.Bottom - (o.Y * bounds.Height)));

            //AddLine(testLineFinal, new SolidColorBrush(UtilityWPF.ColorFromHex("FF0000")), 1, "test line");
        }
        private void DrawBellArms()
        {
            #region OLD
            //var controlPoints = new List<Tuple<Point, Point>>();

            //// Arm1
            //if (!trkBellArmsLeftLen.Value.IsNearZero())
            //{
            //    Vector arm1 = new Vector(1, 1).ToUnit() * trkBellArmsLeftLen.Value;

            //    arm1 = arm1.
            //        ToVector3D().
            //        GetRotatedVector(new Vector3D(0, 0, -1), trkBellArmsLeftAngle.Value).
            //        ToVector2D();

            //    controlPoints.Add(Tuple.Create(new Point(0, 0), arm1.ToPoint()));
            //}

            //// Arm2
            //if (!trkBellArmsRightLen.Value.IsNearZero())
            //{
            //    Vector arm2 = new Vector(-1, -1).ToUnit() * trkBellArmsRightLen.Value;

            //    arm2 = arm2.
            //        ToVector3D().
            //        GetRotatedVector(new Vector3D(0, 0, -1), trkBellArmsRightAngle.Value).
            //        ToVector2D();

            //    Point point = new Point(1, 1);
            //    controlPoints.Add(Tuple.Create(point, point + arm2));
            //}

            //// Bezier
            //Point3D[] controlPoints3D = controlPoints.
            //    Select(o => o.Item2.ToPoint3D()).
            //    ToArray();

            //BezierSegment3D bezier = new BezierSegment3D(0, 1, controlPoints3D, new[] { new Point3D(0, 0, 0), new Point3D(1, 1, 0) });
            #endregion

            RandomBellArgs bezier = new RandomBellArgs(trkBellArmsLeftLen.Value, trkBellArmsLeftAngle.Value, trkBellArmsRightLen.Value, trkBellArmsRightAngle.Value);

            // Points
            Random rand = StaticRandom.GetRandomForThread();

            IEnumerable<double> samples = Enumerable.Range(0, 75000).
                //Select(o => BezierUtil.GetPoint(rand.NextDouble(), bezier).Y);
                Select(o => rand.NextBell(bezier));

            IEnumerable<Point> idealLine = BezierUtil.GetPoints(100, bezier.Bezier).
                Select(o => o.ToPoint2D());

            // Draw
            DrawGraph(samples, idealLine);

            Rect bounds = GetBounds();

            if (!trkBellArmsLeftLen.Value.IsNearZero())
            {
                Point from = GetScaledPoint(bezier.Bezier.EndPoint0, bounds);
                Point to = GetScaledPoint(bezier.Bezier.ControlPoints[0], bounds);
                AddLine(from, to, _brushLightBlue);
            }

            if (!trkBellArmsRightLen.Value.IsNearZero())
            {
                Point from = GetScaledPoint(bezier.Bezier.EndPoint1, bounds);
                Point to = GetScaledPoint(bezier.Bezier.ControlPoints[bezier.Bezier.ControlPoints.Length - 1], bounds);
                AddLine(from, to, _brushLightBlue);
            }

            // Report coords
            lblBellValues.Text = GetBellArmValues();
            lblBellCtrlPoints.Text = GetBellPoints(bezier.Bezier.ControlPoints.Select(o => o.ToPoint2D()));
        }
        private void DrawBell3()
        {
            Point[] points = GetBellPoints(txtBell3.Text);
            if (points == null)
            {
                txtBell3.Effect = _errorEffect;
                return;
            }

            txtBell3.Effect = null;

            BezierSegment3D bezier = new BezierSegment3D(0, 1, points.Select(o => o.ToPoint3D()).ToArray(), new[] { new Point3D(0, 0, 0), new Point3D(1, 1, 0) });

            Random rand = StaticRandom.GetRandomForThread();

            IEnumerable<double> samples = Enumerable.Range(0, 100000).
                Select(o => BezierUtil.GetPoint(rand.NextDouble(), bezier).Y);

            IEnumerable<Point> idealLine = BezierUtil.GetPoints(100, bezier).
                Select(o => o.ToPoint2D());

            DrawGraph(samples, idealLine);

            Rect bounds = GetBounds();

            for (int cntr = 0; cntr < bezier.ControlPoints.Length - 1; cntr++)
            {
                Point from = GetScaledPoint(bezier.ControlPoints[cntr], bounds);
                Point to = GetScaledPoint(bezier.ControlPoints[cntr + 1], bounds);
                AddLine(from, to, _brushLightBlue);
            }

            if (bezier.ControlPoints.Length > 0)
            {
                Point from = GetScaledPoint(bezier.EndPoint0, bounds);
                Point to = GetScaledPoint(bezier.ControlPoints[0], bounds);
                AddLine(from, to, _brushLightBlue);

                from = GetScaledPoint(bezier.EndPoint1, bounds);
                to = GetScaledPoint(bezier.ControlPoints[bezier.ControlPoints.Length - 1], bounds);
                AddLine(from, to, _brushLightBlue);
            }
        }

        private void DrawGraph(IEnumerable<double> randomSamples, IEnumerable<Point> idealLine)
        {
            Tuple<double, int>[] usage = Categorize(randomSamples);

            double max = usage.Max(o => o.Item2);

            Clear();

            Rect bounds = GetBounds();

            IEnumerable<Point> samplePoints = usage.
                Select(o =>
                {
                    double percent = o.Item2 / max;
                    //return new Point(bounds.Left + (o.Item1 * bounds.Width), bounds.Bottom - (percent * bounds.Height));
                    return GetScaledPoint(o.Item1, percent, bounds);
                });

            IEnumerable<Point> idealLineFinal = idealLine.
                //Select(o => new Point(bounds.Left + (o.X * bounds.Width), bounds.Bottom - (o.Y * bounds.Height)));
                Select(o => GetScaledPoint(o, bounds));

            // Grid
            AddLine(bounds.BottomLeft, bounds.TopRight, _brushVeryLight);      // diagonal

            AddLine(bounds.TopLeft, bounds.TopRight, _brushVeryLight);      // horz 1
            double y = bounds.Bottom - (bounds.Height * .75);
            AddLine(new Point(bounds.Left, y), new Point(bounds.Right, y), _brushVeryLight);      // horz .75
            y = bounds.Bottom - (bounds.Height * .5);
            AddLine(new Point(bounds.Left, y), new Point(bounds.Right, y), _brushVeryLight);      // horz .5
            y = bounds.Bottom - (bounds.Height * .25);
            AddLine(new Point(bounds.Left, y), new Point(bounds.Right, y), _brushVeryLight);      // horz .25

            AddLine(bounds.TopRight, bounds.BottomRight, _brushVeryLight);      // vert 1
            double x = bounds.Left + (bounds.Width * .75);
            AddLine(new Point(x, bounds.Top), new Point(x, bounds.Bottom), _brushVeryLight);      // vert .75
            x = bounds.Left + (bounds.Width * .5);
            AddLine(new Point(x, bounds.Top), new Point(x, bounds.Bottom), _brushVeryLight);      // vert .5
            x = bounds.Left + (bounds.Width * .25);
            AddLine(new Point(x, bounds.Top), new Point(x, bounds.Bottom), _brushVeryLight);      // vert .25

            AddLine(new[] { bounds.TopLeft, bounds.BottomLeft, bounds.BottomRight }, _brushDark);     // axiis

            // Important Lines
            AddLine(samplePoints, _brushSample, 2, "sample occurrence");
            AddLine(idealLineFinal, _brushIdeal, 1, "graph of function\r\ny=F(rand.nextdouble)");

            // Show statistics:
            //      area under curve
            //      avg
            //      stand dev
        }

        private static Tuple<double, int>[] Categorize(IEnumerable<double> randomValues, double precision = .01)
        {
            double half = precision / 2d;

            return randomValues.
                Select(o => new
                {
                    Orig = o,
                    Int = (o / precision).ToInt_Floor(),        // the int represents a bucket of all items that fall between a precision
                }).
                ToLookup(o => o.Int).
                OrderBy(o => o.Key).
                Select(o => Tuple.Create((o.Key * precision) + half, o.Count())).
                ToArray();
        }

        private void Clear()
        {
            _canvas.Children.Clear();
        }

        private void AddDot(Point position, Brush brush, double size = 16, string tooltip = null)
        {
            Ellipse dot = new Ellipse()
            {
                Fill = brush,
                Width = size,
                Height = size
            };

            double halfSize = size / 2d;

            Canvas.SetLeft(dot, position.X - halfSize);
            Canvas.SetTop(dot, position.Y - halfSize);

            if (tooltip != null)
            {
                dot.ToolTip = tooltip;
            }

            _canvas.Children.Add(dot);
        }
        private void AddLine(Point from, Point to, Brush brush, double width = 1, string tooltip = null)
        {
            Line line = new Line()
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = brush,
                StrokeThickness = width
            };

            if (tooltip != null)
            {
                line.ToolTip = tooltip;
            }

            _canvas.Children.Add(line);
        }
        private void AddLine(IEnumerable<Point> points, Brush brush, double width = 1, string tooltip = null)
        {
            PathFigure figure = new PathFigure() { IsClosed = false };
            figure.StartPoint = points.First();
            figure.Segments.AddRange(points.Skip(1).Select(o => new LineSegment() { Point = o }));

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            var path = new System.Windows.Shapes.Path()
            {
                Stroke = brush,
                StrokeThickness = width,
                Data = geometry,
            };

            if (tooltip != null)
            {
                path.ToolTip = tooltip;
            }

            _canvas.Children.Add(path);
        }

        private Rect GetBounds()
        {
            double marginX = _canvas.ActualWidth * .05;
            double marginY = _canvas.ActualHeight * .05;

            return new Rect(marginX, marginY, _canvas.ActualWidth - (marginX * 2), _canvas.ActualHeight - (marginY * 2));
        }

        private string GetBellArmValues()
        {
            var reportArm = new Func<Slider, Slider, string>((len, ang) =>
            {
                if (len.Value.IsNearZero())
                {
                    return "---";
                }

                return string.Format("l {0}, a {1}", Math.Round(len.Value, 2).ToString(), Math.Round(ang.Value, 1).ToString());
            });

            string arm1 = "left: " + reportArm(trkBellArmsLeftLen, trkBellArmsLeftAngle);
            string arm2 = "right: " + reportArm(trkBellArmsRightLen, trkBellArmsRightAngle);

            return arm1 + "\r\n" + arm2;
        }

        private void RebuildBell3Combo()
        {
            string foldername = UtilityCore.GetOptionsFolder();
            foldername = System.IO.Path.Combine(foldername, FOLDER);

            string[] filenames = Directory.GetFiles(foldername, "*" + BELL3);

            try
            {
                _isProgramaticallyChanging = true;

                cboBell3.Items.Clear();

                foreach (string filename in filenames)
                {
                    Point[] points = UtilityCore.DeserializeFromFile<Point[]>(filename);

                    string pointText = string.Join("\r\n", points.Select(o => string.Format("{0} {1}", o.X, o.Y)));

                    TextBlock text = new TextBlock()
                    {
                        Text = string.Format("preset {0}", cboBell3.Items.Count + 1),
                        Tag = pointText,
                    };

                    cboBell3.Items.Add(text);
                }
            }
            finally
            {
                _isProgramaticallyChanging = false;
            }
        }

        private static Point[] GetBellPoints(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            List<Point> retVal = new List<Point>();

            foreach (string line in text.Split("\r\n".ToCharArray()))
            {
                if (line.Trim() == "")
                {
                    continue;
                }

                Match match = Regex.Match(line, @"^\s*(?<x>[^\s]+)\s+(?<y>[^\s]+)\s*$");
                if (!match.Success)
                {
                    return null;
                }

                double x, y;
                if (!double.TryParse(match.Groups["x"].Value, out x))
                {
                    return null;
                }

                if (!double.TryParse(match.Groups["y"].Value, out y))
                {
                    return null;
                }

                retVal.Add(new Point(x, y));
            }

            return retVal.ToArray();
        }
        private static string GetBellPoints(IEnumerable<Point> points)
        {
            var lines = points.
                Select(o => string.Format("{0} {1}", Math.Round(o.X, 2), Math.Round(o.Y, 2)));

            return string.Join("\r\n", lines);
        }

        private static Point GetScaledPoint(Point3D point, Rect rect)
        {
            return GetScaledPoint(point.X, point.Y, rect);
        }
        private static Point GetScaledPoint(Point point, Rect rect)
        {
            return GetScaledPoint(point.X, point.Y, rect);
        }
        private static Point GetScaledPoint(double x, double y, Rect rect)
        {
            return new Point(rect.Left + (x * rect.Width), rect.Bottom - (y * rect.Height));
        }

        #endregion
    }
}
