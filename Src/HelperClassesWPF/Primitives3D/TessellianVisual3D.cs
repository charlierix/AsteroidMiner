using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF.Primitives3D
{
    public abstract class TessellianVisual3D : PrimitiveVisual3D, ISupportInitialize
    {
        protected const double _PI_over_180 = (Math.PI / 180);

        private int _isInitialising;
        private bool _changed;

        #region ISupportInitialize Members

        public TessellianVisual3D()
        {
            _changed = true;
        }

        public void BeginInit()
        {
            if (_isInitialising == 0)
                _changed = false;

            _isInitialising++;
        }

        public void EndInit()
        {
            _isInitialising--;

            if (_isInitialising == 0)
            {
                if ((this.Content == null) || _changed)
                    Update();
            }
        }

        private void Update()
        {
            Geometry3D geometry = Tessellate();
            GeometryModel3D content;
            if (geometry != null)
            {
                if (GeometryFrozen)
                    geometry.Freeze();

                content = new GeometryModel3D();
                content.Geometry = geometry;
            }
            else
                content = null;

            SetContent(content);
        }

        protected abstract Geometry3D Tessellate();

        protected static double DegreesToRadians(double degrees)
        {
            return degrees * _PI_over_180;
        }

        #endregion

		#region Public Properties

		#region DependencyProperty: Geometry

		public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register("Geometry", typeof(GeometryModel3D), typeof(TessellianVisual3D));

		#endregion

		#region DependencyProperty: IsSolid

		public static readonly DependencyProperty IsSolidProperty = DependencyProperty.Register("IsSolid", typeof(bool), typeof(TessellianVisual3D), new PropertyMetadata(true, OnIsSolidChanged));

		private static void OnIsSolidChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			TessellianVisual3D item = (TessellianVisual3D)sender;

			if (item._isInitialising > 0)
				item._changed = true;
			else
				item.Update();
		}

		public bool IsSolid
		{
			get { return (bool)GetValue(IsSolidProperty); }
			set { SetValue(IsSolidProperty, value); }
		}

		#endregion

		#region DependencyProperty: Columns

		public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(int), typeof(TessellianVisual3D), new PropertyMetadata(32, OnColumnsChanged));

		private static void OnColumnsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			TessellianVisual3D item = (TessellianVisual3D)sender;

			int value = (int)e.NewValue;
			if (value < 1)
				throw new ArgumentException("The Columns property has to be larger than zero.");

			if (item._isInitialising > 0)
				item._changed = true;
			else
				item.Update();
		}

		public int Columns
		{
			get { return (int)GetValue(ColumnsProperty); }
			set { SetValue(ColumnsProperty, value); }
		}

		#endregion

		#region DependencyProperty: Rows

		public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(int), typeof(TessellianVisual3D), new PropertyMetadata(32, OnRowsChanged));

		private static void OnRowsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			TessellianVisual3D item = (TessellianVisual3D)sender;

			int value = (int)e.NewValue;
			if (value < 1)
				throw new ArgumentException("The Rows property has to be larger than zero.");

			if (item._isInitialising > 0)
				item._changed = true;
			else
				item.Update();
		}

		public int Rows
		{
			get { return (int)GetValue(RowsProperty); }
			set { SetValue(RowsProperty, value); }
		}

		#endregion

		#region DependencyProperty: GeometryFrozen

		public static readonly DependencyProperty GeometryFrozenProperty = DependencyProperty.Register("GeometryFrozen", typeof(bool), typeof(TessellianVisual3D), new PropertyMetadata(false, null, OnCoerceGeometryFrozenProperty));

		private static object OnCoerceGeometryFrozenProperty(DependencyObject d, object baseValue)
		{
			TessellianVisual3D visual3D = (TessellianVisual3D)d;
			bool value = (bool)baseValue;

			if ((visual3D.Content != null) && (visual3D.Content is GeometryModel3D))
			{
				GeometryModel3D model = (GeometryModel3D)visual3D.Content;
				if (model.Geometry != null)
					return model.Geometry.IsFrozen;
			}

			return value;
		}

		public bool GeometryFrozen
		{
			get { return (bool)GetValue(GeometryFrozenProperty); }
			set { SetValue(GeometryFrozenProperty, value); }
		}

		#endregion        

		#endregion
	}
}
