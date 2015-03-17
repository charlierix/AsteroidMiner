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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;

namespace Game.Newt.Testers.ChaseForces
{
    public partial class ForceEntry : UserControl
    {
        #region Events

        public event EventHandler ValueChanged = null;

        #endregion

        #region Declaration Section

        private const string TITLE = "LinearForceEntry";

        private const string DIRECTION_DIRECTION = "Toward";
        private const string DIRECTION_VELOCITY_ANY = "Drag - velocity";
        private const string DIRECTION_VELOCITY_ALONG = "Drag - velocity along";
        private const string DIRECTION_VELOCITY_ALONGIFTOWARD = "Drag - vel along if toward";
        private const string DIRECTION_VELOCITY_ALONGIFAWAY = "Drag - vel along if away";
        private const string DIRECTION_VELOCITY_ORTH = "Drag - velocity orth";

        private readonly bool _isLinear;

        private GradientStops _gradientPopupChild = null;
        private Tuple<double, double>[] _gradient = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ForceEntry(bool isLinear)
        {
            InitializeComponent();

            _isLinear = isLinear;

            this.DataContext = this;

            #region cboDirection.Items

            //There is no way to get the xml comments of the enums, so hardcoding it all instead (not ideal, but I can't think of another way)
            //foreach (string name in Enum.GetNames(typeof(ChaseDirectionType)))
            //{
            //    cboDirection.Items.Add(name.Replace("_", " "));
            //}

            string chaseItem = _isLinear ? "point" : "direction";

            TextBlock text = new TextBlock()
            {
                Text = DIRECTION_DIRECTION,
                ToolTip = "The force is along the direction vector",
                Foreground = Brushes.Black,
            };
            cboDirection.Items.Add(text);

            text = new TextBlock()
            {
                Text = DIRECTION_VELOCITY_ANY,
                ToolTip = "Drag is applied to the entire velocity",
                Foreground = Brushes.Black,
            };
            cboDirection.Items.Add(text);

            text = new TextBlock()
            {
                Text = DIRECTION_VELOCITY_ALONG,
                ToolTip = "Drag is only applied along the part of the velocity that is along the direction to the chase " + chaseItem,
                Foreground = Brushes.Black,
            };
            cboDirection.Items.Add(text);

            text = new TextBlock()
            {
                Text = DIRECTION_VELOCITY_ALONGIFTOWARD,
                ToolTip = string.Format("Drag is only applied along the part of the velocity that is along the direction to the chase {0}.  But only if that velocity is toward the chase {0}", chaseItem),
                Foreground = Brushes.Black,
            };
            cboDirection.Items.Add(text);

            text = new TextBlock()
            {
                Text = DIRECTION_VELOCITY_ALONGIFAWAY,
                ToolTip = string.Format("Drag is only applied along the part of the velocity that is along the direction to the chase {0}.  But only if that velocity is away from chase {0}", chaseItem),
                Foreground = Brushes.Black,
            };
            cboDirection.Items.Add(text);

            text = new TextBlock()
            {
                Text = DIRECTION_VELOCITY_ORTH,
                ToolTip = "Drag is only applied along the part of the velocity that is othrogonal to the direction to the chase " + chaseItem,
                Foreground = Brushes.Black,
            };
            cboDirection.Items.Add(text);

            #endregion

            if (!_isLinear)
            {
                trkValue.Value = .1;
                trkValue.Maximum = .15;

                chkIsDistRadius.Visibility = Visibility.Collapsed;
            }

            cboDirection.SelectedIndex = 0;

            _initialized = true;

            RebuildSummaryGraphic();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is what will show when the mouse is not over the control (set up in a style template in Stylesheet.xaml)
        /// </summary>
        public FrameworkElement SummaryGraphic
        {
            get { return (FrameworkElement)GetValue(SummaryGraphicProperty); }
            set { SetValue(SummaryGraphicProperty, value); }
        }
        public static readonly DependencyProperty SummaryGraphicProperty = DependencyProperty.Register("SummaryGraphic", typeof(FrameworkElement), typeof(ForceEntry), new UIPropertyMetadata(null, SummaryGraphicPropertyChanged));

        public bool IsPopupShowing
        {
            get { return (bool)GetValue(IsPopupShowingProperty); }
            set { SetValue(IsPopupShowingProperty, value); }
        }
        public static readonly DependencyProperty IsPopupShowingProperty = DependencyProperty.Register("IsPopupShowing", typeof(bool), typeof(ForceEntry), new PropertyMetadata(false));

        #endregion

        #region Public Methods

        public ChasePoint_Force GetChaseObject_Linear()
        {
            if (!_isLinear)
            {
                throw new InvalidOperationException("This method can only be called when the control represents linear");
            }

            if (!chkEnabled.IsChecked.Value)
            {
                return null;
            }

            ChaseDirectionType direction = GetDirectionFromCombobox(cboDirection);

            return new ChasePoint_Force(
                direction,
                trkValue.Value,
                chkIsAccel.IsChecked.Value,
                chkIsSpring.IsChecked.Value,
                chkIsDistRadius.IsChecked.Value,
                _gradient);
        }
        public ChaseOrientation_Torque GetChaseObject_Orientation()
        {
            if (_isLinear)
            {
                throw new InvalidOperationException("This method can only be called when the control represents orientation");
            }

            if (!chkEnabled.IsChecked.Value)
            {
                return null;
            }

            ChaseDirectionType direction = GetDirectionFromCombobox(cboDirection);

            return new ChaseOrientation_Torque(
                direction,
                trkValue.Value,
                chkIsAccel.IsChecked.Value,
                chkIsSpring.IsChecked.Value,
                _gradient);
        }

        /// <summary>
        /// This is a helper method that builds an entry with the desired settings
        /// </summary>
        public static ForceEntry GetNewEntry_Linear(ChaseDirectionType direction, double value, bool isAccel = true, bool isSpring = false, bool isDistanceRadius = true, Tuple<double, double>[] gradient = null)
        {
            ForceEntry retVal = new ForceEntry(true);

            SetDirectionInCombobox(retVal.cboDirection, direction);

            retVal.trkValue.Value = value;
            retVal.trkValue.Maximum = value * 4;

            retVal.chkIsAccel.IsChecked = isAccel;
            retVal.chkIsSpring.IsChecked = isSpring;
            retVal.chkIsDistRadius.IsChecked = isDistanceRadius;

            retVal.StoreGradient(gradient);

            return retVal;
        }
        public static ForceEntry GetNewEntry_Orientation(ChaseDirectionType direction, double value, bool isAccel = true, bool isSpring = false, Tuple<double, double>[] gradient = null)
        {
            ForceEntry retVal = new ForceEntry(false);

            SetDirectionInCombobox(retVal.cboDirection, direction);

            retVal.trkValue.Value = value;
            retVal.trkValue.Maximum = value * 4;

            retVal.chkIsAccel.IsChecked = isAccel;
            retVal.chkIsSpring.IsChecked = isSpring;

            retVal.StoreGradient(gradient);

            return retVal;
        }

        /// <summary>
        /// This returns a visual of a graph that can be added to a canvas
        /// </summary>
        public static IEnumerable<UIElement> GetGradientGraph(double width, double height, Tuple<double, double>[] gradient, Color fill, Color stroke)
        {
            if (gradient == null || gradient.Length <= 1)       // need at least two for a gradient
            {
                return new UIElement[0];
            }
            else if (width.IsNearZero() || height.IsNearZero())
            {
                return new UIElement[0];
            }

            List<UIElement> retVal = new List<UIElement>();

            double maxPercent = gradient.Max(o => o.Item2);

            if (maxPercent > 1)
            {
                #region 100% line

                // Draw a dashed line at 1 (showing the 100% mark)
                Color color100 = UtilityWPF.AlphaBlend(UtilityWPF.AlphaBlend(stroke, Colors.Gray, .85), Colors.Transparent, .66);

                double y100 = ((maxPercent - 1) / maxPercent) * height;

                Line line100 = new Line()
                {
                    Stroke = new SolidColorBrush(color100),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new[] { 4d, 2d }),
                    X1 = 0,
                    X2 = width,
                    Y1 = y100,
                    Y2 = y100,
                };

                retVal.Add(line100);

                #endregion
            }

            if (maxPercent < 1)
            {
                // Do this so the graph is to scale (doesn't always go to the top)
                maxPercent = 1;
            }

            double lastGradX = gradient[gradient.Length - 1].Item1;
            if (!Math3D.IsNearZero(lastGradX) && lastGradX > 0)
            {
                Polyline polyLine = new Polyline();
                Polygon polyFill = new Polygon();

                polyLine.Stroke = new SolidColorBrush(stroke);
                polyLine.StrokeThickness = 2;

                polyFill.Fill = new SolidColorBrush(fill);

                //NOTE: gradient must be sorted on Item1
                double xScale = width / lastGradX;

                for (int cntr = 0; cntr < gradient.Length; cntr++)
                {
                    double x = gradient[cntr].Item1 * xScale;
                    double y = ((maxPercent - gradient[cntr].Item2) / maxPercent) * height;

                    polyLine.Points.Add(new Point(x, y));
                    polyFill.Points.Add(new Point(x, y));
                }

                // Circle back fill to make a polygon
                polyFill.Points.Add(new Point(polyFill.Points[polyFill.Points.Count - 1].X, height));
                polyFill.Points.Add(new Point(polyFill.Points[0].X, height));

                retVal.Add(polyFill);
                retVal.Add(polyLine);
            }

            return retVal;
        }

