using System.Windows.Media.Media3D;
using System.Collections.Generic;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
    // I don't see any reason for this to derive from CCollision
    public class CCollisionConvexPrimitives : CCollision
    {
        #region Constructor

        public CCollisionConvexPrimitives(CWorld pWorld)
            : base(pWorld)
        {
        }

        #endregion

        #region Methods

        //TODO:  These create methods should be static helper methods that return a CCollisionConvexPrimitives (or overloads of the constructor,
        // but that would make it hard to know what you're calling)

        public void CreateNull()
        {
            m_Handle = Newton.NewtonCreateNull(m_World.Handle);

            //CHashTables.Collision.Add(m_Handle, this);
        }

        public void CreateSphere(Vector3D pRadius, Matrix3D pOffsetMatrix)
        {
            NewtonMatrix aMatrix = new NewtonMatrix(pOffsetMatrix);
            m_Handle = Newton.NewtonCreateSphere(m_World.Handle, (float)pRadius.X, (float)pRadius.Y, (float)pRadius.Z, aMatrix.NWMatrix);

            CHashTables.Collision.Add(m_Handle, this);
        }

        public void CreateBox(Vector3D pSize, Matrix3D pOffsetMatrix)
        {
            NewtonMatrix aMatrix = new NewtonMatrix(pOffsetMatrix);
            m_Handle = Newton.NewtonCreateBox(m_World.Handle,
            (float)pSize.X, (float)pSize.Y, (float)pSize.Z, aMatrix.NWMatrix);

            CHashTables.Collision.Add(m_Handle, this);
        }

        public void CreateCone(float pRadius, float pHeight, Matrix3D pOffsetMatrix)
        {
            NewtonMatrix aMatrix = new NewtonMatrix(pOffsetMatrix);
            m_Handle = Newton.NewtonCreateCone(m_World.Handle,
                pRadius, pHeight, aMatrix.NWMatrix);

            CHashTables.Collision.Add(m_Handle, this);
        }

        public void CreateCapsule(float pRadius, float pHeight, Matrix3D pOffsetMatrix)
        {
            NewtonMatrix aMatrix = new NewtonMatrix(pOffsetMatrix);
            m_Handle = Newton.NewtonCreateCapsule(m_World.Handle,
                pRadius, pHeight, aMatrix.NWMatrix);

            CHashTables.Collision.Add(m_Handle, this);
        }

        public void CreateCylinder(float pRadius, float pHeight, Matrix3D pOffsetMatrix)
        {
            NewtonMatrix aMatrix = new NewtonMatrix(pOffsetMatrix);
            m_Handle = Newton.NewtonCreateCylinder(m_World.Handle,
                pRadius, pHeight, aMatrix.NWMatrix);

            CHashTables.Collision.Add(m_Handle, this);
        }

        public void CreateChamferCylinder(float pRadius, float pHeight, Matrix3D pOffsetMatrix)
        {
            NewtonMatrix aMatrix = new NewtonMatrix(pOffsetMatrix);
            m_Handle = Newton.NewtonCreateChamferCylinder(m_World.Handle,
                pRadius, pHeight, aMatrix.NWMatrix);

            CHashTables.Collision.Add(m_Handle, this);
        }

        public void CreateConvexHull(ICollection<Point3D> verticies, Matrix3D pOffsetMatrix)
        {
            float[,] aVertices = new float[verticies.Count, 3];

            int i = 0;
            foreach (Vector3D nextVertice in verticies)
            {
                aVertices[i, 0] = (float)nextVertice.X;
                aVertices[i, 1] = (float)nextVertice.Y;
                aVertices[i++, 2] = (float)nextVertice.Z;
            }

            NewtonMatrix aMatrix = new NewtonMatrix(pOffsetMatrix);
            m_Handle = Newton.NewtonCreateConvexHull(m_World.Handle,
                verticies.Count,
                aVertices,
                sizeof(float) * 3,
                aMatrix.NWMatrix);

            CHashTables.Collision.Add(m_Handle, this);
        }

        public CCollisionConvexPrimitives CreateConvexHullModifier()
        {
            CheckHandle();
            MakeUnique();
            CCollisionConvexPrimitives modifier = new CCollisionConvexPrimitives(m_World);
            modifier.m_Handle = Newton.NewtonCreateConvexHullModifier(m_World.Handle, m_Handle);

            CHashTables.Collision.Add(modifier.m_Handle, modifier);

            return modifier;
        }

        #endregion

        #region Properties

        public uint ConvexUserID
        {
            get
            {
                CheckHandle();
                return Newton.NewtonConvexCollisionGetUserID(m_Handle);
            }
            set
            {
                CheckHandle();
                Newton.NewtonConvexCollisionSetUserID(m_Handle, value);
            }
        }

        public float Volume
        {
            get
            {
                CheckHandle();
                return Newton.NewtonConvexCollisionCalculateVolume(m_Handle);
            }
        }

        public InertialMatrix CalculateInertialMatrix
        {
            get
            {
                CheckHandle();

                NewtonVector3 aInertia = new NewtonVector3(new Vector3D());
                NewtonVector3 aOrigin = new NewtonVector3(new Vector3D());

                Newton.NewtonConvexCollisionCalculateInertialMatrix(m_Handle,
                    aInertia.NWVector3,
                    aOrigin.NWVector3);

                return new InertialMatrix(aInertia.ToDirectX(), aOrigin.ToDirectX());
            }
        }

        #endregion
    }
}