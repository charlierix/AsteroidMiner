using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public class Selector
    {
        #region Enum: SelectionMode

        private enum SelectionMode
        {
            None,
            Rectangle,
            Selected		//TODO:  Add resize boxes, and rotate handle
        }

        #endregion
        #region Enum: MouseButtonDown

        private enum MouseButtonDown
        {
            None = 0,
            Left,
            Right		//TODO:  differentiate between velocity set and angular velocity set (double click right)
        }

        #endregion

        #region Class: MousePosition

        private class MousePosition
        {
            public int Tick = int.MinValue;
            public MyVector Position = null;

            public MousePosition(MyVector position)
            {
                this.Tick = Environment.TickCount;
                this.Position = position;
            }
        }

        #endregion

        #region Declaration Section

        // Misc global objects
        private LargeMapViewer2D _picturebox = null;
        private SimpleMap _map = null;
        private ObjectRenderer _renderer = null;

        private bool _active = false;
        private SelectionMode _mode = SelectionMode.None;

        private MouseButtonDown _isMouseDown = MouseButtonDown.None;

        // Where the mouse was pushed
        private MyVector _mouseDownPoint = null;
        private MyVector _curMousePoint = null;
        private List<MousePosition> _prevMousePositions = new List<MousePosition>();		// This one is only used during a left drag

        // Currently selected objects
        private List<long> _selectedObjects = new List<long>();
        private SortedList<long, MyVector> _draggingPositionOffsets = new SortedList<long, MyVector>();		// this only has meaning while the left mouse is down, and mode is selected

        // While objects are being dragged (not simply selected, but dragged), they are changed into
        // stationary so that they collide believably.  But as soon as the user lets go, they need to be changed
        // back into standard.  This list remembers those objects.
        private List<long> _tempStationaryObjects = new List<long>();

        // These are keys that I care about
        private bool _isShiftPressed = false;
        private bool _isCtrlPressed = false;

        // This gets updated every time the mouse move occurs
        private int _lastMouseMove = Environment.TickCount;

        //TODO:  Move this to the main form
        private List<RadarBlip> _myClipboard = new List<RadarBlip>();
        private MyVector _clipboardPositionCenter = null;		// this is the center of position of the objects stored in the clipboard.  That way they can be centered on the mouse during paste

        private List<long> _cantDeleteTokens = null;

        #endregion

        #region Constructor

        public Selector(LargeMapViewer2D picturebox, SimpleMap map, ObjectRenderer renderer, List<long> cantDeleteTokens)
        {
            _picturebox = picturebox;
            _map = map;
            _renderer = renderer;
            _cantDeleteTokens = cantDeleteTokens;

            _picturebox.MouseDown += new MouseEventHandler(picturebox_MouseDown);
            _picturebox.MouseUp += new MouseEventHandler(picturebox_MouseUp);
            _picturebox.MouseMove += new MouseEventHandler(picturebox_MouseMove);

            _picturebox.KeyDown += new KeyEventHandler(picturebox_KeyDown);
            _picturebox.KeyUp += new KeyEventHandler(picturebox_KeyUp);
        }

        #endregion

        #region Public Properties

        public bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                _active = value;

                _isMouseDown = MouseButtonDown.None;

                //TODO:  Don't do this here, but make a public method to do this (so they can switch to pan, gravmouse,
                // etc modes, and not lose all the selected objects)
                _selectedObjects.Clear();
            }
        }

        public long[] SelectedObjects
        {
            get
            {
                return _selectedObjects.ToArray();
            }
        }

        public List<long> TempStationaryObjects
        {
            get
            {
                return _tempStationaryObjects;
            }
        }

        #endregion

        #region Public Methods

        public bool IsSelected(long token)
        {
            return _selectedObjects.Contains(token);
        }

        /// <summary>
        /// This will help me adjust the velocity of any objects that are being physically dragged around
        /// </summary>
        public void TimerTick(double elapsedTime)
        {
            if (!(_isMouseDown == MouseButtonDown.Left && _mode == SelectionMode.Selected))
            {
                // They aren't actively dragging objects around.  There is nothing for this function to do.
                return;
            }

            // Figure out what their velocity should be
            MyVector newVelocity = TimerSprtCalculateVelocity();

            // Physics may have knocked the objects around.  Put them all where they are supposed to be
            foreach (RadarBlip blip in _map.GetAllBlips())
            {
                if (_selectedObjects.Contains(blip.Token))
                {
                    // Enforce the new position
                    blip.Sphere.Position.StoreNewValues(_curMousePoint + _draggingPositionOffsets[blip.Token]);

                    // Give them all the same velocity
                    if (blip.Sphere is Ball)
                    {
                        ((Ball)(blip.Sphere)).Velocity.StoreNewValues(newVelocity);
                    }
                }
            }
        }

        public void Draw()
        {
            if ((_isShiftPressed || _isCtrlPressed) && _myClipboard.Count > 0)
            {
                double opacity = GetPastingBallsOpacity();

                if (opacity > 0d)
                {
                    #region Draw Pasting Balls

                    Ball tempBall = null;

                    foreach (BallBlip blip in _myClipboard)
                    {
                        // Clone the ball
                        tempBall = blip.Ball.CloneBall();

                        // Figure out where to place the pasted object
                        MyVector newPosition = tempBall.Position - _clipboardPositionCenter;
                        newPosition.Add(_curMousePoint);
                        tempBall.Position.StoreNewValues(newPosition);

                        // Draw it
                        if (tempBall is SolidBall)
                        {
                            _renderer.DrawSolidBall((SolidBall)tempBall, ObjectRenderer.DrawMode.Building, blip.CollisionStyle, false, opacity);
                        }
                        else
                        {
                            _renderer.DrawBall(tempBall, ObjectRenderer.DrawMode.Building, blip.CollisionStyle, false, opacity);
                        }
                    }

                    #endregion
                }
            }

            if (_isMouseDown == MouseButtonDown.Left && _mode == SelectionMode.Rectangle)
            {
                #region Draw Rectangle

                MyVector topLeft, bottomRight;
                GetProperCorners(out topLeft, out bottomRight, _mouseDownPoint, _curMousePoint);

                _picturebox.DrawRectangle(Color.Silver, DashStyle.Dash, 1d, topLeft, bottomRight);

                #endregion
            }
            else if (_isMouseDown == MouseButtonDown.Right)
            {
                // Draw Velocity
                _picturebox.DrawLine(Color.Chartreuse, 12d, _mouseDownPoint, _curMousePoint);
            }

            //TODO:  draw the angular velocity

        }

        #endregion

        #region Misc Control Events

        void picturebox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_active)
            {
                return;
            }

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                _mouseDownPoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));
                _curMousePoint = _mouseDownPoint.Clone();
                _prevMousePositions.Clear();
                _tempStationaryObjects.Clear();		// this should be cleared by now anyway
            }

            if (e.Button == MouseButtons.Left)
            {
                #region Left

                _isMouseDown = MouseButtonDown.Left;

                // Get all the blips
                List<RadarBlip> remainingBlips = new List<RadarBlip>(_map.GetAllBlips());

                bool selectedPrevious = false;
                #region See if they selected one of the previously selected objects

                foreach (long token in _selectedObjects)
                {
                    RadarBlip blip = FindAndRemove(token, remainingBlips);

                    if (blip == null)
                    {
                        continue;
                    }

                    if (selectedPrevious)
                    {
                        // I just wanted to remove this blip from the total list
                        continue;
                    }

                    if (SelectionTest(blip, _curMousePoint))
                    {
                        selectedPrevious = true;
                    }
                }

                #endregion

                // Check for ctrl or shift key being pressed (if they are, don't clear the previous)
                if (!selectedPrevious && !(_isShiftPressed || _isCtrlPressed))
                {
                    _selectedObjects.Clear();
                }

                bool selectedNew = false;
                #region See if they clicked on any other objects

                foreach (RadarBlip blip in remainingBlips)
                {
                    if (SelectionTest(blip, _curMousePoint))
                    {
                        _selectedObjects.Add(blip.Token);
                        selectedNew = true;
                    }
                }

                #endregion

                // Set my mode
                if (selectedPrevious || selectedNew)
                {
                    _mode = SelectionMode.Selected;

                    #region Rebuild the offsets list (and temp stationary)

                    _draggingPositionOffsets.Clear();
                    foreach (RadarBlip blip in _map.GetAllBlips())
                    {
                        if (_selectedObjects.Contains(blip.Token))
                        {
                            _draggingPositionOffsets.Add(blip.Token, blip.Sphere.Position - _curMousePoint);

                            if (blip.CollisionStyle == CollisionStyle.Standard)
                            {
                                _tempStationaryObjects.Add(blip.Token);
                                blip.CollisionStyle = CollisionStyle.Stationary;
                            }
                        }
                    }

                    #endregion
                }
                else
                {
                    _mode = SelectionMode.Rectangle;
                }

                #endregion
            }
            else if (e.Button == MouseButtons.Right)
            {
                #region Right

                if (_mode == SelectionMode.Selected && _selectedObjects.Count > 0)
                {
                    _isMouseDown = MouseButtonDown.Right;
                }
                else
                {
                    // If nothing is selected, then there is nothing for me to do
                    _isMouseDown = MouseButtonDown.None;
                }

                #endregion
            }
        }
        void picturebox_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_active)
            {
                return;
            }

            if (_isMouseDown == MouseButtonDown.Left && e.Button == MouseButtons.Left)
            {
                #region Left

                _isMouseDown = MouseButtonDown.None;
                _curMousePoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));

                switch (_mode)
                {
                    case SelectionMode.Rectangle:
                        #region Add Objects Inside Rectangle

                        // Add any objects that were in the rectangle (I don't need to look at ctrl or shift here, I'm just adding)
                        foreach (RadarBlip blip in _map.GetAllBlips())
                        {
                            if (!_selectedObjects.Contains(blip.Token))
                            {
                                if (SelectionTest(blip, _mouseDownPoint, _curMousePoint))
                                {
                                    _selectedObjects.Add(blip.Token);
                                }
                            }
                        }

                        #endregion
                        break;

                    case SelectionMode.Selected:
                        #region Clear temp stationary objects

                        // While dragging, their velocities were manipulated, now just let them fly.  (they're still selected)

                        // Turn any temp stationary objects back into standard objects
                        foreach (RadarBlip blip in _map.GetAllBlips())
                        {
                            if (_tempStationaryObjects.Contains(blip.Token))
                            {
                                blip.CollisionStyle = CollisionStyle.Standard;
                            }
                        }

                        _tempStationaryObjects.Clear();

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown SelectionMode: " + _mode);
                }

                // Set my mode
                if (_selectedObjects.Count > 0)
                {
                    _mode = SelectionMode.Selected;
                }
                else
                {
                    _mode = SelectionMode.None;
                }

                #endregion
            }
            else if (_isMouseDown == MouseButtonDown.Right && e.Button == MouseButtons.Right)
            {
                #region Right

                _isMouseDown = MouseButtonDown.None;
                _curMousePoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));

                // Apply the velocity to all the selected objects
                MyVector newVelocity = _curMousePoint - _mouseDownPoint;
                newVelocity.Divide(10);

                foreach (RadarBlip blip in _map.GetAllBlips())
                {
                    if (_selectedObjects.Contains(blip.Token))
                    {
                        if (blip is BallBlip)
                        {
                            ((BallBlip)blip).Ball.Velocity.StoreNewValues(newVelocity);
                        }
                    }
                }

                #endregion
            }

        }
        void picturebox_MouseMove(object sender, MouseEventArgs e)
        {
            _lastMouseMove = Environment.TickCount;

            // Remember the mouse position
            if (_curMousePoint != null && _prevMousePositions != null && _picturebox != null)
            {
                _prevMousePositions.Add(new MousePosition(_curMousePoint.Clone()));
                _curMousePoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));
            }

            if (!_active || _isMouseDown == MouseButtonDown.None)
            {
                return;
            }

            if (_isMouseDown == MouseButtonDown.Left)
            {
                #region Left

                switch (_mode)
                {
                    case SelectionMode.Rectangle:
                        // Nothing to do, when draw is called, the rectangle will correspond to the new position
                        break;

                    case SelectionMode.Selected:
                        // Move the objects to the new locations.  If they collide along the way, I will need to reset
                        // their positions during timer.
                        #region Set Positions

                        foreach (RadarBlip blip in _map.GetAllBlips())
                        {
                            if (_selectedObjects.Contains(blip.Token))
                            {
                                blip.Sphere.Position.StoreNewValues(_curMousePoint + _draggingPositionOffsets[blip.Token]);
                            }
                        }

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown SelectionMode: " + _mode);
                }

                #endregion
            }
        }

        void picturebox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_active)
            {
                return;
            }

            // NOTE:  This event fires A LOT while the key is being held down, so don't do anything expensive

            if (e.Shift)
            {
                _isShiftPressed = true;
            }

            if (e.Control)
            {
                _isCtrlPressed = true;
            }

            if ((e.KeyCode == Keys.C && e.Control) || (e.KeyCode == Keys.Insert && e.Control))
            {
                Copy();
            }
            else if ((e.KeyCode == Keys.V && e.Control) || (e.KeyCode == Keys.Insert && e.Shift))
            {
                Paste();
            }
            if ((e.KeyCode == Keys.X && e.Control) || (e.KeyCode == Keys.Delete && e.Shift))
            {
                Copy();
                foreach (long token in _selectedObjects)
                {
                    if (_cantDeleteTokens.Contains(token))
                    {
                        continue;
                    }

                    _map.Remove(token);
                }
            }
        }
        void picturebox_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_active)
            {
                return;
            }

            if (e.KeyData == Keys.ShiftKey)
            {
                _isShiftPressed = false;
            }

            if (e.KeyData == Keys.ControlKey)
            {
                _isCtrlPressed = false;
            }
        }

        #endregion

        #region Private Methods

        private bool SelectionTest(RadarBlip blip, MyVector point)
        {
            // Just worry about spheres for now

            MyVector line = point - blip.Sphere.Position;

            return line.GetMagnitude() <= blip.Sphere.Radius;
        }
        private bool SelectionTest(RadarBlip blip, MyVector rectCorner1, MyVector rectCorner2)
        {
            // Just worry about spheres for now

            // I'll just cheat and let the rectangle do the work for me.  There are several flaws with this, but it
            // works for now

            MyVector topLeft, bottomRight;
            GetProperCorners(out topLeft, out bottomRight, rectCorner1, rectCorner2);

            RectangleF rect = new RectangleF(topLeft.ToPointF(), new SizeF(Convert.ToSingle(bottomRight.X - topLeft.X), Convert.ToSingle(bottomRight.Y - topLeft.Y)));
            return rect.Contains(blip.Sphere.Position.ToPointF());
        }

        /// <summary>
        /// This function take the two corners passed in, and figures out which is topleft, and which is bottomright
        /// </summary>
        private void GetProperCorners(out MyVector topLeft, out MyVector bottomRight, MyVector vector1, MyVector vector2)
        {
            if (vector1.X <= vector2.X && vector1.Y <= vector2.Y)
            {
                topLeft = vector1;
                bottomRight = vector2;
            }
            else if (vector2.X <= vector1.X && vector2.Y <= vector1.Y)
            {
                topLeft = vector2;
                bottomRight = vector1;
            }
            else if (vector1.X <= vector2.X && vector2.Y <= vector1.Y)
            {
                topLeft = new MyVector(vector1.X, vector2.Y, 0);
                bottomRight = new MyVector(vector2.X, vector1.Y, 0);
            }
            else
            {
                topLeft = new MyVector(vector2.X, vector1.Y, 0);
                bottomRight = new MyVector(vector1.X, vector2.Y, 0);
            }
        }

        private RadarBlip FindAndRemove(long token, List<RadarBlip> blips)
        {
            for (int cntr = 0; cntr < blips.Count; cntr++)
            {
                if (blips[cntr].Token == token)
                {
                    RadarBlip retVal = blips[cntr];
                    blips.RemoveAt(cntr);
                    return retVal;
                }
            }

            return null;
        }

        /// <summary>
        /// This function prunes the list of positions that are too old.  It then figures out the average velocity
        /// of the remaining positions.
        /// </summary>
        private MyVector TimerSprtCalculateVelocity()
        {
            const int RETENTION = 75;		// milliseconds

            int curTick = Environment.TickCount;

            // Add the current to the list
            _prevMousePositions.Add(new MousePosition(_curMousePoint.Clone()));

            #region Prune List

            int lastIndex = -1;
            for (int cntr = _prevMousePositions.Count - 1; cntr >= 0; cntr--)
            {
                if (curTick - _prevMousePositions[cntr].Tick > RETENTION)
                {
                    // This item is too old.  And since the list is in time order, all the entries before this are also too old
                    lastIndex = cntr;
                    break;
                }
            }

            if (lastIndex >= 0)
            {
                //_prevMousePositions.RemoveRange(lastIndex, _prevMousePositions.Count - lastIndex);
                _prevMousePositions.RemoveRange(0, lastIndex + 1);
            }

            #endregion

            #region Calculate Velocity

            MyVector retVal = new MyVector(0, 0, 0);

            // Add up all the instantaneous velocities
            for (int cntr = 0; cntr < _prevMousePositions.Count - 1; cntr++)
            {
                retVal.Add(_prevMousePositions[cntr + 1].Position - _prevMousePositions[cntr].Position);
            }

            // Take the average
            if (_prevMousePositions.Count > 2)		// if count is 0 or 1, then retVal will still be (0,0,0).  If it's 2, then I would be dividing by 1, which is pointless
            {
                retVal.Divide(_prevMousePositions.Count - 1);
            }

            #endregion

            // Exit Function
            return retVal;
        }

        private void Copy()
        {
            if (_mode != SelectionMode.Selected || _selectedObjects.Count == 0)
            {
                return;
            }

            _myClipboard.Clear();
            _clipboardPositionCenter = new MyVector();

            foreach (RadarBlip blip in _map.GetAllBlips())
            {
                if (_selectedObjects.Contains(blip.Token))
                {
                    _myClipboard.Add(Scenes.CloneBlip(blip, _map));
                    _clipboardPositionCenter.Add(blip.Sphere.Position);
                }
            }

            if (_myClipboard.Count > 0)
            {
                _clipboardPositionCenter.Divide(_myClipboard.Count);
            }
        }
        private void Paste()
        {
            if (_myClipboard.Count == 0)
            {
                return;
            }

            _selectedObjects.Clear();

            foreach (RadarBlip blip in _myClipboard)
            {
                // Clone this object
                RadarBlip tempBlip = Scenes.CloneBlip(blip, _map);

                // Figure out where to place the pasted object
                MyVector newPosition = tempBlip.Sphere.Position - _clipboardPositionCenter;
                newPosition.Add(_curMousePoint);
                tempBlip.Sphere.Position.StoreNewValues(newPosition);

                // Add it
                _selectedObjects.Add(tempBlip.Token);
                _map.Add(tempBlip);
            }

            _mode = SelectionMode.Selected;
        }

        private double GetPastingBallsOpacity()
        {
            const int INITIALDELAY = 750;
            const int FADEDURATION = 300;

            int tickCount = Environment.TickCount;

            if (tickCount - _lastMouseMove <= INITIALDELAY)
            {
                return 1d;
            }

            if (tickCount - _lastMouseMove < INITIALDELAY + FADEDURATION)
            {
                int fadeElapse = FADEDURATION - (tickCount - _lastMouseMove - INITIALDELAY);		// I reversed it so I can run the result through the LERP function

                return UtilityHelper.GetScaledValue_Capped(0d, 1d, 0, FADEDURATION, fadeElapse);
            }

            return 0d;
        }

        #endregion
    }
}
