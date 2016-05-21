using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.MapParts
{
    public class SpaceStation : IMapObject, IPartUpdatable
    {
        #region Declaration Section

        private DiffuseMaterial _forceField_DiffuseMaterial = null;
        private EmissiveMaterial _forceField_EmissiveFront = null;
        private EmissiveMaterial _forceField_EmissiveRear = null;

        private DispatcherTimer _timer = null;

        private double _forceFieldOpacity = 0d;

        #endregion

        #region Constructor

        public SpaceStation(Point3D position, World world, int materialID, Quaternion orientation)
        {
            //TODO:  Windows, lights

            MaterialGroup material = null;
            GeometryModel3D geometry = null;
            Model3DGroup models = new Model3DGroup();

            // These are random, so pull them once
            Color hullColor = WorldColors.SpaceStationHull;
            SpecularMaterial hullSpecular = WorldColors.SpaceStationHullSpecular;

            double radius = 8;
            this.Radius = radius * 1.25;		// this is the extremes of the force field
            double mass = 10000;

            #region Interior Visuals

            // These are visuals that will stay oriented to the ship, but don't count in collision calculations

            #region Hull - Torus

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(hullColor)));
            material.Children.Add(hullSpecular);

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetTorus(30, 10, radius * .15, radius);

            // Model Group
            models.Children.Add(geometry);

            #endregion
            #region Hull - Spine

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(hullColor)));
            material.Children.Add(hullSpecular);

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, radius * .075, radius * .66);
            Transform3DGroup spineTransform2 = new Transform3DGroup();
            spineTransform2.Children.Add(new TranslateTransform3D(radius * .1, 0, 0));
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
                material.Children.Add(new DiffuseMaterial(new SolidColorBrush(hullColor)));
                material.Children.Add(hullSpecular);

                // Geometry Model
                geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, radius * .05, radius * .9);
                Transform3DGroup spokeTransform = new Transform3DGroup();
                spokeTransform.Children.Add(new TranslateTransform3D(radius * .45, 0, 0));     // the cylinder is built along the y axis, but is centered halfway
                spokeTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), cntr * 120d)));
                geometry.Transform = spokeTransform;

                // Model Group
                models.Children.Add(geometry);
            }

            #endregion
            #region Hull - Top inner

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, hullColor, .25d))));
            material.Children.Add(hullSpecular);

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, radius * .11, radius * .01);
            Transform3DGroup spokeTransform2 = new Transform3DGroup();
            spokeTransform2.Children.Add(new TranslateTransform3D((radius * .51) - .5, 0, 0));     // the cylinder is built along the x axis, but is centered halfway
            spokeTransform2.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
            geometry.Transform = spokeTransform2;

            // Model Group
            models.Children.Add(geometry);

            #endregion
            #region Hull - Top outer

            //TODO: The two cylinders cause flicker, come up with the definition of a ring (or do some texture mapping - if so, see if the texture can be vector graphics)

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Black, hullColor, .25d))));
            material.Children.Add(hullSpecular);

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, radius * .12, radius * .0095);
            Transform3DGroup spokeTransform3 = new Transform3DGroup();
            spokeTransform3.Children.Add(new TranslateTransform3D((radius * .5) - .5, 0, 0));     // the cylinder is built along the y axis, but is centered halfway
            spokeTransform3.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
            geometry.Transform = spokeTransform3;

            // Model Group
            models.Children.Add(geometry);

            #endregion

            #endregion

            #region Glass

            // Material
            //NOTE:  There is an issue with drawing objects inside a semitransparent object - they have to be added in order (so stuff added after a semitransparent won't be visible behind it)
            Brush skinBrush = new SolidColorBrush(WorldColors.SpaceStationGlass);  // the skin is semitransparent, so you can see the components inside

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(skinBrush));
            material.Children.Add(WorldColors.SpaceStationGlassSpecular_Front);     // more reflective (and white light)

            MaterialGroup backMaterial = new MaterialGroup();
            backMaterial.Children.Add(new DiffuseMaterial(skinBrush));
            backMaterial.Children.Add(WorldColors.SpaceStationGlassSpecular_Back);       // dark light, and not very reflective

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = backMaterial;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(6, radius, radius, radius * .25);

            // Model Group
            models.Children.Add(geometry);

            #endregion

            #region Exterior Visuals

            // There is a bug in WPF where visuals added after a semitransparent one won't show inside.  So if you want to add exterior
            // bits that aren't visible inside, this would be the place

            #endregion

            #region Force Field

            Vector3D forceFieldSize = new Vector3D(radius * 1.25, radius * 1.25, radius * .75);

            // Material
            _forceField_DiffuseMaterial = new DiffuseMaterial(null);//Brushes.Transparent);		// the momentarily brush will change when there is a collision
            _forceField_EmissiveFront = new EmissiveMaterial(null);//Brushes.Transparent);
            _forceField_EmissiveRear = new EmissiveMaterial(null);//Brushes.Transparent);

            material = new MaterialGroup();
            material.Children.Add(_forceField_DiffuseMaterial);
            material.Children.Add(_forceField_EmissiveFront);

            backMaterial = new MaterialGroup();
            backMaterial.Children.Add(_forceField_DiffuseMaterial);
            backMaterial.Children.Add(_forceField_EmissiveRear);

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = backMaterial;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(6, forceFieldSize.X, forceFieldSize.Y, forceFieldSize.Z);

            // Model Group
            models.Children.Add(geometry);

            #endregion

            this.Model = models;

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
            model.Content = models;

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            using (CollisionHull hull = CollisionHull.CreateSphere(world, 0, forceFieldSize, null))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { model });
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = .01f;
                this.PhysicsBody.AngularDamping = new Vector3D(.001f, .001f, .001f);
            }

            #endregion

            this.CreationTime = DateTime.UtcNow;
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

        public bool IsDisposed
        {
            get
            {
                return this.PhysicsBody.IsDisposed;
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
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
            Vector3D axis = this.PhysicsBody.DirectionToWorld(new Vector3D(0, 0, 1));

            this.PhysicsBody.AngularVelocity = axis * SpinDegreesPerSecond;
        }
        public void Update_AnyThread(double elapsedTime)
        {
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 5;
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

        public double SpinDegreesPerSecond
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Whenever the station collides with something, the force field shows full intensity, then fades to zero (purely visual)
        /// </summary>
        public void Collided(MaterialCollisionArgs e)
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
                _timer.Tick += new EventHandler(Timer_Tick);
            }

            _forceFieldOpacity = 1d;
            _timer.Start();
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            const double FIELDDURATION = 250d;		// It should reach zero after 500 milliseconds

            if (_forceFieldOpacity > 0d)
            {
                #region Still Active

                // Set the colors
                _forceField_DiffuseMaterial.Brush = new SolidColorBrush(UtilityWPF.AlphaBlend(WorldColors.SpaceStationForceField, Colors.Transparent, _forceFieldOpacity));
                _forceField_EmissiveFront.Brush = new SolidColorBrush(UtilityWPF.AlphaBlend(WorldColors.SpaceStationForceFieldEmissive_Front, Colors.Transparent, _forceFieldOpacity));
                _forceField_EmissiveRear.Brush = new SolidColorBrush(UtilityWPF.AlphaBlend(WorldColors.SpaceStationForceFieldEmissive_Back, Colors.Transparent, _forceFieldOpacity));

                // Diminish the opacity for the next tick
                if (_timer.Interval.TotalMilliseconds > FIELDDURATION)
                {
                    _forceFieldOpacity = -1d;
                }
                else
                {
                    _forceFieldOpacity -= (1d / FIELDDURATION) * _timer.Interval.TotalMilliseconds;
                }

                #endregion
            }
            else
            {
                #region Set transparent

                _forceField_DiffuseMaterial.Brush = null; // Brushes.Transparent;
                _forceField_EmissiveFront.Brush = null;// Brushes.Transparent;
                _forceField_EmissiveRear.Brush = null;// Brushes.Transparent;

                _timer.Stop();

                #endregion
            }
        }

        #endregion
    }
}
