using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics_153;

namespace Game.Newt.AsteroidMiner_153
{
    public class SpaceStation : IMapObject
    {
        #region Declaration Section

        private Map _map = null;

        private double _currentAngle = 0d;
        private DateTime _lastAngleUpdateTime = DateTime.Now;

        private Transform3DGroup _mainTransform = null;

        #endregion

        #region IMapObject Members

        public ConvexBody3D PhysicsBody
        {
            get
            {
                return null;
            }
        }

        private List<ModelVisual3D> _visuals = null;
        public IEnumerable<ModelVisual3D> Visuals3D
        {
            get
            {
                return _visuals;
            }
        }

        private Point3D _positionWorld = new Point3D(0, 0, 0);
        public Point3D PositionWorld
        {
            get
            {
                return _positionWorld;
            }
        }

        public Vector3D VelocityWorld
        {
            get
            {
                return new Vector3D(0, 0, 0);
            }
        }

        private double _radius = 8;
        public double Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't change radius once created");
                }

                _radius = value;
            }
        }

        #endregion

        #region Public Properties

        private Color _hullColor = Colors.Gray;
        public Color HullColor
        {
            get
            {
                return _hullColor;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't change color once created");
                }

                _hullColor = value;
            }
        }

        private double _spinDegreesPerSecond = 0d;
        public double SpinDegreesPerSecond
        {
            get
            {
                return _spinDegreesPerSecond;
            }
            set
            {
                _spinDegreesPerSecond = value;
            }
        }

        #endregion

        #region Public Methods

        public void CreateStation(Map map, Point3D worldPosition)
        {
            //TODO:  Windows, lights

            _map = map;
            _positionWorld = worldPosition;
            _visuals = new List<ModelVisual3D>();

            MaterialGroup material = null;
            GeometryModel3D geometry = null;
            Model3DGroup models = new Model3DGroup();

            #region Interior Visuals

            // These are visuals that will stay oriented to the ship, but don't count in collision calculations

            #region Hull - Torus

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_hullColor)));
            material.Children.Add(new SpecularMaterial(Brushes.Silver, 75d));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetTorus(30, 10, _radius * .15, _radius);

            // Model Group
            models.Children.Add(geometry);

            #endregion
            #region Hull - Spine

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_hullColor)));
            material.Children.Add(new SpecularMaterial(Brushes.Silver, 75d));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, _radius * .07, _radius * .66);
            Transform3DGroup spineTransform2 = new Transform3DGroup();
            spineTransform2.Children.Add(new TranslateTransform3D(_radius * .1, 0, 0));
            spineTransform2.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90)));
            geometry.Transform = spineTransform2;

            // Model Group
            models.Children.Add(geometry);

            #endregion
            #region Hull - Spokes

            for (int cntr = 0; cntr < 3; cntr++)
            {
                // Material
                material = new MaterialGroup();
                material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_hullColor)));
                material.Children.Add(new SpecularMaterial(Brushes.Silver, 75d));

                // Geometry Model
                geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, _radius * .05, _radius * .9);
                Transform3DGroup spokeTransform = new Transform3DGroup();
                spokeTransform.Children.Add(new TranslateTransform3D(_radius * .45, 0, 0));     // the cylinder is built along the x axis, but is centered halfway
                spokeTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), cntr * 120d)));
                geometry.Transform = spokeTransform;

                // Model Group
                models.Children.Add(geometry);
            }

            #endregion
            #region Hull - Top inner

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, _hullColor, .25d))));
            material.Children.Add(new SpecularMaterial(Brushes.Silver, 75d));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, _radius * .11, _radius * .01);
            Transform3DGroup spokeTransform2 = new Transform3DGroup();
            spokeTransform2.Children.Add(new TranslateTransform3D((_radius * .51) - .5, 0, 0));     // the cylinder is built along the x axis, but is centered halfway
            spokeTransform2.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
            geometry.Transform = spokeTransform2;

            // Model Group
            models.Children.Add(geometry);

            #endregion
            #region Hull - Top outer

            //TODO: The two cylinders cause flicker, come up with the definition of a ring

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Black, _hullColor, .25d))));
            material.Children.Add(new SpecularMaterial(Brushes.Silver, 75d));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, _radius * .12, _radius * .0095);
            Transform3DGroup spokeTransform3 = new Transform3DGroup();
            spokeTransform3.Children.Add(new TranslateTransform3D((_radius * .5) - .5, 0, 0));     // the cylinder is built along the x axis, but is centered halfway
            spokeTransform3.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
            geometry.Transform = spokeTransform3;

            // Model Group
            models.Children.Add(geometry);

            #endregion

            #endregion

            #region Glass

            // Material
            //NOTE:  There is an issue with drawing objects inside a semitransparent object - they have to be added in order (so stuff added after a semitransparent won't be visible behind it)
            //Brush skinBrush = new SolidColorBrush(Color.FromArgb(25, 190, 240, 240));  // making the skin semitransparent, so you can see the components inside
            Brush skinBrush = new SolidColorBrush(Color.FromArgb(25, 220, 240, 240));  // making the skin semitransparent, so you can see the components inside

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(skinBrush));
            material.Children.Add(new SpecularMaterial(Brushes.White, 85d));     // more reflective (and white light)

            MaterialGroup backMaterial = new MaterialGroup();
            backMaterial.Children.Add(new DiffuseMaterial(skinBrush));
            backMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)), 10d));       // dark light, and not very reflective

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = backMaterial;
            geometry.Geometry = UtilityWPF.GetSphere(6, _radius * 1, _radius * 1, _radius * .25);

            // Model Group
            models.Children.Add(geometry);

            #endregion

            #region Exterior Visuals

            // There is a bug in WPF where visuals added after a semitransparent one won't show inside.  So if you want to add exterior
            // bits that aren't visible inside, this would be the place

            #endregion

            _mainTransform = new Transform3DGroup();
            _mainTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0)));
            _mainTransform.Children.Add(new TranslateTransform3D(worldPosition.ToVector()));

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();      // this is the expensive one.  make as few of these as I can get away with
            model.Content = models;
            model.Transform = _mainTransform;

            // Add to my list
            _visuals.Add(model);
            _map.Viewport.Children.Add(model);
            _map.AddItem(this);
        }

        public void WorldUpdating()
        {
            if (_map == null)
            {
                throw new InvalidOperationException("Need to call CreateStation first");
            }

            // Figure out the new angle
            DateTime currentTime = DateTime.Now;
            double elapsedSeconds = (currentTime - _lastAngleUpdateTime).TotalSeconds;

            _currentAngle += _spinDegreesPerSecond * elapsedSeconds;

            // Keep it between 0 and 360
            while (true)
            {
                if (_currentAngle < 0d)
                {
                    _currentAngle += 360d;
                }
                else if (_currentAngle > 360d)
                {
                    _currentAngle -= 360d;
                }
                else
                {
                    break;
                }
            }

            // All the station graphics are tied to this, so apply the new angle
            _mainTransform.Children.Clear();
            _mainTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), _currentAngle)));
            _mainTransform.Children.Add(new TranslateTransform3D(_positionWorld.ToVector()));

            // Remember the time
            _lastAngleUpdateTime = currentTime;
        }

        public decimal GetMineralValue(MineralType mineralType)
        {
            //TODO:  Slowly adjust my prices from the norm over time (maybe even have my own cargo bay that holds minerals)
            return Mineral.GetSuggestedValue(mineralType);
        }
        public decimal GetFuelValue()
        {
            return .5m;		// .5 credits per 1 unit fuel
        }

        #endregion
    }
}
