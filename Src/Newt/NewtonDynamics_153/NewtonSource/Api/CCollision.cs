using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public partial class CCollision		//	should this be abstract?
	{
		#region Members

		//private int _ref = 0;
		protected IntPtr m_Handle;
		internal IntPtr Handle { get { return m_Handle; } }

		protected CWorld m_World;
		public CWorld World { get { return m_World; } }

		#endregion

		#region Constructor

		public CCollision(CWorld pWorld)
		{
			if (pWorld == null) throw new ArgumentNullException("pWorld");

			m_World = pWorld;
		}

		#endregion

		#region Methods

		public void Release()
		{
			if (m_Handle != IntPtr.Zero)
			{
				Newton.NewtonReleaseCollision(m_World.Handle, m_Handle);
				CHashTables.Collision.Remove(m_Handle);
			}
		}

		public void MakeUnique()
		{
			CheckHandle();
			Newton.NewtonCollisionMakeUnique(m_World.Handle, m_Handle);
		}

		protected void CheckHandle()
		{
			if (m_Handle == IntPtr.Zero)
				throw new InvalidOperationException(Properties.Resources.CCollision_HandleNotInitialised);
		}

		public int PointDistance(Vector3D pPoint, CCollision pCollision, Matrix3D pMatrix, Vector3D pContact, Vector3D pNormal)
		{
			return Newton.NewtonCollisionPointDistance(m_World.Handle,
				new NewtonVector3(pPoint).NWVector3,
				m_Handle,
				new NewtonMatrix(pMatrix).NWMatrix,
				new NewtonVector3(pContact).NWVector3,
				new NewtonVector3(pNormal).NWVector3);
		}

		public int ClosestPoint(CCollision pCollisionA, Matrix3D pMatrixA, CCollision pCollisionB, Matrix3D pMatrixB, Vector3D pContactA, Vector3D pContactB, Vector3D pNormalAB)
		{
			return Newton.NewtonCollisionClosestPoint(m_World.Handle,
				m_Handle,
				new NewtonMatrix(pMatrixA).NWMatrix,
				pCollisionB.m_Handle,
				new NewtonMatrix(pMatrixB).NWMatrix,
				new NewtonVector3(pContactA).NWVector3,
				new NewtonVector3(pContactB).NWVector3,
				new NewtonVector3(pNormalAB).NWVector3);
		}

		public int Collide(IntPtr pNewtonWorld, int pMaxSize, Matrix3D pMatrixA, CCollision pCollisionB, Matrix3D pMatrixB, Vector3D pContacts, Vector3D pNormals, Vector3D pPenetration)
		{
			return Newton.NewtonCollisionCollide(m_World.Handle,
				pMaxSize,
				m_Handle,
				new NewtonMatrix(pMatrixA).NWMatrix,
				pCollisionB.m_Handle,
				new NewtonMatrix(pMatrixB).NWMatrix,
				new NewtonVector3(pContacts).NWVector3,
				new NewtonVector3(pNormals).NWVector3,
				new NewtonVector3(pPenetration).NWVector3);
		}

		public int CollideContinue(int pMaxSize, float pTimestap, Matrix3D pMatrixA, Vector3D pVelocA, Vector3D pOmegaA, CCollision pCollisionB, Matrix3D pMatrixB, Vector3D pVelocB, Vector3D pOmegaB, float pTimeOfImpact, Vector3D pContacts, Vector3D pNormals, float[] pPenetration)
		{
			return Newton.NewtonCollisionCollideContinue(m_World.Handle,
				pMaxSize,
				pTimestap,
				m_Handle,
				new NewtonMatrix(pMatrixA).NWMatrix,
				new NewtonVector3(pVelocA).NWVector3,
				new NewtonVector3(pOmegaA).NWVector3,
				pCollisionB.m_Handle,
				new NewtonMatrix(pMatrixB).NWMatrix,
				new NewtonVector3(pVelocB).NWVector3,
				new NewtonVector3(pOmegaB).NWVector3,
				pTimeOfImpact,
				new NewtonVector3(pContacts).NWVector3,
				new NewtonVector3(pNormals).NWVector3,
				pPenetration);

		}

		public float RayCast(Vector3D p0, Vector3D p1, Vector3D pNormals, int pAttribute)
		{
			return Newton.NewtonCollisionRayCast(m_Handle,
				new NewtonVector3(p0).NWVector3,
				new NewtonVector3(p1).NWVector3,
				new NewtonVector3(pNormals).NWVector3,
				pAttribute);
		}

		public void CalculateAABB(Matrix3D pMatrix, Vector3D p0, Vector3D p1)
		{
			Newton.NewtonCollisionCalculateAABB(m_Handle,
				new NewtonMatrix(pMatrix).NWMatrix,
				new NewtonVector3(p0).NWVector3,
				new NewtonVector3(p1).NWVector3);
		}

		#endregion

		#region Properties

		public Matrix3D ConvexHullModifierMatrix
		{
			get
			{
				CheckHandle();
				NewtonMatrix aMatrix = new NewtonMatrix(Matrix3D.Identity);
				Newton.NewtonConvexHullModifierGetMatrix(m_Handle, aMatrix.NWMatrix);
				return aMatrix.ToDirectX();
			}
			set
			{
				CheckHandle();
				NewtonMatrix aMatrix = new NewtonMatrix(value);
				Newton.NewtonConvexHullModifierSetMatrix(m_Handle, aMatrix.NWMatrix);
			}
		}

		#endregion
	}
}

