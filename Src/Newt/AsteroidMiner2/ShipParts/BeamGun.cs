using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	#region Class: BeamGunToolItem

	public class BeamGunToolItem : PartToolItemBase
	{
		#region Constructor

		public BeamGunToolItem(EditorOptions options)
			: base(options)
		{
			_visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
			this.TabName = PartToolItemBase.TAB_SHIPPART;
		}

		#endregion

		#region Public Properties

		public override string Name
		{
			get
			{
				return "Beam Gun";
			}
		}
		public override string Description
		{
			get
			{
				return "Short range energy weapon.  More like a laser sword or flame thrower than a laser gun.  Consumes energy";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_WEAPON;
			}
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
			return new BeamGunDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: BeamGunDesign

	public class BeamGunDesign : PartDesignBase
	{
		#region Constructor

		public BeamGunDesign(EditorOptions options)
			: base(options) { }

		#endregion

		#region Public Properties

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
				return PartDesignAllowedRotation.X_Y_Z;
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

		#endregion

		#region Private Methods

		private Model3DGroup CreateGeometry(bool isFinal)
		{
			Model3DGroup retVal = new Model3DGroup();

			GeometryModel3D geometry;
			MaterialGroup material;
			DiffuseMaterial diffuse;
			SpecularMaterial specular;
			EmissiveMaterial emissive;

			int domeSegments = isFinal ? 2 : 10;
			int cylinderSegments = isFinal ? 6 : 35;

			#region Mount Box

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.GunBase));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.GunBase));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.GunBaseSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = UtilityWPF.GetCube(new Point3D(-.115, -.1, -.5), new Point3D(.115, .1, -.1));

			retVal.Children.Add(geometry);

			#endregion

			#region Barrel

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.GunBarrel));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.GunBarrel));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.GunBarrelSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			const double OUTERRADIUS = .045;
			const double INNERRADIUS = .04;

			List<TubeRingBase> rings = new List<TubeRingBase>();

			rings.Add(new TubeRingRegularPolygon(0, false, OUTERRADIUS * .33, OUTERRADIUS * .33, false));		//	Start at the base of the barrel
			rings.Add(new TubeRingRegularPolygon(.69, false, OUTERRADIUS * .33, OUTERRADIUS * .33, false));		//	This is the tip of the barrel
			//rings.Add(new TubeRingRegularPolygon(0, false, INNERRADIUS, INNERRADIUS, false));		//	Curl to the inside
			//rings.Add(new TubeRingRegularPolygon(-.75d, false, INNERRADIUS, INNERRADIUS, false));		//	Loop back to the base

			geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, new TranslateTransform3D(0, 0, -.49d));

			retVal.Children.Add(geometry);

			#endregion

			#region Dish

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.BeamGunDish));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.BeamGunDish));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.BeamGunDishSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			rings = new List<TubeRingBase>();
			rings.Add(new TubeRingDome(0, false, domeSegments));
			rings.Add(new TubeRingRegularPolygon(.16, false, OUTERRADIUS * 3.5d, OUTERRADIUS * 3.5d, false));
			rings.Add(new TubeRingDome(-.08, false, domeSegments));		//	concave dish

			geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, new TranslateTransform3D(0, 0, -.12d));

			retVal.Children.Add(geometry);

			#endregion

			#region Focus Crystal

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.BeamGunCrystal));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.BeamGunCrystal));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.BeamGunCrystalSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);
			emissive = this.Options.WorldColors.BeamGunCrystalEmissive;
			this.MaterialBrushes.Add(new MaterialColorProps(emissive));
			material.Children.Add(emissive);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			//rings = new List<TubeRingBase>();
			//rings.Add(new TubeRingPoint(0, false));
			//rings.Add(new TubeRingRegularPolygon(.667, false, OUTERRADIUS * 2d, OUTERRADIUS * 2d, false));
			//rings.Add(new TubeRingPoint(1.33, false));

			//geometry.Geometry = UtilityWPF.GetMultiRingedTube(3, rings, true, false, new TranslateTransform3D(0, 0, .25d));

			List<TubeRingDefinition_ORIG> rings2 = new List<TubeRingDefinition_ORIG>();
			rings2.Add(new TubeRingDefinition_ORIG(0, false));
			rings2.Add(new TubeRingDefinition_ORIG(OUTERRADIUS * 3d, OUTERRADIUS * 3d, .067d, true, false));		//	I think the old tube definition is diameter instead of radius, so needs to be doubled
			rings2.Add(new TubeRingDefinition_ORIG(.133, false));

			geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(5, rings2, false, false);
			geometry.Transform = new TranslateTransform3D(0, 0, .15d);

			retVal.Children.Add(geometry);

			#endregion

			#region Trim

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.BeamGunTrim));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.BeamGunTrim));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.BeamGunTrimSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = UtilityWPF.GetCube(new Point3D(-.095, -.11, -.45), new Point3D(.095, .11, -.15));

			retVal.Children.Add(geometry);

			#endregion

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

		#endregion
	}

	#endregion
	#region Class: BeamGun

	public class BeamGun
	{
		public const string PARTTYPE = "BeamGun";

	}

	#endregion
}
