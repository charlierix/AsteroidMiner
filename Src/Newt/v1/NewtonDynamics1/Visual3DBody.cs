using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class Visual3DBody : Visual3DBodyBase, IMesh
    {
		#region IMesh Members

		public IList<Point3D> GetPoints()
		{
			if (CollisionMask != null)
				return ((IMesh)CollisionMask).GetPoints();
			else
				return null;
		}

		#endregion

        #region CollisionMask Property

        public static readonly DependencyProperty CollisionMaskProperty =
            DependencyProperty.Register("CollisionMask", typeof(CollisionMask), typeof(Visual3DBody), new PropertyMetadata(CollisionMaskChanged));

        private static void CollisionMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Visual3DBody body = (Visual3DBody)d;

            if (body.IsInitialised)
                throw new InvalidOperationException("Currently the CollisionMask can not be changed once the body has been initialised.");
        }

        public new CollisionMask CollisionMask
        {
            get { return (CollisionMask)GetValue(CollisionMaskProperty); }
            set { SetValue(CollisionMaskProperty, value); }
        }

        #endregion

        protected override CollisionMask OnInitialise(Matrix3D initialMatrix)
        {
            if (CollisionMask != null)
            {
                CollisionMask.Initialise(this.World);

                return CollisionMask;
            }
            else
                return null;
        }

        protected override void CalculateMass(float mass)
        {
            //
        }
    }
}
