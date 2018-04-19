using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.v2.NewtonDynamics.Import;

namespace Game.Newt.v2.NewtonDynamics
{
    // Good summary with pictures:
    // http://newtondynamics.com/wiki/index.php5?title=Joints

    //TODO:  See if these try to return to the initialized position through spring forces, or are free (I think that's what Stiffness is for?)
    //TODO:  These allow the second body to be null, figure out when that would be useful - doesn't a joint only make sense between two bodies?

    #region class: JointBase

    public abstract class JointBase : IDisposable
    {
        #region Constructor

        //NOTE:  Forcing handle to be passed in forces the derived classes to expose static create methods instead of public constructors, but I prefer the certainty
        protected JointBase(World world, IntPtr handle)
        {
            this.World = world;
            this.Handle = handle;

            ObjectStorage.Instance.AddJoint(handle, this);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ObjectStorage.Instance.RemoveJoint(this.Handle);

                Newton.NewtonDestroyJoint(this.World.Handle, this.Handle);

                //this.Handle = IntPtr.Zero;
            }
        }

        #endregion

        #region Public Properties

        public World World
        {
            get;
            private set;
        }

        public IntPtr Handle
        {
            get;
            private set;
        }

        public Body Body1
        {
            get
            {
                IntPtr bodyHandle = Newton.NewtonJointGetBody0(this.Handle);
                return ObjectStorage.Instance.GetBody(bodyHandle);
            }
        }
        //TODO:  See if this throws an exception if called from a JointUpVector
        public Body Body2
        {
            get
            {
                IntPtr bodyHandle = Newton.NewtonJointGetBody1(this.Handle);
                return ObjectStorage.Instance.GetBody(bodyHandle);
            }
        }

        /// <summary>
        /// This defaults to false
        /// </summary>
        /// <remarks>
        /// TODO:  See what happens if there are 3 bodies across 2 joints.  Will the first and third collide with each other?
        /// NOTE:  Also look at Body.IsCollidableWithBodiesConnectByJoints
        /// </remarks>
        public bool ShouldLinkedBodiesCollideEachOther
        {
            get
            {
                int retVal = Newton.NewtonJointGetCollisionState(this.Handle);
                return retVal == 0 ? false : true;
            }
            set
            {
                Newton.NewtonJointSetCollisionState(this.Handle, value ? 1 : 0);
            }
        }

        /// <summary>
        /// A value from 0 to 1 (default is .9 for most joints)
        /// </summary>
        /// <remarks>
        /// From the wiki:
        /// 
        /// * Constraint keep bodies together by calculating the exact force necessary to cancel the relative acceleration between one or
        /// more common points fixed in the two bodies. The problem is that when the bodies drift apart due to numerical integration
        /// inaccuracies, the reaction force work to pull eliminated the error but at the expense of adding extra energy to the system, does
        /// violating the rule that constraint forces must be work less. This is a inevitable situation and the only think we can do is to
        /// minimize the effect of the extra energy by dampening the force by some amount. In essence the stiffness coefficient tell Newton
        /// calculate the precise reaction force by only apply a fraction of it to the joint point. And value of 1.0 will apply the exact force,
        /// and a value of zero will apply only 10 percent. 
        ///
        /// * The stiffness is set to a all around value that work well for most situation, however the application can play with these parameter
        /// to make finals adjustment. A high value will make the joint stronger but more prompt to vibration of instability; a low value will
        /// make the joint more stable but weaker.
        /// </remarks>
        public double Stiffness
        {
            get
            {
                return Newton.NewtonJointGetStiffness(this.Handle);
            }
            set
            {
                Newton.NewtonJointSetStiffness(this.Handle, Convert.ToSingle(value));
            }
        }

        #endregion

        //TODO:  Implement this if you need a Destroying event
        //internal static extern void NewtonJointSetDestructor(IntPtr joint, NewtonConstraintDestructor destructor);

        //TODO:  Implement this when needed (looks like a way to get debug info about the joint to see how it's set up)
        //internal static extern void NewtonJointGetInfo(IntPtr joint, IntPtr info);

