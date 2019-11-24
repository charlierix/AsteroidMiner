using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.MapObjects
{
    //TODO:
    //      Make the part types unlockable based on experience, and only get experience in what they've picked up/purchased and used
    //      This class should expose a class that tells what weapon types gain experience - ex: if the weapon has a handle and a short chain, both would gain experience but a ratio
    //      Even if a part is unlocked, make the available quality of that part limited based on experience
    //      Calculate cost

    //TODO: Also take damage when striking a bot that is in the middle of ramming

    public class Weapon : IMapObject, IGivesDamage, ISensorVisionPoints, IDisposable
    {
        #region Declaration Section

        private readonly WeaponHandle _handle;
        private readonly WeaponPart _headLeft;
        private readonly WeaponPart _headRight;

        private readonly SortedList<uint, Tuple<WeaponPart, Vector3D>> _partsByCollisionID = new SortedList<uint, Tuple<WeaponPart, Vector3D>>();

        private readonly double _mass;

        private Model3D _attachModel = null;

        private readonly bool _isAttachInMiddle;
        private readonly double _massLeft;
        private readonly double _massRight;

        private readonly Point3D[] _sensorVisionPoints;

        private Quaternion _initialRotation = Quaternion.Identity;

        #endregion

        #region Constructor

        //TODO: Once more than a single handle is passed in, may need to take in the attach point explicitely (or it should be part of the container WeaponDNA object)
        public Weapon(WeaponDNA dna, Point3D position, World world, int materialID)
        {
            if (dna == null || dna.Handle == null)
            {
                throw new ArgumentException("dna must have a handle");
            }
            else if (Math1D.IsNearZero(dna.Handle.AttachPointPercent) && dna.HeadLeft != null)
            {
                throw new ApplicationException("can't have a weapon head on the left (that's where the attach point is)");
            }
            else if (Math1D.IsNearValue(dna.Handle.AttachPointPercent, 1d) && dna.HeadRight != null)
            {
                throw new ApplicationException("can't have a weapon head on the right (that's where the attach point is)");
            }

            IsUsable = true;

            IsGraphicsOnly = world == null;

            _handle = new WeaponHandle(Materials, dna.Handle);

            #region HeadLeft

            if (dna.HeadLeft == null)
            {
                _headLeft = null;
            }
            else if (dna.HeadLeft is WeaponSpikeBallDNA dnaBall)
            {
                _headLeft = new WeaponSpikeBall(Materials, dnaBall);
            }
            else if (dna.HeadLeft is WeaponAxeDNA dnaAxe)
            {
                _headLeft = new WeaponAxe(Materials, dnaAxe, true);
            }
            else
            {
                throw new ApplicationException("Unknown type of dna.HeadLeft: " + dna.HeadLeft.GetType().ToString());
            }

            #endregion
            #region HeadRight

            if (dna.HeadRight == null)
            {
                _headRight = null;
            }
            else if (dna.HeadRight is WeaponSpikeBallDNA dnaBall)
            {
                _headRight = new WeaponSpikeBall(Materials, dnaBall);
            }
            else if (dna.HeadRight is WeaponAxeDNA dnaAxe)
            {
                _headRight = new WeaponAxe(Materials, dnaAxe, false);
            }
            else
            {
                throw new ApplicationException("Unknown type of dna.HeadRight: " + dna.HeadRight.GetType().ToString());
            }

            #endregion

            DNA = new WeaponDNA()
            {
                UniqueID = dna.UniqueID,
                Name = dna.Name,
                Handle = _handle.DNA,
                HeadLeft = _headLeft?.DNAPart,
                HeadRight = _headRight?.DNAPart,
            };

            #region WPF Model

            _model = GetModel(DNA, _handle.Model, _headLeft?.Model, _headRight?.Model);        // I'll leave the below note alone.  It's good to see progress :)

            // Attach Point
            _attachModel = GetModel_AttachPoint(DNA.Handle);

            if (_showAttachPoint)
            {
                _model.Children.Add(_attachModel);
            }

            _visuals3D = new Visual3D[] { new ModelVisual3D() { Content = Model } };

            #endregion

            #region Physics Body

            if (IsGraphicsOnly)
            {
                PhysicsBody = null;
                Token = TokenGenerator.NextToken();
                _mass = GetMass(dna);
            }
            else
            {
                var massBreakdown = GetMassMatrix_CenterOfMass(DNA);
                _mass = massBreakdown.Item1.Mass;

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(position.ToVector()));

                using (CollisionHull hull = GetCollisionHull(DNA, world, _partsByCollisionID, _handle, _headLeft, _headRight))
                {
                    PhysicsBody = new Body(hull, transform.Value, _mass, _visuals3D)
                    {
                        MaterialGroupID = materialID,
                        LinearDamping = .01d,
                        AngularDamping = new Vector3D(.01d, .01d, .01d),
                        MassMatrix = massBreakdown.Item1,
                        CenterOfMass = massBreakdown.Item2,
                    };

                    PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
                }

                Token = PhysicsBody.Token;

                // Rotate it so new ones will spawn pointing down
                if (DNA.Handle.AttachPointPercent < .5d)
                {
                    _initialRotation = new Quaternion(new Vector3D(0, 0, 1), 90);
                }
                else
                {
                    _initialRotation = new Quaternion(new Vector3D(0, 0, 1), -90);
                }

                //TODO: Figure out how to set this here, and not screw stuff up when attaching to the bot
                //PhysicsBody.Rotation = _initialRotation;
            }

            #endregion

            Radius = DNA.Handle.Length / 2d;

            #region Attach Point

            AttachPoint = new Point3D(-(DNA.Handle.Length / 2d) + (DNA.Handle.Length * DNA.Handle.AttachPointPercent), 0, 0);
            //AttachPoint = new Point3D((DNA.Handle.Length / 2d) - (DNA.Handle.Length * DNA.Handle.AttachPointPercent), 0, 0);

            if (Math1D.IsNearZero(DNA.Handle.AttachPointPercent) || Math1D.IsNearValue(DNA.Handle.AttachPointPercent, 1d))
            {
                _isAttachInMiddle = false;
                _massLeft = _mass;      // these masses shouldn't be used anyway
                _massRight = _mass;
            }
            else
            {
                _isAttachInMiddle = true;
                //TODO: Take the heads into account
                _massLeft = _mass * DNA.Handle.AttachPointPercent;
                _massRight = _mass * (1d - DNA.Handle.AttachPointPercent);
            }

            #endregion

            #region SensorVision Points

            if (!IsGraphicsOnly)
            {
                _sensorVisionPoints = GetSensorVisionPoints_Model(_handle, _headLeft, _headRight, PhysicsBody.CenterOfMass.ToVector());
            }

            #endregion

            CreationTime = DateTime.UtcNow;
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
                _isDisposed = true;

                if (!this.IsGraphicsOnly)
                {
                    this.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);

                    this.PhysicsBody.Dispose();
                }
            }
        }

        #endregion
        #region IMapObject Members

        public long Token
        {
            get;
            private set;
        }

        private volatile bool _isDisposed = false;
        public bool IsDisposed
        {
            get
            {
                return _isDisposed || (!this.IsGraphicsOnly && this.PhysicsBody.IsDisposed);
            }
        }

        public Body PhysicsBody
        {
            get;
            private set;
        }

        private readonly Visual3D[] _visuals3D;
        public Visual3D[] Visuals3D
        {
            get
            {
                return _visuals3D;
            }
        }

        private Model3DGroup _model = null;
        public Model3D Model
        {
            get
            {
                return _model;
            }
        }

        public Point3D PositionWorld
        {
            get
            {
                #region Validate
#if DEBUG
                if (this.IsGraphicsOnly)
                {
                    throw new InvalidOperationException("PositionWorld can't be called when this class is graphics only");
                }
#endif
                #endregion

                return this.PhysicsBody.Position;
            }
        }
        public Vector3D VelocityWorld
        {
            get
            {
                #region Validate
#if DEBUG
                if (this.IsGraphicsOnly)
                {
                    throw new InvalidOperationException("VelocityWorld can't be called when this class is graphics only");
                }
#endif
                #endregion

                return this.PhysicsBody.Velocity;
            }
        }
        public Matrix3D OffsetMatrix
        {
            get
            {
                #region Validate
#if DEBUG
                if (this.IsGraphicsOnly)
                {
                    throw new InvalidOperationException("OffsetMatrix can't be called when this class is graphics only");
                }
#endif
                #endregion

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
        #region IGivesDamage Members

        /// <summary>
        /// This serves two purposes.  When a weapon strikes something, it may get slightly damaged.  It also damages the item that
        /// it strikes
        /// </summary>
        public DamageProps CalculateDamage(MaterialCollision[] collisions)
        {
            #region Validate
#if DEBUG
            if (this.IsGraphicsOnly)
            {
                throw new InvalidOperationException("Collided can't be called when this class is graphics only");
            }
#endif
            #endregion

            if (collisions.Length == 0)
            {
                return null;
            }

            var avgCollision = MaterialCollision.GetAverageCollision(collisions, this.PhysicsBody);

            // If the attach point is in the middle, then only a percent of the mass should be used
            Point3D avgPosLocal = this.PhysicsBody.PositionFromWorld(avgCollision.Item1);

            #region Figure out mass

            double massForDamage = _mass;
            if (_isAttachInMiddle)
            {
                if (avgPosLocal.X > this.AttachPoint.X)
                {
                    massForDamage = _massRight;
                }
                else
                {
                    massForDamage = _massLeft;
                }
            }

            #endregion

            // Technically, this method should return speed, mass.  And the items that are struck should decide how much
            // damage (rubber armor would respond differently than glass).  But this game is a bit simplistic, and the armor
            // is assumed to all be roughly the same material.
            double kinetic = CalculateDamage_Square(avgCollision.Item2, massForDamage);

            DamageProps kineticDamage = new DamageProps(avgCollision.Item1, kinetic);

            #region Extra damange

            // Figure out which parts of the weapon the collision was with (there could be multiple collisions, so figure out the majority)
            var groups = collisions.
                Select(o => o.GetCollisionHull(this.PhysicsBody).UserID).
                GroupBy(o => o).
                OrderByDescending(o => o.Count()).
                ToArray();

            // In case there's a tie, use the larger value (1 is handle, 2 and up are weapons heads.  So use the weapon heads when in doubt)
            uint collisionHullID = groups.
                Where(o => o.Key == groups[0].Key).
                OrderByDescending(o => o.Key).
                First().
                Key;

            // Ask the part it hit for any extra damage
            DamageProps extraDamage = new DamageProps();

            Tuple<WeaponPart, Vector3D> collidedPart;
            if (_partsByCollisionID.TryGetValue(collisionHullID, out collidedPart))
            {
                extraDamage = collidedPart.Item1.CalculateExtraDamage(avgPosLocal + collidedPart.Item2, avgCollision.Item2);
            }

            #endregion

            return DamageProps.GetMerged(kineticDamage, extraDamage);
        }

        #endregion
        #region ISensorVisionPoints

        public Point3D[] GetSensorVisionPoints()
        {
            if (IsGraphicsOnly)
            {
                throw new InvalidOperationException("This function should never be called when graphics only");
            }

            return GetSensorVisionPoints(_sensorVisionPoints, PhysicsBody);
        }

        internal static Point3D[] GetSensorVisionPoints(Point3D[] pointsModel, Body physicsBody)
        {
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(physicsBody.Rotation)));
            transform.Children.Add(new TranslateTransform3D(physicsBody.Position.ToVector()));

            Point3D[] points = pointsModel.ToArray();       // make a copy

            transform.Transform(points);

            return points;
        }

        #endregion

        #region Public Properties

        public readonly bool IsGraphicsOnly;

        /// <summary>
        /// True: The weapon can be attached to a bot and used
        /// False: This is just a part of a weapon (like the head of an axe)
        /// </summary>
        public readonly bool IsUsable;

        /// <summary>
        /// This is in model coords
        /// </summary>
        public readonly Point3D AttachPoint;

        public IGravityField Gravity
        {
            get;
            set;
        }

        private static ThreadLocal<WeaponMaterialCache> _materials = new ThreadLocal<WeaponMaterialCache>(() => new WeaponMaterialCache());       // need to pass the delegate, or Value will just be null
        private WeaponMaterialCache Materials
        {
            get
            {
                return _materials.Value;
            }
        }

        private bool _showAttachPoint = true;
        public bool ShowAttachPoint
        {
            get
            {
                return _showAttachPoint;
            }
            set
            {
                _showAttachPoint = value;

                if (_model != null && _attachModel != null)
                {
                    if (_showAttachPoint)
                    {
                        if (!_model.Children.Contains(_attachModel))
                        {
                            _model.Children.Add(_attachModel);
                        }
                    }
                    else
                    {
                        if (_model.Children.Contains(_attachModel))
                        {
                            _model.Children.Remove(_attachModel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This can be used to recreate this weapon (certain sections may have been null or incorrect when
        /// constructed, but this will be fully populated)
        /// </summary>
        public readonly WeaponDNA DNA;

        /// <summary>
        /// PhysicsBody could be null (look at IsGraphicsOnly), so mass is exposed explicitly
        /// </summary>
        public double Mass => _mass;

        //public bool IsExtended
        //{
        //    get
        //    {
        //        //TODO: Finish this
        //        return false;
        //    }
        //}

        #endregion

        #region Public Methods

        /// <summary>
        /// This places the weapon's attach point at the requested location
        /// </summary>
        public void MoveToAttachPoint(Point3D point)
        {
            #region validate
#if DEBUG
            if (this.IsGraphicsOnly)
            {
                throw new InvalidOperationException("MoveToAttachPoint can't be called when this class is graphics only");
            }
#endif
            #endregion

            PhysicsBody.Position = point - PhysicsBody.DirectionToWorld(AttachPoint.ToVector());
        }

        //public void Extend()
        //{
        //}
        //public void Retract()
        //{
        //}

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {

            //TODO: Put this in MapObjectChaseTorque (as written, this only works in one plane.  need to handle when the map is a large cylinder - lock to the plane tangent the cylinder that they are over at the moment)
            const double MAXANG = .25d;
            Vector3D angularVelocity = this.PhysicsBody.AngularVelocity;
            if (Math.Abs(angularVelocity.X) > MAXANG || Math.Abs(angularVelocity.Y) > MAXANG)
            {
                this.PhysicsBody.AngularVelocity = new Vector3D(angularVelocity.X * .9, angularVelocity.Y * .9, angularVelocity.Z);
            }





            if (this.Gravity != null)
            {
                Vector3D acceleration = this.Gravity.GetForce(e.Body.Position);     // even though it's called getforce, treat it like an acceleration (so all instances fall the same)

                // f=ma
                e.Body.AddForce(acceleration * _mass);
            }
        }

        #endregion

        #region Private Methods - Model

        private static Model3DGroup GetModel(WeaponDNA dna, Model3DGroup handleModel, Model3DGroup headLeftModel, Model3DGroup headRightModel)
        {
            if (headLeftModel == null && headRightModel == null)
            {
                return handleModel;
            }

            var models = new List<(Model3DGroup model, double alongX)>();

            models.Add((handleModel, 0d));      // add the handle first, so that semi transparent balls look right

            if (headLeftModel != null)
            {
                models.Add((headLeftModel, dna.Handle.Length / -2d));
            }

            if (headRightModel != null)
            {
                models.Add((headRightModel, dna.Handle.Length / 2d));
            }

            return GetModel(models.ToArray());
        }
        private static Model3DGroup GetModel((Model3DGroup model, double alongX)[] models)
        {
            Model3DGroup retVal = new Model3DGroup();

            foreach (var model in models)
            {
                // Apply a transform to slide this part
                if (!Math1D.IsNearZero(model.alongX))
                {
                    // The weapon is built along the x axis
                    TranslateTransform3D translate = new TranslateTransform3D(model.alongX, 0, 0);

                    if (model.model.Transform == null || (model.model.Transform is MatrixTransform3D && ((MatrixTransform3D)model.model.Transform).Value.IsIdentity))
                    {
                        // This model doesn't have an existing transform, so just overrite it
                        model.model.Transform = translate;
                    }
                    else
                    {
                        // There's already a transform.  Create a group
                        Transform3DGroup transform = new Transform3DGroup();

                        transform.Children.Add(model.model.Transform);
                        transform.Children.Add(translate);

                        model.model.Transform = transform;
                    }
                }

                // Store in the return group
                retVal.Children.Add(model.model);
            }

            //retVal.Freeze();      // can't freeze it.  The attach point gets added/removed from this group

            return retVal;
        }

        private Model3D GetModel_AttachPoint(WeaponHandleDNA dna)
        {
            double offsetX = -(dna.Length / 2d) + (dna.Length * dna.AttachPointPercent);

            double offsetY = dna.Radius * .5d;
            double offsetZ = dna.Radius * 2.5d;
            double radius = dna.Radius / 3d;

            Model3DGroup retVal = new Model3DGroup();

            // Line Z
            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = this.Materials.Handle_AttachPoint1;
            geometry.BackMaterial = geometry.Material;

            geometry.Geometry = UtilityWPF.GetLine(new Point3D(offsetX, -offsetY, -offsetZ), new Point3D(offsetX, offsetY, offsetZ), radius);

            retVal.Children.Add(geometry);

            // Line Y
            geometry = new GeometryModel3D();

            geometry.Material = this.Materials.Handle_AttachPoint2;
            geometry.BackMaterial = geometry.Material;

            geometry.Geometry = UtilityWPF.GetLine(new Point3D(offsetX, offsetY, -offsetZ), new Point3D(offsetX, -offsetY, offsetZ), radius);

            retVal.Children.Add(geometry);

            //// Line 0Z
            //geometry = new GeometryModel3D();

            //geometry.Material = this.Materials.Handle_AttachPoint3;
            //geometry.BackMaterial = geometry.Material;

            //double offset0Z = offsetZ * 1.3d;
            //geometry.Geometry = UtilityWPF.GetLine(new Point3D(offsetX, 0, -offset0Z), new Point3D(offsetX, 0, offset0Z), radius * .5d);

            //retVal.Children.Add(geometry);

            // Exit Function
            return retVal;
        }

        #endregion
        #region Private Methods

        private static double CalculateDamage_Linear(double speed, double mass)
        {
            const double MASSNORMALIZE = 100;

            // This was my first attempt, but isn't realistic.  It favors mass too much.  Speed should be more important

            return speed * (mass / MASSNORMALIZE);
        }
        private static double CalculateDamage_Square(double speed, double mass)
        {
            const double MASSNORMALIZE = 100;

            // This equation should be if mass is equal to the standard
            // This assumes that the speed of a decent hit is 10.  50 would be a really fast hit
            //2*(.1*x)^3

            double mult = 2d * (mass / MASSNORMALIZE);
            //double mult = 4d * (mass / MASSNORMALIZE);

            double inner = speed / 10d;

            return mult * (inner * inner * inner);
        }

        private static double GetDensity(WeaponSpikeBallMaterial ball)
        {
            WeaponHandleMaterial equivalent;
            switch (ball)
            {
                case WeaponSpikeBallMaterial.Wood:
                    equivalent = WeaponHandleMaterial.Hard_Wood;
                    break;

                case WeaponSpikeBallMaterial.Bronze_Iron:
                    equivalent = WeaponHandleMaterial.Bronze;
                    break;

                case WeaponSpikeBallMaterial.Iron_Steel:
                    equivalent = WeaponHandleMaterial.Steel;
                    break;

                case WeaponSpikeBallMaterial.Composite:
                    equivalent = WeaponHandleMaterial.Composite;
                    break;

                case WeaponSpikeBallMaterial.Klinth:
                    equivalent = WeaponHandleMaterial.Klinth;
                    break;

                case WeaponSpikeBallMaterial.Moon:
                    equivalent = WeaponHandleMaterial.Moon;
                    break;

                default:
                    throw new ApplicationException("Unknown WeaponSpikeBallMaterial: " + ball.ToString());
            }

            return GetDensity(equivalent);
        }
        private static double GetDensity(WeaponHandleMaterial handle)
        {
            switch (handle)
            {
                // http://www.engineeringtoolbox.com/wood-density-d_40.html
                case WeaponHandleMaterial.Soft_Wood:
                    //return .4d;       //accurate, but wildly too low
                    return 200d;
                case WeaponHandleMaterial.Hard_Wood:
                    //return .8d;
                    return 400;

                // http://www.engineeringtoolbox.com/metal-alloys-densities-d_50.html
                case WeaponHandleMaterial.Bronze:
                    return 7500d;
                case WeaponHandleMaterial.Iron:
                case WeaponHandleMaterial.Steel:
                    return 7850d;

                case WeaponHandleMaterial.Composite:
                    return 2000d;

                case WeaponHandleMaterial.Klinth:
                    return 5000d;

                case WeaponHandleMaterial.Moon:
                    return 6000d;
                //case WeaponHandleMaterial.Wraith:
                //    return 800d;

                default:
                    throw new ApplicationException("Unknown WeaponHandleMaterial: " + handle.ToString());
            }
        }

        //NOTE: Be sure to dispose this hull once added to the body
        private static CollisionHull GetCollisionHull(WeaponDNA dna, World world, SortedList<uint, Tuple<WeaponPart, Vector3D>> partsByCollisionID, WeaponHandle handle, WeaponPart headLeft, WeaponPart headRight)
        {
            List<CollisionHull> hulls = new List<CollisionHull>();

            Vector3D headTranslate = new Vector3D(dna.Handle.Length / -2d, 0, 0);       // this if for the left

            #region Build individual hulls

            int nextCollisionID = 1;

            // Handle
            hulls.Add(CollisionHull.CreateCylinder(world, nextCollisionID, dna.Handle.Radius, dna.Handle.Length, null));

            partsByCollisionID.Add((uint)nextCollisionID, Tuple.Create((WeaponPart)handle, new Vector3D(0, 0, 0)));
            nextCollisionID++;

            // Left Ball
            if (dna.HeadLeft != null)
            {
                if (dna.HeadLeft is WeaponSpikeBallDNA dnaBall)
                {
                    hulls.Add(GetCollisionHull_SpikeBall(dnaBall, world, nextCollisionID, headTranslate));
                }
                else if (dna.HeadLeft is WeaponAxeDNA dnaAxe)
                {
                    hulls.Add(GetCollisionHull_Axe(dnaAxe, world, nextCollisionID, headTranslate, (WeaponAxe)headLeft));
                }
                else
                {
                    throw new ApplicationException("Unknown dna.HeadLeft: " + dna.HeadLeft.GetType().ToString());
                }

                partsByCollisionID.Add((uint)nextCollisionID, Tuple.Create((WeaponPart)headLeft, headTranslate));
                nextCollisionID++;
            }

            // Right Ball
            if (dna.HeadRight != null)
            {
                if (dna.HeadRight is WeaponSpikeBallDNA dnaBall)
                {
                    hulls.Add(GetCollisionHull_SpikeBall(dnaBall, world, nextCollisionID, -headTranslate));
                }
                else if (dna.HeadRight is WeaponAxeDNA dnaAxe)
                {
                    hulls.Add(GetCollisionHull_Axe(dnaAxe, world, nextCollisionID, -headTranslate, (WeaponAxe)headRight));
                }
                else
                {
                    throw new ApplicationException("Unknown dna.HeadRight: " + dna.HeadRight.GetType().ToString());
                }

                partsByCollisionID.Add((uint)nextCollisionID, Tuple.Create((WeaponPart)headRight, -headTranslate));
                nextCollisionID++;
            }

            #endregion

            CollisionHull retVal = null;

            if (hulls.Count == 0)
            {
                throw new ApplicationException("Didn't get any weapon parts");
            }
            else if (hulls.Count == 1)
            {
                retVal = hulls[0];
            }
            else
            {
                retVal = CollisionHull.CreateCompoundCollision(world, 0, hulls.ToArray());

                foreach (CollisionHull hull in hulls)
                {
                    hull.Dispose();
                }
            }

            return retVal;
        }
        private static CollisionHull GetCollisionHull_SpikeBall(WeaponSpikeBallDNA dna, World world, int shapeID, Vector3D translate)
        {
            Vector3D radius = new Vector3D(dna.Radius, dna.Radius, dna.Radius);
            Matrix3D matrix = new TranslateTransform3D(translate).Value;

            return CollisionHull.CreateSphere(world, shapeID, radius, matrix);
        }
        private static CollisionHull GetCollisionHull_Axe(WeaponAxeDNA dna, World world, int shapeID, Vector3D translate, WeaponAxe axe)
        {
            Point3D[] points = UtilityWPF.GetPointsFromMesh(axe.Model);

            return CollisionHull.CreateConvexHull(world, shapeID, points);      // no need for a transform, the points are already shifted
        }

        private static Tuple<MassMatrix, Point3D> GetMassMatrix_CenterOfMass(WeaponDNA dna, double inertiaMultiplier = 1d)
        {
            List<UtilityNewt.MassPart> parts = new List<UtilityNewt.MassPart>();

            double cellSize = dna.Handle.Length / 5d;

            UtilityNewt.IObjectMassBreakdown breakdown;
            double mass;

            #region handle

            mass = GetMass(dna.Handle);

            breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, new Vector3D(dna.Handle.Length, dna.Handle.Radius * 2, dna.Handle.Radius * 2), cellSize);

            parts.Add(new UtilityNewt.MassPart(new Point3D(0, 0, 0), mass, Quaternion.Identity, breakdown));

            #endregion

            foreach (var head in new[] { Tuple.Create(dna.HeadLeft, dna.Handle.Length / -2d), Tuple.Create(dna.HeadRight, dna.Handle.Length / 2d) })
            {
                if (head.Item1 != null)
                {
                    if (head.Item1 is WeaponSpikeBallDNA ballDNA)
                    {
                        #region spike ball

                        mass = GetMass(ballDNA);

                        double radius = ballDNA.Radius;

                        Point3D position = new Point3D(head.Item2, 0, 0);

                        breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, new Vector3D(radius * 2, radius * 2, radius * 2), cellSize);

                        parts.Add(new UtilityNewt.MassPart(position, mass, Quaternion.Identity, breakdown));

                        #endregion
                    }
                    else if (head.Item1 is WeaponAxeDNA axeDNA)
                    {
                        #region axe

                        mass = GetMass(axeDNA);

                        Vector3D size = GetAxeSize(axeDNA);

                        double yOffset = 0d;
                        if (axeDNA.Sides == WeaponAxeSides.Single_BackFlat || axeDNA.Sides == WeaponAxeSides.Single_BackSpike)
                        {
                            yOffset = axeDNA.SizeSingle * .5;      // CreateBox centers size, this will push the blade to the right (leaving some for the back
                        }

                        Point3D position = new Point3D(head.Item2, yOffset, 0);

                        breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);

                        parts.Add(new UtilityNewt.MassPart(position, mass, Quaternion.Identity, breakdown));

                        #endregion
                    }
                    else
                    {
                        throw new ApplicationException("Unknown weapon part type: " + head.Item1.GetType().ToString());
                    }
                }
            }

            return UtilityNewt.GetMassMatrix_CenterOfMass(parts.ToArray(), cellSize, inertiaMultiplier);
        }

        private static double GetMass(WeaponDNA dna)
        {
            double mass = GetMass(dna.Handle);

            foreach (var head in new[] { dna.HeadLeft, dna.HeadRight })
            {
                if (head != null)
                {
                    if (head is WeaponSpikeBallDNA ballDNA)
                    {
                        mass += GetMass(ballDNA);
                    }
                    else if (head is WeaponAxeDNA axeDNA)
                    {
                        mass += GetMass(axeDNA);
                    }
                    else
                    {
                        throw new ApplicationException("Unknown weapon part type: " + head.GetType().ToString());
                    }
                }
            }

            return mass;
        }
        private static double GetMass(WeaponHandleDNA dna)
        {
            double radiusExaggerated = dna.Radius * 2d;
            double volume = Math.PI * radiusExaggerated * radiusExaggerated * dna.Length;
            return GetDensity(dna.HandleMaterial) * volume;
        }
        private static double GetMass(WeaponSpikeBallDNA dna)
        {
            //radiusExaggerated = dna.Radius * 2d;
            double radiusExaggerated = dna.Radius * 1d;

            double volume = 4d / 3d * Math.PI * radiusExaggerated * radiusExaggerated * radiusExaggerated;
            double mass = GetDensity(dna.Material) * volume;
            mass *= 3;

            return mass;
        }
        private static double GetMass(WeaponAxeDNA dna)
        {
            Vector3D size = GetAxeSize(dna);

            double volume = size.X * size.Y * size.Z;
            double mass = GetDensity(WeaponSpikeBallMaterial.Iron_Steel) * volume;
            mass *= 3;

            return mass;
        }

        private static Vector3D GetAxeSize(WeaponAxeDNA dna)
        {
            switch (dna.Sides)
            {
                case WeaponAxeSides.Single_BackFlat:
                case WeaponAxeSides.Single_BackSpike:
                    return new Vector3D(dna.SizeSingle * 1.1, dna.SizeSingle, dna.SizeSingle * .05);

                case WeaponAxeSides.Double:
                    return new Vector3D(dna.SizeSingle * 2, dna.SizeSingle, dna.SizeSingle * .05);

                default:
                    throw new ApplicationException("Unknown WeaponAxeSides: " + dna.Sides.ToString());
            }
        }

        private static Point3D[] GetSensorVisionPoints_Model(WeaponHandle handle, WeaponPart headLeft, WeaponPart headRight, Vector3D centerMass)
        {
            List<Point3D> retVal = new List<Point3D>();

            // Handle
            var halfLength = handle.DNA.Length / 2d;
            //var attach = _handle.DNA.AttachPointPercent;      // attach point doesn't matter for this function.  It's used by the physics engine to connect to the bot with a joint.  But the weapon body is always centered at zero (actually centerMass)

            retVal.Add(new Point3D(-halfLength, 0, 0));
            retVal.Add(new Point3D(0, 0, 0));
            retVal.Add(new Point3D(halfLength, 0, 0));

            // Axe Heads (don't worry about mace heads.  They generally aren't large enough to need extra points)
            var axeHeads = new[] { headLeft, headRight }.
                Where(o => o is WeaponAxe);

            foreach (WeaponAxe axe in axeHeads)
            {
                var bounds = axe.Model.Bounds;

                retVal.Add(new Point3D(bounds.X, bounds.Y, 0));
                retVal.Add(new Point3D(bounds.X + bounds.SizeX, bounds.Y, 0));

                retVal.Add(new Point3D(bounds.X, bounds.Y + bounds.SizeY, 0));
                retVal.Add(new Point3D(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY, 0));
            }

            return retVal.
                Select(o => o - centerMass).
                ToArray();
        }

        #endregion
    }

    #region class: WeaponDNA

    public class WeaponDNA
    {
        /// <summary>
        /// This is generated when first created, but then needs to persist for the life of the weapon
        /// </summary>
        /// <remarks>
        /// Weapon.Token is only unique within the current process.  But this guid stays the same across saves/loads.
        /// Also, when the weapon is transferred from the world to inventory and back, new isntances of weapon will
        /// be created, but with this same dna.  So token changes, but this guid stays the same
        /// </remarks>
        public Guid UniqueID { get; set; }

        /// <summary>
        /// This doesn't have to be unique (or even populated), just giving the user a chance to name it
        /// </summary>
        public string Name { get; set; }

        // For now, this is always required
        //TODO: Once blades are implemented, allow a blade to be the only thing
        //TODO: Once chain handles are implemented, allow multiple chains to be held
        public WeaponHandleDNA Handle { get; set; }

        //TODO: Don't hardcode to spikeballs
        //TODO: Instead of fixed properties, somehow define a chain of parts { WeaponSpikeBallDNA, WeaponHandleDNA(rope), WeaponHandleDNA(rod), WeaponAxeDNA }
        public WeaponPartDNA HeadLeft { get; set; }
        public WeaponPartDNA HeadRight { get; set; }

        #region Public Methods

        //TODO: Take in a bool to see if it is required to be useable
        public static WeaponDNA GetRandomDNA()
        {
            Random rand = StaticRandom.GetRandomForThread();

            WeaponHandleDNA handle = WeaponHandleDNA.GetRandomDNA();

            WeaponPartDNA leftHead = null;
            if (!Math1D.IsNearZero(handle.AttachPointPercent) && rand.NextDouble() < .25)
            {
                if (rand.NextBool())
                {
                    leftHead = WeaponSpikeBallDNA.GetRandomDNA(handle);
                }
                else
                {
                    leftHead = WeaponAxeDNA.GetRandomDNA();
                }

                leftHead.IsBackward = rand.NextBool();
            }

            WeaponPartDNA rightHead = null;
            if (!Math1D.IsNearValue(handle.AttachPointPercent, 1d) && rand.NextDouble() < .25)
            {
                if (rand.NextBool())
                {
                    rightHead = WeaponSpikeBallDNA.GetRandomDNA(handle);
                }
                else
                {
                    rightHead = WeaponAxeDNA.GetRandomDNA();
                }

                rightHead.IsBackward = rand.NextBool();
            }

            return new WeaponDNA()
            {
                UniqueID = Guid.NewGuid(),
                Handle = handle,
                HeadLeft = leftHead,
                HeadRight = rightHead
            };
        }

        /// <summary>
        /// This tries to get the value from the from list.  If it doesn't exist, it uses the valueIfNew.
        /// This always stores the return value in the to list
        /// </summary>
        public static T GetKeyValue<T>(string key, SortedList<string, T> from, SortedList<string, T> to, T valueIfNew)
        {
            // Get the value
            T retVal;
            if (from != null && from.ContainsKey(key))
            {
                retVal = from[key];
            }
            else
            {
                retVal = valueIfNew;
            }

            // Store the value
            if (to.ContainsKey(key))        // to should be guaranteed to be non null before this method is called
            {
                to[key] = retVal;
            }
            else
            {
                to.Add(key, retVal);
            }

            // Exit Function
            return retVal;
        }

        // These are helpers for specific datatypes
        public static Vector3D GetKeyValue_Vector(string keyBase, SortedList<string, double> from, SortedList<string, double> to, Vector3D vectorIfNew)
        {
            string nameX = keyBase + "X";
            string nameY = keyBase + "Y";
            string nameZ = keyBase + "Z";

            // Get the value
            double x, y, z;

            if (from != null && from.ContainsKey(nameX))
            {
                x = from[nameX];
                y = from[nameY];       // this method is what populates the sorted list.  if X exists, so should Y and Z
                z = from[nameZ];
            }
            else
            {
                x = vectorIfNew.X;
                y = vectorIfNew.Y;
                z = vectorIfNew.Z;
            }

            // Store the value
            if (to.ContainsKey(nameX))        // to should be guaranteed to be non null before this method is called
            {
                to[nameX] = x;
                to[nameY] = y;
                to[nameZ] = z;
            }
            else
            {
                to.Add(nameX, x);
                to.Add(nameY, y);
                to.Add(nameZ, z);
            }

            return new Vector3D(x, y, z);
        }
        public static bool GetKeyValue_Bool(string key, SortedList<string, double> from, SortedList<string, double> to, bool valueIfNew)
        {
            return Math1D.IsNearZero(GetKeyValue(key, from, to, valueIfNew ? 0d : 1d));
        }

        #endregion
    }

    #endregion
    #region class: WeaponPartDNA

    public class WeaponPartDNA
    {
        // These these store custom settings
        public SortedList<string, double> KeyValues { get; set; }
        public MaterialDefinition[] MaterialsForCustomizable { get; set; }

        public bool IsBackward { get; set; }
    }

    #endregion

    #region class: WeaponPart

    public abstract class WeaponPart
    {
        public Model3DGroup Model { get; protected set; }

        public abstract WeaponPartDNA DNAPart { get; }

        /// <summary>
        /// This gives the part a chance to damage itself, and also returns any additional damage
        /// </summary>
        public abstract DamageProps CalculateExtraDamage(Point3D positionLocal, double collisionSpeed);
    }

    #endregion

    #region class: WeaponMaterialCache

    public class WeaponMaterialCache
    {
        //TODO: Wood and metal should have some slight variants, not all identical

        // Attach Point
        private System.Windows.Media.Media3D.Material _handle_AttachPoint1 = null;
        public System.Windows.Media.Media3D.Material Handle_AttachPoint1
        {
            get
            {
                if (_handle_AttachPoint1 == null)
                {
                    //_handle_AttachPoint1 = UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("80808080"));
                    _handle_AttachPoint1 = UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("80FFFFFF"));
                }

                return _handle_AttachPoint1;
            }
        }

        private System.Windows.Media.Media3D.Material _handle_AttachPoint2 = null;
        public System.Windows.Media.Media3D.Material Handle_AttachPoint2
        {
            get
            {
                if (_handle_AttachPoint2 == null)
                {
                    _handle_AttachPoint2 = UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("80000000"));
                }

                return _handle_AttachPoint2;
            }
        }

        private System.Windows.Media.Media3D.Material _handle_AttachPoint3 = null;
        public System.Windows.Media.Media3D.Material Handle_AttachPoint3
        {
            get
            {
                if (_handle_AttachPoint3 == null)
                {
                    _handle_AttachPoint3 = UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("B005E81C"));
                }

                return _handle_AttachPoint3;
            }
        }

        // Wood
        private System.Windows.Media.Media3D.Material _handle_SoftWood = null;
        public System.Windows.Media.Media3D.Material Handle_SoftWood
        {
            get
            {
                if (_handle_SoftWood == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("CCAF89"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("20FFCC89")), .2d));

                    material.Freeze();

                    _handle_SoftWood = material;
                }

                return _handle_SoftWood;
            }
        }

        private System.Windows.Media.Media3D.Material _handle_HardWood = null;
        public System.Windows.Media.Media3D.Material Handle_HardWood
        {
            get
            {
                if (_handle_HardWood == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("4D3A1E"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("608C7149")), .35d));

                    material.Freeze();

                    _handle_HardWood = material;
                }

                return _handle_HardWood;
            }
        }

        private System.Windows.Media.Media3D.Material _ball_Wood = null;
        public System.Windows.Media.Media3D.Material Ball_Wood
        {
            get
            {
                if (_ball_Wood == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    //material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("423829"))));       // a good dark wood color

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("695B4A"))));

                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("60856F50")), .35d));

                    material.Freeze();

                    _ball_Wood = material;
                }

                return _ball_Wood;
            }
        }

        // Metal
        private System.Windows.Media.Media3D.Material _handle_Bronze = null;
        public System.Windows.Media.Media3D.Material Handle_Bronze
        {
            get
            {
                if (_handle_Bronze == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("783D18"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D48F53")), .8d));

                    material.Freeze();

                    _handle_Bronze = material;
                }

                return _handle_Bronze;
            }
        }

        private System.Windows.Media.Media3D.Material _ball_Bronze = null;
        public System.Windows.Media.Media3D.Material Ball_Bronze
        {
            get
            {
                if (_ball_Bronze == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("783D18"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D48F53")), .8d));

                    material.Freeze();

                    _ball_Bronze = material;
                }

                return _ball_Bronze;
            }
        }

        private System.Windows.Media.Media3D.Material _handle_Iron = null;
        public System.Windows.Media.Media3D.Material Handle_Iron
        {
            get
            {
                if (_handle_Iron == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("363532"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("51504C")), .8d));

                    material.Freeze();

                    _handle_Iron = material;
                }

                return _handle_Iron;
            }
        }

        private System.Windows.Media.Media3D.Material _ball_Iron = null;
        public System.Windows.Media.Media3D.Material Ball_Iron
        {
            get
            {
                if (_ball_Iron == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("363532"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("51504C")), .8d));

                    material.Freeze();

                    _ball_Iron = material;
                }

                return _ball_Iron;
            }
        }

        private System.Windows.Media.Media3D.Material _spike_Iron = null;
        public System.Windows.Media.Media3D.Material Spike_Iron
        {
            get
            {
                if (_spike_Iron == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    //material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("363532"))));
                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("2E2D2A"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("51504C")), .8d));

                    material.Freeze();

                    _spike_Iron = material;
                }

                return _spike_Iron;
            }
        }

        private System.Windows.Media.Media3D.Material _handle_Steel = null;
        public System.Windows.Media.Media3D.Material Handle_Steel
        {
            get
            {
                if (_handle_Steel == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("56534E"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0B6BCCC")), .8d));

                    material.Freeze();

                    _handle_Steel = material;
                }

                return _handle_Steel;
            }
        }

        private System.Windows.Media.Media3D.Material _spike_Steel = null;
        public System.Windows.Media.Media3D.Material Spike_Steel
        {
            get
            {
                if (_spike_Steel == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("827E76"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("B0BFC5D6")), .8d));

                    material.Freeze();

                    _spike_Steel = material;
                }

                return _spike_Steel;
            }
        }

        // Moon
        private System.Windows.Media.Media3D.Material _handle_Moon = null;
        public System.Windows.Media.Media3D.Material Handle_Moon
        {
            get
            {
                if (_handle_Moon == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("E8DDD3"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0424854")), 20d));
                    material.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30CCD3E0"))));

                    material.Freeze();

                    _handle_Moon = material;
                }

                return _handle_Moon;
            }
        }

        private System.Windows.Media.Media3D.Material _handle_MoonGem = null;
        public System.Windows.Media.Media3D.Material Handle_MoonGem
        {
            get
            {
                if (_handle_MoonGem == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("90467C82"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A01E62B0")), 50d));
                    material.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("204F989C"))));

                    material.Freeze();

                    _handle_MoonGem = material;
                }

                return _handle_MoonGem;
            }
        }

        public Color Handle_MoonGemLight = UtilityWPF.ColorFromHex("6DBCC9");

        private System.Windows.Media.Media3D.Material _ball_Moon = null;
        public System.Windows.Media.Media3D.Material Ball_Moon
        {
            get
            {
                if (_ball_Moon == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0386469"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0175AA6")), 50d));
                    material.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("20438185"))));

                    material.Freeze();

                    _ball_Moon = material;
                }

                return _ball_Moon;
            }
        }

        private System.Windows.Media.Media3D.Material _spike_Moon = null;
        public System.Windows.Media.Media3D.Material Spike_Moon
        {
            get
            {
                if (_spike_Moon == null)
                {
                    MaterialGroup material = new MaterialGroup();

                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("08FAF6F2"))));
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0544E42")), 5d));
                    material.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("10423E34"))));

                    material.Freeze();

                    _spike_Moon = material;
                }

                return _spike_Moon;
            }
        }

        public static Tuple<MaterialGroup, MaterialGroup, MaterialDefinition[]> GetKlinth(MaterialDefinition[] materialsForCustomizable_Existing = null)
        {
            MaterialDefinition[] materialsFinal = WeaponHandleDNA.GetRandomMaterials_Klinth(materialsForCustomizable_Existing);

            MaterialGroup[] materials = new MaterialGroup[2];

            for (int cntr = 0; cntr < 2; cntr++)
            {
                materials[cntr] = new MaterialGroup();

                Color color = UtilityWPF.ColorFromHex(materialsFinal[cntr].DiffuseColor);
                materials[cntr].Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, color.R, color.G, color.B))));

                color = UtilityWPF.ColorFromHex(materialsFinal[cntr].SpecularColor);
                materials[cntr].Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, color.R, color.G, color.B)), 75));

                color = UtilityWPF.ColorFromHex(materialsFinal[cntr].EmissiveColor);
                materials[cntr].Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(20, color.R, color.G, color.B))));

                materials[cntr].Freeze();
            }

            return Tuple.Create(materials[0], materials[1], materialsFinal);
        }
        public static Tuple<MaterialGroup, MaterialGroup, MaterialGroup, MaterialDefinition[]> GetComposite(MaterialDefinition[] materialsForCustomizable_Existing = null)
        {
            MaterialDefinition[] materialsFinal = WeaponHandleDNA.GetRandomMaterials_Composite(materialsForCustomizable_Existing);

            MaterialGroup[] materials = new MaterialGroup[3];

            for (int cntr = 0; cntr < 3; cntr++)
            {
                materials[cntr] = new MaterialGroup();

                Color color = UtilityWPF.ColorFromHex(materialsFinal[cntr].DiffuseColor);

                materials[cntr].Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B))));     // making sure there is no semitransparency
                materials[cntr].Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 50));
            }

            return Tuple.Create(materials[0], materials[1], materials[2], materialsFinal);
        }
    }

    #endregion

    #region Weapon Head

    //TODO: Instead of an enum, use derived classes
    //Damage Types:
    //  Blunt - high chance of knockout
    //  Slash - high damage to low armor (and handles)
    //  Pierce - neglects armor
    public enum WeaponHeadType
    {
        Blade,      // give a property for curvature.  allow for varying widths along the spine of the blade.  give a property for edge type: single, single+tip, double (single edge would be cheaper)
        Axe,        // single side (sort of a combo of blade and hammer), double side, circular
        Spike,      // the tip does good piercing damage, but only when impact is along the length of the spike.  Indirect blow does same damage as a handle
        Hammer,
        SpikedBall,     // not sure what this is called - the end of a mace or flail.  give a property for how elongated it is (go from sphere to rod)
        Shield,      // a flat plate, or maybe a ball
        Scythe,     // not practical, but looks cool
    }
    public abstract class WeaponHeadBase
    {
        /// <summary>
        /// True=Can't attach anything onto this (blade, spike)
        /// False=Can attach more to this (hammer, mace)
        /// </summary>
        public abstract bool IsFinal { get; }
    }

    #endregion
}
