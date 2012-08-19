namespace Game.Orig.HelperClassesGDI.Controls
{
	partial class PiePanel
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
            this.expandCollapsePieButton1 = new Game.Orig.HelperClassesGDI.Controls.ExpandCollapsePieButton();
			this.SuspendLayout();
			// 
			// expandCollapsePieButton1
			// 
			this.expandCollapsePieButton1.BackColor = System.Drawing.SystemColors.ControlLight;
			this.expandCollapsePieButton1.Location = new System.Drawing.Point(0, 0);
			this.expandCollapsePieButton1.Name = "expandCollapsePieButton1";
			this.expandCollapsePieButton1.Radius = 300;
			this.expandCollapsePieButton1.Size = new System.Drawing.Size(300, 300);
			this.expandCollapsePieButton1.TabIndex = 0;
			// 
			// PiePanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.expandCollapsePieButton1);
			this.Name = "PiePanel";
			this.Size = new System.Drawing.Size(300, 300);
			this.ResumeLayout(false);

		}

		#endregion

		private ExpandCollapsePieButton expandCollapsePieButton1;
	}
}
