using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows;
using System.Windows.Media;
using Game.Newt.HelperClasses;

namespace Game.Newt.NewtonDynamics_153
{
	public abstract class Visual3DBodyBase : Body
	{
		#region Declaration Section

		private ModelVisual3D _visual;
		private Matrix3D _offsetMatrix = Matrix3D.Identity;

		#endregion

		#region Constructor

		public Visual3DBodyBase()
		{
		}

		public Visual3DBodyBase(World world, ModelVisual3D visual)
		{
			if (visual == null) throw new ArgumentNullException("visual");

			_visual = visual;
			Initialise(world);
		}

		#endregion

		#region Public Properties

		public ModelVisual3D Visual
		{
			get { return _visual; }
			internal set { _visual = value; }
		}

		#endregion

		protected override CollisionMask OnInitialise(World world)
		{
			if (_visual != null)
			{
				DependencyObject parent = VisualTreeHelper.GetParent(_visual);
				if (parent == null)
					throw new InvalidOperationException("The Visual3D is not currently connected to a Viewport.");

				VisualMatrix = MathUtils.GetTransformToWorld(_visual);

				if (!(parent is Viewport3DVisual))
				{
					ModelVisual3D parentModel = (parent as ModelVisual3D);
					if (parentModel == null)
						throw new InvalidOperationException("The Visual3D does not belong to a ModelVisual3D.");

					if (!(this is INullBody))
					{
						Viewport3DVisual viewport = Viewport3DHelper.GetViewportVisual(_visual);
						parentModel.Children.Remove(_visual);
						viewport.Children.Add(_visual);
					}
				}

				_visual.Transform = VisualTransform;

				return OnInitialise(VisualMatrix);
			}
			else
				return null;
		}

		protected abstract CollisionMask OnInitialise(Matrix3D initialMatrix);

		/// <summary>
		/// Offset the collision mask for the body.
		/// </summary>
		/// <param name="offset">The offset relative to the model space.</param>
		/// <param name="offsetModel">if set to <c>true</c> the model will be transformed by the offset, otherwise the internal offset matrix will be modified.</param>
		/// <remarks>
		/// This will reposition the body and the model transform for the visual attached to the body.
		/// </remarks>
		protected virtual void OffsetCollisionMask(Vector3D offset, bool offsetModel)
		{
			// offset the body to keep the geometry at the same position.

			// rotate the offset into the body's actual world offset orientation.
			Vector3D o = NewtonBody.Matrix.Transform(offset);

			Matrix3D t = Matrix3D.Identity;
			o.X *= -1;
			o.Y *= -1;
			o.Z *= -1;
			t.Translate(o);
			NewtonBody.Matrix *= t;

			// reposition the model for the visual by transforming the model data.

			Matrix3D m = VisualMatrix;
			m = Math3D.GetScaleMatrix(ref m);// MathUtils.GetTransformToWorld(_visual);
			m.Invert();
			o = m.Transform(offset);

			t = Matrix3D.Identity;
			t.Translate(o);

			if (offsetModel)
				_visual.Content.Transform = new MatrixTransform3D(_visual.Content.Transform.Value * t);
			else
				_offsetMatrix *= t;

			UpdateVisualMatrix(); // repositions the Visual to the new body position.
		}

		protected override Matrix3D OnGetCollisionMatrix()
		{
			return _offsetMatrix;
		}
	}
}
