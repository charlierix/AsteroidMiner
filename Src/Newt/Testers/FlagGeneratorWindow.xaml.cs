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
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;

namespace Game.Newt.Testers
{
    public partial class FlagGeneratorWindow : Window
    {
        #region Declaration Section

        private FlagBackType _backType = FlagBackType.Solid;
        private FlagOverlayType _overlayType1 = FlagOverlayType.Border;
        private FlagOverlayType _overlayType2 = FlagOverlayType.Border;

        private readonly DropShadowEffect _selectEffect;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public FlagGeneratorWindow()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            _selectEffect = new DropShadowEffect()
            {
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 14,
                Color = UtilityWPF.ColorFromHex("0086E2"),
                Opacity = .8,
            };

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            const double WIDTH = 45;
            const double HEIGHT = 30;

            try
            {
                trkDarkness_ValueChanged(this, null);
                chkOverlay1_Checked(this, null);
                chkOverlay2_Checked(this, null);

                #region Back Buttons

                foreach (FlagBackType backType in Enum.GetValues(typeof(FlagBackType)))
                {
                    FlagProps props = new FlagProps()
                    {
                        BackType = backType,
                        Back1 = "FFF",
                        Back2 = "000",
                        Back3 = "888",
                    };

                    panelBacks.Children.Add(GetFlagButton(props, WIDTH, HEIGHT, backType));
                }

                #endregion

                #region Overlay Buttons

                foreach (FlagOverlayType overlayType in Enum.GetValues(typeof(FlagOverlayType)))
                {
                    FlagProps props = new FlagProps()
                    {
                        BackType = FlagBackType.Solid,
                        Back1 = "FFF",
                        Overlay1 = new FlagOverlay()
                        {
                            Type = overlayType,
                            Color = "000",
                        },
                    };

                    panelOverlays1.Children.Add(GetFlagButton(props, WIDTH, HEIGHT, overlayType));
                    panelOverlays2.Children.Add(GetFlagButton(props, WIDTH, HEIGHT, overlayType));
                }

                #endregion

                panelBacks.Children[0].Effect = _selectEffect;
                panelOverlays1.Children[0].Effect = _selectEffect;
                panelOverlays2.Children[0].Effect = _selectEffect;

                RedrawFlag();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawFlag();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandBack1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                txtBack1.Text = UtilityWPF.GetRandomColor(0, 192).ToHex(false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandBack2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                txtBack2.Text = UtilityWPF.GetRandomColor(0, 192).ToHex(false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandBack3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                txtBack3.Text = UtilityWPF.GetRandomColor(0, 192).ToHex(false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BackType_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!(e.OriginalSource is Rectangle))
                {
                    throw new ApplicationException("Expected a rectangle");
                }

                Rectangle senderCast = (Rectangle)e.OriginalSource;

                if (senderCast.Tag == null)
                {
                    throw new ApplicationException("Expected tag to be populated");
                }

                SelectBackType((FlagBackType)senderCast.Tag);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkOverlay1_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                Visibility visibility = chkOverlay1.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;

                grdOverlay1.Visibility = visibility;
                panelOverlays1.Visibility = visibility;

                RedrawFlag();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandOverlay1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                txtOverlay1.Text = UtilityWPF.GetRandomColor(0, 255).ToHex(false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Overlays1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!(e.OriginalSource is Rectangle))
                {
                    throw new ApplicationException("Expected a rectangle");
                }

                Rectangle senderCast = (Rectangle)e.OriginalSource;

                if (senderCast.Tag == null)
                {
                    throw new ApplicationException("Expected tag to be populated");
                }

                SelectOverlay((FlagOverlayType)senderCast.Tag, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkOverlay2_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                Visibility visibility = chkOverlay2.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;

                grdOverlay2.Visibility = visibility;
                panelOverlays2.Visibility = visibility;

                RedrawFlag();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandOverlay2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                txtOverlay2.Text = UtilityWPF.GetRandomColor(0, 255).ToHex(false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Overlays2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!(e.OriginalSource is Rectangle))
                {
                    throw new ApplicationException("Expected a rectangle");
                }

                Rectangle senderCast = (Rectangle)e.OriginalSource;

                if (senderCast.Tag == null)
                {
                    throw new ApplicationException("Expected tag to be populated");
                }

                SelectOverlay((FlagOverlayType)senderCast.Tag, 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RandOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var randOptions = FlagGenerator.GetRandomEnums();

                SelectBackType(randOptions.Item1);
                SelectOverlay(randOptions.Item2, 1);
                SelectOverlay(randOptions.Item3, 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandColors_Click(object sender, RoutedEventArgs e)
        {
            ColorHSV[] colors = FlagGenerator.GetRandomColors();

            txtBack1.Text = colors[0].ToRGB().ToHex(false, false);
            txtBack2.Text = colors[1].ToRGB().ToHex(false, false);
            txtBack3.Text = colors[2].ToRGB().ToHex(false, false);
            txtOverlay1.Text = colors[3].ToRGB().ToHex(false, false);
            txtOverlay2.Text = colors[4].ToRGB().ToHex(false, false);
        }

        private void ColorCategories_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                canvasDebug.Children.Clear();

                double width = canvasDebug.ActualWidth;
                double height = canvasDebug.ActualHeight;

                // Reverse so the top priority is drawn first
                foreach (var category in new FlagGenerator.FlagColorCategoriesCache().Categories.Reverse())
                {
                    Rectangle rect = new Rectangle()
                    {
                        Width = category.Item1.Width / 100 * width,
                        Height = category.Item1.Height / 100 * height,
                        Fill = new SolidColorBrush(UtilityWPF.GetRandomColor(0, 255)),
                        ToolTip = category.Item2.ToString(),
                    };

                    rect.MouseDown += new MouseButtonEventHandler((s, b) =>
                    {
                        FlagGenerator.FlagColorCategory cat = UtilityCore.EnumParse<FlagGenerator.FlagColorCategory>(((Rectangle)s).ToolTip.ToString());
                        double? hue = b.LeftButton == MouseButtonState.Pressed ? (double?)null : StaticRandom.NextDouble(360);
                        DrawCategory(cat, hue);
                    });

                    Canvas.SetLeft(rect, category.Item1.X / 100 * width);
                    double top = height - ((category.Item1.Y + category.Item1.Height) / 100 * height);
                    Canvas.SetTop(rect, top);

                    canvasDebug.Children.Add(rect);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkDarkness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                byte rgb = Convert.ToByte(Math.Round(trkDarkness.Value * 255));
                grdFlags.Background = new SolidColorBrush(Color.FromRgb(rgb, rgb, rgb));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void SelectBackType(FlagBackType backType)
        {
            _backType = backType;

            foreach (Grid button in panelBacks.Children)
            {
                foreach (UIElement buttonChild in button.Children)
                {
                    if (buttonChild is Rectangle)
                    {
                        if ((FlagBackType)((Rectangle)buttonChild).Tag == backType)
                        {
                            button.Effect = _selectEffect;
                        }
                        else
                        {
                            button.Effect = null;
                        }

                        break;
                    }
                }
            }

            RedrawFlag();
        }
        private void SelectOverlay(FlagOverlayType? overlayType, int which)
        {
            // Figure out which one to change
            UniformGrid panel;
            switch (which)
            {
                case 1:
                    panel = panelOverlays1;
                    chkOverlay1.IsChecked = overlayType != null;
                    _overlayType1 = overlayType ?? _overlayType1;
                    break;

                case 2:
                    panel = panelOverlays2;
                    chkOverlay2.IsChecked = overlayType != null;
                    _overlayType2 = overlayType ?? _overlayType2;
                    break;

                default:
                    throw new ApplicationException("Unknown overlay: " + which.ToString());
            }

            // Set the effect
            foreach (Grid button in panel.Children)
            {
                foreach (UIElement buttonChild in button.Children)
                {
                    if (buttonChild is Rectangle)
                    {
                        if (overlayType != null && (FlagOverlayType)((Rectangle)buttonChild).Tag == overlayType.Value)
                        {
                            button.Effect = _selectEffect;
                        }
                        else
                        {
                            button.Effect = null;
                        }

                        break;
                    }
                }
            }

            RedrawFlag();
        }

        private void RedrawFlag()
        {
            canvasDebug.Children.Clear();
            canvasSmall.Children.Clear();
            canvasMed.Children.Clear();
            canvasLarge.Children.Clear();
            lblError.Text = "";
            lblError.Visibility = Visibility.Collapsed;

            try
            {
                FlagProps props = new FlagProps()
                {
                    BackType = _backType,
                    Back1 = txtBack1.Text,
                    Back2 = txtBack2.Text,
                    Back3 = txtBack3.Text,
                };

                if (chkOverlay1.IsChecked.Value)
                {
                    props.Overlay1 = new FlagOverlay()
                    {
                        Type = _overlayType1,
                        Color = txtOverlay1.Text,
                    };
                }

                if (chkOverlay2.IsChecked.Value)
                {
                    props.Overlay2 = new FlagOverlay()
                    {
                        Type = _overlayType2,
                        Color = txtOverlay2.Text,
                    };
                }

                canvasSmall.Children.Add(new FlagVisual(canvasSmall.ActualWidth, canvasSmall.Height, props));
                canvasMed.Children.Add(new FlagVisual(canvasMed.ActualWidth, canvasMed.Height, props));
                canvasLarge.Children.Add(new FlagVisual(canvasLarge.ActualWidth, canvasLarge.Height, props));
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                lblError.Visibility = Visibility.Visible;
            }
        }

        private void DrawCategory(FlagGenerator.FlagColorCategory matchCategory, double? hue)
        {
            canvasDebug.Children.Clear();

            double width = canvasDebug.ActualWidth;
            double height = canvasDebug.ActualHeight;

            int size = 20;

            int count = Convert.ToInt32((Math.Ceiling(width / size) * Math.Ceiling(height / size)));

            FlagGenerator.FlagColorCategoriesCache cache = new FlagGenerator.FlagColorCategoriesCache();

            Color[] colors = Enumerable.Range(0, count).
                AsParallel().
                Select(o => cache.GetRandomColor(matchCategory, hue).ToRGB()).
                ToArray();

            int index = 0;

            for (int x = 0; x < width; x += size)
            {
                for (int y = 0; y < height; y += size)
                {
                    Rectangle rect = new Rectangle()
                    {
                        Width = size,
                        Height = size,
                        Fill = new SolidColorBrush(colors[index]),
                        ToolTip = colors[index].ToHex(false, false),
                    };

                    rect.MouseDown += new MouseButtonEventHandler((s, b) => { Clipboard.SetText(((Rectangle)s).ToolTip.ToString()); });

                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);

                    canvasDebug.Children.Add(rect);

                    index++;
                }
            }
        }

        private static UIElement GetFlagButton(FlagProps props, double width, double height, Enum enumValue)
        {
            //NOTE: I couldn't get mouse events to fire.  Finally, I put a transparent rectangle over the flag visual, and that works
            Grid retVal = new Grid() { Margin = new Thickness(3) };

            retVal.Children.Add(new FlagVisual(width, height, props));

            retVal.Children.Add(new Rectangle() { Width = width, Height = height, Fill = Brushes.Transparent, ToolTip = enumValue.ToString(), Tag = enumValue });        // store it in the tag to make things easier

            return retVal;
        }

        #endregion
    }
}
