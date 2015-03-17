using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateNull(IntPtr pNewtonWorld);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateSphere(IntPtr pNewtonWorld,
			float pRadiusX,
			float pRadiusY,
			float pRadiusZ,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pOffsetMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateBox(IntPtr pNewtonWorld,
			float pSizex,
			float pSizey,
			float pSizez,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pOffsetMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateCone(IntPtr pNewtonWorld,
			float pRadius,
			float pHeight,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pOffsetMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateCapsule(IntPtr pNewtonWorld,
			float pRadius,
			float pHeight,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pOffsetMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateCylinder(IntPtr pNewtonWorld,
			float pRadius,
			float pHeight,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pOffsetMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateChamferCylinder(IntPtr pNewtonWorld,
			float pRadius,
			float pHeight,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pOffsetMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateConvexHull(IntPtr pNewtonWorld,
			int pCount,
			[MarshalAs(UnmanagedType.LPArray /* SizeConst = 180000*/)]float[,] pVertexCloud,
			int pStrideInBytes,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pOffsetMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateConvexHullModifier(IntPtr pNewtonWorld,
			IntPtr pConvexHullCollision);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonConvexHullModifierGetMatrix(IntPtr pConvexHullCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonConvexHullModifierSetMatrix(IntPtr pConvexHullCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonConvexCollisionSetUserID(IntPtr pConvexCollision,
			uint pId);
	
		[DllImport("Newton_153.dll")]
		internal static extern uint NewtonConvexCollisionGetUserID(IntPtr pConvexCollision);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonConvexCollisionCalculateVolume(IntPtr pConvexCollision);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonConvexCollisionCalculateInertialMatrix(IntPtr pConvexCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pInertia,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pOrigin);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonCollisionMakeUnique(IntPtr pNewtonWorld,
			IntPtr pCollision);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonReleaseCollision(IntPtr pNewtonWorld,
			IntPtr pCcollision);
	}
}
