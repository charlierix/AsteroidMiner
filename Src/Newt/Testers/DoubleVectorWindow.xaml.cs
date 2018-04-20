﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;

namespace Game.Newt.Testers
{
    /// <summary>
    /// This form was written to test DoubleVector.GetAngleAroundAxis
    /// </summary>
    public partial class DoubleVectorWindow : Window
    {
        #region Class: ItemColors

        private class ItemColors
        {
            private Random _rand = new Random();

            public Color Highlight = UtilityWPF.ColorFromHex("F2BC57");
            public Color LightLight = UtilityWPF.ColorFromHex("FBFCF2");
            public Color Light = UtilityWPF.ColorFromHex("F1F1E4");
            public Color Gray = UtilityWPF.ColorFromHex("8B8C84");
            public Color DarkGray = UtilityWPF.ColorFromHex("3C4140");

            //public Color TrackballAxisX = UtilityWPF.ColorFromHex("8B8C84");
            //public Color TrackballAxisY = UtilityWPF.ColorFromHex("72726C");
            //public Color TrackballAxisZ = UtilityWPF.ColorFromHex("989990");

            public DiffuseMaterial TrackballAxisMajor_From = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("4C638C")));
            public Color TrackballAxisLine_From = UtilityWPF.ColorFromHex("604D5B73");
            public DiffuseMaterial TrackballAxisMinor_From = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("4C638C")));

            public DiffuseMaterial TrackballAxisMajor_To = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("694C8C")));
            public Color TrackballAxisLine_To = UtilityWPF.ColorFromHex("605E4D73");
            public DiffuseMaterial TrackballAxisMinor_To = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("694C8C")));

            public SpecularMaterial TrackballAxisSpecular = new SpecularMaterial(Brushes.White, 100d);

            public Color TrackballGrabberHoverLight = UtilityWPF.ColorFromHex("3F382A");

            public Color FromLine
            {
                get
                {
                    //return this.Gray;
                    return UtilityWPF.ColorFromHex("446EB8");
                }
            }
            public Color ToLine
            {
                get
                {
                    //return this.Gray;
                    return UtilityWPF.ColorFromHex("6B4896");
                }
            }
            public Color RotatedLine
            {
                get
                {
                    return this.Highlight;
                }
            }
        }

        #endregion

        #region Declaration Section

        private const double LINELENGTH_X = 2d;
        private const double LINELENGTH_Y = .5d;
        private const double LINELENGTH_Z = 1.5d;

        private Random _rand = new Random();
        private ItemColors _colors = new ItemColors();
        private bool _isInitialized = false;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        // 0=X, 1=Y, 2=Z
        ScreenSpaceLines3D[] _lineFrom = null;
        ScreenSpaceLines3D[] _lineTo = null;
        ScreenSpaceLines3D[] _lineTransformed = null;

        List<Visual3D> _tempVisuals = new List<Visual3D>();

        private List<ModelVisual3D> _orientationVisualsFrom = new List<ModelVisual3D>();
        private TrackballGrabber _orientationTrackballFrom = null;

        private List<ModelVisual3D> _orientationVisualsTo = new List<ModelVisual3D>();
        private TrackballGrabber _orientationTrackballTo = null;

        #endregion

        #region Constructor

        public DoubleVectorWindow()
        {
            InitializeComponent();
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

                // Trackball Controls
                SetupFromTrackball();
                SetupToTrackball();

                // Add the 3 axiis to the main viewport
                _lineFrom = AddLines(_orientationTrackballFrom.Transform, true);
                _lineTo = AddLines(_orientationTrackballTo.Transform, false);

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void OrientationTrackballFrom_RotationChanged(object sender, EventArgs e)
        {
            ClearTempVisuals();

            // Wipe the transformed lines until they click the button
            ClearLines(_lineTransformed);
            _lineTransformed = null;

            // Rebuild the from lines
            ClearLines(_lineFrom);
            _lineFrom = AddLines(_orientationTrackballFrom.Transform, true);
        }
        private void OrientationTrackballTo_RotationChanged(object sender, EventArgs e)
        {
            ClearTempVisuals();

            // Wipe the transformed lines until they click the button
            ClearLines(_lineTransformed);
            _lineTransformed = null;

            // Rebuild the to lines
            ClearLines(_lineTo);
            _lineTo = AddLines(_orientationTrackballTo.Transform, false);
        }

        private void grdFrom_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (chkSnap45Degrees.IsChecked.Value)
            {
                EnsureSnapped45(_orientationTrackballFrom);
            }
        }
        private void grdTo_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (chkSnap45Degrees.IsChecked.Value)
            {
                EnsureSnapped45(_orientationTrackballTo);
            }
        }
        private void chkSnap45Degrees_Checked(object sender, RoutedEventArgs e)
        {
            if (chkSnap45Degrees.IsChecked.Value)
            {
                EnsureSnapped45(_orientationTrackballFrom);
                EnsureSnapped45(_orientationTrackballTo);
            }
        }

        private void btnTestIt_Click(object sender, RoutedEventArgs e)
        {
            Vector3D defaultStand = new Vector3D(LINELENGTH_X * .75d, 0, 0);
            Vector3D defaultOrth = new Vector3D(0, 0, LINELENGTH_Z * .75d);

            DoubleVector from = new DoubleVector(_orientationTrackballFrom.Transform.Transform(defaultStand), _orientationTrackballFrom.Transform.Transform(defaultOrth));
            DoubleVector to = new DoubleVector(_orientationTrackballTo.Transform.Transform(defaultStand), _orientationTrackballTo.Transform.Transform(defaultOrth));

            // This is the method that this form is testing
            Quaternion quat = from.GetRotation(to);

            //DoubleVector rotated = new DoubleVector(quat.GetRotatedVector(defaultStand), quat.GetRotatedVector(defaultOrth));
            DoubleVector rotated = new DoubleVector(quat.GetRotatedVector(from.Standard), quat.GetRotatedVector(from.Orth));		// quat describes how to rotate from From to To (not from Default to To)

            //Vector3D axis = quat.Axis.ToUnit() * (LINELENGTH_X * 2d);
            Vector3D axis = _orientationTrackballFrom.Transform.Transform(quat.Axis.ToUnit() * (LINELENGTH_X * 1.5d));		// I rotate axis by from, because I want it relative to from (axis isn't in world coords)

            pnlReport.Children.Clear();
            ClearTempVisuals();
            ClearLines(_lineTransformed);
            _lineTransformed = AddLines(rotated, axis);
        }
        private void btnGetRotationDblVect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Quaternion from = _orientationTrackballFrom.Transform.ToQuaternion();
                Quaternion to = _orientationTrackballTo.Transform.ToQuaternion();
                Quaternion delta1 = Math3D.GetRotation(from, to);
                //Quaternion delta = Quaternion.Slerp(from, to, 1, true);        // this does the rotation for me, but doesn't tell how to rotate
                Quaternion delta2 = GetDelta(from, to);

                pnlReport.Children.Clear();
                ClearTempVisuals();
                ClearLines(_lineFrom);
                ClearLines(_lineTo);
                ClearLines(_lineTransformed);

                _lineFrom = AddLines(new[] { from }, _colors.FromLine);
                _lineTo = AddLines(new[] { to }, _colors.ToLine);
                _lineTransformed = AddLines(new[] { delta1, delta2 }, _colors.RotatedLine);

                string format = "axis={0} | angle={1}";
                double fontsize = 16;
                pnlReport.Children.Add(new TextBlock() { Text = string.Format(format, from.Axis.ToString(true), from.Angle.ToString()), Foreground = new SolidColorBrush(_colors.FromLine), FontSize = fontsize });
                pnlReport.Children.Add(new TextBlock() { Text = string.Format(format, to.Axis.ToString(true), to.Angle.ToString()), Foreground = new SolidColorBrush(_colors.ToLine), FontSize = fontsize });
                pnlReport.Children.Add(new TextBlock() { Text = string.Format(format, delta1.Axis.ToString(true), delta1.Angle.ToString()), Foreground = new SolidColorBrush(_colors.RotatedLine), FontSize = fontsize });
                pnlReport.Children.Add(new TextBlock() { Text = string.Format(format, delta2.Axis.ToString(true), delta2.Angle.ToString()), Foreground = new SolidColorBrush(_colors.RotatedLine), FontSize = fontsize });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnGetRotationPlanes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pnlReport.Children.Clear();
                ClearTempVisuals();
                //ClearLines(_lineFrom);
                //ClearLines(_lineTo);
                ClearLines(_lineTransformed);

                Quaternion from = _orientationTrackballFrom.Transform.ToQuaternion();
                Quaternion to = _orientationTrackballTo.Transform.ToQuaternion();

                Point3D[] points = Enumerable.Range(0, 6).
                    Select(o => Math3D.GetRandomVector_Circular(5)).        // this creates vectors in the XY plane
                    Select(o => new Point3D(o.X, 0, o.Y)).      // the visuals are built along XZ plane
                    Select((o, i) => i < 3 ? from.GetRotatedVector(o) : to.GetRotatedVector(o)).        // first three use from, last three use to
                    ToArray();

                Triangle plane1 = new Triangle(points[0], points[1], points[2]);
                Triangle plane2 = new Triangle(points[3], points[4], points[5]);

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = UtilityWPF.GetPlane(plane1, 10, _colors.FromLine, Colors.Silver, center: new Point3D(0, 0, 0));
                _viewport.Children.Add(visual);
                _tempVisuals.Add(visual);

                visual = new ModelVisual3D();
                visual.Content = UtilityWPF.GetPlane(plane2, 10, _colors.ToLine, Colors.Silver, center: new Point3D(0, 0, 0));
                _viewport.Children.Add(visual);
                _tempVisuals.Add(visual);

                Quaternion delta1 = Math3D.GetRotation(from, to);
                //Quaternion delta2 = Math3D.GetRotation(plane1, plane2);
                //Quaternion delta2 = GetRotation(plane1, plane2);
                Quaternion delta2 = Math3D.GetRotation(plane1.Point1 - plane1.Point0, plane1.Point2 - plane1.Point0, plane2.Point1 - plane2.Point0, plane2.Point2 - plane2.Point0);

                //Quaternion deltaX = Math3D.GetRotation(from.GetRotatedVector(new Vector3D(1, 0, 0)), to.GetRotatedVector(new Vector3D(1, 0, 0)));
                //Quaternion deltaY = Math3D.GetRotation(from.GetRotatedVector(new Vector3D(0, 1, 0)), to.GetRotatedVector(new Vector3D(0, 1, 0)));
                //Quaternion deltaZ = Math3D.GetRotation(from.GetRotatedVector(new Vector3D(0, 0, 1)), to.GetRotatedVector(new Vector3D(0, 0, 1)));

                _tempVisuals.AddRange(AddLines(new[] { delta1 }, _colors.RotatedLine));
                _tempVisuals.AddRange(AddLines(new[] { delta2 }, Colors.Red));
                //_tempVisuals.AddRange(BuildLines(new[] { deltaX }, Colors.Black));
                //_tempVisuals.AddRange(BuildLines(new[] { deltaY }, Colors.Gray));
                //_tempVisuals.AddRange(BuildLines(new[] { deltaZ }, Colors.White));


                Tuple<Point3D, Point3D>[] segments = new[] { Tuple.Create(plane1.Point0, plane1.Point1), Tuple.Create(plane1.Point1, plane1.Point2), Tuple.Create(plane1.Point2, plane1.Point0) };
                _tempVisuals.AddRange(AddLines(segments, _colors.FromLine));

                var segmentsRotated = segments.Select(o => Tuple.Create(delta1.GetRotatedVector(o.Item1), delta1.GetRotatedVector(o.Item2)));
                _tempVisuals.AddRange(AddLines(segmentsRotated, _colors.RotatedLine));

                segmentsRotated = segments.Select(o => Tuple.Create(delta2.GetRotatedVector(o.Item1), delta2.GetRotatedVector(o.Item2)));
                _tempVisuals.AddRange(AddLines(segmentsRotated, Colors.Red));


                Point3D testPoint = new Point3D(1, 0, 0);
                _tempVisuals.Add(AddDot(testPoint, Colors.Black));
                _tempVisuals.Add(AddDot(delta1.GetRotatedVector(testPoint), _colors.RotatedLine));
                _tempVisuals.Add(AddDot(delta2.GetRotatedVector(testPoint), Colors.Red));



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnGetRotationPlanes2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pnlReport.Children.Clear();
                ClearTempVisuals();
                ClearLines(_lineTransformed);

                Quaternion trackballFromQuat = _orientationTrackballFrom.Transform.ToQuaternion();
                Quaternion trackballToQuat = _orientationTrackballTo.Transform.ToQuaternion();

                Point3D[] points1 = Enumerable.Range(0, 3).
                    Select(o => Math3D.GetRandomVector_Circular(5)).        // this creates vectors in the XY plane
                    Select(o => new Point3D(o.X, 0, o.Y)).      // the visuals are built along XZ plane
                    Select(o => trackballFromQuat.GetRotatedVector(o)).
                    ToArray();

                Triangle plane1 = new Triangle(points1[0], points1[1], points1[2]);

                Quaternion delta1 = Math3D.GetRotation(trackballFromQuat, trackballToQuat);
                Transform3D transform = new RotateTransform3D(new QuaternionRotation3D(delta1));

                Triangle plane2 = new Triangle(transform.Transform(points1[0]), transform.Transform(points1[1]), transform.Transform(points1[2]));

                // Test out the get rotate method
                Quaternion delta2 = Math3D.GetRotation(plane1.Point1 - plane1.Point0, plane1.Point2 - plane1.Point0, plane2.Point1 - plane2.Point0, plane2.Point2 - plane2.Point0);

                #region Draw

                // Planes
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = UtilityWPF.GetPlane(plane1, 10, _colors.FromLine, Colors.Silver, center: new Point3D(0, 0, 0));
                _viewport.Children.Add(visual);
                _tempVisuals.Add(visual);

                visual = new ModelVisual3D();
                visual.Content = UtilityWPF.GetPlane(plane2, 10, _colors.ToLine, Colors.Silver, center: new Point3D(0, 0, 0));
                _viewport.Children.Add(visual);
                _tempVisuals.Add(visual);

                // Quats
                _tempVisuals.AddRange(AddLines(new[] { delta1 }, _colors.RotatedLine));
                _tempVisuals.AddRange(AddLines(new[] { delta2 }, Colors.Red));

                // Triangles
                Tuple<Point3D, Point3D>[] segments = new[] { Tuple.Create(plane1.Point0, plane1.Point1), Tuple.Create(plane1.Point1, plane1.Point2), Tuple.Create(plane1.Point2, plane1.Point0) };
                _tempVisuals.AddRange(AddLines(segments, _colors.FromLine));

                var segmentsRotated = segments.Select(o => Tuple.Create(delta1.GetRotatedVector(o.Item1), delta1.GetRotatedVector(o.Item2)));
                _tempVisuals.AddRange(AddLines(segmentsRotated, _colors.RotatedLine));

                segmentsRotated = segments.Select(o => Tuple.Create(delta2.GetRotatedVector(o.Item1), delta2.GetRotatedVector(o.Item2)));
                _tempVisuals.AddRange(AddLines(segmentsRotated, Colors.Red));

                // Points
                Point3D testPoint = new Point3D(1, 0, 0);
                _tempVisuals.Add(AddDot(testPoint, Colors.Black));
                _tempVisuals.Add(AddDot(delta1.GetRotatedVector(testPoint), _colors.RotatedLine));
                _tempVisuals.Add(AddDot(delta2.GetRotatedVector(testPoint), Colors.Red));

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Transform2D_bad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //bad
                ITriangle plane = new Triangle(new Point3D(1.63269152179486, 2.53001877553981, 0), new Point3D(0.544230507264952, 2.53001877553981, 0), new Point3D(0.544230507264952, 2.53001877553981, 4.56340608087975));

                Point3D[] polyPoints = new[]
                {
                    new Point3D(0.544230507264952,2.53001877553981,0),
                    new Point3D(0.544230507264952,2.53001877553981,4.56340608087975),
                    new Point3D(1.63269152179486,2.53001877553981,4.56340608087975),
                    new Point3D(1.63269152179486,2.53001877553981,0),
                };

                VisualizeTranform2D_overall(plane, polyPoints);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Transform2D_better_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //better
                ITriangle plane = new Triangle(new Point3D(1.63269152179486, 2.53001877553981, 0), new Point3D(1.63269152179486, 2.53001877553981, 4.56340608087975), new Point3D(1.63269152179486, 0, 4.56340608087975));

                Point3D[] polyPoints = new[]
                {
                    new Point3D(1.63269152179486,2.53001877553981,4.56340608087975),
                    new Point3D(1.63269152179486,0,4.56340608087975),
                    new Point3D(1.63269152179486,0,0),
                    new Point3D(1.63269152179486,2.53001877553981,0),
                };

                VisualizeTranform2D_overall(plane, polyPoints);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RotateTestCase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const double AXISLEN = 3;
                const double LINETHICK = .01;
                const double DOTRAD = .05;

                Color colorFrom1 = UtilityWPF.ColorFromHex("666");
                Color colorFrom2 = UtilityWPF.ColorFromHex("000");
                Color colorTo1 = UtilityWPF.ColorFromHex("FFF");
                Color colorTo2 = UtilityWPF.ColorFromHex("CCC");

                #region initial conditions

                //Vector3D from1 = new Vector3D(-1.08846101452991, 0, 0);
                //Vector3D from2 = new Vector3D(0, 0, 5.4064833988896);
                Vector3D from1 = new Vector3D(-1, 0, 0);
                Vector3D from2 = new Vector3D(0, 0, 1);
                Vector3D to1 = new Vector3D(1, 0, 0);
                Vector3D to2 = new Vector3D(0, 1, 0);

                #region RANDOM ROTATE

                // This rotation doesn't matter.  It seems to be the perfect 90 then perfect 180
                //Transform3D randRot = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation()));
                //from1 = randRot.Transform(from1);
                //from2 = randRot.Transform(from2);
                //to1 = randRot.Transform(to1);
                //to2 = randRot.Transform(to2);

                #endregion
                #region SLIGHT ADJUST

                //double maxAngle = .1;
                //from1 = Math3D.GetRandomVector_Cone(from1, maxAngle);
                //from2 = Math3D.GetRandomVector_Cone(from2, maxAngle);
                //to1 = Math3D.GetRandomVector_Cone(to1, maxAngle);
                //to2 = Math3D.GetRandomVector_Cone(to2, maxAngle);

                #endregion

                var window = new HelperClassesWPF.Controls3D.Debug3DWindow()
                {
                    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("AAA")),
                };

                window.AddLine(new Point3D(0, 0, 0), from1.ToPoint(), LINETHICK, colorFrom1);
                window.AddLine(new Point3D(0, 0, 0), from2.ToPoint(), LINETHICK, colorFrom2);

                window.AddLine(new Point3D(0, 0, 0), to1.ToPoint(), LINETHICK, colorTo1);
                window.AddLine(new Point3D(0, 0, 0), to2.ToPoint(), LINETHICK, colorTo2);

                window.Show();

                #endregion

                #region quat multiply

                window = new HelperClassesWPF.Controls3D.Debug3DWindow()
                {
                    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("777")),
                };

                window.AddAxisLines(AXISLEN, LINETHICK * .5);

                Quaternion rotation_mult = Math3D.GetRotation(from1, from2, to1, to2);
                //Quaternion rotation_mult = GetRotation_fixed(window, from1, from2, to1, to2);

                ITriangle planeInit = new Triangle(new Point3D(-1, 0, 0), new Point3D(0, 0, 0), new Point3D(0, 0, 1));

                Point3D[] planeRotatedPoints = planeInit.PointArray.ToArray();
                rotation_mult.GetRotatedVector(planeRotatedPoints);

                ITriangle planeRotated = new Triangle(planeRotatedPoints[0], planeRotatedPoints[1], planeRotatedPoints[2]);

                window.AddLine(new Point3D(0, 0, 0), (planeInit[1] - planeInit[0]).ToPoint(), LINETHICK, colorFrom1);
                window.AddLine(new Point3D(0, 0, 0), (planeInit[2] - planeInit[0]).ToPoint(), LINETHICK, colorFrom2);
                window.AddLine(new Point3D(0, 0, 0), (planeRotated[1] - planeRotated[0]).ToPoint(), LINETHICK, colorTo1);
                window.AddLine(new Point3D(0, 0, 0), (planeRotated[2] - planeRotated[0]).ToPoint(), LINETHICK, colorTo2);

                window.AddPlane(planeInit, AXISLEN, UtilityWPF.ColorFromHex("00FF00"), UtilityWPF.ColorFromHex("A0FFA0"));
                window.AddPlane(planeRotated, AXISLEN, UtilityWPF.ColorFromHex("FF0000"), UtilityWPF.ColorFromHex("FFA0A0"));

                window.Show();

                #endregion

                #region tranform group

                //---------------
                // The transform group with two rotate transforms had the same issue as quaternion.multiply
                // A perfect 180 degree quaternion caused random failure
                //---------------

                //window = new HelperClassesWPF.Controls3D.Debug3DWindow();

                //window.AddAxisLines(AXISLEN, LINETHICK * .5);

                //Transform3D transform = GetRotation_transform(window, from1, from2, to1, to2);

                //planeRotatedPoints = planeInit.PointArray.
                //    Select(o => transform.Transform(o)).
                //    ToArray();

                //planeRotated = new Triangle(planeRotatedPoints[0], planeRotatedPoints[1], planeRotatedPoints[2]);

                //window.AddLine(new Point3D(0, 0, 0), (planeInit[1] - planeInit[0]).ToPoint(), LINETHICK, colorFrom1);
                //window.AddLine(new Point3D(0, 0, 0), (planeInit[2] - planeInit[0]).ToPoint(), LINETHICK, colorFrom2);
                //window.AddLine(new Point3D(0, 0, 0), (planeRotated[1] - planeRotated[0]).ToPoint(), LINETHICK, colorTo1);
                //window.AddLine(new Point3D(0, 0, 0), (planeRotated[2] - planeRotated[0]).ToPoint(), LINETHICK, colorTo2);

                //window.AddPlane(planeInit, AXISLEN, UtilityWPF.ColorFromHex("8F8"), UtilityWPF.ColorFromHex("A0FFA0"));
                //window.AddPlane(planeRotated, AXISLEN, UtilityWPF.ColorFromHex("F88"), UtilityWPF.ColorFromHex("FFA0A0"));

                //window.Show();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods - transform 2D

        private static void VisualizeTranform2D_overall(ITriangle plane, Point3D[] polyPoints)
        {
            const double AXISLEN = 3;
            const double LINETHICK = .01;
            const double DOTRAD = .05;

            var window = new HelperClassesWPF.Controls3D.Debug3DWindow();

            window.AddAxisLines(AXISLEN, LINETHICK * .5);

            #region plane

            window.AddDots(plane.PointArray, DOTRAD * 1.1, UtilityWPF.ColorFromHex("FF0000"));
            window.AddPlane(plane, AXISLEN, UtilityWPF.ColorFromHex("FF0000"));

            window.AddLine(plane[0], plane[1], LINETHICK, Colors.White);
            window.AddLine(plane[0], plane[2], LINETHICK, Colors.Black);

            #endregion

            //var transform2D = GetTransformTo2D_custom_CLEAN(window, plane);
            var transform2D = GetTransformTo2D_custom_USES_DBLVECT(window, plane);

            #region transformed 2D - natural

            var transformed_natural = polyPoints.
                //Select(o => transform2D.Item1.Transform(o).ToPoint2D().ToPoint3D()).
                Select(o => transform2D.Item1.Transform(o)).
                ToArray();

            ITriangle transformedPlane_natural = new Triangle(transformed_natural[0], transformed_natural[1], transformed_natural[2]);

            window.AddDots(transformed_natural, DOTRAD, UtilityWPF.ColorFromHex("70FF70"));
            window.AddPlane(transformedPlane_natural, AXISLEN, UtilityWPF.ColorFromHex("40FF40"), UtilityWPF.ColorFromHex("C0FFC0"));

            #endregion
            #region transformed 2D - forced

            var transformed_forced = polyPoints.
                Select(o => transform2D.Item1.Transform(o).ToPoint2D().ToPoint3D()).
                //Select(o => transform2D.Item1.Transform(o)).
                ToArray();

            ITriangle transformedPlane_forced = new Triangle(transformed_forced[0], transformed_forced[1], transformed_forced[2]);

            window.AddDots(transformed_forced, DOTRAD, UtilityWPF.ColorFromHex("FF7070"));
            window.AddPlane(transformedPlane_forced, AXISLEN, UtilityWPF.ColorFromHex("FF4040"), UtilityWPF.ColorFromHex("FFC0C0"));

            #endregion

            #region transformed back 3D - natural

            var transformedBack_natural = transformed_natural.
                Select(o => transform2D.Item2.Transform(o)).
                ToArray();

            window.AddDots(transformedBack_natural, DOTRAD, UtilityWPF.ColorFromHex("C0FFC0"));
            window.AddPlane(new Triangle(transformedBack_natural[0], transformedBack_natural[1], transformedBack_natural[2]), AXISLEN, UtilityWPF.ColorFromHex("C0FFC0"));

            #endregion
            #region transformed back 3D - forced

            var transformedBack_forced = transformed_forced.
                Select(o => transform2D.Item2.Transform(o)).
                ToArray();

            window.AddDots(transformedBack_forced, DOTRAD, UtilityWPF.ColorFromHex("FFC0C0"));
            window.AddPlane(new Triangle(transformedBack_forced[0], transformedBack_forced[1], transformedBack_forced[2]), AXISLEN, UtilityWPF.ColorFromHex("FFC0C0"));

            #endregion

            window.Show();
        }

        public static Tuple<Transform3D, Transform3D> GetTransformTo2D_custom_USES_DBLVECT(HelperClassesWPF.Controls3D.Debug3DWindow window, ITriangle triangle)
        {
            if (Math.Abs(Vector3D.DotProduct(triangle.NormalUnit, new Vector3D(0, 0, 1))).IsNearValue(1))
            {
                // It's already 2D
                window.AddMessage("already 2D");
                return new Tuple<Transform3D, Transform3D>(new TranslateTransform3D(0, 0, -triangle.Point0.Z), new TranslateTransform3D(0, 0, triangle.Point0.Z));
            }

            Vector3D line1 = triangle.Point1 - triangle.Point0;
            Vector3D randomOrth = Math3D.GetOrthogonal(line1, triangle.Point2 - triangle.Point0);

            DoubleVector from = new DoubleVector(line1, randomOrth);
            DoubleVector to = new DoubleVector(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));

            Quaternion rotation = from.GetRotation(to);

            Transform3DGroup transformTo2D = new Transform3DGroup();
            transformTo2D.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));

            // Need to rotate the point so that it's parallel to the XY plane, then subtract off it's Z
            Point3D rotatedXYPlane = transformTo2D.Transform(triangle[0]);
            transformTo2D.Children.Add(new TranslateTransform3D(0, 0, -rotatedXYPlane.Z));

            Transform3DGroup transformTo3D = new Transform3DGroup();
            transformTo3D.Children.Add(new TranslateTransform3D(0, 0, rotatedXYPlane.Z));
            transformTo3D.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation.ToReverse())));

            return new Tuple<Transform3D, Transform3D>(transformTo2D, transformTo3D);
        }
        public static Tuple<Transform3D, Transform3D> GetTransformTo2D_custom_CLEAN(HelperClassesWPF.Controls3D.Debug3DWindow window, ITriangle triangle)
        {
            Vector3D zUp = new Vector3D(0, 0, 1);

            if (Math.Abs(Vector3D.DotProduct(triangle.NormalUnit, zUp)).IsNearValue(1))
            {
                // It's already 2D
                window.AddMessage("already 2D");
                return new Tuple<Transform3D, Transform3D>(new TranslateTransform3D(0, 0, -triangle.Point0.Z), new TranslateTransform3D(0, 0, triangle.Point0.Z));
            }

            // Don't bother with a double vector, just rotate the normal
            Quaternion rotation = Math3D.GetRotation(triangle.NormalUnit, zUp);

            Transform3DGroup transformTo2D = new Transform3DGroup();
            transformTo2D.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));

            // Need to rotate the point so that it's parallel to the XY plane, then subtract off it's Z
            Point3D rotatedXYPlane = transformTo2D.Transform(triangle[0]);
            transformTo2D.Children.Add(new TranslateTransform3D(0, 0, -rotatedXYPlane.Z));

            Transform3DGroup transformTo3D = new Transform3DGroup();
            transformTo3D.Children.Add(new TranslateTransform3D(0, 0, rotatedXYPlane.Z));
            transformTo3D.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation.ToReverse())));

            return new Tuple<Transform3D, Transform3D>(transformTo2D, transformTo3D);
        }

        #endregion
        #region Private Methods

        private static void EnsureSnapped45(TrackballGrabber trackball)
        {
            //TODO:  I think this is invalid thinking.  Instead?:
            //		Use the quaternion to project a vector
            //		Snap that projected vector to the nearest aligned vector
            //		Get a new quaternion that describes how to rotate to that snapped vector

            //TODO:  When the trackball's transform is set, the lights get screwed up

            Quaternion currentOrientation = trackball.Transform.ToQuaternion();

            // Snap the axis to nearest 45
            Vector3D snappedAxis = currentOrientation.Axis;

            // Snap the angle to nearest 45
            double snappedAngle = GetSnappedAngle(currentOrientation.Angle, 45d);

            trackball.Transform = new RotateTransform3D(new AxisAngleRotation3D(snappedAxis, snappedAngle));
        }

        private static double GetSnappedAngle(double angle, double snapValue)
        {
            double retVal = angle;

            // Make sure angle is between -360 to 360
            while (retVal < -360d)
            {
                retVal += 360d;
            }

            while (retVal > 360d)
            {
                retVal -= 360d;
            }

            double absSnapValue = Math.Abs(snapValue);

            // Check for positive angles
            for (double fromAngle = 0d; fromAngle <= 360d; fromAngle += absSnapValue)
            {
                if (retVal > fromAngle && retVal < fromAngle + absSnapValue)
                {
                    if (retVal - fromAngle < absSnapValue * .5d)
                    {
                        retVal = fromAngle;
                    }
                    else
                    {
                        retVal = fromAngle + absSnapValue;
                    }

                    return retVal;
                }
            }

            // Check for negative angles
            for (double fromAngle = 0d; fromAngle >= -360d; fromAngle -= absSnapValue)
            {
                if (retVal > fromAngle - absSnapValue && retVal < fromAngle)
                {
                    if (fromAngle - retVal < absSnapValue * .5d)
                    {
                        retVal = fromAngle;
                    }
                    else
                    {
                        retVal = fromAngle - absSnapValue;
                    }

                    return retVal;
                }
            }

            // It's already snapped to that value
            return retVal;
        }

        private void SetupFromTrackball()
        {
            Model3DGroup model = new Model3DGroup();

            // Major arrow along x
            model.Children.Add(TrackballGrabber.GetMajorArrow(Axis.X, true, _colors.TrackballAxisMajor_From, _colors.TrackballAxisSpecular));

            // Minor arrow along z
            model.Children.Add(TrackballGrabber.GetMinorArrow(Axis.Z, true, _colors.TrackballAxisMinor_From, _colors.TrackballAxisSpecular));

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            _viewportFrom.Children.Add(visual);
            _orientationVisualsFrom.Add(visual);

            // Create the trackball
            _orientationTrackballFrom = new TrackballGrabber(grdFrom, _viewportFrom, 1d, _colors.TrackballGrabberHoverLight);
            _orientationTrackballFrom.SyncedLights.Add(_cameraFromRotateLight);
            _orientationTrackballFrom.RotationChanged += new EventHandler(OrientationTrackballFrom_RotationChanged);

            // Faint lines
            _orientationTrackballFrom.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.X, false, _colors.TrackballAxisLine_From));
            _orientationTrackballFrom.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Y, _colors.TrackballAxisLine_From));
            _orientationTrackballFrom.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.Z, false, _colors.TrackballAxisLine_From));
        }
        private void SetupToTrackball()
        {
            Model3DGroup model = new Model3DGroup();

            // Major arrow along x
            model.Children.Add(TrackballGrabber.GetMajorArrow(Axis.X, true, _colors.TrackballAxisMajor_To, _colors.TrackballAxisSpecular));

            // Minor arrow along z
            model.Children.Add(TrackballGrabber.GetMinorArrow(Axis.Z, true, _colors.TrackballAxisMinor_To, _colors.TrackballAxisSpecular));

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            _viewportTo.Children.Add(visual);
            _orientationVisualsTo.Add(visual);

            // Create the trackball
            _orientationTrackballTo = new TrackballGrabber(grdTo, _viewportTo, 1d, _colors.TrackballGrabberHoverLight);
            _orientationTrackballTo.SyncedLights.Add(_cameraToLight);
            _orientationTrackballTo.RotationChanged += new EventHandler(OrientationTrackballTo_RotationChanged);

            // Faint lines
            _orientationTrackballTo.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.X, false, _colors.TrackballAxisLine_To));
            _orientationTrackballTo.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Y, _colors.TrackballAxisLine_To));
            _orientationTrackballTo.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.Z, false, _colors.TrackballAxisLine_To));
        }

        private void ClearTempVisuals()
        {
            _viewport.Children.RemoveAll(_tempVisuals);
            _tempVisuals.Clear();
        }
        private void ClearLines(ScreenSpaceLines3D[] lines)
        {
            if (lines != null)
            {
                for (int cntr = 0; cntr < lines.Length; cntr++)
                {
                    if (_viewport.Children.Contains(lines[cntr]))
                    {
                        _viewport.Children.Remove(lines[cntr]);
                    }
                }
            }
        }

        private ScreenSpaceLines3D[] AddLines(RotateTransform3D transform, bool isFromLine)
        {
            ScreenSpaceLines3D[] retVal = new ScreenSpaceLines3D[3];

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                retVal[cntr] = new ScreenSpaceLines3D(true);
                retVal[cntr].Color = isFromLine ? _colors.FromLine : _colors.ToLine;
                retVal[cntr].Thickness = cntr == 1 ? 1d : 2d;
            }

            retVal[0].AddLine(transform.Transform(new Point3D(0, 0, 0)), transform.Transform(new Point3D(LINELENGTH_X, 0, 0)));
            retVal[1].AddLine(transform.Transform(new Point3D(0, -LINELENGTH_Y, 0)), transform.Transform(new Point3D(0, LINELENGTH_Y, 0)));
            retVal[2].AddLine(transform.Transform(new Point3D(0, 0, 0)), transform.Transform(new Point3D(0, 0, LINELENGTH_Z)));

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                _viewport.Children.Add(retVal[cntr]);
            }

            return retVal;
        }
        private ScreenSpaceLines3D[] AddLines(DoubleVector transformedLine, Vector3D rotationAxis)
        {
            ScreenSpaceLines3D[] retVal = new ScreenSpaceLines3D[4];

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                retVal[cntr] = new ScreenSpaceLines3D(true);
                retVal[cntr].Color = _colors.RotatedLine;

                if (cntr == 1)
                {
                    retVal[cntr].Thickness = 3d;		// this one isn't really used
                }
                else if (cntr == 3)
                {
                    retVal[cntr].Thickness = 1d;
                }
                else
                {
                    retVal[cntr].Thickness = 6d;
                }
            }

            retVal[0].AddLine(new Point3D(0, 0, 0), transformedLine.Standard.ToPoint());
            retVal[1].AddLine(new Point3D(0, 0, 0), new Point3D(0, 0, 0));		// just adding this so everything stays lined up
            retVal[2].AddLine(new Point3D(0, 0, 0), transformedLine.Orth.ToPoint());
            retVal[3].AddLine(new Point3D(0, 0, 0), rotationAxis.ToPoint());

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                _viewport.Children.Add(retVal[cntr]);
            }

            return retVal;
        }
        private ScreenSpaceLines3D[] AddLines(Quaternion[] quaternions, Color color)
        {
            ScreenSpaceLines3D[] retVal = new ScreenSpaceLines3D[1];

            retVal[0] = new ScreenSpaceLines3D();
            retVal[0].Color = color;
            retVal[0].Thickness = 3d;

            foreach (Quaternion quat in quaternions)
            {
                retVal[0].AddLine(new Point3D(0, 0, 0), quat.Axis.ToPoint());
            }

            _viewport.Children.Add(retVal[0]);

            return retVal;
        }
        private ScreenSpaceLines3D[] AddLines(IEnumerable<Tuple<Point3D, Point3D>> segments, Color color)
        {
            ScreenSpaceLines3D[] retVal = new ScreenSpaceLines3D[1];

            retVal[0] = new ScreenSpaceLines3D();
            retVal[0].Color = color;
            retVal[0].Thickness = 3d;

            foreach (var segment in segments)
            {
                retVal[0].AddLine(segment.Item1, segment.Item2);
            }

            _viewport.Children.Add(retVal[0]);

            return retVal;
        }

        private Visual3D AddDot(Point3D position, Color color, double radius = .03)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, 4, true);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;
            visual.Transform = new TranslateTransform3D(position.ToVector());

            _viewport.Children.Add(visual);
            return visual;
        }

        private static Quaternion GetDelta(Quaternion from, Quaternion to)
        {
            DoubleVector fromVect = from.GetRotatedVector(new DoubleVector(1, 0, 0, 0, 1, 0));
            DoubleVector toVect = to.GetRotatedVector(new DoubleVector(1, 0, 0, 0, 1, 0));

            return Math3D.GetRotation(fromVect, toVect);
        }

        #endregion
    }
}
