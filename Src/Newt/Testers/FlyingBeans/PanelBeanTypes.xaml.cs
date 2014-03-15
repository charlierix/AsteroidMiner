using System;
using System.Collections.Generic;
using System.IO;
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

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.Controls;
using Game.Newt.AsteroidMiner2.ShipParts;

namespace Game.Newt.Testers.FlyingBeans
{
    public partial class PanelBeanTypes : UserControl
    {
        #region Declaration Section

        private const string MSGBOXCAPTION = "PanelBeanTypes";

        private readonly NewtonDynamics.World _world;

        private List<ShipIcon> _icons = new List<ShipIcon>();

        #endregion

        #region Constructor

        public PanelBeanTypes(FlyingBeanOptions options, NewtonDynamics.World world)
        {
            InitializeComponent();

            _world = world;

            this.Options = options;
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
                // Clear existing
                if (_options != null)
                {
                    foreach (ShipIcon icon in _icons.ToArray())
                    {
                        RemoveIconVisual(icon);
                        _options.NewBeanList.Remove(icon.ShipName);
                    }
                }

                // Store it
                _options = value;

                // Show the icons
                foreach (string name in _options.NewBeanList.Keys)
                {
                    AddIconVisual(name, _options.NewBeanList[name], _world);
                }

                SetButtonEnabled();
            }
        }

        #endregion

        #region Public Methods

