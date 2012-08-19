using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public class CTransformUtility
	{
		public static Vector3D MatrixToEulerAngle(Matrix3D pMatrix)
		{
			NewtonVector3 aNewtonVector3 = new NewtonVector3( new Vector3D() );
			Newton.NewtonGetEulerAngle(new NewtonMatrix(pMatrix).NWMatrix,
				aNewtonVector3.NWVector3);
			return aNewtonVector3.ToDirectX();
		}

		public static Matrix3D EulerAngleToMatrix(Vector3D pEulersAngles)
		{
			NewtonMatrix aNewtonMatrix = new NewtonMatrix(Matrix3D.Identity);
			Newton.NewtonSetEulerAngle(new NewtonVector3(pEulersAngles).NWVector3,
				aNewtonMatrix.NWMatrix);
			return aNewtonMatrix.ToDirectX();
		}
	}
}
