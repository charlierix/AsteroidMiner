using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;
using System.Text.RegularExpressions;

namespace Game.Newt.AsteroidMiner2
{
	//TODO: Add a constraint that cargo bays need tubes between them, and a tube exposed to space (or a hatch if connected to the hull)
	//TODO: PartBase needs to support hit points
	//TODO: PartToolItemBase and PartDesignBase need to support single instance (with dna).  This will represent salvaged parts

	#region Class: PartToolItemBase

	/// <summary>
	/// This is used to represent the tool in the toolbox
	/// </summary>
	public abstract class PartToolItemBase
	{
		#region Declaration Section

		//TODO: The other part types don't allow intersection, but structural can (if a bar goes through a container, it is assumed that
		//it's actually 2 bars that are welded to the outside of the container (no bar running through the inside)

		//TODO: Come up with icons for these
		public const string CATEGORY_CONTAINER = "Containers";		//	Cargo, Fuel, Energy Tank, Ammo
		public const string CATEGORY_PROPULSION = "Propulsion";		//	Thruster, Tractor Beam, Grapple Gun/Spider Web
		public const string CATEGORY_SENSOR = "Sensors";		//	Brain, Eyes (camera), Eyes (range), Fluid Flow, Gravity
		public const string CATEGORY_WEAPON = "Weapons";		//	Projectile Gun, Energy Gun, Spike
		public const string CATEGORY_CONVERTERS = "Converters";		//	Radiation-Energy, Cargo-Energy, Cargo-Fuel
		public const string CATEGORY_EQUIPMENT = "Equipment";		//	this is sort of a misc category
		public const string CATEGORY_SHIELD = "Shields";		//	this is sort of a misc category
		public const string CATEGORY_STRUCTURAL = "Structural";		//	Hulls, Armor Shards, Spars, Wings
		public const string CATEGORY_GROUPS = "Groups";
		public const string CATEGORY_VISUALS = "Visuals";

		public const string TAB_SHIPPART = "Parts";		//	parts won't allow intersection with other parts
		public const string TAB_SHIPSTRUCTURE = "Structural";		//	structural will allow intersection
		public const string TAB_VISUAL = "Visuals";

		//TODO: Come up with tabs/categories for a map editor:
		//		Stars, Planets, Asteroids, Minerals
		//		Radiation/Fluid zones
		//		Vector Field
		//		Item Source/Sink

		#endregion

		#region Constructor

		public PartToolItemBase(EditorOptions options)
		{
			this.Options = options;
			string test = this.GetType().ToString();
		}

		#endregion

		#region Public Properties

		protected EditorOptions Options
		{
			get;
			private set;
		}

		public abstract string Name
		{
			get;
		}
		public abstract string Description
		{
			get;
		}
		public abstract string Category
		{
			get;
		}

		/// <summary>
		/// This is set to a default value by each derived part, but can be changed by consuming code
		/// </summary>
		/// <remarks>
		/// There are a lot of categories, and a tab for each would be tedious.  So probably have fewer tabs, and group by category
		/// </remarks>
		public string TabName
		{
			get;
			set;
		}

		private string _partType = null;
		/// <summary>
		/// This is useful for matching this to the corresponding design and final parts.  This gets stored in the dna class
		/// </summary>
		public string PartType
		{
			get
			{
				if (_partType == null)
				{
					Match match = Regex.Match(this.GetType().ToString(), @"\.(?<name>[^\.]+)ToolItem$");		//	this assumes that each derived class ends with the word Design
					if (match.Success)
					{
						_partType = match.Groups["name"].Value;
					}
					else
					{
						throw new ApplicationException("All classes that derive from this are expected to end with the word ToolItem (and share the same name with the design and final classes)");
					}
				}

				return _partType;
			}
		}

		//	If the unique part is set, then only this one part is allowed to exist.  Also, this part can't be modified beyond changing
		//	position/rotation
		public bool IsUnique
		{
			get
			{
				return this.UniquePart != null;
			}
		}
		public PartDesignBase UniquePart
		{
			get;
			set;
		}

		public abstract UIElement Visual2D
		{
			get;
		}

		#endregion

		#region Public Methods

