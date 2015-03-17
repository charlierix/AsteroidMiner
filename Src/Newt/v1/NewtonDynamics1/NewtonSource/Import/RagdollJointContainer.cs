using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateRagDoll(IntPtr pNewtonWorld);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonDestroyRagDoll(IntPtr pNewtonWorld,
			IntPtr pRagDoll);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollBegin(IntPtr pRagDoll);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollEnd(IntPtr pRagDoll);


		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollSetFriction(IntPtr pRagDoll,
			float pFriction);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonRagDollFindBone(IntPtr pRagDoll,
			int pId);
	
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonRagDollGetRootBone(IntPtr pRagDoll);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollSetForceAndTorqueCallback(IntPtr pRagDoll,
			NewtonApplyForceAndTorque pCallback);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollSetTransformCallback(IntPtr pRagDoll,
			NewtonSetRagDollTransform pCallback);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonRagDollAddBone(IntPtr pRagDoll,
			IntPtr pParent,
			IntPtr pUserData,
			float pMass,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix,
			IntPtr pBoneCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pSize);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonRagDollBoneGetUserData(IntPtr pBone);
	
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonRagDollBoneGetBody(IntPtr pBone);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollBoneSetID(IntPtr pBone,
			int pId);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollBoneSetLimits(IntPtr pBone,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pConeDir,
			float pMinConeAngle,
			float pMaxConeAngle,
			float pMaxTwistAngle,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pBilateralConeDir,
			float pNegativeBilateralConeAngle,
			float pPositiveBilateralConeAngle);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonRagDollBoneGetChild(IntPtr pBone);
	
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonRagDollBoneGetSibling(IntPtr pBone);
	
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonRagDollBoneGetParent(IntPtr pBone);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollBoneSetLocalMatrix(IntPtr pBone,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollBoneSetGlobalMatrix(IntPtr pBone,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollBoneGetLocalMatrix(IntPtr pBone,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
		
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonRagDollBoneGetGlobalMatrix(IntPtr pBone,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
	}
}
