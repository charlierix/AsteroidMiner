using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

//This came out of a forum on the newton site, it's written against 1.53
//namespace NewtonTest
//{
//    static public class Program
//    {
//        static public void Main()
//        {
//            int World = Newton.World.Create(0, 0);
//            Newton.World.SetSolverModel(World, 8);
//            Newton.World.SetFrictionModel(World, 1);

//            int col = Newton.Collision.CreateBox(World, 2, 2, 2, null);
//            int body = Newton.Body.Create(World, col);
//            Newton.Body.SetMassMatrix(body, 1, 1, 1, 1);
//            Matrix mat = Matrix.Identity;
//            Newton.Body.SetMatrix(body, ref mat);
//            Vector3 vec = new Vector3(10, 20, 30);
//            Newton.Body.AddTorque(body, ref vec);
//            while (true)
//            {
//                Matrix matrix = Matrix.Zero;
//                Newton.World.Update(World, .01f);
//                Newton.Body.GetMatrix(body, ref matrix);
//                Console.WriteLine(matrix.M12 + " " + matrix.M13 + " " + matrix.M14 + " " + matrix.M11);
//                Console.WriteLine(matrix.M22 + " " + matrix.M23 + " " + matrix.M24 + " " + matrix.M21);
//                Console.WriteLine(matrix.M32 + " " + matrix.M33 + " " + matrix.M34 + " " + matrix.M31);
//                Console.WriteLine(matrix.M42 + " " + matrix.M43 + " " + matrix.M44 + " " + matrix.M41);
//            }
//            Newton.World.Destroy(World);
//        }
//    }

//    public struct NewtonHingeSliderUpdateDesc
//    {
//        public float accel;
//        public float minFriction;
//        public float maxFriction;
//        public float timestep;
//    };

//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void SetTransformCB(int body, ref Matrix matrix);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void SetForceAndTorqueCB(int body);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void CreateTreeCollision(int bodyWithTreeCollision, int body, float[] vertex, int vertexstrideInBytes, int indexCount, int[] indexArray);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate int MaterialSetCollisionBegin(int material, int body0, int body1);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate int MaterialSetCollisionProcess(int material, int contact);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void MaterialSetCollisionEnd(int material);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void BodyLeaveWorld(int body);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void BodyIterator(int body);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate float WorldRayCastCB(int body, ref Vector3 normal, int collisionID, [MarshalAs(UnmanagedType.IUnknown)] object userData, float intersetParam);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void CollisionIteratorCB(int body, int vertexCount, IntPtr faceVert, int ID);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate int HingeJointCB(int joint, ref NewtonHingeSliderUpdateDesc desc);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate int SliderJointCB(int joint, ref NewtonHingeSliderUpdateDesc desc);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate int UniversalJointCB(int joint, IntPtr desc);

