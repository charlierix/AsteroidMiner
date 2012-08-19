using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateSlider(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pivotPoint,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pinDir,
			IntPtr pChildBody,
			IntPtr pParentBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSliderSetUserCallback(IntPtr pSlider,
			NewtonSlider pCallback);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonSliderGetJointPosit(IntPtr pSlider);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonSliderGetJointVeloc(IntPtr pSlider);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSliderGetJointForce(IntPtr pSlider,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonSliderCalculateStopAccel(IntPtr pSlider,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc,
			float pPosition);
	}
}
