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
	#region Class: ConverterMatterToFuelToolItem

	public class ConverterMatterToFuelToolItem : PartToolItemBase
	{
		#region Constructor

		public ConverterMatterToFuelToolItem(EditorOptions options)
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
				return "Matter to Fuel Converter";
			}
		}
		public override string Description
		{
			get
			{
				return "Pulls matter out of the cargo bay, consumes some energy, and puts fuel in the fuel tank";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_CONVERTERS;
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
			return new ConverterMatterToFuelDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterMatterToFuelDesign

	public class ConverterMatterToFuelDesign : PartDesignBase
	{
		#region Constructor

		public ConverterMatterToFuelDesign(EditorOptions options)
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
			return CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
				_scaleTransform, _rotateTransform, _translateTransform,
				this.Options.WorldColors.ConverterBase, this.Options.WorldColors.ConverterBaseSpecular, this.Options.WorldColors.ConverterFuel, this.Options.WorldColors.ConverterFuelSpecular,
				isFinal);
		}

		internal static Model3DGroup CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, ScaleTransform3D scaleTransform, QuaternionRotation3D rotateTransform, TranslateTransform3D translateTransform, Color baseColor, SpecularMaterial baseSpecular, Color colorColor, SpecularMaterial colorSpecular, bool isFinal)
		{
			const double SCALE = .5d;

			Model3DGroup retVal = new Model3DGroup();

			GeometryModel3D geometry;
			MaterialGroup material;
			DiffuseMaterial diffuse;
			SpecularMaterial specular;

			#region Main Cube

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(baseColor));
			materialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, baseColor));
			material.Children.Add(diffuse);
			specular = baseSpecular;
			materialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				selectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = GetMeshBase(SCALE);

			retVal.Children.Add(geometry);

			#endregion

			#region Color Cube

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(colorColor));
			materialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, colorColor));
			material.Children.Add(diffuse);
			specular = colorSpecular;
			materialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				selectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = GetMeshColor(SCALE);

			retVal.Children.Add(geometry);

			#endregion

			//	Transform
			Transform3DGroup transformGlobal = new Transform3DGroup();
			if (isFinal)
			{
				transformGlobal.Children.Add(scaleTransform.Clone());
				transformGlobal.Children.Add(new RotateTransform3D(rotateTransform.Clone()));
				transformGlobal.Children.Add(translateTransform.Clone());
			}
			else
			{
				transformGlobal.Children.Add(scaleTransform);
				transformGlobal.Children.Add(new RotateTransform3D(rotateTransform));
				transformGlobal.Children.Add(translateTransform);
			}
			retVal.Transform = transformGlobal;

			//	Exit Function
			return retVal;
		}

		internal static MeshGeometry3D GetMeshBase(double scale)
		{
			//NOTE: These tips are negative
			return GetMesh(.5d * scale, -.1d * scale, 1);
		}
		internal static MeshGeometry3D GetMeshColor(double scale)
		{
			return GetMesh(.35d * scale, .15d * scale, 1);
		}
		internal static MeshGeometry3D GetMesh(double half, double tip, int faceCount)
		{
			// Define 3D mesh object
			MeshGeometry3D retVal = new MeshGeometry3D();

			int pointOffset = 0;

			//	Front
			Transform3DGroup transform = new Transform3DGroup();
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
			transform.Children.Add(new TranslateTransform3D(half, 0, 0));
			ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

			//	Right
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
			ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

			//	Back
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
			ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

			//	Left
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
			ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

			//	Top
			transform = new Transform3DGroup();
			transform.Children.Add(new TranslateTransform3D(0, 0, half));
			ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

			//	Bottom
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
			ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

			//	Exit Function
			return retVal;
		}
		internal static void GetMeshFace(ref int pointOffset, MeshGeometry3D mesh, Transform3D transform, double halfWidth, double halfHeight, double tip, int numPyramids)
		{
			double faceWidth = halfWidth / numPyramids;
			double faceHeight = halfHeight / numPyramids;

			int to = numPyramids - 1;
			int from = to * -1;

			for (int x = from; x <= to; x += 2)
			{
				for (int y = from; y <= to; y += 2)
				{
					double offsetX = faceWidth * x;
					double offsetY = faceHeight * y;

					//	Bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY - faceHeight, 0)));		//	left bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY - faceHeight, 0)));		//	right bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;

					//	Right
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY - faceHeight, 0)));		//	right bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY + faceHeight, 0)));		//	right top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;

					//	Top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY + faceHeight, 0)));		//	right top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY + faceHeight, 0)));		//	left top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;

					//	Left
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY + faceHeight, 0)));		//	left top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY - faceHeight, 0)));		//	left bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;
				}
			}
		}

		#endregion
	}

	#endregion
	#region Class: ConverterMatterToFuel

	public class ConverterMatterToFuel
	{
		public const string PARTTYPE = "ConverterMatterToFuel";
	}

	#endregion
}
