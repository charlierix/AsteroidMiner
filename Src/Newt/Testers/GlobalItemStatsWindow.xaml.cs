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
using System.Windows.Shapes;
using System.Windows.Threading;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers
{
    public partial class GlobalItemStatsWindow : Window
    {
        #region Declaration Section

        private DispatcherTimer _timer;

        #endregion

        #region Constructor

        public GlobalItemStatsWindow()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1d);
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.IsEnabled = true;
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // ObjectStorage
                int worldCount, hullCount, bodyCount, jointCount;
                ObjectStorage.Instance.GetStats(out worldCount, out hullCount, out bodyCount, out jointCount);

                lblWorlds.Text = worldCount.ToString("N0");
                lblHulls.Text = hullCount.ToString("N0");
                lblBodies.Text = bodyCount.ToString("N0");
                lblJoints.Text = jointCount.ToString("N0");

                // NeuralPool
                lblNeuralPoolThreads.Text = NeuralPool.Instance.NumThreads.ToString("N0");

                int buckets, links;
                NeuralPool.Instance.GetStats(out buckets, out links);

                lblNeuralPoolBuckets.Text = buckets.ToString("N0");
                lblNeuralPoolLinks.Text = links.ToString("N0");

                // TokenGenerator
                lblToken.Text = TokenGenerator.Instance.GetCurrentToken_DEBUGGINGONLY().ToString("N0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnGarbageCollect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ellipseGC.Fill = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128));

                await Task.Run(() => GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true));

                ellipseGC.Fill = new SolidColorBrush(Color.FromArgb(128, 128, 255, 128));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
