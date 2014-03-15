using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Game.Newt.NewtonDynamics.Import;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics
{
    //TODO: Remove this
    public static class DisposeTests
    {
        public static void ShareHullsAcrossWorlds()
        {
            // Worlds
            Newton.NewtonSetMemorySystem(null, null);

            IntPtr world1 = Newton.NewtonCreate();
            Newton.NewtonSetSolverModel(world1, 1);		//adaptive
            Newton.NewtonCollisionDestructor callback1 = new Newton.NewtonCollisionDestructor(HullDestroyed);
            Newton.NewtonSetCollisionDestructor(world1, callback1);

            IntPtr world2 = Newton.NewtonCreate();
            Newton.NewtonSetSolverModel(world2, 1);
            Newton.NewtonCollisionDestructor callback2 = new Newton.NewtonCollisionDestructor(HullDestroyed);
            Newton.NewtonSetCollisionDestructor(world2, callback2);

            // Hulls
            IntPtr hull1a = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, null);
            IntPtr hull1b = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, null);		// handle is same as 1a

            IntPtr hull2a = Newton.NewtonCreateBox(world2, 1f, 1f, 1f, 0, null);
            IntPtr hull2b = Newton.NewtonCreateBox(world2, 1f, 2f, 1f, 0, null);

            // Dispose
            Newton.NewtonReleaseCollision(world1, hull1a);
            Newton.NewtonReleaseCollision(world1, hull1b);
            Newton.NewtonReleaseCollision(world1, hull1b);		// this just silently does nothing

            Newton.NewtonReleaseCollision(world2, hull2a);
            Newton.NewtonReleaseCollision(world2, hull2b);
        }
        public static void ShareHullsWithBodies()
        {
            // Worlds
            Newton.NewtonSetMemorySystem(null, null);

            IntPtr world1 = Newton.NewtonCreate();
            Newton.NewtonSetSolverModel(world1, 1);		//adaptive
            Newton.NewtonCollisionDestructor callback1 = new Newton.NewtonCollisionDestructor(HullDestroyed);
            Newton.NewtonSetCollisionDestructor(world1, callback1);

            // Hulls
            IntPtr hull1a = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, null);
            IntPtr hull1b = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, null);		// handle is same as 1a
            IntPtr hull1c = Newton.NewtonCreateBox(world1, 2f, 1f, 1f, 0, null);		// unique hull

            // Bodies
            IntPtr body1a = Newton.NewtonCreateBody(world1, hull1a, new NewtonMatrix(Matrix3D.Identity).Matrix);
            Newton.NewtonBodyDestructor bodyCallback1a = new Newton.NewtonBodyDestructor(BodyDestroyed);
            Newton.NewtonBodySetDestructorCallback(body1a, bodyCallback1a);

            //IntPtr body1b = Newton.NewtonCreateBody(world1, hull1b, new NewtonMatrix(Matrix3D.Identity).Matrix);
            //Newton.NewtonBodyDestructor bodyCallback1b = new Newton.NewtonBodyDestructor(BodyDestroyed);
            //Newton.NewtonBodySetDestructorCallback(body1b, bodyCallback1b);

            IntPtr body1c = Newton.NewtonCreateBody(world1, hull1c, new NewtonMatrix(Matrix3D.Identity).Matrix);
            Newton.NewtonBodyDestructor bodyCallback1c = new Newton.NewtonBodyDestructor(BodyDestroyed);
            Newton.NewtonBodySetDestructorCallback(body1c, bodyCallback1c);

            //NOTE: NewtonDestroyAllBodies will cause the events to fire.  But destroybody calls will throw an exception
            //Newton.NewtonDestroyAllBodies(world1);
            //Newton.NewtonDestroy(world1);

            // Dispose
            Newton.NewtonReleaseCollision(world1, hull1a);		// Whenever a hull is used in a body, it needs to be released
            Newton.NewtonReleaseCollision(world1, hull1b);
            Newton.NewtonReleaseCollision(world1, hull1c);

            Newton.NewtonDestroyBody(world1, body1a);
            Newton.NewtonDestroyBody(world1, body1a);
            //Newton.NewtonDestroyBody(world1, body1b);
            Newton.NewtonDestroyBody(world1, body1c);
        }
        public static void ShareHullsWithBodies_alt()
        {
            // Worlds
            Newton.NewtonSetMemorySystem(null, null);

            IntPtr world1 = Newton.NewtonCreate();
            Newton.NewtonSetSolverModel(world1, 1);		//adaptive
            Newton.NewtonCollisionDestructor callback1 = new Newton.NewtonCollisionDestructor(HullDestroyed);
            Newton.NewtonSetCollisionDestructor(world1, callback1);

            // Hulls
            IntPtr hull1a = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, null);
            //IntPtr hull1b = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, null);		// handle is same as 1a
            IntPtr hull1c = Newton.NewtonCreateBox(world1, 2f, 1f, 1f, 0, null);		// unique hull

            // Bodies
            IntPtr body1a = Newton.NewtonCreateBody(world1, hull1a, new NewtonMatrix(Matrix3D.Identity).Matrix);
            Newton.NewtonBodyDestructor bodyCallback1a = new Newton.NewtonBodyDestructor(BodyDestroyed);
            Newton.NewtonBodySetDestructorCallback(body1a, bodyCallback1a);

            IntPtr body1b = Newton.NewtonCreateBody(world1, hull1a, new NewtonMatrix(Matrix3D.Identity).Matrix);		// reusing hull1a
            Newton.NewtonBodyDestructor bodyCallback1b = new Newton.NewtonBodyDestructor(BodyDestroyed);
            Newton.NewtonBodySetDestructorCallback(body1b, bodyCallback1b);

            IntPtr body1c = Newton.NewtonCreateBody(world1, hull1c, new NewtonMatrix(Matrix3D.Identity).Matrix);
            Newton.NewtonBodyDestructor bodyCallback1c = new Newton.NewtonBodyDestructor(BodyDestroyed);
            Newton.NewtonBodySetDestructorCallback(body1c, bodyCallback1c);

            // Dispose
            Newton.NewtonReleaseCollision(world1, hull1a);
            Newton.NewtonReleaseCollision(world1, hull1a);		// this extra release is causing 1a to be destroyed after body1a is destroyed (but 1b is still around) - this should never happen in practice.  One hull shouldn't be shared across bodies without two calls to newtcreate hull
            Newton.NewtonReleaseCollision(world1, hull1c);

            Newton.NewtonDestroyBody(world1, body1a);
            Newton.NewtonDestroyBody(world1, body1b);
            Newton.NewtonDestroyBody(world1, body1c);
        }
        public static void CompundCollisionHull()
        {
            // Worlds
            Newton.NewtonSetMemorySystem(null, null);

            IntPtr world1 = Newton.NewtonCreate();
            Newton.NewtonSetSolverModel(world1, 1);		//adaptive
            Newton.NewtonCollisionDestructor callback1 = new Newton.NewtonCollisionDestructor(HullDestroyed);
            Newton.NewtonSetCollisionDestructor(world1, callback1);

            // Hulls
            IntPtr hull1a = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, new NewtonMatrix(new TranslateTransform3D(-.5d, -.5d, 0d).Value).Matrix);
            IntPtr hull1b = Newton.NewtonCreateBox(world1, 1f, 1f, 1f, 0, new NewtonMatrix(new TranslateTransform3D(.5d, .5d, 0d).Value).Matrix);
            IntPtr hullCompound = Newton.NewtonCreateCompoundCollision(world1, 2, new IntPtr[] { hull1a, hull1b }, 0);

            // Bodies
            //IntPtr body1a = Newton.NewtonCreateBody(world1, hull1a, new NewtonMatrix(Matrix3D.Identity).Matrix);
            //Newton.NewtonBodyDestructor bodyCallback1a = new Newton.NewtonBodyDestructor(BodyDestroyed);
            //Newton.NewtonBodySetDestructorCallback(body1a, bodyCallback1a);

            IntPtr body1Comp = Newton.NewtonCreateBody(world1, hullCompound, new NewtonMatrix(Matrix3D.Identity).Matrix);
            Newton.NewtonBodyDestructor bodyCallback1Comp = new Newton.NewtonBodyDestructor(BodyDestroyed);
            Newton.NewtonBodySetDestructorCallback(body1Comp, bodyCallback1Comp);

            IntPtr hullActual = Newton.NewtonBodyGetCollision(body1Comp);

            // Dispose
            Newton.NewtonReleaseCollision(world1, hullCompound);
            Newton.NewtonReleaseCollision(world1, hull1a);
            Newton.NewtonReleaseCollision(world1, hull1b);

            //Newton.NewtonDestroyBody(world1, body1a);
            Newton.NewtonDestroyBody(world1, body1Comp);
        }

        // Event Listeners
        private static void HullDestroyed(IntPtr newtonWorld, IntPtr collision)
        {
        }
        private static void BodyDestroyed(IntPtr body)
        {
        }
    }
}
