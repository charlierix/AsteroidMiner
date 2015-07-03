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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.Convolution
{
    //TODO: This has too much logic copied from the ImageFilters window.  Make a custom control for the kernel wrap panel.  Support dragdrop, rightclick, selected event
    public partial class CompositeFilter : Window
    {
        #region Events

        public event EventHandler<ConvolutionSet2D> SaveRequested = null;

        #endregion

        #region Declaration Section

        private readonly string DATAFORMAT_MYKERNEL = "data format - CompositeFilter - kernel - " + Guid.NewGuid().ToString();      // this guid will make sure it can't dragdrop between composite windows

        private List<ConvolutionBase2D> _kernels = new List<ConvolutionBase2D>();
        private int _selectedKernelIndex = -1;

        private Tuple<Border, int> _mouseDownOn = null;

        private readonly DropShadowEffect _selectEffect;

        private readonly ContextMenu _kernelContextMenu;

        private bool _isProgramaticallyChanging = false;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public CompositeFilter(ConvolutionSet2D set = null)
        {
            InitializeComponent();

            // Selected effect
            _selectEffect = new DropShadowEffect()
            {
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 40,
                Color = UtilityWPF.ColorFromHex("FFEB85"),
                Opacity = 1,
            };

            // Context Menu
            _kernelContextMenu = (ContextMenu)this.Resources["kernelContextMenu"];

            // Combobox
            foreach (SetOperationType operation in Enum.GetValues(typeof(SetOperationType)))
            {
                cboPostOperation.Items.Add(operation);
            }
            cboPostOperation.SelectedIndex = 0;

            // Load set passed in
            if (set != null)
            {
                foreach (var child in set.Convolutions)
                {
                    InsertKernel(child);
                }

                cboPostOperation.SelectedValue = set.OperationType;

                lblInstructions.Visibility = Visibility.Collapsed;
            }

            _initialized = true;

            RefreshStatsPanel();
        }

        #endregion

        #region Event Listeners

        private void panel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                _mouseDownOn = null;

                Tuple<Border, int> clickedCtrl = GetSelectedKernel(e.OriginalSource);
                if (clickedCtrl == null)
                {
                    return;
                }

                _mouseDownOn = clickedCtrl;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void panel_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_mouseDownOn == null)
                {
                    return;
                }

                // Start Drag
                ImageFilters.DragDataObject dragObject = new ImageFilters.DragDataObject(_mouseDownOn.Item2, _mouseDownOn.Item1, _kernels[_mouseDownOn.Item2]);
                DataObject dragData = new DataObject(DATAFORMAT_MYKERNEL, dragObject);
                DragDrop.DoDragDrop(_mouseDownOn.Item1, dragData, DragDropEffects.Move);

                _mouseDownOn = null;        // this way another drag won't start until they release and reclick
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void panel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mouseDownOn = null;

                Tuple<Border, int> clickedCtrl = GetSelectedKernel(e.OriginalSource);
                if (clickedCtrl == null)
                {
                    return;
                }

                SelectKernel(clickedCtrl.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void panel_PreviewDragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DATAFORMAT_MYKERNEL))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else if (e.Data.GetDataPresent(ImageFilters.DATAFORMAT_KERNEL))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void panel_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DATAFORMAT_MYKERNEL))
                {
                    #region Rearrange

                    lblInstructions.Visibility = System.Windows.Visibility.Collapsed;

                    var dropped = (ImageFilters.DragDataObject)e.Data.GetData(DATAFORMAT_MYKERNEL);

                    int insertIndex = ImageFilters.GetDropInsertIndex(panel, e, true);
                    if (insertIndex < 0)
                    {
                        return;
                    }

                    if (insertIndex >= dropped.Index)
                    {
                        insertIndex--;
                    }

                    panel.Children.RemoveAt(dropped.Index);
                    _kernels.RemoveAt(dropped.Index);

                    panel.Children.Insert(insertIndex, dropped.Control);
                    _kernels.Insert(insertIndex, dropped.Kernel);

                    SelectKernel(insertIndex);

                    #endregion
                }
                else if (e.Data.GetDataPresent(ImageFilters.DATAFORMAT_KERNEL))
                {
                    #region From main

                    lblInstructions.Visibility = System.Windows.Visibility.Collapsed;

                    var dropped = (ImageFilters.DragDataObject)e.Data.GetData(ImageFilters.DATAFORMAT_KERNEL);

                    int insertIndex = ImageFilters.GetDropInsertIndex(panel, e, true);
                    if (insertIndex < 0)
                    {
                        return;
                    }

                    InsertKernel(dropped.Kernel, insertIndex);

                    SelectKernel(insertIndex);

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkGain_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChanging || _selectedKernelIndex < 0)
                {
                    return;
                }

                Convolution2D selected = _kernels[_selectedKernelIndex] as Convolution2D;
                if (selected == null)
                {
                    return;
                }

                Convolution2D changed = new Convolution2D(selected.Values, selected.Width, selected.Height, selected.IsNegPos, trkGain.Value, selected.Iterations, selected.ExpandBorder);

                _kernels[_selectedKernelIndex] = changed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkIterations_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized || _isProgramaticallyChanging || _selectedKernelIndex < 0)
                {
                    return;
                }

                Convolution2D selected = _kernels[_selectedKernelIndex] as Convolution2D;
                if (selected == null)
                {
                    return;
                }

                Convolution2D changed = new Convolution2D(selected.Values, selected.Width, selected.Height, selected.IsNegPos, selected.Gain, Convert.ToInt32(trkIterations.Value), selected.ExpandBorder);

                _kernels[_selectedKernelIndex] = changed;

                RefreshStatsPanel();        // iterations affects the stats
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KernelDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> kernel = GetSelectedKernel(_kernelContextMenu.PlacementTarget);
                if (kernel == null)
                {
                    MessageBox.Show("Couldn't identify filter", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                panel.Children.Remove(kernel.Item1);
                _kernels.RemoveAt(kernel.Item2);

                if (_selectedKernelIndex == kernel.Item2)
                {
                    _selectedKernelIndex = -1;
                }
                else if (_selectedKernelIndex > kernel.Item2)
                {
                    _selectedKernelIndex--;
                }

                SelectKernel(_selectedKernelIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.SaveRequested == null)
                {
                    MessageBox.Show("There is no listener to the save event", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_kernels.Count == 0)
                {
                    MessageBox.Show("You need to add kernels before saving", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SetOperationType operation = (SetOperationType)cboPostOperation.SelectedValue;

                if (operation == SetOperationType.MaxOf)
                {
                    if (_kernels.Count < 2)
                    {
                        MessageBox.Show("MaxOf needs at least two children", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Tuple<int, int> firstReduce = _kernels[0].GetReduction();

                    for (int cntr = 1; cntr < _kernels.Count; cntr++)
                    {
                        Tuple<int, int> nextReduce = _kernels[cntr].GetReduction();

                        if (firstReduce.Item1 != nextReduce.Item1 || firstReduce.Item2 != nextReduce.Item2)
                        {
                            MessageBox.Show("When the operation is MaxOf, then all kernels must reduce the same amount", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                ConvolutionSet2D set = new ConvolutionSet2D(_kernels.ToArray(), operation);

                this.SaveRequested(this, set);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void InsertKernel(ConvolutionBase2D kernel, int index = -1)
        {
            Border border = Convolutions.GetKernelThumbnail(kernel, 80, _kernelContextMenu);

            if (index < 0)
            {
                panel.Children.Add(border);
                _kernels.Add(kernel);
            }
            else
            {
                panel.Children.Insert(index, border);
                _kernels.Insert(index, kernel);
            }
        }

        private void SelectKernel(int index)
        {
            #region Set the effect

            int childIndex = 0;
            foreach (UIElement child in panel.Children)
            {
                if (childIndex == index)
                {
                    child.Effect = _selectEffect;
                }
                else
                {
                    child.Effect = null;
                }

                childIndex++;
            }

            #endregion

            _selectedKernelIndex = index;

            #region Update left panel

            if (_selectedKernelIndex >= 0 && _selectedKernelIndex < _kernels.Count && _kernels[_selectedKernelIndex] is Convolution2D)
            {
                _isProgramaticallyChanging = true;

                Convolution2D selectedSingle = (Convolution2D)_kernels[_selectedKernelIndex];

                trkGain.Value = selectedSingle.Gain;
                trkIterations.Value = selectedSingle.Iterations;

                grdSelectedSingle.Visibility = Visibility.Visible;

                _isProgramaticallyChanging = false;
            }
            else
            {
                grdSelectedSingle.Visibility = Visibility.Hidden;
            }

            #endregion

            // Stats panel
            RefreshStatsPanel();
        }

        private Tuple<Border, int> GetSelectedKernel(object source)
        {
            Border clickedCtrl;

            if (source is Border)
            {
                clickedCtrl = (Border)source;
            }
            else if (source is Image)
            {
                Image image = (Image)source;
                clickedCtrl = (Border)image.Parent;
            }
            else if (source == panel)
            {
                return null;
            }
            else
            {
                throw new ApplicationException("Expected an image or border");
            }

            // Get the index
            int index = panel.Children.IndexOf(clickedCtrl);
            if (index < 0)
            {
                throw new ApplicationException("Couldn't find clicked item");
            }

            if (_kernels.Count != panel.Children.Count)
            {
                throw new ApplicationException("_kernels and panelKernels are out of sync");
            }

            return Tuple.Create(clickedCtrl, index);
        }

        private void RefreshStatsPanel()
        {
            if (_selectedKernelIndex < 0)
            {
                lblSelectedSize.Content = "";
                lblSelectedReduction.Content = "";
            }

            int totalReduceX = 0;
            int totalReduceY = 0;

            for (int cntr = 0; cntr < _kernels.Count; cntr++)
            {
                var childReduce = _kernels[cntr].GetReduction();

                totalReduceX += childReduce.Item1;
                totalReduceY += childReduce.Item2;

                if (cntr == _selectedKernelIndex)
                {
                    if (_kernels[cntr] is Convolution2D)
                    {
                        Convolution2D childSingle = (Convolution2D)_kernels[cntr];
                        lblSelectedSize.Content = string.Format("{0}x{1}", childSingle.Width, childSingle.Height);
                    }
                    else
                    {
                        lblSelectedSize.Content = "";
                    }

                    lblSelectedReduction.Content = string.Format("{0}x{1}", childReduce.Item1, childReduce.Item2);
                }
            }

            lblTotalReduction.Content = string.Format("{0}x{1}", totalReduceX, totalReduceY);
        }

        #endregion
    }
}
