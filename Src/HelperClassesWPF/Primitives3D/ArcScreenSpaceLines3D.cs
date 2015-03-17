using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF.Primitives3D
{
    public class ArcScreenSpaceLines3D : ScreenSpaceLines3D
    {
        #region StartAngleProperty

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register(
                "StartAngle",
                typeof(double),
                typeof(ArcScreenSpaceLines3D), new PropertyMetadata(0.0, OnStartAngleChanged));

        private static void OnStartAngleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ArcScreenSpaceLines3D item = (ArcScreenSpaceLines3D)sender;

            if (item._isInitialising == 0)
                item.Update(true);
        }

        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        #endregion

        #region StopAngleProperty

        public static readonly DependencyProperty StopAngleProperty =
            DependencyProperty.Register(
                "StopAngle",
                typeof(double),
                typeof(ArcScreenSpaceLines3D), new PropertyMetadata(360.0, OnStopAngleChanged));

        private static void OnStopAngleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ArcScreenSpaceLines3D item = (ArcScreenSpaceLines3D)sender;

            if (item._isInitialising == 0)
                item.Update(true);
        }

        public double StopAngle
        {
            get { return (double)GetValue(StopAngleProperty); }
            set { SetValue(StopAngleProperty, value); }
        }

        #endregion

        #region CenterPointProperty

        public static readonly DependencyProperty CenterPointProperty =
            DependencyProperty.Register(
                "CenterPoint",
                typeof(Point3D),
                typeof(ArcScreenSpaceLines3D), new PropertyMetadata(OnCenterPointChanged));

        private static void OnCenterPointChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ArcScreenSpaceLines3D item = (ArcScreenSpaceLines3D)sender;

            if (item._isInitialising == 0)
                item.Update(true);
        }

        public Point3D CenterPoint
        {
            get { return (Point3D)GetValue(CenterPointProperty); }
            set { SetValue(CenterPointProperty, value); }
        }

        #endregion

        #region RadiusProperty

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(
                "Radius",
                typeof(double),
                typeof(ArcScreenSpaceLines3D), new PropertyMetadata(10.0, OnRadiusChanged));

        private static void OnRadiusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ArcScreenSpaceLines3D item = (ArcScreenSpaceLines3D)sender;

            if (item._isInitialising == 0)
                item.Update(true);
        }

        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        #endregion

        #region SegmentsProperty

        public static readonly DependencyProperty SegmentsProperty =
            DependencyProperty.Register(
                "Segments",
                typeof(int),
                typeof(ArcScreenSpaceLines3D), new PropertyMetadata(32, OnSegmentsChanged));

        private static void OnSegmentsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ArcScreenSpaceLines3D item = (ArcScreenSpaceLines3D)sender;

            if (item._isInitialising == 0)
                item.Update(true);
        }

        public int Segments
        {
            get { return (int)GetValue(SegmentsProperty); }
            set { SetValue(SegmentsProperty, value); }
        }

        #endregion

        public override void EndInit()
        {
            Update(false);

            base.EndInit();
        }

        //TODO: change how this is called
        private void Update(bool beginInit)
        {
            if (beginInit)
                BeginInit();

            Points.Clear();
            ScreenSpaceLines3DHelper.AddArc(this, this.CenterPoint, this.Radius, this.Segments, this.StartAngle, this.StopAngle);

            if (beginInit)
                EndInit();
        }
    }
}
