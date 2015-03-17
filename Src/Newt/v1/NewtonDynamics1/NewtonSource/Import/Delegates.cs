using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr AllocMemory(int pSizeInBytes);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FreeMemory(IntPtr pPtr, int pSizeInBytes);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonSerialize(IntPtr pSerializeHandle, IntPtr pBuffer, uint pSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonDeserialize(IntPtr pSerializeHandle, IntPtr pBuffer, uint pSize);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonUserMeshCollisionCollide(
			[MarshalAs(UnmanagedType.LPStruct)]NewtonUserMeshCollisionCollideDesc pCollideDescData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate float NewtonUserMeshCollisionRayHit(
			[MarshalAs(UnmanagedType.LPStruct)]NewtonUserMeshCollisionRayHitDesc pLineDescData);
	
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonUserMeshCollisionDestroy(IntPtr pDescData);
	
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonTreeCollision(IntPtr pNewtonBodyWithTreeCollision,
			IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pVertex,
			int pVertexstrideInBytes,
			int pIndexCount,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 300)]int[] pIndexArray);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonBodyDestructor(IntPtr pNewtonBody);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonApplyForceAndTorque(IntPtr pNewtonBody);
	
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonBodyActivationState(IntPtr pNewtonBody, uint pState);


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonSetTransform(
			IntPtr body,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
	
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonSetRagDollTransform(IntPtr pBone);
	
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int NewtonGetBuoyancyPlane(int pCollisionID,
			IntPtr pContext,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pGlobalSpaceMatrix,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 4)]float[] pGlobalSpacePlane);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonVehicleTireUpdate(IntPtr pVehicle);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint NewtonWorldRayPrefilterCallback(IntPtr pNewtonBody,
			IntPtr pNewtonCollision,
			IntPtr pUserData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate float NewtonWorldRayFilterCallback(IntPtr pNewtonBody,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pHitNormal,
			int pCollisionID,
			IntPtr pUserData,
			float pIntersetParam);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonBodyLeaveWorld(IntPtr pNewtonBody);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int NewtonContactBegin(IntPtr pMaterial,
			IntPtr pNewtonBody0,
			IntPtr pNewtonBody1);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int NewtonContactProcess(IntPtr pMaterial, IntPtr pContact);
	
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonContactEnd(IntPtr pMaterial);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonBodyIterator(IntPtr pNewtonBody);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonCollisionIterator(IntPtr pNewtonBody,
			int pVertexCount,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pFaceArray,
			int pFaceId);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonBall(IntPtr pNewtonBall);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint NewtonHinge(IntPtr pHinge,
             [In, Out, MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc);
	
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint NewtonSlider(IntPtr pSlider,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint NewtonUniversal(IntPtr pUniversal,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint NewtonCorkscrew(IntPtr pCorkscrew,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc);
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonUserBilateral(IntPtr pUserJoint);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NewtonConstraintDestructor(IntPtr pMe);
	}
}

