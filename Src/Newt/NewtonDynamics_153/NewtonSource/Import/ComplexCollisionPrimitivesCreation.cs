using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateCompoundCollision(IntPtr pNewtonWorld,
			int pCount,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 300)]IntPtr[] pCollisionPrimitiveArray);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateUserMeshCollision(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pMinBox,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pMaxBox,
			IntPtr pUserData,
			NewtonUserMeshCollisionCollide pCollideCallback,
			NewtonUserMeshCollisionRayHit pRayHitCallback,
			NewtonUserMeshCollisionDestroy pDestroyCallback);
	}
}
