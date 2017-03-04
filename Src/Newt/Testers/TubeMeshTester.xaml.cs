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
using System.Windows.Threading;

using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;

namespace Game.Newt.Testers
{
    public partial class TubeMeshTester : Window
    {
        #region Declaration Section

        private Random _rand = new Random();

        private Visual3D[] _currentVisuals = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<Tuple<string, StackPanel>> _customRows = new List<Tuple<string, StackPanel>>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public TubeMeshTester()
        {
            InitializeComponent();

            // Camera Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewport;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            _isInitialized = true;

            // Make the form look right
            radLight_Checked(this, new RoutedEventArgs());
        }

        #endregion

        #region Event Listeners

        private void radLight_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                _lightContainer.Children.Clear();

                if (radLightStandard.IsChecked.Value)
                {
                    _lightContainer.Children.Add(new DirectionalLight(Colors.White, new Vector3D(1, -1, -1)));
                    _lightContainer.Children.Add(new DirectionalLight(UtilityWPF.ColorFromHex("#303030"), new Vector3D(-1, 1, 1)));
                    _lightContainer.Children.Add(new AmbientLight(Colors.DimGray));
                }
                else if (radLightColored.IsChecked.Value)
                {
                    _lightContainer.Children.Add(new SpotLight(Colors.Green, new Point3D(3, 0, 0), new Vector3D(-1, 0, 0), 120, 90));
                    _lightContainer.Children.Add(new SpotLight(Colors.Orange, new Point3D(-3, 0, 0), new Vector3D(1, 0, 0), 120, 90));

                    _lightContainer.Children.Add(new SpotLight(Colors.Blue, new Point3D(0, 3, 0), new Vector3D(0, -1, 0), 120, 90));
                    _lightContainer.Children.Add(new SpotLight(Colors.Red, new Point3D(0, -3, 0), new Vector3D(0, 1, 0), 120, 90));

                    _lightContainer.Children.Add(new SpotLight(Colors.Purple, new Point3D(0, 0, 3), new Vector3D(0, 0, -1), 120, 90));
                    _lightContainer.Children.Add(new SpotLight(Colors.Yellow, new Point3D(0, 0, -3), new Vector3D(0, 0, 1), 120, 90));
                }
                else
                {
                    throw new ApplicationException("Unknown light type");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnTestModel_Click(object sender, RoutedEventArgs e)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.DodgerBlue)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.Navy), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = GetMineralGeometry1(1, 1, 1, .66);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;

