using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	public class CAllocMemoryEventArgs : EventArgs
	{
		private int m_SizeInBytes;
		public int SizeInBytes { get { return m_SizeInBytes; } }

		public CAllocMemoryEventArgs(int pSizeInBytes)
		{
			m_SizeInBytes = pSizeInBytes;
		}
	}

	public class CFreeMemoryEventArgs : EventArgs
	{
		private IntPtr m_Ptr;
		public IntPtr Ptr { get { return m_Ptr; } }

		private int m_SizeInBytes;
		public int SizeInBytes { get { return m_SizeInBytes; } }

		public CFreeMemoryEventArgs(IntPtr pPtr, int pSizeInBytes)
		{
			m_Ptr = pPtr;
			m_SizeInBytes = pSizeInBytes;
		}
	}

	public class CSerializeEventArgs : EventArgs
	{
		private IntPtr m_SerializeHandle;
		public IntPtr SerializeHandle { get { return m_SerializeHandle; } }

		private IntPtr m_Buffer;
		public IntPtr Buffer { get { return m_Buffer; } }

		private uint m_Size;
		public uint Size { get { return m_Size; } }

		public CSerializeEventArgs(IntPtr pSerializeHandle, IntPtr pBuffer, uint pSize)
		{
			m_SerializeHandle = pSerializeHandle;
			m_Buffer = pBuffer;
			m_Size = pSize;
		}
	}

	public class CDeserializeEventArgs : EventArgs
	{
		private IntPtr m_SerializeHandle;
		public IntPtr SerializeHandle { get { return m_SerializeHandle; } }

		private IntPtr m_Buffer;
		public IntPtr Buffer { get { return m_Buffer; } }

		private uint m_Size;
		public uint Size { get { return m_Size; } }

		public CDeserializeEventArgs(IntPtr pSerializeHandle, IntPtr pBuffer, uint pSize)
		{
			m_SerializeHandle = pSerializeHandle;
			m_Buffer = pBuffer;
			m_Size = pSize;
		}
	}

	public class CUserMeshCollisionCollideEventArgs : EventArgs
	{
		private UserMeshCollisionCollideDesc m_CollideDescData;
		public UserMeshCollisionCollideDesc CollideDescData { get { return m_CollideDescData; } }

		public CUserMeshCollisionCollideEventArgs(UserMeshCollisionCollideDesc pCollideDescData)
		{
			m_CollideDescData = pCollideDescData;
		}
	}

	public class CUserMeshCollisionRayHitEventArgs : EventArgs
	{
		private UserMeshCollisionRayHitDesc m_LineDescData;
		public UserMeshCollisionRayHitDesc LineDescData { get { return m_LineDescData; } }

		public CUserMeshCollisionRayHitEventArgs(UserMeshCollisionRayHitDesc pLineDescData)
		{
			m_LineDescData = pLineDescData;
		}
	}

	public class CUserMeshCollisionDestroyEventArgs : EventArgs
	{
		private IntPtr m_DescData;
		public IntPtr DescData { get { return m_DescData; } }

		public CUserMeshCollisionDestroyEventArgs(IntPtr pDescData)
		{
			m_DescData = pDescData;
		}
	}

	public class CTreeCollisionEventArgs : EventArgs
	{
		private CBody m_Body;
		public CBody Body { get { return m_Body; } }

		private Vector3D m_Vertex;
		public Vector3D Vertex { get { return m_Vertex; } }

		private int m_VertexstrideInBytes;
		public int VertexstrideInBytes { get { return m_VertexstrideInBytes; } }

		private int m_IndexCount;
		public int IndexCount { get { return m_IndexCount; } }

		private int[] m_IndexArray;
		public int[] IndexArray { get { return m_IndexArray; } }

		public CTreeCollisionEventArgs(CBody pBody,
			Vector3D pVertex,
			int pVertexstrideInBytes,
			int pIndexCount,
			int[] pIndexArray)
		{
			m_Body = pBody;
			m_Vertex = pVertex;
			m_VertexstrideInBytes = pVertexstrideInBytes;
			m_IndexCount = pIndexCount;
			m_IndexArray = pIndexArray;
		}
	}

	public class CBodyDestructorEventArgs : EventArgs
	{
		public CBodyDestructorEventArgs()
		{
		}
	}

	public class CApplyForceAndTorqueEventArgs : EventArgs
	{
		public CApplyForceAndTorqueEventArgs()
		{
		}
	}

	public class CBodyActivationStateEventArgs : EventArgs
	{
		private uint m_State;
		public uint State { get { return m_State; } }

		public CBodyActivationStateEventArgs(uint pState)
		{
			m_State = pState;
		}
	}

	public class CSetTransformEventArgs : EventArgs
	{
		private Matrix3D _matrix;
		public Matrix3D Matrix { get { return _matrix; } }

		public CSetTransformEventArgs(Matrix3D pMatrix)
		{
			_matrix = pMatrix;
		}
	}

	public class CSetRagDollTransformEventArgs : EventArgs
	{
		public CSetRagDollTransformEventArgs()
		{
		}
	}

	public class CBuoyancyPlaneEventArgs : EventArgs
	{
		private IntPtr m_Context;
		public IntPtr Context { get { return m_Context; } }

		private Matrix3D m_GlobalSpaceMatrix;
		public Matrix3D GlobalSpaceMatrix3D { get { return m_GlobalSpaceMatrix; } }

		private Point4D m_GlobalSpacePlane;
		public Point4D GlobalSpacePlane { get { return m_GlobalSpacePlane; } }

		public CBuoyancyPlaneEventArgs(IntPtr pContext,
			Matrix3D pGlobalSpaceMatrix,
			Point4D pGlobalSpacePlane)
		{
			m_Context = pContext;
			m_GlobalSpaceMatrix = pGlobalSpaceMatrix;
			m_GlobalSpacePlane = pGlobalSpacePlane;
		}
	}

	public class CVehicleTireUpdateEventArgs : EventArgs
	{
		public CVehicleTireUpdateEventArgs()
		{
		}
	}

	public class CWorldRayPreFilterEventArgs : EventArgs
	{
        private CBody m_Body;
        private CCollision m_Collision;
        private object m_UserData;
        private bool _skip;

        public CWorldRayPreFilterEventArgs(CBody pBody,
			object pUserData,
			CCollision pCollision)
		{
			m_Body = pBody;
			m_UserData = pUserData;
			m_Collision = pCollision;
		}

        public CBody Body
        { 
            get { return m_Body; } 
        }

        public CCollision Collision { get { return m_Collision; } }

        public object UserData { get { return m_UserData; } }

        public bool Skip
        {
            get { return _skip; }
            set { _skip = value; }
        }
    }

	public class CWorldRayFilterEventArgs : EventArgs
	{
		private CBody m_Body;
		public CBody Body { get { return m_Body; } }

		private Vector3D m_HitNormal;
		public Vector3D HitNormal { get { return m_HitNormal; } }

		private object m_UserData;
		public object UserData { get { return m_UserData; } }

		private float m_IntersetParam;
		public float IntersetParam { get { return m_IntersetParam; } }

		public CWorldRayFilterEventArgs(CBody pBody,
			Vector3D pHitNormal,
			object pUserData,
			float pIntersetParam)
		{
			m_Body = pBody;
			m_HitNormal = pHitNormal;
			m_UserData = pUserData;
			m_IntersetParam = pIntersetParam;
		}
	}

	public class CBodyLeaveWorldEventArgs : EventArgs
	{
		private CBody m_Body;
		public CBody Body { get { return m_Body; } }

		public CBodyLeaveWorldEventArgs(CBody pBody)
		{
			m_Body = pBody;
		}
	}
	
	public class CContactBeginEventArgs : EventArgs
	{
		private IntPtr m_Material;
		public IntPtr Material { get { return m_Material; } }

		private CBody m_Body0;
		public CBody Body0 { get { return m_Body0; } }

		private CBody m_Body1;
		public CBody Body1 { get { return m_Body1; } }

        private bool m_AllowCollision = true;
        public bool AllowCollision { get { return m_AllowCollision; } set { m_AllowCollision = value; } }

		public CContactBeginEventArgs(IntPtr pMaterial, CBody pBody0, CBody pBody1)
		{
			m_Material = pMaterial;
			m_Body0 = pBody0;
			m_Body1 = pBody1;
		}
	}

	public class CContactProcessEventArgs : EventArgs
	{
		private IntPtr m_Material;
		public IntPtr Material { get { return m_Material; } }

		private IntPtr m_Contact;
		public IntPtr Contact { get { return m_Contact; } }

        private bool m_AllowCollision = true;
        public bool AllowCollision { get { return m_AllowCollision; } set { m_AllowCollision = value; } }

		public CContactProcessEventArgs(IntPtr pMaterial, IntPtr pContact)
		{
			m_Material = pMaterial;
			m_Contact = pContact;
		}
	}

	public class CContactEndEventArgs : EventArgs
	{
		private IntPtr m_Material;
		public IntPtr Material { get { return m_Material; } }

		public CContactEndEventArgs(IntPtr pMaterial)
		{
			m_Material = pMaterial;
		}
	}

	public class CBodyIteratorEventArgs : EventArgs
	{
		private CBody m_Body;
		public CBody Body { get { return m_Body; } }

		public CBodyIteratorEventArgs(CBody pBody)
		{
			m_Body = pBody;
		}
	}

	public class CCollisionIteratorEventArgs : EventArgs
	{
		private int m_VertexCount;
		public int VertexCount { get { return m_VertexCount; } }

		private float[] m_FaceArray;
		public float[] FaceArray { get { return m_FaceArray; } }

		private int m_FaceId;
		public int FaceId { get { return m_FaceId; } }

		public CCollisionIteratorEventArgs(int pVertexCount, float[] pFaceArray, int pFaceId)
		{
			m_VertexCount = pVertexCount;
			m_FaceArray = pFaceArray;
			m_FaceId = pFaceId;
		}
	}

	public class CBallEventArgs : EventArgs
	{
		public CBallEventArgs()
		{
		}
	}

	public class CHingeEventArgs : EventArgs
	{
		private HingeSliderUpdateDesc m_Desc;
        private bool m_applyConstraint = false;

		public CHingeEventArgs(HingeSliderUpdateDesc pDesc)
		{
			m_Desc = pDesc;
		}

        public HingeSliderUpdateDesc Desc
        {
            get { return m_Desc; }
        }

        public bool ApplyConstraint
        {
            get { return m_applyConstraint; }
            set { m_applyConstraint = value; }
        }
	}

	public class CSliderEventArgs : EventArgs
	{
		private HingeSliderUpdateDesc m_Desc;
		public HingeSliderUpdateDesc Desc { get { return m_Desc; } }

		public CSliderEventArgs(HingeSliderUpdateDesc pDesc)
		{
			m_Desc = pDesc;
		}
	}

	public class CUniversalEventArgs : EventArgs
	{
		private HingeSliderUpdateDesc m_Desc;
		public HingeSliderUpdateDesc Desc { get { return m_Desc; } }

		public CUniversalEventArgs(HingeSliderUpdateDesc pDesc)
		{
			m_Desc = pDesc;
		}
	}

	public class CCorkscrewEventArgs : EventArgs
	{
		private HingeSliderUpdateDesc m_Desc;
		public HingeSliderUpdateDesc Desc { get { return m_Desc; } }

		public CCorkscrewEventArgs(HingeSliderUpdateDesc pDesc)
		{
			m_Desc = pDesc;
		}
	}

	public class CUserBilateralEventArgs : EventArgs
	{
	}

	public class CConstraintDestructorEventArgs : EventArgs
	{
	}
}


