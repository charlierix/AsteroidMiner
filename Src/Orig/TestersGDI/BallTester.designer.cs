namespace Game.Orig.TestersGDI
{
	partial class BallTester
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
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnClear = new System.Windows.Forms.Button();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.chkRunningGravBalls = new System.Windows.Forms.CheckBox();
			this.chkRandVelocity = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.chkDrawTail = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			this.chkDrawAccel = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.btnResetDirFacing = new System.Windows.Forms.Button();
			this.pctLeft = new System.Windows.Forms.PictureBox();
			this.chkApplyGravityShip = new System.Windows.Forms.CheckBox();
			this.pctRight = new System.Windows.Forms.PictureBox();
			this.chkRunningShip = new System.Windows.Forms.CheckBox();
			this.pctDown = new System.Windows.Forms.PictureBox();
			this.pctUp = new System.Windows.Forms.PictureBox();
			this.timer2 = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pctLeft)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctRight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pctUp)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.Black;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox1.Location = new System.Drawing.Point(238, 14);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(579, 579);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.MouseLeave += new System.EventHandler(this.pictureBox1_MouseLeave);
			this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 369);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Location = new System.Drawing.Point(6, 19);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 2;
			this.btnAdd.Text = "Add Ball";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// btnClear
			// 
			this.btnClear.Location = new System.Drawing.Point(6, 48);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(75, 23);
			this.btnClear.TabIndex = 3;
			this.btnClear.Text = "Clear Balls";
			this.btnClear.UseVisualStyleBackColor = true;
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// timer1
			// 
			this.timer1.Interval = 10;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// chkRunningGravBalls
			// 
			this.chkRunningGravBalls.AutoSize = true;
			this.chkRunningGravBalls.Location = new System.Drawing.Point(6, 77);
			this.chkRunningGravBalls.Name = "chkRunningGravBalls";
			this.chkRunningGravBalls.Size = new System.Drawing.Size(66, 17);
			this.chkRunningGravBalls.TabIndex = 4;
			this.chkRunningGravBalls.Text = "Running";
			this.chkRunningGravBalls.UseVisualStyleBackColor = true;
			this.chkRunningGravBalls.CheckedChanged += new System.EventHandler(this.chkRunning_CheckedChanged);
			// 
			// chkRandVelocity
			// 
			this.chkRandVelocity.AutoSize = true;
			this.chkRandVelocity.Location = new System.Drawing.Point(87, 25);
			this.chkRandVelocity.Name = "chkRandVelocity";
			this.chkRandVelocity.Size = new System.Drawing.Size(92, 17);
			this.chkRandVelocity.TabIndex = 9;
			this.chkRandVelocity.Text = "Rand Velocity";
			this.chkRandVelocity.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.chkDrawTail);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.trackBar1);
			this.groupBox1.Controls.Add(this.chkDrawAccel);
			this.groupBox1.Controls.Add(this.btnAdd);
			this.groupBox1.Controls.Add(this.chkRandVelocity);
			this.groupBox1.Controls.Add(this.btnClear);
			this.groupBox1.Controls.Add(this.chkRunningGravBalls);
			this.groupBox1.Location = new System.Drawing.Point(12, 14);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(182, 162);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Gravity Balls (attracted to mouse)";
			// 
			// chkDrawTail
			// 
			this.chkDrawTail.AutoSize = true;
			this.chkDrawTail.Location = new System.Drawing.Point(87, 71);
			this.chkDrawTail.Name = "chkDrawTail";
			this.chkDrawTail.Size = new System.Drawing.Size(71, 17);
			this.chkDrawTail.TabIndex = 13;
			this.chkDrawTail.Text = "Draw Tail";
			this.chkDrawTail.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 111);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(83, 13);
			this.label1.TabIndex = 12;
			this.label1.Text = "Gravity Strength";
			// 
			// trackBar1
			// 
			this.trackBar1.Location = new System.Drawing.Point(6, 127);
			this.trackBar1.Maximum = 15000;
			this.trackBar1.Minimum = 250;
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Size = new System.Drawing.Size(170, 29);
			this.trackBar1.TabIndex = 11;
			this.trackBar1.TickFrequency = 3000;
			this.trackBar1.Value = 2500;
			this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
			// 
			// chkDrawAccel
			// 
			this.chkDrawAccel.AutoSize = true;
			this.chkDrawAccel.Checked = true;
			this.chkDrawAccel.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkDrawAccel.Location = new System.Drawing.Point(87, 48);
			this.chkDrawAccel.Name = "chkDrawAccel";
			this.chkDrawAccel.Size = new System.Drawing.Size(81, 17);
			this.chkDrawAccel.TabIndex = 10;
			this.chkDrawAccel.Text = "Draw Accel";
			this.chkDrawAccel.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.btnResetDirFacing);
			this.groupBox2.Controls.Add(this.pctLeft);
			this.groupBox2.Controls.Add(this.chkApplyGravityShip);
			this.groupBox2.Controls.Add(this.pctRight);
			this.groupBox2.Controls.Add(this.chkRunningShip);
			this.groupBox2.Controls.Add(this.pctDown);
			this.groupBox2.Controls.Add(this.pctUp);
			this.groupBox2.Location = new System.Drawing.Point(12, 213);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(179, 128);
			this.groupBox2.TabIndex = 11;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Spaceship (use arrow keys)";
			// 
			// btnResetDirFacing
			// 
			this.btnResetDirFacing.Location = new System.Drawing.Point(118, 19);
			this.btnResetDirFacing.Name = "btnResetDirFacing";
			this.btnResetDirFacing.Size = new System.Drawing.Size(55, 35);
			this.btnResetDirFacing.TabIndex = 12;
			this.btnResetDirFacing.Text = "reset dirfacing";
			this.btnResetDirFacing.UseVisualStyleBackColor = true;
			this.btnResetDirFacing.Click += new System.EventHandler(this.btnResetDirFacing_Click);
			// 
			// pctLeft
			// 
			this.pctLeft.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctLeft.Location = new System.Drawing.Point(53, 97);
			this.pctLeft.Name = "pctLeft";
			this.pctLeft.Size = new System.Drawing.Size(20, 20);
			this.pctLeft.TabIndex = 15;
			this.pctLeft.TabStop = false;
			// 
			// chkApplyGravityShip
			// 
			this.chkApplyGravityShip.AutoSize = true;
			this.chkApplyGravityShip.Location = new System.Drawing.Point(12, 42);
			this.chkApplyGravityShip.Name = "chkApplyGravityShip";
			this.chkApplyGravityShip.Size = new System.Drawing.Size(90, 17);
			this.chkApplyGravityShip.TabIndex = 1;
			this.chkApplyGravityShip.Text = "Gravity Down";
			this.chkApplyGravityShip.UseVisualStyleBackColor = true;
			// 
			// pctRight
			// 
			this.pctRight.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctRight.Location = new System.Drawing.Point(105, 97);
			this.pctRight.Name = "pctRight";
			this.pctRight.Size = new System.Drawing.Size(20, 20);
			this.pctRight.TabIndex = 14;
			this.pctRight.TabStop = false;
			// 
			// chkRunningShip
			// 
			this.chkRunningShip.AutoSize = true;
			this.chkRunningShip.Location = new System.Drawing.Point(12, 19);
			this.chkRunningShip.Name = "chkRunningShip";
			this.chkRunningShip.Size = new System.Drawing.Size(66, 17);
			this.chkRunningShip.TabIndex = 0;
			this.chkRunningShip.Text = "Running";
			this.chkRunningShip.UseVisualStyleBackColor = true;
			this.chkRunningShip.CheckedChanged += new System.EventHandler(this.chkRunningShip_CheckedChanged);
			// 
			// pctDown
			// 
			this.pctDown.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctDown.Location = new System.Drawing.Point(79, 97);
			this.pctDown.Name = "pctDown";
			this.pctDown.Size = new System.Drawing.Size(20, 20);
			this.pctDown.TabIndex = 13;
			this.pctDown.TabStop = false;
			// 
			// pctUp
			// 
			this.pctUp.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pctUp.Location = new System.Drawing.Point(79, 71);
			this.pctUp.Name = "pctUp";
			this.pctUp.Size = new System.Drawing.Size(20, 20);
			this.pctUp.TabIndex = 12;
			this.pctUp.TabStop = false;
			// 
			// timer2
			// 
			this.timer2.Interval = 10;
			this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
			// 
			// BallTester
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(829, 605);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.pictureBox1);
			this.KeyPreview = true;
			this.Name = "BallTester";
			this.Text = "BallTester";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pctLeft)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctRight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pctUp)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.CheckBox chkRunningGravBalls;
		private System.Windows.Forms.CheckBox chkRandVelocity;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox chkApplyGravityShip;
		private System.Windows.Forms.CheckBox chkRunningShip;
		private System.Windows.Forms.Timer timer2;
		private System.Windows.Forms.PictureBox pctUp;
		private System.Windows.Forms.PictureBox pctLeft;
		private System.Windows.Forms.PictureBox pctRight;
		private System.Windows.Forms.PictureBox pctDown;
		private System.Windows.Forms.Button btnResetDirFacing;
		private System.Windows.Forms.CheckBox chkDrawAccel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TrackBar trackBar1;
		private System.Windows.Forms.CheckBox chkDrawTail;
	}
}