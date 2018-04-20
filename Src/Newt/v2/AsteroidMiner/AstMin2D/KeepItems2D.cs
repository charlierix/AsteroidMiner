using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    // This is copied from Game.Newt.v2.Arcanorum
    public class KeepItems2D : IDisposable
    {
        #region Class: TrackedItem

        private class TrackedItem
        {
            public TrackedItem(IMapObject mapObject, MapObject_ChasePoint_Forces translate, MapObject_ChaseOrientation_Torques rotate, double? graduleTo100PercentDuration, double? delayBeforeGradule, bool didOriginalLimitRotation)
            {
                this.MapObject = mapObject;
                this.Translate = translate;
                this.Rotate = rotate;

                this.GraduleTo100PercentDuration = graduleTo100PercentDuration;
                this.DelayBeforeGradule = delayBeforeGradule;
                this.ElapsedTime = 0d;

                this.DidOriginalLimitRotation = didOriginalLimitRotation;
            }

            public readonly IMapObject MapObject;
            public readonly MapObject_ChasePoint_Forces Translate;
            public readonly MapObject_ChaseOrientation_Torques Rotate;

            public double? GraduleTo100PercentDuration
            {
                get;
                set;
            }
            public double? DelayBeforeGradule
            {
                get;
                set;
            }
            public double ElapsedTime
            {
                get;
                set;
            }

            public readonly bool DidOriginalLimitRotation;
        }

        #endregion

        #region Declaration Section

        private List<TrackedItem> _items = new List<TrackedItem>();

        private readonly Vector3D _chaseDir = new Vector3D(0, 0, 1);

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
                    //NOTE: Only disposing the chase class, because that is being managed by this class.  The body it's chasing is not managed by this class, so should be disposed elsewhere
                    if (item.Translate != null)
                    {
                        item.Translate.Dispose();
                    }

                    if (item.Rotate != null)
                    {
                        item.Rotate.Dispose();
                    }
                }

                _items.Clear();
            }
        }

        #endregion

        #region Public Properties

        // The only reason to set these to false are for debugging
        private bool _shouldApplyForces = true;
        public bool ShouldApplyForces
        {
            get
            {
                return _shouldApplyForces;
            }
            set
            {
                _shouldApplyForces = value;

                ReloadItems();
            }
        }

        private bool _shouldApplyTorques = true;
        public bool ShouldApplyTorques
        {
            get
            {
                return _shouldApplyTorques;
            }
            set
            {
                _shouldApplyTorques = value;

                ReloadItems();
            }
        }

        #endregion

        #region Public Methods

        public void Add(IMapObject item, bool shouldLimitRotation, double? graduleTo100PercentDuration = null, double? delayBeforeGradule = null)
        {
            if (_items.Any(o => o.MapObject.Equals(item)))
            {
                // It's already added
                return;
            }

            Tuple<double, double>[] gradient;

            #region Forces

            MapObject_ChasePoint_Forces chaseForces = null;

            if (_shouldApplyForces)
            {
                List<ChasePoint_Force> forces = new List<ChasePoint_Force>();

                // Attraction Force
                gradient = new[]
                {
                    Tuple.Create(0d, 0d),     // distance, %
                    Tuple.Create(.7d, .28d),
                    Tuple.Create(1d, 1d),
                };
                forces.Add(new ChasePoint_Force(ChaseDirectionType.Attract_Direction, 500, gradient: gradient));

                // This acts like a shock absorber
                gradient = new[]
                {
                    Tuple.Create(0d, .25d),
                    Tuple.Create(.75d, 1d),
                    //Tuple.Create(3d, 0d),
                };
                forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_Along, 10));

                chaseForces = new MapObject_ChasePoint_Forces(item, false);
                chaseForces.Forces = forces.ToArray();
            }

            #endregion
            #region Torques - ORIG

            //MapObject_ChaseOrientation_Torques chaseTorques = null;

            //if (_shouldApplyTorques && shouldLimitRotation)
            //{
            //    List<ChaseOrientation_Torque> torques = new List<ChaseOrientation_Torque>();

            //    double mult = 60;

            //    // Attraction
            //    gradient = new[]
            //    {
            //        Tuple.Create(0d, 0d),     // distance, %
            //        Tuple.Create(10d, 1d),
            //    };
            //    torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Attract_Direction, .6 * mult, gradient: gradient));

            //    // Drag
            //    torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Drag_Velocity_Orth, .015 * mult));

            //    gradient = new[]
            //    {
            //        Tuple.Create(0d, 1d),
            //        Tuple.Create(1.6d, .3d),
            //        Tuple.Create(5d, 0d),
            //    };
            //    //torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Drag_Velocity_AlongIfVelocityToward, .04 * mult, gradient: gradient));
            //    torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway, .04 * mult, gradient: gradient));


            //    chaseTorques = new MapObject_ChaseOrientation_Torques(item);
            //    chaseTorques.Torques = torques.ToArray();
            //}

            #endregion
            #region Torques

            MapObject_ChaseOrientation_Torques chaseTorques = null;

            if (_shouldApplyTorques && shouldLimitRotation)
            {
                List<ChaseOrientation_Torque> torques = new List<ChaseOrientation_Torque>();

                double mult = 300; //600;

                // Attraction
                gradient = new[]
                {
                    Tuple.Create(0d, 0d),     // distance, %
                    Tuple.Create(10d, 1d),
                };
                torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Attract_Direction, .4 * mult, gradient: gradient));

                // Drag
                gradient = new[]        // this gradient is needed, because there needs to be no drag along the desired axis (otherwise, this drag will fight with the user's desire to rotate the ship)
                {
                    Tuple.Create(0d, 0d),     // distance, %
                    Tuple.Create(5d, 1d),
                };
                torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Drag_Velocity_Orth, .0739 * mult, gradient: gradient));

                torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway, .0408 * mult));

                chaseTorques = new MapObject_ChaseOrientation_Torques(item);
                chaseTorques.Torques = torques.ToArray();
            }

            #endregion

            _items.Add(new TrackedItem(item, chaseForces, chaseTorques, graduleTo100PercentDuration, delayBeforeGradule, shouldLimitRotation));
        }
        public void Remove(IMapObject item)
        {
            for (int cntr = 0; cntr < _items.Count; cntr++)
            {
                if (_items[cntr].MapObject.Equals(item))
                {
                    if (_items[cntr].Translate != null)
                    {
                        _items[cntr].Translate.Dispose();
                    }

                    if (_items[cntr].Rotate != null)
                    {
                        _items[cntr].Rotate.Dispose();
                    }

                    _items.RemoveAt(cntr);

                    return;
                }
            }
        }

        public void Update(double elapsedTime)
        {
            foreach (var item in _items)
            {
                if (item.GraduleTo100PercentDuration != null)
                {
                    #region Percent

                    item.ElapsedTime += elapsedTime;

                    double percent;
                    if(item.DelayBeforeGradule != null && item.ElapsedTime < item.DelayBeforeGradule.Value)
                    {
                        percent = 0;
                    }
                    else
                    {
                        percent = (item.ElapsedTime - (item.DelayBeforeGradule ?? 0d)) / item.GraduleTo100PercentDuration.Value;
                    }

                    if (percent > 1d)
                    {
                        percent = 1d;
                        item.GraduleTo100PercentDuration = null;        // setting it to null to make sure it doesn't get evaluated again
                    }

                    if (item.Translate != null)
                    {
                        item.Translate.Percent = percent;
                    }

                    if (item.Rotate != null)
                    {
                        item.Rotate.Percent = percent;
                    }

                    #endregion
                }

                if (item.Translate != null)
                {
                    Point3D position = item.MapObject.PhysicsBody.PositionToWorld(item.MapObject.PhysicsBody.CenterOfMass + item.Translate.Offset);

                    item.Translate.SetPosition(new Point3D(position.X, position.Y, 0));
                }

                if (item.Rotate != null)
                {
                    item.Rotate.SetOrientation(_chaseDir);
                }
            }
        }

        #endregion

        #region Private Methods

        private void ReloadItems()
        {
            TrackedItem[] existing = _items.ToArray();

            // Clear
            foreach (TrackedItem item in existing)
            {
                Remove(item.MapObject);
            }

            // Add them back (they will be added using the public properties current settings)
            foreach (TrackedItem item in existing)
            {
                Add(item.MapObject, item.DidOriginalLimitRotation, null);
            }
        }

        #endregion
    }

    #region Class: KeepItems2D_MANUALLYKEEPING2D

    // This one was manually rotating the ship, I don't remember if the position setting was manual or not

    public class KeepItems2D_MANUALLYKEEPING2D : IDisposable
    {
        #region Class: TrackedItem

        private class TrackedItem
        {
            public TrackedItem(IMapObject mapObject, MapObject_ChasePoint_Forces forces, bool shouldLimitRotation)
            {
                this.MapObject = mapObject;
                this.Forces = forces;
                this.ShouldLimitRotation = shouldLimitRotation;
            }

            public readonly IMapObject MapObject;
            public readonly MapObject_ChasePoint_Forces Forces;
            public readonly bool ShouldLimitRotation;
        }

        #endregion

        #region Declaration Section

        private List<TrackedItem> _items = new List<TrackedItem>();

        private readonly RotateTransform3D _rotate_ToWorld;
        private readonly RotateTransform3D _rotate_FromWorld;

        //private readonly Viewport3D _viewport;
        //private List<Visual3D> _debugVisuals = new List<Visual3D>();

        #endregion

        #region Constructor

        public KeepItems2D_MANUALLYKEEPING2D(RotateTransform3D rotate_ToWorld, RotateTransform3D rotate_FromWorld, Viewport3D viewport)
        {
            _rotate_ToWorld = rotate_ToWorld;
            _rotate_FromWorld = rotate_FromWorld;

            //_viewport = viewport;
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
                    if (item.Forces != null)
                    {
                        item.Forces.Dispose();
                    }

                    if (item.ShouldLimitRotation)
                    {
                        item.MapObject.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
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

        public void Add(IMapObject item, bool shouldLimitRotation)
        {
            if (_items.Any(o => o.MapObject.Equals(item)))
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
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Attract_Direction, 500, gradient: gradient));

            // These act like a shock absorber
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway, 50));

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
                //TODO: This could change over time.  Need to adjust it every once in a while
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

            //if (shouldLimitRotation)
            //{
            //    item.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            //}

            _items.Add(new TrackedItem(item, chaseForces, shouldLimitRotation));
            //_items.Add(new TrackedItem(item, null, shouldLimitRotation));
        }
        public void Remove(IMapObject item)
        {
            for (int cntr = 0; cntr < _items.Count; cntr++)
            {
                if (_items[cntr].MapObject.Equals(item))
                {
                    if (_items[cntr].Forces != null)
                    {
                        _items[cntr].Forces.Dispose();
                    }

                    if (_items[cntr].ShouldLimitRotation)
                    {
                        item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
                    }

                    _items.RemoveAt(cntr);

                    return;
                }
            }
        }

        public void Update()
        {

            return;


            //_viewport.Children.RemoveAll(_debugVisuals);
            //_debugVisuals.Clear();

            foreach (var item in _items)
            {
                if (item.MapObject is ShipPlayer)
                {
                    LimitLinear(item.MapObject.PhysicsBody, _rotate_ToWorld);
                    //LimitRotation(item.MapObject.PhysicsBody, _rotate_FromWorld, _rotate_ToWorld, _viewport, _debugVisuals);
                    LimitRotation(item.MapObject.PhysicsBody, _rotate_FromWorld, _rotate_ToWorld, null, null);
                }
                else
                {
                    LimitLinear(item.MapObject.PhysicsBody, RotateTransform3D.Identity);
                }
            }
        }

        public void Update_ORIG()
        {
            //foreach (var item in _items)
            //{
            //    if (item.Forces == null)
            //    {
            //        continue;
            //    }

            //    Point3D position = item.MapObject.PositionWorld;

            //    // Get a ray
            //    Point3D? chasePoint = this.SnapShape.CastRay(position);

            //    // Chase that point
            //    if (chasePoint == null || Math3D.IsNearValue(position, chasePoint.Value))
            //    {
            //        item.Forces.StopChasing();
            //    }
            //    else
            //    {
            //        item.Forces.SetPosition(chasePoint.Value);
            //    }
            //}
        }

        #endregion

        #region Event Listeners

        //TODO: Finish ChaseObject_?????? instead of hard coding here
        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            //TrackedItem item = _items.FirstOrDefault(o => o.MapObject.Token == e.Body.Token);
            //if (item != null && item.ShouldLimitRotation)
            //{
            //    //LimitRotation(e.Body, _rotate_ToWorld, _rotate_FromWorld);
            //}

            //LimitLinear(e.Body, _rotate_ToWorld);
        }

        #endregion

        #region Private Methods

        private static void LimitLinear(Body body, Transform3D rotate_ToWorld)
        {
            // Position
            Point3D position = body.Position;       //NOTE: Position is at the center of mass

            Point3D centerMassModel = body.CenterOfMass;
            Point3D centerMassWorld = rotate_ToWorld.Transform(centerMassModel);        // position is model coords, but rotated into world coords

            Point3D centerMassActual = body.DirectionToWorld(centerMassModel);

            body.Position = new Point3D(position.X - centerMassActual.X, position.Y - centerMassActual.Y, centerMassWorld.Z);

            // Velocity
            Vector3D velocity = body.Velocity;
            body.Velocity = new Vector3D(velocity.X, velocity.Y, 0);
        }

        private static void LimitRotation(Body body, Transform3D rotate_FromWorld, Transform3D rotate_ToWorld, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            const double DEADDOT = .33;

            Vector3D z = new Vector3D(0, 0, 1);

            Vector3D reversed = rotate_FromWorld.Transform(z);
            Vector3D current = body.DirectionToWorld(reversed);





            Vector3D whatIsThis = rotate_FromWorld.Transform(current);






            Quaternion diff = Math3D.GetRotation(rotate_FromWorld.Transform(current), reversed);


            Vector3D test = body.DirectionToWorld(z);

            Vector3D cross = Vector3D.CrossProduct(z, -test);
            Vector3D check = Vector3D.CrossProduct(z, reversed);
            double dot = Vector3D.DotProduct(cross, check);
            if (Math.Abs(dot) < DEADDOT)
            {
                //TODO: Fix this properly.  The problem has something to do with loss of accuracy around the X axis, because the ship is already being rotated
                //about that axis.  Is this the definition of gimbal lock?
                //In this case, will probably need to rotate everything be 90 degrees, then rebuild diff - or something?
                diff = Quaternion.Identity;
            }
            else if (dot < -DEADDOT)
            {
                diff = new Quaternion(diff.Axis, -diff.Angle);
            }

            //body.Rotation = body.Rotation.RotateBy(diff);






            //Quaternion diff = Math3D.GetRotation(current, z);     why doesn't this work?!?!?!
            //I wonder if the flaw is in the extension methd:
            //  body.Rotation = body.Rotation.RotateBy(diff);
            //
            //Try multiplying quaternions in a different order to see if the diff between current and z can be made to work

            //https://www.google.com/?gws_rd=ssl#safe=active&q=quaternion+multiplication+order
            //http://answers.unity3d.com/questions/810579/quaternion-multiplication-order.html

            //OR: is body.DirectionToWorld doing more than body.Rotation
            //
            //Use the F keys to get and draw various rotations, to test if there's more than just body.Rotation - or something










            //Vector3D verifyRotation = body.Rotation.GetRotatedVector(reversed);

            ////if(!Math3D.IsNearValue(current.ToUnit(), verifyRotation.ToUnit()))        // too strict
            //if (Vector3D.DotProduct(current.ToUnit(), verifyRotation.ToUnit()) < .99)       // this if statement never hits
            //{
            //    //throw new ApplicationException("AAAAHHHHAAAAA!!!!!!!!!!!!!!!");
            //}



            ////TODO: Come up with a visualization that demonstrates why these are different.  That visualization will illuminate the gimbal lock
            //Quaternion diffProper = Math3D.GetRotation(current, z);
            //Quaternion diffHacked = Math3D.GetRotation(whatIsThis, reversed);


            //Point3D position = body.Position;

            //DrawLine(position, z * 100, Colors.DarkGreen, viewport, debugVisuals);
            //DrawLine(position, current * 100, Colors.LimeGreen, viewport, debugVisuals);
            ////if (!Math3D.IsNearZero(diffProper.Angle))
            //{
            //    DrawLine(position, diffProper.Axis * 100, Colors.PaleGreen, viewport, debugVisuals);
            //}


            //DrawLine(position, reversed * 100, Colors.Maroon, viewport, debugVisuals);
            //DrawLine(position, whatIsThis * 100, Colors.OrangeRed, viewport, debugVisuals);
            ////if (Math.Abs(diffHacked.Angle) > .001)
            //{
            //    DrawLine(position, diffHacked.Axis * 100, Colors.Wheat, viewport, debugVisuals);
            //}



            //DrawLine(position, new Vector3D(100, 0, 0), Colors.DeepSkyBlue, viewport, debugVisuals);
            //DrawLine(position, new Vector3D(0, 100, 0), Colors.SteelBlue, viewport, debugVisuals);




        }






        private static void LimitRotation_ONEHALF(Body body, Transform3D rotate_FromWorld, Transform3D rotate_ToWorld, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            Vector3D z = new Vector3D(0, 0, 1);

            Vector3D reversed = rotate_FromWorld.Transform(z);
            Vector3D current = body.DirectionToWorld(reversed);

            Quaternion diff = Math3D.GetRotation(rotate_FromWorld.Transform(current), reversed);

            body.Rotation = body.Rotation.RotateBy(diff);







            Point3D position = body.Position;

            //DrawLine(position, z * 100, Colors.DarkOrchid, viewport, debugVisuals);     // won't be visible
            //DrawLine(position, current * 100, Colors.HotPink, viewport, debugVisuals);

            //DrawLine(position, diff.Axis * 100, Colors.Yellow, viewport, debugVisuals);




            //Vector3D angularVelocity = body.AngularVelocity.GetProjectedVector(z);
            Vector3D angularVelocity = body.AngularVelocity;

            DrawLine(position, angularVelocity * 100, Colors.Yellow, viewport, debugVisuals);

            angularVelocity = angularVelocity.GetProjectedVector(z);

            DrawLine(position, angularVelocity * 100, Colors.HotPink, viewport, debugVisuals);

            body.AngularVelocity = angularVelocity;


        }
        private static void LimitRotation_ONEQUADRANT(Body body, Transform3D rotate_FromWorld, Transform3D rotate_ToWorld, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            Vector3D z = new Vector3D(0, 0, 1);

            Vector3D reversed = rotate_FromWorld.Transform(z);
            Vector3D current = body.DirectionToWorld(reversed);

            //TODO: This only works for one quadrant

            Quaternion diff = Math3D.GetRotation(current, z);
            diff = new Quaternion(rotate_FromWorld.Transform(diff.Axis), diff.Angle);

            body.Rotation = body.Rotation.RotateBy(diff);







            Point3D position = body.Position;

            //DrawLine(position, z * 100, Colors.DarkOrchid, viewport, debugVisuals);     // won't be visible
            //DrawLine(position, current * 100, Colors.HotPink, viewport, debugVisuals);

            //DrawLine(position, diff.Axis * 100, Colors.Yellow, viewport, debugVisuals);




            //Vector3D angularVelocity = body.AngularVelocity.GetProjectedVector(z);
            Vector3D angularVelocity = body.AngularVelocity;

            DrawLine(position, angularVelocity * 100, Colors.Yellow, viewport, debugVisuals);

            angularVelocity = angularVelocity.GetProjectedVector(z);

            DrawLine(position, angularVelocity * 100, Colors.HotPink, viewport, debugVisuals);

            body.AngularVelocity = angularVelocity;


        }

        private static void DrawLine(Point3D position, Vector3D direction, Color color, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            BillboardLine3DSet line = new BillboardLine3DSet()
            {
                Color = color,
                IsReflectiveColor = false,
            };

            line.BeginAddingLines();
            line.AddLine(position, position + direction, .2);
            line.EndAddingLines();

            debugVisuals.Add(line);
            viewport.Children.Add(line);
        }

        #endregion
    }

    #endregion
    #region Class: KeepItems2D_ROTATETOXY

    // This was before the ship's dna was prerotated
    public class KeepItems2D_ROTATETOXY : IDisposable
    {
        #region Class: TrackedItem

        private class TrackedItem
        {
            public TrackedItem(IMapObject mapObject, MapObject_ChasePoint_Forces forces, bool shouldLimitRotation)
            {
                this.MapObject = mapObject;
                this.Forces = forces;
                this.ShouldLimitRotation = shouldLimitRotation;
            }

            public readonly IMapObject MapObject;
            public readonly MapObject_ChasePoint_Forces Forces;
            public readonly bool ShouldLimitRotation;
        }

        #endregion

        #region Declaration Section

        private List<TrackedItem> _items = new List<TrackedItem>();

        private readonly RotateTransform3D _rotate_ToWorld;
        private readonly RotateTransform3D _rotate_FromWorld;

        private readonly Viewport3D _viewport;
        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        #endregion

        #region Constructor

        public KeepItems2D_ROTATETOXY(RotateTransform3D rotate_ToWorld, RotateTransform3D rotate_FromWorld, Viewport3D viewport)
        {
            _rotate_ToWorld = rotate_ToWorld;
            _rotate_FromWorld = rotate_FromWorld;

            _viewport = viewport;
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
                    if (item.Forces != null)
                    {
                        item.Forces.Dispose();
                    }

                    if (item.ShouldLimitRotation)
                    {
                        item.MapObject.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
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

        public void Add(IMapObject item, bool shouldLimitRotation)
        {
            if (_items.Any(o => o.MapObject.Equals(item)))
            {
                // It's already added
                return;
            }

            //#region Forces

            //MapObject_ChasePoint_Forces chaseForces = new MapObject_ChasePoint_Forces(item, false);
            //if (item.PhysicsBody != null)
            //{
            //    //TODO: This could change over time.  Need to adjust it every once in a while
            //    chaseForces.Offset = item.PhysicsBody.CenterOfMass.ToVector();
            //}

            //// Attraction Force
            //chaseForces.Forces.Add(new ChaseForcesGradient<ChaseForcesConstant>(new[]
            //        {
            //            new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, 0d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 20d, ApplyWhenUnderSpeed = 100d }),
            //            new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(false, 1d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d }),
            //            new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, double.MaxValue), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d })
            //        }));

            //// These act like a shock absorber
            //chaseForces.Forces.Add(new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityAway) { BaseAcceleration = 50d });

            //chaseForces.Forces.Add(new ChaseForcesGradient<ChaseForcesDrag>(new[]
            //        {
            //            new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(true, 0d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 100d }),
            //            new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, .75d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 20d }),
            //            new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, 2d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 0d }),
            //        }));

            //#endregion



            //if (shouldLimitRotation)
            //{
            //item.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            //}

            //_items.Add(new TrackedItem(item, chaseForces, shouldLimitRotation));
            _items.Add(new TrackedItem(item, null, shouldLimitRotation));
        }
        public void Remove(IMapObject item)
        {
            for (int cntr = 0; cntr < _items.Count; cntr++)
            {
                if (_items[cntr].MapObject.Equals(item))
                {
                    if (_items[cntr].Forces != null)
                    {
                        _items[cntr].Forces.Dispose();
                    }

                    if (_items[cntr].ShouldLimitRotation)
                    {
                        item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
                    }

                    _items.RemoveAt(cntr);

                    return;
                }
            }
        }

        public void Update()
        {
            _viewport.Children.RemoveAll(_debugVisuals);
            _debugVisuals.Clear();

            foreach (var item in _items)
            {
                if (item.MapObject is ShipPlayer)
                {
                    LimitLinear(item.MapObject.PhysicsBody, _rotate_ToWorld);
                    LimitRotation(item.MapObject.PhysicsBody, _rotate_FromWorld, _rotate_ToWorld, _viewport, _debugVisuals);
                }
                else
                {
                    LimitLinear(item.MapObject.PhysicsBody, RotateTransform3D.Identity);
                }
            }
        }

        public void Update_ORIG()
        {
            //foreach (var item in _items)
            //{
            //    if (item.Forces == null)
            //    {
            //        continue;
            //    }

            //    Point3D position = item.MapObject.PositionWorld;

            //    // Get a ray
            //    Point3D? chasePoint = this.SnapShape.CastRay(position);

            //    // Chase that point
            //    if (chasePoint == null || Math3D.IsNearValue(position, chasePoint.Value))
            //    {
            //        item.Forces.StopChasing();
            //    }
            //    else
            //    {
            //        item.Forces.SetPosition(chasePoint.Value);
            //    }
            //}
        }

        #endregion

        #region Event Listeners

        //TODO: Finish ChaseObject_?????? instead of hard coding here
        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            //TrackedItem item = _items.FirstOrDefault(o => o.MapObject.Token == e.Body.Token);
            //if (item != null && item.ShouldLimitRotation)
            //{
            //    //LimitRotation(e.Body, _rotate_ToWorld, _rotate_FromWorld);
            //}

            //LimitLinear(e.Body, _rotate_ToWorld);
        }

        #endregion

        #region Private Methods

        private static void LimitLinear(Body body, Transform3D rotate_ToWorld)
        {
            // Position
            Point3D position = body.Position;       //NOTE: Position is at the center of mass

            Point3D centerMassModel = body.CenterOfMass;
            Point3D centerMassWorld = rotate_ToWorld.Transform(centerMassModel);        // position is model coords, but rotated into world coords

            Point3D centerMassActual = body.DirectionToWorld(centerMassModel);

            body.Position = new Point3D(position.X - centerMassActual.X, position.Y - centerMassActual.Y, centerMassWorld.Z);

            // Velocity
            Vector3D velocity = body.Velocity;
            body.Velocity = new Vector3D(velocity.X, velocity.Y, 0);
        }

        private static void LimitRotation(Body body, Transform3D rotate_FromWorld, Transform3D rotate_ToWorld, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            const double DEADDOT = .33;

            Vector3D z = new Vector3D(0, 0, 1);

            Vector3D reversed = rotate_FromWorld.Transform(z);
            Vector3D current = body.DirectionToWorld(reversed);





            Vector3D whatIsThis = rotate_FromWorld.Transform(current);






            Quaternion diff = Math3D.GetRotation(rotate_FromWorld.Transform(current), reversed);


            Vector3D test = body.DirectionToWorld(z);

            Vector3D cross = Vector3D.CrossProduct(z, -test);
            Vector3D check = Vector3D.CrossProduct(z, reversed);
            double dot = Vector3D.DotProduct(cross, check);
            if (Math.Abs(dot) < DEADDOT)
            {
                //TODO: Fix this properly.  The problem has something to do with loss of accuracy around the X axis, because the ship is already being rotated
                //about that axis.  Is this the definition of gimbal lock?
                //In this case, will probably need to rotate everything be 90 degrees, then rebuild diff - or something?
                diff = Quaternion.Identity;
            }
            else if (dot < -DEADDOT)
            {
                diff = new Quaternion(diff.Axis, -diff.Angle);
            }

            //body.Rotation = body.Rotation.RotateBy(diff);






            //Quaternion diff = Math3D.GetRotation(current, z);     why doesn't this work?!?!?!
            //I wonder if the flaw is in the extension methd:
            //  body.Rotation = body.Rotation.RotateBy(diff);
            //
            //Try multiplying quaternions in a different order to see if the diff between current and z can be made to work

            //https://www.google.com/?gws_rd=ssl#safe=active&q=quaternion+multiplication+order
            //http://answers.unity3d.com/questions/810579/quaternion-multiplication-order.html

            //OR: is body.DirectionToWorld doing more than body.Rotation
            //
            //Use the F keys to get and draw various rotations, to test if there's more than just body.Rotation - or something










            //Vector3D verifyRotation = body.Rotation.GetRotatedVector(reversed);

            ////if(!Math3D.IsNearValue(current.ToUnit(), verifyRotation.ToUnit()))        // too strict
            //if (Vector3D.DotProduct(current.ToUnit(), verifyRotation.ToUnit()) < .99)       // this if statement never hits
            //{
            //    //throw new ApplicationException("AAAAHHHHAAAAA!!!!!!!!!!!!!!!");
            //}



            ////TODO: Come up with a visualization that demonstrates why these are different.  That visualization will illuminate the gimbal lock
            //Quaternion diffProper = Math3D.GetRotation(current, z);
            //Quaternion diffHacked = Math3D.GetRotation(whatIsThis, reversed);


            //Point3D position = body.Position;

            //DrawLine(position, z * 100, Colors.DarkGreen, viewport, debugVisuals);
            //DrawLine(position, current * 100, Colors.LimeGreen, viewport, debugVisuals);
            ////if (!Math3D.IsNearZero(diffProper.Angle))
            //{
            //    DrawLine(position, diffProper.Axis * 100, Colors.PaleGreen, viewport, debugVisuals);
            //}


            //DrawLine(position, reversed * 100, Colors.Maroon, viewport, debugVisuals);
            //DrawLine(position, whatIsThis * 100, Colors.OrangeRed, viewport, debugVisuals);
            ////if (Math.Abs(diffHacked.Angle) > .001)
            //{
            //    DrawLine(position, diffHacked.Axis * 100, Colors.Wheat, viewport, debugVisuals);
            //}



            //DrawLine(position, new Vector3D(100, 0, 0), Colors.DeepSkyBlue, viewport, debugVisuals);
            //DrawLine(position, new Vector3D(0, 100, 0), Colors.SteelBlue, viewport, debugVisuals);




        }






        private static void LimitRotation_ONEHALF(Body body, Transform3D rotate_FromWorld, Transform3D rotate_ToWorld, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            Vector3D z = new Vector3D(0, 0, 1);

            Vector3D reversed = rotate_FromWorld.Transform(z);
            Vector3D current = body.DirectionToWorld(reversed);

            Quaternion diff = Math3D.GetRotation(rotate_FromWorld.Transform(current), reversed);

            body.Rotation = body.Rotation.RotateBy(diff);







            Point3D position = body.Position;

            //DrawLine(position, z * 100, Colors.DarkOrchid, viewport, debugVisuals);     // won't be visible
            //DrawLine(position, current * 100, Colors.HotPink, viewport, debugVisuals);

            //DrawLine(position, diff.Axis * 100, Colors.Yellow, viewport, debugVisuals);




            //Vector3D angularVelocity = body.AngularVelocity.GetProjectedVector(z);
            Vector3D angularVelocity = body.AngularVelocity;

            DrawLine(position, angularVelocity * 100, Colors.Yellow, viewport, debugVisuals);

            angularVelocity = angularVelocity.GetProjectedVector(z);

            DrawLine(position, angularVelocity * 100, Colors.HotPink, viewport, debugVisuals);

            body.AngularVelocity = angularVelocity;


        }
        private static void LimitRotation_ONEQUADRANT(Body body, Transform3D rotate_FromWorld, Transform3D rotate_ToWorld, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            Vector3D z = new Vector3D(0, 0, 1);

            Vector3D reversed = rotate_FromWorld.Transform(z);
            Vector3D current = body.DirectionToWorld(reversed);

            //TODO: This only works for one quadrant

            Quaternion diff = Math3D.GetRotation(current, z);
            diff = new Quaternion(rotate_FromWorld.Transform(diff.Axis), diff.Angle);

            body.Rotation = body.Rotation.RotateBy(diff);







            Point3D position = body.Position;

            //DrawLine(position, z * 100, Colors.DarkOrchid, viewport, debugVisuals);     // won't be visible
            //DrawLine(position, current * 100, Colors.HotPink, viewport, debugVisuals);

            //DrawLine(position, diff.Axis * 100, Colors.Yellow, viewport, debugVisuals);




            //Vector3D angularVelocity = body.AngularVelocity.GetProjectedVector(z);
            Vector3D angularVelocity = body.AngularVelocity;

            DrawLine(position, angularVelocity * 100, Colors.Yellow, viewport, debugVisuals);

            angularVelocity = angularVelocity.GetProjectedVector(z);

            DrawLine(position, angularVelocity * 100, Colors.HotPink, viewport, debugVisuals);

            body.AngularVelocity = angularVelocity;


        }

        private static void DrawLine(Point3D position, Vector3D direction, Color color, Viewport3D viewport, List<Visual3D> debugVisuals)
        {
            BillboardLine3DSet line = new BillboardLine3DSet()
            {
                Color = color,
                IsReflectiveColor = false,
            };

            line.BeginAddingLines();
            line.AddLine(position, position + direction, .2);
            line.EndAddingLines();

            debugVisuals.Add(line);
            viewport.Children.Add(line);
        }

        #endregion
    }

    #endregion
}
