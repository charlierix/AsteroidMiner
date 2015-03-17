using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    /// <summary>
    /// This is a wrapper to a part or parts that are currently selected
    /// </summary>
    public class SelectedParts
    {
        #region Enum: DraggingModifier

        private enum DraggingModifier
        {
            None,
            ArrowX1,
            ArrowX2,
            ArrowY1,
            ArrowY2,
            ArrowZ1,
            ArrowZ2,
            RingX,
            RingY,
            RingZ,
            BallX1,
            BallX2,
            BallY1,
            BallY2,
            BallZ1,
            BallZ2
        }

        #endregion

        #region Declaration Section

        private Viewport3D _viewport = null;
        private Camera _camera = null;
        private EditorOptions _options = null;

        private List<DesignPart> _parts = new List<DesignPart>();

        private List<ModelVisual3D> _pointLights = new List<ModelVisual3D>();

        private DraggableModifierArrow _arrowX1 = null;
        private DraggableModifierArrow _arrowX2 = null;
        private DraggableModifierArrow _arrowY1 = null;
        private DraggableModifierArrow _arrowY2 = null;
        private DraggableModifierArrow _arrowZ1 = null;
        private DraggableModifierArrow _arrowZ2 = null;

        private DraggableModifierRing _ringX = null;
        private DraggableModifierRing _ringY = null;
        private DraggableModifierRing _ringZ = null;

        private DraggableModifierSphere _ballX1 = null;
        private DraggableModifierSphere _ballX2 = null;
        private DraggableModifierSphere _ballY1 = null;
        private DraggableModifierSphere _ballY2 = null;
        private DraggableModifierSphere _ballZ1 = null;
        private DraggableModifierSphere _ballZ2 = null;

        private DraggingModifier _draggingModifier = DraggingModifier.None;

        #endregion

        #region Constructor

        public SelectedParts(Viewport3D viewport, Camera camera, EditorOptions options)
        {
            _viewport = viewport;
            _camera = camera;
            _options = options;
        }

        #endregion

        #region Public Properties

        public Point3D Position
        {
            get
            {
                if (_parts.Count == 0)
                {
                    return new Point3D(0, 0, 0);
                }
                else if (_parts.Count == 1)
                {
                    return _parts[0].Part3D.Position;
                }
                else
                {
                    return new Point3D(
                        _parts.Average(o => o.Part3D.Position.X),
                        _parts.Average(o => o.Part3D.Position.Y),
                        _parts.Average(o => o.Part3D.Position.Z));
                }
            }
            set
            {
                if (_parts.Count == 0)
                {
                    return;
                }
                else if (_parts.Count == 1)
                {
                    _parts[0].Part3D.Position = value;
                }
                else
                {
                    // Get the current center point
                    Vector3D offset = value - this.Position;

                    foreach (DesignPart part in _parts)
                    {
                        part.Part3D.Position += offset;
                    }
                }

                TransformChanged();
            }
        }

        public Vector3D Scale
        {
            get
            {
                if (_parts.Count == 0)
                {
                    return new Vector3D(0, 0, 0);
                }
                else if (_parts.Count == 1)
                {
                    return _parts[0].Part3D.Scale;
                }
                else
                {
                    // This is the size of the axis aligned bounding box
                    Vector3D min = new Vector3D(
                        _parts.Min(o => o.Part3D.Position.X - (o.Part3D.Scale.X * .5d)),
                        _parts.Min(o => o.Part3D.Position.Y - (o.Part3D.Scale.Y * .5d)),
                        _parts.Min(o => o.Part3D.Position.Z - (o.Part3D.Scale.Z * .5d)));

                    Vector3D max = new Vector3D(
                        _parts.Max(o => o.Part3D.Position.X + (o.Part3D.Scale.X * .5d)),
                        _parts.Max(o => o.Part3D.Position.Y + (o.Part3D.Scale.Y * .5d)),
                        _parts.Max(o => o.Part3D.Position.Z + (o.Part3D.Scale.Z * .5d)));

                    return max - min;
                }
            }
            private set
            {
                if (_parts.Count == 0)
                {
                    return;
                }
                else if (_parts.Count == 1)
                {
                    _parts[0].Part3D.Scale = value;
                }
                else
                {
                    Vector3D currentScale = this.Scale;
                    Vector3D percentMultiplier = new Vector3D(value.X / currentScale.X, value.Y / currentScale.Y, value.Z / currentScale.Z);

                    // Now set each object's new scale
                    foreach (DesignPart part in _parts)
                    {
                        Vector3D scale = part.Part3D.Scale;

                        scale = new Vector3D(
                            GetScaleCapped(scale.X * percentMultiplier.X),
                            GetScaleCapped(scale.Y * percentMultiplier.Y),
                            GetScaleCapped(scale.Z * percentMultiplier.Z));

                        switch (part.Part3D.AllowedScale)
                        {
                            case PartDesignAllowedScale.X_Y_Z:
                                part.Part3D.Scale = scale;
                                break;

                            case PartDesignAllowedScale.XY_Z:
                                double xyAvg = (scale.X + scale.Y) * .5d;
                                part.Part3D.Scale = new Vector3D(xyAvg, xyAvg, scale.Z);
                                break;

                            case PartDesignAllowedScale.XYZ:
                                double xyzAvg = (scale.X + scale.Y + scale.Z) / 3d;
                                part.Part3D.Scale = new Vector3D(xyzAvg, xyzAvg, xyzAvg);
                                break;

                            default:
                                throw new ApplicationException("Unknown PartDesignAllowedScale: " + this.AllowedScale.ToString());
                        }
                    }
                }

                TransformChanged();
            }
        }

        private Quaternion? _orientation = null;
        public Quaternion Orientation
        {
            get
            {
                if (_parts.Count == 0)
                {
                    return _options.DefaultOrientation;
                }
                else if (_parts.Count == 1)
                {
                    return _parts[0].Part3D.Orientation;
                }
                else
                {
                    return _orientation.Value;
                }
            }
            set
            {
                if (_parts.Count == 0)
                {
                    return;
                }
                else if (_parts.Count == 1)
                {
                    _parts[0].Part3D.Orientation = value;
                }
                else
                {
                    // Get the difference between the old and new orientations
                    Quaternion oldNegated = new Quaternion(_orientation.Value.Axis, _orientation.Value.Angle * -1d);
                    Quaternion difference = Quaternion.Multiply(value.ToUnit(), oldNegated.ToUnit()).ToUnit();
                    RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(difference));

                    // All the parts will rotate around this center point
                    Point3D centerPoint = this.Position;

                    foreach (DesignPart part in _parts)
                    {
                        // Pull it into local coords, rotate, then put pack in world coords
                        Vector3D localPosition = part.Part3D.Position - centerPoint;
                        localPosition = transform.Transform(localPosition);
                        Point3D newPosition = centerPoint + localPosition;

                        // Add the part's current orientation to the change
                        Quaternion newOrientation = Quaternion.Multiply(difference, part.Part3D.Orientation.ToUnit());

                        // Store the changes
                        part.Part3D.SetTransform(null, newPosition, newOrientation);
                    }

                    // Store the new group orientation
                    _orientation = value;
                }

                TransformChanged();
            }
        }

        public int Count
        {
            get
            {
                return _parts.Count;
            }
        }

        public bool IsLocked
        {
            get
            {
                return _parts.Any(o => o.Part3D.IsLocked);
            }
            set
            {
                foreach (DesignPart part in _parts)
                {
                    part.Part3D.IsLocked = value;
                }

                if (value)
                {
                    HideModifiers();
                }
                else
                {
                    ShowModifiers();
                }
            }
        }

        public bool IsDraggingModifier
        {
            get
            {
                return _draggingModifier != DraggingModifier.None;
            }
        }

        public PartDesignAllowedScale AllowedScale
        {
            get
            {
                if (_parts.Count == 1)
                {
                    return _parts[0].Part3D.AllowedScale;
                }
                else
                {
                    return PartDesignAllowedScale.X_Y_Z;		// there is no enum value for none, so just return all, even if there are no parts
                }
            }
        }
        public PartDesignAllowedRotation AllowedRotation
        {
            get
            {
                if (_parts.Count == 0)
                {
                    return PartDesignAllowedRotation.None;
                }
                else if (_parts.Count == 1)
                {
                    return _parts[0].Part3D.AllowedRotation;
                }
                else
                {
                    return PartDesignAllowedRotation.X_Y_Z;
                }
            }
        }

        #endregion

        #region Public Methods

        public void AddRange(IEnumerable<DesignPart> parts)
        {
            foreach (DesignPart part in parts)
            {
                Add(part);
            }
        }
        public void Add(DesignPart part)
        {
            HideModifiers();

            _parts.Add(part);

            part.Part3D.IsSelected = true;

            if (_parts.Count > 1)
            {
                _orientation = _options.DefaultOrientation;
            }

            //No need to listen to this event anymore, the transform changed is now called directly by this class's propert sets
            //part.Part3D.TransformChanged += new EventHandler(Part3D_TransformChanged);

            // Point Light
            PointLight pointLight = new PointLight();
            pointLight.Color = _options.EditorColors.SelectionLightColor;
            pointLight.QuadraticAttenuation = 1d;
            pointLight.Range = 10d;
            ModelVisual3D pointLightModel = new ModelVisual3D();
            pointLightModel.Content = pointLight;
            _viewport.Children.Add(pointLightModel);
            _pointLights.Add(pointLightModel);

            if (!this.IsLocked)
            {
                ShowModifiers();
            }

            // Move the visuals to be relative to the part
            TransformChanged();
        }
        public void Remove(DesignPart part)
        {
            int index = _parts.IndexOf(part);
            if (index < 0)
            {
                // This should never happen, but no need to complain
                return;
            }

            if (_parts.Count != _pointLights.Count)
            {
                throw new ApplicationException("The parts and lights should always be synced");
            }

            HideModifiers();

            // PointLight
            _viewport.Children.Remove(_pointLights[index]);
            _pointLights.RemoveAt(index);

            // Part
            //_parts[index].Part3D.TransformChanged -= new EventHandler(Part3D_TransformChanged);
            _parts[index].Part3D.IsSelected = false;
            _parts.RemoveAt(index);

            if (_parts.Count > 1)
            {
                _orientation = _options.DefaultOrientation;
            }

            if (_parts.Count > 0 && !this.IsLocked)
            {
                // Recalculate the modifiers
                ShowModifiers();
            }
        }
        public void Clear()
        {
            while (_parts.Count > 0)
            {
                Remove(_parts[0]);
            }
        }

        public bool IsModifierHit(Visual3D hitVisual)
        {
            return this.GetModifierVisuals().Any(o => o == hitVisual);
        }

        public void StartDraggingModifier(Visual3D hitVisual, Point clickPoint)
        {
            #region Arrows

            if (_arrowX1 != null && _arrowX1.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.ArrowX1;
                _arrowX1.DragAxis = GetArrowRay(_draggingModifier);
                _arrowX1.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_arrowX2 != null && _arrowX2.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.ArrowX2;
                _arrowX2.DragAxis = GetArrowRay(_draggingModifier);
                _arrowX2.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_arrowY1 != null && _arrowY1.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.ArrowY1;
                _arrowY1.DragAxis = GetArrowRay(_draggingModifier);
                _arrowY1.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_arrowY2 != null && _arrowY2.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.ArrowY2;
                _arrowY2.DragAxis = GetArrowRay(_draggingModifier);
                _arrowY2.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_arrowZ1 != null && _arrowZ1.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.ArrowZ1;
                _arrowZ1.DragAxis = GetArrowRay(_draggingModifier);
                _arrowZ1.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_arrowZ2 != null && _arrowZ2.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.ArrowZ2;
                _arrowZ2.DragAxis = GetArrowRay(_draggingModifier);
                _arrowZ2.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }

            #endregion
            #region Rings

            else if (_ringX != null && _ringX.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.RingX;

                Point3D circleCenter;
                Quaternion circleOrientation;
                GetRingLocation(out circleCenter, out circleOrientation);
                _ringX.CircleCenter = circleCenter;
                _ringX.CircleOrientation = circleOrientation;

                _ringX.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_ringY != null && _ringY.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.RingY;

                Point3D circleCenter;
                Quaternion circleOrientation;
                GetRingLocation(out circleCenter, out circleOrientation);
                _ringY.CircleCenter = circleCenter;
                _ringY.CircleOrientation = circleOrientation;

                _ringY.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_ringZ != null && _ringZ.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.RingZ;

                Point3D circleCenter;
                Quaternion circleOrientation;
                GetRingLocation(out circleCenter, out circleOrientation);
                _ringZ.CircleCenter = circleCenter;
                _ringZ.CircleOrientation = circleOrientation;

                _ringZ.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }

            #endregion
            #region Balls

            else if (_ballX1 != null && _ballX1.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.BallX1;

                Point3D sphereCenter;
                Quaternion sphereOrientation;
                GetRingLocation(out sphereCenter, out sphereOrientation);
                _ballX1.SphereCenter = sphereCenter;
                _ballX1.SphereOrientation = sphereOrientation;

                _ballX1.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_ballX2 != null && _ballX2.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.BallX2;

                Point3D sphereCenter;
                Quaternion sphereOrientation;
                GetRingLocation(out sphereCenter, out sphereOrientation);
                _ballX2.SphereCenter = sphereCenter;
                _ballX2.SphereOrientation = sphereOrientation;

                _ballX2.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_ballY1 != null && _ballY1.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.BallY1;

                Point3D sphereCenter;
                Quaternion sphereOrientation;
                GetRingLocation(out sphereCenter, out sphereOrientation);
                _ballY1.SphereCenter = sphereCenter;
                _ballY1.SphereOrientation = sphereOrientation;

                _ballY1.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_ballY2 != null && _ballY2.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.BallY2;

                Point3D sphereCenter;
                Quaternion sphereOrientation;
                GetRingLocation(out sphereCenter, out sphereOrientation);
                _ballY2.SphereCenter = sphereCenter;
                _ballY2.SphereOrientation = sphereOrientation;

                _ballY2.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_ballZ1 != null && _ballZ1.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.BallZ1;

                Point3D sphereCenter;
                Quaternion sphereOrientation;
                GetRingLocation(out sphereCenter, out sphereOrientation);
                _ballZ1.SphereCenter = sphereCenter;
                _ballZ1.SphereOrientation = sphereOrientation;

                _ballZ1.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }
            else if (_ballZ2 != null && _ballZ2.Model == hitVisual)
            {
                _draggingModifier = DraggingModifier.BallZ2;

                Point3D sphereCenter;
                Quaternion sphereOrientation;
                GetRingLocation(out sphereCenter, out sphereOrientation);
                _ballZ2.SphereCenter = sphereCenter;
                _ballZ2.SphereOrientation = sphereOrientation;

                _ballZ2.StartDrag(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
            }

            #endregion
            else
            {
                throw new ArgumentException("The visual passed in is not a modifier");
            }
        }
        public void DragModifier(Point clickPoint)
        {
            // Get the ray that is under this click point
            var ray = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);

            Vector3D position = this.Position.ToVector();
            Vector3D scale = this.Scale;
            RotateTransform3D rotateTransform = new RotateTransform3D(new QuaternionRotation3D(this.Orientation));
            double newScale;

            switch (_draggingModifier)
            {
                case DraggingModifier.ArrowX1:
                case DraggingModifier.ArrowX2:
                    #region ArrowX

                    if (_draggingModifier == DraggingModifier.ArrowX1)
                    {
                        _arrowX1.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                        newScale = GetScale_Arrow((_arrowX1.DragAxis.Origin - position).ToVector().Length);
                    }
                    else
                    {
                        _arrowX2.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                        newScale = GetScale_Arrow((_arrowX2.DragAxis.Origin - position).ToVector().Length);
                    }

                    switch (this.AllowedScale)
                    {
                        case PartDesignAllowedScale.X_Y_Z:
                            this.Scale = new Vector3D(newScale, scale.Y, scale.Z);
                            break;

                        case PartDesignAllowedScale.XY_Z:
                            this.Scale = new Vector3D(newScale, newScale, scale.Z);
                            break;

                        case PartDesignAllowedScale.XYZ:		// only z is visible for this one
                        default:
                            throw new ApplicationException("Unexpected PartDesignAllowedScale: " + this.AllowedScale.ToString());
                    }

                    #endregion
                    break;

                case DraggingModifier.ArrowY1:
                case DraggingModifier.ArrowY2:
                    #region ArrowY

                    if (_draggingModifier == DraggingModifier.ArrowY1)
                    {
                        _arrowY1.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                        newScale = GetScale_Arrow((_arrowY1.DragAxis.Origin - position).ToVector().Length);
                    }
                    else
                    {
                        _arrowY2.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                        newScale = GetScale_Arrow((_arrowY2.DragAxis.Origin - position).ToVector().Length);
                    }

                    // Y is only visible when they are allowed to scale Y independently (no need to look at AllowedScale)
                    this.Scale = new Vector3D(scale.X, newScale, scale.Z);

                    #endregion
                    break;

                case DraggingModifier.ArrowZ1:
                case DraggingModifier.ArrowZ2:
                    #region ArrowZ

                    if (_draggingModifier == DraggingModifier.ArrowZ1)
                    {
                        _arrowZ1.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                        newScale = GetScale_Arrow((_arrowZ1.DragAxis.Origin - position).ToVector().Length);
                    }
                    else
                    {
                        _arrowZ2.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                        newScale = GetScale_Arrow((_arrowZ2.DragAxis.Origin - position).ToVector().Length);
                    }

                    switch (this.AllowedScale)
                    {
                        case PartDesignAllowedScale.X_Y_Z:
                        case PartDesignAllowedScale.XY_Z:
                            this.Scale = new Vector3D(scale.X, scale.Y, newScale);
                            break;

                        case PartDesignAllowedScale.XYZ:
                            this.Scale = new Vector3D(newScale, newScale, newScale);
                            break;

                        default:
                            throw new ApplicationException("Unknown PartDesignAllowedScale: " + this.AllowedScale.ToString());
                    }

                    #endregion
                    break;

                case DraggingModifier.RingX:
                    #region RingX

                    _ringX.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ringX.CircleOrientation;

                    #endregion
                    break;

                case DraggingModifier.RingY:
                    #region RingY

                    _ringY.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ringY.CircleOrientation;

                    #endregion
                    break;

                case DraggingModifier.RingZ:
                    #region RingZ

                    _ringZ.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ringZ.CircleOrientation;

                    #endregion
                    break;

                case DraggingModifier.BallX1:
                    #region BallX1

                    _ballX1.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ballX1.SphereOrientation;

                    #endregion
                    break;

                case DraggingModifier.BallX2:
                    #region BallX2

                    _ballX2.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ballX2.SphereOrientation;

                    #endregion
                    break;

                case DraggingModifier.BallY1:
                    #region BallY1

                    _ballY1.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ballY1.SphereOrientation;

                    #endregion
                    break;

                case DraggingModifier.BallY2:
                    #region BallY2

                    _ballY2.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ballY2.SphereOrientation;

                    #endregion
                    break;

                case DraggingModifier.BallZ1:
                    #region BallZ1

                    _ballZ1.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ballZ1.SphereOrientation;

                    #endregion
                    break;

                case DraggingModifier.BallZ2:
                    #region BallZ2

                    _ballZ2.DragItem(UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint));
                    this.Orientation = _ballZ2.SphereOrientation;

                    #endregion
                    break;

                case DraggingModifier.None:
                    throw new ApplicationException("DraggingModifier shouldn't be none");

                default:
                    throw new ApplicationException("Unknown DraggingModifier: " + _draggingModifier.ToString());
            }
        }
        public void StopDraggingModifier()
        {
            _draggingModifier = DraggingModifier.None;
        }

        public void HotTrackModifier(Visual3D hitVisual)
        {
            foreach (DraggableModifierBase modifier in GetModifiers())
            {
                modifier.IsHotTracked = modifier.Model == hitVisual;
            }
        }
        public void StopHotTrack()
        {
            foreach (DraggableModifierBase modifier in GetModifiers())
            {
                modifier.IsHotTracked = false;
            }
        }

        public IEnumerable<Visual3D> GetModifierVisuals()
        {
            // I can't find ConvertAll, must be new to 4.0
            //return GetModifiers().Select(o => o.Model).Covert;

            List<Visual3D> retVal = new List<Visual3D>();

            foreach (DraggableModifierBase modifier in GetModifiers())
            {
                retVal.Add(modifier.Model);
            }

            return retVal;

            #region OLD

            //List<Visual3D> retVal = new List<Visual3D>();

            //if (_arrowX1 != null)
            //{
            //    retVal.Add(_arrowX1.Model);
            //}

            //if (_arrowX2 != null)
            //{
            //    retVal.Add(_arrowX2.Model);
            //}

            //if (_arrowY1 != null)
            //{
            //    retVal.Add(_arrowY1.Model);
            //}

            //if (_arrowY2 != null)
            //{
            //    retVal.Add(_arrowY2.Model);
            //}

            //if (_arrowZ1 != null)
            //{
            //    retVal.Add(_arrowZ1.Model);
            //}

            //if (_arrowZ2 != null)
            //{
            //    retVal.Add(_arrowZ2.Model);
            //}

            //if (_ringX != null)
            //{
            //    retVal.Add(_ringX.Model);
            //}

            //if (_ringY != null)
            //{
            //    retVal.Add(_ringY.Model);
            //}

            //if (_ringZ != null)
            //{
            //    retVal.Add(_ringZ.Model);
            //}

            //if (_ballX1 != null)
            //{
            //    retVal.Add(_ballX1.Model);
            //}

            //if (_ballX2 != null)
            //{
            //    retVal.Add(_ballX2.Model);
            //}

            //if (_ballY1 != null)
            //{
            //    retVal.Add(_ballY1.Model);
            //}

            //if (_ballY2 != null)
            //{
            //    retVal.Add(_ballY2.Model);
            //}

            //if (_ballZ1 != null)
            //{
            //    retVal.Add(_ballZ1.Model);
            //}

            //if (_ballZ2 != null)
            //{
            //    retVal.Add(_ballZ2.Model);
            //}

            //return retVal;

            #endregion
        }

        public IEnumerable<DesignPart> GetParts()
        {
            return new List<DesignPart>(_parts);		// returning a copy so they can safely foreach and delete/insert at the same time
        }

        public IEnumerable<DesignPart> CloneParts()
        {
            List<DesignPart> retVal = new List<DesignPart>();

            foreach (DesignPart part in _parts)
            {
                retVal.Add(part.Clone());
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        private void ShowModifiers()
        {
            HideModifiers();		// Just in case something is already showing

            // Arrows
            switch (this.AllowedScale)
            {
                case PartDesignAllowedScale.X_Y_Z:
                    GetNewArrows(out _arrowX1, out _arrowX2, DraggingModifier.ArrowX1);
                    GetNewArrows(out _arrowY1, out _arrowY2, DraggingModifier.ArrowY1);
                    GetNewArrows(out _arrowZ1, out _arrowZ2, DraggingModifier.ArrowZ1);
                    break;

                case PartDesignAllowedScale.XY_Z:
                    GetNewArrows(out _arrowX1, out _arrowX2, DraggingModifier.ArrowX1);
                    GetNewArrows(out _arrowZ1, out _arrowZ2, DraggingModifier.ArrowZ1);
                    break;

                case PartDesignAllowedScale.XYZ:
                    GetNewArrows(out _arrowZ1, out _arrowZ2, DraggingModifier.ArrowZ1);
                    break;

                default:
                    throw new ApplicationException("Unknown PartDesignAllowedScale: " + this.AllowedScale.ToString());
            }

            // Rings
            switch (this.AllowedRotation)
            {
                case PartDesignAllowedRotation.None:
                    break;

                case PartDesignAllowedRotation.X_Y:
                    _ringX = GetNewRing(DraggingModifier.RingX);
                    _ringY = GetNewRing(DraggingModifier.RingY);
                    break;

                case PartDesignAllowedRotation.X_Y_Z:
                    _ringX = GetNewRing(DraggingModifier.RingX);
                    _ringY = GetNewRing(DraggingModifier.RingY);
                    _ringZ = GetNewRing(DraggingModifier.RingZ);
                    break;

                default:
                    throw new ApplicationException("Unknown PartDesignAllowedRotation: " + this.AllowedRotation.ToString());
            }

            // Balls
            switch (this.AllowedRotation)
            {
                case PartDesignAllowedRotation.None:
                    break;

                case PartDesignAllowedRotation.X_Y:
                    GetNewBalls(out _ballX1, out _ballX2, DraggingModifier.BallX1);
                    GetNewBalls(out _ballY1, out _ballY2, DraggingModifier.BallY1);
                    break;

                case PartDesignAllowedRotation.X_Y_Z:
                    GetNewBalls(out _ballX1, out _ballX2, DraggingModifier.BallX1);
                    GetNewBalls(out _ballY1, out _ballY2, DraggingModifier.BallY1);
                    GetNewBalls(out _ballZ1, out _ballZ2, DraggingModifier.BallZ1);
                    break;

                default:
                    throw new ApplicationException("Unknown PartDesignAllowedRotation: " + this.AllowedRotation.ToString());
            }

            // Move the visuals to be relative to the part
            TransformChanged();
        }
        private void HideModifiers()
        {
            if (_arrowX1 != null)
            {
                _viewport.Children.Remove(_arrowX1.Model);
                _arrowX1 = null;
            }

            if (_arrowX2 != null)
            {
                _viewport.Children.Remove(_arrowX2.Model);
                _arrowX2 = null;
            }

            if (_arrowY1 != null)
            {
                _viewport.Children.Remove(_arrowY1.Model);
                _arrowY1 = null;
            }

            if (_arrowY2 != null)
            {
                _viewport.Children.Remove(_arrowY2.Model);
                _arrowY2 = null;
            }

            if (_arrowZ1 != null)
            {
                _viewport.Children.Remove(_arrowZ1.Model);
                _arrowZ1 = null;
            }

            if (_arrowZ2 != null)
            {
                _viewport.Children.Remove(_arrowZ2.Model);
                _arrowZ2 = null;
            }

            if (_ringX != null)
            {
                _viewport.Children.Remove(_ringX.Model);
                _ringX = null;
            }

            if (_ringY != null)
            {
                _viewport.Children.Remove(_ringY.Model);
                _ringY = null;
            }

            if (_ringZ != null)
            {
                _viewport.Children.Remove(_ringZ.Model);
                _ringZ = null;
            }

            if (_ballX1 != null)
            {
                _viewport.Children.Remove(_ballX1.Model);
                _ballX1 = null;
            }

            if (_ballX2 != null)
            {
                _viewport.Children.Remove(_ballX2.Model);
                _ballX2 = null;
            }

            if (_ballY1 != null)
            {
                _viewport.Children.Remove(_ballY1.Model);
                _ballY1 = null;
            }

            if (_ballY2 != null)
            {
                _viewport.Children.Remove(_ballY2.Model);
                _ballY2 = null;
            }

            if (_ballZ1 != null)
            {
                _viewport.Children.Remove(_ballZ1.Model);
                _ballZ1 = null;
            }

            if (_ballZ2 != null)
            {
                _viewport.Children.Remove(_ballZ2.Model);
                _ballZ2 = null;
            }
        }

        private void TransformChanged()
        {
            if (_parts.Count != _pointLights.Count)
            {
                throw new ApplicationException("The parts and lights should always be synced");
            }

            Transform3DGroup transform;
            Vector3D position = this.Position.ToVector();
            Vector3D scale = this.Scale;
            RotateTransform3D rotateTransform = new RotateTransform3D(new QuaternionRotation3D(this.Orientation));

            #region Point Lights

            for (int cntr = 0; cntr < _parts.Count; cntr++)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(_parts[cntr].Part3D.Position.ToVector()));
                _pointLights[cntr].Transform = transform;
            }

            #endregion

            #region Arrows

            if (_arrowX1 != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);
                transform.Children.Add(new TranslateTransform3D(position + rotateTransform.Transform(new Vector3D(GetDistance_Arrow(scale.X), 0, 0))));
                _arrowX1.Model.Transform = transform;
            }

            if (_arrowX2 != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);
                transform.Children.Add(new TranslateTransform3D(position - rotateTransform.Transform(new Vector3D(GetDistance_Arrow(scale.X), 0, 0))));
                _arrowX2.Model.Transform = transform;
            }

            if (_arrowY1 != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);
                transform.Children.Add(new TranslateTransform3D(position + rotateTransform.Transform(new Vector3D(0, GetDistance_Arrow(scale.Y), 0))));
                _arrowY1.Model.Transform = transform;
            }

            if (_arrowY2 != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);
                transform.Children.Add(new TranslateTransform3D(position - rotateTransform.Transform(new Vector3D(0, GetDistance_Arrow(scale.Y), 0))));
                _arrowY2.Model.Transform = transform;
            }

            if (_arrowZ1 != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);
                transform.Children.Add(new TranslateTransform3D(position + rotateTransform.Transform(new Vector3D(0, 0, GetDistance_Arrow(scale.Z)))));
                _arrowZ1.Model.Transform = transform;
            }

            if (_arrowZ2 != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);
                transform.Children.Add(new TranslateTransform3D(position - rotateTransform.Transform(new Vector3D(0, 0, GetDistance_Arrow(scale.Z)))));
                _arrowZ2.Model.Transform = transform;
            }

            #endregion

            #region Rings

            if (_ringX != null || _ringY != null || _ringZ != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);
                transform.Children.Add(new TranslateTransform3D(position));

                if (_ringX != null)
                {
                    _ringX.Radius = GetRadius_Ring(scale, DraggingModifier.RingX);
                    _ringX.Model.Transform = transform;
                }

                if (_ringY != null)
                {
                    _ringY.Radius = GetRadius_Ring(scale, DraggingModifier.RingY);
                    _ringY.Model.Transform = transform;
                }

                if (_ringZ != null)
                {
                    _ringZ.Radius = GetRadius_Ring(scale, DraggingModifier.RingZ);
                    _ringZ.Model.Transform = transform;
                }
            }

            #endregion

            #region Balls

            if (_ballX1 != null || _ballX2 != null || _ballY1 != null || _ballY2 != null || _ballZ1 != null || _ballZ2 != null)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(rotateTransform);		//NOTE: The initial transform put the rotation after the translate, because it's a ball on the end of a stick.  But this transform rotates the whole stick first, then places the world pos
                transform.Children.Add(new TranslateTransform3D(position));

                if (_ballX1 != null)
                {
                    _ballX1.Radius = GetRadius_Ball(scale, DraggingModifier.BallX1);
                    _ballX1.Model.Transform = transform;
                }

                if (_ballX2 != null)
                {
                    _ballX2.Radius = GetRadius_Ball(scale, DraggingModifier.BallX2);
                    _ballX2.Model.Transform = transform;
                }

                if (_ballY1 != null)
                {
                    _ballY1.Radius = GetRadius_Ball(scale, DraggingModifier.BallY1);
                    _ballY1.Model.Transform = transform;
                }

                if (_ballY2 != null)
                {
                    _ballY2.Radius = GetRadius_Ball(scale, DraggingModifier.BallY2);
                    _ballY2.Model.Transform = transform;
                }

                if (_ballZ1 != null)
                {
                    _ballZ1.Radius = GetRadius_Ball(scale, DraggingModifier.BallZ1);
                    _ballZ1.Model.Transform = transform;
                }

                if (_ballZ2 != null)
                {
                    _ballZ2.Radius = GetRadius_Ball(scale, DraggingModifier.BallZ2);
                    _ballZ2.Model.Transform = transform;
                }
            }

            #endregion
        }

        private RayHitTestParameters GetArrowRay(DraggingModifier whichArrow)
        {
            Vector3D origin = new Vector3D();
            Vector3D direction = new Vector3D();

            Vector3D position = this.Position.ToVector();
            Vector3D scale = this.Scale;
            RotateTransform3D rotateTransform = new RotateTransform3D(new QuaternionRotation3D(this.Orientation));

            switch (whichArrow)
            {
                case DraggingModifier.ArrowX1:
                    origin = position + rotateTransform.Transform(new Vector3D(GetDistance_Arrow(scale.X), 0, 0));
                    direction = rotateTransform.Transform(new Vector3D(1, 0, 0));
                    break;

                case DraggingModifier.ArrowX2:
                    origin = position - rotateTransform.Transform(new Vector3D(GetDistance_Arrow(scale.X), 0, 0));
                    direction = rotateTransform.Transform(new Vector3D(-1, 0, 0));
                    break;

                case DraggingModifier.ArrowY1:
                    origin = position + rotateTransform.Transform(new Vector3D(0, GetDistance_Arrow(scale.Y), 0));
                    direction = rotateTransform.Transform(new Vector3D(0, 1, 0));
                    break;

                case DraggingModifier.ArrowY2:
                    origin = position - rotateTransform.Transform(new Vector3D(0, GetDistance_Arrow(scale.Y), 0));
                    direction = rotateTransform.Transform(new Vector3D(0, -1, 0));
                    break;

                case DraggingModifier.ArrowZ1:
                    origin = position + rotateTransform.Transform(new Vector3D(0, 0, GetDistance_Arrow(scale.Z)));
                    direction = rotateTransform.Transform(new Vector3D(0, 0, 1));
                    break;

                case DraggingModifier.ArrowZ2:
                    origin = position - rotateTransform.Transform(new Vector3D(0, 0, GetDistance_Arrow(scale.Z)));
                    direction = rotateTransform.Transform(new Vector3D(0, 0, -1));
                    break;

                default:
                    throw new ApplicationException("Unexpected DraggingModifier: " + whichArrow.ToString());
            }

            // Exit Function
            return new RayHitTestParameters(origin.ToPoint(), direction);
        }
        private void GetRingLocation(out Point3D center, out Quaternion orientation)
        {
            center = this.Position;
            orientation = this.Orientation;
        }

        private void GetNewArrows(out DraggableModifierArrow arrow1, out DraggableModifierArrow arrow2, DraggingModifier whichArrow)
        {
            switch (whichArrow)
            {
                case DraggingModifier.ArrowX1:
                case DraggingModifier.ArrowX2:
                    arrow1 = new DraggableModifierArrow(new Quaternion(new Vector3D(0, 1, 0), 90), _options.EditorColors);
                    arrow2 = new DraggableModifierArrow(new Quaternion(new Vector3D(0, 1, 0), -90), _options.EditorColors);
                    break;

                case DraggingModifier.ArrowY1:
                case DraggingModifier.ArrowY2:
                    arrow1 = new DraggableModifierArrow(new Quaternion(new Vector3D(1, 0, 0), -90), _options.EditorColors);
                    arrow2 = new DraggableModifierArrow(new Quaternion(new Vector3D(1, 0, 0), 90), _options.EditorColors);
                    break;

                case DraggingModifier.ArrowZ1:
                case DraggingModifier.ArrowZ2:
                    arrow1 = new DraggableModifierArrow(new Quaternion(new Vector3D(1, 0, 0), 0), _options.EditorColors);
                    arrow2 = new DraggableModifierArrow(new Quaternion(new Vector3D(1, 0, 0), 180), _options.EditorColors);
                    break;

                default:
                    throw new ApplicationException("Unexpected DraggingModifier: " + whichArrow.ToString());
            }

            _viewport.Children.Add(arrow1.Model);
            _viewport.Children.Add(arrow2.Model);
        }
        private DraggableModifierRing GetNewRing(DraggingModifier whichRing)
        {
            DraggableModifierRing retVal;

            double radius = GetRadius_Ring(this.Scale, whichRing);

            switch (whichRing)
            {
                case DraggingModifier.RingX:
                    retVal = new DraggableModifierRing(new Quaternion(new Vector3D(0, 1, 0), 90), radius, _options.EditorColors);
                    break;

                case DraggingModifier.RingY:
                    retVal = new DraggableModifierRing(new Quaternion(new Vector3D(1, 0, 0), 90), radius, _options.EditorColors);
                    break;

                case DraggingModifier.RingZ:
                    retVal = new DraggableModifierRing(new Quaternion(new Vector3D(0, 0, 1), 0), radius, _options.EditorColors);
                    break;

                default:
                    throw new ApplicationException("Unexpected DraggingModifier: " + whichRing.ToString());
            }

            _viewport.Children.Add(retVal.Model);

            return retVal;
        }
        private void GetNewBalls(out DraggableModifierSphere ball1, out DraggableModifierSphere ball2, DraggingModifier whichBall)
        {
            double radius = GetRadius_Ball(this.Scale, whichBall);

            switch (whichBall)
            {
                case DraggingModifier.BallX1:
                case DraggingModifier.BallX2:
                    ball1 = new DraggableModifierSphere(new Vector3D(radius, 0, 0), _options.EditorColors);
                    ball2 = new DraggableModifierSphere(new Vector3D(-radius, 0, 0), _options.EditorColors);
                    break;

                case DraggingModifier.BallY1:
                case DraggingModifier.BallY2:
                    ball1 = new DraggableModifierSphere(new Vector3D(0, radius, 0), _options.EditorColors);
                    ball2 = new DraggableModifierSphere(new Vector3D(0, -radius, 0), _options.EditorColors);
                    break;

                case DraggingModifier.BallZ1:
                case DraggingModifier.BallZ2:
                    ball1 = new DraggableModifierSphere(new Vector3D(0, 0, radius), _options.EditorColors);
                    ball2 = new DraggableModifierSphere(new Vector3D(0, 0, -radius), _options.EditorColors);
                    break;

                default:
                    throw new ApplicationException("Unexpected DraggingModifier: " + whichBall.ToString());
            }

            _viewport.Children.Add(ball1.Model);
            _viewport.Children.Add(ball2.Model);
        }

        private static double GetScale_Arrow(double distance)
        {
            double retVal = (distance - .4d) * 2d;
            return GetScaleCapped(retVal);
        }
        private static double GetScaleCapped(double scale)
        {
            double retVal = scale;
            if (retVal < .1)
            {
                retVal = .1;
            }
            return retVal;
        }
        private static double GetDistance_Arrow(double scale)
        {
            return (scale * .5d) + .4d;
        }
        private static double GetRadius_Ring(Vector3D scale, DraggingModifier whichRing)
        {
            const double MINSIZE = .8d;

            double largestScale;
            switch (whichRing)
            {
                case DraggingModifier.RingX:
                    largestScale = Math.Max(scale.Y, scale.Z);
                    break;

                case DraggingModifier.RingY:
                    largestScale = Math.Max(scale.X, scale.Z);
                    break;

                case DraggingModifier.RingZ:
                    largestScale = Math.Max(scale.X, scale.Y);
                    break;

                default:
                    throw new ApplicationException("Unexpected DraggingModifier: " + whichRing.ToString());
            }

            double retVal = (Math.Sqrt(2d) * largestScale * .5d) + MINSIZE;
            if (retVal < MINSIZE)
            {
                retVal = MINSIZE;
            }

            return retVal;
        }
        private static double GetRadius_Ball(Vector3D scale, DraggingModifier whichBall)
        {
            const double MINSIZE = 1.2d;

            double largestScale;
            switch (whichBall)
            {
                case DraggingModifier.BallX1:
                case DraggingModifier.BallX2:
                    //largestScale = Math.Max(scale.Y, scale.Z);
                    largestScale = scale.X;
                    break;

                case DraggingModifier.BallY1:
                case DraggingModifier.BallY2:
                    //largestScale = Math.Max(scale.X, scale.Z);
                    largestScale = scale.Y;
                    break;

                case DraggingModifier.BallZ1:
                case DraggingModifier.BallZ2:
                    //largestScale = Math.Max(scale.X, scale.Y);
                    largestScale = scale.Z;
                    break;

                default:
                    throw new ApplicationException("Unexpected DraggingModifier: " + whichBall.ToString());
            }

            double retVal = (Math.Sqrt(2d) * largestScale * .5d) + MINSIZE;
            if (retVal < MINSIZE)
            {
                retVal = MINSIZE;
            }

            return retVal;
        }

        private IEnumerable<DraggableModifierBase> GetModifiers()
        {
            List<DraggableModifierBase> retVal = new List<DraggableModifierBase>();

            if (_arrowX1 != null)
            {
                retVal.Add(_arrowX1);
            }

            if (_arrowX2 != null)
            {
                retVal.Add(_arrowX2);
            }

            if (_arrowY1 != null)
            {
                retVal.Add(_arrowY1);
            }

            if (_arrowY2 != null)
            {
                retVal.Add(_arrowY2);
            }

            if (_arrowZ1 != null)
            {
                retVal.Add(_arrowZ1);
            }

            if (_arrowZ2 != null)
            {
                retVal.Add(_arrowZ2);
            }

            if (_ringX != null)
            {
                retVal.Add(_ringX);
            }

            if (_ringY != null)
            {
                retVal.Add(_ringY);
            }

            if (_ringZ != null)
            {
                retVal.Add(_ringZ);
            }

            if (_ballX1 != null)
            {
                retVal.Add(_ballX1);
            }

            if (_ballX2 != null)
            {
                retVal.Add(_ballX2);
            }

            if (_ballY1 != null)
            {
                retVal.Add(_ballY1);
            }

            if (_ballY2 != null)
            {
                retVal.Add(_ballY2);
            }

            if (_ballZ1 != null)
            {
                retVal.Add(_ballZ1);
            }

            if (_ballZ2 != null)
            {
                retVal.Add(_ballZ2);
            }

            return retVal;
        }

        #endregion
    }
}
