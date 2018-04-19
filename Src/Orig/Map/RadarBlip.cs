using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Math3D;

namespace Game.Orig.Map
{

    #region enum: RadarBlipQual

    // It's a little funny for the map component to know what types of objects will use it, but I wanted a centralized enum.
    public enum RadarBlipQual
    {
        BNDLower = -1,		// This will equal the lowest RadarBlip - 1
        WearableItem,
        BuildingItem,
        ThrowableWeapon,
        SwingingWeapon,
        ProjectileWeapon,
        AmmoClip,
        Projectile,
        Mine,
        Person,
        StorageCrate,
        Money,
        Health,
        Mana,
        ArmorShard,
        Inventory,
        Vehicle,
        BNDLowerExpandedSet,     // Everything in the expanded set are blips that derive from the base set
        BNDLowerBallUserDefined = BNDLowerExpandedSet,  // This will equal the lowest BallUserDefined
        BallUserDefined00 = BNDLowerBallUserDefined,
        BallUserDefined01,
        BallUserDefined02,
        BallUserDefined03,
        BallUserDefined04,
        BallUserDefined05,
        BallUserDefined06,
        BallUserDefined07,
        BallUserDefined08,
        BallUserDefined09,
        BallUserDefined10,
        BallUserDefined11,
        BallUserDefined12,
        BallUserDefined13,
        BallUserDefined14,
        BallUserDefined15,
        BNDUpperBallUserDefined = BallUserDefined15,   // This will equal the highest BallUserDefined
        BNDLowerUserDefined,    // This will equal the lowest UserDefined
        UserDefined00 = BNDLowerUserDefined,
        UserDefined01,
        UserDefined02,
        UserDefined03,
        UserDefined04,
        UserDefined05,
        UserDefined06,
        UserDefined07,
        UserDefined08,
        UserDefined09,
        UserDefined10,
        UserDefined11,
        UserDefined12,
        UserDefined13,
        UserDefined14,
        UserDefined15,
        UserDefined16,
        UserDefined17,
        UserDefined18,
        UserDefined19,
        UserDefined20,
        UserDefined21,
        UserDefined22,
        UserDefined23,
        UserDefined24,
        UserDefined25,
        UserDefined26,
        UserDefined27,
        UserDefined28,
        UserDefined29,
        UserDefined30,
        UserDefined31,
        UserDefined32,
        UserDefined33,
        UserDefined34,
        UserDefined35,
        UserDefined36,
        UserDefined37,
        UserDefined38,
        UserDefined39,
        UserDefined40,
        UserDefined41,
        UserDefined42,
        UserDefined43,
        UserDefined44,
        UserDefined45,
        UserDefined46,
        UserDefined47,
        UserDefined48,
        UserDefined49,
        UserDefined50,
        BNDUpperUserDefined = UserDefined50,    // This will equal the highest UserDefined
        BNDUpperExpandedSet = BNDUpperUserDefined,
        BNDUpper,   // This will equal the highest RadarBlip + 1
    }

    #endregion
    #region enum: CollisionStyle

    public enum CollisionStyle
    {
        Ghost = 0,		// No collision handling
        Stationary,		// Collides with others, but is unnaffected by the collisions
        StationaryRotatable,		// Collides with others, but will only spin as a result (position doesn't change).  TODO: I could make the reverse of this, but I can't think of any case where it would be used.  And if I do, all the permutations when doing collision handling will really go up
        Standard		// Full collision response
    }

    #endregion

    /// <summary>
    /// This is the base class of a radar blip.  This is the only type of item that the map class will work with
    /// </summary>
    public class RadarBlip
    {
        #region Declaration Section

        private Sphere _sphere = null;

        // This tells you what type of object I represent.  Every blip type has a unique set of properties that are accessed through QueryItem.
        private RadarBlipQual _qual;
        private int _qualInt;		// This is usefull if the user has this defined as an integer

        // These are unique ID's that the map class knows me by.
        private long _token;
        private Guid _objectID;

        private CollisionStyle _collisionStyle;

        #endregion

        #region Constructor

        /// <summary>
        /// This is the basic constructor.  These are the only things that need to be passed in.
        /// </summary>
        public RadarBlip(Sphere sphere, CollisionStyle collisionStyle, RadarBlipQual blipQual, long token)
            : this(sphere, collisionStyle, blipQual, token, Guid.NewGuid())
        {
        }

        /// <summary>
        /// The only reason you would pass in your own guid is when loading a previously saved scene (the token
        /// works good for processing in ram, but when stuff needs to go to file, use the guid)
        /// </summary>
        public RadarBlip(Sphere sphere, CollisionStyle collisionStyle, RadarBlipQual blipQual, long token, Guid objectID)
        {
            _sphere = sphere;

            _collisionStyle = collisionStyle;

            _token = token;

            _qual = blipQual;
            _qualInt = _qual.GetHashCode();

            _objectID = objectID;
        }

        #endregion

        #region Properties

        public RadarBlipQual Qual
        {
            get
            {
                return _qual;
            }
        }

        public long Token
        {
            get
            {
                return _token;
            }
        }

        public Guid ObjectID
        {
            get
            {
                return _objectID;
            }
        }

        public Sphere Sphere
        {
            get
            {
                return _sphere;
            }
        }

        public CollisionStyle CollisionStyle
        {
            get
            {
                return _collisionStyle;
            }
            set
            {
                _collisionStyle = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This lets you examine any property through a common interface.  Each blip type will have a published enum and corresponding
        /// return data type for each enum value.  You probably want to keep things in the form of a simple datatype, because the only
        /// thing that will use this function is generic algorithms (Like AI.Lookout)
        /// </summary>
        public virtual object QueryItem(int item)
        {
            return null;
        }

        /// <summary>
        /// This function needs to be called before the timer is called.  Between this function and the timer function is when all the outside
        /// forces have a chance to influence the object (gravity, explosion blasts, conveyor belts, etc)
        /// </summary>
        /// <remarks>
        /// I've given this function a bit of thought.  It's really not needed, because when TimerFinish is done, that should be the equivalent
        /// of a new cycle.  But I like having more defined phases (even though there is an extra round of function calls to make)
        /// </remarks>
        public virtual void PrepareForNewCycle()
        {
        }

        /// <summary>
        /// This function can be called many times with different elapsed times passed in.  This function should only figure out a new
        /// position based on the previous tick's velocity and the time passed in.  Every time this function gets called, the starting point
        /// is the last call to PrepareForNewCycle.
        /// </summary>
        public virtual void TimerTestPosition(double elapsedTime)
        {
        }

        /// <summary>
        /// This function is used to calculate the new velocity based on all the forces that occured from Prepare to Now
        /// </summary>
        public virtual void TimerFinish()
        {
        }

        #endregion
    }
}