        #endregion
        #region Protected Methods

        protected virtual void OnValueChanged()
        {
            RebuildSummaryGraphic();

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Event Listeners

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawGradient();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                OnValueChanged();

                if (chkGradient.IsChecked.Value)
                {
                    pnlGradient.Visibility = Visibility.Visible;
                }
                else
                {
                    pnlGradient.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Slider_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GradientPopupChild_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawGradient();
                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GradientBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_gradientPopupChild == null)
                {
                    _gradientPopupChild = new GradientStops();
                    _gradientPopupChild.ParentPopup = popupGradient;        // doing this so it can set StaysOpen when it spawns its own child
                    _gradientPopupChild.ValueChanged += new EventHandler(GradientPopupChild_ValueChanged);

                    popupGradient.Child = _gradientPopupChild;
                }

                popupGradient.Placement = PlacementMode.Relative;
                popupGradient.PlacementTarget = canvasGradient;
                popupGradient.VerticalOffset = 0;
                popupGradient.HorizontalOffset = canvasGradient.ActualWidth + 15;

                this.IsPopupShowing = true;
                popupGradient.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PopupGradient_Closed(object sender, EventArgs e)
        {
            this.IsPopupShowing = false;
        }

        private static void SummaryGraphicPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as ForceEntry;
            if (sender != null)
            {
                sender.ThisSummaryGraphicChanged();
            }
        }
        private void ThisSummaryGraphicChanged()
        {
        }

