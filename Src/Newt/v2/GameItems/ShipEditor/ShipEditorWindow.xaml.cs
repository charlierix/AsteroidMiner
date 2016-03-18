using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xaml;

using Microsoft.Win32;

using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    public partial class ShipEditorWindow : Window
    {
        #region Declaration Section

        //public const string SHIPFOLDER = "Asteroid Miner\\Ships";
        public const string SHIPFOLDER = "Ships";		//UtilityHelper.GetOptionsFolder() gets up to Asteroid Miner, this is a subfolder for ships

        private List<PartToolItemBase> _partToolItems = new List<PartToolItemBase>();

        #endregion

        #region Constructor

        public ShipEditorWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Each part defines a default tab that it wants to be in.  I'll just use those defaults
            _partToolItems.Add(new CargoBayToolItem(editor1.Options));
            _partToolItems.Add(new EnergyTankToolItem(editor1.Options));
            _partToolItems.Add(new FuelTankToolItem(editor1.Options));
            _partToolItems.Add(new PlasmaTankToolItem(editor1.Options));
            _partToolItems.Add(new AmmoBoxToolItem(editor1.Options));
            _partToolItems.Add(new HangarBayToolItem(editor1.Options));
            _partToolItems.Add(new ConverterMatterToEnergyToolItem(editor1.Options));
            _partToolItems.Add(new ConverterMatterToFuelToolItem(editor1.Options));
            _partToolItems.Add(new ConverterMatterToAmmoToolItem(editor1.Options));
            _partToolItems.Add(new ConverterEnergyToFuelToolItem(editor1.Options));
            _partToolItems.Add(new ConverterEnergyToAmmoToolItem(editor1.Options));
            _partToolItems.Add(new ConverterFuelToEnergyToolItem(editor1.Options));
            _partToolItems.Add(new ConverterRadiationToEnergyToolItem(editor1.Options, SolarPanelShape.Triangle));
            _partToolItems.Add(new ConverterRadiationToEnergyToolItem(editor1.Options, SolarPanelShape.Right_Triangle));
            _partToolItems.Add(new ConverterRadiationToEnergyToolItem(editor1.Options, SolarPanelShape.Square));
            _partToolItems.Add(new ConverterRadiationToEnergyToolItem(editor1.Options, SolarPanelShape.Trapazoid));
            _partToolItems.Add(new ConverterRadiationToEnergyToolItem(editor1.Options, SolarPanelShape.Right_Trapazoid));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, ThrusterType.One));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, ThrusterType.Two));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, ThrusterType.Two_One));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, ThrusterType.Two_Two));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, ThrusterType.Two_Two_One));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, ThrusterType.Two_Two_Two));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, new[] { new Vector3D(1, 0, 0), new Vector3D(0, 0, 1) }, "elbow"));
            _partToolItems.Add(new ThrusterToolItem(editor1.Options, new[] { new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1) }, "elbow 3D"));
            _partToolItems.Add(new TractorBeamToolItem(editor1.Options));
            _partToolItems.Add(new BrainToolItem(editor1.Options));
            _partToolItems.Add(new BrainRGBRecognizerToolItem(editor1.Options));
            //_partToolItems.Add(new EyeToolItem(editor1.Options));
            _partToolItems.Add(new CameraColorRGBToolItem(editor1.Options));
            _partToolItems.Add(new SensorGravityToolItem(editor1.Options));
            _partToolItems.Add(new SensorRadiationToolItem(editor1.Options));
            _partToolItems.Add(new SensorTractorToolItem(editor1.Options));
            _partToolItems.Add(new SensorCollisionToolItem(editor1.Options));
            _partToolItems.Add(new SensorFluidToolItem(editor1.Options));
            _partToolItems.Add(new SensorSpinToolItem(editor1.Options));
            _partToolItems.Add(new SensorVelocityToolItem(editor1.Options));
            _partToolItems.Add(new SensorInternalForceToolItem(editor1.Options));
            _partToolItems.Add(new SensorNetForceToolItem(editor1.Options));
            _partToolItems.Add(new ShieldEnergyToolItem(editor1.Options));
            _partToolItems.Add(new ShieldKineticToolItem(editor1.Options));
            _partToolItems.Add(new ShieldTractorToolItem(editor1.Options));
            _partToolItems.Add(new ProjectileGunToolItem(editor1.Options));
            _partToolItems.Add(new BeamGunToolItem(editor1.Options));
            _partToolItems.Add(new GrappleGunToolItem(editor1.Options));
            _partToolItems.Add(new SelfRepairToolItem(editor1.Options));

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;

            Button button = new Button();
            button.Content = "Save";
            button.Click += new RoutedEventHandler(Save_Click);
            panel.Children.Add(button);

            button = new Button();
            button.Content = "Load";
            button.Margin = new Thickness(4d, 0, 0, 0);
            button.Click += new RoutedEventHandler(Load_Click);
            panel.Children.Add(button);

            editor1.SetupEditor(this.Title, _partToolItems, panel);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveShip();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the current ship from the editor
                string name;		// the editor returns this trimmed
                List<string> layerNames;
                SortedList<int, List<DesignPart>> partsByLayer;
                editor1.GetDesign(out name, out layerNames, out partsByLayer);

                if (partsByLayer.Values.SelectMany(o => o).Count() > 0)
                {
                    #region Prompt for save

                    string prompt = "Save current ship first?";
                    if (name == "")
                    {
                        prompt += "\r\n\r\nYou'll need to give it a name first";
                    }

                    MessageBoxResult result = MessageBox.Show(prompt, this.Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            if (name == "")
                            {
                                return;		// they need to name it first
                            }
                            else
                            {
                                SaveShip();
                            }
                            break;

                        case MessageBoxResult.No:
                            break;

                        case MessageBoxResult.Cancel:
                            return;

                        default:
                            throw new ApplicationException("Unexpected MessageBoxResult: " + result.ToString());
                    }

                    #endregion
                }

                LoadShip();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Public Methods

        public static void SaveShip(ShipDNA ship)
        {
            //TODO: Store these in a database (RavenDB), fail over to storing on file if can't connect to DB


            // Make sure the folder exists
            string foldername = System.IO.Path.Combine(UtilityCore.GetOptionsFolder(), SHIPFOLDER);
            if (!Directory.Exists(foldername))
            {
                Directory.CreateDirectory(foldername);
            }

            //string xamlText = XamlWriter.Save(ship);		// this is the old one, it doesn't like generic lists
            string xamlText = XamlServices.Save(ship);

            int infiniteLoopDetector = 0;
            while (true)
            {
                char[] illegalChars = System.IO.Path.GetInvalidFileNameChars();
                string filename = new string(ship.ShipName.Select(o => illegalChars.Contains(o) ? '_' : o).ToArray());
                filename = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff") + " - " + filename + ".xml";
                filename = System.IO.Path.Combine(foldername, filename);

                try
                {
                    using (StreamWriter writer = new StreamWriter(new FileStream(filename, FileMode.CreateNew)))
                    {
                        writer.Write(xamlText);
                        break;
                    }
                }
                catch (IOException ex)
                {
                    infiniteLoopDetector++;
                    if (infiniteLoopDetector > 100)
                    {
                        throw new ApplicationException("Couldn't create the file\r\n" + filename, ex);
                    }
                }
            }
        }
        public static ShipDNA LoadShip(out string errMsg)
        {
            string foldername = System.IO.Path.Combine(UtilityCore.GetOptionsFolder(), SHIPFOLDER);
            Directory.CreateDirectory(foldername);

            // Even if the folder is empty, they may want a folder nearby
            //if (!Directory.Exists(foldername) || Directory.GetFiles(foldername).Length == 0)
            //{
            //    errMsg = "No existing ships were found";
            //    return null;
            //}

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = foldername;
            dialog.Multiselect = false;
            dialog.Title = "Please select a ship";
            bool? result = dialog.ShowDialog();
            if (result == null || !result.Value)
            {
                errMsg = "";
                return null;
            }

            // Load the file
            object deserialized = null;
            using (FileStream file = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //deserialized = XamlReader.Load(file);		// this is the old way, it doesn't like generic lists
                deserialized = XamlServices.Load(file);
            }

            ShipDNA retVal = deserialized as ShipDNA;

            // Exit Function
            errMsg = "";
            return retVal;
        }

        #endregion

        #region Private Methods

        private void SaveShip()
        {
            // Get ship from the editor
            string name;		// the editor returns this trimmed
            List<string> layerNames;
            SortedList<int, List<DesignPart>> partsByLayer;
            editor1.GetDesign(out name, out layerNames, out partsByLayer);

            if (name == "")
            {
                MessageBox.Show("Please give the ship a name first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get the definition of the ship
            ShipDNA ship = new ShipDNA();
            ship.ShipName = name;
            //TODO: Validate the ship
            //ship.IsValid = 
            ship.LayerNames = layerNames;
            ship.PartsByLayer = new SortedList<int, List<ShipPartDNA>>();
            foreach (int layerIndex in partsByLayer.Keys)
            {
                ship.PartsByLayer.Add(layerIndex, partsByLayer[layerIndex].Select(o => o.Part3D.GetDNA()).ToList());
            }

            SaveShip(ship);
        }
        private void LoadShip()
        {
            string errMsg;
            ShipDNA ship = LoadShip(out errMsg);
            if (ship == null)
            {
                if (!string.IsNullOrEmpty(errMsg))
                {
                    MessageBox.Show(errMsg, this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Convert dna into design parts
            SortedList<int, List<DesignPart>> partsByLayer = new SortedList<int, List<DesignPart>>();
            foreach (int layerIndex in ship.PartsByLayer.Keys)
            {
                partsByLayer.Add(layerIndex, ship.PartsByLayer[layerIndex].Select(o => CreateDesignPart(o)).ToList());
            }

            // Show the ship
            editor1.SetDesign(ship.ShipName, ship.LayerNames, partsByLayer);
        }

        private DesignPart CreateDesignPart(ShipPartDNA dna)
        {
            #region Find ToolItem

            // Find the corresponding tool item
            List<PartToolItemBase> toolItems = _partToolItems.Where(o => o.PartType == dna.PartType).ToList();
            if (toolItems.Count == 0)
            {
                throw new ApplicationException("Couldn't find the tool item for \"" + dna.PartType + "\"");
            }

            // Narrow down to one
            PartToolItemBase toolItem = null;
            if (toolItems.Count == 1)		// if there's more than one, then it needs to be further filtered
            {
                toolItem = toolItems[0];
            }
            else if (dna.PartType == ConverterRadiationToEnergy.PARTTYPE)
            {
                #region ConverterRadiationToEnergy

                ConverterRadiationToEnergyDNA dnaCast = (ConverterRadiationToEnergyDNA)dna;

                List<PartToolItemBase> additionalFilter = toolItems.Where(o => ((ConverterRadiationToEnergyToolItem)o).Shape == dnaCast.Shape).ToList();
                if (additionalFilter.Count == 1)
                {
                    toolItem = additionalFilter[0];
                }

                #endregion
            }
            else if (dna.PartType == Thruster.PARTTYPE)
            {
                #region Thruster

                ThrusterDNA dnaCast = (ThrusterDNA)dna;
                if (dnaCast.ThrusterType == ThrusterType.Custom)
                {
                    // Make a new one with the dna's directions
                    toolItem = new ThrusterToolItem(editor1.Options, dnaCast.ThrusterDirections, "Custom");
                }
                else
                {
                    List<PartToolItemBase> additionalFilter = toolItems.Where(o => ((ThrusterToolItem)o).ThrusterType == dnaCast.ThrusterType).ToList();
                    if (additionalFilter.Count == 1)
                    {
                        toolItem = additionalFilter[0];
                    }
                }

                #endregion
            }
            else
            {
                throw new ApplicationException("Should have only found one tool item for this part type: " + dna.PartType);
            }

            if (toolItem == null)
            {
                throw new ApplicationException("Couldn't find the tool item for this part type: " + dna.PartType);
            }

            #endregion

            DesignPart retVal = new DesignPart(editor1.Options);
            retVal.Part2D = toolItem;

            retVal.Part3D = toolItem.GetNewDesignPart();
            retVal.Part3D.SetDNA(dna);

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = retVal.Part3D.Model;
            retVal.Model = visual;

            return retVal;

            #region OLD

            ////TODO: Instead of a switch, have tool item take in a dna, and make its own design (make the method abstract)
            //switch (dna.PartType)
            //{
            //    case "CargoBay":
            //        retVal.Part3D = new CargoBayDesign(editor1.Options);
            //        break;

            //    case "EnergyTank":
            //        retVal.Part3D = new EnergyTankDesign(editor1.Options);
            //        break;

            //    case "FuelTank":
            //        retVal.Part3D = new FuelTankDesign(editor1.Options);
            //        break;

            //    case "AmmoBox":
            //        retVal.Part3D = new AmmoBoxDesign(editor1.Options);
            //        break;

            //    case "HangarBay":
            //        retVal.Part3D = new HangarBayDesign(editor1.Options);
            //        break;

            //    case "ConverterMatterToEnergy":
            //        retVal.Part3D = new ConverterMatterToEnergyDesign(editor1.Options);
            //        break;

            //    case "ConverterMatterToFuel":
            //        retVal.Part3D = new ConverterMatterToFuelDesign(editor1.Options);
            //        break;

            //    case "ConverterMatterToAmmo":
            //        retVal.Part3D = new ConverterMatterToAmmoDesign(editor1.Options);
            //        break;

            //    case "ConverterEnergyToFuel":
            //        retVal.Part3D = new ConverterEnergyToFuelDesign(editor1.Options);
            //        break;

            //    case "ConverterEnergyToAmmo":
            //        retVal.Part3D = new ConverterEnergyToAmmoDesign(editor1.Options);
            //        break;

            //    case "ConverterFuelToEnergy":
            //        retVal.Part3D = new ConverterFuelToEnergyDesign(editor1.Options);
            //        break;


            //                    else if (dna.PartType == "ConverterRadiationToEnergy")
            //{
            //    //retVal.Part3D = new ConverterRadiationToEnergyDesign(editor1.Options);
            //}
            //else if (dna.PartType == "Thruster")
            //{
            //    //retVal.Part3D = new ThrusterDesign(editor1.Options);
            //}

            //    case "TractorBeam":
            //        retVal.Part3D = new TractorBeamDesign(editor1.Options);
            //        break;

            //    case "Brain":
            //        retVal.Part3D = new BrainDesign(editor1.Options);
            //        break;

            //    case "Eye":
            //        retVal.Part3D = new EyeDesign(editor1.Options);
            //        break;

            //    case "ShieldEnergy":
            //        retVal.Part3D = new ShieldEnergyDesign(editor1.Options);
            //        break;

            //    case "ShieldKinetic":
            //        retVal.Part3D = new ShieldKineticDesign(editor1.Options);
            //        break;

            //    case "ShieldTractor":
            //        retVal.Part3D = new ShieldTractorDesign(editor1.Options);
            //        break;

            //    case "ProjectileGun":
            //        retVal.Part3D = new ProjectileGunDesign(editor1.Options);
            //        break;

            //    case "BeamGun":
            //        retVal.Part3D = new BeamGunDesign(editor1.Options);
            //        break;

            //    case "GrappleGun":
            //        retVal.Part3D = new GrappleGunDesign(editor1.Options);
            //        break;

            //    case "SelfRepair":
            //        retVal.Part3D = new SelfRepairDesign(editor1.Options);
            //        break;

            //    default:
            //        throw new ApplicationException("Unknown part type: " + dna.PartType);
            //}

            #endregion
        }

        #endregion
    }
}
