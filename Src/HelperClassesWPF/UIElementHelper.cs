using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF
{
    public class UIElementHelper
    {
        private static T Find<T>(DependencyObject element, IList<T> results)
             where T : DependencyObject
        {
            if (element == null)
                return null;

            T item;

            item = (element as T);
            if (results != null)
            {
                if (item != null)
                    results.Add(item);
            }
            else
                if (item != null)
                    return item;

            int count = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++)
            {
                T result = Find<T>(VisualTreeHelper.GetChild(element, i), results);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static T Find<T>(DependencyObject element)
             where T : DependencyObject
        {
            return Find<T>(element, null);
        }

        public static IList<T> FindAll<T>(DependencyObject element)
             where T : DependencyObject
        {
            List<T> results = new List<T>();
            Find(element, results);
            return results;
        }
    }
}
