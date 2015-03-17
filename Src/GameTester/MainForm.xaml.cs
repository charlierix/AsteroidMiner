﻿using System;
using System.Collections.Generic;
using System.IO;
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

using Game.HelperClassesCore;

namespace Game.GameTester
{
    public partial class MainForm : Window
    {
        #region Declaration Section

        private const string FILE = "MainForm Options.xml";

        #endregion

        #region Constructor

        public MainForm()
        {
            InitializeComponent();

            // Calling this lets modeless windows form receive keyboard events
            System.Windows.Forms.Integration.WindowsFormsHost.EnableWindowsFormsInterop();    // had to add reference to WindowsFormsIntegration

            Game.Orig.HelperClassesGDI.UtilityGDI.EnableVisualStyles();

            TaskGCExceptionHandler.Instance.EnsureSetup();

            this.Background = new SolidColorBrush(SystemColors.ControlColor);

            LoadOptionsFromFile();
        }

        #endregion

        #region Event Listeners

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                SaveOptionsToFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListBoxItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (chkAutoClose.IsChecked.Value)
            {
                // Getting open wpf windows: Application.Current.Windows
                // Getting open winforms: System.Windows.Forms.Application.OpenForms

                // Can't autoclose if there are winforms open, or they will close too
                if (System.Windows.Forms.Application.OpenForms.Count == 0)
                {
                    this.Close();
                }
            }
        }

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
        #region Newton 1.53

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
            new Game.Newt.v1.AsteroidMiner1.AsteroidMiner2D_153.Miner2D().Show();
        }
        private void itemSwarmBots_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.v1.AsteroidMiner1.AsteroidMiner2D_153.SwarmBotTester().Show();
        }

        #endregion
        #region WPF Tests

        private void itemColorVisualizer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.ColorVisualizer().Show();
        }
        private void itemCameraTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.CameraTester().Show();
        }
        private void itemTransformTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.TransformTester().Show();
        }
        private void itemRotateDblVect_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.DoubleVectorWindow().Show();
        }
        private void itemTubeMeshTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.TubeMeshTester().Show();
        }
        private void itemSoundTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.SoundTester().Show();
        }
        private void itemPotatoes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.PotatoWindow().Show();
        }
        private void itemEvenDistribute_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.EvenDistributionSphere().Show();
        }
        private void itemClusteredPoints_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.ClusteredPoints().Show();
        }
        private void itemShadows_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.ShadowsWindow().Show();
        }
        private void itemCurves_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.Curves().Show();
        }

        #endregion
        #region Misc Tests

        private void itemNeuralNet_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.NeuralTester().Show();
        }
        private void itemPainter_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.FluidFields.FluidPainter2D().Show();
        }
        private void itemPainter3D_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.FluidFields.FluidPainter3D().Show();
        }

        #endregion
        #region Newton 2.36

        private void itemGlobalStats_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.GlobalItemStatsWindow().Show();
        }
        private void itemNewt2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.Newt2Tester.Newt2Tester().Show();
        }
        private void itemMultithreadWorlds_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.MultithreadWorlds().Show();
        }
        private void itemTowerWrecker_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.TowerWrecker.TowerWreckerWindow().Show();
        }
        private void itemWindTunnel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.WindTunnelWindow().Show();
        }
        private void itemWindTunnel2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.FluidFields.WindTunnel2().Show();
        }
        private void itemAsteroidField_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.v2.AsteroidMiner.AstField.AsteroidFieldWindow().Show();
        }
        private void itemShipEditor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.v2.GameItems.ShipEditor.ShipEditorWindow().Show();
        }
        private void itemShipPartTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.ShipPartTesterWindow().Show();
        }
        private void itemOverlappingPartsTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.OverlappingPartsWindow().Show();
        }
        private void itemBrainTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.BrainTester().Show();
        }
        private void itemBrainTester2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.BrainTester2().Show();
        }
        private void itemFlyingBeans_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.v2.FlyingBeans.FlyingBeansWindow().Show();
        }
        private void itemShipCameraTester_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.ShipCameraTester().Show();
        }
        private void itemGenePool_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // This should emulate gene pool

            // New parts needed:
            //      Camera, maybe sonar
            //      Collision sensors
            //      CargoBay
            //      MatterToFuel, MatterToEnergy, FuelToEnergy, EnergyToFuel (I think solar panels would be cheating for this simulation)
            //      Joints, Fins, Propellers
            //      EggProducer, or reproductive fluid container - something to siphon off energy so it can be used to make offspring

            // Also support static walls

            // Just choose one or two mineral types, maybe emerald for food, ruby for toxic food.  For now, if the ship collides with the food, that's
            // enough to pick it up

            // This should emulate fluid - need to calculate shadows so that only the parts in the wind are affected

            // This should have sexual reproduction.  Once a ship has enough reserves (either matter, energy, or some reproductive container), it needs
            // to touch another ship, and an offspring will be created from the two.  Make a free floating egg that hangs out for a few seconds before
            // turning into the final child ship.

            // When doing joints, an easy approach is to have jointed flippers, tails, propellers, etc attach to the main body.  So the real ship is still
            // rigid, and it has movable parts stuck to the sides.
            //
            // More complex is to have an arbitrary number of rigid chassis that are separated by joints.  The parts attach to those chassis.  Each chassis
            // will be invisible like the single one is now.

            new Game.Newt.v2.GenePool.GenePoolWindow().Show();
        }
        private void itemChaseForces_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.Testers.ChaseForces.ChaseForcesWindow().Show();
        }
        private void itemArcanorum_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.v2.Arcanorum.ArcanorumWindow().Show();
        }
        private void itemAstMiner2D_2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new Game.Newt.v2.AsteroidMiner.AstMin2D.Miner().Show();
        }

        #endregion

        private void SettingsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the settings folder in windows explorer
                System.Diagnostics.Process.Start(UtilityCore.GetOptionsFolder());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().Show();
        }

        // Future simulations
        private void itemPinkyTheBrain_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Make this a light RTS game.  Either on a flat map, or on the surface of a big sphere - try to give the map hills

            // Each player will be a scripted NPC, represented by a main building

            // There will be two types of mobile units.  These units will just be bricks or pucks that slide over the surface, there doesn't need to be
            // complex physics for legs or wheels.  The input for these units will be a disc of neurons that represent which direction to travel.
            //      Gatherers:
            //          When these get close enough to an energy fountain, they suck up energy, and instant transfer it back to base.
            //          Or, maybe have minerals lying around that they have to drive over.
            //      Soldiers
            //          These will auto shoot at anything that is not from their team

            // Make some kind of brain tower that acts as a spawn point, gun turret, sensor package, control center.  Maybe have it start as a mobile
            // unit, then when it gets to where it needs to go, it permanently converts into a brain tower.  (make it look sort of like deadmau5,
            // just to further the pun)



            // The mobile unit chassis will be hard coded, but will accept a shipdna payload.  This will be a brain, sensors, etc.
            // The brain tower will be similar, but have different capabilities



            // This would be a good tester for the BrainRadio shippart.  The main building, brain towers could have master radios, and the units
            // could have slave radios (one way communication).  There could also be two way BrainRadios.  This could allow for each team being
            // a single distributed organism.


            // ----------------------------------------------

            // Let the human player temporarily override the functions of an NPC team (set waypoints, decide which units to spawn).  Or let them
            // play at a higher level and edit the map, spawn/destroy/move units, etc.

            // Or let the human player be their own, and emulate aspects of the game Sacrifice - just because it's an awesome game
        }
        private void itemSwarmBots2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Take another stab at building swarm bots

            // Most logic is scripted, use neural for fine tuned movement

            // Might be a good use for neural that learns?


            // Parts:
            //      Thrusters
            //      Tractor Beams


            // Orders:
            //      Follow
            //      Strike
            //      Move object to point (or at least in a direction)


        }

        #endregion

        #region Private Methods

        private void LoadOptionsFromFile()
        {
            MainFormOptions options = UtilityCore.ReadOptions<MainFormOptions>(FILE) ?? new MainFormOptions();

            chkAutoClose.IsChecked = options.ShouldAutoClose ?? false;
        }
        private void SaveOptionsToFile()
        {
            MainFormOptions options = new MainFormOptions();

            options.ShouldAutoClose = chkAutoClose.IsChecked.Value;

            UtilityCore.SaveOptions(options, FILE);
        }

        #endregion
    }

    #region Class: MainFormOptions

    /// <summary>
    /// This class gets saved to xaml in their appdata folder
    /// NOTE: All properties are nullable so that new ones can be added, and an old xml will still load
    /// NOTE: Once a property is added, it can never be removed (or an old config will bomb the deserialize)
    /// </summary>
    /// <remarks>
    /// I didn't want this class to be public, but XamlServices fails otherwise
    /// </remarks>
    public class MainFormOptions
    {
        public bool? ShouldAutoClose
        {
            get;
            set;
        }
    }

    #endregion
}
