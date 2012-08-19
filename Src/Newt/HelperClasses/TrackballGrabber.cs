using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Game.Newt.HelperClasses
{
	//TODO:  Also superimpose some kind of icon that opens a context menu of preset orientations (it won't be intuitive for them to right click)
	//TODO:  Maybe draw some rings that people can grab that locks the rotation along an axis (spore uses tab, but that won't be intuitive here.  probably need a checkbox)

	/// <summary>
	/// This will superimpose a trackball when the user mouses over the viewport, and lets them rotate it (raising an event for other objects
	/// to get the new orientation)
	/// </summary>
	/// <remarks>
	/// Makes a REAL glass button  :)
	/// 
	/// Parts of this were copied from the TrackBallRoam class
	/// </remarks>
	public class TrackballGrabber : IDisposable
	{
		#region Events

		public event EventHandler RotationChanged = null;

		#endregion

		#region Declaration Section

		private FrameworkElement _eventSource = null;
		private Viewport3D _viewport = null;
		private PerspectiveCamera _camera = null;

		private ModelVisual3D _sphereModel = null;
		private MaterialGroup _sphereMaterials = null;
		private Material _sphereMaterialHover = null;
		private ModelVisual3D _hoverLight = null;

		private bool _isMouseDown = false;

		private Point _previousPosition2D;
		private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

		#endregion

		#region Constructor

		public TrackballGrabber(FrameworkElement eventSource, Viewport3D viewport, double sphereRadius, Color hoverLightColor)
		{
			if (viewport.Camera == null || !(viewport.Camera is PerspectiveCamera))
			{
				throw new ArgumentException("This class requires a perspective camera to be tied to the viewport");
			}

			_eventSource = eventSource;
			_viewport = viewport;
			_camera = (PerspectiveCamera)viewport.Camera;

			_eventSource.MouseEnter += new System.Windows.Input.MouseEventHandler(EventSource_MouseEnter);
			_eventSource.MouseLeave += new System.Windows.Input.MouseEventHandler(EventSource_MouseLeave);
			_eventSource.MouseDown += new System.Windows.Input.MouseButtonEventHandler(EventSource_MouseDown);
			_eventSource.MouseUp += new System.Windows.Input.MouseButtonEventHandler(EventSource_MouseUp);
			_eventSource.MouseMove += new System.Windows.Input.MouseEventHandler(EventSource_MouseMove);

			#region Sphere

			//	Material
			_sphereMaterials = new MaterialGroup();
			_sphereMaterials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(25, 255, 255, 255))));

			//	This gets added/removed on mouse enter/leave
			_sphereMaterialHover = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(64, 128, 128, 128)), 33d);

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = _sphereMaterials;
			geometry.BackMaterial = _sphereMaterials;
			geometry.Geometry = UtilityWPF.GetSphere(20, sphereRadius);

			//	Model Visual
			_sphereModel = new ModelVisual3D();
			_sphereModel.Content = geometry;

			//	Add it
			_viewport.Children.Add(_sphereModel);

			#endregion
			#region Hover Light

			//	Light
			PointLight hoverLight = new PointLight();
			hoverLight.Color = hoverLightColor;
			hoverLight.Range = sphereRadius * 10;

			_hoverLight = new ModelVisual3D();
			_hoverLight.Content = hoverLight;

			#endregion
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_viewport != null)
			{
				if (_sphereModel != null)
				{
					_viewport.Children.Remove(_sphereModel);
					_sphereModel = null;
				}

				_viewport = null;
			}
		}

		#endregion

		#region Public Properties

		private Quaternion _quaternion = new Quaternion();
		private RotateTransform3D _transform = new RotateTransform3D();
		public RotateTransform3D Transform
		{
			get
			{
				return _transform;
			}
			set
			{
				//	Calculate the difference between the current transform and the one passed in
				Quaternion quatDelta = _transform.ToQuaternion().ToUnit() * value.ToQuaternion().ToUnit();
				quatDelta = new Quaternion(quatDelta.Axis, quatDelta.Angle * -1d);		//	negating, because the camera will be spun opposite of the model

				RotateTransform3D deltaTransform = new RotateTransform3D(new QuaternionRotation3D(quatDelta));

				//	Rotate the camera by that amount
				_camera.Position = deltaTransform.Transform(_camera.Position);
				_camera.UpDirection = deltaTransform.Transform(_camera.UpDirection);
				_camera.LookDirection = deltaTransform.Transform(_camera.LookDirection);

				//	Store the new transform
				_transform = value;

				//	Inform the world of the change
				if (this.RotationChanged != null)
				{
					this.RotationChanged(this, new EventArgs());
				}
			}
		}

		#endregion

		#region Event Listeners

		private void EventSource_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			_sphereMaterials.Children.Add(_sphereMaterialHover);
			_viewport.Children.Add(_hoverLight);
		}
		private void EventSource_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			_sphereMaterials.Children.Remove(_sphereMaterialHover);
			_viewport.Children.Remove(_hoverLight);
		}

		private void EventSource_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			_isMouseDown = true;

			_viewport.Children.Remove(_hoverLight);

			//	By capturing the mouse, mouse events will still come in even when they are moving the mouse
			//	outside the element/form
			Mouse.Capture(_eventSource, CaptureMode.SubTree);		//	I had a case where I used the grid as the event source.  If they clicked one of the 3D objects, the scene would jerk.  But by saying subtree, I still get the event

			_previousPosition2D = e.GetPosition(_eventSource);
			_previousPosition3D = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, _previousPosition2D);
		}
		private void EventSource_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			_isMouseDown = false;

			Mouse.Capture(_eventSource, CaptureMode.None);

			if (_eventSource.IsMouseOver && !_viewport.Children.Contains(_hoverLight))		//	I ran into a case where I click down outside the viewport, then released over (the light was already on from the mouse enter)
			{
				_viewport.Children.Add(_hoverLight);
			}
		}

		private void EventSource_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (!_isMouseDown)
			{
				return;
			}

			Point currentPosition = e.GetPosition(_eventSource);

			//	Avoid any zero axis conditions
			if (currentPosition == _previousPosition2D)
			{
				return;
			}

			//	Project the 2D position onto a sphere
			Vector3D currentPosition3D = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, currentPosition);

			OrbitCamera(currentPosition, currentPosition3D);

			_previousPosition2D = currentPosition;
			_previousPosition3D = currentPosition3D;







		}

		#endregion

		#region Private Methods

		private void OrbitCamera(Point currentPosition, Vector3D currentPosition3D)
		{
			#region Get Mouse Movement - Spherical

			//	Figure out a rotation axis and angle
			Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
			double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);

			//	Quaterion will throw if this happens - sometimes we can get 3D positions that are very similar, so we
			//	avoid the throw by doing this check and just ignoring the event 
			if (axis.Length == 0)
			{
				return;
			}

			//	Now need to rotate the axis into the camera's coords
			// Get the camera's current view matrix.
			Matrix3D viewMatrix = MathUtils.GetViewMatrix(_camera);
			viewMatrix.Invert();

			// Transform the trackball rotation axis relative to the camera orientation.
			axis = viewMatrix.Transform(axis);

			Quaternion deltaRotation = new Quaternion(axis, -angle);
			Quaternion deltaRotationExternal = new Quaternion(axis, angle);

			#endregion

			//	This can't be calculated each mose move.  It causes a wobble when the look direction isn't pointed directly at the origin
			//if (_orbitRadius == null)
			//{
			//    _orbitRadius = OnGetOrbitRadius();
			//}

			//	Figure out the offset in world coords
			Vector3D lookLine = _camera.LookDirection;
			lookLine.Normalize();
			lookLine = lookLine * _camera.Position.ToVector().Length;		//_orbitRadius.Value;		//	the camera is always pointed to the origin, so this shortcut works

			Point3D orbitPointWorld = _camera.Position + lookLine;

			//	Get the opposite of the look line (the line from the orbit center to the camera's position)
			Vector3D lookLineOpposite = lookLine * -1d;

			//	Rotate
			Vector3D[] vectors = new Vector3D[] { lookLineOpposite, _camera.UpDirection, _camera.LookDirection };

			deltaRotation.GetRotatedVector(vectors);

			//	Apply the changes
			_camera.Position = orbitPointWorld + vectors[0];
			_camera.UpDirection = vectors[1];
			_camera.LookDirection = vectors[2];

			_quaternion = _quaternion.ToUnit() * deltaRotationExternal.ToUnit();
			_transform = new RotateTransform3D(new QuaternionRotation3D(_quaternion));

			if (this.RotationChanged != null)
			{
				this.RotationChanged(this, new EventArgs());
			}
		}
		private void OrbitBall_OLD(Point currentPosition, Vector3D currentPosition3D)
		{
			#region Get Mouse Movement - Spherical

			//	Figure out a rotation axis and angle
			Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
			double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);

			//	Quaterion will throw if this happens - sometimes we can get 3D positions that are very similar, so we
			//	avoid the throw by doing this check and just ignoring the event 
			if (axis.Length == 0)
			{
				return;
			}




			//	Is this bit needed?


			//	Now need to rotate the axis into the camera's coords
			// Get the camera's current view matrix.
			Matrix3D viewMatrix = MathUtils.GetViewMatrix(_viewport.Camera);
			viewMatrix.Invert();

			// Transform the trackball rotation axis relative to the camera orientation.
			axis = viewMatrix.Transform(axis);

			//Quaternion deltaRotation = new Quaternion(axis, -angle);
			Quaternion deltaRotation = new Quaternion(axis, angle);

			#endregion


			//_quaternion = _quaternion.ToUnit() * new Quaternion(axis, angle).ToUnit();
			_quaternion = _quaternion.ToUnit() * deltaRotation.ToUnit();
			_transform = new RotateTransform3D(new QuaternionRotation3D(_quaternion));


			if (this.RotationChanged != null)
			{
				this.RotationChanged(this, new EventArgs());
			}



			////	This can't be calculated each mose move.  It causes a wobble when the look direction isn't pointed directly at the origin
			//if (_orbitRadius == null)
			//{
			//    _orbitRadius = OnGetOrbitRadius();
			//}

			////	Figure out the offset in world coords
			//Vector3D lookLine = _camera.LookDirection;
			//lookLine.Normalize();
			//lookLine = lookLine * _orbitRadius.Value;

			//Point3D orbitPointWorld = _camera.Position + lookLine;

			////	Get the opposite of the look line (the line from the orbit center to the camera's position)
			//Vector3D lookLineOpposite = lookLine * -1d;

			////	Rotate
			//Vector3D[] vectors = new Vector3D[] { lookLineOpposite, _camera.UpDirection, _camera.LookDirection };

			//deltaRotation.GetRotatedVector(vectors);

			////	Apply the changes
			//_camera.Position = orbitPointWorld + vectors[0];
			//_camera.UpDirection = vectors[1];
			//_camera.LookDirection = vectors[2];



		}

		private static Vector3D ProjectToTrackball(double width, double height, Point point)
		{
			bool shouldInvertZ = false;

			//	Scale the inputs so -1 to 1 is the edge of the screen
			double x = point.X / (width / 2d);    // Scale so bounds map to [0,0] - [2,2]
			double y = point.Y / (height / 2d);

			x = x - 1d;                           // Translate 0,0 to the center
			y = 1d - y;                           // Flip so +Y is up instead of down

			//	Wrap (otherwise, everything greater than 1 will map to the permiter of the sphere where z = 0)
			bool localInvert;
			x = ProjectToTrackballSprtWrap(out localInvert, x);
			shouldInvertZ |= localInvert;

			y = ProjectToTrackballSprtWrap(out localInvert, y);
			shouldInvertZ |= localInvert;

			//	Project onto a sphere
			double z2 = 1d - (x * x) - (y * y);       // z^2 = 1 - x^2 - y^2
			double z = 0d;
			if (z2 > 0d)
			{
				z = Math.Sqrt(z2);
			}
			else
			{
				//	NOTE:  The wrap logic above should make it so this never happens
				z = 0d;
			}

			if (shouldInvertZ)
			{
				z *= -1d;
			}

			//	Exit Function
			return new Vector3D(x, y, z);
		}
		/// <summary>
		/// This wraps the value so it stays between -1 and 1
		/// </summary>
		private static double ProjectToTrackballSprtWrap(out bool shouldInvertZ, double value)
		{
			//	Everything starts over at 4 (4 becomes zero)
			double retVal = value % 4d;

			double absX = Math.Abs(retVal);
			bool isNegX = retVal < 0d;

			shouldInvertZ = false;

			if (absX >= 3d)
			{
				//	Anything from 3 to 4 needs to be -1 to 0
				//	Anything from -4 to -3 needs to be 0 to 1
				retVal = 4d - absX;

				if (!isNegX)
				{
					retVal *= -1d;
				}
			}
			else if (absX > 1d)
			{
				//	This is the back side of the sphere
				//	Anything from 1 to 3 needs to be flipped (1 stays 1, 2 becomes 0, 3 becomes -1)
				//	-1 stays -1, -2 becomes 0, -3 becomes 1
				retVal = 2d - absX;

				if (isNegX)
				{
					retVal *= -1d;
				}

				shouldInvertZ = true;
			}

			//	Exit Function
			return retVal;
		}

		#endregion
	}
}
