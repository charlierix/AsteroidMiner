using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF.Controls3D
{
    //TODO: this logic here is a bit weird using ScreenSpaceLines3D and recursing in.
    public class CompoundVisual3D : PrimitiveVisual3D
	{
		#region DependencyProperty: Opacity

		public static DependencyProperty OpacityProperty =
            DependencyProperty.Register(
                "Opacity",
                typeof(double),
                typeof(PrimitiveVisual3D), new PropertyMetadata(1.0, OnOpacityChanged));

        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        //public event DependencyPropertyChangedEventHandler MaterialChanged;

        private static void OnOpacityChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            CompoundVisual3D visual = (CompoundVisual3D)sender;

            double opacity = (double)e.NewValue;
            visual.SetOpacity(visual, opacity);
        }

        #endregion

		#region AttatchedProperty: OpacityShared

		protected static readonly DependencyProperty OpacitySharedProperty =
			DependencyProperty.RegisterAttached(
				"OpacityShared",
				typeof(bool),
				typeof(CompoundVisual3D), new PropertyMetadata(true));

		public static bool GetOpacityShared(ModelVisual3D visual)
		{
			return (bool)visual.GetValue(OpacitySharedProperty);
		}

		public static void SetOpacityShared(ModelVisual3D visual, bool value)
		{
			visual.SetValue(OpacitySharedProperty, value);
		}

		#endregion

		#region AttatchedProperty: MaterialShared

		protected static readonly DependencyProperty MaterialSharedProperty =
			DependencyProperty.RegisterAttached(
				"MaterialShared",
				typeof(bool),
				typeof(CompoundVisual3D), new PropertyMetadata(true));

		public static bool GetMaterialShared(ModelVisual3D visual)
		{
			return (bool)visual.GetValue(MaterialSharedProperty);
		}

		public static void SetMaterialShared(ModelVisual3D visual, bool value)
		{
			visual.SetValue(MaterialSharedProperty, value);
		}

		#endregion

        static CompoundVisual3D()
        {
            IsVisibleProperty.OverrideMetadata(typeof(CompoundVisual3D), new PropertyMetadata(OnIsVisibleChanged));
        }

        private static void OnIsVisibleChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            PrimitiveVisual3D p = (PrimitiveVisual3D)sender;

            bool visible = (bool)e.NewValue;

            SetIsVisible(p.Children, visible);
        }

        private static void SetIsVisible(ICollection<Visual3D> items, bool isVisible)
        {
            foreach (Visual3D item in items)
            {
                if (item is PrimitiveVisual3D)
                    ((PrimitiveVisual3D)item).IsVisible = isVisible;
                else if (item is ScreenSpaceLines3D)
                    ((ScreenSpaceLines3D)item).IsVisible = isVisible;

                if (item is ModelVisual3D)
                    SetIsVisible(((ModelVisual3D)item).Children, isVisible);
            }
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualAdded != null)
                SetMaterial((ModelVisual3D)visualAdded, true, true);

            //if (visualRemoved != null)
            //    ClearMaterial((ModelVisual3D)visualRemoved);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == MaterialProperty)
            {
                SetMaterial(this.Children, true, false);

                //if (MaterialChanged != null)
                //    MaterialChanged(this, e);
            }
            else if (e.Property == BackMaterialProperty)
            {
                SetMaterial(this.Children, false, true);
            }
        }

        private void SetColor(IEnumerable<Visual3D> items, DiffuseMaterial material)
        {
            foreach (ModelVisual3D visual in items)
            {
                if (visual is ScreenSpaceLines3D)
                {
                    if ((material != null) && (material.Brush is SolidColorBrush))
                    {
                        Color color = ((SolidColorBrush)material.Brush).Color;
                        color.ScA = (float)material.Brush.Opacity;
                        ((ScreenSpaceLines3D)visual).Color = color;
                    }
                    else
                        ((ScreenSpaceLines3D)visual).Color = Colors.Transparent;
                }
                else if (visual is PrimitiveVisual3D)
                {
                    SetMaterial(visual, true, false);
                }

                SetColor(visual.Children, material);
            }
        }

        private void SetMaterial(IEnumerable<Visual3D> items, bool frontMaterial, bool backMaterial)
        {
            foreach (ModelVisual3D child in items)
                SetMaterial(child, frontMaterial, backMaterial);
        }

        private void SetMaterial(ModelVisual3D visual, bool frontMaterial, bool backMaterial)
        {
            if (GetMaterialShared(visual))
            {
                if (visual is PrimitiveVisual3D)
                {
                    PrimitiveVisual3D primitive = (PrimitiveVisual3D)visual;

                    if (frontMaterial)
                    {
                        if (this.Material != null)
                            primitive.Material = this.Material.CloneCurrentValue();
                        else
                            primitive.Material = null;
                    }

                    if (backMaterial)
                    {
                        if (this.BackMaterial != null)
                            primitive.BackMaterial = this.BackMaterial.CloneCurrentValue();
                        else
                            primitive.BackMaterial = null;
                    }
                }
                else if (visual is ScreenSpaceLines3D)
                {
                    if (frontMaterial)
                    {
                        if (GetMaterialShared(visual))
                            SetColor(new ModelVisual3D[] { visual }, GetDiffuseMaterial(this.Material));
                    }
                }

                foreach (Visual3D child in visual.Children)
                {
                    if (child is ModelVisual3D)
                        SetMaterial((ModelVisual3D)child, frontMaterial, backMaterial);
                }
            }
        }

        /*
        private void ClearMaterial(ModelVisual3D visual)
        {
            if (visual is PrimitiveVisual3D)
            {
                if (visual.GetValue(MaterialProperty) == this.Material)
                {
                    BindingOperations.ClearBinding(visual, MaterialProperty);
                }
            }

            foreach (ModelVisual3D child in visual.Children)
                ClearMaterial(child);
        }
        */

        private DiffuseMaterial GetDiffuseMaterial(Material material)
        {
            if (material is MaterialGroup)
            {
                foreach (Material m in ((MaterialGroup)material).Children)
                {
                    DiffuseMaterial result = GetDiffuseMaterial(m);
                    if (result != null)
                        return result;
                }

                return null;
            }
            else
                return material as DiffuseMaterial;
        }

        private void SetOpacity(IEnumerable<Visual3D> items, double opacity)
        {
            foreach (Visual3D item in items)
            {
                ModelVisual3D m = (item as ModelVisual3D);
                SetOpacity(m, opacity);
            }
        }

        private void SetOpacity(ModelVisual3D visual, double opacity)
        {
            if (GetOpacityShared(visual))
            {
                if (visual is PrimitiveVisual3D)
                {
                    SetOpacity(((PrimitiveVisual3D)visual).Material, opacity);
                }
                else if (visual is ScreenSpaceLines3D)
                {
                    SetOpacity(((ScreenSpaceLines3D)visual).Material, opacity);
                }

                SetOpacity(visual.Children, opacity);
            }
        }

        private void SetOpacity(Material material, double opacity)
        {
            if (material is MaterialGroup)
            {
                foreach (Material child in ((MaterialGroup)material).Children)
                {
                    SetOpacity(child, opacity);
                }
            }
            else if (material is DiffuseMaterial)
            {
                if (!((DiffuseMaterial)material).Brush.IsFrozen)
                    ((DiffuseMaterial)material).Brush.Opacity = opacity;
            }
            else if (material is EmissiveMaterial)
            {
                if (!((EmissiveMaterial)material).Brush.IsFrozen)
                    ((EmissiveMaterial)material).Brush.Opacity = opacity;
            }
            else if (material is SpecularMaterial)
            {
                if (!((SpecularMaterial)material).Brush.IsFrozen)
                    ((SpecularMaterial)material).Brush.Opacity = opacity;
            }
        }
    }
}
