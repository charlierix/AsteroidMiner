using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.HelperClassesOrig
{
    public class GravityController
    {
        #region Declaration Section

        private GravityMode _mode = GravityMode.None;

        private double _gravityMultiplier = 1d;

        private SimpleMap _map = null;

        #endregion

        #region Constructor

        public GravityController(SimpleMap map)
        {
            _map = map;
        }

        #endregion

        #region Public Properties

        public GravityMode Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
            }
        }

        public double GravityMultiplier
        {
            get
            {
                return _gravityMultiplier;
            }
            set
            {
                _gravityMultiplier = value;
            }
        }

        #endregion

        #region Public Methods

        public void Timer()
        {
            switch (_mode)
            {
                case GravityMode.None:
                    break;

                case GravityMode.Down:
                    DoGravityDown();
                    break;

                case GravityMode.EachOther:
                    DoGravityEachOther();
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void DoGravityDown()
        {
            const double ACCELDOWN = .1d;

            MyVector accel = new MyVector(0, ACCELDOWN * _gravityMultiplier, 0);

            foreach(BallBlip blip in _map.GetAllBlips())
            {
                if (blip.CollisionStyle != CollisionStyle.Standard)
                {
                    continue;
                }

                blip.Ball.Acceleration.Add(accel);       // I do acceleration instead of force so they all fall at the same rate
            }
        }
        private void DoGravityEachOther()
        {
			const double GRAVITATIONALCONSTANT = .0001d; //500d;
            double gravitationalConstantAdjusted = GRAVITATIONALCONSTANT * _gravityMultiplier;

            RadarBlip[] blips = _map.GetAllBlips();

            for (int outerCntr = 0; outerCntr < blips.Length - 1; outerCntr++)
            {
                if (blips[outerCntr].CollisionStyle == CollisionStyle.Ghost)
                {
                    continue;
                }

                Ball ball1 = blips[outerCntr].Sphere as Ball;

                for (int innerCntr = outerCntr + 1; innerCntr < blips.Length; innerCntr++)
                {
                    if (blips[innerCntr].CollisionStyle == CollisionStyle.Ghost)
                    {
                        continue;
                    }

                    #region Apply Gravity

                    Ball ball2 = blips[innerCntr].Sphere as Ball;

                    MyVector centerMass1, centerMass2;
                    if (ball1 is TorqueBall)
                    {
                        centerMass1 = ball1.Position + ((TorqueBall)ball1).CenterOfMass;
                    }
                    else
                    {
                        centerMass1 = ball1.Position;
                    }

                    if (ball2 is TorqueBall)
                    {
                        centerMass2 = ball2.Position + ((TorqueBall)ball2).CenterOfMass;
                    }
                    else
                    {
                        centerMass2 = ball2.Position;
                    }

                    MyVector gravityLink = centerMass1 - centerMass2;

                    double force = gravitationalConstantAdjusted * (ball1.Mass * ball2.Mass) / gravityLink.GetMagnitudeSquared();

                    gravityLink.BecomeUnitVector();
                    gravityLink.Multiply(force);

                    if (blips[innerCntr].CollisionStyle == CollisionStyle.Standard)
                    {
                        ball2.ExternalForce.Add(gravityLink);
                    }

                    if (blips[outerCntr].CollisionStyle == CollisionStyle.Standard)
                    {
                        gravityLink.Multiply(-1d);
                        ball1.ExternalForce.Add(gravityLink);
                    }

                    #endregion
                }
            }
        }

        #endregion
    }

    #region enum: GravityMode

    public enum GravityMode
    {
        None = 0,
        Down,
        EachOther
    }

    #endregion
}
