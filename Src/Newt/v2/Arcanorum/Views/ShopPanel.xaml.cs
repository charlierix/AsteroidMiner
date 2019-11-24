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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;

namespace Game.Newt.v2.Arcanorum.Views
{
    public partial class ShopPanel : UserControl
    {
        #region class: DragDropItem

        private class DragDropItem
        {
            public DragDropItem(bool isFromShop, InventoryItem item)
            {
                this.IsFromShop = isFromShop;
                this.Item = item;
            }

            /// <summary>
            /// True: Buying from the shop
            /// False: From the player (selling to the shop)
            /// </summary>
            public readonly bool IsFromShop;

            public readonly InventoryItem Item;
        }

        #endregion

        #region Declaration Section

        private const string DATAFORMAT_DRAGGINGITEM = "data format - item";

        /// <summary>
        /// This is populated when dragging from one listbox to the other
        /// </summary>
        private DragDropItem _draggingItem = null;

        private Shop _shop = null;
        private ArcBot _bot = null;

        private DispatcherTimer _timer = null;
        private DateTime _lastTick = DateTime.UtcNow;

        private List<AnimateRotation> _animationRotates = new List<AnimateRotation>();

        private bool _isProgramaticallyClearingSelection = false;

        #endregion

        #region Constructor

        public ShopPanel()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(75);
            _timer.Tick += Timer_Tick;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Use this when the panel closes to reattach the weapon (it was put in inventory when entering the shop)
        /// </summary>
        public Guid? AttachedWeaponID
        {
            get;
            private set;
        }

        private ItemDetailPanel _detailPanel = null;
        public ItemDetailPanel DetailPanel
        {
            get
            {
                return _detailPanel;
            }
            set
            {
                _detailPanel = value;
            }
        }

        #endregion

        #region Public Methods

        public void ShowInventories(Shop shop, ArcBot bot)
        {
            _shop = shop;
            _bot = bot;
            _animationRotates.Clear();

            if (bot.Weapon == null)
            {
                this.AttachedWeaponID = null;
            }
            else
            {
                this.AttachedWeaponID = bot.Weapon.DNA.UniqueID;
                bot.AttachWeapon(null, ItemToFrom.Nowhere, ItemToFrom.Inventory);
            }

            //TODO: When the user can create weapons in the shop, this will need to be recalculated, and all item cameras need to be adjusted
            double maxWeaponRadius = UtilityCore.Iterate(shop.Inventory.Weapons, bot.Inventory.Weapons).Max(o => o.Radius);

            LoadInventory(lstShopInventory, shop.Inventory, _animationRotates, maxWeaponRadius);
            LoadInventory(lstBotInventory, bot.Inventory, _animationRotates, maxWeaponRadius);
        }

        #endregion

        #region Event Listeners

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_timer != null)
            {
                _lastTick = DateTime.UtcNow;
                _timer.IsEnabled = this.IsVisible;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_animationRotates.Count == 0)
                {
                    return;
                }

                DateTime newTime = DateTime.UtcNow;
                double elapsedTime = (newTime - _lastTick).TotalSeconds;
                _lastTick = newTime;

