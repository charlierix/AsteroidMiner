using System;
using System.Runtime.InteropServices;


namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateBall(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPivotPoint,
			IntPtr pChildBody,
			IntPtr pParentBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBallSetUserCallback(IntPtr pNewtonBall,
			NewtonBall pCallback);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBallGetJointAngle(IntPtr pNewtonBall,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pAngle);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBallGetJointOmega(IntPtr pNewtonBall,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pOmega);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBallGetJointForce(IntPtr pNewtonBall,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonBallSetConeLimits(IntPtr pNewtonBall,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPin,
			float pMaxConeAngle, float pMaxTwistAngle);
	}
}
