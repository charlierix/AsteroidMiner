using System;
using System.Collections.Generic;
using System.IO;		//TODO: This is for save/load, which belongs in the calling window
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
using System.Xaml;		//TODO: This is for save/load, which belongs in the calling window

using Microsoft.Win32;		//TODO: This is for save/load, which belongs in the calling window

using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace Game.Newt.Testers
{
	public partial class ShipEditorWindow : Window
	{
		#region Declaration Section

		private const string SHIPFOLDER = "Asteroid Miner\\Ships";

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
			//	Each part defines a default tab that it wants to be in.  I'll just use those defaults
			_partToolItems.Add(new CargoBayToolItem(editor1.Options));
			_partToolItems.Add(new EnergyTankToolItem(editor1.Options));
			_partToolItems.Add(new FuelTankToolItem(editor1.Options));
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
			_partToolItems.Add(new TractorBeamToolItem(editor1.Options));
			_partToolItems.Add(new BrainToolItem(editor1.Options));
			_partToolItems.Add(new EyeToolItem(editor1.Options));
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
				//	Get the current ship from the editor
				string name;		//	the editor returns this trimmed
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
								return;		//	they need to name it first
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

		#region Private Methods

		private void SaveShip()
		{
			//	Get ship from the editor
			string name;		//	the editor returns this trimmed
			List<string> layerNames;
			SortedList<int, List<DesignPart>> partsByLayer;
			editor1.GetDesign(out name, out layerNames, out partsByLayer);

			if (name == "")
			{
				MessageBox.Show("Please give the ship a name first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			//	Get the definition of the ship
			ShipDNA ship = new ShipDNA();
			ship.ShipName = name;
			//TODO: Validate the ship
			//ship.IsValid = 
			ship.LayerNames = layerNames;
			ship.PartsByLayer = new SortedList<int, List<PartDNA>>();
			foreach (int layerIndex in partsByLayer.Keys)
			{
				ship.PartsByLayer.Add(layerIndex, partsByLayer[layerIndex].Select(o => o.Part3D.GetDNA()).ToList());
			}


			//TODO: Store these in a database (RavenDB), fail over to storing on file if can't connect to DB


			//	Make sure the folder exists
			string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			foldername = System.IO.Path.Combine(foldername, SHIPFOLDER);
			if (!Directory.Exists(foldername))
			{
				Directory.CreateDirectory(foldername);
			}

			//string xamlText = XamlWriter.Save(ship);		//	this is the old one, it doesn't like generic lists
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
		private void LoadShip()
		{
			string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			foldername = System.IO.Path.Combine(foldername, SHIPFOLDER);

			if (!Directory.Exists(foldername) || Directory.GetFiles(foldername).Length == 0)
			{
				MessageBox.Show("No existing ships were found", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			OpenFileDialog dialog = new OpenFileDialog();
			dialog.InitialDirectory = foldername;
			dialog.Multiselect = false;
			dialog.Title = "Please select a ship";
			bool? result = dialog.ShowDialog();
			if (result == null || !result.Value)
			{
				return;
			}

			//	Load the file
			object deserialized = null;
			using (FileStream file = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				//deserialized = XamlReader.Load(file);		//	this is the old way, it doesn't like generic lists
				deserialized = XamlServices.Load(file);
			}

			ShipDNA ship = deserialized as ShipDNA;

			//	Convert dna into design parts
			SortedList<int, List<DesignPart>> partsByLayer = new SortedList<int, List<DesignPart>>();
			foreach (int layerIndex in ship.PartsByLayer.Keys)
			{
				partsByLayer.Add(layerIndex, ship.PartsByLayer[layerIndex].Select(o => CreateDesignPart(o)).ToList());
			}

			//	Show the ship
			editor1.SetDesign(ship.ShipName, ship.LayerNames, partsByLayer);
		}

		private DesignPart CreateDesignPart(PartDNA dna)
		{
			#region Find ToolItem

			//	Find the corresponding tool item
			List<PartToolItemBase> toolItems = _partToolItems.Where(o => o.PartType == dna.PartType).ToList();
			if (toolItems.Count == 0)
			{
				throw new ApplicationException("Couldn't find the tool item for \"" + dna.PartType + "\"");
			}

			//	Narrow down to one
			PartToolItemBase toolItem = null;
			if (toolItems.Count == 1)		//	if there's more than one, then it needs to be further filtered
			{
				toolItem = toolItems[0];
			}
			else if (dna.PartType == "ConverterRadiationToEnergy")
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
			else if (dna.PartType == "Thruster")
			{
				#region Thruster

				ThrusterDNA dnaCast = (ThrusterDNA)dna;
				if (dnaCast.ThrusterType == ThrusterType.Custom)
				{
					throw new ApplicationException("Currently don't support custom thrusters");
				}

				List<PartToolItemBase> additionalFilter = toolItems.Where(o => ((ThrusterToolItem)o).ThrusterType == dnaCast.ThrusterType).ToList();
				if (additionalFilter.Count == 1)
				{
					toolItem = additionalFilter[0];
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

			ModelVisual3D model = new ModelVisual3D();
			model.Content = retVal.Part3D.Model;
			retVal.Model = model;

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
