using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

using Game.HelperClassesWPF.Controls2D;

namespace Game.Newt.v2.FlyingBeans
{
    public partial class PanelSimulation : UserControl
    {
        #region Declaration Section

        private const string MSGBOXCAPTION = "PanelSimulation";

        private List<SliderShowValues.PropSync> _propLinks = new List<SliderShowValues.PropSync>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public PanelSimulation(FlyingBeanOptions options)
        {
            InitializeComponent();

            _options = options;

            PropertyInfo[] propsOptions = typeof(FlyingBeanOptions).GetProperties();

            //trkNumBeans.Value = _options.NumBeansAtATime;
            //trkProbWinner.Value = _options.NewBeanProbOfWinner * 100d;
            //trkGravity.Value = _options.Gravity;

            // Simulation
            _propLinks.Add(new SliderShowValues.PropSync(trkNumBeans, propsOptions.Where(o => o.Name == "NumBeansAtATime").First(), _options, 1, 30));
            _propLinks.Add(new SliderShowValues.PropSync(trkProbWinner, propsOptions.Where(o => o.Name == "NewBeanProbOfWinner").First(), _options, 0, 1));		//TODO: Multiply by 100
            _propLinks.Add(new SliderShowValues.PropSync(trkGravity, propsOptions.Where(o => o.Name == "Gravity").First(), _options, 0, 2));
            chkRandomOrientation.IsChecked = _options.NewBeanRandomOrientation;
            chkRandomSpin.IsChecked = _options.NewBeanRandomSpin;

            // Death
            _propLinks.Add(new SliderShowValues.PropSync(trkLifespan, propsOptions.Where(o => o.Name == "MaxAgeSeconds").First(), _options, 5, 90));
            _propLinks.Add(new SliderShowValues.PropSync(trkAngularVelocity, propsOptions.Where(o => o.Name == "AngularVelocityDeath").First(), _options, 0, 40));
            _propLinks.Add(new SliderShowValues.PropSync(trkGroundCollisions, propsOptions.Where(o => o.Name == "MaxGroundCollisions").First(), _options, 0, 5));

            // Misc
            chkShowExplosions.IsChecked = _options.ShowExplosions;

            _isInitialized = true;
        }

        #endregion

        #region Public Properties

        private FlyingBeanOptions _options;
        public FlyingBeanOptions Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;

                foreach (var link in _propLinks)
                {
                    //if (link.Item is FlyingBeanOptions)		// they're all the same type
                    //{
                    link.Item = value;
                    //}
                }

                chkRandomOrientation.IsChecked = _options.NewBeanRandomOrientation;
                chkRandomSpin.IsChecked = _options.NewBeanRandomSpin;
                chkShowExplosions.IsChecked = _options.ShowExplosions;
            }
        }

        #endregion

        #region Event Listeners

        private void chkRandomOrientation_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                _options.NewBeanRandomOrientation = chkRandomOrientation.IsChecked.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkRandomSpin_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                _options.NewBeanRandomSpin = chkRandomSpin.IsChecked.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkShowExplosions_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                _options.ShowExplosions = chkShowExplosions.IsChecked.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private void trkNumBeans_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.NumBeansAtATime = Convert.ToInt32(trkNumBeans.Value);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkProbWinner_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.NewBeanProbOfWinner = trkProbWinner.Value * .01d;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkGravity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.Gravity = trkGravity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        #endregion
    }
}
