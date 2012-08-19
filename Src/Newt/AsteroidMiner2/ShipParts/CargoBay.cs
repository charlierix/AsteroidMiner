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
	#region Class: CargoBayToolItem

	public class CargoBayToolItem : PartToolItemBase
	{
		#region Constructor

		public CargoBayToolItem(EditorOptions options)
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
				return "Cargo Bay";
			}
		}
		public override string Description
		{
			get
			{
				return "Stores materials";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_CONTAINER;
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
			return new CargoBayDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: CargoBayDesign

	public class CargoBayDesign : PartDesignBase
	{
		#region Constructor

		public CargoBayDesign(EditorOptions options)
			: base(options) { }

		#endregion

		#region Public Properties

		public override PartDesignAllowedScale AllowedScale
		{
			get
			{
				return PartDesignAllowedScale.X_Y_Z;
			}
		}
		public override PartDesignAllowedRotation AllowedRotation
		{
			get
			{
				return PartDesignAllowedRotation.X_Y_Z;
			}
		}

		private GeometryModel3D _geometry = null;
		public override Model3D Model
		{
			get
			{
				if (_geometry == null)
				{
					_geometry = CreateGeometry(false);
				}

				return _geometry;
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

		private GeometryModel3D CreateGeometry(bool isFinal)
		{
			MaterialGroup material = new MaterialGroup();
			DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.CargoBay));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.CargoBay));
			material.Children.Add(diffuse);
			SpecularMaterial specular = this.Options.WorldColors.CargoBaySpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				base.SelectionEmissives.Add(selectionEmissive);
			}

			GeometryModel3D retVal = new GeometryModel3D();
			retVal.Material = material;
			retVal.BackMaterial = material;

			if (isFinal)
			{
				retVal.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-.5, -.5, -.5), new Point3D(.5, .5, .5));
			}
			else
			{
				retVal.Geometry = GetMesh();
			}

			//	Transform
			Transform3DGroup transform = new Transform3DGroup();
			if (isFinal)
			{
				transform.Children.Add(_scaleTransform.Clone());
				transform.Children.Add(new RotateTransform3D(_rotateTransform.Clone()));
				transform.Children.Add(_translateTransform.Clone());
			}
			else
			{
				transform.Children.Add(_scaleTransform);
				transform.Children.Add(new RotateTransform3D(_rotateTransform));
				transform.Children.Add(_translateTransform);
			}
			retVal.Transform = transform;

			//	Exit Function
			return retVal;
		}

		/// <summary>
		/// This returns a cube made of shallow pyramids
		/// </summary>
		/// <remarks>
		/// I was going to keep the tip height static by overriding base.scale, and rebuilding the mesh instead of just scaling
		/// a perfect cube (I would have had to used scaled height/width and translate the sides).  But the scaled cube doesn't
		/// look too bad, and it's a lot easier
		/// </remarks>
		private static MeshGeometry3D GetMesh()
		{
			const double HALF = .5d;
			const double TIP = -.025d;
			//const double HALF = .4;
			//const double TIP = .05d;

			// Define 3D mesh object
			MeshGeometry3D retVal = new MeshGeometry3D();

			int pointOffset = 0;

			//	Front
			Transform3DGroup transform = new Transform3DGroup();
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
			transform.Children.Add(new TranslateTransform3D(HALF, 0, 0));
			GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

			//	Right
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
			GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

			//	Back
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
			GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

			//	Left
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
			GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

			//	Top
			transform = new Transform3DGroup();
			transform.Children.Add(new TranslateTransform3D(0, 0, HALF));
			GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

			//	Bottom
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
			GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

			//	Exit Function
			return retVal;
		}
		private static void GetMeshSprtFace(ref int pointOffset, MeshGeometry3D mesh, Transform3D transform, double halfWidth, double halfHeight, double tip)
		{
			double quarterWidth = halfWidth * .5d;
			double quarterHeight = halfHeight * .5d;

			for (int x = -1; x < 2; x += 2)
			{
				for (int y = -1; y < 2; y += 2)
				{
					double offsetX = quarterWidth * x;
					double offsetY = quarterHeight * y;

					//	Bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY - quarterHeight, 0)));		//	left bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY - quarterHeight, 0)));		//	right bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;

					//	Right
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY - quarterHeight, 0)));		//	right bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY + quarterHeight, 0)));		//	right top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;

					//	Top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY + quarterHeight, 0)));		//	right top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY + quarterHeight, 0)));		//	left top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;

					//	Left
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY + quarterHeight, 0)));		//	left top
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY - quarterHeight, 0)));		//	left bottom
					mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		//	tip
					mesh.TriangleIndices.Add(pointOffset + 0);
					mesh.TriangleIndices.Add(pointOffset + 1);
					mesh.TriangleIndices.Add(pointOffset + 2);
					pointOffset += 3;
				}
			}
		}
		private static void GetMeshSprtFace_OLD(ref int pointOffset, MeshGeometry3D mesh, Transform3D transform, double halfWidth, double halfHeight, double tip)
		{
			//	Bottom
			mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, -halfHeight, 0)));		//	left bottom
			mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, -halfHeight, 0)));		//	right bottom
			mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		//	tip
			mesh.TriangleIndices.Add(pointOffset + 0);
			mesh.TriangleIndices.Add(pointOffset + 1);
			mesh.TriangleIndices.Add(pointOffset + 2);
			pointOffset += 3;

			//	Right
			mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, -halfHeight, 0)));		//	right bottom
			mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, halfHeight, 0)));		//	right top
			mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		//	tip
			mesh.TriangleIndices.Add(pointOffset + 0);
			mesh.TriangleIndices.Add(pointOffset + 1);
			mesh.TriangleIndices.Add(pointOffset + 2);
			pointOffset += 3;

			//	Top
			mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, halfHeight, 0)));		//	right top
			mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, halfHeight, 0)));		//	left top
			mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		//	tip
			mesh.TriangleIndices.Add(pointOffset + 0);
			mesh.TriangleIndices.Add(pointOffset + 1);
			mesh.TriangleIndices.Add(pointOffset + 2);
			pointOffset += 3;

			//	Left
			mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, halfHeight, 0)));		//	left top
			mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, -halfHeight, 0)));		//	left bottom
			mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		//	tip
			mesh.TriangleIndices.Add(pointOffset + 0);
			mesh.TriangleIndices.Add(pointOffset + 1);
			mesh.TriangleIndices.Add(pointOffset + 2);
			pointOffset += 3;
		}

		#endregion
	}

	#endregion
	#region Class: CargoBay

	public class CargoBay
	{
		public const string PARTTYPE = "CargoBay";

	}

	#endregion
}
