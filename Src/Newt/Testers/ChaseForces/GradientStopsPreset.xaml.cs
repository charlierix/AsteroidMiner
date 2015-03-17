using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.ChaseForces
{
    public partial class GradientStopsPreset : UserControl
    {
        #region Enum: Preset

        private enum Preset
        {
            Simple_Up,
            Simple_Down,

            S_Curve_Up,
            S_Curve_Down,

            Cube_Up,
            Cube_Down,

            CubeRoot_Up,
            CubeRoot_Down,

            //TODO: Bell?
        }

        #endregion

        #region Declaration Section

        private const string TITLE = "GradientStopsPreset";

        private bool _wasOKPressed = false;

        #endregion

        #region Constructor

        public GradientStopsPreset()
        {
            InitializeComponent();

            this.DataContext = this;

            #region cboPreset

            const double WIDTH = 50;
            const double HEIGHT = WIDTH / 1.618;

            //Brush rowBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));
            //Brush borderFill = Brushes.White;
            Brush borderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("201F1F1E"));
            //Color graphFill = UtilityWPF.ColorFromHex("10262221");
            Color graphFill = Colors.Transparent;
            Color graphLine = UtilityWPF.ColorFromHex("647A5D");

            Grid.SetIsSharedSizeScope(cboPreset, true);

            foreach (Preset preset in Enum.GetValues(typeof(Preset)))
            {
                // Grid
                Grid row = new Grid()
                {
                    //Background = rowBackground,
                };
                row.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), SharedSizeGroup = "PresetText" });
                row.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "PresetGraph" });

                // Text
                TextBlock text = new TextBlock()
                {
                    Text = preset.ToString().Replace('_', ' '),
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(text, 0);
                row.Children.Add(text);

                // Graph
                Border border = new Border()
                {
                    //Background = borderFill,
                    BorderBrush = borderBrush,
                    Margin = new Thickness(6, 3, 6, 3),
                };

                Canvas canvas = new Canvas()
                {
                    Width = WIDTH,
                    Height = HEIGHT,
                };

                canvas.Children.AddRange(ForceEntry.GetGradientGraph(WIDTH, HEIGHT, GetPreset(preset, 12), graphFill, graphLine));

                border.Child = canvas;
                Grid.SetColumn(border, 1);
                row.Children.Add(border);

                // Add to combo
                cboPreset.Items.Add(row);
            }

            cboPreset.SelectedIndex = 0;

            #endregion
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

        public Tuple<double, double>[] GetSelection()
        {
            if (!_wasOKPressed)
            {
                // They hit cancel, so don't return anything
                return null;
            }

            #region Cast values

            int count;
            if (!int.TryParse(txtCount.Text, out count))
            {
                MessageBox.Show("Couldn't parse count as an integer", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            double fromDist;
            if (!double.TryParse(txtFromDistance.Text, out fromDist))
            {
                MessageBox.Show("Couldn't parse from distance as a floating point number", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            double toDist;
            if (!double.TryParse(txtToDistance.Text, out toDist))
            {
                MessageBox.Show("Couldn't parse to distance as a floating point number", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            double fromPerc;
            if (!double.TryParse(txtFromPercent.Text, out fromPerc))
            {
                MessageBox.Show("Couldn't parse from percent as a floating point number", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            double toPerc;
            if (!double.TryParse(txtToPercent.Text, out toPerc))
            {
                MessageBox.Show("Couldn't parse to percent as a floating point number", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // Preset
            Tuple<Preset, bool> preset = GetPresetType();

            #endregion

            // Make sure that from/to % matches whether the graph is going up or down
            if (preset.Item2 && toPerc < fromPerc)
            {
                MessageBox.Show("To percent must be greater than from percent for this graph type", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            else if (!preset.Item2 && toPerc > fromPerc)
            {
                MessageBox.Show("To percent must be less than from percent for this graph type", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // Get the preset with values from 0 to 1
            var unit = GetPreset(preset.Item1, count);

            // Now stretch the values
            return TransformValues(unit, fromDist, toDist, fromPerc, toPerc, preset.Item2);
        }

        #endregion

        #region Event Listeners

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // I tried many things, and that combo box refused to size itself.  I needed an event to fire after loaded, not just during
            Task.Run(() => Thread.Sleep(20)).
                ContinueWith(result =>
                {
                    //cboPreset.SelectedIndex = 0;
                    cboPreset.InvalidateVisual();
                    cboPreset.UpdateLayout();
                    this.UpdateLayout();
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            _wasOKPressed = true;

            if (this.ParentPopup != null)
            {
                this.ParentPopup.IsOpen = false;
            }
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _wasOKPressed = false;

            if (this.ParentPopup != null)
            {
                this.ParentPopup.IsOpen = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This looks at the combo box's value, and returns the enum
        /// </summary>
        /// <returns>
        /// Item1=Enum value
        /// Item2=Is graph going up
        /// </returns>
        private Tuple<Preset, bool> GetPresetType()
        {
            // Enum
            Preset? preset = null;
            Grid comboSelected = (Grid)cboPreset.SelectedItem;
            foreach (var child in comboSelected.Children)
            {
                if (child is TextBlock)
                {
                    preset = (Preset)Enum.Parse(typeof(Preset), ((TextBlock)child).Text.Replace(' ', '_'));
                }
            }

            if (preset == null)
            {
                throw new ApplicationException("Couldn't figure out which preset was selected");
            }

            // IsUp
            bool isGraphUp = true;
            switch (preset.Value)
            {
                case Preset.Cube_Down:
                case Preset.CubeRoot_Down:
                case Preset.S_Curve_Down:
                case Preset.Simple_Down:
                    isGraphUp = false;
                    break;
            }

            return Tuple.Create(preset.Value, isGraphUp);
        }

        private static Tuple<double, double>[] GetPreset(Preset preset, int count)
        {
            switch (preset)
            {
                case Preset.Simple_Up:
                    #region Simple_Up

                    return GetXs(count).
                        Select(o => Tuple.Create(o, o)).
                        ToArray();

                    #endregion
                case Preset.Simple_Down:
                    #region Simple_Down

                    return ReverseY(GetPreset(Preset.Simple_Up, count));

                    #endregion

                case Preset.Cube_Up:
                    #region Cube

                    return GetXs(count).
                        Select(o => Tuple.Create(o, o * o * o)).
                        ToArray();

                    #endregion
                case Preset.Cube_Down:
                    #region Cube_Down

                    return ReverseX(GetPreset(Preset.Cube_Up, count));

                    #endregion

                case Preset.CubeRoot_Up:
                    #region CubeRoot

                    return ReverseX(GetPreset(Preset.CubeRoot_Down, count));

                    #endregion
                case Preset.CubeRoot_Down:
                    #region CubeRoot_Down

                    return ReverseY(GetPreset(Preset.Cube_Up, count));

                    #endregion

                case Preset.S_Curve_Up:
                    #region S_Curve_Up

                    return GetXs(count).
                        Select(o => Tuple.Create(o, (-Math.Cos(o * Math.PI) * .5) + .5)).
                        ToArray();

                    #endregion
                case Preset.S_Curve_Down:
                    #region S_Curve_Down

                    return ReverseY(GetPreset(Preset.S_Curve_Up, count));

                    #endregion

                default:
                    throw new ApplicationException("Unknown Preset: " + preset.ToString());
            }
        }

        private static Tuple<double, double>[] ReverseX(Tuple<double, double>[] orig)
        {
            var retVal = new Tuple<double, double>[orig.Length];

            int down = orig.Length - 1;
            for (int up = 0; up < orig.Length; up++)
            {
                retVal[up] = Tuple.Create(orig[up].Item1, orig[down].Item2);
                down--;
            }

            return retVal;
        }
        private static Tuple<double, double>[] ReverseY(Tuple<double, double>[] orig)
        {
            return orig.
                Select(o => Tuple.Create(o.Item1, 1d - o.Item2)).
                ToArray();
        }

        private static Tuple<double, double>[] TransformValues(Tuple<double, double>[] orig, double fromDist, double toDist, double fromPerc, double toPerc, bool isUpGraph)
        {
            double percentMinRange = 0;
            double percentMaxRange = 1;
            if (!isUpGraph)
            {
                // The slope is going down, so reverse the min/max
                percentMinRange = 1;
                percentMaxRange = 0;
            }

            return orig.Select(o => Tuple.Create(
                UtilityCore.GetScaledValue(fromDist, toDist, 0, 1, o.Item1),
                UtilityCore.GetScaledValue(fromPerc, toPerc, percentMinRange, percentMaxRange, o.Item2)
                )).ToArray();
        }

        private static IEnumerable<double> GetXs(int count)
        {
            if (count < 2)
            {
                throw new ArgumentException("Count can't be less than 2");
            }

            double step = 1d / (count - 1);

            return Enumerable.Range(0, count).
                Select(o => o * step);
        }

        #endregion

        private void cboPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Cast the values.  If they don't cast, just silently exit
                double fromPerc;
                if (!double.TryParse(txtFromPercent.Text, out fromPerc))
                {
                    return;
                }

                double toPerc;
                if (!double.TryParse(txtToPercent.Text, out toPerc))
                {
                    return;
                }

                // if graph is up, make sure to% >= from% (opposite for down)
                var preset = GetPresetType();

                bool shouldSwap = (preset.Item2 && toPerc < fromPerc) ||
                                                (!preset.Item2 && toPerc > fromPerc);

                if (shouldSwap)
                {
                    // It's supposed to go up, but is going down (or the other way around).  Swap the from to boxes
                    string temp = txtFromPercent.Text;
                    txtFromPercent.Text = txtToPercent.Text;
                    txtToPercent.Text = temp;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