        #endregion

        #region Private Methods

        private static ChaseDirectionType GetDirectionFromCombobox(ComboBox combo)
        {
            TextBlock selection = (TextBlock)combo.SelectedItem;

            switch (selection.Text)
            {
                case DIRECTION_DIRECTION:
                    return ChaseDirectionType.Direction;

                case DIRECTION_VELOCITY_ANY:
                    return ChaseDirectionType.Velocity_Any;

                case DIRECTION_VELOCITY_ALONG:
                    return ChaseDirectionType.Velocity_Along;

                case DIRECTION_VELOCITY_ALONGIFTOWARD:
                    return ChaseDirectionType.Velocity_AlongIfVelocityToward;

                case DIRECTION_VELOCITY_ALONGIFAWAY:
                    return ChaseDirectionType.Velocity_AlongIfVelocityAway;

                case DIRECTION_VELOCITY_ORTH:
                    return ChaseDirectionType.Velocity_Orth;

                default:
                    throw new ApplicationException("Unknown ChasePoint_DirectionType: " + selection.Text);
            }
        }
        private static void SetDirectionInCombobox(ComboBox combo, ChaseDirectionType direction)
        {
            string searchFor = null;
            switch (direction)
            {
                case ChaseDirectionType.Direction:
                    searchFor = DIRECTION_DIRECTION;
                    break;

                case ChaseDirectionType.Velocity_Along:
                    searchFor = DIRECTION_VELOCITY_ALONG;
                    break;

                case ChaseDirectionType.Velocity_AlongIfVelocityAway:
                    searchFor = DIRECTION_VELOCITY_ALONGIFAWAY;
                    break;

                case ChaseDirectionType.Velocity_AlongIfVelocityToward:
                    searchFor = DIRECTION_VELOCITY_ALONGIFTOWARD;
                    break;

                case ChaseDirectionType.Velocity_Any:
                    searchFor = DIRECTION_VELOCITY_ANY;
                    break;

                case ChaseDirectionType.Velocity_Orth:
                    searchFor = DIRECTION_VELOCITY_ORTH;
                    break;

                default:
                    throw new ApplicationException("Unknown ChasePoint_DirectionType: " + direction.ToString());
            }

            bool foundIt = false;

            for (int cntr = 0; cntr < combo.Items.Count; cntr++)
            {
                TextBlock item = (TextBlock)combo.Items[cntr];

                if (item.Text == searchFor)
                {
                    combo.SelectedIndex = cntr;
                    foundIt = true;
                    break;
                }
            }

            if (!foundIt)
            {
                throw new ApplicationException("Didn't find the direction in the combo box items: " + direction.ToString());
            }
        }

