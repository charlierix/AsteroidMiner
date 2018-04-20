using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;

using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    #region Class: DraggableModifierSphere

    public class DraggableModifierSphere : DraggableModifierBase
    {
        #region Declaration Section

        private Vector3D _initialOffset;

        private DragHitShape _dragHitShape = null;

        // When they start to drag the arrow around, this is where they initiated the drag (passed to _dragHitShape.CastRay to
        // determine offset)
        private RayHitTestParameters _dragMouseDownClickRay = null;
        private RayHitTestParameters _dragMouseDownCenterRay = null;

        private Point3D _mouseDownHitPoint = new Point3D(0, 0, 0);
        private Quaternion _mouseDownOrientation = Quaternion.Identity;

        #endregion

        #region Constructor

        //NOTE: Radius isn't the radius of the ball, it's how far that ball is from the center
        public DraggableModifierSphere(Vector3D offset, EditorColors editorColors)
            : base(editorColors)
        {
            _radius = offset.Length;
            _initialOffset = offset.ToUnit();

            this.Model = new ModelVisual3D();
            this.Model.Content = GetBall();
        }

        #endregion

        #region Public Properties

        // Set these two before calling StartDrag.  After calling DragItem, SphereOrientation will hold the new angle
        private Point3D? _sphereCenter = null;
        public Point3D SphereCenter
        {
            get
            {
                if (_sphereCenter == null)
                {
                    throw new InvalidOperationException("SphereCenter hasn't been set yet");
                }

                return _sphereCenter.Value;
            }
            set
            {
                _sphereCenter = value;
            }
        }
        private Quaternion? _sphereOrientation = null;
        public Quaternion SphereOrientation
        {
            get
            {
                if (_sphereOrientation == null)
                {
                    throw new InvalidOperationException("SphereOrientation hasn't been set yet");
                }

                return _sphereOrientation.Value;
            }
            set
            {
                _sphereOrientation = value;
            }
        }

        private double _radius = 0d;
        public double Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                if (_radius == value)
                {
                    return;
                }

                _radius = value;

                // The ball is actually positioned on an invisible stick of length radius (the ball itself is much smaller)
                this.Model.Content = GetBall();
            }
        }

        #endregion

        #region Public Methods

        public override void StartDrag(RayHitTestParameters ray)
        {
            if (_sphereCenter == null || _sphereOrientation == null)
            {
                throw new InvalidOperationException("SphereCenter and SphereOrientation need to be set before calling this method");
            }

            if (_dragHitShape == null)
            {
                _dragHitShape = new DragHitShape();
            }
            _dragHitShape.SetShape_Sphere(this.SphereCenter, this.Radius);

            _dragMouseDownClickRay = ray;
            _dragMouseDownCenterRay = ray;
            //_dragMouseDownCenterRay = new RayHitTestParameters(this.DragAxis.Origin, ray.Direction);		//TODO: the ray through the center of the part really isn't parallel to the click ray (since the perspective camera sees in a cone)

            // Remember where they clicked (to be able to calculate the rotation in DragItem)
            Point3D? hitPoint = _dragHitShape.CastRay(ray);
            if (hitPoint == null)
            {
                // This will never happen for sphere
                _mouseDownHitPoint = new Point3D(0, 0, 0);
            }
            else
            {
                _mouseDownHitPoint = hitPoint.Value;
            }

            _mouseDownOrientation = this.SphereOrientation.ToUnit();
        }
        public override void DragItem(RayHitTestParameters ray)
        {
            if (_sphereCenter == null || _sphereOrientation == null)
            {
                throw new InvalidOperationException("SphereCenter and SphereOrientation need to be set before calling this method");
            }

            Point3D? hitPoint = _dragHitShape.CastRay(_dragMouseDownClickRay, _dragMouseDownCenterRay, ray);

            if (hitPoint != null)
            {
                Quaternion? rotationDelta = _dragHitShape.GetRotation(_mouseDownHitPoint, hitPoint.Value);
                if (rotationDelta != null)
                {
                    this.SphereOrientation = Quaternion.Multiply(rotationDelta.Value.ToUnit(), _mouseDownOrientation);
                }
            }
        }

        #endregion

        #region Private Methods

        private Model3D GetBall()
        {
            GeometryModel3D retVal = new GeometryModel3D();
            MaterialGroup material = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(this.EditorColors.DraggableModifier));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.EditorColors.DraggableModifier));
            material.Children.Add(diffuse);
            SpecularMaterial specular = new SpecularMaterial(new SolidColorBrush(this.EditorColors.DraggableModifier_SpecularColor), this.EditorColors.DraggableModifier_SpecularPower);
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            retVal.Material = material;
            retVal.BackMaterial = material;
            retVal.Geometry = UtilityWPF.GetSphere_LatLon(20, .1);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new TranslateTransform3D(_initialOffset * _radius));

            retVal.Transform = transform;

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: DraggableModifierRing

    public class DraggableModifierRing : DraggableModifierBase
    {
        #region Declaration Section

        // This isn't the final transformed rotation as this ring will appear on screen.  It is just a rotation out of the xy plane
        private Quaternion _initialOrientation;
        private RotateTransform3D _initialRotateTransform;
        private Vector3D _intialAxisX;		// these are X,Y,Z rotated by _rotation
        private Vector3D _intialAxisY;
        private Vector3D _intialAxisZ;

        // This is the angle along the circle that they clicked on
        private Quaternion _mouseDownAngle;

        // These are set during StartDrag
        private RotateTransform3D _rotateTransform = null;
        private Triangle _circlePlane = null;

        #endregion

        #region Constructor

        public DraggableModifierRing(Quaternion orientation, double radius, EditorColors editorColors)
            : base(editorColors)
        {
            _initialOrientation = orientation;
            _radius = radius;

            _initialRotateTransform = new RotateTransform3D(new QuaternionRotation3D(_initialOrientation));
            _intialAxisX = _initialRotateTransform.Transform(new Vector3D(1, 0, 0));
            _intialAxisY = _initialRotateTransform.Transform(new Vector3D(0, 1, 0));
            _intialAxisZ = _initialRotateTransform.Transform(new Vector3D(0, 0, 1));

            this.Model = new ModelVisual3D();
            this.Model.Content = GetRing();
        }

        #endregion

        #region Public Properties

        // Set these two before calling StartDrag.  After calling DragItem, CircleOrientation will hold the new angle
        private Point3D? _circleCenter = null;
        public Point3D CircleCenter
        {
            get
            {
                if (_circleCenter == null)
                {
                    throw new InvalidOperationException("CircleCenter hasn't been set yet");
                }

                return _circleCenter.Value;
            }
            set
            {
                _circleCenter = value;
            }
        }
        private Quaternion? _circleOrientation = null;
        public Quaternion CircleOrientation
        {
            get
            {
                if (_circleOrientation == null)
                {
                    throw new InvalidOperationException("CircleOrientation hasn't been set yet");
                }

                return _circleOrientation.Value;
            }
            set
            {
                _circleOrientation = value;
            }
        }

        private double _radius = 0d;
        public double Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                if (_radius == value)
                {
                    return;
                }

                _radius = value;

                // The ring will look wrong if I just scale the transform.  It needs to be rebuilt
                this.Model.Content = GetRing();
            }
        }

        #endregion

        #region Public Methods

        public override void StartDrag(RayHitTestParameters ray)
        {
            if (_circleCenter == null || _circleOrientation == null)
            {
                throw new InvalidOperationException("CircleCenter and CircleOrientation need to be set before calling this method");
            }

            _rotateTransform = new RotateTransform3D(new QuaternionRotation3D(_circleOrientation.Value));
            _circlePlane = new Triangle(_circleCenter.Value, _circleCenter.Value + _rotateTransform.Transform(_intialAxisX), _circleCenter.Value + _rotateTransform.Transform(_intialAxisY));

            // Compare the ray with the drag ring to see where on the circle they clicked
            Point3D[] nearestCirclePoints, nearestLinePoints;
            if (Math3D.GetClosestPoints_Circle_Line(out nearestCirclePoints, out nearestLinePoints, _circlePlane, _circleCenter.Value, _radius, ray.Origin, ray.Direction, Math3D.RayCastReturn.ClosestToRay))
            {
                _mouseDownAngle = GetAngle(nearestCirclePoints[0]);
                _mouseDownAngle = new Quaternion(_mouseDownAngle.Axis, _mouseDownAngle.Angle * -1d).ToUnit();		// taking angle times -1 so that the drag just has to do a simple multiply
            }
            else
            {
                // The click ray is perpendicular to the circle plane, and they are clicking through the direct center of the circle (this should never happen)
                _mouseDownAngle = Quaternion.Identity;
            }
        }
        public override void DragItem(RayHitTestParameters ray)
        {
            if (_circleCenter == null || _circleOrientation == null)
            {
                throw new InvalidOperationException("CircleCenter and CircleOrientation need to be set before calling this method");
            }

            // See where on the circle they clicked
            // If the method comes back false, it's a very rare case, so just don't drag the ring
            Point3D[] nearestCirclePoints, nearestLinePoints;
            if (Math3D.GetClosestPoints_Circle_Line(out nearestCirclePoints, out nearestLinePoints, _circlePlane, _circleCenter.Value, _radius, ray.Origin, ray.Direction, Math3D.RayCastReturn.ClosestToRay))
            {
                // Figure out what angle they clicked on
                Quaternion clickQuat = GetAngle(nearestCirclePoints[0]);

                // Subtract off off where they first clicked (start drag already negated _mouseDownAngle and made it unit)
                clickQuat = Quaternion.Multiply(_mouseDownAngle, clickQuat.ToUnit());

                // Now store the new location
                _circleOrientation = Quaternion.Multiply(clickQuat.ToUnit(), _circleOrientation.Value.ToUnit());
            }
        }

        #endregion

        #region Private Methods

        private Model3D GetRing()
        {
            double size = UtilityCore.GetScaledValue_Capped(.025d, .15d, .1d, 10d, _radius);

            GeometryModel3D retVal = new GeometryModel3D();
            MaterialGroup material = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(this.EditorColors.DraggableModifier));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.EditorColors.DraggableModifier));
            material.Children.Add(diffuse);
            SpecularMaterial specular = new SpecularMaterial(new SolidColorBrush(this.EditorColors.DraggableModifier_SpecularColor), this.EditorColors.DraggableModifier_SpecularPower);
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            retVal.Material = material;
            retVal.BackMaterial = material;
            retVal.Geometry = UtilityWPF.GetRing(50, _radius - (size * .5d), _radius + (size * .5d), size, _initialRotateTransform);

            return retVal;
        }

        private Quaternion GetAngle(Point3D pointOnCircle)
        {
            RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(_circleOrientation.Value));
            Vector3D axisX = transform.Transform(_intialAxisX);

            Vector3D requestAxis = pointOnCircle - _circleCenter.Value;
            double angle = Vector3D.AngleBetween(axisX, requestAxis);

            Vector3D axis = Vector3D.CrossProduct(axisX, requestAxis);

            return new Quaternion(axis, angle);
        }

        #endregion
    }

    #endregion
    #region Class: DraggableModifierArrow

    /// <summary>
    /// This is a draggable arrow that modifies a part's scale
    /// </summary>
    public class DraggableModifierArrow : DraggableModifierBase
    {
        #region Declaration Section

        private DragHitShape _dragHitShape = null;

        // When they start to drag the arrow around, this is where they initiated the drag (passed to _dragHitShape.CastRay to
        // determine offset)
        private RayHitTestParameters _dragMouseDownClickRay = null;
        private RayHitTestParameters _dragMouseDownCenterRay = null;

        #endregion

        #region Constructor

        public DraggableModifierArrow(Quaternion rotation, EditorColors editorColors)
            : base(editorColors)
        {
            this.Model = new ModelVisual3D();
            this.Model.Content = GetArrow(rotation);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is the line that the arrow can drag along.  DragAxis.Origin is the center of the arrow's position
        /// </summary>
        /// <remarks>
        /// This needs to be set before calling StartDrag.  After calling DragItem, this will be updated with a new position
        /// </remarks>
        public RayHitTestParameters DragAxis
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public override void StartDrag(RayHitTestParameters ray)
        {
            if (this.DragAxis == null)
            {
                throw new InvalidOperationException("DragAxis needs to be set before calling this method");
            }

            if (_dragHitShape == null)
            {
                _dragHitShape = new DragHitShape();
            }
            _dragHitShape.SetShape_Line(this.DragAxis);

            _dragMouseDownClickRay = ray;
            _dragMouseDownCenterRay = new RayHitTestParameters(this.DragAxis.Origin, ray.Direction);		//TODO: the ray through the center of the part really isn't parallel to the click ray (since the perspective camera sees in a cone)
        }
        public override void DragItem(RayHitTestParameters ray)
        {
            if (this.DragAxis == null)
            {
                throw new InvalidOperationException("DragAxis needs to be set before calling this method");
            }

            // Compare the ray with the drag axis to see where on the axis they clicked
            // If the method comes back false, then the click ray is parallel to the axis, so just don't drag the arrow
            Point3D? hitPoint = _dragHitShape.CastRay(_dragMouseDownClickRay, _dragMouseDownCenterRay, ray);
            if (hitPoint != null)
            {
                this.DragAxis = new RayHitTestParameters(hitPoint.Value, this.DragAxis.Direction);
            }
        }

        #endregion

        #region Private Methods

        private GeometryModel3D GetArrow(Quaternion rotation)
        {
            GeometryModel3D retVal = new GeometryModel3D();
            MaterialGroup material = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(this.EditorColors.DraggableModifier));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.EditorColors.DraggableModifier));
            material.Children.Add(diffuse);
            SpecularMaterial specular = new SpecularMaterial(new SolidColorBrush(this.EditorColors.DraggableModifier_SpecularColor), this.EditorColors.DraggableModifier_SpecularPower);
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            retVal.Material = material;
            retVal.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingDome(0, false, 10));
            rings.Add(new TubeRingRegularPolygon(.025, false, .05, .05, false));
            rings.Add(new TubeRingRegularPolygon(.3, false, .05, .05, false));
            rings.Add(new TubeRingRegularPolygon(-.0375, false, .125, .125, false));
            rings.Add(new TubeRingPoint(.2, false));

            RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(rotation));

            retVal.Geometry = UtilityWPF.GetMultiRingedTube(35, rings, true, true, transform);

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: DraggableModifierBase

    public abstract class DraggableModifierBase
    {
        #region Class: MaterialColorProps

        // This is copied from ShipParts.PartDesignBase (I didn't want cross namespace references)
        protected class MaterialColorProps
        {
            public MaterialColorProps(DiffuseMaterial diffuse, Brush brush, Color color)
            {
                this.Diffuse = diffuse;
                this.OrigBrush = brush;		// this constructor explicitely takes brush, because it could be something other than a solid color brush
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

        #region Constructor

        protected DraggableModifierBase(EditorColors editorColors)
        {
            this.EditorColors = editorColors;
        }

        #endregion

        #region Public Properties

        public ModelVisual3D Model
        {
            get;
            protected set;
        }

        private bool _isHotTracked = false;
        public bool IsHotTracked
        {
            get
            {
                return _isHotTracked;
            }
            set
            {
                if (_isHotTracked == value)
                {
                    return;
                }

                _isHotTracked = value;

                SetBrushColors();
            }
        }

        public EditorColors EditorColors
        {
            get;
            private set;
        }

        private List<MaterialColorProps> _materialBrushes = new List<MaterialColorProps>();
        protected List<MaterialColorProps> MaterialBrushes
        {
            get
            {
                return _materialBrushes;
            }
        }

        #endregion

        #region Public Methods

        public abstract void StartDrag(RayHitTestParameters ray);
        public abstract void DragItem(RayHitTestParameters ray);

        #endregion

        #region Private Methods

        private void SetBrushColors()
        {
            foreach (MaterialColorProps material in this.MaterialBrushes)
            {
                if (_isHotTracked)
                {
                    #region Hot Track

                    if (material.Diffuse != null)
                    {
                        material.Diffuse.Brush = new SolidColorBrush(this.EditorColors.DraggableModifierHotTrack);
                    }
                    else if (material.Specular != null)
                    {
                        material.Specular.Brush = new SolidColorBrush(this.EditorColors.DraggableModifierHotTrack_SpecularColor);
                        material.Specular.SpecularPower = this.EditorColors.DraggableModifierHotTrack_SpecularPower;
                    }
                    else if (material.Emissive != null)
                    {
                        material.Emissive.Brush = new SolidColorBrush(this.EditorColors.DraggableModifierHotTrack);
                    }
                    else
                    {
                        throw new ApplicationException("Unknown material type");
                    }

                    #endregion
                }
                else
                {
                    #region Standard

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
            }
        }

        #endregion
    }

    #endregion
}
