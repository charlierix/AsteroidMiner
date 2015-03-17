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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Game.Newt.Testers.ChaseForces
{
    public partial class ChasedPosition : UserControl
    {
        #region Declaration Section

        private const string TITLE = "ChasedPosition";

        private readonly ChasedBall _ball;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ChasedPosition(ChasedBall ball)
        {
            _ball = ball;

            InitializeComponent();

            this.DataContext = this;

            _initialized = true;

            // Fire the events to set the properties
            chkBoundCube_Checked(this, new RoutedEventArgs());
            trkSpeed_ValueChanged(this, new EventArgs());
            trkDelay_ValueChanged(this, new EventArgs());
        }

        #endregion

        #region Event Listeners

        private void radMotion_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                if (radMotionStop.IsChecked.Value)
                {
                    _ball.MotionType_Position = MotionType_Position.Stop;
                }
                else if (radMotionJump.IsChecked.Value)
                {
                    _ball.MotionType_Position = MotionType_Position.Jump;
                }
                else if (radMotionBrownian.IsChecked.Value)
                {
                    _ball.MotionType_Position = MotionType_Position.Brownian;
                }
                else if (radMotionBounceOffWalls.IsChecked.Value)
                {
                    _ball.MotionType_Position = MotionType_Position.BounceOffWalls;
                }
                else if (radMotionOrbit.IsChecked.Value)
                {
                    _ball.MotionType_Position = MotionType_Position.Orbit;
                }
                else
                {
                    throw new ApplicationException("Unknown MotionType");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkSpeed_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                _ball.Speed_Position = trkSpeed.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkBoundCube_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                Vector3D boundry;

                if (chkBoundCube.IsChecked.Value)
                {
                    lblBoundXYZ.Visibility = System.Windows.Visibility.Visible;
                    trkBoundXYZ.Visibility = System.Windows.Visibility.Visible;

                    lblBoundX.Visibility = System.Windows.Visibility.Collapsed;
                    trkBoundX.Visibility = System.Windows.Visibility.Collapsed;
                    lblBoundY.Visibility = System.Windows.Visibility.Collapsed;
                    trkBoundY.Visibility = System.Windows.Visibility.Collapsed;
                    lblBoundZ.Visibility = System.Windows.Visibility.Collapsed;
                    trkBoundZ.Visibility = System.Windows.Visibility.Collapsed;

                    double val = trkBoundXYZ.Value;
                    boundry = new Vector3D(val, val, val);
                }
                else
                {
                    lblBoundXYZ.Visibility = System.Windows.Visibility.Collapsed;
                    trkBoundXYZ.Visibility = System.Windows.Visibility.Collapsed;

                    lblBoundX.Visibility = System.Windows.Visibility.Visible;
                    trkBoundX.Visibility = System.Windows.Visibility.Visible;
                    lblBoundY.Visibility = System.Windows.Visibility.Visible;
                    trkBoundY.Visibility = System.Windows.Visibility.Visible;
                    lblBoundZ.Visibility = System.Windows.Visibility.Visible;
                    trkBoundZ.Visibility = System.Windows.Visibility.Visible;

                    boundry = new Vector3D(trkBoundX.Value, trkBoundY.Value, trkBoundZ.Value);
                }

                _ball.Boundry = boundry;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkBound_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                Vector3D boundry;

                if (chkBoundCube.IsChecked.Value)
                {
                    double val = trkBoundXYZ.Value;
                    boundry = new Vector3D(val, val, val);
                }
                else
                {
                    boundry = new Vector3D(trkBoundX.Value, trkBoundY.Value, trkBoundZ.Value);
                }

                _ball.Boundry = boundry;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkDelay_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                _ball.Delay_Position = trkDelay.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
