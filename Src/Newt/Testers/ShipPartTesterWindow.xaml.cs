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

using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.NewtonDynamics;
using Game.Newt.HelperClasses.Primitives3D;
using Game.Newt.HelperClasses;

namespace Game.Newt.Testers
{
	public partial class ShipPartTesterWindow : Window
	{
		#region Class: TempBody

		/// <summary>
		/// This is a body that shouldn't stay around forever
		/// </summary>
		private class TempBody
		{
			#region Constructor

			public TempBody(Body body, TimeSpan maxAge)
			{
				this.IsTimeLimited = true;

				this.PhysicsBody = body;

				this.CreateTime = DateTime.Now;
				this.MaxAge = maxAge;

				this.MaxDistance = 0d;
				this.MaxDistanceSquared = 0d;
			}
			public TempBody(Body body, double maxDistance)
			{
				this.IsTimeLimited = true;

				this.PhysicsBody = body;

				this.MaxDistance = maxDistance;
				this.MaxDistanceSquared = maxDistance * maxDistance;

				this.CreateTime = DateTime.MinValue;
				this.MaxAge = new TimeSpan();
			}

			#endregion

			#region Public Properties

			public readonly Body PhysicsBody;

			public readonly bool IsTimeLimited;

			//	These have meaning if IsTimeLimited is true
			public readonly DateTime CreateTime;
			public readonly TimeSpan MaxAge;

			//	This has meaning if IsTimeLimited is false
			public readonly double MaxDistance;
			public readonly double MaxDistanceSquared;

			#endregion

			#region Public Methods

			public bool ShouldDie()
			{
				if (this.IsTimeLimited)
				{
					return DateTime.Now - this.CreateTime > MaxAge;
				}
				else
				{
					return this.PhysicsBody.Position.ToVector().LengthSquared > this.MaxDistanceSquared;
				}
			}

			#endregion
		}

		#endregion

		#region Declaration Section

		#region Class: ItemColors

		private class ItemColors
		{
			//Color1
			//ctrllight: E5D5A4
			//ctrl: BFB289
			//ctrldark: 6B644C
			//brown: 473C33
			//red: 941300

			//Color2
			//ctrllight: F2EFDC
			//ctrl: D9D2B0
			//ctrldark: BFB28A
			//olive: 595139
			//burntorange: 732C03

			public Color BoundryLines = Colors.Silver;

			public Color MassBall = Colors.Orange;
			public Color MassBallReflect = Colors.Yellow;
			public double MassBallReflectIntensity = 50d;
		}

		#endregion

		private Random _rand = new Random();

		private bool _isInitialized = false;

		private EditorOptions _editorOptions = new EditorOptions();
		private ItemOptions _itemOptions = new ItemOptions();
		private ItemColors _colors = new ItemColors();

		private Point3D _boundryMin;
		private Point3D _boundryMax;

		private World _world = null;
		private Map _map = null;
		private RadiationField _radiation = null;

		private Ship _ship = null;

		private MaterialManager _materialManager = null;
		private int _material_Asteroid = -1;
		private int _material_Ship = -1;
		private int _material_Sand = -1;

		private ScreenSpaceLines3D _boundryLines = null;

		/// <summary>
		/// This listens to the mouse/keyboard and controls the camera
		/// </summary>
		/// <remarks>
		/// I'll only create this when debuging
		/// </remarks>
		private TrackBallRoam _trackball = null;

		private List<Visual3D> _currentVisuals = new List<Visual3D>();
		private Body _currentBody = null;
		private Ship _currentShip = null;

		private List<TempBody> _tempBodies = new List<TempBody>();
		private DateTime _lastSandAdd = DateTime.MinValue;

		/// <summary>
		/// When a model is created, this is the direction it starts out as
		/// </summary>
		private DoubleVector _defaultDirectionFacing = new DoubleVector(1, 0, 0, 0, 0, 1);

		#endregion

		#region Constructor

