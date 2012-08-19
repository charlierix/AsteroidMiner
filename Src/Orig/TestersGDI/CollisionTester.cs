using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.Orig.Math3D;
using Game.Orig.Map;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;

namespace Game.Orig.TestersGDI
{
	public partial class CollisionTester : Form
	{
		#region Enum: MouseDownOn

		private enum MouseDownOn
		{
			Nothing = 0,
			Ball1,
			Ball1Velocity,
			Ball2,
			Ball2Velocity,
			Triangle1,
			Triangle2,
			Polygon1,
			Polygon2
		}

		#endregion

		#region Class: TriangleTest

		private class TriangleTest : Sphere
		{
			#region Enum: MouseDownOnTriangle

			private enum MouseDownOnTriangle
			{
				Nothing = 0,
				Ball,
				Point1,
				Point2,
				Point3,
				Rotate
			}

			#endregion

			#region Declaration Section

			public Triangle Triangle = null;

			private MyVector _rotateHandle = null;
            private MyVector _offset = null;
			private MouseDownOnTriangle _mouseDown = MouseDownOnTriangle.Nothing;

			private Font _font = new Font("Arial", 8);
			private Brush _fontBrush = new SolidBrush(Color.White);

			#endregion

			#region Constructor

            public TriangleTest(MyVector position, DoubleVector origDirFacing, Triangle triangle)
				: base(position, origDirFacing, 150)
			{
				this.Triangle = triangle;
                _rotateHandle = new MyVector(this.Radius, this.Radius, 0);
			}

			#endregion

			#region Public Properties

            public MyVector Vertex1
			{
				get
				{
					return this.Rotation.GetRotatedVector(this.Triangle.Vertex1, true);
				}
			}
            public MyVector Vertex2
			{
				get
				{
					return this.Rotation.GetRotatedVector(this.Triangle.Vertex2, true);
				}
			}
            public MyVector Vertex3
			{
				get
				{
					return this.Rotation.GetRotatedVector(this.Triangle.Vertex3, true);
				}
			}

            public MyVector Vertex1World
			{
				get
				{
					return this.Position + this.Rotation.GetRotatedVector(this.Triangle.Vertex1, true);
				}
			}
            public MyVector Vertex2World
			{
				get
				{
					return this.Position + this.Rotation.GetRotatedVector(this.Triangle.Vertex2, true);
				}
			}
            public MyVector Vertex3World
			{
				get
				{
					return this.Position + this.Rotation.GetRotatedVector(this.Triangle.Vertex3, true);
				}
			}

			public Triangle TriangleWorld
			{
				get
				{
					return new Triangle(this.Vertex1World, this.Vertex2World, this.Vertex3World);
				}
			}

            public MyVector RotateHandle
			{
				get
				{
					return this.Rotation.GetRotatedVector(_rotateHandle, true);
				}
			}

			#endregion

			#region Public Methods

			/// <summary>
			/// This will return true if it can hangle the click.  Otherwise it returns false
			/// </summary>
            public bool OnMouseDown(MyVector mousePos)
			{
				const double CLICKDISTANCE = 35;

				bool retVal = true;
                MyVector localPos = mousePos - this.Position;

				if (Utility3D.GetDistance3D(localPos, this.Vertex1) <= CLICKDISTANCE)
				{
					_mouseDown = MouseDownOnTriangle.Point1;
					_offset = this.Triangle.Vertex1 - localPos;
				}
				else if (Utility3D.GetDistance3D(localPos, this.Vertex2) <= CLICKDISTANCE)
				{
					_mouseDown = MouseDownOnTriangle.Point2;
					_offset = this.Triangle.Vertex2 - localPos;
				}
				else if (Utility3D.GetDistance3D(localPos, this.Vertex3) <= CLICKDISTANCE)
				{
					_mouseDown = MouseDownOnTriangle.Point3;
					_offset = this.Triangle.Vertex3 - localPos;
				}
				else if (Utility3D.GetDistance3D(localPos, this.RotateHandle) <= CLICKDISTANCE)
				{
					_mouseDown = MouseDownOnTriangle.Rotate;
					_offset = this.RotateHandle - localPos;
				}
				else if (localPos.GetMagnitudeSquared() <= this.Radius * this.Radius)		//	I want to do this last
				{
					_mouseDown = MouseDownOnTriangle.Ball;
					_offset = this.Position - mousePos;
				}
				else
				{
					retVal = false;
				}

				return retVal;
			}
            public void OnMouseMove(MyVector mousePos)
			{
                MyVector localPos = mousePos - this.Position;

				switch (_mouseDown)
				{
					case MouseDownOnTriangle.Ball:
						this.Position.StoreNewValues(mousePos + _offset);
						break;

					case MouseDownOnTriangle.Point1:
						double z1 = this.Triangle.Vertex1.Z;
						this.Triangle.Vertex1.StoreNewValues(this.Rotation.GetRotatedVectorReverse(localPos, true));
						this.Triangle.Vertex1.Z = z1;
						break;

					case MouseDownOnTriangle.Point2:
						double z2 = this.Triangle.Vertex2.Z;
						this.Triangle.Vertex2.StoreNewValues(this.Rotation.GetRotatedVectorReverse(localPos, true));
						this.Triangle.Vertex2.Z = z2;
						break;

					case MouseDownOnTriangle.Point3:
						double z3 = this.Triangle.Vertex3.Z;
						this.Triangle.Vertex3.StoreNewValues(this.Rotation.GetRotatedVectorReverse(localPos, true));
						this.Triangle.Vertex3.Z = z3;
						break;

					case MouseDownOnTriangle.Rotate:
                        MyQuaternion newRotation;
						_rotateHandle.GetAngleAroundAxis(out newRotation, localPos);
						this.Rotation.StoreNewValues(newRotation);
						break;
				}
			}
			public void OnMouseUp()
			{
				_mouseDown = MouseDownOnTriangle.Nothing;
			}

			public void Draw(LargeMapViewer2D viewer, Color backColor)
			{
				const double DOTRADIUS = 35;
				const int ALPHA = 64;

                MyVector point1 = this.Position + this.Vertex1;
                MyVector point2 = this.Position + this.Vertex2;
                MyVector point3 = this.Position + this.Vertex3;

				//	Draw Triangle
				viewer.FillTriangle(backColor, point1, point2, point3);
				viewer.DrawTriangle(Color.Black, 1, point1, point2, point3);

				//	Dot Colors
				Color color1 = point1.Z < -1 ? Color.Black : point1.Z > 1 ? Color.White : Color.MediumPurple;
				Color color2 = point2.Z < -1 ? Color.Black : point2.Z > 1 ? Color.White : Color.MediumPurple;
				Color color3 = point3.Z < -1 ? Color.Black : point3.Z > 1 ? Color.White : Color.MediumPurple;
				Color colorRotate = Color.DarkSeaGreen;

				switch (_mouseDown)
				{
					case MouseDownOnTriangle.Nothing:
					case MouseDownOnTriangle.Ball:
						break;

					case MouseDownOnTriangle.Point1:
						color1 = Color.Silver;
						break;

					case MouseDownOnTriangle.Point2:
						color2 = Color.Silver;
						break;

					case MouseDownOnTriangle.Point3:
						color3 = Color.Silver;
						break;

					case MouseDownOnTriangle.Rotate:
						colorRotate = Color.Chartreuse;
						break;

					default:
						throw new ApplicationException("Unknown MouseDownOnTriangle: " + _mouseDown.ToString());
				}

				color1 = Color.FromArgb(ALPHA, color1);
				color2 = Color.FromArgb(ALPHA, color2);
				color3 = Color.FromArgb(ALPHA, color3);
				colorRotate = Color.FromArgb(ALPHA, colorRotate);
				Color circleColor = Color.FromArgb(ALPHA, Color.Black);

				//	Draw Dots
				viewer.FillCircle(color1, point1, DOTRADIUS);
				viewer.DrawCircle(circleColor, .5d, point1, DOTRADIUS);
				viewer.DrawString("1", _font, _fontBrush, point1, ContentAlignment.MiddleCenter);

				viewer.FillCircle(color2, point2, DOTRADIUS);
				viewer.DrawCircle(circleColor, .5d, point2, DOTRADIUS);
				viewer.DrawString("2", _font, _fontBrush, point2, ContentAlignment.MiddleCenter);

				viewer.FillCircle(color3, point3, DOTRADIUS);
				viewer.DrawCircle(circleColor, .5d, point3, DOTRADIUS);
				viewer.DrawString("3", _font, _fontBrush, point3, ContentAlignment.MiddleCenter);

                MyVector pointRotate = this.Position + this.Rotation.GetRotatedVector(_rotateHandle, true);

				viewer.FillCircle(colorRotate, pointRotate, DOTRADIUS);
				viewer.DrawCircle(circleColor, .5d, pointRotate, DOTRADIUS);
			}

