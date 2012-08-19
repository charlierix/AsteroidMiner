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

using Game.Newt.NewtonDynamics_153;
using Game.Newt.HelperClasses;

namespace Game.Newt.Testers
{
	public partial class GravityCubes1 : Window
	{
		#region Declaration Section

		private const double GRAVITATIONALCONSTANT = 15d;
		private const double MAXRANDVELOCITY = 10d;

		private TrackBallRoam _trackball = null;

		private double _randVelMultiplier = 1d;
		private bool _shouldRandomizeVelocities = false;
		private bool _didRandomizeVelocities = false;

		#endregion

		#region Constructor

		public GravityCubes1()
		{
			InitializeComponent();

			grdPanel.Background = SystemColors.ControlBrush;

			//	Trackball
			_trackball = new TrackBallRoam(_camera);
			_trackball.EventSource = grdViewPort;
			_trackball.AllowZoomOnMouseWheel = true;
			_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
		}

		#endregion

		#region Event Listeners

		private void Window_Closed(object sender, EventArgs e)
		{
			if (_world != null)
			{
				_world.Dispose();
				_world = null;
			}
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			_world.InitialiseBodies();
			_world.Gravity = 0d;
			//_world.SetSize(new Vector3D(-15, -15, -15), new Vector3D(15, 15, 15));		//	NOTE:  they don't bounce off of this boundry, they just stop forever



			//TODO:  Figure out how to build the properties so that they can be called within xaml (need to wait until initialized)
			_redCube.LinearDamping = .01f;
			_blueCube.LinearDamping = .01f;

			//	Each coord seems to represent the amount of damping for that axis
			//_redCube.AngularDamping = new Vector3D(1000, 0, 1000);
			//_blueCube.AngularDamping = new Vector3D(1000, 0, 1000);



			_world.UnPause();
		}

		private void button2_Click(object sender, RoutedEventArgs e)
		{
			_shouldRandomizeVelocities = true;
			_didRandomizeVelocities = false;
			_randVelMultiplier = .75d;
		}
		private void button3_Click(object sender, RoutedEventArgs e)
		{
			_shouldRandomizeVelocities = true;
			_didRandomizeVelocities = false;
			_randVelMultiplier = 1.5d;
		}
		private void button4_Click(object sender, RoutedEventArgs e)
		{
			_shouldRandomizeVelocities = true;
			_didRandomizeVelocities = false;
			_randVelMultiplier = 5d;
		}
		private void button5_Click(object sender, RoutedEventArgs e)
		{
			_shouldRandomizeVelocities = true;
			_didRandomizeVelocities = false;
			_randVelMultiplier = 10d;
		}

		private void btnResetCamera_Click(object sender, RoutedEventArgs e)
		{
			_camera.Position = new Point3D(0, 0, 15);
			_camera.LookDirection = new Vector3D(0, 0, -10);
			_camera.UpDirection = new Vector3D(0, 1, 0);
		}

		private void _world_Updating(object sender, EventArgs e)
		{
			//	I believe this gets called once per timer tick saying that it's about to ask for forces
			if (_shouldRandomizeVelocities && _didRandomizeVelocities)
			{
				_shouldRandomizeVelocities = false;
				_didRandomizeVelocities = false;
			}
		}

		private void Cube_ApplyForce(Body sender, BodyForceEventArgs e)
		{
			if (_shouldRandomizeVelocities)
			{
				#region Set Velocities

				_didRandomizeVelocities = true;

				Vector3D newVelocity = Math3D.GetRandomVectorSpherical(MAXRANDVELOCITY * _randVelMultiplier);

				//sender.Velocity.X = newVelocity.X;
				//sender.Velocity.Y = newVelocity.Y;
				//sender.Velocity.Z = newVelocity.Z;

				e.AddImpulse(newVelocity, sender.CenterOfMass.ToVector());

				#endregion
			}

			#region Do Gravity

			//	Calculate force between the two
			//TODO:  Calculate these forces in one place and remember the results


			Point3D blueCenterWorld = GetWorldCenterMass(_blueCube);
			Point3D redCenterWorld = GetWorldCenterMass(_redCube);

			Vector3D gravityLink = blueCenterWorld - redCenterWorld;

			double force = GRAVITATIONALCONSTANT * (_blueCube.Mass * _redCube.Mass) / gravityLink.LengthSquared;

			gravityLink.Normalize();
			gravityLink = Vector3D.Multiply(force, gravityLink);

			//	Apply the force
			if (sender == _blueCube)
			{
				e.AddForce(Vector3D.Multiply(-1d, gravityLink));
			}
			else if (sender == _redCube)
			{
				e.AddForce(gravityLink);
			}
			else
			{
				MessageBox.Show("Unknown Sender: " + sender.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			#endregion
		}

		#endregion

		#region Private Methods

		private Point3D GetWorldCenterMass(Body body)
		{
			ConvexBody3D bodyCast = body as ConvexBody3D;
			if (bodyCast == null)
			{
				throw new ApplicationException("Couldn't cast body as a ConvexBody3D: " + body.ToString());
			}


			//TODO:  Put this in a property off of convexbody


			////	Get the center of mass in model coords
			//Point3D centerMass = bodyCast.CenterOfMass;
			//Vector3D retVal = new Vector3D(centerMass.X, centerMass.Y, centerMass.Z);

			////	Transform that into world coords
			//body.VisualMatrix.Transform(retVal);


			return body.VisualMatrix.Transform(bodyCast.CenterOfMass);





			//	Exit Function
			//return retVal;
		}

		#endregion
	}
}
