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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Game.Newt.v1.AsteroidMiner1.AsteroidMiner2D_153
{
    public partial class OptionsPanel : UserControl
    {
        #region Events

        public event EventHandler ValueChanged = null;

        public event SetVelocitiesHandler SetVelocities = null;

        public event EventHandler CloseDialog = null;

        #endregion

        #region Declaration Section

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public OptionsPanel()
        {
            InitializeComponent();

            _isInitialized = true;

            trkCameraAngle_ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(trkCameraAngle.Value, trkCameraAngle.Value));
        }

        #endregion

        #region Public Properties

        public bool CameraAlwaysLooksUp
        {
            get
            {
                return chkCameraAlwaysLooksUp.IsChecked.Value;
            }
            set
            {
                chkCameraAlwaysLooksUp.IsChecked = value;
            }
        }

        /// <summary>
        /// 90 is looking straight down on the ship, 0 is along the plane of the ship
        /// </summary>
        public double CameraAngle
        {
            get
            {
                return trkCameraAngle.Value;
            }
            set
            {
                if (value < trkCameraAngle.Minimum)
                {
                    trkCameraAngle.Value = trkCameraAngle.Minimum;
                }
                else if (value > trkCameraAngle.Maximum)
                {
                    trkCameraAngle.Value = trkCameraAngle.Maximum;
                }
                else
                {
                    trkCameraAngle.Value = value;
                }
            }
        }

        public bool ShowDebugVisuals
        {
            get
            {
                return chkShowDebugVisuals.IsChecked.Value;
            }
            set
            {
                chkShowDebugVisuals.IsChecked = value;
            }
        }

        public bool HasGravity
        {
            get
            {
                return chkGravity.IsChecked.Value;
            }
            set
            {
                chkGravity.IsChecked = value;
            }
        }
        public double GravityStrength
        {
            get
            {
                return trkGravity.Value;
            }
            set
            {
                if (value < trkGravity.Minimum)
                {
                    trkGravity.Value = trkGravity.Minimum;
                }
                else if (value > trkGravity.Maximum)
                {
                    trkGravity.Value = trkGravity.Maximum;
                }
                else
                {
                    trkGravity.Value = value;
                }
            }
        }

        public double WorldSize
        {
            set
            {
                lblWorldSize.Text = Math.Round(value, 0).ToString("N0");
            }
        }

        #endregion

        #region Event Listeners

        private void chkCameraAlwaysLooksUp_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        private void trkCameraAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized)
            {
                return;
            }

            lblCameraAngle.Text = Math.Round(trkCameraAngle.Value).ToString() + " degrees";

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        private void chkShowDebugVisuals_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        private void chkGravity_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            // Gravity is implemented in the main thread, so is only calculated sporatically.  If it's too strong, the choppyness
            // is very noticable, so I don't let it be adjustable by the user
            //if (chkGravity.IsChecked.Value)
            //{
            //    trkGravity.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    trkGravity.Visibility = Visibility.Collapsed;
            //}

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }
        private void trkGravity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        private void btnVelocityStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            OnSetVelocities(false, 0d);
        }
        private void btnVelocitySlow_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            OnSetVelocities(true, 5d);
        }
        private void btnVelocityMed_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            OnSetVelocities(true, 20d);
        }
        private void btnVelocityFast_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            OnSetVelocities(true, 60d);
        }
        private void btnVelocityInsane_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            OnSetVelocities(true, 160d);
        }
        private void btnVelocityPlaid_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            // They've gone PLAID!!!!!!!!!!!!!

            OnSetVelocities(true, 500);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (this.CloseDialog == null)
            {
                MessageBox.Show("There is no event listener for the back button", "Back Button", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.CloseDialog(this, new EventArgs());
        }

        #endregion

        #region Protected Methods

        protected void OnSetVelocities(bool isRandom, double speed)
        {
            if (this.SetVelocities == null)
            {
                return;
            }

            SetVelocitiesArgs args = new SetVelocitiesArgs();
            args.Asteroids = chkVelocityAsteroid.IsChecked.Value;
            args.Minerals = chkVelocityMineral.IsChecked.Value;

            args.IsRandom = isRandom;
            args.Speed = speed;

            this.SetVelocities(this, args);
        }

        #endregion
    }

    #region SetVelocities delegate/args

    public delegate void SetVelocitiesHandler(object sender, SetVelocitiesArgs e);

    public class SetVelocitiesArgs : EventArgs
    {
        public bool Asteroids = false;
        public bool Minerals = false;

        public bool IsRandom = false;
        public double Speed = 0d;
    }

    #endregion
}
