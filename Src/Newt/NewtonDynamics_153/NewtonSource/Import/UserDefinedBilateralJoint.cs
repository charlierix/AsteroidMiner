using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateUserJoint(IntPtr pNewtonWorld,
			int pMaxDOF,
			NewtonUserBilateral pCallback,
			IntPtr pChildBody,
			IntPtr pParentBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointAddLinearRow(IntPtr pJoint,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPivot0,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPivot1,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pdir);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointAddAngularRow(IntPtr pJoint,
			float pRelativeAngle,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pDir);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointAddGeneralRow(IntPtr pJoint,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 6)]float[] pJacobian0,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 6)]float[] pJacobian1);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointSetRowMinimumFriction(IntPtr pJoint,
			float pFriction);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointSetRowMaximumFriction(IntPtr pJoint,
			float pFriction);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointSetRowAcceleration(IntPtr pJoint,
			float pAcceleration);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointSetRowSpringDamperAcceleration(IntPtr pJoint,
			float pSpringK,
			float pSpringD);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUserJointSetRowStiffness(IntPtr pJoint,
			float pStiffness);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonUserJointGetRowForce(IntPtr pJoint,
			int pRow);
	}
}
