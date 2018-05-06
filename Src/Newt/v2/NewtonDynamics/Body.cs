using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics.Import;

namespace Game.Newt.v2.NewtonDynamics
{
    //TODO: Don't tie the physics so tight with the graphics:
    //		Make a readonly class that holds current position/orientation (or just use Tuple<Point3D, Quaternion>).  Expose this as a volatile from body.  Figure out how to notify across threads that there is a change
    //		Maybe instead of notifying across threads, store some kind of tickcount for when the last update occured.  Then the main thread can poll all the bodies and update graphics for bodies that were updated more recently than the last tick

    /// <summary>
    /// NOTE:  There is no body collided event exposed from this class.  Instead, call MaterialManager.RegisterCollisionEvent
    /// TODO:  Expose an event off of this class that subscribes to some global listener (tell it what materials you're interested in).  There may be some efficiency issues if not done right?
    /// </summary>
    public class Body : IDisposable, IComparable<Body>, IEquatable<Body>
    {
        #region Events

        #region BodyMoved

        //NOTE:  The constructor already hooked to the newton body moved callback, so there's nothing extra to do here
        public event EventHandler BodyMoved = null;

        private Newton.NewtonSetTransform _newtonBodyMoved = null;
        private void InvokeBodyMoved(IntPtr body, float[] matrix, int threadIndex)
        {
            OnBodyMoved(new BodyMovedArgs(new NewtonMatrix(matrix).ToWPF(), threadIndex));
        }

        protected virtual void OnBodyMoved(BodyMovedArgs e)
        {
            // Update the visuals (bring them from model coords to world coords)
            if (this.Visuals != null)
            {
                foreach (Visual3D visual in this.Visuals)
                {
                    visual.Transform = new MatrixTransform3D(e.OffsetMatrix);
                }
            }

            // Raise the event
            if (this.BodyMoved != null)
            {
                this.BodyMoved(this, e);
            }
        }

        #endregion

        #region ApplyForceAndTorque

        private EventHandler<BodyApplyForceAndTorqueArgs> _applyForceAndTorque = null;
        public event EventHandler<BodyApplyForceAndTorqueArgs> ApplyForceAndTorque
        {
            add
            {
                if (_applyForceAndTorque == null)
                {
                    _newtonApplyForceAndTorque = new Newton.NewtonApplyForceAndTorque(InvokeApplyForceAndTorque);
                    Newton.NewtonBodySetForceAndTorqueCallback(_handle, _newtonApplyForceAndTorque);
                }

                _applyForceAndTorque += value;
            }
            remove
            {
                _applyForceAndTorque -= value;

                if (_applyForceAndTorque == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyForceAndTorque (remove): {_handle}");

                    _newtonApplyForceAndTorque = null;
                    Newton.NewtonBodySetForceAndTorqueCallback(_handle, _newtonApplyForceAndTorque);
                }
            }
        }

        private Newton.NewtonApplyForceAndTorque _newtonApplyForceAndTorque = null;
        private void InvokeApplyForceAndTorque(IntPtr body, float timestep, int threadIndex)
        {
            OnApplyForceAndTorque(new BodyApplyForceAndTorqueArgs(ObjectStorage.Instance.GetBody(body), timestep, threadIndex));
        }

        protected virtual void OnApplyForceAndTorque(BodyApplyForceAndTorqueArgs e)
        {
            if (_applyForceAndTorque != null)
            {
                _applyForceAndTorque(this, e);
            }
        }

        #endregion

        public event EventHandler Disposing = null;

        #endregion

        #region Declaration Section

        private IntPtr _worldHandle;
        private WorldBase _world = null;
        private CollisionHull _collisionHull = null;

        #endregion

        #region Constructor

