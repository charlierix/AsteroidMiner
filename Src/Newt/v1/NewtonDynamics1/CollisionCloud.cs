using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
	/// <summary>
	/// A collision cloud appears to be definded as 2D, and depth sets the Z (see UpdateFaces)
	/// </summary>
    public class CollisionCloud : CollisionTree
    {
        #region Points Property

        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register("Points", typeof(PointCollection), typeof(CollisionCloud), new PropertyMetadata(new PointCollection()));

        public new PointCollection Points
        {
            get { return (PointCollection)GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        #endregion

        #region Depth Property

        public static readonly DependencyProperty DepthProperty =
            DependencyProperty.Register("Depth", typeof(double), typeof(CollisionCloud), new PropertyMetadata(1.0));

        public double Depth
        {
            get { return (double)GetValue(DepthProperty); }
            set { SetValue(DepthProperty, value); }
        }

        #endregion

        protected override void UpdateFaces()
        {
            PointCollection points2D = Points;
            double depth = Depth / 2;

            List<Point3D> points3D = new List<Point3D>();
            for (int i = 0, len = points2D.Count-1; i < len; i++)
            {
                Point3D pointA1 = InitialMatrix.Transform(new Point3D(points2D[i].X, points2D[i].Y, depth));
                Point3D pointA2 = InitialMatrix.Transform(new Point3D(points2D[i].X, points2D[i].Y, -depth));

                Point3D pointB1 = InitialMatrix.Transform(new Point3D(points2D[i+1].X, points2D[i+1].Y, depth));
                Point3D pointB2 = InitialMatrix.Transform(new Point3D(points2D[i+1].X, points2D[i+1].Y, -depth));

                points3D.Add(pointB1);
                points3D.Add(pointA2);
                points3D.Add(pointA1);

                points3D.Add(pointB1);
                points3D.Add(pointB2);
                points3D.Add(pointA2);
            }

            AddFaces(points3D, 3, false);
        }
    }
}
