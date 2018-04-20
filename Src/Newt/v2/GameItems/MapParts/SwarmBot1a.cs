﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.MapParts
{
    //TODO: Have a min speed so they don't stop

    /// <summary>
    /// This is meant to be a simplistic working swarmbot.  Lots of hardcoding
    /// </summary>
    /// <remarks>
    /// This first version doesn't know about the mothership.  All settings are hardcoded/fixed - no attempt to adjust to error
    /// 
    /// It doesn't have fuzzy definitions of map objects with corresponding force settings.  It has:
    ///     chasing swarmbots
    ///     avoiding swarmbots
    ///     avoiding asteroids
    ///     a bunch of other hardcoded settings for objective stroke
    /// 
    /// TODO: There needs to be some global props about desired distance:
    ///     bot-bot
    ///     bot-mothership
    ///     etc
    /// The controller needs to adjust private force settings to minimize the error between desired and actual distances
    /// </remarks>
    public class SwarmBot1a : IDisposable, IMapObject, IPartUpdatable
    {
        #region Class: ForceSettings_Initial

        /// <summary>
        /// These are force settings before considering neighbors, distance, etc
        /// </summary>
        private class ForceSettings_Initial
        {
            // attract
            // repulse
            // orth friction
            // etc
            public ChasePoint_Force[] Forces { get; set; }

            // how much to align with the other item's velocity
            public double? MatchVelocityPercent { get; set; }

            // opacity (amount that this can be ignored when close to an objective)
        }

        #endregion
        #region Class: ForceSettings_Final

        private class ForceSettings_Final
        {

        }

        #endregion
        #region Class: CurrentChasingStroke

        private class CurrentChasingStroke
        {
            public CurrentChasingStroke(long token)
            {
                this.Token = token;
            }

            public readonly long Token;

            public volatile int MinIndex = 0;

            private volatile object _minDistance = (double?)null;
            public double? MinDistance
            {
                get
                {
                    return (double?)_minDistance;
                }
                set
                {
                    _minDistance = value;
                }
            }
        }

        #region ORIG

        //private class CurrentChasingStroke_ORIG
        //{
        //    #region Constructor

        //    public CurrentChasingStroke_ORIG(SwarmObjectiveStrokes.Stroke stroke, double repulseStrength)
        //    {
        //        const double VELOCITYMULT = 10;

        //        var adjustedVelocities = stroke.Points.
        //            Select(o => Tuple.Create(o.Item1, o.Item2 * VELOCITYMULT)).
        //            ToArray();

        //        this.Stroke = new SwarmObjectiveStrokes.Stroke(adjustedVelocities, stroke.DeathTime);

        //        this.UpcomingPoints = CalculateRepulseForces(stroke, repulseStrength);
        //    }

        //    #endregion

        //    public readonly SwarmObjectiveStrokes.Stroke Stroke;

        //    //public readonly StrokePointForce[] ActivePoints;      // no need for special rules with attract.  Just go full accel toward
        //    //public readonly StrokePointForce[] UpcomingPoints;      // the points that have already been passed have no more influence

        //    /// <summary>
        //    /// These will repulse the bot.  The purpose of these is to keep the bot from traveling along the stroke backward to
        //    /// get to the first point (push the bot to the side so it takes a more circular path)
        //    /// </summary>
        //    public readonly StrokePointForce[][] UpcomingPoints;

        //    public volatile int CurrentIndex = 0;

        //    #region Private Methods

        //    private static StrokePointForce[][] CalculateRepulseForces(SwarmObjectiveStrokes.Stroke stroke, double repulseStrength)
        //    {
        //        StrokePointForce[][] retVal = new StrokePointForce[stroke.Points.Length][];

        //        for (int cntr = 0; cntr < retVal.Length; cntr++)
        //        {
        //            retVal[cntr] = CalculateRepulseForces_Index(stroke, cntr, repulseStrength);
        //        }

        //        return retVal;
        //    }
        //    private static StrokePointForce[] CalculateRepulseForces_Index(SwarmObjectiveStrokes.Stroke stroke, int index, double repulseStrength)
        //    {
        //        //NOTE: The last index will be an empty array.  Doing this so the caller doesn't need range checks, they just
        //        //iterate through the forces and come up with nothing
        //        List<StrokePointForce> retVal = new List<StrokePointForce>();

        //        Point3D point = stroke.Points[index].Item1;
        //        Vector3D direction = stroke.Points[index].Item2;

        //        for (int cntr = index + 1; cntr < stroke.Points.Length; cntr++)
        //        {
        //            StrokePointForce force = CalculateRepulseForces_Index_On(stroke, cntr, point, direction, repulseStrength);
        //            if (force == null)
        //            {
        //                // Once a null is encountered, the stroke has come back too far.  Just stop looking at the rest of the stroke.
        //                // It could also be valid to continue processing the stroke, and keep any points that loop back in front, but
        //                // that's extra processing, and a bit silly if they are just drawing spirals
        //                break;
        //            }

        //            retVal.Add(force);

        //            force = CalculateRepulseForces_Index_Half(stroke, cntr, point, direction, repulseStrength);
        //            if (force == null)
        //            {
        //                break;
        //            }

        //            retVal.Add(force);
        //        }

        //        return retVal.ToArray();
        //    }
        //    //TODO: Increase the influence radius a bit when the point is far from the initial
        //    private static StrokePointForce CalculateRepulseForces_Index_On(SwarmObjectiveStrokes.Stroke stroke, int index, Point3D origPoint, Vector3D origDir, double repulseStrength)
        //    {
        //        double prevRadSqr = stroke.Points[index - 1].Item2.LengthSquared;
        //        double curRadSqr = stroke.Points[index].Item2.LengthSquared;

        //        double radius;
        //        if (prevRadSqr < curRadSqr)
        //        {
        //            radius = Math.Sqrt(prevRadSqr);

        //            // no need for a dot product check here, because it's not advancing past the current point
        //        }
        //        else
        //        {
        //            radius = Math.Sqrt(curRadSqr);

        //            // Make sure this doesn't loop back too far
        //            Point3D extendPoint = stroke.Points[index].Item1 + (stroke.Points[index].Item2 * .5);
        //            if (Vector3D.DotProduct(extendPoint - origPoint, origDir) < 0)
        //            {
        //                return null;
        //            }
        //        }

        //        radius = RepulseIncreaseRadius(origPoint, stroke.Points[index].Item1, radius);

        //        return new StrokePointForce(stroke.Points[index].Item1, radius, repulseStrength);
        //    }
        //    private static StrokePointForce CalculateRepulseForces_Index_Half(SwarmObjectiveStrokes.Stroke stroke, int index, Point3D origPoint, Vector3D origDir, double repulseStrength)
        //    {
        //        Point3D to;
        //        if (index < stroke.Points.Length - 1)
        //        {
        //            to = stroke.Points[index + 1].Item1;
        //        }
        //        else
        //        {
        //            // This is the extension of the last item, so just use its velocity
        //            to = stroke.Points[index].Item1 + stroke.Points[index].Item2;
        //        }

        //        if (Vector3D.DotProduct(to - origPoint, origDir) < 0)
        //        {
        //            // This is trying to loop back beyond the original point
        //            return null;
        //        }

        //        Point3D center = stroke.Points[index].Item1 + ((to - stroke.Points[index].Item1) * .5);

        //        double radius = RepulseIncreaseRadius(origPoint, center, (center - stroke.Points[index].Item1).Length);

        //        return new StrokePointForce(center, radius, repulseStrength);
        //    }

        //    private static double RepulseIncreaseRadius(Point3D origPoint, Point3D repulsePoint, double radius)
        //    {
        //        double distance = (repulsePoint - origPoint).Length;
        //        double ratio = distance / radius;

        //        if (ratio < 1)
        //        {
        //            // This should never happen
        //            return distance * .9;
        //        }

        //        //TODO: Increase by ((ratio - 1) / 2d) + 1, or something
        //        //But don't let it grow beyond (radius * 8), or something
        //        //  :)

        //        return radius;
        //    }

        //    #endregion
        //}

        #endregion

        #endregion
        #region Class: StrokePointForce

        private class StrokePointForce
        {
            public StrokePointForce(Point3D position, double effectRadius, double strength)
            {
                this.Position = position;
                this.EffectRadius = effectRadius;
                this.Strength = strength;
            }

            public readonly Point3D Position;

            public readonly double EffectRadius;
            public readonly double Strength;        //NOTE: It's up to the caller to know if this attracts or repels
        }

        #endregion
        #region Class: NearPointResult

        private class NearPointResult
        {
            public NearPointResult(int nearestPoint, double distanceSquared, Tuple<Point3D, Vector3D>[] fullSegment)
            {
                this.IsSegmentHit = false;
                this.NearestPoint = fullSegment[nearestPoint].Item1;
                this.DistanceSquared = distanceSquared;
                this.SegmentFrom = nearestPoint;
                this.SegmentTo = nearestPoint;
                this.FullSegment = fullSegment;
            }
            public NearPointResult(Point3D nearestPoint, int segmentFrom, int segmentTo, double percentAlongSegment, double distanceSquared, Tuple<Point3D, Vector3D>[] fullSegment)
            {
                this.IsSegmentHit = true;
                this.NearestPoint = nearestPoint;
                this.SegmentFrom = segmentFrom;
                this.SegmentTo = segmentTo;
                this.PercentAlongSegment = percentAlongSegment;
                this.DistanceSquared = distanceSquared;
                this.FullSegment = fullSegment;
            }

            public readonly bool IsSegmentHit;

            public readonly Point3D NearestPoint;
            public readonly double DistanceSquared;

            public readonly int SegmentFrom;
            public readonly int SegmentTo;

            public readonly double PercentAlongSegment;

            public readonly Tuple<Point3D, Vector3D>[] FullSegment;
        }

        #endregion

        #region Declaration Section

        private const double ACCEL = 5;

        private readonly Map _map;
        private readonly SwarmObjectiveStrokes _strokes;

        private volatile Tuple<Vector3D?, Vector3D?> _applyForceTorque = null;

        private ForceSettings_Initial _settings_OtherBot_Chase = null;
        private ForceSettings_Initial _settings_OtherBot_Passive = null;
        private ForceSettings_Initial _settings_Asteroid = null;

        private volatile CurrentChasingStroke _currentlyChasingStroke = null;

        private volatile Tuple<long, int> _stroke_index = null;

        #endregion

        #region Constructor

        public SwarmBot1a(double radius, Point3D position, World world, Map map, SwarmObjectiveStrokes strokes, int materialID, Color? color = null)
        {
            _map = map;
            _strokes = strokes;

            this.Radius = radius;
            this.SearchRadius = radius * 100;

            _settings_OtherBot_Chase = CreateForceSettingInitial_OtherBot_Chase();
            _settings_OtherBot_Passive = CreateForceSettingInitial_OtherBot_Passive();
            _settings_Asteroid = CreateForceSettingInitial_Asteroid();

            #region WPF Model

            this.Model = GetModel(radius, color);
            this.Model.Transform = new ScaleTransform3D(radius, radius, radius);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = this.Model;

            #endregion

            #region Physics Body

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            double mass = GetMass(radius);

            using (CollisionHull hull = CollisionHull.CreateSphere(world, 0, new Vector3D(radius / 2, radius / 2, radius / 2), null))
            {
                this.PhysicsBody = new Body(hull, transform.Value, mass, new Visual3D[] { visual });
                //this.PhysicsBody.IsContinuousCollision = true;
                this.PhysicsBody.MaterialGroupID = materialID;
                this.PhysicsBody.LinearDamping = .01d;
                this.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);

                this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            }

            #endregion

            this.CreationTime = DateTime.UtcNow;
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.PhysicsBody.Dispose();
            }
        }

        #endregion
        #region IMapObject Members

        public long Token
        {
            get
            {
                return this.PhysicsBody.Token;
            }
        }

        public bool IsDisposed
        {
            get
            {
                return this.PhysicsBody.IsDisposed;
            }
        }

        //TODO: In the future, there will be multiple bodies connected by joints
        public Body PhysicsBody
        {
            get;
            private set;
        }

        public Visual3D[] Visuals3D
        {
            get
            {
                return this.PhysicsBody.Visuals;
            }
        }
        public Model3D Model
        {
            get;
            private set;
        }

        public Point3D PositionWorld
        {
            get
            {
                return this.PhysicsBody.Position;
            }
        }
        public Vector3D VelocityWorld
        {
            get
            {
                return this.PhysicsBody.Velocity;
            }
        }
        public Matrix3D OffsetMatrix
        {
            get
            {
                return this.PhysicsBody.OffsetMatrix;
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        public DateTime CreationTime
        {
            get;
            private set;
        }

        public int CompareTo(IMapObject other)
        {
            return MapObjectUtil.CompareToT(this, other);
        }

        public bool Equals(IMapObject other)
        {
            return MapObjectUtil.EqualsT(this, other);
        }
        public override bool Equals(object obj)
        {
            return MapObjectUtil.EqualsObj(this, obj);
        }

        public override int GetHashCode()
        {
            return MapObjectUtil.GetHashCode(this);
        }

        #endregion
        #region IPartUpdatable Members

        public virtual void Update_MainThread(double elapsedTime)
        {
            // Bump the age (doing it on the main thread to avoid the chance of thread conflicts)
            this.Age += elapsedTime;
        }
        public virtual void Update_AnyThread(double elapsedTime)
        {
            Interlocked.Increment(ref this.AnyThreadCounter);

            // See what's around
            MapOctree snapshot = _map.LatestSnapshot;
            if (snapshot == null)
            {
                _applyForceTorque = null;
                return;
            }

            Point3D position = this.PositionWorld;

            // Find stuff
            var neighbors = GetNeighbors(snapshot, position);
            var objectiveStroke = _strokes.GetNearestStroke(position, this.SearchRadius_Strokes);

            if (neighbors.Length == 0 && objectiveStroke == null)
            {
                _applyForceTorque = GetSeekForce();
                return;
            }

            _applyForceTorque = GetSwarmForce(neighbors, objectiveStroke, position, this.VelocityWorld);
        }

        public virtual int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
            }
        }
        public virtual int? IntervalSkips_AnyThread
        {
            get
            {
                //TODO: Can probably slow this down
                return 0;
            }
        }

        #endregion

        #region Public Properties

        private volatile object _age = 0d;
        /// <summary>
        /// This is the age of the ship (sum of elapsed time).  This doesn't use datetime.utcnow, it's all based on the elapsed time passed into update (mainthread)
        /// </summary>
        public double Age
        {
            get
            {
                return (double)_age;
            }
            private set
            {
                _age = value;
            }
        }

        private volatile object _searchRadius = 0d;
        public double SearchRadius
        {
            get
            {
                return (double)_searchRadius;
            }
            set
            {
                _searchRadius = value;
            }
        }

        private volatile object _searchRadiusStrokes = 0d;
        public double SearchRadius_Strokes
        {
            get
            {
                return (double)_searchRadiusStrokes;
            }
            set
            {
                _searchRadiusStrokes = value;
            }
        }

        private volatile int _chaseNeighborCount = 5;
        public int ChaseNeighborCount
        {
            get
            {
                return _chaseNeighborCount;
            }
            set
            {
                _chaseNeighborCount = value;
            }
        }

        //NOTE: These are absolute maxes (when in open space)
        private volatile object _maxAccel = 10d;
        public double MaxAccel
        {
            get
            {
                return (double)_maxAccel;
            }
            set
            {
                _maxAccel = value;
            }
        }

        private volatile object _maxAngularAccel = 1d;
        public double MaxAngularAccel
        {
            get
            {
                return (double)_maxAngularAccel;
            }
            set
            {
                _maxAngularAccel = value;
            }
        }

        private volatile object _maxSpeed = 40d;
        public double MaxSpeed
        {
            get
            {
                return (double)_maxSpeed;
            }
            set
            {
                _maxSpeed = value;
            }
        }

        private volatile object _maxAngularSpeed = 10d;
        public double MaxAngularSpeed
        {
            get
            {
                return (double)_maxAngularSpeed;
            }
            set
            {
                _maxAngularSpeed = value;
            }
        }

        // These are for debugging to see if the any thread is called way too much, or not enough
        public long MainThreadCounter = 0;
        public long AnyThreadCounter = 0;

        #endregion

        #region Event Listeners

        private void Body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            Interlocked.Increment(ref this.MainThreadCounter);

            Tuple<Vector3D?, Vector3D?> currentForceTorque = _applyForceTorque;

            if (currentForceTorque != null)
            {
                double seconds = Math.Min(e.Timestep, 1);       // cap at one in case there is a big lag

                if (currentForceTorque.Item1 != null)
                {
                    e.Body.AddForce(currentForceTorque.Item1.Value / seconds);
                }

                if (currentForceTorque.Item2 != null)
                {
                    e.Body.AddTorque(currentForceTorque.Item2.Value / seconds);
                }
            }
        }

        #endregion

        #region Private Methods - chasing forces

        /// <summary>
        /// This is called when there is nothing nearby
        /// </summary>
        private Tuple<Vector3D?, Vector3D?> GetSeekForce()
        {
            #region velocity

            double maxSpeed = this.MaxSpeed;
            double maxAngSpeed = this.MaxAngularSpeed;

            Vector3D velocity = this.VelocityWorld;
            Vector3D angVelocity = this.PhysicsBody.AngularVelocity;

            Vector3D desiredVelocity;
            if (Math3D.IsNearZero(velocity))
                desiredVelocity = Math3D.GetRandomVector_Spherical_Shell(maxSpeed);
            else
                desiredVelocity = Math3D.GetRandomVector_Cone(velocity, 10).ToUnit(false) * maxSpeed;

            Vector3D desiredAngVelocity;
            if (Math3D.IsNearZero(angVelocity))
                desiredAngVelocity = Math3D.GetRandomVector_Spherical_Shell(maxAngSpeed);
            else
                desiredAngVelocity = Math3D.GetRandomVector_Cone(angVelocity, 10).ToUnit(false) * maxAngSpeed;

            #endregion

            #region acceleration

            double maxAccel = this.MaxAccel;
            double maxAngAccel = this.MaxAngularAccel;

            //NOTE: In Body_ApplyForceAndTorque(), it takes time into account.  So these equations just assume time is 1
            Vector3D accel = desiredVelocity - velocity;
            Vector3D angAccel = desiredAngVelocity - angVelocity;

            accel = CapVector(accel, maxAccel);
            angAccel = CapVector(angAccel, maxAngAccel);

            #endregion

            #region force

            Tuple<Vector3D?, Vector3D?> retVal = new Tuple<Vector3D?, Vector3D?>(accel, angAccel);

            retVal = ConvertToForce(retVal);

            #endregion

            return retVal;
        }

        private Tuple<Vector3D?, Vector3D?> GetSwarmForce(Tuple<MapObjectInfo, double, ForceSettings_Initial>[] neighbors, Tuple<SwarmObjectiveStrokes.Stroke, double> objectiveStroke, Point3D position, Vector3D velocity)
        {
            // Get the influence of the neighbors
            Vector3D? neighborAccel = GetNeighborAccel(this, neighbors, position, velocity);

            // Get the pull toward the objective stroke
            Vector3D? strokeAccel = GetStrokeAccel(objectiveStroke == null ? null : objectiveStroke.Item1, position, velocity);

            // Combine them (objective gets more influence as the bot gets closer)
            var retVal = CombineAccels(neighborAccel, strokeAccel, position);
            //var retVal = Tuple.Create(strokeAccel, (Vector3D?)null);

            if (retVal == null ||
                (
                (retVal.Item1 == null || retVal.Item1.Value.IsNearZero()) &&
                (retVal.Item2 == null || retVal.Item2.Value.IsNearZero())
                ))
            {
                return GetSeekForce();
            }

            // Cap it
            retVal = CapAccel(retVal);

            // Turn accel into force
            retVal = ConvertToForce(retVal);

            return retVal;
        }

        private static Vector3D? GetNeighborAccel(IMapObject thisBot, Tuple<MapObjectInfo, double, ForceSettings_Initial>[] neighbors, Point3D position, Vector3D velocity)
        {
            Vector3D? retVal = null;

            foreach (var neighbor in neighbors)
            {
                #region attract/repel

                ChasePoint_GetForceArgs args = new ChasePoint_GetForceArgs(thisBot, neighbor.Item1.Position - position);

                Vector3D? attractRepelAccel = MapObject_ChasePoint_Forces.GetForce(args, neighbor.Item3.Forces);

                #endregion
                #region match velocity

                Vector3D? accel = null;
                if (neighbor.Item3.MatchVelocityPercent != null)
                {
                    Vector3D matchVelocity = GetMatchVelocityForce(neighbor.Item1, velocity);

                    // Combine forces
                    if (attractRepelAccel == null)
                    {
                        accel = matchVelocity;
                    }
                    else
                    {
                        accel = Math3D.GetAverage(new[]
                        {
                            Tuple.Create(attractRepelAccel.Value, 1d),
                            Tuple.Create(matchVelocity, neighbor.Item3.MatchVelocityPercent.Value)        //NOTE: When the percent is 1 (100%), this will cause a 50/50 average with the other accel
                        });
                    }
                }
                else
                {
                    accel = attractRepelAccel;
                }

                #endregion

                // Add to total
                if (accel != null)
                {
                    if (retVal == null)
                    {
                        retVal = accel;
                    }
                    else
                    {
                        retVal = retVal.Value + accel.Value;
                    }
                }
            }

            return retVal;
        }

        private static Vector3D GetMatchVelocityForce(MapObjectInfo mapObject, Vector3D velocity)
        {
            //Vector3D dif = mapObject.Velocity - velocity;     // this just causes the whole blob to have an average of zero
            Vector3D dif = mapObject.Velocity;

            //TODO: Have a gradient of velocity match based on distance.  So if this is really far away from the item, then the item
            //should have no influence

            return dif;
        }

        private Vector3D? GetStrokeAccel(SwarmObjectiveStrokes.Stroke objectiveStroke, Point3D position, Vector3D velocity)
        {
            const double VELOCITYINFLUENCERADIUS = 2d;
            const double TOWARDRADIUSMULT = .5;     // making this smaller than the size of the velocity influence radius so there aren't any dead spots
            const double SKIPPOINTRADIUSMULT = 1d;

            if (objectiveStroke == null)
            {
                return null;
            }

            // Current stroke
            CurrentChasingStroke currentlyChasingStroke = _currentlyChasingStroke;
            if (currentlyChasingStroke == null || currentlyChasingStroke.Token != objectiveStroke.Token)
            {
                currentlyChasingStroke = new CurrentChasingStroke(objectiveStroke.Token);
                _currentlyChasingStroke = currentlyChasingStroke;
            }

            // Get affected by the stroke
            var hits = GetNearestSegmentsOrPoint(objectiveStroke.Points, position, currentlyChasingStroke.MinIndex);
            if (hits == null || (hits.Item1 == null && (hits.Item2 == null || hits.Item2.Length == 0)))
            {
                currentlyChasingStroke.MinIndex = objectiveStroke.Points.Length;
                currentlyChasingStroke.MinDistance = double.MaxValue;
                return null;
            }

            // Stats
            UpdateStrokeStats(currentlyChasingStroke, hits, objectiveStroke.Points, position, this.Radius * SKIPPOINTRADIUSMULT);

            #region build return

            Vector3D retVal = new Vector3D(0, 0, 0);

            double accel = this.MaxAccel;

            if (hits.Item1 != null)
            {
                retVal += GetStrokeAccel_Point(hits.Item1, position, accel, VELOCITYINFLUENCERADIUS, TOWARDRADIUSMULT);
            }

            if (hits.Item2 != null)
            {
                for (int cntr = 0; cntr < hits.Item2.Length; cntr++)
                {
                    bool shouldAttractToward = cntr == 0 && hits.Item1 == null;

                    retVal += GetStrokeAccel_Segment(hits.Item2[cntr], position, accel, VELOCITYINFLUENCERADIUS, TOWARDRADIUSMULT, shouldAttractToward);
                }
            }

            #endregion

            return retVal;
        }

        private static Vector3D GetStrokeAccel_Point(NearPointResult point, Point3D position, double accel, double velocityInfluenceRadius, double towardRadiusMult)
        {
            double radius = point.FullSegment[point.SegmentFrom].Item2.Length * velocityInfluenceRadius;

            // Acceleration toward
            Vector3D toward = GetStrokeAccel_Toward(point, position, accel, radius * towardRadiusMult);

            // Velocity influence
            Vector3D velocity = GetStrokeAccel_Velocity(point, radius);

            return toward + velocity;
        }
        private static Vector3D GetStrokeAccel_Segment(NearPointResult segment, Point3D position, double accel, double velocityInfluenceRadius, double towardRadiusMult, bool shouldAttractToward)
        {
            // LERP Radius
            double fromRadius = segment.FullSegment[segment.SegmentFrom].Item2.Length * velocityInfluenceRadius;
            double toRadius = segment.FullSegment[segment.SegmentTo].Item2.Length * velocityInfluenceRadius;
            double radius = UtilityCore.GetScaledValue(fromRadius, toRadius, 0d, 1d, segment.PercentAlongSegment);

            // Acceleration toward
            Vector3D toward = shouldAttractToward ?
                GetStrokeAccel_Toward(segment, position, accel, radius * towardRadiusMult) :
                new Vector3D(0, 0, 0);

            // Velocity influence
            Vector3D velocity = GetStrokeAccel_Velocity(segment, radius);

            return toward + velocity;
        }

        private static Vector3D GetStrokeAccel_Toward(NearPointResult destination, Point3D position, double accel, double radius)
        {
            if (destination.DistanceSquared.IsNearZero())
            {
                return new Vector3D(0, 0, 0);
            }

            // Go full acceleration directly toward
            Vector3D retVal = (destination.NearestPoint - position).ToUnit(false) * accel;

            if (destination.DistanceSquared < radius * radius)
            {
                // Influence drops to zero when really close to the point
                double percent = UtilityCore.GetScaledValue(0, 1, 0, radius, Math.Sqrt(destination.DistanceSquared));
                retVal *= percent;
            }

            return retVal;
        }
        private static Vector3D GetStrokeAccel_Velocity(NearPointResult destination, double radius)
        {
            if (destination.DistanceSquared > radius * radius)
            {
                return new Vector3D(0, 0, 0);
            }

            Vector3D retVal;
            if (destination.IsSegmentHit)
            {
                retVal = Math3D.LERP(destination.FullSegment[destination.SegmentFrom].Item2, destination.FullSegment[destination.SegmentTo].Item2, destination.PercentAlongSegment);
            }
            else
            {
                retVal = destination.FullSegment[destination.SegmentFrom].Item2;
            }

            double percent = UtilityCore.GetScaledValue(1, 0, 0, radius, Math.Sqrt(destination.DistanceSquared));

            return retVal * percent;
        }

        private static void UpdateStrokeStats(CurrentChasingStroke stroke, Tuple<NearPointResult, NearPointResult[]> hits, Tuple<Point3D, Vector3D>[] strokePoints, Point3D botPosition, double skipPointRadius)
        {
            List<NearPointResult> allHits = new List<NearPointResult>();
            allHits.Add(hits.Item1);
            if (hits.Item2 != null)
            {
                allHits.AddRange(hits.Item2);
            }

            double? distanceSquared = null;
            int? index = null;

            foreach (NearPointResult hit in allHits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (distanceSquared == null || distanceSquared.Value > hit.DistanceSquared)
                {
                    distanceSquared = hit.DistanceSquared;
                }

                int maxIndex = Math.Max(hit.SegmentFrom, hit.SegmentTo);
                if ((index == null || index.Value < maxIndex) && hit.DistanceSquared < skipPointRadius * skipPointRadius)
                {
                    index = maxIndex;
                }

                if (hit.SegmentTo == strokePoints.Length - 1 && Vector3D.DotProduct(botPosition - strokePoints[hit.SegmentFrom].Item1, strokePoints[hit.SegmentFrom].Item2) > 0)
                {
                    // Chasing the last point, and beyond the point's velocity.  So it would have to fly back to get to the point.
                    // This means that it is done chasing this stroke
                    index = strokePoints.Length;        // setting to an index beyond the stroke's range
                }
            }

            // Store the values
            if (distanceSquared != null)
            {
                stroke.MinDistance = Math.Sqrt(distanceSquared.Value);
            }

            if (index != null)
            {
                stroke.MinIndex = index.Value;
            }
        }

        private static Tuple<NearPointResult, NearPointResult[]> GetNearestSegmentsOrPoint(Tuple<Point3D, Vector3D>[] segment, Point3D position, int minIndex)
        {
            if (segment == null || segment.Length == 0 || minIndex >= segment.Length)
            {
                return null;
            }
            else if (minIndex == segment.Length - 1)
            {
                // No segments, just the last point
                return Tuple.Create(
                    new NearPointResult(0, (position - segment[0].Item1).LengthSquared, segment),
                    new NearPointResult[0]);
            }

            List<NearPointResult> pointHits = new List<NearPointResult>();
            List<NearPointResult> segmentHits = new List<NearPointResult>();

            for (int cntr = minIndex; cntr < segment.Length - 1; cntr++)
            {
                var closest = Math3D.GetClosestPoint_LineSegment_Point_verbose(segment[cntr].Item1, segment[cntr + 1].Item1, position);

                double distSqr = (position - closest.Item1).LengthSquared;

                switch (closest.Item2)
                {
                    case Math3D.LocationOnLineSegment.Start:
                        pointHits.Add(new NearPointResult(cntr, distSqr, segment));
                        break;

                    case Math3D.LocationOnLineSegment.Stop:
                        pointHits.Add(new NearPointResult(cntr + 1, distSqr, segment));
                        break;

                    case Math3D.LocationOnLineSegment.Middle:
                        #region middle

                        double segmentLen = (segment[cntr + 1].Item1 - segment[cntr].Item1).Length;

                        double percentAlong;
                        if (segmentLen.IsNearZero())
                        {
                            pointHits.Add(new NearPointResult(cntr, distSqr, segment));
                        }
                        else
                        {
                            percentAlong = (closest.Item1 - segment[cntr].Item1).Length / segmentLen;
                            segmentHits.Add(new NearPointResult(closest.Item1, cntr, cntr + 1, percentAlong, distSqr, segment));
                        }

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown Math3D.LocationOnLineSegment: " + closest.Item2.ToString());
                }
            }

            NearPointResult nearestPoint = pointHits.
                OrderBy(o => o.DistanceSquared).
                FirstOrDefault();

            segmentHits = segmentHits.
                OrderBy(o => o.DistanceSquared).
                ToList();

            // Only include a point hit if it's closer than all segment hits
            if (nearestPoint != null && segmentHits.Count > 0 && segmentHits[0].DistanceSquared <= nearestPoint.DistanceSquared)
            {
                nearestPoint = null;        // it's farther than segment hits, so throw it out
            }

            return Tuple.Create(nearestPoint, segmentHits.ToArray());
        }

        #region FAIL

        //private Tuple<Vector3D?, Vector3D?> GetSeekForce_1()
        //{
        //    #region velocity

        //    double maxSpeed = this.MaxSpeed;
        //    double maxAngSpeed = this.MaxAngularSpeed;

        //    Vector3D velocity = this.VelocityWorld;
        //    Vector3D angVelocity = this.PhysicsBody.AngularVelocity;

        //    Vector3D desiredVelocity = velocity + Math3D.GetRandomVector_Spherical(maxSpeed / 10);
        //    Vector3D desiredAngVelocity = angVelocity + Math3D.GetRandomVector_Spherical(maxAngSpeed / 10);

        //    desiredVelocity = CapVector(desiredVelocity, maxSpeed);
        //    desiredAngVelocity = CapVector(desiredAngVelocity, maxAngSpeed);

        //    #endregion

        //    #region acceleration

        //    double maxAccel = this.MaxAccel;
        //    double maxAngAccel = this.MaxAngularAccel;

        //    //TODO: Come up with better units for converting velocity to acceleration
        //    Vector3D accel = desiredAngVelocity - velocity;
        //    Vector3D angAccel = desiredAngVelocity - angVelocity;

        //    accel = CapVector(accel, maxAccel);
        //    angAccel = CapVector(angAccel, maxAngAccel);

        //    #endregion

        //    #region force

        //    // Multiply by mass to turn accel into force
        //    accel *= this.PhysicsBody.Mass;

        //    MassMatrix inertia = this.PhysicsBody.MassMatrix;
        //    double dot = Math.Abs(Vector3D.DotProduct(angAccel.ToUnit(), inertia.Inertia.ToUnit()));

        //    //TODO: Make sure this is right
        //    angAccel *= dot * inertia.Inertia.Length;

        //    #endregion

        //    return new Tuple<Vector3D?, Vector3D?>(accel, angAccel);
        //}

        //private Vector3D? GetStrokeForce_FIRST(SwarmObjectiveStrokes.Stroke objectiveStroke, Point3D position, Vector3D velocity)
        //{
        //    if (objectiveStroke == null)
        //    {
        //        return null;
        //    }

        //    // For now, just aim straight for the first point
        //    return (objectiveStroke.Points[0].Item1 - position).ToUnit(false) * this.MaxAccel;
        //}
        //private Vector3D? GetStrokeForce_HASREPULSE(SwarmObjectiveStrokes.Stroke objectiveStroke, Point3D position, Vector3D velocity)
        //{
        //    //const double CLOSEENOUGHRADIUSMULT = 2;
        //    //const double VELOCITYINFLUENCERADIUSMULT = 5;
        //    const double CLOSEENOUGHRADIUSMULT = 8;
        //    const double VELOCITYINFLUENCERADIUSMULT = 15;

        //    // Null stroke
        //    if (objectiveStroke == null)
        //    {
        //        lock (_currentStrokeLock) _currentlyChasingStroke = null;
        //        return null;
        //    }

        //    #region current stroke

        //    // Get currently chasing stroke

        //    CurrentChasingStroke currentStroke = null;
        //    lock (_currentStrokeLock)
        //    {
        //        currentStroke = _currentlyChasingStroke;
        //        if (currentStroke == null || currentStroke.Stroke.Token != objectiveStroke.Token)
        //        {
        //            currentStroke = new CurrentChasingStroke(objectiveStroke, ACCEL * 3);
        //            _currentlyChasingStroke = currentStroke;
        //        }
        //    }

        //    #endregion
        //    #region current index of stroke

        //    int currentIndex = currentStroke.CurrentIndex;
        //    Vector3D towardCurrentPoint;
        //    double towardCurrentPointLenSqr;

        //    while (true)
        //    {
        //        if (currentIndex >= currentStroke.Stroke.Points.Length)
        //        {
        //            // Already went through all the points
        //            return null;
        //        }

        //        // If too close to the current point, then advance to the next
        //        towardCurrentPoint = currentStroke.Stroke.Points[currentIndex].Item1 - position;
        //        towardCurrentPointLenSqr = towardCurrentPoint.LengthSquared;
        //        double closeEnoughDist = this.Radius * CLOSEENOUGHRADIUSMULT;

        //        if (towardCurrentPointLenSqr < closeEnoughDist * closeEnoughDist)
        //        {
        //            currentIndex++;
        //            currentStroke.CurrentIndex = currentIndex;
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    #endregion

        //    // Start with attraction to the current point
        //    Vector3D retVal = towardCurrentPoint.ToUnit(false) * this.MaxAccel;

        //    #region influence velocity

        //    // Add the desired velocity

        //    double velocityInfluenceDist = this.Radius * VELOCITYINFLUENCERADIUSMULT;

        //    if (towardCurrentPointLenSqr < velocityInfluenceDist * velocityInfluenceDist)
        //    {
        //        double percent = UtilityCore.GetScaledValue(1d, 0d, 0d, velocityInfluenceDist, Math.Sqrt(towardCurrentPointLenSqr));

        //        retVal += currentStroke.Stroke.Points[currentIndex].Item2 * percent;
        //    }

        //    #endregion
        //    #region repulse

        //    foreach (var repulse in currentStroke.UpcomingPoints[currentIndex])
        //    {
        //        Vector3D dirToPoint = position - repulse.Position;
        //        double lenSqr = dirToPoint.LengthSquared;

        //        if (lenSqr > repulse.EffectRadius * repulse.EffectRadius)
        //        {
        //            // Too far away to be influenced by this point
        //            continue;
        //        }

        //        if (lenSqr.IsNearZero())
        //        {
        //            retVal += Math3D.GetRandomVector_Spherical_Shell(repulse.Strength);
        //        }
        //        else
        //        {
        //            retVal += dirToPoint.ToUnit(false) * repulse.Strength;
        //        }
        //    }

        //    #endregion

        //    return retVal;
        //}
        //private Vector3D? GetStrokeForce_LATEST(SwarmObjectiveStrokes.Stroke objectiveStroke, Point3D position, Vector3D velocity)
        //{
        //    //const double CLOSEENOUGHRADIUSMULT = 2;
        //    //const double VELOCITYINFLUENCERADIUSMULT = 5;
        //    const double CLOSEENOUGHRADIUSMULT = 4;
        //    const double VELOCITYINFLUENCERADIUSMULT = 15;

        //    // Null stroke
        //    if (objectiveStroke == null)
        //    {
        //        lock (_currentStrokeLock) _currentlyChasingStroke = null;
        //        return null;
        //    }

        //    #region current stroke

        //    // Get currently chasing stroke

        //    CurrentChasingStroke currentStroke = null;
        //    lock (_currentStrokeLock)
        //    {
        //        currentStroke = _currentlyChasingStroke;
        //        if (currentStroke == null || currentStroke.Stroke.Token != objectiveStroke.Token)
        //        {
        //            currentStroke = new CurrentChasingStroke(objectiveStroke, ACCEL * 3);
        //            _currentlyChasingStroke = currentStroke;
        //        }
        //    }

        //    #endregion
        //    #region current index of stroke

        //    int currentIndex = currentStroke.CurrentIndex;
        //    Vector3D towardCurrentPoint;
        //    double towardCurrentPointLenSqr;

        //    while (true)
        //    {
        //        //if (currentIndex > 0)
        //        //{
        //        //    return null;
        //        //}

        //        if (currentIndex >= currentStroke.Stroke.Points.Length)
        //        {
        //            // Already went through all the points
        //            return null;
        //        }

        //        // If too close to the current point, then advance to the next
        //        towardCurrentPoint = currentStroke.Stroke.Points[currentIndex].Item1 - position;
        //        towardCurrentPointLenSqr = towardCurrentPoint.LengthSquared;
        //        double closeEnoughDist = this.Radius * CLOSEENOUGHRADIUSMULT;

        //        if (towardCurrentPointLenSqr < closeEnoughDist * closeEnoughDist)
        //        {
        //            currentIndex++;
        //            currentStroke.CurrentIndex = currentIndex;
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    #endregion

        //    // Start with attraction to the current point
        //    Vector3D retVal = towardCurrentPoint.ToUnit(false) * this.MaxAccel;

        //    #region influence velocity

        //    // Add the desired velocity

        //    //double velocityInfluenceDist = this.Radius * VELOCITYINFLUENCERADIUSMULT;

        //    //if (towardCurrentPointLenSqr < velocityInfluenceDist * velocityInfluenceDist)
        //    //{
        //    //    double percent = UtilityCore.GetScaledValue(1d, 0d, 0d, velocityInfluenceDist, Math.Sqrt(towardCurrentPointLenSqr));

        //    //    retVal += currentStroke.Stroke.Points[currentIndex].Item2 * percent;
        //    //}

        //    #endregion
        //    #region repulse

        //    //foreach (var repulse in currentStroke.UpcomingPoints[currentIndex])
        //    //{
        //    //    Vector3D dirToPoint = position - repulse.Position;
        //    //    double lenSqr = dirToPoint.LengthSquared;

        //    //    if (lenSqr > repulse.EffectRadius * repulse.EffectRadius)
        //    //    {
        //    //        // Too far away to be influenced by this point
        //    //        continue;
        //    //    }

        //    //    if (lenSqr.IsNearZero())
        //    //    {
        //    //        retVal += Math3D.GetRandomVector_Spherical_Shell(repulse.Strength);
        //    //    }
        //    //    else
        //    //    {
        //    //        retVal += dirToPoint.ToUnit(false) * repulse.Strength;
        //    //    }
        //    //}

        //    #endregion

        //    return retVal;
        //}
        //private Vector3D? GetStrokeForce_MULTINOVELOCITY(SwarmObjectiveStrokes.Stroke objectiveStroke, Point3D position, Vector3D velocity)
        //{
        //    const double CLOSEENOUGHRADIUSMULT = 3;

        //    if (objectiveStroke == null)
        //    {
        //        return null;
        //    }

        //    Tuple<long, int> stroke_index = _stroke_index;
        //    if (stroke_index == null || stroke_index.Item1 != objectiveStroke.Token)
        //    {
        //        stroke_index = Tuple.Create(objectiveStroke.Token, 0);
        //        _stroke_index = stroke_index;
        //    }

        //    Vector3D toObjective;
        //    while (true)
        //    {
        //        if (stroke_index.Item2 >= objectiveStroke.Points.Length)
        //        {
        //            return null;
        //        }

        //        toObjective = objectiveStroke.Points[stroke_index.Item2].Item1 - position;
        //        if (toObjective.LengthSquared > (this.Radius * CLOSEENOUGHRADIUSMULT) * (this.Radius * CLOSEENOUGHRADIUSMULT))
        //        {
        //            break;
        //        }

        //        stroke_index = Tuple.Create(stroke_index.Item1, stroke_index.Item2 + 1);
        //        _stroke_index = stroke_index;
        //    }

        //    // For now, just aim straight for the first point
        //    return (toObjective).ToUnit(false) * this.MaxAccel;
        //}
        //private Vector3D? GetStrokeForce_MULTIWITHVELOCITY(SwarmObjectiveStrokes.Stroke objectiveStroke, Point3D position, Vector3D velocity)
        //{
        //    const double CLOSEENOUGHRADIUSMULT = 3;
        //    const double VELOCITYINFLUENCERADIUSMULT = 7;

        //    // This fails when they are on or very near the stroke and try to swim up stream.  The point they are trying to
        //    // go to is pushing them along.
        //    //
        //    // So need to just move on to the next point (or maybe just closest point).  Need to take dot product of direction
        //    // and point's velocity into account

        //    if (objectiveStroke == null)
        //    {
        //        return null;
        //    }

        //    Tuple<long, int> stroke_index = _stroke_index;
        //    if (stroke_index == null || stroke_index.Item1 != objectiveStroke.Token)
        //    {
        //        stroke_index = Tuple.Create(objectiveStroke.Token, 0);
        //        _stroke_index = stroke_index;
        //    }

        //    #region get the next point

        //    Vector3D toObjective;
        //    while (true)
        //    {
        //        if (stroke_index.Item2 >= objectiveStroke.Points.Length)
        //        {
        //            return null;
        //        }

        //        toObjective = objectiveStroke.Points[stroke_index.Item2].Item1 - position;
        //        if (toObjective.LengthSquared > (this.Radius * CLOSEENOUGHRADIUSMULT) * (this.Radius * CLOSEENOUGHRADIUSMULT))
        //        {
        //            break;
        //        }

        //        stroke_index = Tuple.Create(stroke_index.Item1, stroke_index.Item2 + 1);
        //        _stroke_index = stroke_index;
        //    }

        //    #endregion

        //    // For now, just aim straight for the first point
        //    Vector3D retVal = (toObjective).ToUnit(false) * this.MaxAccel;

        //    #region influence velocity

        //    // Add the desired velocity

        //    double velocityInfluenceDist = this.Radius * VELOCITYINFLUENCERADIUSMULT;

        //    if (toObjective.LengthSquared < velocityInfluenceDist * velocityInfluenceDist)
        //    {
        //        double percent = UtilityCore.GetScaledValue(2d, 0d, 0d, velocityInfluenceDist, toObjective.Length);

        //        retVal += objectiveStroke.Points[stroke_index.Item2].Item2 * percent;
        //    }

        //    #endregion

        //    return retVal;
        //}
        //private Vector3D? GetStrokeForce_BETTER(SwarmObjectiveStrokes.Stroke objectiveStroke, Point3D position, Vector3D velocity)
        //{
        //    if (objectiveStroke == null)
        //    {
        //        return null;
        //    }

        //    double maxAccel = this.MaxAccel;
        //    double radius = this.Radius;

        //    //TODO: Don't just use the return length, have the function also return a weighted distance to the point (based on dot)
        //    //TODO: Remember which points are objectives, and never choose any index less than that (once null, ignore the stroke)

        //    int startIndex = 0;
        //    var strokeIndex = _stroke_index;
        //    if (strokeIndex != null && strokeIndex.Item1 == objectiveStroke.Token)
        //    {
        //        startIndex = strokeIndex.Item2;
        //    }

        //    if (startIndex >= objectiveStroke.Points.Length)
        //    {
        //        return null;
        //    }

        //    var candidates = Enumerable.Range(startIndex, objectiveStroke.Points.Length - startIndex).
        //        Select(o => new { Index = o, Item = objectiveStroke.Points[o], Force = GetStrokePointForce_BETTER(objectiveStroke.Points[o], position, velocity, maxAccel, radius) }).
        //        Where(o => o.Force != null).
        //        OrderBy(o => o.Force.Value.LengthSquared).
        //        FirstOrDefault();

        //    if (candidates == null)
        //    {
        //        _stroke_index = Tuple.Create(objectiveStroke.Token, objectiveStroke.Points.Length);
        //        return null;
        //    }

        //    _stroke_index = Tuple.Create(objectiveStroke.Token, candidates.Index);
        //    return candidates.Force;
        //}
        //private static Vector3D? GetStrokePointForce_BETTER(Tuple<Point3D, Vector3D> strokePoint, Point3D position, Vector3D velocity, double maxAccel, double radius)
        //{
        //    const double VELOCITYINFLUENCERADIUSMULT = 7;
        //    const double MINDOT = -.7;

        //    Vector3D toPoint = strokePoint.Item1 - position;
        //    double toPointLength = toPoint.Length;

        //    if (toPointLength.IsNearZero())
        //    {
        //        // Sitting on top of the point.  The only force felt is the point's velocity
        //        return strokePoint.Item2;
        //    }

        //    Vector3D toPointUnit = toPoint / toPointLength;

        //    double dot = Vector3D.DotProduct(toPointUnit, strokePoint.Item2.ToUnit());

        //    if (dot < MINDOT)
        //    {
        //        // It would have to fly too directly up stream to get to the point
        //        return null;
        //    }

        //    // Toward Point
        //    Vector3D retVal = toPointUnit * maxAccel;
        //    //if (dot < 0)     // The larger the dot, the larger the force (because when the dot is small, this point is in the wrong direction)
        //    //{
        //    //    double percent = UtilityCore.GetScaledValue(0d, 1d, MINDOT, 0d, dot);
        //    //    retVal *= percent;
        //    //}

        //    // Influence velocity
        //    double velocityInfluenceDist = radius * VELOCITYINFLUENCERADIUSMULT;

        //    if (toPointLength < velocityInfluenceDist)
        //    {
        //        double percent = UtilityCore.GetScaledValue(2d, 0d, 0d, velocityInfluenceDist, toPointLength);
        //        retVal += strokePoint.Item2 * percent;
        //    }

        //    //TODO: Compare a dot with the return force and location/velocity of point to see if it's even possible
        //    //to get to that point

        //    return retVal;
        //}

        #endregion

        #endregion
        #region Private Methods - initial forces

        private ForceSettings_Initial GetForceSetting_Initial(ref int chaseBotCount, MapObjectInfo item, int maxBotCount)
        {
            // Convert into a lookup vector, then do a fuzzy lookup (this will allow the bot to learn about objects instead
            // of a hardcoded switch statement)

            if (item.MapObject is Asteroid)
            {
                return _settings_Asteroid;
            }
            else if (item.MapObject is SwarmBot1a)
            {
                chaseBotCount++;
                if (chaseBotCount <= maxBotCount)
                {
                    return _settings_OtherBot_Chase;
                }
                else
                {
                    return _settings_OtherBot_Passive;
                }
            }
            else
            {
                return null;
            }
        }

        //TODO: Take in dna
        private static ForceSettings_Initial CreateForceSettingInitial_OtherBot_Chase()
        {
            List<ChasePoint_Force> forces = new List<ChasePoint_Force>();

            // Toward -- constant pull toward other bots
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Attract_Direction, ACCEL));

            // Repel -- ramp up repulsion if too close to other bots
            // Friction if about to collide
            forces.AddRange(GetRepelInitialForces());

            // Friction orthogonal to desired vector -- orth friction will help reduce orbits
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_Orth, ACCEL / 8));

            // Extra friction when overshooting the target (not sure if this is useful) -- big ramp up of friction when it gets really close to another bot
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway, ACCEL / 3.2));

            return new ForceSettings_Initial()
            {
                Forces = forces.ToArray(),
                MatchVelocityPercent = .75d,        // if this goes all the way to 100%, then you end up with a long string of bots, because they reach top speed, and the stragglers can't catch up
            };
        }
        private static ForceSettings_Initial CreateForceSettingInitial_OtherBot_Passive()
        {
            // This should only repel if it gets too close
            return new ForceSettings_Initial()
            {
                Forces = GetRepelInitialForces(),
            };
        }
        private static ForceSettings_Initial CreateForceSettingInitial_Asteroid()
        {
            // This should only repel if it gets too close
            return new ForceSettings_Initial()
            {
                Forces = GetRepelInitialForces(5, .2),
            };
        }

        private static ChasePoint_Force[] GetRepelInitialForces(double powerMult = 1, double distMult = 1)
        {
            //Tuple<double,double>[] gradient = GetDropoffGradient(5, 10, 1);
            Tuple<double, double>[] gradient = new[]
            {
                Tuple.Create(0d, 1d),
                Tuple.Create(10d * distMult, 0d),
            };

            ChasePoint_Force repel = new ChasePoint_Force(ChaseDirectionType.Attract_Direction, ACCEL * -3 * powerMult, gradient: gradient);

            gradient = new[]
            {
                Tuple.Create(0d, 1d),
                Tuple.Create(1.5 * distMult, .7d),
                Tuple.Create(3d * distMult, 0d),
            };
            ChasePoint_Force tooCloseAndHotFriction = new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_AlongIfVelocityToward, ACCEL * 4 * powerMult, gradient: gradient);

            return new[] { repel, tooCloseAndHotFriction };
        }

        /// <summary>
        /// This will run from 0 to maxX.  The core function will be 0 to 1, but results will be stretched
        /// </summary>
        private static Tuple<double, double>[] GetDropoffGradient(int count, double maxX, double maxY)
        {
            if (count < 2)
            {
                throw new ArgumentException("Count can't be less than 2");
            }

            double step = 1d / (count - 1);

            return Enumerable.Range(0, count).
                Select(o =>
                {
                    double x1 = o * step;
                    double x2 = 1d - x1;
                    //double y = x2 * x2 * x2;
                    double y = Math.Pow(x2, 1.6);

                    return Tuple.Create(x1 * maxX, y * maxY);
                }).
                ToArray();
        }

        #endregion
        #region Private Methods

        private Tuple<MapObjectInfo, double, ForceSettings_Initial>[] GetNeighbors(MapOctree snapshot, Point3D position)
        {
            // Get nearby items, sort by distance
            double searchRadius = this.SearchRadius;

            var initial = snapshot.GetItems(position, searchRadius).
                Where(o => o.Token != this.Token).
                Select(o => Tuple.Create(o, (o.Position - position).LengthSquared)).
                OrderBy(o => o.Item2).
                ToArray();

            // Get chase settings for each item
            //NOTE: This needs to be done after sorting on distance, because bots will have different settings if too many are actively chased
            var retVal = new List<Tuple<MapObjectInfo, double, ForceSettings_Initial>>();

            int chaseNeighborCount = this.ChaseNeighborCount;
            int currentNeighborCount = 0;

            foreach (var item in initial)
            {
                ForceSettings_Initial chaseProps = GetForceSetting_Initial(ref currentNeighborCount, item.Item1, chaseNeighborCount);

                if (chaseProps != null)
                {
                    retVal.Add(Tuple.Create(item.Item1, item.Item2, chaseProps));
                }
            }

            return retVal.ToArray();
        }

        private Vector3D? CapAccel_Linear(Vector3D? accel)
        {
            if (accel == null)
            {
                return null;
            }

            var retVal = new Tuple<Vector3D?, Vector3D?>(accel, null);

            retVal = CapAccel(retVal);

            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Item1;
            }
        }
        private Tuple<Vector3D?, Vector3D?> CapAccel(Tuple<Vector3D?, Vector3D?> accels)
        {
            if (accels == null)
            {
                return null;
            }

            Vector3D? linear = accels.Item1;
            if (linear != null)
            {
                Vector3D accel = linear.Value;

                // See if a force in this direction will exceed the max speed
                double maxSpeed = this.MaxSpeed;
                double maxSpeedSquared = maxSpeed * maxSpeed;
                Vector3D velocity = this.VelocityWorld;

                if ((velocity + accel).LengthSquared > maxSpeedSquared)
                {
                    #region over speed

                    double velLenSqr = velocity.LengthSquared;
                    if (velLenSqr < maxSpeedSquared && !velLenSqr.IsNearValue(maxSpeedSquared))        // only adjust the acceleration if the current velocity is under speed (because vel+accel will be over speed)
                    {
                        Point3D[] spherePoints, linePoints;
                        Math3D.GetClosestPoints_Sphere_Line(out spherePoints, out linePoints, new Point3D(0, 0, 0), maxSpeed, velocity.ToPoint(), accel, Math3D.RayCastReturn.AlongRayDirection);

                        if (spherePoints == null || spherePoints.Length == 0)
                        {
                            // This should never happen
                            accel = new Vector3D(0, 0, 0);
                        }
                        else
                        {
                            accel = linePoints[0] - velocity.ToPoint();        // there should only be one.  And the ray originated from inside the sphere, so spherePoints and linePoints should be the same
                        }
                    }
                    else if (Vector3D.DotProduct(velocity, accel) > 0)
                    {
                        // The velocity was already overspeed, and the acceleration wanted it to go faster.  Don't bother trying to reduce speed,
                        // just stop accelerating in this direction

                        //NOTE: I thought about just setting acceleration to zero, because even an orthogonal addition to velocity will increase
                        //the speed.  But if the bot was trying to get out of the way, this will let it have some movement instead of completely
                        //going dead
                        Vector3D alongAccel = accel.GetProjectedVector(velocity);
                        accel = accel - alongAccel;     // remove the component that is along the direction of the current velocity
                    }
                    // else, speed is already too great, but accel is against current velocity, so will slow it down

                    #endregion
                }

                // See if this is too large
                double maxAccel = this.MaxAccel;

                if (accel.LengthSquared > maxAccel * maxAccel)
                {
                    accel = accel.ToUnit(false) * maxAccel;
                }

                linear = accel;
            }

            Vector3D? angular = accels.Item2;
            if (angular != null)
            {
                //TODO: Angular
            }

            return new Tuple<Vector3D?, Vector3D?>(linear, angular);
        }
        private static Vector3D CapVector(Vector3D vector, double maxLength)
        {
            if (vector.LengthSquared <= maxLength * maxLength)
            {
                return vector;
            }

            return vector.ToUnit(false) * maxLength;
        }

        private Tuple<Vector3D?, Vector3D?> CombineAccels(Vector3D? neighborAccel, Vector3D? strokeAccel, Point3D position)
        {
            const double THRESHOLD = 10;        // the number of multiples of this.radius
            const double MINOBJECTIVEPERCENT = .7;
            const double MAXOBJECTIVEPERCENT = .99;

            #region nulls

            // It's easy if one of them is null
            if (neighborAccel == null && strokeAccel == null)
            {
                return null;
            }
            else if (strokeAccel == null)
            {
                return new Tuple<Vector3D?, Vector3D?>(neighborAccel, null);
            }
            else if (neighborAccel == null)
            {
                return new Tuple<Vector3D?, Vector3D?>(strokeAccel, null);
            }

            #endregion

            // They need to be capped before combining, or one could completely wash out the other
            neighborAccel = CapAccel_Linear(neighborAccel);
            strokeAccel = CapAccel_Linear(strokeAccel);

            if (neighborAccel == null || strokeAccel == null)
            {
                return CombineAccels(neighborAccel, strokeAccel, position);     // one of them went null.  Recurse, and let the above if statement catch that case
            }

            double? distance = null;
            var currentStroke = _currentlyChasingStroke;
            if (currentStroke != null)
            {
                distance = currentStroke.MinDistance;
            }

            Vector3D retVal;
            if (distance == null)
            {
                // This should never happen
                retVal = Math3D.GetAverage(new[] { neighborAccel.Value, strokeAccel.Value });
            }
            else
            {
                #region weighted average

                // The intention of this is the influence of the swarm reduces as the objective gets closer

                double numRadii = distance.Value / this.Radius;

                double percent;
                if (numRadii > THRESHOLD)
                {
                    percent = MINOBJECTIVEPERCENT;
                }
                else
                {
                    percent = UtilityCore.GetScaledValue(MAXOBJECTIVEPERCENT, MINOBJECTIVEPERCENT, 0d, THRESHOLD, numRadii);
                }

                retVal = Math3D.GetAverage(new[]
                {
                    Tuple.Create(neighborAccel.Value, 1d - percent),
                    Tuple.Create(strokeAccel.Value, percent),
                });

                #endregion
            }

            return new Tuple<Vector3D?, Vector3D?>(retVal, null);
        }

        private Tuple<Vector3D?, Vector3D?> ConvertToForce(Tuple<Vector3D?, Vector3D?> accel)
        {
            if (accel == null)
            {
                return null;
            }

            // Linear
            Vector3D? linear = accel.Item1;
            if (linear != null)
            {
                linear = linear.Value * this.PhysicsBody.Mass;      // f=ma
            }

            // Angular
            Vector3D? angular = accel.Item2;
            if (angular != null)
            {
                MassMatrix inertia = this.PhysicsBody.MassMatrix;
                double dot = Math.Abs(Vector3D.DotProduct(angular.Value.ToUnit(), inertia.Inertia.ToUnit()));

                //TODO: Make sure this is right
                angular = angular.Value * (dot * inertia.Inertia.Length);
            }

            return Tuple.Create(linear, angular);
        }

        private static Model3D GetModel(double radius, Color? color = null)
        {
            Model3DGroup retVal = new Model3DGroup();

            // Blue
            MaterialGroup materials = new MaterialGroup();
            //TODO: Look at color
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("4D56A4"))));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C04AF8FF")), 4));
            materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("104D4960"))));

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_Ico(radius * .75, 1, true);

            retVal.Children.Add(geometry);

            // White
            materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0FFF9DA"))));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A07972A6")), 30));
            materials.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("209C91B3"))));

            geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            var ball = UtilityWPF.GetDodecahedron(radius);
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(ball.AllTriangles);

            retVal.Children.Add(geometry);

            return retVal;
        }

        //TODO: This may get more complex if the swarmbot levels up / adds powerups
        private static double GetMass(double radius)
        {
            const double DENSITY = 1;       // this should be really light -- high speed, low mass -- like an energy ball

            double volume = 4d / 3d * Math.PI * radius * radius * radius;
            return DENSITY * volume;
        }

        #endregion
    }

    #region Class: SwarmClusters

    /// <summary>
    /// This maintains a list of groups of swarmbots
    /// </summary>
    /// <remarks>
    /// This periodically queries the map, and runs swarmbot positions through a SOM
    /// 
    /// This can be used when searching for nearest group, or assigning objectives
    /// </remarks>
    public class SwarmClusters
    {
        #region Declaration Section

        private readonly Map _map;

        #endregion

        #region Constructor

        public SwarmClusters(Map map)
        {
            _map = map;
        }

        #endregion

        public volatile SwarmCluster[] Clusters = null;

        public void Tick()
        {
            var bots = _map.GetItems<SwarmBot1a>(false).ToArray();

            if (bots.Length == 0)
            {
                this.Clusters = null;
                return;
            }

            SOMInput<SwarmBot1a>[] inputs = bots.
                Select(o => new SOMInput<SwarmBot1a>() { Source = o, Weights = o.PositionWorld.ToVectorND() }).
                ToArray();

            SOMRules rules = SOMRules.GetRandomRules();

            SOMResult som = SelfOrganizingMaps.TrainSOM(inputs, rules, false);

            SwarmCluster[] clusters = som.InputsByNode.
                Select(o =>
                {
                    SwarmBot1a[] bots2 = o.
                        Select(p => ((SOMInput<SwarmBot1a>)p).Source).
                        ToArray();

                    return new SwarmCluster(bots2);
                }).
                ToArray();

            this.Clusters = clusters;
        }
    }

    #endregion
    #region Class: SwarmCluster

    public class SwarmCluster
    {
        public SwarmCluster(SwarmBot1a[] bots)
        {
            this.Bots = bots;
        }

        public readonly SwarmBot1a[] Bots;
        public readonly long Token = TokenGenerator.NextToken();

        /// <summary>
        /// This looks at all the bots in the cluster that aren't disposed, and returns current centerpoint, velocity, etc
        /// NOTE: This will return null if all bots are disposed
        /// </summary>
        public SwarmClusterInfo GetCurrentInfo()
        {
            Tuple<Point3D, Vector3D>[] positionsVelocities = this.Bots.
                Where(o => !o.IsDisposed).
                Select(o => Tuple.Create(o.PositionWorld, o.VelocityWorld)).
                ToArray();

            if (positionsVelocities.Length == 0)
            {
                return null;
            }

            return new SwarmClusterInfo(positionsVelocities);
        }

        public static bool IsSame(SwarmCluster[] cluster1, SwarmCluster[] cluster2)
        {
            if (cluster1 == null && cluster2 == null)
            {
                return true;
            }
            else if (cluster1 == null || cluster2 == null)
            {
                return false;
            }
            else if (cluster1.Length != cluster2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < cluster1.Length; cntr++)
            {
                if (cluster1[cntr].Token != cluster2[cntr].Token)
                {
                    return false;
                }
            }

            return true;
        }
    }

    #endregion
    #region Class: SwarmClusterInfo

    public class SwarmClusterInfo
    {
        public SwarmClusterInfo(Tuple<Point3D, Vector3D>[] positionsVelocities)
        {
            this.PositionsVelocities = positionsVelocities;

            Point3D[] positions = positionsVelocities.
                Select(o => o.Item1).
                ToArray();

            this.Count = positionsVelocities.Length;
            this.Center = Math3D.GetCenter(positions);
            this.Velocity = Math3D.GetAverage(positionsVelocities.Select(o => o.Item2));
            this.AABB = Math3D.GetAABB(positions);

            var avg_stddev = Math1D.Get_Average_StandardDeviation(positions.Select(o => (o - this.Center).Length));

            this.Radius = avg_stddev.Item1 + (avg_stddev.Item2 * 2);      // 95% of items are within 2 standard deviations, so use that as the radius
        }

        public readonly int Count;
        public readonly Point3D Center;
        public readonly Vector3D Velocity;
        public readonly Tuple<Point3D, Point3D> AABB;
        /// <summary>
        /// This is approximate radius.  There will be some bots outside this radius, but most will be inside
        /// </summary>
        public readonly double Radius;

        public readonly Tuple<Point3D, Vector3D>[] PositionsVelocities;
    }

    #endregion

    #region Class: SwarmObjectiveStrokes

    //TODO: Make a class that looks at strokes and available bots.  Then assign strokes to bots.  Otherwise the bots will just
    //mob objectives, and leave others untouched, or go out of their way when other bots are closer
    //
    //It would be cool if this intermediate class isn't some hardcoded controller, but a way for them to communicate with each
    //other.  Somehow grow a group brain
    //
    //The bots talk to the local group, and that group votes on how desirable the objecitve is.  Set up a feedback so an upvoted
    //target will be less desirable.  Also have some persistance so a group commits to an objective and doesn't flake between two
    //
    //Have a group NN that considers current position, velocity, distance from stroke, shape of stroke, items near stroke, etc

    public class SwarmObjectiveStrokes
    {
        #region Class: Stroke

        public class Stroke
        {
            public Stroke(Tuple<Point3D, Vector3D>[] points, double deathTime)
            {
                this.Token = TokenGenerator.NextToken();
                this.Points = points;
                this.DeathTime = deathTime;
            }

            public readonly long Token;
            /// <summary>
            /// Points and Velocities
            /// </summary>
            public readonly Tuple<Point3D, Vector3D>[] Points;

            //TODO: If a swarm has locked on to this stroke, then be lenient with the death time.  Also, if a swarm has gone
            //through the stroke, remove this early
            /// <summary>
            /// How long this stroke should stay around
            /// </summary>
            public readonly double DeathTime;
        }

        #endregion
        #region Class: PointsChanged

        public class PointsChangedArgs
        {
            public PointsChangedArgs(PointChangeType changeType, Tuple<Point3D, Vector3D>[] points, long? strokeToken = null)
            {
                this.ChangeType = changeType;
                this.Points = points;
                this.StrokeToken = strokeToken;
            }

            public readonly PointChangeType ChangeType;
            public readonly Tuple<Point3D, Vector3D>[] Points;
            public readonly long? StrokeToken;
        }

        #endregion
        #region Enum: PointChangeType

        public enum PointChangeType
        {
            AddNewStroke,
            AddingPointsToStroke,
            ConvertStroke_Add,
            ConvertStroke_Remove,
            RemoveStroke_Timeout,
            RemoveStroke_Remove,
            RemoveStroke_Clear,
        }

        #endregion

        #region Events

        public event EventHandler<PointsChangedArgs> PointsChanged = null;

        #endregion

        #region Declaration Section

        private readonly object _lock = new object();

        private readonly WorldClock _clock;
        private readonly double _strokeLife_Seconds;
        private readonly double _smallestSubstrokeSize;

        private readonly List<Stroke> _strokes = new List<Stroke>();

        //TODO: Also store the time of each point.  Speed of line segments should influence velocity
        private readonly List<Tuple<Point3D, Vector3D?>> _addingPoints = new List<Tuple<Point3D, Vector3D?>>();

        #endregion

        #region Constructor

        public SwarmObjectiveStrokes(WorldClock clock, double smallestSubstrokeSize, double strokeLife_Seconds = 4)
        {
            _clock = clock;
            _strokeLife_Seconds = strokeLife_Seconds;
            _smallestSubstrokeSize = smallestSubstrokeSize;
        }

        #endregion

        #region Public Methods

        public void AddPointToStroke(Point3D point, Vector3D? velocity = null)
        {
            // Add point
            lock (_lock)
            {
                _addingPoints.Add(Tuple.Create(point, velocity));
            }

            // Raise event
            if (this.PointsChanged != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.AddingPointsToStroke, new[] { Tuple.Create(point, velocity ?? new Vector3D(0, 0, 0)) });
                this.PointsChanged(this, args);
            }
        }
        public void StopStroke()
        {
            Tuple<Point3D, Vector3D?>[] fromPoints;
            Tuple<Point3D, Vector3D>[] toPoints;
            long token;

            lock (_lock)
            {
                if (_addingPoints.Count == 0)
                {
                    return;
                }

                // Build refined stroke
                fromPoints = _addingPoints.ToArray();
                toPoints = BuildStroke(fromPoints, _smallestSubstrokeSize);

                _addingPoints.Clear();
                _strokes.Add(new Stroke(toPoints, GetStopTime()));
                token = _strokes[_strokes.Count - 1].Token;
            }

            // Raise events
            if (this.PointsChanged != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.ConvertStroke_Remove, fromPoints.Select(o => Tuple.Create(o.Item1, o.Item2 ?? new Vector3D(0, 0, 0))).ToArray());
                this.PointsChanged(this, args);

                args = new PointsChangedArgs(PointChangeType.ConvertStroke_Add, toPoints, token);
                this.PointsChanged(this, args);
            }
        }

        public void AddStroke(IEnumerable<Point3D> points)
        {
            Tuple<Point3D, Vector3D>[] eventPoints;
            long token;

            // Add it
            lock (_lock)
            {
                eventPoints = BuildStroke(points.ToArray(), _smallestSubstrokeSize);
                _strokes.Add(new Stroke(eventPoints, GetStopTime()));

                token = _strokes[_strokes.Count - 1].Token;
            }

            // Raise Event
            if (this.PointsChanged != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.AddNewStroke, eventPoints, token);
                this.PointsChanged(this, args);
            }
        }

        public void RemoveStroke(long token)
        {
            Tuple<Point3D, Vector3D>[] eventPoints = null;

            lock (_lock)
            {
                // Find it
                for (int cntr = 0; cntr < _strokes.Count; cntr++)
                {
                    if (_strokes[cntr].Token == token)
                    {
                        // Remove it
                        eventPoints = _strokes[cntr].Points;
                        _strokes.RemoveAt(cntr);
                        break;
                    }
                }
            }

            // Raise Event
            if (this.PointsChanged != null && eventPoints != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.RemoveStroke_Remove, eventPoints, token);
                this.PointsChanged(this, args);
            }
        }

        public void Clear()
        {
            var eventPoints = new List<Tuple<Tuple<Point3D, Vector3D>[], long?>>();

            lock (_lock)
            {
                // Remove pre points
                if (_addingPoints.Count > 0)
                {
                    eventPoints.Add(Tuple.Create(_addingPoints.Select(o => Tuple.Create(o.Item1, o.Item2 ?? new Vector3D(0, 0, 0))).ToArray(), (long?)null));
                    _addingPoints.Clear();
                }

                // Remove strokes
                while (_strokes.Count > 0)
                {
                    eventPoints.Add(Tuple.Create(_strokes[0].Points, (long?)_strokes[0].Token));
                    _strokes.RemoveAt(0);
                }
            }

            // Raise Event
            if (this.PointsChanged != null && eventPoints.Count > 0)
            {
                foreach (var points in eventPoints)
                {
                    PointsChangedArgs args = new PointsChangedArgs(PointChangeType.RemoveStroke_Clear, points.Item1, points.Item2);
                    this.PointsChanged(this, args);
                }
            }
        }

        public void Tick()
        {
            var eventPoints = new List<Tuple<Tuple<Point3D, Vector3D>[], long>>();

            lock (_lock)
            {
                double currentTime = _clock.CurrentTime;

                int index = 0;
                while (index < _strokes.Count)
                {
                    if (currentTime > _strokes[index].DeathTime)
                    {
                        // Remove old stroke
                        eventPoints.Add(Tuple.Create(_strokes[index].Points, _strokes[index].Token));
                        _strokes.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            // Raise Events
            if (this.PointsChanged != null && eventPoints.Count > 0)
            {
                foreach (var points in eventPoints)
                {
                    PointsChangedArgs args = new PointsChangedArgs(PointChangeType.RemoveStroke_Timeout, points.Item1, points.Item2);
                    this.PointsChanged(this, args);
                }
            }
        }

        /// <summary>
        /// This returns the nearest stroke, and the distance from it
        /// </summary>
        public Tuple<Stroke, double> GetNearestStroke(Point3D point, double searchRadius)
        {
            // Only need to look for the first point
            lock (_lock)
            {
                double searchSquared = searchRadius * searchRadius;

                Stroke retVal = null;
                double distanceSquared = double.MaxValue;

                foreach (Stroke stroke in _strokes)
                {
                    double dist = (stroke.Points[0].Item1 - point).LengthSquared;
                    if (dist <= searchSquared && dist < distanceSquared)
                    {
                        retVal = stroke;
                        distanceSquared = dist;
                    }
                }

                if (retVal == null)
                {
                    return null;
                }
                else
                {
                    return Tuple.Create(retVal, Math.Sqrt(distanceSquared));
                }
            }
        }

        public static Tuple<Point3D, Vector3D>[] BuildStroke(Point3D[] points, double smallestSubstrokeSize)
        {
            return BuildStroke(points.
                Select(o => new Tuple<Point3D, Vector3D?>(o, null)).ToArray(),
                smallestSubstrokeSize);
        }
        public static Tuple<Point3D, Vector3D>[] BuildStroke_ORIG(Tuple<Point3D, Vector3D?>[] points, double smallestSubstrokeSize)
        {
            //TODO: Apply a bezier.  Try to reduce the number of points (only need points in direction changes)
            //If nulls are passed in, figure out a good velocity

            return points.
                Select(o => Tuple.Create(o.Item1, o.Item2 ?? new Vector3D(0, 0, 0))).
                ToArray();
        }
        public static Tuple<Point3D, Vector3D>[] BuildStroke(Tuple<Point3D, Vector3D?>[] points, double smallestSubstrokeSize)
        {
            //TODO: Use the velocities that are passed in
            //TODO: Look for the defining points of the curve (points where radius of curve is smallest, also inflection points)
            //TODO: Try to always include the final point (unless it's too close to the beginning)

            if (points == null || points.Length == 0)
            {
                return new Tuple<Point3D, Vector3D>[0];
            }

            double smallestSqr = smallestSubstrokeSize * smallestSubstrokeSize;

            #region determine points

            List<Point3D> finalPoints = new List<Point3D>();

            finalPoints.Add(points[0].Item1);

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                if ((points[cntr].Item1 - finalPoints[finalPoints.Count - 1]).LengthSquared > smallestSqr)
                {
                    finalPoints.Add(points[cntr].Item1);
                }
            }

            #endregion
            #region determine velocities

            Tuple<Point3D, Vector3D>[] retVal = new Tuple<Point3D, Vector3D>[finalPoints.Count];

            for (int cntr = 0; cntr < finalPoints.Count - 1; cntr++)
            {
                retVal[cntr] = Tuple.Create(finalPoints[cntr], finalPoints[cntr + 1] - finalPoints[cntr]);
            }

            if (retVal.Length == 1)
            {
                if (points.Length == 1)
                {
                    // Only one point, so don't bother with a velocity 
                    retVal[0] = Tuple.Create(finalPoints[0], new Vector3D(0, 0, 0));
                }
                else
                {
                    // The last point didn't make it to the final list, but it can still be used for velocity
                    retVal[0] = Tuple.Create(finalPoints[0], points[points.Length - 1].Item1 - finalPoints[0]);
                }
            }
            else
            {
                // The last point should just continue the velocity
                //TODO: May want to try harder to calculate the radius of the curve, and let that influence the velocity (or just
                //average the previous couple points)
                retVal[retVal.Length - 1] = Tuple.Create(finalPoints[retVal.Length - 1], retVal[retVal.Length - 2].Item2);
            }

            #endregion

            return retVal;
        }

        #endregion

        #region Private Methods

        private double GetStopTime()
        {
            //TODO: May want a longer life if it's an elaborate stroke, or far from others, or if there are a lot of existing strokes
            return _clock.CurrentTime + _strokeLife_Seconds;
        }

        #endregion
    }

    #endregion
}
