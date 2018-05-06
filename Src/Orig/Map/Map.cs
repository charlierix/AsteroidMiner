using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Math3D;

namespace Game.Orig.Map
{
    public class SimpleMap		// I wanted to call this Map, but everybody would have to reference it as Map.Map (the using statement doesn't help)
    {
        #region class: CollisionTracker

        private class CollisionTracker
        {

            private const int MAXINCREMENTS = 3;

            private SortedList<long, SortedList<long, int>> _collisions = new SortedList<long, SortedList<long, int>>();

            /// <summary>
            /// This tells me about a collision.  If I return true, I don't know about it.  If I return false, I still know about it.
            /// </summary>
            /// <returns>
            /// True:  Run the collision
            /// False:  Ignore the collision
            /// </returns>
            public bool AddCollision(Collision collision)
            {

                return true;

                // Always put the lower as the key

                long lower, upper;

                #region Sort the tokens

                if (collision.Blip1.Token < collision.Blip2.Token)
                {
                    lower = collision.Blip1.Token;
                    upper = collision.Blip2.Token;
                }
                else
                {
                    lower = collision.Blip2.Token;
                    upper = collision.Blip1.Token;
                }

                #endregion

                // Check the list
                if (_collisions.ContainsKey(lower))
                {
                    if (_collisions[lower].ContainsKey(upper))
                    {
                        // I found it, return false
                        return false;
                    }
                    else
                    {
                        _collisions[lower].Add(upper, 0);
                    }
                }
                else
                {
                    #region Add New

                    _collisions.Add(lower, new SortedList<long, int>());
                    _collisions[lower].Add(upper, 0);

                    #endregion
                }

                // Exit Function
                return true;
            }

            public void IncrementAll()
            {

                int outerCntr = 0;
                int innerCntr = 0;
                long outerKey, innerKey;

                while (outerCntr < _collisions.Keys.Count)
                {
                    innerCntr = 0;
                    outerKey = _collisions.Keys[outerCntr];

                    while (innerCntr < _collisions[outerKey].Keys.Count)
                    {
                        innerKey = _collisions[outerKey].Keys[innerCntr];

                        // Bump the number of times that I've seen this
                        _collisions[outerKey][innerKey]++;

                        if (_collisions[outerKey][innerKey] > MAXINCREMENTS)
                        {
                            _collisions[outerKey].RemoveAt(innerCntr);
                        }
                        else
                        {
                            innerCntr++;
                        }
                    }

                    // See if there are any left
                    if (_collisions[outerKey].Keys.Count == 0)
                    {
                        _collisions.RemoveAt(outerCntr);
                    }
                    else
                    {
                        outerCntr++;
                    }
                }

            }

        }

        #endregion

        #region Events

        /// <summary>
        /// This gets fired once per tick if there were any collisions.  This is the same list as what's returned, but
        /// odds are, these collisions will kick off audio/visual stuff, and I have a feeling an event is easier than
        /// passing an array around.
        /// </summary>
        public event CollisionsDelegate Collisions = null;

        #endregion
        #region Declaration Section

        // While the timer is running, I can't modify my list of blips
        private bool _isTimerRunning = false;
        private List<long> _removeRequests = new List<long>();
        private List<RadarBlip> _addRequests = new List<RadarBlip>();
        private bool _clearRequest = false;
        private List<long> _clearRequestSkipTokens = null;

        /// <summary>
        /// Every time I create a token, I bump this by 1
        /// </summary>
        //private long _tokenGenerator = 0;		// long.MinValue

        /// <summary>
        /// These are all the radar blips, sorted on token
        /// </summary>
        private SortedList<long, RadarBlip> _blips = new SortedList<long, RadarBlip>();

        /// <summary>
        /// This is the subset _blips where IsCollidable is true
        /// </summary>
        private List<RadarBlip> _collidableBlips = new List<RadarBlip>();
        /// <summary>
        /// This is the subset _blips where IsCollidable is false
        /// </summary>
        private List<RadarBlip> _nonCollidableBlips = new List<RadarBlip>();

        private bool _doStandardCollisions = true;

        // These settings control how the timer runs
        private PullApartType _pullApartType = PullApartType.None;
        private int _pullApartInstantMaxIterations = 15;

