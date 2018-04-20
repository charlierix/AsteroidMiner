using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    public class KeepItems2D : IDisposable
    {
        #region Class: LockOrientation

        private class LockOrientation
        {
            public LockOrientation(MapObject_ChaseOrientation_Forces chase, Vector3D rotateAxis, Vector3D modelUp)
            {
                this.Chase = chase;
                this.RotateAxis = rotateAxis;
                this.ModelUp = modelUp;
            }

            public readonly MapObject_ChaseOrientation_Forces Chase;
            public readonly Vector3D RotateAxis;
            public readonly Vector3D ModelUp;
        }

        #endregion

        #region Declaration Section

        private List<Tuple<IMapObject, MapObject_ChasePoint_Forces, LockOrientation>> _items = new List<Tuple<IMapObject, MapObject_ChasePoint_Forces, LockOrientation>>();

        // Can't use this, because it will prevent the weapon from swinging (it keeps directly setting the
        // the weapon's velocity)
        //private List<Tuple<IMapObject, MapObjectChaseVelocity>> _items = new List<Tuple<IMapObject, MapObjectChaseVelocity>>();

        #endregion

        #region Constructor

        public KeepItems2D()
        {
        }

        #endregion

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
                foreach (var item in _items)
                {
                    //NOTE: Only disposing the chase class, because that is being managed by this class.  The body its chasing is not managed by this class, so should be disposed elsewhere
                    item.Item2.Dispose();

                    if (item.Item3 != null)
                    {
                        item.Item3.Chase.Dispose();
                    }
                }

                _items.Clear();
            }
        }

        #endregion

        #region Public Properties

        public DragHitShape SnapShape
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void Add(IMapObject item, bool shouldLockOrientation, Vector3D? orientationRotateAxis = null, Vector3D? orientationModelUp = null)
        {
            if (_items.Any(o => o.Item1.Equals(item)))
            {
                // It's already added
                return;
            }

            #region Forces

            List<ChasePoint_Force> forces = new List<ChasePoint_Force>();

            // Attraction Force
            var gradient = new[]
                {
                    Tuple.Create(0d, .04d),     // distance, %
                    Tuple.Create(1d, 1d),
                };
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Attract_Direction, 500d, gradient: gradient));

            // These act like a shock absorber
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway, 50d));

            gradient = new[]
                {
                    Tuple.Create(0d, 1d),
                    Tuple.Create(.75d, .2d),
                    Tuple.Create(2d, 0d),
                };
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_AlongIfVelocityToward, 100d, gradient: gradient));


            MapObject_ChasePoint_Forces chaseForces = new MapObject_ChasePoint_Forces(item, false);
            if (item.PhysicsBody != null)
            {
                chaseForces.Offset = item.PhysicsBody.CenterOfMass.ToVector();
            }

            chaseForces.Forces = forces.ToArray();

            #region ORIG

            //// Attraction Force
            //chaseForces.Forces.Add(new ChasePoint_ForcesGradient<ChasePoint_ForcesAttract>(new[]
            //        {
            //            new ChasePoint_ForcesGradientStop<ChasePoint_ForcesAttract>(new ChasePoint_Distance(true, 0d), new ChasePoint_ForcesAttract() { BaseAcceleration = 20d, ApplyWhenUnderSpeed = 100d }),
            //            new ChasePoint_ForcesGradientStop<ChasePoint_ForcesAttract>(new ChasePoint_Distance(false, 1d), new ChasePoint_ForcesAttract() { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d }),
            //            new ChasePoint_ForcesGradientStop<ChasePoint_ForcesAttract>(new ChasePoint_Distance(true, double.MaxValue), new ChasePoint_ForcesAttract() { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d })
            //        }));

            //// These act like a shock absorber
            //chaseForces.Forces.Add(new ChasePoint_ForcesDrag(ChasePoint_DirectionType.Velocity_AlongIfVelocityAway) { BaseAcceleration = 50d });

            //chaseForces.Forces.Add(new ChasePoint_ForcesGradient<ChasePoint_ForcesDrag>(new[]
            //        {
            //            new ChasePoint_ForcesGradientStop<ChasePoint_ForcesDrag>(new ChasePoint_Distance(true, 0d), new ChasePoint_ForcesDrag(ChasePoint_DirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 100d }),
            //            new ChasePoint_ForcesGradientStop<ChasePoint_ForcesDrag>(new ChasePoint_Distance(false, .75d), new ChasePoint_ForcesDrag(ChasePoint_DirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 20d }),
            //            new ChasePoint_ForcesGradientStop<ChasePoint_ForcesDrag>(new ChasePoint_Distance(false, 2d), new ChasePoint_ForcesDrag(ChasePoint_DirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 0d }),
            //        }));

            #endregion

            #endregion
            //#region Orientation

            LockOrientation chaseOrientation = null;

            //if (shouldLockOrientation)
            //{
            //    if (orientationRotateAxis == null)
            //    {
            //        throw new ArgumentException("orientationRotateAxis can't be null when told to lock orientation");
            //    }
            //    else if (orientationModelUp == null)
            //    {
            //        throw new ArgumentException("orientationModelUp can't be null when told to lock orientation");
            //    }

            //    chaseOrientation = new LockOrientation(new MapObject_ChaseOrientation_Forces(item), orientationRotateAxis.Value, orientationModelUp.Value);
            //}

            //#endregion

            _items.Add(Tuple.Create(item, chaseForces, chaseOrientation));
        }
        public void Remove(IMapObject item)
        {
            for (int cntr = 0; cntr < _items.Count; cntr++)
            {
                if (_items[cntr].Item1.Equals(item))
                {
                    _items[cntr].Item2.Dispose();

                    if (_items[cntr].Item3 != null)
                    {
                        _items[cntr].Item3.Chase.Dispose();
                    }

                    _items.RemoveAt(cntr);

                    return;
                }
            }
        }

        public void Update()
        {
            foreach (var item in _items)
            {
                Point3D position = item.Item1.PositionWorld;

                // Get a ray
                Point3D? chasePoint = this.SnapShape.CastRay(position);

                // Chase that point
                if (chasePoint == null || Math3D.IsNearValue(position, chasePoint.Value))
                {
                    item.Item2.StopChasing();
                }
                else
                {
                    item.Item2.SetPosition(chasePoint.Value);
                }

                // Lock orientation
                if (item.Item3 != null)
                {
                    if (chasePoint == null)
                    {
                        item.Item3.Chase.StopChasing();
                    }
                    else
                    {
                        Vector3D? normal = this.SnapShape.GetNormal(position);
                        if (normal == null)
                        {
                            item.Item3.Chase.StopChasing();       // this should never happen
                        }
                        else
                        {
                            Point3D centerMass = item.Item1.PhysicsBody.PositionToWorld(item.Item1.PhysicsBody.CenterOfMass);
                            Vector3D axis = item.Item1.PhysicsBody.DirectionToWorld(item.Item3.RotateAxis);

                            Vector3D up = Vector3D.CrossProduct(axis, normal.Value);

                            item.Item3.Chase.SetProps(centerMass, axis, up, item.Item3.ModelUp, true);
                        }
                    }
                }
            }
        }

        #endregion
    }

    //TODO: Put this in MapObjectChase.cs when it's finished
    public class MapObject_ChaseOrientation_Forces : IDisposable
    {
        #region Declaration Section

        private bool _isChasing = false;

        private Point3D _axisThru;
        private Vector3D _axis;

        private Vector3D _up;
        private bool _allowUpOrDown;

        private Vector3D _modelUp;

        #endregion

        #region Constructor

        public MapObject_ChaseOrientation_Forces(IMapObject item)
        {
            this.Item = item;

            this.Item.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
        }

        #endregion

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
                StopChasing();

                this.Item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            }
        }

        #endregion

        #region Public Properties

        public readonly IMapObject Item;

        #endregion

        #region Public Methods

        public void SetProps(Point3D axisThru, Vector3D axis, Vector3D up, Vector3D modelUp, bool allowUpOrDown)
        {
            _axisThru = axisThru;
            _axis = axis;
            _up = up;
            _allowUpOrDown = allowUpOrDown;
            _modelUp = modelUp;

            _isChasing = true;
        }

        public void StopChasing()
        {
            _isChasing = false;
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            // See if there is anything to do
            if (!_isChasing)
            {
                return;
            }

            Vector3D currentUp = e.Body.DirectionToWorld(_modelUp);








        }

        #endregion
    }

    #region ORIG

    //public class KeepItems2D : IDisposable
    //{
    //    #region Declaration Section

    //    private List<Tuple<IMapObject, MapObjectChaseForces>> _items = new List<Tuple<IMapObject, MapObjectChaseForces>>();

    //    // Can't use this, because it will prevent the weapon from swinging (it keeps directly setting the
    //    // the weapon's velocity)
    //    //private List<Tuple<IMapObject, MapObjectChaseVelocity>> _items = new List<Tuple<IMapObject, MapObjectChaseVelocity>>();

    //    #endregion

    //    #region Constructor

    //    public KeepItems2D()
    //    {
    //    }

    //    #endregion

    //    #region IDisposable Members

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (disposing)
    //        {
    //            foreach (var item in _items)
    //            {
    //                //NOTE: Only disposing the chase class, because that is being managed by this class.  The body its chasing is not managed by this class, so should be disposed elsewhere
    //                item.Item2.Dispose();
    //            }

    //            _items.Clear();
    //        }
    //    }

    //    #endregion

    //    #region Public Properties

    //    public DragHitShape SnapShape
    //    {
    //        get;
    //        set;
    //    }

    //    #endregion

    //    #region Public Methods

    //    public void Add(IMapObject item)
    //    {
    //        if (_items.Any(o => o.Item1.Equals(item)))
    //        {
    //            // It's already added
    //            return;
    //        }

    //        //MapObjectChaseVelocity chase = new MapObjectChaseVelocity(item);
    //        //chase.MaxVelocity = 10d;
    //        //chase.Multiplier = 40d;

    //        MapObjectChaseForces chase = new MapObjectChaseForces(item, false);
    //        if (item.PhysicsBody != null)
    //        {
    //            chase.Offset = item.PhysicsBody.CenterOfMass.ToVector();
    //        }

    //        // Attraction Force
    //        chase.Forces.Add(new ChaseForcesGradient<ChaseForcesConstant>(new[]
    //                {
    //                    new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, 0d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 20d, ApplyWhenUnderSpeed = 100d }),
    //                    new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(false, 1d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d }),
    //                    new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, double.MaxValue), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d })
    //                }));

    //        // These act like a shock absorber
    //        chase.Forces.Add(new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityAway) { BaseAcceleration = 50d });

    //        chase.Forces.Add(new ChaseForcesGradient<ChaseForcesDrag>(new[]
    //                {
    //                    new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(true, 0d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 100d }),
    //                    new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, .75d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 20d }),
    //                    new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, 2d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 0d }),
    //                }));

    //        _items.Add(Tuple.Create(item, chase));
    //    }
    //    public void Remove(IMapObject item)
    //    {
    //        for (int cntr = 0; cntr < _items.Count; cntr++)
    //        {
    //            if (_items[cntr].Item1.Equals(item))
    //            {
    //                _items[cntr].Item2.Dispose();
    //                _items.RemoveAt(cntr);

    //                return;
    //            }
    //        }
    //    }

    //    public void Update()
    //    {
    //        foreach (var item in _items)
    //        {
    //            Point3D position = item.Item1.PositionWorld;

    //            // Get a ray
    //            Point3D? chasePoint = this.SnapShape.CastRay(position);

    //            // Chase that point
    //            if (chasePoint == null || Math3D.IsNearValue(position, chasePoint.Value))
    //            {
    //                item.Item2.StopChasing();
    //            }
    //            else
    //            {
    //                item.Item2.SetPosition(chasePoint.Value);
    //            }
    //        }
    //    }

    //    #endregion
    //}

    #endregion
}
