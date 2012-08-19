namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class VelocityProps
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
			this.vectorPanel1 = new Game.Orig.TestersGDI.PhysicsPainter.VectorPanel();
			this.chkRandomVelocity = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.txtMaxVelocity = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.trkAngularVelocity = new System.Windows.Forms.TrackBar();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.zeroToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.txtAngularVelocityRight = new System.Windows.Forms.TextBox();
			this.txtAngularVelocityLeft = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.radAngularVelocityRandom = new System.Windows.Forms.RadioButton();
			this.radAngularVelocityFixed = new System.Windows.Forms.RadioButton();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.trkAngularVelocity)).BeginInit();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// vectorPanel1
			// 
			this.vectorPanel1.BackColor = System.Drawing.SystemColors.Window;
			this.vectorPanel1.Cursor = System.Windows.Forms.Cursors.Cross;
			this.vectorPanel1.Diameter = 102;
			this.vectorPanel1.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.vectorPanel1.Location = new System.Drawing.Point(115, 51);
			this.vectorPanel1.Multiplier = 1;
			this.vectorPanel1.Name = "vectorPanel1";
			this.vectorPanel1.Size = new System.Drawing.Size(102, 102);
			this.vectorPanel1.TabIndex = 17;
			this.vectorPanel1.ValueChanged += new System.EventHandler(this.vectorPanel1_ValueChanged);
			this.vectorPanel1.MultiplierChanged += new System.EventHandler(this.vectorPanel1_MultiplierChanged);
			// 
			// chkRandomVelocity
			// 
			this.chkRandomVelocity.AutoSize = true;
			this.chkRandomVelocity.Location = new System.Drawing.Point(115, 28);
			this.chkRandomVelocity.Name = "chkRandomVelocity";
			this.chkRandomVelocity.Size = new System.Drawing.Size(66, 17);
			this.chkRandomVelocity.TabIndex = 18;
			this.chkRandomVelocity.Text = "Random";
			this.chkRandomVelocity.UseVisualStyleBackColor = true;
			this.chkRandomVelocity.CheckedChanged += new System.EventHandler(this.chkRandomVelocity_CheckedChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(212, 143);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(27, 13);
			this.label3.TabIndex = 16;
			this.label3.Text = "Max";
			// 
			// txtMaxVelocity
			// 
			this.txtMaxVelocity.Location = new System.Drawing.Point(205, 159);
			this.txtMaxVelocity.Name = "txtMaxVelocity";
			this.txtMaxVelocity.Size = new System.Drawing.Size(37, 20);
			this.txtMaxVelocity.TabIndex = 15;
			this.txtMaxVelocity.Text = "300";
			this.txtMaxVelocity.TextChanged += new System.EventHandler(this.txtMaxVelocity_TextChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(112, 12);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(52, 13);
			this.label2.TabIndex = 14;
			this.label2.Text = "Velocity";
			// 
			// trkAngularVelocity
			// 
			this.trkAngularVelocity.ContextMenuStrip = this.contextMenuStrip1;
			this.trkAngularVelocity.Location = new System.Drawing.Point(3, 5);
			this.trkAngularVelocity.Maximum = 100;
			this.trkAngularVelocity.Name = "trkAngularVelocity";
			this.trkAngularVelocity.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkAngularVelocity.Size = new System.Drawing.Size(29, 174);
			this.trkAngularVelocity.TabIndex = 19;
			this.trkAngularVelocity.TickFrequency = 25;
			this.trkAngularVelocity.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
			this.trkAngularVelocity.Value = 50;
			this.trkAngularVelocity.Scroll += new System.EventHandler(this.trkAngularVelocity_Scroll);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.zeroToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(108, 26);
			// 
			// zeroToolStripMenuItem
			// 
			this.zeroToolStripMenuItem.Name = "zeroToolStripMenuItem";
			this.zeroToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.zeroToolStripMenuItem.Text = "Zero";
			this.zeroToolStripMenuItem.Click += new System.EventHandler(this.zeroToolStripMenuItem_Click);
			// 
			// txtAngularVelocityRight
			// 
			this.txtAngularVelocityRight.Location = new System.Drawing.Point(35, 159);
			this.txtAngularVelocityRight.Name = "txtAngularVelocityRight";
			this.txtAngularVelocityRight.Size = new System.Drawing.Size(37, 20);
			this.txtAngularVelocityRight.TabIndex = 20;
			this.txtAngularVelocityRight.Text = ".33";
			this.txtAngularVelocityRight.TextChanged += new System.EventHandler(this.txtAngularVelocityRight_TextChanged);
			// 
			// txtAngularVelocityLeft
			// 
			this.txtAngularVelocityLeft.Location = new System.Drawing.Point(35, 8);
			this.txtAngularVelocityLeft.Name = "txtAngularVelocityLeft";
			this.txtAngularVelocityLeft.Size = new System.Drawing.Size(37, 20);
			this.txtAngularVelocityLeft.TabIndex = 21;
			this.txtAngularVelocityLeft.Text = "-.33";
			this.txtAngularVelocityLeft.TextChanged += new System.EventHandler(this.txtAngularVelocityLeft_TextChanged);
			// 
			// label1
			// 
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(32, 62);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(52, 30);
			this.label1.TabIndex = 22;
			this.label1.Text = "Angular Velocity";
			// 
			// radAngularVelocityRandom
			// 
			this.radAngularVelocityRandom.AutoSize = true;
			this.radAngularVelocityRandom.Location = new System.Drawing.Point(35, 94);
			this.radAngularVelocityRandom.Name = "radAngularVelocityRandom";
			this.radAngularVelocityRandom.Size = new System.Drawing.Size(65, 17);
			this.radAngularVelocityRandom.TabIndex = 23;
			this.radAngularVelocityRandom.Text = "Random";
			this.radAngularVelocityRandom.UseVisualStyleBackColor = true;
			this.radAngularVelocityRandom.CheckedChanged += new System.EventHandler(this.radAngularVelocity_CheckedChanged);
			// 
			// radAngularVelocityFixed
			// 
			this.radAngularVelocityFixed.AutoSize = true;
			this.radAngularVelocityFixed.Checked = true;
			this.radAngularVelocityFixed.Location = new System.Drawing.Point(35, 112);
			this.radAngularVelocityFixed.Name = "radAngularVelocityFixed";
			this.radAngularVelocityFixed.Size = new System.Drawing.Size(50, 17);
			this.radAngularVelocityFixed.TabIndex = 24;
			this.radAngularVelocityFixed.TabStop = true;
			this.radAngularVelocityFixed.Text = "Fixed";
			this.radAngularVelocityFixed.UseVisualStyleBackColor = true;
			this.radAngularVelocityFixed.CheckedChanged += new System.EventHandler(this.radAngularVelocity_CheckedChanged);
			// 
			// VelocityProps
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.txtAngularVelocityLeft);
			this.Controls.Add(this.radAngularVelocityRandom);
			this.Controls.Add(this.radAngularVelocityFixed);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.trkAngularVelocity);
			this.Controls.Add(this.txtAngularVelocityRight);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.vectorPanel1);
			this.Controls.Add(this.txtMaxVelocity);
			this.Controls.Add(this.chkRandomVelocity);
			this.Controls.Add(this.label2);
			this.Name = "VelocityProps";
			this.Size = new System.Drawing.Size(250, 185);
			((System.ComponentModel.ISupportInitialize)(this.trkAngularVelocity)).EndInit();
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private VectorPanel vectorPanel1;
		private System.Windows.Forms.CheckBox chkRandomVelocity;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtMaxVelocity;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TrackBar trkAngularVelocity;
		private System.Windows.Forms.TextBox txtAngularVelocityRight;
		private System.Windows.Forms.TextBox txtAngularVelocityLeft;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton radAngularVelocityRandom;
		private System.Windows.Forms.RadioButton radAngularVelocityFixed;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem zeroToolStripMenuItem;
	}
}
