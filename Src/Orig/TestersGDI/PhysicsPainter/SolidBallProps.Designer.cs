namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class SolidBallProps
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
			this.sizeProps1 = new Game.Orig.TestersGDI.PhysicsPainter.SizeProps();
			this.velocityProps1 = new Game.Orig.TestersGDI.PhysicsPainter.VelocityProps();
			this.SuspendLayout();
			// 
			// piePanelMenuTop1
			// 
			this.piePanelMenuTop1.Location = new System.Drawing.Point(23, 30);
			this.piePanelMenuTop1.Name = "piePanelMenuTop1";
			this.piePanelMenuTop1.Size = new System.Drawing.Size(250, 65);
			this.piePanelMenuTop1.TabIndex = 6;
			this.piePanelMenuTop1.ButtonClicked += new Game.Orig.HelperClassesGDI.Controls.PieMenuButtonClickedHandler(this.piePanelMenuTop1_ButtonClicked);
			this.piePanelMenuTop1.DrawButton += new Game.Orig.HelperClassesGDI.Controls.PieMenuDrawButtonHandler(this.piePanelMenuTop1_DrawButton);
			// 
			// sizeProps1
			// 
			this.sizeProps1.Location = new System.Drawing.Point(46, 137);
			this.sizeProps1.Name = "sizeProps1";
			this.sizeProps1.Size = new System.Drawing.Size(250, 185);
			this.sizeProps1.TabIndex = 7;
			// 
			// velocityProps1
			// 
			this.velocityProps1.Location = new System.Drawing.Point(81, 112);
			this.velocityProps1.Name = "velocityProps1";
			this.velocityProps1.Size = new System.Drawing.Size(250, 185);
			this.velocityProps1.TabIndex = 8;
			// 
			// SolidBallProps
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.velocityProps1);
			this.Controls.Add(this.sizeProps1);
			this.Controls.Add(this.piePanelMenuTop1);
			this.ExpandCollapseVisible = false;
			this.Name = "SolidBallProps";
			this.Radius = 250;
			this.Size = new System.Drawing.Size(250, 250);
			this.Controls.SetChildIndex(this.piePanelMenuTop1, 0);
			this.Controls.SetChildIndex(this.sizeProps1, 0);
			this.Controls.SetChildIndex(this.velocityProps1, 0);
			this.ResumeLayout(false);

		}

		#endregion

		private Game.Orig.HelperClassesGDI.Controls.PiePanelMenuTop piePanelMenuTop1;
		private SizeProps sizeProps1;
		private VelocityProps velocityProps1;









	}
}
