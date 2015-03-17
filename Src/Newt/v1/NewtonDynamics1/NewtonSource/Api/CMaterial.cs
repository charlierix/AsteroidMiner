using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	public partial class CMaterial
	{
		#region Members

		protected CWorld m_World;
		public CWorld World { get { return m_World; } }

		protected int m_ID;
		public int ID { get { return m_ID; } }

		private IntPtr m_Handle;
		internal IntPtr Handle { get { return m_Handle; } }

		#endregion

		#region Constructor

		public CMaterial(CWorld pWorld, IntPtr pHandle)
		{
			m_World = pWorld;
			m_Handle = pHandle;
		}

		public CMaterial(CWorld pWorld, bool pDefaultGroup)
		{
			m_World = pWorld;

			if (pDefaultGroup)
			{
				m_ID = GetDefaultGroupID();
			}
			else
			{
				m_ID = CreateGroupID();
			}

			CHashTables.Material.Add(m_ID, this);
		}

		#endregion

		#region Methods

		private int GetDefaultGroupID()
		{
			return Newton.NewtonMaterialGetDefaultGroupID(m_World.Handle);
		}

		private int CreateGroupID()
		{
			return Newton.NewtonMaterialCreateGroupID(m_World.Handle);
		}

		public void Dispose()
		{
			Newton.NewtonMaterialDestroyAllGroupID(m_World.Handle);

			CHashTables.Material.Remove(m_ID);
		}

		#endregion
		#region Contact Control Methods

		public void DisableContact()
		{
			Newton.NewtonMaterialDisableContact(m_Handle);
		}

		public uint GetBodyCollisionID(CBody pNewtonBody)
		{
			return Newton.NewtonMaterialGetBodyCollisionID(m_Handle, pNewtonBody.Handle);
		}

		public uint GetContactFaceAttribute()
		{
			return Newton.NewtonMaterialGetContactFaceAttribute(m_Handle);
		}

		public void GetContactPositionAndNormal(ref Vector3D pPosition, ref Vector3D pNormal)
		{
			Newton.NewtonMaterialGetContactPositionAndNormal(m_Handle,
				new NewtonVector3(pPosition).NWVector3,
				new NewtonVector3(pNormal).NWVector3);
		}

		public void GetContactTangentDirections(ref Vector3D pDirection0, ref Vector3D pDirection)
		{
			Newton.NewtonMaterialGetContactPositionAndNormal(m_Handle,
				new NewtonVector3(pDirection0).NWVector3,
				new NewtonVector3(pDirection).NWVector3);
		}

		public float GetContactTangentSpeed(CContact pContact, int pIndex)
		{
			return Newton.NewtonMaterialGetContactTangentSpeed(m_Handle,
				pContact.Handle,
				pIndex);
		}

		public float GetContactNormalSpeed(CContact pContact)
		{
			return Newton.NewtonMaterialGetContactNormalSpeed(m_Handle,
				pContact.Handle);
		}
		public float GetContactNormalSpeed(IntPtr pContact)
		{
			return Newton.NewtonMaterialGetContactNormalSpeed(m_Handle,
				pContact);
		}

		public float GetCurrentTimestep()
		{
			return Newton.NewtonMaterialGetCurrentTimestep(m_Handle);
		}

		public CMaterialPhysicsSpecialEffect GetSpecialEffect()
		{
			IntPtr aKey = Newton.NewtonMaterialGetMaterialPairUserData(m_Handle);

			return (CMaterialPhysicsSpecialEffect)CHashTables.MaterialsUserData[aKey];
		}

		public Vector3D GetContactForce()
		{
			NewtonVector3 aForce = new NewtonVector3(new Vector3D());
			Newton.NewtonMaterialGetContactForce(m_Handle, aForce.NWVector3);
			return aForce.ToDirectX();
		}

		public void SetContactFrictionState(int pState, int pIndex)
		{
			Newton.NewtonMaterialSetContactFrictionState(m_Handle, pState, pIndex);
		}

		public void SetContactStaticFrictionCoef(float pCoef, int pIndex)
		{
			Newton.NewtonMaterialSetContactStaticFrictionCoef(m_Handle, pCoef, pIndex);
		}

		public void SetContactKineticFrictionCoef(float pCoef, int pIndex)
		{
			Newton.NewtonMaterialSetContactKineticFrictionCoef(m_Handle, pCoef, pIndex);
		}

		public void SetContactTangentAcceleration(float pAccel, int pIndex)
		{
			Newton.NewtonMaterialSetContactTangentAcceleration(m_Handle, pAccel, pIndex);
		}

		public void SetContactSoftness(float pSoftness)
		{
			Newton.NewtonMaterialSetContactSoftness(m_Handle, pSoftness);
		}

		public void SetContactElasticity(float pRestitution)
		{
			Newton.NewtonMaterialSetContactElasticity(m_Handle, pRestitution);
		}

		public void SetRotateTangentDirections(Vector3D pDirection)
		{
			Newton.NewtonMaterialContactRotateTangentDirections(m_Handle,
						new NewtonVector3(pDirection).NWVector3);
		}

		public void SetContactNormalAcceleration(float pAcceleration)
		{
			Newton.NewtonMaterialSetContactNormalAcceleration(m_Handle, pAcceleration);
		}

		public void SetContactNormalDirection(Vector3D pDirection)
		{
			Newton.NewtonMaterialSetContactNormalDirection(m_Handle,
					new NewtonVector3(pDirection).NWVector3);
		}

		#endregion
	}
}











