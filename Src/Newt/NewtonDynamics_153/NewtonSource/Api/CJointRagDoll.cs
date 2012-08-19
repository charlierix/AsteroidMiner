using System;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public class CRagDoll : CJoint
	{
		#region Members

		private EventHandler<CSetRagDollTransformEventArgs> m_SetTransform;
		private Newton.NewtonSetRagDollTransform m_NewtonSetTransform;

		private EventHandler<CApplyForceAndTorqueEventArgs> m_ApplyForceAndTorque;
		private Newton.NewtonApplyForceAndTorque m_NewtonApplyForceAndTorque;

		#endregion


		#region Constructor

		public CRagDoll(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion


		#region Methods

		public void CreateRagDoll()
		{
			m_Handle = Newton.NewtonCreateRagDoll(m_World.Handle);
		}

		public void DestroyRagDoll()
		{
			Newton.NewtonDestroyRagDoll(m_World.Handle, m_Handle);
		}

		public void RagDollBegin()
		{
			Newton.NewtonRagDollBegin(m_Handle);
		}

		public void RagDollEnd()
		{
			Newton.NewtonRagDollEnd(m_Handle);
		}

		public CJointRagDollBone RagDollFindBone(int pId)
		{
			IntPtr aBone = Newton.NewtonRagDollFindBone(m_Handle, pId);
			return new CJointRagDollBone();
		}

		public CJointRagDollBone RagDollGetRootBone(int pId)
		{
			IntPtr aBone = Newton.NewtonRagDollGetRootBone(m_Handle);
			return new CJointRagDollBone();
		}

		#endregion


		#region Properties

		public float Friction
		{
			set
			{
				Newton.NewtonRagDollSetFriction(m_Handle, value);
			}
		}

		#endregion


		#region Events

		public event EventHandler<CSetRagDollTransformEventArgs> SetRagDollTransform
		{
			add
			{
				if (m_SetTransform == null)
				{
					m_NewtonSetTransform = new Newton.NewtonSetRagDollTransform(InvokeSetTransform);
					Newton.NewtonRagDollSetTransformCallback(m_Handle, m_NewtonSetTransform);
				}

				m_SetTransform += value;
			}

			remove
			{
				m_SetTransform -= value;

				if (m_SetTransform == null)
				{
					m_NewtonSetTransform = null;
					Newton.NewtonRagDollSetTransformCallback(m_Handle, m_NewtonSetTransform);
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
					Newton.NewtonRagDollSetForceAndTorqueCallback(m_Handle, m_NewtonApplyForceAndTorque);
				}

				m_ApplyForceAndTorque += value;
			}

			remove
			{
				m_ApplyForceAndTorque -= value;

				if (m_ApplyForceAndTorque == null)
				{
					m_NewtonApplyForceAndTorque = null;
					Newton.NewtonRagDollSetForceAndTorqueCallback(m_Handle, m_NewtonApplyForceAndTorque);
				}
			}
		}

		#endregion


		#region Invokes

		private void InvokeSetTransform(IntPtr pNewtonBody)
		{
			OnSetTransform(new CSetRagDollTransformEventArgs());
		}

		private void InvokeApplyForceAndTorque(IntPtr pNewtonBody)
		{
			OnApplyForceAndTorque(new CApplyForceAndTorqueEventArgs());
		}

		#endregion


		#region Virtuals

		protected virtual void OnSetTransform(CSetRagDollTransformEventArgs pEventArgs)
		{
			if (m_SetTransform != null)
			{
				m_SetTransform(this, pEventArgs);
			}
		}

		protected virtual void OnApplyForceAndTorque(CApplyForceAndTorqueEventArgs pEventArgs)
		{
			if (m_ApplyForceAndTorque != null)
			{
				m_ApplyForceAndTorque(this, pEventArgs);
			}
		}

		#endregion

	}
}

//public IntPtr NewtonRagDollAddBone(IntPtr pRagDoll,
//    CRagdoll pParent,
//    object pUserData,
//    float pMass,
//    Matrix3D pMatrix,
//    CCollision pBoneCollision,
//    Vector3D pSize);

