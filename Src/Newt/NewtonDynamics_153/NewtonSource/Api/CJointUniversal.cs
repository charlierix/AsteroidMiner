using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public class CJointUniversal : CJoint
	{
		#region Members

		private EventHandler<CUniversalEventArgs> m_Universal;
		private Newton.NewtonUniversal m_NewtonUniversal;

		#endregion


		#region Constructor

		public CJointUniversal(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion


		#region Methods

		public void CreateUniversal(Vector3D pPivotPoint,
			Vector3D pPinDir0,
			Vector3D pPinDir1,
			CBody pChildBody,
			CBody pParentBody)
		{
			m_Handle = Newton.NewtonConstraintCreateUniversal(m_World.Handle,
			   new NewtonVector3(pPivotPoint).NWVector3,
			   new NewtonVector3(pPinDir0).NWVector3,
			   new NewtonVector3(pPinDir1).NWVector3,
			   pChildBody.Handle,
			   pParentBody.Handle);

			CHashTables.Joint.Add(m_Handle, this);
		}

		public float UniversalCalculateStopAlpha0(HingeSliderUpdateDesc pDesc,
			float pAngle)
		{
            Newton.NewtonHingeSliderUpdateDesc aUpdateDesc = new Newton.NewtonHingeSliderUpdateDesc(pDesc);

			aUpdateDesc.m_Accel = pDesc.m_Accel;
			aUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aUpdateDesc.m_Timestep = pDesc.m_Timestep;

			return Newton.NewtonUniversalCalculateStopAlpha0(m_Handle,
				aUpdateDesc,
				pAngle);
		}

		public float UniversalCalculateStopAlpha1(HingeSliderUpdateDesc pDesc,
			float pAngle)
		{
            Newton.NewtonHingeSliderUpdateDesc aUpdateDesc = new Newton.NewtonHingeSliderUpdateDesc(pDesc);

			aUpdateDesc.m_Accel = pDesc.m_Accel;
			aUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aUpdateDesc.m_Timestep = pDesc.m_Timestep;

			return Newton.NewtonUniversalCalculateStopAlpha1(m_Handle,
				aUpdateDesc,
				pAngle);
		}

		#endregion


		#region Properties

		public float UniversalAngle0
		{
			get
			{
				return Newton.NewtonUniversalGetJointAngle0(m_Handle);
			}
		}

		public float UniversalAngle1
		{
			get
			{
				return Newton.NewtonUniversalGetJointAngle1(m_Handle);
			}
		}

		public float UniversalOmega0
		{
			get
			{
				return Newton.NewtonUniversalGetJointOmega0(m_Handle);
			}
		}

		public float UniversalOmega1
		{
			get
			{
				return Newton.NewtonUniversalGetJointOmega1(m_Handle);
			}
		}

		public Vector3D UniversalForce
		{
			get
			{
				NewtonVector3 aForce = new NewtonVector3(new Vector3D());
				Newton.NewtonUniversalGetJointForce(m_Handle, aForce.NWVector3);
				return aForce.ToDirectX();
			}
		}

		#endregion


		#region Events

		public event EventHandler<CUniversalEventArgs> Universal
		{
			add
			{
				if (m_Universal == null)
				{
					m_NewtonUniversal = new Newton.NewtonUniversal(InvokeUniversal);
					Newton.NewtonUniversalSetUserCallback(m_Handle, m_NewtonUniversal);
				}

				m_Universal += value;
			}

			remove
			{
				m_Universal -= value;

				if (m_Universal == null)
				{
					m_NewtonUniversal = null;
					Newton.NewtonUniversalSetUserCallback(m_Handle, m_NewtonUniversal);
				}
			}
		}

		#endregion


		#region Invokes

		private uint InvokeUniversal(IntPtr pNewtonJoint,
			Newton.NewtonHingeSliderUpdateDesc pDesc)
		{
			HingeSliderUpdateDesc aUpdateDesc = new HingeSliderUpdateDesc();

			aUpdateDesc.m_Accel = pDesc.m_Accel;
			aUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aUpdateDesc.m_Timestep = pDesc.m_Timestep;

			OnUniversal(new CUniversalEventArgs(aUpdateDesc));

			return 1;
		}

		#endregion


		#region Virtuals

		protected virtual void OnUniversal(CUniversalEventArgs pEventArgs)
		{
			if (m_Universal != null)
			{
				m_Universal(this, pEventArgs);
			}
		}

		#endregion
	}
}


