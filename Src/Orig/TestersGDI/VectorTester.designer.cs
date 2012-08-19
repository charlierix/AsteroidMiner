namespace Game.Orig.TestersGDI
{
    partial class VectorTester
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
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			this.btnLeft = new System.Windows.Forms.Button();
			this.btnUp = new System.Windows.Forms.Button();
			this.btnRight = new System.Windows.Forms.Button();
			this.btnDown = new System.Windows.Forms.Button();
			this.btnUpLeft = new System.Windows.Forms.Button();
			this.btnUpRight = new System.Windows.Forms.Button();
			this.btnDownLeft = new System.Windows.Forms.Button();
			this.btnDownRight = new System.Windows.Forms.Button();
			this.trkXAxis = new System.Windows.Forms.TrackBar();
			this.trkYAxis = new System.Windows.Forms.TrackBar();
			this.trkZAxis = new System.Windows.Forms.TrackBar();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.btnRotationMatrix = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkXAxis)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkYAxis)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkZAxis)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 12);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 0;
			this.button1.Text = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(93, 12);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 1;
			this.button2.Text = "button2";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.SlateGray;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox1.Location = new System.Drawing.Point(214, 63);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(500, 500);
			this.pictureBox1.TabIndex = 2;
			this.pictureBox1.TabStop = false;
			// 
			// trackBar1
			// 
			this.trackBar1.Location = new System.Drawing.Point(179, 63);
			this.trackBar1.Maximum = 360;
			this.trackBar1.Minimum = -360;
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBar1.Size = new System.Drawing.Size(29, 500);
			this.trackBar1.TabIndex = 3;
			this.trackBar1.TickFrequency = 45;
			this.trackBar1.Scroll += new System.EventHandler(this.trackBar_Scroll);
			// 
			// btnLeft
			// 
			this.btnLeft.Location = new System.Drawing.Point(103, 517);
			this.btnLeft.Name = "btnLeft";
			this.btnLeft.Size = new System.Drawing.Size(23, 23);
			this.btnLeft.TabIndex = 4;
			this.btnLeft.Text = "--";
			this.btnLeft.UseVisualStyleBackColor = true;
			this.btnLeft.Click += new System.EventHandler(this.btnLeft_Click);
			// 
			// btnUp
			// 
			this.btnUp.Location = new System.Drawing.Point(127, 494);
			this.btnUp.Name = "btnUp";
			this.btnUp.Size = new System.Drawing.Size(23, 23);
			this.btnUp.TabIndex = 5;
			this.btnUp.Text = "|";
			this.btnUp.UseVisualStyleBackColor = true;
			this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
			// 
			// btnRight
			// 
			this.btnRight.Location = new System.Drawing.Point(150, 517);
			this.btnRight.Name = "btnRight";
			this.btnRight.Size = new System.Drawing.Size(23, 23);
			this.btnRight.TabIndex = 6;
			this.btnRight.Text = "--";
			this.btnRight.UseVisualStyleBackColor = true;
			this.btnRight.Click += new System.EventHandler(this.btnRight_Click);
			// 
			// btnDown
			// 
			this.btnDown.Location = new System.Drawing.Point(127, 540);
			this.btnDown.Name = "btnDown";
			this.btnDown.Size = new System.Drawing.Size(23, 23);
			this.btnDown.TabIndex = 7;
			this.btnDown.Text = "|";
			this.btnDown.UseVisualStyleBackColor = true;
			this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
			// 
			// btnUpLeft
			// 
			this.btnUpLeft.Location = new System.Drawing.Point(103, 494);
			this.btnUpLeft.Name = "btnUpLeft";
			this.btnUpLeft.Size = new System.Drawing.Size(23, 23);
			this.btnUpLeft.TabIndex = 8;
			this.btnUpLeft.Text = "\\";
			this.btnUpLeft.UseVisualStyleBackColor = true;
			this.btnUpLeft.Click += new System.EventHandler(this.btnUpLeft_Click);
			// 
			// btnUpRight
			// 
			this.btnUpRight.Location = new System.Drawing.Point(150, 494);
			this.btnUpRight.Name = "btnUpRight";
			this.btnUpRight.Size = new System.Drawing.Size(23, 23);
			this.btnUpRight.TabIndex = 9;
			this.btnUpRight.Text = "/";
			this.btnUpRight.UseVisualStyleBackColor = true;
			this.btnUpRight.Click += new System.EventHandler(this.btnUpRight_Click);
			// 
			// btnDownLeft
			// 
			this.btnDownLeft.Location = new System.Drawing.Point(103, 540);
			this.btnDownLeft.Name = "btnDownLeft";
			this.btnDownLeft.Size = new System.Drawing.Size(23, 23);
			this.btnDownLeft.TabIndex = 10;
			this.btnDownLeft.Text = "/";
			this.btnDownLeft.UseVisualStyleBackColor = true;
			this.btnDownLeft.Click += new System.EventHandler(this.btnDownLeft_Click);
			// 
			// btnDownRight
			// 
			this.btnDownRight.Location = new System.Drawing.Point(150, 540);
			this.btnDownRight.Name = "btnDownRight";
			this.btnDownRight.Size = new System.Drawing.Size(23, 23);
			this.btnDownRight.TabIndex = 11;
			this.btnDownRight.Text = "\\";
			this.btnDownRight.UseVisualStyleBackColor = true;
			this.btnDownRight.Click += new System.EventHandler(this.btnDownRight_Click);
			// 
			// trkXAxis
			// 
			this.trkXAxis.Location = new System.Drawing.Point(6, 138);
			this.trkXAxis.Minimum = -10;
			this.trkXAxis.Name = "trkXAxis";
			this.trkXAxis.Size = new System.Drawing.Size(148, 29);
			this.trkXAxis.TabIndex = 12;
			this.trkXAxis.Scroll += new System.EventHandler(this.trackBar_Scroll);
			// 
			// trkYAxis
			// 
			this.trkYAxis.Location = new System.Drawing.Point(73, 27);
			this.trkYAxis.Minimum = -10;
			this.trkYAxis.Name = "trkYAxis";
			this.trkYAxis.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkYAxis.Size = new System.Drawing.Size(29, 104);
			this.trkYAxis.TabIndex = 13;
			this.trkYAxis.Scroll += new System.EventHandler(this.trackBar_Scroll);
			// 
			// trkZAxis
			// 
			this.trkZAxis.Location = new System.Drawing.Point(73, 172);
			this.trkZAxis.Minimum = -10;
			this.trkZAxis.Name = "trkZAxis";
			this.trkZAxis.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trkZAxis.Size = new System.Drawing.Size(29, 104);
			this.trkZAxis.TabIndex = 14;
			this.trkZAxis.Value = 1;
			this.trkZAxis.Scroll += new System.EventHandler(this.trackBar_Scroll);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.trkYAxis);
			this.groupBox1.Controls.Add(this.trkZAxis);
			this.groupBox1.Controls.Add(this.trkXAxis);
			this.groupBox1.Location = new System.Drawing.Point(12, 63);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(161, 292);
			this.groupBox1.TabIndex = 15;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Rotate Around";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(100, 263);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(14, 13);
			this.label3.TabIndex = 17;
			this.label3.Text = "Z";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(100, 27);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(14, 13);
			this.label2.TabIndex = 16;
			this.label2.Text = "Y";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 122);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(14, 13);
			this.label1.TabIndex = 15;
			this.label1.Text = "X";
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(174, 12);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(99, 36);
			this.button3.TabIndex = 16;
			this.button3.Text = "Combining Rotation Quats";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(279, 12);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(132, 36);
			this.button4.TabIndex = 17;
			this.button4.Text = "Combining Rotation Quats, test for drift";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// btnRotationMatrix
			// 
			this.btnRotationMatrix.Location = new System.Drawing.Point(417, 12);
			this.btnRotationMatrix.Name = "btnRotationMatrix";
			this.btnRotationMatrix.Size = new System.Drawing.Size(80, 36);
			this.btnRotationMatrix.TabIndex = 18;
			this.btnRotationMatrix.Text = "quat to from rot matrix";
			this.btnRotationMatrix.UseVisualStyleBackColor = true;
			this.btnRotationMatrix.Click += new System.EventHandler(this.btnRotationMatrix_Click);
			// 
			// button5
			// 
			this.button5.Location = new System.Drawing.Point(496, 12);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(18, 36);
			this.button5.TabIndex = 19;
			this.button5.Text = "2";
			this.button5.UseVisualStyleBackColor = true;
			this.button5.Click += new System.EventHandler(this.button5_Click);
			// 
			// VectorTester
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(726, 575);
			this.Controls.Add(this.button5);
			this.Controls.Add(this.btnRotationMatrix);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnDownRight);
			this.Controls.Add(this.btnDownLeft);
			this.Controls.Add(this.btnUpRight);
			this.Controls.Add(this.btnUpLeft);
			this.Controls.Add(this.btnDown);
			this.Controls.Add(this.btnRight);
			this.Controls.Add(this.btnUp);
			this.Controls.Add(this.btnLeft);
			this.Controls.Add(this.trackBar1);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Name = "VectorTester";
			this.Text = "VectorTester";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkXAxis)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkYAxis)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkZAxis)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Button btnLeft;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.Button btnRight;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnUpLeft;
        private System.Windows.Forms.Button btnUpRight;
        private System.Windows.Forms.Button btnDownLeft;
        private System.Windows.Forms.Button btnDownRight;
        private System.Windows.Forms.TrackBar trkXAxis;
        private System.Windows.Forms.TrackBar trkYAxis;
        private System.Windows.Forms.TrackBar trkZAxis;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.Button btnRotationMatrix;
		private System.Windows.Forms.Button button5;
    }
}