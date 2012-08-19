namespace Game.Orig.TestersGDI.PhysicsPainter
{
    partial class GravityProps
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
            this.label9 = new System.Windows.Forms.Label();
            this.trkGravityForce = new System.Windows.Forms.TrackBar();
            this.radGravityNone = new System.Windows.Forms.RadioButton();
            this.radGravityDown = new System.Windows.Forms.RadioButton();
            this.radGravityBalls = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.trkGravityForce)).BeginInit();
            this.SuspendLayout();
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(108, 202);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(34, 13);
            this.label9.TabIndex = 36;
            this.label9.Text = "Force";
            // 
            // trkGravityForce
            // 
            this.trkGravityForce.Location = new System.Drawing.Point(3, 218);
            this.trkGravityForce.Maximum = 1000;
            this.trkGravityForce.Name = "trkGravityForce";
            this.trkGravityForce.Size = new System.Drawing.Size(233, 29);
            this.trkGravityForce.TabIndex = 35;
            this.trkGravityForce.TickFrequency = 25;
            this.trkGravityForce.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkGravityForce.Value = 100;
            this.trkGravityForce.Scroll += new System.EventHandler(this.trkGravityForce_Scroll);
            // 
            // radGravityNone
            // 
            this.radGravityNone.AutoSize = true;
            this.radGravityNone.Checked = true;
            this.radGravityNone.Location = new System.Drawing.Point(12, 30);
            this.radGravityNone.Name = "radGravityNone";
            this.radGravityNone.Size = new System.Drawing.Size(75, 17);
            this.radGravityNone.TabIndex = 32;
            this.radGravityNone.TabStop = true;
            this.radGravityNone.Text = "No Gravity";
            this.radGravityNone.UseVisualStyleBackColor = true;
            this.radGravityNone.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // radGravityDown
            // 
            this.radGravityDown.AutoSize = true;
            this.radGravityDown.Location = new System.Drawing.Point(12, 53);
            this.radGravityDown.Name = "radGravityDown";
            this.radGravityDown.Size = new System.Drawing.Size(53, 17);
            this.radGravityDown.TabIndex = 33;
            this.radGravityDown.Text = "Down";
            this.radGravityDown.UseVisualStyleBackColor = true;
            this.radGravityDown.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // radGravityBalls
            // 
            this.radGravityBalls.AutoSize = true;
            this.radGravityBalls.Location = new System.Drawing.Point(12, 76);
            this.radGravityBalls.Name = "radGravityBalls";
            this.radGravityBalls.Size = new System.Drawing.Size(47, 17);
            this.radGravityBalls.TabIndex = 34;
            this.radGravityBalls.Text = "Balls";
            this.radGravityBalls.UseVisualStyleBackColor = true;
            this.radGravityBalls.CheckedChanged += new System.EventHandler(this.radGravity_CheckedChanged);
            // 
            // GravityProps
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label9);
            this.Controls.Add(this.trkGravityForce);
            this.Controls.Add(this.radGravityNone);
            this.Controls.Add(this.radGravityDown);
            this.Controls.Add(this.radGravityBalls);
            this.ExpandCollapseVisible = false;
            this.Name = "GravityProps";
            this.Radius = 250;
            this.Size = new System.Drawing.Size(250, 250);
            this.Controls.SetChildIndex(this.radGravityBalls, 0);
            this.Controls.SetChildIndex(this.radGravityDown, 0);
            this.Controls.SetChildIndex(this.radGravityNone, 0);
            this.Controls.SetChildIndex(this.trkGravityForce, 0);
            this.Controls.SetChildIndex(this.label9, 0);
            ((System.ComponentModel.ISupportInitialize)(this.trkGravityForce)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TrackBar trkGravityForce;
        private System.Windows.Forms.RadioButton radGravityNone;
        private System.Windows.Forms.RadioButton radGravityDown;
        private System.Windows.Forms.RadioButton radGravityBalls;
    }
}
