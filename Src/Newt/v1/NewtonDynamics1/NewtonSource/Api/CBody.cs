using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
    public partial class CBody : IDisposable
    {
        #region Members

        private IntPtr m_Handle;
        internal IntPtr Handle { get { return m_Handle; } }

        private CCollision m_Collision;

        private EventHandler<CBuoyancyPlaneEventArgs> m_GetBuoyancyPlane;
        private Newton.NewtonGetBuoyancyPlane m_NewtonGetBuoyancyPlane;

        private EventHandler<CSetTransformEventArgs> m_SetTransform;
        private Newton.NewtonSetTransform m_NewtonSetTransform;

        private EventHandler<CBodyDestructorEventArgs> m_BodyDestructor;
        private Newton.NewtonBodyDestructor m_NewtonBodyDestructor;

        private EventHandler<CBodyActivationStateEventArgs> m_ActivationState;
        private Newton.NewtonBodyActivationState m_NewtonBodyActivationState;

        private EventHandler<CApplyForceAndTorqueEventArgs> m_ApplyForceAndTorque;
        private Newton.NewtonApplyForceAndTorque m_NewtonApplyForceAndTorque;

        private EventHandler<CCollisionIteratorEventArgs> m_CollisionIterator;
        private Newton.NewtonCollisionIterator m_NewtonCollisionIterator;

        #endregion

        #region Constructor

        public CBody(CCollision pCollision)
        {
            if (pCollision == null) throw new ArgumentNullException("pCollision");

            m_Collision = pCollision;
            m_Handle = Newton.NewtonCreateBody(m_Collision.World.Handle, m_Collision.Handle);

            CHashTables.Body.Add(m_Handle, this);
        }

        ~CBody()
        {
            Dispose(false);
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
                if (m_Handle != IntPtr.Zero)
                {
                    CHashTables.Body.Remove(m_Handle);
                    CHashTables.BodyUserData.Remove(m_Handle);
                    Newton.NewtonDestroyBody(m_Collision.World.Handle, m_Handle);
                    m_Handle = IntPtr.Zero;
                }
            }
        }

        #endregion

        #region Methods

        public void Freeze()
        {
            Collision.World.FreezeBody(this);
        }

        public void Unfreeze()
        {
            Collision.World.UnfreezeBody(this);
        }

        /// <summary>
        /// This wants the force in world coords
        /// </summary>
        public void AddForce(Vector3D pForce)
        {
            NewtonVector3 aForce = new NewtonVector3(pForce);
            Newton.NewtonBodyAddForce(m_Handle, aForce.NWVector3);
        }

        /// <summary>
        /// This appears to want the torque in world coords
        /// </summary>
        public void AddTorque(Vector3D pTorque)
        {
            NewtonVector3 aTorque = new NewtonVector3(pTorque);
            Newton.NewtonBodyAddTorque(m_Handle, aTorque.NWVector3);
        }

        /// <summary>
        /// This wants world coords
        /// </summary>
        public void AddImpulse(Vector3D pPointDeltaVelocity, Vector3D pPointPosition)
        {
            Newton.NewtonAddBodyImpulse(m_Handle, new NewtonVector3(pPointDeltaVelocity).NWVector3, new NewtonVector3(pPointPosition).NWVector3);
        }

        public void CoriolisForcesMode(bool pMode)
        {
            Newton.NewtonBodyCoriolisForcesMode(m_Handle, Convert.ToInt32(pMode));
        }

        public void AddBuoyancyForce(float pFluidDensity, float pFluidLinearViscosity, float pFluidAngularViscosity, Vector3D pGravityVector, EventHandler<CBuoyancyPlaneEventArgs> pBuoyancyPlane, int pContext)
        {
            m_GetBuoyancyPlane = pBuoyancyPlane;
            m_NewtonGetBuoyancyPlane = new Newton.NewtonGetBuoyancyPlane(InvokeAddBuoyancyForce);

            Newton.NewtonBodyAddBuoyancyForce(m_Handle, pFluidDensity, pFluidLinearViscosity, pFluidAngularViscosity, new NewtonVector3(pGravityVector).NWVector3, m_NewtonGetBuoyancyPlane, pContext);
        }

        #endregion

        #region Properties

        public Matrix3D Matrix
        {
            get
            {
                NewtonMatrix aMat = new NewtonMatrix(Matrix3D.Identity);
                if (m_Handle != (IntPtr)0) Newton.NewtonBodyGetMatrix(m_Handle, aMat.NWMatrix);
                return aMat.ToDirectX();
            }
            set
            {
                NewtonMatrix aMat = new NewtonMatrix(value);
                if (m_Handle != (IntPtr)0) Newton.NewtonBodySetMatrix(m_Handle, aMat.NWMatrix);
            }
        }

        /// <summary>
        /// This one should be a method, because it's special use
        /// </summary>
        public Matrix3D MatrixRecursive
        {
            set
            {
                NewtonMatrix aMat = new NewtonMatrix(value);
                Newton.NewtonBodySetMatrixRecursive(m_Handle, aMat.NWMatrix);
            }
        }

        public MassMatrix MassMatrix
        {
            get
            {
                float aMass = 0.0f;
                float aIX = 0.0f;
                float aIY = 0.0f;
                float aIZ = 0.0f;
                Newton.NewtonBodyGetMassMatrix(m_Handle,
                    ref aMass,
                    ref aIX, ref aIY, ref aIZ);
                return new MassMatrix(aMass, new Vector3D(aIX, aIY, aIZ)); ;
            }
            set
            {
                Newton.NewtonBodySetMassMatrix(m_Handle, value.m_Mass, (float)value.m_I.X, (float)value.m_I.Y, (float)value.m_I.Z);
            }
        }
        public MassMatrix InvMassMatrix
        {
            get
            {
                float aMass = 0.0f;
                float aIX = 0.0f;
                float aIY = 0.0f;
                float aIZ = 0.0f;
                Newton.NewtonBodyGetInvMass(m_Handle,
                    ref aMass,
                    ref aIX,
                    ref aIY,
                    ref aIZ);

                MassMatrix aMassMat = new MassMatrix(aMass,
                    new Vector3D(aIX, aIY, aIZ));
                return aMassMat;
            }
        }

        /// <summary>
        /// Materials are defined while the world is being created, and define collision props (friction, elasticity, etc)
        /// You can also listen to the material for events when a collision occurs
        /// </summary>
        public int MaterialGroupID
        {
            get
            {
                return (int)Newton.NewtonBodyGetMaterialGroupID(m_Handle);
            }
            set
            {
                Newton.NewtonBodySetMaterialGroupID(m_Handle, (int)value);
            }
        }

        public uint ContinuousCollisionMode
        {
            get
            {
                return (uint)Newton.NewtonBodyGetContinuousCollisionMode(m_Handle);
            }
            set
            {
                Newton.NewtonBodySetContinuousCollisionMode(m_Handle, value);
            }
        }

        public uint JointRecursiveCollision
        {
            get
            {
                return (uint)Newton.NewtonBodyGetJointRecursiveCollision(m_Handle);
            }
            set
            {
                Newton.NewtonBodySetJointRecursiveCollision(m_Handle, value);
            }
        }

        public Vector3D Omega
        {
            get
            {
                NewtonVector3 aVector = new NewtonVector3(new Vector3D());
                Newton.NewtonBodyGetOmega(m_Handle, aVector.NWVector3);
                return aVector.ToDirectX();
            }
            set
            {
                NewtonVector3 aVector = new NewtonVector3(value);
                Newton.NewtonBodySetOmega(m_Handle, aVector.NWVector3);
            }
        }

        /// <summary>
        /// This is in world coords - only valid when called from within ApplyForceAndTorque event.  Newt 2 lets it get called any time
        /// </summary>
        public Vector3D Velocity
        {
            get
            {
                NewtonVector3 aVector = new NewtonVector3(new Vector3D());
                Newton.NewtonBodyGetVelocity(m_Handle, aVector.NWVector3);
                return aVector.ToDirectX();
            }
            set
            {
                NewtonVector3 aVector = new NewtonVector3(value);
                Newton.NewtonBodySetVelocity(m_Handle, aVector.NWVector3);
            }
        }

        // these two can only be used from the ApplyForceAndTorque callback
        public Vector3D Force
        {
            get
            {
                NewtonVector3 aVector = new NewtonVector3(new Vector3D());
                Newton.NewtonBodyGetForce(m_Handle, aVector.NWVector3);
                return aVector.ToDirectX();
            }
            set
            {
                NewtonVector3 aVector = new NewtonVector3(value);
                Newton.NewtonBodySetForce(m_Handle, aVector.NWVector3);
            }
        }
        public Vector3D Torque
        {
            get
            {
                NewtonVector3 aVector = new NewtonVector3(new Vector3D());
                Newton.NewtonBodyGetTorque(m_Handle, aVector.NWVector3);
                return aVector.ToDirectX();
            }
            set
            {
                NewtonVector3 aVector = new NewtonVector3(value);
                Newton.NewtonBodySetTorque(m_Handle, aVector.NWVector3);
            }
        }

        public Vector3D CentreOfMass
        {
            get
            {
                NewtonVector3 aVector = new NewtonVector3(new Vector3D());
                Newton.NewtonBodyGetCentreOfMass(m_Handle, aVector.NWVector3);
                return aVector.ToDirectX();
            }
            set
            {
                NewtonVector3 aVector = new NewtonVector3(value);
                Newton.NewtonBodySetCentreOfMass(m_Handle, aVector.NWVector3);
            }
        }

        public float LinearDamping
        {
            get
            {
                return Newton.NewtonBodyGetLinearDamping(m_Handle);
            }
            set
            {
                Newton.NewtonBodySetLinearDamping(m_Handle, value);
            }
        }

        public Vector3D AngularDamping
        {
            get
            {
                NewtonVector3 aVector = new NewtonVector3(new Vector3D());
                Newton.NewtonBodyGetAngularDamping(m_Handle, ref aVector.NWVector3);
                return aVector.ToDirectX();
            }
            set
            {
                NewtonVector3 aVector = new NewtonVector3(value);
                Newton.NewtonBodySetAngularDamping(m_Handle, aVector.NWVector3);
            }
        }

        public object UserData
        {
            get
            {
                return CHashTables.BodyUserData[m_Handle];
            }
            set
            {
                if (CHashTables.BodyUserData[m_Handle] != null)
                {
                    CHashTables.BodyUserData[m_Handle] = value;
                }
                else
                {
                    CHashTables.BodyUserData.Add(m_Handle, value);
                    Newton.NewtonBodySetUserData(m_Handle, m_Handle);
                }
            }
        }

        public CCollision Collision
        {
            get
            {
                //if(m_CollisionMask.Handle == Newton.NewtonBodyGetCollision(m_Handle))
                //{
                return m_Collision;
                //}
                //return null;
            }
            set
            {
                m_Collision = value;
                Newton.NewtonBodySetCollision(m_Handle, value.Handle);
            }
        }

        public CWorld World
        {
            get
            {
                if (m_Collision.World.Handle == Newton.NewtonBodyGetWorld(m_Handle))
                {
                    return m_Collision.World;
                }
                return null;
            }
        }

        public bool AutoFreeze
        {
            get
            {
                return Convert.ToBoolean(Newton.NewtonBodyGetAutoFreeze(m_Handle));
            }
            set
            {
                Newton.NewtonBodySetAutoFreeze(m_Handle, Convert.ToInt32(value));
            }
        }

        public FreezeTreshold FreezeTreshold
        {
            get
            {
                float aSpeed = 0.0f;
                float aOmega = 0.0f;
                int aFramesCount = 0;
                Newton.NewtonBodyGetFreezeTreshold(m_Handle,
                    ref aSpeed, ref aOmega);
                return new FreezeTreshold(aSpeed, aOmega, aFramesCount);
            }
            set
            {
                Newton.NewtonBodySetFreezeTreshold(m_Handle,
                    value.m_Speed,
                    value.m_Omega,
                    value.m_FramesCount);
            }
        }

        public bool SleepingState
        {
            get
            {
                return Convert.ToBoolean(Newton.NewtonBodyGetSleepingState(m_Handle));
            }
        }

        public AxisAlignedboundingBox AxisAlignedboundingBox
        {
            get
            {
                NewtonVector3 aMin = new NewtonVector3(new Vector3D());
                NewtonVector3 aMax = new NewtonVector3(new Vector3D());
                Newton.NewtonBodyGetAABB(m_Handle, ref aMin.NWVector3, ref aMax.NWVector3);
                return new AxisAlignedboundingBox(aMin.ToDirectX(), aMax.ToDirectX());
            }
        }

        #endregion

        #region Events

        public event EventHandler<CSetTransformEventArgs> SetTransform
        {
            add
            {
                if (m_SetTransform == null)
                {
                    m_NewtonSetTransform = new Newton.NewtonSetTransform(InvokeSetTransform);
                    Newton.NewtonBodySetTransformCallback(m_Handle, m_NewtonSetTransform);
                }

                m_SetTransform += value;
            }

            remove
            {
                m_SetTransform -= value;

                if (m_SetTransform != null)
                {
                    m_NewtonSetTransform = null;
                    Newton.NewtonBodySetTransformCallback(m_Handle, m_NewtonSetTransform);
                }
            }
        }

        public event EventHandler<CBodyDestructorEventArgs> Destructor
        {
            add
            {
                if (m_BodyDestructor == null)
                {
                    m_NewtonBodyDestructor = new Newton.NewtonBodyDestructor(InvokeDestructor);
                    Newton.NewtonBodySetDestructorCallback(m_Handle, m_NewtonBodyDestructor);
                }

                m_BodyDestructor += value;
            }

            remove
            {
                m_BodyDestructor -= value;

                if (m_BodyDestructor == null)
                {
                    m_NewtonBodyDestructor = null;
                    if (m_Handle != IntPtr.Zero)
                        Newton.NewtonBodySetDestructorCallback(m_Handle, m_NewtonBodyDestructor);
                }
            }
        }

        public event EventHandler<CBodyActivationStateEventArgs> ActivationState
        {
            add
            {
                if (m_ActivationState == null)
                {
                    m_NewtonBodyActivationState = new Newton.NewtonBodyActivationState(InvokeActivationState);
                    Newton.NewtonBodySetAutoactiveCallback(m_Handle, m_NewtonBodyActivationState);
                }

                m_ActivationState += value;
            }

            remove
            {
                m_ActivationState -= value;

                if (m_ActivationState == null)
                {
                    m_NewtonBodyActivationState = null;
                    Newton.NewtonBodySetAutoactiveCallback(m_Handle, m_NewtonBodyActivationState);
                }
            }
        }

        public event EventHandler<CApplyForceAndTorqueEventArgs> ApplyForceAndTorque
        {
            add
            {
                if (m_ApplyForceAndTorque == null)
                {
                    m_NewtonApplyForceAndTorque = new Newton.NewtonApplyForceAndTorque(InvokeApplyForceAndTorque);
                    Newton.NewtonBodySetForceAndTorqueCallback(m_Handle, m_NewtonApplyForceAndTorque);
                }

                m_ApplyForceAndTorque += value;
            }

            remove
            {
                m_ApplyForceAndTorque -= value;

                if (m_ApplyForceAndTorque == null)
                {
                    m_NewtonApplyForceAndTorque = null;
                    Newton.NewtonBodySetForceAndTorqueCallback(m_Handle, m_NewtonApplyForceAndTorque);
                }
            }
        }

        public event EventHandler<CCollisionIteratorEventArgs> CollisionIterator
        {
            add
            {
                if (m_CollisionIterator == null)
                {
                    m_NewtonCollisionIterator = new Newton.NewtonCollisionIterator(InvokeCollisionIterator);
                    Newton.NewtonBodyForEachPolygonDo(m_Handle, m_NewtonCollisionIterator);
                }

                m_CollisionIterator += value;
            }

            remove
            {
                m_CollisionIterator -= value;

                if (m_CollisionIterator == null)
                {
                    m_NewtonCollisionIterator = null;
                    Newton.NewtonBodyForEachPolygonDo(m_Handle, m_NewtonCollisionIterator);
                }
            }
        }

        #endregion

        #region Invokes

        private void InvokeSetTransform(IntPtr pNewtonBody, float[] pMatrix)
        {
            NewtonMatrix aMatrix = new NewtonMatrix(pMatrix);
            OnSetTransform(new CSetTransformEventArgs(aMatrix.ToDirectX()));
        }

        private void InvokeDestructor(IntPtr pNewtonBody)
        {
            OnDestructor(new CBodyDestructorEventArgs());
        }

        private void InvokeActivationState(IntPtr pNewtonBody, uint state)
        {
            OnActivationState(new CBodyActivationStateEventArgs(state));
        }

        private void InvokeApplyForceAndTorque(IntPtr pNewtonBody)
        {
            OnApplyForceAndTorque(new CApplyForceAndTorqueEventArgs());
        }

        private void InvokeCollisionIterator(IntPtr pNewtonBody, int pVertexCount, float[] pFaceArray, int pFaceId)
        {
            OnCollisionIterator(new CCollisionIteratorEventArgs(pVertexCount,
                pFaceArray,
                pFaceId));
        }

        private int InvokeAddBuoyancyForce(int pCollisionID,
            IntPtr pContext,
            float[] pGlobalSpaceMatrix,
            float[] pGlobalSpacePlane)
        {
            OnAddBuoyancyForce(new CBuoyancyPlaneEventArgs(pContext,
                new NewtonMatrix(pGlobalSpaceMatrix).ToDirectX(),
                new NewtonVector4(pGlobalSpaceMatrix).ToDirectX()));

            return 1;
        }

        #endregion

        #region Virtuals

        protected virtual void OnSetTransform(CSetTransformEventArgs pEventArgs)
        {
            if (m_SetTransform != null)
            {
                m_SetTransform(this, pEventArgs);
            }
        }

        protected virtual void OnDestructor(CBodyDestructorEventArgs pEventArgs)
        {
            if (m_BodyDestructor != null)
            {
                m_BodyDestructor(this, pEventArgs);
            }
        }

        protected virtual void OnActivationState(CBodyActivationStateEventArgs pEventArgs)
        {
            if (m_ActivationState != null)
            {
                m_ActivationState(this, pEventArgs);
            }
        }

        protected virtual void OnApplyForceAndTorque(CApplyForceAndTorqueEventArgs pEventArgs)
        {
            if (m_ApplyForceAndTorque != null)
            {
                m_ApplyForceAndTorque(this, pEventArgs);
            }
        }

        protected virtual void OnCollisionIterator(CCollisionIteratorEventArgs pEventArgs)
        {
            if (m_CollisionIterator != null)
            {
                m_CollisionIterator(this, pEventArgs);
            }
        }

        protected virtual void OnAddBuoyancyForce(CBuoyancyPlaneEventArgs pEventArgs)
        {
            if (m_GetBuoyancyPlane != null)
            {
                m_GetBuoyancyPlane(this, pEventArgs);
            }
        }

        #endregion
    }
}