			#endregion
		}

		#endregion
		#region Class: PolygonTest

		private class PolygonTest : SpherePolygon
		{

			#region Enum: MouseDownOnPolygon

			private enum MouseDownOnPolygon
			{
				Nothing = 0,
				Ball,
				RotateX,
				RotateY,
				RotateZ,
			}

			#endregion
			#region Enum: RotateHandle

			private enum RotateHandle
			{
				X = 0,
				Y,
				Z
			}

			#endregion

			#region Declaration Section

			private const double DOTRADIUS = 35;

			//	These are the length of the vectors.  Access them through the corresponding public properties to get the
			//	rotated versions
            private MyVector _rotateHandleX = null;
            private MyVector _rotateHandleY = null;
            private MyVector _rotateHandleZ = null;
			//	These only rotate around the Z axis, but they line up with the rotate handles (on screen, the handles rotate
			//	in 2D, but they cause the 3D object to rotate around the corresponding axis)
			private MyQuaternion _rotationX = null;
            private MyQuaternion _rotationY = null;
            private MyQuaternion _rotationZ = null;

            private MyVector _offset = null;
			private MouseDownOnPolygon _mouseDown = MouseDownOnPolygon.Nothing;

			private Font _font = new Font("Arial", 8);
			private Brush _fontBrush = new SolidBrush(Color.White);

			#endregion

			#region Constructor

            public PolygonTest(MyVector position, DoubleVector origDirFacing, MyPolygon polygon, double radius)
				: base(position, origDirFacing, polygon, radius)
			{
                _rotateHandleX = new MyVector(this.Radius + DOTRADIUS, 0, 0);
                _rotateHandleY = new MyVector(this.Radius + (DOTRADIUS * 3), 0, 0);
                _rotateHandleZ = new MyVector(this.Radius + (DOTRADIUS * 5), 0, 0);
                _rotationX = new MyQuaternion(new MyVector(0, 0, 1), 0);
                _rotationY = new MyQuaternion(new MyVector(0, 0, 1), 0);
                _rotationZ = new MyQuaternion(new MyVector(0, 0, 1), 0);
			}

			#endregion

			#region Public Properties

            public MyVector RotateHandleX
			{
				get
				{
					return _rotationX.GetRotatedVector(_rotateHandleX, true);
				}
			}
            public MyVector RotateHandleY
			{
				get
				{
					return _rotationY.GetRotatedVector(_rotateHandleY, true);
				}
			}
            public MyVector RotateHandleZ
			{
				get
				{
					return _rotationZ.GetRotatedVector(_rotateHandleZ, true);
				}
			}

			#endregion

			#region Public Methods

			/// <summary>
			/// This will return true if it can hangle the click.  Otherwise it returns false
			/// </summary>
            public bool OnMouseDown(MyVector mousePos)
			{
				bool retVal = true;
                MyVector localPos = mousePos - this.Position;

				if (Utility3D.GetDistance3D(localPos, this.RotateHandleX) <= DOTRADIUS)
				{
					_mouseDown = MouseDownOnPolygon.RotateX;
					_offset = this.RotateHandleX - localPos;
				}
				else if (Utility3D.GetDistance3D(localPos, this.RotateHandleY) <= DOTRADIUS)
				{
					_mouseDown = MouseDownOnPolygon.RotateY;
					_offset = this.RotateHandleY - localPos;
				}
				else if (Utility3D.GetDistance3D(localPos, this.RotateHandleZ) <= DOTRADIUS)
				{
					_mouseDown = MouseDownOnPolygon.RotateZ;
					_offset = this.RotateHandleZ - localPos;
				}
				else if (localPos.GetMagnitudeSquared() <= this.Radius * this.Radius)		//	I want to do this last
				{
					_mouseDown = MouseDownOnPolygon.Ball;
					_offset = this.Position - mousePos;
				}
				else
				{
					retVal = false;
				}

				return retVal;
			}
            public void OnMouseMove(MyVector mousePos)
			{
                MyVector localPos = mousePos - this.Position;

				switch (_mouseDown)
				{
					case MouseDownOnPolygon.Ball:
						this.Position.StoreNewValues(mousePos + _offset);
						break;

					case MouseDownOnPolygon.RotateX:
                        RotateMe(localPos, _rotateHandleX, _rotationX, new MyVector(1, 0, 0));
						break;

					case MouseDownOnPolygon.RotateY:
                        RotateMe(localPos, _rotateHandleY, _rotationY, new MyVector(0, 1, 0));
						break;

					case MouseDownOnPolygon.RotateZ:
                        RotateMe(localPos, _rotateHandleZ, _rotationZ, new MyVector(0, 0, 1));
						break;
				}
			}
			public void OnMouseUp()
			{
				_mouseDown = MouseDownOnPolygon.Nothing;
			}

			public void Draw(LargeMapViewer2D viewer, Color backColor1, Color backColor2)
			{
				const int ALPHA = 64;
				const double AXISMULT = 1.5d;
				const double AXISWIDTH = 5;

				//	Draw Axis
                viewer.DrawLine(Color.Red, AXISWIDTH, this.Position, this.Position + this.Rotation.GetRotatedVector(new MyVector(this.Radius * AXISMULT, 0, 0), true));
                viewer.DrawLine(Color.Green, AXISWIDTH, this.Position, this.Position + this.Rotation.GetRotatedVector(new MyVector(0, this.Radius * AXISMULT, 0), true));
                viewer.DrawLine(Color.Blue, AXISWIDTH, this.Position, this.Position + this.Rotation.GetRotatedVector(new MyVector(0, 0, this.Radius * AXISMULT), true));

				//	Draw Polygon
				viewer.FillPolygon(backColor1, backColor2, this.Position, this);

				//	Dot Colors
				Color colorRotateX = Color.Red;
				Color colorRotateY = Color.Green;
				Color colorRotateZ = Color.Blue;

				switch (_mouseDown)
				{
					case MouseDownOnPolygon.Nothing:
					case MouseDownOnPolygon.Ball:
						break;

					case MouseDownOnPolygon.RotateX:
						colorRotateX = Color.HotPink;
						break;

					case MouseDownOnPolygon.RotateY:
						colorRotateY = Color.Chartreuse;
						break;

					case MouseDownOnPolygon.RotateZ:
						colorRotateZ = Color.LightBlue;
						break;

					default:
						throw new ApplicationException("Unknown MouseDownOnTriangle: " + _mouseDown.ToString());
				}

				colorRotateX = Color.FromArgb(ALPHA, colorRotateX);
				colorRotateY = Color.FromArgb(ALPHA, colorRotateY);
				colorRotateZ = Color.FromArgb(ALPHA, colorRotateZ);
				Color circleColor = Color.FromArgb(ALPHA, Color.Black);

				//	Draw Dots
                MyVector point = this.Position + this.RotateHandleX;
				viewer.FillCircle(colorRotateX, point, DOTRADIUS);
				viewer.DrawCircle(circleColor, .5d, point, DOTRADIUS);
				viewer.DrawString("X", _font, _fontBrush, point, ContentAlignment.MiddleCenter);

				point = this.Position + this.RotateHandleY;
				viewer.FillCircle(colorRotateY, point, DOTRADIUS);
				viewer.DrawCircle(circleColor, .5d, point, DOTRADIUS);
				viewer.DrawString("Y", _font, _fontBrush, point, ContentAlignment.MiddleCenter);

				point = this.Position + this.RotateHandleZ;
				viewer.FillCircle(colorRotateZ, point, DOTRADIUS);
				viewer.DrawCircle(circleColor, .5d, point, DOTRADIUS);
				viewer.DrawString("Z", _font, _fontBrush, point, ContentAlignment.MiddleCenter);
			}

