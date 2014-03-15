using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Math3D;

namespace Game.Orig.Map
{
    /// <summary>
    /// This is meant to represent simple rigid bodies derived from Ball (either the Ball class or the TorqueBall class)
    /// </summary>
    public class BallBlip : RadarBlip
    {
        #region Declaration Section

        /// <summary>
        /// This is base.Sphere cast as a ball (saves me from a lot of casting logic all over the place)
        /// </summary>
        private Ball _ball;
        private TorqueBall _torqueBall = null;

        #endregion

        #region Constructor

        /// <summary>
        /// This is the basic constructor.  These are the only things that need to be passed in.
        /// </summary>
        public BallBlip(Ball ball, CollisionStyle collisionStyle, RadarBlipQual blipQual, long token)
            : this(ball, collisionStyle, blipQual, token, Guid.NewGuid()) { }

        /// <summary>
        /// The only reason you would pass in your own guid is when loading a previously saved scene (the token
        /// works good for processing in ram, but when stuff needs to go to file, use the guid)
        /// </summary>
        public BallBlip(Ball ball, CollisionStyle collisionStyle, RadarBlipQual blipQual, long token, Guid objectID)
            : base(ball, collisionStyle, blipQual, token, objectID)
        {
            _ball = ball;
            _torqueBall = ball as TorqueBall;		// if it's not, null will be stored
        }

        #endregion

        #region Properties

        /// <summary>
        /// This is the same as base.Sphere, just cast as a ball for convenience
        /// </summary>
        public Ball Ball
        {
            get
            {
                return _ball;
            }
        }

        /// <summary>
        /// This is the same as base.Sphere, just cast as a torqueball for convenience.
        /// This will be null if I only represent a ball.
        /// </summary>
        public TorqueBall TorqueBall
        {
            get
            {
                return _torqueBall;
            }
        }

        #endregion

        #region Overrides

        public override void PrepareForNewCycle()
        {
            _ball.PrepareForNewTimerCycle();

            base.PrepareForNewCycle();
        }

        public override void TimerTestPosition(double elapsedTime)
        {
            _ball.TimerTestPosition(elapsedTime);

            base.TimerTestPosition(elapsedTime);
        }

        public override void TimerFinish()
        {
            _ball.TimerFinish();

            base.TimerFinish();
        }

        #endregion
    }
}
