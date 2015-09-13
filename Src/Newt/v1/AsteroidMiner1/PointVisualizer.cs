using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1;

namespace Game.Newt.v1.AsteroidMiner1
{
    /// <summary>
    /// This class is used to show stats on a point.  It can draw a blip for position, and lines for velocity/acceleration
    /// </summary>
    /// <remarks>
    /// This class can be used 3 different ways, depending on which constructor you call.  Once instantiated, you're locked into
    /// using it that way (can't switch from using custom to physics body)
    /// 
    /// I probably should have used a base and derived classes, but I prefer it flatter
    /// </remarks>
    public class PointVisualizer
    {
        #region Class: ItemProperties

        private class ItemProperties
        {
            public ModelVisual3D Model = null;
            public bool IsPoint = true;
            public Color Color = Colors.White;
            public double SizeMultiplier = 1d;
        }

        #endregion

        #region Declaration Section

        private const int INDEX_POSITION = 0;
        private const int INDEX_VELOCITY = 1;
        private const int INDEX_ACCELERATION = 2;

        private Viewport3D _viewport = null;
        private SharedVisuals _sharedVisuals = null;

        /// <summary>
        /// If true, then the array is loaded with position/velocity/acceleration
        /// </summary>
        private bool _isPosVelAcc = true;

        /// <summary>
        /// This is the body that this class is showing.  Only populated if you use the corresponding constructor overload
        /// </summary>
        private ConvexBody3D _physicsBody = null;

        private ItemProperties[] _items = null;

        // These are used by the 2 constructor overloads that don't take int
        //private ModelVisual3D _dotModel = null;
        //private ModelVisual3D _velocityModel = null;
        //private ModelVisual3D _accelerationModel = null;

        #endregion

        #region Constructor

        public PointVisualizer(Viewport3D viewport, SharedVisuals sharedVisuals)
        {
            _viewport = viewport;
            _sharedVisuals = sharedVisuals;

            _items = new ItemProperties[3];

            // Position
            _items[INDEX_POSITION] = new ItemProperties();
            _items[INDEX_POSITION].IsPoint = true;
            _items[INDEX_POSITION].Color = Colors.Magenta;
            _items[INDEX_POSITION].SizeMultiplier = 1d;

            // Velocity
            _items[INDEX_VELOCITY] = new ItemProperties();
            _items[INDEX_VELOCITY].IsPoint = false;
            _items[INDEX_VELOCITY].Color = Colors.Chartreuse;
            _items[INDEX_VELOCITY].SizeMultiplier = 1d;

            // Acceleration
            _items[INDEX_ACCELERATION] = new ItemProperties();
            _items[INDEX_ACCELERATION].IsPoint = false;
            _items[INDEX_ACCELERATION].Color = Colors.Gold;
            _items[INDEX_ACCELERATION].SizeMultiplier = 1d;
        }

        public PointVisualizer(Viewport3D viewport, SharedVisuals sharedVisuals, ConvexBody3D physicsBody)
            : this(viewport, sharedVisuals)
        {
            _physicsBody = physicsBody;
        }

        public PointVisualizer(Viewport3D viewport, SharedVisuals sharedVisuals, int numCustomVisuals)
            : this(viewport, sharedVisuals)
        {
            // The common construct set up _items for pop/vel/acc, but I'll wipe it, and wait for the user to call this.SetCustomItemProperties
            _items = new ItemProperties[numCustomVisuals];

            _isPosVelAcc = false;
        }

        #endregion

        #region Public Properties

        private bool _showPosition = false;
        public bool ShowPosition
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _showPosition;
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                if (_showPosition == value)
                {
                    // No change, just leave
                    return;
                }

                // Store the new value
                _showPosition = value;

