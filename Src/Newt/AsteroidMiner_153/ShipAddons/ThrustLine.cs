using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics_153;

namespace Game.Newt.AsteroidMiner_153.ShipAddons
{
    /// <summary>
    /// This will produce force, and draw its thrust line
    /// </summary>
    /// <remarks>
    /// I decided to separate a thruster and its thrust lines, since a single thruster will draw itself, and potentially contain
    /// multiple thrust lines
    /// </remarks>
    public class ThrustLine
    {
        #region Declaration Section

        private ModelVisual3D _model = null;

        /// <summary>
        /// The visual cube is always built along the x axis.  This rotates the cube to be along the initial force line
        /// </summary>
        private RotateTransform3D _initialRotate = null;

        private bool _isAddedToViewport = false;

        /// <summary>
        /// This is the force that will be applied to the body
        /// </summary>
        private Vector3D _forceDirection = new Vector3D();

        #endregion

        #region Constructor

        //TODO:  Support variable thrust (the force part is easy, the harder part is scaling the visual)

        public ThrustLine(Viewport3D viewport, SharedVisuals sharedVisuals, Vector3D forceDirection)
            : this(viewport, sharedVisuals, forceDirection, new Vector3D()) { }

        public ThrustLine(Viewport3D viewport, SharedVisuals sharedVisuals, Vector3D forceDirection, Vector3D localOffset)
        {
            this.Viewport = viewport;
            _forceDirection = forceDirection;
            _forceStrength = forceDirection.Length;       // this way they don't have to set this if they don't want
            this.BodyOffset = new TranslateTransform3D(localOffset);       // just setting it to something so it's not null

            #region Create Visual

            // I'll create the visual, but won't add it until they fire the thruster

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.Coral));
            materials.Children.Add(new SpecularMaterial(Brushes.Gold, 100d));

            // Geometry Model
            // Create a skinny 3D rectangle along the x axis
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = sharedVisuals.ThrustLineMesh;


