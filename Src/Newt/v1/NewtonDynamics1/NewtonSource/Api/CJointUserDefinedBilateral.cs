using System;
using Game.Newt.v1.NewtonDynamics1.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
	public class CJointUserDefinedBilateral : CJoint
	{
		#region Members

		private EventHandler<CUserBilateralEventArgs> m_UserBilateral;
		private Newton.NewtonUserBilateral m_NewtonUserBilateral;

		#endregion


		#region Constructor

		public CJointUserDefinedBilateral(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion


		#region Methods

		public void CreateUserBilateral(int pMaxDOF,
			EventHandler<CUserBilateralEventArgs> pCallback,
			CBody pChildBody,
			CBody pParentBody)
		{
			m_UserBilateral = pCallback;
			m_NewtonUserBilateral = new Newton.NewtonUserBilateral(InvokeUserBilateral);

            m_Handle = Newton.NewtonConstraintCreateUserJoint(m_World.Handle,
				pMaxDOF,
				m_NewtonUserBilateral,
				pChildBody.Handle,
				pParentBody.Handle);

			CHashTables.Joint.Add(m_Handle, this);
		}


		public void UserBilateralAddLinearRow(Vector3D pPivot0,
			Vector3D pPivot1,
			Vector3D pdir)
		{
			Newton.NewtonUserJointAddLinearRow(m_Handle,
			new NewtonVector3(pPivot0).NWVector3,
			new NewtonVector3(pPivot1).NWVector3,
			new NewtonVector3(pdir).NWVector3);
		}

		public void UserBilateralAddAngularRow(
			float pRelativeAngle,
			Vector3D pDir)
		{
			Newton.NewtonUserJointAddAngularRow(m_Handle,
				pRelativeAngle,
				new NewtonVector3(pDir).NWVector3);
		}

		public float UserBilateralGetRowForce(int pRow)
		{
			return Newton.NewtonUserJointGetRowForce(m_Handle, pRow);
		}

		//public void NewtonUserJointAddGeneralRow(IntPtr pJoint,
		//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 6)]float[] pJacobian0,
		//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 6)]float[] pJacobian1);

		public void UserBilateralSetRowSpringDamperAcceleration(float pSpringK,
			float pSpringD)
		{
			Newton.NewtonUserJointSetRowSpringDamperAcceleration(m_Handle,
				pSpringK,
				pSpringD);
		}

		#endregion


		#region Properties

		public float UserBilateralRowMinimumFriction
		{
			set
			{
				Newton.NewtonUserJointSetRowMinimumFriction(m_Handle, value);
			}
		}

		public float UserBilateralRowMaximumFriction
		{
			set
			{
				Newton.NewtonUserJointSetRowMaximumFriction(m_Handle, value);
			}
		}

		public float UserBilateralRowAcceleration
		{
			set
			{
				Newton.NewtonUserJointSetRowAcceleration(m_Handle, value);
			}
		}

		public float UserBilateralRowStiffness
		{
			set
			{
				Newton.NewtonUserJointSetRowStiffness(m_Handle, value);
			}
		}

		#endregion


		#region Invokes

		private void InvokeUserBilateral(IntPtr pNewtonJoint)
		{
			OnUserBilateral(new CUserBilateralEventArgs());
		}

		#endregion


		#region Virtuals

		protected virtual void OnUserBilateral(CUserBilateralEventArgs pEventArgs)
		{
			if (m_UserBilateral != null)
			{
				m_UserBilateral(this, pEventArgs);
			}
		}

		#endregion
	}
}
