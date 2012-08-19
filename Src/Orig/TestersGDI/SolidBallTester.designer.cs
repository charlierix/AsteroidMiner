namespace Game.Orig.TestersGDI
{
	partial class SolidBallTester
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
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.button1 = new System.Windows.Forms.Button();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.chkPoolRunning = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.pctS = new System.Windows.Forms.PictureBox();
			this.pctW = new System.Windows.Forms.PictureBox();
			this.pctLeft = new System.Windows.Forms.PictureBox();
			this.pctRight = new System.Windows.Forms.PictureBox();
			this.pctDown = new System.Windows.Forms.PictureBox();
			this.pctUp = new System.Windows.Forms.PictureBox();
			this.btnResetShip = new System.Windows.Forms.Button();
			this.chkShipGravity = new System.Windows.Forms.CheckBox();
			this.radIndependantThrusters = new System.Windows.Forms.RadioButton();
			this.radPairedThrusters = new System.Windows.Forms.RadioButton();
			this.chkShipRunning = new System.Windows.Forms.CheckBox();
			this.timer2 = new System.Windows.Forms.Timer(this.components);
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pctS)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctW)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctLeft)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctRight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctUp)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.Black;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox1.Location = new System.Drawing.Point(277, 11);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(579, 579);
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
			this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
			this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(6, 19);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(134, 23);
			this.button1.TabIndex = 2;
			this.button1.Text = "Reset Pool Ball";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// timer1
			// 
			this.timer1.Interval = 20;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.chkPoolRunning);
			this.groupBox1.Controls.Add(this.button1);
			this.groupBox1.Location = new System.Drawing.Point(46, 30);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(157, 74);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Pool Ball";
			// 
			// chkPoolRunning
			// 
			this.chkPoolRunning.AutoSize = true;
			this.chkPoolRunning.Enabled = false;
			this.chkPoolRunning.Location = new System.Drawing.Point(6, 48);
			this.chkPoolRunning.Name = "chkPoolRunning";
			this.chkPoolRunning.Size = new System.Drawing.Size(66, 17);
			this.chkPoolRunning.TabIndex = 3;
			this.chkPoolRunning.Text = "Running";
			this.chkPoolRunning.UseVisualStyleBackColor = true;
			this.chkPoolRunning.CheckedChanged += new System.EventHandler(this.chkPoolRunning_CheckedChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.trackBar1);
			this.groupBox2.Controls.Add(this.pctS);
			this.groupBox2.Controls.Add(this.pctW);
			this.groupBox2.Controls.Add(this.pctLeft);
			this.groupBox2.Controls.Add(this.pctRight);
			this.groupBox2.Controls.Add(this.pctDown);
			this.groupBox2.Controls.Add(this.pctUp);
			this.groupBox2.Controls.Add(this.btnResetShip);
			this.groupBox2.Controls.Add(this.chkShipGravity);
			this.groupBox2.Controls.Add(this.radIndependantThrusters);
			this.groupBox2.Controls.Add(this.radPairedThrusters);
			this.groupBox2.Controls.Add(this.chkShipRunning);
			this.groupBox2.Location = new System.Drawing.Point(25, 143);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(224, 307);
			this.groupBox2.TabIndex = 4;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Ship";
			// 
			// pctS
			// 
			this.pctS.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctS.Location = new System.Drawing.Point(45, 268);
			this.pctS.Name = "pctS";
			this.pctS.Size = new System.Drawing.Size(20, 20);
			this.pctS.TabIndex = 21;
			this.pctS.TabStop = false;
			// 
			// pctW
			// 
			this.pctW.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctW.Location = new System.Drawing.Point(45, 242);
			this.pctW.Name = "pctW";
			this.pctW.Size = new System.Drawing.Size(20, 20);
			this.pctW.TabIndex = 20;
			this.pctW.TabStop = false;
			// 
			// pctLeft
			// 
			this.pctLeft.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctLeft.Location = new System.Drawing.Point(108, 268);
			this.pctLeft.Name = "pctLeft";
			this.pctLeft.Size = new System.Drawing.Size(20, 20);
			this.pctLeft.TabIndex = 19;
			this.pctLeft.TabStop = false;
			// 
			// pctRight
			// 
			this.pctRight.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctRight.Location = new System.Drawing.Point(160, 268);
			this.pctRight.Name = "pctRight";
			this.pctRight.Size = new System.Drawing.Size(20, 20);
			this.pctRight.TabIndex = 18;
			this.pctRight.TabStop = false;
			// 
			// pctDown
			// 
			this.pctDown.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctDown.Location = new System.Drawing.Point(134, 268);
			this.pctDown.Name = "pctDown";
			this.pctDown.Size = new System.Drawing.Size(20, 20);
			this.pctDown.TabIndex = 17;
			this.pctDown.TabStop = false;
			// 
			// pctUp
			// 
			this.pctUp.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctUp.Location = new System.Drawing.Point(134, 242);
			this.pctUp.Name = "pctUp";
			this.pctUp.Size = new System.Drawing.Size(20, 20);
			this.pctUp.TabIndex = 16;
			this.pctUp.TabStop = false;
			// 
			// btnResetShip
			// 
			this.btnResetShip.Location = new System.Drawing.Point(6, 19);
			this.btnResetShip.Name = "btnResetShip";
			this.btnResetShip.Size = new System.Drawing.Size(134, 23);
			this.btnResetShip.TabIndex = 8;
			this.btnResetShip.Text = "Reset Ship";
			this.btnResetShip.UseVisualStyleBackColor = true;
			this.btnResetShip.Click += new System.EventHandler(this.btnResetShip_Click);
			// 
			// chkShipGravity
			// 
			this.chkShipGravity.AutoSize = true;
			this.chkShipGravity.Location = new System.Drawing.Point(6, 145);
			this.chkShipGravity.Name = "chkShipGravity";
			this.chkShipGravity.Size = new System.Drawing.Size(59, 17);
			this.chkShipGravity.TabIndex = 7;
			this.chkShipGravity.Text = "Gravity";
			this.chkShipGravity.UseVisualStyleBackColor = true;
			// 
			// radIndependantThrusters
			// 
			this.radIndependantThrusters.AutoSize = true;
			this.radIndependantThrusters.Location = new System.Drawing.Point(6, 114);
			this.radIndependantThrusters.Name = "radIndependantThrusters";
			this.radIndependantThrusters.Size = new System.Drawing.Size(204, 17);
			this.radIndependantThrusters.TabIndex = 6;
			this.radIndependantThrusters.Text = "Independent Thrusters (w s, up down)";
			this.radIndependantThrusters.UseVisualStyleBackColor = true;
			// 
			// radPairedThrusters
			// 
			this.radPairedThrusters.AutoSize = true;
			this.radPairedThrusters.Checked = true;
			this.radPairedThrusters.Location = new System.Drawing.Point(6, 91);
			this.radPairedThrusters.Name = "radPairedThrusters";
			this.radPairedThrusters.Size = new System.Drawing.Size(196, 17);
			this.radPairedThrusters.TabIndex = 5;
			this.radPairedThrusters.TabStop = true;
			this.radPairedThrusters.Text = "Paired Thrusters (all four arrow keys)";
			this.radPairedThrusters.UseVisualStyleBackColor = true;
			// 
			// chkShipRunning
			// 
			this.chkShipRunning.AutoSize = true;
			this.chkShipRunning.Enabled = false;
			this.chkShipRunning.Location = new System.Drawing.Point(6, 59);
			this.chkShipRunning.Name = "chkShipRunning";
			this.chkShipRunning.Size = new System.Drawing.Size(66, 17);
			this.chkShipRunning.TabIndex = 4;
			this.chkShipRunning.Text = "Running";
			this.chkShipRunning.UseVisualStyleBackColor = true;
			this.chkShipRunning.CheckedChanged += new System.EventHandler(this.chkShipRunning_CheckedChanged);
			// 
			// timer2
			// 
			this.timer2.Interval = 20;
			this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
			// 
			// trackBar1
			// 
			this.trackBar1.Location = new System.Drawing.Point(6, 196);
			this.trackBar1.Maximum = 90;
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Size = new System.Drawing.Size(212, 29);
			this.trackBar1.TabIndex = 22;
			this.trackBar1.TickFrequency = 45;
			this.trackBar1.Value = 45;
			this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(66, 180);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(92, 13);
			this.label1.TabIndex = 23;
			this.label1.Text = "Thruster Offset";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 180);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(13, 13);
			this.label2.TabIndex = 24;
			this.label2.Text = "0";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(199, 180);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(19, 13);
			this.label3.TabIndex = 25;
			this.label3.Text = "90";
			// 
			// SolidSphereTester
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(868, 602);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.pictureBox1);
			this.KeyPreview = true;
			this.Name = "SolidSphereTester";
			this.Text = "SolidSphereTester";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pctS)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctW)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctLeft)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctRight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctUp)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox chkPoolRunning;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox chkShipRunning;
		private System.Windows.Forms.CheckBox chkShipGravity;
		private System.Windows.Forms.RadioButton radIndependantThrusters;
		private System.Windows.Forms.RadioButton radPairedThrusters;
		private System.Windows.Forms.Timer timer2;
		private System.Windows.Forms.Button btnResetShip;
		private System.Windows.Forms.PictureBox pctS;
		private System.Windows.Forms.PictureBox pctW;
		private System.Windows.Forms.PictureBox pctLeft;
		private System.Windows.Forms.PictureBox pctRight;
		private System.Windows.Forms.PictureBox pctDown;
		private System.Windows.Forms.PictureBox pctUp;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TrackBar trackBar1;
	}
}