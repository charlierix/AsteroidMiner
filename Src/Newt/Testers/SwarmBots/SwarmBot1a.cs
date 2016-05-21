using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.SwarmBots
{
    /// <summary>
    /// This is meant to be a simplistic working swarmbot.  Lots of hardcoding
    /// </summary>
    /// <remarks>
    /// This first version doesn't know about the mothership, or about desired target paths/objectives
    /// 
    /// It doesn't have fuzzy definitions of map objects with corresponding force settings (it has one
    /// for other swarmbots, and a second for asteroids)
    /// 
    /// TOTO: There needs to be some global props about desired distance:
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
            #region Constructor

            public CurrentChasingStroke(SwarmObjectiveStrokes.Stroke stroke, double repulseStrength)
            {
                this.Stroke = stroke;

                this.UpcomingPoints = CalculateRepulseForces(stroke, repulseStrength);
            }

            #endregion

            public readonly SwarmObjectiveStrokes.Stroke Stroke;

            //public readonly StrokePointForce[] ActivePoints;      // no need for special rules with attract.  Just go full accel toward
            //public readonly StrokePointForce[] UpcomingPoints;      // the points that have already been passed have no more influence

            /// <summary>
            /// These will repulse the bot.  The purpose of these is to keep the bot from traveling along the stroke backward to
            /// get to the first point (push the bot to the side so it takes a more circular path)
            /// </summary>
            public readonly StrokePointForce[][] UpcomingPoints;

            public volatile int CurrentIndex = 0;

            #region Private Methods

            private static StrokePointForce[][] CalculateRepulseForces(SwarmObjectiveStrokes.Stroke stroke, double repulseStrength)
            {
                StrokePointForce[][] retVal = new StrokePointForce[stroke.Points.Length][];

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    retVal[cntr] = CalculateRepulseForces_Index(stroke, cntr, repulseStrength);
                }

                return retVal;
            }
            private static StrokePointForce[] CalculateRepulseForces_Index(SwarmObjectiveStrokes.Stroke stroke, int index, double repulseStrength)
            {
                //NOTE: The last index will be an empty array.  Doing this so the caller doesn't need range checks, they just
                //iterate through the forces and come up with nothing
                List<StrokePointForce> retVal = new List<StrokePointForce>();

                Point3D point = stroke.Points[index].Item1;
                Vector3D direction = stroke.Points[index].Item2;

                for (int cntr = index + 1; cntr < stroke.Points.Length; cntr++)
                {
                    StrokePointForce force = CalculateRepulseForces_Index_On(stroke, cntr, point, direction, repulseStrength);
                    if (force == null)
                    {
                        // Once a null is encountered, the stroke has come back too far.  Just stop looking at the rest of the stroke.
                        // It could also be valid to continue processing the stroke, and keep any points that loop back in front, but
                        // that's extra processing, and a bit silly if they are just drawing spirals
                        break;
                    }

                    retVal.Add(force);

                    force = CalculateRepulseForces_Index_Half(stroke, cntr, point, direction, repulseStrength);
                    if (force == null)
                    {
                        break;
                    }

                    retVal.Add(force);
                }

                return retVal.ToArray();
            }
            //TODO: Increase the influence radius a bit when the point is far from the initial
            private static StrokePointForce CalculateRepulseForces_Index_On(SwarmObjectiveStrokes.Stroke stroke, int index, Point3D origPoint, Vector3D origDir, double repulseStrength)
            {
                double prevRadSqr = stroke.Points[index - 1].Item2.LengthSquared;
                double curRadSqr = stroke.Points[index].Item2.LengthSquared;

                double radius;
                if (prevRadSqr < curRadSqr)
                {
                    radius = Math.Sqrt(prevRadSqr);

                    // no need for a dot product check here, because it's not advancing past the current point
                }
                else
                {
                    radius = Math.Sqrt(curRadSqr);

                    // Make sure this doesn't loop back too far
                    Point3D extendPoint = stroke.Points[index].Item1 + (stroke.Points[index].Item2 * .5);
                    if (Vector3D.DotProduct(extendPoint - origPoint, origDir) < 0)
                    {
                        return null;
                    }
                }

                radius = RepulseIncreaseRadius(origPoint, stroke.Points[index].Item1, radius);

                return new StrokePointForce(stroke.Points[index].Item1, radius, repulseStrength);
            }
            private static StrokePointForce CalculateRepulseForces_Index_Half(SwarmObjectiveStrokes.Stroke stroke, int index, Point3D origPoint, Vector3D origDir, double repulseStrength)
            {
                Point3D to;
                if (index < stroke.Points.Length - 1)
                {
                    to = stroke.Points[index + 1].Item1;
                }
                else
                {
                    // This is the extension of the last item, so just use its velocity
                    to = stroke.Points[index].Item1 + stroke.Points[index].Item2;
                }

                if (Vector3D.DotProduct(to - origPoint, origDir) < 0)
                {
                    // This is trying to loop back beyond the original point
                    return null;
                }

                Point3D center = stroke.Points[index].Item1 + ((to - stroke.Points[index].Item1) * .5);

                double radius = RepulseIncreaseRadius(origPoint, center, (center - stroke.Points[index].Item1).Length);

                return new StrokePointForce(center, radius, repulseStrength);
            }

            private static double RepulseIncreaseRadius(Point3D origPoint, Point3D repulsePoint, double radius)
            {
                double distance = (repulsePoint - origPoint).Length;
                double ratio = distance / radius;

                if (ratio < 1)
                {
                    // This should never happen
                    return distance * .9;
                }

                //TODO: Increase by ((ratio - 1) / 2d) + 1, or something
                //But don't let it grow beyond (radius * 8), or something
                //  :)

                return radius;
            }

            #endregion
        }

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

        #region Declaration Section

        private const double ACCEL = 5;

        private readonly Map _map;
        private readonly SwarmObjectiveStrokes _strokes;

        private volatile Tuple<Vector3D?, Vector3D?> _applyForceTorque = null;

        private ForceSettings_Initial _settings_OtherBot_Chase = null;
        private ForceSettings_Initial _settings_OtherBot_Passive = null;
        private ForceSettings_Initial _settings_Asteroid = null;

        private readonly object _currentStrokeLock = new object();
        private CurrentChasingStroke _currentlyChasingStroke = null;

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

        private Tuple<Vector3D?, Vector3D?> GetSeekForce_1()
        {
            #region velocity

            double maxSpeed = this.MaxSpeed;
            double maxAngSpeed = this.MaxAngularSpeed;

            Vector3D velocity = this.VelocityWorld;
            Vector3D angVelocity = this.PhysicsBody.AngularVelocity;

            Vector3D desiredVelocity = velocity + Math3D.GetRandomVector_Spherical(maxSpeed / 10);
            Vector3D desiredAngVelocity = angVelocity + Math3D.GetRandomVector_Spherical(maxAngSpeed / 10);

            desiredVelocity = CapVector(desiredVelocity, maxSpeed);
            desiredAngVelocity = CapVector(desiredAngVelocity, maxAngSpeed);

            #endregion

            #region acceleration

            double maxAccel = this.MaxAccel;
            double maxAngAccel = this.MaxAngularAccel;

            //TODO: Come up with better units for converting velocity to acceleration
            Vector3D accel = desiredAngVelocity - velocity;
            Vector3D angAccel = desiredAngVelocity - angVelocity;

            accel = CapVector(accel, maxAccel);
            angAccel = CapVector(angAccel, maxAngAccel);

            #endregion

            #region force

            // Multiply by mass to turn accel into force
            accel *= this.PhysicsBody.Mass;

            MassMatrix inertia = this.PhysicsBody.MassMatrix;
            double dot = Math.Abs(Vector3D.DotProduct(angAccel.ToUnit(), inertia.Inertia.ToUnit()));

            //TODO: Make sure this is right
            angAccel *= dot * inertia.Inertia.Length;

            #endregion

            return new Tuple<Vector3D?, Vector3D?>(accel, angAccel);
        }
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

            // Multiply by mass to turn accel into force
            accel *= this.PhysicsBody.Mass;

            MassMatrix inertia = this.PhysicsBody.MassMatrix;
            double dot = Math.Abs(Vector3D.DotProduct(angAccel.ToUnit(), inertia.Inertia.ToUnit()));

            //TODO: Make sure this is right
            angAccel *= dot * inertia.Inertia.Length;

            #endregion

            return new Tuple<Vector3D?, Vector3D?>(accel, angAccel);
        }

        private Tuple<Vector3D?, Vector3D?> GetSwarmForce(Tuple<MapObjectInfo, double, ForceSettings_Initial>[] neighbors, Tuple<SwarmObjectiveStrokes.Stroke, double> objectiveStroke, Point3D position, Vector3D velocity)
        {
            // Get the influence of the neighbors
            Vector3D? neighborForce = GetNeighborForce(neighbors, position, velocity);

            // Get the pull toward the objective stroke
            Vector3D? strokeForce = GetStrokeForce(objectiveStroke, position, velocity);

            // Combine them (objective gets more influence as the bot gets closer)
            var retVal = CombineForces(neighborForce, strokeForce, objectiveStroke == null ? (double?)null : objectiveStroke.Item2);

            if (retVal == null ||
                (
                (retVal.Item1 == null || retVal.Item1.Value.IsNearZero()) &&
                (retVal.Item2 == null || retVal.Item2.Value.IsNearZero())
                ))
            {
                return GetSeekForce();
            }

            // Cap it
            retVal = CapForces(retVal);

            return retVal;
        }

        private static Vector3D? GetNeighborForce(Tuple<MapObjectInfo, double, ForceSettings_Initial>[] neighbors, Point3D position, Vector3D velocity)
        {
            Vector3D? retVal = null;

            foreach (var neighbor in neighbors)
            {
                #region attract/repel

                ChasePoint_GetForceArgs args = new ChasePoint_GetForceArgs(neighbor.Item1.MapObject, neighbor.Item1.Position - position);

                Vector3D? attractRepelForce = MapObject_ChasePoint_Forces.GetForce(args, neighbor.Item3.Forces);

                #endregion
                #region match velocity

                Vector3D? force = null;
                if (neighbor.Item3.MatchVelocityPercent != null)
                {
                    Vector3D matchVelocity = GetMatchVelocityForce(neighbor.Item1, velocity);

                    // Combine forces
                    if (attractRepelForce == null)
                    {
                        force = matchVelocity;
                    }
                    else
                    {
                        force = Math3D.GetCenter(new[]
                        {
                            Tuple.Create(attractRepelForce.Value.ToPoint(), 1d),
                            Tuple.Create(matchVelocity.ToPoint(), neighbor.Item3.MatchVelocityPercent.Value)        //NOTE: When the percent is 1 (100%), this will cause a 50/50 average with the other force
                        }).ToVector();
                    }
                }
                else
                {
                    force = attractRepelForce;
                }

                #endregion

                // Add to total
                if (force != null)
                {
                    if (retVal == null)
                    {
                        retVal = force;
                    }
                    else
                    {
                        retVal = retVal.Value + force.Value;
                    }
                }
            }

            return retVal;
        }

        private Vector3D? GetStrokeForce_FIRSTPOINT(Tuple<SwarmObjectiveStrokes.Stroke, double> objectiveStroke, Point3D position, Vector3D velocity)
        {
            if (objectiveStroke == null)
            {
                return null;
            }

            // For now, just aim straight for the first point
            return (objectiveStroke.Item1.Points[0].Item1 - position).ToUnit(false) * this.MaxAccel;
        }
        private Vector3D? GetStrokeForce(Tuple<SwarmObjectiveStrokes.Stroke, double> objectiveStroke, Point3D position, Vector3D velocity)
        {
            const double CLOSEENOUGHRADIUSMULT = 2;

            // Null stroke
            if (objectiveStroke == null)
            {
                lock (_currentStrokeLock) _currentlyChasingStroke = null;
                return null;
            }

            #region current stroke

            // Get currently chasing stroke

            CurrentChasingStroke currentStroke = null;
            lock (_currentStrokeLock)
            {
                currentStroke = _currentlyChasingStroke;
                if (currentStroke == null || currentStroke.Stroke.Token != objectiveStroke.Item1.Token)
                {
                    currentStroke = new CurrentChasingStroke(objectiveStroke.Item1, ACCEL * 3);
                    _currentlyChasingStroke = currentStroke;
                }
            }

            #endregion
            #region current index of stroke

            int currentIndex = currentStroke.CurrentIndex;
            Vector3D towardCurrentPoint;

            while (true)
            {
                if (currentIndex >= currentStroke.Stroke.Points.Length)
                {
                    // Already went through all the points
                    return null;
                }

                // If too close to the current point, then advance to the next
                towardCurrentPoint = currentStroke.Stroke.Points[currentIndex].Item1 - position;
                double closeEnoughDist = this.Radius * CLOSEENOUGHRADIUSMULT;

                if (towardCurrentPoint.LengthSquared < closeEnoughDist * closeEnoughDist)
                {
                    currentIndex++;
                    currentStroke.CurrentIndex = currentIndex;
                }
                else
                {
                    break;
                }
            }

            #endregion

            // Start with attraction to the current point
            Vector3D retVal = towardCurrentPoint.ToUnit(false) * this.MaxAccel;

            foreach (var repulse in currentStroke.UpcomingPoints[currentIndex])
            {
                Vector3D dirToPoint = position - repulse.Position;
                double lenSqr = dirToPoint.LengthSquared;

                if (lenSqr > repulse.EffectRadius * repulse.EffectRadius)
                {
                    // Too far away to be influenced by this point
                    continue;
                }

                if (lenSqr.IsNearZero())
                {
                    retVal += Math3D.GetRandomVector_Spherical_Shell(repulse.Strength);
                }
                else
                {
                    retVal += dirToPoint.ToUnit(false) * repulse.Strength;
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
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Direction, ACCEL));

            // Repel -- ramp up repulsion if too close to other bots
            // Friction if about to collide
            forces.AddRange(GetRepelInitialForces());

            // Friction orthogonal to desired vector -- orth friction will help reduce orbits
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Velocity_Orth, ACCEL / 8));

            // Extra friction when overshooting the target (not sure if this is useful) -- big ramp up of friction when it gets really close to another bot
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Velocity_AlongIfVelocityAway, ACCEL / 3.2));

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

            ChasePoint_Force repel = new ChasePoint_Force(ChaseDirectionType.Direction, ACCEL * -3 * powerMult, gradient: gradient);

            gradient = new[]
            {
                Tuple.Create(0d, 1d),
                Tuple.Create(1.5 * distMult, .7d),
                Tuple.Create(3d * distMult, 0d),
            };
            ChasePoint_Force tooCloseAndHotFriction = new ChasePoint_Force(ChaseDirectionType.Velocity_AlongIfVelocityToward, ACCEL * 4 * powerMult, gradient: gradient);

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

        private Vector3D? CapForces_Linear(Vector3D? force)
        {
            if (force == null)
            {
                return null;
            }

            var retVal = new Tuple<Vector3D?, Vector3D?>(force, null);

            retVal = CapForces(retVal);

            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Item1;
            }
        }
        private Tuple<Vector3D?, Vector3D?> CapForces(Tuple<Vector3D?, Vector3D?> forces)
        {
            if (forces == null)
            {
                return null;
            }

            Vector3D? linear = forces.Item1;
            if (linear != null)
            {
                // Turn the force into an acceleration
                double mass = this.PhysicsBody.Mass;
                Vector3D accel = linear.Value / mass;
                bool wasModified = false;

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

                        wasModified = true;
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
                        wasModified = true;
                    }
                    // else, speed is already too great, but accel is against current velocity, so will slow it down

                    #endregion
                }

                // See if this is too large
                double maxAccel = this.MaxAccel;
                double maxAccelSqr = maxAccel * maxAccel;

                if (accel.LengthSquared > maxAccelSqr)
                {
                    accel = accel.ToUnit(false) * maxAccel;
                    wasModified = true;
                }

                if (wasModified)
                {
                    linear = accel * mass;
                }
            }

            Vector3D? angular = forces.Item2;
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

        private Tuple<Vector3D?, Vector3D?> CombineForces(Vector3D? neighborForce, Vector3D? strokeForce, double? distance)
        {
            const double THRESHOLD = 10;        // the number of multiples of this.radius
            const double MINOBJECTIVEPERCENT = .7;
            const double MAXOBJECTIVEPERCENT = .99;

            #region nulls

            // It's easy if one of them is null
            if (neighborForce == null && strokeForce == null)
            {
                return null;
            }
            else if (strokeForce == null)
            {
                return new Tuple<Vector3D?, Vector3D?>(neighborForce, null);
            }
            else if (neighborForce == null)
            {
                return new Tuple<Vector3D?, Vector3D?>(strokeForce, null);
            }

            #endregion

            // They need to be capped before combining, or one could completely wash out the other
            neighborForce = CapForces_Linear(neighborForce);
            strokeForce = CapForces_Linear(strokeForce);

            if (neighborForce == null || strokeForce == null)
            {
                return CombineForces(neighborForce, strokeForce, distance);     // one of them went null.  Recurse, and let the above if statement catch that case
            }

            Vector3D retVal;
            if (distance == null)
            {
                // This should never happen
                retVal = Math3D.GetAverage(new[] { neighborForce.Value, strokeForce.Value });
            }
            else
            {
                #region weighted average

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

                retVal = Math3D.GetCenter(new[]
                {
                    Tuple.Create(neighborForce.Value.ToPoint(), 1d - percent),
                    Tuple.Create(strokeForce.Value.ToPoint(), percent),
                }).ToVector();

                #endregion
            }

            return new Tuple<Vector3D?, Vector3D?>(retVal, null);
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
}
