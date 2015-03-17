using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1;

namespace Game.Newt.v1.AsteroidMiner1
{
    public class Asteroid : IMapObject
    {
        #region Declaration Section

        private Map _map = null;

        private bool _hasSetInitialVelocity = false;

        // The physics engine keeps this one synced, no need to transform it every frame
        // There's only one model, so I just won't sync anything on update
        //private ModelVisual3D _physicsModel = null;

        #endregion

        #region Constructor

        public Asteroid()
        {
        }

        #endregion

        #region IMapObject Members

        private ConvexBody3D _physicsBody = null;
        public ConvexBody3D PhysicsBody
        {
            get
            {
                return _physicsBody;
            }
        }

        private List<ModelVisual3D> _visuals = new List<ModelVisual3D>();
        public IEnumerable<ModelVisual3D> Visuals3D
        {
            get
            {
                return _visuals;
            }
        }

        public Point3D PositionWorld
        {
            get
            {
                return _physicsBody.PositionToWorld(_physicsBody.CenterOfMass);
            }
        }
        public Vector3D VelocityWorld
        {
            get
            {
                //return _physicsBody.Velocity;
                return _physicsBody.VelocityCached;		// this one is safer (can be called at any time, not just within the apply force/torque event)
            }
        }

        private double _radius = 1d;
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
                    throw new InvalidOperationException("Can't set the radius after the asteroid is created");
                }

                _radius = value;
            }
        }

        #endregion

        #region Public Properties

        private double _mass = 1d;
        public double Mass
        {
            get
            {
                return _mass;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the mass after the asteroid is created");
                }

                _mass = value;
            }
        }

        private Vector3D _initialVelocity = new Vector3D(0, 0, 0);
        public Vector3D InitialVelocity
        {
            get
            {
                return _initialVelocity;
            }
            set
            {
                if (_map != null)
                {
                    throw new InvalidOperationException("Can't set the initial velocity after the asteroid is created");
                }

                _initialVelocity = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the properties, then call create
        /// NOTE:  This adds itself to the viewport and world.  In the future, that should be handled by the caller
        /// </summary>
        public void CreateAsteroid(MaterialManager materialManager, Map map, SharedVisuals sharedVisuals, Vector3D position)
        {
            _map = map;

            #region WPF Model

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.Gray));
            materials.Children.Add(new SpecularMaterial(Brushes.Silver, 100d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = sharedVisuals.AsteroidMesh;
            geometry.Transform = new ScaleTransform3D(_radius, _radius, _radius);

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = new TranslateTransform3D(position);

            // Add to the viewport
            _visuals.Add(model);
            _map.Viewport.Children.Add(model);

            #endregion

            #region Physics Body

            // Make a physics body that represents this shape
            _physicsBody = new ConvexBody3D(_map.World, model);
            //_physicsBody = new ConvexBody3D(_map.World, model, ConvexBody3D.CollisionShape.Sphere, _radius, _radius, _radius);   // for some reason, this is throwing an exception.  debug it

            _physicsBody.MaterialGroupID = materialManager.AsteroidMaterialID;

            _physicsBody.NewtonBody.UserData = this;

            _physicsBody.Mass = Convert.ToSingle(_mass);

            _physicsBody.LinearDamping = .01f;
            _physicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);
            _physicsBody.Override2DEnforcement_Rotation = true;

            _physicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

            #endregion

            // Add to the map
            _map.AddItem(this);
        }

        public void WorldUpdating()
        {
            // Nothing to do
        }

        #endregion

        #region Event Listeners

        private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
        {
            if (sender != _physicsBody)
            {
                return;
            }

            if (!_hasSetInitialVelocity)
            {
                e.AddImpulse(_initialVelocity, new Vector3D(0, 0, 0));
                _hasSetInitialVelocity = true;

                // No need to listen to this even anymore
                _physicsBody.ApplyForce -= new BodyForceEventHandler(Body_ApplyForce);
            }
        }

        #endregion
    }
}
