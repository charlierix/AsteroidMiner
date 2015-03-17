using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.MapParts
{
    public class Mineral : IMapObject
    {
        #region Declaration Section

        private static ThreadLocal<SharedVisuals> _sharedVisuals = new ThreadLocal<SharedVisuals>(() => new SharedVisuals());

        #endregion

        #region Constructor

        public Mineral(MineralType mineralType, Point3D position, double volumeInCubicMeters, World world, int materialID, SharedVisuals sharedVisuals, double densityMult = 1d, double scale = 1d)
        {
            this.MineralType = mineralType;
            this.VolumeInCubicMeters = volumeInCubicMeters;

            this.Model = GetNewVisual(mineralType, sharedVisuals, scale);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
            visual.Content = this.Model;

            this.Density = GetSettingsForMineralType(mineralType).Density * densityMult;

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            ScaleTransform3D scaleTransform = new ScaleTransform3D(scale, scale, scale);
            Point3D[] hullPoints = UtilityWPF.GetPointsFromMesh((MeshGeometry3D)sharedVisuals.GetMineralMesh(mineralType), scaleTransform);

            using (CollisionHull hull = CollisionHull.CreateConvexHull(world, 0, hullPoints))
            {
                this.PhysicsBody = new Body(hull, transform.Value, this.Density * volumeInCubicMeters, new Visual3D[] { visual });
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = .01f;
                this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

                //this.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);
            }

            #endregion

            // Calculate radius
            Point3D aabbMin, aabbMax;
            this.PhysicsBody.GetAABB(out aabbMin, out aabbMax);
            this.Radius = (aabbMax - aabbMin).Length / 2d;

            this.CreationTime = DateTime.Now;
        }

        #endregion

        #region IMapObject Members

        public long Token
        {
            get
            {
                return this.PhysicsBody.Token;
            }
        }

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
        public Model3D Model
        {
            get;
            private set;
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
        public Matrix3D OffsetMatrix
        {
            get
            {
                return this.PhysicsBody.OffsetMatrix;
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        public DateTime CreationTime
        {
            get;
            private set;
        }

        public int CompareTo(IMapObject other)
        {
            return MapObjectUtil.CompareToT(this, other);
        }

        public bool Equals(IMapObject other)
        {
            return MapObjectUtil.EqualsT(this, other);
        }
        public override bool Equals(object obj)
        {
            return MapObjectUtil.EqualsObj(this, obj);
        }

        public override int GetHashCode()
        {
            return MapObjectUtil.GetHashCode(this);
        }

        #endregion

        #region Public Properties

        public MineralType MineralType
        {
            get;
            private set;
        }

        public double VolumeInCubicMeters
        {
            get;
            private set;
        }

        public double Density
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public static Model3D GetNewVisual(MineralType mineralType, SharedVisuals sharedVisuals = null, double scale = 1d)
        {
            if (sharedVisuals == null)
            {
                sharedVisuals = _sharedVisuals.Value;
            }

            MineralStats stats = GetSettingsForMineralType(mineralType);

            Model3DGroup retVal = new Model3DGroup();

            // Material
            MaterialGroup materials = new MaterialGroup();
            if (stats.DiffuseColor.A > 0)
            {
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(stats.DiffuseColor)));
            }

            if (stats.SpecularColor.A > 0)
            {
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(stats.SpecularColor), stats.SpecularPower));
            }

            if (stats.EmissiveColor.A > 0)
            {
                materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(stats.EmissiveColor)));
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = sharedVisuals.GetMineralMesh(mineralType);

            retVal.Children.Add(geometry);

            if (mineralType == MineralType.Rixium)
            {
                #region Rixium Visuals

                // These need to be added after the main crystal, because they are semitransparent

                retVal.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, -.6), .38, .5, sharedVisuals));
                retVal.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, -.3), .44, .75, sharedVisuals));
                retVal.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, 0), .5, 1, sharedVisuals));
                retVal.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, .3), .44, .75, sharedVisuals));
                retVal.Children.Add(GetRixiumTorusVisual(new Vector3D(0, 0, .6), .38, .5, sharedVisuals));

                //TODO:  Look at the global lighting options
                PointLight pointLight = new PointLight();
                pointLight.Color = Color.FromArgb(255, 54, 147, 168);
                pointLight.Range = 20;
                pointLight.LinearAttenuation = .33;
                retVal.Children.Add(pointLight);

                #endregion
            }

            geometry.Transform = new ScaleTransform3D(scale, scale, scale);

            return retVal;
        }

        public static MineralStats GetSettingsForMineralType(MineralType mineralType)
        {
            //NOTE:  The geometry is defined in SharedVisuals.GetMineralMesh()

            switch (mineralType)
            {
                case MineralType.Ice:
                    #region Ice

                    // Going for an ice cube  :)

                    return new MineralStats(
                    Color.FromArgb(192, 201, 233, 242),       // slightly bluish white
                    Color.FromArgb(255, 203, 212, 214),
                    66d,
                    Colors.Transparent,

                    934);

                    #endregion

                case MineralType.Iron:
                    #region Iron

                    // This will be an iron bar (with rust)

                    return new MineralStats(
                    Color.FromArgb(255, 92, 78, 72),
                    Color.FromArgb(255, 117, 63, 40),
                    50d,
                    Colors.Transparent,

                    7900);

                    #endregion

                case MineralType.Graphite:
                    #region Graphite

                    // A shiny lump of coal (but coal won't form in space, so I call it graphite)

                    return new MineralStats(
                    Color.FromArgb(255, 32, 32, 32),
                    Color.FromArgb(255, 209, 209, 209),
                    75d,
                    Colors.Transparent,

                    2267);

                    #endregion

                case MineralType.Gold:
                    #region Gold

                    // A reflective gold bar

                    return new MineralStats(
                    Color.FromArgb(255, 255, 191, 0),
                    Color.FromArgb(255, 212, 138, 0),
                    75d,
                    Colors.Transparent,

                    19300);

                    #endregion

                case MineralType.Platinum:
                    #region Platinum

                    // A reflective platinum bar/plate
                    //TODO:  Make this a flat plate

                    return new MineralStats(
                    Color.FromArgb(255, 166, 166, 166),
                    Color.FromArgb(255, 125, 57, 45),
                    95d,
                    Color.FromArgb(20, 214, 214, 214),

                    21450);

                    #endregion

                case MineralType.Emerald:
                    #region Emerald

                    // A semi transparent double trapazoid

                    return new MineralStats(
                    UtilityWPF.ColorFromHex("D948A340"), //Color.FromArgb(192, 69, 128, 64);
                    UtilityWPF.ColorFromHex("24731C"), //Color.FromArgb(255, 26, 82, 20);
                    100d,
                    UtilityWPF.ColorFromHex("40499100"), //Color.FromArgb(64, 64, 128, 0);

                    2760);

                    #endregion

                case MineralType.Saphire:
                    #region Saphire

                    // A jeweled oval

                    return new MineralStats(
                    Color.FromArgb(160, 39, 53, 102),
                    Color.FromArgb(255, 123, 141, 201),
                    100d,
                    Color.FromArgb(64, 17, 57, 189),

                    4000);

                    #endregion

                case MineralType.Ruby:
                    #region Ruby

                    // A jeweled oval

                    return new MineralStats(
                    Color.FromArgb(180, 176, 0, 0),
                    Color.FromArgb(255, 255, 133, 133),
                    100d,
                    Color.FromArgb(32, 156, 53, 53),

                    4000);

                    #endregion

                case MineralType.Diamond:
                    #region Diamond

                    // A jewel

                    return new MineralStats(
                    Color.FromArgb(128, 230, 230, 230),
                    Color.FromArgb(255, 196, 196, 196),
                    100d,
                    Color.FromArgb(32, 255, 255, 255),

                    3515);

                    #endregion

                case MineralType.Rixium:
                    #region Rixium

                    // A petagon rod
                    // There are also some toruses around it, but they are just visuals.  This rod is the collision mesh

                    return new MineralStats(
                    Color.FromArgb(192, 92, 59, 112),
                        //_specularColor = Color.FromArgb(255, 145, 63, 196);
                    Color.FromArgb(255, 77, 127, 138),
                    100d,
                    Color.FromArgb(64, 112, 94, 122),

                    66666);

                    #endregion

                default:
                    throw new ApplicationException("Unknown MineralType: " + mineralType.ToString());
            }
        }

        public static decimal GetSuggestedCredits(MineralType mineralType)
        {
            //TODO:  Get these from some environment settings
            //NOTE:  The commented values are what I got from websites ($15,000 per carat for diamond seems a bit steep though)
            //I was screwing around, and this gave a pretty nice curve:
            //     =10 * ((5 + log10(value))^2)
            //
            // But I ended up rounding the numbers off to give a smoother curve

            switch (mineralType)
            {
                case MineralType.Ice:
                    //return .0003m;
                    return 25m;

                case MineralType.Iron:
                    //return .0017m;
                    return 50m;

                case MineralType.Graphite:
                    //return .08m;
                    return 150m;

                case MineralType.Gold:
                    //return 49420m;
                    return 400m;

                case MineralType.Platinum:
                    //return 59840m;
                    return 700m;

                case MineralType.Emerald:
                    //return 1250000m;
                    return 1000m;

                case MineralType.Saphire:
                    //return 5000000m;
                    return 1200m;

                case MineralType.Ruby:
                    //return 12500000m;
                    return 1500m;

                case MineralType.Diamond:
                    //return 75000000m;
                    return 2000m;

                case MineralType.Rixium:
                    //return 300000000m;
                    return 5000m;

                default:
                    throw new ApplicationException("Unknown MineralType: " + mineralType.ToString());
            }
        }

        #endregion

        #region Private Methods

        /// <param name="intensity">brightness from 0 to 1</param>
        private static Model3D GetRixiumTorusVisual(Vector3D location, double radius, double intensity, SharedVisuals sharedVisuals)
        {
            // Material
            MaterialGroup material = new MaterialGroup();
            //material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 44, 9, 82))));       // purple color
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 30, 160, 189))));       // teal color
            //material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 104, 79, 130)), 100d));     // purple reflection
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 60, 134, 150)), 100d));

            byte emissiveAlpha = Convert.ToByte(140 * intensity);

            //material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(64, 85, 50, 122))));
            material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(emissiveAlpha, 85, 50, 122))));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = sharedVisuals.GetRixiumTorusMesh(radius);

            Transform3DGroup transforms = new Transform3DGroup();
            transforms.Children.Add(new TranslateTransform3D(location));		// this is in model coords
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
    #region Class: MineralStats

    public class MineralStats
    {
        public MineralStats(Color diffuseColor, Color specularColor, double specularPower, Color emissiveColor, double density)
        {
            this.DiffuseColor = diffuseColor;
            this.SpecularColor = specularColor;
            this.SpecularPower = specularPower;
            this.EmissiveColor = emissiveColor;
            this.Density = density;
        }

        public readonly Color DiffuseColor;
        public readonly Color SpecularColor;
        public readonly double SpecularPower;
        public readonly Color EmissiveColor;
        public readonly double Density;
    }

    #endregion
}
