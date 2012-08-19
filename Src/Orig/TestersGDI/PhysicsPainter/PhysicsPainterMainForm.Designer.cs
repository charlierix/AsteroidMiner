namespace Game.Orig.TestersGDI.PhysicsPainter
{
    partial class PhysicsPainterMainForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PhysicsPainterMainForm));
			this.pnlContainer = new System.Windows.Forms.Panel();
			this.toolStrip2 = new System.Windows.Forms.ToolStrip();
			this.btnClear = new System.Windows.Forms.ToolStripButton();
			this.btnZoomOut = new System.Windows.Forms.ToolStripButton();
			this.btnShowStats = new System.Windows.Forms.ToolStripButton();
			this.btnZoomFit = new System.Windows.Forms.ToolStripButton();
			this.btnBlank2 = new System.Windows.Forms.ToolStripButton();
			this.btnPan = new System.Windows.Forms.ToolStripButton();
			this.btnShip = new System.Windows.Forms.ToolStripButton();
			this.btnVectorFieldArrow = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.btnNewSolidBall = new System.Windows.Forms.ToolStripButton();
			this.btnNewPolygon = new System.Windows.Forms.ToolStripButton();
			this.btnCollisionProps = new System.Windows.Forms.ToolStripButton();
			this.btnBlank6 = new System.Windows.Forms.ToolStripButton();
			this.btnMapShape = new System.Windows.Forms.ToolStripButton();
			this.btnGeneralProps = new System.Windows.Forms.ToolStripButton();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.btnRunning = new System.Windows.Forms.ToolStripButton();
			this.btnZoomIn = new System.Windows.Forms.ToolStripButton();
			this.btnStopVelocity = new System.Windows.Forms.ToolStripButton();
			this.btnBlank3 = new System.Windows.Forms.ToolStripButton();
			this.btnArrow = new System.Windows.Forms.ToolStripButton();
			this.btnGravityArrow = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.btnNewBall = new System.Windows.Forms.ToolStripButton();
			this.btnBlank5 = new System.Windows.Forms.ToolStripButton();
			this.btnNewRigidBody = new System.Windows.Forms.ToolStripButton();
			this.btnNewVectorField = new System.Windows.Forms.ToolStripButton();
			this.btnGravityProps = new System.Windows.Forms.ToolStripButton();
			this.btnScenes = new System.Windows.Forms.ToolStripButton();
			this.btnMenuTester = new System.Windows.Forms.ToolStripButton();
			this.pnlTopSpacer = new System.Windows.Forms.Panel();
			this.pnlLeftSpacer = new System.Windows.Forms.Panel();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.pictureBox1 = new Game.Orig.HelperClassesGDI.LargeMapViewer2D();
			this.mapShape1 = new Game.Orig.TestersGDI.PhysicsPainter.MapShape();
			this.gravityProps1 = new Game.Orig.TestersGDI.PhysicsPainter.GravityProps();
			this.shipProps1 = new Game.Orig.TestersGDI.PhysicsPainter.ShipProps();
			this.scenes1 = new Game.Orig.TestersGDI.PhysicsPainter.Scenes();
			this.ballProps1 = new Game.Orig.TestersGDI.PhysicsPainter.PieBallProps();
			this.solidBallProps1 = new Game.Orig.TestersGDI.PhysicsPainter.SolidBallProps();
			this.pieMenuTester1 = new Game.Orig.HelperClassesGDI.Controls.PieMenuTester();
			this.generalProps1 = new Game.Orig.TestersGDI.PhysicsPainter.GeneralProps();
			this.pnlContainer.SuspendLayout();
			this.toolStrip2.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// pnlContainer
			// 
			this.pnlContainer.Controls.Add(this.toolStrip2);
			this.pnlContainer.Controls.Add(this.toolStrip1);
			this.pnlContainer.Controls.Add(this.pnlTopSpacer);
			this.pnlContainer.Controls.Add(this.pnlLeftSpacer);
			this.pnlContainer.Dock = System.Windows.Forms.DockStyle.Left;
			this.pnlContainer.Location = new System.Drawing.Point(0, 0);
			this.pnlContainer.Name = "pnlContainer";
			this.pnlContainer.Size = new System.Drawing.Size(57, 616);
			this.pnlContainer.TabIndex = 6;
			// 
			// toolStrip2
			// 
			this.toolStrip2.Dock = System.Windows.Forms.DockStyle.Left;
			this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnClear,
            this.btnZoomOut,
            this.btnShowStats,
            this.btnZoomFit,
            this.btnBlank2,
            this.btnPan,
            this.btnShip,
            this.btnVectorFieldArrow,
            this.toolStripButton3,
            this.btnNewSolidBall,
            this.btnNewPolygon,
            this.btnCollisionProps,
            this.btnBlank6,
            this.btnMapShape,
            this.btnGeneralProps});
			this.toolStrip2.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table;
			this.toolStrip2.Location = new System.Drawing.Point(30, 6);
			this.toolStrip2.Name = "toolStrip2";
			this.toolStrip2.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip2.Size = new System.Drawing.Size(24, 610);
			this.toolStrip2.TabIndex = 9;
			this.toolStrip2.Text = "toolStrip1";
			// 
			// btnClear
			// 
			this.btnClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnClear.Image = global::Game.Orig.TestersGDI.Properties.Resources.Clear;
			this.btnClear.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(23, 20);
			this.btnClear.ToolTipText = "Remove All Objects";
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// btnZoomOut
			// 
			this.btnZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnZoomOut.Image = global::Game.Orig.TestersGDI.Properties.Resources.ZoomOut;
			this.btnZoomOut.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnZoomOut.Name = "btnZoomOut";
			this.btnZoomOut.Size = new System.Drawing.Size(23, 20);
			this.btnZoomOut.ToolTipText = "Zoom Out";
			this.btnZoomOut.Click += new System.EventHandler(this.btnZoomOut_Click);
			// 
			// btnShowStats
			// 
			this.btnShowStats.CheckOnClick = true;
			this.btnShowStats.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnShowStats.Enabled = false;
			this.btnShowStats.Image = global::Game.Orig.TestersGDI.Properties.Resources.ShowStats;
			this.btnShowStats.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnShowStats.Name = "btnShowStats";
			this.btnShowStats.Size = new System.Drawing.Size(23, 20);
			this.btnShowStats.ToolTipText = "Show Stats";
			this.btnShowStats.Visible = false;
			this.btnShowStats.Click += new System.EventHandler(this.btnShowStats_Click);
			// 
			// btnZoomFit
			// 
			this.btnZoomFit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnZoomFit.Image = global::Game.Orig.TestersGDI.Properties.Resources.ZoomFit;
			this.btnZoomFit.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnZoomFit.Name = "btnZoomFit";
			this.btnZoomFit.Size = new System.Drawing.Size(23, 20);
			this.btnZoomFit.ToolTipText = "Zoom Fit";
			this.btnZoomFit.Click += new System.EventHandler(this.btnZoomFit_Click);
			// 
			// btnBlank2
			// 
			this.btnBlank2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnBlank2.Enabled = false;
			this.btnBlank2.Image = global::Game.Orig.TestersGDI.Properties.Resources.Blank;
			this.btnBlank2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnBlank2.Name = "btnBlank2";
			this.btnBlank2.Size = new System.Drawing.Size(23, 20);
			// 
			// btnPan
			// 
			this.btnPan.CheckOnClick = true;
			this.btnPan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnPan.Image = global::Game.Orig.TestersGDI.Properties.Resources.Pan;
			this.btnPan.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnPan.Name = "btnPan";
			this.btnPan.Size = new System.Drawing.Size(23, 20);
			this.btnPan.ToolTipText = "Pan (Alt)";
			this.btnPan.Click += new System.EventHandler(this.btnPan_Click);
			// 
			// btnShip
			// 
			this.btnShip.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnShip.Image = global::Game.Orig.TestersGDI.Properties.Resources.Ship7;
			this.btnShip.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnShip.Name = "btnShip";
			this.btnShip.Size = new System.Drawing.Size(23, 20);
			this.btnShip.Text = "Ship";
			this.btnShip.Click += new System.EventHandler(this.btnShip_Click);
			// 
			// btnVectorFieldArrow
			// 
			this.btnVectorFieldArrow.CheckOnClick = true;
			this.btnVectorFieldArrow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnVectorFieldArrow.Enabled = false;
			this.btnVectorFieldArrow.Image = global::Game.Orig.TestersGDI.Properties.Resources.VectorFieldArrow;
			this.btnVectorFieldArrow.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnVectorFieldArrow.Name = "btnVectorFieldArrow";
			this.btnVectorFieldArrow.Size = new System.Drawing.Size(23, 20);
			this.btnVectorFieldArrow.ToolTipText = "Vector Field Arrow";
			this.btnVectorFieldArrow.Visible = false;
			this.btnVectorFieldArrow.Click += new System.EventHandler(this.btnVectorFieldArrow_Click);
			// 
			// toolStripButton3
			// 
			this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton3.Enabled = false;
			this.toolStripButton3.Image = global::Game.Orig.TestersGDI.Properties.Resources.Blank;
			this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton3.Name = "toolStripButton3";
			this.toolStripButton3.Size = new System.Drawing.Size(23, 20);
			// 
			// btnNewSolidBall
			// 
			this.btnNewSolidBall.CheckOnClick = true;
			this.btnNewSolidBall.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnNewSolidBall.Image = global::Game.Orig.TestersGDI.Properties.Resources.BlueDot;
			this.btnNewSolidBall.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnNewSolidBall.Name = "btnNewSolidBall";
			this.btnNewSolidBall.Size = new System.Drawing.Size(23, 20);
			this.btnNewSolidBall.ToolTipText = "New Solid Ball";
			this.btnNewSolidBall.Click += new System.EventHandler(this.btnNewSolidBall_Click);
			// 
			// btnNewPolygon
			// 
			this.btnNewPolygon.CheckOnClick = true;
			this.btnNewPolygon.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnNewPolygon.Enabled = false;
			this.btnNewPolygon.Image = global::Game.Orig.TestersGDI.Properties.Resources.Polygon;
			this.btnNewPolygon.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnNewPolygon.Name = "btnNewPolygon";
			this.btnNewPolygon.Size = new System.Drawing.Size(23, 20);
			this.btnNewPolygon.ToolTipText = "New Polygon";
			this.btnNewPolygon.Visible = false;
			this.btnNewPolygon.Click += new System.EventHandler(this.btnNewPolygon_Click);
			// 
			// btnCollisionProps
			// 
			this.btnCollisionProps.CheckOnClick = true;
			this.btnCollisionProps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCollisionProps.Enabled = false;
			this.btnCollisionProps.Image = global::Game.Orig.TestersGDI.Properties.Resources.CollisionProps;
			this.btnCollisionProps.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCollisionProps.Name = "btnCollisionProps";
			this.btnCollisionProps.Size = new System.Drawing.Size(23, 20);
			this.btnCollisionProps.ToolTipText = "Collision Properties";
			this.btnCollisionProps.Visible = false;
			this.btnCollisionProps.Click += new System.EventHandler(this.btnCollisionProps_Click);
			// 
			// btnBlank6
			// 
			this.btnBlank6.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnBlank6.Enabled = false;
			this.btnBlank6.Image = global::Game.Orig.TestersGDI.Properties.Resources.Blank;
			this.btnBlank6.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnBlank6.Name = "btnBlank6";
			this.btnBlank6.Size = new System.Drawing.Size(23, 20);
			// 
			// btnMapShape
			// 
			this.btnMapShape.CheckOnClick = true;
			this.btnMapShape.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnMapShape.Image = global::Game.Orig.TestersGDI.Properties.Resources.MapShape;
			this.btnMapShape.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnMapShape.Name = "btnMapShape";
			this.btnMapShape.Size = new System.Drawing.Size(23, 20);
			this.btnMapShape.ToolTipText = "Map Shape";
			this.btnMapShape.Click += new System.EventHandler(this.btnMapShape_Click);
			// 
			// btnGeneralProps
			// 
			this.btnGeneralProps.CheckOnClick = true;
			this.btnGeneralProps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnGeneralProps.Image = global::Game.Orig.TestersGDI.Properties.Resources.GeneralProps;
			this.btnGeneralProps.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnGeneralProps.Name = "btnGeneralProps";
			this.btnGeneralProps.Size = new System.Drawing.Size(23, 20);
			this.btnGeneralProps.ToolTipText = "General Properties";
			this.btnGeneralProps.Click += new System.EventHandler(this.btnGeneralProps_Click);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Left;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnRunning,
            this.btnZoomIn,
            this.btnStopVelocity,
            this.btnBlank3,
            this.btnArrow,
            this.btnGravityArrow,
            this.toolStripButton1,
            this.btnNewBall,
            this.btnBlank5,
            this.btnNewRigidBody,
            this.btnNewVectorField,
            this.btnGravityProps,
            this.btnScenes,
            this.btnMenuTester});
			this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table;
			this.toolStrip1.Location = new System.Drawing.Point(6, 6);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(24, 610);
			this.toolStrip1.TabIndex = 8;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// btnRunning
			// 
			this.btnRunning.Checked = true;
			this.btnRunning.CheckOnClick = true;
			this.btnRunning.CheckState = System.Windows.Forms.CheckState.Checked;
			this.btnRunning.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnRunning.Image = global::Game.Orig.TestersGDI.Properties.Resources.Pause;
			this.btnRunning.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnRunning.Name = "btnRunning";
			this.btnRunning.Size = new System.Drawing.Size(23, 20);
			this.btnRunning.ToolTipText = "Pause Simulation (P)";
			this.btnRunning.Click += new System.EventHandler(this.btnRunning_Click);
			// 
			// btnZoomIn
			// 
			this.btnZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnZoomIn.Image = global::Game.Orig.TestersGDI.Properties.Resources.ZoomIn;
			this.btnZoomIn.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnZoomIn.Name = "btnZoomIn";
			this.btnZoomIn.Size = new System.Drawing.Size(23, 20);
			this.btnZoomIn.ToolTipText = "Zoom In";
			this.btnZoomIn.Click += new System.EventHandler(this.btnZoomIn_Click);
			// 
			// btnStopVelocity
			// 
			this.btnStopVelocity.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnStopVelocity.Image = global::Game.Orig.TestersGDI.Properties.Resources.StopVelocity;
			this.btnStopVelocity.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnStopVelocity.Name = "btnStopVelocity";
			this.btnStopVelocity.Size = new System.Drawing.Size(23, 20);
			this.btnStopVelocity.ToolTipText = "Stop All Velocities (Space Bar)";
			this.btnStopVelocity.Click += new System.EventHandler(this.btnStopVelocity_Click);
			// 
			// btnBlank3
			// 
			this.btnBlank3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnBlank3.Enabled = false;
			this.btnBlank3.Image = global::Game.Orig.TestersGDI.Properties.Resources.Blank;
			this.btnBlank3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnBlank3.Name = "btnBlank3";
			this.btnBlank3.Size = new System.Drawing.Size(23, 20);
			// 
			// btnArrow
			// 
			this.btnArrow.CheckOnClick = true;
			this.btnArrow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnArrow.Image = global::Game.Orig.TestersGDI.Properties.Resources.Arrow;
			this.btnArrow.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnArrow.Name = "btnArrow";
			this.btnArrow.Size = new System.Drawing.Size(23, 20);
			this.btnArrow.ToolTipText = "Select Objects";
			this.btnArrow.Click += new System.EventHandler(this.btnArrow_Click);
			// 
			// btnGravityArrow
			// 
			this.btnGravityArrow.CheckOnClick = true;
			this.btnGravityArrow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnGravityArrow.Image = global::Game.Orig.TestersGDI.Properties.Resources.GravityArrow;
			this.btnGravityArrow.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnGravityArrow.Name = "btnGravityArrow";
			this.btnGravityArrow.Size = new System.Drawing.Size(23, 20);
			this.btnGravityArrow.ToolTipText = "Gravity Arrow";
			this.btnGravityArrow.Click += new System.EventHandler(this.btnGravityArrow_Click);
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Enabled = false;
			this.toolStripButton1.Image = global::Game.Orig.TestersGDI.Properties.Resources.Blank;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(23, 20);
			// 
			// btnNewBall
			// 
			this.btnNewBall.CheckOnClick = true;
			this.btnNewBall.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnNewBall.Image = global::Game.Orig.TestersGDI.Properties.Resources.YellowDot;
			this.btnNewBall.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnNewBall.Name = "btnNewBall";
			this.btnNewBall.Size = new System.Drawing.Size(23, 20);
			this.btnNewBall.ToolTipText = "New Ball";
			this.btnNewBall.Click += new System.EventHandler(this.btnNewBall_Click);
			// 
			// btnBlank5
			// 
			this.btnBlank5.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnBlank5.Enabled = false;
			this.btnBlank5.Image = global::Game.Orig.TestersGDI.Properties.Resources.Blank;
			this.btnBlank5.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnBlank5.Name = "btnBlank5";
			this.btnBlank5.Size = new System.Drawing.Size(23, 20);
			// 
			// btnNewRigidBody
			// 
			this.btnNewRigidBody.CheckOnClick = true;
			this.btnNewRigidBody.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnNewRigidBody.Enabled = false;
			this.btnNewRigidBody.Image = global::Game.Orig.TestersGDI.Properties.Resources.RigidBodyDot;
			this.btnNewRigidBody.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnNewRigidBody.Name = "btnNewRigidBody";
			this.btnNewRigidBody.Size = new System.Drawing.Size(23, 20);
			this.btnNewRigidBody.ToolTipText = "New Rigid Body";
			this.btnNewRigidBody.Visible = false;
			this.btnNewRigidBody.Click += new System.EventHandler(this.btnNewRigidBody_Click);
			// 
			// btnNewVectorField
			// 
			this.btnNewVectorField.CheckOnClick = true;
			this.btnNewVectorField.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnNewVectorField.Enabled = false;
			this.btnNewVectorField.Image = global::Game.Orig.TestersGDI.Properties.Resources.VectorField;
			this.btnNewVectorField.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnNewVectorField.Name = "btnNewVectorField";
			this.btnNewVectorField.Size = new System.Drawing.Size(23, 20);
			this.btnNewVectorField.ToolTipText = "New Vector Field";
			this.btnNewVectorField.Visible = false;
			this.btnNewVectorField.Click += new System.EventHandler(this.btnNewVectorField_Click);
			// 
			// btnGravityProps
			// 
			this.btnGravityProps.CheckOnClick = true;
			this.btnGravityProps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnGravityProps.Image = global::Game.Orig.TestersGDI.Properties.Resources.GravityProps;
			this.btnGravityProps.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnGravityProps.Name = "btnGravityProps";
			this.btnGravityProps.Size = new System.Drawing.Size(23, 20);
			this.btnGravityProps.ToolTipText = "Gravity Properties";
			this.btnGravityProps.Click += new System.EventHandler(this.btnGravityProps_Click);
			// 
			// btnScenes
			// 
			this.btnScenes.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnScenes.Image = global::Game.Orig.TestersGDI.Properties.Resources.Layer;
			this.btnScenes.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnScenes.Name = "btnScenes";
			this.btnScenes.Size = new System.Drawing.Size(23, 20);
			this.btnScenes.ToolTipText = "Scenes";
			this.btnScenes.Click += new System.EventHandler(this.btnScenes_Click);
			// 
			// btnMenuTester
			// 
			this.btnMenuTester.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnMenuTester.Image = ((System.Drawing.Image)(resources.GetObject("btnMenuTester.Image")));
			this.btnMenuTester.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnMenuTester.Name = "btnMenuTester";
			this.btnMenuTester.Size = new System.Drawing.Size(23, 20);
			this.btnMenuTester.Text = "toolStripButton1";
			this.btnMenuTester.Visible = false;
			this.btnMenuTester.Click += new System.EventHandler(this.btnMenuTester_Click);
			// 
			// pnlTopSpacer
			// 
			this.pnlTopSpacer.Dock = System.Windows.Forms.DockStyle.Top;
			this.pnlTopSpacer.Location = new System.Drawing.Point(6, 0);
			this.pnlTopSpacer.Name = "pnlTopSpacer";
			this.pnlTopSpacer.Size = new System.Drawing.Size(51, 6);
			this.pnlTopSpacer.TabIndex = 7;
			// 
			// pnlLeftSpacer
			// 
			this.pnlLeftSpacer.Dock = System.Windows.Forms.DockStyle.Left;
			this.pnlLeftSpacer.Location = new System.Drawing.Point(0, 0);
			this.pnlLeftSpacer.Name = "pnlLeftSpacer";
			this.pnlLeftSpacer.Size = new System.Drawing.Size(6, 616);
			this.pnlLeftSpacer.TabIndex = 6;
			// 
			// timer1
			// 
			this.timer1.Interval = 10;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// pictureBox1
			// 
			this.pictureBox1.BackColor = System.Drawing.Color.Gray;
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox1.Location = new System.Drawing.Point(57, 0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.PanMouseButton = System.Windows.Forms.MouseButtons.None;
			this.pictureBox1.Size = new System.Drawing.Size(785, 616);
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			// 
			// mapShape1
			// 
			this.mapShape1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.mapShape1.ExpandCollapseVisible = false;
			this.mapShape1.Location = new System.Drawing.Point(443, 242);
			this.mapShape1.Name = "mapShape1";
			this.mapShape1.Radius = 250;
			this.mapShape1.Size = new System.Drawing.Size(250, 250);
			this.mapShape1.TabIndex = 13;
			// 
			// gravityProps1
			// 
			this.gravityProps1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.gravityProps1.ExpandCollapseVisible = false;
			this.gravityProps1.Location = new System.Drawing.Point(392, 215);
			this.gravityProps1.Name = "gravityProps1";
			this.gravityProps1.Radius = 250;
			this.gravityProps1.Size = new System.Drawing.Size(250, 250);
			this.gravityProps1.TabIndex = 12;
			// 
			// shipProps1
			// 
			this.shipProps1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.shipProps1.ExpandCollapseVisible = false;
			this.shipProps1.Location = new System.Drawing.Point(409, 166);
			this.shipProps1.Name = "shipProps1";
			this.shipProps1.Radius = 250;
			this.shipProps1.Size = new System.Drawing.Size(250, 250);
			this.shipProps1.TabIndex = 10;
			// 
			// scenes1
			// 
			this.scenes1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.scenes1.ExpandCollapseVisible = false;
			this.scenes1.Location = new System.Drawing.Point(335, 166);
			this.scenes1.Name = "scenes1";
			this.scenes1.Radius = 250;
			this.scenes1.Size = new System.Drawing.Size(250, 250);
			this.scenes1.TabIndex = 8;
			// 
			// ballProps1
			// 
			this.ballProps1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ballProps1.ExpandCollapseVisible = false;
			this.ballProps1.Location = new System.Drawing.Point(307, 119);
			this.ballProps1.Name = "ballProps1";
			this.ballProps1.Radius = 250;
			this.ballProps1.Size = new System.Drawing.Size(250, 250);
			this.ballProps1.TabIndex = 7;
			this.ballProps1.Visible = false;
			// 
			// solidBallProps1
			// 
			this.solidBallProps1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.solidBallProps1.ExpandCollapseVisible = false;
			this.solidBallProps1.Location = new System.Drawing.Point(381, 75);
			this.solidBallProps1.Name = "solidBallProps1";
			this.solidBallProps1.Radius = 250;
			this.solidBallProps1.Size = new System.Drawing.Size(250, 250);
			this.solidBallProps1.TabIndex = 9;
			// 
			// pieMenuTester1
			// 
			this.pieMenuTester1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.pieMenuTester1.BackColor = System.Drawing.Color.LightSalmon;
			this.pieMenuTester1.ExpandCollapseVisible = false;
			this.pieMenuTester1.Location = new System.Drawing.Point(320, 48);
			this.pieMenuTester1.Name = "pieMenuTester1";
			this.pieMenuTester1.Radius = 300;
			this.pieMenuTester1.Size = new System.Drawing.Size(300, 300);
			this.pieMenuTester1.TabIndex = 11;
			// 
			// generalProps1
			// 
			this.generalProps1.ExpandCollapseVisible = false;
			this.generalProps1.Location = new System.Drawing.Point(409, 310);
			this.generalProps1.Name = "generalProps1";
			this.generalProps1.Radius = 250;
			this.generalProps1.Size = new System.Drawing.Size(250, 250);
			this.generalProps1.TabIndex = 14;
			// 
			// PhysicsPainter
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(842, 616);
			this.Controls.Add(this.generalProps1);
			this.Controls.Add(this.mapShape1);
			this.Controls.Add(this.gravityProps1);
			this.Controls.Add(this.shipProps1);
			this.Controls.Add(this.scenes1);
			this.Controls.Add(this.ballProps1);
			this.Controls.Add(this.solidBallProps1);
			this.Controls.Add(this.pieMenuTester1);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.pnlContainer);
			this.KeyPreview = true;
			this.Name = "PhysicsPainter";
			this.Text = "PhysicsPainter";
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PhysicsPainter_KeyUp);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PhysicsPainter_KeyDown);
			this.pnlContainer.ResumeLayout(false);
			this.pnlContainer.PerformLayout();
			this.toolStrip2.ResumeLayout(false);
			this.toolStrip2.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Game.Orig.HelperClassesGDI.LargeMapViewer2D pictureBox1;
		private System.Windows.Forms.Panel pnlContainer;
		private System.Windows.Forms.ToolStrip toolStrip2;
		private System.Windows.Forms.ToolStripButton btnShowStats;
		private System.Windows.Forms.ToolStripButton btnPan;
		private System.Windows.Forms.ToolStripButton btnZoomOut;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton btnRunning;
		private System.Windows.Forms.ToolStripButton btnArrow;
		private System.Windows.Forms.ToolStripButton btnZoomIn;
		private System.Windows.Forms.Panel pnlTopSpacer;
		private System.Windows.Forms.Panel pnlLeftSpacer;
		private System.Windows.Forms.ToolStripButton btnClear;
		private System.Windows.Forms.ToolStripButton btnGravityArrow;
		private System.Windows.Forms.ToolStripButton btnVectorFieldArrow;
		private System.Windows.Forms.ToolStripButton btnNewBall;
		private System.Windows.Forms.ToolStripButton btnNewSolidBall;
		private System.Windows.Forms.ToolStripButton btnNewRigidBody;
		private System.Windows.Forms.ToolStripButton btnNewPolygon;
		private System.Windows.Forms.ToolStripButton btnNewVectorField;
		private System.Windows.Forms.ToolStripButton btnGravityProps;
		private System.Windows.Forms.ToolStripButton btnCollisionProps;
		private System.Windows.Forms.ToolStripButton btnGeneralProps;
		private System.Windows.Forms.ToolStripButton btnZoomFit;
		private System.Windows.Forms.ToolStripButton btnBlank3;
		private System.Windows.Forms.ToolStripButton btnBlank6;
		private System.Windows.Forms.ToolStripButton btnBlank5;
		private System.Windows.Forms.ToolStripButton btnStopVelocity;
		private Game.Orig.TestersGDI.PhysicsPainter.PieBallProps ballProps1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ToolStripButton btnScenes;
		private Game.Orig.TestersGDI.PhysicsPainter.Scenes scenes1;
		private Game.Orig.TestersGDI.PhysicsPainter.SolidBallProps solidBallProps1;
		private Game.Orig.TestersGDI.PhysicsPainter.ShipProps shipProps1;
		private System.Windows.Forms.ToolStripButton btnShip;
        private Game.Orig.HelperClassesGDI.Controls.PieMenuTester pieMenuTester1;
        private System.Windows.Forms.ToolStripButton btnMenuTester;
        private Game.Orig.TestersGDI.PhysicsPainter.GravityProps gravityProps1;
		private System.Windows.Forms.ToolStripButton btnBlank2;
		private System.Windows.Forms.ToolStripButton toolStripButton3;
		private System.Windows.Forms.ToolStripButton btnMapShape;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private Game.Orig.TestersGDI.PhysicsPainter.MapShape mapShape1;
		private Game.Orig.TestersGDI.PhysicsPainter.GeneralProps generalProps1;

	}
}