        //NOTE: Taking in token was added so that if a body needs to change size, a new one can be built using
        //the same token.  In general, always pass null to guarantee unique tokens for each body
        public Body(CollisionHull hull, Matrix3D offsetMatrix, double mass, Visual3D[] visuals = null, long? token = null)
        {
            _world = hull.World;
            _worldHandle = _world.Handle;
            this.Visuals = visuals;

            //NOTE:  The collision hull can take a null offset matrix, but the body can't
            _handle = Newton.NewtonCreateBody(_worldHandle, hull.Handle, new NewtonMatrix(offsetMatrix).Matrix);

            IntPtr hullHandlePost = Newton.NewtonBodyGetCollision(_handle);
            if (hullHandlePost != hull.Handle)
            {
                // For some reason, this is coming back with a new handle for compound collision hulls.  So store this second one as well
                ObjectStorage.Instance.AddCollisionHull(hullHandlePost, hull.Clone(hullHandlePost));

                // Testing shows the old one to still be out there.  I should probably clone the hull with this new handle using a special overload of
                // the constructor.  But as long as the user doesn't manipulate hulls after they are bound to a body, everything should be fine
                //int test = Newton.NewtonCollisionIsTriggerVolume(hull.Handle);
            }

            // Listen to the move event (the invoke method is a newton callback that converts the newton stuff into c# friendly stuff)
            _newtonBodyMoved = new Newton.NewtonSetTransform(InvokeBodyMoved);
            Newton.NewtonBodySetTransformCallback(_handle, _newtonBodyMoved);
            this.OffsetMatrix = offsetMatrix;		// calling this explicitly, because it call OnBodyMoved (making sure the visual is synced)

            // Store the default mass matrix
            this.Mass = mass;		// letting the property set build the mass matrix

            if (token == null)
            {
                this.Token = TokenGenerator.NextToken();
            }
            else
            {
                this.Token = token.Value;
            }

            ObjectStorage.Instance.AddBody(_handle, this);

            _world.BodyCreated(this);
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
            if (disposing)// && _handle != IntPtr.Zero)
            {
                if(_isDisposed)
                {
                    System.Diagnostics.Debug.WriteLine($"Dispose already disposed: {_handle}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Disposing: {_handle}");

                    _isDisposed = true;

                    if (Disposing != null)
                    {
                        Disposing(this, new EventArgs());
                    }

                    _world.BodyDisposed(this);      //NOTE: This just gives the .net world object a chance to unhook event listeners
                    Newton.NewtonDestroyBody(_worldHandle, _handle);
                    ObjectStorage.Instance.RemoveBody(_handle);
                    //_handle = IntPtr.Zero;
                }
            }
        }

        #endregion
        #region IComparable<Body> Members

        /// <summary>
        /// I wanted to be able to use bodies as keys in a sorted list
        /// </summary>
        public int CompareTo(Body other)
        {
            if (other == null)
            {
                // I'm greater than null
                return 1;
            }

            if (this.Token < other.Token)
            {
                return -1;
            }
            else if (this.Token > other.Token)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion
        #region IEquatable<Body> Members

        public bool Equals(Body other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return this.Token == other.Token;
            }
        }

        public override bool Equals(object obj)
        {
            Body other = obj as Body;
            if (other == null)
            {
                return false;
            }
            else
            {
                return this.Token == other.Token;
            }
        }

        public override int GetHashCode()
        {
            return this.Token.GetHashCode();
        }

        #endregion

        #region Public Properties

        private IntPtr _handle;
        public IntPtr Handle
        {
            get
            {
                return _handle;
            }
        }

        /// <summary>
        /// This is a unique ID given to each instance of the body
        /// </summary>
        public long Token
        {
            get;
            private set;
        }

        private volatile bool _isDisposed = false;
        public bool IsDisposed { get { return _isDisposed; } }      //auto implemented properties don't add a memory barrier, so need a volatile

        /// <summary>
        /// NOTE:  You can swap out hulls, but newton will dispose the old hull if nothing else is pointing to it.  Then ObjectStorage will be out of sync
        /// </summary>
        public CollisionHull CollisionHull
        {
            get
            {
                IntPtr hullHandle = Newton.NewtonBodyGetCollision(_handle);
                return ObjectStorage.Instance.GetCollisionHull(hullHandle);
            }
            set
            {
                Newton.NewtonBodySetCollision(_handle, value.Handle);
            }
        }

        /// <summary>
        /// This holds the current rotation/translation
        /// WARNING:  Don't use this for scaling
        /// NOTE:  If you just want to get the rotation portion of this matrix, use the Rotation property
        /// </summary>
        public Matrix3D OffsetMatrix
        {
            get
            {
                NewtonMatrix newtMatrix = new NewtonMatrix();
                Newton.NewtonBodyGetMatrix(_handle, newtMatrix.Matrix);
                return newtMatrix.ToWPF();
            }
            set
            {
                Newton.NewtonBodySetMatrix(_handle, new NewtonMatrix(value).Matrix);

                //TODO:  Figure out how to know what thread this is called from
                OnBodyMoved(new BodyMovedArgs(value, -1));
            }
        }

