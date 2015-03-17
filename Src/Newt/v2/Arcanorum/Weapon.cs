using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    //TODO:
    //      Make the part types unlockable based on experience, and only get experience in what they've picked up/purchased and used
    //      This class should expose a class that tells what weapon types gain experience - ex: if the weapon has a handle and a short chain, both would gain experience but a ratio
    //      Even if a part is unlocked, make the available quality of that part limited based on experience
    //      Calculate cost

    //TODO: Also take damage when striking a bot that is in the middle of ramming

    public class Weapon : IMapObject, IGivesDamage, IDisposable
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
            else if (Math3D.IsNearZero(dna.Handle.AttachPointPercent) && dna.HeadLeft != null)
            {
                throw new ApplicationException("can't have a weapon head on the left (that's where the attach point is)");
            }
            else if (Math3D.IsNearValue(dna.Handle.AttachPointPercent, 1d) && dna.HeadRight != null)
            {
                throw new ApplicationException("can't have a weapon head on the right (that's where the attach point is)");
            }

            this.IsUsable = true;

            this.IsGraphicsOnly = world == null;

            _handle = new WeaponHandle(this.Materials, dna.Handle);

            #region HeadLeft

            if (dna.HeadLeft == null)
            {
                _headLeft = null;
            }
            else if (dna.HeadLeft is WeaponSpikeBallDNA)
            {
                _headLeft = new WeaponSpikeBall(this.Materials, (WeaponSpikeBallDNA)dna.HeadLeft);
            }
            else if (dna.HeadLeft is WeaponAxeDNA)
            {
                _headLeft = new WeaponAxe(this.Materials, (WeaponAxeDNA)dna.HeadLeft, true);
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
            else if (dna.HeadRight is WeaponSpikeBallDNA)
            {
                _headRight = new WeaponSpikeBall(this.Materials, (WeaponSpikeBallDNA)dna.HeadRight);
            }
            else if (dna.HeadRight is WeaponAxeDNA)
            {
                _headRight = new WeaponAxe(this.Materials, (WeaponAxeDNA)dna.HeadRight, false);
            }
            else
            {
                throw new ApplicationException("Unknown type of dna.HeadRight: " + dna.HeadRight.GetType().ToString());
            }

            #endregion

            this.DNA = new WeaponDNA()
            {
                UniqueID = dna.UniqueID,
                Name = dna.Name,
                Handle = _handle.DNA,
                HeadLeft = _headLeft == null ? null : _headLeft.DNAPart,
                HeadRight = _headRight == null ? null : _headRight.DNAPart,
            };

            #region WPF Model

            _model = GetModel(this.DNA, _handle.Model, _headLeft == null ? null : _headLeft.Model, _headRight == null ? null : _headRight.Model);

            // Apparently, groovy has syntax to do the above like this (just passes null if _headLeft is null.  Here is a cool page that implements that functionality
            // in c#, but the lambdas end up as more syntax that then the iifs
            // http://codepyre.com/2012/08/safe-dereference-inverse-null-coalescing-support-for-c-sharp/
            //_model = GetModel(this.DNA, _handle.Model, _headLeft?.Model, _headRight?.Model);      

            ModelVisual3D visual = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
            visual.Content = this.Model;
            _visuals3D = new Visual3D[] { visual };

            // Attach Point
            _attachModel = GetModel_AttachPoint(this.DNA.Handle);

            if (_showAttachPoint)
            {
                _model.Children.Add(_attachModel);
            }

            #endregion

            #region Physics Body

            if (this.IsGraphicsOnly)
            {
                this.PhysicsBody = null;
                this.Token = TokenGenerator.NextToken();
            }
            else
            {
                var massBreakdown = GetMassMatrix_CenterOfMass(this.DNA);
                _mass = massBreakdown.Item1.Mass;

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(position.ToVector()));

                using (CollisionHull hull = GetCollisionHull(this.DNA, world, _partsByCollisionID, _handle, _headLeft, _headRight))
                {
                    this.PhysicsBody = new Body(hull, transform.Value, _mass, _visuals3D);
                    this.PhysicsBody.MaterialGroupID = materialID;
                    this.PhysicsBody.LinearDamping = .01d;
                    this.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);
                    this.PhysicsBody.MassMatrix = massBreakdown.Item1;
                    this.PhysicsBody.CenterOfMass = massBreakdown.Item2;

                    this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
                }

                this.Token = this.PhysicsBody.Token;

                // Rotate it so new ones will spawn pointing down
                if (this.DNA.Handle.AttachPointPercent < .5d)
                {
                    _initialRotation = new Quaternion(new Vector3D(0, 0, 1), 90);
                }
                else
                {
                    _initialRotation = new Quaternion(new Vector3D(0, 0, 1), -90);
                }

                //TODO: Figure out how to set this here, and not screw stuff up when attaching to the bot
                //this.PhysicsBody.Rotation = _initialRotation;
            }

            #endregion

            this.Radius = this.DNA.Handle.Length / 2d;

            #region Attach Point

            this.AttachPoint = new Point3D(-(this.DNA.Handle.Length / 2d) + (this.DNA.Handle.Length * this.DNA.Handle.AttachPointPercent), 0, 0);
            //this.AttachPoint = new Point3D((this.DNA.Handle.Length / 2d) - (this.DNA.Handle.Length * this.DNA.Handle.AttachPointPercent), 0, 0);

            if (Math3D.IsNearZero(this.DNA.Handle.AttachPointPercent) || Math3D.IsNearValue(this.DNA.Handle.AttachPointPercent, 1d))
            {
                _isAttachInMiddle = false;
                _massLeft = _mass;      // these masses shouldn't be used anyway
                _massRight = _mass;
            }
            else
            {
                _isAttachInMiddle = true;
                //TODO: Take the heads into account
                _massLeft = _mass * this.DNA.Handle.AttachPointPercent;
                _massRight = _mass * (1d - this.DNA.Handle.AttachPointPercent);
            }

            #endregion

            this.CreationTime = DateTime.Now;
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
        public WeaponDamage CalculateDamage(MaterialCollision[] collisions)
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

            WeaponDamage kineticDamage = new WeaponDamage(avgCollision.Item1, kinetic);

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
            WeaponDamage extraDamage = new WeaponDamage();

            Tuple<WeaponPart, Vector3D> collidedPart;
            if (_partsByCollisionID.TryGetValue(collisionHullID, out collidedPart))
            {
                extraDamage = collidedPart.Item1.CalculateExtraDamage(avgPosLocal + collidedPart.Item2, avgCollision.Item2);
            }

            #endregion

            return WeaponDamage.GetMerged(kineticDamage, extraDamage);
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
        public void MoveToAttachPoint_ORIG(Point3D point)
        {
            #region Validate
#if DEBUG
            if (this.IsGraphicsOnly)
            {
                throw new InvalidOperationException("MoveToAttachPoint can't be called when this class is graphics only");
            }
#endif
            #endregion

            Quaternion prevRotation = this.PhysicsBody.Rotation;

            this.PhysicsBody.Rotation = Quaternion.Identity;
            //this.PhysicsBody.Rotation = _initialRotation;     

            this.PhysicsBody.Position = new Point3D(0, 0, 0);

            // Convert to model coords
            Point3D destinationModel = this.PhysicsBody.PositionFromWorld(point);

            Vector3D attachRotated = prevRotation.GetRotatedVector((this.AttachPoint.ToVector()) * -1d);

            destinationModel = point + attachRotated;

            this.PhysicsBody.Position = this.PhysicsBody.PositionToWorld(destinationModel);
            this.PhysicsBody.Rotation = prevRotation;
            //this.PhysicsBody.Rotation = _initialRotation;
        }
        public void MoveToAttachPoint(Point3D point)
        {
            #region Validate
#if DEBUG
            if (this.IsGraphicsOnly)
            {
                throw new InvalidOperationException("MoveToAttachPoint can't be called when this class is graphics only");
            }
#endif
            #endregion

            Quaternion prevRotation = this.PhysicsBody.Rotation;

            this.PhysicsBody.Rotation = Quaternion.Identity;
            //this.PhysicsBody.Rotation = _initialRotation;     

            this.PhysicsBody.Position = new Point3D(0, 0, 0);

            // Convert to model coords
            Point3D destinationModel = this.PhysicsBody.PositionFromWorld(point);

            Vector3D attachRotated = prevRotation.GetRotatedVector((this.AttachPoint.ToVector()) * -1d);

            destinationModel = point + attachRotated;

            this.PhysicsBody.Position = this.PhysicsBody.PositionToWorld(destinationModel);
            //this.PhysicsBody.Rotation = prevRotation;
            //this.PhysicsBody.Rotation = _initialRotation;
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

            List<Tuple<Model3DGroup, double>> models = new List<Tuple<Model3DGroup, double>>();

            models.Add(Tuple.Create(handleModel, 0d));      // add the handle first, so that semi transparent balls look right

            if (headLeftModel != null)
            {
                models.Add(Tuple.Create(headLeftModel, dna.Handle.Length / -2d));
            }

            if (headRightModel != null)
            {
                models.Add(Tuple.Create(headRightModel, dna.Handle.Length / 2d));
            }

            return GetModel(models.ToArray());
        }
        private static Model3DGroup GetModel(Tuple<Model3DGroup, double>[] models)
        {
            Model3DGroup retVal = new Model3DGroup();

            foreach (var model in models)
            {
                // Apply a transform to slide this part
                if (!Math3D.IsNearZero(model.Item2))
                {
                    // The weapon is built along the x axis
                    TranslateTransform3D translate = new TranslateTransform3D(model.Item2, 0, 0);

                    if (model.Item1.Transform == null || (model.Item1.Transform is MatrixTransform3D && ((MatrixTransform3D)model.Item1.Transform).Value.IsIdentity))
                    {
                        // This model doesn't have an existing transform, so just overrite it
                        model.Item1.Transform = translate;
                    }
                    else
                    {
                        // There's already a transform.  Create a group
                        Transform3DGroup transform = new Transform3DGroup();

                        transform.Children.Add(model.Item1.Transform);
                        transform.Children.Add(translate);

                        model.Item1.Transform = transform;
                    }
                }

                // Store in the return group
                retVal.Children.Add(model.Item1);
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
                if (dna.HeadLeft is WeaponSpikeBallDNA)
                {
                    hulls.Add(GetCollisionHull_SpikeBall((WeaponSpikeBallDNA)dna.HeadLeft, world, nextCollisionID, headTranslate));
                }
                else if (dna.HeadLeft is WeaponAxeDNA)
                {
                    hulls.Add(GetCollisionHull_Axe((WeaponAxeDNA)dna.HeadLeft, world, nextCollisionID, headTranslate, (WeaponAxe)headLeft));
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
                if (dna.HeadRight is WeaponSpikeBallDNA)
                {
                    hulls.Add(GetCollisionHull_SpikeBall((WeaponSpikeBallDNA)dna.HeadRight, world, nextCollisionID, -headTranslate));
                }
                else if (dna.HeadRight is WeaponAxeDNA)
                {
                    hulls.Add(GetCollisionHull_Axe((WeaponAxeDNA)dna.HeadRight, world, nextCollisionID, -headTranslate, (WeaponAxe)headRight));
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

            #region Handle

            double radiusExaggerated = dna.Handle.Radius * 2d;
            double volume = Math.PI * radiusExaggerated * radiusExaggerated * dna.Handle.Length;
            mass = GetDensity(dna.Handle.HandleMaterial) * volume;

            breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, new Vector3D(dna.Handle.Length, dna.Handle.Radius * 2, dna.Handle.Radius * 2), cellSize);

            parts.Add(new UtilityNewt.MassPart(new Point3D(0, 0, 0), mass, Quaternion.Identity, breakdown));

            #endregion

            foreach (var head in new[] { Tuple.Create(dna.HeadLeft, dna.Handle.Length / -2d), Tuple.Create(dna.HeadRight, dna.Handle.Length / 2d) })
            {
                if (head.Item1 != null)
                {
                    if (head.Item1 is WeaponSpikeBallDNA)
                    {
                        #region Spike ball

                        WeaponSpikeBallDNA ball = (WeaponSpikeBallDNA)head.Item1;

                        double radius = ball.Radius;

                        //radiusExaggerated = radius * 2d;
                        radiusExaggerated = radius * 1d;

                        volume = 4d / 3d * Math.PI * radiusExaggerated * radiusExaggerated * radiusExaggerated;
                        mass = GetDensity(ball.Material) * volume;
                        mass *= 3;

                        Point3D position = new Point3D(head.Item2, 0, 0);

                        breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, new Vector3D(radius * 2, radius * 2, radius * 2), cellSize);

                        parts.Add(new UtilityNewt.MassPart(position, mass, Quaternion.Identity, breakdown));

                        #endregion
                    }
                    else if (head.Item1 is WeaponAxeDNA)
                    {
                        #region Axe

                        WeaponAxeDNA axeDNA = (WeaponAxeDNA)head.Item1;

                        Vector3D size = new Vector3D();
                        double yOffset = 0d;

                        switch (axeDNA.Sides)
                        {
                            case WeaponAxeSides.Single_BackFlat:
                            case WeaponAxeSides.Single_BackSpike:
                                size = new Vector3D(axeDNA.SizeSingle * 1.1, axeDNA.SizeSingle, axeDNA.SizeSingle * .05);
                                yOffset = axeDNA.SizeSingle * .5;      // CreateBox centers size, this will push the blade to the right (leaving some for the back
                                break;

                            case WeaponAxeSides.Double:
                                size = new Vector3D(axeDNA.SizeSingle * 2, axeDNA.SizeSingle, axeDNA.SizeSingle * .05);
                                break;

                            default:
                                throw new ApplicationException("Unknown WeaponAxeSides: " + axeDNA.Sides.ToString());
                        }

                        volume = size.X * size.Y * size.Z;
                        mass = GetDensity(WeaponSpikeBallMaterial.Iron_Steel) * volume;
                        mass *= 3;

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

        #endregion
    }

    #region Class: WeaponDNA

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
            WeaponHandleDNA handle = WeaponHandleDNA.GetRandomDNA();

            WeaponPartDNA leftHead = null;
            if (!Math3D.IsNearZero(handle.AttachPointPercent) && StaticRandom.NextDouble() < .25)
            {
                if(StaticRandom.NextBool())
                {
                    leftHead = WeaponSpikeBallDNA.GetRandomDNA(handle);
                }
                else
                {
                    leftHead = WeaponAxeDNA.GetRandomDNA();
                }
            }

            WeaponPartDNA rightHead = null;
            if (!Math3D.IsNearValue(handle.AttachPointPercent, 1d) && StaticRandom.NextDouble() < .25)
            {
                if (StaticRandom.NextBool())
                {
                    rightHead = WeaponSpikeBallDNA.GetRandomDNA(handle);
                }
                else
                {
                    rightHead = WeaponAxeDNA.GetRandomDNA();
                }
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
            return Math3D.IsNearZero(GetKeyValue(key, from, to, valueIfNew ? 0d : 1d));
        }

        #endregion
    }

    #endregion
    #region Class: WeaponPartDNA

    public class WeaponPartDNA
    {
        // These these store custom settings
        public SortedList<string, double> KeyValues { get; set; }
        public MaterialDefinition[] MaterialsForCustomizable { get; set; }
    }

    #endregion

    #region Class: WeaponPart

    public abstract class WeaponPart
    {
        public Model3DGroup Model
        {
            get;
            protected set;
        }

        public abstract WeaponPartDNA DNAPart { get; }

        /// <summary>
        /// This gives the part a chance to damage itself, and also returns any additional damage
        /// </summary>
        public abstract WeaponDamage CalculateExtraDamage(Point3D positionLocal, double collisionSpeed);
    }

    #endregion

    #region Class: WeaponDamage

    public class WeaponDamage
    {
        #region Constructor

        public WeaponDamage(Point3D? position = null, double? kinetic = null, double? pierce = null, double? slash = null/*, double? flame = null, double? freeze = null, double? electric = null, double? poison = null*/)
        {
            this.Position = position;

            this.Kinetic = kinetic;
            this.Pierce = pierce;
            this.Slash = slash;

            //this.Flame = flame;
            //this.Freeze = freeze;
            //this.Electric = electric;
            //this.Poison = poison;
        }

        #endregion

        /// <summary>
        /// This is in world coords
        /// </summary>
        public readonly Point3D? Position;

        public readonly double? Kinetic;
        public readonly double? Pierce;
        public readonly double? Slash;

        //TODO: Implement these - they will require something that applies damage over time (also may amplify damage if susceptible)
        //public readonly double? Flame;        // possibly sets on fire for a small period of time
        //public readonly double? Freeze;       // possibly slows down
        //public readonly double? Electric;     // possibly stuns or weakens
        //public readonly double? Poison;       // possibly applies damage over time

        #region Public Methods

        /// <summary>
        /// This is a helper method that adds up this instance's values, and multiplies each by the class passed in
        /// </summary>
        public double GetDamage()
        {
            double retVal = 0d;

            if (this.Kinetic != null)
            {
                retVal += this.Kinetic.Value;
            }

            if (this.Pierce != null)
            {
                retVal += this.Pierce.Value;
            }

            if (this.Slash != null)
            {
                retVal += this.Slash.Value;
            }

            return retVal;
        }
        public WeaponDamage GetDamage(WeaponDamage multipliers)
        {
            double? kinetic = null;
            if (this.Kinetic != null)
            {
                kinetic = this.Kinetic.Value * (multipliers.Kinetic ?? 1d);
            }

            double? pierce = null;
            if (this.Pierce != null)
            {
                pierce = this.Pierce.Value * (multipliers.Pierce ?? 1d);
            }

            double? slash = null;
            if (this.Slash != null)
            {
                slash = this.Slash.Value * (multipliers.Slash ?? 1d);
            }

            return new WeaponDamage(this.Position, kinetic, pierce, slash);
        }

        public static Tuple<bool, WeaponDamage> DoDamage(ITakesDamage item, WeaponDamage damage)
        {
            if (damage == null)
            {
                return Tuple.Create(false, damage);
            }

            WeaponDamage damageActual = damage.GetDamage(item.ReceiveDamageMultipliers);

            // Remove the damage.  If something was returned, that means hitpoints are zero
            return Tuple.Create(item.HitPoints.RemoveQuantity(damageActual.GetDamage(), false) > 0, damageActual);
        }

        public static WeaponDamage GetMerged(IEnumerable<WeaponDamage> damages)
        {
            double x = 0d;
            double y = 0d;
            double z = 0d;

            double kinetic = 0d;
            double pierce = 0d;
            double slash = 0d;

            int positionCount = 0;
            int kineticCount = 0;
            int pierceCount = 0;
            int slashCount = 0;

            foreach (WeaponDamage dmg in damages)
            {
                if (dmg.Position != null)
                {
                    x += dmg.Position.Value.X;
                    y += dmg.Position.Value.Y;
                    z += dmg.Position.Value.Z;
                    positionCount++;
                }

                if (dmg.Kinetic != null)
                {
                    kinetic += dmg.Kinetic.Value;
                    kineticCount++;
                }

                if (dmg.Pierce != null)
                {
                    pierce += dmg.Pierce.Value;
                    pierceCount++;
                }

                if (dmg.Slash != null)
                {
                    slash += dmg.Slash.Value;
                    slashCount++;
                }
            }

            return new WeaponDamage(
                positionCount == 0 ? (Point3D?)null : new Point3D(x / positionCount, y / positionCount, z / positionCount),
                kineticCount == 0 ? (double?)null : kinetic / kineticCount,
                pierceCount == 0 ? (double?)null : pierce / pierceCount,
                slashCount == 0 ? (double?)null : slash / slashCount);
        }
        public static WeaponDamage GetMerged(WeaponDamage damage1, WeaponDamage damage2)
        {
            return GetMerged(new[] { damage1, damage2 });
        }

        #endregion
    }

    #endregion
    #region Interface: ITakesWeaponDamage

    public interface ITakesDamage
    {
        /// <summary>
        /// Takes damage, returns true when destroyed
        /// </summary>
        /// <returns>
        /// Item1=true if it got destroyed
        /// Item2=how much damage was inflicted
        /// </returns>
        Tuple<bool, WeaponDamage> Damage(WeaponDamage damage, Weapon weapon = null);

        WeaponDamage ReceiveDamageMultipliers { get; }

        Container HitPoints { get; }
    }

    #endregion
    #region Interface: IGivesDamage

    public interface IGivesDamage
    {
        WeaponDamage CalculateDamage(MaterialCollision[] collisions);
    }

    #endregion

    #region Class: WeaponMaterialCache

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