        /// <summary>
        /// This must be set by the outside caller before calling my timer
        /// </summary>
        private CollisionHandler _collisionHandler = null;

        private CollisionTracker _collisionTracker = new CollisionTracker();

        #endregion

        #region Properties

        /// <summary>
        /// I expose this so that the caller can hand me a derived class that knows about special objects
        /// NOTE:  This must be set before calling Timer
        /// </summary>
        public CollisionHandler CollisionHandler
        {
            get
            {
                return _collisionHandler;
            }
            set
            {
                _collisionHandler = value;
            }
        }

        public int Count
        {
            get
            {
                return _blips.Count;
            }
        }

        /// <summary>
        /// I can't think of a good reason to set this to false (other than debugging)
        /// </summary>
        public bool DoStandardCollisions
        {
            get
            {
                return _doStandardCollisions;
            }
            set
            {
                _doStandardCollisions = value;
            }
        }

        public PullApartType TimerPullApartType
        {
            get
            {
                return _pullApartType;
            }
            set
            {
                _pullApartType = value;
            }
        }
        public int TimerPullApartInstantMaxIterations
        {
            get
            {
                return _pullApartInstantMaxIterations;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("Value must be at least 1");
                }
                else if (value > 1000)		// even this is pretty excessive
                {
                    throw new ArgumentOutOfRangeException("Value cannot exceed 1000");
                }

                _pullApartInstantMaxIterations = value;
            }
        }

        #endregion

        #region Public Methods (Add/Remove)

        public void Add(RadarBlip blip)
        {
            // See if I should wait
            if (_isTimerRunning)
            {
                _addRequests.Add(blip);
                return;
            }

            // Add it
            _blips.Add(blip.Token, blip);

            if (blip.CollisionStyle == CollisionStyle.Ghost)
            {
                _nonCollidableBlips.Add(blip);
            }
            else
            {
                _collidableBlips.Add(blip);
            }
        }

        /// <summary>
        /// This function will remove the blip from my knowledge.  I give you the pointer to it in case you want to keep it.
        /// </summary>
        /// <remarks>
        /// For example, if you pick up a rock, call this function, and you will be the parent of that rock.  But, if you throw the
        /// rock, you will need to give the rock back to me.  The reason I went with that approach, is because I don't want to be
        /// in charge of all the items that you are carrying.  It gets more complex if you put an item in your pocket.  Now that
        /// item is concealed.
        /// 
        /// There are two modes of an item being hidden.  There is completely hidden, which means the Audio/Visual class will
        /// be turned off, and there is hidden from my collision detection.  For my collision detection to work, whenever you
        /// pick up an item that needs to be included in collision checks, you will need to modify your radius so that the radius
        /// encompasses all objects that you are holding.
        /// </remarks>
        public RadarBlip Remove(long token)
        {
            RadarBlip retVal = _blips[token];

            // See if I should wait
            if (_isTimerRunning)
            {
                _removeRequests.Add(token);
                return retVal;
            }

            // Remove it
            _blips.Remove(token);
            _nonCollidableBlips.Remove(retVal);		// it is only in one of the lists, but the style may change in the middle of a tick
            _collidableBlips.Remove(retVal);

            // Exit Function
            return retVal;
        }

        public void Clear()
        {
            Clear(new List<long>());
        }
        public void Clear(List<long> skipTokens)
        {
            // See if it should be delayed
            if (_isTimerRunning)
            {
                _clearRequest = true;
                _clearRequestSkipTokens = skipTokens;
            }

            int index = 0;
            #region _blips

            while (index < _blips.Count)
            {
                if (skipTokens.Contains(_blips.Keys[index]))
                {
                    index++;
                }
                else
                {
                    _blips.Remove(_blips.Keys[index]);
                }
            }

            #endregion
            #region _collidableBlips

            index = 0;

            while (index < _collidableBlips.Count)
            {
                if (skipTokens.Contains(_collidableBlips[index].Token))
                {
                    index++;
                }
                else
                {
                    _collidableBlips.RemoveAt(index);
                }
            }

            #endregion
            #region _nonCollidableBlips

            index = 0;

            while (index < _nonCollidableBlips.Count)
            {
                if (skipTokens.Contains(_nonCollidableBlips[index].Token))
                {
                    index++;
                }
                else
                {
                    _nonCollidableBlips.RemoveAt(index);
                }
            }

            #endregion
        }