        private void StoreGradient(Tuple<double, double>[] gradient)
        {
            if (_gradientPopupChild == null)
            {
                _gradientPopupChild = new GradientStops();
                _gradientPopupChild.ParentPopup = popupGradient;        // doing this so it can set StaysOpen when it spawns its own child
                _gradientPopupChild.ValueChanged += new EventHandler(GradientPopupChild_ValueChanged);

                popupGradient.Child = _gradientPopupChild;
            }

            //NOTE: These will raise events
            chkGradient.IsChecked = gradient != null && gradient.Length > 0;
            _gradientPopupChild.StoreSelection(gradient);
        }

        private void RedrawGradient()
        {
            if (chkGradient.IsChecked.Value)
            {
                pnlGradient.Visibility = Visibility.Visible;
            }
            else
            {
                pnlGradient.Visibility = Visibility.Collapsed;
            }

            canvasGradient.Children.Clear();

            if (_gradientPopupChild == null)
            {
                _gradient = null;
            }
            else
            {
                _gradient = _gradientPopupChild.GetSelection();
            }

            if (_gradient != null && _gradient.Length > 0)
            {
                canvasGradient.Children.AddRange(GetGradientGraph(canvasGradient.ActualWidth, canvasGradient.ActualHeight, _gradient, UtilityWPF.ColorFromHex("60FFFFFF"), Colors.White));
            }
        }

