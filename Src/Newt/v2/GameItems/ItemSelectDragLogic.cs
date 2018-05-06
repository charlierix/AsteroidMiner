using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;

namespace Game.Newt.v2.GameItems
{
    //TODO: Let the user drag a selection box and select more than one item at a time (and drag them around as a group)

    /// <summary>
    /// This is a helper class that holds logic to select objects, and drag items around
    /// </summary>
    /// <remarks>
    /// I'd rather put a class like this in the HelperClasses dll, but it needs to know about IMapObject
    /// 
    /// TODO: Throw items   ----- this may be handled now?
    /// 
    /// TODO: This currently only supports dragging item on a plane.  Add more options (these need to be reevaluated each mousemove):
    ///     Adaptive Plane
    ///         This acts like a plane, but if they drag toward other objects, rotate the plane so it will intersect with that item if they keep
    ///         dragging toward it.
    ///         
    ///         Only do for items that are +- a max distance from the initial plane.
    ///         
    ///         The direction they drag indicates which items to consider, (dot product tells how much to honor that deviation, so if they
    ///         aren't dragging directly toward it, choose a plane distance some % between initial plane and line to that point)
    ///         
    ///     Anchor Point
    ///         The user can choose some item or point as an anchor.  Then when they drag the selected item, choose a plane that causes
    ///         the item to intersect with that point
    ///         
    ///         This would be tedious to use in practice with a keyboard/mouse.  Easier with multitouch or kinnect
    /// </remarks>
    public class ItemSelectDragLogic
    {
        #region Events

        public event EventHandler<ItemSelectedArgs> ItemSelected = null;

        #endregion

        #region Declaration Section

        private readonly Map _map;

        private PerspectiveCamera _camera = null;
        private Viewport3D _viewport = null;
        private UIElement _visual = null;       // This is the UIElement that _viewport sits on, and is what is raising the mouse events

        /// <summary>
        /// If the user clicks on an item, this will be non null
        /// </summary>
        private MapObject_ChasePoint_Direct _selectedItem = null;

        /// <summary>
        /// If they are dragging an item around, this will the a plane orthogonal to the camera intersecting the item
        /// </summary>
        private DragHitShape _dragPlane = null;

        // When they start to drag _selectedItem around, this is where they initiated the drag (passed to _dragHitShape.CastRay to
        // determine offset)
        private RayHitTestParameters _dragMouseDownClickRay = null;
        private RayHitTestParameters _dragMouseDownCenterRay = null;

        private List<Point3D> _dragHistory = new List<Point3D>();

        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        #endregion

        #region Constructor

        public ItemSelectDragLogic(Map map, PerspectiveCamera camera, Viewport3D viewport, UIElement visual)
        {
            _map = map;
            _camera = camera;
            _viewport = viewport;
            _visual = visual;
        }

        #endregion

        #region Public Properties

        private readonly List<Type> _selectableTypes = new List<Type>();
        public List<Type> SelectableTypes
        {
            get
            {
                return _selectableTypes;
            }
        }

        private bool _useAdaptiveDragPlane = true;
        public bool UseAdaptiveDragPlane
        {
            get
            {
                return _useAdaptiveDragPlane;
            }
            set
            {
                _useAdaptiveDragPlane = value;
            }
        }

        private bool _shouldMoveItemWithSpring = true;
        public bool ShouldMoveItemWithSpring
        {
            get
            {
                return _shouldMoveItemWithSpring;
            }
            set
            {
                _shouldMoveItemWithSpring = value;
            }
        }

        private bool _shouldSpringCauseTorque = true;
        public bool ShouldSpringCauseTorque
        {
            get
            {
                return _shouldSpringCauseTorque;
            }
            set
            {
                _shouldSpringCauseTorque = value;
            }
        }

        private bool _shouldDampenWhenSpring = true;
        public bool ShouldDampenWhenSpring
        {
            get
            {
                return _shouldDampenWhenSpring;
            }
            set
            {
                _shouldDampenWhenSpring = value;
            }
        }

