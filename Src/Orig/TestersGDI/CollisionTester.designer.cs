namespace Game.Orig.TestersGDI
{
	partial class CollisionTester
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
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.radBallBall = new System.Windows.Forms.RadioButton();
            this.radSolidBallSolidBall = new System.Windows.Forms.RadioButton();
            this.btnZoomFit = new System.Windows.Forms.Button();
            this.btnZoomOut = new System.Windows.Forms.Button();
            this.btnZoomIn = new System.Windows.Forms.Button();
            this.radLineTriangle = new System.Windows.Forms.RadioButton();
            this.radSphereTriangle = new System.Windows.Forms.RadioButton();
            this.radTriangleTriangle = new System.Windows.Forms.RadioButton();
            this.btnPanDown = new System.Windows.Forms.Button();
            this.btnPanUp = new System.Windows.Forms.Button();
            this.btnPanRight = new System.Windows.Forms.Button();
            this.btnPanLeft = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.panel7 = new System.Windows.Forms.Panel();
            this.radSphereSphere = new System.Windows.Forms.RadioButton();
            this.radPolygonPolygon = new System.Windows.Forms.RadioButton();
            this.radSpherePolygon = new System.Windows.Forms.RadioButton();
            this.grpTriangleZ = new System.Windows.Forms.GroupBox();
            this.chkLargeZ = new System.Windows.Forms.CheckBox();
            this.chkTrianglePerpendicular = new System.Windows.Forms.CheckBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.radPoint3Pos = new System.Windows.Forms.RadioButton();
            this.radPoint3Neg = new System.Windows.Forms.RadioButton();
            this.radPoint3Zero = new System.Windows.Forms.RadioButton();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.radPoint2Pos = new System.Windows.Forms.RadioButton();
            this.radPoint2Neg = new System.Windows.Forms.RadioButton();
            this.radPoint2Zero = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.radPoint1Pos = new System.Windows.Forms.RadioButton();
            this.radPoint1Neg = new System.Windows.Forms.RadioButton();
            this.radPoint1Zero = new System.Windows.Forms.RadioButton();
            this.grpTriangleZ2 = new System.Windows.Forms.GroupBox();
            this.chkLargeZ2 = new System.Windows.Forms.CheckBox();
            this.chkTrianglePerpendicular2 = new System.Windows.Forms.CheckBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.radPoint3Pos2 = new System.Windows.Forms.RadioButton();
            this.radPoint3Neg2 = new System.Windows.Forms.RadioButton();
            this.radPoint3Zero2 = new System.Windows.Forms.RadioButton();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.radPoint2Pos2 = new System.Windows.Forms.RadioButton();
            this.radPoint2Neg2 = new System.Windows.Forms.RadioButton();
            this.radPoint2Zero2 = new System.Windows.Forms.RadioButton();
            this.panel6 = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.radPoint1Pos2 = new System.Windows.Forms.RadioButton();
            this.radPoint1Neg2 = new System.Windows.Forms.RadioButton();
            this.radPoint1Zero2 = new System.Windows.Forms.RadioButton();
            this.pictureBox1 = new Game.Orig.HelperClassesGDI.LargeMapViewer2D();
            this.groupBox1.SuspendLayout();
            this.grpTriangleZ.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.grpTriangleZ2.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Interval = 10;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // radBallBall
            // 
            this.radBallBall.AutoSize = true;
            this.radBallBall.Checked = true;
            this.radBallBall.Location = new System.Drawing.Point(36, 22);
            this.radBallBall.Name = "radBallBall";
            this.radBallBall.Size = new System.Drawing.Size(68, 17);
            this.radBallBall.TabIndex = 5;
            this.radBallBall.TabStop = true;
            this.radBallBall.Text = "Ball - Ball";
            this.radBallBall.UseVisualStyleBackColor = true;
            this.radBallBall.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // radSolidBallSolidBall
            // 
            this.radSolidBallSolidBall.AutoSize = true;
            this.radSolidBallSolidBall.Location = new System.Drawing.Point(152, 22);
            this.radSolidBallSolidBall.Name = "radSolidBallSolidBall";
            this.radSolidBallSolidBall.Size = new System.Drawing.Size(114, 17);
            this.radSolidBallSolidBall.TabIndex = 6;
            this.radSolidBallSolidBall.Text = "SolidBall - SolidBall";
            this.radSolidBallSolidBall.UseVisualStyleBackColor = true;
            this.radSolidBallSolidBall.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // btnZoomFit
            // 
            this.btnZoomFit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnZoomFit.Location = new System.Drawing.Point(263, 585);
            this.btnZoomFit.Name = "btnZoomFit";
            this.btnZoomFit.Size = new System.Drawing.Size(75, 23);
            this.btnZoomFit.TabIndex = 7;
            this.btnZoomFit.Text = "Zoom Orig";
            this.btnZoomFit.UseVisualStyleBackColor = true;
            this.btnZoomFit.Click += new System.EventHandler(this.btnZoomFit_Click);
            // 
            // btnZoomOut
            // 
            this.btnZoomOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnZoomOut.Location = new System.Drawing.Point(263, 556);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(75, 23);
            this.btnZoomOut.TabIndex = 8;
            this.btnZoomOut.Text = "Zoom Out";
            this.btnZoomOut.UseVisualStyleBackColor = true;
            this.btnZoomOut.Click += new System.EventHandler(this.btnZoomOut_Click);
            // 
            // btnZoomIn
            // 
            this.btnZoomIn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnZoomIn.Location = new System.Drawing.Point(182, 556);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(75, 23);
            this.btnZoomIn.TabIndex = 9;
            this.btnZoomIn.Text = "Zoom In";
            this.btnZoomIn.UseVisualStyleBackColor = true;
            this.btnZoomIn.Click += new System.EventHandler(this.btnZoomIn_Click);
            // 
            // radLineTriangle
            // 
            this.radLineTriangle.AutoSize = true;
            this.radLineTriangle.Location = new System.Drawing.Point(36, 65);
            this.radLineTriangle.Name = "radLineTriangle";
            this.radLineTriangle.Size = new System.Drawing.Size(92, 17);
            this.radLineTriangle.TabIndex = 10;
            this.radLineTriangle.Text = "Line - Triangle";
            this.radLineTriangle.UseVisualStyleBackColor = true;
            this.radLineTriangle.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // radSphereTriangle
            // 
            this.radSphereTriangle.AutoSize = true;
            this.radSphereTriangle.Location = new System.Drawing.Point(152, 65);
            this.radSphereTriangle.Name = "radSphereTriangle";
            this.radSphereTriangle.Size = new System.Drawing.Size(106, 17);
            this.radSphereTriangle.TabIndex = 11;
            this.radSphereTriangle.Text = "Sphere - Triangle";
            this.radSphereTriangle.UseVisualStyleBackColor = true;
            this.radSphereTriangle.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // radTriangleTriangle
            // 
            this.radTriangleTriangle.AutoSize = true;
            this.radTriangleTriangle.Location = new System.Drawing.Point(36, 88);
            this.radTriangleTriangle.Name = "radTriangleTriangle";
            this.radTriangleTriangle.Size = new System.Drawing.Size(110, 17);
            this.radTriangleTriangle.TabIndex = 12;
            this.radTriangleTriangle.Text = "Triangle - Triangle";
            this.radTriangleTriangle.UseVisualStyleBackColor = true;
            this.radTriangleTriangle.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // btnPanDown
            // 
            this.btnPanDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPanDown.Location = new System.Drawing.Point(93, 516);
            this.btnPanDown.Name = "btnPanDown";
            this.btnPanDown.Size = new System.Drawing.Size(75, 23);
            this.btnPanDown.TabIndex = 13;
            this.btnPanDown.Text = "Pan Down";
            this.btnPanDown.UseVisualStyleBackColor = true;
            this.btnPanDown.Click += new System.EventHandler(this.btnPanDown_Click);
            // 
            // btnPanUp
            // 
            this.btnPanUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPanUp.Location = new System.Drawing.Point(93, 487);
            this.btnPanUp.Name = "btnPanUp";
            this.btnPanUp.Size = new System.Drawing.Size(75, 23);
            this.btnPanUp.TabIndex = 14;
            this.btnPanUp.Text = "Pan Up";
            this.btnPanUp.UseVisualStyleBackColor = true;
            this.btnPanUp.Click += new System.EventHandler(this.btnPanUp_Click);
            // 
            // btnPanRight
            // 
            this.btnPanRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPanRight.Location = new System.Drawing.Point(174, 502);
            this.btnPanRight.Name = "btnPanRight";
            this.btnPanRight.Size = new System.Drawing.Size(75, 23);
            this.btnPanRight.TabIndex = 15;
            this.btnPanRight.Text = "Pan Right";
            this.btnPanRight.UseVisualStyleBackColor = true;
            this.btnPanRight.Click += new System.EventHandler(this.btnPanRight_Click);
            // 
            // btnPanLeft
            // 
            this.btnPanLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPanLeft.Location = new System.Drawing.Point(12, 502);
            this.btnPanLeft.Name = "btnPanLeft";
            this.btnPanLeft.Size = new System.Drawing.Size(75, 23);
            this.btnPanLeft.TabIndex = 16;
            this.btnPanLeft.Text = "Pan Left";
            this.btnPanLeft.UseVisualStyleBackColor = true;
            this.btnPanLeft.Click += new System.EventHandler(this.btnPanLeft_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.panel7);
            this.groupBox1.Controls.Add(this.radSphereSphere);
            this.groupBox1.Controls.Add(this.radPolygonPolygon);
            this.groupBox1.Controls.Add(this.radSpherePolygon);
            this.groupBox1.Controls.Add(this.radBallBall);
            this.groupBox1.Controls.Add(this.radSolidBallSolidBall);
            this.groupBox1.Controls.Add(this.radLineTriangle);
            this.groupBox1.Controls.Add(this.radSphereTriangle);
            this.groupBox1.Controls.Add(this.radTriangleTriangle);
            this.groupBox1.Location = new System.Drawing.Point(2, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(330, 182);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Collision Objects";
            // 
            // panel7
            // 
            this.panel7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel7.Location = new System.Drawing.Point(6, 49);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(314, 1);
            this.panel7.TabIndex = 16;
            // 
            // radSphereSphere
            // 
            this.radSphereSphere.AutoSize = true;
            this.radSphereSphere.Location = new System.Drawing.Point(36, 123);
            this.radSphereSphere.Name = "radSphereSphere";
            this.radSphereSphere.Size = new System.Drawing.Size(102, 17);
            this.radSphereSphere.TabIndex = 15;
            this.radSphereSphere.Text = "Sphere - Sphere";
            this.radSphereSphere.UseVisualStyleBackColor = true;
            this.radSphereSphere.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // radPolygonPolygon
            // 
            this.radPolygonPolygon.AutoSize = true;
            this.radPolygonPolygon.Location = new System.Drawing.Point(152, 146);
            this.radPolygonPolygon.Name = "radPolygonPolygon";
            this.radPolygonPolygon.Size = new System.Drawing.Size(110, 17);
            this.radPolygonPolygon.TabIndex = 14;
            this.radPolygonPolygon.Text = "Polygon - Polygon";
            this.radPolygonPolygon.UseVisualStyleBackColor = true;
            this.radPolygonPolygon.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // radSpherePolygon
            // 
            this.radSpherePolygon.AutoSize = true;
            this.radSpherePolygon.Location = new System.Drawing.Point(36, 146);
            this.radSpherePolygon.Name = "radSpherePolygon";
            this.radSpherePolygon.Size = new System.Drawing.Size(106, 17);
            this.radSpherePolygon.TabIndex = 13;
            this.radSpherePolygon.Text = "Sphere - Polygon";
            this.radSpherePolygon.UseVisualStyleBackColor = true;
            this.radSpherePolygon.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // grpTriangleZ
            // 
            this.grpTriangleZ.Controls.Add(this.chkLargeZ);
            this.grpTriangleZ.Controls.Add(this.chkTrianglePerpendicular);
            this.grpTriangleZ.Controls.Add(this.panel3);
            this.grpTriangleZ.Controls.Add(this.panel2);
            this.grpTriangleZ.Controls.Add(this.panel1);
            this.grpTriangleZ.Location = new System.Drawing.Point(172, 200);
            this.grpTriangleZ.Name = "grpTriangleZ";
            this.grpTriangleZ.Size = new System.Drawing.Size(160, 148);
            this.grpTriangleZ.TabIndex = 18;
            this.grpTriangleZ.TabStop = false;
            this.grpTriangleZ.Text = "Triangle\'s Z";
            this.grpTriangleZ.Visible = false;
            // 
            // chkLargeZ
            // 
            this.chkLargeZ.AutoSize = true;
            this.chkLargeZ.Location = new System.Drawing.Point(91, 125);
            this.chkLargeZ.Name = "chkLargeZ";
            this.chkLargeZ.Size = new System.Drawing.Size(63, 17);
            this.chkLargeZ.TabIndex = 8;
            this.chkLargeZ.Text = "Large Z";
            this.chkLargeZ.UseVisualStyleBackColor = true;
            this.chkLargeZ.CheckedChanged += new System.EventHandler(this.chkLargeZ_CheckedChanged);
            // 
            // chkTrianglePerpendicular
            // 
            this.chkTrianglePerpendicular.AutoSize = true;
            this.chkTrianglePerpendicular.Location = new System.Drawing.Point(6, 19);
            this.chkTrianglePerpendicular.Name = "chkTrianglePerpendicular";
            this.chkTrianglePerpendicular.Size = new System.Drawing.Size(140, 17);
            this.chkTrianglePerpendicular.TabIndex = 7;
            this.chkTrianglePerpendicular.Text = "Perpendicular to Screen";
            this.chkTrianglePerpendicular.UseVisualStyleBackColor = true;
            this.chkTrianglePerpendicular.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.label3);
            this.panel3.Controls.Add(this.radPoint3Pos);
            this.panel3.Controls.Add(this.radPoint3Neg);
            this.panel3.Controls.Add(this.radPoint3Zero);
            this.panel3.Location = new System.Drawing.Point(6, 98);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(145, 19);
            this.panel3.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "3";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // radPoint3Pos
            // 
            this.radPoint3Pos.AutoSize = true;
            this.radPoint3Pos.Location = new System.Drawing.Point(106, 0);
            this.radPoint3Pos.Name = "radPoint3Pos";
            this.radPoint3Pos.Size = new System.Drawing.Size(37, 17);
            this.radPoint3Pos.TabIndex = 3;
            this.radPoint3Pos.Text = "10";
            this.radPoint3Pos.UseVisualStyleBackColor = true;
            this.radPoint3Pos.CheckedChanged += new System.EventHandler(this.radPoint3_CheckedChanged);
            // 
            // radPoint3Neg
            // 
            this.radPoint3Neg.AutoSize = true;
            this.radPoint3Neg.Location = new System.Drawing.Point(26, 0);
            this.radPoint3Neg.Name = "radPoint3Neg";
            this.radPoint3Neg.Size = new System.Drawing.Size(40, 17);
            this.radPoint3Neg.TabIndex = 1;
            this.radPoint3Neg.Text = "-10";
            this.radPoint3Neg.UseVisualStyleBackColor = true;
            this.radPoint3Neg.CheckedChanged += new System.EventHandler(this.radPoint3_CheckedChanged);
            // 
            // radPoint3Zero
            // 
            this.radPoint3Zero.AutoSize = true;
            this.radPoint3Zero.Checked = true;
            this.radPoint3Zero.Location = new System.Drawing.Point(69, 0);
            this.radPoint3Zero.Name = "radPoint3Zero";
            this.radPoint3Zero.Size = new System.Drawing.Size(31, 17);
            this.radPoint3Zero.TabIndex = 2;
            this.radPoint3Zero.TabStop = true;
            this.radPoint3Zero.Text = "0";
            this.radPoint3Zero.UseVisualStyleBackColor = true;
            this.radPoint3Zero.CheckedChanged += new System.EventHandler(this.radPoint3_CheckedChanged);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.radPoint2Pos);
            this.panel2.Controls.Add(this.radPoint2Neg);
            this.panel2.Controls.Add(this.radPoint2Zero);
            this.panel2.Location = new System.Drawing.Point(6, 75);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(145, 19);
            this.panel2.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "2";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // radPoint2Pos
            // 
            this.radPoint2Pos.AutoSize = true;
            this.radPoint2Pos.Location = new System.Drawing.Point(106, 0);
            this.radPoint2Pos.Name = "radPoint2Pos";
            this.radPoint2Pos.Size = new System.Drawing.Size(37, 17);
            this.radPoint2Pos.TabIndex = 3;
            this.radPoint2Pos.Text = "10";
            this.radPoint2Pos.UseVisualStyleBackColor = true;
            this.radPoint2Pos.CheckedChanged += new System.EventHandler(this.radPoint2_CheckedChanged);
            // 
            // radPoint2Neg
            // 
            this.radPoint2Neg.AutoSize = true;
            this.radPoint2Neg.Location = new System.Drawing.Point(26, 0);
            this.radPoint2Neg.Name = "radPoint2Neg";
            this.radPoint2Neg.Size = new System.Drawing.Size(40, 17);
            this.radPoint2Neg.TabIndex = 1;
            this.radPoint2Neg.Text = "-10";
            this.radPoint2Neg.UseVisualStyleBackColor = true;
            this.radPoint2Neg.CheckedChanged += new System.EventHandler(this.radPoint2_CheckedChanged);
            // 
            // radPoint2Zero
            // 
            this.radPoint2Zero.AutoSize = true;
            this.radPoint2Zero.Checked = true;
            this.radPoint2Zero.Location = new System.Drawing.Point(69, 0);
            this.radPoint2Zero.Name = "radPoint2Zero";
            this.radPoint2Zero.Size = new System.Drawing.Size(31, 17);
            this.radPoint2Zero.TabIndex = 2;
            this.radPoint2Zero.TabStop = true;
            this.radPoint2Zero.Text = "0";
            this.radPoint2Zero.UseVisualStyleBackColor = true;
            this.radPoint2Zero.CheckedChanged += new System.EventHandler(this.radPoint2_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.radPoint1Pos);
            this.panel1.Controls.Add(this.radPoint1Neg);
            this.panel1.Controls.Add(this.radPoint1Zero);
            this.panel1.Location = new System.Drawing.Point(6, 50);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(145, 19);
            this.panel1.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "1";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // radPoint1Pos
            // 
            this.radPoint1Pos.AutoSize = true;
            this.radPoint1Pos.Location = new System.Drawing.Point(106, 0);
            this.radPoint1Pos.Name = "radPoint1Pos";
            this.radPoint1Pos.Size = new System.Drawing.Size(37, 17);
            this.radPoint1Pos.TabIndex = 3;
            this.radPoint1Pos.Text = "10";
            this.radPoint1Pos.UseVisualStyleBackColor = true;
            this.radPoint1Pos.CheckedChanged += new System.EventHandler(this.radPoint1_CheckedChanged);
            // 
            // radPoint1Neg
            // 
            this.radPoint1Neg.AutoSize = true;
            this.radPoint1Neg.Location = new System.Drawing.Point(26, 0);
            this.radPoint1Neg.Name = "radPoint1Neg";
            this.radPoint1Neg.Size = new System.Drawing.Size(40, 17);
            this.radPoint1Neg.TabIndex = 1;
            this.radPoint1Neg.Text = "-10";
            this.radPoint1Neg.UseVisualStyleBackColor = true;
            this.radPoint1Neg.CheckedChanged += new System.EventHandler(this.radPoint1_CheckedChanged);
            // 
            // radPoint1Zero
            // 
            this.radPoint1Zero.AutoSize = true;
            this.radPoint1Zero.Checked = true;
            this.radPoint1Zero.Location = new System.Drawing.Point(69, 0);
            this.radPoint1Zero.Name = "radPoint1Zero";
            this.radPoint1Zero.Size = new System.Drawing.Size(31, 17);
            this.radPoint1Zero.TabIndex = 2;
            this.radPoint1Zero.TabStop = true;
            this.radPoint1Zero.Text = "0";
            this.radPoint1Zero.UseVisualStyleBackColor = true;
            this.radPoint1Zero.CheckedChanged += new System.EventHandler(this.radPoint1_CheckedChanged);
            // 
            // grpTriangleZ2
            // 
            this.grpTriangleZ2.Controls.Add(this.chkLargeZ2);
            this.grpTriangleZ2.Controls.Add(this.chkTrianglePerpendicular2);
            this.grpTriangleZ2.Controls.Add(this.panel4);
            this.grpTriangleZ2.Controls.Add(this.panel5);
            this.grpTriangleZ2.Controls.Add(this.panel6);
            this.grpTriangleZ2.Location = new System.Drawing.Point(2, 200);
            this.grpTriangleZ2.Name = "grpTriangleZ2";
            this.grpTriangleZ2.Size = new System.Drawing.Size(160, 148);
            this.grpTriangleZ2.TabIndex = 19;
            this.grpTriangleZ2.TabStop = false;
            this.grpTriangleZ2.Text = "Left Triangle\'s Z";
            this.grpTriangleZ2.Visible = false;
            // 
            // chkLargeZ2
            // 
            this.chkLargeZ2.AutoSize = true;
            this.chkLargeZ2.Location = new System.Drawing.Point(91, 125);
            this.chkLargeZ2.Name = "chkLargeZ2";
            this.chkLargeZ2.Size = new System.Drawing.Size(63, 17);
            this.chkLargeZ2.TabIndex = 8;
            this.chkLargeZ2.Text = "Large Z";
            this.chkLargeZ2.UseVisualStyleBackColor = true;
            this.chkLargeZ2.CheckedChanged += new System.EventHandler(this.chkLargeZ2_CheckedChanged);
            // 
            // chkTrianglePerpendicular2
            // 
            this.chkTrianglePerpendicular2.AutoSize = true;
            this.chkTrianglePerpendicular2.Location = new System.Drawing.Point(6, 19);
            this.chkTrianglePerpendicular2.Name = "chkTrianglePerpendicular2";
            this.chkTrianglePerpendicular2.Size = new System.Drawing.Size(140, 17);
            this.chkTrianglePerpendicular2.TabIndex = 7;
            this.chkTrianglePerpendicular2.Text = "Perpendicular to Screen";
            this.chkTrianglePerpendicular2.UseVisualStyleBackColor = true;
            this.chkTrianglePerpendicular2.CheckedChanged += new System.EventHandler(this.radBallType_CheckedChanged);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.label4);
            this.panel4.Controls.Add(this.radPoint3Pos2);
            this.panel4.Controls.Add(this.radPoint3Neg2);
            this.panel4.Controls.Add(this.radPoint3Zero2);
            this.panel4.Location = new System.Drawing.Point(6, 98);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(145, 19);
            this.panel4.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 17);
            this.label4.TabIndex = 0;
            this.label4.Text = "3";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // radPoint3Pos2
            // 
            this.radPoint3Pos2.AutoSize = true;
            this.radPoint3Pos2.Location = new System.Drawing.Point(106, 0);
            this.radPoint3Pos2.Name = "radPoint3Pos2";
            this.radPoint3Pos2.Size = new System.Drawing.Size(37, 17);
            this.radPoint3Pos2.TabIndex = 3;
            this.radPoint3Pos2.Text = "10";
            this.radPoint3Pos2.UseVisualStyleBackColor = true;
            this.radPoint3Pos2.CheckedChanged += new System.EventHandler(this.radPoint3Changed2_CheckedChanged);
            // 
            // radPoint3Neg2
            // 
            this.radPoint3Neg2.AutoSize = true;
            this.radPoint3Neg2.Location = new System.Drawing.Point(26, 0);
            this.radPoint3Neg2.Name = "radPoint3Neg2";
            this.radPoint3Neg2.Size = new System.Drawing.Size(40, 17);
            this.radPoint3Neg2.TabIndex = 1;
            this.radPoint3Neg2.Text = "-10";
            this.radPoint3Neg2.UseVisualStyleBackColor = true;
            this.radPoint3Neg2.CheckedChanged += new System.EventHandler(this.radPoint3Changed2_CheckedChanged);
            // 
            // radPoint3Zero2
            // 
            this.radPoint3Zero2.AutoSize = true;
            this.radPoint3Zero2.Checked = true;
            this.radPoint3Zero2.Location = new System.Drawing.Point(69, 0);
            this.radPoint3Zero2.Name = "radPoint3Zero2";
            this.radPoint3Zero2.Size = new System.Drawing.Size(31, 17);
            this.radPoint3Zero2.TabIndex = 2;
            this.radPoint3Zero2.TabStop = true;
            this.radPoint3Zero2.Text = "0";
            this.radPoint3Zero2.UseVisualStyleBackColor = true;
            this.radPoint3Zero2.CheckedChanged += new System.EventHandler(this.radPoint3Changed2_CheckedChanged);
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.label5);
            this.panel5.Controls.Add(this.radPoint2Pos2);
            this.panel5.Controls.Add(this.radPoint2Neg2);
            this.panel5.Controls.Add(this.radPoint2Zero2);
            this.panel5.Location = new System.Drawing.Point(6, 75);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(145, 19);
            this.panel5.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(0, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(20, 17);
            this.label5.TabIndex = 0;
            this.label5.Text = "2";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // radPoint2Pos2
            // 
            this.radPoint2Pos2.AutoSize = true;
            this.radPoint2Pos2.Location = new System.Drawing.Point(106, 0);
            this.radPoint2Pos2.Name = "radPoint2Pos2";
            this.radPoint2Pos2.Size = new System.Drawing.Size(37, 17);
            this.radPoint2Pos2.TabIndex = 3;
            this.radPoint2Pos2.Text = "10";
            this.radPoint2Pos2.UseVisualStyleBackColor = true;
            this.radPoint2Pos2.CheckedChanged += new System.EventHandler(this.radPoint2Changed2_CheckedChanged);
            // 
            // radPoint2Neg2
            // 
            this.radPoint2Neg2.AutoSize = true;
            this.radPoint2Neg2.Location = new System.Drawing.Point(26, 0);
            this.radPoint2Neg2.Name = "radPoint2Neg2";
            this.radPoint2Neg2.Size = new System.Drawing.Size(40, 17);
            this.radPoint2Neg2.TabIndex = 1;
            this.radPoint2Neg2.Text = "-10";
            this.radPoint2Neg2.UseVisualStyleBackColor = true;
            this.radPoint2Neg2.CheckedChanged += new System.EventHandler(this.radPoint2Changed2_CheckedChanged);
            // 
            // radPoint2Zero2
            // 
            this.radPoint2Zero2.AutoSize = true;
            this.radPoint2Zero2.Checked = true;
            this.radPoint2Zero2.Location = new System.Drawing.Point(69, 0);
            this.radPoint2Zero2.Name = "radPoint2Zero2";
            this.radPoint2Zero2.Size = new System.Drawing.Size(31, 17);
            this.radPoint2Zero2.TabIndex = 2;
            this.radPoint2Zero2.TabStop = true;
            this.radPoint2Zero2.Text = "0";
            this.radPoint2Zero2.UseVisualStyleBackColor = true;
            this.radPoint2Zero2.CheckedChanged += new System.EventHandler(this.radPoint2Changed2_CheckedChanged);
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.label6);
            this.panel6.Controls.Add(this.radPoint1Pos2);
            this.panel6.Controls.Add(this.radPoint1Neg2);
            this.panel6.Controls.Add(this.radPoint1Zero2);
            this.panel6.Location = new System.Drawing.Point(6, 50);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(145, 19);
            this.panel6.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(0, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(20, 17);
            this.label6.TabIndex = 0;
            this.label6.Text = "1";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // radPoint1Pos2
            // 
            this.radPoint1Pos2.AutoSize = true;
            this.radPoint1Pos2.Location = new System.Drawing.Point(106, 0);
            this.radPoint1Pos2.Name = "radPoint1Pos2";
            this.radPoint1Pos2.Size = new System.Drawing.Size(37, 17);
            this.radPoint1Pos2.TabIndex = 3;
            this.radPoint1Pos2.Text = "10";
            this.radPoint1Pos2.UseVisualStyleBackColor = true;
            this.radPoint1Pos2.CheckedChanged += new System.EventHandler(this.radPoint1Changed2_CheckedChanged);
            // 
            // radPoint1Neg2
            // 
            this.radPoint1Neg2.AutoSize = true;
            this.radPoint1Neg2.Location = new System.Drawing.Point(26, 0);
            this.radPoint1Neg2.Name = "radPoint1Neg2";
            this.radPoint1Neg2.Size = new System.Drawing.Size(40, 17);
            this.radPoint1Neg2.TabIndex = 1;
            this.radPoint1Neg2.Text = "-10";
            this.radPoint1Neg2.UseVisualStyleBackColor = true;
            this.radPoint1Neg2.CheckedChanged += new System.EventHandler(this.radPoint1Changed2_CheckedChanged);
            // 
            // radPoint1Zero2
            // 
            this.radPoint1Zero2.AutoSize = true;
            this.radPoint1Zero2.Checked = true;
            this.radPoint1Zero2.Location = new System.Drawing.Point(69, 0);
            this.radPoint1Zero2.Name = "radPoint1Zero2";
            this.radPoint1Zero2.Size = new System.Drawing.Size(31, 17);
            this.radPoint1Zero2.TabIndex = 2;
            this.radPoint1Zero2.TabStop = true;
            this.radPoint1Zero2.Text = "0";
            this.radPoint1Zero2.UseVisualStyleBackColor = true;
            this.radPoint1Zero2.CheckedChanged += new System.EventHandler(this.radPoint1Changed2_CheckedChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackColor = System.Drawing.Color.SlateGray;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox1.Location = new System.Drawing.Point(344, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.PanMouseButton = System.Windows.Forms.MouseButtons.Left;
            this.pictureBox1.Size = new System.Drawing.Size(624, 596);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            this.pictureBox1.Resize += new System.EventHandler(this.pictureBox1_Resize);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // CollisionTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 622);
            this.Controls.Add(this.grpTriangleZ2);
            this.Controls.Add(this.grpTriangleZ);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnPanLeft);
            this.Controls.Add(this.btnPanRight);
            this.Controls.Add(this.btnPanUp);
            this.Controls.Add(this.btnPanDown);
            this.Controls.Add(this.btnZoomIn);
            this.Controls.Add(this.btnZoomOut);
            this.Controls.Add(this.btnZoomFit);
            this.Controls.Add(this.pictureBox1);
            this.Name = "CollisionTester";
            this.Text = "Dual Object Collisions";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.grpTriangleZ.ResumeLayout(false);
            this.grpTriangleZ.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.grpTriangleZ2.ResumeLayout(false);
            this.grpTriangleZ2.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		//private System.Windows.Forms.PictureBox pictureBox1;
        private Game.Orig.HelperClassesGDI.LargeMapViewer2D pictureBox1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.RadioButton radBallBall;
		private System.Windows.Forms.RadioButton radSolidBallSolidBall;
		private System.Windows.Forms.Button btnZoomFit;
		private System.Windows.Forms.Button btnZoomOut;
		private System.Windows.Forms.Button btnZoomIn;
		private System.Windows.Forms.RadioButton radLineTriangle;
		private System.Windows.Forms.RadioButton radSphereTriangle;
		private System.Windows.Forms.RadioButton radTriangleTriangle;
		private System.Windows.Forms.Button btnPanDown;
		private System.Windows.Forms.Button btnPanUp;
		private System.Windows.Forms.Button btnPanRight;
		private System.Windows.Forms.Button btnPanLeft;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox grpTriangleZ;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton radPoint1Pos;
		private System.Windows.Forms.RadioButton radPoint1Neg;
		private System.Windows.Forms.RadioButton radPoint1Zero;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.RadioButton radPoint3Pos;
		private System.Windows.Forms.RadioButton radPoint3Neg;
		private System.Windows.Forms.RadioButton radPoint3Zero;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RadioButton radPoint2Pos;
		private System.Windows.Forms.RadioButton radPoint2Neg;
		private System.Windows.Forms.RadioButton radPoint2Zero;
		private System.Windows.Forms.CheckBox chkTrianglePerpendicular;
		private System.Windows.Forms.CheckBox chkLargeZ;
		private System.Windows.Forms.GroupBox grpTriangleZ2;
		private System.Windows.Forms.CheckBox chkLargeZ2;
		private System.Windows.Forms.CheckBox chkTrianglePerpendicular2;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.RadioButton radPoint3Pos2;
		private System.Windows.Forms.RadioButton radPoint3Neg2;
		private System.Windows.Forms.RadioButton radPoint3Zero2;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.RadioButton radPoint2Pos2;
		private System.Windows.Forms.RadioButton radPoint2Neg2;
		private System.Windows.Forms.RadioButton radPoint2Zero2;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.RadioButton radPoint1Pos2;
		private System.Windows.Forms.RadioButton radPoint1Neg2;
		private System.Windows.Forms.RadioButton radPoint1Zero2;
		private System.Windows.Forms.RadioButton radPolygonPolygon;
		private System.Windows.Forms.RadioButton radSpherePolygon;
		private System.Windows.Forms.RadioButton radSphereSphere;
		private System.Windows.Forms.Panel panel7;
	}
}