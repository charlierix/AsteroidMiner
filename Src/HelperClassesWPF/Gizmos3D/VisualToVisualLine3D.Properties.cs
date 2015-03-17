using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Data;

namespace Game.HelperClassesWPF.Gizmos3D
{
    public partial class VisualToVisualLine3D
    {
        #region FromVisualProperty

        public static readonly DependencyProperty FromVisualProperty =
            DependencyProperty.Register(
                "FromVisual",
                typeof(ModelVisual3D),
                typeof(VisualToVisualLine3D));

        public ModelVisual3D FromVisual
        {
            get { return (ModelVisual3D)GetValue(FromVisualProperty); }
            set { SetValue(FromVisualProperty, value); }
        }

        #endregion

        #region FromOffsetProperty

        public static readonly DependencyProperty FromOffsetProperty =
            DependencyProperty.Register(
                "FromOffset",
                typeof(Point3D),
                typeof(VisualToVisualLine3D));

        public Point3D FromOffset
        {
            get { return (Point3D)GetValue(FromOffsetProperty); }
            set { SetValue(FromOffsetProperty, value); }
        }

        #endregion

        #region ToVisualProperty

        public static readonly DependencyProperty ToVisualProperty =
            DependencyProperty.Register(
                "ToVisual",
                typeof(ModelVisual3D),
                typeof(VisualToVisualLine3D));

        public ModelVisual3D ToVisual
        {
            get { return (ModelVisual3D)GetValue(ToVisualProperty); }
            set { SetValue(ToVisualProperty, value); }
        }

        #endregion

        #region ToOffsetProperty

        public static readonly DependencyProperty ToOffsetProperty =
            DependencyProperty.Register(
                "ToOffset",
                typeof(Point3D),
                typeof(VisualToVisualLine3D));

        public Point3D ToOffset
        {
            get { return (Point3D)GetValue(ToOffsetProperty); }
            set { SetValue(ToOffsetProperty, value); }
        }

        #endregion

        #region ColorProperty

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                "Color",
                typeof(Color),
                typeof(VisualToVisualLine3D),
                new PropertyMetadata(
                    Colors.White,
                    OnColorChanged));

        private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            VisualToVisualLine3D item = (VisualToVisualLine3D)sender;

            item._line.Color = (Color)args.NewValue;
        }

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public Material Material
        {
            get { return _line.Material; }
        }

        #endregion

        #region ThicknessProperty

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(
                "Thickness",
                typeof(double),
                typeof(VisualToVisualLine3D),
                new PropertyMetadata(
                    1.0,
                    OnThicknessChanged));

        private static void OnThicknessChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            VisualToVisualLine3D item = (VisualToVisualLine3D)sender;

            item._line.Thickness = (double)args.NewValue;
        }

        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }

        #endregion
    }
}
