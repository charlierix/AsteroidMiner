namespace Game.Orig.TestersGDI
{
	partial class MapTester1
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
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.chkRunning = new System.Windows.Forms.CheckBox();
            this.trkElapsedTime = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.txtElapsedTime = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.txtThreshold = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.trkThreshold = new System.Windows.Forms.TrackBar();
            this.btnZoomFit = new System.Windows.Forms.Button();
            this.radGravityNone = new System.Windows.Forms.RadioButton();
            this.radGravityDown = new System.Windows.Forms.RadioButton();
            this.radGravityBalls = new System.Windows.Forms.RadioButton();
            this.btnZeroVelocity = new System.Windows.Forms.Button();
            this.btnRandomVelocity = new System.Windows.Forms.Button();
            this.chkDrawCollisionsRed = new System.Windows.Forms.CheckBox();
            this.trkYBoundry = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.txtElasticity = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.trkElasticity = new System.Windows.Forms.TrackBar();
            this.txtPullApartPercent = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.trkPullApartPercent = new System.Windows.Forms.TrackBar();
            this.chkSmallObjectsAreMassive = new System.Windows.Forms.CheckBox();
            this.btnAddSolidBall = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.trkGravityForce = new System.Windows.Forms.TrackBar();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.trkVectorFieldForce = new System.Windows.Forms.TrackBar();
            this.cboVectorField = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.trkVectorFieldSize = new System.Windows.Forms.TrackBar();
            this.btnAddRigidBody = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.chkIncludeShip = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.trkBoundryScale = new System.Windows.Forms.TrackBar();
            this.btnChaseShip = new System.Windows.Forms.Button();
            this.txtPullApartSpring = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.trkPullApartSpring = new System.Windows.Forms.TrackBar();
            this.radPullApartInstant = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radPullApartNone = new System.Windows.Forms.RadioButton();
            this.radPullApartSpring = new System.Windows.Forms.RadioButton();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.txtStaticFriction = new System.Windows.Forms.TextBox();
            this.txtKineticFriction = new System.Windows.Forms.TextBox();
            this.trkStaticFriction = new System.Windows.Forms.TrackBar();
            this.trkKineticFriction = new System.Windows.Forms.TrackBar();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.chkDoStandardCollisions = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkElapsedTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkYBoundry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkElasticity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkPullApartPercent)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkGravityForce)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkVectorFieldForce)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkVectorFieldSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkBoundryScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkPullApartSpring)).BeginInit();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkStaticFriction)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkKineticFriction)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackColor = System.Drawing.Color.Gray;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox1.Location = new System.Drawing.Point(272, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(580, 589);
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.ZoomOnMouseWheel = true;
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(12, 35);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(60, 23);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "Add Ball";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(12, 64);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(93, 23);
            this.btnRemove.TabIndex = 2;
            this.btnRemove.Text = "Remove Ball";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(111, 64);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(93, 23);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "Clear Balls";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // chkRunning
            // 
            this.chkRunning.AutoSize = true;
            this.chkRunning.Location = new System.Drawing.Point(12, 12);
            this.chkRunning.Name = "chkRunning";
            this.chkRunning.Size = new System.Drawing.Size(66, 17);
            this.chkRunning.TabIndex = 0;
            this.chkRunning.Text = "Running";
            this.chkRunning.UseVisualStyleBackColor = true;
            this.chkRunning.CheckedChanged += new System.EventHandler(this.chkRunning_CheckedChanged);
            // 
            // trkElapsedTime
            // 
            this.trkElapsedTime.Location = new System.Drawing.Point(6, 28);
            this.trkElapsedTime.Maximum = 1000;
            this.trkElapsedTime.Name = "trkElapsedTime";
            this.trkElapsedTime.Size = new System.Drawing.Size(242, 29);
            this.trkElapsedTime.TabIndex = 6;
            this.trkElapsedTime.TickFrequency = 100;
            this.trkElapsedTime.Value = 99;
            this.trkElapsedTime.Scroll += new System.EventHandler(this.trkElapsedTime_Scroll);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Elapsed Time Per Tick";
            // 
            // txtElapsedTime
            // 
            this.txtElapsedTime.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtElapsedTime.Location = new System.Drawing.Point(170, 9);
            this.txtElapsedTime.Name = "txtElapsedTime";
            this.txtElapsedTime.ReadOnly = true;
            this.txtElapsedTime.Size = new System.Drawing.Size(78, 20);
            this.txtElapsedTime.TabIndex = 10;
            this.txtElapsedTime.TabStop = false;
            // 
            // timer1
            // 
            this.timer1.Interval = 10;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // txtThreshold
            // 
            this.txtThreshold.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtThreshold.Location = new System.Drawing.Point(170, 9);
            this.txtThreshold.Name = "txtThreshold";
            this.txtThreshold.ReadOnly = true;
            this.txtThreshold.Size = new System.Drawing.Size(78, 20);
            this.txtThreshold.TabIndex = 13;
            this.txtThreshold.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(154, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Threshold % before penetrating";
            // 
            // trkThreshold
            // 
            this.trkThreshold.Location = new System.Drawing.Point(6, 28);
            this.trkThreshold.Maximum = 1000;
            this.trkThreshold.Name = "trkThreshold";
            this.trkThreshold.Size = new System.Drawing.Size(242, 29);
            this.trkThreshold.TabIndex = 7;
            this.trkThreshold.TickFrequency = 100;
            this.trkThreshold.Value = 19;
            this.trkThreshold.Scroll += new System.EventHandler(this.trkThreshold_Scroll);
            // 
            // btnZoomFit
            // 
            this.btnZoomFit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnZoomFit.Location = new System.Drawing.Point(209, 578);
            this.btnZoomFit.Name = "btnZoomFit";
            this.btnZoomFit.Size = new System.Drawing.Size(57, 23);
            this.btnZoomFit.TabIndex = 15;
            this.btnZoomFit.Text = "Zoom Fit";
            this.btnZoomFit.UseVisualStyleBackColor = true;
            this.btnZoomFit.Click += new System.EventHandler(this.btnZoomFit_Click);
            // 
            // radGravityNone
            // 
            this.radGravityNone.AutoSize = true;
            this.radGravityNone.Checked = true;
            this.radGravityNone.Location = new System.Drawing.Point(6, 19);
            this.radGravityNone.Name = "radGravityNone";
            this.radGravityNone.Size = new System.Drawing.Size(75, 17);
            this.radGravityNone.TabIndex = 10;
            this.radGravityNone.TabStop = true;
            this.radGravityNone.Text = "No Gravity";
            this.radGravityNone.UseVisualStyleBackColor = true;
            this.radGravityNone.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // radGravityDown
            // 
            this.radGravityDown.AutoSize = true;
            this.radGravityDown.Location = new System.Drawing.Point(6, 42);
            this.radGravityDown.Name = "radGravityDown";
            this.radGravityDown.Size = new System.Drawing.Size(53, 17);
            this.radGravityDown.TabIndex = 11;
            this.radGravityDown.Text = "Down";
            this.radGravityDown.UseVisualStyleBackColor = true;
            this.radGravityDown.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // radGravityBalls
            // 
            this.radGravityBalls.AutoSize = true;
            this.radGravityBalls.Location = new System.Drawing.Point(6, 65);
            this.radGravityBalls.Name = "radGravityBalls";
            this.radGravityBalls.Size = new System.Drawing.Size(47, 17);
            this.radGravityBalls.TabIndex = 12;
            this.radGravityBalls.Text = "Balls";
            this.radGravityBalls.UseVisualStyleBackColor = true;
            this.radGravityBalls.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // btnZeroVelocity
            // 
            this.btnZeroVelocity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnZeroVelocity.Location = new System.Drawing.Point(12, 458);
            this.btnZeroVelocity.Name = "btnZeroVelocity";
            this.btnZeroVelocity.Size = new System.Drawing.Size(72, 35);
            this.btnZeroVelocity.TabIndex = 4;
            this.btnZeroVelocity.Text = "Zero Velocities";
            this.btnZeroVelocity.UseVisualStyleBackColor = true;
            this.btnZeroVelocity.Click += new System.EventHandler(this.btnZeroVelocity_Click);
            // 
            // btnRandomVelocity
            // 
            this.btnRandomVelocity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRandomVelocity.Location = new System.Drawing.Point(90, 458);
            this.btnRandomVelocity.Name = "btnRandomVelocity";
            this.btnRandomVelocity.Size = new System.Drawing.Size(72, 35);
            this.btnRandomVelocity.TabIndex = 5;
            this.btnRandomVelocity.Text = "Random Velocities";
            this.btnRandomVelocity.UseVisualStyleBackColor = true;
            this.btnRandomVelocity.Click += new System.EventHandler(this.btnRandomVelocity_Click);
            // 
            // chkDrawCollisionsRed
            // 
            this.chkDrawCollisionsRed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkDrawCollisionsRed.AutoSize = true;
            this.chkDrawCollisionsRed.Location = new System.Drawing.Point(12, 586);
            this.chkDrawCollisionsRed.Name = "chkDrawCollisionsRed";
            this.chkDrawCollisionsRed.Size = new System.Drawing.Size(120, 17);
            this.chkDrawCollisionsRed.TabIndex = 14;
            this.chkDrawCollisionsRed.Text = "Draw Collisions Red";
            this.chkDrawCollisionsRed.UseVisualStyleBackColor = true;
            this.chkDrawCollisionsRed.CheckedChanged += new System.EventHandler(this.chkDrawCollisionsRed_CheckedChanged);
            // 
            // trkYBoundry
            // 
            this.trkYBoundry.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.trkYBoundry.Location = new System.Drawing.Point(229, 482);
            this.trkYBoundry.Maximum = 5000;
            this.trkYBoundry.Minimum = 5;
            this.trkYBoundry.Name = "trkYBoundry";
            this.trkYBoundry.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trkYBoundry.Size = new System.Drawing.Size(29, 94);
            this.trkYBoundry.TabIndex = 13;
            this.trkYBoundry.TickFrequency = 1000;
            this.trkYBoundry.Value = 5000;
            this.trkYBoundry.Scroll += new System.EventHandler(this.trkYBoundry_Scroll);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.Location = new System.Drawing.Point(215, 452);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 35);
            this.label3.TabIndex = 22;
            this.label3.Text = "Y Boundry";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtElasticity
            // 
            this.txtElasticity.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtElasticity.Location = new System.Drawing.Point(170, 57);
            this.txtElasticity.Name = "txtElasticity";
            this.txtElasticity.ReadOnly = true;
            this.txtElasticity.Size = new System.Drawing.Size(78, 20);
            this.txtElasticity.TabIndex = 25;
            this.txtElasticity.TabStop = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 60);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 13);
            this.label4.TabIndex = 24;
            this.label4.Text = "Elasticity";
            // 
            // trkElasticity
            // 
            this.trkElasticity.Location = new System.Drawing.Point(6, 76);
            this.trkElasticity.Maximum = 100;
            this.trkElasticity.Name = "trkElasticity";
            this.trkElasticity.Size = new System.Drawing.Size(242, 29);
            this.trkElasticity.TabIndex = 9;
            this.trkElasticity.TickFrequency = 25;
            this.trkElasticity.Value = 75;
            this.trkElasticity.Scroll += new System.EventHandler(this.trkElasticity_Scroll);
            // 
            // txtPullApartPercent
            // 
            this.txtPullApartPercent.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtPullApartPercent.Location = new System.Drawing.Point(168, 64);
            this.txtPullApartPercent.Name = "txtPullApartPercent";
            this.txtPullApartPercent.ReadOnly = true;
            this.txtPullApartPercent.Size = new System.Drawing.Size(78, 20);
            this.txtPullApartPercent.TabIndex = 28;
            this.txtPullApartPercent.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(30, 67);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 13);
            this.label5.TabIndex = 27;
            this.label5.Text = "Pull Apart %";
            // 
            // trkPullApartPercent
            // 
            this.trkPullApartPercent.Location = new System.Drawing.Point(28, 83);
            this.trkPullApartPercent.Maximum = 1000;
            this.trkPullApartPercent.Name = "trkPullApartPercent";
            this.trkPullApartPercent.Size = new System.Drawing.Size(220, 29);
            this.trkPullApartPercent.TabIndex = 8;
            this.trkPullApartPercent.TickFrequency = 250;
            this.trkPullApartPercent.Value = 140;
            this.trkPullApartPercent.Scroll += new System.EventHandler(this.trkPullApartPercent_Scroll);
            // 
            // chkSmallObjectsAreMassive
            // 
            this.chkSmallObjectsAreMassive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkSmallObjectsAreMassive.Location = new System.Drawing.Point(12, 546);
            this.chkSmallObjectsAreMassive.Name = "chkSmallObjectsAreMassive";
            this.chkSmallObjectsAreMassive.Size = new System.Drawing.Size(120, 34);
            this.chkSmallObjectsAreMassive.TabIndex = 29;
            this.chkSmallObjectsAreMassive.Text = "Smaller Objects are More Massive";
            this.chkSmallObjectsAreMassive.UseVisualStyleBackColor = true;
            this.chkSmallObjectsAreMassive.CheckedChanged += new System.EventHandler(this.chkSmallObjectsAreMassive_CheckedChanged);
            // 
            // btnAddSolidBall
            // 
            this.btnAddSolidBall.Location = new System.Drawing.Point(78, 35);
            this.btnAddSolidBall.Name = "btnAddSolidBall";
            this.btnAddSolidBall.Size = new System.Drawing.Size(85, 23);
            this.btnAddSolidBall.TabIndex = 30;
            this.btnAddSolidBall.Text = "Add SolidBall";
            this.btnAddSolidBall.UseVisualStyleBackColor = true;
            this.btnAddSolidBall.Click += new System.EventHandler(this.btnAddSolidBall_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.trkGravityForce);
            this.groupBox1.Controls.Add(this.radGravityNone);
            this.groupBox1.Controls.Add(this.radGravityDown);
            this.groupBox1.Controls.Add(this.radGravityBalls);
            this.groupBox1.Location = new System.Drawing.Point(9, 334);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(97, 118);
            this.groupBox1.TabIndex = 31;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Gravity";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(31, 102);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(34, 13);
            this.label9.TabIndex = 31;
            this.label9.Text = "Force";
            // 
            // trkGravityForce
            // 
            this.trkGravityForce.Location = new System.Drawing.Point(3, 85);
            this.trkGravityForce.Maximum = 1000;
            this.trkGravityForce.Name = "trkGravityForce";
            this.trkGravityForce.Size = new System.Drawing.Size(90, 29);
            this.trkGravityForce.TabIndex = 30;
            this.trkGravityForce.TickFrequency = 25;
            this.trkGravityForce.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkGravityForce.Value = 20;
            this.trkGravityForce.Scroll += new System.EventHandler(this.trkGravityForce_Scroll);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.trkVectorFieldForce);
            this.groupBox2.Controls.Add(this.cboVectorField);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.trkVectorFieldSize);
            this.groupBox2.Location = new System.Drawing.Point(112, 334);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(151, 118);
            this.groupBox2.TabIndex = 32;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Mouse Vector Field";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(56, 60);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(34, 13);
            this.label7.TabIndex = 29;
            this.label7.Text = "Force";
            // 
            // trkVectorFieldForce
            // 
            this.trkVectorFieldForce.Location = new System.Drawing.Point(4, 46);
            this.trkVectorFieldForce.Maximum = 1000;
            this.trkVectorFieldForce.Name = "trkVectorFieldForce";
            this.trkVectorFieldForce.Size = new System.Drawing.Size(141, 29);
            this.trkVectorFieldForce.TabIndex = 28;
            this.trkVectorFieldForce.TickFrequency = 25;
            this.trkVectorFieldForce.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkVectorFieldForce.Value = 333;
            this.trkVectorFieldForce.Scroll += new System.EventHandler(this.trkVectorFieldForce_Scroll);
            // 
            // cboVectorField
            // 
            this.cboVectorField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboVectorField.FormattingEnabled = true;
            this.cboVectorField.Location = new System.Drawing.Point(6, 19);
            this.cboVectorField.Name = "cboVectorField";
            this.cboVectorField.Size = new System.Drawing.Size(139, 21);
            this.cboVectorField.TabIndex = 27;
            this.cboVectorField.SelectedIndexChanged += new System.EventHandler(this.cboVectorField_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(60, 95);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(27, 13);
            this.label6.TabIndex = 26;
            this.label6.Text = "Size";
            // 
            // trkVectorFieldSize
            // 
            this.trkVectorFieldSize.Location = new System.Drawing.Point(4, 81);
            this.trkVectorFieldSize.Maximum = 1000;
            this.trkVectorFieldSize.Name = "trkVectorFieldSize";
            this.trkVectorFieldSize.Size = new System.Drawing.Size(141, 29);
            this.trkVectorFieldSize.TabIndex = 25;
            this.trkVectorFieldSize.TickFrequency = 25;
            this.trkVectorFieldSize.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkVectorFieldSize.Value = 250;
            this.trkVectorFieldSize.Scroll += new System.EventHandler(this.trkVectorFieldSize_Scroll);
            // 
            // btnAddRigidBody
            // 
            this.btnAddRigidBody.Location = new System.Drawing.Point(169, 35);
            this.btnAddRigidBody.Name = "btnAddRigidBody";
            this.btnAddRigidBody.Size = new System.Drawing.Size(89, 23);
            this.btnAddRigidBody.TabIndex = 33;
            this.btnAddRigidBody.Text = "Add RigidBody";
            this.btnAddRigidBody.UseVisualStyleBackColor = true;
            this.btnAddRigidBody.Click += new System.EventHandler(this.btnAddRigidBody_Click);
            // 
            // chkIncludeShip
            // 
            this.chkIncludeShip.AutoSize = true;
            this.chkIncludeShip.Location = new System.Drawing.Point(181, 12);
            this.chkIncludeShip.Name = "chkIncludeShip";
            this.chkIncludeShip.Size = new System.Drawing.Size(85, 17);
            this.chkIncludeShip.TabIndex = 34;
            this.chkIncludeShip.Text = "Include Ship";
            this.chkIncludeShip.UseVisualStyleBackColor = true;
            this.chkIncludeShip.CheckedChanged += new System.EventHandler(this.chkIncludeShip_CheckedChanged);
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label8.Location = new System.Drawing.Point(166, 452);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(51, 35);
            this.label8.TabIndex = 36;
            this.label8.Text = "Boundry Scale";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // trkBoundryScale
            // 
            this.trkBoundryScale.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.trkBoundryScale.Location = new System.Drawing.Point(180, 482);
            this.trkBoundryScale.Maximum = 25;
            this.trkBoundryScale.Minimum = 1;
            this.trkBoundryScale.Name = "trkBoundryScale";
            this.trkBoundryScale.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trkBoundryScale.Size = new System.Drawing.Size(29, 94);
            this.trkBoundryScale.TabIndex = 35;
            this.trkBoundryScale.TickFrequency = 5;
            this.trkBoundryScale.Value = 1;
            this.trkBoundryScale.Scroll += new System.EventHandler(this.trkBoundryScale_Scroll);
            // 
            // btnChaseShip
            // 
            this.btnChaseShip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnChaseShip.Location = new System.Drawing.Point(148, 578);
            this.btnChaseShip.Name = "btnChaseShip";
            this.btnChaseShip.Size = new System.Drawing.Size(57, 23);
            this.btnChaseShip.TabIndex = 37;
            this.btnChaseShip.Text = "Chase";
            this.btnChaseShip.UseVisualStyleBackColor = true;
            this.btnChaseShip.Click += new System.EventHandler(this.btnChaseShip_Click);
            // 
            // txtPullApartSpring
            // 
            this.txtPullApartSpring.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtPullApartSpring.Location = new System.Drawing.Point(168, 112);
            this.txtPullApartSpring.Name = "txtPullApartSpring";
            this.txtPullApartSpring.ReadOnly = true;
            this.txtPullApartSpring.Size = new System.Drawing.Size(78, 20);
            this.txtPullApartSpring.TabIndex = 40;
            this.txtPullApartSpring.TabStop = false;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(30, 115);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(108, 13);
            this.label10.TabIndex = 39;
            this.label10.Text = "Pull Apart Spring (vel)";
            // 
            // trkPullApartSpring
            // 
            this.trkPullApartSpring.Location = new System.Drawing.Point(28, 131);
            this.trkPullApartSpring.Maximum = 1000;
            this.trkPullApartSpring.Name = "trkPullApartSpring";
            this.trkPullApartSpring.Size = new System.Drawing.Size(220, 29);
            this.trkPullApartSpring.TabIndex = 38;
            this.trkPullApartSpring.TickFrequency = 250;
            this.trkPullApartSpring.Value = 200;
            this.trkPullApartSpring.Scroll += new System.EventHandler(this.trkPullApartSpring_Scroll);
            // 
            // radPullApartInstant
            // 
            this.radPullApartInstant.AutoSize = true;
            this.radPullApartInstant.Location = new System.Drawing.Point(5, 16);
            this.radPullApartInstant.Name = "radPullApartInstant";
            this.radPullApartInstant.Size = new System.Drawing.Size(14, 13);
            this.radPullApartInstant.TabIndex = 41;
            this.radPullApartInstant.UseVisualStyleBackColor = true;
            this.radPullApartInstant.CheckedChanged += new System.EventHandler(this.radPullApart_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radPullApartNone);
            this.panel1.Controls.Add(this.radPullApartSpring);
            this.panel1.Controls.Add(this.radPullApartInstant);
            this.panel1.Location = new System.Drawing.Point(3, 70);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(25, 90);
            this.panel1.TabIndex = 42;
            // 
            // radPullApartNone
            // 
            this.radPullApartNone.AutoSize = true;
            this.radPullApartNone.Location = new System.Drawing.Point(5, 38);
            this.radPullApartNone.Name = "radPullApartNone";
            this.radPullApartNone.Size = new System.Drawing.Size(14, 13);
            this.radPullApartNone.TabIndex = 43;
            this.radPullApartNone.UseVisualStyleBackColor = true;
            this.radPullApartNone.CheckedChanged += new System.EventHandler(this.radPullApart_CheckedChanged);
            // 
            // radPullApartSpring
            // 
            this.radPullApartSpring.AutoSize = true;
            this.radPullApartSpring.Checked = true;
            this.radPullApartSpring.Location = new System.Drawing.Point(5, 61);
            this.radPullApartSpring.Name = "radPullApartSpring";
            this.radPullApartSpring.Size = new System.Drawing.Size(14, 13);
            this.radPullApartSpring.TabIndex = 42;
            this.radPullApartSpring.TabStop = true;
            this.radPullApartSpring.UseVisualStyleBackColor = true;
            this.radPullApartSpring.CheckedChanged += new System.EventHandler(this.radPullApart_CheckedChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(3, 93);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(263, 231);
            this.tabControl1.TabIndex = 43;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.label12);
            this.tabPage1.Controls.Add(this.label11);
            this.tabPage1.Controls.Add(this.txtStaticFriction);
            this.tabPage1.Controls.Add(this.txtKineticFriction);
            this.tabPage1.Controls.Add(this.trkStaticFriction);
            this.tabPage1.Controls.Add(this.trkKineticFriction);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.txtElapsedTime);
            this.tabPage1.Controls.Add(this.txtElasticity);
            this.tabPage1.Controls.Add(this.trkElapsedTime);
            this.tabPage1.Controls.Add(this.trkElasticity);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(255, 205);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Misc";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 156);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(71, 13);
            this.label12.TabIndex = 30;
            this.label12.Text = "Static Friction";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 108);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(76, 13);
            this.label11.TabIndex = 27;
            this.label11.Text = "Kinetic Friction";
            // 
            // txtStaticFriction
            // 
            this.txtStaticFriction.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtStaticFriction.Location = new System.Drawing.Point(170, 153);
            this.txtStaticFriction.Name = "txtStaticFriction";
            this.txtStaticFriction.ReadOnly = true;
            this.txtStaticFriction.Size = new System.Drawing.Size(78, 20);
            this.txtStaticFriction.TabIndex = 31;
            this.txtStaticFriction.TabStop = false;
            // 
            // txtKineticFriction
            // 
            this.txtKineticFriction.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtKineticFriction.Location = new System.Drawing.Point(170, 105);
            this.txtKineticFriction.Name = "txtKineticFriction";
            this.txtKineticFriction.ReadOnly = true;
            this.txtKineticFriction.Size = new System.Drawing.Size(78, 20);
            this.txtKineticFriction.TabIndex = 28;
            this.txtKineticFriction.TabStop = false;
            // 
            // trkStaticFriction
            // 
            this.trkStaticFriction.Enabled = false;
            this.trkStaticFriction.Location = new System.Drawing.Point(6, 172);
            this.trkStaticFriction.Maximum = 1000;
            this.trkStaticFriction.Name = "trkStaticFriction";
            this.trkStaticFriction.Size = new System.Drawing.Size(242, 29);
            this.trkStaticFriction.TabIndex = 29;
            this.trkStaticFriction.TickFrequency = 250;
            this.trkStaticFriction.Value = 1000;
            // 
            // trkKineticFriction
            // 
            this.trkKineticFriction.Enabled = false;
            this.trkKineticFriction.Location = new System.Drawing.Point(6, 124);
            this.trkKineticFriction.Maximum = 1000;
            this.trkKineticFriction.Name = "trkKineticFriction";
            this.trkKineticFriction.Size = new System.Drawing.Size(242, 29);
            this.trkKineticFriction.TabIndex = 26;
            this.trkKineticFriction.TickFrequency = 250;
            this.trkKineticFriction.Value = 1000;
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage2.Controls.Add(this.chkDoStandardCollisions);
            this.tabPage2.Controls.Add(this.label10);
            this.tabPage2.Controls.Add(this.txtPullApartSpring);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.txtPullApartPercent);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.txtThreshold);
            this.tabPage2.Controls.Add(this.panel1);
            this.tabPage2.Controls.Add(this.trkThreshold);
            this.tabPage2.Controls.Add(this.trkPullApartPercent);
            this.tabPage2.Controls.Add(this.trkPullApartSpring);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(255, 205);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Map";
            // 
            // chkDoStandardCollisions
            // 
            this.chkDoStandardCollisions.AutoSize = true;
            this.chkDoStandardCollisions.Checked = true;
            this.chkDoStandardCollisions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDoStandardCollisions.Location = new System.Drawing.Point(3, 166);
            this.chkDoStandardCollisions.Name = "chkDoStandardCollisions";
            this.chkDoStandardCollisions.Size = new System.Drawing.Size(132, 17);
            this.chkDoStandardCollisions.TabIndex = 43;
            this.chkDoStandardCollisions.Text = "Do Standard Collisions";
            this.chkDoStandardCollisions.UseVisualStyleBackColor = true;
            this.chkDoStandardCollisions.CheckedChanged += new System.EventHandler(this.chkDoStandardCollisions_CheckedChanged);
            // 
            // MapTester1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(864, 615);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnChaseShip);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.trkBoundryScale);
            this.Controls.Add(this.chkIncludeShip);
            this.Controls.Add(this.btnAddRigidBody);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnAddSolidBall);
            this.Controls.Add(this.chkSmallObjectsAreMassive);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.trkYBoundry);
            this.Controls.Add(this.chkDrawCollisionsRed);
            this.Controls.Add(this.btnRandomVelocity);
            this.Controls.Add(this.btnZeroVelocity);
            this.Controls.Add(this.btnZoomFit);
            this.Controls.Add(this.chkRunning);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.pictureBox1);
            this.KeyPreview = true;
            this.Name = "MapTester1";
            this.Text = "Map Tester";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkElapsedTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkYBoundry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkElasticity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkPullApartPercent)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkGravityForce)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkVectorFieldForce)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkVectorFieldSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkBoundryScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkPullApartSpring)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkStaticFriction)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkKineticFriction)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private Game.Orig.HelperClassesGDI.LargeMapViewer2D pictureBox1;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnRemove;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.CheckBox chkRunning;
		private System.Windows.Forms.TrackBar trkElapsedTime;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtElapsedTime;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.TextBox txtThreshold;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TrackBar trkThreshold;
		private System.Windows.Forms.Button btnZoomFit;
		private System.Windows.Forms.RadioButton radGravityNone;
		private System.Windows.Forms.RadioButton radGravityDown;
		private System.Windows.Forms.RadioButton radGravityBalls;
		private System.Windows.Forms.Button btnZeroVelocity;
		private System.Windows.Forms.Button btnRandomVelocity;
		private System.Windows.Forms.CheckBox chkDrawCollisionsRed;
		private System.Windows.Forms.TrackBar trkYBoundry;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtElasticity;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TrackBar trkElasticity;
		private System.Windows.Forms.TextBox txtPullApartPercent;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TrackBar trkPullApartPercent;
		private System.Windows.Forms.CheckBox chkSmallObjectsAreMassive;
		private System.Windows.Forms.Button btnAddSolidBall;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TrackBar trkVectorFieldSize;
		private System.Windows.Forms.Button btnAddRigidBody;
		private System.Windows.Forms.ComboBox cboVectorField;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TrackBar trkVectorFieldForce;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.CheckBox chkIncludeShip;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TrackBar trkBoundryScale;
		private System.Windows.Forms.Button btnChaseShip;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TrackBar trkGravityForce;
		private System.Windows.Forms.TextBox txtPullApartSpring;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.TrackBar trkPullApartSpring;
		private System.Windows.Forms.RadioButton radPullApartInstant;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.RadioButton radPullApartSpring;
		private System.Windows.Forms.RadioButton radPullApartNone;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.CheckBox chkDoStandardCollisions;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.TextBox txtStaticFriction;
		private System.Windows.Forms.TextBox txtKineticFriction;
		private System.Windows.Forms.TrackBar trkStaticFriction;
        private System.Windows.Forms.TrackBar trkKineticFriction;
	}
}