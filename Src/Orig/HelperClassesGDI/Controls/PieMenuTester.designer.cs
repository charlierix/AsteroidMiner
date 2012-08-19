namespace Game.Orig.HelperClassesGDI.Controls
{
    partial class PieMenuTester
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
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.button1 = new System.Windows.Forms.Button();
            this.piePanelMenuTop1 = new Game.Orig.HelperClassesGDI.Controls.PiePanelMenuTop();
            this.SuspendLayout();
            // 
            // splitter1
            // 
            this.splitter1.BackColor = System.Drawing.SystemColors.Control;
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 150);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(300, 3);
            this.splitter1.TabIndex = 2;
            this.splitter1.TabStop = false;
            this.splitter1.SplitterMoving += new System.Windows.Forms.SplitterEventHandler(this.splitter1_SplitterMoving);
            // 
            // button1
            // 
            this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.button1.Location = new System.Drawing.Point(113, 277);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 20);
            this.button1.TabIndex = 3;
            this.button1.Text = "Add Button";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // piePanelMenuTop1
            // 
            this.piePanelMenuTop1.BackColor = System.Drawing.Color.DimGray;
            this.piePanelMenuTop1.Dock = System.Windows.Forms.DockStyle.Top;
            this.piePanelMenuTop1.Location = new System.Drawing.Point(0, 0);
            this.piePanelMenuTop1.Name = "piePanelMenuTop1";
            this.piePanelMenuTop1.Size = new System.Drawing.Size(300, 150);
            this.piePanelMenuTop1.TabIndex = 4;
            this.piePanelMenuTop1.ButtonClicked += new Game.Orig.HelperClassesGDI.Controls.PieMenuButtonClickedHandler(this.piePanelMenuTop1_ButtonClicked);
            this.piePanelMenuTop1.DrawButton += new Game.Orig.HelperClassesGDI.Controls.PieMenuDrawButtonHandler(this.piePanelMenuTop1_DrawButton);
            // 
            // PieMenuTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightSalmon;
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.piePanelMenuTop1);
            this.ExpandCollapseVisible = false;
            this.Name = "PieMenuTester";
            this.Controls.SetChildIndex(this.piePanelMenuTop1, 0);
            this.Controls.SetChildIndex(this.button1, 0);
            this.Controls.SetChildIndex(this.splitter1, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Button button1;
        private PiePanelMenuTop piePanelMenuTop1;
    }
}
