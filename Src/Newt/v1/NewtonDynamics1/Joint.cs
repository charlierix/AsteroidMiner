using System;
using System.ComponentModel;
using Game.Newt.v1.NewtonDynamics1.Api;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;

namespace Game.Newt.v1.NewtonDynamics1
{
    public enum CollisionState
    {
        DisableCollisions = 0,
        EnableCollisions = 1
    }

    //TODO: add destroy notifications to attached bodies
    public abstract class Joint : DependencyObject, IDisposable, ISupportInitialize
    {
        protected static void OnConstructorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Joint joint = (Joint)d;

            joint.RebuildJoint();
            OnUnPausePropertyChanged(d, e);
        }

        #region CollisionStateProperty

        public static readonly DependencyProperty CollisionStateProperty =
            DependencyProperty.Register(
                "CollisionState",
                typeof(CollisionState),
                typeof(Joint),
                new PropertyMetadata(CollisionState.DisableCollisions, OnCollisionStateChanged));

        protected static void OnCollisionStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Joint joint = (Joint)d;

            if (joint.IsInitialised)
                joint.NewtonJoint.CollisionState = (int)(CollisionState)e.NewValue;
        }

        public CollisionState CollisionState
        {
            get { return (CollisionState)GetValue(CollisionStateProperty); }
            set { SetValue(CollisionStateProperty, value); }
        }

        #endregion

        #region StiffnessProperty

        public static readonly DependencyProperty StiffnessProperty =
            DependencyProperty.Register(
                "Stiffness",
                typeof(double?),
                typeof(Joint),
                new PropertyMetadata(OnStiffnessChanged));

        protected static void OnStiffnessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                double value = (double)e.NewValue;

                if ((value < 0) || (value > 1))
                    throw new ArgumentOutOfRangeException(
                        e.Property.Name,
                        e.NewValue,
                        string.Format("The {0} Property has to be between 0 and 1.", e.Property.Name));

                Joint joint = (Joint)d;

                if (joint.IsInitialised)
                    joint.NewtonJoint.Stiffness = (float)value;
            }
        }

        public double? Stiffness
        {
            get { return (double?)GetValue(StiffnessProperty); }
            set { SetValue(StiffnessProperty, value); }
        }

        #endregion

        protected static void OnUnPausePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs p)
        {
            Joint joint = (Joint)d;

            if (joint._isInitialised)
            {
                if ((joint._childBody != null) && joint._childBody.IsInitialised)
                    joint._childBody.UnPause();
                /*
                if ((joint._parentBody != null) && joint._parentBody.IsInitialised)
                    joint._parentBody.UnPause();
                 */
            }
        }

        private World _world;
        private Body _parentBody;
        private Body _childBody;
        private CJoint _joint;
        private bool _isInitialised;
        private int _initialising;
        private bool _needsRebuild;

        public Joint()
        {
        }

        public Joint(World world, Body parent, Body child)
        {
            Initialise(world, parent, child);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_joint != null)
                {
                    if (_isInitialised)
                    {
                        _joint.UserData = null;
                        _joint.Dispose();

                        _isInitialised = false;
                    }
                    _joint = null;
                }
            }
        }

        #endregion

        public void Initialise(World world, Body parent, Body child)
        {
            if (_isInitialised)
                return;

            VerifyAccess();

            if (world == null) throw new ArgumentNullException("world");
            if (parent == null) throw new ArgumentNullException("parent");
            parent.VerifyInitialised("Parent");
            if (child == null) throw new ArgumentNullException("child");
            child.VerifyInitialised("Child");

            _world = world;
            _parentBody = parent;
            _childBody = child;

            _joint = OnInitialise();
            if (_joint != null)
            {
                _joint.CollisionState = (int)(CollisionState)this.CollisionState;

                if (this.Stiffness != null)
                    _joint.Stiffness = (float)this.Stiffness;

                _isInitialised = true;
                AfterInitialise();
            }
        }

        public World World
        {
            get { return _world; }
        }

        public Body ParentBody
        {
            get { return _parentBody; }
        }

        public Body ChildBody
        {
            get { return _childBody; }
        }

        public CJoint NewtonJoint
        {
            get { return _joint; }
        }

        internal bool IsInitialised
        {
            get { return _isInitialised; }
        }

        protected abstract CJoint OnInitialise();

        protected virtual void AfterInitialise()
        {
        }

        protected void RebuildJoint()
        {
            VerifyAccess();

            if (IsInitialised)
            {
                if (_initialising > 0)
                    _needsRebuild = true;
                else
                {
                    _joint.Dispose();
                    _isInitialised = false;

                    _joint = OnInitialise();
                    if (_joint != null)
                        _isInitialised = true;

                    _needsRebuild = false;
                }
            }
        }

        protected bool IsInitialising
        {
            get { return (_initialising > 0); }
        }

        internal protected void VerifyNotInitialised(string propertyName)
        {
            if (IsInitialised)
                throw new InvalidOperationException(
                    string.Format("The {0}.{1} property can only be set when initialising the Joint.",
                    this.GetType().Name,
                    propertyName));
        }

        public static Point3D BodyToWorld(Body body, Point3D point)
        {
            return body.Transform.Value.Transform(point);
        }

        public static Vector3D BodyToWorld(Body body, Vector3D vector)
        {
            return body.Transform.Value.Transform(vector);
        }

        public static Point3D WorldToBody(Body body, Point3D point)
        {
            Matrix3D matrix = body.Transform.Value;
            matrix.Invert();

            return matrix.Transform(point);
        }

        public static Vector3D WorldToBody(Body body, Vector3D vector)
        {
            Matrix3D matrix = body.Transform.Value;
            matrix.Invert();

            return matrix.Transform(vector);
        }

        public static Point3D BodyToBody(Body fromBody, Body toBody, Point3D point)
        {
            Matrix3D matrix = BodyToBodyMatrix(fromBody, toBody);

            return matrix.Transform(point);
        }

        public static Vector3D BodyToBody(Body fromBody, Body toBody, Vector3D vector)
        {
            Matrix3D matrix = BodyToBodyMatrix(fromBody, toBody);

            return matrix.Transform(vector);
        }

        public static Matrix3D BodyToBodyMatrix(Body fromBody, Body toBody)
        {
            Matrix3D result = toBody.Transform.Value;
            result.Invert();
            result.Append(fromBody.Transform.Value);

            return result;
        }

        public static Matrix3D BodyToWorldMatrix(Body body)
        {
            return body.Transform.Value;
        }

        public static Matrix3D WorldToBodyMatrix(Body body)
        {
            Matrix3D result = body.Transform.Value;
            result.Invert();

            return result;
        }

        public static Matrix3D BodyToWorldMatrix(Body body, Point3D position, Vector3D direction)
        {
            Matrix3D result = Math3D.CreateZDirectionMatrix(position, direction);
            result.Append(body.Transform.Value);

            return result;
        }

        public static Matrix3D BodyToWorldMatrix(Body body, Vector3D direction)
        {
            return BodyToWorldMatrix(body, new Point3D(), direction);
        }

        #region ISupportInitialize Members

        public virtual void BeginInit()
        {
            _initialising++;
        }

        public virtual void EndInit()
        {
            _initialising--;

            if (_initialising < 0)
                throw new InvalidOperationException("EndInit() called without a matching BeginInit().");

            if (_needsRebuild)
                RebuildJoint();
        }

        #endregion
    }
}
