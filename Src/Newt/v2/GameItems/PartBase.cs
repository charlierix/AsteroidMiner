using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xaml;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems
{
    //TODO: Add a constraint that cargo bays need tubes between them, and a tube exposed to space (or a hatch if connected to the hull)
    //TODO: PartBase needs to support hit points
    //TODO: PartToolItemBase and PartDesignBase need to support single instance (with dna).  This will represent salvaged parts

    #region class: PartToolItemBase

    /// <summary>
    /// This is used to represent the tool in the toolbox
    /// </summary>
    public abstract class PartToolItemBase
    {
        #region Declaration Section

        //TODO: The other part types don't allow intersection, but structural can (if a bar goes through a container, it is assumed that
        //it's actually 2 bars that are welded to the outside of the container (no bar running through the inside)

        public const string CATEGORY_CONTAINER = "Containers";		// Cargo, Fuel, Energy Tank, Ammo
        public const string CATEGORY_PROPULSION = "Propulsion";		// Thruster, Tractor Beam, Grapple Gun/Spider Web
        public const string CATEGORY_SENSOR = "Sensors";		// Brain, Eyes (camera), Eyes (range), Fluid Flow, Gravity
        public const string CATEGORY_WEAPON = "Weapons";		// Projectile Gun, Energy Gun, Spike
        public const string CATEGORY_CONVERTERS = "Converters";		// Radiation-Energy, Cargo-Energy, Cargo-Fuel
        public const string CATEGORY_BRAIN = "Brain";		// this is for brains and other ai parts
        public const string CATEGORY_EQUIPMENT = "Equipment";		// this is sort of a misc category
        public const string CATEGORY_SHIELD = "Shields";		// this is sort of a misc category
        public const string CATEGORY_STRUCTURAL = "Structural";		// Hulls, Armor Shards, Spars, Wings
        public const string CATEGORY_GROUPS = "Groups";
        public const string CATEGORY_VISUALS = "Visuals";

        public const string TAB_SHIPPART = "Parts";		// parts won't allow intersection with other parts
        public const string TAB_SHIPSTRUCTURE = "Structural";		// structural will allow intersection
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
                    Match match = Regex.Match(this.GetType().ToString(), @"\.(?<name>[^\.]+)ToolItem$");		// this assumes that each derived class ends with the word Design
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

        // If the unique part is set, then only this one part is allowed to exist.  Also, this part can't be modified beyond changing
        // position/rotation
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

        protected static UIElement GetVisual2D(string text, string description, EditorOptions options, PartToolItemBase partBase)
        {
            Brush standardBack = new SolidColorBrush(options.EditorColors.PartVisual_BackgroundColor);
            Brush hoverBack = new SolidColorBrush(options.EditorColors.PartVisual_BackgroundColor_Hover);

            // Border
            Border retVal = new Border()
            {
                Background = standardBack,       // this at least needs to be transparent for drag/drop to work
                BorderBrush = new SolidColorBrush(options.EditorColors.PartVisual_BorderColor),
                BorderThickness = new Thickness(1d),
                CornerRadius = new CornerRadius(7d),
                Margin = new Thickness(3d),
            };

            retVal.MouseEnter += (s, e) => { retVal.Background = hoverBack; };
            retVal.MouseLeave += (s, e) => { retVal.Background = standardBack; };

            // Grid
            Grid grid = new Grid()
            {
                Margin = new Thickness(2, 1, 6, 1),
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            retVal.Child = grid;

            // Icon
            Icon3D icon = new Icon3D(partBase.GetNewDesignPart().GetDNA(), options)
            {
                ShowName = false,
                ShowBorder = false,
                Width = 50,
                Height = 50,
                Margin = new Thickness(4),
                AutoRotateOnMouseHover = true,
                AutoRotateParent = retVal,
            };

            Grid.SetColumn(icon, 0);

            grid.Children.Add(icon);

            // Text
            TextBlock textblock = new TextBlock()
            {
                Text = text,
                ToolTip = description,
                Foreground = new SolidColorBrush(options.EditorColors.PartVisual_TextColor),
                FontSize = 11d,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };

            Grid.SetColumn(textblock, 2);

            grid.Children.Add(textblock);

            return retVal;
        }

        #endregion
    }

    #endregion
    #region class: PartDesignBase

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
        #region class: MaterialColorProps

        public class MaterialColorProps
        {
            //public MaterialColorProps(DiffuseMaterial diffuse, Brush brush, Color color)
            //{
            //    this.Diffuse = diffuse;
            //    this.OrigBrush = brush;		// this constructor explicitely takes brush, because it could be something other than a solid color brush
            //    this.OrigColor = color;
            //}
            public MaterialColorProps(DiffuseMaterial diffuse, Color color)
            {
                this.Diffuse = diffuse;
                this.OrigBrush = diffuse.Brush;
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

        #region Constructor

        public PartDesignBase(EditorOptions options, bool isFinalModel)
        {
            this.Options = options;
            this.IsFinalModel = isFinalModel;

            // _orientation is threadsafe, _rotateTransform isn't
            _orientation = new Quaternion(options.DefaultOrientation.X, options.DefaultOrientation.Y, options.DefaultOrientation.Z, options.DefaultOrientation.W);
            //_rotateTransform = new QuaternionRotation3D(_orientation);

            this.SelectionEmissives = new List<EmissiveMaterial>();
            this.MaterialBrushes = new List<MaterialColorProps>();

            this.Token = TokenGenerator.NextToken();
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

        // I don't just take in transform, because some parts may have special requirements on scale.  The implementing class
        // will be responsible for building the transform
        private Vector3D _scale = new Vector3D(1, 1, 1);
        public virtual Vector3D Scale
        {
            get
            {
                //return new Vector3D(ScaleTransform.ScaleX, ScaleTransform.ScaleY, ScaleTransform.ScaleZ);
                return _scale;		// ScaleTransform isn't threadsafe
            }
            set
            {
                _scale = new Vector3D(value.X, value.Y, value.Z);		// saving this second copy so that when a request to build dna comes in, it's thread safe

                if (_scaleTransform != null)
                {
                    //NOTE: This will bomb if _scale is set from a different thread than _scaleTransform
                    _scaleTransform.ScaleX = value.X;
                    _scaleTransform.ScaleY = value.Y;
                    _scaleTransform.ScaleZ = value.Z;
                }

                OnTransformChanged();
            }
        }

        private Point3D _position = new Point3D(0, 0, 0);
        /// <summary>
        /// This is in model coords
        /// </summary>
        public virtual Point3D Position
        {
            get
            {
                //return new Point3D(TranslateTransform.OffsetX, TranslateTransform.OffsetY, TranslateTransform.OffsetZ);
                return _position;		// transform isn't threadsafe
            }
            set
            {
                _position = new Point3D(value.X, value.Y, value.Z);		// storing this second copy to be threadsafe

                if (_translateTransform != null)
                {
                    //NOTE: This will bomb if _position is set from a different thread than _translateTransform
                    _translateTransform.OffsetX = value.X;
                    _translateTransform.OffsetY = value.Y;
                    _translateTransform.OffsetZ = value.Z;
                }

                OnTransformChanged();
            }
        }

        private Quaternion _orientation;		// this is set in the constructor
        /// <summary>
        /// This is in model coords
        /// </summary>
        public virtual Quaternion Orientation
        {
            get
            {
                //return RotateTransform.Quaternion;
                return _orientation;
            }
            set
            {
                _orientation = new Quaternion(value.X, value.Y, value.Z, value.W);

                if (_rotateTransform != null)
                {
                    //NOTE: This will bomb if _orientation is set from a different thread than _rotateTransform
                    _rotateTransform.Quaternion = value;
                }

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
                    Match match = Regex.Match(this.GetType().ToString(), @"\.(?<name>[^\.]+)Design$");		// this assumes that each derived class ends with the word Design
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
                    // No change
                    return;
                }

                _isSelected = value;

                // Change the apperance
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
                    // No change
                    return;
                }

                _isLocked = value;

                // Change the apperance
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
                    // No change
                    return;
                }

                _isActiveLayer = value;

                // Change the apperance
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
        /// This is the model that will be seen in the world (not the editor)
        /// </summary>
        /// <remarks>
        /// This will be mostly the same as the editor model, but shouldn't have debug visuals, and will probably
        /// have a lower triangle count
        /// </remarks>
        public readonly bool IsFinalModel;

        public long Token
        {
            get;
            set;
        }

        #endregion
        #region Protected Properties

        private ScaleTransform3D _scaleTransform = null;
        /// <summary>
        /// NOTE: This can only be used by the GUI thread
        /// </summary>
        protected ScaleTransform3D ScaleTransform
        {
            get
            {
                if (_scaleTransform == null)
                {
                    _scaleTransform = new ScaleTransform3D(_scale);
                }

                return _scaleTransform;
            }
        }

        private TranslateTransform3D _translateTransform = null;
        /// <summary>
        /// NOTE: This can only be used by the GUI thread
        /// </summary>
        protected TranslateTransform3D TranslateTransform
        {
            get
            {
                if (_translateTransform == null)
                {
                    _translateTransform = new TranslateTransform3D(_position.ToVector());
                }

                return _translateTransform;
            }
        }

        private QuaternionRotation3D _rotateTransform = null;
        /// <summary>
        /// NOTE: This can only be used by the GUI thread
        /// </summary>
        protected QuaternionRotation3D RotateTransform
        {
            get
            {
                if (_rotateTransform == null)
                {
                    _rotateTransform = new QuaternionRotation3D(_orientation);
                }

                return _rotateTransform;
            }
        }

        // These will be transparent until the user hovers/selects the object (the derived class just needs to embed these into
        // the geometry.  This base class will color them based on user interaction
        protected List<EmissiveMaterial> SelectionEmissives
        {
            get;
            private set;
        }
        protected internal List<MaterialColorProps> MaterialBrushes
        {
            get;
            private set;
        }

        protected EditorOptions Options
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        //public abstract Model3D GetFinalModel();

        //TODO: Make this abstract
        /// <summary>
        /// This is what gets handed to the physics body
        /// </summary>
        public virtual CollisionHull CreateCollisionHull(WorldBase world)
        {
            throw new ApplicationException("make this abstract");
        }

        //TODO: Make this abstract
        /// <summary>
        /// This is used to calculate the moment of inertia of this part
        /// </summary>
        /// <remarks>
        /// This mass breakdown is thought of as a bunch of solid balls.  The ship will be make rigid bodies of parts, and the moment of inertia of the whole
        /// rigid body will use the parallel axis formula on each part's mass breakdown (for all three axiis)
        /// 
        /// If the part can be thought of as a sphere (uniform scale), then the breakdown can just be one cell
        /// </remarks>
        public virtual UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
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
                _scale = new Vector3D(scale.Value.X, scale.Value.Y, scale.Value.Z);		// this is the threadsafe copy

                _scaleTransform.ScaleX = scale.Value.X;
                _scaleTransform.ScaleY = scale.Value.Y;
                _scaleTransform.ScaleZ = scale.Value.Z;
            }

            if (position != null)
            {
                _position = new Point3D(position.Value.X, position.Value.Y, position.Value.Z);

                _translateTransform.OffsetX = position.Value.X;
                _translateTransform.OffsetY = position.Value.Y;
                _translateTransform.OffsetZ = position.Value.Z;
            }

            if (orientation != null)
            {
                _orientation = new Quaternion(orientation.Value.X, orientation.Value.Y, orientation.Value.Z, orientation.Value.W);

                _rotateTransform.Quaternion = orientation.Value;
            }

            OnTransformChanged();
        }

        /// <summary>
        /// NOTE: A final consumer should call PartBase.GetNewDNA() if you want a fully filled out dna.  PartDesignBase.GetDNA() is
        /// for things like editors.  It won't have things like neural positions, or memory of activity --- only structural
        /// 
        /// NOTE: If a derived class has custom props, then you must override this method and return your own derived dna.  Don't call
        /// base.GetDNA, but instead call base.FillDNA, which will fill out the properties that this class knows about
        /// </summary>
        public virtual ShipPartDNA GetDNA()
        {
            ShipPartDNA retVal = new ShipPartDNA();
            FillDNA(retVal);
            return retVal;
        }
        /// <summary>
        /// This loads this class up with the properties in the dna
        /// NOTE: This will fix errors in DNA.  It changes the object passed in
        /// </summary>
        /// <remarks>
        /// NOTE: If a derived class has custom props, then you must override this method and store your own derived dna.  Don't call
        /// base.SetDNA, but instead call base.StoreDNA, which will store the properties that this class knows about
        /// 
        /// At first, I wasn't modifying dna, but then noticed PartBase constructor stores this.DNA
        /// </remarks>
        public virtual void SetDNA(ShipPartDNA dna)
        {
            if (this.PartType != dna.PartType)
            {
                throw new ArgumentException(string.Format("The dna passed in is not for this class.  DNA={0}, this={1}", dna.PartType, this.PartType));
            }

            StoreDNA(dna);
        }

        /// <summary>
        /// This will request a new tool item class (any design classes that the tool item creates will be different than this)
        /// </summary>
        public abstract PartToolItemBase GetToolItem();

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
        protected virtual void FillDNA(ShipPartDNA dna)
        {
            dna.PartType = this.PartType;
            dna.Scale = this.Scale;		//NOTE: Scale, Position, Orientation used to store their values in transforms, but GetDNA could come from any thread, so I had to store a threadsafe vector as well as a transform.  ugly
            dna.Position = this.Position;
            dna.Orientation = this.Orientation;

            //NOTE: This is done in NeuralUtility.PopulateDNALinks
            //if(this is INeuronContainer)
            //{
            //    dna.Neurons = 
            //    dna.AltNeurons = 
            //    dna.InternalLinks = 
            //    dna.ExternalLinks = 
            //}
        }
        /// <summary>
        /// NOTE: This will fix scale
        /// </summary>
        protected virtual void StoreDNA(ShipPartDNA dna)
        {
            if (this.PartType != dna.PartType)
            {
                throw new ArgumentException(string.Format("The dna passed in is not for this class.  DNA={0}, this={1}", dna.PartType, this.PartType));
            }

            PartDesignAllowedScale allowedScale = this.AllowedScale;
            switch (allowedScale)
            {
                case PartDesignAllowedScale.XYZ:
                    double scale1 = Math1D.Avg(dna.Scale.X, dna.Scale.Y, dna.Scale.Z);
                    dna.Scale = new Vector3D(scale1, scale1, scale1);
                    break;

                case PartDesignAllowedScale.XY_Z:
                    double scale2 = Math1D.Avg(dna.Scale.X, dna.Scale.Y);
                    dna.Scale = new Vector3D(scale2, scale2, dna.Scale.Z);
                    break;

                case PartDesignAllowedScale.None:
                case PartDesignAllowedScale.X_Y_Z:
                    break;

                default:
                    throw new ApplicationException("Unknown PartDesignAllowedScale: " + allowedScale);
            }

            this.Scale = dna.Scale;
            this.Position = dna.Position;
            this.Orientation = dna.Orientation;
        }

        protected Transform3D GetTransformForGeometry(bool isFinal)
        {
            Transform3DGroup retVal = new Transform3DGroup();

            if (isFinal)
            {
                // To avoid threading issues, don't instantiate this.ScaleTransform, RotateTransform, TranslateTransform (it is assumed that parts won't
                // move around for final builds)
                retVal.Children.Add(new ScaleTransform3D(Scale));
                retVal.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Orientation)));
                retVal.Children.Add(new TranslateTransform3D(Position.ToVector()));
            }
            else
            {
                // When it's not final, it's used by the ship editor, and the transform needs to be built out of live transforms
                //NOTE: This method needs to be called from the gui thread
                //NOTE: The manipulation of this.Scale, Orientation, Position will update this.ScaleTransform. RotateTransform, TranslateTransform.  This will bomb if the manipulation is done in the non GUI thread
                retVal.Children.Add(ScaleTransform);
                retVal.Children.Add(new RotateTransform3D(RotateTransform));
                retVal.Children.Add(TranslateTransform);
            }

            return retVal;
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
                        selectionEmissives = this.Options.EditorColors.SelectedLocked_EmissiveBrush;
                    }
                    else
                    {
                        selectionEmissives = this.Options.EditorColors.Selected_EmissiveBrush;
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
    #region class: PartBase

    public abstract class PartBase : IDisposable, ITakesDamage
    {
        #region Events

        public event EventHandler MassChanged = null;

        //NOTE: These events could be called from any thread
        public event EventHandler<PartRequestWorldLocationArgs> RequestWorldLocation = null;
        public event EventHandler<PartRequestWorldSpeedArgs> RequestWorldSpeed = null;
        public event EventHandler<PartRequestParentArgs> RequestParent = null;

        public event EventHandler Destroyed = null;
        public event EventHandler Resurrected = null;

        #endregion

        #region Declaration Section

        private readonly TakesDamageWorker _damageWorker;

        private bool _initialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// WARNING: This will modify the dna's scale (if the scale is invalid)
        /// </summary>
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
        public PartBase(EditorOptions options, ShipPartDNA dna, double hitpointMin, double hitpointSlope, TakesDamageWorker damageWorker)
        {
            if (dna.PartType != this.PartType)
            {
                throw new ArgumentException(string.Format("The dna passed in is not meant for this class.  DNA: \"{0}\", this: \"{1}\"", dna.PartType, this.PartType));
            }

            this.Options = options;
            this.DNA = dna;
            this.Token = TokenGenerator.NextToken();

            // Can't use this.ScaleActual yet
            double size = Math1D.Avg(dna.Scale.X, dna.Scale.Y, dna.Scale.Z);
            this.HitPoints_Max = hitpointMin + (size * hitpointSlope);

            if (dna.PercentDamaged <= 0)
            {
                this.HitPoints_Current = this.HitPoints_Max;
            }
            else if (dna.PercentDamaged >= 1)
            {
                this.HitPoints_Current = 0;
            }
            else
            {
                this.HitPoints_Current = UtilityCore.GetScaledValue(0, this.HitPoints_Max, 0, 1, 1 - dna.PercentDamaged);
            }

            _damageWorker = damageWorker;

            _initialized = true;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            //if (disposing)
            //{
            //}
        }

        #endregion
        #region ITakesDamage

        private readonly object _hitPointLock = new object();

        public bool IsDestroyed
        {
            get
            {
                double hitPoints = this.HitPoints_Current;
                return hitPoints < 0 || hitPoints.IsNearZero();
            }
        }

        private double _hitPoints_Current;
        public double HitPoints_Current
        {
            get
            {
                lock (_hitPointLock)
                {
                    return _hitPoints_Current;
                }
            }
            protected set
            {
                bool wasDetroyed, isDestroyed;

                lock (_hitPointLock)
                {
                    wasDetroyed = _hitPoints_Current < 0 || _hitPoints_Current.IsNearZero();        //NOTE: Can't call IsDestroyed, because it call this get, which has a lock
                    isDestroyed = value < 0 || value.IsNearZero();

                    _hitPoints_Current = value;
                }

                // Don't want to call these inside a lock
                if (!wasDetroyed && isDestroyed)
                {
                    OnDestroyed();
                }
                else if (wasDetroyed && !isDestroyed)
                {
                    OnResurrected();
                }
            }
        }

        public double HitPoints_Max
        {
            get;
            private set;
        }

        public void AdjustHitpoints(double delta)
        {
            bool wasDetroyed, isDestroyed;

            lock (_hitPointLock)
            {
                double newValue = _hitPoints_Current + delta;
                if (newValue < 0)
                {
                    newValue = 0;
                }
                else if (newValue > this.HitPoints_Max)
                {
                    newValue = this.HitPoints_Max;
                }

                wasDetroyed = _hitPoints_Current < 0 || _hitPoints_Current.IsNearZero();        //NOTE: Can't call IsDestroyed, because it call this get, which has a lock
                isDestroyed = newValue.IsNearZero();

                _hitPoints_Current = newValue;
            }

            // Don't want to call these inside a lock
            if (!wasDetroyed && isDestroyed)
            {
                OnDestroyed();
            }
            else if (wasDetroyed && !isDestroyed)
            {
                OnResurrected();
            }
        }

        public virtual void TakeDamage_Collision(IMapObject collidedWith, Point3D positionModel, Vector3D velocityModel, double mass1, double mass2)
        {
            if (_damageWorker == null)
            {
                return;
            }

            double damage = _damageWorker.GetDamage_Collision(collidedWith, positionModel, velocityModel, mass1, mass2);

            AdjustHitpoints(-damage);
        }
        public virtual void TakeDamage_Energy(double amount, Point3D positionModel)
        {
        }
        public virtual void TakeDamage_Heat(double amount, Point3D positionModel)
        {
        }

        #endregion

        #region Public Properties

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
        /// WARNING: This is currently only used by PartSeparator.  The old logic was clearing the model, but I think that was flawed.  The editor plays
        /// with the design class's live transforms.  I think it's best to create all new parts
        /// </summary>
        public virtual Point3D Position
        {
            get
            {
                // This is error prone to think that the current position is always the same as dna position
                //return this.DNA.Position;

                //NOTE: It's expected that the derived part has set up the design class
                return this.Design.Position;
            }
            set
            {
                this.Design.Position = value;
                //_model = null;		// if they moved the part, then the next time Model is requested, it will need to be redrawn
            }
        }
        /// <summary>
        /// This is in model coords
        /// WARNING: If you set orientation, think of this part as burned.  It's best to create a new part for the final world worthy body
        /// </summary>
        public virtual Quaternion Orientation
        {
            get
            {
                // This is error prone to think that the current orientation is always the same as dna orientation
                //return this.DNA.Orientation;

                return this.Design.Orientation;
            }
            set
            {
                this.Design.Orientation = value;
                //_model = null;		// if they moved the part, then the next time Model is requested, it will need to be redrawn
            }
        }

        /// <remarks>
        /// Originally, I intended the parts to use up all of scale, but as I made parts, some would be smaller, or thinner, etc.
        /// So this holds the sizes that are actually used (will always be smaller or equal to dna.Scale)
        /// 
        /// Some of the parts are actually shapes other than cubes, but this just gives a rough approximation of size
        /// </remarks>
        public abstract Vector3D ScaleActual
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
        public virtual Model3D Model
        {
            get
            {
                if (!this.Design.IsFinalModel)
                {
                    throw new ApplicationException("The design's IsFinalModel must be true for a final (world viewed) part");
                }

                return this.Design.Model;
            }
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
                    Match match = Regex.Match(this.GetType().ToString(), @"\.(?<name>[^\.]+)$");		// this assumes that each derived class is the same name as the toolitem and design, but no suffix
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

        protected ShipPartDNA DNA
        {
            get;
            private set;
        }
        protected PartDesignBase Design
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public virtual CollisionHull CreateCollisionHull(WorldBase world)
        {
            return this.Design.CreateCollisionHull(world);
        }
        public virtual UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return this.Design.GetMassBreakdown(cellSize);
        }

        /// <summary>
        /// This creates dna that can be stored/mutated/etc
        /// </summary>
        /// <remarks>
        /// It's cumbersome to have both the design class and partbase have some of the same properties, and shared responsibility of
        /// filling out the dna class.  This came about because of the need for the editor to have a certain amount of functionality, and the
        /// real world parts to have slightly more functionality.  But it's certainly a good candidate for rework
        /// </remarks>
        public virtual ShipPartDNA GetNewDNA()
        {
            // Initially, I had this return a clone of this.DNA.  this.DNA should have the same values as the part's current properties, but the design
            // class stores the actual values.  So to remove possible errors, I decided to make this call Design.GetDNA
            ShipPartDNA retVal = this.Design.GetDNA();

            INeuronContainer thisCast = this as INeuronContainer;
            if (thisCast != null)
            {
                //NOTE: So this takes care of most scenarios.  If AltNeurons are used, or other odd scenarios, then override this method (see Brain)
                //NOTE: The design class doesn't hold neurons, since it's only used by the editor, so fill out the rest of the dna here
                retVal.Neurons = thisCast.Neruons_All.
                    Select(o => o.Position).
                    ToArray();

                //NOTE: I decided not to store the links within the part classes themselves.  The parts don't directly use the links, so it would
                //be cumbersome.  Instead, the owner of the parts (the ship) should be responsible for populating the returned dna class with
                //links
            }

            retVal.PercentDamaged = (this.HitPoints_Max - this.HitPoints_Current) / this.HitPoints_Max;

            return retVal;
        }

        /// <summary>
        /// Some parts need to know their location in world coords (sensors).  So this raises an event that needs to be filled out by the owner
        /// of the part.
        /// </summary>
        /// <remarks>
        /// I didn't want the part to have a direct reference to the ship.  There are various test forms that don't use ship.  There could also be
        /// several independent simulations that use a completely different class than ship.  Also, parts could be mounted to movable limbs.
        /// So it's up to the container to know all of that complexity, and just return world coords when asked.
        /// </remarks>
        public (Point3D position, Quaternion rotation) GetWorldLocation()
        {
            if (this.RequestWorldLocation == null)
            {
                //TODO: If this becomes tedious, just return the location in model coords.  I just figured an exception would make it easier to catch plumbing issues
                throw new ApplicationException("There is no event handler for RequestWorldLocation");
            }

            PartRequestWorldLocationArgs args = new PartRequestWorldLocationArgs();

            this.RequestWorldLocation(this, args);

            if (args.Position == null)
            {
                throw new ApplicationException("The event handler for RequestWorldLocation didn't set position");
            }
            else if (args.Orientation == null)
            {
                throw new ApplicationException("The event handler for RequestWorldLocation didn't set orientation");
            }

            return (args.Position.Value, args.Orientation.Value);
        }
        /// <summary>
        /// Item1 = Velocity
        /// Item2 = AngularVelocity
        /// Item3 = Velocity at point (only returned if a position is passed in)
        /// NOTE: atPoint is in model coords, and should only be non null if you want the velocity at that point to be calculated
        /// </summary>
        public (Vector3D velocity, Vector3D angularVelocity, Vector3D? velocityAtPoint) GetWorldSpeed(Point3D? atPoint)
        {
            if (this.RequestWorldSpeed == null)
            {
                throw new ApplicationException("There is no event handler for RequestWorldSpeed");
            }

            PartRequestWorldSpeedArgs args = new PartRequestWorldSpeedArgs();
            args.GetVelocityAtPoint = atPoint;

            this.RequestWorldSpeed(this, args);

            if (args.Velocity == null)
            {
                throw new ApplicationException("The event handler for RequestWorldSpeed didn't set velocity");
            }
            else if (args.AngularVelocity == null)
            {
                throw new ApplicationException("The event handler for RequestWorldSpeed didn't set angular velocity");
            }
            else if (atPoint != null && args.VelocityAtPoint == null)
            {
                throw new ApplicationException("The event handler for RequestWorldSpeed didn't set velocity at point");
            }

            return (args.Velocity.Value, args.AngularVelocity.Value, args.VelocityAtPoint);
        }
        /// <summary>
        /// Try to avoid using this unless necessary.  If the part is hosted in something other than a map object, then the parent
        /// could come back null
        /// </summary>
        /// <remarks>
        /// The case that needed this was SwarmBay.  The swarm bots need to know the location/size/speed of the parent bot
        /// </remarks>
        public IMapObject GetParent()
        {
            if (this.RequestParent == null)
            {
                throw new ApplicationException("There is no event handler for RequestParent");
            }

            PartRequestParentArgs args = new PartRequestParentArgs();

            this.RequestParent(this, args);

            return args.Parent;
        }

        public static (Point3D min, Point3D max) GetAABB(IEnumerable<PartBase> parts)
        {
            QuaternionRotation3D quatRot = new QuaternionRotation3D(Quaternion.Identity);
            RotateTransform3D transform = new RotateTransform3D(quatRot);

            bool sawPart = false;

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (PartBase part in parts)
            {
                sawPart = true;

                quatRot.Quaternion = part.Orientation;

                GetAABB_Test(ref minX, ref maxX, transform, part.Position, part.ScaleActual, Axis.X);
                GetAABB_Test(ref minY, ref maxY, transform, part.Position, part.ScaleActual, Axis.Y);
                GetAABB_Test(ref minZ, ref maxZ, transform, part.Position, part.ScaleActual, Axis.Z);
            }

            if (sawPart)
            {
                return (new Point3D(minX, minY, minZ), new Point3D(maxX, maxY, maxZ));
            }
            else
            {
                return (new Point3D(0, 0, 0), new Point3D(0, 0, 0));
            }
        }
        private static void GetAABB_Test(ref double min, ref double max, RotateTransform3D transform, Point3D point, Vector3D scale, Axis axis)
        {
            double half = scale.Coord(axis) / 2d;

            double dist1 = (point + transform.Transform(Math3D.GetNewVector(axis, half))).Coord(axis);
            double dist2 = (point + transform.Transform(Math3D.GetNewVector(axis, -half))).Coord(axis);

            if (dist1 < min)
            {
                min = dist1;
            }

            if (dist2 < min)
            {
                min = dist2;
            }

            if (dist1 > max)
            {
                max = dist1;
            }

            if (dist2 > max)
            {
                max = dist2;
            }
        }

        public void EnsureCorrectGraphics_StandardDestroyed()
        {
            if (this.IsDestroyed)
            {
                #region destroyed

                foreach (PartDesignBase.MaterialColorProps material in this.Design.MaterialBrushes)
                {
                    if (material.Diffuse != null)
                    {
                        material.Diffuse.Brush = WorldColors.Destroyed_Brush;
                    }
                    else if (material.Specular != null)
                    {
                        material.Specular.Brush = WorldColors.Destroyed_SpecularBrush;
                        material.Specular.SpecularPower = WorldColors.Destroyed_SpecularPower;
                    }
                    else if (material.Emissive != null)
                    {
                        material.Emissive.Brush = Brushes.Transparent;
                    }
                    else
                    {
                        throw new ApplicationException("Unknown material type");
                    }
                }

                #endregion
            }
            else
            {
                #region standard

                foreach (PartDesignBase.MaterialColorProps material in this.Design.MaterialBrushes)
                {
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
                }

                #endregion
            }
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

        //TODO: Have another graphic option in Design for informational.  It would be the same color set used for all parts (health: Green-Yellow-Red | unused parts: White-Red | etc)
        protected virtual void OnDestroyed()
        {
            if (!_initialized)
            {
                return;
            }

            EnsureCorrectGraphics_StandardDestroyed();

            if (this.Destroyed != null)
            {
                this.Destroyed(this, new EventArgs());
            }
        }
        protected virtual void OnResurrected()
        {
            //NOTE: This gets called when hitpoints are first assigned (before the part's derived constructor has created the design)
            if (!_initialized)
            {
                return;
            }

            EnsureCorrectGraphics_StandardDestroyed();

            if (this.Resurrected != null)
            {
                this.Resurrected(this, new EventArgs());
            }
        }

        #endregion
    }

    #endregion


    #region THOUGHTS

    public class PartVisuals
    {
        public readonly Model3D Model;

        public readonly PartVisuals_MaterialSet[] Materials;

        public Color InfoColor
        {
            get
            {
                //return this.Materials_Info.Item1.Color;
                return Colors.Yellow;
            }
            set
            {
                //this.Materials_Info.Item1.Color = value;
            }
        }
    }
    public class PartVisuals_MaterialSet
    {
        public readonly MaterialGroup Group;

        // These are overlay brushes
        public readonly EmissiveMaterial SelectionEmissive;
        public readonly PartDesignBase.MaterialColorProps MaterialBrush;

        //No need for these - just update the brush in this.MaterialBrush (see PartDesignBase.SetEditorBrushes)
        // Only one of these sets will be applied at a time
        /// <summary>
        /// These are the standard set of materials
        /// </summary>
        public readonly System.Windows.Media.Media3D.Material[] Materials_Standard;
        /// <summary>
        /// These are what to use when the part is destroyed
        /// </summary>
        public readonly System.Windows.Media.Media3D.Material[] Materials_Destroyed;
        /// <summary>
        /// These are used to convey informational (they are colored with InfoColor)
        /// </summary>
        /// <remarks>
        /// Item1=The material that takes the variable color
        /// Item2=Any other materials to make it look nice (probably just specular)
        /// 
        /// One example of informational coloring would be health: { Red, Yellow, Green }
        /// 
        /// Others could be:
        ///     stress analysis
        ///     shield coverage
        ///     contribution to moment of inertia
        ///     mass
        ///     parts in line of fire (from weapons or thrusters)
        ///     parts that aren't fed by containers
        /// </remarks>
        public readonly Tuple<DiffuseMaterial, System.Windows.Media.Media3D.Material[]> Materials_Info;
    }

    #endregion


    #region class: ShipPartDNA

    /// <summary>
    /// This holds properties of a part
    /// </summary>
    /// <remarks>
    /// This class is meant to stay simple, and be able to be serialized to xaml and json, which means no custom constructors,
    /// just public properties
    /// 
    /// Also, for xaml serialization, don't use 3.5's XamlWriter.Save and XamlReader.Load, they suck.
    /// Instead use 4.0's XamlServices.Load/Save in system.xaml.dll
    /// 
    /// Originally, there was a class that derived from PartDNA called PartNeuralDNA.  But almost every part has a neuron,
    /// and there was a lot of code to check if PartNeuralDNA, overriden methods, etc
    /// 
    /// NOTE: The neurons and links can be null if the dna was built by the editor.
    /// </remarks>
    public class ShipPartDNA
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

        /// <summary>
        /// This is the location of neurons
        /// </summary>
        /// <remarks>
        /// Some parts have multiple types of neurons (brain has readwrite neurons, and the brain chemicals are write only).  This property
        /// should hold the most common type, and AltNeurons holds the others
        /// </remarks>
        public Point3D[] Neurons
        {
            get;
            set;
        }
        /// <summary>
        /// This holds sets of "specialty" neurons.
        /// The first index is the set, the second index is individual neurons within that set
        /// </summary>
        /// <remarks>
        /// I could have just made Neurons a jagged array, and then wouldn't have a need for AltNeurons.  But jagged arrays are tedious to
        /// use, and most parts will only have one set of neurons
        /// </remarks>
        public Point3D[][] AltNeurons
        {
            get;
            set;
        }

        /// <summary>
        /// Internal links are only between the neurons within this part
        /// </summary>
        public NeuralLinkDNA[] InternalLinks
        {
            get;
            set;
        }
        /// <summary>
        /// External links are links between this part and other neural containers.  Also, only the links that go from that other part into
        /// this part are stored (to avoid dual storage of links - only backpointers are stored)
        /// </summary>
        /// <remarks>
        /// I tried to just store:
        ///		Tuple(Point3D, Quaternion, NeuralLinkDNA)[] ExternalLinks
        ///		
        /// But that couldn't be serialized (no default constructor), so I had to create a derived class
        /// </remarks>
        public NeuralLinkExternalDNA[] ExternalLinks
        {
            get;
            set;
        }

        /// <summary>
        /// This is named so that the default of 0 is fully healthy (in case the property is never set)
        /// </summary>
        /// <remarks>
        /// 0 - full hit points
        /// 1 - fully destroyed
        /// </remarks>
        public double PercentDamaged
        {
            get;
            set;
        }

        public virtual bool IsEqual(ShipPartDNA dna, bool comparePositionOrientation = false, bool compareNeural = false)
        {
            if (dna == null)
            {
                return false;
            }

            if (this.PartType != dna.PartType)
            {
                return false;
            }

            if (!Math3D.IsNearValue(this.Scale, dna.Scale))
            {
                return false;
            }

            if (comparePositionOrientation)
            {
                if (!Math3D.IsNearValue(this.Position, dna.Position))
                {
                    return false;
                }

                if (!Math3D.IsNearValue(this.Orientation, dna.Orientation))
                {
                    return false;
                }
            }

            if (compareNeural)
            {
                throw new ApplicationException("finish this");
            }

            return true;
        }
    }

    #endregion
    #region class: MapPartDNA

    /// <summary>
    /// This has some reuse with ShipPartDNA.  I could make a base class to hold the common items, but ship parts
    /// and map parts will never be stored in the same list, and it just feels too formal
    /// </summary>
    public class MapPartDNA
    {
        public string PartType
        {
            get;
            set;
        }

        // ShipPartDNA uses scale, but I think most map parts will just need a generic size.  May want to change this back to scale if that assumption is wrong
        public double Radius
        {
            get;
            set;
        }

        public Point3D Position
        {
            get;
            set;
        }
        public Quaternion Orientation
        {
            get;
            set;
        }

        public Vector3D Velocity
        {
            get;
            set;
        }
        public Vector3D AngularVelocity
        {
            get;
            set;
        }
    }

    #endregion

    #region class: MassBreakdownCache

    /// <summary>
    /// Every design part needs to calculate a mass breakdown, then remember it in case the same request comes in
    /// </summary>
    public class MassBreakdownCache
    {
        public MassBreakdownCache(UtilityNewt.IObjectMassBreakdown breakdown, Vector3D scale, double cellSize)
        {
            Breakdown = breakdown;
            Scale = scale;
            CellSize = cellSize;
        }

        public readonly UtilityNewt.IObjectMassBreakdown Breakdown;
        public readonly Vector3D Scale;
        public readonly double CellSize;
    }

    #endregion

    #region interface: IPartUpdate

    /// <summary>
    /// This is for parts that need to be regularly updated (like sensors)
    /// </summary>
    public interface IPartUpdatable
    {
        /// <summary>
        /// This is called on the same thread the object was created on.  This should do as little as possible.  Basically, just graphics
        /// </summary>
        void Update_MainThread(double elapsedTime);
        /// <summary>
        /// This is called on a random thread each time
        /// </summary>
        void Update_AnyThread(double elapsedTime);

        // These are hints for how often to call update.  These values need to be the same for any instance of that type (that way
        // optimizations can be done at the type level instead of evaluating each instance)
        //
        // A value of zero means don't skip any updates.  One would skip every other update, two would be 1 tick,
        // 2 skips, 1 tick, 2 skips, etc.  This way, items that don't need to be called as often can give larger skip values.
        //
        // A value of null means don't bother calling that method (no code inside that method)
        int? IntervalSkips_MainThread { get; }
        int? IntervalSkips_AnyThread { get; }
    }

    #endregion
    #region class: PartRequestWorldLocationArgs

    public class PartRequestWorldLocationArgs : EventArgs
    {
        public Point3D? Position = null;
        public Quaternion? Orientation = null;
    }

    #endregion
    #region class: PartRequestWorldSpeedArgs

    public class PartRequestWorldSpeedArgs : EventArgs
    {
        /// <summary>
        /// Set this (in model coords) if you want VelocityAtPoint (world coords) to be calculated
        /// </summary>
        public Point3D? GetVelocityAtPoint = null;
        public Vector3D? VelocityAtPoint = null;

        public Vector3D? Velocity = null;
        public Vector3D? AngularVelocity = null;
    }

    #endregion
    #region class: PartRequestParentArgs

    public class PartRequestParentArgs : EventArgs
    {
        public IMapObject Parent = null;
    }

    #endregion
    #region class: PartAllowedScale

    /// <summary>
    /// This is a singleton made to look like a static class.  It returns PartDesignAllowedScale enum from PartDesign classes (using reflection)
    /// </summary>
    public class PartAllowedScale
    {
        #region Declaration Section

        private static readonly object _lockStatic = new object();
        private readonly object _lockInstance;

        /// <summary>
        /// The static constructor makes sure that this instance is created only once.  The outside users of this class
        /// call the static property Instance to get this one instance copy.  (then they can use the rest of the instance
        /// methods)
        /// </summary>
        private static PartAllowedScale _instance;


        private SortedList<string, PartDesignAllowedScale?> _scalesByPart;


        #endregion

        #region Constructor / Instance Property

        /// <summary>
        /// Static constructor.  Called only once before the first time you use my static properties/methods.
        /// </summary>
        static PartAllowedScale()
        {
            lock (_lockStatic)
            {
                // If the instance version of this class hasn't been instantiated yet, then do so
                if (_instance == null)
                {
                    _instance = new PartAllowedScale();
                }
            }
        }
        /// <summary>
        /// Instance constructor.  This is called only once by one of the calls from my static constructor.
        /// </summary>
        private PartAllowedScale()
        {
            _lockInstance = new object();

            _scalesByPart = new SortedList<string, PartDesignAllowedScale?>();
        }

        /// <summary>
        /// This is how you get at my instance.  The act of calling this property guarantees that the static constructor gets called
        /// exactly once (per process?)
        /// </summary>
        private static PartAllowedScale Instance
        {
            get
            {
                // There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
                // finished before this function gets called
                return _instance;
            }
        }

        #endregion

        #region Public Methods

        public static PartDesignAllowedScale GetForPart(string partType, bool throwExceptionIfNotFound)
        {
            PartDesignAllowedScale? retVal = PartAllowedScale.Instance.GetForPartInstance(partType);

            if (retVal == null)
            {
                if (throwExceptionIfNotFound)
                {
                    throw new ApplicationException("Didn't find ALLOWEDSCALE for " + partType);
                }
                else
                {
                    return PartDesignAllowedScale.XYZ;		// this is the most limiting choice
                }
            }
            else
            {
                return retVal.Value;
            }
        }

        #endregion
        #region Private Methods

        private PartDesignAllowedScale? GetForPartInstance(string partType)
        {
            const string NAMESPACE = "Game.Newt.v2.GameItems.ShipParts";

            #region Clean up the string

            if (!partType.EndsWith("Design"))
            {
                partType += "Design";
            }

            int indexOf = partType.LastIndexOf('.');
            if (indexOf >= 0)
            {
                partType = partType.Substring(indexOf + 1);
            }

            #endregion

            lock (_lockInstance)
            {
                if (!_scalesByPart.ContainsKey(partType))
                {
                    #region Add to list

                    Type type = null;
                    try
                    {
                        type = Type.GetType(NAMESPACE + "." + partType, true);
                    }
                    catch (Exception)
                    {
                        //TODO: May also want to try map parts
                        type = null;
                    }

                    PartDesignAllowedScale? scale = null;
                    if (type != null)
                    {
                        FieldInfo field = GetConstants(type).Where(o => o.Name == "ALLOWEDSCALE").FirstOrDefault();
                        if (field != null)
                        {
                            try
                            {
                                scale = (PartDesignAllowedScale)field.GetRawConstantValue();
                            }
                            catch (Exception)
                            {
                                scale = null;
                            }
                        }
                    }

                    _scalesByPart.Add(partType, scale);

                    #endregion
                }

                // Exit Function
                return _scalesByPart[partType];
            }
        }

        /// <summary>
        /// This method will return all the constants from a particular
        /// type including the constants from all the base types
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://weblogs.asp.net/whaggard/archive/2003/02/20/2708.aspx
        /// </remarks>
        /// <param name="type">type to get the constants for</param>
        /// <returns>array of FieldInfos for all the constants</returns>
        private static FieldInfo[] GetConstants(Type type)
        {
            // Gets all public and static fields (FlattenHierarchy get from base types as well)
            return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).
                // IsLiteral determines if its value is written at compile time and not changeable
                // IsInitOnly determine if the field can be set in the body of the constructor
                // for C# a field which is readonly keyword would have both true but a const field would have only IsLiteral equal to true
                Where(o => o.IsLiteral && !o.IsInitOnly).
                ToArray();
        }

        #endregion
    }

    #endregion

    #region enum: PartDesignAllowedScale

    public enum PartDesignAllowedScale
    {
        /// <summary>
        /// This is an override value for parts that can't be resized or copied, only moved around (purchased parts)
        /// </summary>
        None,
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
    #region enum: PartDesignAllowedRotation

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
    #region enum: ShipPartType

    /// <summary>
    /// This will likely never be used as an enum, just use the derived part classes.  This is just a place to organize some thoughts
    /// </summary>
    internal enum ShipPartType
    {
        // Structures
        Hull,
        ArmorShard,
        Spar,
        Wing,

        // Containers
        CargoBay,
        FuelTank,
        EnergyTank,
        AmmoBox,

        // Brains
        Brain,
        BrainRadio,		// this will expose a neuron (or cloud) that can be read/written and will be running on a certain frequency.  It will average the neuron's value with other radios in range/frequency.  This will allow ships to have a form of telepathy
        BrainPlugin,		// this one wouldn't be used in a pure evolution simulation.  Instead, this should be thought of as something like an OR, AND, NAND gate.  It is a brain that was trained by some external utility to recognize certain patterns.  It would have a small set of output neurons placed far away from the working neurons (like 6 neurons in a pentagon plate, always off the X axis - or something like that).  Instead of the external links simply hooking to whatever is at some point, it should request specific parts to hook to (ex: give me a camera and a force sensor, regardless of location, ignore all other parts).  This way, a random ship could be assembled, then download some plugin brains trained for what you want, and wire it up.
        BrainScripted,		// this would have similar interface as BrainPlugin (ask for certain parts, have well defined outputs, or even ask for a set of parts to directly control), but instead of internally being a neural net, it would run a bit of custom code (like javascript).  That would give the user a chance to write their own AI logic.  (ex: when plugin brain detects food, and cargo < X, fire neurons.  etc).  This wouldn't do much good controlling thrusters/guns, but it could be used to trigger anxiety (or concepts like that).  Or if controlling other parts, could be used to directly raise shields, jetison cargo, put parts on standby (and if manipulating BrainRadio, alerting others)
        ScriptedOutput_Vector,		// this has neurons in a uniform sphere, and takes in a vector.  It provides a way for BrainScripted to output a vector, which could be interpreted as fly in this direction, shoot in this direction, etc (maybe BrainScripted should just have an enum for the output neuron pattern: uniform sphere, plate, L, etc) (an L shape has implied orientaion, similar to those stickers for smartphones to superimpose 3D objects onto)

        // A cargo bay is required to have at least one tube.  Nothing wider than the tube can go through it, and the tube can't be wider than a cargo bay
        // Cargo bays are cubes, but these tubes will be cylinders (make the transfer instant so that cargo won't be stored in a tube)
        Tube_Cargo_Cargo,		// used to transfer from one cargo bay to another
        Tube_Cargo_Mouth,		// used to connect a cargo bay to the outside world
        Tube_Cargo_Transfer,		// this lets the bot transfer cargo to other bots, but one end must be tied to a cargo bay

        // These let the bot share/take energy and fuel with other bots.  Internal flow of energy/fuel isn't modeled, it's just assumed to go
        // through wires/tubes.  But to share with the outside, two of the same type of transfer tubes need to be joined, and the transfer needs
        // to be authorized.
        //
        // These will be a bit complex to implement, but will allow for much richer social interaction, and bot specialization
        Transfer_Energy,
        Transfer_Fuel,

        // Converters
        Converter_MatterToFuel,		// this will have a cargo section, and output fuel (it won't actively feed on an existing cargo bay, small amounts of cargo need to be loaded into it).  Also, this may require exact materials (1 part graphite, 1 part water, x energy)
        Converter_MatterToEnergy,		// this can take any matter, the denser the better
        Converter_EnergyToFuel,		// this will connect between an existing energy tank and fuel tank
        Converter_EnergyToAmmo,
        Converter_RadiationToEnergy,		// this is a solar cell

        // Motion
        Thruster,
        TractorBeam,
        GrappleGun,
        ImpulseEngine,      // this would be a cheat.  Input would be a linear vector and rotation vector.  It would directly apply forces to the body (not force at point for linear motion, but just direct force)

        // Equipment
        ThrustController,		// thrusters expose a neuron that will directly control that particular thruster.  This should expose a spherical shell of input neurons that represents a desired vector.  This will translate that vector into the set of thrusters needed to make the ship go in that direction without introducing spin
        ProjectileGun,		// fires projectiles (could be simple slugs, or missiles, or drones).  It will need an ammo tank that only it can use
        BeamGun,		// probably keep this short range, more like a laser sword, or flame thrower
        Spike,		// good for ramming
        VibroSpike,		// very good for ramming, consumes small amounts of energy
        EnergyShield,		// blocks radiation
        KineticShield,		// blocks impacts
        TractorShield,		// won't let tractor beams lock on

        // Sensors
        //NOTE: Some of these camera/sensor types are algorithm hacks that could be modeled in a neural net.  But my goal isn't to perfectly model
        //biologicals, it is to allow for complex organisms to emerge efficiently.  Neural nets are inneficient when modeled on a computer.  So by
        //offering a bit more dedicated part types, the neural nets can stay simplified, and focus on the job of higher order thought.  Besides, any of
        //these parts could be manufactured in real life, and be used by a real robot.
        //NOTE: When implementing some of these cameras, a separate viewport/perspective camera will need to be set up, and draw to a bitmap.
        //But don't create a unique viewport for every ship's camera sensor, make a camera pool class that will set up a fixed number of viewports/cameras,
        //then will move a camera to the sensor's location, take a picture, store to a volatile, then move on to the next camera.  Each viewport will need
        //to independently have its own set of Visual3Ds and keep their transforms up to date (use the map class to manage calling the camera pool
        //class, so that the main form just talks to the map)
        Camera_Color_RGB,		// expose N plates of neurons, each sensitive to a certain frequency range (red,green,blue) - or instead of independent plates, have packets of cones together like real retinas (then just space the clusters farther apart) (in neural coords, the positions don't have to fit it in any predefined real world volume.  They just have to be consistent from parent to child)
        Camera_Color_Varied,
        Camera_Color_Gray,
        Camera_Edge,		// run the image through an edge detect algorithm first
        Camera_Movement,		// only fire pixels that have changed from the previous frame. 1 for initial change, then linear fade to 0 over a small time.  May also need a threshold of change, only go to 1 if the change is large.  That way slow movement won't register as much - the only way for the threshold idea to work is to look at surrounding pixels to see how fast (this one may not be nessassary if the listening neurons can do this naturally) (this one may not be very useful in a space environment where everything is moving all the time)
        Camera_Change,		// the movement camera would focus on pixels sliding across the image, and a change camera would focus on pixels that suddenly change (not movement, more like blinking lights, or objects that suddenly appear/disappear)
        RangeFinder,		// a camera paints a bitmap with reflected color.  This paints a bitmap with reflected distance
        Ears,
        RadiationDetector,		// how much ambient radiation there is (geiger counter)
        GeneralForceSensor,		// outputs the sum of all forces felt (gravity, tractor, fluid, impacts, thruster, etc)
        GravityForceSensor,		// outputs how much gravity force is felt - this is like the inner ear
        TractorForceSensor,		// outputs how much tractor force is felt (from others, not from the ship's own tractors)
        FluidForceSensor,		// outputs how much fluid force is felt (I think this should be separate from 
        ReactionForceSensor,		// outputs how much force is felt from the various ship's parts (thrusters, tractors, grapple, gun recoil, etc)
        SpinSensor,		// outputs how fast the ship is spinning around its center of mass
        CollisionSensor,		// this should report any collision on the whole ship (instead of only reporting when the sensor itself is whacked)

        // Display
        EmotionLights,      // emulate a few LEDs.  Different colors could represent different internal desires (red for attack, etc)
        Tail,       // have a tail that emulates a dog's tail  http://spectrum.ieee.org/automaton/robotics/home-robots/wagging-tails-help-robots-communicate

        // Multi World
        // Each world runs independent of the others (with its own clock).  These worlds may be on the same machine, or spread across machines
        // in a network.
        //
        // In order for cross world jumping to work, these worlds need to be thought of as bubbles along either a line / a plane / a cube.
        //
        // When jumping from one world to another, the 3D coords stay the same, you just shift out of one world, and into another.  If the worlds
        // are different sized, the coords need to be scaled so each world has a size of one.
        //
        // While shifting, you are ghosted in both.  Damage dealt/recieved will be a percent of how much you are in that particular world
        //
        // A bigger engine will let you make the shift faster.
        //
        // Some advantages to shifting
        //		Jump to a world with less harsh conditions / fewer predators / easier prey
        //		Hop to a world, hitch a ride on an asteroid, hop back when you are where you wanted to go
        //		If you are in a large world, hop to a small one, travel a small distance and hop back
        //		If there is an obstacle in your current world, hop to a different one, fly past where the obstacle would have been, hop back
        //		Ambush others that don't have cross dimension abilities
        CrossDimensionCamera,		// this can see parallel worlds
        CrossDimensionRangeFinder,
        CrossDimensionEngine,		// this can jump to parallel worlds
        CrossDimensionShield,		// this won't allow cross dimension engines to work within a certain radius (allows sheep to prevent wolves from teleporting right next to them)

        // Joints
        //TODO: Come up with some joints:
        //		Some passive with degrees of freedom, a desired rest position, and springs to return to that position - these would be used as shock absorbers
        //		Some active that will have lock limits, and the desired rest position can be controlled - these would be used as servos/muscles
    }

    #endregion
    #region enum: MapPartType

    /// <summary>
    /// These would be a bunch of parts that could be dropped into a map editor (same control as the ship editor, just different parts given to it)
    /// </summary>
    internal enum MapPartType
    {
        Star,		// this will destroy anything that touches it (also pin a vector field and radiation field to it)

        Planet_Fixed,		// this is a planet that stays stationary on the map
        Planet_Float,		// this is a planet that flies freely around the map

        Asteroid,		// let the user place individual asteroids (they will probably be larger ones)
        AsteroidCloud,
        AsteroidRing,		// instead of a ring, may want to let the user draw as a brush stroke

        // When choosing minerals, give the user some options:
        //		Specific mineral type
        //		Low value types (chooses random minerals that are considered low value)
        //		Med value types
        //		High value types
        Mineral,
        MineralCloud,
        MineralRing,

        // Probably want some different types of generator:
        //		PointSource:  The items spawn from a single point (combine with a star for a bit of realism)
        //		CloudSource:  The items spawn somewhere within an area (could be the size of the whole map)
        //		RingSource:  The items spawn somewhere within a torus
        AsteroidGenerator,		// it will spawn random asteroids every once in a while (only if the count gets too low?)
        MineralGenerator,

        // Right now, the space station is just for the human player's benefit (as a marketplace/repair).  In the future, the space station may want to
        // try to maintain various preassigned conditions around it.  Sort of like a gardner or shepherd.  Let it manipulate its environment with
        // tractor beams, weapons, custodian ships.  Its inteligence should be scripted instead of purely neural.
        //
        // For example:
        //		Reward certain behavior
        //		Punish certain behavior
        //		Maintain certain densities of asteroids/minerals/ships
        SpaceStation,

        // Make several different shapes, orientation.  Make it either affect force or acceleration
        //		Inward:  A sphere with in pointing force (need to have either constant, linear dropoff, quadratic dropoff of the force relative to center)
        //		Outward:  Same as in, but pointed away
        //		SwirlCylinder:  Force is in (ignoring Z), then an angle to the side
        //		SwirlEllipse:  Force is in to a point, then an angle to the side
        //		JetStream:  A free drawn tube of force
        VectorField,

        ViscocityField,		// either constant, linear dropoff, quadratic dropoff.  Pin it to a planet or large asteroid for interesting effects (or pin two, one for water, and an outer one for air)

        RadiationField,		// an arbitrary blob of radiation (combine with a viscocity field to make a nebula)
    }

    #endregion
}
