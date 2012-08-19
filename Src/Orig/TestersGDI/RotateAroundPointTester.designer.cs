namespace Game.Orig.TestersGDI
{
	partial class RotateAroundPointTester
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
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			this.btnReset = new System.Windows.Forms.Button();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.radCenter = new System.Windows.Forms.RadioButton();
			this.radOffset = new System.Windows.Forms.RadioButton();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.SlateGray;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox1.Location = new System.Drawing.Point(235, 12);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(500, 500);
			this.pictureBox1.TabIndex = 3;
			this.pictureBox1.TabStop = false;
			// 
			// trackBar1
			// 
			this.trackBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBar1.Location = new System.Drawing.Point(200, 12);
			this.trackBar1.Maximum = 360;
			this.trackBar1.Minimum = -360;
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBar1.Size = new System.Drawing.Size(29, 500);
			this.trackBar1.TabIndex = 4;
			this.trackBar1.TickFrequency = 45;
			this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
			// 
			// btnReset
			// 
			this.btnReset.Location = new System.Drawing.Point(12, 12);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(75, 23);
			this.btnReset.TabIndex = 5;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// timer1
			// 
			this.timer1.Interval = 10;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// radCenter
			// 
			this.radCenter.AutoSize = true;
			this.radCenter.Checked = true;
			this.radCenter.Location = new System.Drawing.Point(33, 185);
			this.radCenter.Name = "radCenter";
			this.radCenter.Size = new System.Drawing.Size(128, 17);
			this.radCenter.TabIndex = 6;
			this.radCenter.TabStop = true;
			this.radCenter.Text = "Rotate Around Center";
			this.radCenter.UseVisualStyleBackColor = true;
			// 
			// radOffset
			// 
			this.radOffset.AutoSize = true;
			this.radOffset.Location = new System.Drawing.Point(33, 208);
			this.radOffset.Name = "radOffset";
			this.radOffset.Size = new System.Drawing.Size(125, 17);
			this.radOffset.TabIndex = 7;
			this.radOffset.Text = "Rotate Around Offset";
			this.radOffset.UseVisualStyleBackColor = true;
			// 
			// pictureBox2
			// 
			this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox2.BackColor = System.Drawing.Color.SlateGray;
			this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox2.Location = new System.Drawing.Point(741, 12);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(500, 500);
			this.pictureBox2.TabIndex = 8;
			this.pictureBox2.TabStop = false;
			// 
			// RotateAroundPointTester
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1253, 524);
			this.Controls.Add(this.pictureBox2);
			this.Controls.Add(this.radOffset);
			this.Controls.Add(this.radCenter);
			this.Controls.Add(this.btnReset);
			this.Controls.Add(this.trackBar1);
			this.Controls.Add(this.pictureBox1);
			this.Name = "RotateAroundPointTester";
			this.Text = "RotateAroundPointTester";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.TrackBar trackBar1;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.RadioButton radCenter;
		private System.Windows.Forms.RadioButton radOffset;
		private System.Windows.Forms.PictureBox pictureBox2;
	}
}