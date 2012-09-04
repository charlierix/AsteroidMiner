using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2
{
	//TODO: Make sure the part visuals and hulls and mass breakdowns are aligned (not one along X and one along Z)
	//TODO: Support changes in mass
	//TODO: Support parts getting damaged/destroyed/repaired (probably keep the parts around maybe 75% mass, with a charred visual)
	//TODO: I'm still undecided about whether to make this derive from PartBase or not, I think no, but give it a similar signature

	public class Ship : IMapObject
	{
		#region Declaration Section

		private EditorOptions _options = null;
		private ItemOptions _itemOptions = null;
		private ShipDNA _dna = null;
		private RadiationField _radiation = null;

		//	For now, hardcode each part type in its own list

		private List<AmmoBox> _ammo = new List<AmmoBox>();
		private IContainer _ammoGroup = null;		//	this is used by the converters to fill up the ammo boxes.  The logic to match ammo boxes with guns is more complex than a single group
		private List<FuelTank> _fuel = new List<FuelTank>();
		private IContainer _fuelGroup = null;		//	this will either be null, ContainerGroup, or a single FuelTank
		private List<EnergyTank> _energy = new List<EnergyTank>();
		private IContainer _energyGroup = null;		//	this will either be null, ContainerGroup, or a single EnergyTank

		private List<ConverterEnergyToAmmo> _convertEnergyToAmmo = new List<ConverterEnergyToAmmo>();
		private List<ConverterEnergyToFuel> _convertEnergyToFuel = new List<ConverterEnergyToFuel>();
		private List<ConverterFuelToEnergy> _converterFuelToEnergy = new List<ConverterFuelToEnergy>();
		private List<ConverterRadiationToEnergy> _convertRadiationToEnergy = new List<ConverterRadiationToEnergy>();

		private List<Thruster> _thrust = new List<Thruster>();

		private List<PartBase> _allParts = new List<PartBase>();

		#endregion

		#region Constructor

		public Ship(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int materialID, RadiationField radiation)
		{
			_options = options;
			_itemOptions = itemOptions;
			_dna = dna;

			BuildParts();

			#region WPF

			//TODO: Remember this so that flames and other visuals can be added/removed.  That way there will still only be one model visual
			//TODO: When joints are supported, some of the parts (or part groups) will move relative to the others.  There can still be a single model visual
			Model3DGroup models = new Model3DGroup();

			foreach (PartBase part in _allParts)
			{
				models.Children.Add(part.Model);
			}

			ModelVisual3D model = new ModelVisual3D();
			model.Content = models;

			#endregion

			#region Physics Body

			MassMatrix massMatrix;
			Point3D centerMass;
			GetInertiaTensorAndCenterOfMass_Points(out massMatrix, out centerMass, _allParts.ToArray(), _dna.PartsByLayer.Values.SelectMany(o => o).ToArray());

			//	For now, just make a composite collision hull out of all the parts
			CollisionHull[] partHulls = _allParts.Select(o => o.CreateCollisionHull(world)).ToArray();
			CollisionHull hull = CollisionHull.CreateCompoundCollision(world, 0, partHulls);

			this.PhysicsBody = new Body(hull, Matrix3D.Identity, 1d, new Visual3D[] { model });		//	just passing a dummy value for mass, the real mass matrix is calculated later
			this.PhysicsBody.MaterialGroupID = materialID;
			this.PhysicsBody.LinearDamping = .01f;
			this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);
			this.PhysicsBody.CenterOfMass = centerMass;
			this.PhysicsBody.MassMatrix = massMatrix;

			#endregion

			//	Calculate radius
			Point3D aabbMin, aabbMax;
			this.PhysicsBody.GetAABB(out aabbMin, out aabbMax);
			this.Radius = (aabbMax - aabbMin).Length / 2d;
		}

		#endregion

		#region IMapObject Members

		//TODO: In the future, there will be multiple bodies connected by joints
		public Body PhysicsBody
		{
			get;
			private set;
		}

		public Visual3D[] Visuals3D
		{
			get
			{
				return this.PhysicsBody.Visuals;
			}
		}

		public Point3D PositionWorld
		{
			get
			{
				return this.PhysicsBody.Position;
			}
		}
		public Vector3D VelocityWorld
		{
			get
			{
				return this.PhysicsBody.Velocity;
			}
		}

		public double Radius
		{
			get;
			private set;
		}

		#endregion

		#region Public Properties

		//public double DryMass
		//{
		//    get;
		//    private set;
		//}
		//public double TotalMass
		//{
		//    get;
		//    private set;
		//}

		//	These are exposed for debugging convienience.  Don't change their capacity, you'll mess stuff up
		public IContainer Ammo
		{
			get
			{
				return _ammoGroup;
			}
		}
		public IContainer Energy
		{
			get
			{
				return _energyGroup;
			}
		}
		public IContainer Fuel
		{
			get
			{
				return _fuelGroup;
			}
		}

		//	This is exposed for debugging
		public List<Thruster> Thrusters
		{
			get
			{
				return _thrust;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This is for debugging to manual update the mass matrix
		/// </summary>
		public void RecalculateMass()
		{
			MassMatrix massMatrix;
			Point3D centerMass;
			GetInertiaTensorAndCenterOfMass_Points(out massMatrix, out centerMass, _allParts.ToArray(), _dna.PartsByLayer.Values.SelectMany(o => o).ToArray());

			this.PhysicsBody.CenterOfMass = centerMass;
			this.PhysicsBody.MassMatrix = massMatrix;
		}

		#endregion

		#region Private Methods

		private void BuildParts()
		{
			#region Containers

			//	Containers need to be built up front
			foreach (PartDNA dna in _dna.PartsByLayer.Values.SelectMany(o => o).Where(o => o.PartType == AmmoBox.PARTTYPE || o.PartType == FuelTank.PARTTYPE || o.PartType == EnergyTank.PARTTYPE))
			{
				switch (dna.PartType)
				{
					case AmmoBox.PARTTYPE:
						_ammo.Add(new AmmoBox(_options, _itemOptions, dna));
						break;

					case FuelTank.PARTTYPE:
						_fuel.Add(new FuelTank(_options, _itemOptions, dna));
						break;

					case EnergyTank.PARTTYPE:
						_energy.Add(new EnergyTank(_options, _itemOptions, dna));
						break;

					default:
						throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
				}
			}

			//NOTE: The parts can handle being handed a null container.  It doesn't add much value to have parts that are dead
			//weight, but I don't want to penalize a design for having thrusters, but no fuel tank.  Maybe descendants will develop
			//fuel tanks and be a winning design

			//	Build groups
			_fuelGroup = BuildPartsSprtContainerGroup(_fuel);
			_energyGroup = BuildPartsSprtContainerGroup(_energy);
			_ammoGroup = BuildPartsSprtContainerGroup(_ammo);

			//TODO: Figure out which ammo boxes to put with guns.  These are some of the rules that should be considered, they are potentially competing
			//rules, so each rule should form its own links with a weight for each link.  Then choose the pairings with the highest weight (maybe use a bit of
			//randomness)
			//		- Group guns that are the same size
			//		- Pair up boxes and guns that are close together
			//		- Smaller guns should hook to smaller boxes

			//var gunsBySize = _dna.PartsByLayer.Values.SelectMany(o => o).Where(o => o.PartType == ProjectileGun.PARTTYPE).GroupBy(o => o.Scale.LengthSquared);

			#region OLD

			//if (_fuel.Count == 1)
			//{
			//    _fuelGroup = _fuel[0];
			//}
			//else if (_fuel.Count > 1)
			//{
			//    ContainerGroup group = new ContainerGroup();
			//    group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;		//	this is the most efficient option
			//    foreach (IContainer container in _fuel)
			//    {
			//        group.AddContainer(container);
			//    }

			//    _fuelGroup = group;
			//}

			//if (_energy.Count == 1)
			//{
			//    _energyGroup = _energy[0];
			//}
			//else if (_energy.Count > 1)
			//{
			//    ContainerGroup group = new ContainerGroup();
			//    group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;		//	this is the most efficient option
			//    foreach (IContainer container in _energy)
			//    {
			//        group.AddContainer(container);
			//    }

			//    _energyGroup = group;
			//}

			#endregion

			#endregion

			foreach (PartDNA dna in _dna.PartsByLayer.Values.SelectMany(o => o))
			{
				switch (dna.PartType)
				{
					case AmmoBox.PARTTYPE:
					case FuelTank.PARTTYPE:
					case EnergyTank.PARTTYPE:
						//	These were built previously
						break;

					case ConverterEnergyToAmmo.PARTTYPE:
						_convertEnergyToAmmo.Add(new ConverterEnergyToAmmo(_options, _itemOptions, dna, _energyGroup, _ammoGroup));
						break;

					case ConverterEnergyToFuel.PARTTYPE:
						_convertEnergyToFuel.Add(new ConverterEnergyToFuel(_options, _itemOptions, dna, _energyGroup, _fuelGroup));
						break;

					case ConverterFuelToEnergy.PARTTYPE:
						_converterFuelToEnergy.Add(new ConverterFuelToEnergy(_options, _itemOptions, dna, _fuelGroup, _energyGroup));
						break;

					case ConverterRadiationToEnergy.PARTTYPE:
						_convertRadiationToEnergy.Add(new ConverterRadiationToEnergy(_options, _itemOptions, (ConverterRadiationToEnergyDNA)dna, _energyGroup, _radiation));
						break;

					case Thruster.PARTTYPE:
						_thrust.Add(new Thruster(_options, _itemOptions, (ThrusterDNA)dna, _fuelGroup));
						break;

					default:
						throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
				}
			}

			#region All Parts

			_allParts.AddRange(_ammo);
			_allParts.AddRange(_fuel);
			_allParts.AddRange(_energy);
			_allParts.AddRange(_convertEnergyToAmmo);
			_allParts.AddRange(_convertEnergyToFuel);
			_allParts.AddRange(_converterFuelToEnergy);
			_allParts.AddRange(_convertRadiationToEnergy);
			_allParts.AddRange(_thrust);

			#endregion
		}
		private static IContainer BuildPartsSprtContainerGroup(IEnumerable<IContainer> containers)
		{
			IContainer retVal = null;

			int count = containers.Count();

			if (count == 1)
			{
				retVal = containers.First();
			}
			else if (count > 1)
			{
				ContainerGroup group = new ContainerGroup();
				group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;		//	this is the most efficient option
				foreach (IContainer container in containers)
				{
					group.AddContainer(container);
				}

				retVal = group;
			}

			return retVal;
		}

		//TODO: Account for the invisible structural filler between the parts (probably just do a convex hull and give the filler a uniform density)
		private static void GetInertiaTensorAndCenterOfMass_Points(out MassMatrix matrix, out Point3D center, PartBase[] parts, PartDNA[] dna)
		{
			#region Prep work

			//	Break the mass of the parts into pieces
			double cellSize = dna.Select(o => Math.Max(Math.Max(o.Scale.X, o.Scale.Y), o.Scale.Z)).Max() * .2d;		//	break the largest object up into roughly 5x5x5
			UtilityNewt.IObjectMassBreakdown[] massBreakdowns = parts.Select(o => o.GetMassBreakdown(cellSize)).ToArray();

			double cellSphereMultiplier = (cellSize * .5d) * (cellSize * .5d) * .4d;		//	2/5 * r^2

			double[] partMasses = parts.Select(o => o.TotalMass).ToArray();
			double totalMass = partMasses.Sum();
			double totalMassInverse = 1d / totalMass;

			Vector3D axisX = new Vector3D(1d, 0d, 0d);
			Vector3D axisY = new Vector3D(0d, 1d, 0d);
			Vector3D axisZ = new Vector3D(0d, 0d, 1d);

			#endregion

			#region Ship's center of mass

			//	Calculate the ship's center of mass
			double centerX = 0d;
			double centerY = 0d;
			double centerZ = 0d;
			for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
			{
				//	Shift the part into ship coords
				Point3D centerMass = parts[cntr].Position + massBreakdowns[cntr].CenterMass.ToVector();

				centerX += centerMass.X * partMasses[cntr];
				centerY += centerMass.Y * partMasses[cntr];
				centerZ += centerMass.Z * partMasses[cntr];
			}

			center = new Point3D(centerX * totalMassInverse, centerY * totalMassInverse, centerZ * totalMassInverse);

			#endregion

			#region Local inertias

			//	Get the local moment of inertia of each part for each of the three ship's axiis
			//TODO: If the number of cells is large, this would be a good candidate for running in parallel, but this method keeps cellSize pretty course
			Vector3D[] localInertias = new Vector3D[massBreakdowns.Length];
			for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
			{
				RotateTransform3D localRotation = new RotateTransform3D(new QuaternionRotation3D(parts[cntr].Orientation.ToReverse()));



				//TODO: Verify these results with the equation for the moment of inertia of a cylinder






				//NOTE: Each mass breakdown adds up to a mass of 1, so putting that mass back now (otherwise the ratios of masses between parts would be lost)
				localInertias[cntr] = new Vector3D(
					GetInertia(massBreakdowns[cntr], localRotation.Transform(axisX), cellSphereMultiplier) * partMasses[cntr],
					GetInertia(massBreakdowns[cntr], localRotation.Transform(axisY), cellSphereMultiplier) * partMasses[cntr],
					GetInertia(massBreakdowns[cntr], localRotation.Transform(axisZ), cellSphereMultiplier) * partMasses[cntr]);
			}

			#endregion
			#region Global inertias

			//	Apply the parallel axis theorem to each part
			double shipInertiaX = 0d;
			double shipInertiaY = 0d;
			double shipInertiaZ = 0d;
			for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
			{
				//	Shift the part into ship coords
				Point3D partCenter = parts[cntr].Position + massBreakdowns[cntr].CenterMass.ToVector();

				shipInertiaX += GetInertia(partCenter, localInertias[cntr].X, partMasses[cntr], center, axisX);
				shipInertiaY += GetInertia(partCenter, localInertias[cntr].Y, partMasses[cntr], center, axisY);
				shipInertiaZ += GetInertia(partCenter, localInertias[cntr].Z, partMasses[cntr], center, axisZ);
			}

			#endregion

			//	Newton want the inertia vector to be one, so divide of the mass of all the parts
			matrix = new MassMatrix(totalMass, new Vector3D(shipInertiaX * totalMassInverse, shipInertiaY * totalMassInverse, shipInertiaZ * totalMassInverse));
		}

		/// <summary>
		/// This calculates the moment of inertia of the body around the axis (the axis goes through the center of mass)
		/// </summary>
		/// <remarks>
		/// Inertia of a body is the sum of all the mr^2
		/// 
		/// Each cell of the mass breakdown needs to be thought of as a sphere.  If it were a point mass, then for a body with only one
		/// cell, the mass would be at the center, and it would have an inertia of zero.  So by using the parallel axis theorem on each cell,
		/// the returned inertia is accurate.  The reason they need to thought of as spheres instead of cubes, is because the inertia is the
		/// same through any axis of a sphere, but not for a cube.
		/// 
		/// So sphereMultiplier needs to be 2/5 * cellRadius^2
		/// </remarks>
		private static double GetInertia(UtilityNewt.IObjectMassBreakdown body, Vector3D axis, double sphereMultiplier)
		{
			double retVal = 0d;

			//	Cache this point in case the property call is somewhat expensive
			Point3D center = body.CenterMass;

			foreach (var pointMass in body)
			{
				if (pointMass.Item2 == 0d)
				{
					continue;
				}

				//	Tack on the inertia of the cell sphere (2/5*mr^2)
				retVal += pointMass.Item2 * sphereMultiplier;

				//	Get the distance between this point and the axis
				double distance = Math3D.GetClosestDistanceBetweenPointAndLine(body.CenterMass, axis, pointMass.Item1);

				//	Now tack on the md^2
				retVal += pointMass.Item2 * distance * distance;
			}

			//	Exit Function
			return retVal;
		}
		/// <summary>
		/// This returns the inertia of the part relative to the ship's axis
		/// NOTE: The other overload takes a vector that was transformed into the part's model coords.  The vector passed to this overload is in ship's model coords
		/// </summary>
		private static double GetInertia(Point3D partCenter, double partInertia, double partMass, Point3D shipCenterMass, Vector3D axis)
		{
			//	Start with the inertia of the part around the axis passed in
			double retVal = partInertia;

			//	Get the distance between the part and the axis
			double distance = Math3D.GetClosestDistanceBetweenPointAndLine(shipCenterMass, axis, partCenter);

			//	Now tack on the md^2
			retVal += partMass * distance * distance;

			//	Exit Function
			return retVal;
		}

		private static void GetCenterMass(out Point3D center, out double mass, IEnumerable<PartBase> parts)
		{
			if (parts.Count() == 0)
			{
				center = new Point3D(0, 0, 0);
				mass = 0;
				return;
			}

			double x = 0;
			double y = 0;
			double z = 0;
			mass = 0;

			foreach (PartBase part in parts)
			{
				Point3D partPos = part.Position;
				double partMass = part.TotalMass;

				x += partPos.X * partMass;
				y += partPos.Y * partMass;
				z += partPos.Z * partMass;

				mass += partMass;
			}

			x /= mass;
			y /= mass;
			z /= mass;

			center = new Point3D(x, y, z);
		}

		#endregion
	}

	#region Class: ShipDNA

	public class ShipDNA
	{
		//TODO: Make some ship level properties

		public string ShipName
		{
			get;
			set;
		}

		public List<string> LayerNames
		{
			get;
			set;
		}

		public SortedList<int, List<PartDNA>> PartsByLayer
		{
			get;
			set;
		}

		/// <summary>
		/// This is just a convenience if you don't care about the ship's name and layers (it always creates 1 layer)
		/// </summary>
		public static ShipDNA Create(IEnumerable<PartDNA> parts)
		{
			return Create(Guid.NewGuid().ToString(), parts);
		}
		/// <summary>
		/// This is just a convenience if you don't care about the layers (it always creates 1 layer)
		/// </summary>
		public static ShipDNA Create(string name, IEnumerable<PartDNA> parts)
		{
			ShipDNA retVal = new ShipDNA();
			retVal.ShipName = Guid.NewGuid().ToString();
			retVal.LayerNames = new string[] { "layer1" }.ToList();
			retVal.PartsByLayer = new SortedList<int, List<PartDNA>>();
			retVal.PartsByLayer.Add(0, new List<PartDNA>(parts));

			return retVal;
		}
	}

	#endregion
}