		public abstract PartDesignBase GetNewDesignPart();

		protected static UIElement GetVisual2D(string text, string description, EditorColors editorColors)
		{
			//TODO:  Use icons with tooltips

			Border retVal = new Border();
			retVal.BorderBrush = new SolidColorBrush(editorColors.PartVisualBorderColor);
			retVal.BorderThickness = new Thickness(1d);
			retVal.CornerRadius = new CornerRadius(3d);
			retVal.Margin = new Thickness(2d);

			TextBlock textblock = new TextBlock();
			textblock.Text = text;
			textblock.ToolTip = description;
			textblock.Foreground = new SolidColorBrush(editorColors.PartVisualTextColor);
			textblock.FontSize = 10d;
			textblock.Margin = new Thickness(3d, 1d, 3d, 1d);
			textblock.HorizontalAlignment = HorizontalAlignment.Center;
			textblock.VerticalAlignment = VerticalAlignment.Center;

			retVal.Child = textblock;

			return retVal;
		}

		#endregion
	}

	#endregion
	#region Class: PartDesignBase

	/// <summary>
	/// This is used to represent the part on the 3D surface
	/// </summary>
	/// <remarks>
	/// TODO: Figure out how to handle compound parts
	/// TODO: Figure out how to serialize to xaml
	/// 
	/// There should be a generic decorator class used for any 3D object:  Manipulate position/rotation in model coords, with
	/// an optional ability to manipulate scale.  More options when they hold in tab.  It will work with this class
	/// </remarks>
	public abstract class PartDesignBase
	{
		#region Class: MaterialColorProps

		public class MaterialColorProps
		{
			public MaterialColorProps(DiffuseMaterial diffuse, Brush brush, Color color)
			{
				this.Diffuse = diffuse;
				this.OrigBrush = brush;		//	this constructor explicitely takes brush, because it could be something other than a solid color brush
				this.OrigColor = color;
			}
			public MaterialColorProps(SpecularMaterial specular)
			{
				this.Specular = specular;

				SolidColorBrush brush = specular.Brush as SolidColorBrush;
				if (brush == null)
				{
					throw new ApplicationException("The specular was expected to be set up with a solid color brush.  Expand this method");
				}

				this.OrigBrush = brush;
				this.OrigColor = brush.Color;
				this.OrigSpecular = specular.SpecularPower;
			}
			public MaterialColorProps(EmissiveMaterial emissive)
			{
				this.Emissive = emissive;

				SolidColorBrush brush = emissive.Brush as SolidColorBrush;
				if (brush == null)
				{
					throw new ApplicationException("The emissive was expected to be set up with a solid color brush.  Expand this method");
				}

				this.OrigBrush = brush;
				this.OrigColor = brush.Color;
			}

			public Brush OrigBrush
			{
				get;
				private set;
			}
			public Color OrigColor
			{
				get;
				private set;
			}

			public double OrigSpecular
			{
				get;
				private set;
			}

			public DiffuseMaterial Diffuse
			{
				get;
				private set;
			}
			public SpecularMaterial Specular
			{
				get;
				private set;
			}
			public EmissiveMaterial Emissive
			{
				get;
				private set;
			}
		}

		#endregion

		#region Events

		public event EventHandler TransformChanged = null;

		#endregion

		#region Declaration Section

		protected ScaleTransform3D _scaleTransform = new ScaleTransform3D(1d, 1d, 1d);
		protected TranslateTransform3D _translateTransform = new TranslateTransform3D(0d, 0d, 0d);
		protected QuaternionRotation3D _rotateTransform = null;//new QuaternionRotation3D(new Quaternion(new Vector3D(1d, 0d, 0d), 0d));		//	need to use the value from the options class

		#endregion

		#region Constructor

		public PartDesignBase(EditorOptions options)
		{
			this.Options = options;

			_rotateTransform = new QuaternionRotation3D(options.DefaultOrientation);

			this.SelectionEmissives = new List<EmissiveMaterial>();
			this.MaterialBrushes = new List<MaterialColorProps>();

			this.Token = TokenGenerator.Instance.NextToken();
		}

		#endregion

		#region Public Properties

