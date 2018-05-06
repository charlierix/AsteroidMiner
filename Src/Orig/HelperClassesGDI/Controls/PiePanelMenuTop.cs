using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Game.Orig.HelperClassesGDI.Controls
{
    /// <summary>
    /// This turns the pie panel into a tab control
    /// </summary>
    public partial class PiePanelMenuTop : PiePanelTop
    {
        #region class: PieButton

        private class PieButton
        {
            private string _name = "";
            public Bitmap Bitmap = null;
            public Rectangle Rectangle;
            public GraphicsPath Outline = null;

            public PieButton(string name)
            {
                _name = name;
                this.Rectangle = new Rectangle(0, 0, 1, 1);
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }
        }

        #endregion

        #region Events

        public event PieMenuButtonClickedHandler ButtonClicked = null;
        public event PieMenuDrawButtonHandler DrawButton = null;

        #endregion

        #region Declaration Section

        private int _buttonSize = 10;

        private List<PieButton> _buttons = new List<PieButton>();

        private int _clickedIndex = 0;
        private Brush _clickedBrush = null;
        private Pen _clickedPen = null;

        private int _hotTrack = -1;
        private Brush _hotTrackBrush = null;
        private Pen _hotTrackPen = null;

        #endregion

        #region Constructor

        public PiePanelMenuTop()
        {
            InitializeComponent();

            ResetHotTrackPens();
        }

        #endregion

        #region Public Properties

        public int ButtonSize
        {
            get
            {
                return _buttonSize;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This will add a button.  The buttons will be automatically sized and placed starting at the top left.
        /// The buttons will be made as big as possible to fit the available space (but they will all be the same
        /// size, and always square)
        /// </summary>
        /// <remarks>
        /// There is currently no remove or clear buttons (or rearrange).  I figure I'll add that functionality when
        /// it's needed.
        /// </remarks>
        public void AddButton(string name)
        {
            // Look for a button by that name
            foreach (PieButton button in _buttons)
            {
                if (button.Name == name)
                {
                    throw new ArgumentException("There is already a button by that name: " + name);
                }
            }

            // Add it
            _buttons.Add(new PieButton(name));

            RepositionButtons();
        }

        /// <summary>
        /// This tells me that the properties have changed, and I need to invalidate my buttons
        /// </summary>
        /// <remarks>
        /// TODO: If there is too much flicker, then this method should be broken into one for each button (but that would
        /// end up as a management headache for the calling class
        /// </remarks>
        public void PropsChanged()
        {
            for (int cntr = 0; cntr < _buttons.Count; cntr++)
            {
                DrawButton_Private(cntr);
            }

            this.Invalidate();
        }

        #endregion

        #region Protected Methods

        protected virtual void OnButtonClicked(int index, string name)
        {
            if (this.ButtonClicked != null)
            {
                this.ButtonClicked(this, new PieMenuButtonClickedArgs(index, name));
            }
        }

        protected virtual void OnDrawButton(int index, string name, int buttonSize, Graphics graphics)
        {
            if (this.DrawButton != null)
            {
                this.DrawButton(this, new PieMenuDrawButtonArgs(index, name, buttonSize, graphics));
            }
        }

        #endregion

        #region Overrides

        protected override void OnMouseDown(MouseEventArgs e)
        {
            //TODO, shift the location so it looks clicked

            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_hotTrack >= 0)
            {
                _clickedIndex = _hotTrack;
                OnButtonClicked(_hotTrack, _buttons[_hotTrack].Name);
                this.Invalidate();
            }

            base.OnMouseUp(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int oldHotTrack = _hotTrack;
            _hotTrack = -1;

            // See if they are over any of the buttons
            for (int cntr = 0; cntr < _buttons.Count; cntr++)
            {
                if (_buttons[cntr].Outline.IsVisible(e.X, e.Y))
                {
                    _hotTrack = cntr;
                    break;
                }
            }

            if (oldHotTrack != _hotTrack)
            {
                this.Invalidate();
            }

            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hotTrack != -1)
            {
                _hotTrack = -1;
                this.Invalidate();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            ResetHotTrackPens();

            base.OnBackColorChanged(e);
        }

        protected override void OnResize(EventArgs e)
        {
            RepositionButtons();

            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_buttons.Count == 0)
            {
                base.OnPaint(e);
            }
            else
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

                // Draw the buttons
                for (int cntr = 0; cntr < _buttons.Count; cntr++)
                {
                    if (_buttons[cntr].Bitmap == null || _buttons[cntr].Outline == null)
                    {
                        continue;
                    }
                    if (!_buttons[cntr].Rectangle.IntersectsWith(e.ClipRectangle))
                    {
                        continue;
                    }

                    // Background
                    if (cntr == _clickedIndex && _clickedBrush != null)
                    {
                        e.Graphics.FillPath(_clickedBrush, _buttons[cntr].Outline);
                    }
                    if (cntr == _hotTrack && _hotTrackBrush != null)
                    {
                        e.Graphics.FillPath(_hotTrackBrush, _buttons[cntr].Outline);
                    }

                    // Button Image
                    e.Graphics.DrawImageUnscaled(_buttons[cntr].Bitmap, _buttons[cntr].Rectangle.Location);

                    // Border
                    if (cntr == _clickedIndex && _clickedPen != null)
                    {
                        e.Graphics.DrawPath(_clickedPen, _buttons[cntr].Outline);
                    }
                    if (cntr == _hotTrack && _hotTrackPen != null)
                    {
                        e.Graphics.DrawPath(_hotTrackPen, _buttons[cntr].Outline);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void ResetHotTrackPens()
        {
            // Clicked
            if (_clickedBrush != null)
            {
                _clickedBrush.Dispose();
                _clickedBrush = null;
            }
            if (_clickedPen != null)
            {
                _clickedPen.Dispose();
                _clickedPen = null;
            }

            _clickedBrush = new SolidBrush(SystemColors.Window);
            _clickedPen = new Pen(UtilityGDI.AlphaBlend(SystemColors.HotTrack, this.BackColor, .33d));

            // HotTrack
            if (_hotTrackBrush != null)
            {
                _hotTrackBrush.Dispose();
                _hotTrackBrush = null;
            }
            if (_hotTrackPen != null)
            {
                _hotTrackPen.Dispose();
                _hotTrackPen = null;
            }

            _hotTrackBrush = new SolidBrush(UtilityGDI.AlphaBlend(SystemColors.HotTrack, this.BackColor, .05d));
            _hotTrackPen = new Pen(UtilityGDI.AlphaBlend(SystemColors.HotTrack, this.BackColor, .33d));
        }

        private void RepositionButtons()
        {
            if (this.Width <= 2 || this.Height <= 2)
            {
                return;
            }

            if (_buttons.Count == 0)
            {
                return;
            }

            const int OUTERMARGIN = 4;
            const int INNERMARGIN = 2;

            // Figure out the max button size, and how they should be tiled
            List<int> buttonsPerRow = GetButtonArrangement(out _buttonSize, INNERMARGIN, OUTERMARGIN);
            if (_buttonSize == -1)
            {
                MessageBox.Show("Buttons don't fit in the space provided", "Pie Panel Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _buttonSize = 1;
            }

            // Place and draw the buttons
            int buttonCntr = 0;
            for (int rowCntr = 0; rowCntr < buttonsPerRow.Count; rowCntr++)
            {
                // All buttons in this row will be at this y coord
                int y = this.Height - OUTERMARGIN - ((buttonsPerRow.Count - rowCntr) * _buttonSize) - ((buttonsPerRow.Count - rowCntr - 1) * INNERMARGIN);

                for (int colCntr = 0; colCntr < buttonsPerRow[rowCntr]; colCntr++)
                {
                    #region Place Button

                    // Figure out where this button will sit
                    _buttons[buttonCntr].Rectangle = new Rectangle(OUTERMARGIN + (colCntr * _buttonSize) + (colCntr * INNERMARGIN), y, _buttonSize, _buttonSize);

                    // Rebuild the graphics path
                    //TODO: Support slightly rounded corners
                    if (_buttons[buttonCntr].Outline != null)
                    {
                        _buttons[buttonCntr].Outline.Dispose();
                        _buttons[buttonCntr].Outline = null;
                    }

                    _buttons[buttonCntr].Outline = UtilityGDI.GetRoundedRectangle(_buttons[buttonCntr].Rectangle, _buttonSize / 3);

                    // Draw the button
                    DrawButton_Private(buttonCntr);

                    #endregion

                    // Point to the next button
                    buttonCntr++;
                }
            }

            // Any time the layout changes, I should probably redraw
            this.Invalidate();
        }

        /// <summary>
        /// This will ensure that the bitmap for the button exists and is the right size, then requests that the button
        /// is drawn for the appropriate size
        /// </summary>
        private void DrawButton_Private(int index)      // the event is named DrawButton, and I can't think of any better name for this method
        {
            // Kill the old bitmap if nessassary
            if (_buttons[index].Bitmap != null && _buttons[index].Bitmap.Width != _buttonSize)
            {
                _buttons[index].Bitmap.Dispose();
                _buttons[index].Bitmap = null;
            }

            // Build a new bitmap if nessassary
            if (_buttons[index].Bitmap == null)
            {
                _buttons[index].Bitmap = new Bitmap(_buttonSize, _buttonSize);
            }

            // Draw the button from scratch
            using (Graphics graphics = Graphics.FromImage(_buttons[index].Bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;		// I can't blit cleartype, it looks like crap
                graphics.Clear(Color.Transparent);

                // Let the parent class draw it
                OnDrawButton(index, _buttons[index].Name, _buttonSize, graphics);
            }
        }

        private List<int> GetButtonArrangement(out int buttonSize, int innerMargin, int outerMargin)
        {
            List<int> retVal = new List<int>();

            int radius = this.Width - (outerMargin * 2);

            // Since the menu always sits at the top of the pie, it will always be wider than taller.  So figure out how many
            // buttons will fit in a single row.
            retVal.Add(_buttons.Count);
            int y = this.Width - this.Height;     // this y is relative to the circle, not the control
            buttonSize = GetMaxButtonSize_SingleRow(_buttons.Count, y, -1, 0, radius, innerMargin);

            // Now try to put buttons in other rows, one button at a time
            int limitingIndex = 0;

            while (true)
            {
                List<int> testArrangement = GetButtonArrangementSprtTryPop(retVal, y, buttonSize, limitingIndex, radius, innerMargin);
                if (testArrangement == null)
                {
                    // No other arrangement is possible
                    break;
                }

                int testSize = GetMaxButtonSize_MultiRow(out limitingIndex, testArrangement, y, buttonSize, radius, innerMargin);
                if (testSize < buttonSize)     // if I say equal, then it will stop prematurely, but without it, it stacks them up slightly more than I like
                {
                    // This isn't an improvement over the current arrangement
                    break;
                }

                // Use this as the new arrangement
                retVal = testArrangement;
                buttonSize = testSize;
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This will pop a button out of the limiting row, and place it in an above row
        /// </summary>
        /// <remarks>
        /// This method will only return an arrangement that fits (with the same size buttons)
        /// </remarks>
        /// <returns>
        /// Null -> Can't modify the arrangement further
        /// NonNull -> An alternate arrangement with one button shifted to an above row (or new row)
        /// </returns>
        private static List<int> GetButtonArrangementSprtTryPop(List<int> curArrangement, int bottomY, int size, int limitingIndex, int radius, int innerMargin)
        {
            if (curArrangement[limitingIndex] == 1)
            {
                // The limiting row only has one item in it
                return null;
            }

            // See if one of the existing rows has room
            for (int cntr = limitingIndex - 1; cntr >= 0; cntr--)
            {
                if (DoesRowFit(curArrangement[cntr] + 1, bottomY, size, curArrangement.Count - 1 - cntr, radius, innerMargin))
                {
                    // Found one
                    List<int> retVal = CloneList(curArrangement);

                    retVal[limitingIndex]--;
                    retVal[cntr]++;

                    return retVal;
                }
            }

            // See if another row is possible
            if (!DoesRowFit(1, bottomY, size, curArrangement.Count, radius, innerMargin))
            {
                return null;
            }

            // A new row fits
            List<int> retVal1 = CloneList(curArrangement);
            retVal1[limitingIndex]--;
            retVal1.Insert(0, 1);

            // Exit Function
            return retVal1;
        }

        /// <summary>
        /// This finds the largest button size that will allow this arrangement to fit
        /// </summary>
        /// <param name="buttonLayout">
        /// Zero element is the top row, the last element is the row that sits on the y line.  The value of each element is the
        /// number of buttons in that row.
        /// </param>
        /// <param name="startSize">This cannot be -1</param>
        private static int GetMaxButtonSize_MultiRow(out int limitingIndex, List<int> buttonLayout, int bottomY, int startSize, int radius, int innerMargin)
        {
            if (startSize <= 0)    // GetMaxButtonSize_SingleRow allows -1, but this method doesn't
            {
                throw new ArgumentOutOfRangeException("startSize", startSize, "startSize must be greater than zero: " + startSize.ToString());
            }

            limitingIndex = -1;
            int retVal = startSize;
            bool exceededCircle = false;

            while (true)
            {
                // Test all the rows with this size
                for (int cntr = 0; cntr < buttonLayout.Count; cntr++)
                {
                    // The for loop is counting up, but the rows need to be processed last to first
                    int index = buttonLayout.Count - 1 - cntr;

                    if (!DoesRowFit(buttonLayout[index], bottomY, retVal, cntr, radius, innerMargin))
                    {
                        limitingIndex = index;
                        exceededCircle = true;
                        break;
                    }
                }

                // See if this was too big
                if (exceededCircle)
                {
                    if (retVal == startSize)
                    {
                        // I don't support shrinking, because the caller only cares if the buttons will grow, so I won't waste my time.
                        return 0;
                    }
                    else
                    {
                        return retVal - 1;
                    }
                }

                // Prep for one pixel bigger
                retVal++;
            }

            throw new ApplicationException("Execution should never get here");
        }
        private static int GetMaxButtonSize_MultiRow_ORIG(out int limitingIndex, List<int> buttonLayout, int bottomY, int startSize, int radius, int innerMargin)
        {
            if (startSize <= 0)    // GetMaxButtonSize_SingleRow allows -1, but this method doesn't
            {
                throw new ArgumentOutOfRangeException("startSize", startSize, "startSize must be greater than zero: " + startSize.ToString());
            }

            limitingIndex = -1;
            int retVal = int.MaxValue;
            int[] maxSizeOfEachRow = new int[buttonLayout.Count];





            // Find the max size of each row
            //TODO:  Loop the other direction so that if there is a tie, limitingIndex will be the topmost row
            for (int cntr = 0; cntr < buttonLayout.Count; cntr++)
            {
                // The for loop is counting up, but the rows need to be processed last to first
                int index = buttonLayout.Count - 1 - cntr;

                // Figure out what line the boxes will be sitting on
                int curY = bottomY + (startSize * cntr) + (innerMargin * cntr);

                // See how big this row can be
                maxSizeOfEachRow[index] = GetMaxButtonSize_SingleRow(buttonLayout[index], curY, startSize, cntr, radius, innerMargin);

                if (maxSizeOfEachRow[index] < retVal)
                {
                    limitingIndex = index;
                    retVal = maxSizeOfEachRow[index];
                }
            }






            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This function will find the max size that the squares can be and still fit inside the portion of the pi
        /// </summary>
        /// <remarks>
        /// I implement sort of a binary search.  I can't think of a way to get the answer immediately, so I get it
        /// by trial and error
        /// 
        /// This overload is nearly identical to the simpler overload, except this one is meant for rows other than the bottom one
        /// Once this overload is complete, the simpler one can be deleted (just send -1 for start size, and a growth multiplier of
        /// zero)
        /// </remarks>
        /// <param name="numSquares">The number of squares to make fit</param>
        /// <param name="y">The y position that marks the bottom of the row (0 being the center of the circle)</param>
        /// <param name="startSize">The size of button to start with (-1 if this is the bottom row, and you're not using the output of a previous call)</param>
        /// <param name="yGrowthMultiplier">
        /// Every time I try to increase the return val by x, I need to add yGrowthMultiplier*x (because there are rows under this
        /// one that will grow as well)
        /// </param>
        /// <param name="radius">The radius of the circle</param>
        /// <param name="innerMargin">The gap between each button</param>
        /// <returns>The height of the squares (or -1 if they won't fit at all)</returns>
        private static int GetMaxButtonSize_SingleRow(int numSquares, int y, int startSize, int yGrowthMultiplier, int radius, int innerMargin)
        {
            #region Validate Params

            if (numSquares <= 0)
            {
                throw new ArgumentOutOfRangeException("numSquares", numSquares.ToString(), "numSquares must be positive: " + numSquares.ToString());
            }

            if (y < 0)
            {
                throw new ArgumentOutOfRangeException("y", y.ToString(), "y must be zero or greater: " + y.ToString());
            }

            if (startSize != -1 && startSize < 1)
            {
                throw new ArgumentOutOfRangeException("startSize", startSize.ToString(), "startSize must either be -1 or be greater than zero: " + startSize.ToString());
            }

            if (yGrowthMultiplier < 0)
            {
                throw new ArgumentOutOfRangeException("yGrowthMultiplier", yGrowthMultiplier.ToString(), "yGrowthMultiplier must be positive or zero: " + yGrowthMultiplier.ToString());
            }

            if (radius <= 0)
            {
                throw new ArgumentOutOfRangeException("radius", radius.ToString(), "radius must be positive: " + radius.ToString());
            }

            if (innerMargin < 0)
            {
                throw new ArgumentOutOfRangeException("innerMargin", innerMargin.ToString(), "innerMargin must be positive or zero: " + innerMargin.ToString());
            }

            #endregion

            if (numSquares + ((numSquares - 1) * innerMargin) >= radius || GetCircleHeightAbovePoint(numSquares + ((numSquares - 1) * innerMargin), y + 1 + yGrowthMultiplier + (innerMargin * yGrowthMultiplier), radius) < 0)
            {
                // They wouldn't even fit if they were 1 pixel
                return -1;
            }

            int lastLow = 0;
            int lastHigh = -1;     // While this is negative, I grow unbounded.  Then once this is set, I'm in binary search mode.
            int retVal = startSize;
            if (startSize == -1)
            {
                retVal = radius / (numSquares >= 10 ? numSquares + 1 : 10);
            }

            int difference = 0;

            bool goSmaller = true;

            while (true)
            {
                if ((numSquares * retVal) + ((numSquares - 1) * innerMargin) >= radius)     // quick test to see if x is greater than radius
                {
                    goSmaller = true;
                }
                else
                {
                    #region Try This Size

                    // X=(NumSquares*ProposedSize) + (NumMargins*MarginWidth)
                    int testX = (numSquares * retVal) + ((numSquares - 1) * innerMargin);
                    // Y=BottomLine + ProposedSize + (Height of boxes underneath) + (MarginWidth*NumMargins)
                    int testY = y + retVal + (retVal * yGrowthMultiplier) + (innerMargin * yGrowthMultiplier);
                    // Test x and y against the circle equation
                    difference = GetCircleHeightAbovePoint(testX, testY, radius);

                    if (difference == 0)
                    {
                        // Found an exact match (sits exactly on the circle edge)
                        return retVal;
                    }
                    else if (difference < 0)
                    {
                        // Went outside the circle
                        goSmaller = true;
                    }
                    else
                    {
                        // Can go bigger
                        goSmaller = false;
                    }

                    #endregion
                }

                if (goSmaller)
                {
                    #region Prep for smaller size

                    lastHigh = retVal;

                    if ((retVal - lastLow) == 1)
                    {
                        // lastLow was the largest size
                        return lastLow;
                    }
                    else
                    {
                        // Take the average
                        retVal = (lastLow + retVal) / 2;
                    }

                    #endregion
                }
                else
                {
                    #region Prep for larger size

                    lastLow = retVal;

                    if (lastHigh == -1)
                    {
                        if (startSize == -1)
                        {
                            retVal *= 2;
                        }
                        else
                        {
                            // If they passed in a value, then it's probably pretty close to the max already
                            retVal++;
                        }
                    }
                    else if ((lastHigh - retVal) == 1)
                    {
                        // I'm as big as I can get
                        return retVal;
                    }
                    else
                    {
                        // Take the average
                        retVal = (retVal + lastHigh) / 2;
                    }

                    #endregion
                }
            }

            throw new ApplicationException("Execution should never get here");
        }

        /// <summary>
        /// This returns whether the row will fit using the desired size
        /// </summary>
        private static bool DoesRowFit(int numSquares, int bottomY, int size, int yGrowthMultiplier, int radius, int innerMargin)
        {
            // X=(NumSquares*ProposedSize) + (NumMargins*MarginWidth)
            int testX = (numSquares * size) + ((numSquares - 1) * innerMargin);

            // Y=BottomLine + Size + (Height of boxes underneath) + (NumMargins*MarginWidth)
            int testY = bottomY + size + (size * yGrowthMultiplier) + (innerMargin * yGrowthMultiplier);

            // Exit Function
            try
            {
                return GetCircleHeightAbovePoint(testX, testY, radius) >= 0;      // a negative value means the row is outside the circle
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// This function returns the y position of the circle above the point passed in
        /// </summary>
        /// <param name="x">The location to get the y coord (must be positive)</param>
        /// <param name="y">y isn't a nessasary input to this function, but the return subtracts this y out</param>
        /// <param name="radius">The radius of the circle</param>
        /// <returns>The height of the circle at the x passed in minus the y passed in</returns>
        private static int GetCircleHeightAbovePoint(int x, int y, int radius)
        {
            if (x < 0)
            {
                throw new ArgumentOutOfRangeException("X can't be negative: " + x.ToString());
            }
            else if (x >= radius)
            {
                throw new ArgumentOutOfRangeException("X can't be greater than radius: " + x.ToString());
            }

            // Equation of a circle (this function only deals with quadrant 1):
            // x^2 + y^2 = r^2
            double retVal = Math.Sqrt((radius * radius) - (x * x));

            // Exit Function
            return Convert.ToInt32(retVal) - y;
        }

        private static List<int> CloneList(List<int> list)
        {
            List<int> retVal = new List<int>();

            for (int cntr = 0; cntr < list.Count; cntr++)
            {
                retVal.Add(list[cntr]);
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #region PieMenuButtonClicked Delegate/Args

    public delegate void PieMenuButtonClickedHandler(object sender, PieMenuButtonClickedArgs e);

    public class PieMenuButtonClickedArgs : EventArgs
    {
        private int _index = -1;
        private string _name = "";

        public PieMenuButtonClickedArgs(int index, string name)
        {
            _index = index;
            _name = name;
        }

        public int Index
        {
            get
            {
                return _index;
            }
        }
        public string Name
        {
            get
            {
                return _name;
            }
        }
    }

    #endregion
    #region PieMenuDrawButton Delegate/Args

    public delegate void PieMenuDrawButtonHandler(object sender, PieMenuDrawButtonArgs e);

    public class PieMenuDrawButtonArgs : EventArgs
    {
        #region Declaration Section

        private int _index = -1;
        private string _name = "";
        private int _buttonSize = 0;
        private Graphics _graphics = null;

        #endregion

        #region Constructor

        public PieMenuDrawButtonArgs(int index, string name, int buttonSize, Graphics graphics)
        {
            _index = index;
            _name = name;
            _buttonSize = buttonSize;
            _graphics = graphics;
        }

        #endregion

        #region Public Properties

        public int Index
        {
            get
            {
                return _index;
            }
        }
        public string Name
        {
            get
            {
                return _name;
            }
        }

        public int ButtonSize
        {
            get
            {
                return _buttonSize;
            }
        }

        public Graphics Graphics
        {
            get
            {
                return _graphics;
            }
        }

        #endregion
    }

    #endregion
}
