using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.ChaseForces
{
    public class ChasedBall
    {
        #region Events

        public event EventHandler BoundrySizeChanged = null;

        #endregion

        #region Declaration Section

        // How long the current state has been running (only used for Jump and Brownian)
        private double _elapsedPosition = 0;
        private double _elapsedDirection = 0;

        private Vector3D _velocityUnit = Math3D.GetRandomVector_Spherical_Shell(1);

        #endregion

        #region Public Properties

        public MotionType_Position MotionType_Position
        {
            get;
            set;
        }
        public MotionType_Orientation MotionType_Orientation
        {
            get;
            set;
        }

        public Point3D Position
        {
            get;
            set;
        }

        private Vector3D _direction = new Vector3D(0, 0, 1);
        /// <summary>
        /// I was going to call this Orientation, but that felt more wrong
        /// </summary>
        public Vector3D Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;
            }
        }

        /// <summary>
        /// The ball will always be this speed (unless stopped or jumping)
        /// </summary>
        public double Speed_Position
        {
            get;
            set;
        }
        public double Speed_Orientation
        {
            get;
            set;
        }

        /// <summary>
        /// How long to keep the same setting (only used for Jump and Brownian)
        /// </summary>
        public double Delay_Position
        {
            get;
            set;
        }
        public double Delay_Orientation
        {
            get;
            set;
        }

        private Vector3D _boundry = new Vector3D(0, 0, 0);
        /// <summary>
        /// The ball will stay between +- Boundry
        /// </summary>
        public Vector3D Boundry
        {
            get
            {
                return _boundry;
            }
            set
            {
                _boundry = value;

                if (this.BoundrySizeChanged != null)
                {
                    this.BoundrySizeChanged(this, new EventArgs());
                }
            }
        }

        #endregion

        #region Public Methods

        public void Update(double elapsedTime)
        {
            _elapsedPosition += elapsedTime;
            _elapsedDirection += elapsedTime;

            #region Position

            switch (this.MotionType_Position)
            {
                case Testers.ChaseForces.MotionType_Position.Stop:
                    break;

                case Testers.ChaseForces.MotionType_Position.Jump:
                    Update_Position_Jump();
                    break;

                case Testers.ChaseForces.MotionType_Position.Brownian:
                    Update_Position_Brownian(elapsedTime);
                    break;

                case Testers.ChaseForces.MotionType_Position.BounceOffWalls:
                    Update_Position_BounceOffWalls(elapsedTime);
                    break;

                case Testers.ChaseForces.MotionType_Position.Orbit:
                    Update_Position_Orbit(elapsedTime);
                    break;

                default:
                    throw new ApplicationException("Unknown MotionType_Position: " + this.MotionType_Position);
            }

            #endregion
            #region Orientation

            switch (this.MotionType_Orientation)
            {
                case Testers.ChaseForces.MotionType_Orientation.Stop:
                    break;

                case Testers.ChaseForces.MotionType_Orientation.Jump:
                    Update_Direction_Jump();
                    break;

                default:
                    throw new ApplicationException("Unknown MotionType_Orientation: " + this.MotionType_Orientation);
            }

            #endregion
        }

        #endregion

        #region Private Methods

        private void Update_Position_Jump()
        {
            if (_elapsedPosition < this.Delay_Position)
            {
                return;
            }

            this.Position = Math3D.GetRandomVector(-this.Boundry, this.Boundry).ToPoint();

            _elapsedPosition = 0;
        }
        private void Update_Position_Brownian(double elapsedTime)
        {
            if (_elapsedPosition > this.Delay_Position)
            {
                _velocityUnit = Math3D.GetRandomVector_Spherical_Shell(1);
                _elapsedPosition = 0;
            }

            ReflectOffWall_Velocity(elapsedTime);

            this.Position = GetProjectedPosition(elapsedTime);

            ReflectOffWall_Position();
        }
        private void Update_Position_BounceOffWalls(double elapsedTime)
        {
            ReflectOffWall_Velocity(elapsedTime);

            this.Position = GetProjectedPosition(elapsedTime);

            ReflectOffWall_Position();
        }
        private void Update_Position_Orbit(double elapsedTime)
        {
            //NOTE: This method doesn't try to limit motion inside the boundry rectangle, it just used the boundry to calculate orbit radius
            double orbitRadius = this.Boundry.Length * .8;

            Vector3D posVect = this.Position.ToVector();

            // Fix position
            if (Math3D.IsNearZero(posVect))
            {
                posVect = Math3D.GetRandomVector_Spherical_Shell(orbitRadius);
            }
            else if (!Math3D.IsNearValue(this.Position.ToVector().LengthSquared, orbitRadius * orbitRadius))
            {
                posVect = posVect.ToUnit() * orbitRadius;
            }

            // Figure out how many degrees to turn
            double circ = Math.PI * orbitRadius * 2;
            double angle = this.Speed_Position / circ * 360 * elapsedTime;

            posVect = posVect.GetRotatedVector(_velocityUnit, angle);       // using the velocity vector as the axis of rotation

            this.Position = posVect.ToPoint();
        }

        private void Update_Direction_Jump()
        {
            if (_elapsedDirection < this.Delay_Orientation)
            {
                return;
            }

            this.Direction = Math3D.GetRandomVector_Spherical_Shell(1);

            _elapsedDirection = 0;
        }

        private void ReflectOffWall_Velocity(double elapsedTime)
        {
            // Project to what the position will be at
            Point3D position = GetProjectedPosition(elapsedTime);

            // X
            if (this.Position.X < this.Boundry.X && position.X >= this.Boundry.X)
            {
                _velocityUnit.X = -_velocityUnit.X;
            }
            else if (this.Position.X > -this.Boundry.X && position.X <= -this.Boundry.X)
            {
                _velocityUnit.X = -_velocityUnit.X;
            }

            // Y
            if (this.Position.Y < this.Boundry.Y && position.Y >= this.Boundry.Y)
            {
                _velocityUnit.Y = -_velocityUnit.Y;
            }
            else if (this.Position.Y > -this.Boundry.Y && position.Y <= -this.Boundry.Y)
            {
                _velocityUnit.Y = -_velocityUnit.Y;
            }

            // Z
            if (this.Position.Z < this.Boundry.Z && position.Z >= this.Boundry.Z)
            {
                _velocityUnit.Z = -_velocityUnit.Z;
            }
            else if (this.Position.Z > -this.Boundry.Z && position.Z <= -this.Boundry.Z)
            {
                _velocityUnit.Z = -_velocityUnit.Z;
            }
        }
        private void ReflectOffWall_Position()
        {
            // X
            if (this.Position.X > this.Boundry.X)
            {
                this.Position = new Point3D(this.Boundry.X, this.Position.Y, this.Position.Z);
            }
            else if (this.Position.X < -this.Boundry.X)
            {
                this.Position = new Point3D(-this.Boundry.X, this.Position.Y, this.Position.Z);
            }

            // Y
            if (this.Position.Y > this.Boundry.Y)
            {
                this.Position = new Point3D(this.Position.X, this.Boundry.Y, this.Position.Z);
            }
            else if (this.Position.Y < -this.Boundry.Y)
            {
                this.Position = new Point3D(this.Position.X, -this.Boundry.Y, this.Position.Z);
            }

            // Z
            if (this.Position.Z > this.Boundry.Z)
            {
                this.Position = new Point3D(this.Position.X, this.Position.Y, this.Boundry.Z);
            }
            else if (this.Position.Z < -this.Boundry.Z)
            {
                this.Position = new Point3D(this.Position.X, this.Position.Y, -this.Boundry.Z);
            }
        }

        private Point3D GetProjectedPosition(double elapsedTime)
        {
            return this.Position + (_velocityUnit * (this.Speed_Position * elapsedTime));
        }

        #endregion
    }

    #region Enum: MotionType_Position

    public enum MotionType_Position
    {
        Stop,
        /// <summary>
        /// Jumps to a position, and sits until delay makes it jump again
        /// </summary>
        Jump,
        /// <summary>
        /// Goes a constant direction and speed (unless it bounces off a wall) until delay makes it choose a new direction
        /// </summary>
        Brownian,
        Orbit,
        /// <summary>
        /// Goes a constant direction and speed, bouncing off of walls
        /// </summary>
        BounceOffWalls,
    }

    #endregion
    #region Enum: MotionType_Orientation

    public enum MotionType_Orientation
    {
        Stop,
        Jump,
        Brownian,
        Constant,
    }

    #endregion
}