        #endregion
        #region Public Methods (Find)

        public RadarBlip[] GetAllBlips()
        {
            RadarBlip[] retVal = new RadarBlip[_blips.Values.Count];

            for (int blipCntr = 0; blipCntr < retVal.Length; blipCntr++)
            {
                retVal[blipCntr] = _blips.Values[blipCntr];
            }

            return retVal;
        }

        #endregion
        #region Public Methods (Timer)

        /// <summary>
        /// This function needs to be called before the timer is called.  Between this function and the timer function is
        /// when all the outside forces have a chance to influence the all of the blips (gravity, explosion blasts, conveyor
        /// belts, etc)
        /// </summary>
        /// <remarks>
        /// Any blips that contain other blips need to pass the message along (if they think their children will be affected
        /// by outide forces)
        /// </remarks>
        public void PrepareForNewTimerCycle()
        {
            #region Make sure the blips are in the right piles

            // There is a chance that the blips changed states, and rather than deal with calling a delgate in the
            // blip's property set, I'll just do a quick check here

            int index = 0;
            while (index < _collidableBlips.Count)
            {
                if (_collidableBlips[index].CollisionStyle == CollisionStyle.Ghost)
                {
                    _nonCollidableBlips.Add(_collidableBlips[index]);
                    _collidableBlips.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            index = 0;
            while (index < _nonCollidableBlips.Count)
            {
                if (_nonCollidableBlips[index].CollisionStyle != CollisionStyle.Ghost)
                {
                    _collidableBlips.Add(_nonCollidableBlips[index]);
                    _nonCollidableBlips.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            #endregion

            foreach (RadarBlip blip in _blips.Values)
            {
                blip.PrepareForNewCycle();
            }
        }

        /// <summary>
        /// This function calls timers and does collision detection (and response)
        /// </summary>
        /// <remarks>
        /// There are two phases that I go through:
        /// 
        /// Phase 1 occurs before I call the timers:  If I get a penetrating collision, I will move the objects directly away from each other (no
        /// physics or rotation, just separation).  I will repeat this phase 1 until there are no collisions, or the max iterations is exceeded. (this
        /// could happen if there is a large blob of ojects sticking together)
        /// 
        /// Phase 2 occurs while I'm calling the timers:  I will try to step by elapsed time.  If I get a penetrating collision, I will do a binary
        /// search to find the moment of collision.  I then apply physics to the two colliding objects, call finish on all the objects, and repeat
        /// with the remaining time
        /// </remarks>
        /// <returns>The blips that collided during this tick (so you can play sounds, or whatever) - this is the same list as what's in the event</returns>
        public Collision[] Timer(double elapsedTime)
        {
            bool isOuterTimer = false;
            if (!_isTimerRunning)
            {
                isOuterTimer = true;

                // Let myself know that the timer is running
                _isTimerRunning = true;

                _removeRequests.Clear();
                _addRequests.Clear();
                _clearRequest = false;
                _clearRequestSkipTokens = null;
            }

            List<Collision> retVal;

            if (_collidableBlips.Count > 0)
            {
                #region Collidable Blips

                // Do the real collision handling
                List<Collision> standardCollisions = null;
                if (_doStandardCollisions)
                {
                    standardCollisions = TimerStandard(elapsedTime);
                }
                else
                {
                    foreach (RadarBlip blip in _collidableBlips)
                    {
                        blip.TimerTestPosition(elapsedTime);
                        blip.TimerFinish();
                    }
                }

                // See if I should pull them apart more directly
                List<Collision> pullApartCollisions = null;
                switch (_pullApartType)
                {
                    case PullApartType.None:
                        break;

                    case PullApartType.Instant:
                        pullApartCollisions = TimerPullApartInstant();
                        break;

                    case PullApartType.Force:
                        pullApartCollisions = TimerPullApartSpring();
                        break;

                    default:
                        throw new ApplicationException("Unknown PullApartType: " + _pullApartType.ToString());
                }

                // Raise some events, and let the outside world play sounds, do damage, whatever
                retVal = MergeCollisionLists(standardCollisions, pullApartCollisions);

                if (retVal.Count > 0 && this.Collisions != null)
                {
                    this.Collisions(this, retVal.ToArray());
                }

                #endregion
            }
            else
            {
                retVal = new List<Collision>();
            }

            // The noncollidable blips need to be called
            foreach (RadarBlip blip in _nonCollidableBlips)
            {
                blip.TimerTestPosition(elapsedTime);
                blip.TimerFinish();
            }

            // Bump the tracker
            _collisionTracker.IncrementAll();

            // Add/Remove pending blips
            if (isOuterTimer)
            {
                _isTimerRunning = false;
                AddRemovePending();
            }
            else
            {
                int three = 5;		// I don't think this ever hits
            }

            if (retVal.Count == 0)
            {
                return null;
            }
            else
            {
                return retVal.ToArray();
            }
        }

        #endregion
        #region Public Methods (Misc)

        /// <summary>
        /// Every radar blip needs a unique token from this function
        /// </summary>
        /// <remarks>
        /// Tokens aren't persisted across runs.  Use the ObjectID for that
        /// </remarks>
        //public long GetNextToken()
        //{
        //    _tokenGenerator++;
        //    return _tokenGenerator;
        //}

        #endregion

        #region Private Methods

        /// <summary>
        /// The job of this function is to pull stuff away from each other when they are already inside each other's space.  I don't
        /// smack them like pool balls (that's done in the standard phase)
        /// </summary>
        private List<Collision> TimerPullApartInstant()
        {
            int infiniteLoopDetector = 0;
            SortedList<long, List<long>> retVal = new SortedList<long, List<long>>();

            do
            {
                // Find a penetrating collision
                Collision collision = PullApartInstantSprtFindPenetrating();

                if (collision == null)
                {
                    // Nothing is penetrating.  Jump out of the loop
                    break;
                }

                // Something is penetrating.  Make sure my return structure knows about it
                AddToGlobalCollisions(retVal, collision);

                // Pull these two items apart
                _collisionHandler.PullItemsApartInstant(collision.Blip1, collision.Blip2);

                // Bump the counter
                infiniteLoopDetector++;
            } while (infiniteLoopDetector < _pullApartInstantMaxIterations);

            // Exit Function
            return FlattenGlobalList(retVal);
        }
        private Collision PullApartInstantSprtFindPenetrating()
        {
            // I will only look at the collidable list
            for (int outerCntr = 0; outerCntr < _collidableBlips.Count - 1; outerCntr++)
            {
                for (int innerCntr = outerCntr + 1; innerCntr < _collidableBlips.Count; innerCntr++)
                {
                    if (_collidableBlips[outerCntr].CollisionStyle == CollisionStyle.Standard && _collidableBlips[innerCntr].CollisionStyle == CollisionStyle.Standard)
                    {
                        // They both can be affected by being pulled apart, so I will check them
                        if (_collisionHandler.IsColliding(_collidableBlips[outerCntr], _collidableBlips[innerCntr]) == CollisionDepth.Penetrating)
                        {
                            // Found penetration
                            return new Collision(_collidableBlips[outerCntr], _collidableBlips[innerCntr]);
                        }
                    }
                }
            }

            // Nothing penetrating
            return null;
        }

        private List<Collision> TimerPullApartSpring()
        {
            List<Collision> retVal = PullApartSpringSprtFindAllPenetrating();

            foreach (Collision collision in retVal)
            {
                // Pull these two items apart
                _collisionHandler.PullItemsApartSpring(collision.Blip1, collision.Blip2);
            }

            // Exit Function
            return retVal;
        }
        private List<Collision> PullApartSpringSprtFindAllPenetrating()
        {
            List<Collision> retVal = new List<Collision>();

            // I will only look at the collidable list
            for (int outerCntr = 0; outerCntr < _collidableBlips.Count - 1; outerCntr++)
            {
                for (int innerCntr = outerCntr + 1; innerCntr < _collidableBlips.Count; innerCntr++)
                {
                    if (_collidableBlips[outerCntr].CollisionStyle == CollisionStyle.Standard || _collidableBlips[innerCntr].CollisionStyle == CollisionStyle.Standard)
                    {
                        // At least one of them can be affected by being pulled apart, so I will check them
                        if (_collisionHandler.IsColliding(_collidableBlips[outerCntr], _collidableBlips[innerCntr]) == CollisionDepth.Penetrating)
                        {
                            // Found penetration
                            retVal.Add(new Collision(_collidableBlips[outerCntr], _collidableBlips[innerCntr]));
                        }
                    }
                }
            }

            // Nothing penetrating
            return retVal;
        }

        private List<Collision> TimerStandard(double elapsedTime)
        {
            const int MAXINNERTRIES = 5;
            const int MAXOUTERTRIES = 10;

            bool shouldPrepareForNew = false;		// I only need to do this if the time was chopped down
            double currentTime = 0;
            double targetTime = elapsedTime;
            int innerTries = 0;
            int outerTries = 0;

            SortedList<long, List<long>> retVal = new SortedList<long, List<long>>();
            List<Collision> touchingCollisions;

            while (currentTime < elapsedTime && outerTries < MAXOUTERTRIES)
            {
                if (shouldPrepareForNew)
                {
                    #region PrepareForNew

                    shouldPrepareForNew = false;

                    foreach (RadarBlip blip in _collidableBlips)
                    {
                        blip.PrepareForNewCycle();
                    }

                    innerTries = 0;

                    #endregion
                }

                // Try to move all the blips, but stop on the first penetrating collision.  If I make it all the way
                // through the function, I may have a list of touching collisions
                Collision penetrating = TimerStandardSprtTryMove(out touchingCollisions, targetTime - currentTime, innerTries >= MAXINNERTRIES);

                if (penetrating != null)
                {
                    // Roll back time, and try again
                    targetTime = (currentTime + targetTime) / 2d;
                    innerTries++;
                    continue;
                }

                touchingCollisions = TimerStandardSprtFilterPreviousCollisions(touchingCollisions, retVal);

                if (touchingCollisions.Count > 0)
                {
                    // Bounce them off of each other
                    TimerStandardSprtCollide(touchingCollisions);
                    AddToGlobalCollisions(retVal, touchingCollisions);
                    shouldPrepareForNew = true;
                }

                // Commit this time slice
                foreach (RadarBlip blip in _collidableBlips)
                {
                    blip.TimerFinish();		// blip.TimerTestPosition was called in TimerPhase2SprtTryMove
                }

                // Prep for a new step
                currentTime = targetTime;
                targetTime = elapsedTime;
                outerTries++;
            }

            #region Orig Draft
            /*

			CollisionDepth collisionDepth;

			while (currentTime < elapsedTime)
			{
				DoPhysics(targetTime - currentTime);

				collisionDepth = DoCollisionDetection();

				if (collisionDepth == CollisionState.Penetrating)
				{
					// Went too far, scale back
					targetTime = (currentTime + targetTime) / 2d;
				}
				else
				{
					if (collisionDepth == CollisionState.Colliding)
					{
						int cntr = 0;
						do
						{
							ResolveCollisions();
							cntr++;
						} while ((CheckForCollisions() == CollisionState.Colliding) && (cntr < 100));
					}

					// Prep for a new step
					currentTime = targetTime;
					targetTime = elapsedTime;
				}
			}

			*/
            #endregion

            // Exit Function
            return FlattenGlobalList(retVal);
        }
        private List<Collision> TimerStandard_OLD(double elapsedTime)
        {
            const int MAXINNERTRIES = 5;
            const int MAXOUTERTRIES = 10;

            bool shouldPrepareForNew = false;		// I only need to do this if the time was chopped down
            double currentTime = 0;
            double targetTime = elapsedTime;
            int innerTries = 0;
            int outerTries = 0;

            SortedList<long, List<long>> retVal = new SortedList<long, List<long>>();
            List<Collision> touchingCollisions;

            while (currentTime < elapsedTime && outerTries < MAXOUTERTRIES)
            {
                if (shouldPrepareForNew)
                {
                    #region PrepareForNew

                    shouldPrepareForNew = false;

                    foreach (RadarBlip blip in _collidableBlips)
                    {
                        blip.PrepareForNewCycle();
                    }

                    innerTries = 0;

                    #endregion
                }

                // Try to move all the blips, but stop on the first penetrating collision.  If I make it all the way
                // through the function, I may have a list of touching collisions
                Collision penetrating = TimerStandardSprtTryMove(out touchingCollisions, targetTime - currentTime, innerTries >= MAXINNERTRIES);

                if (penetrating != null)
                {
                    // Roll back time, and try again
                    targetTime = (currentTime + targetTime) / 2d;
                    innerTries++;
                    continue;
                }

                if (touchingCollisions.Count > 0)
                {
                    // Bounce them off of each other
                    TimerStandardSprtCollide(touchingCollisions);
                    AddToGlobalCollisions(retVal, touchingCollisions);
                    shouldPrepareForNew = true;
                }

                // Commit this time slice
                foreach (RadarBlip blip in _collidableBlips)
                {
                    blip.TimerFinish();		// blip.TimerTestPosition was called in TimerPhase2SprtTryMove
                }

                // Prep for a new step
                currentTime = targetTime;
                targetTime = elapsedTime;
                outerTries++;
            }

            #region Orig Draft
            /*

			CollisionDepth collisionDepth;

			while (currentTime < elapsedTime)
			{
				DoPhysics(targetTime - currentTime);

				collisionDepth = DoCollisionDetection();

				if (collisionDepth == CollisionState.Penetrating)
				{
					// Went too far, scale back
					targetTime = (currentTime + targetTime) / 2d;
				}
				else
				{
					if (collisionDepth == CollisionState.Colliding)
					{
						int cntr = 0;
						do
						{
							ResolveCollisions();
							cntr++;
						} while ((CheckForCollisions() == CollisionState.Colliding) && (cntr < 100));
					}

					// Prep for a new step
					currentTime = targetTime;
					targetTime = elapsedTime;
				}
			}

			*/
            #endregion

            // Exit Function
            return FlattenGlobalList(retVal);
        }
        private Collision TimerStandardSprtTryMove(out List<Collision> touchingCollisions, double elapsedTime, bool treatPenetratingAsTouching)
        {
            touchingCollisions = new List<Collision>();

            // Move the first blip
            _collidableBlips[0].TimerTestPosition(elapsedTime);

            for (int outerCntr = 0; outerCntr < _collidableBlips.Count - 1; outerCntr++)
            {
                for (int innerCntr = outerCntr + 1; innerCntr < _collidableBlips.Count; innerCntr++)
                {
                    if (outerCntr == 0)
                    {
                        // Move this blip
                        _collidableBlips[innerCntr].TimerTestPosition(elapsedTime);
                    }

                    // See if these two will benefit from a collision (if they won't, then I will end up short circuiting trying
                    // to process two immoble penetrating objects)

                    if (_collidableBlips[outerCntr].CollisionStyle == CollisionStyle.Standard || _collidableBlips[innerCntr].CollisionStyle == CollisionStyle.Standard)
                    {
                        // Since at least one of them is standard, it can be pushed away
                    }
                    //else if (_collidableBlips[outerCntr].CollisionStyle == CollisionStyle.StationaryRotatable || _collidableBlips[innerCntr].CollisionStyle == CollisionStyle.StationaryRotatable)
                    //{
                    //    // At least one is rotatable.  If they both are, they may spin each other.  If the other is stationary, then			<---- On second thought, I don't like this.  My algorithm would contiually divide time during each frame.  Too inneficient
                    //		// it will stop the spin of the rotatable one
                    //}
                    else
                    {
                        // These two blips wouldn't benefit from colliding with each other
                        continue;
                    }

                    // See if these two are colliding
                    switch (_collisionHandler.IsColliding(_collidableBlips[outerCntr], _collidableBlips[innerCntr]))
                    {
                        case CollisionDepth.Touching:
                            touchingCollisions.Add(new Collision(_collidableBlips[outerCntr], _collidableBlips[innerCntr]));
                            break;

                        case CollisionDepth.Penetrating:
                            if (treatPenetratingAsTouching)
                            {
                                // Too many cycles have occurred.  Plow ahead with this
                                touchingCollisions.Add(new Collision(_collidableBlips[outerCntr], _collidableBlips[innerCntr]));
                            }
                            else
                            {
                                // No need to keep scanning.  This one's a show stopper
                                return new Collision(_collidableBlips[outerCntr], _collidableBlips[innerCntr]);
                            }
                            break;
                    }

                }
            }

            // Exit Function
            return null;
        }
        private void TimerStandardSprtCollide(List<Collision> touchingCollisions)
        {
            foreach (Collision collision in touchingCollisions)
            {
                if (!_collisionHandler.Collide(collision.Blip1, collision.Blip2))
                {
                    //throw new ApplicationException("These blips were marked as collidable, but the collision handler didn't know what to do with them: " + collision.Blip1.GetType().ToString() + ", " + collision.Blip2.GetType().ToString());
                }
            }
        }
        private List<Collision> TimerStandardSprtFilterPreviousCollisions(List<Collision> currentCollisions, SortedList<long, List<long>> previousCollisions)
        {
            List<Collision> retVal = new List<Collision>();

            bool addIt;

            foreach (Collision collision in currentCollisions)
            {
                addIt = true;

                if (previousCollisions.ContainsKey(collision.Blip1.Token))
                {
                    if (previousCollisions[collision.Blip1.Token].Contains(collision.Blip2.Token))
                    {
                        // These two have already collided.  Don't do it again
                        addIt = false;
                    }
                }
                else if (previousCollisions.ContainsKey(collision.Blip2.Token))
                {
                    if (previousCollisions[collision.Blip2.Token].Contains(collision.Blip1.Token))
                    {
                        // These two have already collided.  Don't do it again
                        addIt = false;
                    }
                }

                if (addIt && _collisionTracker.AddCollision(collision))
                {
                    // These two haven't collided yet
                    retVal.Add(collision);
                }
            }

            // Exit Function
            return retVal;
        }

        private List<Collision> MergeCollisionLists(List<Collision> standardCollisions, List<Collision> pullApartCollisions)
        {
            SortedList<long, List<long>> total = new SortedList<long, List<long>>();

            if (standardCollisions != null)
            {
                AddToGlobalCollisions(total, standardCollisions);
            }

            if (pullApartCollisions != null)
            {
                AddToGlobalCollisions(total, pullApartCollisions);
            }

            // Exit Function
            return FlattenGlobalList(total);
        }
        private void AddToGlobalCollisions(SortedList<long, List<long>> returnTokens, List<Collision> collisions)
        {
            foreach (Collision collision in collisions)
            {
                AddToGlobalCollisions(returnTokens, collision);
            }
        }
        private void AddToGlobalCollisions(SortedList<long, List<long>> returnTokens, Collision collision)
        {
            long lower, upper;

            if (collision.Blip1.Token < collision.Blip2.Token)
            {
                lower = collision.Blip1.Token;
                upper = collision.Blip2.Token;
            }
            else
            {
                lower = collision.Blip2.Token;
                upper = collision.Blip1.Token;
            }

            // Always put the lower as the key

            if (returnTokens.ContainsKey(lower))
            {
                #region Add upper to lower

                if (!returnTokens[lower].Contains(upper))
                {
                    returnTokens[lower].Add(upper);
                }

                #endregion
            }
            else
            {
                #region Add New

                returnTokens.Add(lower, new List<long>());
                returnTokens[lower].Add(upper);

                #endregion
            }
        }
        private List<Collision> FlattenGlobalList(SortedList<long, List<long>> collidingTokens)
        {
            List<Collision> retVal = new List<Collision>();

            foreach (long token1 in collidingTokens.Keys)
            {
                foreach (long token2 in collidingTokens[token1])
                {
                    retVal.Add(new Collision(_blips[token1], _blips[token2]));
                }
            }

            return retVal;
        }

        private void AddRemovePending()
        {
            if (_clearRequest)
            {
                Clear(_clearRequestSkipTokens);
            }
            else
            {
                foreach (long token in _removeRequests)
                {
                    Remove(token);
                }
            }

            foreach (RadarBlip blip in _addRequests)
            {
                Add(blip);
            }
        }

        #endregion
    }

    #region class: Collision

    public class Collision
    {
        public RadarBlip Blip1;
        public RadarBlip Blip2;

        public Collision(RadarBlip blip1, RadarBlip blip2)
        {
            this.Blip1 = blip1;
            this.Blip2 = blip2;
        }
    }

    #endregion

    #region delegate: CollisionsDelegate

    // The convention is to name delegates as somethingHandler, but I think this is the less confusing name
    public delegate void CollisionsDelegate(object sender, Collision[] collisions);

    #endregion

    #region enum: PullApartType

    public enum PullApartType
    {
        None = 0,
        Instant,
        Force
    }

    #endregion
}
