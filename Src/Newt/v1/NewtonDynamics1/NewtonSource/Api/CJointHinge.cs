using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	public class CJointHinge : CJoint
	{
		#region Members

		private EventHandler<CHingeEventArgs> m_Hinge;
		private Newton.NewtonHinge m_NewtonHinge;

		#endregion


		#region Constructor

		public CJointHinge(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion


		#region Methods

		public void CreateHinge(Vector3D pPivotPoint,
			Vector3D pinDir,
			CBody pChildBody,
			CBody pParentBody)
		{
			m_Handle = Newton.NewtonConstraintCreateHinge(m_World.Handle,
				new NewtonVector3(pPivotPoint).NWVector3,
				new NewtonVector3(pinDir).NWVector3,
				pChildBody.Handle,
				pParentBody.Handle);

			CHashTables.Joint.Add(m_Handle, this);
		}

        public float HingeCalculateStopAlpha(HingeSliderUpdateDesc pDesc, float pAngle)
		{
			Newton.NewtonHingeSliderUpdateDesc aUpdateDesc = new Newton.NewtonHingeSliderUpdateDesc(pDesc);

			aUpdateDesc.m_Accel = pDesc.m_Accel;
			aUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aUpdateDesc.m_Timestep = pDesc.m_Timestep;

			return Newton.NewtonHingeCalculateStopAlpha(m_Handle,
                aUpdateDesc,
				pAngle);
		}

		#endregion


		#region Properties
		
		public float HingeAngle
		{
			get
			{
				return Newton.NewtonHingeGetJointAngle(m_Handle);
			}
		}

		public float HingeOmega
		{
			get
			{
				return Newton.NewtonHingeGetJointOmega(m_Handle);
			}
		}

		public Vector3D HingeForce
		{
			get
			{
				NewtonVector3 aForce = new NewtonVector3(new Vector3D());
				Newton.NewtonHingeGetJointForce(m_Handle, aForce.NWVector3);
				return aForce.ToDirectX();
			}
		}

		#endregion


		#region Events

		public event EventHandler<CHingeEventArgs> Hinge
		{
			add
			{
				if (m_Hinge == null)
				{
					m_NewtonHinge = new Newton.NewtonHinge(InvokeHinge);
                    Newton.NewtonHingeSetUserCallback(m_Handle, m_NewtonHinge);
				}

				m_Hinge += value;
			}

			remove
			{
				m_Hinge -= value;

				if (m_Hinge == null)
				{
					m_NewtonHinge = null;
					Newton.NewtonHingeSetUserCallback(m_Handle, m_NewtonHinge);
				}
			}
		}

		#endregion

		#region Invokes

        private uint InvokeHinge(IntPtr pNewtonJoint, Newton.NewtonHingeSliderUpdateDesc pDesc)
		{
            CHingeEventArgs e = new CHingeEventArgs(new HingeSliderUpdateDesc(pDesc));
            OnHinge(e);

            if (e.ApplyConstraint)
            {
                e.Desc.ToNewton(pDesc);
                return 1;
            }
            else
                return 0;
		}

		#endregion


		#region Virtuals

		protected virtual void OnHinge(CHingeEventArgs pEventArgs)
		{
			if (m_Hinge != null)
				m_Hinge(this, pEventArgs);
		}

		#endregion
	}
}
