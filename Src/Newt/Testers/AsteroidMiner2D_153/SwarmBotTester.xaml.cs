using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner_153;
using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Primitives3D;
using Game.Newt.NewtonDynamics_153;

namespace Game.Newt.Testers.AsteroidMiner2D_153
{
	public partial class SwarmBotTester : Window
	{
		#region Declaration Section

		private const double WIDTH_SMALL = 20d;
		private const double WIDTH_LARGE = 100d;
		private const double DEPTH = 5d;
		private const double CAMERAZ_SMALL = 50d;
		private const double CAMERAZ_LARGE = 250d;
		private const double MOUSECURSORLARGESCALE = 5d;

		private bool _isInitialized = false;

		private List<ScreenSpaceLines3D> _lines = new List<ScreenSpaceLines3D>();

		private bool _isLargeMap = false;
		private Vector3D _boundryMin = new Vector3D(-WIDTH_SMALL, -WIDTH_SMALL, -DEPTH);		//	I need to set Z to something.  If the Z plates are too close together, then objects will get stuck and not move (constantly colliding)
		private Vector3D _boundryMax = new Vector3D(WIDTH_SMALL, WIDTH_SMALL, DEPTH);

		private List<SwarmBot2> _swarmBots = new List<SwarmBot2>();

		/// <summary>
		/// The mouse cursor is hidden, and this is placed in that position instead.  That way I can be sure the swarmbots
		/// are aiming where I think
		/// </summary>
		private ModelVisual3D _mouseCursor = null;
		private GeometryModel3D _mouseCursorGeometry = null;

		private bool _isLimitedVision = false;
		private bool _isShowingThrust = false;

		private SwarmBot2.BehaviorType _activeBehavior = SwarmBot2.BehaviorType.StraightToTarget;

