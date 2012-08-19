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
using System.Windows.Navigation;
using System.Windows.Shapes;

using Game.Newt.AsteroidMiner_153;
using Game.Newt.HelperClasses;

namespace Game.Newt.Testers.AsteroidMiner2D_153
{
	public partial class SpaceDockPanel : UserControl
	{
		#region Events

		public event EventHandler LaunchShip = null;

		#endregion

		#region Declaration Section

		private const string SWARMRADIOPREFIX = "radSwarm";

		private Random _rand = new Random();

		private Ship _ship = null;
		private SpaceStation _station = null;

		private List<Viewport3D> _mineralViewers = new List<Viewport3D>();

		private bool _wasSwarmTrackChanged = false;

		private bool _programaticallySettingSwarmSettings = false;

		private bool _isInitialized = false;

		#endregion

		#region Constructor

		public SpaceDockPanel()
		{
			InitializeComponent();

			#region Swarmbot Choices

			_programaticallySettingSwarmSettings = true;

			AddSwarmRadioButton(Ship.SwarmFormation.None, "No swarmbots", true);
			AddSwarmRadioButton(Ship.SwarmFormation.SurroundShip, "Surround ship", false);
			AddSwarmRadioButton(Ship.SwarmFormation.AllFront, "All in front", false);
			AddSwarmRadioButton(Ship.SwarmFormation.AllRear, "All behind", false);
			AddSwarmRadioButton(Ship.SwarmFormation.Triangle, "Triangle (point in front)", false);
			AddSwarmRadioButton(Ship.SwarmFormation.ReverseTriangle, "Triangle (point in rear)", false);
			AddSwarmRadioButton(Ship.SwarmFormation.Pentagon, "Pentagon (point in front)", false);
			AddSwarmRadioButton(Ship.SwarmFormation.ReversePentagon, "Pentagon (point in rear)", false);

			_programaticallySettingSwarmSettings = true;

			#endregion

			_isInitialized = true;

			//	Make the form look right
			trkNumSwarmBots_ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(trkNumSwarmBots.Value, trkNumSwarmBots.Value));
		}

		#endregion

		#region Public Methods

		public void ShipDocking(Ship ship, SpaceStation station)
		{
			_ship = ship;
			_station = station;

			//NOTE:  The credits can be non integer, but I will just round down (actual purchaces may be portions of a dollar though)
			lblCredits.Content = _ship.Credits.ToString("N0");
			lblRefuelCredits.Content = (Convert.ToDecimal(_ship.FuelQuantityMax - _ship.FuelQuantityCurrent) * _station.GetFuelValue()).ToString("N0");

			LoadResourcePanel();

			_programaticallySettingSwarmSettings = true;

			#region Set swarmbot formation

			string currentFormation = SWARMRADIOPREFIX + _ship.Swarmbots.ToString();

			foreach (UIElement child in pnlSwarmBots.Children)
			{
				RadioButton radio = child as RadioButton;
				if (radio == null)
				{
					continue;
				}

				if (radio.Name == currentFormation)
				{
					radio.IsChecked = true;
					break;
				}
			}

			#endregion

			trkNumSwarmBots.Value = _ship.NumSwarmbots;

			_programaticallySettingSwarmSettings = false;
		}

		#endregion

		#region Event Listeners

