using System;

namespace Game.Newt.NewtonDynamics_153.Api
{
    public partial class CJoint : IDisposable
    {
        #region Members

        protected IntPtr m_Handle;
        internal IntPtr Handle { get { return m_Handle; } }

        protected CWorld m_World;
        public CWorld World { get { return m_World; } }

        private EventHandler<CConstraintDestructorEventArgs> m_ConstraintDestructor;
        private Newton.NewtonConstraintDestructor m_NewtonConstraintDestructor;

        #endregion

        #region Constructor

        public CJoint(CWorld pWorld)
        {
            if (pWorld == null) throw new ArgumentNullException("pWorld");

            m_World = pWorld;
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (m_Handle != IntPtr.Zero)
            {
                Newton.NewtonDestroyJoint(m_World.Handle, m_Handle);

                CHashTables.JointUserData.Remove(m_Handle);
                CHashTables.Joint.Remove(m_Handle);
                m_Handle = IntPtr.Zero;
            }
        }

        #endregion


        #region Properties

        public object UserData
        {
            get
            {
                return CHashTables.JointUserData[m_Handle];
            }
            set
            {
                if (CHashTables.JointUserData[m_Handle] != null)
                {
                    CHashTables.JointUserData[m_Handle] = value;
                }
                else
                {
                    CHashTables.JointUserData.Add(m_Handle, value);
                    Newton.NewtonJointSetUserData(m_Handle, m_Handle);
                }
            }
        }

        public int CollisionState
        {
            get
            {
                return Newton.NewtonJointGetCollisionState(m_Handle);
            }
            set
            {
                Newton.NewtonJointSetCollisionState(m_Handle, value);
            }
        }

        public float Stiffness
        {
            get
            {
                return Newton.NewtonJointGetStiffness(m_Handle);
            }
            set
            {
                Newton.NewtonJointSetStiffness(m_Handle, value);
            }
        }

        #endregion


        #region Events

        public event EventHandler<CConstraintDestructorEventArgs> ConstraintDestructor
        {
            add
            {
                if (m_ConstraintDestructor == null)
                {
                    m_NewtonConstraintDestructor = new Newton.NewtonConstraintDestructor(InvokeDestructor);
                    Newton.NewtonJointSetDestructor(m_Handle, m_NewtonConstraintDestructor);
                }

                m_ConstraintDestructor += value;
            }

            remove
            {
                m_ConstraintDestructor -= value;

                if (m_ConstraintDestructor == null)
                {
                    m_NewtonConstraintDestructor = null;
                    Newton.NewtonJointSetDestructor(m_Handle, m_NewtonConstraintDestructor);
                }
            }
        }

        #endregion


        #region Invokes

        protected void InvokeDestructor(IntPtr pNewtonJoint)
        {
            OnDestructor(new CConstraintDestructorEventArgs());
        }

        #endregion


        #region Virtuals

        protected virtual void OnDestructor(CConstraintDestructorEventArgs pEventArgs)
        {
            if (m_ConstraintDestructor != null)
            {
                m_ConstraintDestructor(this, pEventArgs);
            }
        }

        #endregion
    }
}
