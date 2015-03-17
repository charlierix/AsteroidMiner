using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	public class CJointBallSocket : CJoint
	{
		#region Members

		private EventHandler<CBallEventArgs> m_Ball;
		private Newton.NewtonBall m_NewtonBall;

		#endregion

		#region Constructor

		public CJointBallSocket(CWorld pWorld)
			: base(pWorld)
		{
		}
		
		#endregion
	
		#region Methods

		public void NewtonConstraintCreateBall(Vector3D pPivotPoint,
			CBody pChildBody,
			CBody pParentBody)
		{
			m_Handle = Newton.NewtonConstraintCreateBall(m_World.Handle,
			new NewtonVector3(pPivotPoint).NWVector3,
			pChildBody.Handle,
			pParentBody.Handle);

			CHashTables.Joint.Add(m_Handle, this);
		}

		#endregion

		#region Properties

		public Vector3D BallAngle
		{
			get
			{
				NewtonVector3 aAngle = new NewtonVector3(new Vector3D());
				Newton.NewtonBallGetJointAngle(m_Handle, aAngle.NWVector3);
				return aAngle.ToDirectX();
			}
		}

		public Vector3D BallOmega
		{
			get
			{
				NewtonVector3 aOmega = new NewtonVector3( new Vector3D() );
				Newton.NewtonBallGetJointOmega(m_Handle, aOmega.NWVector3);
				return aOmega.ToDirectX();
			}
		}

		public Vector3D BallForce
		{
			get
			{
				NewtonVector3 aForce = new NewtonVector3( new Vector3D() );
				Newton.NewtonBallGetJointForce(m_Handle, aForce.NWVector3);
				return aForce.ToDirectX();
			}
		}

		public void BallSetConeLimits(Vector3D pPin,
			float pMaxConeAngle,
			float pMaxTwistAngle)
		{
			Newton.NewtonBallSetConeLimits(m_Handle,
				new NewtonVector3(pPin).NWVector3,
				pMaxConeAngle,
				pMaxTwistAngle);
		}

		#endregion

		#region Events

		public event EventHandler<CBallEventArgs> Ball
		{
			add
			{
				if (m_Ball == null)
				{
					m_NewtonBall = InvokeBall;
					Newton.NewtonBallSetUserCallback(m_Handle, m_NewtonBall);
				}

				m_Ball += value;
			}

			remove
			{
				m_Ball -= value;

				if (m_Ball == null)
				{
					m_NewtonBall = null;
					Newton.NewtonBallSetUserCallback(m_Handle, m_NewtonBall);
				}
			}
		}

		#endregion

		#region Invokes

		protected void InvokeBall(IntPtr pNewtonJoint)
		{
			OnBall(new CBallEventArgs());
		}

		#endregion


		#region Virtuals

		protected virtual void OnBall(CBallEventArgs pEventArgs)
		{
			if (m_Ball != null)
			{
				m_Ball(this, pEventArgs);
			}
		}

		#endregion
	}
}