                ClearModels();		// just clear them and wait for the next update to get everything right
            }
        }
        private bool _showVelocity = true;
        public bool ShowVelocity
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _showVelocity;
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                if (_showVelocity == value)
                {
                    return;
                }

                _showVelocity = value;

                ClearModels();		// just clear them and wait for the next update to get everything right
            }
        }
        private bool _showAcceleration = true;
        public bool ShowAcceleration
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _showAcceleration;
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                if (_showAcceleration == value)
                {
                    return;
                }

                _showAcceleration = value;

                ClearModels();		// just clear them and wait for the next update to get everything right
            }
        }

        public Color PositionColor
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _items[INDEX_POSITION].Color;
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                _items[INDEX_POSITION].Color = value;

                ClearModels();
            }
        }
        public Color VelocityColor
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _items[INDEX_VELOCITY].Color;
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                _items[INDEX_VELOCITY].Color = value;

                ClearModels();
            }
        }
        public Color AccelerationColor
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _items[INDEX_ACCELERATION].Color;
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                _items[INDEX_ACCELERATION].Color = value;

                ClearModels();
            }
        }

        public double PositionRadius
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _items[INDEX_POSITION].SizeMultiplier;
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                _items[INDEX_POSITION].SizeMultiplier = value;

                ClearModels();
            }
        }

        public double VelocityAccelerationLengthMultiplier
        {
            get
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                return _items[INDEX_VELOCITY].SizeMultiplier;		// both this and acceleration are the same, so just pull from one of them
            }
            set
            {
                if (!_isPosVelAcc)
                {
                    throw new InvalidOperationException("This can't be used when the custom count constructor was called");
                }

                _items[INDEX_VELOCITY].SizeMultiplier = value;
                _items[INDEX_ACCELERATION].SizeMultiplier = value;

                ClearModels();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is used if you call the constructor that takes an integer.  Don't use the public properties, call this method instead for each of the items
        /// to be drawn
        /// </summary>
        /// <param name="isPoint">
        /// True:  This will draw a sphere
        /// False:  This will draw a line
        /// </param>
        /// <param name="sizeMultiplier">
        /// If isPoint is true, this is the radius.  If false, this is the line length multiplier
        /// </param>
        public void SetCustomItemProperties(int index, bool isPoint, Color color, double sizeMultiplier)
        {
            if (_isPosVelAcc)
            {
                throw new InvalidOperationException("This can't be used when the position/velocity/acceleration constructors were called");
            }

            ClearModels();

            _items[index] = new ItemProperties();
            _items[index].IsPoint = isPoint;
            _items[index].Color = color;
            _items[index].SizeMultiplier = sizeMultiplier;

            // The first call to update will initialize the models
        }

        /// <summary>
        /// This is the overload you use when you call the constructor with no extra params
        /// </summary>
        public void Update(Point3D positionWorld, Vector3D velocityWorld, Vector3D accelerationWorld)
        {
            if (!_isPosVelAcc)
            {
                throw new InvalidOperationException("This can't be used when the custom count constructor was called");
            }

            //if (_physicsBody != null)
            //{
            //    throw new InvalidOperationException("This overload of update (with params) can't be called when there is a physics body");
            //}

            if (_items[INDEX_POSITION].Model == null)
            {
                InitializeModels();
            }

            // Position
            if (_showPosition)
            {
                _items[INDEX_POSITION].Model.Transform = new TranslateTransform3D(positionWorld.ToVector());
            }

            // Velocity
            if (_showVelocity)
            {
                _items[INDEX_VELOCITY].Model.Transform = GetLineTransform(positionWorld, velocityWorld, _items[INDEX_VELOCITY].SizeMultiplier);
            }

            // Acceleration
            if (_showAcceleration)
            {
                _items[INDEX_ACCELERATION].Model.Transform = GetLineTransform(positionWorld, accelerationWorld, _items[INDEX_ACCELERATION].SizeMultiplier);
            }
        }
        /// <summary>
        /// This is the overload you use when you call the constructor with the physics body
        /// </summary>
        public void Update()
        {
            if (_physicsBody == null)
            {
                throw new InvalidOperationException("This overload of update (no params) can't be called when there is no physics body");
            }

            Update(_physicsBody.PositionToWorld(_physicsBody.CenterOfMass), _physicsBody.VelocityCached, _showAcceleration ? _physicsBody.AccelerationCached : new Vector3D(0, 0, 0));		// only request acceleration if needed (the body has to do math if it's requested)
        }
        /// <summary>
        /// This is the overload you use when you call the constructor with custom items count (this must be called for each index)
        /// This is when the item is a point
        /// </summary>
        public void Update(int index, Point3D positionWorld)
        {
            if (_isPosVelAcc)
            {
                throw new InvalidOperationException("This can't be used when the position/velocity/acceleration constructors were called");
            }
            else if (_items[index] == null)
            {
                throw new InvalidOperationException("Update can't be called until SetCustomItemProperties has been called for this index");
            }
            else if (!_items[index].IsPoint)
            {
                throw new InvalidOperationException("This overload only works with a point");
            }

            if (_items[index].Model == null)
            {
                InitializeModels();
            }

            _items[index].Model.Transform = new TranslateTransform3D(positionWorld.ToVector());
        }
        /// <summary>
        /// This is the overload you use when you call the constructor with custom items count (this must be called for each index)
        /// This is when the item is a line (lineVectorWorld is a vector coming out of positionWorld, not a second position)
        /// </summary>
        public void Update(int index, Point3D positionWorld, Vector3D lineVectorWorld)
        {
            if (_isPosVelAcc)
            {
                throw new InvalidOperationException("This can't be used when the position/velocity/acceleration constructors were called");
            }
            else if (_items[index] == null)
            {
                throw new InvalidOperationException("Update can't be called until SetCustomItemProperties has been called for this index");
            }
            else if (_items[index].IsPoint)
            {
                throw new InvalidOperationException("This overload only works with a line");
            }

            if (_items[index].Model == null)
            {
                InitializeModels();
            }

            _items[index].Model.Transform = GetLineTransform(positionWorld, lineVectorWorld, _items[index].SizeMultiplier);
        }

        public void HideAll()
        {
            if (_isPosVelAcc)
            {
                this.ShowPosition = false;
                this.ShowVelocity = false;
                this.ShowAcceleration = false;
            }
            else
            {
                // There's no way to hide the custom items.  Just clear them (the next call to update will put them back)
                ClearModels();
            }
        }

        #endregion

        #region Private Methods

        private Transform3DGroup GetLineTransform(Point3D positionWorld, Vector3D vectorWorld, double lengthMultiplier)
        {
            Transform3DGroup retVal = new Transform3DGroup();
            retVal.Children.Add(new ScaleTransform3D(vectorWorld.Length * lengthMultiplier, 1d, 1d));

            // Rotation
            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, new Vector3D(1, 0, 0), vectorWorld);		// the original mesh is a length of 1 along the x axis

            if (radians != 0d)
            {
                retVal.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians))));
            }

            // Translation
            retVal.Children.Add(new TranslateTransform3D(positionWorld.ToVector()));

            // Exit Function
            return retVal;
        }

        private void InitializeModels()
        {
            #region Build Models

            for (int cntr = 0; cntr < _items.Length; cntr++)
            {
                if (_items[cntr] == null)
                {
                    //throw new ApplicationException("Index " + cntr.ToString() + " hasn't been set up yet");
                    continue;		// I'll be more forgiving, and only throw an exception if they are updating this index
                }

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(_items[cntr].Color)));
                materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

                if (_items[cntr].IsPoint)
                {
                    #region Point

                    // Geometry Model
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = _sharedVisuals.PointVisualizerDotMesh;
                    geometry.Transform = new ScaleTransform3D(_items[cntr].SizeMultiplier, _items[cntr].SizeMultiplier, _items[cntr].SizeMultiplier);

                    // Model Visual
                    _items[cntr].Model = new ModelVisual3D();
                    _items[cntr].Model.Content = geometry;
                    _items[cntr].Model.Transform = new TranslateTransform3D(new Vector3D());		// I'll wait for update to give this a real value

                    #endregion
                }
                else
                {
                    #region Line

                    // Geometry Model ()
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = _sharedVisuals.ThrustLineMesh;		// a skinny 3D rectangle along the x axis

                    // Model Visual
                    _items[cntr].Model = new ModelVisual3D();
                    _items[cntr].Model.Content = geometry;
                    _items[cntr].Model.Transform = new TranslateTransform3D();        // I won't do anything with this right now

                    #endregion
                }
            }

            #endregion

            #region Add to the viewport

            if (_isPosVelAcc)
            {
                if (_showPosition)
                {
                    _viewport.Children.Add(_items[INDEX_POSITION].Model);
                }

                if (_showVelocity)
                {
                    _viewport.Children.Add(_items[INDEX_VELOCITY].Model);
                }

                if (_showAcceleration)
                {
                    _viewport.Children.Add(_items[INDEX_ACCELERATION].Model);
                }
            }
            else
            {
                // There's no bool to show/hide for custom
                for (int cntr = 0; cntr < _items.Length; cntr++)
                {
                    if (_items[cntr] != null)
                    {
                        _viewport.Children.Add(_items[cntr].Model);
                    }
                }
            }

            #endregion
        }
        private void ClearModels()
        {
            if (_isPosVelAcc)
            {
                _viewport.Children.Remove(_items[INDEX_POSITION].Model);		// it's safe to call even if it was never added to the viewport
                _viewport.Children.Remove(_items[INDEX_VELOCITY].Model);
                _viewport.Children.Remove(_items[INDEX_ACCELERATION].Model);

                _items[INDEX_POSITION].Model = null;
                _items[INDEX_VELOCITY].Model = null;
                _items[INDEX_ACCELERATION].Model = null;
            }
            else
            {
                // There's no bool to show/hide for custom
                for (int cntr = 0; cntr < _items.Length; cntr++)
                {
                    if (_items[cntr] != null)
                    {
                        _viewport.Children.Remove(_items[cntr].Model);
                        _items[cntr].Model = null;
                    }
                }
            }
        }

        #endregion
    }
}
