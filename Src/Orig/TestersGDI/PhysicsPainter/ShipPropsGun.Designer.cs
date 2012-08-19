namespace Game.Orig.TestersGDI.PhysicsPainter
{
	partial class ShipPropsGun
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
			this.lblThrustMax = new System.Windows.Forms.Label();
			this.lblThrustMin = new System.Windows.Forms.Label();
			this.trkMachineGunOffset = new System.Windows.Forms.TrackBar();
			this.lblThruster = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.lblCrossoverMax = new System.Windows.Forms.Label();
			this.lblCrossoverMin = new System.Windows.Forms.Label();
			this.trkCrossoverDistance = new System.Windows.Forms.TrackBar();
			this.label3 = new System.Windows.Forms.Label();
			this.chkInfinity = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.chkIgnoreOtherProjectiles = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.trkMachineGunOffset)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trkCrossoverDistance)).BeginInit();
			this.SuspendLayout();
			// 
			// lblThrustMax
			// 
			this.lblThrustMax.AutoSize = true;
			this.lblThrustMax.Location = new System.Drawing.Point(219, 137);
			this.lblThrustMax.Name = "lblThrustMax";
			this.lblThrustMax.Size = new System.Drawing.Size(19, 13);
			this.lblThrustMax.TabIndex = 47;
			this.lblThrustMax.Text = "90";
			// 
			// lblThrustMin
			// 
			this.lblThrustMin.AutoSize = true;
			this.lblThrustMin.Location = new System.Drawing.Point(157, 137);
			this.lblThrustMin.Name = "lblThrustMin";
			this.lblThrustMin.Size = new System.Drawing.Size(13, 13);
			this.lblThrustMin.TabIndex = 46;
			this.lblThrustMin.Text = "1";
			// 
			// trkMachineGunOffset
			// 
			this.trkMachineGunOffset.Location = new System.Drawing.Point(157, 153);
			this.trkMachineGunOffset.Maximum = 90;
			this.trkMachineGunOffset.Minimum = 1;
			this.trkMachineGunOffset.Name = "trkMachineGunOffset";
			this.trkMachineGunOffset.Size = new System.Drawing.Size(81, 29);
			this.trkMachineGunOffset.TabIndex = 44;
			this.trkMachineGunOffset.TickFrequency = 45;
			this.trkMachineGunOffset.Value = 30;
			this.trkMachineGunOffset.Scroll += new System.EventHandler(this.trkMachineGunOffset_Scroll);
			// 
			// lblThruster
			// 
			this.lblThruster.AutoSize = true;
			this.lblThruster.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblThruster.Location = new System.Drawing.Point(174, 137);
			this.lblThruster.Name = "lblThruster";
			this.lblThruster.Size = new System.Drawing.Size(41, 13);
			this.lblThruster.TabIndex = 45;
			this.lblThruster.Text = "Offset";
			this.lblThruster.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// lblCrossoverMax
			// 
			this.lblCrossoverMax.AutoSize = true;
			this.lblCrossoverMax.Location = new System.Drawing.Point(114, 137);
			this.lblCrossoverMax.Name = "lblCrossoverMax";
			this.lblCrossoverMax.Size = new System.Drawing.Size(37, 13);
			this.lblCrossoverMax.TabIndex = 51;
			this.lblCrossoverMax.Text = "15000";
			// 
			// lblCrossoverMin
			// 
			this.lblCrossoverMin.AutoSize = true;
			this.lblCrossoverMin.Location = new System.Drawing.Point(3, 137);
			this.lblCrossoverMin.Name = "lblCrossoverMin";
			this.lblCrossoverMin.Size = new System.Drawing.Size(25, 13);
			this.lblCrossoverMin.TabIndex = 50;
			this.lblCrossoverMin.Text = "500";
			// 
			// trkCrossoverDistance
			// 
			this.trkCrossoverDistance.Location = new System.Drawing.Point(3, 153);
			this.trkCrossoverDistance.Maximum = 1000;
			this.trkCrossoverDistance.Name = "trkCrossoverDistance";
			this.trkCrossoverDistance.Size = new System.Drawing.Size(148, 29);
			this.trkCrossoverDistance.TabIndex = 48;
			this.trkCrossoverDistance.TickFrequency = 250;
			this.trkCrossoverDistance.Value = 500;
			this.trkCrossoverDistance.Scroll += new System.EventHandler(this.trkCrossoverDistance_Scroll);
			// 
			// label3
			// 
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(15, 119);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(121, 16);
			this.label3.TabIndex = 49;
			this.label3.Text = "Crossover Distance";
			this.label3.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// chkInfinity
			// 
			this.chkInfinity.AutoSize = true;
			this.chkInfinity.Checked = true;
			this.chkInfinity.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkInfinity.Location = new System.Drawing.Point(47, 133);
			this.chkInfinity.Name = "chkInfinity";
			this.chkInfinity.Size = new System.Drawing.Size(56, 17);
			this.chkInfinity.TabIndex = 52;
			this.chkInfinity.Text = "Infinity";
			this.chkInfinity.UseVisualStyleBackColor = true;
			this.chkInfinity.CheckedChanged += new System.EventHandler(this.chkInfinity_CheckedChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(3, 97);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(82, 13);
			this.label4.TabIndex = 53;
			this.label4.Text = "Machine Gun";
			this.label4.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.panel1.Location = new System.Drawing.Point(90, 104);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(138, 1);
			this.panel1.TabIndex = 54;
			// 
			// chkIgnoreOtherProjectiles
			// 
			this.chkIgnoreOtherProjectiles.AutoSize = true;
			this.chkIgnoreOtherProjectiles.Checked = true;
			this.chkIgnoreOtherProjectiles.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkIgnoreOtherProjectiles.Location = new System.Drawing.Point(6, 7);
			this.chkIgnoreOtherProjectiles.Name = "chkIgnoreOtherProjectiles";
			this.chkIgnoreOtherProjectiles.Size = new System.Drawing.Size(136, 17);
			this.chkIgnoreOtherProjectiles.TabIndex = 55;
			this.chkIgnoreOtherProjectiles.Text = "Ignore Other Projectiles";
			this.chkIgnoreOtherProjectiles.UseVisualStyleBackColor = true;
			this.chkIgnoreOtherProjectiles.CheckedChanged += new System.EventHandler(this.chkIgnoreOtherProjectiles_CheckedChanged);
			// 
			// ShipPropsGun
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.chkIgnoreOtherProjectiles);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.chkInfinity);
			this.Controls.Add(this.lblCrossoverMax);
			this.Controls.Add(this.lblCrossoverMin);
			this.Controls.Add(this.trkCrossoverDistance);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.lblThrustMax);
			this.Controls.Add(this.lblThrustMin);
			this.Controls.Add(this.trkMachineGunOffset);
			this.Controls.Add(this.lblThruster);
			this.Name = "ShipPropsGun";
			this.Size = new System.Drawing.Size(250, 185);
			((System.ComponentModel.ISupportInitialize)(this.trkMachineGunOffset)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trkCrossoverDistance)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblThrustMax;
		private System.Windows.Forms.Label lblThrustMin;
		private System.Windows.Forms.TrackBar trkMachineGunOffset;
		private System.Windows.Forms.Label lblThruster;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Label lblCrossoverMax;
		private System.Windows.Forms.Label lblCrossoverMin;
		private System.Windows.Forms.TrackBar trkCrossoverDistance;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox chkInfinity;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.CheckBox chkIgnoreOtherProjectiles;

	}
}
