using System;
using System.Collections.Generic;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public class CJointVehicle : CJoint
    {
        #region Members

        private EventHandler<CVehicleTireUpdateEventArgs> m_VehicleTireUpdate;
        private Newton.NewtonVehicleTireUpdate m_NewtonVehicleTireUpdate;

        #endregion

        #region Constructor

        public CJointVehicle(CWorld pWorld)
            : base(pWorld)
        {
        }

        #endregion

        #region Methods

        public void CreateVehicle(Vector3D pUpDir, CBody pNewtonBody)
        {
            m_Handle = Newton.NewtonConstraintCreateVehicle(m_World.Handle,
                new NewtonVector3(pUpDir).NWVector3,
                pNewtonBody.Handle);

            CHashTables.Joint.Add(m_Handle, this);
        }

        public void VehicleReset()
        {
            Newton.NewtonVehicleReset(m_Handle);
        }

        public CTire VehicleAddTire(Matrix3D pLocalMatrix,
            Vector3D pPin,
            float pMass,
            float pWidth,
            float pRadius,
            float pSuspesionShock,
            float pSuspesionSpring,
            float pSuspesionLength,
            object pUserData,
            int pCollisionID)
        {
            IntPtr aTireHandle = Newton.NewtonVehicleAddTire(m_Handle,
                        new NewtonMatrix(pLocalMatrix).NWMatrix,
                        new NewtonVector3(pPin).NWVector3,
                        pMass,
                        pWidth,
                        pRadius,
                        pSuspesionShock,
                        pSuspesionSpring,
                        pSuspesionLength,
                        (IntPtr)0, //pUserData.GetHashCode(),
                        pCollisionID);

            CTire aTire = new CTire(this, aTireHandle);
            aTire.UserData = pUserData;

            return aTire;
        }

        public void VehicleRemoveTire(CTire pTire)
        {
            pTire.Remove();
        }

        #endregion

        #region Properties

        public CTire VehicleFirstTire
        {
            get
            {
                IntPtr aHandle = Newton.NewtonVehicleGetFirstTireID(m_Handle);
                return (CTire)CHashTables.Tire[aHandle];
            }
        }

        #endregion

        #region Events

        public event EventHandler<CVehicleTireUpdateEventArgs> VehicleTireUpdate
        {
            add
            {
                if (m_VehicleTireUpdate == null)
                {
                    m_NewtonVehicleTireUpdate = new Newton.NewtonVehicleTireUpdate(InvokeVehicleTireUpdate);
                    Newton.NewtonVehicleSetTireCallback(m_Handle, m_NewtonVehicleTireUpdate);
                }

                m_VehicleTireUpdate += value;
            }

            remove
            {
                m_VehicleTireUpdate -= value;

                if (m_VehicleTireUpdate == null)
                {
                    m_NewtonVehicleTireUpdate = null;
                    Newton.NewtonVehicleSetTireCallback(m_Handle, m_NewtonVehicleTireUpdate);
                }
            }
        }

        #endregion

        #region Invokes

        private void InvokeVehicleTireUpdate(IntPtr pNewtonJoint)
        {
            OnVehicleTireUpdate(new CVehicleTireUpdateEventArgs());
        }

        #endregion

        #region Virtuals

        protected virtual void OnVehicleTireUpdate(CVehicleTireUpdateEventArgs pEventArgs)
        {
            if (m_VehicleTireUpdate != null)
            {
                m_VehicleTireUpdate(this, pEventArgs);
            }
        }

        #endregion

        #region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                List<CTire> tires = new List<CTire>();

                for (CTire tire = VehicleFirstTire; tire != null; tire = tire.NextTire)
                    tires.Add(tire);

                foreach (CTire tire in tires)
                    tire.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
