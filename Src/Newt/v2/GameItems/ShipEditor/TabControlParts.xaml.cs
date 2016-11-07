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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    public partial class TabControlParts : UserControl
    {
        #region Events

        // This gets fired when a drag is started, but not dropped
        public event EventHandler<TabControlParts_DragItem> DragDropCancelled = null;

        #endregion

        #region Declaration Section

        private const string TITLE = "TabControlParts";

        public const string DRAGDROP_FORMAT = "PartsTabControlFormat";

        private Point? _dragStart = null;

        #endregion

        #region Constructor

        public TabControlParts()
        {
            InitializeComponent();

            tabCtrl.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        #endregion

        #region Event Listeners

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// There is a flaw in the tab control where if all tabs are removed, then a new one is added, that single tab won't be selected (it will get
        /// selected when a second tab is added)
        /// 
        /// This event listener fixes that flaw
        /// </summary>
        /// <remarks>
        /// http://stackoverflow.com/questions/1177006/how-to-make-sure-my-wpf-tabcontrol-always-has-a-selected-tab-when-it-contains-at
        /// </remarks>
        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            try
            {
                ItemContainerGenerator senderCast = sender as ItemContainerGenerator;
                if (senderCast == null || tabCtrl == null)
                {
                    return;
                }

                if (senderCast.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated && tabCtrl.SelectedIndex == -1)
                {
                    tabCtrl.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //http://www.wpftutorial.net/draganddrop.html
        private void TabControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Store the mouse position
                _dragStart = e.GetPosition(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TabControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            bool shouldClearDragStart = true;

            try
            {
                if (_dragStart == null || e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }

                TabControlPartsVM context = this.DataContext as TabControlPartsVM;
                if (context == null)
                {
                    return;
                }

                // Get the current mouse position
                Point mousePos = e.GetPosition(null);
                Vector diff = _dragStart.Value - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Get the item they are dragging
                    TabControlParts_DragItem dragItem = context.GetDragItem((DependencyObject)e.OriginalSource);
                    if (dragItem == null)
                    {
                        return;
                    }

                    shouldClearDragStart = false;

                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject(DRAGDROP_FORMAT, dragItem);
                    if (DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move) == DragDropEffects.None)
                    {
                        // If they drag around, but not in a place where Drop gets fired, then execution reenters here.
                        if (this.DragDropCancelled != null)
                        {
                            this.DragDropCancelled(this, dragItem);
                        }
                    }
                    else
                    {
                        shouldClearDragStart = true;
                    }
                }
                else
                {
                    shouldClearDragStart = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (shouldClearDragStart)
                {
                    _dragStart = null;
                }
            }
        }

        #endregion
    }
}
