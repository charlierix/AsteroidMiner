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
using System.Windows.Threading;

using Game.Newt.AsteroidMiner2;
using Game.Newt.HelperClasses.Controls2D;

namespace Game.Newt.Testers.FlyingBeans
{
    public partial class PanelTracking : UserControl
    {
        #region Events

        public event EventHandler WinnerListsRecreated = null;
        public event EventHandler NumFinalistsChanged = null;
        public event EventHandler KillLivingBeans = null;

        #endregion

        #region Declaration Section

        private const string MSGBOXCAPTION = "PanelTracking";

        private List<SliderShowValues.PropSync> _propLinks = new List<SliderShowValues.PropSync>();

        private DispatcherTimer _delayTimer = null;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public PanelTracking(FlyingBeanOptions options)
        {
            InitializeComponent();

            _options = options;

            PropertyInfo[] propsOptions = typeof(FlyingBeanOptions).GetProperties();

            _propLinks.Add(new SliderShowValues.PropSync(trkLineagesFinal, propsOptions.Where(o => o.Name == "TrackingMaxLineagesFinal").First(), _options, 1, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkNumPerLineageFinal, propsOptions.Where(o => o.Name == "TrackingMaxPerLineageFinal").First(), _options, 1, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkLineagesLive, propsOptions.Where(o => o.Name == "TrackingMaxLineagesLive").First(), _options, 1, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkNumPerLineageLive, propsOptions.Where(o => o.Name == "TrackingMaxPerLineageLive").First(), _options, 1, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkNumCandidates, propsOptions.Where(o => o.Name == "FinalistCount").First(), _options, 0, 8));
            _propLinks.Add(new SliderShowValues.PropSync(trkScanFrequency, propsOptions.Where(o => o.Name == "TrackingScanFrequencySeconds").First(), _options, .1, 5));

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
            }
        }

        #endregion

        #region Event Listeners

        private void trkNumCandidates_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.NumFinalistsChanged != null)
                {
                    // Inform the world after a delay to make sure the property has been changed

                    if (_delayTimer == null)
                    {
                        _delayTimer = new DispatcherTimer();
                        _delayTimer.Interval = TimeSpan.FromMilliseconds(500);
                        _delayTimer.Tick += new EventHandler(DelayTimer_Tick);
                    }

                    _delayTimer.IsEnabled = false;		// going false first to make sure the timer starts over (in case they are dragging the slider around)
                    _delayTimer.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DelayTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _delayTimer.IsEnabled = false;

                if (this.NumFinalistsChanged != null)
                {
                    this.NumFinalistsChanged(this, new EventArgs());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WinnerList.WinningBean[] finalistDNA = null;
                if (chkKeepFinalists.IsChecked.Value)
                {
                    var dump = _options.WinnersFinal.Current;

                    if (dump != null)
                    {
                        finalistDNA = dump.SelectMany(o => o.BeansByLineage).SelectMany(o => o.Item2).ToArray();
                    }
                }

                _options.WinnersLive = new WinnerList(true, _options.TrackingMaxLineagesLive, _options.TrackingMaxPerLineageLive);
                _options.WinnersFinal = new WinnerList(false, _options.TrackingMaxLineagesFinal, _options.TrackingMaxPerLineageFinal);

                if (this.WinnerListsRecreated != null)
                {
                    this.WinnerListsRecreated(this, new EventArgs());
                }

                if (chkKillEmAll.IsChecked.Value && this.KillLivingBeans != null)
                {
                    this.KillLivingBeans(this, new EventArgs());
                }

                // Make the finalists recompete for position
                if (chkKeepFinalists.IsChecked.Value && finalistDNA != null)
                {
                    foreach (var finalist in finalistDNA)
                    {
                        _options.WinnerCandidates.Add(finalist.DNA, finalist.Score);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
