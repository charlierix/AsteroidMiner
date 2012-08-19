using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.Orig.HelperClassesGDI.Controls;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public partial class GeneralProps : PiePanel
	{
		#region Declaration Section

		private const string BUTTON_DENSITY = "Density";

		#endregion

		#region Constructor

		public GeneralProps()
		{
			InitializeComponent();

			//	Position Everything
			Resized();

			// Add Menu Buttons
			piePanelMenuTop1.AddButton(BUTTON_DENSITY);
		}

		#endregion

		#region Public Methods

		public void SetPointers()
		{
		}

		#endregion

		#region Misc Control Events

		private void piePanelMenu1_DrawButton(object sender, PieMenuDrawButtonArgs e)
		{
			switch (e.Name)
			{
				case BUTTON_DENSITY:
					e.Graphics.DrawString("Density", new Font("Arial", 8), Brushes.Black, 0, e.ButtonSize - 13);
					break;

				default:
					MessageBox.Show("Unknown Button: " + e.Name, "General Props - Draw Button", MessageBoxButtons.OK, MessageBoxIcon.Error);
					break;
			}
		}
		private void piePanelMenu1_ButtonClicked(object sender, PieMenuButtonClickedArgs e)
		{
			switch (e.Name)
			{
				case BUTTON_DENSITY:
					ShowPropertyTab(generalPropsDensity1);
					break;

				default:
					MessageBox.Show("Unknown Button: " + e.Name, "General Props - Menu Button Clicked", MessageBoxButtons.OK, MessageBoxIcon.Error);
					break;
			}
		}

		#endregion
		#region Overrides

		protected override void OnBackColorChanged(EventArgs e)
		{
			piePanelMenuTop1.BackColor = SystemColors.Control;

			generalPropsDensity1.BackColor = this.BackColor;

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
			if (piePanelMenuTop1 == null || generalPropsDensity1 == null)
			{
				//	OnResize is getting called before the child controls get created
				return;
			}

			piePanelMenuTop1.Left = 0;
			piePanelMenuTop1.Top = 0;
			piePanelMenuTop1.Width = this.Width;
			piePanelMenuTop1.Height = this.Height - generalPropsDensity1.Height;

			generalPropsDensity1.Left = 0;
			generalPropsDensity1.Top = piePanelMenuTop1.Height;
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
