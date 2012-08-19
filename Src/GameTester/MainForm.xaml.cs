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
using System.Windows.Shapes;

namespace Game.GameTester
{
	public partial class MainForm : Window
	{
		#region Constructor

		public MainForm()
		{
			InitializeComponent();

			// Calling this lets modeless windows form receive keyboard events
			System.Windows.Forms.Integration.WindowsFormsHost.EnableWindowsFormsInterop();    // had to add reference to WindowsFormsIntegration

			Game.Orig.HelperClassesGDI.UtilityGDI.EnableVisualStyles();

			this.Background = new SolidColorBrush(SystemColors.ControlColor);
		}

		#endregion

		#region Event Listeners

		#region Original Engine

		private void itemVector_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.VectorTester().Show();
		}
		private void itemRotateAroundPoint_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.RotateAroundPointTester().Show();
		}
		private void itemBall_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.BallTester().Show();
		}
		private void itemSolidBall_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.SolidBallTester().Show();
		}
		private void itemRigidBody1_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.RigidBodyTester1().Show();
		}
		private void itemRigidBody2_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.RigidBodyTester2().Show();
		}
		private void itemPolygon_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.PolygonTester().Show();
		}

		private void itemCollision_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.CollisionTester().Show();
		}
		private void itemMap_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.MapTester1().Show();
		}
		private void itemPhysicsPainter_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersGDI.PhysicsPainter.PhysicsPainterMainForm().Show();
		}
		private void item3DTester1_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Orig.TestersWPF.ThreeDTester1().Show();
		}

		#endregion
		#region WPF Tests

		private void itemCameraTester_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.CameraTester().Show();
		}
		private void itemTransformTester_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.TransformTester().Show();
		}
		private void itemSoundTester_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.SoundTester().Show();
		}
		private void itemRotateDblVect_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.DoubleVectorWindow().Show();
		}
		private void itemTubeMeshTester_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.TubeMeshTester().Show();
		}
		private void itemPotatoes_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.PotatoWindow().Show();
		}

		#endregion
		#region Newton Engine

		private void itemGravityCubes1_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.GravityCubes1().Show();
		}
		private void itemGravityCubes2_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.GravityCubes2().Show();
		}
		private void itemCollisionShapes_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.CollisionShapes().Show();
		}
		private void itemAstMiner2D_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.AsteroidMiner2D_153.Miner2D().Show();
		}
		private void itemSwarmBots_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.AsteroidMiner2D_153.SwarmBotTester().Show();
		}

		private void itemNewt2_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.Newt2Tester.Newt2Tester().Show();
		}
		private void itemTowerWrecker_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.TowerWrecker.TowerWreckerWindow().Show();
		}
		private void itemWindTunnel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.WindTunnelWindow().Show();
		}
		private void itemAsteroidMiner2_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.AsteroidMiner2.View.AsteroidMinerWindow().Show();
		}
		private void itemShipEditor_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.ShipEditorWindow().Show();
		}
		private void itemShipPartTester_MouseUp(object sender, MouseButtonEventArgs e)
		{
			new Game.Newt.Testers.ShipPartTesterWindow().Show();
		}

		#endregion

		private void About_Click(object sender, RoutedEventArgs e)
		{
			new AboutWindow().Show();
		}

		#endregion
	}
}
