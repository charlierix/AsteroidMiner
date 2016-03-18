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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public partial class SpaceDockPanel : UserControl
    {
        #region Events

        public event EventHandler LaunchShip = null;

        #endregion

        #region Declaration Section

        private const string TITLE = "Space Dock";

        private const string REFILL_FUEL = "Fuel";
        private const string REFILL_ENERGY = "Energy";
        private const string REFILL_PLASMA = "Plasma";
        private const string REFILL_AMMO = "Ammo";
        private const string REFILL_REPAIR = "Repair";
        private const string REFILL_ALL = "All";

        private const string ACTION_VIEW = "View";
        private const string ACTION_BUY = "Buy";
        private const string ACTION_SELL = "Sell";
        private const string ACTION_HANGAR = "Hangar";        // store in hangar
        private const string ACTION_USE = "Use";        // store in ship
        private const string ACTION_REMOVE = "Remove";        // remove from ship/hangar into free space

        private readonly string[] _actions_Station_Ships = new[] { ACTION_VIEW, ACTION_BUY };
        private readonly string[] _actions_Station_Parts = new[] { ACTION_VIEW, ACTION_BUY };
        private readonly string[] _actions_Station_Minerals = new[] { ACTION_VIEW, ACTION_BUY };

        //TODO: allow renaming ships (also need an option for the current ship)
        private readonly string[] _actions_Player_Cargo = new[] { ACTION_VIEW, ACTION_SELL, ACTION_HANGAR, ACTION_REMOVE };
        private readonly string[] _actions_Player_Hangar = new[] { ACTION_VIEW, ACTION_SELL, ACTION_USE, ACTION_REMOVE };
        private readonly string[] _actions_Player_FreeSpace = new[] { ACTION_VIEW, ACTION_SELL, ACTION_USE, ACTION_HANGAR };

        private readonly EditorOptions _editorOptions;
        private readonly ItemOptions _itemOptions;
        private readonly Map _map;
        private readonly int _material_Ship;
        private readonly int _material_Projectile;

        private Player _player = null;
        private SpaceStation2D _station = null;
        private World _world = null;

        // These are the mineral types sorted by price
        //TODO: If the suggested price changes for the AsteroidMiner2D, make sure this is updated
        private static Lazy<MineralType[]> _mineralTypeSortOrder = new Lazy<MineralType[]>(() =>
            ((MineralType[])Enum.GetValues(typeof(MineralType))).
                //Select(o => new { Type = o, Credits = Mineral.GetSuggestedCredits(o) }).
                Select(o => new { Type = o, Credits = ItemOptionsAstMin2D.GetCredits_Mineral(o) }).
                OrderByDescending(o => o.Credits).
                Select(o => o.Type).
                ToArray());

        #endregion

        #region Constructor

        public SpaceDockPanel(EditorOptions editorOptions, ItemOptions itemOptions, Map map, int material_Ship, int material_Projectile)
        {
            InitializeComponent();

            _editorOptions = editorOptions;
            _itemOptions = itemOptions;
            _map = map;
            _material_Ship = material_Ship;
            _material_Projectile = material_Projectile;
        }

        #endregion

        #region Public Methods

        //TODO: Take any other objects that are near the player at the time of docking
        public void ShipDocking(Player player, SpaceStation2D station, World world)
        {
            _player = player;
            _station = station;
            _world = world;

            //NOTE: Can't use pnlFlag.ActualWidth, because it could be zero when this panel is newly instantiated
            pnlFlag.Content = new FlagVisual(pnlFlag.Width, pnlFlag.Height, _station.Flag.FlagProps);       // can't just store it directly, it's the wrong size

            // Stop the ship
            //TODO: Stop any other objects that were passed in
            if (_player.Ship != null)
            {
                _player.Ship.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
                _player.Ship.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);
            }

            _player.CreditsChanged += new EventHandler(Player_CreditsChanged);
            Player_CreditsChanged(this, new EventArgs());

            _player.ShipChanged += new EventHandler<ShipChangedArgs>(Player_ShipChanged);
            Player_ShipChanged(this, new ShipChangedArgs(null, _player.Ship));

            OnHangarChanged();

            //TODO: Finish showing all the panels
            ShowStationMinerals();
            //ShowStationParts();
            ShowStationShips(world);

            ShowShipCargo();
            //ShowNearbyItems();
            ShowHangar();

            GenerateRefillButtons();
        }

        #endregion

        #region Event Listeners

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            const double MINWIDTH = 280;

            try
            {
                double panelWidth = pnlStationMinerals.ActualWidth;

                int columns = Convert.ToInt32(Math.Floor(panelWidth / MINWIDTH));
                if (columns == 0)
                {
                    columns = 1;
                }

                if (pnlStationMinerals.Columns == columns)
                {
                    return;
                }

                pnlStationMinerals.Columns = columns;
                pnlStationParts.Columns = columns;
                pnlStationShips.Columns = columns;

                pnlCargo.Columns = columns;
                pnlNearbyItems.Columns = columns;
                pnlHangar.Columns = columns;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Player_CreditsChanged(object sender, EventArgs e)
        {
            try
            {
                //NOTE:  The credits can be non integer, but round it for the display (actual purchaces may be portions of a dollar though)
                lblCredits.Text = _player.Credits.ToString("N0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Player_ShipChanged(object sender, ShipChangedArgs e)
        {
            try
            {
                if (_player.Ship == null)
                {
                    pnlPlayerIcon.Content = null;
                }
                else
                {
                    Icon3D icon = new Icon3D("", _player.Ship.GetNewDNA(), _world);
                    icon.ShowName = false;
                    icon.ShowBorder = false;

                    pnlPlayerIcon.Content = icon;
                }

                GenerateRefillButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLoadShipFromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string foldername = System.IO.Path.Combine(UtilityCore.GetOptionsFolder(), ShipEditorWindow.SHIPFOLDER);

                ShipSelectorWindow dialog = new ShipSelectorWindow(foldername, _world);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                bool? dialogResult = dialog.ShowDialog();

                if (dialogResult == null || !dialogResult.Value)
                {
                    return;
                }

                foreach (ShipDNA dna in dialog.SelectedItems)
                {
                    Inventory inventory = new Inventory(dna, 1);

                    _station.StationInventory.Add(inventory);
                    AddStationShipGraphic(inventory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBuyHangarSpace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var price = _station.GetHangarSpacePrice_Buy();

                if (_player.Credits < price.Item2)
                {
                    PlayFailSound(btnBuyHangarSpace);
                    return;
                }

                _player.Credits -= price.Item2;

                _station.BuyHangarSpace();

                OnHangarChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSellHangarSpace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var price = _station.GetHangarSpacePrice_Sell();
                if (price.Item1 == 0)
                {
                    // Nothing to sell
                    PlayFailSound(btnSellHangarSpace);
                    return;
                }

                int newVolume = _station.PurchasedVolume - price.Item1;

                if (_station.UsedVolume > newVolume)
                {
                    #region Make room

                    // Need to make room.  Throw out the least valuable items first

                    // Sort the hangar contents by price
                    List<InventoryEntry> hangarContents = new List<InventoryEntry>();
                    foreach (InventoryEntry entry in pnlHangar.Children)
                    {
                        hangarContents.Add(entry);
                    }

                    hangarContents = hangarContents.OrderBy(o => o.Credits).ToList();

                    // Keep removing until it's safe to sell back the volume
                    while (_station.UsedVolume > newVolume)
                    {
                        for (int cntr = 0; cntr < _station.PlayerInventory.Count; cntr++)
                        {
                            if (_station.PlayerInventory[cntr].Token == hangarContents[0].Inventory.Token)
                            {
                                _station.PlayerInventory.RemoveAt(cntr);
                                break;
                            }
                        }

                        pnlHangar.Children.Remove(hangarContents[0]);
                        hangarContents.RemoveAt(0);
                    }

                    #endregion
                }

                _player.Credits += price.Item2;

                _station.SellHangarSpace();

                OnHangarChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InventoryEntry_ActionClicked(object sender, InventoryActionClickedArgs e)
        {
            try
            {
                InventoryEntry entry = sender as InventoryEntry;
                if (entry == null)
                {
                    throw new ApplicationException("Expected sender to be InventoryEntry: " + sender == null ? "<null>" : sender.GetType().ToString());
                }

                switch (e.Action)
                {
                    case ACTION_BUY:
                        #region Buy

                        Buy(entry, e, sender as UIElement);

                        #endregion
                        break;

                    case ACTION_SELL:
                        #region Sell

                        Sell(entry, e);

                        #endregion
                        break;

                    case ACTION_USE:
                        #region Use

                        if (e.Inventory.Ship != null)
                        {
                            SwapShip(entry);
                        }
                        else
                        {
                            throw new ApplicationException("finish this");
                        }

                        // The other two should just be stored in the ship's cargo

                        #endregion
                        break;

                    case ACTION_HANGAR:
                        #region Hangar

                        if (_station.PurchasedVolume - _station.UsedVolume < entry.Inventory.Volume)
                        {
                            PlayFailSound(sender as UIElement);
                            return;
                        }

                        RemoveFrom_Cargo_Hangar_Neaby(entry);
                        StoreIn_Hangar_Nearby(entry.Inventory, e.Name);        // it won't go in nearby, because the if statement made sure the hangar has room

                        #endregion
                        break;

                    case ACTION_REMOVE:
                        #region Nearby

                        RemoveFrom_Cargo_Hangar_Neaby(entry);
                        StoreIn_Nearby(entry.Inventory, e.Name);

                        #endregion
                        break;

                    case ACTION_VIEW:
                        throw new ApplicationException("finish this: " + e.Action);

                    default:
                        throw new ApplicationException("Unknown action: " + e.Action);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refill_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //RefillButton senderCast = sender as RefillButton;
                RefillButton senderCast = e.OriginalSource as RefillButton;
                if (senderCast == null)
                {
                    throw new ApplicationException("Unknown type of sender: " + sender == null ? "<null>" : sender.GetType().ToString());
                }

                ShipPlayer ship = _player.Ship;

                if (ship == null)
                {
                    throw new ApplicationException("This should never get called when the ship is null");
                }

                switch (senderCast.Text)
                {
                    case REFILL_REPAIR:
                        throw new ApplicationException("Finish this: " + senderCast.Text);

                    case REFILL_FUEL:
                        FillContainer(ship.Fuel, _station.GetPrice_Buy_Fuel(), senderCast);
                        break;

                    case REFILL_ENERGY:
                        FillContainer(ship.Energy, _station.GetPrice_Buy_Energy(), senderCast);
                        break;

                    case REFILL_PLASMA:
                        FillContainer(ship.Plasma, _station.GetPrice_Buy_Plasma(), senderCast);
                        break;

                    case REFILL_AMMO:
                        FillContainer(ship.Ammo, _station.GetPrice_Buy_Ammo(), senderCast);
                        break;

                    case REFILL_ALL:
                        FillContainer(ship.Fuel, _station.GetPrice_Buy_Fuel(), senderCast);
                        FillContainer(ship.Energy, _station.GetPrice_Buy_Energy(), senderCast);
                        FillContainer(ship.Plasma, _station.GetPrice_Buy_Plasma(), senderCast);
                        FillContainer(ship.Ammo, _station.GetPrice_Buy_Ammo(), senderCast);
                        break;

                    default:
                        throw new ApplicationException("Unknown refill button text: " + senderCast.Text);
                }

                UpdateRefillPrices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLaunch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.LaunchShip == null)
                {
                    MessageBox.Show("There is no event listener for the launch button", "Launch Button", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //TODO: Eject nearby items into space instead of taking them
                while (pnlNearbyItems.Children.Count > 0)
                {
                    InventoryEntry item = (InventoryEntry)pnlNearbyItems.Children[0];

                    RemoveFrom_Cargo_Hangar_Neaby(item);
                    StoreIn_Mineral_Part_Ship(item.Inventory);
                }

                // This panel is about to close, so drop references
                _player.CreditsChanged -= new EventHandler(Player_CreditsChanged);
                _player.ShipChanged -= new EventHandler<ShipChangedArgs>(Player_ShipChanged);
                _player = null;
                _station = null;

                this.LaunchShip(this, new EventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void OnHangarChanged()
        {
            lblPurchasedHangarVolume.Text = _station.PurchasedVolume.ToString("N0");
            lblUsedHangarVolume.Text = _station.UsedVolume.ToString("N0");

            var buyPrice = _station.GetHangarSpacePrice_Buy();
            btnBuyHangarSpace.Credits = buyPrice.Item2;

            var sellPrice = _station.GetHangarSpacePrice_Sell();
            btnSellHangarSpace.Credits = sellPrice.Item2;
        }

        #endregion

        #region Private Methods - inventory actions

        private void Buy(InventoryEntry entry, InventoryActionClickedArgs e, UIElement clickedControl)
        {
            if (_player.Credits >= e.Credits)
            {
                _player.Credits -= e.Credits;
            }
            else
            {
                // Not enough money
                PlayFailSound(clickedControl);
                return;
            }

            RemoveFrom_Mineral_Part_Ship(entry);

            if (e.Inventory.Ship != null)
            {
                StoreIn_Hangar_Nearby(e.Inventory, e.Name);
            }
            else
            {
                StoreIn_Cargo_Hangar_Nearby(e.Inventory, e.Name);
            }
        }
        private void Sell(InventoryEntry entry, InventoryActionClickedArgs e)
        {
            _player.Credits += e.Credits;

            RemoveFrom_Cargo_Hangar_Neaby(entry);
            StoreIn_Mineral_Part_Ship(entry.Inventory);
        }

        private async void SwapShip(InventoryEntry entry)
        {
            const double STARTPERCENT = .25;



            //TODO: Remember the cargo in a separate object
            //TODO: Remember the part damage in a separate object



            // Remove the old entry
            RemoveFrom_Cargo_Hangar_Neaby(entry);

            // Store the old ship
            if (_player.Ship != null)
            {
                Inventory prevInventory = new Inventory(_player.Ship.GetNewDNA(), 1d);
                string prevName = GetName_Ship(prevInventory.Ship);

                StoreIn_Hangar_Nearby(prevInventory, prevName);
            }

            //NOTE: Grabbing this now, because the ship creation is async, and station could be null
            // by the time it's finished
            Point3D stationPosition = _station.PositionWorld;

            // Need to do this now so that the old ship is off the map before adding a new one (the new one sometimes goes flying from the collision)
            _player.Ship = null;

            ShipExtraArgs args = new ShipExtraArgs()
            {
                Options = _editorOptions,
                ItemOptions = _itemOptions,
                Material_Projectile = _material_Projectile,
                RunNeural = false,
            };

            // Create the new ship
            ShipPlayer ship = ShipPlayer.GetNewShip(entry.Inventory.Ship, _world, _material_Ship, _map, args);

            if (ship.Energy != null)
            {
                ship.Energy.QuantityCurrent = ship.Energy.QuantityMax * STARTPERCENT;
            }
            if (ship.Plasma != null)
            {
                ship.Plasma.QuantityCurrent = ship.Plasma.QuantityMax * STARTPERCENT;
            }
            if (ship.Fuel != null)
            {
                ship.Fuel.QuantityCurrent = ship.Fuel.QuantityMax * STARTPERCENT;
            }
            if (ship.Ammo != null)
            {
                ship.Ammo.QuantityCurrent = ship.Ammo.QuantityMax * STARTPERCENT;
            }
            ship.RecalculateMass();

            ship.PhysicsBody.Position = new Point3D(stationPosition.X, stationPosition.Y, 0);
            ship.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
            ship.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);

            _player.Ship = ship;        // the ship changed event listener will add the ship to the map
        }

        #endregion
        #region Private Methods - add/remove inventory

        private void StoreIn_Mineral_Part_Ship(Inventory inventory)
        {
            _station.StationInventory.Add(inventory);

            if (inventory.Ship != null)
            {
                AddStationShipGraphic(inventory);
            }
            else if (inventory.Part != null)
            {
                throw new ApplicationException("finish this");
            }
            else if (inventory.Mineral != null)
            {
                AddStationMineralGraphic(inventory);
            }
            else
            {
                throw new ArgumentException("Unknown type of inventory");
            }
        }

        private void RemoveFrom_Mineral_Part_Ship(InventoryEntry entry)
        {
            InventoryEntryRemoved(entry);

            _station.StationInventory.Remove(entry.Inventory);

            if (entry.Inventory.Ship != null)
            {
                pnlStationShips.Children.Remove(entry);
            }
            else if (entry.Inventory.Part != null)
            {
                pnlStationParts.Children.Remove(entry);
            }
            else if (entry.Inventory.Mineral != null)
            {
                pnlStationMinerals.Children.Remove(entry);
            }
        }

        private void StoreIn_Hangar_Nearby(Inventory inventory, string name)
        {
            // Figure out where to put it
            string[] actions;
            Panel panel;
            bool isHangar;

            if (_station.PurchasedVolume - _station.UsedVolume >= inventory.Volume)
            {
                // Hangar
                actions = _actions_Player_Hangar;
                panel = pnlHangar;
                isHangar = true;
            }
            else
            {
                // Nearby
                actions = _actions_Player_FreeSpace;
                panel = pnlNearbyItems;
                isHangar = false;
            }

            // Create an entry with the sell price
            decimal credits = _station.GetPrice_Sell(inventory);

            InventoryEntry newEntry = new InventoryEntry();
            newEntry.SetInventory(inventory, name, credits, _world, actions);

            // Store it
            //panel.Children.Add(newEntry);
            AddToPanel(panel, newEntry);
            InventoryEntryAdded(newEntry);

            if (isHangar)
            {
                _station.PlayerInventory.Add(inventory);
                OnHangarChanged();
            }
        }
        private void StoreIn_Cargo_Hangar_Nearby(Inventory inventory, string name)
        {
            if (inventory.Mineral == null)
            {
                //TODO: Also support storing other than mineral
                throw new ApplicationException("finish this");
            }

            Cargo_Mineral cargo = new Cargo_Mineral(inventory.Mineral.MineralType, inventory.Mineral.Density, inventory.Mineral.Volume);

            if (_player.Ship.CargoBays.Add(cargo))
            {
                AddShipCargoGraphic(inventory);
            }
            else
            {
                StoreIn_Hangar_Nearby(inventory, name);
            }
        }
        private void StoreIn_Nearby(Inventory inventory, string name)
        {
            // Create an entry with the sell price
            decimal credits = _station.GetPrice_Sell(inventory);

            InventoryEntry newEntry = new InventoryEntry();
            newEntry.SetInventory(inventory, name, credits, _world, _actions_Player_FreeSpace);

            // Store it
            //pnlNearbyItems.Children.Add(newEntry);
            AddToPanel(pnlNearbyItems, newEntry);
            InventoryEntryAdded(newEntry);
        }

        private void RemoveFrom_Cargo_Hangar_Neaby(InventoryEntry entry)
        {
            InventoryEntryRemoved(entry);

            if (pnlCargo.Children.Contains(entry))
            {
                #region Cargo

                pnlCargo.Children.Remove(entry);

                if (entry.Inventory.Mineral != null)
                {
                    _player.Ship.CargoBays.RemoveMineral_Volume(entry.Inventory.Mineral.MineralType, entry.Inventory.Mineral.Volume);
                }
                //else if (entry.Inventory.Part != null)
                //{

                //}
                else
                {
                    throw new ApplicationException("Unknown type of entry");
                }

                #endregion
            }
            else if (pnlHangar.Children.Contains(entry))
            {
                #region Hangar

                pnlHangar.Children.Remove(entry);

                _station.PlayerInventory.Remove(entry.Inventory);

                OnHangarChanged();

                #endregion
            }
            else if (pnlNearbyItems.Children.Contains(entry))
            {
                #region Nearby Items

                pnlNearbyItems.Children.Remove(entry);

                #endregion
            }
            else
            {
                throw new ArgumentException("Can't find the owner of the entry: " + entry.ToString());
            }
        }

        private void ShowStationShips(World world)
        {
            // Clear old
            foreach (InventoryEntry entry in pnlStationShips.Children)
            {
                InventoryEntryRemoved(entry);
            }
            pnlStationShips.Children.Clear();

            // Add new
            foreach (Inventory inventory in _station.StationInventory.Where(o => o.Ship != null))
            {
                AddStationShipGraphic(inventory);
            }
        }
        private void AddStationShipGraphic(Inventory inventory)
        {
            string name = GetName(inventory);
            decimal credits = _station.GetPrice_Buy(inventory);

            InventoryEntry entry = new InventoryEntry();
            entry.SetInventory(inventory, name, credits, _world, _actions_Station_Ships);

            //pnlStationShips.Children.Add(entry);
            AddToPanel(pnlStationShips, entry);

            InventoryEntryAdded(entry);
        }

        private void ShowStationMinerals()
        {
            // Clear old
            foreach (InventoryEntry entry in pnlStationMinerals.Children)
            {
                InventoryEntryRemoved(entry);
            }
            pnlStationMinerals.Children.Clear();

            // Add new
            foreach (Inventory inventory in _station.StationInventory.Where(o => o.Mineral != null))
            {
                AddStationMineralGraphic(inventory);
            }
        }
        private void AddStationMineralGraphic(Inventory inventory)
        {
            string name = GetName(inventory);
            decimal credits = _station.GetPrice_Buy(inventory);

            InventoryEntry entry = new InventoryEntry();
            entry.SetInventory(inventory, name, credits, _world, _actions_Station_Minerals);

            //pnlStationMinerals.Children.Add(entry);
            AddToPanel(pnlStationMinerals, entry);

            InventoryEntryAdded(entry);
        }

        private void ShowShipCargo()
        {
            // Clear old
            foreach (InventoryEntry entry in pnlCargo.Children)
            {
                InventoryEntryRemoved(entry);
            }
            pnlCargo.Children.Clear();

            // Add new
            if (_player.Ship == null || _player.Ship.CargoBays == null)
            {
                return;
            }

            foreach (Cargo cargo in _player.Ship.CargoBays.GetCargoSnapshot())
            {
                Inventory inventory = null;

                switch (cargo.CargoType)
                {
                    case CargoType.Mineral:
                        Cargo_Mineral cargoMineral = (Cargo_Mineral)cargo;
                        inventory = new Inventory(ItemOptionsAstMin2D.GetMineral(cargoMineral.MineralType, cargoMineral.Volume));
                        break;

                    case CargoType.ShipPart:
                        Cargo_ShipPart part = (Cargo_ShipPart)cargo;
                        inventory = new Inventory(part.DNA, 1, 1);
                        break;

                    default:
                        throw new ApplicationException("Unknown CargoType: " + cargo.CargoType.ToString());
                }

                AddShipCargoGraphic(inventory);
            }
        }
        private void AddShipCargoGraphic(Inventory inventory)
        {
            string name = GetName(inventory);
            decimal credits = _station.GetPrice_Sell(inventory);

            InventoryEntry entry = new InventoryEntry();
            entry.SetInventory(inventory, name, credits, _world, _actions_Player_Cargo);

            //pnlCargo.Children.Add(entry);
            AddToPanel(pnlCargo, entry);

            InventoryEntryAdded(entry);
        }

        private void ShowHangar()
        {
            // Clear old
            foreach (InventoryEntry entry in pnlHangar.Children)
            {
                InventoryEntryRemoved(entry);
            }
            pnlHangar.Children.Clear();

            // Add new
            foreach (Inventory inventory in _station.PlayerInventory)
            {
                AddHangarItemGraphic(inventory);
            }
        }
        private void AddHangarItemGraphic(Inventory inventory)
        {
            string name = GetName(inventory);
            decimal credits = _station.GetPrice_Sell(inventory);

            InventoryEntry entry = new InventoryEntry();
            entry.SetInventory(inventory, name, credits, _world, _actions_Player_Hangar);

            //pnlHangar.Children.Add(entry);
            AddToPanel(pnlHangar, entry);

            InventoryEntryAdded(entry);
        }

        #endregion
        #region Private Methods

        private static string GetName(Inventory inventory)
        {
            if (inventory.Ship != null)
            {
                return GetName_Ship(inventory.Ship);
            }
            else if (inventory.Part != null)
            {
                return inventory.Part.PartType;
            }
            else if (inventory.Mineral != null)
            {
                return inventory.Mineral.MineralType.ToString();
            }
            else
            {
                throw new ApplicationException("Unknown type of inventory");
            }
        }
        private static string GetName_Ship(ShipDNA dna)
        {
            string retVal = dna.ShipName;

            if (string.IsNullOrWhiteSpace(retVal))
            {
                retVal = dna.ShipLineage ?? "";
            }

            retVal = retVal.Trim();

            return retVal;
        }

        //TODO: Figure out attached events instead
        private void InventoryEntryAdded(InventoryEntry entry)
        {
            entry.ActionClicked += new EventHandler<InventoryActionClickedArgs>(InventoryEntry_ActionClicked);
        }
        private void InventoryEntryRemoved(InventoryEntry entry)
        {
            entry.ActionClicked -= new EventHandler<InventoryActionClickedArgs>(InventoryEntry_ActionClicked);
        }

        private void GenerateRefillButtons()
        {
            // Remove existing
            pnlRefills.Children.Clear();

            ShipPlayer ship = _player.Ship;
            if (ship == null)
            {
                return;
            }

            RefillButton button;
            Thickness margin = new Thickness(2);

            //NOTE: Credits are populated at the end of this method

            #region  Fuel

            if (ship.Fuel != null)
            {
                pnlRefills.Children.Add(new RefillButton()
                {
                    Text = REFILL_FUEL,
                    Margin = margin,
                });
            }

            #endregion
            #region Energy

            if (ship.Energy != null)
            {
                pnlRefills.Children.Add(new RefillButton()
                {
                    Text = REFILL_ENERGY,
                    Margin = margin,
                });
            }

            #endregion
            #region Plasma

            if (ship.Plasma != null)
            {
                pnlRefills.Children.Add(new RefillButton()
                {
                    Text = REFILL_PLASMA,
                    Margin = margin,
                });
            }

            #endregion
            #region Ammo

            if (ship.Ammo != null)
            {
                pnlRefills.Children.Add(new RefillButton()
                {
                    Text = REFILL_AMMO,
                    Margin = margin,
                });
            }

            #endregion
            #region Repair ship

            pnlRefills.Children.Add(new RefillButton()
            {
                Text = REFILL_REPAIR,
                Margin = margin,
            });

            #endregion
            #region All

            pnlRefills.Children.Add(new RefillButton()
            {
                Text = REFILL_ALL,
                Margin = margin,
            });

            #endregion

            UpdateRefillPrices();
        }
        private void UpdateRefillPrices()
        {
            decimal sumCost = 0m;

            foreach (RefillButton button in pnlRefills.Children)
            {
                decimal cost = 0m;

                if (_player.Ship != null)
                {
                    switch (button.Text)
                    {
                        case REFILL_REPAIR:
                            break;

                        case REFILL_FUEL:
                            cost = GetCost(_player.Ship.Fuel, _station.GetPrice_Buy_Fuel());
                            break;

                        case REFILL_ENERGY:
                            cost = GetCost(_player.Ship.Energy, _station.GetPrice_Buy_Energy());
                            break;

                        case REFILL_PLASMA:
                            cost = GetCost(_player.Ship.Plasma, _station.GetPrice_Buy_Plasma());
                            break;

                        case REFILL_AMMO:
                            cost = GetCost(_player.Ship.Ammo, _station.GetPrice_Buy_Ammo());
                            break;

                        case REFILL_ALL:
                            //NOTE: The All button is always the last in the list, so sumCost is safe to use here
                            cost = sumCost;
                            break;

                        default:
                            throw new ApplicationException("Unknown refill button text: " + button.Text);
                    }
                }

                button.Credits = cost;

                sumCost += cost;        //NOTE: For the all button, sumCost is now doubled, but the All button is last, so the variable won't be used again
            }
        }

        private static decimal GetCost(IContainer container, decimal costPerUnit)
        {
            if (container == null)
            {
                return 0m;
            }
            else
            {
                return Convert.ToDecimal(container.QuantityMax - container.QuantityCurrent) * costPerUnit;
            }
        }

        private void FillContainer(IContainer container, decimal costPerUnit, UIElement clickedControl)
        {
            if (container == null || container.QuantityCurrent == container.QuantityMax)
            {
                // Just exit, this isn't an error
                return;
            }
            else if (_player.Credits == 0m)
            {
                PlayFailSound(clickedControl);
                return;
            }

            double amountToBy = container.QuantityMax - container.QuantityCurrent;

            // Figure out how much to spend
            decimal cost = Convert.ToDecimal(amountToBy) * costPerUnit;
            if (cost > _player.Credits)
            {
                cost = _player.Credits;

                // Buy a subset
                amountToBy = Convert.ToDouble(cost / costPerUnit);
            }

            // Buy it
            container.QuantityCurrent += amountToBy;
            _player.Credits -= cost;        //NOTE: There is an event listener tied to this property that will update the label
        }

        private void PlayFailSound(UIElement clickedControl)
        {
            //TODO: Play a sound

            if (clickedControl != null)
            {
                DropShadowEffect dropShadow = new DropShadowEffect()
                {
                    Color = Colors.Red,
                    Opacity = 0,
                    BlurRadius = 15,
                    Direction = 0,
                    ShadowDepth = 0,
                };
                clickedControl.Effect = dropShadow;

                DoubleAnimation animation = new DoubleAnimation()
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromSeconds(.2)),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3),
                    //RepeatBehavior = RepeatBehavior.Forever
                };

                dropShadow.BeginAnimation(DropShadowEffect.OpacityProperty, animation);
            }
        }

        //TODO: use proper mvvm sort techniques
        /// <summary>
        /// This adds the item to the panel, maintaining the sort order
        /// NOTE: This is a hack that moves all the panel's entries + new to a list, sorts the list, then adds them back to the panel
        /// </summary>
        private static void AddToPanel(Panel panel, InventoryEntry entry)
        {
            // Pop existing entries
            List<InventoryEntry> entries = new List<InventoryEntry>();

            foreach (InventoryEntry existingEntry in panel.Children)
            {
                entries.Add(existingEntry);
            }

            panel.Children.Clear();

            // Add new
            entries.Add(entry);

            // Add back sorted
            foreach (InventoryEntry sorted in SortInventory(entries))
            {
                panel.Children.Add(sorted);
            }
        }

        private static IEnumerable<InventoryEntry> SortInventory(IEnumerable<InventoryEntry> entries)
        {
            List<InventoryEntry> retVal = new List<InventoryEntry>();

            foreach (InventoryEntry entry in entries)
            {
                bool added = false;
                for (int cntr = 0; cntr < retVal.Count; cntr++)
                {
                    if (Is1LessThan2(entry, retVal[cntr]))
                    {
                        retVal.Insert(cntr, entry);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    retVal.Add(entry);
                }
            }

            return retVal;
        }

        /// <summary>
        /// This is sort of like a -1,0,1 compare, but only returns the -1 case or not -1
        /// </summary>
        private static bool Is1LessThan2(InventoryEntry entry1, InventoryEntry entry2)
        {
            // Sort by type
            bool? differentType = Is1LessThan2_DifferentTypes(entry1, entry2);
            if (differentType != null)
            {
                return differentType.Value;
            }

            // Sort within type
            if (entry1.Inventory.Ship != null)
            {
                return Is1LessThan2_Ship(entry1, entry2);
            }
            else if (entry1.Inventory.Part != null)
            {
                return Is1LessThan2_Part(entry1, entry2);
            }
            else if (entry1.Inventory.Mineral != null)
            {
                return Is1LessThan2_Mineral(entry1, entry2);
            }

            return false;
        }
        private static bool? Is1LessThan2_DifferentTypes(InventoryEntry entry1, InventoryEntry entry2)
        {
            var typeSorter = new Func<Inventory, int>(o =>
            {
                if (o.Ship != null) return 0;
                else if (o.Part != null) return 1;
                else if (o.Mineral != null) return 2;
                else return 100;
            });

            int i1 = typeSorter(entry1.Inventory);
            int i2 = typeSorter(entry2.Inventory);

            if (i1 == i2)
            {
                return null;
            }
            else
            {
                return i1 < i2;
            }
        }
        private static bool Is1LessThan2_Ship(InventoryEntry entry1, InventoryEntry entry2)
        {
            // Compare Names
            int compare = entry1.Inventory.Ship.ShipName.CompareTo(entry2.Inventory.Ship.ShipName);
            if (compare < 0) return true;
            else if (compare > 0) return false;

            // Same name
            return entry1.Credits > entry2.Credits;
        }
        private static bool Is1LessThan2_Part(InventoryEntry entry1, InventoryEntry entry2)
        {
            // Compare Names
            int compare = entry1.Name.CompareTo(entry2.Name);
            if (compare < 0) return true;
            else if (compare > 0) return false;

            // Same name
            return entry1.Credits > entry2.Credits;
        }
        private static bool Is1LessThan2_Mineral(InventoryEntry entry1, InventoryEntry entry2)
        {
            // Sort by type
            int i1 = Array.IndexOf(_mineralTypeSortOrder.Value, entry1.Inventory.Mineral.MineralType);
            int i2 = Array.IndexOf(_mineralTypeSortOrder.Value, entry2.Inventory.Mineral.MineralType);

            if (i1 < i2)
            {
                return true;
            }
            else if (i1 > i2)
            {
                return false;
            }

            // They are the same type, sort by volume
            return entry1.Inventory.Mineral.Volume > entry2.Inventory.Mineral.Volume;
        }

        #endregion
    }
}
