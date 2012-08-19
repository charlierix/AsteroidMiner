namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class GeneralProps
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
			this.piePanelMenuTop1 = new Game.Orig.HelperClassesGDI.Controls.PiePanelMenuTop();
			this.generalPropsDensity1 = new Game.Orig.TestersGDI.PhysicsPainter.GeneralPropsDensity();
			this.SuspendLayout();
			// 
			// piePanelMenuTop1
			// 
			this.piePanelMenuTop1.Location = new System.Drawing.Point(26, 31);
			this.piePanelMenuTop1.Name = "piePanelMenuTop1";
			this.piePanelMenuTop1.Size = new System.Drawing.Size(250, 65);
			this.piePanelMenuTop1.TabIndex = 1;
			this.piePanelMenuTop1.ButtonClicked += new Game.Orig.HelperClassesGDI.Controls.PieMenuButtonClickedHandler(this.piePanelMenu1_ButtonClicked);
			this.piePanelMenuTop1.DrawButton += new Game.Orig.HelperClassesGDI.Controls.PieMenuDrawButtonHandler(this.piePanelMenu1_DrawButton);
			// 
			// generalPropsDensity1
			// 
			this.generalPropsDensity1.Location = new System.Drawing.Point(69, 127);
			this.generalPropsDensity1.Name = "generalPropsDensity1";
			this.generalPropsDensity1.Size = new System.Drawing.Size(250, 185);
			this.generalPropsDensity1.TabIndex = 2;
			// 
			// GeneralProps
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.generalPropsDensity1);
			this.Controls.Add(this.piePanelMenuTop1);
			this.ExpandCollapseVisible = false;
			this.Name = "GeneralProps";
			this.Radius = 250;
			this.Size = new System.Drawing.Size(250, 250);
			this.Controls.SetChildIndex(this.piePanelMenuTop1, 0);
			this.Controls.SetChildIndex(this.generalPropsDensity1, 0);
			this.ResumeLayout(false);

		}

		#endregion

		private Game.Orig.HelperClassesGDI.Controls.PiePanelMenuTop piePanelMenuTop1;
		private GeneralPropsDensity generalPropsDensity1;
	}
}
