using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

using Game.HelperClassesCore;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    /// <summary>
    /// Since there are so many classes that need to draw similiar things, this class will do the actual
    /// drawing in order to keep a consistent look.
    /// </summary>
    /// <remarks>
    /// This class won't do any drawing on its own (due to events).  It will only draw what it's told
    /// to.  It is some other class's job to iterate through all the objects once per frame.
    /// </remarks>
    public class ObjectRenderer
    {
        #region Enum: DrawMode

        public enum DrawMode
        {
            Standard = 0,
            Selected,
            Building
        }

        #endregion

        #region Declaration Section

        // Fill Props
        private Color REDCOLOR = Color.Tomato;		// they won't let me make it a const, but I'll treat it like one
        private const int STANDARDBALLFILLOPACTIY = 160;
        private const int BUILDINGBALLFILLOPACTIY = 50;

        private const float STANDARDOUTLINEWIDTH = 1f;

        private LargeMapViewer2D _viewer = null;

        private bool _drawVelocities = false;

        // Pen Props
        private Color _buildingPenColor = Color.FromArgb(128, Color.Black);
        private Color _standardPenColor = Color.Black;

        #endregion

        #region Constructor

        public ObjectRenderer(LargeMapViewer2D viewer)
        {
            _viewer = viewer;
        }

        #endregion

        #region Public Properties

        public bool DrawVelocities
        {
            get
            {
                return _drawVelocities;
            }
            set
            {
                _drawVelocities = value;
            }
        }

        public LargeMapViewer2D Viewer
        {
            get
            {
                return _viewer;
            }
        }

        #endregion

        #region Public Methods

        public void DrawBall(Ball ball, DrawMode mode, CollisionStyle collisionStyle, bool drawRed)
        {
            DrawBall(ball, mode, collisionStyle, drawRed, 1d);
        }
        /// <summary>
        /// The opacity passed in will be applied after figuring out the opacity of mode (if mode says 128, and opacity is .5, then
        /// the final opacity is 64)
        /// </summary>
        public void DrawBall(Ball ball, DrawMode mode, CollisionStyle collisionStyle, bool drawRed, double opacity)
        {
            Color color;
            #region Figure out the color

            if (drawRed)
            {
                color = REDCOLOR;
            }
            else
            {
                color = Color.Gold;
            }

            #endregion

            int finalOpacity;
            #region Figure out the opacity

            switch (mode)
            {
                case DrawMode.Building:
                    finalOpacity = BUILDINGBALLFILLOPACTIY;
                    break;

                case DrawMode.Selected:
                case DrawMode.Standard:
                    finalOpacity = STANDARDBALLFILLOPACTIY;
                    break;

                default:
                    throw new ApplicationException("Unknown DrawMode: " + mode.ToString());
            }

            finalOpacity = Convert.ToInt32(finalOpacity * opacity);
            if (finalOpacity < 0)
            {
                finalOpacity = 0;
            }
            else if (finalOpacity > 255)
            {
                finalOpacity = 255;
            }

            #endregion

            // Collision Style
            DrawCollisionStyle(ball.Position, ball.Radius, collisionStyle);

            // Fill the circle
            using (Brush brush = new SolidBrush(Color.FromArgb(finalOpacity, color)))
            {
                _viewer.FillCircle(brush, ball.Position, ball.Radius);
            }

            #region Draw the edge

            switch (mode)
            {
                case DrawMode.Building:
                    _viewer.DrawCircle(Color.FromArgb(finalOpacity, _buildingPenColor), STANDARDOUTLINEWIDTH, ball.Position, ball.Radius);
                    break;

                case DrawMode.Standard:
                    _viewer.DrawCircle(Color.FromArgb(finalOpacity, _standardPenColor), STANDARDOUTLINEWIDTH, ball.Position, ball.Radius);
                    break;

                case DrawMode.Selected:
                    _viewer.DrawCircle_Selected(ball.Position, ball.Radius);
                    break;
            }

            #endregion

            //TODO:  Show Stats
        }

        public void DrawSolidBall(SolidBall ball, DrawMode mode, CollisionStyle collisionStyle, bool drawRed)
        {
            DrawSolidBall(ball, mode, collisionStyle, drawRed, 1d);
        }
        /// <summary>
        /// The opacity passed in will be applied after figuring out the opacity of mode (if mode says 128, and opacity is .5, then
        /// the final opacity is 64)
        /// </summary>
        public void DrawSolidBall(SolidBall ball, DrawMode mode, CollisionStyle collisionStyle, bool drawRed, double opacity)
        {
            Color color;
            #region Figure out the color

            if (drawRed)
            {
                color = REDCOLOR;
            }
            else
            {
                color = Color.RoyalBlue;
            }

            #endregion

            int finalOpacity;
            #region Figure out the opacity

            switch (mode)
            {
                case DrawMode.Building:
                    finalOpacity = BUILDINGBALLFILLOPACTIY;
                    break;

                case DrawMode.Selected:
                case DrawMode.Standard:
                    finalOpacity = STANDARDBALLFILLOPACTIY;
                    break;

                default:
                    throw new ApplicationException("Unknown DrawMode: " + mode.ToString());
            }

            finalOpacity = Convert.ToInt32(finalOpacity * opacity);
            if (finalOpacity < 0)
            {
                finalOpacity = 0;
            }
            else if (finalOpacity > 255)
            {
                finalOpacity = 255;
            }

            #endregion

            MyVector dirFacing;
            #region Figure out direction facing

            dirFacing = ball.DirectionFacing.Standard.Clone();
            dirFacing.BecomeUnitVector();
            dirFacing.Multiply(ball.Radius);
            dirFacing.Add(ball.Position);

            #endregion

            // Collision Style
            DrawCollisionStyle(ball.Position, ball.Radius, collisionStyle);

            // Fill the circle
            using (Brush brush = new SolidBrush(Color.FromArgb(finalOpacity, color)))
            {
                _viewer.FillCircle(brush, ball.Position, ball.Radius);
            }

            // Draw direction facing
            _viewer.DrawLine(Color.FromArgb(finalOpacity, Color.White), 2d, ball.Position, dirFacing);

            #region Draw the edge

            switch (mode)
            {
                case DrawMode.Building:
                    _viewer.DrawCircle(Color.FromArgb(finalOpacity, _buildingPenColor), STANDARDOUTLINEWIDTH, ball.Position, ball.Radius);
                    break;

                case DrawMode.Standard:
                    _viewer.DrawCircle(Color.FromArgb(finalOpacity, _standardPenColor), STANDARDOUTLINEWIDTH, ball.Position, ball.Radius);
                    break;

                case DrawMode.Selected:
                    _viewer.DrawCircle_Selected(ball.Position, ball.Radius);
                    break;
            }

            #endregion

            //TODO:  Show Stats
        }

        public void DrawRigidBody(RigidBody body, DrawMode mode, CollisionStyle collisionStyle, bool drawRed)
        {
        }

        public void DrawProjectile(Projectile projectile, DrawMode drawMode, bool drawRed)
        {
            Color fillColor, penColor;
            int opacity = STANDARDBALLFILLOPACTIY;
            #region Figure out the color

            if (drawRed)
            {
                fillColor = REDCOLOR;
                penColor = _standardPenColor;
            }
            else if (projectile.State == ProjectileState.Flying)
            {
                fillColor = Color.Black;
                penColor = Color.White;
            }
            else if (projectile.State == ProjectileState.Exploding)
            {
                fillColor = Color.FromArgb(250, Color.DarkOrange);
                penColor = Color.Yellow;
                opacity = 64;
            }
            else if (projectile.State == ProjectileState.Dying)
            {
                return;
            }
            else
            {
                throw new ApplicationException("Unknown ProjectileState: " + projectile.State.ToString());
            }

            #endregion

            // Fill the circle
            using (Brush brush = new SolidBrush(Color.FromArgb(opacity, fillColor)))
            {
                _viewer.FillCircle(brush, projectile.Ball.Position, projectile.Ball.Radius);
            }

            #region Draw the edge

            switch (drawMode)
            {
                case DrawMode.Standard:
                    _viewer.DrawCircle(penColor, STANDARDOUTLINEWIDTH, projectile.Ball.Position, projectile.Ball.Radius);
                    break;

                case DrawMode.Selected:
                    _viewer.DrawCircle_Selected(projectile.Ball.Position, projectile.Ball.Radius);
                    break;
            }

            #endregion

            //TODO:  Show Stats
        }

        public void DrawVectorField(VectorField2D field, DrawMode mode)
        {
        }

        #endregion

        #region Private Methods

        private void DrawCollisionStyle(MyVector position, double bodyRadius, CollisionStyle collisionStyle)
        {

            const double MINSIZE = 75d;
            const double MAXSIZE = 250d;

            if (collisionStyle == CollisionStyle.Standard)
            {
                return;
            }

            #region Figure out how big to make it

            double radius = bodyRadius * .5d;

            if (radius < MINSIZE)
            {
                if (bodyRadius > MINSIZE)
                {
                    radius = MINSIZE;
                }
                else
                {
                    radius = bodyRadius;		// I don't want to use MINSIZE, because this could look funny
                }
            }
            else if (radius > MAXSIZE)
            {
                radius = MAXSIZE;
            }

            double halfRadius = radius * .5d;

            #endregion

            // Figure out the line thickness
            double lineThickness = UtilityCore.GetScaledValue_Capped(4d, 15d, MINSIZE, MAXSIZE, radius);

            // Draw Icon
            switch (collisionStyle)
            {
                case CollisionStyle.Ghost:
                    #region Ghost

                    // Make a tilde (I was going to make two, but one seems to get the point across)
                    MyVector[] points = new MyVector[4];

                    points[0] = new MyVector(position.X - halfRadius, position.Y + (halfRadius / 8d), position.Z);
                    points[1] = new MyVector(position.X - (halfRadius / 3d), position.Y - (halfRadius / 4d), position.Z);
                    points[2] = new MyVector(position.X + (halfRadius / 3d), position.Y + (halfRadius / 4d), position.Z);
                    points[3] = new MyVector(position.X + halfRadius, position.Y - (halfRadius / 8d), position.Z);

                    _viewer.DrawArc(Color.Navy, lineThickness, points, false);

                    #endregion
                    break;

                case CollisionStyle.Stationary:
                    #region Stationary

                    _viewer.DrawLine(Color.Maroon, lineThickness, new MyVector(position.X - halfRadius, position.Y - halfRadius, position.Z), new MyVector(position.X + halfRadius, position.Y + halfRadius, position.Z));
                    _viewer.DrawLine(Color.Maroon, lineThickness, new MyVector(position.X - halfRadius, position.Y + halfRadius, position.Z), new MyVector(position.X + halfRadius, position.Y - halfRadius, position.Z));

                    #endregion
                    break;

                case CollisionStyle.StationaryRotatable:
                    #region StationaryRotatable

                    _viewer.DrawCircle(Color.Maroon, lineThickness, position, halfRadius);

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown CollisionStyle: " + collisionStyle.ToString());
            }

        }

        #endregion
    }
}