		public ShipPartTesterWindow()
		{
			InitializeComponent();

			_isInitialized = true;
		}

		#endregion

		#region Event Listeners

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			#region Trackball

			//	Trackball
			_trackball = new TrackBallRoam(_camera);
			_trackball.KeyPanScale = 1d;
			_trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
			_trackball.AllowZoomOnMouseWheel = true;

			#region copied from MouseComplete_NoLeft - middle button changed

			TrackBallMapping complexMapping = null;

			//	Middle Button
			complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
			complexMapping.Add(MouseButton.Middle);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateAroundLookDirection, MouseButton.Middle, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

			complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
			complexMapping.Add(MouseButton.Middle);
			complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Zoom, MouseButton.Middle, new Key[] { Key.LeftShift, Key.RightShift }));

			//retVal.Add(new TrackBallMapping(CameraMovement.Pan_AutoScroll, MouseButton.Middle));
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan, MouseButton.Middle));

			//	Left+Right Buttons (emulate middle)
			complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);

			complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			_trackball.Mappings.Add(complexMapping);

			complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);

			complexMapping = new TrackBallMapping(CameraMovement.Zoom);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
			_trackball.Mappings.Add(complexMapping);

			//complexMapping = new TrackBallMapping(CameraMovement.Pan_AutoScroll);
			complexMapping = new TrackBallMapping(CameraMovement.Pan);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			_trackball.Mappings.Add(complexMapping);

			//	Right Button
			complexMapping = new TrackBallMapping(CameraMovement.RotateInPlace_AutoScroll);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateInPlace, MouseButton.Right, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit_AutoScroll, MouseButton.Right, new Key[] { Key.LeftAlt, Key.RightAlt }));
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));

			#endregion

			//_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.Keyboard_ASDW_In));		// let the ship get asdw instead of the camera
			//_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
			_trackball.ShouldHitTestOnOrbit = true;

			#endregion
		}
		private void Window_Closed(object sender, EventArgs e)
		{
			try
			{
				//	Temp Bodies
				foreach (TempBody body in _tempBodies)
				{
					body.PhysicsBody.Dispose();
				}
				_tempBodies.Clear();

				//	Current Body
				if (_currentBody != null)
				{
					_currentBody.Dispose();
				}
				_currentBody = null;

				//TODO: Ship

				//	Map
				if (_map != null)
				{
					_map.Dispose();		//	this will dispose the physics bodies
					_map = null;
				}

				//	World
				if (_world != null)
				{
					_world.Pause();
					_world.Dispose();
					_world = null;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void World_Updating(object sender, WorldUpdatingArgs e)
		{
			try
			{
				#region Remove temp bodies

				int index = 0;
				while (index < _tempBodies.Count)
				{
					if (_tempBodies[index].ShouldDie())
					{
						foreach (Visual3D visual in _tempBodies[index].PhysicsBody.Visuals)
						{
							_viewport.Children.Remove(visual);
						}

						_tempBodies[index].PhysicsBody.Dispose();

						_tempBodies.RemoveAt(index);
					}
					else
					{
						index++;
					}
				}

				#endregion

				if (radFallingSand.IsChecked.Value || (radFallingSandPlates.IsChecked.Value && (DateTime.Now - _lastSandAdd).TotalMilliseconds > 5000d))
				{
					#region Falling Sand

					bool isPlate = radFallingSandPlates.IsChecked.Value;
					Color color = UtilityWPF.ColorFromHex("FFF5EE95");
					Vector3D size = new Vector3D(.05, .05, .05);
					double maxDist = 1.1 * size.LengthSquared;
					double mass = .01;
					double radius = 1d;
					double startHeight = 1d;
					TimeSpan lifespan = TimeSpan.FromSeconds(15d);
					Vector3D velocity = new Vector3D(0, 0, -.25);
					int numCubes = isPlate ? 150 : 1;

					//	Calculate positions (they must all be calculated up front, or some sand will be built later)
					List<Vector3D> positions = new List<Vector3D>();
					for (int cntr = 0; cntr < numCubes; cntr++)
					{
						Vector3D startPos;
						while (true)
						{
							startPos = Math3D.GetRandomVectorSpherical2D(radius);

							if (!positions.Any(o => (o - startPos).LengthSquared < maxDist))
							{
								break;
							}
						}

						positions.Add(startPos);
					}

					//	Place the sand
					for (int cntr = 0; cntr < numCubes; cntr++)
					{
						Vector3D startPos = new Vector3D(positions[cntr].X, positions[cntr].Y, startHeight);
						Quaternion orientation = isPlate ? Quaternion.Identity : Math3D.GetRandomRotation();

						Body body = GetNewBody(CollisionShapeType.Box, color, size, mass, startPos.ToPoint(), orientation, _material_Sand);
						body.Velocity = velocity;
						body.LinearDamping = 0d;
						body.AngularDamping = new Vector3D(0d, 0d, 0d);

						_tempBodies.Add(new TempBody(body, lifespan));
					}

					//	Remember when this was done
					_lastSandAdd = DateTime.Now;

					#endregion
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Collision_Ship(object sender, MaterialCollisionArgs e)
		{
		}
		private void Collision_Sand(object sender, MaterialCollisionArgs e)
		{
			//	When the sand hits the ship, it is staying alive too long and messing up other sand
			Body body = e.GetBody(_material_Sand);

			TempBody tempBody = _tempBodies.FirstOrDefault(o => o.PhysicsBody == body);
			if (tempBody != null)
			{
				_tempBodies.Remove(tempBody);
				_tempBodies.Add(new TempBody(tempBody.PhysicsBody, TimeSpan.FromSeconds(1)));
			}
		}

		private void btnAmmoBox_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
				ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;

				double mass1 = ammoBox.TotalMass;

				double remainder = ammoBox.AddQuantity(.05d, false);
				double mass2 = ammoBox.TotalMass;

				remainder = ammoBox.AddQuantity(500d, false);
				double mass3 = ammoBox.TotalMass;

				double output = ammoBox.RemoveQuantity(.1d, false);
				double mass4 = ammoBox.TotalMass;

				output = ammoBox.RemoveQuantity(ammoBox.RemovalMultiple * 2d, false);
				double mass5 = ammoBox.TotalMass;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSingleFuel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				double mass1 = fuelTank.TotalMass;

				double remainder = fuelTank.AddQuantity(.05d, false);
				double mass2 = fuelTank.TotalMass;

				remainder = fuelTank.AddQuantity(500d, false);
				double mass3 = fuelTank.TotalMass;

				double output = fuelTank.RemoveQuantity(.1d, false);
				double mass4 = fuelTank.TotalMass;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSingleEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				double mass1 = energyTank.TotalMass;

				double remainder = energyTank.AddQuantity(.05d, false);
				double mass2 = energyTank.TotalMass;

				remainder = energyTank.AddQuantity(500d, false);
				double mass3 = energyTank.TotalMass;

				double output = energyTank.RemoveQuantity(.1d, false);
				double mass4 = energyTank.TotalMass;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnMultiEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				List<EnergyTank> tanks = new List<EnergyTank>();
				ContainerGroup group = new ContainerGroup();
				group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;

				for (int cntr = 0; cntr < 3; cntr++)
				{
					PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
					double xy = _rand.NextDouble() * 3d;
					double z = _rand.NextDouble() * 3d;
					dna.Scale = new Vector3D(xy, xy, z);

					EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

					tanks.Add(energyTank);
					group.AddContainer(energyTank);
				}

				#region Test1

				double max = group.QuantityMax;

				double remainder1 = group.AddQuantity(max * .5d, false);
				double remainder2 = group.RemoveQuantity(max * .25d, false);
				double remainder3 = group.RemoveQuantity(max * .33d, true);		//	should fail
				double remainder4 = group.AddQuantity(max, false);		//	partial add

				group.QuantityCurrent *= .5d;

				group.RemoveContainer(tanks[0], false);
				group.AddContainer(tanks[0]);

				group.RemoveContainer(tanks[0], true);
				group.AddContainer(tanks[0]);

				#endregion

				//TODO: Finish these
				//TODO: Test setting max (with just regular containers)

				#region Test2

				group.QuantityCurrent = 0d;

				group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;
				//group.Ownership = ContainerGroup.ContainerOwnershipType.QuantitiesCanChange;

				tanks[0].QuantityCurrent *= .5d;		//	this will make sole owner fail, but the second enum will handle it

				group.QuantityCurrent = max * .75d;

				group.RemoveQuantity(max * .33d, false);
				group.AddQuantity(max * .5d, false);

				group.RemoveContainer(tanks[0], true);
				group.AddContainer(tanks[0]);

				#endregion

				#region Test3

				group.OnlyRemoveMultiples = true;
				group.RemovalMultiple = max * .25d;

				group.QuantityCurrent = group.QuantityMax;

				double rem1 = group.RemoveQuantity(max * .1d, false);
				double rem2 = group.RemoveQuantity(max * .1d, true);

				group.QuantityCurrent = group.QuantityMax;

				double rem3 = group.RemoveQuantity(max * .3d, false);
				double rem4 = group.RemoveQuantity(max * .3d, true);

				#endregion
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnEnergyToAmmo_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(ConverterEnergyToAmmo.PARTTYPE);
				ConverterEnergyToAmmo converter = new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dna, energyTank, ammoBox);

				energyTank.QuantityCurrent = energyTank.QuantityMax;

				double mass = converter.DryMass;
				mass = converter.TotalMass;

				converter.Transfer(1d, .5d);
				converter.Transfer(1d, 1d);
				converter.Transfer(1d, .1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnEnergyToFuel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(ConverterEnergyToFuel.PARTTYPE);
				ConverterEnergyToFuel converter = new ConverterEnergyToFuel(_editorOptions, _itemOptions, dna, energyTank, fuelTank);

				energyTank.QuantityCurrent = energyTank.QuantityMax;

				double mass = converter.DryMass;
				mass = converter.TotalMass;

				converter.Transfer(1d, .5d);
				converter.Transfer(1d, 1d);
				converter.Transfer(1d, .1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnFuelToEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(ConverterFuelToEnergy.PARTTYPE);
				ConverterFuelToEnergy converter = new ConverterFuelToEnergy(_editorOptions, _itemOptions, dna, fuelTank, energyTank);

				fuelTank.QuantityCurrent = fuelTank.QuantityMax;

				double mass = converter.DryMass;
				mass = converter.TotalMass;

				converter.Transfer(1d, .5d);
				converter.Transfer(1d, 1d);
				converter.Transfer(1d, .1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSolarPanel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				RadiationField radiation = new RadiationField();
				radiation.AmbientRadiation = 1d;

				ConverterRadiationToEnergyDNA dna2 = new ConverterRadiationToEnergyDNA()
				{
					PartType = ConverterRadiationToEnergy.PARTTYPE,
					Shape = GetRandomEnum<SolarPanelShape>(),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				ConverterRadiationToEnergy solar = new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dna2, energyTank, radiation);

				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				ThrusterDNA dna2 = new ThrusterDNA()
				{
					PartType = Thruster.PARTTYPE,
					ThrusterType = GetRandomEnum<ThrusterType>(),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				Thruster thruster = new Thruster(_editorOptions, _itemOptions, dna2, fuelTank);

				fuelTank.QuantityCurrent = fuelTank.QuantityMax;

				double percent;
				Vector3D? thrust;

				do
				{
					percent = 1d;
					thrust = thruster.Fire(ref percent, 0, 1d);
				} while (fuelTank.QuantityCurrent > 0d);

				percent = 1d;
				thrust = thruster.Fire(ref percent, 0, 1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnStandaloneAmmoEmpty_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
				ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;

				BuildStandalonePart(ammoBox);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneAmmoFull_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
				ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;
				ammoBox.QuantityCurrent = ammoBox.QuantityMax;

				BuildStandalonePart(ammoBox);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneFuelEmpty_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				BuildStandalonePart(fuelTank);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneFuelFull_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);
				fuelTank.QuantityCurrent = fuelTank.QuantityMax;

				BuildStandalonePart(fuelTank);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				BuildStandalonePart(energyTank);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneEnergyToAmmo_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(ConverterEnergyToAmmo.PARTTYPE);
				ConverterEnergyToAmmo converter = new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dna, null, null);

				BuildStandalonePart(converter);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneEnergyToFuel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(ConverterEnergyToFuel.PARTTYPE);
				ConverterEnergyToFuel converter = new ConverterEnergyToFuel(_editorOptions, _itemOptions, dna, null, null);

				BuildStandalonePart(converter);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneFuelToEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(ConverterFuelToEnergy.PARTTYPE);
				ConverterFuelToEnergy converter = new ConverterFuelToEnergy(_editorOptions, _itemOptions, dna, null, null);

				BuildStandalonePart(converter);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneSolarPanel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				RadiationField radiation = new RadiationField();
				radiation.AmbientRadiation = 1d;

				ConverterRadiationToEnergyDNA dna = new ConverterRadiationToEnergyDNA()
				{
					PartType = ConverterRadiationToEnergy.PARTTYPE,
					Shape = GetRandomEnum<SolarPanelShape>(),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				ConverterRadiationToEnergy solar = new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dna, null, radiation);

				BuildStandalonePart(solar);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneThruster_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ThrusterDNA dna = new ThrusterDNA()
				{
					PartType = Thruster.PARTTYPE,
					ThrusterType = GetRandomEnum<ThrusterType>(),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				Thruster thruster = new Thruster(_editorOptions, _itemOptions, dna, null);

				BuildStandalonePart(thruster);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnShipBasic_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();

				if (_ship != null)
				{
					_map.RemoveItem(_ship);
					_ship = null;
				}

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-.75, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });
				parts.Add(new PartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(.75, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

				ShipDNA shipDNA = new ShipDNA();
				shipDNA.ShipName = "basic";
				shipDNA.LayerNames = new string[] { "layer1" }.ToList();
				shipDNA.PartsByLayer = new SortedList<int, List<PartDNA>>();
				shipDNA.PartsByLayer.Add(0, parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);

				_map.AddItem(_ship);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnBoxBreakdown_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearCurrent();
				Random rand = StaticRandom.GetRandomForThread();

				#region Unit Tests

				//var cube1 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .3);
				//var cube2 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .065);
				//var cube3 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .5);

				//var rect1 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .3);
				//var rect2 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .065);
				//var rect3 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .5);

				//for (int cntr = 0; cntr < 1000; cntr++)
				//{
				//    Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
				//    double cellSize = .1 + (rand.NextDouble() * 1.2);
				//    var rect = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);
				//}

				#endregion

				//	Visual
				Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
				double cellSize = .1 + (rand.NextDouble() * 1.2);
				var rect = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);

				double radMult = (rect.CellSize * .75d) / rect.Masses.Max();
				DoubleVector dirFacing = new DoubleVector(1, 0, 0, 0, 1, 0);

				for (int cntr = 0; cntr < rect.Centers.Length; cntr++)
				{
					double radius = rect.Masses[cntr] * radMult;

					CollisionHull dummy1; Transform3DGroup dummy2; Quaternion dummy3; DiffuseMaterial dummy4;
					ModelVisual3D visual = GetWPFModel(out dummy1, out dummy2, out dummy3, out dummy4, CollisionShapeType.Box, _colors.MassBall, _colors.MassBallReflect, _colors.MassBallReflectIntensity, new Vector3D(radius, radius, radius), rect.Centers[cntr], dirFacing, false);

					_viewport.Children.Add(visual);
					_currentVisuals.Add(visual);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCylinderBreakdown_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearCurrent();
				Random rand = StaticRandom.GetRandomForThread();

				//	Visual
				Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
				double cellSize = .1 + (rand.NextDouble() * 1.2);
				var cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

				double radMult = (cylinder.CellSize * .75d) / cylinder.Masses.Max();
				DoubleVector dirFacing = new DoubleVector(1, 0, 0, 0, 1, 0);

				for (int cntr = 0; cntr < cylinder.Centers.Length; cntr++)
				{
					double radius = cylinder.Masses[cntr] * radMult;

					CollisionHull dummy1; Transform3DGroup dummy2; Quaternion dummy3; DiffuseMaterial dummy4;
					ModelVisual3D visual = GetWPFModel(out dummy1, out dummy2, out dummy3, out dummy4, CollisionShapeType.Box, _colors.MassBall, _colors.MassBallReflect, _colors.MassBallReflectIntensity, new Vector3D(radius, radius, radius), cylinder.Centers[cntr], dirFacing, false);

					_viewport.Children.Add(visual);
					_currentVisuals.Add(visual);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Good links:
		/// http://newtondynamics.com/wiki/index.php5?title=NewtonBodySetMassMatrix
		/// 
		/// http://en.wikipedia.org/wiki/List_of_moments_of_inertia
		/// 
		/// http://en.wikipedia.org/wiki/Moment_of_inertia
		/// 
		/// 
		/// Say there are 3 solid spheres, each with a different mass and radius.  Each individual sphere's moment of inertia is 2/5*m*r^2 for each x,y,z
		/// axis, because they are perfectly symetrical.  So now that each individual sphere inertia is known, the 3 as a rigid body can use the parallel axis
		/// theorem.
		/// 
		/// ------------
		/// 
		/// However, say there are 3 solid cylinders, each with a different mass height radius, and each with a random orientation.  Each individual cylinder's
		/// moment of inertia in its local coords is (mr^2)/2 along the axis, and (3mr^2 + 3mh^2)/12 along two poles perp to the axis.
		/// 
		/// BUT the combined body can't use those simplified equations because each cylinder as an arbitrary orienation relative to the whole body's axis, so
		/// those simplifications for each individual body along the whole's axiis can't be used.  The only way is to evenly divide the parts into many uniform
		/// pieces and add up all the mr^2 of each piece (r being the distance from the axis)
		/// 
		/// In other words:
		///		Ix = sum(m* (dist from x)^2) 
		///		Iy = sum(m* (dist from y)^2) 
		///		Iz = sum(m* (dist from z)^2) 
		/// 
		/// Then use parallel axis theorem for each part because each part's center of mass is not along the whole's axiis (they may be in rare cases, but not likely)
		/// 
		/// -------------
		/// 
		/// Newt seems to want the mass separate from the vector, so I guess the vector is just all the r^2's
		/// 
		/// -------------
		/// 
		/// Rather than the ship knowing how to divide up each part, the part should have abstract methods:
		///		Point3D GetCenterMass()
		///		Vector3D GetInertia(Tuple[Vector3D,Vector3D,Vector3D] rays)		//	each of the 3 rays is through the object's center of mass.  The rays are assumed to all be perp to each other
		///		
		/// This way, if the part knows the object is a sphere, it can just return (2mr^2)/5, regardless of the vector passed in.
		/// Otherwise, it would need to break down the object however it decides to
		/// 
		/// As a side note, if those methods could be made static, and pass in a dna, then you could wrap them in a task, and spin
		/// up 3 tasks for each part (one for each axis).  Then await all the tasks. -- actually, keep the method an instance method,
		/// but make a static utility class for various solids.  Then the part can cache previous results for those rays.
		/// 
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnMassMatrix_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				Body body = GetNewBody(CollisionShapeType.Box, Colors.Orange, new Vector3D(1, 1, 1), 1, new Point3D(0, 0, 0), Quaternion.Identity, _material_Ship);
				body.LinearDamping = .01f;
				body.AngularDamping = new Vector3D(.01f, .01f, .01f);

				var prevMatrix = body.MassMatrix;


				double x = 1;
				double y = 1;
				double z = 1;


				body.MassMatrix = new MassMatrix(10, new Vector3D(x, y, z));


				//	Remember them
				_currentBody = body;
				_currentVisuals.AddRange(body.Visuals);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void radFallingSand_Checked(object sender, RoutedEventArgs e)
		{
			if (!_isInitialized)
			{
				return;
			}

			try
			{
				EnsureWorldStarted();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private void ClearCurrent()
		{
			foreach (Visual3D visual in _currentVisuals)
			{
				_viewport.Children.Remove(visual);
			}
			_currentVisuals.Clear();

			if (_currentBody != null)
			{
				_currentBody.Dispose();
				_currentBody = null;
			}

			if (_currentShip != null)
			{
				throw new ApplicationException("finish this");
			}
		}

		private void EnsureWorldStarted()
		{
			if (_world != null)
			{
				return;
			}

			#region Init World

			//	Set the size of the world to something a bit random (gets boring when it's always the same size)
			double halfSize = 50d;
			_boundryMin = new Point3D(-halfSize, -halfSize, -halfSize);
			_boundryMax = new Point3D(halfSize, halfSize, halfSize);

			_world = new World();
			_world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

			List<Point3D[]> innerLines, outerLines;
			_world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

			//	Draw the lines
			_boundryLines = new ScreenSpaceLines3D(true);
			_boundryLines.Thickness = 1d;
			_boundryLines.Color = _colors.BoundryLines;
			_viewport.Children.Add(_boundryLines);

			foreach (Point3D[] line in innerLines)
			{
				_boundryLines.AddLine(line[0], line[1]);
			}

			#endregion
			#region Materials

			_materialManager = new MaterialManager(_world);

			//	Asteroid
			Game.Newt.NewtonDynamics.Material material = new Game.Newt.NewtonDynamics.Material();
			material.Elasticity = .25d;
			material.StaticFriction = .9d;
			material.KineticFriction = .75d;
			_material_Asteroid = _materialManager.AddMaterial(material);

			//	Ship
			material = new Game.Newt.NewtonDynamics.Material();
			material.Elasticity = .75d;
			material.StaticFriction = .5d;
			material.KineticFriction = .2d;
			_material_Ship = _materialManager.AddMaterial(material);

			//	Sand
			material = new Game.Newt.NewtonDynamics.Material();
			material.Elasticity = .5d;
			material.StaticFriction = .5d;
			material.KineticFriction = .33d;
			_material_Sand = _materialManager.AddMaterial(material);

			_materialManager.RegisterCollisionEvent(_material_Ship, _material_Asteroid, Collision_Ship);
			_materialManager.RegisterCollisionEvent(_material_Ship, _material_Sand, Collision_Sand);

			#endregion

			_map = new Map(_viewport, null, _world);
			//_map.SnapshotFequency_Milliseconds = 125;
			//_map.SnapshotMaxItemsPerNode = 10;
			_map.ShouldBuildSnapshots = false;
			//_map.ShouldShowSnapshotLines = true;
			//_map.ShouldSnapshotCentersDrift = false;

			_radiation = new RadiationField();
			_radiation.AmbientRadiation = 1d;

			_world.UnPause();
		}

		private void BuildStandalonePart(PartBase part)
		{
			EnsureWorldStarted();
			ClearCurrent();

			//	WPF
			ModelVisual3D model = new ModelVisual3D();
			model.Content = part.Model;

			_viewport.Children.Add(model);
			_currentVisuals.Add(model);

			//	Physics
			CollisionHull hull = part.CreateCollisionHull(_world);
			Body body = new Body(hull, Matrix3D.Identity, part.TotalMass, new Visual3D[] { model });
			body.MaterialGroupID = _material_Ship;
			body.LinearDamping = .01f;
			body.AngularDamping = new Vector3D(.01f, .01f, .01f);

			_currentBody = body;
		}

		private Body GetNewBody(CollisionShapeType shape, Color color, Vector3D size, double mass, Point3D position, Quaternion orientation, int materialID)
		{
			DoubleVector dirFacing = _defaultDirectionFacing.GetRotatedVector(orientation.Axis, orientation.Angle);

			//	Get the wpf model
			CollisionHull hull;
			Transform3DGroup transform;
			Quaternion rotation;
			DiffuseMaterial bodyMaterial;
			ModelVisual3D model = GetWPFModel(out hull, out transform, out rotation, out bodyMaterial, shape, color, Colors.White, 100d, size, position, dirFacing, true);

			//	Add to the viewport
			_viewport.Children.Add(model);

			//	Make a physics body that represents this shape
			Body retVal = new Body(hull, transform.Value, 1d, new Visual3D[] { model });
			retVal.AutoSleep = false;		//	the falling sand was falling asleep, even though the velocity was non zero (the sand would suddenly stop)
			retVal.MaterialGroupID = materialID;

			return retVal;
		}
		private ModelVisual3D GetWPFModel(out CollisionHull hull, out Transform3DGroup transform, out Quaternion rotation, out DiffuseMaterial bodyMaterial, CollisionShapeType shape, Color color, Color reflectionColor, double reflectionIntensity, Vector3D size, Point3D position, DoubleVector directionFacing, bool createHull)
		{
			//	Material
			MaterialGroup materials = new MaterialGroup();
			bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
			materials.Children.Add(bodyMaterial);
			materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;

			hull = null;

			switch (shape)
			{
				case CollisionShapeType.Box:
					Vector3D halfSize = size / 2d;
					geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize.X, -halfSize.Y, -halfSize.Z), new Point3D(halfSize.X, halfSize.Y, halfSize.Z));
					if (createHull)
					{
						hull = CollisionHull.CreateBox(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Sphere:
					geometry.Geometry = UtilityWPF.GetSphere(5, size.X, size.Y, size.Z);
					if (createHull)
					{
						hull = CollisionHull.CreateSphere(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Cylinder:
					geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCylinder(_world, 0, size.X, size.Y, null);
					}
					break;

				case CollisionShapeType.Cone:
					geometry.Geometry = UtilityWPF.GetCone_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCone(_world, 0, size.X, size.Y, null);
					}
					break;

				default:
					throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
			}

			//	Transform
			transform = new Transform3DGroup();		//	rotate needs to be added before translate



			//rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);		//	can't use double vector, it over rotates (not anymore, but this is still isn't rotating correctly)

			rotation = Math3D.GetRotation(_defaultDirectionFacing.Standard, directionFacing.Standard);



			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
			transform.Children.Add(new TranslateTransform3D(position.ToVector()));

			//	Model Visual
			ModelVisual3D retVal = new ModelVisual3D();
			retVal.Content = geometry;
			retVal.Transform = transform;

			//	Exit Function
			return retVal;
		}

		private PartDNA GetDefaultDNA(string partType)
		{
			PartDNA retVal = new PartDNA();

			retVal.PartType = partType;

			retVal.Position = new Point3D(0, 0, 0);
			retVal.Orientation = Quaternion.Identity;
			retVal.Scale = new Vector3D(1, 1, 1);

			return retVal;
		}

		private T GetRandomEnum<T>()
		{
			Array allValues = Enum.GetValues(typeof(T));
			if (allValues.Length == 0)
			{
				throw new ArgumentException("This enum has no values");
			}

			return (T)allValues.GetValue(_rand.Next(allValues.Length));
		}

		#endregion
	}
}
