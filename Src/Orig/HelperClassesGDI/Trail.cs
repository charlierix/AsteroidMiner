using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Game.Orig.Math3D;

namespace Game.Orig.HelperClassesGDI
{
    /// <summary>
    /// This class is meant to show a trail or streak coming from an object.
    /// </summary>
    public class Trail
    {
        #region Declaration Section

        /// <summary>
        /// The very first time a point is passed in, I need to set the entire array equal to that point.  Also, I don't need to draw anything if
        /// this is still false.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// These are the points that I will keep track of
        /// </summary>
        private MyVector[] _points = null;

        /// <summary>
        /// This is the pointer of the most recent entry into _points.  The point immediatly following this one is the
        /// last one entered.
        /// </summary>
        private int _curPtr = -1;

        // This is the color at the start of the trail (the most recent entry)
        private int _startRed = 255;
        private int _startGreen = 255;
        private int _startBlue = 255;

        // This is the color at the end of the trail (the oldest entry)
        private int _endRed = 0;
        private int _endGreen = 0;
        private int _endBlue = 0;

        float _startWidth = 1f;
        float _endWidth = 1f;

        #endregion

        #region Constructor

        /// <remarks>
        /// This is the standard constructor
        /// </remarks>
        public Trail(int size)
        {
            _points = new MyVector[size];
        }

        /// <summary>
        /// This constructor is meant to assist the clone function
        /// </summary>
        protected Trail(MyVector[] points, int curPtr, int startRed, int startGreen, int startBlue, int endRed, int endGreen, int endBlue)
        {

            // Store stuff passed in
            _points = points;
            _curPtr = curPtr;
            _startRed = startRed;
            _startGreen = startGreen;
            _startBlue = startBlue;
            _endRed = endRed;
            _endGreen = endGreen;
            _endBlue = endBlue;

            // Set up the other misc stuff
            _isInitialized = true;

        }

        #endregion

        #region Public Methods

        public Trail Clone()
        {
            return new Trail((MyVector[])_points.Clone(), _curPtr, _startRed, _startGreen, _startBlue, _endRed, _endGreen, _endBlue);
        }

        public void SetColors(Color start, Color end)
        {
            _startRed = start.R;
            _startGreen = start.G;
            _startBlue = start.B;

            _endRed = end.R;
            _endGreen = end.G;
            _endBlue = end.B;
        }

        public void SetWidths(float startWidth, float endWidth)
        {
            _startWidth = startWidth;
            _endWidth = endWidth;
        }
        public void SetWidths(double startWidth, double endWidth)
        {
            // I take in double, because most of the rest of my code is with doubles, and I don't want the caller to
            // have to convert down all over the place
            _startWidth = Convert.ToSingle(startWidth);
            _endWidth = Convert.ToSingle(endWidth);
        }

        /// <summary>
        /// This function pushes the point passed in, and pops the oldest point off
        /// NOTE:  You most likely want to clone the vector passed in (otherwise, I'll have 100 references to the
        /// same vector)
        /// </summary>
        public void AddPoint(MyVector position)
        {

            // See if I need to initialize the array
            if (!_isInitialized)
            {
                #region Initialize Array

                _isInitialized = true;

                for (int pointCntr = 0; pointCntr < _points.Length; pointCntr++)
                {
                    _points[pointCntr] = position;
                }

                #endregion
            }

            // Figure out where to store it
            _curPtr++;
            if (_curPtr >= _points.Length)
            {
                _curPtr = 0;
            }

            // Store the point passed in
            _points[_curPtr] = position;

        }

        public void Draw(Graphics graphics)
        {

            if (!_isInitialized)
            {
                return;
            }

            double intensity = 1;
            double decrementIntensity = 1d / _points.Length;
            int fromPoint, toPoint;

            // Go from the current index down to zero
            for (int cntr = _curPtr; cntr >= 0; cntr--)
            {
                // Figure out the from to points
                fromPoint = cntr;
                toPoint = cntr - 1;
                if (toPoint < 0)
                {
                    toPoint = _points.Length - 1;
                }

                // Draw this segment of the trail
                DrawLine(graphics, fromPoint, toPoint, intensity);

                // Decrement the intensity
                intensity -= decrementIntensity;
            }

            // Go from the max down to the current point
            for (int cntr = _points.Length - 1; cntr > _curPtr + 1; cntr--)
            {
                // Draw this segment of the trail
                DrawLine(graphics, cntr, cntr - 1, intensity);

                // Decrement the intensity
                intensity -= decrementIntensity;
            }

        }

        #endregion

        #region Private Methods

        private void DrawLine(Graphics graphics, int fromPoint, int toPoint, double intensity)
        {

            Color color = Color.FromArgb(Convert.ToInt32((_startRed * intensity) + (_endRed * (1d - intensity))),
                                                                Convert.ToInt32((_startGreen * intensity) + (_endGreen * (1d - intensity))),
                                                                Convert.ToInt32((_startBlue * intensity) + (_endBlue * (1d - intensity))));

            float width = Convert.ToSingle((_startWidth * intensity) + (_endWidth * (1d - intensity)));

            Pen pen = new Pen(color, width);
            graphics.DrawLine(pen, _points[fromPoint].ToPointF(), _points[toPoint].ToPointF());
            pen.Dispose();

        }

        #endregion
    }
}
