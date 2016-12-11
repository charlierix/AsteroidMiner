using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    /// <summary>
    /// This class is resposnsible for giving a ship to the editor, building a new ship from the edited, distributing
    /// parts.  It a has a very specific, singular purpose
    /// </summary>
    /// <remarks>
    /// This class only stays instantiated during a ship edit
    /// 
    /// The station classes are a bit of a mess.  There's no clear model and view.  So inventory is stored in different ways
    /// 
    /// There's just enough complexity and loose member variables that this dedicated class is needed
    /// </remarks>
    public class EditShipTransfer
    {
        #region Declaration Section

        private readonly Player _player;
        private readonly SpaceDockPanel _spaceDock;

        private readonly Editor _editor;
        private readonly EditorOptions _editorOptions;

        private readonly World _world;
        private readonly int _material_Ship;
        private readonly Map _map;
        private readonly ShipExtraArgs _shipExtra;

        private Tuple<Cargo_ShipPart, PartDesignBase>[] _fromCargo = null;
        private Tuple<InventoryEntry, PartDesignBase>[] _fromNearby = null;
        private Tuple<InventoryEntry, PartDesignBase>[] _fromHangar = null;
        private ShipPartDNA[] _fromShip = null;

        #endregion

        #region Constructor

        public EditShipTransfer(Player player, SpaceDockPanel spaceDock, Editor editor, EditorOptions editorOptions, World world, int material_Ship, Map map, ShipExtraArgs shipExtra)
        {
            _player = player;
            _spaceDock = spaceDock;
            _editor = editor;
            _editorOptions = editorOptions;
            _world = world;
            _material_Ship = material_Ship;
            _map = map;
            _shipExtra = shipExtra;
        }

        #endregion

        /// <summary>
        /// This gathers parts and sets up the editor
        /// </summary>
        public void EditShip(string title, UIElement managementControl)
        {
            PartDesignBase[] combinedParts = GatherParts(false);

            // Fix the orientations (or they will be random when dragged to the surface, which is really annoying to manually fix)
            foreach (PartDesignBase part in combinedParts)
            {
                part.Orientation = Quaternion.Identity;
            }

            _editor.SetupEditor(title, combinedParts, managementControl);

            #region Show ship

            ShipDNA dna = _player.Ship.GetNewDNA();

            SortedList<int, List<DesignPart>> partsByLayer = new SortedList<int, List<DesignPart>>();
            foreach (int layerIndex in dna.PartsByLayer.Keys)
            {
                partsByLayer.Add(layerIndex, dna.PartsByLayer[layerIndex].Select(o => CreateDesignPart(o, _editorOptions)).ToList());
            }

            _editor.SetDesign(dna.ShipName, dna.LayerNames, partsByLayer);

            #endregion

            // Remember what parts came from the ship
            _fromShip = dna.PartsByLayer.
                SelectMany(o => o.Value).
                ToArray();
        }

        /// <summary>
        /// This creates a new ship and puts syncronizes part
        /// </summary>
        public bool ShipEdited()
        {
            ShipDNA newDNA = GetDNAFromEditor(_editor);

            ShipPlayer newShip = null;
            try
            {
                // Create the new ship
                newShip = ShipPlayer.GetNewShip(newDNA, _world, _material_Ship, _map, _shipExtra);
            }
            catch (Exception ex)
            {
                return false;
            }

            TransferContainers(_player.Ship, newShip);

            List<ShipPartDNA> unaccountedParts = newDNA.PartsByLayer.
                SelectMany(o => o.Value).
                ToList();

            Cargo[] remainder = TransferCargo(_player.Ship, newShip, unaccountedParts);

            RedistributeParts(remainder, unaccountedParts);

            newShip.RecalculateMass();

            SwapShip(_player, newShip);

            return true;
        }

        #region Private Methods

        private PartDesignBase[] GatherParts(bool isFinalModel)
        {
            // CargoBay
            _fromCargo = _player.Ship.CargoBays == null ?
                new Tuple<Cargo_ShipPart, PartDesignBase>[0] :
                _player.Ship.CargoBays.GetCargoSnapshot().
                    Select(o => o as Cargo_ShipPart).
                    Where(o => o != null).
                    Select(o => Tuple.Create(o, BotConstructor.GetPartDesign(o.PartDNA, _editorOptions, isFinalModel))).
                    ToArray();

            // Nearby
            _fromNearby = GatherParts_FromPanel(_spaceDock.pnlNearbyItems.Children, _editorOptions, isFinalModel);

            // Hangar
            _fromHangar = GatherParts_FromPanel(_spaceDock.pnlHangar.Children, _editorOptions, isFinalModel);

            // Combine them
            return _fromCargo.Select(o => o.Item2).
                Concat(_fromNearby.Select(o => o.Item2)).
                Concat(_fromHangar.Select(o => o.Item2)).
                ToArray();
        }

        private static Tuple<InventoryEntry, PartDesignBase>[] GatherParts_FromPanel(UIElementCollection panel, EditorOptions editorOptions, bool isFinalModel)
        {
            var retVal = new List<Tuple<InventoryEntry, PartDesignBase>>();

            foreach (var item in panel)
            {
                InventoryEntry inventory = item as InventoryEntry;
                if (inventory == null || inventory.Inventory.Part == null)
                {
                    continue;
                }

                PartDesignBase part = BotConstructor.GetPartDesign(inventory.Inventory.Part, editorOptions, isFinalModel);

                retVal.Add(Tuple.Create(inventory, part));
            }

            return retVal.ToArray();
        }

        private static DesignPart CreateDesignPart(ShipPartDNA dna, EditorOptions options)
        {
            DesignPart retVal = new DesignPart(options)
            {
                Part2D = null,      // setting 2D to null will tell the editor that the part can't be resized or copied, only moved around
                Part3D = BotConstructor.GetPartDesign(dna, options, false),
            };

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = retVal.Part3D.Model;
            retVal.Model = visual;

            return retVal;
        }

        private static ShipDNA GetDNAFromEditor(Editor editor)
        {
            // Get dna from editor
            string name;
            List<string> layerNames;
            SortedList<int, List<DesignPart>> partsByLayer;
            editor.GetDesign(out name, out layerNames, out partsByLayer);

            // Create ship dna
            ShipDNA retVal = new ShipDNA()
            {
                ShipName = name,
                LayerNames = layerNames,
                PartsByLayer = new SortedList<int, List<ShipPartDNA>>(),
            };

            foreach (int layerIndex in partsByLayer.Keys)
            {
                retVal.PartsByLayer.Add(layerIndex, partsByLayer[layerIndex].Select(o => o.Part3D.GetDNA()).ToList());
            }

            return retVal;
        }

        private void RedistributeParts(Cargo[] remainder, List<ShipPartDNA> unaccountedParts)
        {
            // Look for parts that are currently in the station, but are now also part of the ship
            foreach (var inventory in _fromNearby.Concat(_fromHangar))
            {
                if (IsInList(unaccountedParts, inventory.Item1.Inventory.Part, true))
                {
                    // This is now part of the ship.  Remove from the station
                    RemoveFromStation(inventory.Item1);
                }
            }

            // These two are external to the station's current inventory, so they need be processed after looking at the
            // current inventory

            foreach (Cargo cargo in remainder)
            {
                StoreInStation(cargo);
            }

            // Remove unnaccounted that came from the old ship
            foreach (ShipPartDNA dna in _fromShip)
            {
                if (!IsInList(unaccountedParts, dna, true))
                {
                    StoreInStation(dna);
                }
            }

            if (unaccountedParts.Count > 0)
            {
                // These are parts that are in the new ship.  They should have come from the old ship, or cargo bays, or nearby, or hangar
                throw new ApplicationException("There are still unnaccounted parts");
            }
        }

        private void StoreInStation(ShipPartDNA dna)
        {
            Inventory inventory = new Inventory(dna);
            _spaceDock.AddInventory(inventory, true);
        }
        private void StoreInStation(Cargo cargo)
        {
            Inventory inventory;
            if (cargo is Cargo_Mineral)
            {
                Cargo_Mineral cargoMineral = (Cargo_Mineral)cargo;
                MineralDNA mineralDNA = ItemOptionsAstMin2D.GetMineral(cargoMineral.MineralType, cargoMineral.Volume);
                inventory = new Inventory(mineralDNA);
            }
            else if (cargo is Cargo_ShipPart)
            {
                Cargo_ShipPart cargoPart = (Cargo_ShipPart)cargo;
                inventory = new Inventory(cargoPart.PartDNA);
            }
            else
            {
                throw new ApplicationException("Unknown type of cargo: " + cargo.GetType().ToString());
            }

            _spaceDock.AddInventory(inventory, true);
        }
        private void RemoveFromStation(InventoryEntry inventory)
        {
            _spaceDock.RemoveInventory(inventory, true);
        }

        private static void TransferContainers(Bot from, Bot to)
        {
            // Put as much quantity back as will fit.  Overflow just gets thrown away
            if (to.Energy != null && from.Energy != null)
            {
                to.Energy.QuantityCurrent = from.Energy.QuantityCurrent;
            }

            if (to.Plasma != null && from.Plasma != null)
            {
                to.Plasma.QuantityCurrent = from.Plasma.QuantityCurrent;
            }

            if (to.Fuel != null && from.Fuel != null)
            {
                to.Fuel.QuantityCurrent = from.Fuel.QuantityCurrent;
            }

            if (to.Ammo != null && from.Ammo != null)
            {
                to.Ammo.QuantityCurrent = from.Ammo.QuantityCurrent;
            }
        }
        private static Cargo[] TransferCargo(Bot from, Bot to, List<ShipPartDNA> unaccountedParts)
        {
            if (from.CargoBays == null)
            {
                return new Cargo[0];
            }

            if (to.CargoBays == null)
            {
                return from.CargoBays.GetCargoSnapshot();
            }

            List<Cargo> retVal = new List<Cargo>();

            foreach (Cargo cargo in from.CargoBays.GetCargoSnapshot())
            {
                if (cargo is Cargo_ShipPart && IsInList(unaccountedParts, ((Cargo_ShipPart)cargo).PartDNA, true))
                {
                    // This cargo is now part of the ship.  Don't transfer to the new cargo
                    continue;
                }

                if (!to.CargoBays.Add(cargo))
                {
                    retVal.Add(cargo);
                }
            }

            return retVal.ToArray();
        }

        private static void SwapShip(Player player, ShipPlayer newShip)
        {
            Point3D position = player.Ship.PositionWorld;
            player.Ship = null;

            newShip.PhysicsBody.Position = position;
            newShip.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
            newShip.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);

            player.Ship = newShip;
        }

        private static bool IsInList(List<ShipPartDNA> parts, ShipPartDNA part, bool removeIfFound)
        {
            int index = 0;
            while (index < parts.Count)
            {
                if (parts[index].IsEqual(part))
                {
                    if (removeIfFound)
                    {
                        parts.RemoveAt(index);
                    }

                    return true;
                }

                index++;
            }

            return false;
        }

        #endregion
    }
}
