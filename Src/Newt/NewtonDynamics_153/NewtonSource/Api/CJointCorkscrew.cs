using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public class CJointCorkscrew : CJoint
	{
		#region Members

		private EventHandler<CCorkscrewEventArgs> m_Corkscrew;
		private Newton.NewtonCorkscrew m_NewtonCorkscrew;

		#endregion


		#region Constructor

		public CJointCorkscrew(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion


		#region Methods

		public void CreateCorkscrew(Vector3D pPivotPoint,
			Vector3D pPinDir,
			CBody pChildBody,
			CBody pParentBody)
		{
			m_Handle = Newton.NewtonConstraintCreateCorkscrew(m_World.Handle,
				new NewtonVector3(pPivotPoint).NWVector3,
				new NewtonVector3(pPinDir).NWVector3,
				pChildBody.Handle,
				pParentBody.Handle);

			CHashTables.Joint.Add(m_Handle, this);
		}

		public float CorkscrewPosition
		{
			get
			{
				return Newton.NewtonCorkscrewGetJointPosit(m_Handle);
			}
		}

		public float CorkscrewAngle
		{
			get
			{
				return Newton.NewtonCorkscrewGetJointAngle(m_Handle);
			}
		}

		public float CorkscrewVelocity
		{
			get
			{
				return Newton.NewtonCorkscrewGetJointVeloc(m_Handle);
			}
		}

		public float CorkscrewOmega
		{
			get
			{
				return Newton.NewtonCorkscrewGetJointOmega(m_Handle);
			}
		}

		public Vector3D CorkscrewForce
		{
			get
			{
				NewtonVector3 aForce = new NewtonVector3( new Vector3D() );
				Newton.NewtonCorkscrewGetJointForce(m_Handle, aForce.NWVector3);
				return aForce.ToDirectX();
			}
		}


		public float CorkscrewCalculateStopAlpha(HingeSliderUpdateDesc pDesc,
			float pAngle)
		{
            Newton.NewtonHingeSliderUpdateDesc aHingeSliderUpdateDesc = new Newton.NewtonHingeSliderUpdateDesc(pDesc);

			aHingeSliderUpdateDesc.m_Accel = pDesc.m_Accel;
			aHingeSliderUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aHingeSliderUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aHingeSliderUpdateDesc.m_Timestep = pDesc.m_Timestep;

			return Newton.NewtonCorkscrewCalculateStopAlpha(m_Handle,
				aHingeSliderUpdateDesc,
				pAngle);
		}

		public float CorkscrewCalculateStopAccel(HingeSliderUpdateDesc pDesc,
			float pPosition)
		{
            Newton.NewtonHingeSliderUpdateDesc aHingeSliderUpdateDesc = new Newton.NewtonHingeSliderUpdateDesc(pDesc);

			aHingeSliderUpdateDesc.m_Accel = pDesc.m_Accel;
			aHingeSliderUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aHingeSliderUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aHingeSliderUpdateDesc.m_Timestep = pDesc.m_Timestep;

			return Newton.NewtonCorkscrewCalculateStopAccel(m_Handle,
				aHingeSliderUpdateDesc,
				pPosition);
		}

		#endregion


		#region Events

		public event EventHandler<CCorkscrewEventArgs> Corkscrew
		{
			add
			{
				if (m_Corkscrew == null)
				{
					m_NewtonCorkscrew = new Newton.NewtonCorkscrew(InvokeCorkscrew);
					Newton.NewtonCorkscrewSetUserCallback(m_Handle, m_NewtonCorkscrew);
				}

				m_Corkscrew += value;
			}

			remove
			{
				m_Corkscrew -= value;

				if (m_Corkscrew == null)
				{
					m_NewtonCorkscrew = null;
					Newton.NewtonCorkscrewSetUserCallback(m_Handle, m_NewtonCorkscrew);
				}
			}
		}

		#endregion

		#region Invokes

		private uint InvokeCorkscrew(IntPtr pNewtonJoint,
			Newton.NewtonHingeSliderUpdateDesc pDesc)
		{
			HingeSliderUpdateDesc aHingeSliderUpdateDesc = new HingeSliderUpdateDesc();

			aHingeSliderUpdateDesc.m_Accel = pDesc.m_Accel;
			aHingeSliderUpdateDesc.m_MaxFriction = pDesc.m_MaxFriction;
			aHingeSliderUpdateDesc.m_MinFriction = pDesc.m_MinFriction;
			aHingeSliderUpdateDesc.m_Timestep = pDesc.m_Timestep;

			OnCorkscrew(new CCorkscrewEventArgs(aHingeSliderUpdateDesc));

			return 1;
		}

		#endregion


		#region Virtuals

		protected virtual void OnCorkscrew(CCorkscrewEventArgs pEventArgs)
		{
			if (m_Corkscrew != null)
			{
				m_Corkscrew(this, pEventArgs);
			}
		}

		#endregion
	}
}