        /// <summary>
        /// If you change this (default is (0,0,0)), you'll probably want to change the mass matrix as well
        /// This is in local coords
        /// </summary>
        public Point3D CenterOfMass
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetCentreOfMass(_handle, retVal.Vector);
                return retVal.ToPointWPF();
            }
            set
            {
                Newton.NewtonBodySetCentreOfMass(_handle, new NewtonVector3(value).Vector);
            }
        }

        /// <summary>
        /// This is just a convenience
        /// This is in world coords
        /// </summary>
        public Point3D Position
        {
            get
            {
                return PositionToWorld(this.CenterOfMass);
            }
            set
            {
                // Creating a transform group that combines the current rotation with the new position
                Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate

                Quaternion rotation = this.Rotation;
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(rotation.Axis, rotation.Angle)));

                transform.Children.Add(new TranslateTransform3D(value.X, value.Y, value.Z));

                this.OffsetMatrix = transform.Value;
            }
        }
        /// <summary>
        /// This is here as a convenience, it is the rotation part of the offset matrix
        /// This is in world coords
        /// </summary>
        /// <remarks>
        /// From newton.cpp:
        /// 
        /// Get the rotation part of the transformation matrix of a body, in form of a unit quaternion.
        ///
        /// Parameters:
        /// *const NewtonBody* *bodyPtr - pointer to the body.
        /// *const dFloat* *rotPtr - pointer to an array of 4 floats that will hold the global rotation of the rigid body.
        ///
        /// Return: Nothing.
        ///
        /// Remarks: The rotation matrix is written set in the form of a unit quaternion in the format Rot (q0, q1, q1, q3)
        /// 
        /// Remarks: The rotation quaternion is the same as what the application would get by using at function to extract a quaternion form a matrix.
        /// however since the rigid body already contained the rotation in it, it is more efficient to just call this function avoiding expensive conversion. 
        ///
        /// Remarks: this function could be very useful for the implementation of pseudo frame rate independent simulation.
        /// by running the simulation at a fix rate and using linear interpolation between the last two simulation frames. 
        /// to determine the exact fraction of the render step.
        /// </remarks>
        public Quaternion Rotation
        {
            get
            {
                // From a forum:
                //q0 = cos(a/2)
                //q1 = sin(a/2) * x
                //q2 = sin(a/2) * y
                //q3 = sin(a/2) * z

                NewtonQuaternion retVal = new NewtonQuaternion();
                Newton.NewtonBodyGetRotation(_handle, retVal.Quaternion);
                return retVal.ToWPF();
            }
            set
            {
                // Creating a transform group that combines the current position with the new rotation
                Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(value.Axis, value.Angle)));
                transform.Children.Add(new TranslateTransform3D(this.Position.ToVector() - this.DirectionToWorld(this.CenterOfMass.ToVector())));

                this.OffsetMatrix = transform.Value;
            }
        }

        /// <summary>
        /// This is how newton knows about mass.  It is how much inertia the body has along the various axiis
        /// </summary>
        /// <remarks>
        /// Imagine a fighter plane.  It has a lot of mass, but it's concentrated along the tube of the plane, so it can do rolls very easily because the moment
        /// of inertia is small along the tube, but larger for pitch and yaw.
        /// 
        /// Look at this.Mass to see how a default mass matrix is built
        /// </remarks>
        public MassMatrix MassMatrix
        {
            get
            {
                float mass = 0f;
                float inertiaX = 0f;
                float inertiaY = 0f;
                float inertiaZ = 0f;

                Newton.NewtonBodyGetMassMatrix(_handle, ref mass, ref inertiaX, ref inertiaY, ref inertiaZ);

                return new MassMatrix(mass, new Vector3D(inertiaX, inertiaY, inertiaZ));
            }
            set
            {
                Newton.NewtonBodySetMassMatrix(_handle, Convert.ToSingle(value.Mass), Convert.ToSingle(value.Inertia.X), Convert.ToSingle(value.Inertia.Y), Convert.ToSingle(value.Inertia.Z));
            }
        }
        public MassMatrix InverseMassMatrix
        {
            get
            {
                float mass = 0f;
                float inertiaX = 0f;
                float inertiaY = 0f;
                float inertiaZ = 0f;

                Newton.NewtonBodyGetInvMass(_handle, ref mass, ref inertiaX, ref inertiaY, ref inertiaZ);

                return new MassMatrix(mass, new Vector3D(inertiaX, inertiaY, inertiaZ));
            }
        }

        /// <summary>
        /// This property is here as a convenience.  Newton works with mass matrix.  The property set will build a matrix based on the geometry of the
        /// hull (assuming uniform density?).  If you don't like the default, calculate the matrix directly
        /// </summary>
        public double Mass
        {
            get
            {
                return this.MassMatrix.Mass;
            }
            set
            {
                InertialMatrix inertiaMatrix = this.CollisionHull.CalculateInertialMatrix();
                MassMatrix massMatrix = new MassMatrix(value, inertiaMatrix.Inertia * value);
                this.MassMatrix = massMatrix;
            }
        }

        /// <summary>
        /// Materials are defined while the world is being created, and define collision props (friction, elasticity, etc)
        /// You can also call MaterialManager.RegisterCollisionEvent to listen to an event whenever two bodies of those materials collide
        /// </summary>
        public int MaterialGroupID
        {
            get
            {
                return Newton.NewtonBodyGetMaterialGroupID(_handle);
            }
            set
            {
                Newton.NewtonBodySetMaterialGroupID(_handle, value);
            }
        }

        /// <summary>
        /// Material also has this property.  I'm guessing this is an override?
        /// </summary>
        public bool IsContinuousCollision
        {
            get
            {
                int retVal = Newton.NewtonBodyGetContinuousCollisionMode(_handle);
                return retVal == 1 ? true : false;
            }
            set
            {
                Newton.NewtonBodySetContinuousCollisionMode(_handle, Convert.ToUInt32(value ? 1 : 0));
            }
        }

        /// <summary>
        /// Returns whether this body will collide with any of the bodies in a chain connected by joints/bodies
        /// </summary>
        /// <remarks>
        /// TODO:  See how this differs from JointBase.ShouldLinkedBodiesCollideEachOther
        /// TODO:  See if the set affects this body only, or all bodies in the chain
        /// 
        /// From the wiki:
        /// 
        /// sometimes when making complicated arrangements of linked bodies it is possible the collision geometry of these bodies is in the
        /// way of the joints work space. This could be a problem for the normal operation of the joints. When this situation happens the
        /// application can determine which bodies are the problem and disable collision for those bodies while they are linked by joints. For
        /// the collision to be disable for a pair of body, both bodies must have the collision disabled. If the joints connecting the bodies are
        /// destroyed these bodies become collidable automatically. This feature can also be achieved by making special material for the whole
        /// configuration of jointed bodies, however it is a lot easier just to set collision disable for jointed bodies. 
        /// </remarks>
        public bool IsCollidableWithBodiesConnectByJoints
        {
            get
            {
                int retVal = Newton.NewtonBodyGetJointRecursiveCollision(_handle);
                return retVal == 0 ? false : true;
            }
            set
            {
                Newton.NewtonBodySetJointRecursiveCollision(_handle, Convert.ToUInt32(value ? 1 : 0));
            }
        }

        /// <summary>
        /// World Coords.
        /// In 1.53, this function is only effective when called from inside the NewtonApplyForceAndTorque callback.  As of 2.00, it can be called anywhere.
        /// </summary>
        /// <remarks>
        /// What's your vector victor?
        /// </remarks>
        public Vector3D Velocity
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetVelocity(_handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
            set
            {
                Newton.NewtonBodySetVelocity(_handle, new NewtonVector3(value).Vector);
            }
        }

        /// <summary>
        /// This is in world coords
        /// In 1.53, this function is only effective when called from inside the NewtonApplyForceAndTorque callback.  As of 2.00, it can be called anywhere.
        /// </summary>
        public Vector3D AngularVelocity
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetOmega(_handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
            set
            {
                Newton.NewtonBodySetOmega(_handle, new NewtonVector3(value).Vector);
            }
        }

        /// <summary>
        /// NOTE:  This can only be used from within an ApplyForceAndTorque callback
        /// </summary>
        public Vector3D Force
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetForce(_handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
            set
            {
                Newton.NewtonBodySetForce(_handle, new NewtonVector3(value).Vector);
            }
        }
        /// <summary>
        /// NOTE:  This can only be used from within an ApplyForceAndTorque callback
        /// </summary>
        public Vector3D Torque
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetTorque(_handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
            set
            {
                Newton.NewtonBodySetTorque(_handle, new NewtonVector3(value).Vector);
            }
        }

        /// <summary>
        /// The force property only has meaning from within the ApplyForceAndTorque callback.  This is the force from the last time that was called
        /// </summary>
        public Vector3D ForceCached
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetForceAcc(_handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
        }
        /// <summary>
        /// The torque property only has meaning from within the ApplyForceAndTorque callback.  This is the torque from the last time that was called
        /// </summary>
        public Vector3D TorqueCached
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetTorqueAcc(_handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
        }

        /// <summary>
        /// Value from 0 to 1, default is .1
        /// </summary>
        /// <remarks>
        /// This acts as a simple viscocity (doesn't take shape into account)
        /// </remarks>
        public double LinearDamping
        {
            get
            {
                return Newton.NewtonBodyGetLinearDamping(_handle);
            }
            set
            {
                Newton.NewtonBodySetLinearDamping(_handle, Convert.ToSingle(value));
            }
        }
        /// <summary>
        /// Value from 0 to 1, default is .1
        /// </summary>
        /// <remarks>
        /// This acts as a simple viscocity (doesn't take shape into account)
        /// </remarks>
        public Vector3D AngularDamping
        {
            get
            {
                NewtonVector3 retVal = new NewtonVector3();
                Newton.NewtonBodyGetAngularDamping(_handle, retVal.Vector);
                return retVal.ToVectorWPF();
            }
            set
            {
                Newton.NewtonBodySetAngularDamping(_handle, new NewtonVector3(value).Vector);
            }
        }

        /// <summary>
        /// Default is true (a body will go to sleep when it's in equilibrium, and come out if there's a collision or forces/torques are applied)
        /// Not sure what happens if velocity or angular velocity are applied directly
        /// </summary>
        public bool AutoSleep
        {
            get
            {
                int retVal = Newton.NewtonBodyGetAutoSleep(_handle);
                return retVal == 1 ? true : false;
            }
            set
            {
                Newton.NewtonBodySetAutoSleep(_handle, value ? 1 : 0);
            }
        }

        public bool IsAsleep
        {
            get
            {
                int retVal = Newton.NewtonBodyGetSleepState(_handle);
                return retVal == 1 ? true : false;
            }
        }

        /// <summary>
        /// Sleep is just an optimization, the user shouldn't notice when something is asleep or not.  You can explicitly freeze something, and
        /// it will stay frozen until something collides with it (but will be unaffected by gravity) - at least that's my interpretation, may be wrong
        /// </summary>
        /// <remarks>
        /// From a forum:
        /// 
        /// Re: Newton 2 - Freeze & sleep
        /// by Julio Jerez » Tue Aug 26, 2008 4:29 pm
        /// 
        /// Oh I see.
        /// There are two flag in a rigid body.
        /// auto_sleep_flag and freeze_flag
        /// freeze_fag is controlled by the functions
        /// int NewtonBodyGetFreezeState(const NewtonBody* body);
        /// void NewtonBodySetFreezeState (const NewtonBody* body, int state); 
        /// the two function stop a body form being simulate even i fteh body has no reached the state of equilibrium.
        /// Say you want to place by box in mid air and you do not wan it to fall even if gravity is acting of it.
        /// For the you careate the bdy an dyou call 
        /// NewtonBodySetFreezeState(body, int state)
        /// the body will not move until eney of these evene happens:
        /// a collision, an impulse, of a call to function
        /// NewtonBodySetFreezeState (body, 0); 
        /// basically it is a one time flag, the is control by the application.
        /// must applications do not need use this flag if bodies are in equlibrium when they are created, 
        /// 
        /// auto_sleep_flag is controlled by the functions i scontrolled by 
        /// int NewtonBodyGetSleepState (const NewtonBody* body);
        /// int NewtonBodyGetAutoSleep (const NewtonBody* body);
        /// void NewtonBodySetAutoSleep (const NewtonBody* body, int state); 
        /// when auto_sleep_flag is one, a body will not be simulated if the body and all of the other bodies linked to that body by a joint are in equilibrium. If autosleo is zero the body will always simulated.
        /// You set auto_sleep_flag using NewtonBodySetAutoSleep (const NewtonBody* body, int state);
        /// 
        /// int NewtonBodyGetAutoSleep (const NewtonBody* body); 
        /// tell you if the body is sleeping. (meanning is in equlibrium and aouto sleep flag is one
        /// int NewtonBodyGetSleepState (const NewtonBody* body); 
        /// tell you if the flag autosleep is one zero
        /// </remarks>
        public bool IsFrozen
        {
            get
            {
                int retVal = Newton.NewtonBodyGetFreezeState(_handle);
                return retVal == 1 ? true : false;
            }
            set
            {
                Newton.NewtonBodySetFreezeState(_handle, value ? 1 : 0);
            }
        }

        /// <summary>
        /// These are wpf visuals for this body.  This doesn't affect newton at all, it's just a way to visualize the body (can be null)
        /// </summary>
        /// <remarks>
        /// Whenever OnBodyMoved is called, these visuals are synced to the body's offset matrix
        /// </remarks>
        public Visual3D[] Visuals
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The force needs to be in world coords
        /// NOTE:  This can only be called during an ApplyForceAndTorque event
        /// </summary>
        public void AddForce(Vector3D force)
        {
            Newton.NewtonBodyAddForce(_handle, new NewtonVector3(force).Vector);
        }
        /// <summary>
        /// The torque needs to be in world coords
        /// NOTE:  This can only be called during an ApplyForceAndTorque event
        /// </summary>
        public void AddTorque(Vector3D torque)
        {
            Newton.NewtonBodyAddTorque(_handle, new NewtonVector3(torque).Vector);
        }

        /// <summary>
        /// This is in world coords
        /// NOTE:  This can only be called during an ApplyForceAndTorque event
        /// </summary>
        public void AddForceAtPoint(Vector3D force, Point3D positionOnBody)
        {
            // I need to calculate this offset in world coords, because the force is in world coords
            Vector3D offsetFromMassWorld = positionOnBody - this.Position;

            Vector3D translationForce, torque;
            Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out torque, offsetFromMassWorld, force);

            AddForce(translationForce);
            AddTorque(torque);
        }
        /// <summary>
        /// This is in world coords
        /// I think it's ok to call this at any time
        /// </summary>
        /// <remarks>
        /// Be careful with this one, it's probably better to use forces
        /// </remarks>
        public void AddVelocityAtPoint(Vector3D deltaVelocity, Point3D positionOnBody)
        {
            Newton.NewtonBodyAddImpulse(_handle, new NewtonVector3(deltaVelocity).Vector, new NewtonVector3(positionOnBody).Vector);
        }

        /// <summary>
        /// Tells you how much force to apply to get the desired velocity (doesn't consider rotation)
        /// </summary>
        /// <remarks>
        /// From newton.cpp:
        /// 
        /// Calculate the next force that needs to be applied to the body to archive the desired velocity in the current time step.
        ///
        /// Parameters:
        /// *const NewtonBody* *bodyPtr - pointer to the body.
        /// *dFloat* timestep - time step that the force will be applyed. 
        /// *const dFloat* *desiredVeloc - pointer to an array of 3 floats containing the desired velocity.
        /// *dFloat* *forceOut - pointer to an array of 3 floats to hold the calculated net force.
        ///
        /// Remark: this function can be useful when creating object for game play.
        /// 
        /// remark: this treat the body as a point mass and is uses the solver to calculates the net force that need to be applied to the body
        /// such that is reach the desired velocity in the next time step.
        /// In general the force should be calculated by the expression f = M * (dsiredVeloc - bodyVeloc) / timestep
        /// however due to algorithmic optimization and limitations if such equation is used then the solver will generate a different desired velocity.
        /// </remarks>
        public Vector3D GetForceForDesiredVelocity(Vector3D desiredVelocity, double timestep)
        {
            NewtonVector3 retVal = new NewtonVector3();

            Newton.NewtonBodyCalculateInverseDynamicsForce(_handle, Convert.ToSingle(timestep), new NewtonVector3(desiredVelocity).Vector, retVal.Vector);

            return retVal.ToVectorWPF();
        }

        /// <summary>
        /// Same as the OffsetMatrix property, but this one does it for all connected bodies (like dragging a chain around from
        /// one of the links - physics aren't applied to the chain, it's a static move/rotate)
        /// </summary>
        /// <remarks>
        /// NOTE:  Velocity and Angular velocity of all related bodies will be zero after this
        /// NOTE:  Remove the chain from any static objects first, or bad things will happen
        /// </remarks>
        public void SetOffsetMatrixRecursive(Matrix3D offsetMatrix)
        {
            Newton.NewtonBodySetMatrixRecursive(_handle, new NewtonMatrix(offsetMatrix).Matrix);
        }

        /// <summary>
        /// This is the axis aligned box in world coords
        /// </summary>
        public void GetAABB(out Point3D minPoint, out Point3D maxPoint)
        {
            NewtonVector3 newtMin = new NewtonVector3();
            NewtonVector3 newtMax = new NewtonVector3();

            Newton.NewtonBodyGetAABB(_handle, newtMin.Vector, newtMax.Vector);

            minPoint = newtMin.ToPointWPF();
            maxPoint = newtMax.ToPointWPF();
        }

        /// <summary>
        /// Gets the joints that are attached to this body (returns zero length array if there are none)
        /// WARNING:  This method doesn't seem to work
        /// </summary>
        public JointBase[] GetJoints()
        {
            List<JointBase> retVal = new List<JointBase>();

            IntPtr jointHandle = Newton.NewtonBodyGetFirstJoint(_handle);
            while (jointHandle != IntPtr.Zero)
            {
                retVal.Add(ObjectStorage.Instance.GetJoint(jointHandle));

                jointHandle = Newton.NewtonBodyGetNextJoint(_handle, jointHandle);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This returns the velocity that the body is moving at the point - takes angular velocity into consideration
        /// (point passed in and velocity returned are in world coords)
        /// </summary>
        public Vector3D GetVelocityAtPoint(Point3D point)
        {
            Vector3D angularVelocity = this.AngularVelocity;

            if (Math3D.IsNearZero(angularVelocity))
            {
                return this.Velocity;
            }
            else
            {
                Vector3D offsetLocal = this.PositionFromWorld(point) - this.CenterOfMass;

                Vector3D velocityRotation = Vector3D.CrossProduct(this.DirectionFromWorld(angularVelocity), offsetLocal);
                velocityRotation = this.DirectionToWorld(velocityRotation);

                return velocityRotation + this.Velocity;
            }
        }
        public Vector3D GetVelocityAtPoint_OLD(Point3D point)
        {
            // For some reason, even though the angular velocity and position are translated to model
            // coords, the outcome is in world coords
            Vector3D retVal = Vector3D.CrossProduct(this.DirectionFromWorld(this.AngularVelocity), point - this.Position);

            // Tack on the linear velocity
            return retVal + this.Velocity;
        }

        // These are math utility methods that transform points/directions using the body's offset matrix
        /// <summary>
        /// This is a helper method that transforms the position in local coords to the position in world coords
        /// </summary>
        public Vector3D PositionToWorld(Vector3D positionLocal)
        {
            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return this.OffsetMatrix.Transform(positionLocal.ToPoint()).ToVector();
        }
        public Point3D PositionToWorld(Point3D positionLocal)
        {
            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return this.OffsetMatrix.Transform(positionLocal);
        }

        public Vector3D PositionFromWorld(Vector3D positionWorld)
        {
            Matrix3D matrix = this.OffsetMatrix;
            matrix.Invert();

            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return matrix.Transform(positionWorld.ToPoint()).ToVector();
        }
        public Point3D PositionFromWorld(Point3D positionWorld)
        {
            Matrix3D matrix = this.OffsetMatrix;
            matrix.Invert();

            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return matrix.Transform(positionWorld);
        }

        /// <summary>
        /// This is a helper method that transforms the direction in local coords to the direction in world coords
        /// </summary>
        public Vector3D DirectionToWorld(Vector3D directionLocal)
        {
            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return this.OffsetMatrix.Transform(directionLocal);
        }
        public Point3D DirectionToWorld(Point3D directionLocal)
        {
            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return this.OffsetMatrix.Transform(directionLocal.ToVector()).ToPoint();
        }

        public Vector3D DirectionFromWorld(Vector3D directionWorld)
        {
            Matrix3D matrix = this.OffsetMatrix;
            matrix.Invert();

            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return matrix.Transform(directionWorld);
        }
        public Point3D DirectionFromWorld(Point3D directionWorld)
        {
            Matrix3D matrix = this.OffsetMatrix;
            matrix.Invert();

            //NOTE:  This transform method acts different based on whether a vector or point is passed in
            return matrix.Transform(directionWorld.ToVector()).ToPoint();
        }

        #endregion

        //TODO:  Figure out what a contact joint is, how is it different than a regular joint?
        //    NEWTON_API NewtonJoint* NewtonBodyGetFirstContactJoint (const NewtonBody* const body);
        //    NEWTON_API NewtonJoint* NewtonBodyGetNextContactJoint (const NewtonBody* const body, const NewtonJoint* const contactJoint);

        //    NEWTON_API void* NewtonContactJointGetFirstContact (const NewtonJoint* const contactJoint);
        //    NEWTON_API void* NewtonContactJointGetNextContact (const NewtonJoint* const contactJoint, void* const contact);

        //    NEWTON_API int NewtonContactJointGetContactCount(const NewtonJoint* const contactJoint);
        //    NEWTON_API void NewtonContactJointRemoveContact(const NewtonJoint* const contactJoint, void* const contact); 

        //TODO:  Implement this when needed, and when I figure out how to implement without taking an event (the 1.53 wrapper code
        // looks promising, but it's late and I'm tired)
        //public void AddBuoyancyForce(double fluidDensity, double fluidLinearViscosity, double fluidAngularViscosity, Vector3D gravity)
        //{
        //    // Why is the buoyancy plane a callback?  Why can't I just pass in a plane? - the callback is asking the collision hull, context seems to be a pointer for that callback's convenience
        //    Newton.NewtonBodyAddBuoyancyForce(_handle, Convert.ToSingle(fluidDensity), Convert.ToSingle( fluidLinearViscosity), Convert.ToSingle( fluidAngularViscosity), new NewtonVector3(gravity).Vector, NewtonGetBuoyancyPlane buoyancyPlane, void* const context);
        //}

        //TODO:  Implement when needed (it's a disposing event)
        //    NEWTON_API void  NewtonBodySetDestructorCallback (const NewtonBody* const body, NewtonBodyDestructor callback);

        //TODO:  Implement this if there is a need
        //    NEWTON_API NewtonApplyForceAndTorque NewtonBodyGetForceAndTorqueCallback (const NewtonBody* const body);

        //TODO:  Implement this if there's a need
        //    NEWTON_API NewtonSetTransform NewtonBodyGetTransformCallback (const NewtonBody* const body);

        //TODO:  Support UserData (maybe a special secion in ObjectStorage)
        //    NEWTON_API void* NewtonBodyGetUserData (const NewtonBody* const body);
        //    NEWTON_API void  NewtonBodySetUserData (const NewtonBody* const body, void* const userData);

        //TODO:  Add this if I need it for debugging
        //    NEWTON_API NewtonWorld* NewtonBodyGetWorld (const NewtonBody* const body);
    }

    #region struct: MassMatrix

    public struct MassMatrix
    {
        public double Mass;
        public Vector3D Inertia;

        public MassMatrix(double mass, Vector3D inertia)
        {
            this.Mass = mass;
            this.Inertia = inertia;
        }
    }

    #endregion

    #region class: BodyMovedArgs

    public class BodyMovedArgs : EventArgs
    {
        public BodyMovedArgs(Matrix3D offsetMatrix, int threadIndex)
        {
            this.OffsetMatrix = offsetMatrix;
            this.ThreadIndex = threadIndex;
        }

        //NOTE:  By the time the event is raised, this offset matrix is the same as the body's offset matrix
        public Matrix3D OffsetMatrix
        {
            get;
            private set;
        }

        public int ThreadIndex
        {
            get;
            private set;
        }
    }

    #endregion
    #region class: BodyApplyForceAndTorqueArgs

    public class BodyApplyForceAndTorqueArgs : EventArgs
    {
        public BodyApplyForceAndTorqueArgs(Body body, double timestep, int threadIndex)
        {
            this.Body = body;
            this.Timestep = timestep;
            this.ThreadIndex = threadIndex;
        }

        public Body Body
        {
            get;
            private set;
        }
        public double Timestep
        {
            get;
            private set;
        }
        public int ThreadIndex
        {
            get;
            private set;
        }
    }

    #endregion
}
