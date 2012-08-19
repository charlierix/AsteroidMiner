using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics_153.Api;

namespace Game.Newt.NewtonDynamics_153
{
    public class Ray : ModelVisual3D
	{
		#region Declaration Section

		private List<CWorldRayFilterEventArgs> _hitTests = new List<CWorldRayFilterEventArgs>();

		private RayCastResult _hitResult;

		#endregion

		#region Constructor

		public Ray()
			: base() { }

		/// <summary>
		/// The position and direction are in world coords
		/// </summary>
		public Ray(Point3D position, Vector3D direction)
			: base()
		{
			this.DirectionOrigin = ObjectOrigin.World;
			this.Direction = direction;

			this.Transform = new TranslateTransform3D(position.X, position.Y, position.Z);
		}

		#endregion

		#region HitDistanceProperty

		protected static readonly DependencyPropertyKey HitDistancePropertyKey = DependencyProperty.RegisterReadOnly("HitDistance", typeof(double), typeof(Ray), new PropertyMetadata(0.0));

        public static readonly DependencyProperty HitDistanceProperty = HitDistancePropertyKey.DependencyProperty;

        public double HitDistance
        {
            get { return (double)GetValue(HitDistanceProperty); }
            set { SetValue(HitDistancePropertyKey, value); }
        }

        #endregion
        #region DirectionProperty

        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register("Direction", typeof(Vector3D), typeof(Ray), new PropertyMetadata(new Vector3D()));

        public Vector3D Direction
        {
            get { return (Vector3D)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }

        #endregion
        #region DirectionOriginProperty

        public static readonly DependencyProperty DirectionOriginProperty =
            DependencyProperty.Register("DirectionOrigin", typeof(ObjectOrigin), typeof(Ray), new PropertyMetadata(ObjectOrigin.Local));

        public ObjectOrigin DirectionOrigin
        {
            get { return (ObjectOrigin)GetValue(DirectionOriginProperty); }
            set { SetValue(DirectionOriginProperty, value); }
        }

        #endregion
		#region RayLengthProperty

		public double RayLength
        {
            get { return (double)GetValue(RayLengthProperty); }
            set { SetValue(RayLengthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RayLength.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RayLengthProperty = DependencyProperty.Register("RayLength", typeof(double), typeof(Ray), new PropertyMetadata(10000.0));

		#endregion

        public void Update(World world, World.BodyFilterType filterType, params Body[] bodies)
        {
            _hitResult = world.CastRay(this, filterType, bodies);

			if (_hitResult != null)
			{
				HitDistance = _hitResult.HitDistance;
			}
			else
			{
				HitDistance = -1;
			}
        }

        public RayCastResult HitResult
        {
            get { return _hitResult; }
        }

        private void ray_WorldRayFilter(object sender, CWorldRayFilterEventArgs e)
        {
            _hitTests.Add(e);
        }

        /*
        private void ray_WorldRayPreFilter(object sender, CWorldRayPreFilterEventArgs e)
        {
        }
         */
    }
}
