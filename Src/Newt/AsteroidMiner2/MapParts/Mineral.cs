﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.NewtonDynamics;
using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner2.MapParts
{
	public class Mineral : IMapObject
	{
		#region Constructor

		public Mineral(MineralType mineralType, Point3D position, double volumeInCubicMeters, World world, int materialID, MaterialManager materialManager, SharedVisuals sharedVisuals)
		{
			if (mineralType == MineralType.Custom)
			{
				throw new ApplicationException("Custom minerals aren't currently supported");
			}

			this.MineralType = mineralType;

			// Overwrite my public properties based on the mineral type
			Color diffuseColor, specularColor, emissiveColor;
			double specularPower, mass;
			GetSettingsForMineralType(out diffuseColor, out specularColor, out specularPower, out emissiveColor, out mass, mineralType, volumeInCubicMeters);

			Model3DGroup models = new Model3DGroup();

			#region WPF Model

			//	Material
			MaterialGroup materials = new MaterialGroup();
			if (diffuseColor.A > 0)
			{
				materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(diffuseColor)));
			}

			if (specularColor.A > 0)
			{
				materials.Children.Add(new SpecularMaterial(new SolidColorBrush(specularColor), specularPower));
			}

			if (emissiveColor.A > 0)
			{
				materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(emissiveColor)));
			}

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;

			//if (_mineralType == MineralType.Custom)
			//{
			//    geometry.Geometry = UtilityWPF.GetMultiRingedTube(_numSides, _rings);
			//}
			//else
			//{
			geometry.Geometry = sharedVisuals.GetMineralMesh(mineralType);
			//}
			models.Children.Add(geometry);

			if (mineralType == MineralType.Rixium)
			{
				#region Rixium Visuals

				//	These need to be added after the may crystal, because they are semitransparent

				models.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, -.6), .38, .5, sharedVisuals));
				models.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, -.3), .44, .75, sharedVisuals));
				models.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, 0), .5, 1, sharedVisuals));
				models.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, .3), .44, .75, sharedVisuals));
				models.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, .6), .38, .5, sharedVisuals));

				//TODO:  Look at the global lighting options
				PointLight pointLight = new PointLight();
				pointLight.Color = Color.FromArgb(255, 54, 147, 168);
				pointLight.Range = 20;
				pointLight.LinearAttenuation = .33;
				models.Children.Add(pointLight);

				#endregion
			}

			//	Model Visual
			ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
			model.Content = models;

			Transform3DGroup transform = new Transform3DGroup();
			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
			transform.Children.Add(new TranslateTransform3D(position.ToVector()));
			model.Transform = transform;

			#endregion

			#region Physics Body

			Point3D[] hullPoints = UtilityWPF.GetPointsFromMesh((MeshGeometry3D)geometry.Geometry);

			CollisionHull hull = CollisionHull.CreateConvexHull(world, 0, hullPoints, 0.002d, null);

			this.PhysicsBody = new Body(hull, model.Transform.Value, mass, new Visual3D[] { model });
			this.PhysicsBody.MaterialGroupID = materialID;
			this.PhysicsBody.LinearDamping = .01f;
			this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

			//this.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

			#endregion

			//	Calculate radius
			Point3D aabbMin, aabbMax;
			this.PhysicsBody.GetAABB(out aabbMin, out aabbMax);
			this.Radius = (aabbMax - aabbMin).Length / 2d;
		}

		#endregion

		#region IMapObject Members

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

		public MineralType MineralType
		{
			get;
			private set;
		}

		#endregion

		#region Private Methods

		private static void GetSettingsForMineralType(out Color diffuseColor, out Color specularColor, out double specularPower, out Color emissiveColor, out double mass, MineralType mineralType, double volumeInCubicMeters)
		{
			//NOTE:  The geometry is defined in SharedVisuals.GetMineralMesh()

			switch (mineralType)
			{
				case MineralType.Ice:
					#region Ice

					// Going for an ice cube  :)

					diffuseColor = Color.FromArgb(192, 201, 233, 242);       // slightly bluish white
					specularColor = Color.FromArgb(255, 203, 212, 214);
					specularPower = 66d;
					emissiveColor = Colors.Transparent;

					mass = 934 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Iron:
					#region Iron

					// This will be an iron bar (with rust)

					diffuseColor = Color.FromArgb(255, 92, 78, 72);
					specularColor = Color.FromArgb(255, 117, 63, 40);
					specularPower = 50d;
					emissiveColor = Colors.Transparent;

					mass = 7900 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Graphite:
					#region Graphite

					// A shiny lump of coal (but coal won't form in space, so I call it graphite)

					//_diffuseColor = Color.FromArgb(255, 64, 64, 64);
					diffuseColor = Color.FromArgb(255, 32, 32, 32);
					specularColor = Color.FromArgb(255, 209, 209, 209);
					specularPower = 75d;
					emissiveColor = Colors.Transparent;

					mass = 2267 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Gold:
					#region Gold

					// A reflective gold bar

					diffuseColor = Color.FromArgb(255, 255, 191, 0);
					specularColor = Color.FromArgb(255, 212, 138, 0);
					specularPower = 75d;
					emissiveColor = Colors.Transparent;

					mass = 19300 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Platinum:
					#region Platinum

					// A reflective platinum bar/plate
					//TODO:  Make this a flat plate

					diffuseColor = Color.FromArgb(255, 166, 166, 166);
					specularColor = Color.FromArgb(255, 125, 57, 45);
					specularPower = 95d;
					emissiveColor = Color.FromArgb(20, 214, 214, 214);

					mass = 21450 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Emerald:
					#region Emerald

					// A semi transparent double trapazoid

					diffuseColor = Color.FromArgb(192, 69, 128, 64);
					specularColor = Color.FromArgb(255, 26, 82, 20);
					specularPower = 100d;
					emissiveColor = Color.FromArgb(32, 64, 128, 0);

					mass = 2760 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Saphire:
					#region Saphire

					// A jeweled oval

					diffuseColor = Color.FromArgb(160, 39, 53, 102);
					specularColor = Color.FromArgb(255, 123, 141, 201);
					specularPower = 100d;
					emissiveColor = Color.FromArgb(64, 17, 57, 189);

					mass = 4000 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Ruby:
					#region Ruby

					// A jeweled oval

					diffuseColor = Color.FromArgb(180, 176, 0, 0);
					specularColor = Color.FromArgb(255, 255, 133, 133);
					specularPower = 100d;
					emissiveColor = Color.FromArgb(32, 156, 53, 53);

					mass = 4000 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Diamond:
					#region Diamond

					// A jewel

					diffuseColor = Color.FromArgb(128, 230, 230, 230);
					specularColor = Color.FromArgb(255, 196, 196, 196);
					specularPower = 100d;
					emissiveColor = Color.FromArgb(32, 255, 255, 255);

					mass = 3515 * volumeInCubicMeters;

					#endregion
					break;

				case MineralType.Rixium:
					#region Rixium

					// A petagon rod
					// There are also some toruses around it, but they are just visuals.  This rod is the collision mesh

					diffuseColor = Color.FromArgb(192, 92, 59, 112);
					//_specularColor = Color.FromArgb(255, 145, 63, 196);
					specularColor = Color.FromArgb(255, 77, 127, 138);
					specularPower = 100d;
					emissiveColor = Color.FromArgb(64, 112, 94, 122);

					mass = 66666 * volumeInCubicMeters;

					#endregion
					break;

				default:
					throw new ApplicationException("Unknown MineralType: " + mineralType.ToString());
			}
		}

		/// <param name="intensity">brightness from 0 to 1</param>
		private static Model3D GetRixiumTorusVisual(Vector3D location, double radius, double intensity, SharedVisuals sharedVisuals)
		{
			//	Material
			MaterialGroup material = new MaterialGroup();
			//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 44, 9, 82))));       // purple color
			material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 30, 160, 189))));       // teal color
			//material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 104, 79, 130)), 100d));     // purple reflection
			material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 60, 134, 150)), 100d));

			byte emissiveAlpha = Convert.ToByte(140 * intensity);

			//material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(64, 85, 50, 122))));
			material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(emissiveAlpha, 85, 50, 122))));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = material;
			geometry.BackMaterial = material;
			geometry.Geometry = sharedVisuals.GetRixiumTorusMesh(radius);

			Transform3DGroup transforms = new Transform3DGroup();
			transforms.Children.Add(new TranslateTransform3D(location));		//	this is in model coords
			geometry.Transform = transforms;

			// Exit Function
			return geometry;
		}

		#endregion
	}

	#region Enum: MineralType

	/// <summary>
	/// I tried to list these in order of value
	/// </summary>
	/// <remarks>
	/// The density is in kg/cu.m
	/// 1 carat is 200 mg (or .0002 kg)
	/// 
	/// TODO:  The $ are way out of whack to be useful in game
	/// </remarks>
	public enum MineralType
	{
		Custom,
		/// <summary>
		/// Density = 934
		/// </summary>
		Ice,
		/// <summary>
		/// $1.70 per metric ton
		/// $.0017 per kg
		/// Density = 7,900
		/// </summary>
		Iron,
		/// <summary>
		/// $70 per short ton
		/// $.08 per kg
		/// Density = 2,267
		/// </summary>
		/// <remarks>
		/// Can't use coal, because there's no way it would appear naturally in space
		/// </remarks>
		Graphite,
		/// <summary>
		/// $1,400 per oz
		/// $49,420 per kg
		/// Density = 19,300
		/// </summary>
		Gold,
		/// <summary>
		/// $1,700 per oz
		/// $59,840 per kg
		/// Density = 21,450
		/// </summary>
		Platinum,
		/// <summary>
		/// $250 per carat
		/// $1,250,000 per kg
		/// Density = 2,760
		/// </summary>
		Emerald,
		/// <summary>
		/// $1,000 per carat
		/// $5,000,000 per kg
		/// Density = 4,000
		/// </summary>
		Saphire,
		/// <summary>
		/// $2,500 per carat
		/// $12,500,000 per kg
		/// Density = 4,000
		/// </summary>
		Ruby,
		/// <summary>
		/// $15,000 per carat
		/// $75,000,000 per kg
		/// Density = 3,515
		/// </summary>
		Diamond,
		/// <summary>
		/// $300,000,000 per kg
		/// Density = 66,666
		/// </summary>
		Rixium
	}

	#endregion
}