using System;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public class CJointRagDollBone
	{
		#region Members

		protected IntPtr m_Handle;
		internal IntPtr Handle { get { return m_Handle; } }

		protected CWorld m_World;
		public CWorld World { get { return m_World; } }

		#endregion


		#region Constructor

		public CJointRagDollBone()
		{

		}

		#endregion

	
		#region Methods


		#endregion
	}
}


//public IntPtr NewtonRagDollBoneGetUserData(IntPtr pBone);

//public IntPtr NewtonRagDollBoneGetBody(IntPtr pBone);

//public void NewtonRagDollBoneSetID(IntPtr pBone, int pId);

//public void NewtonRagDollBoneSetLimits(IntPtr pBone,
//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pConeDir,
//    float pMinConeAngle,
//    float pMaxConeAngle,
//    float pMaxTwistAngle,
//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]float[] pBilateralConeDir,
//    float pNegativeBilateralConeAngle,
//    float pPositiveBilateralConeAngle);

//public int NewtonRagDollBoneGetChild(IntPtr pBone);

//public int NewtonRagDollBoneGetSibling(IntPtr pBone);

//public int NewtonRagDollBoneGetParent(IntPtr pBone);

//public void NewtonRagDollBoneGetLocalMatrix(IntPtr pBone,
//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
//public void NewtonRagDollBoneSetLocalMatrix(IntPtr pBone,
//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);

//public void NewtonRagDollBoneGetGlobalMatrix(IntPtr pBone,
//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
//public void NewtonRagDollBoneSetGlobalMatrix(IntPtr pBone,
//    [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]float[] pMatrix);
