using System.Windows;
using System.Windows.Data;

namespace Game.Newt.HelperClasses
{
    public static class BindingHelper
    {
        public static BindingExpressionBase SetBinding(DependencyObject target, DependencyProperty targetProperty, DependencyObject source, DependencyProperty sourceProperty, BindingMode mode, IValueConverter converter)
        {
            Binding binding = new Binding(sourceProperty.Name);
            binding.Source = source;
            binding.Converter = converter;
            binding.Mode = mode;

            return BindingOperations.SetBinding(target, targetProperty, binding);
        }

        public static BindingExpressionBase SetBinding(DependencyObject target, DependencyProperty targetProperty, DependencyObject source, DependencyProperty sourceProperty)
        {
            return SetBinding(target, targetProperty, source, sourceProperty, BindingMode.Default, null);
        }

        public static BindingExpressionBase SetBinding(DependencyObject target, DependencyProperty targetProperty, DependencyObject source)
        {
            return SetBinding(target, targetProperty, source, targetProperty, BindingMode.Default, null);
        }

        public static BindingExpressionBase SetBinding(DependencyObject obj, DependencyProperty targetProperty, DependencyProperty sourceProperty)
        {
            return SetBinding(obj, targetProperty, obj, sourceProperty, BindingMode.Default, null);
        }

        public static BindingExpressionBase SetReadOnlyBinding(DependencyObject target, DependencyProperty targetProperty, DependencyObject source, DependencyProperty sourceProperty)
        {
            return SetBinding(target, targetProperty, source, sourceProperty, BindingMode.OneWay, null);
        }

        public static BindingExpressionBase SetReadOnlyBinding(DependencyObject target, DependencyProperty targetProperty, DependencyObject source)
        {
            return SetReadOnlyBinding(target, targetProperty, source, targetProperty);
        }

        public static BindingExpressionBase SetReadOnlyBinding(DependencyObject obj, DependencyProperty targetProperty, DependencyProperty sourceProperty)
        {
            return SetReadOnlyBinding(obj, targetProperty, obj, sourceProperty);
        }
    }
}
