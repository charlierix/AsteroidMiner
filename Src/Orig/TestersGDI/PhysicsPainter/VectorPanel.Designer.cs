namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class VectorPanel
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
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.displayItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.displayItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.btnNormalize = new System.Windows.Forms.ToolStripMenuItem();
			this.btnMaximize = new System.Windows.Forms.ToolStripMenuItem();
			this.btnRandom = new System.Windows.Forms.ToolStripMenuItem();
			this.btnNegate = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.btnZero = new System.Windows.Forms.ToolStripMenuItem();
			this.btnZeroX = new System.Windows.Forms.ToolStripMenuItem();
			this.btnZeroY = new System.Windows.Forms.ToolStripMenuItem();
			this.btnZeroZ = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.btnShowToolTip = new System.Windows.Forms.ToolStripMenuItem();
			this.txtMultiplier = new System.Windows.Forms.ToolStripTextBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.displayItem1,
            this.displayItem2,
            this.toolStripSeparator2,
            this.btnNormalize,
            this.btnMaximize,
            this.btnRandom,
            this.btnNegate,
            this.toolStripSeparator1,
            this.btnZero,
            this.btnZeroX,
            this.btnZeroY,
            this.btnZeroZ,
            this.toolStripSeparator3,
            this.btnShowToolTip,
            this.txtMultiplier});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(161, 287);
			this.contextMenuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip1_ItemClicked);
			// 
			// displayItem1
			// 
			this.displayItem1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.displayItem1.Name = "displayItem1";
			this.displayItem1.Size = new System.Drawing.Size(160, 22);
			this.displayItem1.Text = "coords";
			// 
			// displayItem2
			// 
			this.displayItem2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.displayItem2.Name = "displayItem2";
			this.displayItem2.Size = new System.Drawing.Size(160, 22);
			this.displayItem2.Text = "length";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(157, 6);
			// 
			// btnNormalize
			// 
			this.btnNormalize.Name = "btnNormalize";
			this.btnNormalize.Size = new System.Drawing.Size(160, 22);
			this.btnNormalize.Text = "Normalize";
			this.btnNormalize.ToolTipText = "Sets the length to one (maintain\'s the direction)";
			// 
			// btnMaximize
			// 
			this.btnMaximize.Name = "btnMaximize";
			this.btnMaximize.Size = new System.Drawing.Size(160, 22);
			this.btnMaximize.Text = "Maximize";
			this.btnMaximize.ToolTipText = "This keeps the direction, but sets the length to the max value";
			// 
			// btnRandom
			// 
			this.btnRandom.Name = "btnRandom";
			this.btnRandom.Size = new System.Drawing.Size(160, 22);
			this.btnRandom.Text = "Random";
			this.btnRandom.ToolTipText = "Sets to a random direction and length";
			// 
			// btnNegate
			// 
			this.btnNegate.Name = "btnNegate";
			this.btnNegate.Size = new System.Drawing.Size(160, 22);
			this.btnNegate.Text = "Negate";
			this.btnNegate.ToolTipText = "Negates the current vector";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(157, 6);
			// 
			// btnZero
			// 
			this.btnZero.Name = "btnZero";
			this.btnZero.Size = new System.Drawing.Size(160, 22);
			this.btnZero.Text = "Zero";
			this.btnZero.ToolTipText = "Sets to zero";
			// 
			// btnZeroX
			// 
			this.btnZeroX.Name = "btnZeroX";
			this.btnZeroX.Size = new System.Drawing.Size(160, 22);
			this.btnZeroX.Text = "Zero X";
			this.btnZeroX.ToolTipText = "Sets the X to zero";
			// 
			// btnZeroY
			// 
			this.btnZeroY.Name = "btnZeroY";
			this.btnZeroY.Size = new System.Drawing.Size(160, 22);
			this.btnZeroY.Text = "Zero Y";
			this.btnZeroY.ToolTipText = "Sets the Y to zero";
			// 
			// btnZeroZ
			// 
			this.btnZeroZ.Name = "btnZeroZ";
			this.btnZeroZ.Size = new System.Drawing.Size(160, 22);
			this.btnZeroZ.Text = "Zero Z";
			this.btnZeroZ.ToolTipText = "Sets the Z to zero";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(157, 6);
			// 
			// btnShowToolTip
			// 
			this.btnShowToolTip.Name = "btnShowToolTip";
			this.btnShowToolTip.Size = new System.Drawing.Size(160, 22);
			this.btnShowToolTip.Text = "Show ToolTip";
			// 
			// txtMultiplier
			// 
			this.txtMultiplier.AutoToolTip = true;
			this.txtMultiplier.Name = "txtMultiplier";
			this.txtMultiplier.Size = new System.Drawing.Size(100, 21);
			this.txtMultiplier.ToolTipText = "Max Vector Length";
			this.txtMultiplier.TextChanged += new System.EventHandler(this.txtMultiplier_TextChanged);
			// 
			// toolTip1
			// 
			this.toolTip1.Active = false;
			// 
			// VectorPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ContextMenuStrip = this.contextMenuStrip1;
			this.Cursor = System.Windows.Forms.Cursors.Cross;
			this.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.Name = "VectorPanel";
			this.contextMenuStrip1.ResumeLayout(false);
			this.contextMenuStrip1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem btnNormalize;
		private System.Windows.Forms.ToolStripMenuItem btnRandom;
		private System.Windows.Forms.ToolStripMenuItem btnNegate;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem btnZero;
		private System.Windows.Forms.ToolStripMenuItem btnZeroX;
		private System.Windows.Forms.ToolStripMenuItem btnZeroY;
		private System.Windows.Forms.ToolStripMenuItem btnZeroZ;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ToolStripMenuItem btnMaximize;
		private System.Windows.Forms.ToolStripMenuItem displayItem1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem btnShowToolTip;
		private System.Windows.Forms.ToolStripMenuItem displayItem2;
		private System.Windows.Forms.ToolStripTextBox txtMultiplier;
	}
}
