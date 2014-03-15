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

namespace Game.Newt.Testers
{
    /// <summary>
    /// Interaction logic for TransformTester.xaml
    /// </summary>
    public partial class TransformTester : Window
    {
        #region Declaration Section

        //TODO:  Replace this with the final trackball
        private CameraTester.TrackBallRoam_local _trackball = null;

        /// <summary>
        /// These are the cubes that show the axiis
        /// </summary>
        private List<ModelVisual3D> _axisCubes = new List<ModelVisual3D>();

        /// <summary>
        /// There should only be one set of test cubes at a time
        /// </summary>
        private List<ModelVisual3D> _testCubes = new List<ModelVisual3D>();

        #endregion

        #region Constructor

        public TransformTester()
        {
            InitializeComponent();

            #region Make cubes

            CreateCube(_axisCubes, Colors.White, .25d, new Point3D());

            CreateCube(_axisCubes, Colors.Pink, .25d, new Point3D(-10, 0, 0));
            CreateCube(_axisCubes, Colors.Red, .25d, new Point3D(10, 0, 0));

            CreateCube(_axisCubes, Colors.Chartreuse, .25d, new Point3D(0, -10, 0));
            CreateCube(_axisCubes, Colors.Green, .25d, new Point3D(0, 10, 0));

            CreateCube(_axisCubes, Colors.PowderBlue, .25d, new Point3D(0, 0, -10));
            CreateCube(_axisCubes, Colors.Blue, .25d, new Point3D(0, 0, 10));

            #endregion

            // Trackball
            _trackball = new CameraTester.TrackBallRoam_local(_camera);
            _trackball.EventSource = grdViewPort;

            this.Background = SystemColors.ControlBrush;
        }

        #endregion

        #region Event Listeners

        private void btnResetCamera_Click(object sender, RoutedEventArgs e)
        {
            _camera.Position = new Point3D(0, 0, 25);
            _camera.LookDirection = new Vector3D(0, 0, -1);
            _camera.UpDirection = new Vector3D(0, 1, 0);
            _camera.FieldOfView = 45;
        }

        private void btnTest1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearCubes(_testCubes);

                // Translate Only (negative)
                ModelVisual3D cube = CreateCube(_testCubes, UtilityWPF.AlphaBlend(Colors.Gold, Colors.White, .5d), 1d, new Point3D(0, 0, 0));
                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(-5, 0, 0));
                cube.Transform = transform;

                // Translate/Rotate
                cube = CreateCube(_testCubes, UtilityWPF.AlphaBlend(Colors.Gold, Colors.White, 1d), 1d, new Point3D(0, 0, 0));
                transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(5, 0, 0));
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45)));
                cube.Transform = transform;

                // Rotate/Translate
                cube = CreateCube(_testCubes, UtilityWPF.AlphaBlend(Colors.Gold, Colors.White, 0d), 1d, new Point3D(0, 0, 0));
                transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45)));
                transform.Children.Add(new TranslateTransform3D(5, 0, 0));
                cube.Transform = transform;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private ModelVisual3D CreateCube(List<ModelVisual3D> cubeContainer, Color color, double size, Point3D location)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.Geometry = UtilityWPF.GetCube(size);
            //geometry.Geometry = UtilityWPF.GetRectangle3D()

            // Transform
            Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d))));
            transform.Children.Add(new TranslateTransform3D(location.ToVector()));

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = transform;

            // Add to the viewport
            _viewport.Children.Add(retVal);

            cubeContainer.Add(retVal);

            // Exit Function
            return retVal;
        }

        private void ClearCubes(List<ModelVisual3D> cubeContainer)
        {
            while (cubeContainer.Count > 0)
            {
                _viewport.Children.Remove(cubeContainer[0]);
                cubeContainer.RemoveAt(0);
            }
        }

        #endregion
    }
}
