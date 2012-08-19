namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class PieBallProps
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
			this.radRandomSize = new System.Windows.Forms.RadioButton();
			this.radDrawSize = new System.Windows.Forms.RadioButton();
			this.radFixedSize = new System.Windows.Forms.RadioButton();
			this.trkSize = new System.Windows.Forms.TrackBar();
			this.txtMaxSize = new System.Windows.Forms.TextBox();
			this.txtMinSize = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.txtMaxVelocity = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.vectorPanel1 = new Game.Orig.TestersGDI.PhysicsPainter.VectorPanel();
			this.chkRandom = new System.Windows.Forms.CheckBox();
			this.cboCollisionStyle = new System.Windows.Forms.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.trkSize)).BeginInit();
			this.SuspendLayout();
			// 
			// radRandomSize
			// 
			this.radRandomSize.AutoSize = true;
			this.radRandomSize.Location = new System.Drawing.Point(7, 175);
			this.radRandomSize.Name = "radRandomSize";
			this.radRandomSize.Size = new System.Drawing.Size(88, 17);
			this.radRandomSize.TabIndex = 1;
			this.radRandomSize.Text = "Random Size";
			this.radRandomSize.UseVisualStyleBackColor = true;
			this.radRandomSize.CheckedChanged += new System.EventHandler(this.Size_CheckedChanged);
			// 
			// radDrawSize
			// 
			this.radDrawSize.AutoSize = true;
			this.radDrawSize.Checked = true;
			this.radDrawSize.Location = new System.Drawing.Point(7, 156);
			this.radDrawSize.Name = "radDrawSize";
			this.radDrawSize.Size = new System.Drawing.Size(73, 17);
			this.radDrawSize.TabIndex = 2;
			this.radDrawSize.TabStop = true;
			this.radDrawSize.Text = "Draw Size";
			this.radDrawSize.UseVisualStyleBackColor = true;
			this.radDrawSize.CheckedChanged += new System.EventHandler(this.Size_CheckedChanged);
			// 
			// radFixedSize
			// 
			this.radFixedSize.AutoSize = true;
			this.radFixedSize.Location = new System.Drawing.Point(101, 175);
			this.radFixedSize.Name = "radFixedSize";
			this.radFixedSize.Size = new System.Drawing.Size(73, 17);
			this.radFixedSize.TabIndex = 3;
			this.radFixedSize.Text = "Fixed Size";
			this.radFixedSize.UseVisualStyleBackColor = true;
			this.radFixedSize.CheckedChanged += new System.EventHandler(this.Size_CheckedChanged);
			// 
			// trkSize
			// 
			this.trkSize.Location = new System.Drawing.Point(7, 218);
			this.trkSize.Maximum = 1000;
			this.trkSize.Name = "trkSize";
			this.trkSize.Size = new System.Drawing.Size(227, 29);
			this.trkSize.TabIndex = 4;
			this.trkSize.TickFrequency = 125;
			this.trkSize.Value = 500;
			this.trkSize.Scroll += new System.EventHandler(this.trkSize_Scroll);
			// 
			// txtMaxSize
			// 
			this.txtMaxSize.Location = new System.Drawing.Point(177, 198);
			this.txtMaxSize.Name = "txtMaxSize";
			this.txtMaxSize.Size = new System.Drawing.Size(57, 20);
			this.txtMaxSize.TabIndex = 6;
			this.txtMaxSize.Text = "500";
			this.txtMaxSize.TextChanged += new System.EventHandler(this.txtMaxSize_TextChanged);
			// 
			// txtMinSize
			// 
			this.txtMinSize.Location = new System.Drawing.Point(7, 198);
			this.txtMinSize.Name = "txtMinSize";
			this.txtMinSize.Size = new System.Drawing.Size(57, 20);
			this.txtMinSize.TabIndex = 5;
			this.txtMinSize.Text = "100";
			this.txtMinSize.TextChanged += new System.EventHandler(this.txtMinSize_TextChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(106, 201);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(31, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Size";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(85, 30);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(52, 13);
			this.label2.TabIndex = 9;
			this.label2.Text = "Velocity";
			// 
			// txtMaxVelocity
			// 
			this.txtMaxVelocity.Location = new System.Drawing.Point(119, 112);
			this.txtMaxVelocity.Name = "txtMaxVelocity";
			this.txtMaxVelocity.Size = new System.Drawing.Size(45, 20);
			this.txtMaxVelocity.TabIndex = 10;
			this.txtMaxVelocity.Text = "300";
			this.txtMaxVelocity.TextChanged += new System.EventHandler(this.txtMaxVelocity_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(119, 96);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(67, 13);
			this.label3.TabIndex = 11;
			this.label3.Text = "Max Velocity";
			// 
			// vectorPanel1
			// 
			this.vectorPanel1.BackColor = System.Drawing.SystemColors.Window;
			this.vectorPanel1.Cursor = System.Windows.Forms.Cursors.Cross;
			this.vectorPanel1.Diameter = 110;
			this.vectorPanel1.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.vectorPanel1.Location = new System.Drawing.Point(10, 27);
			this.vectorPanel1.Multiplier = 1;
			this.vectorPanel1.Name = "vectorPanel1";
			this.vectorPanel1.Size = new System.Drawing.Size(110, 110);
			this.vectorPanel1.TabIndex = 12;
			// 
			// chkRandom
			// 
			this.chkRandom.AutoSize = true;
			this.chkRandom.Location = new System.Drawing.Point(7, 12);
			this.chkRandom.Name = "chkRandom";
			this.chkRandom.Size = new System.Drawing.Size(66, 17);
			this.chkRandom.TabIndex = 13;
			this.chkRandom.Text = "Random";
			this.chkRandom.UseVisualStyleBackColor = true;
			this.chkRandom.CheckedChanged += new System.EventHandler(this.chkRandom_CheckedChanged);
			// 
			// cboCollisionStyle
			// 
			this.cboCollisionStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboCollisionStyle.FormattingEnabled = true;
			this.cboCollisionStyle.Location = new System.Drawing.Point(119, 143);
			this.cboCollisionStyle.Name = "cboCollisionStyle";
			this.cboCollisionStyle.Size = new System.Drawing.Size(102, 21);
			this.cboCollisionStyle.TabIndex = 14;
			this.cboCollisionStyle.SelectedIndexChanged += new System.EventHandler(this.cboCollisionStyle_SelectedIndexChanged);
			// 
			// PieBallProps
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.cboCollisionStyle);
			this.Controls.Add(this.vectorPanel1);
			this.Controls.Add(this.chkRandom);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.txtMaxVelocity);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtMaxSize);
			this.Controls.Add(this.txtMinSize);
			this.Controls.Add(this.trkSize);
			this.Controls.Add(this.radRandomSize);
			this.Controls.Add(this.radFixedSize);
			this.Controls.Add(this.radDrawSize);
			this.ExpandCollapseVisible = false;
			this.Name = "PieBallProps";
			this.Radius = 250;
			this.Size = new System.Drawing.Size(250, 250);
			this.Controls.SetChildIndex(this.radDrawSize, 0);
			this.Controls.SetChildIndex(this.radFixedSize, 0);
			this.Controls.SetChildIndex(this.radRandomSize, 0);
			this.Controls.SetChildIndex(this.trkSize, 0);
			this.Controls.SetChildIndex(this.txtMinSize, 0);
			this.Controls.SetChildIndex(this.txtMaxSize, 0);
			this.Controls.SetChildIndex(this.label1, 0);
			this.Controls.SetChildIndex(this.label2, 0);
			this.Controls.SetChildIndex(this.txtMaxVelocity, 0);
			this.Controls.SetChildIndex(this.label3, 0);
			this.Controls.SetChildIndex(this.chkRandom, 0);
			this.Controls.SetChildIndex(this.vectorPanel1, 0);
			this.Controls.SetChildIndex(this.cboCollisionStyle, 0);
			((System.ComponentModel.ISupportInitialize)(this.trkSize)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RadioButton radRandomSize;
		private System.Windows.Forms.RadioButton radDrawSize;
		private System.Windows.Forms.RadioButton radFixedSize;
		private System.Windows.Forms.TrackBar trkSize;
		private System.Windows.Forms.TextBox txtMaxSize;
		private System.Windows.Forms.TextBox txtMinSize;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtMaxVelocity;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ToolTip toolTip1;
		private VectorPanel vectorPanel1;
		private System.Windows.Forms.CheckBox chkRandom;
		private System.Windows.Forms.ComboBox cboCollisionStyle;
	}
}