//    static public class Newton
//    {
//        static public class World
//        {
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreate")]
//            static public extern int Create(int a, int b);
//            [DllImport("Newton.dll", EntryPoint = "NewtonGetGlobalScale")]
//            static public extern float GetGlobalScale(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonSetSolverModel")]
//            static public extern void SetSolverModel(int a, int b);
//            [DllImport("Newton.dll", EntryPoint = "NewtonSetFrictionModel")]
//            static public extern void SetFrictionModel(int a, int b);
//            [DllImport("Newton.dll", EntryPoint = "NewtonUpdate")]
//            static public extern void Update(int newtonWorld, float timestep);
//            [DllImport("Newton.dll", EntryPoint = "NewtonWorldCollide")]
//            static public extern void Collide(int newtonWorld, int maxSize, int collisionA, ref Matrix offsetA, int collisionB, ref Matrix offsetB, float[] contacts, float[] normals, float[] penetration);
//            [DllImport("Newton.dll", EntryPoint = "NewtonDestroy")]
//            static public extern void Destroy(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonSetMinimumFrameRate")]
//            static public extern void SetMinimumFrameRate(int newtonWorld, float frameRate);
//            [DllImport("Newton.dll", EntryPoint = "NewtonGetTimeStep")]
//            static public extern float GetTimeStep(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonDestroyAllBodies")]
//            static public extern void DestroyAllBodies(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonSetWorldSize")]
//            static public extern void SetSize(int newtonWorld, ref Vector3 min, ref Vector3 max);
//            [DllImport("Newton.dll", EntryPoint = "NewtonSetBodyLeaveWorldEvent")]
//            static public extern void SetBodyLeaveWorldEvent(int newtonWorld, BodyLeaveWorld cb);
//            [DllImport("Newton.dll", EntryPoint = "NewtonWorldFreezeBody")]
//            static public extern void FreezeBody(int newtonWorld, int body);
//            [DllImport("Newton.dll", EntryPoint = "NewtonWorldUnfreezeBody")]
//            static public extern void UnfreezeBody(int newtonWorld, int body);
//            [DllImport("Newton.dll", EntryPoint = "NewtonWorldForEachBodyDo")]
//            static public extern void ForEachBodyDo(int newtonWorld, BodyIterator cb);
//            [DllImport("Newton.dll", EntryPoint = "NewtonGetVersion")]
//            static public extern int GetVersion(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonWorldSetUserData")]
//            static public extern void SetUserData(int bodyPtr, [MarshalAs(UnmanagedType.IUnknown)] object o);
//            [DllImport("Newton.dll", EntryPoint = "NewtonWorldGetUserData")]
//            [return: MarshalAs(UnmanagedType.IUnknown)]
//            static public extern object GetUserData(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonWorldRayCast")]
//            static public extern void RayCast(int newtonWorld, ref Vector3 p0, ref Vector3 p1, WorldRayCastCB cb, [MarshalAs(UnmanagedType.IUnknown)] object userData);
//        }
//        static public class Material
//        {
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetDefaultGroupID")]
//            static public extern int GetDefaultGroupID(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialCreateGroupID")]
//            static public extern int CreateGroupID(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialDestroyAllGroupID")]
//            static public extern void DestroyAllGroupID(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetDefaultSoftness")]
//            static public extern void SetDefaultSoftness(int newtonWorld, int id0, int id1, float softnessCoef);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetDefaultElasticity")]
//            static public extern void SetDefaultElasticity(int newtonWorld, int id0, int id1, float elasticCoef);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetDefaultCollidable")]
//            static public extern void SetDefaultCollidable(int newtonWorld, int id0, int id1, int state);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetDefaultFriction")]
//            static public extern void SetDefaultFriction(int newtonWorld, int id0, int id1, float staticFriction, float kineticFriction);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetCollisionCallback")]
//            static public extern void SetCollisionCallback(int newtonWorld, int id0, int id1, [MarshalAs(UnmanagedType.IUnknown)] object UserData, MaterialSetCollisionBegin cb1, MaterialSetCollisionProcess cb2, MaterialSetCollisionEnd cb3);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetUserData ")]
//            static public extern int GetUserData(int newtonWorld, int id0, int id1);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialDisableContact")]
//            static public extern void DisableContact(int materialHandle);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetMaterialPairUserData")]
//            [return: MarshalAs(UnmanagedType.IUnknown)]
//            static public extern object GetMaterialPairUserData(int materialHandle);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetContactFaceAttribute")]
//            static public extern uint GetContactFaceAttribute(int materialHandle);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetCurrentTimestep")]
//            static public extern float GetCurrentTimestep(int materialHandle);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetContactNormalSpeed")]
//            static public extern float GetContactNormalSpeed(int materialHandle, int contactHandle);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetContactTangentSpeed")]
//            static public extern float GetContactTangentSpeed(int materialHandle, int contactHandle, int index);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetContactPositionAndNormal")]
//            static public extern void GetContactPositionAndNormal(int materialHandle, ref Vector3 position, ref Vector3 normal);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetContactForce")]
//            static public extern void GetContactForce(int materialHandle, ref Vector3 force);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetContactTangentDirections")]
//            static public extern void GetContactTangentDirections(int materialHandle, ref Vector3 dir0, ref Vector3 dir1);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialGetBodyCollisionID")]
//            static public extern uint GetBodyCollisionID(int materialHandle, int body);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetContactSoftness")]
//            static public extern void SetContactSoftness(int materialHandle, float softness);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetContactElasticity")]
//            static public extern void SetContactElasticity(int materialHandle, float restitution);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetContactFrictionState")]
//            static public extern void SetContactFrictionState(int materialHandle, int state, int index);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetContactStaticFrictionCoef")]
//            static public extern void SetContactStaticFrictionCoef(int materialHandle, float coef, int index);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetContactKineticFrictionCoef")]
//            static public extern void SetContactKineticFrictionCoef(int materialHandle, float coef, int index);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialSetContactTangentAcceleration")]
//            static public extern void SetContactTangentAcceleration(int materialHandle, float accel, int index);
//            [DllImport("Newton.dll", EntryPoint = "NewtonMaterialContactRotateTangentDirections")]
//            static public extern void ContactRotateTangentDirections(int materialHandle, ref Vector3 alignVector);
//        }
//        static public class Collision
//        {
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateNull")]
//            static public extern int CreateNull(int newtonWorld, float dx, float dy, float dz, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateBox")]
//            static public extern int CreateBox(int newtonWorld, float dx, float dy, float dz, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateBox")]
//            static public extern int CreateBox(int newtonWorld, float dx, float dy, float dz, float[] matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateSphere")]
//            static public extern int CreateSphere(int newtonWorld, float rx, float ry, float rz, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateSphere")]
//            static public extern int CreateSphere(int newtonWorld, float rx, float ry, float rz, float[] matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateCone")]
//            static public extern int CreateCone(int newtonWorld, float radius, float height, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateCone")]
//            static public extern int CreateCone(int newtonWorld, float radius, float height, float[] matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateCapsule")]
//            static public extern int CreateCapsule(int newtonWorld, float radius, float height, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateCapsule")]
//            static public extern int CreateCapsule(int newtonWorld, float radius, float height, float[] matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateCylinder")]
//            static public extern int CreateCylinder(int newtonWorld, float radius, float height, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateCylinder")]
//            static public extern int CreateCylinder(int newtonWorld, float radius, float height, float[] matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateChamferCylinder")]
//            static public extern int CreateChamferCylinder(int newtonWorld, float radius, float height, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateChamferCylinder")]
//            static public extern int CreateChamferCylinder(int newtonWorld, float radius, float height, float[] matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateConvexHull")]
//            static public extern int CreateConvexHull(int newtonWorld, int count, float[] vertexCloud, int strideInBytes, ref Matrix offsetMatrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateConvexHullModifier")]
//            static public extern int CreateConvexHullModifier(int newtonWorld, int convexHullCollision);
//            [DllImport("Newton.dll", EntryPoint = "NewtonConvexHullModifierGetMatrix ")]
//            static public extern void ConvexHullModifierGetMatrix(int convexHullCollision, ref Matrix matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonConvexHullModifierSetMatrix ")]
//            static public extern void ConvexHullModifierSetMatrix(int convexHullCollision, ref Matrix matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateCompoundCollision")]
//            static public extern int CreateCompoundCollision(int newtonWorld, int count, int[] collisionPrimitiveArray);
//            [DllImport("Newton.dll", EntryPoint = "NewtonConvexCollisionSetUserID")]
//            static public extern void ConvexCollisionSetUserID(int newtonWorld, uint id);
//            [DllImport("Newton.dll", EntryPoint = "NewtonConvexCollisionGetUserID")]
//            static public extern uint ConvexCollisionSetUserID(int newtonWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateTreeCollision")]
//            static public extern int CreateTreeCollision(int bodyPtr, CreateTreeCollision cb);
//            [DllImport("Newton.dll", EntryPoint = "NewtonTreeCollisionBeginBuild")]
//            static public extern void TreeCollisionBeginBuild(int treeCollision);
//            [DllImport("Newton.dll", EntryPoint = "NewtonTreeCollisionAddFace")]
//            static public extern void TreeCollisionAddFace(int bodyPtr, int vertexCount, float[] vertexPtr, int strideInBytes, int faceAttribute);
//            [DllImport("Newton.dll", EntryPoint = "NewtonTreeCollisionEndBuild")]
//            static public extern void TreeCollisionEndBuild(int treeCollision, int optimize);
//            [DllImport("Newton.dll", EntryPoint = "NewtonTreeCollisionGetFaceAtribute")]
//            static public extern void TreeCollisionGetFaceAtribute(int treeCollision, int[] faceIndexArray);
//            [DllImport("Newton.dll", EntryPoint = "NewtonReleaseCollision")]
//            static public extern void ReleaseCollision(int newtonWorld, int collisionPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCollisionCalculateAABB")]
//            static public extern void CalculateAABB(int collisionPtr, ref Matrix offsetMatrix, ref Vector3 min, ref Vector3 max);
//            [DllImport("Newton.dll", EntryPoint = "NewtonCollisionRayCast")]
//            static public extern float RayCast(int collisionPtr, ref Vector3 p0, ref Vector3 p1, ref Vector3 Normal, int[] attribute);
//        }
//        static public class Body
//        {
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateBody")]
//            static public extern int Create(int newtonWorld, int collisionPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetUserData")]
//            static public extern void SetUserData(int bodyPtr, [MarshalAs(UnmanagedType.IUnknown)] object o);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetUserData")]
//            [return: MarshalAs(UnmanagedType.IUnknown)]
//            static public extern object GetUserData(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetWorld")]
//            static public extern int GetWorld(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetTransformCallback")]
//            static public extern void SetTransformCallback(int bodyPtr, SetTransformCB cb);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetForceAndTorqueCallback")]
//            static public extern void SetForceAndTorqueCallback(int bodyPtr, SetForceAndTorqueCB cb);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetMassMatrix")]
//            static public extern void SetMassMatrix(int bodyPtr, float mass, float Ixx, float Iyy, float Izz);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetMassMatrix")]
//            static public extern void GetMassMatrix(int bodyPtr, out float mass, out float Ixx, out float Iyy, out float Izz);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetInvMass")]
//            static public extern void GetInvMass(int bodyPtr, out float mass, out float Ixx, out float Iyy, out float Izz);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetMatrix")]
//            static public extern void SetMatrix(int bodyPtr, ref Matrix matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetMatrixRecursive")]
//            static public extern void SetMatrixRecursive(int bodyPtr, ref Matrix matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetMatrix")]
//            static public extern void GetMatrix(int bodyPtr, ref Matrix matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetForce")]
//            static public extern void SetForce(int bodyPtr, ref Vector3 force);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyAddForce")]
//            static public extern void AddForce(int bodyPtr, ref Vector3 force);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetForce")]
//            static public extern void GetForce(int bodyPtr, ref Vector3 force);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetTorque")]
//            static public extern void SetTorque(int bodyPtr, ref Vector3 torque);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyAddTorque")]
//            static public extern void AddTorque(int bodyPtr, ref Vector3 torque);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetTorque")]
//            static public extern void GetTorque(int bodyPtr, ref Vector3 torque);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetTotalVolume")]
//            static public extern float GetTotalVolume(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyAddBuoyancyForce")]
//            static public extern void AddBuoyancyForce(int bodyPtr, float fluidDensity, float fluidLinearViscosity, float fluidAngularViscosity, float[] gravityVector, int buoyancyPlane);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetCollision")]
//            static public extern void SetCollision(int bodyPtr, int collision);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyCoriolisForcesMode")]
//            static public extern void CoriolisForcesMode(int bodyPtr, int mode);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetCollision")]
//            static public extern int GetCollision(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetMaterialGroupID")]
//            static public extern void SetMaterialGroupID(int bodyPtr, int materialID);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetMaterialGroupID")]
//            static public extern int GetMaterialGroupID(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetJointRecursiveCollision")]
//            static public extern void SetJointRecursiveCollision(int bodyPtr, int state);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetJointRecursiveCollision")]
//            static public extern int GetJointRecursiveCollision(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetAutoFreeze")]
//            static public extern void SetAutoFreeze(int bodyPtr, int state);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetAutoFreeze")]
//            static public extern int GetAutoFreeze(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetSleepingState")]
//            static public extern int GetSleepingState(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetFreezeTreshold")]
//            static public extern void SetFreezeTreshold(int bodyPtr, float freezeSpeedMag2, float freezeOmegaMag2, int framesCount);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetFreezeTreshold")]
//            static public extern void GetFreezeTreshold(int bodyPtr, ref float freezeSpeedMag2, ref float freezeOmegaMag2);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetAABB")]
//            static public extern void GetAABB(int bodyPtr, ref Vector3 min, ref Vector3 max);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetVelocity")]
//            static public extern void SetVelocity(int bodyPtr, ref Vector3 velocity);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetVelocity")]
//            static public extern void GetVelocity(int bodyPtr, ref Vector3 velocity);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetOmega")]
//            static public extern void SetOmega(int bodyPtr, ref Vector3 omega);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetOmega")]
//            static public extern void GetOmega(int bodyPtr, ref Vector3 omega);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetLinearDamping")]
//            static public extern void SetLinearDamping(int bodyPtr, float linearDamp);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetLinearDamping")]
//            static public extern float GetLinearDamping(int bodyPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodySetAngularDamping")]
//            static public extern void SetAngularDamping(int bodyPtr, ref Vector3 angularDamp);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyGetAngularDamping")]
//            static public extern void GetAngularDamping(int bodyPtr, ref Vector3 angularDamp);
//            [DllImport("Newton.dll", EntryPoint = "NewtonBodyForEachPolygonDo")]
//            static public extern void ForEachPolygonDo(int bodyPtr, CollisionIteratorCB cb);
//            [DllImport("Newton.dll", EntryPoint = "NewtonAddBodyImpulse")]
//            static public extern void AddImpulse(int bodyPtr, ref Vector3 pointDeltaVeloc, ref Vector3 pointPosit);
//            [DllImport("Newton.dll", EntryPoint = "NewtonGetEulerAngle ")]
//            static public extern void GetEulerAngle(ref Matrix matrix, ref Vector3 angles);
//            [DllImport("Newton.dll", EntryPoint = "NewtonSetEulerAngle ")]
//            static public extern void SetEulerAngle(ref Matrix matrix, ref Vector3 angles);
//        }
//        static public class Joint
//        {
//            static public class Ball
//            {
//                [DllImport("Newton.dll", EntryPoint = "NewtonConstraintCreateBall")]
//                static public extern int Create(int world, ref Vector3 pivotPoint, int childBody, int parentBody);
//                [DllImport("Newton.dll", EntryPoint = "NewtonBallSetConeLimits")]
//                static public extern void SetConeLimits(int bodyPtr, ref Vector3 pin, float maxConeAngle, float maxTwistAngle);
//                [DllImport("Newton.dll", EntryPoint = "NewtonBallGetJointAngle")]
//                static public extern void GetAngle(int jointPtr, ref Vector3 angle);
//                [DllImport("Newton.dll", EntryPoint = "NewtonBallGetJointOmega")]
//                static public extern void GetOmega(int jointPtr, ref Vector3 omega);
//                [DllImport("Newton.dll", EntryPoint = "NewtonBallGetJointForce")]
//                static public extern void GetForce(int jointPtr, ref Vector3 force);
//            }
//            static public class Hinge
//            {
//                [DllImport("Newton.dll", EntryPoint = "NewtonConstraintCreateHinge")]
//                static public extern int Create(int bodyPtr, ref Vector3 pivotPoint, ref Vector3 pinDir, int childBody, int parentBody);
//                [DllImport("Newton.dll", EntryPoint = "NewtonHingeGetJointAngle")]
//                static public extern float GetAngle(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonHingeGetJointOmega")]
//                static public extern float GetOmega(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonHingeGetJointForce")]
//                static public extern void GetForce(int jointPtr, ref Vector3 force);
//                [DllImport("Newton.dll", EntryPoint = "NewtonHingeCalculateStopAlpha")]
//                static public extern float CalculateStopAlpha(int jointPtr, ref NewtonHingeSliderUpdateDesc desc, float angleLimit);
//                [DllImport("Newton.dll", EntryPoint = "NewtonHingeSetUserCallback")]
//                static public extern void SetUserCallback(int jointPtr, HingeJointCB callback);
//            }
//            static public class Slider
//            {
//                [DllImport("Newton.dll", EntryPoint = "NewtonConstraintCreateSlider")]
//                static public extern int Create(int bodyPtr, ref Vector3 pivotPoint, ref Vector3 pinDir, int childBody, int parentBody);
//                [DllImport("Newton.dll", EntryPoint = "NewtonSliderGetJointPosit")]
//                static public extern float GetPosit(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonSliderGetJointVeloc")]
//                static public extern float GetVeloc(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonSliderGetJointForce")]
//                static public extern void GetForce(int jointPtr, ref Vector3 force);
//                [DllImport("Newton.dll", EntryPoint = "NewtonSliderSetUserCallback")]
//                static public extern void SetUserCallback(int jointPtr, SliderJointCB callback);
//                [DllImport("Newton.dll", EntryPoint = "NewtonSliderCalculateStopAccel")]
//                static public extern float CalculateStopAccel(int jointPtr, ref NewtonHingeSliderUpdateDesc desc, float angleLimit);
//            }
//            static public class Corkscrew
//            {
//                [DllImport("Newton.dll", EntryPoint = "NewtonConstraintCreateCorkscrew")]
//                static public extern void Create(int bodyPtr, float[] pivotPoint, float[] pinDir, int childBody, int parentBody);
//                [DllImport("Newton.dll", EntryPoint = "NewtonCorkscrewGetJointPosit ")]
//                static public extern float GetPosit(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonCorkscrewGetJointVeloc")]
//                static public extern float GetVeloc(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonCorkscrewGetJointAngle ")]
//                static public extern float GetAngle(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonCorkscrewGetJointOmega")]
//                static public extern float GetOmega(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonCorkscrewGetJointForce")]
//                static public extern void GetForce(int jointPtr, ref Vector3 force);
//            }
//            static public class Universal
//            {
//                [DllImport("Newton.dll", EntryPoint = "NewtonConstraintCreateUniversal")]
//                static public extern int Create(int nWorld, ref Vector3 pivotPoint, ref Vector3 pinDir0, ref Vector3 pinDir1, int childBody, int parentBody);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalGetJointAngle0")]
//                static public extern float GetAngle0(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalGetJointAngle1")]
//                static public extern float GetAngle1(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalGetJointOmega0")]
//                static public extern float GetOmega0(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalGetJointOmega1")]
//                static public extern float GetOmega1(int jointPtr);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalGetJointForce")]
//                static public extern void GetForce(int jointPtr, ref Vector3 force);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalSetUserCallback")]
//                static public extern void SetUserCallback(int jointPtr, UniversalJointCB callback);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalCalculateStopAlpha0")]
//                static public extern float CalculateStopAlpha0(int jointPtr, IntPtr desc, float angleLimit);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUniversalCalculateStopAlpha1")]
//                static public extern float CalculateStopAlpha1(int jointPtr, IntPtr desc, float angleLimit);
//            }
//            static public class UpVector
//            {
//                [DllImport("Newton.dll", EntryPoint = "NewtonConstraintCreateUpVector")]
//                static public extern int Create(int nWorld, ref Vector3 pinDir, int body);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUpVectorGetPin")]
//                static public extern void GetPin(int jointPtr, ref Vector3 pin);
//                [DllImport("Newton.dll", EntryPoint = "NewtonUpVectorSetPin")]
//                static public extern void SetPin(int jointPtr, ref Vector3 pin);
//            }