        private Color? _springColor = null;
        /// <summary>
        /// If null, no line will be shown
        /// </summary>
        public Color? SpringColor
        {
            get
            {
                return _springColor;
            }
            set
            {
                _springColor = value;
            }
        }

        private bool _showDebugVisuals = false;
        public bool ShowDebugVisuals
        {
            get
            {
                return _showDebugVisuals;
            }
            set
            {
                _showDebugVisuals = value;
            }
        }

        #endregion

        #region Public Methods

        public void LeftMouseDown(MouseButtonEventArgs e, Visual3D[] ignoreVisuals)
        {
            // Fire a ray at the mouse point
            Point clickPoint = e.GetPosition(_visual);

            RayHitTestParameters clickRay;
            List<MyHitTestResult> hits = UtilityWPF.CastRay(out clickRay, clickPoint, _visual, _camera, _viewport, true, ignoreVisuals);

            // See if they clicked on something
            var clickedItem = GetHit(hits);
            if (clickedItem != null)
            {
                ChangeSelectedItem(clickedItem, clickPoint);
            }
            else
            {
                UnselectItem();
            }

            // Update the drag plane
            ChangeDragPlane(clickRay);
        }
        public void MouseMove(MouseEventArgs e)
        {
            if (_dragPlane != null)
            {
                DragItem(e.GetPosition(_visual));
            }
        }
        public void LeftMouseUp(MouseButtonEventArgs e)
        {
            if (_selectedItem != null)
            {
                _selectedItem.StopDragging();
            }

            ClearDebugVisuals();

            _dragPlane = null;
            _dragHistory.Clear();
        }

        public void UnselectItem()
        {
            if (_selectedItem != null)
            {
                _selectedItem.Item.PhysicsBody.Disposing -= new EventHandler(PhysicsBody_Disposing);

                // Let overridden versions of the selected items remove any selection visuals
                _selectedItem.Dispose();
            }

            ClearDebugVisuals();

            _selectedItem = null;
            _dragPlane = null;
            _dragHistory.Clear();
        }

        #endregion
        #region Protected Methods

        /// <summary>
        /// This informs the caller that an item was selected, and gives them a chance to instantiate an overriden
        /// SelectedItems instance that holds custom selection visuals
        /// </summary>
        protected virtual MapObject_ChasePoint_Direct OnItemSelected(IMapObject item, Vector3D offset, Point clickPoint)
        {
            if (this.ItemSelected == null)
            {
                // No listeners
                return new MapObject_ChasePoint_Direct(item, offset, this.ShouldMoveItemWithSpring, this.ShouldSpringCauseTorque, this.ShouldDampenWhenSpring, _viewport, this.SpringColor);
            }

            ItemSelectedArgs args = new ItemSelectedArgs(item, offset, clickPoint, this.ShouldMoveItemWithSpring, this.ShouldSpringCauseTorque, this.SpringColor);

            // Raise the event
            this.ItemSelected(this, args);

            // See if they created a custom instance
            if (args.Requested_SelectedItem_Instance != null)
            {
                return args.Requested_SelectedItem_Instance;
            }
            else
            {
                return new MapObject_ChasePoint_Direct(item, offset, this.ShouldMoveItemWithSpring, this.ShouldSpringCauseTorque, this.ShouldDampenWhenSpring, _viewport, this.SpringColor);
            }
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_Disposing(object sender, EventArgs e)
        {
            UnselectItem();
        }

        #endregion

        #region Private Methods

        private Tuple<IMapObject, MyHitTestResult> GetHit(List<MyHitTestResult> hits)
        {
            // Get the list of selectable items
            IMapObject[] candidates = _selectableTypes.
                SelectMany(o => _map.GetItems(o, true)).
                Where(o => o.Visuals3D != null).
                ToArray();

            foreach (var hit in hits)		// hits are sorted by distance, so this method will only return the closest match
            {
                Visual3D visualHit = hit.ModelHit.VisualHit;
                if (visualHit == null)
                {
                    continue;
                }

                // See if this visual is part of one of the candidates
                IMapObject item = candidates.Where(o => o.Visuals3D.Any(p => p == visualHit)).FirstOrDefault();

                if (item != null)
                {
                    return Tuple.Create(item, hit);
                }
            }

            return null;
        }

        /// <summary>
        /// This is called whenever an item has been clicked on.  It figures out if it's a newly selected item, or the
        /// currently selected item, and updates accordingly
        /// </summary>
        private void ChangeSelectedItem(Tuple<IMapObject, MyHitTestResult> item, Point clickPoint)
        {
            Vector3D offset = item.Item2.Point - item.Item1.PositionWorld;

            if (_selectedItem != null && _selectedItem.Item == item.Item1)
            {
                // They reclicked on the same item.  Just update the camera position
                _selectedItem.Offset = offset;
                return;
            }

            // Remove the old
            UnselectItem();

            // Inform the world, giving them a chance to create selection visuals (and store those visuals in an overridden SelectedItem instance)
            MapObject_ChasePoint_Direct selectedItem = OnItemSelected(item.Item1, offset, clickPoint);

            // Store the new
            _selectedItem = selectedItem;

            // Need to hook to this in case the body is disposed while it is selected
            item.Item1.PhysicsBody.Disposing += new EventHandler(PhysicsBody_Disposing);
        }

        private void ChangeDragPlane(RayHitTestParameters clickRay)
        {
            _dragHistory.Clear();

            if (_selectedItem == null)
            {
                _dragPlane = null;
                return;
            }

            //NOTE: This was copied from Game.Newt.v2.GameItems.ShipEditor.Editor.ChangeDragHitShape()

            Point3D point = _selectedItem.Item.PositionWorld;

            RayHitTestParameters cameraLookCenter = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth * .5d, _viewport.ActualHeight * .5d));

