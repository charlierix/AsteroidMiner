using System;
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

using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Primitives3D;

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
                _lineFrom = BuildLines(_orientationTrackballFrom.Transform, true);
                _lineTo = BuildLines(_orientationTrackballTo.Transform, false);

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
            // Wipe the transformed lines until they click the button
            ClearLines(_lineTransformed);
            _lineTransformed = null;

            // Rebuild the from lines
            ClearLines(_lineFrom);
            _lineFrom = BuildLines(_orientationTrackballFrom.Transform, true);
        }
        private void OrientationTrackballTo_RotationChanged(object sender, EventArgs e)
        {
            // Wipe the transformed lines until they click the button
            ClearLines(_lineTransformed);
            _lineTransformed = null;

            // Rebuild the to lines
            ClearLines(_lineTo);
            _lineTo = BuildLines(_orientationTrackballTo.Transform, false);
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
            ClearLines(_lineTransformed);
            _lineTransformed = BuildLines(rotated, axis);
        }
        private void btnGetRotation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Quaternion from = _orientationTrackballFrom.Transform.ToQuaternion();
                Quaternion to = _orientationTrackballTo.Transform.ToQuaternion();
                Quaternion delta1 = Math3D.GetRotation(from, to);
                //Quaternion delta = Quaternion.Slerp(from, to, 1, true);        // this does the rotation for me, but doesn't tell how to rotate
                Quaternion delta2 = GetDelta(from, to);

                pnlReport.Children.Clear();
                ClearLines(_lineFrom);
                ClearLines(_lineTo);
                ClearLines(_lineTransformed);

                _lineFrom = BuildLines(new [] { from }, _colors.FromLine);
                _lineTo = BuildLines(new [] { to }, _colors.ToLine);
                _lineTransformed = BuildLines(new [] { delta1, delta2 }, _colors.RotatedLine);

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

        private ScreenSpaceLines3D[] BuildLines(RotateTransform3D transform, bool isFromLine)
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
        private ScreenSpaceLines3D[] BuildLines(DoubleVector transformedLine, Vector3D rotationAxis)
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
        private ScreenSpaceLines3D[] BuildLines(Quaternion[] quaternions, Color color)
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

        private static Quaternion GetDelta(Quaternion from, Quaternion to)
        {
            DoubleVector fromVect = from.GetRotatedVector(new DoubleVector(1, 0, 0, 0, 1, 0));
            DoubleVector toVect = to.GetRotatedVector(new DoubleVector(1, 0, 0, 0, 1, 0));

            return Math3D.GetRotation(fromVect, toVect);
        }

        #endregion
    }
}