		private void trkNumSwarmBots_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!_isInitialized)
			{
				return;
			}

			lblNumSwarmBots.Text = trkNumSwarmBots.Value.ToString("N0");

			//	mouseup will make the swarm bots (I was getting memory exceptions when I destroyed/created bots too fast on the value
			//	change event)
			//NOTE:  This will fail if they use arrow keys, but they won't  :)
			_wasSwarmTrackChanged = true;		
		}
		private void trkNumSwarmBots_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_wasSwarmTrackChanged = false;
		}
		private void trkNumSwarmBots_MouseUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (!_isInitialized || !_wasSwarmTrackChanged)
				{
					return;
				}

				if (_ship != null && !_programaticallySettingSwarmSettings)
				{
					_ship.NumSwarmbots = Convert.ToInt32(trkNumSwarmBots.Value);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Space Dock", MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}
		private void chkSwarmbotsSameSize_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isInitialized)
				{
					return;
				}

				if (_ship != null && !_programaticallySettingSwarmSettings)
				{
					_ship.AreSwarmbotsUniformSize = chkSwarmbotsSameSize.IsChecked.Value;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Space Dock", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void SwarmRadio_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isInitialized || _programaticallySettingSwarmSettings)
				{
					return;
				}

				RadioButton senderCast = sender as RadioButton;
				if (senderCast == null)
				{
					MessageBox.Show("Unknown radio button", "Space Dock", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				//	Figure out what they clicked on
				Ship.SwarmFormation formation = (Ship.SwarmFormation)Enum.Parse(typeof(Ship.SwarmFormation), senderCast.Name.Substring(SWARMRADIOPREFIX.Length));

				if (formation == _ship.Swarmbots)
				{
					return;
				}

				//TODO:  Exchange money first

				_ship.Swarmbots = formation;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Space Dock", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnSellAllResources_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				decimal cargoValue = 0m;
				foreach (Mineral mineral in _ship.CargoBayContents)
				{
					decimal mineralValue = _station.GetMineralValue(mineral.MineralType);

					cargoValue += mineralValue;
				}

				_ship.SellCargo(cargoValue);

				//lblTotalResourceValue.Content = "0";    // done in the clear method
				lblCredits.Content = _ship.Credits.ToString("N0");

				ClearResourcePanel();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Space Dock", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnRefuel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_ship.Credits == 0m || _ship.FuelQuantityMax == _ship.FuelQuantityCurrent)
				{
					//TODO:  Play a cancelled sound
					return;
				}

				double fuel = _ship.FuelQuantityMax - _ship.FuelQuantityCurrent;

				//	Figure out how much to spend
				decimal cost = Convert.ToDecimal(fuel) * _station.GetFuelValue();
				if (cost > _ship.Credits)
				{
					cost = _ship.Credits;

					//	Buy a subset of fuel
					fuel = Convert.ToDouble(cost / _station.GetFuelValue());
				}

				//	Buy it
				_ship.BuyFuel(fuel, cost);

				lblRefuelCredits.Content = (Convert.ToDecimal(_ship.FuelQuantityMax - _ship.FuelQuantityCurrent) * _station.GetFuelValue()).ToString("N0");
				lblCredits.Content = _ship.Credits.ToString("N0");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Space Dock", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnLaunch_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (this.LaunchShip == null)
				{
					MessageBox.Show("There is no event listener for the launch button", "Launch Button", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				// This panel is about to close, so forget my reference to the ship and station
				_ship = null;
				_station = null;

				this.LaunchShip(this, new EventArgs());
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Space Dock", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private void ClearResourcePanel()
		{
			grdResources.Children.Clear();
			grdResources.RowDefinitions.Clear();
			lblTotalResourceValue.Content = "0";

			foreach (Viewport3D viewport in _mineralViewers)
			{
				//TODO:  Figure out how to preserve the lights
				viewport.Children.Clear();
			}
		}

		private void LoadResourcePanel()
		{
			// Init
			ClearResourcePanel();
			if (_ship.CargoBayContents.Count == 0)
			{
				return;
			}

			#region Group the cargo by type

			SortedList<MineralType, List<Mineral>> mineralsByType = new SortedList<MineralType, List<Mineral>>();
			foreach (Mineral mineral in _ship.CargoBayContents)
			{
				if (!mineralsByType.ContainsKey(mineral.MineralType))
				{
					mineralsByType.Add(mineral.MineralType, new List<Mineral>());
				}

				mineralsByType[mineral.MineralType].Add(mineral);
			}

			#endregion

			#region Get the prices

			// (and the total while I'm at it)
			decimal cargoValue = 0m;
			List<decimal> mineralTypePrices = new List<decimal>();

			foreach (MineralType mineralType in mineralsByType.Keys)
			{
				decimal price = _station.GetMineralValue(mineralType);
				mineralTypePrices.Add(price);

				cargoValue += price * mineralsByType[mineralType].Count;
			}

			#endregion

			#region Sort by price

			// Build indicies that point to the keys
			int[] indices = new int[mineralsByType.Keys.Count];
			for (int cntr = 0; cntr < indices.Length; cntr++)
			{
				indices[cntr] = cntr;
			}

			// Sort by value
			Array.Sort(mineralTypePrices.ToArray(), indices);

			// The key pointed to by indices[0] is the cheapest and indices[len - 1] is the most expensive

			#endregion

			#region Rebuild Grid

			for (int cntr = 0; cntr < indices.Length; cntr++)
			{
				RowDefinition rowDef = new RowDefinition();
				rowDef.Height = new GridLength(1, GridUnitType.Auto);
				grdResources.RowDefinitions.Add(rowDef);

				List<Mineral> rowMinerals = mineralsByType[mineralsByType.Keys[indices[cntr]]];
				decimal price = mineralTypePrices[indices[cntr]];

				// Column 0 - Icon
				#region Icon

				if (cntr >= _mineralViewers.Count)
				{
					_mineralViewers.Add(CreateMineralViewport());
				}

				AddMineralToViewport(_mineralViewers[cntr], rowMinerals[0]);

				Grid.SetColumn(_mineralViewers[cntr], 0);
				Grid.SetRow(_mineralViewers[cntr], cntr);
				grdResources.Children.Add(_mineralViewers[cntr]);

				#endregion

				// Column 1 - Name
				TextBlock textBlock = new TextBlock();
				textBlock.HorizontalAlignment = HorizontalAlignment.Left;
				textBlock.VerticalAlignment = VerticalAlignment.Center;
				textBlock.Margin = new Thickness(2, 0, 0, 0);
				textBlock.Text = rowMinerals[0].MineralType.ToString();
				Grid.SetColumn(textBlock, 1);
				Grid.SetRow(textBlock, cntr);
				grdResources.Children.Add(textBlock);

				// Column 2 - Price x Quantity
				textBlock = new TextBlock();
				textBlock.HorizontalAlignment = HorizontalAlignment.Left;
				textBlock.VerticalAlignment = VerticalAlignment.Center;
				textBlock.Text = price.ToString("N0") + " x " + rowMinerals.Count.ToString();
				Grid.SetColumn(textBlock, 2);
				Grid.SetRow(textBlock, cntr);
				grdResources.Children.Add(textBlock);

				// Column 3 - Total Price
				textBlock = new TextBlock();
				textBlock.HorizontalAlignment = HorizontalAlignment.Right;
				textBlock.VerticalAlignment = VerticalAlignment.Center;
				textBlock.Margin = new Thickness(0, 0, 2, 0);
				textBlock.Text = (price * rowMinerals.Count).ToString("N0");
				Grid.SetColumn(textBlock, 3);
				Grid.SetRow(textBlock, cntr);
				grdResources.Children.Add(textBlock);
			}

			#endregion

			lblTotalResourceValue.Content = cargoValue.ToString("N0");
		}

		private Viewport3D CreateMineralViewport()
		{
			Viewport3D retVal = new Viewport3D();

			retVal.Camera = new PerspectiveCamera(new Point3D(0, 0, 2.5), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45);

			//TODO:  Figure out how to set up the lights only once

			retVal.Width = 38;
			retVal.Height = 38;

			return retVal;
		}
		private void AddMineralToViewport(Viewport3D viewport, Mineral mineral)
		{
			#region Add lights

			Model3DGroup lightGroup = new Model3DGroup();
			lightGroup.Children.Add(new AmbientLight(Colors.DimGray));
			lightGroup.Children.Add(new DirectionalLight(Colors.Silver, new Vector3D(1, -1, -1)));

			ModelVisual3D lights = new ModelVisual3D();
			lights.Content = lightGroup;

			viewport.Children.Add(lights);

			#endregion

			//NOTE:  This relies on the fact that the mineral was removed from the main screen (the visuals can only belong to
			// one viewport at a time)
			foreach (ModelVisual3D visual in mineral.Visuals3D)
			{
				// I can't use this one, because the rixium rings will get screwed up
				//visual.Transform = new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10d), Math3D.GetNearZeroValue(_rand, 360d)));
				visual.Transform = new TranslateTransform3D();

				viewport.Children.Add(visual);
			}
		}

		private void AddSwarmRadioButton(Ship.SwarmFormation formation, string text, bool isChecked)
		{
			RadioButton radio = new RadioButton();
			radio.Name = SWARMRADIOPREFIX + formation.ToString();
			radio.Content = text;
			radio.IsChecked = isChecked;
			radio.Checked += new RoutedEventHandler(SwarmRadio_Checked);
			radio.Margin = new Thickness(2);
			radio.FontSize = 12;		//	stupid things inherit from the expander's font size

			pnlSwarmBots.Children.Add(radio);
		}

		#endregion
	}
}
