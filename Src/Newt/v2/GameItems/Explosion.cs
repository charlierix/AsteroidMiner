using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems
{
    //TODO:  The explosion class is only pushing on the center of mass.  It needs to account for the geometry of what it's hitting - make this a generic helper, I also want to the
    // helper to calculate fluid friction along arbitrary directions (to model fish and airplanes) - the explosion is like a point light source, and fluid is more of a directional light

    #region class: ExplosionWithVisual

    public class ExplosionWithVisual : Explosion, IDisposable
    {
        #region Declaration Section

        private Viewport3D _viewport = null;

        // This is the expanding sphere
        private ModelVisual3D _visual = null;
        private DiffuseMaterial _material = null;

        // Every explosion needs a light
        private PointLight _pointLight = null;

        private bool _isExplode = false;
        private double _visualStartRadius = 0d;
        private Color _baseColor = Colors.Transparent;

        #endregion

        #region Constructor

        /// <summary>
        /// This overload will keep the explosion relative to the body.
        /// NOTE:  This class won't touch body.Visuals
        /// </summary>
        public ExplosionWithVisual(Body body, double waveSpeed, double forceAtCenter, double maxRadius, Viewport3D viewport, double visualStartRadius)
            : base(body, waveSpeed, forceAtCenter, maxRadius)
        {
            _viewport = viewport;
            _visualStartRadius = visualStartRadius;
            CreateVisual();
        }
        public ExplosionWithVisual(Point3D centerPoint, double waveSpeed, double forceAtCenter, double maxRadius, Viewport3D viewport, double visualStartRadius)
            : base(centerPoint, waveSpeed, forceAtCenter, maxRadius)
        {
            _viewport = viewport;
            _visualStartRadius = visualStartRadius;
            CreateVisual();
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
            if (disposing)
            {
                RemoveVisual();
            }
        }

        #endregion

        #region Public Properties

        public Visual3D Visual
        {
            get
            {
                return _visual;
            }
        }

        #endregion

        #region Overrides

        public override bool Update(double elapsedTime)
        {
            bool retVal = base.Update(elapsedTime);

            if (retVal)
            {
                RemoveVisual();
            }
            else
            {
                #region Update Visual

                // Transparency
                double transparency = UtilityCore.GetScaledValue_Capped(0d, 1d, _visualStartRadius, this.MaxRadius * .75d, this.Radius);		// I want it to become invisible sooner
                _material.Brush = new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Transparent, _baseColor, transparency));

                // Scale (this is what Body.OnBodyMoved does, plus scale)
                //NOTE:  The radius gets huge, but the force drops off quickly, so it doesn't look right if I scale it to full size
                double scale = UtilityCore.GetScaledValue_Capped(1d, (this.MaxRadius * .05d) / _visualStartRadius, _visualStartRadius, this.MaxRadius, this.Radius);

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                if (this.Body == null)
                {
                    transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));
                }
                else
                {
                    transform.Children.Add(new MatrixTransform3D(this.Body.OffsetMatrix));
                }

                _visual.Transform = transform;

                //NOTE:  The shell is smaller than this.Radius (once it overtakes _visualStartRadius)
                if (this.Radius > _visualStartRadius * 2)
                {
                    _pointLight.Range = this.Radius;
                }

                #endregion
            }

            // Exit Function
            return retVal;
        }

        #endregion

        #region Private Methods

        private void CreateVisual()
        {
            //NOTE:  I am not adding this to this.Body.Visuals, because I need to scale the visual as well (also, this class removes the visual when update reaches zero)

            _isExplode = _forceAtCenter > 0d;

            // Figure out colors
            Color reflectColor, lightColor;
            if (_isExplode)
            {
                // Explode
                _baseColor = Colors.Coral;
                reflectColor = Colors.Gold;
                lightColor = Color.FromArgb(128, 252, 255, 136);
            }
            else
            {
                // Implode
                _baseColor = Colors.CornflowerBlue;
                reflectColor = Colors.DarkOrchid;
                lightColor = Color.FromArgb(128, 183, 190, 255);
            }

            // Shell
            _visual = GetWPFModel(out _material, _baseColor, reflectColor, 30d, _visualStartRadius, this.Position);		// I want to keep this pretty close to the original method, so I'll let it build a full ModelVisual, and I'll pull the model out of that

            Model3DGroup models = new Model3DGroup();
            models.Children.Add(_visual.Content);

            // Light
            _pointLight = new PointLight();
            _pointLight.Color = lightColor;
            _pointLight.Range = _visualStartRadius * 2;		// radius will eventually overtake this, then it will be set to radius
            _pointLight.QuadraticAttenuation = .33;
            models.Children.Add(_pointLight);

            // Now store the group instead
            _visual.Content = models;

            _viewport.Children.Add(_visual);
        }
        private void RemoveVisual()
        {
            if (_visual != null)
            {
                _viewport.Children.Remove(_visual);
                _visual = null;
            }
        }

        private static ModelVisual3D GetWPFModel(out DiffuseMaterial bodyMaterial, Color color, Color reflectionColor, double reflectionIntensity, double radius, Point3D position)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
            materials.Children.Add(bodyMaterial);
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, radius, radius, radius);

            // Transform
            TranslateTransform3D transform = new TranslateTransform3D(position.ToVector());

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region class: Explosion

    /// <summary>
    /// This models an explosion over time
    /// </summary>
    /// <remarks>
    /// The radius of the shockwave is a constant velocity (very fast, but constant)
    /// 
    /// I assume the force of the wave is k/dist^2
    /// 
    /// Not sure, but I assume the forces behind the shockwave are much smaller, so it's a moving wall of force
    /// 
    /// This would make a good base class.  This is a spherical explosion (not a shaped charge) that doesn't get deformed by the bodies that
    /// it hits.
    /// 
    /// It looks like if you are going to have a shaped charge, forces increase along the wrinkle lines?
    /// </remarks>
    public class Explosion
    {
        #region Declaration Section

        protected double _waveSpeed = 0d;
        protected double _forceAtCenter = 0d;

        #endregion

        #region Constructor

        /// <summary>
        /// NOTE:  You will probably want to make the projectile non collidable when it becomes the source of a collision (swap its material with one
        /// that doesn't collide)
        /// </summary>
        public Explosion(Body body, double waveSpeed, double forceAtCenter, double maxRadius)
        {
            this.Body = body;

            _waveSpeed = waveSpeed;
            _forceAtCenter = forceAtCenter;
            _maxRadius = maxRadius;
        }
        public Explosion(Point3D centerPoint, double waveSpeed, double forceAtCenter, double maxRadius)
        {
            _position = centerPoint;

            _waveSpeed = waveSpeed;
            _forceAtCenter = forceAtCenter;
            _maxRadius = maxRadius;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is the body that exploded (could be null if this is an arbitrarily generated explosion)
        /// </summary>
        public Body Body
        {
            get;
            private set;
        }

        private Point3D? _position = null;
        public Point3D Position
        {
            get
            {
                if (_position != null)
                {
                    // They used the constructor that doesn't take a body, and instead takes a position directly
                    return _position.Value;
                }
                else
                {
                    // They used the constructor that takes a body.  Use its position (it could still be moving)
                    return this.Body.Position;
                }
            }
            set
            {
                if (_position == null)
                {
                    throw new InvalidOperationException("Position can only be set when this class used the constructor that takes an explicit position");
                }

                _position = value;
            }
        }

        private double _radius = 0d;
        /// <summary>
        /// This is the maximum radius that this explosion currently reaches (changes every time update is called)
        /// </summary>
        public double Radius
        {
            get
            {
                return _radius;
            }
        }

        private double _maxRadius = 0d;
        public double MaxRadius
        {
            get
            {
                return _maxRadius;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This will whack the body by the explosion.
        /// NOTE:  Only call from within the Body.ApplyForceAndTorque callback
        /// </summary>
        public void ApplyForceToBody(Body body)
        {
            //TODO:  This needs to add some rotation.  Otherwise it just looks odd (everything is too even)

            Vector3D explosiveForce = GetForceAtPoint(body.Position);
            if (explosiveForce.X != 0d || explosiveForce.Y != 0d || explosiveForce.Z != 0d)
            {
                body.AddForce(explosiveForce);
            }
        }
        /// <summary>
        /// This returns the force that this explosion generates on the point (point is in world coords)
        /// </summary>
        /// <remarks>
        /// This is over simplified.  The force should really depend on the shape of the object, and the object should also interfere with the
        /// explosion's shock wave (reflect it back like a water ripple bouncing off a wall)
        /// </remarks>
        public Vector3D GetForceAtPoint(Point3D point)
        {
            Vector3D lineFromCenter = point - this.Position;

            double distanceSquared = lineFromCenter.LengthSquared;

            if (distanceSquared > _radius * _radius || distanceSquared == 0d)
            {
                return new Vector3D(0, 0, 0);
            }
            //else if (distance < _radius * .666667)		// commenting this, because fast moving waves will completly miss bodies which looks really bad
            //{
            //    // The force is only felt in the shockwave.  Once the wave has passed there is nearly no force
            //    //TODO:  Figure out what this force is.  Every site I visit talks about the force of an explosion, but not the force within this bubble behind
            //    // the shockwave.  Looking at slow motion video of explosions, it seems pretty still, so I'm just going to return zero
            //    //
            //    // It's a complete guess, but I'm going to set the depth of this wave at 33% of the current radius
            //    return new Vector3D(0, 0, 0);
            //}

            double force = _forceAtCenter / (4d * Math.PI * distanceSquared);
            if (double.IsInfinity(force) || double.IsNaN(force) || force > _forceAtCenter)
            {
                force = _forceAtCenter;
            }

            lineFromCenter.Normalize();
            lineFromCenter = lineFromCenter * force;

            return lineFromCenter;
        }

        /// <summary>
        /// This updates the explosion (which changes the current force and radius).  Returns true when the explosion is finished
        /// </summary>
        public virtual bool Update(double elapsedTime)
        {
            _radius += elapsedTime * _waveSpeed;

            if (_radius > _maxRadius)
            {
                // This explosion has expired.  I'm not going to bother cleaning up member variables.  The caller should remove this class anyway
                return true;
            }

            return false;
        }

        #endregion
    }

    #endregion
}
