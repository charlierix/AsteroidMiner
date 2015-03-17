using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonCollisionPointDistance(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPoint,
			IntPtr pCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pContact,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pNormal);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonCollisionClosestPoint(IntPtr pNewtonWorld,
			IntPtr pCollisionA,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrixA,
			IntPtr pCollisionB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrixB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pContactA,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pContactB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pNormalAB);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonCollisionCollide(IntPtr pNewtonWorld,
			int pMaxSize,
			IntPtr pCollisionA,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrixA,
			IntPtr pCollisionB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrixB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pContacts,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pNormals,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPenetration);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonCollisionCollideContinue(IntPtr pNewtonWorld,
			int pMaxSize,
			float pTimestap,
			IntPtr pCollisionA,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrixA,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pVelocA,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pOmegaA,
			IntPtr pCollisionB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrixB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pVelocB,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pOmegaB,
			float pTimeOfImpact,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pContacts,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pNormals,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3000)]float[] pPenetration);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonCollisionRayCast(IntPtr pCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pNormals,
			int pAttribute);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonCollisionCalculateAABB(IntPtr pCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1);
	}
}
