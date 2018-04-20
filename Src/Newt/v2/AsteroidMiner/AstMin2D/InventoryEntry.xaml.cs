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
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.ShipEditor;
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

        public void SetInventory(Inventory inventory, string name, decimal credits, double? creditsPercent, double? volumePercent, World world, string[] actionButtons, EditorOptions options)
        {
            // Icon
            pnlIcon.Content = BuildIcon(inventory, world, options, this);

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

            // Price
            lblPrice.Text = credits.ToString("N0");
            if (creditsPercent == null)
            {
                lblPricePercent.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblPricePercent.Text = GetPercentText(creditsPercent.Value);
            }

            // Volume
            lblVolume.Text = Math.Round(inventory.Volume, 2).ToString();
            if (volumePercent == null)
            {
                lblVolumePercent.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblVolumePercent.Text = GetPercentText(volumePercent.Value);
            }

            // Mass
            //lblMass.Text = Math.Round(inventory.Mass, 2).ToString();
            lblMass.Text = inventory.Mass.ToStringSignificantDigits(2);

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

        #region Private Methods

        private static Icon3D BuildIcon(Inventory inventory, World world, EditorOptions options, FrameworkElement parent)
        {
            Icon3D retVal = null;

            if (inventory.Ship != null)
            {
                retVal = new Icon3D("", inventory.Ship, world);       // don't want to autorotate the ship icons.  This is a 2D game, and the ships will always be viewed from the top
            }
            else if (inventory.Part != null)
            {
                retVal = new Icon3D(inventory.Part, options)
                {
                    AutoRotateOnMouseHover = true,
                    AutoRotateParent = parent,
                };
            }
            else if (inventory.Mineral != null)
            {
                retVal = new Icon3D(inventory.Mineral.MineralType)
                {
                    AutoRotateOnMouseHover = true,
                    AutoRotateParent = parent,
                };
            }

            if (retVal != null)
            {
                retVal.ShowName = false;
                retVal.ShowBorder = false;
            }

            return retVal;
        }

        private static string GetPercentText(double percent)
        {
            percent *= 100d;

            string number;
            if (Math.Abs(percent) > 1)
            {
                number = percent.ToInt_Round().ToString();
            }
            else
            {
                number = percent.ToStringSignificantDigits(1);
            }

            return number + "%";
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
