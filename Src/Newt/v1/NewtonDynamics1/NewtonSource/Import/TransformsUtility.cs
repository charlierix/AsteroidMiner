using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonGetEulerAngle(
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pEulersAngles);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSetEulerAngle(
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pEulersAngles,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
	}
}
