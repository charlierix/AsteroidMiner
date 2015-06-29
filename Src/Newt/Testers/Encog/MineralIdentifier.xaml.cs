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
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.MapParts;

namespace Game.Newt.Testers.Encog
{
    public partial class MineralIdentifier : Window
    {
        #region Declaration Section

        private const double CAMERADISTANCE = 2.8;

        private Visual3D _mineralVisual = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private readonly Vector3D _lightDirection = new Vector3D(-1, -1, -1);

        private bool _initialized = false;

        #endregion

        #region Constructor

        public MineralIdentifier()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            #region Tab: Single Image

            // Camera Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            _trackball.MouseWheelScale *= .1;
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            // Mineral Types
            foreach (MineralType mineral in Enum.GetValues(typeof(MineralType)))
            {
                cboMineral.Items.Add(mineral);
            }
            cboMineral.SelectedIndex = 0;

            #endregion
            #region Tab: Training Data

            // Mineral Types
            foreach (MineralType mineral in Enum.GetValues(typeof(MineralType)))
            {
                CheckBox mineralCheckbox = new CheckBox()
                {
                    Content = mineral.ToString(),
                    Tag = mineral,
                    Margin = new Thickness(2),
                };

                pnlMineralSelections.Children.Add(mineralCheckbox);
            }

            // Convolutions
            //TODO: Store and display these (for now, just do the MaxAbs of horz and vert sobel)

            #endregion

            _initialized = true;

            cboMineral_SelectionChanged(this, null);
            ResetCamera_Click(this, null);
        }

        #endregion

        #region Event Listeners - single image

        private void cboMineral_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                if (_mineralVisual != null)
                {
                    _viewport.Children.Remove(_mineralVisual);
                }

                MineralType mineralType = (MineralType)cboMineral.SelectedValue;

                // Create visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = Mineral.GetNewVisual(mineralType);
                //visual.Transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation()));

                _mineralVisual = visual;
                _viewport.Children.Add(_mineralVisual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _camera.Position = new Point3D(0, -CAMERADISTANCE, 0);
                _camera.LookDirection = new Vector3D(0, 1, 0);
                _camera.UpDirection = new Vector3D(0, 0, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandomCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Position
                Vector3D position = Math3D.GetRandomVector_Spherical(CAMERADISTANCE / 2, CAMERADISTANCE * 2);

                // Look Direction
                Vector3D lookDirection = position * -1;

                Vector3D rotateAxis = Math3D.GetRandomVector_Cone(Math3D.GetArbitraryOrhonganal(lookDirection), 20);
                Quaternion rotate = new Quaternion(rotateAxis, Math3D.GetNearZeroValue(20));

                lookDirection = rotate.GetRotatedVector(lookDirection);

                // Up Vector
                Vector3D up = Math3D.GetArbitraryOrhonganal(lookDirection);

                // Commit
                _camera.Position = position.ToPoint();
                _camera.LookDirection = lookDirection;
                _camera.UpDirection = up;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetLightPos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                directionalBright.Direction = _lightDirection;
                directionDim.Direction = _lightDirection * -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandomLightPos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                directionalBright.Direction = Math3D.GetRandomVector_Spherical_Shell(1);
                directionDim.Direction = -directionalBright.Direction;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WhiteLight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                directionalBright.Color = UtilityWPF.ColorFromHex("FFFFFF");
                directionDim.Color = UtilityWPF.ColorFromHex("707070");
                ambient.Color = UtilityWPF.ColorFromHex("787878");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandomLight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Go more extreme (put that in a separate button)
                directionalBright.Color = UtilityWPF.GetRandomColor(215, 255);
                directionDim.Color = UtilityWPF.GetRandomColor(100, 155);
                ambient.Color = UtilityWPF.GetRandomColor(115, 140);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int size;
                if (!int.TryParse(txtImageSize.Text, out size))
                {
                    MessageBox.Show("Couldn't parse image size as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (size < 1)
                {
                    MessageBox.Show("Size must be at least 1", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (radColor.IsChecked.Value)
                {
                    // No need to convert in/out of bitmap source
                    image.Source = UtilityWPF.RenderControl(grdViewPort, size, size, true);
                    return;
                }

                // Render the control
                BitmapCustomCachedBytes bitmap = null;

                if (radGrayTransparent.IsChecked.Value)
                {
                    bitmap = (BitmapCustomCachedBytes)UtilityWPF.RenderControl(grdViewPort, size, size, false, Colors.Transparent, true);
                }
                else if (radGrayBlack.IsChecked.Value)
                {
                    Brush background = grdViewPort.Background;
                    grdViewPort.Background = Brushes.Black;
                    grdViewPort.UpdateLayout();

                    bitmap = (BitmapCustomCachedBytes)UtilityWPF.RenderControl(grdViewPort, size, size, false, Colors.Black, true);

                    grdViewPort.Background = background;
                }
                else
                {
                    throw new ApplicationException("Unknown radio button");
                }

                // Convert to gray
                var colors = bitmap.GetColorBytes().
                    Select(o =>
                    {
                        byte gray = Convert.ToByte(UtilityWPF.ConvertToGray(o[1], o[2], o[3]));
                        return new byte[] { o[0], gray, gray, gray };
                    }).
                    ToArray();

                // Show it
                image.Source = UtilityWPF.GetBitmap(colors, size, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region Event Listeners - training data

        private void CheckBox_MineralSelection_Checked(object sender, RoutedEventArgs e)
        {

        }

        #endregion
    }
}
