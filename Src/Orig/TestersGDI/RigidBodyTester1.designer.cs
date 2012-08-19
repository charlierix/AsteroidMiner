namespace Game.Orig.TestersGDI
{
	partial class RigidBodyTester1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RigidBodyTester1));
            this.pictureBox1 = new Game.Orig.HelperClassesGDI.LargeMapViewer2D();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnAddMass = new System.Windows.Forms.Button();
            this.btnResetShip = new System.Windows.Forms.Button();
            this.txtTotalMass = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtNumPointMasses = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblBuildShipInstructions = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pnlThrustDirection = new System.Windows.Forms.Panel();
            this.radThrustDirectionForce = new System.Windows.Forms.RadioButton();
            this.radThrustDirectionFlame = new System.Windows.Forms.RadioButton();
            this.radThrusterCustom = new System.Windows.Forms.RadioButton();
            this.radThrusterTwin = new System.Windows.Forms.RadioButton();
            this.radThrusterStandard = new System.Windows.Forms.RadioButton();
            this.pctA = new System.Windows.Forms.PictureBox();
            this.pctD = new System.Windows.Forms.PictureBox();
            this.pctS = new System.Windows.Forms.PictureBox();
            this.pctW = new System.Windows.Forms.PictureBox();
            this.pctLeft = new System.Windows.Forms.PictureBox();
            this.pctRight = new System.Windows.Forms.PictureBox();
            this.pctDown = new System.Windows.Forms.PictureBox();
            this.pctUp = new System.Windows.Forms.PictureBox();
            this.btnResetThrusters = new System.Windows.Forms.Button();
            this.lblResetThrustersInstructions = new System.Windows.Forms.Label();
            this.lblCurrentInstruction = new System.Windows.Forms.Label();
            this.btnResetZoom = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.radGravityBall = new System.Windows.Forms.RadioButton();
            this.radGravityDown = new System.Windows.Forms.RadioButton();
            this.radGravityNone = new System.Windows.Forms.RadioButton();
            this.chkRunning = new System.Windows.Forms.CheckBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnGravBallRandomSpeed = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.btnResetGravityBall = new System.Windows.Forms.Button();
            this.btnChaseShip = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.pnlThrustDirection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pctA)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctS)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctUp)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackColor = System.Drawing.Color.Black;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox1.Location = new System.Drawing.Point(308, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.PanMouseButton = System.Windows.Forms.MouseButtons.Right;
            this.pictureBox1.Size = new System.Drawing.Size(580, 580);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.ZoomOnMouseWheel = true;
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            this.pictureBox1.Resize += new System.EventHandler(this.pictureBox1_Resize);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnAddMass);
            this.groupBox1.Controls.Add(this.btnResetShip);
            this.groupBox1.Controls.Add(this.txtTotalMass);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtNumPointMasses);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.lblBuildShipInstructions);
            this.groupBox1.Location = new System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(271, 181);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Ship Builder";
            // 
            // btnAddMass
            // 
            this.btnAddMass.Location = new System.Drawing.Point(6, 48);
            this.btnAddMass.Name = "btnAddMass";
            this.btnAddMass.Size = new System.Drawing.Size(135, 23);
            this.btnAddMass.TabIndex = 7;
            this.btnAddMass.Text = "Add Mass";
            this.btnAddMass.UseVisualStyleBackColor = true;
            this.btnAddMass.Click += new System.EventHandler(this.btnAddMass_Click);
            // 
            // btnResetShip
            // 
            this.btnResetShip.Location = new System.Drawing.Point(6, 19);
            this.btnResetShip.Name = "btnResetShip";
            this.btnResetShip.Size = new System.Drawing.Size(135, 23);
            this.btnResetShip.TabIndex = 6;
            this.btnResetShip.Text = "Reset Ship";
            this.btnResetShip.UseVisualStyleBackColor = true;
            this.btnResetShip.Click += new System.EventHandler(this.btnResetShip_Click);
            // 
            // txtTotalMass
            // 
            this.txtTotalMass.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtTotalMass.Location = new System.Drawing.Point(165, 151);
            this.txtTotalMass.Name = "txtTotalMass";
            this.txtTotalMass.ReadOnly = true;
            this.txtTotalMass.Size = new System.Drawing.Size(100, 20);
            this.txtTotalMass.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(6, 154);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(153, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Total Mass";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtNumPointMasses
            // 
            this.txtNumPointMasses.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtNumPointMasses.Location = new System.Drawing.Point(165, 125);
            this.txtNumPointMasses.Name = "txtNumPointMasses";
            this.txtNumPointMasses.ReadOnly = true;
            this.txtNumPointMasses.Size = new System.Drawing.Size(100, 20);
            this.txtNumPointMasses.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(6, 128);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(153, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Number of Point Masses";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblBuildShipInstructions
            // 
            this.lblBuildShipInstructions.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblBuildShipInstructions.ForeColor = System.Drawing.Color.DimGray;
            this.lblBuildShipInstructions.Location = new System.Drawing.Point(6, 74);
            this.lblBuildShipInstructions.Name = "lblBuildShipInstructions";
            this.lblBuildShipInstructions.Size = new System.Drawing.Size(259, 48);
            this.lblBuildShipInstructions.TabIndex = 1;
            this.lblBuildShipInstructions.Text = "Add masses to the rigid body by dragging a circle.  The start of the click is the" +
                " center of the mass, the radius of the circle is the amount of mass";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pnlThrustDirection);
            this.groupBox2.Controls.Add(this.radThrusterCustom);
            this.groupBox2.Controls.Add(this.radThrusterTwin);
            this.groupBox2.Controls.Add(this.radThrusterStandard);
            this.groupBox2.Controls.Add(this.pctA);
            this.groupBox2.Controls.Add(this.pctD);
            this.groupBox2.Controls.Add(this.pctS);
            this.groupBox2.Controls.Add(this.pctW);
            this.groupBox2.Controls.Add(this.pctLeft);
            this.groupBox2.Controls.Add(this.pctRight);
            this.groupBox2.Controls.Add(this.pctDown);
            this.groupBox2.Controls.Add(this.pctUp);
            this.groupBox2.Controls.Add(this.btnResetThrusters);
            this.groupBox2.Controls.Add(this.lblResetThrustersInstructions);
            this.groupBox2.Location = new System.Drawing.Point(6, 193);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(271, 264);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Truster Mapping";
            // 
            // pnlThrustDirection
            // 
            this.pnlThrustDirection.Controls.Add(this.radThrustDirectionForce);
            this.pnlThrustDirection.Controls.Add(this.radThrustDirectionFlame);
            this.pnlThrustDirection.Location = new System.Drawing.Point(144, 147);
            this.pnlThrustDirection.Name = "pnlThrustDirection";
            this.pnlThrustDirection.Size = new System.Drawing.Size(126, 43);
            this.pnlThrustDirection.TabIndex = 33;
            // 
            // radThrustDirectionForce
            // 
            this.radThrustDirectionForce.AutoSize = true;
            this.radThrustDirectionForce.Location = new System.Drawing.Point(0, 17);
            this.radThrustDirectionForce.Name = "radThrustDirectionForce";
            this.radThrustDirectionForce.Size = new System.Drawing.Size(125, 17);
            this.radThrustDirectionForce.TabIndex = 34;
            this.radThrustDirectionForce.Text = "Draw Force Direction";
            this.radThrustDirectionForce.UseVisualStyleBackColor = true;
            this.radThrustDirectionForce.CheckedChanged += new System.EventHandler(this.radThrustDirection_CheckedChanged);
            // 
            // radThrustDirectionFlame
            // 
            this.radThrustDirectionFlame.AutoSize = true;
            this.radThrustDirectionFlame.Checked = true;
            this.radThrustDirectionFlame.Location = new System.Drawing.Point(0, 0);
            this.radThrustDirectionFlame.Name = "radThrustDirectionFlame";
            this.radThrustDirectionFlame.Size = new System.Drawing.Size(126, 17);
            this.radThrustDirectionFlame.TabIndex = 33;
            this.radThrustDirectionFlame.TabStop = true;
            this.radThrustDirectionFlame.Text = "Draw Flame Direction";
            this.radThrustDirectionFlame.UseVisualStyleBackColor = true;
            this.radThrustDirectionFlame.CheckedChanged += new System.EventHandler(this.radThrustDirection_CheckedChanged);
            // 
            // radThrusterCustom
            // 
            this.radThrusterCustom.AutoSize = true;
            this.radThrusterCustom.Location = new System.Drawing.Point(6, 181);
            this.radThrusterCustom.Name = "radThrusterCustom";
            this.radThrusterCustom.Size = new System.Drawing.Size(60, 17);
            this.radThrusterCustom.TabIndex = 32;
            this.radThrusterCustom.Text = "Custom";
            this.radThrusterCustom.UseVisualStyleBackColor = true;
            this.radThrusterCustom.CheckedChanged += new System.EventHandler(this.radThruster_CheckedChanged);
            // 
            // radThrusterTwin
            // 
            this.radThrusterTwin.AutoSize = true;
            this.radThrusterTwin.Enabled = false;
            this.radThrusterTwin.Location = new System.Drawing.Point(6, 164);
            this.radThrusterTwin.Name = "radThrusterTwin";
            this.radThrusterTwin.Size = new System.Drawing.Size(48, 17);
            this.radThrusterTwin.TabIndex = 31;
            this.radThrusterTwin.Text = "Twin";
            this.radThrusterTwin.UseVisualStyleBackColor = true;
            this.radThrusterTwin.CheckedChanged += new System.EventHandler(this.radThruster_CheckedChanged);
            // 
            // radThrusterStandard
            // 
            this.radThrusterStandard.AutoSize = true;
            this.radThrusterStandard.Checked = true;
            this.radThrusterStandard.Location = new System.Drawing.Point(6, 147);
            this.radThrusterStandard.Name = "radThrusterStandard";
            this.radThrusterStandard.Size = new System.Drawing.Size(68, 17);
            this.radThrusterStandard.TabIndex = 30;
            this.radThrusterStandard.TabStop = true;
            this.radThrusterStandard.Text = "Standard";
            this.radThrusterStandard.UseVisualStyleBackColor = true;
            this.radThrusterStandard.CheckedChanged += new System.EventHandler(this.radThruster_CheckedChanged);
            // 
            // pctA
            // 
            this.pctA.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctA.Location = new System.Drawing.Point(6, 238);
            this.pctA.Name = "pctA";
            this.pctA.Size = new System.Drawing.Size(20, 20);
            this.pctA.TabIndex = 29;
            this.pctA.TabStop = false;
            this.pctA.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // pctD
            // 
            this.pctD.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctD.Location = new System.Drawing.Point(58, 238);
            this.pctD.Name = "pctD";
            this.pctD.Size = new System.Drawing.Size(20, 20);
            this.pctD.TabIndex = 28;
            this.pctD.TabStop = false;
            this.pctD.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // pctS
            // 
            this.pctS.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctS.Location = new System.Drawing.Point(32, 238);
            this.pctS.Name = "pctS";
            this.pctS.Size = new System.Drawing.Size(20, 20);
            this.pctS.TabIndex = 27;
            this.pctS.TabStop = false;
            this.pctS.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // pctW
            // 
            this.pctW.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctW.Location = new System.Drawing.Point(32, 212);
            this.pctW.Name = "pctW";
            this.pctW.Size = new System.Drawing.Size(20, 20);
            this.pctW.TabIndex = 26;
            this.pctW.TabStop = false;
            this.pctW.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // pctLeft
            // 
            this.pctLeft.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctLeft.Location = new System.Drawing.Point(193, 238);
            this.pctLeft.Name = "pctLeft";
            this.pctLeft.Size = new System.Drawing.Size(20, 20);
            this.pctLeft.TabIndex = 25;
            this.pctLeft.TabStop = false;
            this.pctLeft.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // pctRight
            // 
            this.pctRight.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctRight.Location = new System.Drawing.Point(245, 238);
            this.pctRight.Name = "pctRight";
            this.pctRight.Size = new System.Drawing.Size(20, 20);
            this.pctRight.TabIndex = 24;
            this.pctRight.TabStop = false;
            this.pctRight.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // pctDown
            // 
            this.pctDown.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctDown.Location = new System.Drawing.Point(219, 238);
            this.pctDown.Name = "pctDown";
            this.pctDown.Size = new System.Drawing.Size(20, 20);
            this.pctDown.TabIndex = 23;
            this.pctDown.TabStop = false;
            this.pctDown.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // pctUp
            // 
            this.pctUp.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pctUp.Location = new System.Drawing.Point(219, 212);
            this.pctUp.Name = "pctUp";
            this.pctUp.Size = new System.Drawing.Size(20, 20);
            this.pctUp.TabIndex = 22;
            this.pctUp.TabStop = false;
            this.pctUp.Click += new System.EventHandler(this.pctThrust_Click);
            // 
            // btnResetThrusters
            // 
            this.btnResetThrusters.Location = new System.Drawing.Point(6, 19);
            this.btnResetThrusters.Name = "btnResetThrusters";
            this.btnResetThrusters.Size = new System.Drawing.Size(135, 23);
            this.btnResetThrusters.TabIndex = 3;
            this.btnResetThrusters.Text = "Reset Thrusters";
            this.btnResetThrusters.UseVisualStyleBackColor = true;
            this.btnResetThrusters.Click += new System.EventHandler(this.btnResetThrusters_Click);
            // 
            // lblResetThrustersInstructions
            // 
            this.lblResetThrustersInstructions.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblResetThrustersInstructions.ForeColor = System.Drawing.Color.DimGray;
            this.lblResetThrustersInstructions.Location = new System.Drawing.Point(6, 45);
            this.lblResetThrustersInstructions.Name = "lblResetThrustersInstructions";
            this.lblResetThrustersInstructions.Size = new System.Drawing.Size(259, 99);
            this.lblResetThrustersInstructions.TabIndex = 2;
            this.lblResetThrustersInstructions.Text = resources.GetString("lblResetThrustersInstructions.Text");
            // 
            // lblCurrentInstruction
            // 
            this.lblCurrentInstruction.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblCurrentInstruction.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentInstruction.ForeColor = System.Drawing.Color.LightCoral;
            this.lblCurrentInstruction.Location = new System.Drawing.Point(12, 504);
            this.lblCurrentInstruction.Name = "lblCurrentInstruction";
            this.lblCurrentInstruction.Size = new System.Drawing.Size(290, 88);
            this.lblCurrentInstruction.TabIndex = 5;
            this.lblCurrentInstruction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblCurrentInstruction.Visible = false;
            // 
            // btnResetZoom
            // 
            this.btnResetZoom.Location = new System.Drawing.Point(6, 77);
            this.btnResetZoom.Name = "btnResetZoom";
            this.btnResetZoom.Size = new System.Drawing.Size(84, 23);
            this.btnResetZoom.TabIndex = 5;
            this.btnResetZoom.Text = "Reset Zoom";
            this.btnResetZoom.UseVisualStyleBackColor = true;
            this.btnResetZoom.Click += new System.EventHandler(this.btnResetZoom_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(6, 48);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(84, 23);
            this.btnStop.TabIndex = 4;
            this.btnStop.Text = "Stop Ship";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // radGravityBall
            // 
            this.radGravityBall.AutoSize = true;
            this.radGravityBall.Location = new System.Drawing.Point(6, 161);
            this.radGravityBall.Name = "radGravityBall";
            this.radGravityBall.Size = new System.Drawing.Size(78, 17);
            this.radGravityBall.TabIndex = 3;
            this.radGravityBall.Text = "Gravity Ball";
            this.radGravityBall.UseVisualStyleBackColor = true;
            this.radGravityBall.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // radGravityDown
            // 
            this.radGravityDown.AutoSize = true;
            this.radGravityDown.Location = new System.Drawing.Point(6, 141);
            this.radGravityDown.Name = "radGravityDown";
            this.radGravityDown.Size = new System.Drawing.Size(112, 17);
            this.radGravityDown.TabIndex = 2;
            this.radGravityDown.Text = "Downward Gravity";
            this.radGravityDown.UseVisualStyleBackColor = true;
            this.radGravityDown.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // radGravityNone
            // 
            this.radGravityNone.AutoSize = true;
            this.radGravityNone.Checked = true;
            this.radGravityNone.Location = new System.Drawing.Point(6, 121);
            this.radGravityNone.Name = "radGravityNone";
            this.radGravityNone.Size = new System.Drawing.Size(75, 17);
            this.radGravityNone.TabIndex = 1;
            this.radGravityNone.TabStop = true;
            this.radGravityNone.Text = "No Gravity";
            this.radGravityNone.UseVisualStyleBackColor = true;
            this.radGravityNone.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // chkRunning
            // 
            this.chkRunning.AutoSize = true;
            this.chkRunning.Location = new System.Drawing.Point(6, 6);
            this.chkRunning.Name = "chkRunning";
            this.chkRunning.Size = new System.Drawing.Size(66, 17);
            this.chkRunning.TabIndex = 0;
            this.chkRunning.Text = "Running";
            this.chkRunning.UseVisualStyleBackColor = true;
            this.chkRunning.CheckedChanged += new System.EventHandler(this.chkRunning_CheckedChanged);
            // 
            // timer1
            // 
            this.timer1.Interval = 10;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.ItemSize = new System.Drawing.Size(143, 18);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(290, 489);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl1.TabIndex = 7;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(282, 463);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Build";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnChaseShip);
            this.tabPage2.Controls.Add(this.btnGravBallRandomSpeed);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Controls.Add(this.numericUpDown1);
            this.tabPage2.Controls.Add(this.btnResetGravityBall);
            this.tabPage2.Controls.Add(this.radGravityBall);
            this.tabPage2.Controls.Add(this.radGravityDown);
            this.tabPage2.Controls.Add(this.btnStop);
            this.tabPage2.Controls.Add(this.radGravityNone);
            this.tabPage2.Controls.Add(this.btnResetZoom);
            this.tabPage2.Controls.Add(this.chkRunning);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(282, 463);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Simulate";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnGravBallRandomSpeed
            // 
            this.btnGravBallRandomSpeed.Location = new System.Drawing.Point(135, 210);
            this.btnGravBallRandomSpeed.Name = "btnGravBallRandomSpeed";
            this.btnGravBallRandomSpeed.Size = new System.Drawing.Size(118, 37);
            this.btnGravBallRandomSpeed.TabIndex = 9;
            this.btnGravBallRandomSpeed.Text = "Random GravBall Speeds";
            this.btnGravBallRandomSpeed.UseVisualStyleBackColor = true;
            this.btnGravBallRandomSpeed.Click += new System.EventHandler(this.btnGravBallRandomSpeed_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(126, 250);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 33);
            this.label1.TabIndex = 8;
            this.label1.Text = "Number Of Gravity Balls";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(201, 253);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(52, 20);
            this.numericUpDown1.TabIndex = 7;
            this.numericUpDown1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // btnResetGravityBall
            // 
            this.btnResetGravityBall.Location = new System.Drawing.Point(135, 184);
            this.btnResetGravityBall.Name = "btnResetGravityBall";
            this.btnResetGravityBall.Size = new System.Drawing.Size(118, 24);
            this.btnResetGravityBall.TabIndex = 6;
            this.btnResetGravityBall.Text = "Reset Gravity Ball";
            this.btnResetGravityBall.UseVisualStyleBackColor = true;
            this.btnResetGravityBall.Click += new System.EventHandler(this.btnResetGravityBall_Click);
            // 
            // btnChaseShip
            // 
            this.btnChaseShip.Location = new System.Drawing.Point(96, 48);
            this.btnChaseShip.Name = "btnChaseShip";
            this.btnChaseShip.Size = new System.Drawing.Size(84, 23);
            this.btnChaseShip.TabIndex = 10;
            this.btnChaseShip.Text = "Chase Ship";
            this.btnChaseShip.UseVisualStyleBackColor = true;
            this.btnChaseShip.Click += new System.EventHandler(this.btnChaseShip_Click);
            // 
            // RigidBodyTester1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 603);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.lblCurrentInstruction);
            this.Controls.Add(this.pictureBox1);
            this.KeyPreview = true;
            this.Name = "RigidBodyTester1";
            this.Text = "RigidBodyTester";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.pnlThrustDirection.ResumeLayout(false);
            this.pnlThrustDirection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pctA)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctD)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctS)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctUp)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private Game.Orig.HelperClassesGDI.LargeMapViewer2D pictureBox1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label lblBuildShipInstructions;
		private System.Windows.Forms.TextBox txtTotalMass;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtNumPointMasses;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Button btnResetThrusters;
		private System.Windows.Forms.Label lblResetThrustersInstructions;
		private System.Windows.Forms.PictureBox pctA;
		private System.Windows.Forms.PictureBox pctD;
		private System.Windows.Forms.PictureBox pctS;
		private System.Windows.Forms.PictureBox pctW;
		private System.Windows.Forms.PictureBox pctLeft;
		private System.Windows.Forms.PictureBox pctRight;
		private System.Windows.Forms.PictureBox pctDown;
		private System.Windows.Forms.PictureBox pctUp;
		private System.Windows.Forms.Label lblCurrentInstruction;
		private System.Windows.Forms.Button btnAddMass;
		private System.Windows.Forms.Button btnResetShip;
		private System.Windows.Forms.RadioButton radGravityDown;
		private System.Windows.Forms.RadioButton radGravityNone;
		private System.Windows.Forms.CheckBox chkRunning;
		private System.Windows.Forms.RadioButton radGravityBall;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.RadioButton radThrusterCustom;
		private System.Windows.Forms.RadioButton radThrusterTwin;
		private System.Windows.Forms.RadioButton radThrusterStandard;
		private System.Windows.Forms.Button btnStop;
		private System.Windows.Forms.Button btnResetZoom;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Panel pnlThrustDirection;
		private System.Windows.Forms.RadioButton radThrustDirectionForce;
		private System.Windows.Forms.RadioButton radThrustDirectionFlame;
		private System.Windows.Forms.Button btnResetGravityBall;
		private System.Windows.Forms.NumericUpDown numericUpDown1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnGravBallRandomSpeed;
        private System.Windows.Forms.Button btnChaseShip;
	}
}