			#endregion


			

			#region download

			/*


			//	It looks like his definition of a polygon is simply a list of vectors.  It doesn't appear to be made of triangles,
			//	but arbitrary sided faces.
			//
			//	Actually, the more I think about it, I think is definition of a poly is strictly 2D.  This should be replaced with
			//	triangle
			//typedef std::vector<Vector3> poly_t;



			//	This function should probably be moved to triangle (assuming it's not the same as GetClosestPointOnTriangle())
			private bool PointInPoly(MyVector p, poly_t v, Vector n)
			{
				for (int i = 0; i < v.size(); i++)
				{
					if (Vector.Dot(p - v[i], Vector.Cross(n, v[(i + 1) % v.size()] - v[i]), false) < 0d)
					{
						return false;
					}
				}

				return true;
			}

			private Vector ClosestPointOnPlane(Vector p, Vector n, float d)
			{
				return p - (n * (Vector.Dot(p, n, false) - d));
			}

			private Vector ClosestPointOnSegment(Vector p, Vector p1, Vector p2)
			{
				Vector dir = p2 - p1;
				Vector diff = p - p1;

				double t = Vector.Dot(diff, dir, false) / Vector.Dot(dir, dir, false);
				if (t <= 0.0f)
				{
					return p1;
				}
				else if (t >= 1.0f)
				{
					return p2;
				}

				return p1 + t * dir;
			}

Vector3 ClosestPointOnPoly(const Vector3& p, const poly_t& v)
{
    // Poly plane
    Vector3 n = Vector3::Normalize(Vector3::Cross(v[1] - v[0], v[2] - v[0]));
    float d = v[0].Dot(n);
	
    // Closest point on plane to p
    Vector3 closest = ClosestPointOnPlane(p, n, d);
	
    // If p is in the poly, we've found our closest point
    if (PointInPoly(closest, v, n))
        return closest;
		
    // Else find the closest point to a poly edge
    bool found = false;
    float minDist;
    for (int i = 0; i < v.size(); ++i)
    {
        Vector3 temp = ClosestPointOnSegment(p, v[i], v[(i + 1) % v.size()]);
        float dist = Vector3::LengthSquared(p - temp);
        if (!found || dist < minDist)
        {
            found = true;
            minDist = dist;
            closest = temp;
        }
    }
    return closest;
}

bool IntersectSpherePoly(const Vector3& c, float r, const poly_t& poly, Vector3& n, float& d)
{
    Vector3 p = ClosestPointOnPoly(c, poly);
    n = c - p;
    float dist = n.LengthSquared();
    if (dist > r * r)
        return false;
		
    dist = Math::Sqrt(dist);
    n /= dist;
    d = r - dist;
    return true;
}







*/

			#endregion


            public static MyVector[] TestCollision(out MyVector trueCollision, Sphere sphere, MyVector polygonCenterPoint, IMyPolygon polygon, Sphere polygonSphere)
			{
                MyVector[] retVal = IsIntersecting_SpherePolygon(sphere, polygonCenterPoint, polygon, polygonSphere);

				if (retVal == null)
				{
					trueCollision = null;
					return null;
				}

				//	Find the closest point
				double minDist = double.MaxValue;
				int minDistIndex = -1;

				for (int returnCntr = 0; returnCntr < retVal.Length; returnCntr++)
				{
					double curDist = Utility3D.GetDistance3D(sphere.Position, retVal[returnCntr]);
					if (curDist < minDist)
					{
						minDist = curDist;
						minDistIndex = returnCntr;
					}
				}

				trueCollision = retVal[minDistIndex];
				return retVal;
			}


			/// <summary>
			/// This overload will do a sphere/sphere check first, and if those intersect, then it will do the more
			/// expensive sphere/poly check
			/// </summary>
			/// <param name="polygonSphere">A sphere that totally surrounds the polygon</param>
            public static MyVector[] IsIntersecting_SpherePolygon(Sphere sphere, MyVector polygonCenterPoint, IMyPolygon polygon, Sphere polygonSphere)
			{
				//	Do a sphere/sphere check first
				if (!CollisionHandler.IsIntersecting_SphereSphere_Bool(sphere, polygonSphere))
				{
					return null;
				}

				//	The spheres intersect.  Now do the sphere/poly check
				return IsIntersecting_SpherePolygon(sphere, polygonCenterPoint, polygon);
			}
			/// <summary>
			/// This overload compares the sphere and polygon (no up front sphere/sphere check)
			/// </summary>
            public static MyVector[] IsIntersecting_SpherePolygon(Sphere sphere, MyVector polygonCenterPoint, IMyPolygon polygon)
			{
                List<MyVector> retVal = new List<MyVector>();

				//	See if I need to recurse on the polygon's children
				if (polygon.HasChildren)
				{
					#region Test Child Polys

                    MyVector[] curCollisions;

					//	Call myself for each of the child polys
                    foreach (IMyPolygon childPoly in polygon.ChildPolygons)
					{
						curCollisions = IsIntersecting_SpherePolygon(sphere, polygonCenterPoint, childPoly);

						if (curCollisions != null)
						{
							retVal.AddRange(curCollisions);
						}
					}

					if (retVal.Count > 0)
					{
						return retVal.ToArray();
					}
					else
					{
						return null;
					}

					#endregion
				}

				#region Test Edges

				//	Compare the sphere with the edges
                MyVector curCollision;
				foreach (Triangle triangle in polygon.Triangles)
				{
					curCollision = CollisionHandler.IsIntersecting_SphereTriangle(sphere, polygonCenterPoint + triangle);

					if (curCollision != null)
					{
						retVal.Add(curCollision);
					}
				}

				if (retVal.Count > 0)
				{
					return retVal.ToArray();
				}

				#endregion



				//TODO:  collide the sphere with the interior of the poly



				//	Exit Function
				return null;
			}





			#region Private Methods

            private void RotateMe(MyVector localPos, MyVector rotateHandle, MyQuaternion rotation2D, MyVector rotateAround3D)
			{
				//	Get Current Angle
                double curRadians = MyVector.GetAngleBetweenVectors(rotateHandle, rotation2D.GetRotatedVector(rotateHandle, true));

				//	Rotate the 2D Handle
                MyQuaternion newRotationX;
				rotateHandle.GetAngleAroundAxis(out newRotationX, localPos);
				rotation2D.StoreNewValues(newRotationX);

				//	Get New Angle
                double newRadians = MyVector.GetAngleBetweenVectors(rotateHandle, rotation2D.GetRotatedVector(rotateHandle, true));

				double rotateRadians = newRadians - curRadians;
				if (rotation2D.GetRotatedVector(rotateHandle, true).Y < 0) rotateRadians *= -1;		//	This statement is cheating.  I'm using the fact that the rotate handles all lie on the XY plane

				//	Apply the difference to the 3D object (me)
				this.RotateAroundAxis(this.Rotation.GetRotatedVector(rotateAround3D, true), rotateRadians);
			}

