using System;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Game.HelperClassesWPF
{
    public static class Viewport3DHelper
	{
		#region Class: FindResult<T>

		public class FindResult<T>
        {
            private readonly Visual3D _visual;
            private readonly T _item;

            public FindResult(Visual3D visual, T item)
            {
                _visual = visual;
                _item = item;
            }

            public Visual3D Visual
            {
                get { return _visual; }
            }

            public T Item
            {
                get { return _item; }
            }
        }

		#endregion

		public static Viewport3DVisual GetViewportVisual(DependencyObject visual)
        {
            if (!(visual is Visual3D))
            {
                throw new ArgumentException("Must be of type Visual3D.", "visual");
            }

            while (visual != null)
            {
                if (!(visual is ModelVisual3D))
                {
                    break;
                }

                visual = VisualTreeHelper.GetParent(visual);
            }

            if (visual != null)
            {
                Viewport3DVisual viewport = visual as Viewport3DVisual;

                if (viewport == null)
                {
                    // In WPF 3D v1 the only possible configuration is a chain of
                    // ModelVisual3Ds leading up to a Viewport3DVisual.

                    throw new ApplicationException(
                        String.Format("Unsupported type: '{0}'.  Expected tree of ModelVisual3Ds leading up to a Viewport3DVisual.",
                        visual.GetType().FullName));
                }

                return viewport;
            }
            else
                return null;
        }
        public static Viewport3DVisual GetViewportVisual(Viewport3D viewport)
        {
            int count =  VisualTreeHelper.GetChildrenCount(viewport);
            if (count > 0)
                return (Viewport3DVisual)VisualTreeHelper.GetChild(viewport, 0);
            else
                return null;
        }

        public static void Dispose(ICollection<Visual3D> items, bool clearChildren)
        {
            foreach (Visual3D item in items)
            {
                Dispose(item, false, clearChildren);
            }

            if (clearChildren)
                items.Clear();
        }
        public static void Dispose(DependencyObject item, bool removeSelf, bool clearChildren)
        {
            if (item is ModelVisual3D)
            {
                Dispose(((ModelVisual3D)item).Children, clearChildren);
            }
            else
            {
                if (clearChildren) throw new ArgumentException("Can not clear the children on a Visual.", "clearChildren");

                for (int i = VisualTreeHelper.GetChildrenCount(item) - 1; i >= 0; i--)
                    Dispose(VisualTreeHelper.GetChild(item, i), removeSelf, clearChildren);
            }

            if (removeSelf)
            {
                object p = VisualTreeHelper.GetParent(item);

                if (p is ModelVisual3D)
                    ((ModelVisual3D)p).Children.Remove((Visual3D)item);
                else if (p is Viewport3DVisual)
                    ((Viewport3DVisual)p).Children.Remove((Visual3D)item);
                else if (p is Panel)
                    ((Panel)p).Children.Remove((UIElement)item);
                else
                    new ArgumentException("Can only removeSelf for ModelVisual3D, Viewport3DVisual and Panel.", "removeSelf");
            }

            IDisposable disposable = (item as IDisposable);
            if (disposable != null)
                disposable.Dispose();
        }
        public static void Dispose(DependencyObject item)
        {
            Dispose(item, true, false);
        }

        public static Visual3D IsParent(Visual3D visual, params Visual3D[] parents)
        {
            while (visual != null)
            {
                int index = Array.IndexOf(parents, visual);
                if (index >= 0)
                    return parents[index];

                visual = (VisualTreeHelper.GetParent(visual) as Visual3D);
            }

            return null;
        }

        public static FindResult<T> Find<T>(Viewport3DVisual viewport)
            where T : DependencyObject
        {
            foreach (Visual3D item in viewport.Children)
            {
                FindResult<T> result = Find<T>(item);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static FindResult<T> Find<T>(Viewport3D viewport)
             where T : DependencyObject
        {
            foreach (Visual3D item in viewport.Children)
            {
                FindResult<T> result = Find<T>(item);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static FindResult<T> Find<T>(Visual3D visual, IList<FindResult<T>> results)
             where T : DependencyObject
        {
            if (visual == null)
                return null;

            T item;

            item = (visual as T);
            if (item != null)
                return new FindResult<T>(visual, item);

            ModelVisual3D model = (visual as ModelVisual3D);
            if (model != null)
            {
                item = (model.Content as T);
                if (item != null)
                    return new FindResult<T>(visual, item);

                foreach (Visual3D i in model.Children)
                {
                    if (results != null)
                    {
                        Find<T>(i, results);
                    }
                    else
                    {
                        FindResult<T> result = Find<T>(i, null);
                        if (result != null)
                            return result;
                    }
                }
            }

            return null;
        }

        public static FindResult<T> Find<T>(Visual3D visual)
             where T : DependencyObject
        {
            return Find<T>(visual, null);
        }

        public static IList<FindResult<T>> FindAll<T>(Visual3D visual)
             where T : DependencyObject
        {
            List<FindResult<T>> results = new List<FindResult<T>>();
            Find<T>(visual, results);
            return results;
        }

        public static IList<FindResult<T>> FindAll<T>(Viewport3DVisual viewport)
            where T : DependencyObject
        {
            List<FindResult<T>> result = new List<FindResult<T>>();

            foreach (Visual3D item in viewport.Children)
            {
                Find<T>(item, result);
            }

            return result;
        }

        public static IList<FindResult<T>> FindAll<T>(Viewport3D viewport)
            where T : DependencyObject
        {
            List<FindResult<T>> result = new List<FindResult<T>>();

            foreach (Visual3D item in viewport.Children)
            {
                Find<T>(item, result);
            }

            return result;
        }

        public static void CopyChildren(Viewport3D targetViewport, Viewport3D sourceViewport)
        {
            CopyChildren(targetViewport.Children, sourceViewport.Children);
        }
        public static void CopyChildren(Viewport3DVisual targetViewport, Viewport3DVisual sourceViewport)
        {
            CopyChildren(targetViewport.Children, sourceViewport.Children);
        }
        public static void CopyChildren(Visual3DCollection targetCollection, Visual3DCollection sourceCollection)
        {
            foreach (Visual3D item in sourceCollection)
            {
                Visual3D newVisual3D = (Visual3D)Activator.CreateInstance(item.GetType());
                ModelVisual3D newModel = (newVisual3D as ModelVisual3D);
                if (newModel != null)
                {
                    ModelVisual3D sourceModel = (ModelVisual3D)item;
                    newModel.Content = sourceModel.Content;
                    newModel.Transform = sourceModel.Transform;

                    CopyChildren(newModel.Children, sourceModel.Children);
                }
                targetCollection.Add(newVisual3D);
            }
        }
    }

	//public class Viewport3D : System.Windows.Controls.Viewport3D
	//{
	//    protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
	//    {
	//        ProcessChildren(this.Children);

	//        base.OnRender(drawingContext);
	//    }

	//    private void ProcessChildren(Visual3DCollection items)
	//    {
	//        foreach (Visual3D item in items)
	//        {
	//            IRenderNotify render = (item as IRenderNotify);
	//            if (render != null)
	//                render.Render();

	//            ModelVisual3D visual = (item as ModelVisual3D);
	//            if (visual != null)
	//                ProcessChildren(visual.Children);
	//        }
	//    }
	//}

}
