using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public partial class KeyboardHints : Form
    {
        public KeyboardHints()
        {
            InitializeComponent();
        }

        [DefaultValue("")]
        public string KeyboardText
        {
            get
            {
                return label1.Text;
            }
            set
            {
                label1.Text = value;
            }
        }
    }
}