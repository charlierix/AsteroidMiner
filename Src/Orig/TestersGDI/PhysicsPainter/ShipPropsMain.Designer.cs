namespace Game.Orig.TestersGDI.PhysicsPainter
{
    partial class ShipPropsMain
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
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.btnChase = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblThrustMax = new System.Windows.Forms.Label();
            this.lblThrustMin = new System.Windows.Forms.Label();
            this.trkThrusterOffset = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.lblSize = new System.Windows.Forms.Label();
            this.txtMaxSize = new System.Windows.Forms.TextBox();
            this.txtMinSize = new System.Windows.Forms.TextBox();
            this.trkSize = new System.Windows.Forms.TrackBar();
            this.lblThruster = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.trkThrusterOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkSize)).BeginInit();
            this.SuspendLayout();
            // 
            // cboType
            // 
            this.cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboType.FormattingEnabled = true;
            this.cboType.Location = new System.Drawing.Point(46, 11);
            this.cboType.Name = "cboType";
            this.cboType.Size = new System.Drawing.Size(122, 21);
            this.cboType.TabIndex = 46;
            this.cboType.SelectedIndexChanged += new System.EventHandler(this.cboType_SelectedIndexChanged);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(70, 55);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(77, 13);
            this.linkLabel1.TabIndex = 45;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Keyboard Help";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // btnChase
            // 
            this.btnChase.Location = new System.Drawing.Point(112, 71);
            this.btnChase.Name = "btnChase";
            this.btnChase.Size = new System.Drawing.Size(50, 23);
            this.btnChase.TabIndex = 44;
            this.btnChase.Text = "Chase";
            this.btnChase.UseVisualStyleBackColor = true;
            this.btnChase.Click += new System.EventHandler(this.btnChase_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(56, 71);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(50, 23);
            this.btnStop.TabIndex = 38;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // lblThrustMax
            // 
            this.lblThrustMax.AutoSize = true;
            this.lblThrustMax.Location = new System.Drawing.Point(216, 133);
            this.lblThrustMax.Name = "lblThrustMax";
            this.lblThrustMax.Size = new System.Drawing.Size(19, 13);
            this.lblThrustMax.TabIndex = 43;
            this.lblThrustMax.Text = "90";
            // 
            // lblThrustMin
            // 
            this.lblThrustMin.AutoSize = true;
            this.lblThrustMin.Location = new System.Drawing.Point(154, 133);
            this.lblThrustMin.Name = "lblThrustMin";
            this.lblThrustMin.Size = new System.Drawing.Size(13, 13);
            this.lblThrustMin.TabIndex = 42;
            this.lblThrustMin.Text = "0";
            // 
            // trkThrusterOffset
            // 
            this.trkThrusterOffset.Location = new System.Drawing.Point(154, 149);
            this.trkThrusterOffset.Maximum = 90;
            this.trkThrusterOffset.Name = "trkThrusterOffset";
            this.trkThrusterOffset.Size = new System.Drawing.Size(81, 29);
            this.trkThrusterOffset.TabIndex = 40;
            this.trkThrusterOffset.TickFrequency = 45;
            this.trkThrusterOffset.Value = 45;
            this.trkThrusterOffset.Scroll += new System.EventHandler(this.trkThrusterOffset_Scroll);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(5, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 39;
            this.label2.Text = "Type";
            // 
            // lblSize
            // 
            this.lblSize.AutoSize = true;
            this.lblSize.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSize.Location = new System.Drawing.Point(63, 133);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(31, 13);
            this.lblSize.TabIndex = 37;
            this.lblSize.Text = "Size";
            // 
            // txtMaxSize
            // 
            this.txtMaxSize.Location = new System.Drawing.Point(99, 130);
            this.txtMaxSize.Name = "txtMaxSize";
            this.txtMaxSize.Size = new System.Drawing.Size(49, 20);
            this.txtMaxSize.TabIndex = 36;
            this.txtMaxSize.Text = "500";
            this.txtMaxSize.TextChanged += new System.EventHandler(this.txtMaxSize_TextChanged);
            // 
            // txtMinSize
            // 
            this.txtMinSize.Location = new System.Drawing.Point(8, 130);
            this.txtMinSize.Name = "txtMinSize";
            this.txtMinSize.Size = new System.Drawing.Size(49, 20);
            this.txtMinSize.TabIndex = 35;
            this.txtMinSize.Text = "200";
            this.txtMinSize.TextChanged += new System.EventHandler(this.txtMinSize_TextChanged);
            // 
            // trkSize
            // 
            this.trkSize.Location = new System.Drawing.Point(8, 149);
            this.trkSize.Maximum = 1000;
            this.trkSize.Name = "trkSize";
            this.trkSize.Size = new System.Drawing.Size(140, 29);
            this.trkSize.TabIndex = 34;
            this.trkSize.TickFrequency = 125;
            this.trkSize.Value = 500;
            this.trkSize.Scroll += new System.EventHandler(this.trkSize_Scroll);
            // 
            // lblThruster
            // 
            this.lblThruster.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThruster.Location = new System.Drawing.Point(169, 117);
            this.lblThruster.Name = "lblThruster";
            this.lblThruster.Size = new System.Drawing.Size(54, 29);
            this.lblThruster.TabIndex = 41;
            this.lblThruster.Text = "Thruster Offset";
            this.lblThruster.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // ShipPropsMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cboType);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.btnChase);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.lblThrustMax);
            this.Controls.Add(this.lblThrustMin);
            this.Controls.Add(this.trkThrusterOffset);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblSize);
            this.Controls.Add(this.txtMaxSize);
            this.Controls.Add(this.txtMinSize);
            this.Controls.Add(this.trkSize);
            this.Controls.Add(this.lblThruster);
            this.Name = "ShipPropsMain";
            this.Size = new System.Drawing.Size(250, 185);
            ((System.ComponentModel.ISupportInitialize)(this.trkThrusterOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button btnChase;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblThrustMax;
        private System.Windows.Forms.Label lblThrustMin;
        private System.Windows.Forms.TrackBar trkThrusterOffset;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.TextBox txtMaxSize;
        private System.Windows.Forms.TextBox txtMinSize;
        private System.Windows.Forms.TrackBar trkSize;
        private System.Windows.Forms.Label lblThruster;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
