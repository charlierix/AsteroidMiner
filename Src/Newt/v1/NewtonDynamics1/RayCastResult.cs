using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
	public class RayCastResult
	{
		private Body _body;
		private double _hitDistance;
		private Vector3D _normal;
		private double _intersectFactor;

		public RayCastResult(Body body, double hitDistance, Vector3D normal, double intersectParam)
		{
			_body = body;
			_hitDistance = hitDistance;
			_normal = normal;
			_intersectFactor = intersectParam;
		}

		public Body Body
		{
			get { return _body; }
		}

		public double HitDistance
		{
			get { return _hitDistance; }
		}

		public Vector3D Normal
		{
			get { return _normal; }
		}

		public double IntersectFactor
		{
			get { return _intersectFactor; }
		}
	}
}
