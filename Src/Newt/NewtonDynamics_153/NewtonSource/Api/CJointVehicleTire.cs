using System;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public class CTire : IDisposable
    {
        #region Members

        private IntPtr m_Handle;
        internal IntPtr Handle { get { return m_Handle; } }

        private CJoint m_Vehicle;
        public CJoint Vehicle { get { return m_Vehicle; } }

        #endregion

        #region Constructor

        public CTire(CJoint pVehicle, IntPtr pHandle)
        {
            m_Vehicle = pVehicle;
            m_Handle = pHandle;

            CHashTables.Tire.Add(m_Handle, this);
        }

        #endregion

        #region Methods

        public float CalculateMaxBrakeAcceleration()
        {
            return Newton.NewtonVehicleTireCalculateMaxBrakeAcceleration(m_Vehicle.Handle, m_Handle);
        }

        public void SetBrakeAcceleration(float pAccelaration, float pTorqueLimit)
        {
            Newton.NewtonVehicleTireSetBrakeAcceleration(m_Vehicle.Handle,
                m_Handle,
                pAccelaration,
                pTorqueLimit);
        }

        public void Remove()
        {
            if (m_Handle != (IntPtr)0)
            {
                CHashTables.TireUserData.Remove(m_Handle);
                CHashTables.Tire.Remove(m_Handle);
                Newton.NewtonVehicleRemoveTire(m_Vehicle.Handle, m_Handle);
                m_Handle = (IntPtr)0;
            }
        }

        #endregion

        #region Properties

        public CTire NextTire
        {
            get
            {
                IntPtr aHandle = Newton.NewtonVehicleGetNextTireID(m_Vehicle.Handle, m_Handle);
                return (CTire)CHashTables.Tire[aHandle];
            }
        }

        public bool IsAirBorne
        {
            get
            {
                return Convert.ToBoolean(Newton.NewtonVehicleTireIsAirBorne(m_Vehicle.Handle, m_Handle));
            }
        }

        public bool LostSideGrip
        {
            get
            {
                return Convert.ToBoolean(Newton.NewtonVehicleTireLostSideGrip(m_Vehicle.Handle, m_Handle));
            }
        }

        public bool LostTraction
        {
            get
            {
                return Convert.ToBoolean(Newton.NewtonVehicleTireLostTraction(m_Vehicle.Handle, m_Handle));
            }
        }

        public object UserData
        {
            get
            {
                return (object)CHashTables.TireUserData[m_Handle];
            }
            set
            {
                if (CHashTables.TireUserData[m_Handle] != null)
                {
                    CHashTables.TireUserData[m_Handle] = value;
                }
                else
                {
                    CHashTables.TireUserData.Add(m_Handle, value);
                }
            }
        }

        public float Omega
        {
            get
            {
                return Newton.NewtonVehicleGetTireOmega(m_Vehicle.Handle, m_Handle);
            }
        }

        public float NormalLoad
        {
            get
            {
                return Newton.NewtonVehicleGetTireNormalLoad(m_Vehicle.Handle, m_Handle);
            }
        }

        public float SteerAngle
        {
            get
            {
                return Newton.NewtonVehicleGetTireSteerAngle(m_Vehicle.Handle, m_Handle);
            }

            set
            {
                Newton.NewtonVehicleSetTireSteerAngle(m_Vehicle.Handle, m_Handle, value);
            }
        }

        public float LateralSpeed
        {
            get
            {
                return Newton.NewtonVehicleGetTireLateralSpeed(m_Vehicle.Handle, m_Handle);
            }
        }

        public float LongitudinalSpeed
        {
            get
            {
                return Newton.NewtonVehicleGetTireLongitudinalSpeed(m_Vehicle.Handle, m_Handle);
            }
        }

        public Matrix3D Matrix
        {
            get
            {
                NewtonMatrix aMatrix = new NewtonMatrix(Matrix3D.Identity);
                Newton.NewtonVehicleGetTireMatrix(m_Vehicle.Handle, m_Handle, aMatrix.NWMatrix);
                return aMatrix.ToDirectX();
            }
        }

        public float Torque
        {
            set
            {
                Newton.NewtonVehicleSetTireTorque(m_Vehicle.Handle, m_Handle, value);
            }
        }

        public float MaxSideSleepSpeed
        {
            set
            {
                Newton.NewtonVehicleSetTireMaxSideSleepSpeed(m_Vehicle.Handle, m_Handle, value);
            }
        }

        public float SideSleepCoeficient
        {
            set
            {
                Newton.NewtonVehicleSetTireSideSleepCoeficient(m_Vehicle.Handle, m_Handle, value);
            }
        }

        public float MaxLongitudinalSlideSpeed
        {
            set
            {
                Newton.NewtonVehicleSetTireMaxLongitudinalSlideSpeed(m_Vehicle.Handle, m_Handle, value);
            }
        }

        public float LongitudinalSlideCoeficient
        {
            set
            {
                Newton.NewtonVehicleSetTireLongitudinalSlideCoeficient(m_Vehicle.Handle, m_Handle, value);
            }
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
                Remove();
            }
        }

        #endregion
    }
}