		private Brush _toggleBrush = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));

		SharedVisuals _sharedVisuals = new SharedVisuals();

		#endregion

		#region Constructor

		public SwarmBotTester()
		{
			InitializeComponent();

			#region Init World

			_world.InitialiseBodies();
			//_world.ShouldForce2D = true;
			//_world.Gravity = -10d;     // -600d;    //this is set by the trackbar
			//_world.Gravity = 0d;

			List<Point3D[]> innerLines, outerLines;
			_world.SetCollisionBoundry(out innerLines, out outerLines, _viewport, _boundryMin, _boundryMax);

			Color lineColor = Color.FromArgb(255, 200, 200, 180);
			foreach (Point3D[] line in innerLines)
			{
				//	Need to wait until the window is loaded to call lineModel.CalculateGeometry
				ScreenSpaceLines3D lineModel = new ScreenSpaceLines3D(false);
				lineModel.Thickness = 1d;
				lineModel.Color = lineColor;
				lineModel.AddLine(line[0], line[1]);

				_viewport.Children.Add(lineModel);
				_lines.Add(lineModel);
			}

			//_world.SimulationSpeed = .1d;      // this was when I had to set change in velocity instead of applying forces
			_world.SimulationSpeed = 8d;
			_world.UnPause();

			#endregion

			_isInitialized = true;

			// Make the form look right
			trkGravity_ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(trkGravity.Value, trkGravity.Value));
			SetActiveBehavior();
		}

		#endregion

		#region Event Listeners

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			RecalculateLineGeometries();

			CreateMouseVisual();

			for (int cntr = 0; cntr < 42; cntr++)
			{
				SwarmBot2 swarmBot = new SwarmBot2();
				swarmBot.Radius = .75d;
				swarmBot.ThrustForce = 6d;
				swarmBot.Behavior = _activeBehavior;
				swarmBot.CreateBot(_viewport, _sharedVisuals, _world, Math3D.GetRandomVector(_boundryMin, _boundryMax).ToPoint());
				_swarmBots.Add(swarmBot);
			}

			// Now that they're all created, tell each about the others
			foreach (SwarmBot2 bot in _swarmBots)
			{
				foreach (SwarmBot2 otherBot in _swarmBots)
				{
					if (otherBot == bot)
					{
						continue;
					}

					bot.OtherBots.Add(otherBot);
				}
			}
		}
		private void Window_Closed(object sender, EventArgs e)
		{
			if (_world != null)
			{
				_world.Dispose();
				_world = null;
			}
		}

		/// <summary>
		/// This is raised by _world once per frame (this is raised, then it requests forces for bodies)
		/// </summary>
		private void World_Updating(object sender, EventArgs e)
		{
			try
			{
				foreach (SwarmBot2 bot in _swarmBots)
				{
					bot.WorldUpdating();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
		{
		}

		private void grdViewPort_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RecalculateLineGeometries();
		}
		private void Camera_Changed(object sender, EventArgs e)
		{
			RecalculateLineGeometries();
		}

		private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!_isInitialized || _swarmBots.Count == 0)
			{
				return;
			}

			foreach (SwarmBot2 bot in _swarmBots)
			{
				bot.IsAttacking = true;
			}

			//TODO:  LeftMouse makes the bots attack the mouse
			//TODO:  RightMouse makes the bots free roam

			// -or- they stay in free roam until left is held down, right makes them attack
		}
		private void grdViewPort_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (!_isInitialized || _swarmBots.Count == 0)
			{
				return;
			}

			foreach (SwarmBot2 bot in _swarmBots)
			{
				bot.IsAttacking = false;
			}
		}

		private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
		{
			if (!_isInitialized || _swarmBots.Count == 0)
			{
				return;
			}

			// currentPosition is in the grid's coords.  I need to convert into 3D world coords
			Point currentPosition = e.GetPosition(grdViewPort);

			// HitTest (I don't like being forced to create a plane like this)
			HitTestResult result = VisualTreeHelper.HitTest(_viewport, currentPosition);
			if (result != null && result is RayMeshGeometry3DHitTestResult && result.VisualHit == _hitTestPlane)       // if it's not the plane, then the hit point seems to be in model coords, or something
			{
				RayMeshGeometry3DHitTestResult result3D = (RayMeshGeometry3DHitTestResult)result;

				Point3D worldPoint = result3D.PointHit;
				worldPoint.Z = 0;      // I had to put the plate below everything else, so I'll pretend the Z is zero (because even though it's transparent, nothing is rendering under it)

				// Move the mouse cursor sphere to this point
				_mouseCursor.Transform = new TranslateTransform3D(result3D.PointHit.ToVector());

				// Tell the swarm bot where to go
				foreach (SwarmBot2 bot in _swarmBots)
				{
					bot.ChasePoint = worldPoint;
				}
			}

			#region Failed Attempts

			// this one doesn't work right, and I don't feel like figuring it out
			//if (result != null && result is RayMeshGeometry3DHitTestResult)
			//{
			//    RayMeshGeometry3DHitTestResult result3D = (RayMeshGeometry3DHitTestResult)result;

			//    _mouseCursor.Transform = new TranslateTransform3D(result3D.PointHit.X, result3D.PointHit.Y, 0);
			//}

			// These are attempts at calculating the point myself

			#region Attempt1

			//Matrix3D matrix = MathUtils.GetWorldToViewportTransform(_viewport);
			//if (!matrix.HasInverse)
			//{
			//    return;
			//}

			//matrix.Invert();

			//// This is generating EXTREMELY small numbers
			//Point3D pointWorld = matrix.Transform(new Point3D(currentPosition.X, currentPosition.Y, 0));

			#endregion
			#region Attempt2

			//Viewport3DVisual dummy;
			//bool success;
			//Matrix3D matrix2 = MathUtils.TryTransformTo2DAncestor(_lines[0], out dummy, out success);
			//matrix2.Invert();

			//Point3D pointWorld = matrix2.Transform(new Point3D(currentPosition.X, currentPosition.Y, 0));


			//_mouseCursor.Transform = new TranslateTransform3D(pointWorld.X, pointWorld.Y, 0);

			#endregion

			//This doesn't work either!!! (width is 800, but viewing a box that's 20)

			// None of that 3D projection works, so I'll cheat (works, since the camera is looking straight down)
			//_mouseCursor.Transform = new TranslateTransform3D(currentPosition.X - (grdViewPort.Width / 2), currentPosition.Y - (grdViewPort.Height / 2), 0);
			//_mouseCursor.Transform = new TranslateTransform3D(currentPosition.X - (this.Width / 2), 0, 0);

			#endregion
		}

		private void lblLimitToClosest_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_isLimitedVision = !_isLimitedVision;
			int limitCount;
			double visionLimit;

			if (_isLimitedVision)
			{
				lblLimitToClosest.Background = _toggleBrush;
				limitCount = 4;      //TODO:  1 + (count / 4)
				visionLimit = 20;
			}
			else
			{
				lblLimitToClosest.Background = Brushes.Transparent;
				limitCount = int.MaxValue;
				visionLimit = double.MaxValue;
			}

			foreach (SwarmBot2 bot in _swarmBots)
			{
				bot.NumClosestBotsToLookAt = limitCount;
				bot.VisionLimit = visionLimit;
			}
		}
		private void lblLargeMap_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_isLargeMap = !_isLargeMap;

			// Come up with new values (and color the control)
			Point3D cameraPos = new Point3D();
			double mouseCursorScale = 1d;
			if (_isLargeMap)
			{
				lblLargeMap.Background = _toggleBrush;

				_boundryMin = new Vector3D(-WIDTH_LARGE, -WIDTH_LARGE, -DEPTH);
				_boundryMax = new Vector3D(WIDTH_LARGE, WIDTH_LARGE, DEPTH);
				cameraPos = new Point3D(0, 0, CAMERAZ_LARGE);

				mouseCursorScale = MOUSECURSORLARGESCALE;
			}
			else
			{
				lblLargeMap.Background = Brushes.Transparent;

				_boundryMin = new Vector3D(-WIDTH_SMALL, -WIDTH_SMALL, -DEPTH);
				_boundryMax = new Vector3D(WIDTH_SMALL, WIDTH_SMALL, DEPTH);
				cameraPos = new Point3D(0, 0, CAMERAZ_SMALL);
			}

			#region Change Boundry

			foreach (ScreenSpaceLines3D line in _lines)
			{
				_viewport.Children.Remove(line);
			}
			_lines.Clear();

			List<Point3D[]> innerLines, outerLines;
			_world.SetCollisionBoundry(out innerLines, out outerLines, _viewport, _boundryMin, _boundryMax);

			//TODO:  Make a private method that does this
			Color lineColor = Color.FromArgb(255, 200, 200, 180);
			foreach (Point3D[] line in innerLines)
			{
				//	Need to wait until the window is loaded to call lineModel.CalculateGeometry
				ScreenSpaceLines3D lineModel = new ScreenSpaceLines3D(false);
				lineModel.Thickness = 1d;
				lineModel.Color = lineColor;
				lineModel.AddLine(line[0], line[1]);

				_viewport.Children.Add(lineModel);
				_lines.Add(lineModel);
			}

			RecalculateLineGeometries();

			#endregion

			_mouseCursorGeometry.Transform = new ScaleTransform3D(mouseCursorScale, mouseCursorScale, mouseCursorScale);

			_camera.Position = cameraPos;
		}
		private void lblShowThrust_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_isShowingThrust = !_isShowingThrust;

			if (_isShowingThrust)
			{
				lblShowThrust.Background = _toggleBrush;
			}
			else
			{
				lblShowThrust.Background = Brushes.Transparent;
			}

			foreach (SwarmBot2 bot in _swarmBots)
			{
				bot.ShouldDrawThrustLine = _isShowingThrust;
			}
		}

		private void trkGravity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_world.Gravity = -1d * UtilityHelper.GetScaledValue_Capped(0d, 5d, trkGravity.Minimum, trkGravity.Maximum, trkGravity.Value);
		}

		private void pnlBehaviors_MouseEnter(object sender, MouseEventArgs e)
		{
			//pnlBehaviors.LayoutTransform = new ScaleTransform(1d, 1d);
		}
		private void pnlBehaviors_MouseLeave(object sender, MouseEventArgs e)
		{
			//pnlBehaviors.LayoutTransform = new ScaleTransform(.5d, .5d);
		}

		private void lblBehaviorSimple_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_activeBehavior = SwarmBot2.BehaviorType.StraightToTarget;
			SetActiveBehavior();
		}
		private void lblBehaviorSimple1_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_activeBehavior = SwarmBot2.BehaviorType.StraightToTarget_VelocityAware1;
			SetActiveBehavior();
		}
		private void lblBehaviorSimple2_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_activeBehavior = SwarmBot2.BehaviorType.StraightToTarget_VelocityAware2;
			SetActiveBehavior();
		}
		private void lblCenterFlock_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_activeBehavior = SwarmBot2.BehaviorType.TowardCenterOfFlock;
			SetActiveBehavior();
		}
		private void lblCenterFlockAvoid_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_activeBehavior = SwarmBot2.BehaviorType.CenterFlock_AvoidNeighbors;
			SetActiveBehavior();
		}
		private void lblCenterFlockAvoidVelocity_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_activeBehavior = SwarmBot2.BehaviorType.CenterFlock_AvoidNeighbors_FlockVelocity;
			SetActiveBehavior();
		}
		private void lblFlockingChasePoint_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_activeBehavior = SwarmBot2.BehaviorType.Flocking_ChasePoint;
			SetActiveBehavior();
		}

		#endregion

		#region Private Methods

		private void RecalculateLineGeometries()
		{
			foreach (ScreenSpaceLines3D line in _lines)
			{
				line.CalculateGeometry();
			}
		}

		private void CreateMouseVisual()
		{
			//	Material
			MaterialGroup material = new MaterialGroup();
			material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 227, 227, 154))));
			material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 235, 245, 184)), 50d));

			//	Geometry Model
			_mouseCursorGeometry = new GeometryModel3D();
			_mouseCursorGeometry.Material = material;
			_mouseCursorGeometry.BackMaterial = material;
			_mouseCursorGeometry.Geometry = UtilityWPF.GetSphere(4, .1, .1, .1);
			_mouseCursorGeometry.Transform = new ScaleTransform3D(1, 1, 1);

			//	Model Visual
			_mouseCursor = new ModelVisual3D();
			_mouseCursor.Content = _mouseCursorGeometry;

			//NOTE: _mouseCursor.Transform is set on each mousemove event

			//	Add to the viewport
			_viewport.Children.Add(_mouseCursor);
		}

		private void SetActiveBehavior()
		{
			// Tell the bots
			foreach (SwarmBot2 bot in _swarmBots)
			{
				bot.Behavior = _activeBehavior;
			}

			//TODO:  Just restyle an option group

			// Show the active selection
			lblBehaviorSimple.Background = Brushes.Transparent;
			lblBehaviorSimple1.Background = Brushes.Transparent;
			lblBehaviorSimple2.Background = Brushes.Transparent;
			lblCenterFlock.Background = Brushes.Transparent;
			lblCenterFlockAvoid.Background = Brushes.Transparent;
			lblCenterFlockAvoidVelocity.Background = Brushes.Transparent;
			lblFlockingChasePoint.Background = Brushes.Transparent;

			switch (_activeBehavior)
			{
				case SwarmBot2.BehaviorType.StraightToTarget:
					lblBehaviorSimple.Background = _toggleBrush;
					break;

				case SwarmBot2.BehaviorType.StraightToTarget_VelocityAware1:
					lblBehaviorSimple1.Background = _toggleBrush;
					break;

				case SwarmBot2.BehaviorType.StraightToTarget_VelocityAware2:
					lblBehaviorSimple2.Background = _toggleBrush;
					break;

				case SwarmBot2.BehaviorType.TowardCenterOfFlock:
					lblCenterFlock.Background = _toggleBrush;
					break;

				case SwarmBot2.BehaviorType.CenterFlock_AvoidNeighbors:
					lblCenterFlockAvoid.Background = _toggleBrush;
					break;

				case SwarmBot2.BehaviorType.CenterFlock_AvoidNeighbors_FlockVelocity:
					lblCenterFlockAvoidVelocity.Background = _toggleBrush;
					break;

				case SwarmBot2.BehaviorType.Flocking_ChasePoint:
					lblFlockingChasePoint.Background = _toggleBrush;
					break;

				default:
					throw new ApplicationException("Unknown SwarmBot2.BehaviorType: " + _activeBehavior.ToString());
			}
		}

		#endregion
	}
}