        private void RebuildSummaryGraphic()
        {
            const double SUFFIXFONTSIZE = 10;

            if (!_initialized)
            {
                this.SummaryGraphic = new Rectangle() { Fill = Brushes.Red, MinWidth = 10, MinHeight = 10 };
                return;
            }

            if (!chkEnabled.IsChecked.Value)
            {
                #region Disabled

                this.SummaryGraphic = new TextBlock()
                {
                    Text = "disabled",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("70D0D0D0")),
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                #endregion
                return;
            }

            TextBlock text;

            Grid retVal = new Grid();
            retVal.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "SummaryValue" });
            retVal.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "SummarySuffix" });
            retVal.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "SummaryGradient" });
            retVal.Background = Brushes.Transparent;        // this is so the mouse over will affect the entire rectangle

            // Make these small and stack vertically
            StackPanel suffix = new StackPanel()
            {
                Margin = new Thickness(8, 0, 0, 0),
            };
            Grid.SetColumn(suffix, 1);
            retVal.Children.Add(suffix);

            #region Direction description

            //NOTE: This only shows when it's drag

            string directionText = null;

            TextBlock selection = (TextBlock)cboDirection.SelectedItem;
            switch (selection.Text)
            {
                case DIRECTION_DIRECTION:
                    break;

                case DIRECTION_VELOCITY_ANY:
                    directionText = "any";
                    break;

                case DIRECTION_VELOCITY_ALONG:
                    directionText = "along";
                    break;

                case DIRECTION_VELOCITY_ALONGIFTOWARD:
                    directionText = "along toward";
                    break;

                case DIRECTION_VELOCITY_ALONGIFAWAY:
                    directionText = "along away";
                    break;

                case DIRECTION_VELOCITY_ORTH:
                    directionText = "orth";
                    break;

                default:
                    throw new ApplicationException("Unknown ChaseDirectionType: " + selection.Text);
            }

            if (directionText != null)
            {
                text = new TextBlock()
                {
                    Text = directionText,
                    FontSize = SUFFIXFONTSIZE,
                };

                suffix.Children.Add(text);
            }

            #endregion

            #region Accel/Force

            text = new TextBlock()
            {
                Text = chkIsAccel.IsChecked.Value ? "accel" : "force",
                FontSize = SUFFIXFONTSIZE,
            };

            suffix.Children.Add(text);

            #endregion

            #region Spring

            if (chkIsSpring.IsChecked.Value)
            {
                text = new TextBlock()
                {
                    Text = "spring",
                    FontSize = SUFFIXFONTSIZE,
                };

                suffix.Children.Add(text);
            }

            #endregion

            #region Gradient

            if (chkGradient.IsChecked.Value)
            {
                double canvasWidth = 40;
                double canvasHeight = canvasWidth / 1.618;

                Canvas canvas = new Canvas()
                {
                    Width = canvasWidth,
                    Height = canvasHeight,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0),
                    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("10FFFFFF")),
                };

                //NOTE: Can't use canvas.ActualWidth yet, because it's still zero
                canvas.Children.AddRange(GetGradientGraph(canvasWidth, canvasHeight, _gradient, UtilityWPF.ColorFromHex("40FFFFFF"), UtilityWPF.ColorFromHex("A0FFFFFF")));

                Grid.SetColumn(canvas, 2);

                retVal.Children.Add(canvas);
            }

            #endregion

            #region Distance

            //TODO: Also show if gradient is used
            if (chkIsSpring.IsChecked.Value)
            {
                text = new TextBlock()
                {
                    Text = "dist " + (chkIsDistRadius.IsChecked.Value ? "radius" : "actual"),
                    FontSize = SUFFIXFONTSIZE,
                };

                suffix.Children.Add(text);
            }

            #endregion

            //NOTE: Doing this after looking at direction to know what color to use
            #region Value

            text = new TextBlock()
            {
                Text = trkValue.ValueDisplay,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Effect = new DropShadowEffect()
                {
                    Color = directionText == null ? UtilityWPF.ColorFromHex("23AC0D") : UtilityWPF.ColorFromHex("D61C1C"),      //nonnull means drag
                    Opacity = directionText == null ? 1 : .9,
                    BlurRadius = 8,
                    ShadowDepth = 3
                },
            };

            Grid.SetColumn(text, 0);
            retVal.Children.Add(text);

            #endregion

            this.SummaryGraphic = retVal;
        }

        #endregion
    }
}
