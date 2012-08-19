using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.Orig.HelperClassesGDI;
using Game.Orig.HelperClassesGDI.Controls;
using Game.Orig.Map;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public partial class ShipProps : PiePanel
	{
		#region Declaration Section

        private const string BUTTON_MAIN = "Main";
        private const string BUTTON_TRACTOR = "Tractor";
		private const string BUTTON_GUN = "Gun";

        private LargeMapViewer2D _picturebox = null;
		private ShipController _shipController = null;
		private SimpleMap _map = null;

		#endregion

		#region Constructor

		public ShipProps()
		{
			InitializeComponent();

            //	Position Everything
            Resized();

            // Add Menu Buttons
            piePanelMenu1.AddButton(BUTTON_MAIN);
            piePanelMenu1.AddButton(BUTTON_TRACTOR);
			piePanelMenu1.AddButton(BUTTON_GUN);
		}

		#endregion

		#region Public Methods

		public void SetPointers(LargeMapViewer2D picturebox, ShipController shipController, SimpleMap map)
		{
            _picturebox = picturebox;
			_shipController = shipController;
			_map = map;

            shipPropsMain1.SetPointers(picturebox, shipController);
			shipPropsTractor1.SetPointers(shipController, map);
			shipPropsGun1.SetPointers(shipController);

            _shipController.FinishedSetup();

			ShowPropertyTab(shipPropsMain1);
		}

		#endregion

        #region Misc Control Events

        private void piePanelMenu1_DrawButton(object sender, PieMenuDrawButtonArgs e)
        {
            switch (e.Name)
            {
                case BUTTON_MAIN:
                    e.Graphics.DrawString("Main", new Font("Arial", 8), Brushes.Black, 0, e.ButtonSize - 13);
                    break;

                case BUTTON_TRACTOR:
					e.Graphics.DrawString("Tractor", new Font("Arial", 8), Brushes.Black, 0, e.ButtonSize - 13);
                    break;

				case BUTTON_GUN:
					e.Graphics.DrawString("Gun", new Font("Arial", 8), Brushes.Black, 0, e.ButtonSize - 13);
					break;

                default:
                    MessageBox.Show("Unknown Button: " + e.Name, "Ship Props - Draw Button", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
        private void piePanelMenu1_ButtonClicked(object sender, PieMenuButtonClickedArgs e)
        {
			switch (e.Name)
			{
				case BUTTON_MAIN:
					ShowPropertyTab(shipPropsMain1);
					break;

				case BUTTON_TRACTOR:
					ShowPropertyTab(shipPropsTractor1);
					break;

				case BUTTON_GUN:
					ShowPropertyTab(shipPropsGun1);
					break;

				default:
					MessageBox.Show("Unknown Button: " + e.Name, "Ship Props - Menu Button Clicked", MessageBoxButtons.OK, MessageBoxIcon.Error);
					break;
			}
        }

        #endregion
        #region Overrides

        protected override void OnBackColorChanged(EventArgs e)
        {
            piePanelMenu1.BackColor = SystemColors.Control;

            //shipPropsMain1.BackColor = this.BackColor;

            base.OnBackColorChanged(e);
        }

        protected override void OnResize(EventArgs e)
        {
            Resized();
            base.OnResize(e);
        }

        #endregion

        #region Private Methods

        private void Resized()
        {
            if (piePanelMenu1 == null || shipPropsMain1 == null)
            {
                //	OnResize is getting called before the child controls get created
                return;
            }

            piePanelMenu1.Left = 0;
            piePanelMenu1.Top = 0;
            piePanelMenu1.Width = this.Width;
            piePanelMenu1.Height = this.Height - shipPropsMain1.Height;

            shipPropsMain1.Left = 0;
			shipPropsMain1.Top = piePanelMenu1.Height;

			shipPropsTractor1.Width = shipPropsMain1.Width;
			shipPropsTractor1.Height = shipPropsMain1.Height;
			shipPropsTractor1.Left = 0;
			shipPropsTractor1.Top = piePanelMenu1.Height;

			shipPropsGun1.Width = shipPropsMain1.Width;
			shipPropsGun1.Height = shipPropsMain1.Height;
			shipPropsGun1.Left = 0;
			shipPropsGun1.Top = piePanelMenu1.Height;
		}

		/// <summary>
		/// This shows the tab passed in, and hides all others
		/// </summary>
		private void ShowPropertyTab(PiePanelBottom propertyTab)
		{
			foreach (Control childControl in this.Controls)
			{
				if (childControl is PiePanelBottom)
				{
					if (childControl == propertyTab)
					{
						childControl.Visible = true;
					}
					else
					{
						childControl.Visible = false;
					}
				}
			}
		}

        #endregion
    }
}
