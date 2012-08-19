using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2
{
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

			//	For now, just make a composite collision hull out of all the parts
			CollisionHull[] partHulls = _allParts.Select(o => o.CreateCollisionHull(world)).ToArray();
			CollisionHull hull = CollisionHull.CreateCompoundCollision(world, 0, partHulls);

			this.PhysicsBody = new Body(hull, Matrix3D.Identity, 1d, new Visual3D[] { model });		//	just passing a dummy value for mass, the real mass matrix is calculated later
			this.PhysicsBody.MaterialGroupID = materialID;
			this.PhysicsBody.LinearDamping = .01f;
			this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

			//TODO: Calculate mass
			//TODO: For the parts that can change mass, cache results and reuse them when they are that same mass again
			//this.PhysicsBody.MassMatrix = 


			//TODO: Add some mass for the invisible structural "stuff" holding all the parts together.  May want to come up with a convex hull and assume uniform density


			//NOTE: I can't just assume everything is a point mass.  I need to calculate the inerta matrix of each part, then combine all the matrices together into a final matrix


			//this only thinks of it as point masses
			//Game.Orig.Math3D.RigidBody.ResetInertiaTensorAndCenterOfMass

			//see the bottom post
			//http://bulletphysics.org/Bullet/phpBB3/viewtopic.php?f=4&t=246

			//a simple algorithm (I think it just assumes point masses)
			//http://www.melax.com/volint


			//MassMatrix massMatrix;
			//Point3D centerMass;
			//GetInertiaTensorAndCenterOfMass_Points(out massMatrix, out centerMass, _allParts);




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

		public double DryMass
		{
			get;
			private set;
		}
		public double TotalMass
		{
			get;
			private set;
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

		private static void GetInertiaTensorAndCenterOfMass_Points(out MassMatrix matrix, out Point3D center, IEnumerable<PartBase> parts)
		{
			throw new ApplicationException("finish this");

			//const double NEARZERO = .0001d;

			//if (parts.Count() == 0)
			//{
			//    matrix = new MassMatrix(NEARZERO, new Vector3D(NEARZERO, NEARZERO, NEARZERO));
			//    center = new Point3D(0, 0, 0);
			//    return;
			//}

			////	Figure out the center of mass
			//double totalMass;
			//GetCenterMass(out center, out totalMass, parts);

			////	Get the locations of the parts relative to the center of mass (instead of relative to the center of position)
			//Point3D centerCopy = center;
			//Vector3D[] massLocations = parts.Select(o => o.Position - centerCopy).ToArray();

			////	Figure out the inertia tensor
			//Matrix3D inertiaTensor = new Matrix3D();
			//#region Calculate Tensor

			//int index = -1;
			//foreach(PartBase part in parts)
			//{
			//    index++;
			//    double mass = part.TotalMass;

			//    //	M(Y^2 + Z^2)
			//    inertiaTensor.M11 += mass * ((massLocations[massCntr].Y * massLocations[massCntr].Y) + (massLocations[massCntr].Z * massLocations[massCntr].Z));

			//    //	M(X^2 + Z^2)
			//    inertiaTensor.M22 += mass * ((massLocations[massCntr].X * massLocations[massCntr].X) + (massLocations[massCntr].Z * massLocations[massCntr].Z));

			//    //	M(X^2 + Y^2)
			//    inertiaTensor.M33 += mass * ((massLocations[massCntr].X * massLocations[massCntr].X) + (massLocations[massCntr].Y * massLocations[massCntr].Y));

			//    //	MXY
			//    inertiaTensor.M21 += mass * massLocations[massCntr].X * massLocations[massCntr].Y;

			//    //	MXZ
			//    inertiaTensor.M31 += _masses[massCntr].Mass * massLocations[massCntr].X * massLocations[massCntr].Z;

			//    //	MYZ
			//    inertiaTensor.M32 += _masses[massCntr].Mass * massLocations[massCntr].Y * massLocations[massCntr].Z;
			//}

			////	Finish up the non diagnals (it's actually the negative sum for them, and the transpose elements have
			////	the same value)
			//inertiaTensor.M21 *= -1;
			//inertiaTensor.M12 = inertiaTensor.M21;

			//inertiaTensor.M31 *= -1;
			//inertiaTensor.M13 = inertiaTensor.M31;

			//inertiaTensor.M32 *= -1;
			//inertiaTensor.M23 = inertiaTensor.M32;

			//#endregion





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
	}

	#endregion
}
