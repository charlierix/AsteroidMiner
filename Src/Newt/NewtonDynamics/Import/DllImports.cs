using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Game.Newt.NewtonDynamics.Import
{
	//	If you want to know more, go here, and paste into the search box on the left side:
	//	http://newtondynamics.com/wiki/index.php5?title=API_Database
	//
	//	Also I sometimes found additional comments in the newton.cpp file
	internal static class Newton
	{
		#region Structures

		#region Struct: NewtonCollisionInfoRecord

		//TODO:  Figure out how to import this in c# - it may have already changed in a future version:
		//http://www.newtondynamics.com/forum/viewtopic.php?f=12&t=6654

		//[StructLayout(LayoutKind.Sequential)]
		//internal struct NewtonCollisionInfoRecord
		//{
		//    public float[,] m_offsetMatrix;		//[4][4]
		//    public int m_collisionType;				// tag id to identify the collision primitive
		//    public int m_referenceCount;				// the current reference count for this collision		
		//    public int m_collisionUserID;				

		//    struct NewtonBoxParam
		//    {
		//        public float m_x;
		//        public float m_y;
		//        public float m_z;
		//    }

		//    struct NewtonSphereParam
		//    {
		//        public float m_r0;
		//        public float m_r1;
		//        public float m_r2;
		//    }

		//    struct NewtonCylinderParam
		//    {
		//        public float m_r0;
		//        public float m_r1;
		//        public float m_height;
		//    }

		//    struct NewtonCapsuleParam
		//    {
		//        public float m_r0;
		//        public float m_r1;
		//        public float m_height;
		//    }

		//    struct NewtonConeParam
		//    {
		//        public float m_r;
		//        public float m_height;
		//    }

		//    struct NewtonChamferCylinderParam
		//    {
		//        public float m_r;
		//        public float m_height;
		//    }

		//    struct NewtonConvexHullParam
		//    {
		//        public int m_vertexCount;
		//        public int m_vertexStrideInBytes;
		//        public int m_faceCount;
		//        public dFloat* m_vertex;
		//    }

		//    struct NewtonConvexHullModifierParam
		//    {
		//        public NewtonCollision* m_chidren;
		//    }

		//    struct NewtonCompoundCollisionParam
		//    {
		//        public int m_chidrenCount;
		//        public NewtonCollision** m_chidren;
		//    }

		//    struct NewtonCollisionTreeParam
		//    {
		//        public int m_vertexCount;
		//        public int m_indexCount;
		//    }

		//    struct NewtonHeightFieldCollisionParam
		//    {
		//        public int m_width;
		//        public int m_height;
		//        public int m_gridsDiagonals;
		//        public float m_horizonalScale;
		//        public float m_verticalScale;
		//        public unsigned short *m_elevation;
		//        public char *m_atributes;
		//    }

		//    struct NewtonSceneCollisionParam
		//    {
		//        public int m_childrenProxyCount;
		//    }

		//    union 
		//    {
		//        NewtonBoxParam m_box;									
		//        NewtonConeParam m_cone;
		//        NewtonSphereParam m_sphere;
		//        NewtonCapsuleParam m_capsule;
		//        NewtonCylinderParam m_cylinder;
		//        NewtonChamferCylinderParam m_chamferCylinder;
		//        NewtonConvexHullParam m_convexHull;
		//        NewtonCompoundCollisionParam m_compoundCollision;
		//        NewtonConvexHullModifierParam m_convexHullModifier;
		//        NewtonCollisionTreeParam m_collisionTree;
		//        NewtonHeightFieldCollisionParam m_heightField;
		//        NewtonSceneCollisionParam m_sceneCollision;
		//        float[] m_paramArray;		    // user define collision can use this to store information		//[64]
		//    }
		//}

		#endregion
		#region Struct: NewtonJointRecord

		[StructLayout(LayoutKind.Sequential)]
		internal struct NewtonJointRecord
		{
			public float[,] m_attachmenMatrix_0;		//	[4][4]
			public float[,] m_attachmenMatrix_1;		//[4][4]
			public float[] m_minLinearDof;		//	[3]
			public float[] m_maxLinearDof;		//	[3]
			public float[] m_minAngularDof;		//	[3]
			public float[] m_maxAngularDof;		//	[3]
			public IntPtr m_attachBody_0;		//	const NewtonBody*
			public IntPtr m_attachBody_1;		//	const NewtonBody*
			public float[] m_extraParameters;		//	[16]
			public int m_bodiesCollisionOn;
			public char[] m_descriptionType;		//	[32]
		}

		#endregion

		#region Struct: NewtonUserMeshCollisionCollideDesc

		[StructLayout(LayoutKind.Sequential)]
		internal struct NewtonUserMeshCollisionCollideDesc
		{
			public float[] m_boxP0;						// lower bounding box of intersection query in local space		//	[4]
			public float[] m_boxP1;						// upper bounding box of intersection query in local space		//	[4]
			public int m_threadNumber;						// current thread executing this query
			public int m_faceCount;                        // the application should set here how many polygons intersect the query box
			public int m_vertexStrideInBytes;              // the application should set here the size of each vertex
			public IntPtr m_userData;                       // user data passed to the collision geometry at creation time
			public float[] m_vertex;                       // the application should the pointer to the vertex array. 
			public int m_userAttribute;                   // the application should set here the pointer to the user data, one for each face
			public int m_faceIndexCount;                  // the application should set here the pointer to the vertex count of each face.
			public int m_faceVertexIndex;                 // the application should set here the pointer index array for each vertex on a face.
			public IntPtr m_objBody;                  // pointer to the colliding body		//	NewtonBody*
			public IntPtr m_polySoupBody;             // pointer to the rigid body owner of this collision tree		//	NewtonBody*
		}

		#endregion
		#region Struct: NewtonWorldConvexCastReturnInfo

		[StructLayout(LayoutKind.Sequential)]
		internal struct NewtonWorldConvexCastReturnInfo
		{
			public float[] m_point;						// collision point in global space		//	[4]
			public float[] m_normal;						// surface normal at collision point in global space		//	[4]
			public float[] m_normalOnHitPoint;           // surface normal at the surface of the hit body, is the same as the normal calculated by a ray cast hitting the body at the hit point		//	[4]
			public float m_penetration;                   // contact penetration at collision point
			public int m_contactID;	                    // collision ID at contact point
			public IntPtr m_hitBody;			// body hit at contact point		//	const NewtonBody*
		}

		#endregion
		#region Struct: NewtonUserMeshCollisionRayHitDesc

		[StructLayout(LayoutKind.Sequential)]
		internal struct NewtonUserMeshCollisionRayHitDesc
		{
			public float[] m_p0;							// ray origin in collision local space		//	[4]
			public float[] m_p1;                         // ray destination in collision local space			//	[4]
			public float[] m_normalOut;					// copy here the normal at the ray intersection		//	[4]
			public int m_userIdOut;                        // copy here a user defined id for further feedback  
			public IntPtr m_userData;                       // user data passed to the collision geometry at creation time		//	void*
		}

		#endregion
		#region Struct: NewtonHingeSliderUpdateDesc

		[StructLayout(LayoutKind.Sequential)]
		internal struct NewtonHingeSliderUpdateDesc
		{
			public float m_accel;
			public float m_minFriction;
			public float m_maxFriction;
			public float m_timestep;
		}

		#endregion

		#endregion

		#region Callbacks (delegates)

		#region Newton callback functions

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate IntPtr NewtonAllocMemory(int sizeInBytes);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonFreeMemory(IntPtr ptr, int sizeInBytes);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonDestroyWorld(IntPtr newtonWorld);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint NewtonGetTicksCountCallback();


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonSerialize(IntPtr serializeHandle, IntPtr buffer, int size);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonDeserialize(IntPtr serializeHandle, IntPtr buffer, int size);

		#endregion
		#region User Collision Callbacks

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonUserMeshCollisionDestroyCallback(IntPtr userData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonUserMeshCollisionCollideCallback([MarshalAs(UnmanagedType.LPStruct)]NewtonUserMeshCollisionCollideDesc collideDescData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate float NewtonUserMeshCollisionRayHitCallback([MarshalAs(UnmanagedType.LPStruct)]NewtonUserMeshCollisionRayHitDesc lineDescData);

		//TODO:  Fix the NewtonCollisionInfoRecord struct, then this callback can be used
		//internal delegate void NewtonUserMeshCollisionGetCollisionInfo(IntPtr userData, [MarshalAs(UnmanagedType.LPStruct)]NewtonCollisionInfoRecord infoRecord);
		//TODO:  Figure out how to map the pointer to pointer (const dFloat** const vertexArray)
		//internal delegate int NewtonUserMeshCollisionGetFacesInAABB(IntPtr userData, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1, const dFloat** const vertexArray, int* const vertexCount, int* const vertexStrideInBytes, const int* const indexList, int maxIndexCount, const int* const userDataList);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate float NewtonCollisionTreeRayCastCallback(IntPtr body, IntPtr treeCollision, float interception, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, int faceId, IntPtr usedData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate float NewtonHeightFieldRayCastCallback(IntPtr body, IntPtr heightFieldCollision, float interception, int row, int col, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, int faceId, IntPtr usedData);


		// collision tree call back (obsoleted no recommended)
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonTreeCollisionCallback(IntPtr bodyWithTreeCollision, IntPtr body, int faceID, int vertexCount, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertex, int vertexStrideInBytes);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonBodyDestructor(IntPtr body);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonApplyForceAndTorque(IntPtr body, float timestep, int threadIndex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonSetTransform(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, int threadIndex);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int NewtonIslandUpdate(IntPtr newtonWorld, IntPtr islandHandle, int bodyCount);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonBodyLeaveWorld(IntPtr body, int threadIndex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonDestroyBodyByExeciveForce(IntPtr body, IntPtr contact);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonCollisionDestructor(IntPtr newtonWorld, IntPtr collision);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int NewtonCollisionCompoundBreakableCallback(IntPtr mesh, IntPtr userData, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] planeMatrixOut);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int NewtonGetBuoyancyPlane(int collisionID, IntPtr context, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] globalSpaceMatrix, [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)]float[] globalSpacePlane);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint NewtonWorldRayPrefilterCallback(IntPtr body, IntPtr collision, IntPtr userData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate float NewtonWorldRayFilterCallback(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] hitNormal, int collisionID, IntPtr userData, float intersectParam);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int NewtonOnAABBOverlap(IntPtr material, IntPtr body0, IntPtr body1, int threadIndex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonContactsProcess(IntPtr contact, float timestep, int threadIndex);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonBodyIterator(IntPtr body, IntPtr userData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonJointIterator(IntPtr joint, IntPtr userData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonCollisionIterator(IntPtr userData, int vertexCount, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] faceArray, int faceId);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonBallCallBack(IntPtr ball, float timestep);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint NewtonHingeCallBack(IntPtr hinge, [MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc desc);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint NewtonSliderCallBack(IntPtr slider, [MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc desc);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint NewtonUniversalCallBack(IntPtr universal, [MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc desc);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint NewtonCorkscrewCallBack(IntPtr corkscrew, [MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc desc);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonUserBilateralCallBack(IntPtr userJoint, float timestep, int threadIndex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonUserBilateralGetInfoCallBack(IntPtr userJoint, [MarshalAs(UnmanagedType.LPStruct)]NewtonJointRecord info);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void NewtonConstraintDestructor(IntPtr me);

		//	internal delegate void NewtonSetRagDollTransform(IntPtr bone);
		//	internal delegate void NewtonBodyActivationState(IntPtr  body, uint state);
		//	internal delegate void NewtonVehicleTireUpdate(IntPtr vehicle, dFloat timestep);

		#endregion

		#endregion

		#region World Control Functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonWorldGetVersion();
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonWorldFloatSize();

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonGetMemoryUsed();
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetMemorySystem(NewtonAllocMemory malloc, NewtonFreeMemory mfree);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreate();
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonDestroy(IntPtr newtonWorld);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonDestroyAllBodies(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUpdate(IntPtr newtonWorld, float timestep);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonInvalidateCache(IntPtr newtonWorld);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionUpdate(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetSolverModel(IntPtr newtonWorld, int model);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetPlatformArchitecture(IntPtr newtonWorld, int mode);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonGetPlatformArchitecture(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 256)]char[] description);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetMultiThreadSolverOnSingleIsland(IntPtr newtonWorld, int mode);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonGetMultiThreadSolverOnSingleIsland(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetPerformanceClock(IntPtr newtonWorld, NewtonGetTicksCountCallback callback);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint NewtonReadPerformanceTicks(IntPtr newtonWorld, uint performanceEntry);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint NewtonReadThreadPerformanceTicks(IntPtr newtonWorld, uint threadIndex);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonWorldCriticalSectionLock(IntPtr newtonWorld);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonWorldCriticalSectionUnlock(IntPtr newtonWorld);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetThreadsCount(IntPtr newtonWorld, int threads);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonGetThreadsCount(IntPtr newtonWorld);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonGetMaxThreadsCount(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetFrictionModel(IntPtr newtonWorld, int model);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetMinimumFrameRate(IntPtr newtonWorld, float frameRate);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetBodyLeaveWorldEvent(IntPtr newtonWorld, NewtonBodyLeaveWorld callback);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetWorldSize(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] minPoint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] maxPoint);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetIslandUpdateEvent(IntPtr newtonWorld, NewtonIslandUpdate islandUpdate);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetCollisionDestructor(IntPtr newtonWorld, NewtonCollisionDestructor callback);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetDestroyBodyByExeciveForce(IntPtr newtonWorld, NewtonDestroyBodyByExeciveForce callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonWorldForEachJointDo(IntPtr newtonWorld, NewtonJointIterator callback, IntPtr userData);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonWorldForEachBodyInAABBDo(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1, NewtonBodyIterator callback, IntPtr userData);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonWorldSetUserData(IntPtr newtonWorld, IntPtr userData);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonWorldGetUserData(IntPtr newtonWorld);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonWorldSetDestructorCallBack(IntPtr newtonWorld, NewtonDestroyWorld destructor);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern NewtonDestroyWorld NewtonWorldGetDestructorCallBack(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonWorldRayCast(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1, NewtonWorldRayFilterCallback filter, IntPtr userData, NewtonWorldRayPrefilterCallback prefilter);
		//NOTE:  I have no idea what hitParam is - dynamically sized array?  byref float? (which doesn't make sense, because it's dfloat* const hitParam)
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonWorldConvexCast(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] target, IntPtr shape, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]float[] hitParam, IntPtr userData, NewtonWorldRayPrefilterCallback prefilter, IntPtr info, int maxContactsCount, int threadIndex);

		#endregion
		#region World Utility Functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonWorldGetBodyCount(IntPtr newtonWorld);
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonWorldGetConstraintCount(IntPtr newtonWorld);

		#endregion

		#region Simulation islands

		//	More like a collision island (not sure why it's called simulation island)
		//	Just a fancy name for a group of bodies currently colliding

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonIslandGetBody(IntPtr island, int bodyIndex);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonIslandGetBodyAABB(IntPtr island, int bodyIndex, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1);

		#endregion

		#region Physics Material

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMaterialCreateGroupID(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMaterialGetDefaultGroupID(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialDestroyAllGroupID(IntPtr newtonWorld);


		// material definitions that can not be overwritten in function callback
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMaterialGetUserData(IntPtr newtonWorld, int id0, int id1);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetSurfaceThickness(IntPtr newtonWorld, int id0, int id1, float thickness);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContinuousCollisionMode(IntPtr newtonWorld, int id0, int id1, int state);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetCollisionCallback(IntPtr newtonWorld, int id0, int id1, IntPtr userData, NewtonOnAABBOverlap aabbOverlap, NewtonContactsProcess process);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetDefaultSoftness(IntPtr newtonWorld, int id0, int id1, float value);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetDefaultElasticity(IntPtr newtonWorld, int id0, int id1, float elasticCoef);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetDefaultCollidable(IntPtr newtonWorld, int id0, int id1, int state);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetDefaultFriction(IntPtr newtonWorld, int id0, int id1, float staticFriction, float kineticFriction);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonWorldGetFirstMaterial(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonWorldGetNextMaterial(IntPtr newtonWorld, IntPtr material);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonWorldGetFirstBody(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonWorldGetNextBody(IntPtr newtonWorld, IntPtr curBody);

		#endregion
		#region Physics Contact control functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMaterialGetMaterialPairUserData(IntPtr material);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint NewtonMaterialGetContactFaceAttribute(IntPtr material);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint NewtonMaterialGetBodyCollisionID(IntPtr material, IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonMaterialGetContactNormalSpeed(IntPtr material);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialGetContactForce(IntPtr material, IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialGetContactPositionAndNormal(IntPtr material, IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] posit, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialGetContactTangentDirections(IntPtr material, IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] dir0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] dir1);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonMaterialGetContactTangentSpeed(IntPtr material, int index);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContactSoftness(IntPtr material, float softness);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContactElasticity(IntPtr material, float restitution);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContactFrictionState(IntPtr material, int state, int index);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContactFrictionCoef(IntPtr material, float staticFrictionCoef, float kineticFrictionCoef, int index);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContactNormalAcceleration(IntPtr material, float accel);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContactNormalDirection(IntPtr material, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] directionVector);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialSetContactTangentAcceleration(IntPtr material, float accel, int index);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMaterialContactRotateTangentDirections(IntPtr material, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] directionVector);

		#endregion

		#region convex collision primitives creation functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateNull(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateSphere(IntPtr newtonWorld, float radiusX, float radiusY, float radiusZ, int shapeID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] offsetMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateBox(IntPtr newtonWorld, float dx, float dy, float dz, int shapeID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] offsetMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateCone(IntPtr newtonWorld, float radius, float height, int shapeID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] offsetMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateCapsule(IntPtr newtonWorld, float radius, float height, int shapeID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] offsetMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateCylinder(IntPtr newtonWorld, float radius, float height, int shapeID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] offsetMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateChamferCylinder(IntPtr newtonWorld, float radius, float height, int shapeID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] offsetMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateConvexHull(IntPtr newtonWorld, int count, [MarshalAs(UnmanagedType.LPArray /* SizeConst = 180000*/)]float[,] vertexCloud, int strideInBytes, float tolerance, int shapeID, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] offsetMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateConvexHullFromMesh(IntPtr newtonWorld, IntPtr mesh, float tolerance, int shapeID);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateConvexHullModifier(IntPtr newtonWorld, IntPtr convexHullCollision, int shapeID);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonConvexHullModifierGetMatrix(IntPtr convexHullCollision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonConvexHullModifierSetMatrix(IntPtr convexHullCollision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonCollisionIsTriggerVolume(IntPtr convexCollision);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionSetAsTriggerVolume(IntPtr convexCollision, int trigger);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionSetMaxBreakImpactImpulse(IntPtr convexHullCollision, float maxImpactImpulse);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCollisionGetMaxBreakImpactImpulse(IntPtr convexHullCollision);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionSetUserID(IntPtr convexCollision, uint id);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint NewtonCollisionGetUserID(IntPtr convexCollision);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonConvexHullGetFaceIndices(IntPtr convexHullCollision, int face, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]int[] faceIndices);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonConvexCollisionCalculateVolume(IntPtr convexCollision);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonConvexCollisionCalculateInertialMatrix(IntPtr convexCollision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] inertia, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] origin);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionMakeUnique(IntPtr newtonWorld, IntPtr collision);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonReleaseCollision(IntPtr newtonWorld, IntPtr collision);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonAddCollisionReference(IntPtr collision);

		#endregion
		#region mass/spring/damper collision shape

		//	internal static extern IntPtr NewtonCreateSoftShape (IntPtr newtonWorld);
		//	internal static extern void NewtonSoftBodySetMassCount (IntPtr convexCollision, int count);
		//	internal static extern void NewtonSoftBodySetSpringCount (IntPtr convexCollision, int count);

		//	internal static extern void NewtonSoftBodySetMass (IntPtr convexCollision, int index, dFloat mass, dFloat* position);
		//	internal static extern int NewtonSoftBodySetSpring (IntPtr convexCollision, int index, int mass0, int mass1, dFloat stiffness, dFloat damper);
		//	internal static extern int NewtonSoftBodyGetMassArray (IntPtr convexCollision, dFloat* masses, dFloat** positions);	

		#endregion
		#region complex collision primitives creation functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateCompoundCollision(IntPtr newtonWorld, int count, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]IntPtr[] collisionPrimitiveArray, int shapeID);
		//internal static extern IntPtr NewtonCreateCompoundCollisionFromMesh (IntPtr newtonWorld, IntPtr mesh, dFloat concavity, int shapeID, int subShapeID);
		//internal static extern IntPtr NewtonCreateCompoundCollisionFromMesh(IntPtr newtonWorld, IntPtr mesh, int maxSubShapesCount, int shapeID, int subShapeID);

		#endregion
		#region complex breakable collision primitives interface

		//NOTE:  The breakable collision isn't finalized

		//	internal static extern IntPtr NewtonCreateCompoundBreakable (IntPtr newtonWorld, int meshCount, 
		//															   NewtonMesh* const solids[], NewtonMesh* const splitePlanes[], 
		//															   dFloat* const matrixPallete, int* const shapeIDArray, dFloat* const densities,
		//															   int shapeID, int debriID, NewtonCollisionCompoundBreakableCallback callback, void* buildUsedData);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateCompoundBreakable(IntPtr newtonWorld, int meshCount, IntPtr solids, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]int[] shapeIDArray, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]float[] densities, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]int[] internalFaceMaterial, int shapeID, int debriID, float debriSeparationGap);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCompoundBreakableResetAnchoredPieces(IntPtr compoundBreakable);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCompoundBreakableSetAnchoredPieces(IntPtr compoundBreakable, int fixShapesCount, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrixPallete, IntPtr fixedShapesArray);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonCompoundBreakableGetVertexCount(IntPtr compoundBreakable);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCompoundBreakableGetVertexStreams(IntPtr compoundBreakable, int vertexStrideInByte, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertex, int normalStrideInByte, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, int uvStrideInByte, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] uv);		//	not sure what uv is


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBreakableGetMainMesh(IntPtr compoundBreakable);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBreakableGetFirstComponent(IntPtr compoundBreakable);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBreakableGetNextComponent(IntPtr component);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBreakableBeginDelete(IntPtr compoundBreakable);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBreakableCreateDebrieBody(IntPtr compoundBreakable, IntPtr component);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBreakableDeleteComponent(IntPtr compoundBreakable, IntPtr component);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBreakableEndDelete(IntPtr compoundBreakable);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBreakableGetComponentsInRadius(IntPtr compoundBreakable, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] position, float radius, IntPtr segments, int maxCount);		//	segments is another double pointer


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBreakableGetFirstSegment(IntPtr breakableComponent);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBreakableGetNextSegment(IntPtr segment);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBreakableSegmentGetMaterial(IntPtr segment);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBreakableSegmentGetIndexCount(IntPtr segment);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBreakableSegmentGetIndexStream(IntPtr compoundBreakable, IntPtr meshOwner, IntPtr segment, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]int[] index);		//	int* const index (not sure what index is supposed to be)

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBreakableSegmentGetIndexStreamShort(IntPtr compoundBreakable, IntPtr meshOwner, IntPtr segment, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]Int16[] index);		//	short int* const index

		//	TODO:  Fix the NewtonCollisionInfoRecord struct, then the delegates, then this will work (but it doesn't seem like I'll ever need it)
		//internal static extern IntPtr NewtonCreateUserMeshCollision (IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] minBox, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] maxBox, IntPtr userData, NewtonUserMeshCollisionCollideCallback collideCallback, NewtonUserMeshCollisionRayHitCallback rayHitCallback, NewtonUserMeshCollisionDestroyCallback destroyCallback, NewtonUserMeshCollisionGetCollisionInfo getInfoCallback, NewtonUserMeshCollisionGetFacesInAABB facesInAABBCallback, int shapeID);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateSceneCollision(IntPtr newtonWorld, int shapeID);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonSceneCollisionCreateProxy(IntPtr scene, IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSceneCollisionDestroyProxy(IntPtr scene, IntPtr Proxy);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSceneProxySetMatrix(IntPtr proxy, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSceneProxyGetMatrix(IntPtr proxy, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSceneSetProxyUserData(IntPtr proxy, IntPtr userData);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonSceneGetProxyUserData(IntPtr proxy);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonSceneGetFirstProxy(IntPtr scene);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonSceneGetNextProxy(IntPtr scene, IntPtr proxy);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSceneCollisionOptimize(IntPtr scene);

		#endregion
		#region Collision serialization functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateCollisionFromSerialization(IntPtr newtonWorld, NewtonDeserialize deserializeFunction, IntPtr serializeHandle);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionSerialize(IntPtr newtonWorld, IntPtr collision, NewtonSerialize serializeFunction, IntPtr serializeHandle);

		//TODO:  Fix NewtonCollisionInfoRecord first
		//internal static extern void NewtonCollisionGetInfo (IntPtr collision, NewtonCollisionInfoRecord* const collisionInfo);

		#endregion
		#region Static collision shapes functions

		//NOTE:  The .h file uses char[] for the attributeMap, but I believe byte is the better datatype (unless I'm mistaken, these are material ID's for each grid point, so
		//	you can define different elasticities for different parts of your terrain)
		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateHeightFieldCollision(IntPtr newtonWorld, int width, int height, int gridsDiagonals, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]short[] elevationMap, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]byte[] attributeMap, float horizontalScale, float verticalScale, int shapeID);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonHeightFieldSetUserRayCastCallback(IntPtr treeCollision, NewtonHeightFieldRayCastCallback rayHitCallback);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateTreeCollision(IntPtr newtonWorld, int shapeID);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonTreeCollisionSetUserRayCastCallback(IntPtr treeCollision, NewtonCollisionTreeRayCastCallback rayHitCallback);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonTreeCollisionBeginBuild(IntPtr treeCollision);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonTreeCollisionAddFace(IntPtr treeCollision, int vertexCount, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertexPtr, int strideInBytes, int faceAttribute);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonTreeCollisionEndBuild(IntPtr treeCollision, int optimize);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonTreeCollisionGetFaceAtribute(IntPtr treeCollision, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceIndexArray);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonTreeCollisionSetFaceAtribute(IntPtr treeCollision, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceIndexArray, int attribute);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonTreeCollisionGetVertexListIndexListInAABB(IntPtr treeCollision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = 3*/)]float[,] vertexArray, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] vertexCount, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] vertexStrideInBytes, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] indexList, int maxIndexCount, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceAttribute);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonStaticCollisionSetDebugCallback(IntPtr staticCollision, NewtonTreeCollisionCallback userCallback);

		#endregion
		#region General purpose collision library functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonCollisionPointDistance(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] point, IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] contact, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, int threadIndex);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonCollisionClosestPoint(IntPtr newtonWorld, IntPtr collisionA, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrixA, IntPtr collisionB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrixB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] contactA, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] contactB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normalAB, int threadIndex);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonCollisionCollide(IntPtr newtonWorld, int maxSize, IntPtr collisionA, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrixA, IntPtr collisionB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrixB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] contacts, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normals, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] penetration, int threadIndex);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonCollisionCollideContinue(IntPtr newtonWorld, int maxSize, float timestep, IntPtr collisionA, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrixA, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] velocA, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] omegaA, IntPtr collisionB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrixB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] velocB, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] omegaB, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]float[] timeOfImpact, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] contacts, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normals, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] penetration, int threadIndex);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionSupportVertex(IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] dir, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertex);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCollisionRayCast(IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, ref int attribute);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionCalculateAABB(IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCollisionForEachPolygonDo(IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, NewtonCollisionIterator callback, IntPtr userData);

		#endregion

		#region transforms utility functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonGetEulerAngle([MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] eulersAngles);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSetEulerAngle([MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] eulersAngles, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCalculateSpringDamperAcceleration(float dt, float ks, float x, float kd, float s);

		#endregion

		#region body manipulation functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonCreateBody(IntPtr newtonWorld, IntPtr collision, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonDestroyBody(IntPtr newtonWorld, IntPtr body);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyAddForce(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyAddTorque(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] torque);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyCalculateInverseDynamicsForce(IntPtr body, float timestep, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] desiredVeloc, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] forceOut);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetMatrix(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetMatrixRecursive(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetMassMatrix(IntPtr body, float mass, float Ixx, float Iyy, float Izz);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetMaterialGroupID(IntPtr body, int id);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetContinuousCollisionMode(IntPtr body, uint state);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetJointRecursiveCollision(IntPtr body, uint state);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetOmega(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] omega);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetVelocity(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] velocity);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetForce(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetTorque(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] torque);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetCentreOfMass(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] com);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetLinearDamping(IntPtr body, float linearDamp);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetAngularDamping(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] angularDamp);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetUserData(IntPtr body, IntPtr userData);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetCollision(IntPtr body, IntPtr collision);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBodyGetSleepState(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBodyGetAutoSleep(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetAutoSleep(IntPtr body, int state);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBodyGetFreezeState(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetFreezeState(IntPtr body, int state);


		//	internal static extern void NewtonBodySetAutoFreeze(IntPtr body, int state);
		//	internal static extern void NewtonBodyCoriolisForcesMode (IntPtr body, int mode);
		//	internal static extern void NewtonBodySetGyroscopicForcesMode (IntPtr body, int mode);
		//	internal static extern int  NewtonBodyGetGyroscopicForcesMode (IntPtr body);
		//	internal static extern int  NewtonBodyGetFreezeState (IntPtr body);
		//	internal static extern void NewtonBodySetFreezeState  (IntPtr body, int state);
		//	internal static extern void NewtonBodyGetFreezeTreshold (IntPtr body, dFloat* freezeSpeed2, dFloat* freezeOmega2);
		//	internal static extern void NewtonBodySetFreezeTreshold (IntPtr body, dFloat freezeSpeed2, dFloat freezeOmega2, int framesCount);
		//	internal static extern void NewtonBodySetAutoactiveCallback (IntPtr body, NewtonBodyActivationState callback);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetDestructorCallback(IntPtr body, NewtonBodyDestructor callback);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetTransformCallback(IntPtr body, NewtonSetTransform callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern NewtonSetTransform NewtonBodyGetTransformCallback(IntPtr body);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodySetForceAndTorqueCallback(IntPtr body, NewtonApplyForceAndTorque callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern NewtonApplyForceAndTorque NewtonBodyGetForceAndTorqueCallback(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBodyGetUserData(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBodyGetWorld(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBodyGetCollision(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBodyGetMaterialGroupID(IntPtr body);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBodyGetContinuousCollisionMode(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonBodyGetJointRecursiveCollision(IntPtr body);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetMatrix(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetRotation(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] rotation);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetMassMatrix(IntPtr body, ref float mass, ref float Ixx, ref float Iyy, ref float Izz);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetInvMass(IntPtr body, ref float invMass, ref float invIxx, ref float invIyy, ref float invIzz);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetOmega(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vector);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetVelocity(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vector);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetForce(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vector);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetTorque(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vector);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetForceAcc(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vector);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetTorqueAcc(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vector);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetCentreOfMass(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] com);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonBodyGetLinearDamping(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetAngularDamping(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vector);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyGetAABB(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBodyGetFirstJoint(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBodyGetNextJoint(IntPtr body, IntPtr joint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBodyGetFirstContactJoint(IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonBodyGetNextContactJoint(IntPtr body, IntPtr contactJoint);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonContactJointGetFirstContact(IntPtr contactJoint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonContactJointGetNextContact(IntPtr contactJoint, IntPtr contact);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonContactJointGetContactCount(IntPtr contactJoint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonContactJointRemoveContact(IntPtr contactJoint, IntPtr contact);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonContactGetMaterial(IntPtr contact);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyAddBuoyancyForce(IntPtr body, float fluidDensity, float fluidLinearViscosity, float fluidAngularViscosity, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] gravityVector, NewtonGetBuoyancyPlane buoyancyPlane, IntPtr context);

		//	internal static extern void NewtonBodyForEachPolygonDo (IntPtr body, NewtonCollisionIterator callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBodyAddImpulse(IntPtr body, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pointDeltaVeloc, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pointPosit);

		#endregion

		#region Common joint functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonJointGetUserData(IntPtr joint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonJointSetUserData(IntPtr joint, IntPtr userData);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonJointGetBody0(IntPtr joint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonJointGetBody1(IntPtr joint);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonJointGetInfo(IntPtr joint, IntPtr info);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonJointGetCollisionState(IntPtr joint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonJointSetCollisionState(IntPtr joint, int state);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonJointGetStiffness(IntPtr joint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonJointSetStiffness(IntPtr joint, float stiffness);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonDestroyJoint(IntPtr newtonWorld, IntPtr joint);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonJointSetDestructor(IntPtr joint, NewtonConstraintDestructor destructor);

		#endregion
		#region Ball and Socket joint functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonConstraintCreateBall(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivotPoint, IntPtr childBody, IntPtr parentBody);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBallSetUserCallback(IntPtr ball, NewtonBallCallBack callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBallGetJointAngle(IntPtr ball, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] angle);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBallGetJointOmega(IntPtr ball, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] omega);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBallGetJointForce(IntPtr ball, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonBallSetConeLimits(IntPtr ball, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pin, float maxConeAngle, float maxTwistAngle);

		#endregion
		#region Hinge joint functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonConstraintCreateHinge(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivotPoint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir, IntPtr childBody, IntPtr parentBody);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonHingeSetUserCallback(IntPtr hinge, NewtonHingeCallBack callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonHingeGetJointAngle(IntPtr hinge);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonHingeGetJointOmega(IntPtr hinge);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonHingeGetJointForce(IntPtr hinge, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonHingeCalculateStopAlpha(IntPtr hinge, IntPtr desc, float angle);

		#endregion
		#region Slider joint functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonConstraintCreateSlider(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivotPoint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir, IntPtr childBody, IntPtr parentBody);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSliderSetUserCallback(IntPtr slider, NewtonSliderCallBack callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonSliderGetJointPosit(IntPtr slider);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonSliderGetJointVeloc(IntPtr slider);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonSliderGetJointForce(IntPtr slider, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonSliderCalculateStopAccel(IntPtr slider, IntPtr desc, float position);

		#endregion
		#region Corkscrew joint functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonConstraintCreateCorkscrew(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivotPoint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir, IntPtr childBody, IntPtr parentBody);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCorkscrewSetUserCallback(IntPtr corkscrew, NewtonCorkscrewCallBack callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCorkscrewGetJointPosit(IntPtr corkscrew);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCorkscrewGetJointAngle(IntPtr corkscrew);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCorkscrewGetJointVeloc(IntPtr corkscrew);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCorkscrewGetJointOmega(IntPtr corkscrew);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonCorkscrewGetJointForce(IntPtr corkscrew, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCorkscrewCalculateStopAlpha(IntPtr corkscrew, IntPtr desc, float angle);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonCorkscrewCalculateStopAccel(IntPtr corkscrew, IntPtr desc, float position);

		#endregion
		#region Universal joint functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonConstraintCreateUniversal(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivotPoint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir1, IntPtr childBody, IntPtr parentBody);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUniversalSetUserCallback(IntPtr universal, NewtonUniversalCallBack callback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonUniversalGetJointAngle0(IntPtr universal);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonUniversalGetJointAngle1(IntPtr universal);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonUniversalGetJointOmega0(IntPtr universal);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonUniversalGetJointOmega1(IntPtr universal);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUniversalGetJointForce(IntPtr universal, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] force);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonUniversalCalculateStopAlpha0(IntPtr universal, IntPtr desc, float angle);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonUniversalCalculateStopAlpha1(IntPtr universal, IntPtr desc, float angle);

		#endregion
		#region Up vector joint functions

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonConstraintCreateUpVector(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir, IntPtr body);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUpVectorGetPin(IntPtr upVector, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pin);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUpVectorSetPin(IntPtr upVector, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pin);

		#endregion
		#region User defined bilateral Joint

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonConstraintCreateUserJoint(IntPtr newtonWorld, int maxDOF, NewtonUserBilateralCallBack callback, NewtonUserBilateralGetInfoCallBack getInfo, IntPtr childBody, IntPtr parentBody);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointSetFeedbackCollectorCallback(IntPtr joint, NewtonUserBilateralCallBack getFeedback);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointAddLinearRow(IntPtr joint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivot0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivot1, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] dir);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointAddAngularRow(IntPtr joint, float relativeAngle, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] dir);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointAddGeneralRow(IntPtr joint, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] jacobian0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] jacobian1);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointSetRowMinimumFriction(IntPtr joint, float friction);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointSetRowMaximumFriction(IntPtr joint, float friction);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointSetRowAcceleration(IntPtr joint, float acceleration);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointSetRowSpringDamperAcceleration(IntPtr joint, float springK, float springD);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonUserJointSetRowStiffness(IntPtr joint, float stiffness);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern float NewtonUserJointGetRowForce(IntPtr joint, int row);

		#endregion

		#region Mesh functions

		//	Not sure why this was called Mesh joint functions.  This seems to be a custom implementation a mesh (not sure how closely it mirrors wpf's Geometry3D)

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreate(IntPtr newtonWorld);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreateFromMesh(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreateFromCollision(IntPtr collision);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshConvexHull(IntPtr newtonWorld, int count, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]float[] vertexCloud, int strideInBytes, float tolerance);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreatePlane(IntPtr newtonWorld, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] locationMatrix, float witdth, float breadth, int material, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] textureMatrix0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] textureMatrix1);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshDestroy(IntPtr mesh);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshCalculateOOBB(IntPtr mesh, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] matrix, ref float x, ref float y, ref float z);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshCalculateVertexNormals(IntPtr mesh, float angleInRadians);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshApplySphericalMapping(IntPtr mesh, int material);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshApplyBoxMapping(IntPtr mesh, int front, int side, int top);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshApplyCylindricalMapping(IntPtr mesh, int cylinderMaterial, int capMaterial);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshIsOpenMesh(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshFixTJoints(IntPtr mesh);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshPolygonize(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshTriangulate(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshUnion(IntPtr mesh, IntPtr clipper, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] clipperMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshDifference(IntPtr mesh, IntPtr clipper, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] clipperMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshIntersection(IntPtr mesh, IntPtr clipper, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] clipperMatrix);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshClip(IntPtr mesh, IntPtr clipper, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] clipperMatrix, IntPtr topMesh, IntPtr bottomMesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshConvexDecomposition(IntPtr mesh, int maxCount);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshVoronoiDecomposition(IntPtr mesh, int pointCount, int pointStrideInBytes, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]float[] pointCloud, int internalMaterial, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] textureMatrix);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonRemoveUnusedVertices(IntPtr mesh, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] vertexRemapTable);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshBeginFace(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshAddFace(IntPtr mesh, int vertexCount, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertex, int strideInBytes, int materialIndex);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshEndFace(IntPtr mesh);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshBuildFromVertexListIndexList(IntPtr mesh, int faceCount, ref int faceIndexCount, ref int faceMaterialIndex, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertex, int vertexStrideInBytes, ref int vertexIndex, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, int normalStrideInBytes, ref int normalIndex, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] uv0, int uv0StrideInBytes, ref int uv0Index, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] uv1, int uv1StrideInBytes, ref int uv1Index);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshGetVertexStreams(IntPtr mesh, int vertexStrideInByte, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertex, int normalStrideInByte, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, int uvStrideInByte0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] uv0, int uvStrideInByte1, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] uv1);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshGetIndirectVertexStreams(IntPtr mesh, int vertexStrideInByte, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] vertex, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] vertexIndices, ref int vertexCount, int normalStrideInByte, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] normal, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] normalIndices, ref int normalCount, int uvStrideInByte0, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] uv0, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] uvIndices0, ref int uvCount0, int uvStrideInByte1, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] uv1, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] uvIndices1, ref int uvCount1);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshBeginHandle(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshEndHandle(IntPtr mesh, IntPtr handle);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshFirstMaterial(IntPtr mesh, IntPtr handle);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshNextMaterial(IntPtr mesh, IntPtr handle, int materialId);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshMaterialGetMaterial(IntPtr mesh, IntPtr handle, int materialId);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshMaterialGetIndexCount(IntPtr mesh, IntPtr handle, int materialId);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshMaterialGetIndexStream(IntPtr mesh, IntPtr handle, int materialId, ref int index);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshMaterialGetIndexStreamShort(IntPtr mesh, IntPtr handle, int materialId, ref short index);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreateFirstSingleSegment(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreateNextSingleSegment(IntPtr mesh, IntPtr segment);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreateFirstLayer(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshCreateNextLayer(IntPtr mesh, IntPtr segment);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetTotalFaceCount(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetTotalIndexCount(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshGetFaces(IntPtr mesh, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceIndexCount, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] faceMaterial, IntPtr faceIndices);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetPointCount(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetPointStrideInByte(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetPointArray(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetNormalArray(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetUV0Array(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetUV1Array(IntPtr mesh);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetVertexCount(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetVertexStrideInByte(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetVertexArray(IntPtr mesh);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetFirstVertex(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetNextVertex(IntPtr mesh, IntPtr vertex);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetVertexIndex(IntPtr mesh, IntPtr vertex);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetFirstPoint(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetNextPoint(IntPtr mesh, IntPtr point);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetPointIndex(IntPtr mesh, IntPtr point);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetVertexIndexFromPoint(IntPtr mesh, IntPtr point);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetFirstEdge(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetNextEdge(IntPtr mesh, IntPtr edge);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshGetEdgeIndices(IntPtr mesh, IntPtr edge, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] v0, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] v1);

		//internal static extern void NewtonMeshGetEdgePointIndices (IntPtr mesh, IntPtr edge, int* const v0, int* const v1);


		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetFirstFace(IntPtr mesh);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr NewtonMeshGetNextFace(IntPtr mesh, IntPtr face);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshIsFaceOpen(IntPtr mesh, IntPtr face);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetFaceMaterial(IntPtr mesh, IntPtr face);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int NewtonMeshGetFaceIndexCount(IntPtr mesh, IntPtr face);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshGetFaceIndices(IntPtr mesh, IntPtr face, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] indices);

		[DllImport("newton.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void NewtonMeshGetFacePointIndices(IntPtr mesh, IntPtr face, [MarshalAs(UnmanagedType.LPArray/*, SizeConst = */)]int[] indices);

		#endregion
	}
}
