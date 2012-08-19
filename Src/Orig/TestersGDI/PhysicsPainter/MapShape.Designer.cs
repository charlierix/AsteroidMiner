namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class MapShape
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
			this.trkWidth = new System.Windows.Forms.TrackBar();
			this.label5 = new System.Windows.Forms.Label();
			this.trkHeight = new System.Windows.Forms.TrackBar();
			this.chkForceSquare = new System.Windows.Forms.CheckBox();
			this.lblSize = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.trkWidth)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkHeight)).BeginInit();
			this.SuspendLayout();
			// 
			// trkWidth
			// 
			this.trkWidth.Location = new System.Drawing.Point(22, 218);
			this.trkWidth.Maximum = 1000;
			this.trkWidth.Name = "trkWidth";
			this.trkWidth.Size = new System.Drawing.Size(225, 29);
			this.trkWidth.TabIndex = 65;
			this.trkWidth.TickFrequency = 200;
			this.trkWidth.Value = 200;
			this.trkWidth.Scroll += new System.EventHandler(this.trkWidth_Scroll);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.ForeColor = System.Drawing.SystemColors.WindowText;
			this.label5.Location = new System.Drawing.Point(112, 110);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(59, 13);
			this.label5.TabIndex = 64;
			this.label5.Text = "Map Size";
			// 
			// trkHeight
			// 
			this.trkHeight.Location = new System.Drawing.Point(3, 3);
			this.trkHeight.Maximum = 1000;
			this.trkHeight.Name = "trkHeight";
			this.trkHeight.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkHeight.Size = new System.Drawing.Size(29, 225);
			this.trkHeight.TabIndex = 66;
			this.trkHeight.TickFrequency = 200;
			this.trkHeight.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
			this.trkHeight.Value = 200;
			this.trkHeight.Scroll += new System.EventHandler(this.trkHeight_Scroll);
			// 
			// chkForceSquare
			// 
			this.chkForceSquare.AutoSize = true;
			this.chkForceSquare.Checked = true;
			this.chkForceSquare.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkForceSquare.Location = new System.Drawing.Point(38, 43);
			this.chkForceSquare.Name = "chkForceSquare";
			this.chkForceSquare.Size = new System.Drawing.Size(90, 17);
			this.chkForceSquare.TabIndex = 67;
			this.chkForceSquare.Text = "Force Square";
			this.chkForceSquare.UseVisualStyleBackColor = true;
			this.chkForceSquare.CheckedChanged += new System.EventHandler(this.chkForceSquare_CheckedChanged);
			// 
			// lblSize
			// 
			this.lblSize.Location = new System.Drawing.Point(91, 126);
			this.lblSize.Name = "lblSize";
			this.lblSize.Size = new System.Drawing.Size(100, 23);
			this.lblSize.TabIndex = 68;
			this.lblSize.Text = "lblSize";
			this.lblSize.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(125, 202);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 13);
			this.label1.TabIndex = 69;
			this.label1.Text = "Width";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(38, 110);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(38, 13);
			this.label2.TabIndex = 70;
			this.label2.Text = "Height";
			// 
			// MapShape
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.lblSize);
			this.Controls.Add(this.chkForceSquare);
			this.Controls.Add(this.trkWidth);
			this.Controls.Add(this.trkHeight);
			this.Controls.Add(this.label5);
			this.ExpandCollapseVisible = false;
			this.Name = "MapShape";
			this.Radius = 250;
			this.Size = new System.Drawing.Size(250, 250);
			this.Controls.SetChildIndex(this.label5, 0);
			this.Controls.SetChildIndex(this.trkHeight, 0);
			this.Controls.SetChildIndex(this.trkWidth, 0);
			this.Controls.SetChildIndex(this.chkForceSquare, 0);
			this.Controls.SetChildIndex(this.lblSize, 0);
			this.Controls.SetChildIndex(this.label1, 0);
			this.Controls.SetChildIndex(this.label2, 0);
			((System.ComponentModel.ISupportInitialize)(this.trkWidth)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkHeight)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TrackBar trkWidth;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TrackBar trkHeight;
		private System.Windows.Forms.CheckBox chkForceSquare;
		private System.Windows.Forms.Label lblSize;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
	}
}
