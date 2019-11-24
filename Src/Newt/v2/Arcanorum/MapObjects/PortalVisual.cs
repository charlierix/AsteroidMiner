using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum.MapObjects
{
    public class PortalVisual : IMapObject
    {
        #region Declaration Section

        private TaskScheduler _mainThread = TaskScheduler.FromCurrentSynchronizationContext();

        #endregion

        #region Constructor

        public PortalVisual(object item, double radius)
        {
            if (item is Shop)
            {
                this.PortalType = PortalVisualType.Shop;
            }
            else
            {
                throw new ApplicationException("Unknown item's type: " + item.GetType().ToString());
            }

            this.Item = item;

            #region WPF Model

            this.Model = GetModel(out this.BackdropPanelColors, this.PortalType, radius);

            _rotateTransform = new QuaternionRotation3D();
            _translateTransform = new TranslateTransform3D();

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(_rotateTransform));
            transform.Children.Add(_translateTransform);

            ModelVisual3D visual = new ModelVisual3D();
            visual.Transform = transform;
            visual.Content = this.Model;

            this.Visuals3D = new Visual3D[] { visual };

            #endregion

            this.Token = TokenGenerator.NextToken();
            this.Radius = radius;
            this.CreationTime = DateTime.UtcNow;
        }

        #endregion

        #region IMapObject Members

        public long Token
        {
            get;
            private set;
        }

        public bool IsDisposed
        {
            get
            {
                return false;
            }
        }

        public Body PhysicsBody
        {
            get
            {
                return null;
            }
        }

        public Visual3D[] Visuals3D
        {
            get;
            private set;
        }
        public Model3D Model
        {
            get;
            private set;
        }

        private Point3D _positionWorld;     // storing this off, because the transform isn't threadsafe
        private TranslateTransform3D _translateTransform = null;
        public Point3D PositionWorld
        {
            get
            {
                //return new Point3D(_translateTransform.OffsetX, _translateTransform.OffsetY, _translateTransform.OffsetZ);
                return _positionWorld;
            }
            set
            {
                Task.Factory.StartNew(() =>
                    {
                        // The transform must be set in the same thread that created it
                        _translateTransform.OffsetX = value.X;
                        _translateTransform.OffsetY = value.Y;
                        _translateTransform.OffsetZ = value.Z;
                    }, CancellationToken.None, TaskCreationOptions.None, _mainThread).Wait();

                _positionWorld = value;
            }
        }

        private Quaternion _rotationWorld;      // the transform isn't threadsafe
        private QuaternionRotation3D _rotateTransform = null;
        public Quaternion RotationWorld
        {
            get
            {
                //return _rotateTransform.Quaternion;
                return _rotationWorld;
            }
            set
            {
                Task.Factory.StartNew(() =>
                {
                    // The transform must be set in the same thread that created it
                    _rotateTransform.Quaternion = value;
                }, CancellationToken.None, TaskCreationOptions.None, _mainThread).Wait();

                _rotationWorld = value;
            }
        }

        public Vector3D VelocityWorld
        {
            get
            {
                return new Vector3D(0, 0, 0);
            }
        }
        public Matrix3D OffsetMatrix
        {
            get
            {
                throw new InvalidOperationException("This class doesn't have a physics body");
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

        public readonly PortalVisualType PortalType;

        public readonly object Item;

        public readonly BotShellColorsDNA BackdropPanelColors;

        #endregion

        #region Private Methods

        private static Model3D GetModel(out BotShellColorsDNA backdropColors, PortalVisualType portalType, double radius)
        {
            switch (portalType)
            {
                case PortalVisualType.Shop:
                    return GetModel_Shop(out backdropColors, radius);

                default:
                    throw new ApplicationException("Unknown PortalVisualType: " + portalType.ToString());
            }
        }

        private static Model3D GetModel_Shop(out BotShellColorsDNA backdropColors, double radius)
        {
            Model3DGroup retVal = new Model3DGroup();

            backdropColors = new BotShellColorsDNA();
            backdropColors.DiffuseDrift = 0;
            backdropColors.EmissiveColor = "00000000";

            #region Plate

            backdropColors.InnerColorDiffuse = "554A3A";

            // Material
            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(backdropColors.InnerColorDiffuse))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40958265")), 20d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetSphere_LatLon(6, radius, radius, radius * .1);

            retVal.Children.Add(geometry);

            #endregion

            #region Gold Mineral

            // Get gold's color
            backdropColors.Light = Mineral.GetSettingsForMineralType(MineralType.Gold).DiffuseColor.ToHex();

            // Get the model of a gold mineral
            Model3D model = Mineral.GetNewVisual(MineralType.Gold);

            // Figure out the scale
            Rect3D aabb = model.Bounds;
            double halfSize = Math1D.Max(aabb.SizeX, aabb.SizeY, aabb.SizeZ) / 2d;

            double scale = (radius * .66d) / halfSize;
            model.Transform = new ScaleTransform3D(scale, scale, scale);

            retVal.Children.Add(model);

            #endregion

            return retVal;
        }

        #endregion
    }

    #region enum: PortalVisualType

    public enum PortalVisualType
    {
        Shop,
        //Home,
        //Cave,
        //Arena,
        //OtherMap,
    }

    #endregion

    #region class: Shop

    public class Shop
    {
        public Shop(Inventory inventory)
        {
            this.Inventory = inventory;
        }

        public readonly Inventory Inventory;
    }

    #endregion
}