        //TODO:  Implement UserData across all the newton classes
        //internal static extern IntPtr NewtonJointGetUserData(IntPtr joint);
        //internal static extern void NewtonJointSetUserData(IntPtr joint, IntPtr userData);
    }

    #endregion

    #region class: JointBallAndSocket

    public class JointBallAndSocket : JointBase
    {
        protected JointBallAndSocket(World world, IntPtr handle)
            : base(world, handle) { }

        /// <summary>
        /// Locks translation, but allows for any rotation
        /// </summary>
        /// <param name="pivotPoint">world coords</param>
        public static JointBallAndSocket CreateBallAndSocket(World world, Point3D pivotPoint, Body body1, Body body2)
        {
            IntPtr handle = Newton.NewtonConstraintCreateBall(world.Handle, new NewtonVector3(pivotPoint).Vector, body1.Handle, body2.Handle);

            return new JointBallAndSocket(world, handle);
        }

        /// <summary>
        /// The joint defaults to no limits
        /// </summary>
        /// <param name="coneAxis">world coords, must be a unit vector</param>
        /// <param name="maxConeRadians">Null is no limit (if nonnull, newton clamps this to between the equivalent of 5 to 175 degrees)</param>
        /// <param name="maxTwistRadians">Null is no limit</param>
        public void SetConeLimits(Vector3D coneAxis, double? maxConeRadians, double? maxTwistRadians)
        {
            Newton.NewtonBallSetConeLimits(this.Handle, new NewtonVector3(coneAxis).Vector, maxConeRadians == null ? 0f : Convert.ToSingle(maxConeRadians.Value), maxTwistRadians == null ? 0f : Convert.ToSingle(maxTwistRadians));
        }

        //TODO:  Implement this
        //internal static extern void NewtonBallSetUserCallback(IntPtr ball, NewtonBallCallBack callback);
        //internal static extern void NewtonBallGetJointAngle(IntPtr ball, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] angle);		// these can only be used from the callback (I think)
        //internal static extern void NewtonBallGetJointOmega(IntPtr ball, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] omega);
        //internal static extern void NewtonBallGetJointForce(IntPtr ball, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);
    }

    #endregion
    #region class: JointHinge

    public class JointHinge : JointBase
    {
        protected JointHinge(World world, IntPtr handle)
            : base(world, handle) { }

        /// <summary>
        /// No translation, just rotation - like a door hinge.  If you want to allow translation as well, see corkscrew
        /// </summary>
        /// <param name="pivotPoint">Point along the hinge line (world coords)</param>
        /// <parparam name="pinDirection">Line of action of the hinge (world coords)</parparam>
        public static JointHinge CreateHinge(World world, Point3D pivotPoint, Vector3D pinDirection, Body body1, Body body2)
        {
            IntPtr handle = Newton.NewtonConstraintCreateHinge(world.Handle, new NewtonVector3(pivotPoint).Vector, new NewtonVector3(pinDirection).Vector, body1.Handle, body2.Handle);

            return new JointHinge(world, handle);
        }

        //TODO:  Implement this
        //internal static extern void NewtonHingeSetUserCallback(IntPtr hinge, NewtonHingeCallBack callback);
        //internal static extern float NewtonHingeGetJointAngle(IntPtr hinge);		// these can only be used from the callback (I think)
        //internal static extern float NewtonHingeGetJointOmega(IntPtr hinge);
        //internal static extern void NewtonHingeGetJointForce(IntPtr hinge, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);
        //internal static extern float NewtonHingeCalculateStopAlpha(IntPtr hinge, IntPtr desc, float angle);
    }

    #endregion
    #region class: JointSlider

    public class JointSlider : JointBase
    {
        protected JointSlider(World world, IntPtr handle)
            : base(world, handle) { }

        /// <summary>
        /// Locks translation to a single line (like a car's shocks).  Doesn't allow rotation - that's what corkscrew is for
        /// </summary>
        /// <param name="pivotPoint">Point along the slide line (world coords)</param>
        /// <parparam name="pinDirection">Direction of allowed travel (world coords)</parparam>
        public static JointSlider CreateSlider(World world, Point3D pivotPoint, Vector3D pinDirection, Body body1, Body body2)
        {
            IntPtr handle = Newton.NewtonConstraintCreateSlider(world.Handle, new NewtonVector3(pivotPoint).Vector, new NewtonVector3(pinDirection).Vector, body1.Handle, body2.Handle);

            return new JointSlider(world, handle);
        }

        //TODO:  Implement this
        //internal static extern void NewtonSliderSetUserCallback(IntPtr slider, NewtonSliderCallBack callback);
        //internal static extern float NewtonSliderGetJointPosit(IntPtr slider);		// these can only be used from the callback (I think)
        //internal static extern float NewtonSliderGetJointVeloc(IntPtr slider);
        //internal static extern void NewtonSliderGetJointForce(IntPtr slider, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);
        //internal static extern float NewtonSliderCalculateStopAccel(IntPtr slider, IntPtr desc, float position);
    }

    #endregion
    #region class: JointCorkscrew

    public class JointCorkscrew : JointBase
    {
        protected JointCorkscrew(World world, IntPtr handle)
            : base(world, handle) { }

        /// <summary>
        /// This is like a slider + hinge.  It allows sliding along the axis, as well as rotation around that axis
        /// </summary>
        /// <param name="pivotPoint">Point along the slide line (world coords)</param>
        /// <parparam name="pinDirection">Direction of allowed travel and spin (world coords)</parparam>
        public static JointCorkscrew CreateCorkscrew(World world, Point3D pivotPoint, Vector3D pinDirection, Body body1, Body body2)
        {
            IntPtr handle = Newton.NewtonConstraintCreateCorkscrew(world.Handle, new NewtonVector3(pivotPoint).Vector, new NewtonVector3(pinDirection).Vector, body1.Handle, body2.Handle);

            return new JointCorkscrew(world, handle);
        }

        //TODO:  Implement this
        //internal static extern void NewtonCorkscrewSetUserCallback(IntPtr corkscrew, NewtonCorkscrewCallBack callback);
        //internal static extern float NewtonCorkscrewGetJointPosit(IntPtr corkscrew);		// these can only be used from the callback (I think)
        //internal static extern float NewtonCorkscrewGetJointAngle(IntPtr corkscrew);
        //internal static extern float NewtonCorkscrewGetJointVeloc(IntPtr corkscrew);
        //internal static extern float NewtonCorkscrewGetJointOmega(IntPtr corkscrew);
        //internal static extern void NewtonCorkscrewGetJointForce(IntPtr corkscrew, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);
        //internal static extern float NewtonCorkscrewCalculateStopAlpha(IntPtr corkscrew, IntPtr desc, float angle);
        //internal static extern float NewtonCorkscrewCalculateStopAccel(IntPtr corkscrew, IntPtr desc, float position);
    }

    #endregion
    #region class: JointUniversal

    public class JointUniversal : JointBase
    {
        protected JointUniversal(World world, IntPtr handle)
            : base(world, handle) { }

        /// <summary>
        /// This is like a hinge joint, but around two axiis (orthoganal to each other).  I assume this is like a u-joint on a drive shaft
        /// </summary>
        /// <remarks>
        /// Saw a good animation here
        /// http://en.wikipedia.org/wiki/Universal_joint
        /// </remarks>
        /// <param name="pivotPoint">Point along the slide line (world coords)</param>
        /// <parparam name="pinDirection1">One line of action of the hinge (world coords) - must be orthogonal to pinDirection2</parparam>
        /// <parparam name="pinDirection2">Another line of action of the hinge (world coords) - must be orthogonal to pinDirection1</parparam>
        public static JointUniversal CreateUniversal(World world, Point3D pivotPoint, Vector3D pinDirection1, Vector3D pinDirection2, Body body1, Body body2)
        {
            IntPtr handle = Newton.NewtonConstraintCreateUniversal(world.Handle, new NewtonVector3(pivotPoint).Vector, new NewtonVector3(pinDirection1).Vector, new NewtonVector3(pinDirection2).Vector, body1.Handle, body2.Handle);

            return new JointUniversal(world, handle);
        }

        //TODO:  Implement this
        //internal static extern void NewtonUniversalSetUserCallback(IntPtr universal, NewtonUniversalCallBack callback);
        //internal static extern float NewtonUniversalGetJointAngle0(IntPtr universal);		// these can only be used from the callback (I think)
        //internal static extern float NewtonUniversalGetJointAngle1(IntPtr universal);
        //internal static extern float NewtonUniversalGetJointOmega0(IntPtr universal);
        //internal static extern float NewtonUniversalGetJointOmega1(IntPtr universal);
        //internal static extern void NewtonUniversalGetJointForce(IntPtr universal, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);
        //internal static extern float NewtonUniversalCalculateStopAlpha0(IntPtr universal, IntPtr desc, float angle);
        //internal static extern float NewtonUniversalCalculateStopAlpha1(IntPtr universal, IntPtr desc, float angle);
    }

    #endregion
    #region class: JointUpVector

    public class JointUpVector : JointBase
    {
        protected JointUpVector(World world, IntPtr handle)
            : base(world, handle) { }

        /// <summary>
        /// This is different than the other joints.  It locks a body to a single axis of rotation (doesn't affect translation)
        /// </summary>
        /// <param name="pinDirection">Line of action of the hinge (world coords)</param>
        public static JointUpVector CreateUpVector(World world, Vector3D pinDirection, Body body)
        {
            // Up vector joint functions
            IntPtr handle = Newton.NewtonConstraintCreateUpVector(world.Handle, new NewtonVector3(pinDirection).Vector, body.Handle);

            return new JointUpVector(world, handle);
        }

        /// <summary>
        /// This lets you change the axis after it's created.  Be careful to only make small changes at a time, or you'll get vibration
        /// </summary>
        public Vector3D PinDirection
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonUpVectorGetPin(this.Handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
            set
            {
                Newton.NewtonUpVectorSetPin(this.Handle, new NewtonVector3(value).Vector);
            }
        }
    }

    #endregion
    #region class: JointUserDefined

    //public class JointUserDefined : JointBase
    //{
    //TODO:  Implement this when needed (it lets the user create a custom joint - translation (x,y,z) and/or rotation (x,y,z) restrictions)
    // User defined bilateral Joint
    //internal static extern IntPtr NewtonConstraintCreateUserJoint(IntPtr newtonWorld, int maxDOF, NewtonUserBilateralCallBack callback, NewtonUserBilateralGetInfoCallBack getInfo, IntPtr childBody, IntPtr parentBody);
    //internal static extern void NewtonUserJointSetFeedbackCollectorCallback(IntPtr joint, NewtonUserBilateralCallBack getFeedback);
    //internal static extern void NewtonUserJointAddLinearRow(IntPtr joint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivot0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivot1, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] dir);
    //internal static extern void NewtonUserJointAddAngularRow(IntPtr joint, float relativeAngle, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] dir);
    //internal static extern void NewtonUserJointAddGeneralRow(IntPtr joint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] jacobian0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] jacobian1);
    //internal static extern void NewtonUserJointSetRowMinimumFriction(IntPtr joint, float friction);
    //internal static extern void NewtonUserJointSetRowMaximumFriction(IntPtr joint, float friction);
    //internal static extern void NewtonUserJointSetRowAcceleration(IntPtr joint, float acceleration);
    //internal static extern void NewtonUserJointSetRowSpringDamperAcceleration(IntPtr joint, float springK, float springD);
    //internal static extern void NewtonUserJointSetRowStiffness(IntPtr joint, float stiffness);
    //internal static extern float NewtonUserJointGetRowForce(IntPtr joint, int row);
    //}

    #endregion
}
