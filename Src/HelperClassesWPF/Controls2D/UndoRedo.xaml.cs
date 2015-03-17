using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Game.HelperClassesCore;

namespace Game.HelperClassesWPF.Controls2D
{
    /// <summary>
    /// This could be used as a standalone control, but is really meant to be embedded in a glass button
    /// </summary>
    /// <remarks>
    /// This is basically just a vector graphic turned into a contol.  This lets the user set colors, and it grays out when
    /// disabled.  There may be better ways of doing this, it feels like overkill
    /// </remarks>
    public partial class UndoRedo : UserControl
    {
        #region Constructor

        public UndoRedo()
        {
            InitializeComponent();

            // Without this, the xaml can't bind to the custom dependency properties
            this.DataContext = this;

            ChangeColors();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether to show the undo or redo arrow
        /// </summary>
        public bool IsUndo
        {
            get
            {
                return (bool)GetValue(IsUndoProperty);
            }
            set
            {
                SetValue(IsUndoProperty, value);

                if (value)
                {
                    rctUndo.Opacity = 1d;
                    rctRedo.Opacity = 0d;
                }
                else
                {
                    rctUndo.Opacity = 0d;
                    rctRedo.Opacity = 1d;
                }
            }
        }
        public static readonly DependencyProperty IsUndoProperty = DependencyProperty.Register("IsUndo", typeof(bool), typeof(UndoRedo), new UIPropertyMetadata(true));

        /// <summary>
        /// The outline color of the arrow
        /// </summary>
        public Color ArrowBorder
        {
            get
            {
                return (Color)GetValue(ArrowBorderProperty);
            }
            set
            {
                SetValue(ArrowBorderProperty, value);
                ChangeColors();
            }
        }
        public static readonly DependencyProperty ArrowBorderProperty = DependencyProperty.Register("ArrowBorder", typeof(Color), typeof(UndoRedo), new UIPropertyMetadata(Color.FromRgb(67, 124, 157)));

        /// <summary>
        /// The darker, but more colorful arrow background gradient color
        /// </summary>
        public Color ArrowBackgroundFrom
        {
            get
            {
                return (Color)GetValue(ArrowBackgroundFromProperty);
            }
            set
            {
                SetValue(ArrowBackgroundFromProperty, value);
                ChangeColors();
            }
        }
        public static readonly DependencyProperty ArrowBackgroundFromProperty = DependencyProperty.Register("ArrowBackgroundFrom", typeof(Color), typeof(UndoRedo), new UIPropertyMetadata(Color.FromRgb(79, 150, 192)));

        /// <summary>
        /// The brighter, but more gray arrow background gradient color
        /// </summary>
        public Color ArrowBackgroundTo
        {
            get
            {
                return (Color)GetValue(ArrowBackgroundToProperty);
            }
            set
            {
                SetValue(ArrowBackgroundToProperty, value);
                ChangeColors();
            }
        }
        public static readonly DependencyProperty ArrowBackgroundToProperty = DependencyProperty.Register("ArrowBackgroundTo", typeof(Color), typeof(UndoRedo), new UIPropertyMetadata(Color.FromRgb(159, 184, 198)));

        #endregion
        #region Private Properties

        // The xaml rectangles are bound to these properties, which are changed in this.ChangeColors
        private Brush ArrowBorderFinal
        {
            get
            {
                return (Brush)GetValue(ArrowBorderFinalProperty);
            }
            set
            {
                SetValue(ArrowBorderFinalProperty, value);
            }
        }
        private static readonly DependencyProperty ArrowBorderFinalProperty = DependencyProperty.Register("ArrowBorderFinal", typeof(Brush), typeof(UndoRedo), new UIPropertyMetadata(Brushes.Black));

        private Color ArrowBackgroundFromFinal
        {
            get
            {
                return (Color)GetValue(ArrowBackgroundFromFinalProperty);
            }
            set
            {
                SetValue(ArrowBackgroundFromFinalProperty, value);
            }
        }
        private static readonly DependencyProperty ArrowBackgroundFromFinalProperty = DependencyProperty.Register("ArrowBackgroundFromFinal", typeof(Color), typeof(UndoRedo), new UIPropertyMetadata(Colors.Blue));

        private Color ArrowBackgroundToFinal
        {
            get
            {
                return (Color)GetValue(ArrowBackgroundToFinalProperty);
            }
            set
            {
                SetValue(ArrowBackgroundToFinalProperty, value);
            }
        }
        private static readonly DependencyProperty ArrowBackgroundToFinalProperty = DependencyProperty.Register("ArrowBackgroundToFinal", typeof(Color), typeof(UndoRedo), new UIPropertyMetadata(Colors.Silver));

        #endregion

        #region Event Listeners

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Calling the property set will change the visibility of the arrow visuals
            this.IsUndo = this.IsUndo;
            ChangeColors();
        }

        private void Grid_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ChangeColors();
        }

        #endregion

        #region Private Methods

        private void ChangeColors()
        {
            const double ALPHAMULT = .33d;

            if (this.IsEnabled)
            {
                this.ArrowBorderFinal = new SolidColorBrush(this.ArrowBorder);
                this.ArrowBackgroundFromFinal = this.ArrowBackgroundFrom;
                this.ArrowBackgroundToFinal = this.ArrowBackgroundTo;
            }
            else
            {
                ColorHSV hsv = this.ArrowBorder.ToHSV();
                double a = UtilityCore.GetScaledValue_Capped(0d, 255d, 0d, 255d, this.ArrowBorder.A * ALPHAMULT);
                this.ArrowBorderFinal = new SolidColorBrush(UtilityWPF.HSVtoRGB(Convert.ToByte(a), hsv.H, 0d, hsv.V));

                hsv = this.ArrowBackgroundFrom.ToHSV();
                a = UtilityCore.GetScaledValue_Capped(0d, 255d, 0d, 255d, this.ArrowBackgroundFrom.A * ALPHAMULT);
                this.ArrowBackgroundFromFinal = UtilityWPF.HSVtoRGB(Convert.ToByte(a), hsv.H, 0d, hsv.V);

                hsv = this.ArrowBackgroundTo.ToHSV();
                a = UtilityCore.GetScaledValue_Capped(0d, 255d, 0d, 255d, this.ArrowBackgroundTo.A * ALPHAMULT);
                this.ArrowBackgroundToFinal = UtilityWPF.HSVtoRGB(Convert.ToByte(a), hsv.H, 0d, hsv.V);
            }
        }

        #endregion
    }
}
