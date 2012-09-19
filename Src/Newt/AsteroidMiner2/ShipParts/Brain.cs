using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	//TODO: Use color options class

	#region Class: BrainToolItem

	public class BrainToolItem : PartToolItemBase
	{
		#region Constructor

		public BrainToolItem(EditorOptions options)
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
				return "Brain";
			}
		}
		public override string Description
		{
			get
			{
				return "Consumes energy, makes decisions :)";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_EQUIPMENT;
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
			return new BrainDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: BrainDesign

	public class BrainDesign : PartDesignBase
	{
		#region Constructor

		public BrainDesign(EditorOptions options)
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
			const double HEIGHT = 1d;
			const double SCALE = .75d / HEIGHT;
			const double INSIDEPOINTRADIUS = .45d;

			ScaleTransform3D scaleTransform = new ScaleTransform3D(SCALE, SCALE, SCALE);

			int domeSegments = isFinal ? 2 : 10;
			int cylinderSegments = isFinal ? 6 : 35;

			Model3DGroup retVal = new Model3DGroup();

			GeometryModel3D geometry;
			MaterialGroup material;
			DiffuseMaterial diffuse;
			SpecularMaterial specular;

			Transform3DGroup transformGroup = new Transform3DGroup();
			//transformGroup.Children.Add(new TranslateTransform3D(0, 0, ));
			transformGroup.Children.Add(scaleTransform);

			#region Insides

			if (!isFinal)
			{
				List<Point3D[]> insidePoints = new List<Point3D[]>();
				for (int cntr = 0; cntr < 3; cntr++)
				{
					GetLineBranch(insidePoints, Math3D.GetRandomVectorSpherical(INSIDEPOINTRADIUS).ToPoint(), INSIDEPOINTRADIUS, INSIDEPOINTRADIUS * .8d, .33d, 4);
				}

				Random rand = StaticRandom.GetRandomForThread();

				foreach (Point3D[] lineSegment in insidePoints)
				{
					geometry = new GeometryModel3D();
					material = new MaterialGroup();

					Color color = this.Options.WorldColors.BrainInsideStrand;		//	storing this, because it's random
					diffuse = new DiffuseMaterial(new SolidColorBrush(color));
					this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, color));
					material.Children.Add(diffuse);

					specular = this.Options.WorldColors.BrainInsideStrandSpecular;
					this.MaterialBrushes.Add(new MaterialColorProps(specular));
					material.Children.Add(specular);

					//if (!isFinal)
					//{
					EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
					material.Children.Add(selectionEmissive);
					base.SelectionEmissives.Add(selectionEmissive);
					//}

					geometry.Material = material;
					geometry.BackMaterial = material;

					Vector3D line = lineSegment[1] - lineSegment[0];
					double lineLength = line.Length;
					double halfLength = lineLength * .5d;
					double widestWidth = lineLength * .033d;

					List<TubeRingBase> rings = new List<TubeRingBase>();
					rings.Add(new TubeRingPoint(0, false));
					rings.Add(new TubeRingRegularPolygon(halfLength, false, widestWidth, widestWidth, false));
					rings.Add(new TubeRingPoint(halfLength, false));

					Quaternion zRot = new Quaternion(new Vector3D(0, 0, 1), 360d * rand.NextDouble()).ToUnit();
					Quaternion rotation = Math3D.GetRotation(new Vector3D(0, 0, 1), line).ToUnit();

					transformGroup = new Transform3DGroup();
					transformGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Quaternion.Multiply(rotation, zRot))));
					transformGroup.Children.Add(new TranslateTransform3D(lineSegment[0].ToVector()));
					transformGroup.Children.Add(scaleTransform);

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(3, rings, true, false, transformGroup);

					retVal.Children.Add(geometry);
				}
			}

			#endregion

			#region Lights

			//	Neat effect, but it makes my fan spin up, and won't slow back down.  Need to add an animation property to the options
			//	class (and listen for when it toggles)

			//if (!isFinal)
			//{
			//    int numLights = 1 + this.Options.Random.Next(3);

			//    for (int cntr = 0; cntr < numLights; cntr++)
			//    {
			//        PointLight light = new PointLight();
			//        light.Color = Colors.Black;
			//        light.Range = SCALE * INSIDEPOINTRADIUS * 2d;
			//        light.LinearAttenuation = 1d;

			//        transformGroup = new Transform3DGroup();
			//        transformGroup.Children.Add(new TranslateTransform3D(Math3D.GetRandomVectorSpherical(this.Options.Random, INSIDEPOINTRADIUS)));
			//        transformGroup.Children.Add(scaleTransform);
			//        light.Transform = transformGroup;

			//        retVal.Children.Add(light);

			//        ColorAnimation animation = new ColorAnimation();
			//        animation.From = UtilityWPF.ColorFromHex("CC1266");
			//        animation.To = Colors.Black;
			//        animation.Duration = new Duration(TimeSpan.FromSeconds(1d + (this.Options.Random.NextDouble() * 5d)));
			//        animation.AutoReverse = true;
			//        animation.RepeatBehavior = RepeatBehavior.Forever;
			//        animation.AccelerationRatio = .5d;
			//        animation.DecelerationRatio = .5d;

			//        light.BeginAnimation(PointLight.ColorProperty, animation);
			//    }
			//}

			#endregion

			#region Outer Shell

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			Color shellColor = this.Options.WorldColors.Brain;
			if (!isFinal)
			{
				shellColor = UtilityWPF.AlphaBlend(shellColor, Colors.Transparent, .75d);
			}
			diffuse = new DiffuseMaterial(new SolidColorBrush(shellColor));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, shellColor));
			material.Children.Add(diffuse);

			specular = this.Options.WorldColors.BrainSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				base.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			transformGroup = new Transform3DGroup();
			//transformGroup.Children.Add(new TranslateTransform3D(0, 0, 1.65 / -2d));
			transformGroup.Children.Add(scaleTransform);


			Point3D[] spherePoints = new Point3D[100];
			for (int cntr = 0; cntr < spherePoints.Length; cntr++)
			{
				spherePoints[cntr] = Math3D.GetRandomVectorSphericalShell(.5d).ToPoint();
			}

			transformGroup.Transform(spherePoints);

			TriangleIndexed[] sphereTriangles = UtilityWPF.GetConvexHull(spherePoints);

			geometry.Geometry = UtilityWPF.GetMeshFromTriangles(sphereTriangles);

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

		/// <summary>
		/// This is a recursive method that adds a line to the point passed in, and has a chance of branching
		/// </summary>
		private static void GetLineBranch(List<Point3D[]> resultPoints, Point3D fromPoint, double radius, double maxDistFromPoint, double splitProbability, int remaining)
		{
			if (remaining < 0)
			{
				return;
			}

			int numBranches = 1;

			//	See if this should do a split
			if (StaticRandom.NextDouble() < splitProbability)
			{
				numBranches = StaticRandom.NextDouble() > .8 ? 3 : 2;
			}

			for (int cntr = 0; cntr < numBranches; cntr++)
			{
				//	Add a line
				Vector3D toPoint;
				do
				{
					toPoint = (fromPoint + Math3D.GetRandomVectorSpherical(maxDistFromPoint)).ToVector();
				} while (toPoint.LengthSquared > radius * radius);

				resultPoints.Add(new Point3D[] { fromPoint, toPoint.ToPoint() });

				double newMaxDist = maxDistFromPoint * .85d;

				//	Make the next call have a higher chance of branching
				double newSplitProbability = 1d - ((1d - splitProbability) / 1.2d);

				//	Recurse
				GetLineBranch(resultPoints, toPoint.ToPoint(), radius, newMaxDist, newSplitProbability, remaining - 1);
			}
		}

		#endregion
	}

	#endregion
	#region Class: Brain

	public class Brain
	{
		public const string PARTTYPE = "Brain";

	}

	#endregion
}
