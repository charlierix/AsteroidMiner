using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	public class CJointSlider : CJoint
	{
		#region Members

		private EventHandler<CSliderEventArgs> m_Slider;
		private Newton.NewtonSlider m_NewtonSlider;

		#endregion


		#region Constructor

		public CJointSlider(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion

		#region Methods

		public void CreateSlider(Vector3D pPivotPoint,
			Vector3D pinDir,
			CBody pChildBody,
			CBody pParentBody)
		{
			m_Handle = Newton.NewtonConstraintCreateSlider(m_World.Handle,
				new NewtonVector3(pPivotPoint).NWVector3,
				new NewtonVector3(pinDir).NWVector3,
				pChildBody.Handle,
				pParentBody.Handle);

			CHashTables.Joint.Add(m_Handle, this);
		}

		public float SliderCalculateStopAlpha(HingeSliderUpdateDesc pDesc,
			float pPosition)
		{
            Newton.NewtonHingeSliderUpdateDesc aUpdateDesc = new Newton.NewtonHingeSliderUpdateDesc(pDesc);

			aUpdateDesc.m_Accel = pDesc.m_Accel;
			aUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aUpdateDesc.m_Timestep = pDesc.m_Timestep;

			return Newton.NewtonSliderCalculateStopAccel(m_Handle,
				aUpdateDesc,
				pPosition);
		}

		#endregion


		#region Properties
		
		public float SliderPosition
		{
			get
			{
				return Newton.NewtonSliderGetJointPosit(m_Handle);
			}
		}

		public float SliderVelocity
		{
			get
			{
				return Newton.NewtonSliderGetJointVeloc(m_Handle);
			}
		}

		public Vector3D SliderForce
		{
			get
			{
				NewtonVector3 aForce = new NewtonVector3( new Vector3D() );
				Newton.NewtonSliderGetJointForce(m_Handle, aForce.NWVector3);
				return aForce.ToDirectX();
			}
		}

		#endregion


		#region Events

		public event EventHandler<CSliderEventArgs> Slider
		{
			add
			{
				if (m_Slider == null)
				{
					m_NewtonSlider = new Newton.NewtonSlider(InvokeSlider);
					Newton.NewtonSliderSetUserCallback(m_Handle, m_NewtonSlider);
				}

				m_Slider += value;
			}

			remove
			{
				m_Slider -= value;

				if (m_Slider == null)
				{
					m_NewtonSlider = null;
					Newton.NewtonSliderSetUserCallback(m_Handle, m_NewtonSlider);
				}
			}
		}

		#endregion

		#region Invokes

		private uint InvokeSlider(IntPtr pNewtonJoint,
			Newton.NewtonHingeSliderUpdateDesc pDesc)
		{
			HingeSliderUpdateDesc aUpdateDesc = new HingeSliderUpdateDesc();

			aUpdateDesc.m_Accel = pDesc.m_Accel;
			aUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aUpdateDesc.m_Timestep = pDesc.m_Timestep;

			OnSlider(new CSliderEventArgs(aUpdateDesc));

			return 1;
		}

		#endregion


		#region Virtuals

		protected virtual void OnSlider(CSliderEventArgs pEventArgs)
		{
			if (m_Slider != null)
			{
				m_Slider(this, pEventArgs);
			}
		}

		#endregion
	}
}
