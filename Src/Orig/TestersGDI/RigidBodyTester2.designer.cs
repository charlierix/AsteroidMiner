namespace Game.Orig.TestersGDI
{
	partial class RigidBodyTester2
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
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.btnResetPartial = new System.Windows.Forms.Button();
			this.txtAngularMomentum = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.trkAngularMomentum = new System.Windows.Forms.TrackBar();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this.chkCoupledBothMass = new System.Windows.Forms.CheckBox();
			this.chkCoupledYMass = new System.Windows.Forms.CheckBox();
			this.chkCoupledXMass = new System.Windows.Forms.CheckBox();
			this.trkWestMass = new System.Windows.Forms.TrackBar();
			this.trkEastMass = new System.Windows.Forms.TrackBar();
			this.trkNorthMass = new System.Windows.Forms.TrackBar();
			this.trkSouthMass = new System.Windows.Forms.TrackBar();
			this.label3 = new System.Windows.Forms.Label();
			this.chkCoupledBothPos = new System.Windows.Forms.CheckBox();
			this.chkCoupledYPos = new System.Windows.Forms.CheckBox();
			this.chkCoupledXPos = new System.Windows.Forms.CheckBox();
			this.trkNorthPos = new System.Windows.Forms.TrackBar();
			this.trkSouthPos = new System.Windows.Forms.TrackBar();
			this.trkEastPos = new System.Windows.Forms.TrackBar();
			this.trkWestPos = new System.Windows.Forms.TrackBar();
			this.txtAngularVelocity = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnResetTotal = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkAngularMomentum)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trkWestMass)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkEastMass)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkNorthMass)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkSouthMass)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkNorthPos)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkSouthPos)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkEastPos)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkWestPos)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.SlateGray;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox1.Location = new System.Drawing.Point(297, 12);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(481, 481);
			this.pictureBox1.TabIndex = 4;
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
			this.chkRunning.TabIndex = 5;
			this.chkRunning.Text = "Running";
			this.chkRunning.UseVisualStyleBackColor = true;
			this.chkRunning.CheckedChanged += new System.EventHandler(this.chkRunning_CheckedChanged);
			// 
			// timer1
			// 
			this.timer1.Interval = 10;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// btnResetPartial
			// 
			this.btnResetPartial.Location = new System.Drawing.Point(12, 64);
			this.btnResetPartial.Name = "btnResetPartial";
			this.btnResetPartial.Size = new System.Drawing.Size(98, 23);
			this.btnResetPartial.TabIndex = 7;
			this.btnResetPartial.Text = "Reset Partial";
			this.btnResetPartial.UseVisualStyleBackColor = true;
			this.btnResetPartial.Click += new System.EventHandler(this.btnResetPartial_Click);
			// 
			// txtAngularMomentum
			// 
			this.txtAngularMomentum.BackColor = System.Drawing.SystemColors.ControlLight;
			this.txtAngularMomentum.Location = new System.Drawing.Point(213, 95);
			this.txtAngularMomentum.Name = "txtAngularMomentum";
			this.txtAngularMomentum.ReadOnly = true;
			this.txtAngularMomentum.Size = new System.Drawing.Size(78, 20);
			this.txtAngularMomentum.TabIndex = 13;
			this.txtAngularMomentum.TabStop = false;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 102);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(98, 13);
			this.label1.TabIndex = 12;
			this.label1.Text = "Angular Momentum";
			// 
			// trkAngularMomentum
			// 
			this.trkAngularMomentum.Location = new System.Drawing.Point(12, 118);
			this.trkAngularMomentum.Maximum = 1000;
			this.trkAngularMomentum.Name = "trkAngularMomentum";
			this.trkAngularMomentum.Size = new System.Drawing.Size(279, 29);
			this.trkAngularMomentum.TabIndex = 11;
			this.trkAngularMomentum.TickFrequency = 100;
			this.trkAngularMomentum.Value = 500;
			this.trkAngularMomentum.Scroll += new System.EventHandler(this.trkAngularMomentum_Scroll);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.chkCoupledBothMass);
			this.groupBox1.Controls.Add(this.chkCoupledYMass);
			this.groupBox1.Controls.Add(this.chkCoupledXMass);
			this.groupBox1.Controls.Add(this.trkWestMass);
			this.groupBox1.Controls.Add(this.trkEastMass);
			this.groupBox1.Controls.Add(this.trkNorthMass);
			this.groupBox1.Controls.Add(this.trkSouthMass);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.chkCoupledBothPos);
			this.groupBox1.Controls.Add(this.chkCoupledYPos);
			this.groupBox1.Controls.Add(this.chkCoupledXPos);
			this.groupBox1.Controls.Add(this.trkNorthPos);
			this.groupBox1.Controls.Add(this.trkSouthPos);
			this.groupBox1.Controls.Add(this.trkEastPos);
			this.groupBox1.Controls.Add(this.trkWestPos);
			this.groupBox1.Location = new System.Drawing.Point(12, 163);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(279, 332);
			this.groupBox1.TabIndex = 18;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Point Mass";
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.Black;
			this.label4.Location = new System.Drawing.Point(179, 16);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(93, 17);
			this.label4.TabIndex = 33;
			this.label4.Text = "Masses";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// chkCoupledBothMass
			// 
			this.chkCoupledBothMass.AutoSize = true;
			this.chkCoupledBothMass.Location = new System.Drawing.Point(182, 36);
			this.chkCoupledBothMass.Name = "chkCoupledBothMass";
			this.chkCoupledBothMass.Size = new System.Drawing.Size(90, 17);
			this.chkCoupledBothMass.TabIndex = 32;
			this.chkCoupledBothMass.Text = "Coupled Both";
			this.chkCoupledBothMass.UseVisualStyleBackColor = true;
			this.chkCoupledBothMass.CheckedChanged += new System.EventHandler(this.chkCoupledBothMass_CheckedChanged);
			// 
			// chkCoupledYMass
			// 
			this.chkCoupledYMass.AutoSize = true;
			this.chkCoupledYMass.Location = new System.Drawing.Point(182, 82);
			this.chkCoupledYMass.Name = "chkCoupledYMass";
			this.chkCoupledYMass.Size = new System.Drawing.Size(75, 17);
			this.chkCoupledYMass.TabIndex = 31;
			this.chkCoupledYMass.Text = "Coupled Y";
			this.chkCoupledYMass.UseVisualStyleBackColor = true;
			this.chkCoupledYMass.CheckedChanged += new System.EventHandler(this.chkCoupledYMass_CheckedChanged);
			// 
			// chkCoupledXMass
			// 
			this.chkCoupledXMass.AutoSize = true;
			this.chkCoupledXMass.Location = new System.Drawing.Point(182, 59);
			this.chkCoupledXMass.Name = "chkCoupledXMass";
			this.chkCoupledXMass.Size = new System.Drawing.Size(75, 17);
			this.chkCoupledXMass.TabIndex = 30;
			this.chkCoupledXMass.Text = "Coupled X";
			this.chkCoupledXMass.UseVisualStyleBackColor = true;
			this.chkCoupledXMass.CheckedChanged += new System.EventHandler(this.chkCoupledXMass_CheckedChanged);
			// 
			// trkWestMass
			// 
			this.trkWestMass.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
			this.trkWestMass.Location = new System.Drawing.Point(6, 180);
			this.trkWestMass.Maximum = 1000;
			this.trkWestMass.Name = "trkWestMass";
			this.trkWestMass.Size = new System.Drawing.Size(106, 29);
			this.trkWestMass.TabIndex = 29;
			this.trkWestMass.TickFrequency = 250;
			this.trkWestMass.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkWestMass.Value = 500;
			this.trkWestMass.Scroll += new System.EventHandler(this.trkWestMass_Scroll);
			// 
			// trkEastMass
			// 
			this.trkEastMass.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
			this.trkEastMass.Location = new System.Drawing.Point(167, 180);
			this.trkEastMass.Maximum = 1000;
			this.trkEastMass.Name = "trkEastMass";
			this.trkEastMass.Size = new System.Drawing.Size(106, 29);
			this.trkEastMass.TabIndex = 28;
			this.trkEastMass.TickFrequency = 250;
			this.trkEastMass.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkEastMass.Value = 500;
			this.trkEastMass.Scroll += new System.EventHandler(this.trkEastMass_Scroll);
			// 
			// trkNorthMass
			// 
			this.trkNorthMass.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
			this.trkNorthMass.Location = new System.Drawing.Point(141, 211);
			this.trkNorthMass.Maximum = 1000;
			this.trkNorthMass.Name = "trkNorthMass";
			this.trkNorthMass.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkNorthMass.Size = new System.Drawing.Size(29, 106);
			this.trkNorthMass.TabIndex = 27;
			this.trkNorthMass.TickFrequency = 250;
			this.trkNorthMass.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkNorthMass.Value = 500;
			this.trkNorthMass.Scroll += new System.EventHandler(this.trkNorthMass_Scroll);
			// 
			// trkSouthMass
			// 
			this.trkSouthMass.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
			this.trkSouthMass.Location = new System.Drawing.Point(141, 38);
			this.trkSouthMass.Maximum = 1000;
			this.trkSouthMass.Name = "trkSouthMass";
			this.trkSouthMass.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkSouthMass.Size = new System.Drawing.Size(29, 106);
			this.trkSouthMass.TabIndex = 26;
			this.trkSouthMass.TickFrequency = 250;
			this.trkSouthMass.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkSouthMass.Value = 500;
			this.trkSouthMass.Scroll += new System.EventHandler(this.trkSouthMass_Scroll);
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.SystemColors.Control;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.ForeColor = System.Drawing.Color.Black;
			this.label3.Location = new System.Drawing.Point(6, 16);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(93, 17);
			this.label3.TabIndex = 25;
			this.label3.Text = "Positions";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// chkCoupledBothPos
			// 
			this.chkCoupledBothPos.AutoSize = true;
			this.chkCoupledBothPos.Location = new System.Drawing.Point(9, 36);
			this.chkCoupledBothPos.Name = "chkCoupledBothPos";
			this.chkCoupledBothPos.Size = new System.Drawing.Size(90, 17);
			this.chkCoupledBothPos.TabIndex = 24;
			this.chkCoupledBothPos.Text = "Coupled Both";
			this.chkCoupledBothPos.UseVisualStyleBackColor = true;
			this.chkCoupledBothPos.CheckedChanged += new System.EventHandler(this.chkCoupledBothPos_CheckedChanged);
			// 
			// chkCoupledYPos
			// 
			this.chkCoupledYPos.AutoSize = true;
			this.chkCoupledYPos.Location = new System.Drawing.Point(9, 82);
			this.chkCoupledYPos.Name = "chkCoupledYPos";
			this.chkCoupledYPos.Size = new System.Drawing.Size(75, 17);
			this.chkCoupledYPos.TabIndex = 23;
			this.chkCoupledYPos.Text = "Coupled Y";
			this.chkCoupledYPos.UseVisualStyleBackColor = true;
			this.chkCoupledYPos.CheckedChanged += new System.EventHandler(this.chkCoupledYPos_CheckedChanged);
			// 
			// chkCoupledXPos
			// 
			this.chkCoupledXPos.AutoSize = true;
			this.chkCoupledXPos.Location = new System.Drawing.Point(9, 59);
			this.chkCoupledXPos.Name = "chkCoupledXPos";
			this.chkCoupledXPos.Size = new System.Drawing.Size(75, 17);
			this.chkCoupledXPos.TabIndex = 22;
			this.chkCoupledXPos.Text = "Coupled X";
			this.chkCoupledXPos.UseVisualStyleBackColor = true;
			this.chkCoupledXPos.CheckedChanged += new System.EventHandler(this.chkCoupledXPos_CheckedChanged);
			// 
			// trkNorthPos
			// 
			this.trkNorthPos.BackColor = System.Drawing.SystemColors.Control;
			this.trkNorthPos.Location = new System.Drawing.Point(106, 211);
			this.trkNorthPos.Maximum = 1000;
			this.trkNorthPos.Name = "trkNorthPos";
			this.trkNorthPos.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkNorthPos.Size = new System.Drawing.Size(29, 106);
			this.trkNorthPos.TabIndex = 21;
			this.trkNorthPos.TickFrequency = 250;
			this.trkNorthPos.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkNorthPos.Value = 500;
			this.trkNorthPos.Scroll += new System.EventHandler(this.trkNorthPos_Scroll);
			// 
			// trkSouthPos
			// 
			this.trkSouthPos.BackColor = System.Drawing.SystemColors.Control;
			this.trkSouthPos.Location = new System.Drawing.Point(106, 38);
			this.trkSouthPos.Maximum = 1000;
			this.trkSouthPos.Name = "trkSouthPos";
			this.trkSouthPos.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkSouthPos.Size = new System.Drawing.Size(29, 106);
			this.trkSouthPos.TabIndex = 20;
			this.trkSouthPos.TickFrequency = 250;
			this.trkSouthPos.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkSouthPos.Value = 500;
			this.trkSouthPos.Scroll += new System.EventHandler(this.trkSouthPos_Scroll);
			// 
			// trkEastPos
			// 
			this.trkEastPos.BackColor = System.Drawing.SystemColors.Control;
			this.trkEastPos.Location = new System.Drawing.Point(167, 145);
			this.trkEastPos.Maximum = 1000;
			this.trkEastPos.Name = "trkEastPos";
			this.trkEastPos.Size = new System.Drawing.Size(106, 29);
			this.trkEastPos.TabIndex = 19;
			this.trkEastPos.TickFrequency = 250;
			this.trkEastPos.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkEastPos.Value = 500;
			this.trkEastPos.Scroll += new System.EventHandler(this.trkEastPos_Scroll);
			// 
			// trkWestPos
			// 
			this.trkWestPos.BackColor = System.Drawing.SystemColors.Control;
			this.trkWestPos.Location = new System.Drawing.Point(6, 145);
			this.trkWestPos.Maximum = 1000;
			this.trkWestPos.Name = "trkWestPos";
			this.trkWestPos.Size = new System.Drawing.Size(106, 29);
			this.trkWestPos.TabIndex = 18;
			this.trkWestPos.TickFrequency = 250;
			this.trkWestPos.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.trkWestPos.Value = 500;
			this.trkWestPos.Scroll += new System.EventHandler(this.trkWestPos_Scroll);
			// 
			// txtAngularVelocity
			// 
			this.txtAngularVelocity.BackColor = System.Drawing.SystemColors.ControlLight;
			this.txtAngularVelocity.Location = new System.Drawing.Point(213, 48);
			this.txtAngularVelocity.Name = "txtAngularVelocity";
			this.txtAngularVelocity.ReadOnly = true;
			this.txtAngularVelocity.Size = new System.Drawing.Size(78, 20);
			this.txtAngularVelocity.TabIndex = 19;
			this.txtAngularVelocity.TabStop = false;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(210, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(83, 13);
			this.label2.TabIndex = 20;
			this.label2.Text = "Angular Velocity";
			// 
			// btnResetTotal
			// 
			this.btnResetTotal.Location = new System.Drawing.Point(12, 35);
			this.btnResetTotal.Name = "btnResetTotal";
			this.btnResetTotal.Size = new System.Drawing.Size(98, 23);
			this.btnResetTotal.TabIndex = 21;
			this.btnResetTotal.Text = "Reset Total";
			this.btnResetTotal.UseVisualStyleBackColor = true;
			this.btnResetTotal.Click += new System.EventHandler(this.btnResetTotal_Click);
			// 
			// RigidBodyTester2
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(790, 504);
			this.Controls.Add(this.btnResetTotal);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.txtAngularVelocity);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.txtAngularMomentum);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.trkAngularMomentum);
			this.Controls.Add(this.btnResetPartial);
			this.Controls.Add(this.chkRunning);
			this.Controls.Add(this.pictureBox1);
			this.Name = "RigidBodyTester2";
			this.Text = "RigidBodyTester2";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkAngularMomentum)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trkWestMass)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkEastMass)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkNorthMass)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkSouthMass)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkNorthPos)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkSouthPos)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkEastPos)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkWestPos)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Game.Orig.HelperClassesGDI.LargeMapViewer2D pictureBox1;
		private System.Windows.Forms.CheckBox chkRunning;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Button btnResetPartial;
		private System.Windows.Forms.TextBox txtAngularMomentum;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TrackBar trkAngularMomentum;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TrackBar trkNorthPos;
		private System.Windows.Forms.TrackBar trkSouthPos;
		private System.Windows.Forms.TrackBar trkEastPos;
		private System.Windows.Forms.TrackBar trkWestPos;
		private System.Windows.Forms.TextBox txtAngularVelocity;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox chkCoupledYPos;
		private System.Windows.Forms.CheckBox chkCoupledXPos;
		private System.Windows.Forms.Button btnResetTotal;
		private System.Windows.Forms.CheckBox chkCoupledBothPos;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.CheckBox chkCoupledBothMass;
		private System.Windows.Forms.CheckBox chkCoupledYMass;
		private System.Windows.Forms.CheckBox chkCoupledXMass;
		private System.Windows.Forms.TrackBar trkWestMass;
		private System.Windows.Forms.TrackBar trkEastMass;
		private System.Windows.Forms.TrackBar trkNorthMass;
		private System.Windows.Forms.TrackBar trkSouthMass;
	}
}