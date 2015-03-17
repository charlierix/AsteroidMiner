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
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public partial class InventoryEntry : UserControl
    {
        #region Events

        public event EventHandler<InventoryActionClickedArgs> ActionClicked = null;

        //TODO: Figure out how to expose this so it can be an attached event.  These examples end up being a TON of code just for a stupid event
        //All I want is for the client to put this in a higher level element (so much discussion for such a simple thing):
        //<Grid InventoryEntry.ActionClicked="InventoryEntry_ActionClicked">
        //http://stackoverflow.com/questions/3779674/how-do-i-attach-property-to-a-custom-event
        //https://social.msdn.microsoft.com/Forums/vstudio/en-US/266ea9ee-3d6d-47f1-abbb-215d2dd8b40f/looking-for-a-working-attached-events-sample?forum=wpf

        #endregion

        #region Constructor

        public InventoryEntry()
        {
            InitializeComponent();

            DataContext = this;
        }

        #endregion

        #region Public Properties

        public Inventory Inventory
        {
            get;
            private set;
        }
        public string InventoryName
        {
            get;
            private set;
        }
        public decimal Credits
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void SetInventory(Inventory inventory, string name, decimal credits, World world, string[] actionButtons)
        {
            // Icon
            Icon3D icon = null;

            if (inventory.Ship != null)
            {
                icon = new Icon3D("", inventory.Ship, world);
            }
            else if (inventory.Mineral != null)
            {
                icon = new Icon3D(inventory.Mineral.MineralType);
            }

            if (icon != null)
            {
                icon.ShowName = false;
                icon.ShowBorder = false;
            }

            pnlIcon.Content = icon;

            // Name
            lblName.Text = name;

            if (inventory.Count == 1)
            {
                lblMultipleX.Visibility = Visibility.Collapsed;
                lblMultiple.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblMultipleX.Visibility = Visibility.Visible;
                lblMultiple.Visibility = Visibility.Visible;
                lblMultiple.Text = inventory.Count.ToString("N0");
            }

            // Quantities
            lblVolume.Text = Math.Round(inventory.Volume, 2).ToString();
            lblMass.Text = Math.Round(inventory.Volume, 2).ToString();
            lblPrice.Text = credits.ToString("N0");

            // Action Buttons
            pnlActionButtons.Children.Clear();

            switch (actionButtons.Length)
            {
                case 2:
                case 4:
                    pnlActionButtons.Columns = 2;
                    break;

                default:
                    pnlActionButtons.Columns = 3;
                    break;
            }

            pnlActionButtons.Rows = Convert.ToInt32(Math.Ceiling(actionButtons.Length / Convert.ToDouble(pnlActionButtons.Columns)));

            foreach (string action in actionButtons)
            {
                pnlActionButtons.Children.Add(new Button()
                {
                    Content = action
                });
            }

            // Store props
            this.Inventory = inventory;
            this.InventoryName = name;
            this.Credits = credits;
        }

        #endregion

        #region Event Listeners

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.ActionClicked != null)
                {
                    Button button = e.OriginalSource as Button;
                    if (button == null)
                    {
                        throw new ApplicationException("Expected original source to be button");
                    }

                    string action = button.Content as string;
                    if (action == null)
                    {
                        throw new ApplicationException("Expected button's content to be text");
                    }

                    this.ActionClicked(this, new InventoryActionClickedArgs(action, this.Inventory, this.InventoryName, this.Credits));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Inventory Entry", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    #region Class: InventoryActionClickedArgs

    public class InventoryActionClickedArgs : EventArgs
    {
        public InventoryActionClickedArgs(string action, Inventory inventory, string name, decimal credits)
        {
            this.Action = action;
            this.Inventory = inventory;
            this.Name = name;
            this.Credits = credits;
        }

        public readonly string Action;
        public readonly Inventory Inventory;
        public readonly string Name;
        public readonly decimal Credits;
    }

    #endregion
}
