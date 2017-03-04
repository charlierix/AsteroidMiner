using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF.Controls3D
{
    public class PrimitiveVisual3D : ModelVisual3D
    {
        #region MaterialProperty

        public static DependencyProperty MaterialProperty =
            DependencyProperty.Register(
                "Material",
                typeof(Material),
                typeof(PrimitiveVisual3D), new PropertyMetadata(null, OnMaterialChanged));

        public Material Material
        {
            get { return (Material)GetValue(MaterialProperty); }
            set { SetValue(MaterialProperty, value); }
        }

        private static void OnMaterialChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            PrimitiveVisual3D p = (PrimitiveVisual3D)sender;

            GeometryModel3D geometry = (p.Content as GeometryModel3D);
            if (geometry != null)
                geometry.Material = (Material)e.NewValue;
        }

        #endregion

        #region BackMaterialProperty

        public static DependencyProperty BackMaterialProperty =
            DependencyProperty.Register(
                "BackMaterial",
                typeof(Material),
                typeof(PrimitiveVisual3D), new PropertyMetadata(null, OnBackMaterialChanged));

        public Material BackMaterial
        {
            get { return (Material)GetValue(BackMaterialProperty); }
            set { SetValue(BackMaterialProperty, value); }
        }

        protected static void OnBackMaterialChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            PrimitiveVisual3D p = (PrimitiveVisual3D)sender;

            GeometryModel3D geometry = (p.Content as GeometryModel3D);
            if (geometry != null)
                geometry.BackMaterial = (Material)e.NewValue;
        }

        #endregion

        #region IsVisibleProperty

        public static DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
                "IsVisible",
                typeof(bool),
                typeof(PrimitiveVisual3D), new PropertyMetadata(true, OnIsVisibleChanged));

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        private static void OnIsVisibleChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            PrimitiveVisual3D p = (PrimitiveVisual3D)sender;

            bool visible = (bool)e.NewValue;

            if (visible)
                p.Content = p._content;
            else
                p.Content = null;
        }

        #endregion

        private GeometryModel3D _content;

        protected void SetContent(GeometryModel3D content)
        {
            _content = content;
            if (_content != null)
            {
                _content.Material = this.Material;
                _content.BackMaterial = this.BackMaterial;
            }

            if (this.IsVisible)
                this.Content = content;
        }

        /*
        protected static void OnGeometryChanged(DependencyObject d)
        {
            PrimitiveVisual3D p = (PrimitiveVisual3D)d;

            if (p._content != null)
                p._content.Geometry = p.Tessellate();
        }
        */
    }
}
