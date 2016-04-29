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
using Game.Newt.v2.GameItems.Collections;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.GameItems.Controls
{
    public partial class BrainRGBRecognizerViewer : UserControl
    {
        #region Declaration Section

        private readonly DispatcherTimer _timer;

        private SOMResult _currentSom = null;
        private Tuple<BrainRGBRecognizer.TrainerInput, BrainRGBRecognizer.TrainerInput> _currentTrainingData = null;

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

                bool isColor = _recognizer.IsColor;

                #endregion

                #region latest image

                double[] image = _recognizer.LatestImage;
                if (image == null)
                {
                    canvasPixels.Source = null;
                }
                else
                {
                    if (isColor)
                    {
                        canvasPixels.Source = UtilityWPF.GetBitmap_RGB(image, cameraWidthHeight.Item1, cameraWidthHeight.Item2);
                    }
                    else
                    {
                        canvasPixels.Source = UtilityWPF.GetBitmap(image, cameraWidthHeight.Item1, cameraWidthHeight.Item2);
                    }
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
                    SelfOrganizingMapsWPF.ShowResults2D_Tiled(panelSOM, som, cameraWidthHeight.Item1, cameraWidthHeight.Item2, DrawSOMTile);
                    _currentSom = som;
                    panelSOM.Visibility = Visibility.Visible;
                }

                #endregion

                #region training data

                var trainingData = _recognizer.TrainingData;

                if (trainingData == null || trainingData.Item1 == null || trainingData.Item1.ImportantEvents == null || trainingData.Item1.ImportantEvents.Length == 0)
                {
                    _currentTrainingData = null;
                    panelTrainingData.Child = null;
                    panelTrainingData.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (!IsSame(_currentTrainingData, trainingData))
                    {
                        _currentTrainingData = trainingData;
                        DrawTrainingData(panelTrainingData, trainingData);
                        panelTrainingData.Visibility = Visibility.Visible;
                    }
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

            if (cast.Source.Original.Length == e.TileWidth * e.TileHeight)
            {
                DrawSOMTile_Gray(e, cast.Source.Original);
            }
            else if (cast.Source.Original.Length == e.TileWidth * e.TileHeight * 3)
            {
                DrawSOMTile_Color(e, cast.Source.Original);
            }
            else
            {
                throw new ApplicationException("The original image isn't the expected size");
            }
        }
        private static void DrawSOMTile_Color(SelfOrganizingMapsWPF.DrawTileArgs e, double[] source)
        {
            //NOTE: Copied from UtilityWPF.GetBitmap_RGB

            for (int y = 0; y < e.TileHeight; y++)
            {
                int offsetImageY = (e.ImageY + y) * e.Stride;
                int offsetSourceY = y * e.TileWidth * 3;

                for (int x = 0; x < e.TileWidth; x++)
                {
                    int indexImage = offsetImageY + ((e.ImageX + x) * e.PixelWidth);
                    int indexSource = offsetSourceY + (x * 3);

                    byte r = (source[indexSource + 0] * 255).ToByte_Round();
                    byte g = (source[indexSource + 1] * 255).ToByte_Round();
                    byte b = (source[indexSource + 2] * 255).ToByte_Round();

                    e.BitmapPixelBytes[indexImage + 3] = 255;
                    e.BitmapPixelBytes[indexImage + 2] = r;
                    e.BitmapPixelBytes[indexImage + 1] = g;
                    e.BitmapPixelBytes[indexImage + 0] = b;
                }
            }
        }
        private static void DrawSOMTile_Gray(SelfOrganizingMapsWPF.DrawTileArgs e, double[] source)
        {
            //NOTE: Copied from UtilityWPF.GetBitmap

            for (int y = 0; y < e.TileHeight; y++)
            {
                int offsetImageY = (e.ImageY + y) * e.Stride;
                int offsetSourceY = y * e.TileWidth;

                for (int x = 0; x < e.TileWidth; x++)
                {
                    int indexImage = offsetImageY + ((e.ImageX + x) * e.PixelWidth);

                    byte gray = (source[offsetSourceY + x] * 255).ToByte_Round();

                    e.BitmapPixelBytes[indexImage + 3] = 255;
                    e.BitmapPixelBytes[indexImage + 2] = gray;
                    e.BitmapPixelBytes[indexImage + 1] = gray;
                    e.BitmapPixelBytes[indexImage + 0] = gray;
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

        private static void DrawTrainingData(Border border, Tuple<BrainRGBRecognizer.TrainerInput, BrainRGBRecognizer.TrainerInput> trainingData)
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(6) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            DrawTrainingData_Heading(grid, "raw inputs");
            DrawTrainingData_Set(grid, trainingData.Item1);

            DrawTrainingData_Heading(grid, "normalized inputs");
            DrawTrainingData_Set(grid, trainingData.Item2);

            border.Child = grid;
        }
        private static void DrawTrainingData_Heading(Grid grid, string text)
        {
            if (grid.RowDefinitions.Count > 0)
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(6) });
            }
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            TextBlock textblock = new TextBlock()
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 10,
            };

            Grid.SetColumn(textblock, 0);
            Grid.SetColumnSpan(textblock, 3);
            Grid.SetRow(textblock, grid.RowDefinitions.Count - 1);

            grid.Children.Add(textblock);
        }
        private static void DrawTrainingData_Set(Grid grid, BrainRGBRecognizer.TrainerInput trainingData)
        {
            var byType = trainingData.ImportantEvents.
                ToLookup(o => o.Item1.Type).
                OrderBy(o => o.Key.ToString()).
                ToArray();

            foreach (var typeSet in byType)
            {
                IEnumerable<double[]> examples = typeSet.
                    OrderBy(o => o.Item1.Time).     // may want to order descending by strength
                    Select(o => o.Item2);

                DrawTrainingData_Classification(grid, typeSet.Key.ToString(), examples, trainingData.Width, trainingData.Height, trainingData.IsColor);
            }

            if (trainingData.UnimportantEvents.Length > 0)
            {
                DrawTrainingData_Classification(grid, "<nothing>", trainingData.UnimportantEvents, trainingData.Width, trainingData.Height, trainingData.IsColor);
            }
        }
        private static void DrawTrainingData_Classification(Grid grid, string heading, IEnumerable<double[]> examples, int width, int height, bool isColor)
        {
            if (grid.RowDefinitions.Count > 0)
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(6) });
            }
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            #region heading

            // Heading (just reusing the progress bar header to get a consistent look)
            FrameworkElement header = GetNNOutputBar(heading, null);

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

            foreach (var example in examples)
            {
                BitmapSource source;
                if (isColor)
                {
                    source = UtilityWPF.GetBitmap_RGB(example, width, height);
                }
                else
                {
                    source = UtilityWPF.GetBitmap(example, width, height);
                }

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

        private static bool IsSame(Tuple<BrainRGBRecognizer.TrainerInput, BrainRGBRecognizer.TrainerInput> data1, Tuple<BrainRGBRecognizer.TrainerInput, BrainRGBRecognizer.TrainerInput> data2)
        {
            if (data1 == null && data2 == null)
            {
                return true;
            }
            else if (data1 == null || data2 == null)
            {
                return false;
            }

            return IsSame(data1.Item1, data2.Item1) &&      // compare raw inputs
                IsSame(data1.Item2, data2.Item2);       // compare normalized inputs
        }
        private static bool IsSame(BrainRGBRecognizer.TrainerInput data1, BrainRGBRecognizer.TrainerInput data2)
        {
            if (data1 == null && data2 == null)
            {
                return true;
            }
            else if (data1 == null || data2 == null)
            {
                return false;
            }

            return data1.Token == data2.Token;      // I decided to just add a token

            //if (data1.Width != data2.Width || data1.Height != data2.Height)
            //{
            //    return false;
            //}
            //else if (data1.ImportantEvents.Length != data2.ImportantEvents.Length)
            //{
            //    return false;
            //}

            //// Don't want to compare every pixel
            //int step = 13;
            //if (data1.Width % step == 0)
            //{
            //    step = 11;      // it's not a big deal if step is a multiple of width.  I would just prefer the samples to not all be in the same column
            //}

            //for (int cntr = 0; cntr < data1.ImportantEvents.Length; cntr++)
            //{
            //    if (!IsSame(data1.ImportantEvents[cntr].Item1, data2.ImportantEvents[cntr].Item1))
            //    {
            //        return false;
            //    }
            //    else if (!IsSame(data1.ImportantEvents[cntr].Item2, data2.ImportantEvents[cntr].Item2, step))
            //    {
            //        return false;
            //    }
            //}

            //return true;
        }
        private static bool IsSame(LifeEventVectorArgs args1, LifeEventVectorArgs args2)
        {
            if (args1 == null && args2 == null)
            {
                return true;
            }
            else if (args1 == null || args2 == null)
            {
                return false;
            }

            if (args1.Type != args2.Type)
            {
                return false;
            }
            else if (args1.Time != args2.Time)
            {
                return false;
            }
            else if (args1.Strength != args2.Strength)
            {
                return false;
            }
            else if (!IsSame(args1.Vector, args2.Vector))       // the vectors are small, so just compare all elements
            {
                return false;
            }

            return true;
        }
        private static bool IsSame(double[] array1, double[] array2, int step = 1)
        {
            if (array1 == null && array2 == null)
            {
                return true;
            }
            else if (array1 == null || array2 == null)
            {
                return false;
            }
            else if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < array1.Length; cntr += step)
            {
                if (array1[cntr] != array2[cntr])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