//            [DllImport("Newton.dll", EntryPoint = "NewtonJointSetUserData")]
//            static public extern void SetUserData(int jointPtr, [MarshalAs(UnmanagedType.IUnknown)] object o);
//            [DllImport("Newton.dll", EntryPoint = "NewtonJointGetUserData")]
//            [return: MarshalAs(UnmanagedType.IUnknown)]
//            static public extern object GetUserData(int jointPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonJointSetCollisionState")]
//            static public extern void SetCollisionState(int jointPtr, int state);
//            [DllImport("Newton.dll", EntryPoint = "NewtonJointGetCollisionState")]
//            static public extern int GetCollisionState(int jointPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonJointSetStiffness")]
//            static public extern void SetStiffness(int jointPtr, float stifness);
//            [DllImport("Newton.dll", EntryPoint = "NewtonJointGetStiffness")]
//            static public extern float GetStiffness(int jointPtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonDestroyJoint")]
//            static public extern void Destroy(int nWorld, int jointPtr);
//        }
//        static public class Ragdoll
//        {
//            [DllImport("Newton.dll", EntryPoint = "NewtonCreateRagDoll")]
//            static public extern int Create(int nWorld);
//            [DllImport("Newton.dll", EntryPoint = "NewtonDestroyRagDoll")]
//            static public extern void Destroy(int nWorld, int ragDoll);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollBegin")]
//            static public extern void Begin(int ragDoll);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollEnd")]
//            static public extern void End(int ragDoll);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollAddBone")]
//            static public extern void AddBone(int ragDoll, int parentBone, [MarshalAs(UnmanagedType.IUnknown)] object o, float mass, ref Matrix matrix, int boneCollision, ref Vector3 size);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollBoneGetUserData")]
//            [return: MarshalAs(UnmanagedType.IUnknown)]
//            static public extern object BoneGetUserData(int ragdollBonePtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollBoneSetID")]
//            static public extern void BoneSetID(int ragdollBonePtr, int id);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollFindBone")]
//            static public extern int FindBone(int ragdollBonePtr, int id);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollBoneGetBody")]
//            static public extern int BoneGetBody(int ragdollBonePtr);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollBoneSetLimits")]
//            static public extern void BoneSetLimits(int ragdollBonePtr, ref Vector3 coneDir, float minConeAngle, float maxConeAngle, float maxTwistAngle, float[] lateralConeDir, float negativeBilateralConeAngle, float positiveBilateralConeAngle);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollBoneGetLocalMatrix")]
//            static public extern void BoneGetLocalMatrix(int ragdollBonePtr, ref Matrix matrix);
//            [DllImport("Newton.dll", EntryPoint = "NewtonRagDollBoneGetGlobalMatrix")]
//            static public extern void BoneGetGlobalMatrix(int ragdollBonePtr, ref Matrix matrix);
//        }
//    }
//}