			#endregion

		}

		#endregion

		#region Declaration Section

		private const int PAN = 100;

		//	Standard Collision Objects
		private Ball _ball1 = null;
		private Ball _ball2 = null;

		//	Local Collision Objects
		private TriangleTest _triangle1 = null;
		private TriangleTest _triangle2 = null;		//	2 is actually on the left, since 1 started out as the one on the right (2 is only used during triangle-triangle collisions)
		private PolygonTest _polygon1 = null;
		private PolygonTest _polygon2 = null;		//	2 is on the left for polygons too

		//private LargeMapViewer2D _viewer = null;

        private MyVector _boundryLower = new MyVector(-3000, -3000, 0);
        private MyVector _boundryUpper = new MyVector(3000, 3000, 0);

		CollisionHandler _collider = new CollisionHandler();

		#region Mouse

		private MouseDownOn _mouseDownOn;
		//	These are in world coords
        private MyVector _mouseDownPoint = new MyVector();		//	this is the point at the time of mousedown
        private MyVector _curMousePoint = new MyVector();
        private MyVector _offset = new MyVector();		//	this is the offset from the center of the object that they clicked

		#endregion

		#endregion

		#region Constructor

		public CollisionTester()
		{
			InitializeComponent();

			//	Balls
            _ball1 = new Ball(new MyVector(-500, 0, 0), new DoubleVector(0, 1, 0, 1, 0, 0), 150, 150, 1, 1, 1, _boundryLower, _boundryUpper);
			_ball1.Velocity.X = 200;
			_ball1.Velocity.Y = 50;

            _ball2 = new Ball(new MyVector(500, 0, 0), new DoubleVector(0, 1, 0, 1, 0, 0), 150, 150, 1, 1, 1, _boundryLower, _boundryUpper);
			_ball2.Velocity.X = -200;
			_ball2.Velocity.Y = -50;

			//	Viewer
            pictureBox1.SetBorder(_boundryLower, _boundryUpper);
            pictureBox1.ShowBorder(Color.LightSteelBlue, 10d);

			//	Raise events
			pictureBox1_Resize(this, new EventArgs());

			timer1.Enabled = true;
		}

		#endregion

		#region Misc Control Events

		private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
		{
			const double VELOCITYCLICKDISTANCE = 35;

            MyVector clickPoint = pictureBox1.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));

            if (e.Button == MouseButtons.Left)
            {
                if (radBallBall.Checked || radSolidBallSolidBall.Checked || radSphereSphere.Checked)
                {
                    #region Balls

                    if (_ball1 != null && Utility3D.GetDistance3D(clickPoint, GetVelocityEnd(_ball1)) <= VELOCITYCLICKDISTANCE)
                    {
                        _mouseDownOn = MouseDownOn.Ball1Velocity;
                        _offset = _ball1.Velocity - clickPoint;
                    }
                    else if (_ball2 != null && Utility3D.GetDistance3D(clickPoint, GetVelocityEnd(_ball2)) <= VELOCITYCLICKDISTANCE)
                    {
                        _mouseDownOn = MouseDownOn.Ball2Velocity;
                        _offset = _ball2.Velocity - clickPoint;
                    }
                    else if (_ball1 != null && Utility3D.GetDistance3D(clickPoint, _ball1.Position) <= _ball1.Radius)
                    {
                        _mouseDownOn = MouseDownOn.Ball1;
                        _offset = _ball1.Position - clickPoint;
                    }
                    else if (_ball2 != null && Utility3D.GetDistance3D(clickPoint, _ball2.Position) <= _ball2.Radius)
                    {
                        _mouseDownOn = MouseDownOn.Ball2;
                        _offset = _ball2.Position - clickPoint;
                    }
                    else
                    {
                        return;
                    }
                    #endregion
                }
                else if (radLineTriangle.Checked || radSphereTriangle.Checked)
                {
                    #region 1 Triangle

                    if (_ball1 != null && Utility3D.GetDistance3D(clickPoint, GetVelocityEnd(_ball1)) <= VELOCITYCLICKDISTANCE)
                    {
                        _mouseDownOn = MouseDownOn.Ball1Velocity;
                        _offset = _ball1.Velocity - clickPoint;
                    }
                    else if (_ball1 != null && Utility3D.GetDistance3D(clickPoint, _ball1.Position) <= _ball1.Radius)
                    {
                        _mouseDownOn = MouseDownOn.Ball1;
                        _offset = _ball1.Position - clickPoint;
                    }
                    else if (_triangle1 != null && _triangle1.OnMouseDown(clickPoint))
                    {
                        _mouseDownOn = MouseDownOn.Triangle1;		//	no need to set the offset.  the triangle takes care of everything internally
                    }
                    else
                    {
                        return;
                    }

                    #endregion
                }
                else if (radTriangleTriangle.Checked)
                {
                    #region Triangles

                    if (_triangle1 != null && _triangle1.OnMouseDown(clickPoint))
                    {
                        _mouseDownOn = MouseDownOn.Triangle1;		//	no need to set the offset.  the triangle takes care of everything internally
                    }
                    else if (_triangle2 != null && _triangle2.OnMouseDown(clickPoint))
                    {
                        _mouseDownOn = MouseDownOn.Triangle2;		//	no need to set the offset.  the triangle takes care of everything internally
                    }
                    else
                    {
                        return;
                    }

                    #endregion
                }
                else if (radSpherePolygon.Checked)
                {
                    #region 1 Polygon

                    if (_ball1 != null && Utility3D.GetDistance3D(clickPoint, _ball1.Position) <= _ball1.Radius)
                    {
                        _mouseDownOn = MouseDownOn.Ball1;
                        _offset = _ball1.Position - clickPoint;
                    }
                    else if (_polygon1 != null && _polygon1.OnMouseDown(clickPoint))
                    {
                        _mouseDownOn = MouseDownOn.Polygon1;		//	no need to set the offset.  the polygon takes care of everything internally
                    }
                    else
                    {
                        return;
                    }

                    #endregion
                }
                else if (radPolygonPolygon.Checked)
                {
                    #region Polygons

                    if (_polygon1 != null && _polygon1.OnMouseDown(clickPoint))
                    {
                        _mouseDownOn = MouseDownOn.Polygon1;		//	no need to set the offset.  the polygon takes care of everything internally
                    }
                    else if (_polygon2 != null && _polygon2.OnMouseDown(clickPoint))
                    {
                        _mouseDownOn = MouseDownOn.Polygon2;		//	no need to set the offset.  the polygon takes care of everything internally
                    }
                    else
                    {
                        return;
                    }

                    #endregion
                }
            }

			_mouseDownPoint = clickPoint;

			_curMousePoint = clickPoint.Clone();

