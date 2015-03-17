using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateHinge(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivotPoint,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir,
			IntPtr pChildBody,
			IntPtr pParentBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonHingeSetUserCallback(IntPtr pHinge,
			NewtonHinge pCallback);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonHingeGetJointAngle(IntPtr pHinge);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonHingeGetJointOmega(IntPtr pHinge);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonHingeGetJointForce(IntPtr pHinge,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonHingeCalculateStopAlpha(IntPtr pHinge,
            [MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc, float pAngle);
	}
}
