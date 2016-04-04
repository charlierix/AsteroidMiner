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
using System.Windows.Threading;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.GameItems.Controls
{
    public partial class BrainRGBRecognizerViewer : UserControl
    {
        #region Declaration Section

        private readonly DispatcherTimer _timer;

        private SOMResult _currentSom = null;

        #endregion

        #region Constructor

        public BrainRGBRecognizerViewer()
        {
            InitializeComponent();

            // This doesn't start until they pass the recognizer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += Timer_Tick;
        }

        #endregion

        //TODO: Expose some color properties

        #region Public Properties

        private BrainRGBRecognizer _recognizer = null;
        public BrainRGBRecognizer Recognizer
        {
            get
            {
                return _recognizer;
            }
            set
            {
                _recognizer = value;

                if (_recognizer == null)
                {
                    ClearVisuals();
                    _timer.Stop();
                }
                else
                {
                    //TODO: Set up the visuals
                    _timer.Start();
                }
            }
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                #region prep

                lblError.Text = "";
                lblError.Visibility = Visibility.Collapsed;

                if (_recognizer == null)
                {
                    _timer.Stop();
                    ClearVisuals("no recognizer set");
                }

                if (!_recognizer.IsOn)
                {
                    ClearVisuals("Powered Off");
                    return;
                }

                Tuple<int, int> cameraWidthHeight = _recognizer.CameraWidthHeight;
                if (cameraWidthHeight == null)
                {
                    ClearVisuals("recognizer's camera not set");
                    return;
                }

                #endregion

                #region latest image

                double[] image = _recognizer.LatestImage;
                if (image == null)
                {
                    canvasPixels.Source = null;
                }
                else
                {
                    canvasPixels.Source = UtilityWPF.GetBitmap(image, cameraWidthHeight.Item1, cameraWidthHeight.Item2);
                }

                #endregion

                #region nn outputs

                Tuple<LifeEventType, double>[] nnOutputs = _recognizer.CurrentOutput;

                panelOutputs.Children.Clear();

                if (nnOutputs != null)
                {
                    DrawNNOutputs(panelOutputs, nnOutputs);
                }

                #endregion

                #region som

                SOMResult som = _recognizer.SOM;
                bool shouldRenderSOM = false;

                if (som == null)
                {
                    panelSOM.Visibility = Visibility.Collapsed;
                    panelSOM.Child = null;
                    _currentSom = null;
                }
                else if (som != null)
                {
                    shouldRenderSOM = true;

                    if (_currentSom != null && som.Nodes.Length == _currentSom.Nodes.Length && som.Nodes.Length > 0 && som.Nodes[0].Token == _currentSom.Nodes[0].Token)
                    {
                        shouldRenderSOM = false;
                    }
                }

                if (shouldRenderSOM)
                {
                    //SelfOrganizingMapsWPF.ShowResults2D_Tiled(panelSOM, som, 16, 16, cameraWidthHeight.Item1, cameraWidthHeight.Item2);
                    SelfOrganizingMapsWPF.ShowResults2D_Tiled(panelSOM, som, cameraWidthHeight.Item1, cameraWidthHeight.Item2, DrawSOMTile);
                    _currentSom = som;
                    panelSOM.Visibility = Visibility.Visible;
                }

                #endregion

                #region training data

                BrainRGBRecognizer.TrainerInput trainingData = _recognizer.TrainingData;

                if (trainingData == null || trainingData.ImportantEvents == null || trainingData.ImportantEvents.Length == 0)
                {
                    panelTrainingData.Child = null;
                    panelTrainingData.Visibility = Visibility.Collapsed;
                }
                else
                {
                    DrawTrainingData(panelTrainingData, trainingData);
                    panelTrainingData.Visibility = Visibility.Visible;
                }

                #endregion
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                lblError.Visibility = Visibility.Visible;
            }
        }

        private static void DrawSOMTile(SelfOrganizingMapsWPF.DrawTileArgs e)
        {
            SOMInput<SOMList.SOMItem> cast = e.Tile as SOMInput<SOMList.SOMItem>;
            if (cast == null)
            {
                throw new InvalidCastException("Expected SOMInput<SOMList.SOMItem>: " + e.Tile == null ? "<null>" : e.Tile.GetType().ToString());
            }
            else if (cast.Source == null)
            {
                throw new ApplicationException("cast.Source is null");
            }
            else if (cast.Source.Original == null)
            {
                throw new ApplicationException("The original image shouldn't be null");
            }
            else if (cast.Source.Original.Length != e.TileWidth * e.TileHeight)
            {
                throw new ApplicationException("The original image isn't the expected size");
            }

            //NOTE: Copied from UtilityWPF.GetBitmap

            for (int y = 0; y < e.TileHeight; y++)
            {
                int offsetImageY = (e.ImageY + y) * e.Stride;
                int offsetTileY = y * e.TileWidth;

                for (int x = 0; x < e.TileWidth; x++)
                {
                    int indexImage = offsetImageY + ((e.ImageX + x) * e.PixelWidth);

                    int gray = (cast.Source.Original[offsetTileY + x] * 255).ToInt_Round();
                    if (gray < 0) gray = 0;
                    if (gray > 255) gray = 255;

                    byte grayByte = Convert.ToByte(gray);

                    e.BitmapPixelBytes[indexImage + 3] = 255;
                    e.BitmapPixelBytes[indexImage + 2] = grayByte;
                    e.BitmapPixelBytes[indexImage + 1] = grayByte;
                    e.BitmapPixelBytes[indexImage + 0] = grayByte;
                }
            }
        }

        #endregion

        #region Private Methods

        private void ClearVisuals(string errorMessage = null)
        {
            canvasPixels.Source = null;

            panelSOM.Child = null;
            panelSOM.Visibility = Visibility.Collapsed;

            panelTrainingData.Child = null;
            panelTrainingData.Visibility = Visibility.Collapsed;

            if (errorMessage != null)
            {
                lblError.Text = errorMessage;
                lblError.Visibility = Visibility.Visible;
            }
        }

        private static void DrawNNOutputs(UniformGrid panel, Tuple<LifeEventType, double>[] outputs)
        {
            // Keep them sorted so that it's consistent frame to frame (make sure they don't jump around)
            outputs = outputs.
                OrderBy(o => o.Item1.ToString()).
                ToArray();

            foreach (var nnout in outputs)
            {
                #region OLD

                //Grid grid = new Grid()
                //{
                //    Margin = new Thickness(2),
                //};
                //grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(nnout.Item2, GridUnitType.Star) });
                //grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1 - nnout.Item2, GridUnitType.Star) });

                //// Filled %
                //Border border = new Border()
                //{
                //    Background = Brushes.DarkTurquoise,
                //    CornerRadius = new CornerRadius(0),
                //    HorizontalAlignment = HorizontalAlignment.Stretch,
                //    VerticalAlignment = VerticalAlignment.Stretch,
                //};
                //Grid.SetColumn(border, 0);
                //grid.Children.Add(border);

                //// Remainder
                //border = new Border()
                //{
                //    Background = Brushes.Teal,
                //    CornerRadius = new CornerRadius(0),
                //    HorizontalAlignment = HorizontalAlignment.Stretch,
                //    VerticalAlignment = VerticalAlignment.Stretch,
                //};
                //Grid.SetColumn(border, 1);
                //grid.Children.Add(border);

                //// Outline
                //border = new Border()
                //{
                //    BorderBrush = Brushes.Coral,
                //    BorderThickness = new Thickness(1),
                //    CornerRadius = new CornerRadius(0),
                //    HorizontalAlignment = HorizontalAlignment.Stretch,
                //    VerticalAlignment = VerticalAlignment.Stretch,
                //};
                //Grid.SetColumn(border, 0);
                //Grid.SetColumnSpan(border, 2);
                //grid.Children.Add(border);

                //// Text
                //TextBlock text = new TextBlock()
                //{
                //    Text = string.Format("{0} {1}", nnout.Item1, (nnout.Item2 * 100).ToInt_Round()),
                //    Padding = new Thickness(6, 4, 6, 4),
                //    HorizontalAlignment = HorizontalAlignment.Center,
                //    VerticalAlignment = VerticalAlignment.Center,
                //};
                //Grid.SetColumn(text, 0);
                //Grid.SetColumnSpan(text, 2);
                //grid.Children.Add(text);

                #endregion

                panel.Children.Add(GetNNOutputBar(nnout.Item1.ToString(), nnout.Item2));
            }
        }

        private static FrameworkElement GetNNOutputBar(string name, double? percent)
        {
            Grid grid = new Grid()
            {
                Margin = new Thickness(2),
            };

            if (percent == null || percent.Value == 0d)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });        // 0 pixel seems to mess it up
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }
            else
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(percent.Value, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1 - percent.Value, GridUnitType.Star) });
            }

            // Filled %
            Border border = new Border()
            {
                Background = Brushes.DarkTurquoise,
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(border, 0);
            grid.Children.Add(border);

            // Remainder
            border = new Border()
            {
                Background = Brushes.Teal,
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(border, 1);
            grid.Children.Add(border);

            // Outline
            border = new Border()
            {
                BorderBrush = Brushes.Coral,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(border, 0);
            Grid.SetColumnSpan(border, 2);
            grid.Children.Add(border);

            // Text
            string caption = name;
            if (percent != null)
            {
                name += " " + (percent.Value * 100).ToInt_Round().ToString();
            }

            TextBlock text = new TextBlock()
            {
                Text = caption,
                Padding = new Thickness(6, 4, 6, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(text, 0);
            Grid.SetColumnSpan(text, 2);
            grid.Children.Add(text);

            return grid;
        }

        private static void DrawTrainingData(Border border, BrainRGBRecognizer.TrainerInput trainingData)
        {
            var byType = trainingData.ImportantEvents.
                ToLookup(o => o.Item1.Type).
                OrderBy(o => o.Key.ToString()).
                ToArray();

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(6) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            foreach (var typeSet in byType)
            {
                if (grid.RowDefinitions.Count > 0)
                {
                    grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(6) });
                }
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                #region heading

                // Heading (just reusing the progress bar header to get a consistent look)
                FrameworkElement header = GetNNOutputBar(typeSet.Key.ToString(), null);

                //header.LayoutTransform = new RotateTransform(-90);        // this just takes up too much vertical space

                Grid.SetColumn(header, 0);
                Grid.SetRow(header, grid.RowDefinitions.Count - 1);
                grid.Children.Add(header);

                #endregion

                #region examples

                //TODO: May want to use a lighter weight way of drawing these

                WrapPanel examplePanel = new WrapPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                foreach (var example in typeSet.OrderBy(o => o.Item1.Time))     // may want to order descending by strength
                {
                    BitmapSource source = UtilityWPF.GetBitmap(example.Item2, trainingData.Width, trainingData.Height);
                    Image image = new Image()
                    {
                        Source = source,
                        Width = source.PixelWidth * 2,      // if this isn't set, the image will take up all of the width, and be huge
                        Height = source.PixelHeight * 2,
                    };

                    examplePanel.Children.Add(image);
                }

                Grid.SetColumn(examplePanel, 2);
                Grid.SetRow(examplePanel, grid.RowDefinitions.Count - 1);
                grid.Children.Add(examplePanel);

                #endregion
            }

            border.Child = grid;
        }

        #endregion
    }
}
