using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Game.Orig.HelperClassesGDI.Controls
{
    public partial class PieMenuTester : PiePanel
    {
        Random _rand = new Random();
        private List<Color> _buttonColors = new List<Color>();

        public PieMenuTester()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int index = _buttonColors.Count;

            _buttonColors.Add(Color.FromArgb(_rand.Next(255), _rand.Next(255), _rand.Next(255)));

            piePanelMenuTop1.AddButton("button" + index.ToString());
        }

        private void piePanelMenuTop1_ButtonClicked(object sender, PieMenuButtonClickedArgs e)
        {
            MessageBox.Show(e.Name + " clicked (" + e.Index.ToString() + ")", "PieMenuTester", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void piePanelMenuTop1_DrawButton(object sender, PieMenuDrawButtonArgs e)
        {
            e.Graphics.Clear(_buttonColors[e.Index]);
        }

        private void splitter1_SplitterMoving(object sender, SplitterEventArgs e)
        {
            piePanelMenuTop1.Height = e.SplitY;
        }
    }
}