            // Store it
            SetCurrentVisual(visual);
        }
        private void btnCube_Click(object sender, RoutedEventArgs e)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.Beige)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.Chocolate), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = GetCube(1);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;

            // Store it
            SetCurrentVisual(visual);
        }
        private void btnCubeWithNormals_Click(object sender, RoutedEventArgs e)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.Beige)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.Chocolate), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = GetCubeNormals(1);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;

            // Store it
            SetCurrentVisual(visual);
        }

        private void btnDissectMessage_Click(object sender, RoutedEventArgs e)
        {
            int numSides = _rand.Next(5, 11);

            StringBuilder report = new StringBuilder();

            int lowerIndex = 2;
            int upperIndex = numSides - 1;
            int lastUsedIndex = 0;
            bool shouldBumpLower = true;

            report.AppendLine("0,1,2");

            while (lowerIndex < upperIndex)
            {
                report.Append(lowerIndex.ToString());
                report.Append(",");
                report.Append(upperIndex.ToString());
                report.Append(",");
                report.Append(lastUsedIndex.ToString());
                report.AppendLine();

                if (shouldBumpLower)
                {
                    lastUsedIndex = lowerIndex;
                    lowerIndex++;
                }
                else
                {
                    lastUsedIndex = upperIndex;
                    upperIndex--;
                }
                shouldBumpLower = !shouldBumpLower;
            }



            MessageBox.Show(report.ToString());



        }

        private void btnSinglePolygon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numSides;
                if (!int.TryParse(txtNumSides.Text, out numSides))
                {
                    throw new ApplicationException("Couldn't parse number of sides");
                }

                List<TubeRingDefinition_ORIG> rings = new List<TubeRingDefinition_ORIG>();
                rings.Add(new TubeRingDefinition_ORIG(1d, 1d, 0d, true, false));

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.HotPink)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.Teal), 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(numSides, rings, chkSoftSides.IsChecked.Value, true);

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                // Store it
                SetCurrentVisual(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolygonAndPoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numSides;
                if (!int.TryParse(txtNumSides.Text, out numSides))
                {
                    throw new ApplicationException("Couldn't parse number of sides");
                }

                List<TubeRingDefinition_ORIG> rings = new List<TubeRingDefinition_ORIG>();
                rings.Add(new TubeRingDefinition_ORIG(1d, 1d, 0d, true, false));
                rings.Add(new TubeRingDefinition_ORIG(1d, false));

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.Teal)));
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 64, 192, 192))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.Orange), 100d));
                //materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(32, 255, 192, 128))));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(numSides, rings, chkSoftSides.IsChecked.Value, true);

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                // Store it
                SetCurrentVisual(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPointAndPolygon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numSides;
                if (!int.TryParse(txtNumSides.Text, out numSides))
                {
                    throw new ApplicationException("Couldn't parse number of sides");
                }

                List<TubeRingDefinition_ORIG> rings = new List<TubeRingDefinition_ORIG>();
                rings.Add(new TubeRingDefinition_ORIG(0d, false));
                rings.Add(new TubeRingDefinition_ORIG(1d, 1d, 1d, true, false));

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.Silver)));
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 64, 192, 192))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.Tomato), 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(numSides, rings, chkSoftSides.IsChecked.Value, true);

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                // Store it
                SetCurrentVisual(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolygonAndPolygon_Click(object sender, RoutedEventArgs e)
        {
            double RADICAL2 = Math.Sqrt(2d);

            try
            {
                int numSides;
                if (!int.TryParse(txtNumSides.Text, out numSides))
                {
                    throw new ApplicationException("Couldn't parse number of sides");
                }

                List<TubeRingDefinition_ORIG> rings = new List<TubeRingDefinition_ORIG>();
                rings.Add(new TubeRingDefinition_ORIG(RADICAL2, RADICAL2, 1d, true, false));
                rings.Add(new TubeRingDefinition_ORIG(RADICAL2, RADICAL2, 1d, true, false));

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 157, 145, 181))));
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 64, 192, 192))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.Green), 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(numSides, rings, chkSoftSides.IsChecked.Value, true);

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                // Store it
                SetCurrentVisual(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnJewel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numSides;
                if (!int.TryParse(txtNumSides.Text, out numSides))
                {
                    throw new ApplicationException("Couldn't parse number of sides");
                }

                List<TubeRingDefinition_ORIG> rings = new List<TubeRingDefinition_ORIG>();
                //rings.Add(new TubeRingDefinition(0));
                rings.Add(new TubeRingDefinition_ORIG(.001, .001, 0, true, false));       //NOTE: If the object is going to be semitransparent, then don't use the pyramid overload.  I think it's because the normal points down, and lighting isn't the way you'd expect
                rings.Add(new TubeRingDefinition_ORIG(1.5, 1.5, .8d, true, false));
                rings.Add(new TubeRingDefinition_ORIG(.75, .75, .25d, true, false));

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(192, 69, 128, 64))));
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(10, 69, 128, 64))));
                //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 69, 128, 64))));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 26, 82, 20)), 100d));
                materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(32, 64, 128, 0))));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(numSides, rings, chkSoftSides.IsChecked.Value, true);

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                // Store it
                SetCurrentVisual(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCustomInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pnlCustomError.Visibility = Visibility.Collapsed;

                int insertAt = int.Parse(txtCustomInsertIndex.Text) - 1;
                StackPanel panel = new StackPanel();
                panel.Orientation = Orientation.Horizontal;

                //TODO: This is very hardcoded and ugly.  Use MVVM properly

                panel.Children.Add(new TextBlock() { FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center });

                panel.Children.Add(new TextBlock() { Text = "dist", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                panel.Children.Add(new TextBox() { Text = "1", VerticalAlignment = VerticalAlignment.Center });

                switch (cboCustomType.Text)
                {
                    case "Poly":
                        panel.Children.Add(new TextBlock() { Text = "radiusX", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                        panel.Children.Add(new TextBox() { Text = "1", VerticalAlignment = VerticalAlignment.Center });

                        panel.Children.Add(new TextBlock() { Text = "radiusY", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                        panel.Children.Add(new TextBox() { Text = "1", VerticalAlignment = VerticalAlignment.Center });

                        panel.Children.Add(new CheckBox() { Content = "closed if endcap", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                        break;

                    case "Point":
                        break;

                    case "Dome":
                        panel.Children.Add(new TextBlock() { Text = "num seg phi", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                        panel.Children.Add(new TextBox() { Text = "3", VerticalAlignment = VerticalAlignment.Center });
                        break;

                    default:
                        throw new ApplicationException("Unknown type: " + cboCustomType.Text);
                }

                _customRows.Insert(insertAt, Tuple.Create(cboCustomType.Text, panel));
                pnlCustomEntries.Children.Insert(insertAt, panel);

                RebuildCustom();
            }
            catch (Exception ex)
            {
                pnlCustomError.Visibility = Visibility.Visible;
                lblCustomError.Text = ex.Message;
            }
        }
        private void btnCustomDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pnlCustomError.Visibility = Visibility.Collapsed;

                int removeAt = int.Parse(txtCustomDeleteIndex.Text) - 1;

                if (removeAt < 0 || removeAt >= _customRows.Count)
                {
                    throw new ApplicationException("Invalid index");
                }

                _customRows.RemoveAt(removeAt);
                pnlCustomEntries.Children.RemoveAt(removeAt);

                RebuildCustom();        //this has its own try block
            }
            catch (Exception ex)
            {
                pnlCustomError.Visibility = Visibility.Visible;
                lblCustomError.Text = ex.Message;
            }
        }
        private void ClearCustom_Click(object sender, RoutedEventArgs e)
        {
            pnlCustomEntries.Children.Clear();
            _customRows.Clear();
            RebuildCustom();        //this has its own try block
        }
        private void RefreshCustom_Click(object sender, RoutedEventArgs e)
        {
            RebuildCustom();        //this has its own try block
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

        #endregion

        #region Private Methods

        private void SetCurrentVisual(Visual3D visual)
        {
            if (_currentVisuals != null)
            {
                _viewport.Children.RemoveAll(_currentVisuals);
                _currentVisuals = null;
            }

            List<Visual3D> visuals = new List<Visual3D>();

            if (chkShowNormals.IsChecked.Value)
            {
                #region Show normals

                ModelVisual3D visualCast = visual as ModelVisual3D;
                if (visualCast != null)
                {
                    GeometryModel3D modelCast = visualCast.Content as GeometryModel3D;
                    if (modelCast != null)
                    {
                        MeshGeometry3D geometryCast = modelCast.Geometry as MeshGeometry3D;
                        if (geometryCast != null)
                        {
                            // Mesh point normals
                            if (geometryCast.Positions.Count == geometryCast.Normals.Count)
                            {
                                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                                lines.Color = Colors.HotPink;
                                lines.Thickness = 1d;

                                for (int cntr = 0; cntr < geometryCast.Positions.Count; cntr++)
                                {
                                    lines.AddLine(geometryCast.Positions[cntr], geometryCast.Positions[cntr] + geometryCast.Normals[cntr]);
                                }

                                visuals.Add(lines);
                            }

                            // Triangle normals
                            bool hadTriangle = false;

                            ScreenSpaceLines3D lines2 = new ScreenSpaceLines3D();
                            lines2.Color = Colors.Gold;
                            lines2.Thickness = 1d;

                            foreach (ITriangle triangle in UtilityWPF.GetTrianglesFromMesh(geometryCast))
                            {
                                hadTriangle = true;
                                lines2.AddLine(triangle.GetCenterPoint(), triangle.GetCenterPoint() + triangle.NormalUnit);
                            }

                            if (hadTriangle)
                            {
                                visuals.Add(lines2);
                            }
                        }
                    }
                }

                #endregion
            }

            visuals.Add(visual);

            _currentVisuals = visuals.ToArray();
            _viewport.Children.AddRange(_currentVisuals);
        }

        /// <summary>
        /// This creates a 3D double trapazoid
        /// </summary>
        /// <param name="xSize">Width of the base</param>
        /// <param name="ySize">Height of the base</param>
        /// <param name="zSize">This is the depth of the trapazoid</param>
        /// <param name="percentShrink">What percent of the base the tops of the trapazoids should be (0 will be a point, 1 will be the same size as the base)</param>
        private static MeshGeometry3D GetMineralGeometry1(double xSize, double ySize, double zSize, double percentShrink)
        {
            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            double halfXSize = xSize * .5d;
            double halfSmallXSize = halfXSize * percentShrink;
            double halfYSize = ySize * .5d;
            double halfSmallYSize = halfYSize * percentShrink;
            double halfZSize = zSize / 2d;

            // Top points
            retVal.Positions.Add(new Point3D(-halfSmallXSize, -halfSmallYSize, halfZSize));		// 0
            retVal.Positions.Add(new Point3D(-halfSmallXSize, halfSmallYSize, halfZSize));		// 1
            retVal.Positions.Add(new Point3D(halfSmallXSize, halfSmallYSize, halfZSize));		// 2
            retVal.Positions.Add(new Point3D(halfSmallXSize, -halfSmallYSize, halfZSize));		// 3

            // Base points
            retVal.Positions.Add(new Point3D(-halfXSize, -halfYSize, 0));		// 4
            retVal.Positions.Add(new Point3D(-halfXSize, halfYSize, 0));		// 5
            retVal.Positions.Add(new Point3D(halfXSize, halfYSize, 0));		// 6
            retVal.Positions.Add(new Point3D(halfXSize, -halfYSize, 0));		// 7

            // Bottom points
            retVal.Positions.Add(new Point3D(-halfSmallXSize, -halfSmallYSize, -halfZSize));		// 8
            retVal.Positions.Add(new Point3D(-halfSmallXSize, halfSmallYSize, -halfZSize));		// 9
            retVal.Positions.Add(new Point3D(halfSmallXSize, halfSmallYSize, -halfZSize));		// 10
            retVal.Positions.Add(new Point3D(halfSmallXSize, -halfSmallYSize, -halfZSize));		// 11

            // Top Face
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(0);

            // Top Front Face
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(7);

            // Top Right Face
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(6);

            // Top Rear Face
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(5);

            // Top Left Face
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(0);

            // Bottom Front Face
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(8);
            retVal.TriangleIndices.Add(11);
            retVal.TriangleIndices.Add(11);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(4);

            // Bottom Right Face
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(11);
            retVal.TriangleIndices.Add(10);
            retVal.TriangleIndices.Add(10);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(7);

            // Bottom Rear Face
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(10);
            retVal.TriangleIndices.Add(9);
            retVal.TriangleIndices.Add(9);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(6);

            // Bottom Left Face
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(9);
            retVal.TriangleIndices.Add(8);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(9);

            // Bottom Face
            retVal.TriangleIndices.Add(8);
            retVal.TriangleIndices.Add(9);
            retVal.TriangleIndices.Add(10);
            retVal.TriangleIndices.Add(9);
            retVal.TriangleIndices.Add(10);
            retVal.TriangleIndices.Add(11);

            retVal.TriangleIndices.Add(8);        // This last triangle was missing.  Not sure why
            retVal.TriangleIndices.Add(10);
            retVal.TriangleIndices.Add(11);

            // shouldn't I set normals?
            //retVal.Normals

            // Exit Function
            //retVal.Freeze();
            return retVal;
        }

        private static MeshGeometry3D GetCube(double size)
        {
            double halfSize = size / 2d;

            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            retVal.Positions.Add(new Point3D(-halfSize, -halfSize, halfSize));  // 0
            retVal.Positions.Add(new Point3D(halfSize, -halfSize, halfSize));       // 1
            retVal.Positions.Add(new Point3D(halfSize, halfSize, halfSize));        // 2
            retVal.Positions.Add(new Point3D(-halfSize, halfSize, halfSize));       // 3

            retVal.Positions.Add(new Point3D(-halfSize, -halfSize, -halfSize));        // 4
            retVal.Positions.Add(new Point3D(halfSize, -halfSize, -halfSize));  // 5
            retVal.Positions.Add(new Point3D(halfSize, halfSize, -halfSize));       // 6
            retVal.Positions.Add(new Point3D(-halfSize, halfSize, -halfSize));  // 7

            // Front face
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(0);

            // Back face
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(6);

            // Right face
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(2);

            // Top face
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(7);

            // Bottom face
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(5);

            // Right face
            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(4);

            // shouldn't I set normals?
            //retVal.Normals

            // Exit Function
            //retVal.Freeze();
            return retVal;
        }
        private static MeshGeometry3D GetCubeNormals(double size)
        {
            double halfSize = size / 2d;

            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            //retVal.Positions.Add(new Point3D(-halfSize, -halfSize, halfSize));  // 0
            //retVal.Positions.Add(new Point3D(halfSize, -halfSize, halfSize));       // 1
            //retVal.Positions.Add(new Point3D(halfSize, halfSize, halfSize));        // 2
            //retVal.Positions.Add(new Point3D(-halfSize, halfSize, halfSize));       // 3

            //retVal.Positions.Add(new Point3D(-halfSize, -halfSize, -halfSize));        // 4
            //retVal.Positions.Add(new Point3D(halfSize, -halfSize, -halfSize));  // 5
            //retVal.Positions.Add(new Point3D(halfSize, halfSize, -halfSize));       // 6
            //retVal.Positions.Add(new Point3D(-halfSize, halfSize, -halfSize));  // 7

            Vector3D normal;

            #region Front face

            retVal.Positions.Add(new Point3D(-halfSize, -halfSize, halfSize));  // 0
            retVal.Positions.Add(new Point3D(halfSize, -halfSize, halfSize));       // 1
            retVal.Positions.Add(new Point3D(halfSize, halfSize, halfSize));        // 2
            retVal.Positions.Add(new Point3D(-halfSize, halfSize, halfSize));       // 3

            normal = new Vector3D(0, 0, 1);
            retVal.Normals.Add(normal);
            retVal.Normals.Add(normal);
            retVal.Normals.Add(normal);
            retVal.Normals.Add(normal);

            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(2);

            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(0);

            #endregion

            #region Back face

            retVal.Positions.Add(new Point3D(-halfSize, -halfSize, -halfSize));        // 4
            retVal.Positions.Add(new Point3D(halfSize, -halfSize, -halfSize));  // 5
            retVal.Positions.Add(new Point3D(halfSize, halfSize, -halfSize));       // 6
            retVal.Positions.Add(new Point3D(-halfSize, halfSize, -halfSize));  // 7

            normal = new Vector3D(0, 0, -1);
            retVal.Normals.Add(normal);
            retVal.Normals.Add(normal);
            retVal.Normals.Add(normal);
            retVal.Normals.Add(normal);

            retVal.TriangleIndices.Add(6);
            retVal.TriangleIndices.Add(5);
            retVal.TriangleIndices.Add(4);

            retVal.TriangleIndices.Add(4);
            retVal.TriangleIndices.Add(7);
            retVal.TriangleIndices.Add(6);

            #endregion

            //#region Right face

            //retVal.TriangleIndices.Add(1);
            //retVal.TriangleIndices.Add(5);
            //retVal.TriangleIndices.Add(2);
            //retVal.TriangleIndices.Add(5);
            //retVal.TriangleIndices.Add(6);
            //retVal.TriangleIndices.Add(2);

            //#endregion

            //#region Top face

            //retVal.TriangleIndices.Add(2);
            //retVal.TriangleIndices.Add(6);
            //retVal.TriangleIndices.Add(3);
            //retVal.TriangleIndices.Add(3);
            //retVal.TriangleIndices.Add(6);
            //retVal.TriangleIndices.Add(7);

            //#endregion

            //#region Bottom face

            //retVal.TriangleIndices.Add(5);
            //retVal.TriangleIndices.Add(1);
            //retVal.TriangleIndices.Add(0);
            //retVal.TriangleIndices.Add(0);
            //retVal.TriangleIndices.Add(4);
            //retVal.TriangleIndices.Add(5);

            //#endregion

            //#region Right face

            //retVal.TriangleIndices.Add(4);
            //retVal.TriangleIndices.Add(0);
            //retVal.TriangleIndices.Add(3);
            //retVal.TriangleIndices.Add(3);
            //retVal.TriangleIndices.Add(7);
            //retVal.TriangleIndices.Add(4);

            //#endregion

            // Exit Function
            //retVal.Freeze();
            return retVal;
        }

        private void RebuildCustom()
        {
            try
            {
                pnlCustomError.Visibility = Visibility.Collapsed;

                #region Fix the row numbers

                for (int cntr = 0; cntr < _customRows.Count; cntr++)
                {
                    ((TextBlock)_customRows[cntr].Item2.Children[0]).Text = (cntr + 1).ToString();
                }

                txtCustomInsertIndex.Text = (_customRows.Count + 1).ToString();

                #endregion

                List<TubeRingBase> rings = new List<TubeRingBase>();

                #region Parse the rows

                for (int cntr = 0; cntr < _customRows.Count; cntr++)
                {
                    StackPanel panel = _customRows[cntr].Item2;

                    // Distance
                    double distance = double.Parse(((TextBox)panel.Children[2]).Text);

                    switch (_customRows[cntr].Item1)
                    {
                        case "Poly":
                            TubeRingRegularPolygon poly = new TubeRingRegularPolygon(distance, false,
                                double.Parse(((TextBox)panel.Children[4]).Text),
                                double.Parse(((TextBox)panel.Children[6]).Text),
                                ((CheckBox)panel.Children[7]).IsChecked.Value);

                            rings.Add(poly);
                            break;

                        case "Point":
                            TubeRingPoint point = new TubeRingPoint(distance, false);

                            rings.Add(point);
                            break;

                        case "Dome":
                            TubeRingDome dome = new TubeRingDome(distance, false,
                                int.Parse(((TextBox)panel.Children[4]).Text));

                            rings.Add(dome);
                            break;

                        default:
                            throw new ApplicationException("Unknown type: " + _customRows[cntr].Item1);
                    }
                }

                #endregion

                MeshGeometry3D mesh = UtilityWPF.GetMultiRingedTube(int.Parse(txtNumSides.Text), rings, chkSoftSides.IsChecked.Value, true);

                // Material
                MaterialGroup material = new MaterialGroup();

                Color color = UtilityWPF.ColorFromHex(txtDiffuse.Text);
                material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));

                if (chkSpecular.IsChecked.Value)
                {
                    color = UtilityWPF.ColorFromHex(txtSpecular.Text);
                    double power = double.Parse(txtSpecularPower.Text);

                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(color), power));
                }

                if (chkEmissive.IsChecked.Value)
                {
                    color = UtilityWPF.ColorFromHex(txtEmissive.Text);
                    material.Children.Add(new EmissiveMaterial(new SolidColorBrush(color)));
                }

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = mesh;

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                // Store it
                SetCurrentVisual(visual);
            }
            catch (Exception ex)
            {
                pnlCustomError.Visibility = Visibility.Visible;
                lblCustomError.Text = ex.Message;
            }
        }

        #endregion
    }
}
