using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;
using Game.Newt.v2.GameItems;

namespace Game.Newt.Testers.ChaseForces
{
    public partial class GradientStops : UserControl
    {
        #region class: StopEntry

        private class StopEntry
        {
            public event EventHandler ValueChanged = null;

            public StopEntry(SliderShowValues distance, SliderShowValues percent)
            {
                this.Distance = distance;
                this.Percent = percent;

                distance.ValueChanged += new EventHandler(Slider_ValueChanged);
                percent.ValueChanged += new EventHandler(Slider_ValueChanged);
            }

            public readonly SliderShowValues Distance;
            public readonly SliderShowValues Percent;

            private void Slider_ValueChanged(object sender, EventArgs e)
            {
                if (this.ValueChanged != null)
                {
                    this.ValueChanged(this, new EventArgs());
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler ValueChanged = null;

        #endregion

        #region Declaration Section

        private const string TITLE = "GradientStops";

        private GradientStopsPreset _presetPopupChild = null;

        /// <summary>
        /// These are the rows that get added to grdStops
        /// </summary>
        private List<StopEntry> _entries = new List<StopEntry>();

        #endregion

        #region Constructor

        public GradientStops()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        public Popup ParentPopup
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public GradientEntry[] GetSelection()
        {
            return _entries.
                Select(o => new GradientEntry(o.Distance.Value, o.Percent.Value)).
                OrderBy(o => o.Distance).      // it should already be sorted, but just making sure
                ToArray();
        }

        public void StoreSelection(GradientEntry[] gradient)
        {
            Clear();

            if (gradient != null)
            {
                for (int cntr = 0; cntr < gradient.Length; cntr++)
                {
                    Insert(cntr, gradient[cntr].Distance, gradient[cntr].Percent);
                }
            }

            OnValueChanged();
        }

        #endregion
        #region Protected Methods

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Event Listeners

        private void Entry_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = 0;
                if (!int.TryParse(txtInsertIndex.Text, out index))
                {
                    MessageBox.Show("Couldn't parse insert index as an integer", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (index < 0 || index > _entries.Count)
                {
                    MessageBox.Show("Insert index is out of range", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double distance = 0;
                double percent = 0;

                //TODO: Take the average of the entries around it

                Insert(index, distance, percent);

                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = 0;
                if (!int.TryParse(txtDeleteIndex.Text, out index))
                {
                    MessageBox.Show("Couldn't parse delete index as an integer", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (index < 0 || index >= _entries.Count)
                {
                    MessageBox.Show("Delete index is out of range", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Delete(index);

                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clear();

                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_presetPopupChild == null)
                {
                    _presetPopupChild = new GradientStopsPreset();
                    _presetPopupChild.ParentPopup = presetPopup;
                    presetPopup.Child = _presetPopupChild;
                }

                if (this.ParentPopup != null)        // it should never be null
                {
                    this.ParentPopup.StaysOpen = true;
                }

                presetPopup.Placement = PlacementMode.Relative;
                presetPopup.PlacementTarget = this;
                presetPopup.VerticalOffset = 0;
                presetPopup.HorizontalOffset = this.ActualWidth + 5;

                presetPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PresetPopup_Closed(object sender, EventArgs e)
        {
            try
            {
                // Go back to being able to close when they click off this control
                if (this.ParentPopup != null)
                {
                    // Make sure the mouse is over this control before changing StaysOpen, or this popup will disapear
                    //UtilityWPF.SetMousePosition(this, new Point(this.ActualWidth / 2, this.ActualHeight / 2));
                    //UtilityWPF.SetMousePosition(btnPreset, new Point(btnPreset.ActualWidth / 2, btnPreset.ActualHeight / 2));

                    // Actually, setting focus is enough to keep it alive
                    //this.Focus();

                    // Actually, nothing is needed to keep it alive :)

                    this.ParentPopup.StaysOpen = false;
                }

                // Get what they chose
                var selection = _presetPopupChild.GetSelection();
                if (selection == null)
                {
                    return;
                }

                // Load this control with the selected values
                StoreSelection(selection);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void Insert(int index, double distance, double percent)
        {
            #region Validate

            if (index < 0 || index > _entries.Count)
            {
                throw new ArgumentException("Insert index is out of range");
            }

            if (distance < 0)
            {
                throw new ArgumentException("Distance can't be negative");
            }

            #endregion

            // Create the row
            SliderShowValues distanceCtrl = new SliderShowValues()
            {
                Minimum = 0,
                Maximum = Math.Max(10, distance * 4),
                Value = distance,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            SliderShowValues percentCtrl = new SliderShowValues()
            {
                Minimum = Math.Min(0, percent),
                Maximum = Math.Max(1, percent),
                Value = percent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            StopEntry entry = new StopEntry(distanceCtrl, percentCtrl);

            _entries.Insert(index, entry);

            // Add to grid
            Grid.SetColumn(entry.Distance, 0);
            Grid.SetColumn(entry.Percent, 2);

            if (grdStops.RowDefinitions.Count - 1 == index)
            {
                grdStops.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            }

            grdStops.Children.Add(entry.Distance);
            grdStops.Children.Add(entry.Percent);

            SyncGridRowIndices();

            entry.ValueChanged += new EventHandler(Entry_ValueChanged);

            txtInsertIndex.Text = _entries.Count.ToString();
        }
        private void Delete(int index)
        {
            if (index < 0 || index >= _entries.Count)
            {
                throw new ArgumentException("Delete index is out of range");
            }

            grdStops.Children.Remove(_entries[index].Distance);
            grdStops.Children.Remove(_entries[index].Percent);

            _entries.RemoveAt(index);

            SyncGridRowIndices();

            txtInsertIndex.Text = _entries.Count.ToString();
        }
        private void Clear()
        {
            foreach (StopEntry entry in _entries)
            {
                grdStops.Children.Remove(entry.Distance);
                grdStops.Children.Remove(entry.Percent);
            }

            _entries.Clear();

            txtInsertIndex.Text = _entries.Count.ToString();
        }

        private void SyncGridRowIndices()
        {
            for (int cntr = 0; cntr < _entries.Count; cntr++)
            {
                Grid.SetRow(_entries[cntr].Distance, cntr + 1);     // adding one because of the header
                Grid.SetRow(_entries[cntr].Percent, cntr + 1);
            }
        }

        #endregion
    }
}
