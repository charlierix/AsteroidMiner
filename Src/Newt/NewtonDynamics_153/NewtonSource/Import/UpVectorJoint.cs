using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateUpVector(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPinDir,
			IntPtr pNewtonBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUpVectorGetPin(IntPtr pUpVector,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPin);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUpVectorSetPin(IntPtr pUpVector,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPin);
	}
}