            // Come up with the right plane
            Vector3D standard = Math3D.GetArbitraryOrhonganal(cameraLookCenter.Direction);
            Vector3D orth = Vector3D.CrossProduct(standard, cameraLookCenter.Direction);
            ITriangle plane = new Triangle(point, point + standard, point + orth);

            _dragPlane = new DragHitShape();
            _dragPlane.SetShape_Plane(plane);

            _dragMouseDownClickRay = clickRay;
            _dragMouseDownCenterRay = new RayHitTestParameters(point, clickRay.Direction);		//TODO: the ray through the center of the part really isn't parallel to the click ray (since the perspective camera sees in a cone)
        }
        /// <summary>
        /// This overload uses the current plane, but places it at a different point
        /// </summary>
        private void ChangeDragPlane(Point3D point)
        {
            if (_selectedItem == null || _dragPlane == null)
            {
                throw new InvalidOperationException("This method is only allowed if the drag plane currently exists");
            }

            //TODO: May get some mathmatical drift over time

            // Get the current
            ITriangle current;
            _dragPlane.GetShape_Plane(out current);

            Vector3D direction01 = (current.Point1 - current.Point0).ToUnit();
            Vector3D direction02 = (current.Point2 - current.Point0).ToUnit();

            Triangle newTriangle = new Triangle(point, point + direction01, point + direction02);
            _dragPlane.SetShape_Plane(newTriangle);
        }

        private void DragItem(Point clickPoint)
        {
            bool usingAdaptive = this.UseAdaptiveDragPlane && !Keyboard.IsKeyDown(Key.LeftShift);

            // Don't do this here
            //if (usingAdaptive)
            //{
            //    // Refresh the plane before casting a ray onto it.
            //    // (mousemove will only fire when they actually move the mouse, so the item could be off the plane that _dragPlane knows
            //    // about)
            //    // When not using adaptive, the item needs to be dragged strictly onto the plane from when they first started dragging.  But
            //    // adaptive tries to drag the item into other items that are off the plane


            //    //TODO: I think this is a bit flawed.  It can cause the drag plane to move perpendicular to itself too easily at times

            //    ChangeDragPlane();
            //}

            #region Fire Ray

            //NOTE: This was copied from Game.Newt.v2.GameItems.ShipEditor.Editor.ChangeDragHitShape()

            RayHitTestParameters mouseRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);
            Point3D? hitPoint;
            if (_selectedItem.IsUsingSpring)
            {
                hitPoint = _dragPlane.CastRay(mouseRay);
            }
            else
            {
                hitPoint = _dragPlane.CastRay(_dragMouseDownClickRay, _dragMouseDownCenterRay, mouseRay, _camera, _viewport);
            }

