using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	//TODO:  Draw thrust lines whenever activated (use an orange cone)

	#region Class: ThrusterToolItem

	//TODO: Support custom thrusters
	public class ThrusterToolItem : PartToolItemBase
	{
		#region Constructor

		public ThrusterToolItem(EditorOptions options, ThrusterType thrusterType)
			: base(options)
		{
			this.ThrusterType = thrusterType;
			_visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
			this.TabName = PartToolItemBase.TAB_SHIPPART;
		}

		#endregion

		#region Public Properties

		public override string Name
		{
			get
			{
				return "Thruster (" + this.ThrusterType.ToString().ToLower().Replace('_', ' ') + ")";
			}
		}
		public override string Description
		{
			get
			{
				return "Consumes fuel, and produces force";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_PROPULSION;
			}
		}

		public ThrusterType ThrusterType
		{
			get;
			private set;
		}

		private UIElement _visual2D = null;
		public override UIElement Visual2D
		{
			get
			{
				return _visual2D;
			}
		}

		#endregion

		#region Public Methods

		public override PartDesignBase GetNewDesignPart()
		{
			return new ThrusterDesign(this.Options, this.ThrusterType);
		}

		#endregion
	}

	#endregion
	#region Class: ThrusterDesign

	public class ThrusterDesign : PartDesignBase
	{
		#region Declaration Section

		public const double RADIUSPERCENTOFSCALE = .2d;

		private Point3D[] _pointsForHull = null;

		private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

		#endregion

		#region Constructor

		public ThrusterDesign(EditorOptions options, ThrusterType thrusterType)
			: base(options)
		{
			if (thrusterType == ThrusterType.Custom)
			{
				throw new ArgumentException("This overload doesn't allow a ThrusterType of Custom");
			}

			this.ThrusterType = thrusterType;
		}
		public ThrusterDesign(EditorOptions options, Vector3D[] thrusters)
			: base(options)
		{
			this.ThrusterType = ThrusterType.Custom;
			this.ThrusterDirections = thrusters;
		}

		#endregion

		#region Public Properties

		public ThrusterType ThrusterType
		{
			get;
			private set;
		}
		//	These are unit vectors
		public Vector3D[] ThrusterDirections
		{
			get;
			private set;
		}

		public override PartDesignAllowedScale AllowedScale
		{
			get
			{
				return PartDesignAllowedScale.XYZ;
			}
		}
		public override PartDesignAllowedRotation AllowedRotation
		{
			get
			{
				switch (this.ThrusterType)
				{
					case ThrusterType.One:
					case ThrusterType.Two:
						return PartDesignAllowedRotation.X_Y_Z;

					default:
						return PartDesignAllowedRotation.X_Y_Z;
				}
			}
		}

		private Model3DGroup _geometries = null;
		public override Model3D Model
		{
			get
			{
				if (_geometries == null)
				{
					_geometries = CreateGeometry(false);
				}

				return _geometries;
			}
		}

		#endregion

		#region Public Methods

		public override Model3D GetFinalModel()
		{
			return CreateGeometry(true);
		}

		public override PartDNA GetDNA()
		{
			ThrusterDNA retVal = new ThrusterDNA();

			base.FillDNA(retVal);
			retVal.ThrusterType = this.ThrusterType;
			retVal.ThrusterDirections = this.ThrusterDirections;

			return retVal;
		}
		public override void SetDNA(PartDNA dna)
		{
			if (!(dna is ThrusterDNA))
			{
				throw new ArgumentException("The class passed in must be ConverterRadiationToEnergyDNA");
			}

			ThrusterDNA dnaCast = (ThrusterDNA)dna;

			base.StoreDNA(dna);

			this.ThrusterType = dnaCast.ThrusterType;
			this.ThrusterDirections = dnaCast.ThrusterDirections;
		}

		public override CollisionHull CreateCollisionHull(WorldBase world)
		{
			Transform3DGroup transform = new Transform3DGroup();
			//transform.Children.Add(new ScaleTransform3D(this.Scale));		//	it ignores scale
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		//	the physics hull is along x, but dna is along z
			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
			transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

			Vector3D scale = this.Scale;

			switch (this.ThrusterType)
			{
				case ShipParts.ThrusterType.One:
				case ShipParts.ThrusterType.Two:
					#region Cylinder

					double radius = RADIUSPERCENTOFSCALE * (scale.X + scale.Y) * .5d;
					double height = scale.Z;

					if (height < radius * 2d)
					{
						//	Newton keeps the capsule caps spherical, but the visual scales them.  So when the height is less than the radius, newton
						//	make a sphere.  So just make a cylinder instead
						return CollisionHull.CreateChamferCylinder(world, 0, radius, height, transform.Value);
						//return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
					}
					else
					{
						//NOTE: The visual changes the caps around, but I want the physics to be a capsule
						return CollisionHull.CreateCapsule(world, 0, radius, height, transform.Value);
					}

					#endregion

				default:
					#region Convex Hull

					if (_pointsForHull == null)		//	the thruster calls get final model in its constructor, so this should never be needed
					{
						CreateGeometry(true);
					}

					double maxScale = Math3D.Max(scale.X * RADIUSPERCENTOFSCALE, scale.Y * RADIUSPERCENTOFSCALE, scale.Z);
					Point3D[] points = _pointsForHull.Select(o => new Point3D(o.X * maxScale, o.Y * maxScale, o.Z * maxScale)).ToArray();

					return CollisionHull.CreateConvexHull(world, 0, points, 0.002d, transform.Value);

					#endregion
			}
		}

		public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
		{
			if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
			{
				//	This has already been built for this size
				return _massBreakdown.Item1;
			}

			UtilityNewt.ObjectMassBreakdownSet breakdown = null;

			switch (this.ThrusterType)
			{
				case ShipParts.ThrusterType.One:
				case ShipParts.ThrusterType.Two:
					breakdown = GetMassBreakdownSprtCylinder(this.Scale, cellSize);
					break;

				case ShipParts.ThrusterType.Two_One:
				case ShipParts.ThrusterType.Two_Two:
				case ShipParts.ThrusterType.Two_Two_One:
				case ShipParts.ThrusterType.Two_Two_Two:
				case ShipParts.ThrusterType.Custom:
					breakdown = GetMassBreakdownSprtMulti(this.Scale, cellSize, this.ThrusterDirections);
					break;

				default:
					throw new ApplicationException("Unknown ThrusterType: " + this.ThrusterType.ToString());
			}

			//	Store this
			_massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

			//	Exit Function
			return _massBreakdown.Item1;
		}

		public static Vector3D[] GetThrusterDirections(ThrusterType thrusterType)
		{
			Vector3D[] retVal = null;

			switch (thrusterType)
			{
				case ThrusterType.One:
					#region OneWay

					//	Directions
					retVal = new Vector3D[1];
					retVal[0] = new Vector3D(0, 0, 1);		//	the visual's bottle points down, but the thrust is up

					#endregion
					break;

				case ThrusterType.Two:
					#region TwoWay

					//	Directions
					retVal = new Vector3D[2];
					retVal[0] = new Vector3D(0, 0, 1);
					retVal[1] = new Vector3D(0, 0, -1);

					#endregion
					break;

				case ThrusterType.Two_One:
					#region Two_One

					//	Directions
					retVal = new Vector3D[3];
					retVal[0] = new Vector3D(0, 0, 1);
					retVal[1] = new Vector3D(0, 0, -1);
					retVal[2] = new Vector3D(1, 0, 0);

					#endregion
					break;

				case ThrusterType.Two_Two:
					#region Two_Two

					//	Directions
					retVal = new Vector3D[4];
					retVal[0] = new Vector3D(0, 0, 1);
					retVal[1] = new Vector3D(0, 0, -1);
					retVal[2] = new Vector3D(1, 0, 0);
					retVal[3] = new Vector3D(-1, 0, 0);

					#endregion
					break;

				case ThrusterType.Two_Two_One:
					#region Two_Two_One

					//	Directions
					retVal = new Vector3D[5];
					retVal[0] = new Vector3D(0, 0, 1);
					retVal[1] = new Vector3D(0, 0, -1);
					retVal[2] = new Vector3D(1, 0, 0);
					retVal[3] = new Vector3D(-1, 0, 0);
					retVal[4] = new Vector3D(0, 1, 0);

					#endregion
					break;

				case ThrusterType.Two_Two_Two:
					#region Two_Two_Two

					//	Directions
					retVal = new Vector3D[6];
					retVal[0] = new Vector3D(0, 0, 1);
					retVal[1] = new Vector3D(0, 0, -1);
					retVal[2] = new Vector3D(1, 0, 0);
					retVal[3] = new Vector3D(-1, 0, 0);
					retVal[4] = new Vector3D(0, 1, 0);
					retVal[5] = new Vector3D(0, -1, 0);

					#endregion
					break;

				case ThrusterType.Custom:
					#region Custom

					throw new ApplicationException("finish implementing custom thrusters");

					#endregion
					break;

				default:
					throw new ApplicationException("Unknown ThrusterType: " + thrusterType.ToString());
			}

			//	Exit Function
			return retVal;
		}

		#endregion

		#region Private Methods

		private Model3DGroup CreateGeometry(bool isFinal)
		{
			MaterialGroup frontMaterial = new MaterialGroup();
			DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.Thruster));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.Thruster));
			frontMaterial.Children.Add(diffuse);
			SpecularMaterial specular = this.Options.WorldColors.ThrusterSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			frontMaterial.Children.Add(specular);

			MaterialGroup backMaterial = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.ThrusterBack));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.ThrusterBack));
			backMaterial.Children.Add(diffuse);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				frontMaterial.Children.Add(selectionEmissive);
				base.SelectionEmissives.Add(selectionEmissive);

				selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				backMaterial.Children.Add(selectionEmissive);
				base.SelectionEmissives.Add(selectionEmissive);
			}

			GeometryModel3D geometry;
			List<TubeRingBase> rings;
			Transform3DGroup transform;
			double scale;

			int domeSegments = isFinal ? 2 : 10;
			int cylinderSegments = isFinal ? 6 : 35;

			Model3DGroup retVal = new Model3DGroup();

			switch (this.ThrusterType)
			{
				case ThrusterType.One:
					#region OneWay

					//	Geometry 1
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings1Full(domeSegments);

					scale = 1d / rings.Sum(o => o.DistFromPrevRing);		//	Scale this so the height is 1
					transform = new Transform3DGroup();
					transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);
					retVal.Children.Add(geometry);

					#endregion
					break;

				case ThrusterType.Two:
					#region TwoWay

					//	Geometry 1
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings2Full();

					scale = 1d / rings.Sum(o => o.DistFromPrevRing);		//	Scale this so the height is 1
					transform = new Transform3DGroup();
					transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);
					retVal.Children.Add(geometry);

					#endregion
					break;

				case ThrusterType.Two_One:
					#region Two_One

					//	Geometry 1
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings2Full();

					scale = 1d / rings.Sum(o => o.DistFromPrevRing);		//	Scale this so the height is 1
					transform = new Transform3DGroup();
					transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);
					retVal.Children.Add(geometry);

					//	Geometry 2
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings1Half(false, domeSegments);
					transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
					transform.Children.Add(new TranslateTransform3D(-scale * rings.Sum(o => o.DistFromPrevRing), 0, 0));
					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transform);

					retVal.Children.Add(geometry);

					#endregion
					break;

				case ThrusterType.Two_Two:
					#region Two_Two

					//	Geometry Model1
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings2Full();

					scale = 1d / rings.Sum(o => o.DistFromPrevRing);		//	Scale this so the height is 1
					transform = new Transform3DGroup();
					transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

					retVal.Children.Add(geometry);

					//	Geometry Model2
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

					retVal.Children.Add(geometry);

					#endregion
					break;

				case ThrusterType.Two_Two_One:
					#region Two_Two_One

					//	Geometry Model1
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings2Full();

					scale = 1d / rings.Sum(o => o.DistFromPrevRing);		//	Scale this so the height is 1
					transform = new Transform3DGroup();
					transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

					retVal.Children.Add(geometry);

					//	Geometry Model2
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

					retVal.Children.Add(geometry);

					//	Geometry Model3
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings1Half(false, domeSegments);
					transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
					transform.Children.Add(new TranslateTransform3D(0, -scale * rings.Sum(o => o.DistFromPrevRing), 0));
					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transform);

					retVal.Children.Add(geometry);

					#endregion
					break;

				case ThrusterType.Two_Two_Two:
					#region Two_Two_Two

					//	Geometry Model1
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					rings = GetThrusterRings2Full();

					scale = 1d / rings.Sum(o => o.DistFromPrevRing);		//	Scale this so the height is 1
					transform = new Transform3DGroup();
					transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

					retVal.Children.Add(geometry);

					//	Geometry Model2
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

					retVal.Children.Add(geometry);

					//	Geometry Model3
					geometry = new GeometryModel3D();
					geometry.Material = frontMaterial;
					geometry.BackMaterial = backMaterial;

					transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

					retVal.Children.Add(geometry);

					#endregion
					break;

				case ThrusterType.Custom:
					#region Custom

					throw new ApplicationException("finish implementing custom thrusters");

					#endregion
					break;

				default:
					throw new ApplicationException("Unknown ThrusterType: " + this.ThrusterType.ToString());
			}

			//	Directions
			this.ThrusterDirections = GetThrusterDirections(this.ThrusterType);

			//	Remember the points
			if (isFinal)
			{
				List<Point3D> pointsForHull = new List<Point3D>();
				foreach (GeometryModel3D child in retVal.Children)
				{
					pointsForHull.AddRange(UtilityWPF.GetPointsFromMesh((MeshGeometry3D)child.Geometry));
				}
				_pointsForHull = pointsForHull.ToArray();
			}

			//	Transform
			Transform3DGroup transformGlobal = new Transform3DGroup();
			if (isFinal)
			{
				transformGlobal.Children.Add(_scaleTransform.Clone());
				transformGlobal.Children.Add(new RotateTransform3D(_rotateTransform.Clone()));
				transformGlobal.Children.Add(_translateTransform.Clone());
			}
			else
			{
				transformGlobal.Children.Add(_scaleTransform);
				transformGlobal.Children.Add(new RotateTransform3D(_rotateTransform));
				transformGlobal.Children.Add(_translateTransform);
			}
			retVal.Transform = transformGlobal;

			//	Exit Function
			return retVal;
		}

		private static List<TubeRingBase> GetThrusterRings1Full(int domeSegments)
		{
			List<TubeRingBase> retVal = new List<TubeRingBase>();

			retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
			retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
			retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
			retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
			retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.05, 1.05, false));
			retVal.Add(new TubeRingDome(.66, false, domeSegments));

			return retVal;
		}
		private static List<TubeRingBase> GetThrusterRings1Half(bool includeDome, int domeSegments)
		{
			List<TubeRingBase> retVal = new List<TubeRingBase>();

			retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
			retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
			retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
			retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
			if (includeDome)
			{
				retVal.Add(new TubeRingDome(.55, false, domeSegments));
			}

			return retVal;
		}
		private static List<TubeRingBase> GetThrusterRings2Full()
		{
			List<TubeRingBase> retVal = new List<TubeRingBase>();

			retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
			retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
			retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
			retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
			retVal.Add(new TubeRingRegularPolygon(1.5, false, 1, 1, false));
			retVal.Add(new TubeRingRegularPolygon(.5, false, .9, .9, false));
			retVal.Add(new TubeRingRegularPolygon(1.2, false, .25, .25, false));

			return retVal;
		}

		private static UtilityNewt.ObjectMassBreakdownSet GetMassBreakdownSprtCylinder(Vector3D scale, double cellSize)
		{
			//	Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
			//	Reducing Z a bit, because the thruster has tapered ends
			Vector3D size = new Vector3D(scale.Z * .9d, scale.X * RADIUSPERCENTOFSCALE * 2d, scale.Y * RADIUSPERCENTOFSCALE * 2d);

			//	Cylinder
			UtilityNewt.ObjectMassBreakdown cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

			Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));		//	the physics hull is along x, but dna is along z

			//	Rotated
			return new UtilityNewt.ObjectMassBreakdownSet(new UtilityNewt.ObjectMassBreakdown[] { cylinder }, new Transform3D[] { transform });
		}
		private static UtilityNewt.ObjectMassBreakdownSet GetMassBreakdownSprtMulti(Vector3D scale, double cellSize, Vector3D[] directions)
		{
			//	Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
			Vector3D size = new Vector3D(scale.Z, scale.X * RADIUSPERCENTOFSCALE * 2d, scale.Y * RADIUSPERCENTOFSCALE * 2d);

			//	Center ball
			double centerSize = Math.Max(size.X * RADIUSPERCENTOFSCALE * 2d, Math.Max(size.Y, size.Z));
			centerSize *= .5d;
			double centerVolume = 4d / 3d * Math.PI * Math.Pow(centerSize * .5d, 3d);
			var centerBall = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(centerSize, centerSize, centerSize), centerSize * 1.1d);

			//	Cylinder
			Vector3D cylinderSize = new Vector3D(size.X * .35d, size.Y, size.Z);		//	the cylinder's length is the radius (or half of x) minus the center ball, and a bit less, since it's not a complete cylinder
			double cylinderVolume = Math.Pow((cylinderSize.Y + cylinderSize.Z) / 4d, 2d) * Math.PI * cylinderSize.X;		//	dividing by 4, because div 2 is the average, then another 2 is to convert diameter to radius
			var cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, cylinderSize, cellSize);

			//	Build up the final
			Vector3D cylinderOffset = new Vector3D((centerSize * .5d) + (cylinderSize.X * .5d), 0d, 0d);
			Vector3D offset = new Vector3D(1, 0, 0);

			List<Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>> items = new List<Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>>();

			items.Add(new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(centerBall, new Point3D(0, 0, 0), Quaternion.Identity, centerVolume));

			foreach (Vector3D dir in directions)
			{
				Quaternion quat = Math3D.GetRotation(offset, dir);
				Point3D translate = new RotateTransform3D(new QuaternionRotation3D(quat)).Transform(cylinderOffset).ToPoint();

				items.Add(new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(cylinder, translate, quat, cylinderVolume));
			}

			//	Exit Function
			return UtilityNewt.Combine(items.ToArray());
		}

		#endregion
	}

	#endregion
	#region Class: Thruster

	public class Thruster : PartBase
	{
		#region Declaration Section

		public const string PARTTYPE = "Thruster";

		private ItemOptions _itemOptions = null;

		private ThrusterDNA _dnaCast = null;
		private ThrusterDesign _design = null;

		private IContainer _fuelTanks = null;

		private double _mass = 0d;
		private double _forceStrength = 0d;

		#endregion

		#region Constructor

		public Thruster(EditorOptions options, ItemOptions itemOptions, ThrusterDNA dna, IContainer fuelTanks)
			: base(options, dna)
		{
			_itemOptions = itemOptions;
			_dnaCast = dna;
			_fuelTanks = fuelTanks;

			_design = new ThrusterDesign(options, dna.ThrusterType);
			_design.SetDNA(dna);

			//	Need to create the model so that ThrusterDirections is populated
			_model = _design.GetFinalModel();

			double cylinderVolume = GetVolume(dna);
			_mass = GetMass(itemOptions, _design.ThrusterDirections.Length, cylinderVolume);
			_forceStrength = cylinderVolume * itemOptions.ThrusterStrengthRatio;

			RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(dna.Orientation));
			this.ThrusterDirectionsShip = _design.ThrusterDirections.Select(o => transform.Transform(o)).ToArray();		//NOTE: It is expected that _design.ThrusterDirections are unit vectors
		}

		#endregion

		#region Public Properties

		public override double DryMass
		{
			get
			{
				return _mass;
			}
		}
		public override double TotalMass
		{
			get
			{
				return _mass;
			}
		}

		public override Point3D Position
		{
			get
			{
				return this.DNA.Position;
			}
		}
		public override Quaternion Orientation
		{
			get
			{
				return this.DNA.Orientation;
			}
		}

		private Model3D _model = null;
		public override Model3D Model
		{
			get
			{
				return _model;
			}
		}

		public ThrusterType ThrusterType
		{
			get
			{
				return _design.ThrusterType;
			}
		}
		//	These are unit vectors
		public Vector3D[] ThrusterDirectionsModel
		{
			get
			{
				return _design.ThrusterDirections;
			}
		}
		public Vector3D[] ThrusterDirectionsShip
		{
			get;
			private set;
		}

		/// <summary>
		/// When drawing a thrust line visual, this is where to start it
		/// </summary>
		public double ThrustVisualStartRadius
		{
			get
			{
				double maxSize = Math3D.Max(_design.Scale.X, _design.Scale.Y, _design.Scale.Z);		//	they should all be the same

				return maxSize * .5d;
			}
		}

		public double ForceAtMax
		{
			get
			{
				return _forceStrength;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This will fire the thruster
		/// NOTE: It is assumed that the thruster is firing steady throughout elapsedTime
		/// NOTE: The percent max is byref so that the caller knows what percent was actually used (good for visuals/sounds)
		/// NOTE: The returned vector is in ship coords
		/// </summary>
		public Vector3D? Fire(ref double percentMax, int index, double elapsedTime)
		{
			double actualForce;

			if (_fuelTanks != null && _fuelTanks.QuantityCurrent > 0d)
			{
				//	Figure out how much force will be generated
				actualForce = _forceStrength * percentMax;

				//	See how much fuel that will take
				double fuelToUse = actualForce * elapsedTime * _itemOptions.FuelToThrustRatio;

				//	Try to burn that much fuel
				double fuelUnused = _fuelTanks.RemoveQuantity(fuelToUse, false);
				if (fuelUnused > 0d)
				{
					//	Not enough fuel, reduce the amount of force
					actualForce -= fuelUnused / (elapsedTime * _itemOptions.FuelToThrustRatio);
					percentMax *= (fuelToUse - fuelUnused) / fuelToUse;		//	multiply by the ratio that was actually used
				}
			}
			else
			{
				//	No fuel
				actualForce = 0d;
				percentMax = 0d;
			}

			//	Exit Function
			if (actualForce > 0d)
			{
				return this.ThrusterDirectionsShip[index] * actualForce;
			}
			else
			{
				return null;
			}
		}

		public override CollisionHull CreateCollisionHull(WorldBase world)
		{
			return _design.CreateCollisionHull(world);
		}

		public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
		{
			return _design.GetMassBreakdown(cellSize);
		}

		#endregion

		#region Private Methods

		private static double GetVolume(ThrusterDNA dna)
		{
			//	Just assume it's a cylinder
			double radX = dna.Scale.X * ThrusterDesign.RADIUSPERCENTOFSCALE;
			double radY = dna.Scale.Y * ThrusterDesign.RADIUSPERCENTOFSCALE;
			double height = dna.Scale.Z;

			return Math.PI * radX * radY * height;
		}

		private static double GetMass(ItemOptions itemOptions, int thrustCount, double cylinderVolume)
		{
			//	Get the mass of the cylinder
			double retVal = cylinderVolume * itemOptions.ThrusterDensity;

			//	Instead of trying some complex formula to figure out how much each extra thruster weighs, just add
			//	a percent of the base mass for each additional thruster
			if (thrustCount > 1)
			{
				retVal += (thrustCount - 1) * (retVal * itemOptions.ThrusterAdditionalMassPercent);
			}

			//	Exit Function
			return retVal;
		}

		#endregion
	}

	#endregion

	#region Class: ThrusterDNA

	public class ThrusterDNA : PartDNA
	{
		public ThrusterType ThrusterType
		{
			get;
			set;
		}
		public Vector3D[] ThrusterDirections
		{
			get;
			set;
		}
	}

	#endregion

	#region Enum: ThrusterType

	public enum ThrusterType
	{
		/// <summary>
		/// Big thruster, pointing in a single direction
		/// </summary>
		One,
		/// <summary>
		/// Two thrusters pointing +/-  z axis
		/// </summary>
		Two,
		/// <summary>
		/// Two way along the z axis, one way along the x axis
		/// </summary>
		Two_One,
		/// <summary>
		/// Two way along the z axis, two way along the x axis
		/// </summary>
		Two_Two,
		/// <summary>
		/// Two way along the z axis, two way along the x axis, one way along the y axis
		/// </summary>
		Two_Two_One,
		/// <summary>
		/// 3 Two ways along the x/y/z axiis
		/// </summary>
		Two_Two_Two,
		/// <summary>
		/// An array of vectors is passed in, and a thruster is built along each vector (both size and direction)
		/// </summary>
		Custom
	}

	#endregion
}
