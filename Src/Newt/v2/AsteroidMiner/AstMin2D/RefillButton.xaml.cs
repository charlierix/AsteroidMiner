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

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public partial class RefillButton : Button
    {
        public RefillButton()
        {
            InitializeComponent();

            DataContext = this;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(RefillButton), new PropertyMetadata(""));

        private decimal _credits = 0m;
        public decimal Credits
        {
            get
            {
                return _credits;
            }
            set
            {
                _credits = value;

                this.CreditsDisplay = _credits.ToString("N0");

                lblCreditsText.Content = "credit" + (this.CreditsDisplay == "1" ? "" : "s");
            }
        }

        //TODO: Figure out how to use a value converter
        private string CreditsDisplay
        {
            get { return (string)GetValue(CreditsDisplayProperty); }
            set { SetValue(CreditsDisplayProperty, value); }
        }
        private static readonly DependencyProperty CreditsDisplayProperty = DependencyProperty.Register("CreditsDisplay", typeof(string), typeof(RefillButton), new PropertyMetadata("0"));
    }
}
