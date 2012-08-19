using System;
using System.Runtime.InteropServices;

namespace Game.Newt.NewtonDynamics_153
{
	internal partial class Newton
	{
		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonConstraintCreateVehicle(IntPtr pNewtonWorld,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pUpDir,
			IntPtr pNewtonBody);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleReset(IntPtr pVehicle);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleSetTireCallback(IntPtr pVehicle,
			NewtonVehicleTireUpdate pUpdate);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonVehicleAddTire(IntPtr pVehicle,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pLocalMatrix,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pPin,
			float pMass,
			float pWidth,
			float pRadius,
			float pSuspesionShock,
			float pSuspesionSpring,
			float pSuspesionLength,
			IntPtr pUserData,
			int pCollisionID);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleRemoveTire(IntPtr pVehicle,
			IntPtr pTireId);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonVehicleGetFirstTireID(IntPtr pVehicle);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonVehicleGetNextTireID(IntPtr pVehicle,
			IntPtr pTireId);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonVehicleTireIsAirBorne(IntPtr pVehicle,
			IntPtr pTireId);

		[DllImport("Newton_153.dll")]
		internal static extern int NewtonVehicleTireLostSideGrip(IntPtr pVehicle,
			IntPtr pTireId);
	
		[DllImport("Newton_153.dll")]
		internal static extern int NewtonVehicleTireLostTraction(IntPtr pVehicle,
			IntPtr pTireId);

		[DllImport("Newton_153.dll")]
		internal static extern IntPtr NewtonVehicleGetTireUserData(IntPtr pVehicle,
			IntPtr pTireId);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonVehicleGetTireOmega(IntPtr pVehicle,
			IntPtr pTireId);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonVehicleGetTireNormalLoad(IntPtr pVehicle,
			IntPtr pTireId);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonVehicleGetTireSteerAngle(IntPtr pVehicle,
			IntPtr pTireId);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonVehicleGetTireLateralSpeed(IntPtr pVehicle,
			IntPtr pTireId);
	
		[DllImport("Newton_153.dll")]
		internal static extern float NewtonVehicleGetTireLongitudinalSpeed(IntPtr pVehicle,
			IntPtr pTireId);
	
		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleGetTireMatrix(IntPtr pVehicle,
			IntPtr pTireId,
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);


		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleSetTireTorque(IntPtr pVehicle,
			IntPtr pTireId,
			float pTorque);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleSetTireSteerAngle(IntPtr pVehicle,
			IntPtr pTireId,
			float pAngle);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleSetTireMaxSideSleepSpeed(IntPtr pVehicle,
			IntPtr pTireId,
			float pSpeed);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleSetTireSideSleepCoeficient(IntPtr pVehicle,
			IntPtr pTireId,
			float pCoeficient);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleSetTireMaxLongitudinalSlideSpeed(IntPtr pVehicle,
			IntPtr pTireId,
			float pSpeed);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleSetTireLongitudinalSlideCoeficient(IntPtr pVehicle,
			IntPtr pTireId,
			float pCoeficient);

		[DllImport("Newton_153.dll")]
		internal static extern float NewtonVehicleTireCalculateMaxBrakeAcceleration(IntPtr pVehicle,
			IntPtr pTireId);

		[DllImport("Newton_153.dll")]
		internal static extern void NewtonVehicleTireSetBrakeAcceleration(IntPtr pVehicle,
			IntPtr pTireId,
			float pAccelaration,
			float pTorqueLimit);
	}
}
