using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using Game.Newt.v1.NewtonDynamics1.Api;
using Game.HelperClassesWPF;

namespace Game.Newt.v1.NewtonDynamics1
{
    public abstract class CollisionTree : CollisionMask, IMesh
	{
		#region Declaration Section

		private readonly List<Point3D> _points = new List<Point3D>();
        private Matrix3D _initialMatrix;

		#endregion

		#region Constructor

		public CollisionTree()
		{
		}
		public CollisionTree(World world)
        {
            Initialise(world, Matrix3D.Identity);
        }
        public CollisionTree(World world, Matrix3D initialMatrix)
        {
            Initialise(world, initialMatrix);
		}

		#endregion

		#region IMesh Members

		IList<Point3D> IMesh.GetPoints()
		{
			return _points;
		}

		#endregion

		#region Protected Properties

		protected Matrix3D InitialMatrix
		{
			get { return _initialMatrix; }
		}

		protected List<Point3D> Points
		{
			get { return _points; }
		}

		#endregion

		#region Public Methods

		public void Initialise(World world, Matrix3D initialMatrix)
        {
            _initialMatrix = Math3D.GetScaleMatrix(ref initialMatrix);
            Initialise(world);
        }

        public new CCollisionTree NewtonCollision
        {
            get { return (CCollisionTree)base.NewtonCollision; }
		}

		#endregion
		#region Protected Methods

		protected virtual void AddFaces(IList<Point3D> points, int pointsPerFace, bool duplicatePoints)
		{
			if (duplicatePoints)
			{
				if (pointsPerFace != 3)
					throw new ArgumentOutOfRangeException("pointsPerFace", 3, "When using duplicatePoints mode, the pointsPerFace has to be 3");

				Point3D[] facePoints = new Point3D[pointsPerFace];
				for (int i = 0, count = points.Count - (pointsPerFace - 1); i < count; )
				{
					for (int f = 0; f < pointsPerFace; f++, i++)
						facePoints[f] = points[i];
					NewtonCollision.TreeAddFace(facePoints);
					_points.AddRange(facePoints);

					i -= 2;
				}
			}
			else
			{
				NewtonCollision.TreeAddFaces(points, pointsPerFace);
				_points.AddRange(points);
			}
		}

		protected virtual void AddFace(Point3D[] points)
		{
			NewtonCollision.TreeAddFace(points);
			_points.AddRange(points);
		}

		protected abstract void UpdateFaces();

		#endregion

		#region Overrides

		protected override CCollision OnInitialise()
        {
            CCollisionTree collision = new CCollisionTree(this.World.NewtonWorld);
            collision.CreateCollisionTree();

            return collision;
        }

        protected override void OnInitialiseEnd()
        {
            base.OnInitialiseEnd();

            _points.Clear();
            NewtonCollision.TreeBeginBuild();
            try
            {
                UpdateFaces();
            }
            finally
            {
                NewtonCollision.TreeEndBuild(true);
            }
		}

		#endregion
    }
}
