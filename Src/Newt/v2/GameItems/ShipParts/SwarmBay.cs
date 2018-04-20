using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    //TODO: Visual should be a truncated octahedron.  Tile some hexagon holes onto the hexagon portions
    //NO: it needs to be directional

    #region Class: SwarmBayToolItem

    public class SwarmBayToolItem : PartToolItemBase
    {
        #region Constructor

        public SwarmBayToolItem(EditorOptions options)
            : base(options)
        {
            this.TabName = PartToolItemBase.TAB_SHIPPART;
            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options, this);
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Swarm Bay";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes plasma, creates and controls swarm bots";
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
            return new SwarmBayDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: SwarmBayDesign

    public class SwarmBayDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double SCALE = 1d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public SwarmBayDesign(EditorOptions options, bool isFinalModel)
            : base(options, isFinalModel) { }

        #endregion

        #region Public Properties

        public override PartDesignAllowedScale AllowedScale
        {
            get
            {
                return ALLOWEDSCALE;
            }
        }
        public override PartDesignAllowedRotation AllowedRotation
        {
            get
            {
                return PartDesignAllowedRotation.X_Y_Z;
            }
        }

        private Model3DGroup _model = null;
        public override Model3D Model
        {
            get
            {
                if (_model == null)
                {
                    _model = CreateGeometry(this.IsFinalModel);
                }

                return _model;
            }
        }

        #endregion

        #region Public Methods

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D size = new Vector3D(this.Scale.X * SCALE * .5d, this.Scale.Y * SCALE * .5d, this.Scale.Z * SCALE * .5d);

            return CollisionHull.CreateSphere(world, 0, size, transform.Value);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(this.Scale.X * SCALE, this.Scale.Y * SCALE, this.Scale.Z * SCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new SwarmBayToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        //TODO: Rewrite this.  Make it look like a cave, or sea shell - something organic with an opening
        private Model3DGroup CreateGeometry(bool isFinal)
        {
            ScaleTransform3D scaleTransform = new ScaleTransform3D(SCALE, SCALE, SCALE);

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(scaleTransform);

            #region Outer Shell

            geometry = new GeometryModel3D();
            material = new MaterialGroup();

            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.SwarmBay_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.SwarmBay_Color));
            material.Children.Add(diffuse);

            specular = WorldColors.SwarmBay_Specular;
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
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));

            geometry.Geometry = UtilityWPF.GetSphere_Ico(.5, 0, false);
            geometry.Transform = transformGroup;

            retVal.Children.Add(geometry);

            #endregion
            #region Line

            BillboardLine3D line = new BillboardLine3D();
            line.Color = WorldColors.SwarmBay_Color;
            line.Thickness = .05 * SCALE;
            line.IsReflectiveColor = false;
            line.FromPoint = new Point3D(0, 0, 0);
            line.ToPoint = new Point3D(0, 0, .55 * SCALE);

            retVal.Children.Add(line.Model);

            #endregion

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: SwarmBay

    public class SwarmBay : PartBase, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "SwarmBay";

        private readonly ItemOptions _itemOptions;

        private readonly Map _map;
        private readonly World _world;
        private readonly int _material_SwarmBot;

        private readonly IContainer _plasma;

        //TODO: Don't hand the global strokes list to the bots.  Instead intercept, vote with other swarm bays, and only pass certain strokes
        private readonly SwarmObjectiveStrokes _strokes;

        private readonly double _timeBetweenBots;
        private double _timeSinceLastBot = 0;

        private readonly int _maxBots;
        private readonly List<SwarmBot1b> _bots = new List<SwarmBot1b>();

        private readonly double _plasmaTankThreshold;

        private readonly double _birthCost;
        private readonly double _birthRadius;

        #endregion

        #region Constructor

        public SwarmBay(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, Map map, World world, int material_SwarmBot, IContainer plasma, SwarmObjectiveStrokes strokes)
            : base(options, dna, itemOptions.SwarmBay_Damage.HitpointMin, itemOptions.SwarmBay_Damage.HitpointSlope, itemOptions.SwarmBay_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _map = map;
            _world = world;
            _material_SwarmBot = material_SwarmBot;
            _plasma = plasma;
            _strokes = strokes;

            this.Design = new SwarmBayDesign(options, true);
            this.Design.SetDNA(dna);

            double volume, radius;
            GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);
            //this.Radius = radius;

            _timeBetweenBots = StaticRandom.NextPercent(itemOptions.SwarmBay_BirthRate, .1);

            int maxCount = (itemOptions.SwarmBay_MaxCount * Math1D.Avg(dna.Scale.X, dna.Scale.Y, dna.Scale.Z)).ToInt_Round();
            if (maxCount < 0)
            {
                maxCount = 1;
            }
            _maxBots = maxCount;

            _plasmaTankThreshold = itemOptions.SwarmBay_Birth_TankThresholdPercent;

            _birthCost = itemOptions.SwarmBay_BirthCost;
            _birthRadius = itemOptions.SwarmBay_BirthSize / 2d;

            if (_map != null)
            {
                _map.ItemRemoved += Map_ItemRemoved;
            }

            this.Destroyed += SwarmBay_Destroyed;
        }

        #endregion

        #region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_map != null)
                {
                    _map.ItemRemoved -= Map_ItemRemoved;        // don't want this firing for each item removed

                    foreach (SwarmBot1b bot in _bots.ToArray())     // to array, just in case the list is manipulated inside the loop
                    {
                        _map.RemoveItem(bot);
                    }
                }
            }

            base.Dispose(disposing);
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
            // See if enough time has passed for another shot to be fired
            _timeSinceLastBot += elapsedTime;

            if (!this.IsDestroyed && _timeSinceLastBot >= _timeBetweenBots && _bots.Count < _maxBots)
            {
                bool plasmaThresholdMet = false;
                if (_plasma != null)
                {
                    double plasmaPercent = _plasma.QuantityCurrent / _plasma.QuantityMax;
                    plasmaThresholdMet = plasmaPercent >= _plasmaTankThreshold;
                }

                if (plasmaThresholdMet && CreateNewBot())
                {
                    _timeSinceLastBot = 0;
                }
            }
        }
        public void Update_AnyThread(double elapsedTime)
        {
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Public Properties

        private readonly double _mass;
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

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        #endregion

        #region Event Listeners

        private void Map_ItemRemoved(object sender, MapItemArgs e)
        {
            long token = e.Item.Token;

            int index = 0;
            while (index < _bots.Count)
            {
                if (_bots[index].Token == token)
                {
                    _bots.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private void SwarmBay_Destroyed(object sender, EventArgs e)
        {
            foreach (SwarmBot1b bot in _bots.ToArray())     // to array, just in case the list is manipulated inside the loop
            {
                _map.RemoveItem(bot);
            }
            _bots.Clear();
        }

        #endregion

        #region Private Methods

        private bool CreateNewBot()
        {
            // Pull plasma
            if (_plasma == null || _plasma.RemoveQuantity(_birthCost, true) > 0)
            {
                return false;
            }

            #region get position

            // this was copied from ProjectileGun

            // Model coords
            Point3D modelPosition = this.Design.Position;

            double length = this.Design.Scale.Z / 2d;
            length *= 3;     // make it a bit longer so it doesn't collide with the bay
            Vector3D modelDirection = new Vector3D(0, 0, length);

            // World coords
            var worldLoc = GetWorldLocation();
            var worldSpeed = GetWorldSpeed(null);

            Vector3D worldDirection = worldLoc.Item2.GetRotatedVector(modelDirection);
            Vector3D worldDirectionUnit = worldDirection.ToUnit();

            Point3D worldPosition = worldLoc.Item1 + worldDirection;

            #endregion

            double radius = StaticRandom.NextPercent(_birthRadius, .07);

            //NOTE: Could come back null if this part is in some kind of tester
            IMapObject parent = GetParent();

            // Create the bot
            SwarmBot1b bot = new SwarmBot1b(radius, worldPosition, parent, _world, _map, _strokes, _material_SwarmBot, _itemOptions.SwarmBot_HealRate, _itemOptions.SwarmBot_DamageAtMaxSpeed, _itemOptions.SwarmBot_MaxHealth);

            SetBotConstraints(bot);

            bot.PhysicsBody.Velocity = worldSpeed.Item1;
            bot.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(3d);

            _map.AddItem(bot);
            _bots.Add(bot);

            return true;
        }

        //TODO: A controller should actively manage these settings
        private void SetBotConstraints(SwarmBot1b bot)
        {
            const double SEARCHSLOPE = .66d;
            const double STROKESEARCHMULT = 2d;
            const double NEIGHBORSLOPE = .75d;
            const double ACCELSLOPE = 1.33d;
            const double ANGACCELSLOPE = .75d;
            const double ANGSPEEDSLOPE = .25d;

            double ratio = bot.Radius / _birthRadius;

            bot.SearchRadius = _itemOptions.SwarmBot_SearchRadius * SetBotConstraints_Mult(SEARCHSLOPE, ratio);
            bot.SearchRadius_Strokes = bot.SearchRadius * STROKESEARCHMULT;
            bot.ChaseNeighborCount = (_itemOptions.SwarmBot_ChaseNeighborCount * SetBotConstraints_Mult(NEIGHBORSLOPE, ratio)).ToInt_Round();
            bot.MaxAccel = _itemOptions.SwarmBot_MaxAccel * SetBotConstraints_Mult(ACCELSLOPE, 1 / ratio);
            bot.MaxAngularAccel = _itemOptions.SwarmBot_MaxAngAccel * SetBotConstraints_Mult(ANGACCELSLOPE, 1 / ratio);
            bot.MinSpeed = _itemOptions.SwarmBot_MinSpeed;
            bot.MaxSpeed = _itemOptions.SwarmBot_MaxSpeed;       // not adjusting this, or the swarm will pull apart when they get up to speed
            bot.MaxAngularSpeed = _itemOptions.SwarmBot_MaxAngSpeed * SetBotConstraints_Mult(ANGSPEEDSLOPE, 1 / ratio);
        }
        private static double SetBotConstraints_Mult(double slope, double value, double center = 1d)
        {
            // y=mx+b
            // y=mx + (1-m)

            double minX = center / 2d;
            double maxX = center * 2d;

            double minY = (slope * minX) + (center - slope);
            double maxY = (slope * maxX) + (center - slope);

            return UtilityCore.GetScaledValue(minY, maxY, minX, maxX, value);
        }

        private static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions)
        {
            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// they should be identical anyway
            radius *= SwarmBayDesign.SCALE;		// scale it

            volume = 4d / 3d * Math.PI * radius * radius * radius;
            mass = volume * itemOptions.SwarmBay_Density;

            actualScale = new Vector3D(radius * 2d, radius * 2d, radius * 2d);
        }

        #endregion
    }

    #endregion
}
