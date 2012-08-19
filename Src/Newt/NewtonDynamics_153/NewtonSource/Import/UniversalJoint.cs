using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateUniversal(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPivotPoint,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPinDir0,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPinDir1,
			IntPtr pChildBody,
			IntPtr pParentBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUniversalSetUserCallback(IntPtr pUniversal,
			NewtonUniversal pCallback);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonUniversalGetJointAngle0(IntPtr pUniversal);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonUniversalGetJointAngle1(IntPtr pUniversal);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonUniversalGetJointOmega0(IntPtr pUniversal);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonUniversalGetJointOmega1(IntPtr pUniversal);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUniversalGetJointForce(IntPtr pUniversal,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonUniversalCalculateStopAlpha0 (IntPtr pUniversal,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc,
			float pAngle);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonUniversalCalculateStopAlpha1 (IntPtr pUniversal,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc,
			float pAngle);
	}
}