            // If they are dragging something, then don't pan (gets reset on mouse up)
            if (_mouseDownOn != MouseDownOn.Nothing)
            {
                pictureBox1.PanMouseButton = MouseButtons.None;
            }
		}
		private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
		{
			_mouseDownOn = MouseDownOn.Nothing;
            pictureBox1.PanMouseButton = MouseButtons.Left;    // this might have been disabled in mouse down

			if (_triangle1 != null)
			{
				_triangle1.OnMouseUp();
			}

			if (_triangle2 != null)
			{
				_triangle2.OnMouseUp();
			}

			if (_polygon1 != null)
			{
				_polygon1.OnMouseUp();
			}

			if (_polygon2 != null)
			{
				_polygon2.OnMouseUp();
			}
		}
		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
            MyVector mousePoint = pictureBox1.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));

			switch (_mouseDownOn)
			{
				case MouseDownOn.Ball1:
					_ball1.Position.StoreNewValues(mousePoint + _offset);
					break;

				case MouseDownOn.Ball1Velocity:
					_ball1.Velocity.StoreNewValues(mousePoint + _offset);
					break;

				case MouseDownOn.Ball2:
					_ball2.Position.StoreNewValues(mousePoint + _offset);
					break;

				case MouseDownOn.Ball2Velocity:
					_ball2.Velocity.StoreNewValues(mousePoint + _offset);
					break;

				case MouseDownOn.Triangle1:
					_triangle1.OnMouseMove(mousePoint);
					break;

				case MouseDownOn.Triangle2:
					_triangle2.OnMouseMove(mousePoint);
					break;

				case MouseDownOn.Polygon1:
					_polygon1.OnMouseMove(mousePoint);
					break;

				case MouseDownOn.Polygon2:
					_polygon2.OnMouseMove(mousePoint);
					break;
			}

			_curMousePoint.StoreNewValues(mousePoint);
		}

		private void pictureBox1_Resize(object sender, EventArgs e)
		{
			btnZoomFit_Click(this, new EventArgs());
		}

		private void radBallType_CheckedChanged(object sender, EventArgs e)
		{
			grpTriangleZ.Visible = false;
			grpTriangleZ2.Visible = false;
			_triangle1 = null;
			_triangle2 = null;
			_polygon1 = null;
			_polygon2 = null;

			if (radBallBall.Checked || radSolidBallSolidBall.Checked)
			{
				#region Balls

				Ball newBall1, newBall2;

				if (radBallBall.Checked)
				{
					//	Switch them out with a standard ball
					newBall1 = new Ball(_ball1.Position.Clone(), new DoubleVector(0, 1, 0, 1, 0, 0), _ball1.Radius, _ball1.Mass, _ball1.Elasticity, _ball1.KineticFriction, _ball1.StaticFriction, _boundryLower, _boundryUpper);
					newBall1.Velocity.StoreNewValues(_ball1.Velocity.Clone());

					newBall2 = new Ball(_ball2.Position.Clone(), new DoubleVector(0, 1, 0, 1, 0, 0), _ball2.Radius, _ball2.Mass, _ball2.Elasticity, _ball2.KineticFriction, _ball2.StaticFriction, _boundryLower, _boundryUpper);
					newBall2.Velocity.StoreNewValues(_ball2.Velocity.Clone());
				}
				else if (radSolidBallSolidBall.Checked)
				{
					//	Switch them out with solidballs
					newBall1 = new SolidBall(_ball1.Position.Clone(), new DoubleVector(0, 1, 0, 1, 0, 0), _ball1.Radius, _ball1.Mass, _ball1.Elasticity, _ball1.KineticFriction, _ball1.StaticFriction, _boundryLower, _boundryUpper);
					newBall1.Velocity.StoreNewValues(_ball1.Velocity.Clone());
                    ((SolidBall)newBall1).AngularVelocity.StoreNewValues(new MyVector(0, 0, 200));

					newBall2 = new SolidBall(_ball2.Position.Clone(), new DoubleVector(0, 1, 0, 1, 0, 0), _ball2.Radius, _ball2.Mass, _ball2.Elasticity, _ball2.KineticFriction, _ball2.StaticFriction, _boundryLower, _boundryUpper);
					newBall2.Velocity.StoreNewValues(_ball2.Velocity.Clone());
                    ((SolidBall)newBall2).AngularVelocity.StoreNewValues(new MyVector(0, 0, 200));
				}
				else
				{
					MessageBox.Show("Unknown radio button", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				_ball1 = newBall1;
				_ball2 = newBall2;

				#endregion
			}
			else if (radSphereSphere.Checked)
			{
				#region Spheres

				//	leave the balls alone?

				#endregion
			}
			else if (radLineTriangle.Checked || radSphereTriangle.Checked || radTriangleTriangle.Checked)
			{
				#region Triangles

				if (radLineTriangle.Checked)
				{
					#region Line Triangle

					//	I will leave ball1 alone.  I will use that one's velocity as the line

					grpTriangleZ.Visible = true;

					//	Make the the triangle
					if (chkTrianglePerpendicular.Checked)
					{
						_triangle1 = new TriangleTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(0, 0, -100, 0, 100, 0, 0, -100, 100));

						radPoint1Neg.Enabled = radPoint1Pos.Enabled = radPoint1Zero.Enabled = false;
						radPoint2Neg.Enabled = radPoint2Pos.Enabled = radPoint2Zero.Enabled = false;
						radPoint3Neg.Enabled = radPoint3Pos.Enabled = radPoint3Zero.Enabled = false;
					}
					else
					{
						_triangle1 = new TriangleTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(-150, -150, 0, 150, -150, 0, 0, 150, 0));

						radPoint1Neg.Enabled = radPoint1Pos.Enabled = radPoint1Zero.Enabled = true;
						radPoint2Neg.Enabled = radPoint2Pos.Enabled = radPoint2Zero.Enabled = true;
						radPoint3Neg.Enabled = radPoint3Pos.Enabled = radPoint3Zero.Enabled = true;

						radPoint1_CheckedChanged(this, new EventArgs());
						radPoint2_CheckedChanged(this, new EventArgs());
						radPoint3_CheckedChanged(this, new EventArgs());
					}

					#endregion
				}
				else if (radSphereTriangle.Checked)
				{
					#region Sphere Triangle

					//	I will leave ball1 alone.  I won't use its velocity, just radius

					grpTriangleZ.Visible = true;

					//	Make the the triangle
					if (chkTrianglePerpendicular.Checked)
					{
						_triangle1 = new TriangleTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(0, 0, -100, 0, 100, 0, 0, -100, 100));

						radPoint1Neg.Enabled = radPoint1Pos.Enabled = radPoint1Zero.Enabled = false;
						radPoint2Neg.Enabled = radPoint2Pos.Enabled = radPoint2Zero.Enabled = false;
						radPoint3Neg.Enabled = radPoint3Pos.Enabled = radPoint3Zero.Enabled = false;
					}
					else
					{
						_triangle1 = new TriangleTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(-150, -150, 0, 150, -150, 0, 0, 150, 0));

						radPoint1Neg.Enabled = radPoint1Pos.Enabled = radPoint1Zero.Enabled = true;
						radPoint2Neg.Enabled = radPoint2Pos.Enabled = radPoint2Zero.Enabled = true;
						radPoint3Neg.Enabled = radPoint3Pos.Enabled = radPoint3Zero.Enabled = true;

						radPoint1_CheckedChanged(this, new EventArgs());
						radPoint2_CheckedChanged(this, new EventArgs());
						radPoint3_CheckedChanged(this, new EventArgs());
					}

					#endregion
				}
				else if (radTriangleTriangle.Checked)
				{
					#region Triangle Triangle

					grpTriangleZ.Visible = true;
					grpTriangleZ2.Visible = true;

					#region Triangle1 (right side)

					//	Make the the triangle
					if (chkTrianglePerpendicular.Checked)
					{
						_triangle1 = new TriangleTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(0, 0, -100, 0, 100, 0, 0, -100, 100));

						radPoint1Neg.Enabled = radPoint1Pos.Enabled = radPoint1Zero.Enabled = false;
						radPoint2Neg.Enabled = radPoint2Pos.Enabled = radPoint2Zero.Enabled = false;
						radPoint3Neg.Enabled = radPoint3Pos.Enabled = radPoint3Zero.Enabled = false;
					}
					else
					{
						_triangle1 = new TriangleTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(-150, -150, 0, 150, -150, 0, 0, 150, 0));

						radPoint1Neg.Enabled = radPoint1Pos.Enabled = radPoint1Zero.Enabled = true;
						radPoint2Neg.Enabled = radPoint2Pos.Enabled = radPoint2Zero.Enabled = true;
						radPoint3Neg.Enabled = radPoint3Pos.Enabled = radPoint3Zero.Enabled = true;

						radPoint1_CheckedChanged(this, new EventArgs());
						radPoint2_CheckedChanged(this, new EventArgs());
						radPoint3_CheckedChanged(this, new EventArgs());
					}

					#endregion
					#region Triangle2 (left side)

					//	Make the the triangle
					if (chkTrianglePerpendicular2.Checked)
					{
						_triangle2 = new TriangleTest(_ball1.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(0, 0, -100, 0, 100, 0, 0, -100, 100));

						radPoint1Neg2.Enabled = radPoint1Pos2.Enabled = radPoint1Zero2.Enabled = false;
						radPoint2Neg2.Enabled = radPoint2Pos2.Enabled = radPoint2Zero2.Enabled = false;
						radPoint3Neg2.Enabled = radPoint3Pos2.Enabled = radPoint3Zero2.Enabled = false;
					}
					else
					{
						_triangle2 = new TriangleTest(_ball1.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), new Triangle(-150, -150, 0, 150, -150, 0, 0, 150, 0));

						radPoint1Neg2.Enabled = radPoint1Pos2.Enabled = radPoint1Zero2.Enabled = true;
						radPoint2Neg2.Enabled = radPoint2Pos2.Enabled = radPoint2Zero2.Enabled = true;
						radPoint3Neg2.Enabled = radPoint3Pos2.Enabled = radPoint3Zero2.Enabled = true;

						radPoint1Changed2_CheckedChanged(this, new EventArgs());
						radPoint2Changed2_CheckedChanged(this, new EventArgs());
						radPoint3Changed2_CheckedChanged(this, new EventArgs());
					}

					#endregion

					#endregion
				}
				else
				{
					MessageBox.Show("Unknown radio button", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				#endregion
			}
			else
			{
				#region Polygons

				if (radSpherePolygon.Checked)
				{
					#region Sphere Polygon

					//	I will leave ball1 alone.  I won't use its velocity, just radius

					//	Make the the polygon
					//_polygon1 = new PolygonTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), Polygon.CreateTetrahedron(_ball2.Radius * 2, true), _ball2.Radius * 2);
					_polygon1 = new PolygonTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), MyPolygon.CreateCube(_ball2.Radius * 5, true), _ball2.Radius * 4);

					#endregion
				}
				else if (radPolygonPolygon.Checked)
				{
					#region Polygon Polygon

					//	Polygon1 is on the right (so it uses ball2 as its source)
					_polygon1 = new PolygonTest(_ball2.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), MyPolygon.CreateTetrahedron(_ball2.Radius * 2, true), _ball2.Radius * 2);
                    _polygon2 = new PolygonTest(_ball1.Position.Clone(), new DoubleVector(1, 0, 0, 0, 1, 0), MyPolygon.CreateCube(_ball1.Radius * 2, true), _ball2.Radius * 2);

					#endregion
				}
				else
				{
					MessageBox.Show("Unknown radio button", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				#endregion
			}
		}

		#region Triangle GroupBox Events

		#region Triangle1

		private void chkLargeZ_CheckedChanged(object sender, EventArgs e)
		{
			if (chkLargeZ.Checked)
			{
				radPoint1Neg.Text = "-250";
				radPoint1Pos.Text = "250";

				radPoint2Neg.Text = "-250";
				radPoint2Pos.Text = "250";

				radPoint3Neg.Text = "-250";
				radPoint3Pos.Text = "250";
			}
			else
			{
				radPoint1Neg.Text = "-10";
				radPoint1Pos.Text = "10";

				radPoint2Neg.Text = "-10";
				radPoint2Pos.Text = "10";

				radPoint3Neg.Text = "-10";
				radPoint3Pos.Text = "10";
			}

			radPoint1_CheckedChanged(sender, new EventArgs());
			radPoint2_CheckedChanged(sender, new EventArgs());
			radPoint3_CheckedChanged(sender, new EventArgs());
		}

		private void radPoint1_CheckedChanged(object sender, EventArgs e)
		{
			if (_triangle1 == null)
			{
				return;
			}

			if(radPoint1Neg.Checked)
			{
				_triangle1.Triangle.Vertex1.Z = int.Parse(radPoint1Neg.Text);
			}
			else if(radPoint1Zero.Checked)
			{
				_triangle1.Triangle.Vertex1.Z = int.Parse(radPoint1Zero.Text);
			}
			else if (radPoint1Pos.Checked)
			{
				_triangle1.Triangle.Vertex1.Z = int.Parse(radPoint1Pos.Text);
			}
			else
			{
				MessageBox.Show("None of the radio buttons are pushed", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void radPoint2_CheckedChanged(object sender, EventArgs e)
		{
			if (_triangle1 == null)
			{
				return;
			}

			if (radPoint2Neg.Checked)
			{
				_triangle1.Triangle.Vertex2.Z = int.Parse(radPoint2Neg.Text);
			}
			else if (radPoint2Zero.Checked)
			{
				_triangle1.Triangle.Vertex2.Z = int.Parse(radPoint2Zero.Text);
			}
			else if (radPoint2Pos.Checked)
			{
				_triangle1.Triangle.Vertex2.Z = int.Parse(radPoint2Pos.Text);
			}
			else
			{
				MessageBox.Show("None of the radio buttons are pushed", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void radPoint3_CheckedChanged(object sender, EventArgs e)
		{
			if (_triangle1 == null)
			{
				return;
			}

			if (radPoint3Neg.Checked)
			{
				_triangle1.Triangle.Vertex3.Z = int.Parse(radPoint3Neg.Text);
			}
			else if (radPoint3Zero.Checked)
			{
				_triangle1.Triangle.Vertex3.Z = int.Parse(radPoint3Zero.Text);
			}
			else if (radPoint3Pos.Checked)
			{
				_triangle1.Triangle.Vertex3.Z = int.Parse(radPoint3Pos.Text);
			}
			else
			{
				MessageBox.Show("None of the radio buttons are pushed", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
		#region Triangle2

		private void chkLargeZ2_CheckedChanged(object sender, EventArgs e)
		{
			if (chkLargeZ2.Checked)
			{
				radPoint1Neg2.Text = "-250";
				radPoint1Pos2.Text = "250";

				radPoint2Neg2.Text = "-250";
				radPoint2Pos2.Text = "250";

				radPoint3Neg2.Text = "-250";
				radPoint3Pos2.Text = "250";
			}
			else
			{
				radPoint1Neg2.Text = "-10";
				radPoint1Pos2.Text = "10";

				radPoint2Neg2.Text = "-10";
				radPoint2Pos2.Text = "10";

				radPoint3Neg2.Text = "-10";
				radPoint3Pos2.Text = "10";
			}

			radPoint1Changed2_CheckedChanged(sender, new EventArgs());
			radPoint2Changed2_CheckedChanged(sender, new EventArgs());
			radPoint3Changed2_CheckedChanged(sender, new EventArgs());
		}

		private void radPoint1Changed2_CheckedChanged(object sender, EventArgs e)
		{
			if (_triangle2 == null)
			{
				return;
			}

			if (radPoint1Neg2.Checked)
			{
				_triangle2.Triangle.Vertex1.Z = int.Parse(radPoint1Neg2.Text);
			}
			else if (radPoint1Zero2.Checked)
			{
				_triangle2.Triangle.Vertex1.Z = int.Parse(radPoint1Zero2.Text);
			}
			else if (radPoint1Pos2.Checked)
			{
				_triangle2.Triangle.Vertex1.Z = int.Parse(radPoint1Pos2.Text);
			}
			else
			{
				MessageBox.Show("None of the radio buttons are pushed", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void radPoint2Changed2_CheckedChanged(object sender, EventArgs e)
		{
			if (_triangle2 == null)
			{
				return;
			}

			if (radPoint2Neg2.Checked)
			{
				_triangle2.Triangle.Vertex2.Z = int.Parse(radPoint2Neg2.Text);
			}
			else if (radPoint2Zero2.Checked)
			{
				_triangle2.Triangle.Vertex2.Z = int.Parse(radPoint2Zero2.Text);
			}
			else if (radPoint2Pos2.Checked)
			{
				_triangle2.Triangle.Vertex2.Z = int.Parse(radPoint2Pos2.Text);
			}
			else
			{
				MessageBox.Show("None of the radio buttons are pushed", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void radPoint3Changed2_CheckedChanged(object sender, EventArgs e)
		{
			if (_triangle2 == null)
			{
				return;
			}

			if (radPoint3Neg2.Checked)
			{
				_triangle2.Triangle.Vertex3.Z = int.Parse(radPoint3Neg2.Text);
			}
			else if (radPoint3Zero2.Checked)
			{
				_triangle2.Triangle.Vertex3.Z = int.Parse(radPoint3Zero2.Text);
			}
			else if (radPoint3Pos2.Checked)
			{
				_triangle2.Triangle.Vertex3.Z = int.Parse(radPoint3Pos2.Text);
			}
			else
			{
				MessageBox.Show("None of the radio buttons are pushed", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion

		#endregion

		private void btnPanUp_Click(object sender, EventArgs e)
		{
            pictureBox1.PanView(new MyVector(0, -PAN, 0), true, false);
		}
		private void btnPanDown_Click(object sender, EventArgs e)
		{
            pictureBox1.PanView(new MyVector(0, PAN, 0), true, false);
		}
		private void btnPanLeft_Click(object sender, EventArgs e)
		{
            pictureBox1.PanView(new MyVector(-PAN, 0, 0), true, false);
		}
		private void btnPanRight_Click(object sender, EventArgs e)
		{
            pictureBox1.PanView(new MyVector(PAN, 0, 0), true, false);
		}

		private void btnZoomIn_Click(object sender, EventArgs e)
		{
            pictureBox1.ZoomRelative(.5);
		}
		private void btnZoomOut_Click(object sender, EventArgs e)
		{
            pictureBox1.ZoomRelative(-.5);
		}
		private void btnZoomFit_Click(object sender, EventArgs e)
		{
            pictureBox1.ZoomSet(.33);
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (radBallBall.Checked || radSolidBallSolidBall.Checked)
			{
				TimerBalls();
			}
			else if (radLineTriangle.Checked)
			{
				TimerLineTriangle();
			}
			else if (radSphereTriangle.Checked)
			{
				TimerSphereTriangle();
			}
			else if (radTriangleTriangle.Checked)
			{
				TimerTriangleTriangle();
			}
			else if (radSphereSphere.Checked)
			{
				TimerSphereSphere();
			}
			else if (radSpherePolygon.Checked)
			{
				TimerSpherePolygon();
			}
			else if (radPolygonPolygon.Checked)
			{
				TimerPolygonPolygon();
			}

            //Application.DoEvents();
		}

		#endregion

		#region Private Methods

        private MyVector GetVelocityEnd(Ball ball)
		{
			return GetVelocityEnd(ball.Position, ball);
		}
        private MyVector GetVelocityEnd(MyVector position, Ball ball)
		{
			return position + ball.Velocity;
		}

        private MyVector GetAngularVelocityEnd(TorqueBall ball)
		{
			return GetAngularVelocityEnd(ball.Position, ball);
		}
        private MyVector GetAngularVelocityEnd(MyVector position, TorqueBall ball)
		{
			//	The angular velocity is comming out of the z axis.  I need to lay it down onto the XY plane.

			if (ball.AngularVelocity.IsZero)
			{
				return GetVelocityEnd(ball);
			}

            MyVector retVal = ball.AngularVelocity.Clone();

            retVal.RotateAroundAxis(new MyVector(1, 0, 0), Math.PI / 2d);

            double rotateAngle = MyVector.GetAngleBetweenVectors(new MyVector(1, 0, 0), ball.Velocity);
            if (MyVector.Dot(new MyVector(0, 1, 0), ball.Velocity) < 0)
			{
				rotateAngle *= -1;
			}

            retVal.RotateAroundAxis(new MyVector(0, 0, 1), rotateAngle);

			retVal.Add(GetVelocityEnd(position, ball));

			return retVal;
		}

		private void TimerBalls()
		{
			#region Collide

			BallBlip collided1 = new BallBlip(_ball1.CloneBall(), CollisionStyle.Standard, RadarBlipQual.BallUserDefined00, 1);
			collided1.Ball.Velocity.StoreNewValues(_ball1.Velocity);
			BallBlip collided2 = new BallBlip(_ball2.CloneBall(), CollisionStyle.Standard, RadarBlipQual.BallUserDefined00, 2);
			collided2.Ball.Velocity.StoreNewValues(_ball2.Velocity);

			collided1.PrepareForNewCycle();
			collided2.PrepareForNewCycle();

			_collider.Collide(collided1, collided2);

			collided1.TimerTestPosition(1);
			collided2.TimerTestPosition(1);
			collided1.TimerFinish();
			collided2.TimerFinish();

			#endregion

            pictureBox1.PrepareForNewDraw();

			#region Draw Balls

			//	Ball1
			pictureBox1.FillCircle(UtilityGDI.AlphaBlend(Color.Orange, Color.Silver, .25), _ball1.Position, _ball1.Radius);
            pictureBox1.DrawCircle(Color.DimGray, 1, _ball1.Position, _ball1.Radius);

            pictureBox1.DrawLine(Color.Yellow, 8, _ball1.Position, GetVelocityEnd(_ball1));
            pictureBox1.DrawLine(Color.Black, 4, _ball1.Position, GetVelocityEnd(_ball1.Position, collided1.Ball));

			if (_ball1 is TorqueBall)
			{
                pictureBox1.DrawLine(Color.White, 8, GetVelocityEnd(_ball1), GetAngularVelocityEnd((TorqueBall)_ball1));
                pictureBox1.DrawLine(Color.Brown, 4, GetVelocityEnd(_ball1.Position, collided1.Ball), GetAngularVelocityEnd(_ball1.Position, collided1.TorqueBall));
			}

			//	Ball2
			pictureBox1.FillCircle(UtilityGDI.AlphaBlend(Color.DodgerBlue, Color.Silver, .5), _ball2.Position, _ball2.Radius);
            pictureBox1.DrawCircle(Color.DimGray, 1, _ball2.Position, _ball2.Radius);

			pictureBox1.DrawLine(UtilityGDI.AlphaBlend(Color.White, Color.Navy, .8), 8, _ball2.Position, GetVelocityEnd(_ball2));
            pictureBox1.DrawLine(Color.Black, 4, _ball2.Position, GetVelocityEnd(_ball2.Position, collided2.Ball));

			if (_ball2 is TorqueBall)
			{
                pictureBox1.DrawLine(Color.White, 8, GetVelocityEnd(_ball2), GetAngularVelocityEnd((TorqueBall)_ball2));
                pictureBox1.DrawLine(Color.Brown, 4, GetVelocityEnd(_ball2.Position, collided2.Ball), GetAngularVelocityEnd(_ball2.Position, collided2.TorqueBall));
			}

			#endregion

            pictureBox1.FinishedDrawing();
		}
		private void TimerLineTriangle()
		{
			#region Collide

            //MyVector collisionPoint = CollisionHandler.IsIntersecting_LinePlane(_ball1.Position, _ball1.Velocity, _triangle1.TriangleWorld, false);

            MyVector collisionPoint = CollisionHandler.IsIntersecting_LineTriangle(_ball1.Position, _ball1.Velocity, _triangle1.TriangleWorld, true);

			#endregion

            pictureBox1.PrepareForNewDraw();

			#region Draw Objects

            pictureBox1.DrawLine(Color.Yellow, 8, _ball1.Position, GetVelocityEnd(_ball1));

			if (collisionPoint != null)
			{
                _triangle1.Draw(pictureBox1, Color.FromArgb(128, Color.Tomato));
                pictureBox1.FillCircle(Color.Black, collisionPoint, 8);
			}
			else
			{
                _triangle1.Draw(pictureBox1, Color.FromArgb(128, Color.DodgerBlue));
			}

			#endregion

            pictureBox1.FinishedDrawing();
		}
		private void TimerSphereTriangle()
		{
			#region Collide

            MyVector collisionPoint = CollisionHandler.IsIntersecting_SphereTriangle(_ball1, _triangle1.TriangleWorld);

			#endregion

            pictureBox1.PrepareForNewDraw();

			#region Draw Objects

			//	Ball1
			pictureBox1.FillCircle(UtilityGDI.AlphaBlend(Color.Orange, Color.Silver, .25), _ball1.Position, _ball1.Radius);
            pictureBox1.DrawCircle(Color.DimGray, 1, _ball1.Position, _ball1.Radius);

			//	Triangle1
			if (collisionPoint != null)
			{
                _triangle1.Draw(pictureBox1, Color.FromArgb(128, Color.Tomato));
                pictureBox1.FillCircle(Color.Black, collisionPoint, 8);
			}
			else
			{
                _triangle1.Draw(pictureBox1, Color.FromArgb(128, Color.DodgerBlue));
			}

			#endregion

            pictureBox1.FinishedDrawing();
		}
		private void TimerTriangleTriangle()
		{
			#region Collide

            MyVector[] collisionPoints = CollisionHandler.IsIntersecting_TriangleTriangle(_triangle1.TriangleWorld, _triangle2.TriangleWorld);

			#endregion

            pictureBox1.PrepareForNewDraw();

			#region Draw Objects

			if (collisionPoints != null)
			{
                _triangle1.Draw(pictureBox1, Color.FromArgb(128, Color.Tomato));
                _triangle2.Draw(pictureBox1, Color.FromArgb(128, Color.Tomato));

                pictureBox1.DrawLine(Color.DimGray, 1, collisionPoints[0], collisionPoints[1]);
                pictureBox1.FillCircle(Color.Black, collisionPoints[0], 8);
                pictureBox1.FillCircle(Color.Black, collisionPoints[1], 8);
			}
			else
			{
                _triangle1.Draw(pictureBox1, Color.FromArgb(128, Color.DodgerBlue));
                _triangle2.Draw(pictureBox1, Color.FromArgb(128, Color.Orange));
			}

			#endregion

            pictureBox1.FinishedDrawing();
		}
		private void TimerSphereSphere()
		{
			#region Collide

            MyVector collisionPoint = CollisionHandler.IsIntersecting_SphereSphere(_ball1, _ball2);

			#endregion

            pictureBox1.PrepareForNewDraw();

			#region Draw Objects

			//	Ball1
			pictureBox1.FillCircle(UtilityGDI.AlphaBlend(Color.Orange, Color.Silver, .25), _ball1.Position, _ball1.Radius);
            pictureBox1.DrawCircle(Color.DimGray, 1, _ball1.Position, _ball1.Radius);


			//	Ball2
			pictureBox1.FillCircle(UtilityGDI.AlphaBlend(Color.DodgerBlue, Color.Silver, .5), _ball2.Position, _ball2.Radius);
            pictureBox1.DrawCircle(Color.DimGray, 1, _ball2.Position, _ball2.Radius);


            pictureBox1.FillCircle(Color.DarkOrange, _ball1.Position, 8);
            pictureBox1.FillCircle(Color.Indigo, _ball2.Position, 8);


			if (collisionPoint != null)
			{
                pictureBox1.FillCircle(Color.Black, collisionPoint, 8);
			}

			#endregion

            pictureBox1.FinishedDrawing();
		}
		private void TimerSpherePolygon()
		{
			#region Collide

            MyVector truePoint;
            //MyVector[] collisionPoints = PolygonTest.IsIntersecting_SpherePolygon(_ball1, _polygon1.Position, _polygon1, _polygon1);
            MyVector[] collisionPoints = PolygonTest.TestCollision(out truePoint, _ball1, _polygon1.Position, _polygon1, _polygon1);

			#endregion

            pictureBox1.PrepareForNewDraw();

			#region Draw Objects

			//	Ball1
			if (collisionPoints != null)
			{
                pictureBox1.FillCircle(Color.FromArgb(128, Color.Tomato), _ball1.Position, _ball1.Radius);
			}
			else
			{
				pictureBox1.FillCircle(UtilityGDI.AlphaBlend(Color.Orange, Color.Silver, .25), _ball1.Position, _ball1.Radius);
			}
            pictureBox1.DrawCircle(Color.DimGray, 1, _ball1.Position, _ball1.Radius);

			//	Polygon1
			if (collisionPoints != null)
			{
                _polygon1.Draw(pictureBox1, Color.FromArgb(128, Color.Tomato), Color.FromArgb(128, Color.Firebrick));
			}
			else
			{
                _polygon1.Draw(pictureBox1, Color.FromArgb(128, Color.DodgerBlue), Color.FromArgb(128, Color.DarkTurquoise));
			}

			if (collisionPoints != null)
			{
                foreach (MyVector collisionPoint in collisionPoints)
				{
                    pictureBox1.FillCircle(Color.Black, collisionPoint, 12);
				}
                pictureBox1.FillCircle(Color.White, truePoint, 8);
			}


			#endregion

            pictureBox1.FinishedDrawing();
		}
		private void TimerPolygonPolygon()
		{
			#region Collide

            MyVector collisionPoint = null;	//PolygonTest.IsIntersecting_PolygonPolygon(_polygon1, _polygon2);

			#endregion

            pictureBox1.PrepareForNewDraw();

			#region Draw Objects

			if (collisionPoint != null)
			{
                _polygon1.Draw(pictureBox1, Color.FromArgb(128, Color.Tomato), Color.FromArgb(128, Color.Firebrick));
                _polygon2.Draw(pictureBox1, Color.FromArgb(128, Color.Tomato), Color.FromArgb(128, Color.Firebrick));

                pictureBox1.FillCircle(Color.Black, collisionPoint, 8);
			}
			else
			{
                _polygon1.Draw(pictureBox1, Color.FromArgb(128, Color.DodgerBlue), Color.FromArgb(128, Color.DarkTurquoise));
                _polygon2.Draw(pictureBox1, Color.FromArgb(128, Color.Orange), Color.FromArgb(128, Color.Khaki));
			}

			#endregion

            pictureBox1.FinishedDrawing();
		}

		#endregion
	}
}