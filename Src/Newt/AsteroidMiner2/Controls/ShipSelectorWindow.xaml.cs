using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xaml;

using Game.Newt.AsteroidMiner2;

namespace Game.Newt.AsteroidMiner2.Controls
{
    public partial class ShipSelectorWindow : Window
    {
        #region Declaration Section

        private NewtonDynamics.World _world = null;

        private List<ShipDNA> _currentDNA = new List<ShipDNA>();

        private Tuple<Task<Tuple<string, ShipDNA>[]>, CancellationTokenSource> _currentTask = null;
        private List<Tuple<Task<Tuple<string, ShipDNA>[]>, CancellationTokenSource>> _cancellingTasks = new List<Tuple<Task<Tuple<string, ShipDNA>[]>, CancellationTokenSource>>();

        #endregion

        #region Constructor

        public ShipSelectorWindow(string foldername, NewtonDynamics.World world)
            : this(world)
        {
            grdFolder.Visibility = Visibility.Visible;
            lblStatus.Visibility = Visibility.Visible;

            // Let the textchange event load the listbox
            txtFoldername.Text = foldername;
        }

        public ShipSelectorWindow(ShipDNA[] dna, NewtonDynamics.World world)
            : this(world)
        {
            grdFolder.Visibility = Visibility.Collapsed;
            lblStatus.Visibility = Visibility.Collapsed;

            foreach (ShipDNA item in dna)
            {
                AddItem(item, null);
            }
        }

        private ShipSelectorWindow(NewtonDynamics.World world)
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            _world = world;
        }

        #endregion

        #region Public Properties

        public ShipDNA[] SelectedItems
        {
            get;
            private set;
        }

        #endregion

        #region Event Listeners

        private void txtFoldername_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }
        private void txtFoldername_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (filenames == null || filenames.Length == 0)
                {
                    MessageBox.Show("No folders selected", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (filenames.Length > 1)
                {
                    MessageBox.Show("Only one folder allowed", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                txtFoldername.Text = filenames[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // This will cause textchange to fire, and the list will be refreshed
                    txtFoldername.Text = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtFoldername_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Clear the current results
            lstItems.Items.Clear();
            _currentDNA.Clear();
            lblStatus.Content = "Scanning...";

            if (!Directory.Exists(txtFoldername.Text))
            {
                return;
            }

            // Cancel a current running task
            if (_currentTask != null)
            {
                _currentTask.Item2.Cancel();
                _cancellingTasks.Add(_currentTask);		// don't wait for it, just throw it in a list
                _currentTask = null;
            }

            CancellationTokenSource cancel = new CancellationTokenSource();
            CancellationToken cancelToken = cancel.Token;

            string foldername = txtFoldername.Text;

            var task = new Task<Tuple<string, ShipDNA>[]>(() =>
            {
                // Find ship dna files
                return ScanFolder(foldername, cancelToken);
            }, cancelToken);

            task.ContinueWith(result =>
            {
                // Show results
                ShowScanResults(result);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            _currentTask = Tuple.Create(task, cancel);

            task.Start();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstItems.SelectedItems == null || lstItems.SelectedItems.Count == 0)
                {
                    MessageBox.Show("There is nothing selected", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.DialogResult = false;
                    return;
                }

                // Pull the dna out of the selected rows
                List<ShipDNA> selectedItems = new List<ShipDNA>();
                foreach (Grid item in lstItems.SelectedItems)
                {
                    selectedItems.Add(((ShipIcon)item.Children[0]).ShipDNA);
                }
                this.SelectedItems = selectedItems.ToArray();

                // Report success
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.DialogResult = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static Tuple<string, ShipDNA>[] ScanFolder(string foldername, CancellationToken cancel)
        {
            //NOTE: This method is running in an arbitrary thread

            try
            {
                List<Tuple<string, ShipDNA>> retVal = new List<Tuple<string, ShipDNA>>();

                foreach (string filename in Directory.EnumerateFiles(foldername, "*.xml"))
                {
                    if (cancel.IsCancellationRequested)
                    {
                        return null;
                    }

                    try
                    {
                        // Try to deserialize this file as shipdna
                        ShipDNA dna = (ShipDNA)XamlServices.Load(filename);

                        // Success, add it
                        retVal.Add(Tuple.Create(filename, dna));
                    }
                    catch (Exception) { }
                }

                return retVal.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }
        private void ShowScanResults(Task<Tuple<string, ShipDNA>[]> result)
        {
            //NOTE: This method is running on the gui thread

            try
            {
                var cancellingTask = _cancellingTasks.Where(o => o.Item1 == result).FirstOrDefault();

                if (cancellingTask != null)
                {
                    // This task was cancelled, ignore the results
                    _cancellingTasks.Remove(cancellingTask);
                }
                else if (_currentTask != null && _currentTask.Item1 == result)
                {
                    #region Standard Flow

                    if (result.Result != null && result.Result.Length > 0)
                    {
                        // Show results
                        foreach (var file in result.Result)
                        {
                            AddItem(file.Item2, file.Item1);
                        }

                        lblStatus.Content = "";
                    }
                    else
                    {
                        lblStatus.Content = "No ships in this folder";
                    }

                    _currentTask = null;

                    #endregion
                }
                else
                {
                    // Not sure what happened, just go away
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddItem(ShipDNA dna, string filename)
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1d, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4d) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1d, GridUnitType.Star) });

            ShipIcon icon = new ShipIcon(dna.ShipName, dna, _world);
            icon.ShowShipName = false;
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);
            icon.Width = 60d;		// this has logic to keep itself square

            StackPanel panel = new StackPanel();
            panel.HorizontalAlignment = HorizontalAlignment.Left;
            panel.VerticalAlignment = VerticalAlignment.Center;

            Label label = new Label();
            label.Content = dna.ShipName;
            label.FontSize = 14;
            label.FontWeight = FontWeights.DemiBold;
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Center;
            panel.Children.Add(label);

            if (!string.IsNullOrEmpty(filename))
            {
                label = new Label();
                label.Content = filename;
                label.FontSize = 10;
                label.Foreground = new SolidColorBrush(Color.FromRgb(96, 96, 96));
                label.HorizontalAlignment = HorizontalAlignment.Left;
                label.VerticalAlignment = VerticalAlignment.Center;
                panel.Children.Add(label);
            }

            Grid.SetColumn(panel, 2);
            grid.Children.Add(panel);

            lstItems.Items.Add(grid);
            _currentDNA.Add(dna);
        }

        #endregion
    }
}
