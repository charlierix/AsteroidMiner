using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateBody(IntPtr pNewtonWorld,
			IntPtr collision);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonDestroyBody(IntPtr pNewtonWorld,
			IntPtr pNewtonBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyAddForce(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyAddTorque(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] torque);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetMatrix(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetMatrixRecursive(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetMassMatrix(IntPtr pBody,
			float pMass,
			float pIxx,
			float pIyy,
			float pIzz);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetMaterialGroupID(IntPtr pNewtonBody,
			int pId);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetContinuousCollisionMode(IntPtr pNewtonBody,
			uint pState);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetJointRecursiveCollision(IntPtr pNewtonBody,
			uint pState);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetOmega(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pOmega);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetVelocity(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] velocity);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetForce(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetTorque(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] torque);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetCentreOfMass(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pCOM);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetLinearDamping(IntPtr pNewtonBody,
			float pLinearDamp);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetAngularDamping(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] angularDamp);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetUserData(IntPtr pNewtonBody,
			IntPtr pUserData);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyCoriolisForcesMode(IntPtr pNewtonBody,
			int pMode);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetCollision(IntPtr pNewtonBody,
			IntPtr pCollision);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetAutoFreeze(IntPtr pNewtonBody,
			int pState);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetFreezeTreshold(IntPtr pNewtonBody,
			float pFreezeSpeed,
			float pFreezeOmega,
			int pFramesCount);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetTransformCallback(IntPtr pNewtonBody,
			NewtonSetTransform pCallback);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetDestructorCallback(IntPtr pNewtonBody,
			NewtonBodyDestructor pCallback);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetAutoactiveCallback(IntPtr pNewtonBody,
			NewtonBodyActivationState pCallback);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodySetForceAndTorqueCallback(IntPtr pNewtonBody,
			NewtonApplyForceAndTorque pCallback);
		
		[DllImport("Newton_153.dll")]
		internal static extern NewtonApplyForceAndTorque NewtonBodyGetForceAndTorqueCallback(IntPtr pNewtonBody);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonBodyGetUserData(IntPtr pNewtonBody);
		
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonBodyGetWorld(IntPtr pNewtonBody);
		
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonBodyGetCollision(IntPtr pNewtonBody);
		
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonBodyGetMaterialGroupID(IntPtr pNewtonBody);
		
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonBodyGetContinuousCollisionMode(IntPtr pNewtonBody);
		
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonBodyGetJointRecursiveCollision(IntPtr pNewtonBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetMatrix(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetMassMatrix(IntPtr pNewtonBody,
			ref float pMass,
			ref float pIxx,
			ref float pIyy,
			ref float pIzz);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetInvMass(IntPtr pNewtonBody,
			ref float pInvMass,
			ref float pInvIxx,
			ref float pInvIyy,
			ref float pInvIzz);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetOmega(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pVector);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetVelocity(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pVector);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetForce(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pVector);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetTorque(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pVector);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetCentreOfMass(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] com);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonBodyGetSleepingState(IntPtr pNewtonBody);
		
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonBodyGetAutoFreeze(IntPtr pNewtonBody);
		
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonBodyGetLinearDamping(IntPtr pNewtonBody);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetAngularDamping(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]ref float[] pVector);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetAABB(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]ref float[] p0,
		   [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]ref float[] p1);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyGetFreezeTreshold(IntPtr pNewtonBody,
			ref float pFreezeSpeed,
			ref float pFreezeOmega);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyAddBuoyancyForce(IntPtr pNewtonBody,
			float pFluidDensity,
			float pFluidLinearViscosity,
			float pFluidAngularViscosity,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pGravityVector,
			NewtonGetBuoyancyPlane pBuoyancyPlane,
			int pContext);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBodyForEachPolygonDo(IntPtr pNewtonBody,
			NewtonCollisionIterator pCallback);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonAddBodyImpulse(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPointDeltaVelocity,
		   [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPointPosition);
	}
}
