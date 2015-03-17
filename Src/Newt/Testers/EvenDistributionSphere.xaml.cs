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
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;

namespace Game.Newt.Testers
{
    public partial class EvenDistributionSphere : Window
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

            public Dot(bool isStatic, Point3D position)
            {
                this.IsStatic = isStatic;
                _position = position;

                ModelVisual3D model = BuildDot(isStatic);
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

            public Visual3D Visual
            {
                get;
                private set;
            }

            #endregion

            #region Private Methods

            private static ModelVisual3D BuildDot(bool isStatic)
            {
                Color color = UtilityWPF.ColorFromHex(isStatic ? DOTCOLOR_STATIC : DOTCOLOR);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, DOTRADIUS, DOTRADIUS, DOTRADIUS);

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

        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public EvenDistributionSphere()
        {
            InitializeComponent();

            _isInitialized = true;

            UpdateCalcDistLabel();
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
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void CalcDist_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            UpdateCalcDistLabel();
            UpdateReport();
        }

        private void btnAddDots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius;
                if (!double.TryParse(txtRadius.Text, out radius))
                {
                    MessageBox.Show("Couldn't parse radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int count;
                if (!int.TryParse(txtNumDots.Text, out count))
                {
                    MessageBox.Show("Couldn't parse the number of dots as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ClearDebugVisuals();

                for (int cntr = 0; cntr < count; cntr++)
                {
                    Dot dot = new Dot(false, Math3D.GetRandomVector_Spherical(radius).ToPoint());

                    _dots.Add(dot);
                    _viewport.Children.Add(dot.Visual);
                }

                UpdateCalcDistLabel();
                UpdateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAddStaticDots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius;
                if (!double.TryParse(txtRadius.Text, out radius))
                {
                    MessageBox.Show("Couldn't parse radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int count;
                if (!int.TryParse(txtNumDots.Text, out count))
                {
                    MessageBox.Show("Couldn't parse the number of dots as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ClearDebugVisuals();

                for (int cntr = 0; cntr < count; cntr++)
                {
                    Dot dot = new Dot(true, Math3D.GetRandomVector_Spherical(radius).ToPoint());

                    _dots.Add(dot);
                    _viewport.Children.Add(dot.Visual);
                }

                UpdateCalcDistLabel();
                UpdateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCalculateForces_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius;
                if (!double.TryParse(txtRadius.Text, out radius))
                {
                    MessageBox.Show("Couldn't parse radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double calcDist;
                if (chkCalcDist.IsChecked.Value)
                {
                    if (!double.TryParse(txtCalcDist.Text, out calcDist))
                    {
                        MessageBox.Show("Couldn't parse calculation distance as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    calcDist = GetCalcDistance(GetApproximateCount(_dots, radius), radius);
                }

                ClearDebugVisuals();

                if ((chkInteriorPoints.IsChecked.Value && chkInwardForce.IsChecked.Value) || chkRepulsiveForce.IsChecked.Value)
                {
                    Vector3D[] forces = new Vector3D[_dots.Count];

                    if (chkInteriorPoints.IsChecked.Value && chkInwardForce.IsChecked.Value)
                    {
                        // Inward Force
                        GetInwardForces(forces, _dots);
                    }

                    if (chkRepulsiveForce.IsChecked.Value)
                    {
                        // Repulsion Force
                        GetRepulsionForces(forces, _dots, calcDist);
                    }

                    #region Show Lines

                    ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                    lines.Thickness = 1d;
                    lines.Color = UtilityWPF.ColorFromHex("A8A8A8");
                    for (int cntr = 0; cntr < _dots.Count; cntr++)
                    {
                        lines.AddLine(_dots[cntr].Position, _dots[cntr].Position + forces[cntr]);
                    }

                    _debugVisuals.Add(lines);
                    _viewport.Children.Add(lines);

                    #endregion
                }

                if (chkShowNearest.IsChecked.Value)
                {
                    #region Show nearest neighbors
                    #endregion
                }

                UpdateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnMovePoints_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius;
                if (!double.TryParse(txtRadius.Text, out radius))
                {
                    MessageBox.Show("Couldn't parse radius as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double percent;
                if (!double.TryParse(txtMovePercent.Text, out percent))
                {
                    MessageBox.Show("Couldn't parse percent as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                percent /= 100d;

                double calcDist;
                if (chkCalcDist.IsChecked.Value)
                {
                    if (!double.TryParse(txtCalcDist.Text, out calcDist))
                    {
                        MessageBox.Show("Couldn't parse calculation distance as a double", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    calcDist = GetCalcDistance(GetApproximateCount(_dots, radius), radius);
                }

                ClearDebugVisuals();


                if (!chkInteriorPoints.IsChecked.Value)
                {
                    // Snap them to the surface of the sphere
                    SnapToSphere(_dots, radius);
                }



                Vector3D[] forces = new Vector3D[_dots.Count];
                if (chkInteriorPoints.IsChecked.Value && chkInwardForce.IsChecked.Value)
                {
                    // Inward Force
                    GetInwardForces(forces, _dots);
                }

                if (chkRepulsiveForce.IsChecked.Value)
                {
                    // Repulsion Force
                    GetRepulsionForces(forces, _dots, calcDist);
                }

                // Move the points
                for (int cntr = 0; cntr < _dots.Count; cntr++)
                {
                    if (!_dots[cntr].IsStatic)
                    {
                        _dots[cntr].Position += forces[cntr] * percent;
                    }
                }



                if (!chkInteriorPoints.IsChecked.Value)
                {
                    // Now that they've moved, snap them back to the surface of the sphere
                    SnapToSphere(_dots, radius);
                }




                if (chkShowForcesAfterMove.IsChecked.Value)
                {
                    btnCalculateForces_Click(this, new RoutedEventArgs());
                }

                if (chkShowRadius.IsChecked.Value)
                {
                    #region Show Radius

                    double maxRadius = _dots.Where(o => !o.IsStatic).Max(o => o.Position.ToVector().Length);
                    Visual3D visual = null;

                    if (Math3D.IsNearValue(radius, maxRadius))
                    {
                        // Draw one green one
                        visual = GetSphereVisual(radius, UtilityWPF.ColorFromHex("1000FF00"));
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                    else if (maxRadius < radius)
                    {
                        // Small than desired radius
                        visual = GetSphereVisual(maxRadius, UtilityWPF.ColorFromHex("10FF0000"));
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);

                        visual = GetSphereVisual(radius, UtilityWPF.ColorFromHex("10000000"));
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                    else
                    {
                        // Larger than desired radius
                        visual = GetSphereVisual(radius, UtilityWPF.ColorFromHex("10000000"));
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);

                        visual = GetSphereVisual(maxRadius, UtilityWPF.ColorFromHex("10FF0000"));
                        _debugVisuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }

                    #endregion
                }

                UpdateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClearDots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearDebugVisuals();

                foreach (Dot dot in _dots)
                {
                    _viewport.Children.Remove(dot.Visual);
                }

                _dots.Clear();

                UpdateCalcDistLabel();
                UpdateReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClearDebugVisuals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearDebugVisuals();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearDebugVisuals()
        {
            foreach (Visual3D visual in _debugVisuals)
            {
                _viewport.Children.Remove(visual);
            }

            _debugVisuals.Clear();
        }

        private void UpdateCalcDistLabel()
        {
            try
            {
                double radius;
                if (double.TryParse(txtRadius.Text, out radius))
                {
                    double calcDist = GetCalcDistance(GetApproximateCount(_dots, radius), radius);

                    lblCalcDistCalculated.Text = Math.Round(calcDist, 3).ToString();
                }
                else
                {
                    lblCalcDistCalculated.Text = "exception";
                }
            }
            catch (Exception)
            {
                lblCalcDistCalculated.Text = "exception";
            }
        }
        private void UpdateReport()
        {
            // Total Dots
            lblTotalDots.Text = _dots.Count.ToString();

            // Figure out how many dots are used for calling GetCalcDistance
            double radius;
            if (double.TryParse(txtRadius.Text, out radius))
            {
                lblApproxDots.Text = Math.Round(GetApproximateCount(_dots, radius), 3).ToString();
            }
            else
            {
                lblApproxDots.Text = "error";
            }

            // Radius
            if (_dots.Count == 0)
            {
                lblActualRadius.Text = "0";
            }
            else
            {
                var nonStatic = _dots.Where(o => !o.IsStatic).ToArray();
                if (nonStatic.Length == 0)
                {
                    lblActualRadius.Text = "0";
                }
                else
                {
                    lblActualRadius.Text = Math.Round(nonStatic.Max(o => o.Position.ToVector().Length), 3).ToString();
                }
            }
        }

        private static void GetInwardForces(Vector3D[] forces, List<Dot> dots)
        {
            const double INWARDFORCE = 1d;

            for (int cntr = 0; cntr < dots.Count; cntr++)
            {
                if (dots[cntr].IsStatic)
                {
                    continue;
                }

                Vector3D direction = dots[cntr].Position.ToVector();

                double length = direction.Length;
                double power = 1d;

                direction = direction.ToUnit() * (Math.Pow(length, power) * INWARDFORCE * -1d);

                forces[cntr] += direction;
            }
        }

        private static double GetApproximateCount(List<Dot> dots, double radius)
        {
            if (dots.All(o => !o.IsStatic))
            {
                return dots.Count;
            }

            double[] rads = dots.Select(o => o.Position.ToVector().Length).ToArray();
            double maxRadius = radius * .15d;
            double retVal = 0d;

            for (int cntr = 0; cntr < dots.Count; cntr++)
            {
                if (!dots[cntr].IsStatic)
                {
                    // The movable dots are always counted
                    retVal += 1d;
                }
                else if (rads[cntr] <= radius)
                {
                    // Fully include everything that's <= radius
                    retVal += 1d;
                }
                else
                {
                    // Scale out the ones that are > radius
                    double percent = (rads[cntr] - radius) / maxRadius;
                    if (percent < 1d && percent > 0d)
                    {
                        retVal += percent;
                    }
                }
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// I used this website to curve fit:
        /// http://zunzun.com/
        /// </summary>
        private static double GetCalcDistance(double count, double radius)
        {
            //TODO: These values are good for count <= 20.  Need different values for a larger count

            const double A = .013871652011048237d;
            const double B = .50644756797276580d;
            const double C = -.15228176942968946d;
            const double D = .026504677205836713d;

            if (count <= 1)
            {
                return 0d;
            }

            double ln = Math.Log(count, Math.E);

            double resultRadius = A + B * ln + C * Math.Pow(ln, 2d) + D * Math.Pow(ln, 3d);

            return radius / resultRadius;
        }

        private static void GetRepulsionForces(Vector3D[] forces, List<Dot> dots, double maxDist)
        {
            const double STRENGTH = 1d;

            for (int outer = 0; outer < dots.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < dots.Count; inner++)
                {
                    Vector3D link = dots[outer].Position - dots[inner].Position;

                    // Force should be max when distance is zero, and linearly drop off to nothing
                    double inverseDistance = maxDist - link.Length;

                    double force = 0d;
                    if (inverseDistance > 0d)
                    {
                        force = STRENGTH * inverseDistance;
                    }

                    if (!double.IsNaN(force) && !double.IsInfinity(force))
                    {
                        link.Normalize();
                        link *= force;

                        if (!dots[inner].IsStatic)
                        {
                            forces[inner] -= link;
                        }

                        if (!dots[outer].IsStatic)
                        {
                            forces[outer] += link;
                        }
                    }
                }
            }
        }

        private static Visual3D GetSphereVisual(double radius, Color color)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(20, radius, radius, radius);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            return retVal;
        }

        private static void SnapToSphere(List<Dot> dots, double radius)
        {
            foreach (Dot dot in dots)
            {
                if (!dot.IsStatic)
                {
                    dot.Position = (dot.Position.ToVector().ToUnit() * radius).ToPoint();
                }
            }
        }

        #endregion
    }
}
