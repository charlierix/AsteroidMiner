using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateCorkscrew(IntPtr pNewtonWorld, 
			float[] pPivotPoint,
			float[] pPinDir,
			IntPtr pChildBody,
			IntPtr pParentBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonCorkscrewSetUserCallback(IntPtr pCorkscrew,
			NewtonCorkscrew pCallback);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonCorkscrewGetJointPosit(IntPtr pCorkscrew);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonCorkscrewGetJointAngle(IntPtr pCorkscrew);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonCorkscrewGetJointVeloc(IntPtr pCorkscrew);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonCorkscrewGetJointOmega(IntPtr pCorkscrew);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonCorkscrewGetJointForce(IntPtr pCorkscrew,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonCorkscrewCalculateStopAlpha(IntPtr pCorkscrew,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc,
			float pAngle);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonCorkscrewCalculateStopAccel(IntPtr pCorkscrew,
			[MarshalAs(UnmanagedType.LPStruct)]NewtonHingeSliderUpdateDesc pDesc,
			float position);
	}
}
