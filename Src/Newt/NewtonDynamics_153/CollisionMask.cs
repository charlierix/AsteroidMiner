using System;
using System.Collections.Generic;
using System.ComponentModel;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows;

namespace Game.Newt.NewtonDynamics_153
{
    [TypeConverter(typeof(CollisionMaskConverter))]
    public abstract class CollisionMask : DependencyObject, IDisposable
    {
        private World _world;
        private CCollision _collision;
        private bool _isInitialised;

        public static CCollisionConvexPrimitives[] GetNewtonCollisions(ICollection<ConvexCollisionMask> collisions)
        {
            int i = 0;
            CCollisionConvexPrimitives[] handles = new CCollisionConvexPrimitives[collisions.Count];
            foreach (ConvexCollisionMask c in collisions)
            {
                handles[i++] = c.NewtonCollision;
            }
            return handles;
        }

        public CollisionMask()
        {
        }

        public CollisionMask(World world)
        {
            Initialise(world);
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_collision != null)
            {
                _collision.Release();
                _collision = null;
            }
        }

        #endregion

        public void Initialise(World world)
        {
            if (_isInitialised)
                return;

            if (world == null) throw new ArgumentNullException("world");

            _world = world;
            _collision = OnInitialise();
            OnInitialiseEnd();
            _isInitialised = true;
        }

        public World World
        {
            get { return _world; }
        }

        public CCollision NewtonCollision
        {
            get { return _collision; }
        }

        public bool IsInitialised
        {
            get { return _isInitialised; }
        }

        protected abstract CCollision OnInitialise();

        protected virtual void OnInitialiseEnd()
        {
        }
    }
}
