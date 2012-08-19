using System;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public struct UserMeshCollisionCollideDesc
	{
		public Point4D m_BoxP0;					// lower bounding box of intersection query in local space
		public Point4D m_BoxP1;					// upper bounding box of intersection query in local space
		public object m_UserData;					// user data passed to the collision geometry at creation time
		public int m_FaceCount;					// the application should set here how many polygons intersect the query box
		public float[] m_Vertex;            	// the application should the pointer to the vertex array. 
		public int m_VertexStrideInBytes;   	// the application should set here the size of each vertex
		public int m_UserAttribute;         	// the application should set here the pointer to the user data, one for each face
		public int m_FaceIndexCount;        	// the application should set here the pointer to the vertex count of each face.
		public int m_FaceVertexIndex;       	// the application should set here the pointer index array for each vertex on a face.
		public CBody m_ObjBody;               	// pointer to the colliding body
		public CBody m_PolySoupBody;          	// pointer to the rigid body owner of this collision tree 

		internal UserMeshCollisionCollideDesc(Newton.NewtonUserMeshCollisionCollideDesc pDesc)
		{
			m_BoxP0 = new NewtonVector4(pDesc.m_BoxP0).ToDirectX();
			m_BoxP1 = new NewtonVector4(pDesc.m_BoxP1).ToDirectX();
			m_UserData = pDesc.m_UserData;
			m_FaceCount = pDesc.m_FaceCount;
			m_Vertex = pDesc.m_Vertex;
			m_VertexStrideInBytes = pDesc.m_VertexStrideInBytes;
			m_UserAttribute = pDesc.m_UserAttribute;
			m_FaceIndexCount = pDesc.m_FaceIndexCount;
			m_FaceVertexIndex = pDesc.m_FaceVertexIndex;
			m_ObjBody =  (CBody)CHashTables.Body[pDesc.m_ObjBody];
			m_PolySoupBody = (CBody)CHashTables.Body[pDesc.m_PolySoupBody];
		}

		internal Newton.NewtonUserMeshCollisionCollideDesc ToNewton()
		{
			Newton.NewtonUserMeshCollisionCollideDesc aUpdateDesc = new Newton.NewtonUserMeshCollisionCollideDesc();

//			aUpdateDesc.
			//aUpdateDesc.m_BoxP0 = new NewtonVector4(m_BoxP0).NWVector4;
			//aUpdateDesc.m_BoxP1 = new NewtonVector4(m_BoxP1).NWVector4;
			//aUpdateDesc.m_UserData = CHashTables.HashtableBodyUserData[m_UserData];

			return aUpdateDesc;
		}
	}

	public struct UserMeshCollisionRayHitDesc
	{
		public Point4D m_P0;				// ray origin in collision local space
		public Point4D m_P1;				// ray destination in collision local space   
		public Point4D m_NormalOut;			// copy here the normal at the rat intersection
		public int m_UserIdOut;				// copy here a user defined id for further feedback  
		public object m_UserData;			// user data passed to the collision geometry at creation time

		internal UserMeshCollisionRayHitDesc(Newton.NewtonUserMeshCollisionRayHitDesc pRayHitDesc)
		{
			m_NormalOut = new NewtonVector4(pRayHitDesc.m_NormalOut).ToDirectX();
			m_P0 = new NewtonVector4(pRayHitDesc.m_P0).ToDirectX();
			m_P1 = new NewtonVector4(pRayHitDesc.m_P1).ToDirectX();
			m_UserData = CHashTables.BodyUserData[pRayHitDesc.m_UserData];
			m_UserIdOut = pRayHitDesc.m_UserIdOut;
		}

		internal Newton.NewtonUserMeshCollisionRayHitDesc ToNewton()
		{
			Newton.NewtonUserMeshCollisionRayHitDesc aUpdateDesc = new Newton.NewtonUserMeshCollisionRayHitDesc();

			aUpdateDesc.m_NormalOut = new NewtonVector4(m_NormalOut).NWVector4;
			aUpdateDesc.m_P0 = new NewtonVector4(m_P0).NWVector4;
			aUpdateDesc.m_P1 = new NewtonVector4(m_P1).NWVector4;
			aUpdateDesc.m_UserData = (IntPtr)CHashTables.BodyUserData[m_UserData];
			aUpdateDesc.m_UserIdOut = m_UserIdOut;

			return aUpdateDesc;
		}
	}

	public class HingeSliderUpdateDesc
	{
		public float m_Accel;
		public float m_MinFriction;
		public float m_MaxFriction;
		public float m_Timestep;

        public HingeSliderUpdateDesc()
        {
        }

        internal HingeSliderUpdateDesc(Newton.NewtonHingeSliderUpdateDesc pUpdateDesc)
		{
            m_Accel = pUpdateDesc.m_Accel;
            m_MaxFriction = pUpdateDesc.m_MaxFriction;
            m_MinFriction = pUpdateDesc.m_MinFriction;
            m_Timestep = pUpdateDesc.m_Timestep;
		}

		internal void ToNewton(Newton.NewtonHingeSliderUpdateDesc newton)
		{
            newton.m_Accel = this.m_Accel;
            newton.m_MinFriction = this.m_MinFriction;
            newton.m_MaxFriction = this.m_MaxFriction;
            newton.m_Timestep = this.m_Timestep;
		}
	}

	public struct MassMatrix
	{
		public float m_Mass;
		public Vector3D m_I;

		public MassMatrix(float pMass, Vector3D pI)
		{
			m_Mass = pMass;
			m_I = pI;
		}
	}

	public struct FreezeTreshold
	{
		public float m_Speed;
		public float m_Omega;
		public int m_FramesCount;

		public FreezeTreshold(float pSpeed, float pOmega, int pFramesCount)
		{
			m_Speed = pSpeed;
			m_Omega = pOmega;
			m_FramesCount = pFramesCount;
		}
	}

	public struct AxisAlignedboundingBox 
	{
		public Vector3D m_Minimum;
		public Vector3D m_Maximum;

		public AxisAlignedboundingBox(Vector3D pMinimum, Vector3D pMaximum)
		{
			m_Minimum = pMinimum;
			m_Maximum = pMaximum;
		}
	}

	public struct InertialMatrix
	{
		public Vector3D m_Inertia;
		public Vector3D m_Origin;

		public InertialMatrix(Vector3D pInertia, Vector3D pOrigin)
		{
			m_Inertia = pInertia;
			m_Origin = pOrigin;
		}
	}
/*
	public struct Jacobian
	{
		public float[] m_Jacobian;

		public Jacobian(NewtonJacobian pNewtonJacobian)
		{
			m_Jacobian = new float[6];

			for (int i = 0; i < 6; i++)
			{
				m_Jacobian[i] = pNewtonJacobian[i];
			}
		}
	}*/
}
