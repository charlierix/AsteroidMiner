using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
        [DllImport("kernel32")]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonCreate(AllocMemory pAllocMemory, FreeMemory pFreeMemory);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonDestroy(IntPtr pNewtonWorld);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonDestroyAllBodies(IntPtr pNewtonWorld);




		[DllImport("Newton_153.dll")]
		internal static extern void NewtonUpdate(IntPtr pNewtonWorld, float pTimestep);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSetPlatformArchitecture(IntPtr pNewtonWorld, int pMode);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSetSolverModel(IntPtr pNewtonWorld, int pModel);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSetFrictionModel(IntPtr pNewtonWorld, int pModel);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonGetTimeStep(IntPtr pNewtonWorld);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSetMinimumFrameRate(IntPtr pNewtonWorld, float pFrameRate);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSetBodyLeaveWorldEvent(IntPtr pNewtonWorld,
			NewtonBodyLeaveWorld pCallback);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonSetWorldSize(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pMinPoint,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pMaxPoint);



		[DllImport("Newton_153.dll")]
		internal static extern void NewtonWorldFreezeBody(IntPtr pNewtonWorld, IntPtr pNewtonBody);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonWorldUnfreezeBody(IntPtr pNewtonWorld, IntPtr pNewtonBody);



		[DllImport("Newton_153.dll")]
		internal static extern void NewtonWorldForEachBodyDo(IntPtr pNewtonWorld,
			NewtonBodyIterator pCallback);



		[DllImport("Newton_153.dll")]
		internal static extern void NewtonWorldSetUserData(IntPtr pNewtonWorld, IntPtr pUserData);
	
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonWorldGetUserData(IntPtr pNewtonWorld);
		
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonWorldGetVersion(IntPtr pNewtonWorld);



		[DllImport("Newton_153.dll")]
		internal static extern void NewtonWorldRayCast(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p0,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] p1,
			NewtonWorldRayFilterCallback pWorldRayFilterCallback,
			IntPtr pUserData,
			NewtonWorldRayPrefilterCallback pWorldRayPrefilterCallback);



		// world utility functions
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonGetBodiesCount();

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonGetActiveBodiesCount();

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonGetActiveConstraintsCount();

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonGetGlobalScale(IntPtr pNewtonWorld);
	}
}
