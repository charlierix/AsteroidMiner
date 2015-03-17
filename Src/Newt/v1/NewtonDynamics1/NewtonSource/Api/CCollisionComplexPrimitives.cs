using System;
using System.Windows.Media.Media3D;
using System.Collections.Generic;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	/// <summary>
	/// This is a group of convex collision shapes
	/// </summary>
	public class CCollisionComplexPrimitives : CCollision
	{
		#region Members

		private EventHandler<CUserMeshCollisionCollideEventArgs> m_UserMeshCollisionCollide;
		private Newton.NewtonUserMeshCollisionCollide m_NewtonUserMeshCollisionCollide;

		private EventHandler<CUserMeshCollisionRayHitEventArgs> m_UserMeshCollisionRayHit;
		private Newton.NewtonUserMeshCollisionRayHit m_NewtonUserMeshCollisionRayHit;

		private EventHandler<CUserMeshCollisionDestroyEventArgs> m_UserMeshCollisionDestroy;
		private Newton.NewtonUserMeshCollisionDestroy m_NewtonUserMeshCollisionDestroy;

		#endregion

		#region Constructor

		public CCollisionComplexPrimitives(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion

		#region Methods

		public void CreateCompound(ICollection<CCollisionConvexPrimitives> convexCollisions)
		{
			IntPtr[] handles = new IntPtr[convexCollisions.Count];
			int i = 0;
			foreach (CCollisionConvexPrimitives collsion in convexCollisions)
			{
				handles[i++] = collsion.Handle;
			}
			m_Handle = Newton.NewtonCreateCompoundCollision(m_World.Handle, handles.Length, handles);

			CHashTables.Collision.Add(m_Handle, this);
		}

		public void CreateUserMesh(Vector3D pMinBox, Vector3D pMaxBox,
			EventHandler<CUserMeshCollisionCollideEventArgs> pCollideCallback,
			EventHandler<CUserMeshCollisionRayHitEventArgs> pRayHitCallback,
			EventHandler<CUserMeshCollisionDestroyEventArgs> pDestroyCallback)
		{
			m_UserMeshCollisionCollide = pCollideCallback;
			m_NewtonUserMeshCollisionCollide = new Newton.NewtonUserMeshCollisionCollide(InvokeUserMeshCollisionCollide);

			m_UserMeshCollisionRayHit = pRayHitCallback;
			m_NewtonUserMeshCollisionRayHit = new Newton.NewtonUserMeshCollisionRayHit(InvokeUserMeshCollisionRayHit);

			m_UserMeshCollisionDestroy = pDestroyCallback;
			m_NewtonUserMeshCollisionDestroy = new Newton.NewtonUserMeshCollisionDestroy(InvokeUserMeshCollisionDestroy);

			m_Handle = Newton.NewtonCreateUserMeshCollision(m_World.Handle,
				new NewtonVector3(pMinBox).NWVector3,
				new NewtonVector3(pMaxBox).NWVector3,
				new IntPtr(),
				m_NewtonUserMeshCollisionCollide,
				m_NewtonUserMeshCollisionRayHit,
				m_NewtonUserMeshCollisionDestroy);

			CHashTables.Collision.Add(m_Handle, this);
		}

		#endregion

		public InertialMatrix CalculateInertialMatrix
		{
			get
			{
				NewtonVector3 aInertia = new NewtonVector3(new Vector3D());
				NewtonVector3 aOrigin = new NewtonVector3(new Vector3D());

				Newton.NewtonConvexCollisionCalculateInertialMatrix(m_Handle,
					aInertia.NWVector3,
					aOrigin.NWVector3);

				return new InertialMatrix(aInertia.ToDirectX(), aOrigin.ToDirectX());
			}
		}

		#region Invokes

		private void InvokeUserMeshCollisionCollide(Newton.NewtonUserMeshCollisionCollideDesc pCollideDescData)
		{
			UserMeshCollisionCollideDesc aCollideDescData = new UserMeshCollisionCollideDesc(pCollideDescData);

			//aCollideDescData.m_BoxP0 = new NewtonVector4(pCollideDescData.m_BoxP0).ToDirectX();
			//aCollideDescData.m_BoxP1 = new NewtonVector4(pCollideDescData.m_BoxP1).ToDirectX();
			//aCollideDescData.m_FaceCount = pCollideDescData.m_FaceCount;
			//aCollideDescData.m_FaceIndexCount = pCollideDescData.m_FaceIndexCount;
			//aCollideDescData.m_FaceVertexIndex = pCollideDescData.m_FaceVertexIndex;
			//aCollideDescData.m_ObjBody = (CBody)CHashTables.Body[pCollideDescData.m_ObjBody];
			//aCollideDescData.m_PolySoupBody = (CBody)CHashTables.Body[pCollideDescData.m_PolySoupBody];
			//aCollideDescData.m_UserAttribute = pCollideDescData.m_UserAttribute;
			//aCollideDescData.m_UserData = pCollideDescData.m_UserData;
			//aCollideDescData.m_Vertex = pCollideDescData.m_Vertex;
			//aCollideDescData.m_VertexStrideInBytes = pCollideDescData.m_VertexStrideInBytes;

			OnUserMeshCollisionCollide(
				new CUserMeshCollisionCollideEventArgs(aCollideDescData));
		}

		private float InvokeUserMeshCollisionRayHit(Newton.NewtonUserMeshCollisionRayHitDesc pLineDescData)
		{
			UserMeshCollisionRayHitDesc aUserMeshCollisionRayHitDesc = new UserMeshCollisionRayHitDesc();

			aUserMeshCollisionRayHitDesc.m_NormalOut = new NewtonVector4(pLineDescData.m_NormalOut).ToDirectX();
			aUserMeshCollisionRayHitDesc.m_P0 = new NewtonVector4(pLineDescData.m_P0).ToDirectX();
			aUserMeshCollisionRayHitDesc.m_P1 = new NewtonVector4(pLineDescData.m_P1).ToDirectX();
			aUserMeshCollisionRayHitDesc.m_UserData = pLineDescData.m_UserData;
			aUserMeshCollisionRayHitDesc.m_UserIdOut = pLineDescData.m_UserIdOut;

			OnUserMeshCollisionRayHit(
				new CUserMeshCollisionRayHitEventArgs(aUserMeshCollisionRayHitDesc));

			return 1.2f;
		}

		private void InvokeUserMeshCollisionDestroy(IntPtr pDescData)
		{
			OnUserMeshCollisionDestroy(
				new CUserMeshCollisionDestroyEventArgs(pDescData));
		}

		#endregion

		#region Virtuals

		protected virtual void OnUserMeshCollisionCollide(CUserMeshCollisionCollideEventArgs pEventArgs)
		{
			if (m_UserMeshCollisionCollide != null)
			{
				m_UserMeshCollisionCollide(this, pEventArgs);
			}
		}

		protected virtual void OnUserMeshCollisionRayHit(CUserMeshCollisionRayHitEventArgs pEventArgs)
		{
			if (m_UserMeshCollisionRayHit != null)
			{
				m_UserMeshCollisionRayHit(this, pEventArgs);
			}
		}

		protected virtual void OnUserMeshCollisionDestroy(CUserMeshCollisionDestroyEventArgs pEventArgs)
		{
			if (m_UserMeshCollisionDestroy != null)
			{
				m_UserMeshCollisionDestroy(this, pEventArgs);
			}
		}

		#endregion
	}
}