        public void AddFromFile(string subfolder)
        {
            string foldername = EnsureShipFolderExists(subfolder);

            ShipSelectorWindow dialog = new ShipSelectorWindow(foldername, _world);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            bool? dialogResult = dialog.ShowDialog();

            if (dialogResult == null || !dialogResult.Value)
            {
                return;
            }

            List<Tuple<List<string>, string[]>> missingParts = new List<Tuple<List<string>, string[]>>();

            // Add the selected items
            foreach (ShipDNA dna in dialog.SelectedItems)
            {
                // Make sure this name is unique - it's ok to modify the dna directly, it came from a file, and the dialog is about to go away
                dna.ShipName = GetUniqueName(dna.ShipName);

                #region Detect missing parts

                string[] missing = FindMissingParts(dna);
                if (missing != null && missing.Length > 0)
                {
                    var existing = missingParts.Where(o => o.Item2.Union(missing).Count() == missing.Length).FirstOrDefault();
                    if (existing == null)
                    {
                        missingParts.Add(Tuple.Create(new string[] { dna.ShipName }.ToList(), missing));
                    }
                    else
                    {
                        existing.Item1.Add(dna.ShipName);
                    }
                }

                #endregion

                AddIconVisual(dna.ShipName, dna, _world);
                _options.NewBeanList.Add(dna.ShipName, dna);
            }

            SetButtonEnabled();

            #region Report Missing

            if (missingParts.Count > 0)
            {
                bool isSingular = missingParts.Count == 1 && missingParts[0].Item1.Count == 1;

                StringBuilder report = new StringBuilder();
                if (isSingular)
                {
                    report.Append("This ship doesn't");
                }
                else
                {
                    report.Append("These ships don't");
                }
                report.AppendLine(" have enough parts to function properly:");

                foreach (var set in missingParts)
                {
                    report.AppendLine();
                    report.Append(string.Join(", ", set.Item1));
                    report.AppendLine(":");
                    report.AppendLine(string.Join(", ", set.Item2));
                }

                MessageBox.Show(report.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            #endregion
        }

        public static string EnsureShipFolderExists(string subfolder)
        {
            // Start with the main ship window
            string retVal = System.IO.Path.Combine(UtilityHelper.GetOptionsFolder(), ShipEditorWindow.SHIPFOLDER);

            // If requested, go to a subfolder
            if (!string.IsNullOrEmpty(subfolder))
            {
                retVal = System.IO.Path.Combine(retVal, subfolder);
            }

            // Make sure it exists
            if (!Directory.Exists(retVal))
            {
                Directory.CreateDirectory(retVal);
            }

            // Exit Function
            return retVal;
        }

        #endregion

        #region Event Listeners

        private void Icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl);

                // Toggle IsSelected
                foreach (ShipIcon icon in _icons)
                {
                    if (icon == sender)
                    {
                        if (isCtrl)
                        {
                            icon.IsSelected = !icon.IsSelected;
                        }
                        else
                        {
                            icon.IsSelected = true;
                        }
                    }
                    else
                    {
                        if (!isCtrl)		// if ctrl is held in, leave the others alone
                        {
                            icon.IsSelected = false;
                        }
                    }
                }

                // Enable remove button
                SetButtonEnabled();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void pnlIconGridBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!(e.Source is ShipIcon))
                {
                    // They clicked on the dead space outside of the icons.  Clear the selection
                    foreach (ShipIcon icon in _icons)
                    {
                        icon.IsSelected = false;
                    }

                    SetButtonEnabled();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lock (_options.Lock)
                {
                    foreach (ShipIcon icon in _icons.Where(o => o.IsSelected).ToArray())
                    {
                        RemoveIconVisual(icon);
                        _options.NewBeanList.Remove(icon.ShipName);
                    }

                    SetButtonEnabled();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (ShipIcon icon in _icons.ToArray())
                {
                    RemoveIconVisual(icon);
                    _options.NewBeanList.Remove(icon.ShipName);
                }

                SetButtonEnabled();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lock (_options.Lock)
                {
                    // Get the items in default that aren't currently available
                    string[] names = _options.DefaultBeanList.Keys.Where(o => !_options.NewBeanList.ContainsKey(o)).ToArray();

                    if (names.Length == 0)
                    {
                        MessageBox.Show("All of the default beans are already in use", MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Ask the user to choose
                    ShipDNA[] choices = names.Select(o => _options.DefaultBeanList[o]).ToArray();

                    if (choices.Length > 1)
                    {
                        ShipSelectorWindow dialog = new ShipSelectorWindow(choices, _world);
                        dialog.Width = 300;
                        dialog.Height = 418;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        bool? dialogResult = dialog.ShowDialog();

                        if (dialogResult == null || !dialogResult.Value)
                        {
                            return;
                        }

                        choices = dialog.SelectedItems;
                    }

                    // Add the selected items
                    foreach (ShipDNA dna in choices)
                    {
                        AddIconVisual(dna.ShipName, dna, _world);
                        _options.NewBeanList.Add(dna.ShipName, dna);
                    }
                }

                SetButtonEnabled();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAddFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddFromFile("");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void AddIconVisual(string name, ShipDNA dna, NewtonDynamics.World world)
        {
            ShipIcon icon = new ShipIcon(name, dna, world);
            icon.VerticalAlignment = VerticalAlignment.Top;
            icon.MouseDown += new MouseButtonEventHandler(Icon_MouseDown);

            _icons.Add(icon);
            grdIcons.Children.Add(icon);
        }
        private void RemoveIconVisual(ShipIcon icon)
        {
            icon.MouseDown -= new MouseButtonEventHandler(Icon_MouseDown);

            grdIcons.Children.Remove(icon);
            _icons.Remove(icon);
        }

        private void SetButtonEnabled()
        {
            btnRemove.IsEnabled = _icons.Any(o => o.IsSelected);
            btnClear.IsEnabled = _icons.Count > 0;
        }

        private string GetUniqueName(string name)
        {
            string[] existingNames = UtilityHelper.Iterate(_options.DefaultBeanList.Keys, _options.NewBeanList.Keys).ToArray();

            int suffix = 0;
            while (true)
            {
                string retVal = name;
                if (suffix > 0)
                {
                    retVal += " (" + suffix.ToString() + ")";
                }

                if (!existingNames.Any(o => o.Equals(retVal, StringComparison.OrdinalIgnoreCase)))
                {
                    return retVal;
                }

                suffix++;
            }
        }

        private static string[] FindMissingParts(ShipDNA dna)
        {
            // Without this minimum list of parts, the ship won't work
            //TODO: When I come up with more sensor types, look for any type of sensor
            //NOTE: Brain isn't required if the sensor-thruster link multiplier has been increased, but I'll just say it's required
            string[] partList = new string[] { SensorGravity.PARTTYPE, Brain.PARTTYPE, EnergyTank.PARTTYPE, FuelTank.PARTTYPE, Thruster.PARTTYPE };

            // Get all the parts in the ship
            string[] usedParts = dna.PartsByLayer.Values.SelectMany(o => o).Select(o => o.PartType).Distinct().ToArray();

            // Return the missing ones
            return partList.Where(o => !usedParts.Contains(o)).ToArray();
        }

        #endregion
    }
}