            #endregion

            if (hitPoint == null)
            {
                return;
            }

            Point3D adjustedHitPoint = hitPoint.Value;

            if (usingAdaptive)
            {
                adjustedHitPoint = DragItem_Adaptive(hitPoint.Value);
            }

            _dragHistory.Add(adjustedHitPoint);

            //NOTE: SelectedItem may set the position directly, or apply a spring force toward this point passed in
            _selectedItem.SetPosition(adjustedHitPoint);

            #region NOT USED

            //There is no rotation for a plane
            //Quaternion? rotationDelta = _dragPlane.GetRotation(clickPoint, hitPoint.Value);
            //if (rotationDelta != null)
            //{
            //    Quaternion newRotation = Quaternion.Multiply(rotationDelta.Value.ToUnit(), _mouseDownDragOrientation);
            //    _selectedItem.Orientation = newRotation;
            //}

            #endregion
        }

        private Point3D DragItem_Adaptive(Point3D hitPoint)
        {
            const double MINDOT_LEFTRIGHT = .97d;
            const double MINDOT_UPDOWN = .8d;

            if (_showDebugVisuals)
            {
                ClearDebugVisuals();
            }

            // Figure out what direction and how fast they are dragging the item (not how fast the item is moving, how fast the mouse is
            // projected in 3D)
            Vector3D? dragVelocity = GetDragVelocity();

            if (dragVelocity == null)
            {
                // Don't know the direction yet, so no special logic needed
                return hitPoint;
            }

            //if (_showDebugVisuals)
            //{
            //    ShowLine(hitPoint, hitPoint + dragVelocity.Value, Colors.Gold);
            //}

            double dragSpeed = dragVelocity.Value.Length;

            Point3D itemPosition = _selectedItem.Item.PositionWorld;

            ITriangle dragPlane;
            _dragPlane.GetShape_Plane(out dragPlane);

            // Figure out how far out to look
            double radius = GetSearchRadius(dragPlane, dragSpeed);

            //if (_showDebugVisuals)
            //{
            //    ShowLine(hitPoint, hitPoint + new Vector3D(radius, 0, 0), Colors.Blue);
            //}

            // Look along that direction for other items - filter out items that are too far from the drag plane
            Point3D[] allNearby = GetNearbyObjects(itemPosition, radius);
            if (allNearby.Length == 0)
            {
                return hitPoint;
            }

            // Take the dot product of the drag vector with the direction to each item
            //NOTE: By filtering with dot product, items that are too steep from the drag plane are also ignored
            var nearby = GetFilteredNearby(allNearby, itemPosition, dragVelocity.Value, dragPlane, MINDOT_LEFTRIGHT, MINDOT_UPDOWN);
            if (nearby.Length == 0)
            {
                return hitPoint;
            }

            if (_showDebugVisuals)
            {
                ShowLine(nearby.Select(o => Tuple.Create(itemPosition, o.Item1)), Colors.Gray);
            }

            // Get the average direction along the plane of (drag line x drag plane normal)
            Vector3D? averageDirectionUnit = GetScaledDirection(nearby);
            if (averageDirectionUnit == null)
            {
                return hitPoint;
            }

            // Compare the previous hit point with the proposed hit point, and make sure it is the appropriate slope away from the drag plane
            //NOTE: Not using the average velocity, just the last point.  Execution won't get here if _dragHistory is empty
            Point3D? newHitPoint = GetOffsetDirection(dragPlane, _dragHistory[_dragHistory.Count - 1], hitPoint, averageDirectionUnit.Value);
            if (newHitPoint == null)
            {
                return hitPoint;
            }

            // Change the drag plane to be at the new height.  The direction stays the same, only the intersect point changes
            //NOTE: My first attempt was to set the drag point at the item's location each time outside of this method, but it got get unstable.  This
            //way, the item could oscillate up and down out of the plane, but the plane will follow the slope of the line to the nearest item
            ChangeDragPlane(newHitPoint.Value);

            // Exit Function
            return newHitPoint.Value;
        }
        private Vector3D? GetDragVelocity()
        {
            if (_dragHistory.Count > 6)
            {
                // Don't let the history get too big
                _dragHistory.RemoveAt(0);
            }

            if (_dragHistory.Count < 3)
            {
                // Not enough data
                return null;
            }

            Vector3D retVal = new Vector3D(0, 0, 0);

            // Add up all the instantaneous velocities
            for (int cntr = 0; cntr < _dragHistory.Count - 1; cntr++)
            {
                retVal += _dragHistory[cntr + 1] - _dragHistory[cntr];
            }

            // Take the average
            return retVal / (_dragHistory.Count - 1);
        }
        private double GetSearchRadius(ITriangle dragPlane, double dragSpeed)
        {
            // Get the difference from the camera to the drag plane, this will determine how far out to look for items
            // Use plane distance as a rough estimate of how far the camera can see side to side
            double planeDistance = Math.Abs(Math3D.DistanceFromPlane(dragPlane, _camera.Position));
            planeDistance *= .5d;

            // Drag speed is tiny, so just return what the camera can see
            return planeDistance;



            //double dragScale = dragSpeed * 10;

            //if (dragScale < planeDistance)
            //{
            //    // They are dragging slowly, don't project too far
            //    return dragScale;
            //}
            //else
            //{
            //    // Doesn't matter how fast they're dragging.  Don't project farther than they can see
            //    return planeDistance;
            //}
        }
        private Point3D[] GetNearbyObjects(Point3D center, double radius)
        {
            //TODO: filter what they can drag toward
            //TODO: frequent requests should reuse results of a prev call

            MapOctree snapshot = _map.LatestSnapshot;
            if (snapshot != null)
            {
                // The snapshot is designed for this kind of request, so use it
                return snapshot.GetItems(center, radius).
                    Where(o => o.MapObject != _selectedItem.Item).      // don't return the item behing dragged
                    Select(o => o.Position).ToArray();
            }

            double radSquared = radius * radius;

            // Do a brute force scan of all objects
            return _map.GetAllItems().
                Where(o => o != _selectedItem.Item).        // don't return the item behing dragged
                Select(o => o.PhysicsBody.Position).
                Where(o => (center - o).LengthSquared <= radSquared).
                ToArray();
        }
        private static Tuple<Point3D, Vector3D>[] GetFilteredNearby(Point3D[] nearby, Point3D position, Vector3D dragVelocity, ITriangle dragPlane, double minDot_LeftRight, double minDot_UpDown)
        {
            List<Tuple<Point3D, Vector3D>> retVal = new List<Tuple<Point3D, Vector3D>>();

            // Get the portion of the drag velocity that is along the plane, then convert to a unit vector            
            Vector3D? velocity = GetVectorAlongPlaneUnit(dragVelocity, dragPlane);
            if (velocity == null)
            {
                return retVal.ToArray();
            }

            for (int cntr = 0; cntr < nearby.Length; cntr++)
            {
                Vector3D directionToPoint = nearby[cntr] - position;

                DoubleVector directionSplit = Math3D.SplitVector(directionToPoint, dragPlane);

                #region Left/Right

                if (Math3D.IsNearZero(directionSplit.Standard))
                {
                    continue;
                }

                Vector3D directionAlongPlaneUnit = directionSplit.Standard.ToUnit();

                // Compare left/right (it's more restrictive)
                double dotLeftRight = Vector3D.DotProduct(directionAlongPlaneUnit, velocity.Value);
                if (dotLeftRight < minDot_LeftRight)
                {
                    continue;
                }

                #endregion
                #region Up/Down

                // Get the portion along the velocity, then add the portion away from the plane (which eliminates the left/right part)
                Vector3D directionUpDownUnit = directionToPoint.GetProjectedVector(velocity.Value) + directionSplit.Orth;
                if (Math3D.IsNearZero(directionUpDownUnit))
                {
                    continue;
                }

                directionUpDownUnit.Normalize();

                // Compare up/down
                double dotUpDown = Vector3D.DotProduct(directionUpDownUnit, velocity.Value);
                if (dotUpDown < minDot_UpDown)
                {
                    continue;
                }

                #endregion

                // Add it (only storing the up/down portion, because that's all that's needed)
                retVal.Add(Tuple.Create(nearby[cntr], directionUpDownUnit));
            }

            return retVal.ToArray();
        }
        private static Vector3D? GetVectorAlongPlaneUnit(Vector3D vector, ITriangle plane)
        {
            DoubleVector split = Math3D.SplitVector(vector, plane);

            if (Math3D.IsNearZero(split.Standard))
            {
                return null;
            }

            return split.Standard.ToUnit();
        }
        private static Vector3D? GetScaledDirection(Tuple<Point3D, Vector3D>[] nearby)
        {
            Vector3D retVal = new Vector3D(0, 0, 0);

            for (int cntr = 0; cntr < nearby.Length; cntr++)
            {
                retVal += nearby[cntr].Item2;
            }

            if (Math3D.IsNearZero(retVal))
            {
                return null;
            }

            return retVal.ToUnit();
        }
        private Point3D? GetOffsetDirection(ITriangle dragPlane, Point3D prevPoint, Point3D curPoint, Vector3D slopeUnit)
        {
            Vector3D velocity = curPoint - prevPoint;
            if (Math3D.IsNearZero(velocity))
            {
                return null;
            }

            // Divide it up (doesn't matter what the orth part is, that's throw away.  Only keeping the part along the plane)
            DoubleVector velocitySplit = Math3D.SplitVector(velocity, dragPlane);

            Vector3D slopeScaled = slopeUnit * velocity.Length;     // slope is already a unit vector
            DoubleVector slopeSplit = Math3D.SplitVector(slopeScaled, dragPlane);

            // Rebuild so that the returned point has the appropriate slope away from the plane

            Vector3D velocityNew = velocitySplit.Standard + slopeSplit.Orth;

            return prevPoint + velocityNew;
        }

        private void ShowLine(Point3D point1, Point3D point2, Color color)
        {
            ScreenSpaceLines3D line = new ScreenSpaceLines3D();
            line.Color = color;
            line.Thickness = 1;
            line.AddLine(point1, point2);

            _debugVisuals.Add(line);
            _viewport.Children.Add(line);
        }
        private void ShowLine(IEnumerable<Tuple<Point3D, Point3D>> lines, Color color)
        {
            ScreenSpaceLines3D line = new ScreenSpaceLines3D();
            line.Color = color;
            line.Thickness = 1;

            foreach (var segment in lines)
            {
                line.AddLine(segment.Item1, segment.Item2);
            }

            _debugVisuals.Add(line);
            _viewport.Children.Add(line);
        }
        private void ClearDebugVisuals()
        {
            if (_debugVisuals.Count > 0)
            {
                foreach (Visual3D visual in _debugVisuals)
                {
                    _viewport.Children.Remove(visual);

                    if (visual is IDisposable)
                    {
                        ((IDisposable)visual).Dispose();
                    }
                }

                _debugVisuals.Clear();
            }
        }

        private static Tuple<Vector3D?, Vector3D?> SplitVectorUnit(Vector3D vector, ITriangle plane)
        {
            DoubleVector split = Math3D.SplitVector(vector, plane);

            Vector3D? standardUnit = null;
            if (!Math3D.IsNearZero(split.Standard))
            {
                standardUnit = split.Standard.ToUnit();
            }

            Vector3D? orthUnit = null;
            if (!Math3D.IsNearZero(split.Orth))
            {
                orthUnit = split.Orth.ToUnit();
            }

            return Tuple.Create(standardUnit, orthUnit);
        }

        #region Adaptive - OLD

        //private Point3D DragItem_Adaptive(Point3D hitPoint)
        //{
        //    const double MINDOT = .5d;

        //    // Figure out what direction and how fast they are dragging the item (not how fast the item is moving, how fast the mouse is
        //    // projected in 3D)
        //    Vector3D? dragVelocity = GetDragVelocity(hitPoint);

        //    if (dragVelocity == null)
        //    {
        //        // Don't know the direction yet, so no special logic needed
        //        return hitPoint;
        //    }

        //    double dragSpeed = dragVelocity.Value.Length;

        //    Point3D itemPosition = _selectedItem.Item.PositionWorld;

        //    ITriangle dragPlane;
        //    _dragPlane.GetShape_Plane(out dragPlane);

        //    // Figure out how far out to look
        //    double radius = GetSearchRadius(dragPlane, dragSpeed);

        //    // Look along that direction for other items - filter out items that are too far from the drag plane
        //    Point3D[] allNearby = GetNearbyObjects(itemPosition, radius);
        //    if (allNearby.Length == 0)
        //    {
        //        return hitPoint;
        //    }

        //    // Take the dot product of the drag vector with the direction to each item
        //    //NOTE: By filtering with dot product, items that are too steep from the drag plane are also ignored
        //    Tuple<Point3D, Vector3D, double>[] nearby = GetFilteredNearby(allNearby, itemPosition, dragVelocity.Value, MINDOT);
        //    if (nearby.Length == 0)
        //    {
        //        return hitPoint;
        //    }

        //    // Get a scaled average of the direction along the drag line
        //    Vector3D? averageDirection = GetScaledDirection(nearby);
        //    if (averageDirection == null)
        //    {
        //        return hitPoint;
        //    }

        //    // Adjust the hit point perp to the drag plane so that it will intersect with the item they are dragging toward
        //    Vector3D? offsetDirection = GetOffsetDirection(dragPlane, dragSpeed, averageDirection.Value);
        //    if (offsetDirection == null)
        //    {
        //        return hitPoint;
        //    }

        //    return hitPoint + offsetDirection.Value;
        //}
        //private Vector3D? GetDragVelocity(Point3D hitPoint)
        //{
        //    _dragHistory.Add(hitPoint);

        //    if (_dragHistory.Count > 4)
        //    {
        //        // Don't let the history get too big
        //        _dragHistory.RemoveAt(0);
        //    }

        //    if (_dragHistory.Count < 3)
        //    {
        //        // Not enough data
        //        return null;
        //    }

        //    Vector3D retVal = new Vector3D(0, 0, 0);

        //    // Add up all the instantaneous velocities
        //    for (int cntr = 0; cntr < _dragHistory.Count - 1; cntr++)
        //    {
        //        retVal += _dragHistory[cntr + 1] - _dragHistory[cntr];
        //    }

        //    // Take the average
        //    return retVal / (_dragHistory.Count - 1);
        //}
        //private double GetSearchRadius(ITriangle dragPlane, double dragSpeed)
        //{
        //    // Get the difference from the camera to the drag plane, this will determine how far out to look for items
        //    // Use plane distance as a rough estimate of how far the camera can see side to side
        //    double planeDistance = Math3D.GetPlaneDistance(dragPlane.NormalUnit, _camera.Position);

        //    double dragScale = dragSpeed * 10;

        //    if (dragScale < planeDistance)
        //    {
        //        // They are dragging slowly, don't project too far
        //        return dragScale;
        //    }
        //    else
        //    {
        //        // Doesn't matter how fast they're dragging.  Don't project farther than they can see
        //        return planeDistance;
        //    }
        //}
        //private Point3D[] GetNearbyObjects(Point3D center, double radius)
        //{
        //    //TODO: filter what they can drag toward
        //    //TODO: frequent requests should reuse results of a prev call

        //    MapOctree snapshot = _map.LatestSnapshot;
        //    if (snapshot != null)
        //    {
        //        // The snapshot is designed for this kind of request, so use it
        //        return snapshot.GetItems(center, radius).
        //            Where(o => o.MapObject != _selectedItem.Item).      // don't return the item behing dragged
        //            Select(o => o.Position).ToArray();
        //    }

        //    double radSquared = radius * radius;

        //    // Do a brute force scan of all objects
        //    return _map.GetAllObjects().
        //        Where(o => o != _selectedItem.Item).        // don't return the item behing dragged
        //        Select(o => o.PhysicsBody.Position).
        //        Where(o => (center - o).LengthSquared <= radSquared).
        //        ToArray();
        //}
        //private static Tuple<Point3D, Vector3D, double>[] GetFilteredNearby(Point3D[] nearby, Point3D position, Vector3D dragVelocity, double minDot)
        //{
        //    List<Tuple<Point3D, Vector3D, double>> retVal = new List<Tuple<Point3D, Vector3D, double>>();

        //    Vector3D dragDirectionUnit = dragVelocity.ToUnit();

        //    for (int cntr = 0; cntr < nearby.Length; cntr++)
        //    {
        //        Vector3D directionToPoint = nearby[cntr] - position;
        //        directionToPoint.Normalize();

        //        double dot = Vector3D.DotProduct(directionToPoint, dragDirectionUnit);

        //        if (dot >= minDot)
        //        {
        //            retVal.Add(Tuple.Create(nearby[cntr], directionToPoint, dot));
        //        }
        //    }

        //    return retVal.ToArray();
        //}
        //private static Vector3D? GetScaledDirection(Tuple<Point3D, Vector3D, double>[] nearby)
        //{
        //    //NOTE: nearby.Item2 needs to be a unit vector already

        //    Vector3D retVal = new Vector3D(0, 0, 0);

        //    for (int cntr = 0; cntr < nearby.Length; cntr++)
        //    {
        //        retVal += nearby[cntr].Item2 * nearby[cntr].Item3;
        //    }

        //    if (Math3D.IsNearZero(retVal))
        //    {
        //        return null;
        //    }

        //    return retVal.ToUnit();
        //}
        //private static Vector3D? GetOffsetDirection(ITriangle dragPlane, double dragSpeed, Vector3D offsetUnit)
        //{
        //    // Get the portion of offsetUnit that is along the drag plane
        //    double dot = Vector3D.DotProduct(dragPlane.NormalUnit, offsetUnit);

        //    if (Math3D.IsNearZero(dot))
        //    {
        //        return null;
        //    }

        //    return offsetUnit * Math.Abs(dot) * dragSpeed;
        //}

        #endregion

        #endregion
    }

    #region class: ItemSelectedArgs

    public class ItemSelectedArgs : EventArgs
    {
        public ItemSelectedArgs(IMapObject item, Vector3D offset, Point clickPoint, bool shouldMoveItemWithSpring, bool shouldSpringCauseTorque, Color? springColor)
        {
            this.Item = item;
            this.Offset = offset;
            this.ClickPoint = clickPoint;
            this.ShouldMoveItemWithSpring = shouldMoveItemWithSpring;
            this.ShouldSpringCauseTorque = shouldSpringCauseTorque;
            this.SpringColor = springColor;
        }

        public readonly IMapObject Item;
        public readonly Vector3D Offset;
        public readonly Point ClickPoint;

        public readonly bool ShouldMoveItemWithSpring;
        public readonly bool ShouldSpringCauseTorque;
        public readonly Color? SpringColor;

        /// <summary>
        /// This gives the listener a chance to populate a class that is derived from SelectedItem.  If this is still null when the event comes back, a
        /// standard SelectedItem base class will be created
        /// </summary>
        /// <remarks>
        /// This is not the most intuitive design, but I couldn't think of a better way
        /// </remarks>
        public MapObject_ChasePoint_Direct Requested_SelectedItem_Instance
        {
            get;
            set;
        }
    }

    #endregion
}
