using System;
using Game.Newt.NewtonDynamics_153.Api;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct NewtonUserMeshCollisionCollideDesc
		{
			public float[] m_BoxP0;					// lower bounding box of intersection query in local space
			public float[] m_BoxP1;					// upper bounding box of intersection query in local space
			public IntPtr m_UserData;				// user data passed to the collision geometry at creation time
			public int m_FaceCount;					// the application should set here how many polygons intersect the query box
			public float[] m_Vertex;            	// the application should the pointer to the vertex array. 
			public int m_VertexStrideInBytes;   	// the application should set here the size of each vertex
			public int m_UserAttribute;         	// the application should set here the pointer to the user data, one for each face
			public int m_FaceIndexCount;        	// the application should set here the pointer to the vertex count of each face.
			public int m_FaceVertexIndex;       	// the application should set here the pointer index array for each vertex on a face.
			public IntPtr m_ObjBody;               	// pointer to the colliding body
			public IntPtr m_PolySoupBody;          	// pointer to the rigid body owner of this collision tree 
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NewtonUserMeshCollisionRayHitDesc
		{
			public float[] m_P0;				// ray origin in collision local space
			public float[] m_P1;				// ray destination in collision local space   
			public float[] m_NormalOut;			// copy here the normal at the rat intersection
			public int m_UserIdOut;				// copy here a user defined id for further feedback  
			public IntPtr m_UserData;			// user data passed to the collision geometry at creation time
		}

	    [StructLayout(LayoutKind.Sequential)]
        public class NewtonHingeSliderUpdateDesc
	    {
			public float m_Accel;
            public float m_MinFriction;
            public float m_MaxFriction;
            public float m_Timestep;

            public NewtonHingeSliderUpdateDesc(HingeSliderUpdateDesc desc)
            {
                m_Accel = desc.m_Accel;
                m_MinFriction = desc.m_MinFriction;
                m_MaxFriction = desc.m_MaxFriction;
                m_Timestep = desc.m_Timestep;
            }
		}
	}
}