		public abstract PartDesignAllowedScale AllowedScale
		{
			get;
		}
		public abstract PartDesignAllowedRotation AllowedRotation
		{
			get;
		}

		//	I don't just take in transform, because some parts may have special requirements on scale.  The implementing class
		//	will be responsible for building the transform
		public virtual Vector3D Scale
		{
			get
			{
				return new Vector3D(_scaleTransform.ScaleX, _scaleTransform.ScaleY, _scaleTransform.ScaleZ);
			}
			set
			{
				_scaleTransform.ScaleX = value.X;
				_scaleTransform.ScaleY = value.Y;
				_scaleTransform.ScaleZ = value.Z;

				OnTransformChanged();
			}
		}
		/// <summary>
		/// This is in model coords
		/// </summary>
		public virtual Point3D Position
		{
			get
			{
				return new Point3D(_translateTransform.OffsetX, _translateTransform.OffsetY, _translateTransform.OffsetZ);
			}
			set
			{
				_translateTransform.OffsetX = value.X;
				_translateTransform.OffsetY = value.Y;
				_translateTransform.OffsetZ = value.Z;

				OnTransformChanged();
			}
		}
		/// <summary>
		/// This is in model coords
		/// </summary>
		public virtual Quaternion Orientation
		{
			get
			{
				return _rotateTransform.Quaternion;
			}
			set
			{
				_rotateTransform.Quaternion = value;

				OnTransformChanged();
			}
		}

		/// <summary>
		/// This is the visual
		/// </summary>
		/// <remarks>
		/// This will likely either be a GeometryModel3D or Model3DGroup
		/// 
		/// I didn't make this model visual, because a single model visual could be made of all the different parts of a ship (assuming
		/// it's a rigid body)
		/// </remarks>
		public abstract Model3D Model
		{
			get;
		}

		private string _partType = null;
		/// <summary>
		/// This is useful for matching this to the corresponding tool item and final parts.  This gets stored in the dna class
		/// </summary>
		public string PartType
		{
			get
			{
				if (_partType == null)
				{
					Match match = Regex.Match(this.GetType().ToString(), @"\.(?<name>[^\.]+)Design$");		//	this assumes that each derived class ends with the word Design
					if (match.Success)
					{
						_partType = match.Groups["name"].Value;
					}
					else
					{
						throw new ApplicationException("All classes that derive from this are expected to end with the word Design (and share the same name with the tool item and final classes)");
					}
				}

				return _partType;
			}
		}

		private bool _isSelected = false;
		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (_isSelected == value)
				{
					//	No change
					return;
				}

				_isSelected = value;

