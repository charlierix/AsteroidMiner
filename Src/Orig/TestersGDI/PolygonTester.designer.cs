namespace Game.Orig.TestersGDI
{
	partial class PolygonTester
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.pictureBox1 = new Game.Orig.HelperClassesGDI.LargeMapViewer2D();
			this.chkRunning = new System.Windows.Forms.CheckBox();
			this.radCube = new System.Windows.Forms.RadioButton();
			this.radTetrahedron = new System.Windows.Forms.RadioButton();
			this.btnSpinStop = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.btnSpinZ = new System.Windows.Forms.Button();
			this.btnSpinY = new System.Windows.Forms.Button();
			this.btnSpinX = new System.Windows.Forms.Button();
			this.btnSpinRandom = new System.Windows.Forms.Button();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.lblNotes = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.DimGray;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox1.Location = new System.Drawing.Point(238, 12);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(542, 542);
			this.pictureBox1.TabIndex = 5;
			this.pictureBox1.TabStop = false;
			// 
			// chkRunning
			// 
			this.chkRunning.AutoSize = true;
			this.chkRunning.Checked = true;
			this.chkRunning.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkRunning.Location = new System.Drawing.Point(12, 12);
			this.chkRunning.Name = "chkRunning";
			this.chkRunning.Size = new System.Drawing.Size(66, 17);
			this.chkRunning.TabIndex = 6;
			this.chkRunning.Text = "Running";
			this.chkRunning.UseVisualStyleBackColor = true;
			this.chkRunning.CheckedChanged += new System.EventHandler(this.chkRunning_CheckedChanged);
			// 
			// radCube
			// 
			this.radCube.AutoSize = true;
			this.radCube.Checked = true;
			this.radCube.Location = new System.Drawing.Point(12, 75);
			this.radCube.Name = "radCube";
			this.radCube.Size = new System.Drawing.Size(50, 17);
			this.radCube.TabIndex = 7;
			this.radCube.TabStop = true;
			this.radCube.Text = "Cube";
			this.radCube.UseVisualStyleBackColor = true;
			this.radCube.CheckedChanged += new System.EventHandler(this.radPolygon_CheckedChanged);
			// 
			// radTetrahedron
			// 
			this.radTetrahedron.AutoSize = true;
			this.radTetrahedron.Location = new System.Drawing.Point(12, 98);
			this.radTetrahedron.Name = "radTetrahedron";
			this.radTetrahedron.Size = new System.Drawing.Size(83, 17);
			this.radTetrahedron.TabIndex = 8;
			this.radTetrahedron.Text = "Tetrahedron";
			this.radTetrahedron.UseVisualStyleBackColor = true;
			this.radTetrahedron.CheckedChanged += new System.EventHandler(this.radPolygon_CheckedChanged);
			// 
			// btnSpinStop
			// 
			this.btnSpinStop.Location = new System.Drawing.Point(8, 19);
			this.btnSpinStop.Name = "btnSpinStop";
			this.btnSpinStop.Size = new System.Drawing.Size(75, 23);
			this.btnSpinStop.TabIndex = 9;
			this.btnSpinStop.Text = "Stop";
			this.btnSpinStop.UseVisualStyleBackColor = true;
			this.btnSpinStop.Click += new System.EventHandler(this.btnSpinStop_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox1.Controls.Add(this.btnSpinZ);
			this.groupBox1.Controls.Add(this.btnSpinY);
			this.groupBox1.Controls.Add(this.btnSpinX);
			this.groupBox1.Controls.Add(this.btnSpinRandom);
			this.groupBox1.Controls.Add(this.btnSpinStop);
			this.groupBox1.Location = new System.Drawing.Point(12, 404);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(220, 150);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Spin";
			// 
			// btnSpinZ
			// 
			this.btnSpinZ.Location = new System.Drawing.Point(8, 115);
			this.btnSpinZ.Name = "btnSpinZ";
			this.btnSpinZ.Size = new System.Drawing.Size(75, 23);
			this.btnSpinZ.TabIndex = 13;
			this.btnSpinZ.Text = "Z";
			this.btnSpinZ.UseVisualStyleBackColor = true;
			this.btnSpinZ.Click += new System.EventHandler(this.btnSpinZ_Click);
			// 
			// btnSpinY
			// 
			this.btnSpinY.Location = new System.Drawing.Point(8, 86);
			this.btnSpinY.Name = "btnSpinY";
			this.btnSpinY.Size = new System.Drawing.Size(75, 23);
			this.btnSpinY.TabIndex = 12;
			this.btnSpinY.Text = "Y";
			this.btnSpinY.UseVisualStyleBackColor = true;
			this.btnSpinY.Click += new System.EventHandler(this.btnSpinY_Click);
			// 
			// btnSpinX
			// 
			this.btnSpinX.Location = new System.Drawing.Point(8, 57);
			this.btnSpinX.Name = "btnSpinX";
			this.btnSpinX.Size = new System.Drawing.Size(75, 23);
			this.btnSpinX.TabIndex = 11;
			this.btnSpinX.Text = "X";
			this.btnSpinX.UseVisualStyleBackColor = true;
			this.btnSpinX.Click += new System.EventHandler(this.btnSpinX_Click);
			// 
			// btnSpinRandom
			// 
			this.btnSpinRandom.Location = new System.Drawing.Point(89, 19);
			this.btnSpinRandom.Name = "btnSpinRandom";
			this.btnSpinRandom.Size = new System.Drawing.Size(75, 23);
			this.btnSpinRandom.TabIndex = 10;
			this.btnSpinRandom.Text = "Random";
			this.btnSpinRandom.UseVisualStyleBackColor = true;
			this.btnSpinRandom.Click += new System.EventHandler(this.btnSpinRandom_Click);
			// 
			// timer1
			// 
			this.timer1.Interval = 10;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// lblNotes
			// 
			this.lblNotes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblNotes.Location = new System.Drawing.Point(9, 265);
			this.lblNotes.Name = "lblNotes";
			this.lblNotes.Size = new System.Drawing.Size(223, 136);
			this.lblNotes.TabIndex = 11;
			// 
			// PolygonTester
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(792, 566);
			this.Controls.Add(this.lblNotes);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.radTetrahedron);
			this.Controls.Add(this.radCube);
			this.Controls.Add(this.chkRunning);
			this.Controls.Add(this.pictureBox1);
			this.Name = "PolygonTester";
			this.Text = "PolygonTester";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Game.Orig.HelperClassesGDI.LargeMapViewer2D pictureBox1;
		private System.Windows.Forms.CheckBox chkRunning;
		private System.Windows.Forms.RadioButton radCube;
		private System.Windows.Forms.RadioButton radTetrahedron;
		private System.Windows.Forms.Button btnSpinStop;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button btnSpinRandom;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Button btnSpinZ;
		private System.Windows.Forms.Button btnSpinY;
		private System.Windows.Forms.Button btnSpinX;
		private System.Windows.Forms.Label lblNotes;
	}
}