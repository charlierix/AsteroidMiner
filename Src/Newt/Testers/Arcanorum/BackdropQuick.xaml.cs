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
using System.Windows.Shapes;

namespace Game.Newt.Testers.Arcanorum
{
    public partial class BackdropQuick : Window
    {
        public event EventHandler ValueChanged = null;

        private bool _isInitialized = false;

        public BackdropQuick()
        {
            InitializeComponent();

            _isInitialized = true;

            SliderChanged();
        }

        // Some stable settings for different angles
        //CylinderThetaOffset = 25.5
        //CylinderHeight = 13.9
        //CylinderRadius = 28.8
        //CylinderTranslate = 11

        //CylinderThetaOffset = 12
        //CylinderHeight = 27.6
        //CylinderRadius = 121
        //CylinderTranslate = 66.3

        public double CylinderThetaOffset { get; set; }
        public double CylinderHeight { get; set; }
        public double CylinderRadius { get; set; }
        public double CylinderTranslate { get; set; }
        public double PixelMultiplier { get; set; }
        public int CylinderNumSegments { get; set; }

        private void Slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                SliderChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Slider2_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                SliderChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SliderChanged()
        {
            this.CylinderThetaOffset = trkTheta.Value;
            this.CylinderHeight = trkHeight.Value;
            this.CylinderRadius = trkRadius.Value;
            this.CylinderTranslate = trkTranslate.Value;
            this.PixelMultiplier = trkPixel.Value;
            this.CylinderNumSegments = Convert.ToInt32(trkSegments.Value);

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }
    }
}