				//	Change the apperance
				SetEditorBrushes();
			}
		}

		private bool _isLocked = false;
		public bool IsLocked
		{
			get
			{
				return _isLocked;
			}
			set
			{
				if (_isLocked == value)
				{
					//	No change
					return;
				}

				_isLocked = value;

				//	Change the apperance
				SetEditorBrushes();
			}
		}

		private bool _isActiveLayer = true;
		public bool IsActiveLayer
		{
			get
			{
				return _isActiveLayer;
			}
			set
			{
				if (_isActiveLayer == value)
				{
					//	No change
					return;
				}

				_isActiveLayer = value;

				//	Change the apperance
				SetEditorBrushes();
			}
		}

		/// <summary>
		/// If this part is unique, that means it can't be changed beyond position/rotation.  (it represents a salvaged part)
		/// </summary>
		public bool IsUnique
		{
			get;
			set;
		}

		/// <summary>
		/// These will be transparent until the user hovers/selects the object (the derived class just needs to embed these into
		/// the geometry.  This base class will color them based on user interaction
		/// </summary>
		protected List<EmissiveMaterial> SelectionEmissives
		{
			get;
			private set;
		}
		protected List<MaterialColorProps> MaterialBrushes
		{
			get;
			private set;
		}

		protected EditorOptions Options
		{
			get;
			private set;
		}

		public long Token
		{
			get;
			set;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This is the model that will be seen in the world (not the editor)
		/// </summary>
		/// <remarks>
		/// This will be mostly the same as the editor model, but shouldn't have debug visuals, and will probably
		/// have a lower triangle count
		/// </remarks>
		public abstract Model3D GetFinalModel();

		/// <summary>
		/// This is what gets handed to the physics body
		/// </summary>
		public virtual CollisionHull CreateCollisionHull(WorldBase world)
		{
			throw new ApplicationException("make this abstract");
		}

		/// <summary>
		/// This lets you change several things at once
		/// </summary>
		public void SetTransform(Vector3D? scale, Point3D? position, Quaternion? orientation)
		{
			if (scale != null)
			{
				_scaleTransform.ScaleX = scale.Value.X;
				_scaleTransform.ScaleY = scale.Value.Y;
				_scaleTransform.ScaleZ = scale.Value.Z;
			}

			if (position != null)
			{
				_translateTransform.OffsetX = position.Value.X;
				_translateTransform.OffsetY = position.Value.Y;
				_translateTransform.OffsetZ = position.Value.Z;
			}

			if (orientation != null)
			{
				_rotateTransform.Quaternion = orientation.Value;
			}

			OnTransformChanged();
		}

		/// <summary>
		/// NOTE: If a derived class has custom props, then you must override this method and return your own derived dna.  Don't call
		/// base.GetDNA, but instead call base.FillDNA, which will fill out the properties that this class knows about
		/// </summary>
		public virtual PartDNA GetDNA()
		{
			PartDNA retVal = new PartDNA();
			FillDNA(retVal);
			return retVal;
		}
		/// <summary>
		/// This loads this class up with the properties in the dna
		/// </summary>
		/// <remarks>
		/// NOTE: If a derived class has custom props, then you must override this method and store your own derived dna.  Don't call
		/// base.SetDNA, but instead call base.StoreDNA, which will store the properties that this class knows about
		/// </remarks>
		public virtual void SetDNA(PartDNA dna)
		{
			if (this.PartType != dna.PartType)
			{
				throw new ArgumentException(string.Format("The dna passed in is not for this class.  DNA={0}, this={1}", dna.PartType, this.PartType));
			}

			StoreDNA(dna);
		}

		#endregion
		#region Protected Methods

		protected virtual void OnTransformChanged()
		{
			if (this.TransformChanged != null)
			{
				this.TransformChanged(this, new EventArgs());
			}
		}

		/// <summary>
		/// This will populate the dna class with the values from this base class.  You should only bother to override this method if your 
		/// inheritance will go 3+ deep, and each shell can fill up what it knows about
		/// </summary>
		protected virtual void FillDNA(PartDNA dna)
		{
			dna.PartType = this.PartType;
			dna.Scale = this.Scale;
			dna.Position = this.Position;
			dna.Orientation = this.Orientation;

			#region OLD

			//string typeName = this.GetType().ToString();

			//Match match = Regex.Match(typeName, @"\.(?<name>[^\.]+)Design$");		//	this assumes that each derived class ends with the word Design
			//if (match.Success)
			//{
			//    dna.PartType = match.Groups["name"].Value;
			//}
			//else
			//{
			//    int index = typeName.LastIndexOf('.');
			//    if (index < 0)
			//    {
			//        dna.PartType = typeName;
			//    }
			//    else
			//    {
			//        dna.PartType = typeName.Substring(index + 1);		//	class names can't end in dot, so no need to check for that
			//    }
			//}

			#endregion
		}
		protected virtual void StoreDNA(PartDNA dna)
		{
			if (this.PartType != dna.PartType)
			{
				throw new ArgumentException(string.Format("The dna passed in is not for this class.  DNA={0}, this={1}", dna.PartType, this.PartType));
			}

			this.Scale = dna.Scale;
			this.Position = dna.Position;
			this.Orientation = dna.Orientation;
		}

		#endregion

		#region Private Methods

		private void SetEditorBrushes()
		{
			const byte INACTIVECOLOR = 80;

			#region Selection Emissives

			Brush selectionEmissives = Brushes.Transparent;

			if (_isActiveLayer)
			{
				if (_isSelected)
				{
					if (_isLocked)
					{
						selectionEmissives = this.Options.EditorColors.SelectedLockedEmissiveBrush;
					}
					else
					{
						selectionEmissives = this.Options.EditorColors.SelectedEmissiveBrush;
					}
				}
			}

			foreach (EmissiveMaterial emissive in this.SelectionEmissives)
			{
				emissive.Brush = selectionEmissives;
			}

			#endregion

			#region Layer Color

			foreach (MaterialColorProps material in this.MaterialBrushes)
			{
				if (_isActiveLayer)
				{
					#region Active

					if (material.Diffuse != null)
					{
						material.Diffuse.Brush = material.OrigBrush;
					}
					else if (material.Specular != null)
					{
						material.Specular.Brush = material.OrigBrush;
						material.Specular.SpecularPower = material.OrigSpecular;
					}
					else if (material.Emissive != null)
					{
						material.Emissive.Brush = material.OrigBrush;
					}
					else
					{
						throw new ApplicationException("Unknown material type");
					}

					#endregion
				}
				else
				{
					#region Inactive

					Color grayColor = UtilityWPF.AlphaBlend(material.OrigColor, Color.FromArgb(material.OrigColor.A, INACTIVECOLOR, INACTIVECOLOR, INACTIVECOLOR), .1d);
					SolidColorBrush grayBrush = new SolidColorBrush(grayColor);

					if (material.Diffuse != null)
					{
						material.Diffuse.Brush = grayBrush;
					}
					else if (material.Specular != null)
					{
						material.Specular.Brush = grayBrush;
						material.Specular.SpecularPower = 20;
					}
					else if (material.Emissive != null)
					{
						material.Emissive.Brush = grayBrush;
					}
					else
					{
						throw new ApplicationException("Unknown material type");
					}

					#endregion
				}
			}

			#endregion
		}

		#endregion
	}

	#endregion
	#region Class: PartBase

	public abstract class PartBase
	{
		#region Events

		public event EventHandler MassChanged = null;

		#endregion

		#region Constructor

		/// <remarks>
		/// How these classes get instantiated and shared is a bit odd.  Not sure if this is the best way or not.  There are two+ different
		/// paths these parts are used:
		/// 
		/// 	Editor:
		/// 		PartToolItemBase -> PartDesignBase -> PartDNA
		/// 
		/// 	World:
		/// 		PartDNA -> PartBase -> (which internally creates PartDesignBase)
		/// </remarks>
		public PartBase(EditorOptions options, PartDNA dna)
		{
			if (dna.PartType != this.PartType)
			{
				throw new ArgumentException(string.Format("The dna passed in is not meant for this class.  DNA: \"{0}\", this: \"{1}\"", dna.PartType, this.PartType));
			}

			this.Options = options;
			this.DNA = dna;
			this.Token = TokenGenerator.Instance.NextToken();
		}

		#endregion

		#region Public Properties

		//TODO: Expose Scale/Pos/Rot (probably as public get, protected set)

		/// <summary>
		/// This is how much mass this addon has when empty
		/// </summary>
		public abstract double DryMass
		{
			get;
		}
		/// <summary>
		/// This is how much mass this addon has total (DryMass + the mass of its contents)
		/// </summary>
		public abstract double TotalMass
		{
			get;
		}

		/// <summary>
		/// This is in model coords
		/// </summary>
		public abstract Point3D Position
		{
			get;
		}
		/// <summary>
		/// This is in model coords
		/// </summary>
		public abstract Quaternion Orientation
		{
			get;
		}

		/// <summary>
		/// This is the visual
		/// </summary>
		/// <remarks>
		/// This will likely either be a GeometryModel3D or Model3DGroup
		/// 
		/// I didn't make this model visual, because a single model visual could be made of all the different parts of a ship (assuming
		/// it's a rigid body)
		/// </remarks>
		public abstract Model3D Model
		{
			get;
		}

		private string _partType = null;
		/// <summary>
		/// This is useful for matching this to the corresponding tool item and design parts.  This gets stored in the dna class
		/// </summary>
		public string PartType
		{
			get
			{
				if (_partType == null)
				{
					Match match = Regex.Match(this.GetType().ToString(), @"\.(?<name>[^\.]+)$");		//	this assumes that each derived class is the same name as the toolitem and design, but no suffix
					if (match.Success)
					{
						_partType = match.Groups["name"].Value;
					}
					else
					{
						throw new ApplicationException("This class name should never end with a dot");
					}
				}

				return _partType;
			}
		}

		protected PartDNA DNA
		{
			get;
			private set;
		}

		public EditorOptions Options
		{
			get;
			private set;
		}

		public long Token
		{
			get;
			private set;
		}

		#endregion

		#region Public Methods

		public virtual CollisionHull CreateCollisionHull(WorldBase world)
		{
			throw new ApplicationException("make this abstract");
		}

		#endregion
		#region Protected Methods

		protected virtual void OnMassChanged()
		{
			if (this.MassChanged != null)
			{
				this.MassChanged(this, new EventArgs());
			}
		}

		#endregion
	}

	#endregion

	#region Class: PartDNA

	/// <summary>
	/// This holds properties of a part
	/// </summary>
	/// <remarks>
	/// This class is meant to stay simple, and be able to be serialized to xaml and json, which means no custom constructors,
	/// just public properties
	/// 
	/// Also, for xaml serialization, don't use 3.5's XamlWriter.Save and XamlReader.Load, they suck.
	/// Instead use 4.0's XamlServices.Load/Save in system.xaml.dll
	/// </remarks>
	public class PartDNA
	{
		/// <summary>
		/// This is what type of part this is for
		/// </summary>
		/// <remarks>
		/// I don't want to make this class abstract so that it can be determined by the derived class type.  I don't want to make
		/// an enum, because that wouldn't allow growth.  So I'm going with a string, and will populated with a portion of the
		/// design base's .GetType.ToString
		/// </remarks>
		public string PartType
		{
			get;
			set;
		}

		/// <summary>
		/// Try to avoid making derived class that store custom properties that could just be inferred from scale (like mass, volume, energy draw, etc)
		/// </summary>
		public Vector3D Scale
		{
			get;
			set;
		}
		/// <summary>
		/// This is in model coords
		/// </summary>
		public Point3D Position
		{
			get;
			set;
		}
		/// <summary>
		/// This is in model coords
		/// </summary>
		public Quaternion Orientation
		{
			get;
			set;
		}
	}

	#endregion

	#region Enum: PartDesignAllowedScale

	public enum PartDesignAllowedScale
	{
		/// <summary>
		/// Each of the 3 axis can be scaled independently
		/// </summary>
		X_Y_Z,
		/// <summary>
		/// All 3 axiis are tied together (there is a single scale value)
		/// </summary>
		XYZ,
		/// <summary>
		/// XY are tied together, Z is independent
		/// </summary>
		XY_Z
	}

	#endregion
	#region Enum: PartDesignAllowedRotation

	public enum PartDesignAllowedRotation
	{
		/// <summary>
		/// No rotations (this is useful for spheres)
		/// </summary>
		None,
		/// <summary>
		/// No rotation around the Z (this is useful for cylinders)
		/// </summary>
		/// <remarks>
		/// Actually, this is too restrictive (even for cylinders).  Say you rotate down 45 degrees.  Then want to rotate down 30
		/// degrees along a different plane, you need to rotate around z to get a ring to line up with that plane. - a lot of words,
		/// but it's frustrating in certain cases without that z ring
		/// </remarks>
		X_Y,
		/// <summary>
		/// All three axiis allow rotation
		/// </summary>
		X_Y_Z
	}

	#endregion
	#region Enum: ShipPartType

	/// <summary>
	/// This will likely never be used as an enum, just use the derived part classes.  This is just a place to organize some thoughts
	/// </summary>
	internal enum ShipPartType
	{
		//	Structures
		Hull,
		ArmorShard,
		Spar,
		Wing,

		//	Containers
		CargoBay,
		FuelTank,
		EnergyTank,
		AmmoBox,
		BrainContainer,		//	this won't be the brain itself, but will place limits on size, energy draw, processing time, etc

		//	A cargo bay is required to have at least one tube.  Nothing wider than the tube can go through it, and the tube can't be wider than a cargo bay
		//	Cargo bays are cubes, but these tubes will be cylinders (make the transfer instant so that cargo won't be stored in a tube)
		Tube_Cargo_Cargo,		//	used to transfer from one cargo bay to another
		Tube_Cargo_Mouth,		//	used to connect a cargo bay to the outside world
		Tube_Cargo_Transfer,		//	this lets the bot transfer cargo to other bots, but one end must be tied to a cargo bay

		//	These let the bot share/take energy and fuel with other bots.  Internal flow of energy/fuel isn't modeled, it's just assumed to go
		//	through wires/tubes.  But to share with the outside, two of the same type of transfer tubes need to be joined, and the transfer needs
		//	to be authorized.
		//
		//	These will be a bit complex to implement, but will allow for much richer social interaction, and bot specialization
		Transfer_Energy,
		Transfer_Fuel,

		//	Converters
		Converter_MatterToFuel,		//	this will have a cargo section, and output fuel (it won't actively feed on an existing cargo bay, small amounts of cargo need to be loaded into it).  Also, this may require exact materials (1 part graphite, 1 part water, x energy)
		Converter_MatterToEnergy,		//	this can take any matter, the denser the better
		Converter_EnergyToFuel,		//	this will connect between an existing energy tank and fuel tank
		Converter_EnergyToAmmo,
		Converter_RadiationToEnergy,		//	this is a solar cell

		//	Equipment
		Thruster,
		TractorBeam,
		GrappleGun,
		ProjectileGun,		//	fires projectiles (could be simple slugs, or missles, or drones).  It will need an ammo tank that only it can use
		BeamGun,		//	probably keep this short range, more like a laser sword, or flame thrower
		Spike,		//	good for ramming
		VibroSpike,		//	very good for ramming, consumes small amounts of energy
		EnergyShield,		//	blocks radiation
		KineticShield,		//	blocks impacts
		TractorShield,		//	won't let tractor beams lock on

		//	Sensors
		Camera,
		RangeFinder,		//	a camera paints a bitmap with reflected color.  This paints a bitmap with reflected distance
		Ears,
		RadiationDetector,		//	how much ambient radiation there is (geiger counter)
		GeneralForceSensor,		//	outputs the sum of all forces felt (gravity, tractor, fluid, impacts, thruster, etc)
		GravityForceSensor,		//	outputs how much gravity force is felt - this is like the inner ear
		TractorForceSensor,		//	outputs how much tractor force is felt (from others, not from the ship's own tractors)
		FluidForceSensor,		//	outputs how much fluid force is felt (I think this should be separate from 
		ReactionForceSensor,		//	outputs how much force is felt from the various ship's parts (thrusters, tractors, grapple, gun recoil, etc)

		//	Multi World
		//	Each world runs independent of the others (with its own clock).  These worlds may be on the same machine, or spread across machines
		//	in a network.
		//
		//	In order for cross world jumping to work, these worlds need to be thought of as bubbles along either a line / a plane / a cube.
		//
		//	When jumping from one world to another, the 3D coords stay the same, you just shift out of one world, and into another.  If the worlds
		//	are different sized, the coords need to be scaled so each world has a size of one.
		//
		//	While shifting, you are ghosted in both.  Damage dealt/recieved will be a percent of how much you are in that particular world
		//
		//	A bigger engine will let you make the shift faster.
		//
		//	Some advantages to shifting
		//		Jump to a world with less harsh conditions / fewer predators / easier prey
		//		Hop to a world, hitch a ride on an asteroid, hop back when you are where you wanted to go
		//		If you are in a large world, hop to a small one, travel a small distance and hop back
		//		If there is an obsticle in your current world, hop to a different one, fly past where the obsticle would have been, hop back
		//		Ambush others that don't have cross dimension abilities
		CrossDimensionCamera,		//	this can see parallel worlds
		CrossDimensionRangeFinder,
		CrossDimensionEngine,		//	this can jump to parallel worlds
		CrossDimensionShield,		//	this won't allow cross dimension engines to work within a certain radius

		//	Joints
		//TODO: Come up with some joints:
		//		Some passive with degrees of freedom, a desired rest position, and springs to return to that position - these would be used as shock absorbers
		//		Some active that will have lock limits, and the desired rest position can be controlled - these would be used as servos/muscles
	}

	#endregion
}
