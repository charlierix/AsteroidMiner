namespace Game.Orig.TestersGDI.PhysicsPainter
{
    partial class ShipPropsTractor
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
			this.cboType = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.lblThrustMax = new System.Windows.Forms.Label();
			this.lblThrustMin = new System.Windows.Forms.Label();
			this.trkSweepAngle = new System.Windows.Forms.TrackBar();
			this.lblThruster = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.txtMaxSize = new System.Windows.Forms.TextBox();
			this.trkSize = new System.Windows.Forms.TrackBar();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.txtStrengthMax = new System.Windows.Forms.TextBox();
			this.trkStrengthNear = new System.Windows.Forms.TrackBar();
			this.trkStrengthFar = new System.Windows.Forms.TrackBar();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.trkSweepAngle)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkSize)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkStrengthNear)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkStrengthFar)).BeginInit();
			this.SuspendLayout();
			// 
			// cboType
			// 
			this.cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboType.DropDownWidth = 200;
			this.cboType.FormattingEnabled = true;
			this.cboType.Location = new System.Drawing.Point(46, 11);
			this.cboType.Name = "cboType";
			this.cboType.Size = new System.Drawing.Size(122, 21);
			this.cboType.TabIndex = 48;
			this.cboType.SelectedIndexChanged += new System.EventHandler(this.cboType_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(5, 14);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(35, 13);
			this.label2.TabIndex = 47;
			this.label2.Text = "Type";
			// 
			// lblThrustMax
			// 
			this.lblThrustMax.AutoSize = true;
			this.lblThrustMax.Location = new System.Drawing.Point(215, 134);
			this.lblThrustMax.Name = "lblThrustMax";
			this.lblThrustMax.Size = new System.Drawing.Size(25, 13);
			this.lblThrustMax.TabIndex = 52;
			this.lblThrustMax.Text = "360";
			// 
			// lblThrustMin
			// 
			this.lblThrustMin.AutoSize = true;
			this.lblThrustMin.Location = new System.Drawing.Point(128, 134);
			this.lblThrustMin.Name = "lblThrustMin";
			this.lblThrustMin.Size = new System.Drawing.Size(13, 13);
			this.lblThrustMin.TabIndex = 51;
			this.lblThrustMin.Text = "1";
			// 
			// trkSweepAngle
			// 
			this.trkSweepAngle.Location = new System.Drawing.Point(125, 153);
			this.trkSweepAngle.Maximum = 360;
			this.trkSweepAngle.Minimum = 1;
			this.trkSweepAngle.Name = "trkSweepAngle";
			this.trkSweepAngle.Size = new System.Drawing.Size(111, 29);
			this.trkSweepAngle.TabIndex = 49;
			this.trkSweepAngle.TickFrequency = 45;
			this.trkSweepAngle.Value = 45;
			this.trkSweepAngle.Scroll += new System.EventHandler(this.trkSweepAngle_Scroll);
			// 
			// lblThruster
			// 
			this.lblThruster.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblThruster.Location = new System.Drawing.Point(153, 123);
			this.lblThruster.Name = "lblThruster";
			this.lblThruster.Size = new System.Drawing.Size(54, 29);
			this.lblThruster.TabIndex = 50;
			this.lblThruster.Text = "Sweep Angle";
			this.lblThruster.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.SystemColors.WindowText;
			this.label1.Location = new System.Drawing.Point(51, 134);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(31, 13);
			this.label1.TabIndex = 56;
			this.label1.Text = "Size";
			// 
			// txtMaxSize
			// 
			this.txtMaxSize.Location = new System.Drawing.Point(90, 131);
			this.txtMaxSize.Name = "txtMaxSize";
			this.txtMaxSize.Size = new System.Drawing.Size(39, 20);
			this.txtMaxSize.TabIndex = 55;
			this.txtMaxSize.Text = "8000";
			this.txtMaxSize.TextChanged += new System.EventHandler(this.txtMaxSize_TextChanged);
			// 
			// trkSize
			// 
			this.trkSize.Location = new System.Drawing.Point(8, 153);
			this.trkSize.Maximum = 1000;
			this.trkSize.Name = "trkSize";
			this.trkSize.Size = new System.Drawing.Size(121, 29);
			this.trkSize.TabIndex = 53;
			this.trkSize.TickFrequency = 125;
			this.trkSize.Value = 800;
			this.trkSize.Scroll += new System.EventHandler(this.trkSize_Scroll);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(5, 134);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(25, 13);
			this.label3.TabIndex = 57;
			this.label3.Text = "100";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(10, 47);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(13, 13);
			this.label4.TabIndex = 61;
			this.label4.Text = "0";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.ForeColor = System.Drawing.SystemColors.WindowText;
			this.label5.Location = new System.Drawing.Point(35, 47);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(55, 13);
			this.label5.TabIndex = 60;
			this.label5.Text = "Strength";
			// 
			// txtStrengthMax
			// 
			this.txtStrengthMax.Location = new System.Drawing.Point(90, 44);
			this.txtStrengthMax.Name = "txtStrengthMax";
			this.txtStrengthMax.Size = new System.Drawing.Size(80, 20);
			this.txtStrengthMax.TabIndex = 59;
			this.txtStrengthMax.Text = "500000000";
			this.txtStrengthMax.TextChanged += new System.EventHandler(this.txtStrengthMax_TextChanged);
			// 
			// trkStrengthNear
			// 
			this.trkStrengthNear.Location = new System.Drawing.Point(8, 66);
			this.trkStrengthNear.Maximum = 1000;
			this.trkStrengthNear.Name = "trkStrengthNear";
			this.trkStrengthNear.Size = new System.Drawing.Size(121, 29);
			this.trkStrengthNear.TabIndex = 58;
			this.trkStrengthNear.TickFrequency = 125;
			this.trkStrengthNear.TickStyle = System.Windows.Forms.TickStyle.None;
			this.trkStrengthNear.Value = 500;
			this.trkStrengthNear.Scroll += new System.EventHandler(this.trkStrengthNear_Scroll);
			// 
			// trkStrengthFar
			// 
			this.trkStrengthFar.Location = new System.Drawing.Point(8, 83);
			this.trkStrengthFar.Maximum = 1000;
			this.trkStrengthFar.Name = "trkStrengthFar";
			this.trkStrengthFar.Size = new System.Drawing.Size(121, 29);
			this.trkStrengthFar.TabIndex = 62;
			this.trkStrengthFar.TickFrequency = 125;
			this.trkStrengthFar.Value = 250;
			this.trkStrengthFar.Scroll += new System.EventHandler(this.trkStrengthFar_Scroll);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(128, 67);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(42, 13);
			this.label6.TabIndex = 63;
			this.label6.Text = "At Zero";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(128, 83);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(85, 13);
			this.label7.TabIndex = 64;
			this.label7.Text = "At Max Distance";
			// 
			// ShipPropsTractor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.trkStrengthFar);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.txtStrengthMax);
			this.Controls.Add(this.trkStrengthNear);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtMaxSize);
			this.Controls.Add(this.trkSize);
			this.Controls.Add(this.lblThrustMax);
			this.Controls.Add(this.lblThrustMin);
			this.Controls.Add(this.trkSweepAngle);
			this.Controls.Add(this.lblThruster);
			this.Controls.Add(this.cboType);
			this.Controls.Add(this.label2);
			this.Name = "ShipPropsTractor";
			this.Size = new System.Drawing.Size(250, 185);
			((System.ComponentModel.ISupportInitialize)(this.trkSweepAngle)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkSize)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkStrengthNear)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkStrengthFar)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Label lblThrustMax;
		private System.Windows.Forms.Label lblThrustMin;
		private System.Windows.Forms.TrackBar trkSweepAngle;
		private System.Windows.Forms.Label lblThruster;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtMaxSize;
		private System.Windows.Forms.TrackBar trkSize;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtStrengthMax;
		private System.Windows.Forms.TrackBar trkStrengthNear;
		private System.Windows.Forms.TrackBar trkStrengthFar;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;

    }
}