            // Figure out how much to rotate the cube to be along the opposite of the force line.  I do the opposite, because
            // thruster flames shoot in the opposite direction that they're pushing
            Vector3D flameLine = forceDirection;
            flameLine.Negate();

            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, new Vector3D(1, 0, 0), flameLine);

            if (radians == 0d)
            {
                _initialRotate = null;
            }
            else
            {
                _initialRotate = new RotateTransform3D(new AxisAngleRotation3D(axis, Math3D.RadiansToDegrees(radians)));
            }

            //// Transform
            //Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(axis, Math3D.RadiansToDegrees(radians))));
            //transform.Children.Add(new TranslateTransform3D(from));




            // Model Visual
            _model = new ModelVisual3D();
            _model.Content = geometry;
            _model.Transform = new TranslateTransform3D();        // I won't do anything with this right now

            #endregion
        }

        #endregion

        #region Public Properties

        public Viewport3D Viewport
        {
            get;
            private set;
        }

        /// <summary>
        /// This is where the thrust line is located (and pointed) relative to the origin of the ship
        /// </summary>
        /// <remarks>
        /// This is not how to transform to world coords, just an offset within the ship's model coords
        /// </remarks>
        public Transform3D BodyOffset
        {
            get;
            set;
        }

        private bool _isFiring = false;
        public bool IsFiring
        {
            get
            {
                return _isFiring;
            }
            set
            {
                _isFiring = value;

                if (!_isFiring && _isAddedToViewport)     // if it is now firing, I will wait until Update is called before adding to the viewport - that way it can be transformed to world coords first
                {
                    // Remove from the viewport (quit showing the line)
                    this.Viewport.Children.Remove(_model);
                    _isAddedToViewport = false;
                }
            }
        }

        private double _forceStrength = 1d;
        public double ForceStrength
        {
            get
            {
                return _forceStrength;
            }
            set
            {
                _forceStrength = value;
            }
        }

        private double? _lineMaxLength = null;
        /// <summary>
        /// This is how long to draw the line when 100% force is applied
        /// </summary>
        public double LineMaxLength
        {
            get
            {
                // If it's not explicitely set, I'll just use the force length
                if (_lineMaxLength == null)
                {
                    return _forceStrength;
                }
                else
                {
                    return _lineMaxLength.Value;
                }
            }
            set
            {
                _lineMaxLength = value;
            }
        }

        private FuelTank _fuelTank = null;
        public FuelTank FuelTank
        {
            get
            {
                return _fuelTank;
            }
            set
            {
                _fuelTank = value;
            }
        }

        private double _fuelToThrustRatio = .001;
        public double FuelToThrustRatio
        {
            get
            {
                return _fuelToThrustRatio;
            }
            set
            {
                _fuelToThrustRatio = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Call this to position the flame visual in the proper place within the world (it's safe to call, even if the thruster
        /// isn't firing)
        /// </summary>
        /// <param name="worldTransform">This describes how to transfrom from model coords to world coords</param>
        public void DrawVisual(double percentMax, Transform3D worldTransform)
        {
            if (!_isFiring)        // the property set will make sure the visual is no longer known to the viewport
            {
                return;
            }

            // Figure out how to place it in world coords
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(this.LineMaxLength * percentMax, 1d, 1d));
            if (_initialRotate != null)
            {
                transform.Children.Add(_initialRotate);
            }
            transform.Children.Add(this.BodyOffset);
            transform.Children.Add(worldTransform);

            // Show the visual in the proper location
            _model.Transform = transform;

            // Make sure the viewport is showing this
            if (!_isAddedToViewport)
            {
                this.Viewport.Children.Add(_model);
                _isAddedToViewport = true;
            }
        }
        /// <summary>
        /// This overload lets you pass in a transform to steer the thruster (in local coords)
        /// </summary>
        public void DrawVisual(double percentMax, Transform3D worldTransform, Transform3D localSteeringTransform)
        {
            if (!_isFiring)        // the property set will make sure the visual is no longer known to the viewport
            {
                return;
            }

            // Combine the two, and call my other overload
            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(localSteeringTransform);
            transformGroup.Children.Add(worldTransform);

            DrawVisual(percentMax, transformGroup);
        }

        /// <summary>
        /// Call this to apply the thruster force to the physics body
        /// </summary>
        public void ApplyForce(double percentMax, Transform3D worldTransform, BodyForceEventArgs e)
        {
            if (!_isFiring)        // the property set will make sure the visual is no longer known to the viewport
            {
                return;
            }

            double actualForce = _forceStrength * percentMax;
            if (_fuelTank != null)
            {
                #region Use Fuel

                if (_fuelTank.QuantityCurrent > 0d)
                {
                    double fuelToUse = actualForce * e.ElapsedTime * _fuelToThrustRatio;

                    double fuelUnused = _fuelTank.RemoveQuantity(fuelToUse, false);
                    if (fuelUnused > 0d)
                    {
                        // Not enough fuel, reduce the amount of force
                        actualForce -= fuelUnused / (e.ElapsedTime * _fuelToThrustRatio);
                    }
                }
                else
                {
                    actualForce = 0d;
                }

                #endregion
            }

            if (actualForce == 0d)
            {
                // No force to apply
                //TODO:  Play a clicking sound, or some kind of error tone
                //TODO:  Don't show the flames
                return;
            }

            // Figure out how to place it in world coords
            Transform3DGroup transform = new Transform3DGroup();       // I don't use _initialRotate, because that's for the visual (it's always created along the X axis, but _force is already stored correctly)
            transform.Children.Add(this.BodyOffset);
            transform.Children.Add(worldTransform);

            Vector3D positionOnBodyWorld = transform.Transform(new Point3D(0, 0, 0)).ToVector();    // note that I have to use a point (transform acts different on points than vectors)
            Vector3D deltaForceWorld = transform.Transform(_forceDirection);
            deltaForceWorld.Normalize();
            deltaForceWorld *= actualForce;

            // Apply the force
            e.AddForceAtPoint(deltaForceWorld, positionOnBodyWorld);
        }
        public void ApplyForce(double percentMax, Transform3D worldTransform, Transform3D localSteeringTransform, BodyForceEventArgs e)
        {
            if (!_isFiring)        // the property set will make sure the visual is no longer known to the viewport
            {
                return;
            }

            // Combine the two, and call my other overload
            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(localSteeringTransform);
            transformGroup.Children.Add(worldTransform);

            ApplyForce(percentMax, transformGroup, e);
        }

        #endregion
    }
}
