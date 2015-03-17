using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
    public class CWorld : IDisposable
    {
        #region Members

        protected IntPtr m_Handle;
        public IntPtr Handle { get { return m_Handle; } }

        private EventHandler<CBodyLeaveWorldEventArgs> m_BodyLeaveWorld;
        private Newton.NewtonBodyLeaveWorld m_NewtonBodyLeaveWorld;

        private EventHandler<CBodyIteratorEventArgs> m_BodyIterator;
        private Newton.NewtonBodyIterator m_NewtonBodyIterator;

        private EventHandler<CWorldRayPreFilterEventArgs> m_WorldRayPrefilter;
        private Newton.NewtonWorldRayPrefilterCallback m_NewtonWorldRayPrefilter;

        private EventHandler<CWorldRayFilterEventArgs> m_WorldRayFilter;
        private Newton.NewtonWorldRayFilterCallback m_NewtonWorldRayFilter;

        /// <summary>
        /// Pause
        /// </summary>
        protected bool m_Pause = false;
        public bool Pause
        {
            get { return m_Pause; }
            set { m_Pause = value; }
        }

        #endregion

        #region Constructor

        public CWorld()
        {
            m_Handle = Newton.NewtonCreate(null, null);
        }

        ~CWorld()
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
                //BUG: this will dispose ALL world's bodies.
                CJoint[] joints = new CJoint[CHashTables.Joint.Count];
                CHashTables.Joint.Values.CopyTo(joints, 0);

                foreach (CJoint joint in joints)
                    joint.Dispose();

                CBody[] bodies = new CBody[CHashTables.Body.Count];
                CHashTables.Body.Values.CopyTo(bodies, 0);

                foreach (CBody body in bodies)
                    body.Dispose();

                if (m_Handle != IntPtr.Zero)
                {
                    Newton.NewtonDestroy(m_Handle);
                    m_Handle = IntPtr.Zero;
                }

                // Stuff was left behind, so I'm clearing everything (there's likely only one world anyway)
                CHashTables.Clear();
            }
        }

        #endregion

        #region Methods

        public void DestroyAllBodies()
        {
            Newton.NewtonDestroyAllBodies(m_Handle);
        }

        public void Update(float pTimeStep)
        {
            if (m_Handle != IntPtr.Zero)
            {
                Newton.NewtonUpdate(m_Handle, pTimeStep);
            }
        }

        public void SetPlatformArchitecture(EPlatformArchitecture pMode)
        {
            Newton.NewtonSetPlatformArchitecture(m_Handle, (int)pMode);
        }

        public void SetSolverModel(SolverModel pModel)
        {
            Newton.NewtonSetSolverModel(m_Handle, (int)pModel);
        }

        public void SetFrictionModel(FrictionModel pModel)
        {
            Newton.NewtonSetFrictionModel(m_Handle, (int)pModel);
        }

        public void FreezeBody(CBody pBody)
        {
            Newton.NewtonWorldFreezeBody(m_Handle, pBody.Handle);
        }

        public void UnfreezeBody(CBody pBody)
        {
            Newton.NewtonWorldUnfreezeBody(m_Handle, pBody.Handle);
        }

        public void SetSize(Vector3D pMinPoint, Vector3D pMaxPoint)
        {
            Newton.NewtonSetWorldSize(m_Handle,
                new NewtonVector3(pMinPoint).NWVector3,
                new NewtonVector3(pMaxPoint).NWVector3);
        }

        public void WorldRayCast(Vector3D origin, Vector3D endPoint, EventHandler<CWorldRayFilterEventArgs> pWorldRayFilter, object userData, EventHandler<CWorldRayPreFilterEventArgs> pWorldRayPrefilter)
        {
            if (pWorldRayFilter != null)
            {
                m_WorldRayFilter = new EventHandler<CWorldRayFilterEventArgs>(pWorldRayFilter);
                m_NewtonWorldRayFilter = new Newton.NewtonWorldRayFilterCallback(InvokeWorldRayFilter);
            }
            else
                m_NewtonWorldRayFilter = null;

            if (pWorldRayPrefilter != null)
            {
                m_WorldRayPrefilter = new EventHandler<CWorldRayPreFilterEventArgs>(pWorldRayPrefilter);
                m_NewtonWorldRayPrefilter = new Newton.NewtonWorldRayPrefilterCallback(InvokeWorldRayPrefilter);
            }
            else
                m_NewtonWorldRayPrefilter = null;

            Newton.NewtonWorldRayCast(m_Handle,
                new NewtonVector3(origin).NWVector3,
                new NewtonVector3(endPoint).NWVector3,
                m_NewtonWorldRayFilter,
                (IntPtr)(userData ?? IntPtr.Zero),
                m_NewtonWorldRayPrefilter);
        }

        #endregion

        #region Properties

        public int Version
        {
            get
            {
                return Newton.NewtonWorldGetVersion(m_Handle);
            }
        }

        public float TimeStep
        {
            get
            {
                return Newton.NewtonGetTimeStep(m_Handle);
            }
        }

        public float MinimumFrameRate
        {
            set
            {
                Newton.NewtonSetMinimumFrameRate(m_Handle, value);
            }
        }

        public IntPtr UserData
        {
            get
            {
                return Newton.NewtonWorldGetUserData(m_Handle);
            }
            set
            {
                Newton.NewtonWorldSetUserData(m_Handle, value);
            }
        }

        public int BodiesCount
        {
            get
            {
                return Newton.NewtonGetBodiesCount();
            }
        }

        // Returns the list of known bodies
        public CBody[] Bodies
        {
            get
            {
                CBody[] retVal = new CBody[CHashTables.Body.Count];
                CHashTables.Body.Values.CopyTo(retVal, 0);

                return retVal;
            }
        }

        public int ActiveBodiesCount
        {
            get
            {
                return Newton.NewtonGetActiveBodiesCount();
            }
        }

        public int ActiveConstraintsCount
        {
            get
            {
                return Newton.NewtonGetActiveConstraintsCount();
            }
        }

        public float GlobalScale
        {
            get
            {
                return Newton.NewtonGetGlobalScale(m_Handle);
            }
        }

        #endregion

        #region Events

        public event EventHandler<CBodyLeaveWorldEventArgs> BodyLeaveWorld
        {
            add
            {
                if (m_BodyLeaveWorld == null)
                {
                    m_NewtonBodyLeaveWorld = new Newton.NewtonBodyLeaveWorld(InvokeBodyLeaveWorld);
                    Newton.NewtonSetBodyLeaveWorldEvent(m_Handle, m_NewtonBodyLeaveWorld);
                }

                m_BodyLeaveWorld += value;
            }

            remove
            {
                m_BodyLeaveWorld -= value;

                if (m_BodyLeaveWorld == null)
                {
                    m_NewtonBodyLeaveWorld = null;
                    Newton.NewtonSetBodyLeaveWorldEvent(m_Handle, m_NewtonBodyLeaveWorld);
                }
            }
        }

        public event EventHandler<CBodyIteratorEventArgs> BodyIterator
        {
            add
            {
                if (m_BodyIterator == null)
                {
                    m_NewtonBodyIterator = new Newton.NewtonBodyIterator(InvokeBodyIterator);
                    Newton.NewtonWorldForEachBodyDo(m_Handle, m_NewtonBodyIterator);
                }

                m_BodyIterator += value;
            }

            remove
            {
                m_BodyIterator -= value;

                if (m_BodyIterator == null)
                {
                    m_NewtonBodyIterator = null;
                    Newton.NewtonWorldForEachBodyDo(m_Handle, m_NewtonBodyIterator);
                }
            }
        }

        #endregion

        #region Invokes

        private void InvokeBodyLeaveWorld(IntPtr pNewtonBody)
        {
            OnBodyLeaveWorld(new CBodyLeaveWorldEventArgs((CBody)CHashTables.Body[pNewtonBody]));
        }

        private void InvokeBodyIterator(IntPtr pNewtonBody)
        {
            OnBodyIterator(new CBodyIteratorEventArgs((CBody)CHashTables.Body[pNewtonBody]));
        }

        private uint InvokeWorldRayPrefilter(IntPtr pNewtonBody,
                IntPtr pUserData,
                IntPtr pNewtonCollision)
        {
            var args = new CWorldRayPreFilterEventArgs((CBody)CHashTables.Body[pNewtonBody],
                                                       CHashTables.BodyUserData[pNewtonBody],
                                                       (CCollision)CHashTables.Collision[pNewtonCollision]);
            OnWorldRayPrefilter(args);
            return args.Skip ? (uint)0 : (uint)1;
        }

        private float InvokeWorldRayFilter(IntPtr pNewtonBody,
            float[] pHitNormal,
            Int32 pCollisionID,
            IntPtr pUserData,
            float pIntersetParam)
        {
            OnWorldRayFilter(new CWorldRayFilterEventArgs((CBody)CHashTables.Body[pNewtonBody],
                                                                new NewtonVector3(pHitNormal).ToDirectX(),
                                                                CHashTables.BodyUserData[pNewtonBody],
                                                                pIntersetParam));

            return pIntersetParam;
        }

        #endregion

        #region Virtuals

        protected virtual void OnBodyLeaveWorld(CBodyLeaveWorldEventArgs pEventArgs)
        {
            if (m_BodyLeaveWorld != null)
            {
                m_BodyLeaveWorld(this, pEventArgs);
            }
        }

        protected virtual void OnBodyIterator(CBodyIteratorEventArgs pEventArgs)
        {
            if (m_BodyIterator != null)
            {
                m_BodyIterator(this, pEventArgs);
            }
        }

        protected virtual void OnWorldRayPrefilter(CWorldRayPreFilterEventArgs pEventArgs)
        {
            if (m_WorldRayPrefilter != null)
            {
                m_WorldRayPrefilter(this, pEventArgs);
            }
        }

        protected virtual void OnWorldRayFilter(CWorldRayFilterEventArgs pEventArgs)
        {
            if (m_WorldRayFilter != null)
            {
                m_WorldRayFilter(this, pEventArgs);
            }
        }

        #endregion
    }
}
