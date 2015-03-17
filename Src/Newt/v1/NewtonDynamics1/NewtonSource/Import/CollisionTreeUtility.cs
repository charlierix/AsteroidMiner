using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreateTreeCollision(IntPtr pNewtonWorld,
			NewtonTreeCollision pCallback);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonTreeCollisionBeginBuild(IntPtr pNewtonTreeCollision);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonTreeCollisionAddFace(IntPtr pNewtonTreeCollision,
			int pVertexCount,
            [MarshalAs(UnmanagedType.LPArray /* SizeConst = 180000*/)]float[] pVertexPtr,
			int pStrideInBytes,
			int pFaceAttribute);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonTreeCollisionEndBuild(IntPtr pNewtonTreeCollision,
			int pOptimize);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonTreeCollisionSerialize(IntPtr pNewtonTreeCollision,
			NewtonSerialize pSerializeFunction,
			int pSerializeHandle);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonCreateTreeCollisionFromSerialization(IntPtr pNewtonWorld,
			NewtonTreeCollision pCallback,
			NewtonDeserialize pDeserializeFunction,
			int pSerializeHandle);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonTreeCollisionGetFaceAtribute(IntPtr pNewtonTreeCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]int[] pFaceIndexArray);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonTreeCollisionSetFaceAtribute(IntPtr pNewtonTreeCollision,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]int[] pFaceIndexArray,
			int pAttribute);
	}
}
