using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialDisableContact(IntPtr pMaterial);
		
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonMaterialGetCurrentTimestep(IntPtr pMaterial);
	
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonMaterialGetMaterialPairUserData(IntPtr pMaterial);
		
		[DllImport("Newton_153.dll")]
		internal static extern uint NewtonMaterialGetContactFaceAttribute(IntPtr pMaterial);
	
		[DllImport("Newton_153.dll")]
		internal static extern uint NewtonMaterialGetBodyCollisionID(IntPtr pMaterial,
			IntPtr pNewtonBody);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonMaterialGetContactNormalSpeed(IntPtr pMaterial,
			IntPtr pContact);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialGetContactForce(IntPtr pMaterial,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pForce);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialGetContactPositionAndNormal(IntPtr pMaterial,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPosition,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pNormal);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialGetContactTangentDirections(IntPtr pMaterial,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pDirection0,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pDirection);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonMaterialGetContactTangentSpeed(IntPtr pMaterial,
			IntPtr pContact,
			int pIndex);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactSoftness(IntPtr pMaterial,
			float pSoftness);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactElasticity(IntPtr pMaterial,
			float pRestitution);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactFrictionState(IntPtr pMaterial,
			int pState,
			int pIndex);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactStaticFrictionCoef(IntPtr pMaterial,
			float pCoef,
			int pIndex);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactKineticFrictionCoef(IntPtr pMaterial,
			float pCoef,
			int pIndex);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactNormalAcceleration(IntPtr pMaterial,
			float pAccel);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactNormalDirection(IntPtr pMaterial,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pDirectionVector);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialSetContactTangentAcceleration(IntPtr pMaterial,
			float pAccel,
			int pIndex);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonMaterialContactRotateTangentDirections(IntPtr pMaterial,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pDirectionVector);
	}
}
