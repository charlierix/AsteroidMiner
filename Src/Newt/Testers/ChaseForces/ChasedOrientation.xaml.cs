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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Game.Newt.Testers.ChaseForces
{
    public partial class ChasedOrientation : UserControl
    {
        #region Declaration Section

        private const string TITLE = "ChasedPosition";

        private readonly ChasedBall _ball;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ChasedOrientation(ChasedBall ball)
        {
            _ball = ball;

            InitializeComponent();

            this.DataContext = this;

            _initialized = true;

            // Fire the events to set the properties
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
                    _ball.MotionType_Orientation = MotionType_Orientation.Stop;
                }
                else if (radMotionJump.IsChecked.Value)
                {
                    _ball.MotionType_Orientation = MotionType_Orientation.Jump;
                }
                else if (radMotionBrownian.IsChecked.Value)
                {
                    _ball.MotionType_Orientation = MotionType_Orientation.Brownian;
                }
                else if (radMotionConstant.IsChecked.Value)
                {
                    _ball.MotionType_Orientation = MotionType_Orientation.Constant;
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

                _ball.Speed_Orientation = trkSpeed.Value;
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

                _ball.Delay_Orientation = trkDelay.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
