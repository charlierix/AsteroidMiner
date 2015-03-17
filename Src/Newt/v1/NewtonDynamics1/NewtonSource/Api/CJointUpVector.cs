using Game.Newt.v1.NewtonDynamics1.Api;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
	public class CJointUpVector : CJoint
	{
		#region Constructor

		public CJointUpVector(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion


		#region Methods

		public void CreateUpVector(Vector3D pPinDir, CBody pBody)
		{
			m_Handle = Newton.NewtonConstraintCreateUpVector(m_World.Handle,
				new NewtonVector3(pPinDir).NWVector3,
				pBody.Handle);

			CHashTables.Joint.Add(m_Handle, this);
		}

		#endregion

		#region Properties

		public Vector3D UpVectorPin
		{
			get
			{
				NewtonVector3 aPin = new NewtonVector3(new Vector3D());
				Newton.NewtonUpVectorGetPin(m_Handle, 
					aPin.NWVector3);
				return aPin.ToDirectX();
			}
			set
			{
				Newton.NewtonUpVectorSetPin(m_Handle,
					new NewtonVector3(value).NWVector3);
			}
		}

		#endregion
	}
}
