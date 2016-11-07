using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    public partial class TabDebugWindow : Window
    {
        private readonly TabControlParts _tab;
        private readonly TabControlPartsVM_FixedSupply _vm;

        private readonly DispatcherTimer _timer;

        public TabDebugWindow(TabControlParts tab)
        {
            InitializeComponent();

            _tab = tab;
            _vm = (TabControlPartsVM_FixedSupply)tab.DataContext;

            this.Title = _vm.Token.ToString();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
            _timer.IsEnabled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _timer.IsEnabled = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            panel.Children.Clear();

            // Get the parts from the viewmodel
            // Show/Sort name | volume
            var parts = _vm.TabParts_DEBUG.
                Select(o => string.Format("{0} | {1}, {2}, {3} | {4}", o.PartType, o.Scale.X.ToStringSignificantDigits(2), o.Scale.Y.ToStringSignificantDigits(2), o.Scale.Z.ToStringSignificantDigits(2), o.Token)).
                OrderBy(o => o).
                Select(o => new TextBlock() { Text = o });

            foreach (TextBlock part in parts)
            {
                panel.Children.Add(part);
            }


            panel.Children.Add(new Rectangle()
            {
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Fill = Brushes.Silver,
            });


            foreach(long token in _vm.PreviousRemoved)
            {
                panel.Children.Add(new TextBlock() { Text = token.ToString() });
            }
        }
    }
}
