namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class ShipProps
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.shipPropsMain1 = new Game.Orig.TestersGDI.PhysicsPainter.ShipPropsMain();
			this.piePanelMenu1 = new Game.Orig.HelperClassesGDI.Controls.PiePanelMenuTop();
			this.shipPropsTractor1 = new Game.Orig.TestersGDI.PhysicsPainter.ShipPropsTractor();
			this.shipPropsGun1 = new Game.Orig.TestersGDI.PhysicsPainter.ShipPropsGun();
			this.SuspendLayout();
			// 
			// shipPropsMain1
			// 
			this.shipPropsMain1.Location = new System.Drawing.Point(63, 124);
			this.shipPropsMain1.Name = "shipPropsMain1";
			this.shipPropsMain1.Size = new System.Drawing.Size(250, 185);
			this.shipPropsMain1.TabIndex = 1;
			// 
			// piePanelMenu1
			// 
			this.piePanelMenu1.Dock = System.Windows.Forms.DockStyle.Top;
			this.piePanelMenu1.Location = new System.Drawing.Point(0, 0);
			this.piePanelMenu1.Name = "piePanelMenu1";
			this.piePanelMenu1.Size = new System.Drawing.Size(250, 65);
			this.piePanelMenu1.TabIndex = 2;
			this.piePanelMenu1.ButtonClicked += new Game.Orig.HelperClassesGDI.Controls.PieMenuButtonClickedHandler(this.piePanelMenu1_ButtonClicked);
			this.piePanelMenu1.DrawButton += new Game.Orig.HelperClassesGDI.Controls.PieMenuDrawButtonHandler(this.piePanelMenu1_DrawButton);
			// 
			// shipPropsTractor1
			// 
			this.shipPropsTractor1.Location = new System.Drawing.Point(102, 104);
			this.shipPropsTractor1.Name = "shipPropsTractor1";
			this.shipPropsTractor1.Size = new System.Drawing.Size(250, 185);
			this.shipPropsTractor1.TabIndex = 3;
			// 
			// shipPropsGun1
			// 
			this.shipPropsGun1.Location = new System.Drawing.Point(24, 162);
			this.shipPropsGun1.Name = "shipPropsGun1";
			this.shipPropsGun1.Size = new System.Drawing.Size(250, 185);
			this.shipPropsGun1.TabIndex = 4;
			// 
			// ShipProps
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.shipPropsTractor1);
			this.Controls.Add(this.shipPropsMain1);
			this.Controls.Add(this.piePanelMenu1);
			this.Controls.Add(this.shipPropsGun1);
			this.ExpandCollapseVisible = false;
			this.Name = "ShipProps";
			this.Radius = 250;
			this.Size = new System.Drawing.Size(250, 250);
			this.Controls.SetChildIndex(this.shipPropsGun1, 0);
			this.Controls.SetChildIndex(this.piePanelMenu1, 0);
			this.Controls.SetChildIndex(this.shipPropsMain1, 0);
			this.Controls.SetChildIndex(this.shipPropsTractor1, 0);
			this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.ToolTip toolTip1;
        private ShipPropsMain shipPropsMain1;
        private Game.Orig.HelperClassesGDI.Controls.PiePanelMenuTop piePanelMenu1;
		private ShipPropsTractor shipPropsTractor1;
		private ShipPropsGun shipPropsGun1;
	}
}
