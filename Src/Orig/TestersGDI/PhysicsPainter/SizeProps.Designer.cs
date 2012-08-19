namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class SizeProps
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
			this.txtMaxSize = new System.Windows.Forms.TextBox();
			this.txtMinSize = new System.Windows.Forms.TextBox();
			this.trkSize = new System.Windows.Forms.TrackBar();
			this.radRandomSize = new System.Windows.Forms.RadioButton();
			this.radFixedSize = new System.Windows.Forms.RadioButton();
			this.radDrawSize = new System.Windows.Forms.RadioButton();
			this.cboCollisionStyle = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.chkTemporary = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.trkSize)).BeginInit();
			this.SuspendLayout();
			// 
			// txtMaxSize
			// 
			this.txtMaxSize.Location = new System.Drawing.Point(179, 131);
			this.txtMaxSize.Name = "txtMaxSize";
			this.txtMaxSize.Size = new System.Drawing.Size(57, 20);
			this.txtMaxSize.TabIndex = 12;
			this.txtMaxSize.Text = "500";
			this.txtMaxSize.TextChanged += new System.EventHandler(this.txtMaxSize_TextChanged);
			// 
			// txtMinSize
			// 
			this.txtMinSize.Location = new System.Drawing.Point(9, 131);
			this.txtMinSize.Name = "txtMinSize";
			this.txtMinSize.Size = new System.Drawing.Size(57, 20);
			this.txtMinSize.TabIndex = 11;
			this.txtMinSize.Text = "100";
			this.txtMinSize.TextChanged += new System.EventHandler(this.txtMinSize_TextChanged);
			// 
			// trkSize
			// 
			this.trkSize.Location = new System.Drawing.Point(9, 151);
			this.trkSize.Maximum = 1000;
			this.trkSize.Name = "trkSize";
			this.trkSize.Size = new System.Drawing.Size(227, 29);
			this.trkSize.TabIndex = 10;
			this.trkSize.TickFrequency = 125;
			this.trkSize.Value = 500;
			this.trkSize.Scroll += new System.EventHandler(this.trkSize_Scroll);
			// 
			// radRandomSize
			// 
			this.radRandomSize.AutoSize = true;
			this.radRandomSize.Location = new System.Drawing.Point(9, 35);
			this.radRandomSize.Name = "radRandomSize";
			this.radRandomSize.Size = new System.Drawing.Size(88, 17);
			this.radRandomSize.TabIndex = 7;
			this.radRandomSize.Text = "Random Size";
			this.radRandomSize.UseVisualStyleBackColor = true;
			this.radRandomSize.CheckedChanged += new System.EventHandler(this.Size_CheckedChanged);
			// 
			// radFixedSize
			// 
			this.radFixedSize.AutoSize = true;
			this.radFixedSize.Location = new System.Drawing.Point(9, 58);
			this.radFixedSize.Name = "radFixedSize";
			this.radFixedSize.Size = new System.Drawing.Size(73, 17);
			this.radFixedSize.TabIndex = 9;
			this.radFixedSize.Text = "Fixed Size";
			this.radFixedSize.UseVisualStyleBackColor = true;
			this.radFixedSize.CheckedChanged += new System.EventHandler(this.Size_CheckedChanged);
			// 
			// radDrawSize
			// 
			this.radDrawSize.AutoSize = true;
			this.radDrawSize.Checked = true;
			this.radDrawSize.Location = new System.Drawing.Point(9, 12);
			this.radDrawSize.Name = "radDrawSize";
			this.radDrawSize.Size = new System.Drawing.Size(73, 17);
			this.radDrawSize.TabIndex = 8;
			this.radDrawSize.TabStop = true;
			this.radDrawSize.Text = "Draw Size";
			this.radDrawSize.UseVisualStyleBackColor = true;
			this.radDrawSize.CheckedChanged += new System.EventHandler(this.Size_CheckedChanged);
			// 
			// cboCollisionStyle
			// 
			this.cboCollisionStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboCollisionStyle.FormattingEnabled = true;
			this.cboCollisionStyle.Location = new System.Drawing.Point(111, 77);
			this.cboCollisionStyle.Name = "cboCollisionStyle";
			this.cboCollisionStyle.Size = new System.Drawing.Size(102, 21);
			this.cboCollisionStyle.TabIndex = 15;
			this.cboCollisionStyle.SelectedIndexChanged += new System.EventHandler(this.cboCollisionStyle_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.SystemColors.WindowText;
			this.label1.Location = new System.Drawing.Point(108, 61);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(86, 13);
			this.label1.TabIndex = 16;
			this.label1.Text = "Collision Style";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.ForeColor = System.Drawing.SystemColors.WindowText;
			this.label2.Location = new System.Drawing.Point(108, 134);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(31, 13);
			this.label2.TabIndex = 17;
			this.label2.Text = "Size";
			// 
			// chkTemporary
			// 
			this.chkTemporary.AutoSize = true;
			this.chkTemporary.Location = new System.Drawing.Point(111, 22);
			this.chkTemporary.Name = "chkTemporary";
			this.chkTemporary.Size = new System.Drawing.Size(76, 17);
			this.chkTemporary.TabIndex = 18;
			this.chkTemporary.Text = "Temporary";
			this.chkTemporary.UseVisualStyleBackColor = true;
			this.chkTemporary.CheckedChanged += new System.EventHandler(this.chkTemporary_CheckedChanged);
			// 
			// SizeProps
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.chkTemporary);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cboCollisionStyle);
			this.Controls.Add(this.txtMaxSize);
			this.Controls.Add(this.txtMinSize);
			this.Controls.Add(this.trkSize);
			this.Controls.Add(this.radRandomSize);
			this.Controls.Add(this.radFixedSize);
			this.Controls.Add(this.radDrawSize);
			this.Name = "SizeProps";
			this.Size = new System.Drawing.Size(250, 185);
			((System.ComponentModel.ISupportInitialize)(this.trkSize)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox txtMaxSize;
		private System.Windows.Forms.TextBox txtMinSize;
		private System.Windows.Forms.TrackBar trkSize;
		private System.Windows.Forms.RadioButton radRandomSize;
		private System.Windows.Forms.RadioButton radFixedSize;
		private System.Windows.Forms.RadioButton radDrawSize;
		private System.Windows.Forms.ComboBox cboCollisionStyle;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.CheckBox chkTemporary;
	}
}
