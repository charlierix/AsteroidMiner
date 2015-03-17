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
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers
{
    public partial class ColorVisualizer : Window
    {
        #region Class: VisualTracker

        private class VisualTracker
        {
            public VisualTracker(AxisAngleRotation3D transform)
            {
                this.Transform = transform;

                this.DeltaAngle = StaticRandom.NextDouble(-DELTAANGLE, DELTAANGLE);
            }

            public readonly AxisAngleRotation3D Transform;

            public double DeltaAngle { get; set; }
        }

        #endregion

        #region Declaration Section

        private const double DELTAANGLE = 3d;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private DispatcherTimer _timer;
        private DateTime _lastRotateSwitch = DateTime.Now;

        private MaterialGroup _material = null;

        private List<VisualTracker> _visuals = new List<VisualTracker>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public ColorVisualizer()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Trackball

                // Camera Trackball
                _trackball = new TrackBallRoam(_camera);
                //_trackball.ShouldHitTestOnOrbit = true;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

                #endregion

                //Material
                _material = new MaterialGroup();
                RefreshColors();

                // Visuals
                AddVisual(UtilityWPF.GetCylinder_AlongX(60, 1, 2), new Point3D(-2.5, 2.5, 0));
                AddVisual(UtilityWPF.GetCube_IndependentFaces(new Point3D(-1, -1, -1), new Point3D(1, 1, 1)), new Point3D(2.5, 2.5, 0));

                AddVisual(UtilityWPF.GetSphere_LatLon(20, 1), new Point3D(-2.5, -2.5, 0));

                var hull = Math3D.GetConvexHull(Enumerable.Range(0, 40).Select(o => Math3D.GetRandomVector_Spherical(1.1, 1.3).ToPoint()).ToArray());
                AddVisual(UtilityWPF.GetMeshFromTriangles(hull), new Point3D(2.5, -2.5, 0));
                AddVisual(UtilityWPF.GetMeshFromTriangles_IndependentFaces(hull), new Point3D(2.5, -6, 0));

                // Timer
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(20);
                _timer.Tick += Timer_Tick;
                _timer.Start();

                _isInitialized = true;

                RefreshColors();
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
                if (!_isInitialized)
                {
                    return;
                }

                bool shouldSwitch = false;
                if ((DateTime.Now - _lastRotateSwitch).TotalSeconds > 10)
                {
                    shouldSwitch = true;
                    _lastRotateSwitch = DateTime.Now;
                }

                foreach (VisualTracker tracker in _visuals)
                {
                    if (shouldSwitch)
                    {
                        // Set a new random angle
                        tracker.Transform.Axis = Math3D.GetRandomVector_Spherical(1);
                        tracker.DeltaAngle = StaticRandom.NextDouble(-DELTAANGLE, DELTAANGLE);
                    }

                    // Rotate it
                    double angle = tracker.Transform.Angle;

                    angle += tracker.DeltaAngle;

                    if (angle > 360)
                    {
                        angle -= 360;
                    }
                    else if (angle < 0)
                    {
                        angle += 360;
                    }

                    tracker.Transform.Angle = angle;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                lblError.Text = "";

                RefreshColors();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                lblError.Text = ex.Message;
            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                lblError.Text = "";

                RefreshColors();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                lblError.Text = ex.Message;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                // Show this on the default browser
                System.Diagnostics.Process.Start(e.Uri.OriginalString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder report = new StringBuilder();

                // Simple
                if (chkDiffuse.IsChecked.Value)
                {
                    report.Append("Diffuse\t\t");
                    report.AppendLine(txtDiffuse.Text);
                }

                if (chkSpecular.IsChecked.Value)
                {
                    report.Append("Specular\t");
                    report.Append(txtSpecular.Text);
                    report.Append("\t");
                    report.AppendLine(txtSpecularPower.Text);
                }

                if (chkEmissive.IsChecked.Value)
                {
                    report.Append("Emissive\t");
                    report.AppendLine(txtEmissive.Text);
                }

                report.AppendLine();
                report.AppendLine();
                report.AppendLine();

                // C#
                if (chkDiffuse.IsChecked.Value)
                {
                    report.AppendLine(string.Format("new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(\"{0}\")))", txtDiffuse.Text));
                }

                if (chkSpecular.IsChecked.Value)
                {
                    report.AppendLine(string.Format("new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(\"{0}\")), {1})", txtSpecular.Text, txtSpecularPower.Text));
                }

                if (chkEmissive.IsChecked.Value)
                {
                    report.AppendLine(string.Format("new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(\"{0}\")))", txtEmissive.Text));
                }

                report.AppendLine();
                report.AppendLine();
                report.AppendLine();

                // C# pure
                if (chkDiffuse.IsChecked.Value)
                {
                    report.AppendLine(string.Format("new DiffuseMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(\"#{0}\")))", txtDiffuse.Text));
                }

                if (chkSpecular.IsChecked.Value)
                {
                    report.AppendLine(string.Format("new SpecularMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(\"#{0}\")), {1})", txtSpecular.Text, txtSpecularPower.Text));
                }

                if (chkEmissive.IsChecked.Value)
                {
                    report.AppendLine(string.Format("new EmissiveMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(\"#{0}\")))", txtEmissive.Text));
                }

                report.AppendLine();
                report.AppendLine();
                report.AppendLine();

                // XAML
                report.AppendLine("<GeometryModel3D.Material>");
                report.AppendLine("\t<MaterialGroup>");

                if (chkDiffuse.IsChecked.Value)
                {
                    report.AppendLine(string.Format("\t\t<DiffuseMaterial Color=\"#{0}\"/>", txtDiffuse.Text));
                }

                if (chkSpecular.IsChecked.Value)
                {
                    report.AppendLine(string.Format("\t\t<SpecularMaterial Color=\"#{0}\" SpecularPower=\"{1}\"/>", txtSpecular.Text, txtSpecularPower.Text));
                }

                if (chkEmissive.IsChecked.Value)
                {
                    report.AppendLine(string.Format("\t\t<EmissiveMaterial Color=\"#{0}\"/>", txtEmissive.Text));
                }

                report.AppendLine("\t</MaterialGroup>");
                report.AppendLine("</GeometryModel3D.Material>");

                Clipboard.SetText(report.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void RefreshColors()
        {
            Color color;

            // Background
            color = UtilityWPF.ColorFromHex(txtBackground.Text);
            grdViewPort.Background = new SolidColorBrush(color);

            _material.Children.Clear();

            // Diffuse
            if (chkDiffuse.IsChecked.Value)
            {
                color = UtilityWPF.ColorFromHex(txtDiffuse.Text);
                _material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            }

            // Specular
            if (chkSpecular.IsChecked.Value)
            {
                color = UtilityWPF.ColorFromHex(txtSpecular.Text);
                double power = double.Parse(txtSpecularPower.Text);

                _material.Children.Add(new SpecularMaterial(new SolidColorBrush(color), power));
            }

            // Emissive
            if (chkEmissive.IsChecked.Value)
            {
                color = UtilityWPF.ColorFromHex(txtEmissive.Text);
                _material.Children.Add(new EmissiveMaterial(new SolidColorBrush(color)));
            }
        }

        private void AddVisual(MeshGeometry3D mesh, Point3D position)
        {
            GeometryModel3D model = new GeometryModel3D();

            model.Material = _material;
            model.BackMaterial = _material;

            model.Geometry = mesh;

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            Transform3DGroup transform = new Transform3DGroup();
            AxisAngleRotation3D rotation = new AxisAngleRotation3D(Math3D.GetRandomVector_Spherical(1), StaticRandom.NextDouble(360));
            transform.Children.Add(new RotateTransform3D(rotation));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            visual.Transform = transform;

            _viewport.Children.Add(visual);
            _visuals.Add(new VisualTracker(rotation));
        }

        #endregion
    }
}
