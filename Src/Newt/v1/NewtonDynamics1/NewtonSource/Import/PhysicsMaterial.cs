using System;
using System.Runtime.InteropServices;

namespace Game.Newt.v1.NewtonDynamics1
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonMaterialGetDefaultGroupID(IntPtr pNewtonWorld);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonMaterialCreateGroupID(IntPtr pNewtonWorld);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialDestroyAllGroupID(IntPtr pNewtonWorld);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetDefaultSoftness(IntPtr pNewtonWorld,
			int pId0,
			int pId1,
			float pValue);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetDefaultElasticity(IntPtr pNewtonWorld,
			int pId0,
			int pId1,
			float pElasticCoef);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetDefaultCollidable(IntPtr pNewtonWorld,
			int pId0,
			int pId1,
			int pState);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContinuousCollisionMode(IntPtr pNewtonWorld,
			int pId0,
			int pId1,
			int pState);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetDefaultFriction(IntPtr pNewtonWorld,
			int pId0,
			int pId1,
			float pStaticFriction,
			float pKineticFriction);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetCollisionCallback(IntPtr pNewtonWorld,
			int pId0,
			int pId1,
			IntPtr pUserData,
			NewtonContactBegin pBegin,
			NewtonContactProcess pProcess,
			NewtonContactEnd pEnd);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonMaterialGetUserData(IntPtr pNewtonWorld,
			int pId0,
			int pId1);
	}
}