                foreach (AnimateRotation animate in _animationRotates)
                {
                    animate.Tick(elapsedTime);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ShopPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _draggingItem = null;

                var clickedItem = GetClickedItem(e.Source as DependencyObject);
                if (clickedItem == null)
                {
                    return;
                }

                _draggingItem = new DragDropItem(clickedItem.Item2 == lstShopInventory, clickedItem.Item1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ShopPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_draggingItem == null)
                {
                    return;
                }
                else if (e.LeftButton != MouseButtonState.Pressed)
                {
                    _draggingItem = null;
                    return;
                }

                DataObject dragData = new DataObject(DATAFORMAT_DRAGGINGITEM, _draggingItem);
                DragDrop.DoDragDrop(_draggingItem.Item, dragData, DragDropEffects.Move);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ShopPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DATAFORMAT_DRAGGINGITEM))
                {
                    // Don't do any more validations here, wait until drop
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
                MessageBox.Show(ex.ToString(), "ShopPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (_draggingItem == null)
                {
                    return;
                }

                // Figure out which listbox this was dropped in
                ListBox droppedListbox = GetDroppedListbox(e.Source as DependencyObject);
                if (droppedListbox == null)
                {
                    return;
                }

                // Cancel if they drop into the same listbox
                if (_draggingItem.IsFromShop)
                {
                    if (droppedListbox == lstShopInventory)
                    {
                        return;
                    }
                }
                else
                {
                    if (droppedListbox == lstBotInventory)
                    {
                        return;
                    }
                }

                // Transfer
                if (_draggingItem.IsFromShop)
                {
                    BuyItem(_draggingItem.Item);
                }
                else
                {
                    SellItem(_draggingItem.Item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ShopPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // No matter what happens, this needs to become null
                _draggingItem = null;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isProgramaticallyClearingSelection)
                {
                    return;
                }

                if (_detailPanel == null)
                {
                    return;
                }

                ListBox sourceCast = e.Source as ListBox;
                if (sourceCast == null)
                {
                    return;
                }

                InventoryItem item = sourceCast.SelectedItem as InventoryItem;

                if (item == null)
                {
                    _detailPanel.Item = null;
                }
                else
                {
                    _detailPanel.Item = item.Item;

                    #region Unselect other listbox

                    _isProgramaticallyClearingSelection = true;

                    if (sourceCast == lstBotInventory)
                    {
                        lstShopInventory.SelectedItem = null;
                    }
                    else
                    {
                        lstBotInventory.SelectedItem = null;
                    }

                    _isProgramaticallyClearingSelection = false;

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ShopPanel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static void LoadInventory(ListBox listbox, Inventory inventory, List<AnimateRotation> animations, double maxWeaponRadius)
        {
            // Clear existing
            listbox.Items.Clear();

            Random rand = StaticRandom.GetRandomForThread();

            if (inventory.Weapons.Count > 0)
            {
                #region Weapons


                foreach (Weapon weapon in inventory.Weapons)
                {
                    //NOTE: No need for a border, the ListBoxItem has been restyled to be a border

                    InventoryItem item = new InventoryItem();
                    item.SetItem(weapon, weapon.Radius / maxWeaponRadius);

                    listbox.Items.Add(item);

                    item.RotateTransform.Quaternion = new Quaternion(new Vector3D(1, 0, 0), StaticRandom.NextDouble(360d));
                    animations.Add(AnimateRotation.Create_Constant(item.RotateTransform, rand.NextDouble(4d, 7d)));
                }

                #endregion
            }
        }

        private Tuple<InventoryItem, ListBox> GetClickedItem(DependencyObject source)
        {
            if (source == null)
            {
                return null;
            }

            InventoryItem item = null;

            DependencyObject next = source;

            // Keep walking up through parents and find the inventory item and listbox
            //NOTE: ListBoxItem won't be in the chain, because the listbox was retemplated
            while (next != null)
            {
                if (next is InventoryItem)
                {
                    item = next as InventoryItem;
                }
                else if (next is ListBox)
                {
                    if (item == null)
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create(item, (ListBox)next);
                    }
                }

                next = VisualTreeHelper.GetParent(next);
            }

            return null;
        }
        private ListBox GetDroppedListbox(DependencyObject source)
        {
            if (source == null)
            {
                return null;
            }

            DependencyObject next = source;
            while (next != null)
            {
                if (next is ListBox)
                {
                    return (ListBox)next;
                }

                next = VisualTreeHelper.GetParent(next);
            }

            return null;
        }

        //TODO: Transfer money
        private void BuyItem(InventoryItem item)
        {
            if (!(item.Item is Weapon))
            {
                throw new ApplicationException("Unknown item type: " + item.Item.GetType().ToString());
            }

            Weapon weapon = (Weapon)item.Item;

            //TODO: Make sure there is enough money

            // Take from the shop
            _shop.Inventory.Weapons.Remove(item.Item as Weapon);
            lstShopInventory.Items.Remove(item);

            // Add to the bot
            _bot.Inventory.Weapons.Add(item.Item as Weapon);
            lstBotInventory.Items.Add(item);

            // Select it
            lstBotInventory.SelectedItem = item;
        }
        private void SellItem(InventoryItem item)
        {
            if (!(item.Item is Weapon))
            {
                throw new ApplicationException("Unknown item type: " + item.Item.GetType().ToString());
            }

            Weapon weapon = (Weapon)item.Item;

            // Take from the bot
            _bot.Inventory.Weapons.Remove(item.Item as Weapon);
            lstBotInventory.Items.Remove(item);

            // Add to the shop
            _shop.Inventory.Weapons.Add(item.Item as Weapon);
            lstShopInventory.Items.Add(item);

            // Select it
            lstShopInventory.SelectedItem = item;
        }

        #endregion
    }
}
