using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonJointGetUserData(IntPtr pJoint);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonJointSetUserData(IntPtr pJoint,
			IntPtr pUserData);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonJointGetCollisionState(IntPtr pJoint);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonJointSetCollisionState(IntPtr pJoint,
			int pState);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonJointGetStiffness(IntPtr pJoint);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonJointSetStiffness(IntPtr pJoint,
			float pStiffness);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonDestroyJoint(IntPtr pNewtonWorld,
			IntPtr pJoint);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonJointSetDestructor(IntPtr pJoint,
			NewtonConstraintDestructor pDestructor);
	}
}
