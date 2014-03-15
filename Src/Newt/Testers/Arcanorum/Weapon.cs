using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.Arcanorum
{
    //TODO:
    //      Make the part types unlockable based on experience, and only get experience in what they've picked up/purchased and used
    //      This class should expose a class that tells what weapon types gain experience - ex: if the weapon has a handle and a short chain, both would gain experience but a ratio
    //      Even if a part is unlocked, make the available quality of that part limited based on experience
    //      Calculate cost

    //TODO: Also take damage when striking a bot that is in the middle of ramming

    public class Weapon : IMapObject, IGivesDamage, IDisposable
    {
        #region Class: MaterialCache

        private class MaterialCache
        {
            //TODO: Wood and metal should have some slight variants, not all identical
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

            private System.Windows.Media.Media3D.Material _handle_AttachPoint = null;
            public System.Windows.Media.Media3D.Material Handle_AttachPoint
            {
                get
                {
                    if (_handle_AttachPoint == null)
                    {
                        _handle_AttachPoint = UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("40808080"));
                    }

                    return _handle_AttachPoint;
                }
            }

            public Color Handle_MoonGemLight = UtilityWPF.ColorFromHex("6DBCC9");
        }

        #endregion

        #region Declaration Section

        private readonly WeaponHandleDNA _handle;

        private readonly double _mass;

        private Model3D _attachModel = null;

        private readonly bool _isAttachInMiddle;
        private readonly double _massLeft;
        private readonly double _massRight;

        #endregion

        #region Constructor

        //TODO: Once more than a single handle is passed in, may need to take in the attach point explicitely (or it should be part of the container WeaponDNA object)
        public Weapon(WeaponHandleDNA handle, Point3D position, World world, int materialID)
        {
            this.IsGraphicsOnly = world == null;

            #region WPF Model

            var model = GetModel(handle);
            _model = model.Item1;
            this.DNA = model.Item2;

            ModelVisual3D visual = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
            visual.Content = this.Model;
            _visuals3D = new Visual3D[] { visual };

            #endregion

            #region Physics Body

            double radiusExaggerated = handle.Radius * 2d;
            double volume = Math.PI * radiusExaggerated * radiusExaggerated * handle.Length;
            _mass = GetDensity(handle.HandleMaterial) * volume;

            if (this.IsGraphicsOnly)
            {
                this.PhysicsBody = null;
                this.Token = TokenGenerator.Instance.NextToken();
            }
            else
            {
                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(position.ToVector()));

                using (CollisionHull hull = CollisionHull.CreateCylinder(world, 0, handle.Radius, handle.Length, null))
                {
                    this.PhysicsBody = new Body(hull, transform.Value, _mass, _visuals3D);
                    this.PhysicsBody.MaterialGroupID = materialID;
                    //this.PhysicsBody.LinearDamping = .5d;
                    //this.PhysicsBody.LinearDamping = .1d;
                    this.PhysicsBody.LinearDamping = .01d;
                    this.PhysicsBody.AngularDamping = new Vector3D(.01d, .01d, .01d);

                    this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
                }

                this.Token = this.PhysicsBody.Token;

                this.PhysicsBody.Rotation = new Quaternion(new Vector3D(0, 0, 1), 90);

                if (handle.AttachPointPercent > .5d)
                {
                    // Rotate it so new ones will spawn pointing down
                    this.PhysicsBody.Rotation = this.PhysicsBody.Rotation.RotateBy(new Quaternion(new Vector3D(0, 0, 1), 180));
                }
            }

            #endregion

            this.Radius = handle.Length / 2d;

            _handle = handle;

            #region Attach Point

            this.AttachPoint = new Point3D(-(handle.Length / 2d) + (handle.Length * handle.AttachPointPercent), 0, 0);

            if (Math3D.IsNearZero(handle.AttachPointPercent) || Math3D.IsNearValue(handle.AttachPointPercent, 1d))
            {
                _isAttachInMiddle = false;
                _massLeft = _mass;      // these masses shouldn't be used anyway
                _massRight = _mass;
            }
            else
            {
                _isAttachInMiddle = true;
                _massRight = _mass * handle.AttachPointPercent;
                _massLeft = _mass * (1d - handle.AttachPointPercent);
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
                    this.PhysicsBody.ApplyForceAndTorque -= new EventHandler<NewtonDynamics.BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);

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

            //TODO: Take damage, dull the blade, etc

            if (collisions.Length == 0)
            {
                return null;
            }

            var avgCollision = MaterialCollision.GetAverageCollision(collisions, this.PhysicsBody);

            #region Figure out mass

            double massForDamage = _mass;
            if (_isAttachInMiddle)
            {
                // If the attach point is in the middle, then only a percent of the mass should be used
                Point3D avgPosLocal = this.PhysicsBody.PositionFromWorld(avgCollision.Item1);

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

            return new WeaponDamage(avgCollision.Item1, kinetic);
        }

        #endregion

        #region Public Properties

        public readonly bool IsGraphicsOnly;

        /// <summary>
        /// This is in model coords
        /// </summary>
        public readonly Point3D AttachPoint;

        public IGravityField Gravity
        {
            get;
            set;
        }

        private static ThreadLocal<MaterialCache> _materials = new ThreadLocal<MaterialCache>(() => new MaterialCache());       // need to pass the delegate, or Value will just be null
        private MaterialCache Materials
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
        public readonly WeaponHandleDNA DNA;

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
            this.PhysicsBody.Position = new Point3D(0, 0, 0);

            // Convert to model coords
            Point3D destinationModel = this.PhysicsBody.PositionFromWorld(point);

            Vector3D attachRotated = prevRotation.GetRotatedVector(this.AttachPoint.ToVector());

            destinationModel = point + attachRotated;

            this.PhysicsBody.Position = this.PhysicsBody.PositionToWorld(destinationModel);
            this.PhysicsBody.Rotation = prevRotation;
        }

        //public void Extend()
        //{
        //}
        //public void Retract()
        //{
        //}

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, NewtonDynamics.BodyApplyForceAndTorqueArgs e)
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

        #region Private Methods - GetModel

        private Tuple<Model3DGroup, WeaponHandleDNA> GetModel(WeaponHandleDNA dna)
        {
            WeaponHandleDNA finalDNA = UtilityHelper.Clone(dna);
            if (finalDNA.KeyValues == null)
            {
                finalDNA.KeyValues = new SortedList<string, double>();
            }

            Model3DGroup model = null;

            // If there is a mismatch between desired material and type, prefer material
            finalDNA.HandleType = GetHandleType(dna.HandleMaterial, dna.HandleType);

            // Build the handle
            switch (finalDNA.HandleType)
            {
                case WeaponHandleType.Rod:
                    model = GetModel_Rod(dna, finalDNA);
                    break;

                case WeaponHandleType.Rope:
                    throw new ApplicationException("Rope currently isn't supported");

                default:
                    throw new ApplicationException("Unknown WeaponHandleType: " + finalDNA.HandleType.ToString());
            }

            // Exit Function
            return Tuple.Create(model, finalDNA);

            #region WRAITH
            //retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.DiffuseColor))));
            //retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.SpecularColor)), this.SpecularPower.Value));
            //retVal.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.EmissiveColor))));


            //case WeaponHandleMaterial.Wraith:
            //    #region Wraith

            //    retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("6048573E"))));
            //    retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("202C1D33")), 50d));
            //    retVal.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("1021222B"))));

            //    #endregion
            //    break;




            //if (useDebugVisual)
            //{
            //    geometry.Geometry = UtilityWPF.GetLine(new Point3D(-(handle.Length / 2d), 0, 0), new Point3D(handle.Length / 2d, 0, 0), handle.Radius * 2d);
            //}
            #endregion
        }

        private Model3DGroup GetModel_Rod(WeaponHandleDNA dna, WeaponHandleDNA finalDNA)
        {
            Model3DGroup retVal = new Model3DGroup();

            switch (dna.HandleMaterial)
            {
                case WeaponHandleMaterial.Soft_Wood:
                case WeaponHandleMaterial.Hard_Wood:
                    GetModel_Rod_Wood(retVal, dna, finalDNA);
                    break;

                case WeaponHandleMaterial.Bronze:
                case WeaponHandleMaterial.Iron:
                case WeaponHandleMaterial.Steel:
                    GetModel_Rod_Metal(retVal, dna, finalDNA);
                    break;

                case WeaponHandleMaterial.Composite:
                    GetModel_Rod_Composite(retVal, dna, finalDNA);
                    break;

                case WeaponHandleMaterial.Klinth:
                    GetModel_Rod_Klinth(retVal, dna, finalDNA);
                    break;

                case WeaponHandleMaterial.Moon:
                    GetModel_Rod_Moon(retVal, dna, finalDNA);
                    break;

                default:
                    throw new ApplicationException("Unknown WeaponHandleMaterial: " + dna.HandleMaterial.ToString());
            }

            // Attach Point
            _attachModel = GetModel_Rod_AttachPoint(dna, finalDNA);

            if (_showAttachPoint)
            {
                retVal.Children.Add(_attachModel);
            }

            // Exit Function
            return retVal;
        }

        private Model3D GetModel_Rod_AttachPoint(WeaponHandleDNA dna, WeaponHandleDNA finalDNA)
        {
            //NOTE: This is backward from the constructor
            double offsetX = (dna.Length / 2d) - (dna.Length * dna.AttachPointPercent);

            double offsetY = dna.Radius * .5d;
            double offsetZ = dna.Radius * 2.5d;
            double radius = dna.Radius / 3d;

            Model3DGroup retVal = new Model3DGroup();

            // Line Z
            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = this.Materials.Handle_AttachPoint;
            geometry.BackMaterial = geometry.Material;

            geometry.Geometry = UtilityWPF.GetLine(new Point3D(offsetX, -offsetY, -offsetZ), new Point3D(offsetX, offsetY, offsetZ), radius);

            retVal.Children.Add(geometry);

            // Line Y
            geometry = new GeometryModel3D();

            geometry.Material = this.Materials.Handle_AttachPoint;
            geometry.BackMaterial = geometry.Material;

            geometry.Geometry = UtilityWPF.GetLine(new Point3D(offsetX, offsetY, -offsetZ), new Point3D(offsetX, -offsetY, offsetZ), radius);

            retVal.Children.Add(geometry);

            // Exit Function
            return retVal;
        }

        //TODO: random chance of randomly placing metal fittings on the handle
        private void GetModel_Rod_Wood(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA)
        {
            const double PERCENT = 1;

            Random rand = StaticRandom.GetRandomForThread();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            GeometryModel3D geometry = new GeometryModel3D();

            #region Material

            switch (dna.HandleMaterial)
            {
                case WeaponHandleMaterial.Soft_Wood:
                    geometry.Material = this.Materials.Handle_SoftWood;
                    break;

                case WeaponHandleMaterial.Hard_Wood:
                    geometry.Material = this.Materials.Handle_HardWood;
                    break;

                default:
                    throw new ApplicationException("Unexpected WeaponHandleMaterial: " + dna.HandleMaterial.ToString());
            }

            geometry.BackMaterial = geometry.Material;

            #endregion

            #region Tube

            double maxX1 = GetKeyValue("maxX1", from, to, rand.NextDouble(.45, .7));
            double maxX2 = GetKeyValue("maxX2", from, to, rand.NextDouble(.45, .7));

            double maxY1 = GetKeyValue("maxY1", from, to, rand.NextDouble(.85, 1.05));
            double maxY2 = GetKeyValue("maxY2", from, to, rand.NextDouble(.85, 1.05));

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, maxX1 * .45, maxY1 * .75, true));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("ring1", from, to, rand.NextPercent(.5, PERCENT)), false, maxX1 * .5, maxY1 * 1, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("ring2", from, to, rand.NextPercent(2, PERCENT)), false, maxX1 * .4, maxY1 * .8, false));

            rings.Add(new TubeRingRegularPolygon(GetKeyValue("ring3", from, to, rand.NextPercent(5, PERCENT)), false, Math3D.Avg(maxX1, maxX2) * .35, Math3D.Avg(maxY1, maxY2) * .75, false));

            rings.Add(new TubeRingRegularPolygon(GetKeyValue("ring4", from, to, rand.NextPercent(5, PERCENT)), false, maxX2 * .4, maxY2 * .8, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("ring5", from, to, rand.NextPercent(2, PERCENT)), false, maxX2 * .5, maxY2 * 1, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("ring6", from, to, rand.NextPercent(.5, PERCENT)), false, maxX2 * .45, maxY2 * .75, true));

            rings = TubeRingBase.FitNewSize(rings, dna.Radius * Math.Max(maxX1, maxX2), dna.Radius * Math.Max(maxY1, maxY2), dna.Length);        // multiplying x by maxX, because the rings were defined with x maxing at maxX, and y maxing at 1

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(10, rings, true, true, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x

            #endregion

            geometries.Children.Add(geometry);
        }

        private void GetModel_Rod_Metal(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA)
        {
            Random rand = StaticRandom.GetRandomForThread();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            #region Materials

            System.Windows.Media.Media3D.Material material1, material2;

            switch (dna.HandleMaterial)
            {
                case WeaponHandleMaterial.Bronze:
                    material1 = this.Materials.Handle_Bronze;       // the property get returns a slightly random color
                    material2 = this.Materials.Handle_Bronze;
                    break;

                case WeaponHandleMaterial.Iron:
                    material1 = this.Materials.Handle_Iron;
                    material2 = this.Materials.Handle_Iron;
                    break;

                case WeaponHandleMaterial.Steel:
                    material1 = this.Materials.Handle_Steel;
                    material2 = this.Materials.Handle_Steel;
                    break;

                default:
                    throw new ApplicationException("Unexpected WeaponHandleMaterial: " + dna.HandleMaterial.ToString());
            }

            #endregion

            #region Ends

            double capRadius = dna.Radius * 1.1;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, capRadius * .75, capRadius * .75, true));
            rings.Add(new TubeRingRegularPolygon(capRadius * .2, false, capRadius, capRadius, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("capWidth", from, to, capRadius * (1d + rand.NextPow(7d, 2.2d, false))), false, capRadius, capRadius, false));
            rings.Add(new TubeRingRegularPolygon(capRadius * .8, false, capRadius * .75, capRadius * .75, true));

            double capHeight = TubeRingBase.GetTotalHeight(rings);
            double halfLength = dna.Length / 2d;
            double halfCap = capHeight / 2d;

            // Cap 1
            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = material1;
            geometry.BackMaterial = material1;

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x
            transform.Children.Add(new TranslateTransform3D(-halfLength + halfCap, 0, 0));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(6, rings, false, true, transform);

            geometries.Children.Add(geometry);

            // Cap 2
            geometry = new GeometryModel3D();

            geometry.Material = material1;
            geometry.BackMaterial = material1;

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));      // the tube builds along z, but this class wants along x
            transform.Children.Add(new TranslateTransform3D(halfLength - halfCap, 0, 0));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(6, rings, false, true, transform);

            geometries.Children.Add(geometry);

            #endregion

            #region Shaft

            geometry = new GeometryModel3D();

            geometry.Material = material2;
            geometry.BackMaterial = material2;

            rings = new List<TubeRingBase>();

            rings.Add(new TubeRingRegularPolygon(0, false, dna.Radius * .8, dna.Radius * .8, true));
            rings.Add(new TubeRingRegularPolygon(dna.Length - capHeight, false, dna.Radius * .8, dna.Radius * .8, true));

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 45)));      // make the bar impact along the edge instead of the flat part

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(4, rings, false, true, transform);

            geometries.Children.Add(geometry);

            #endregion
        }

        private void GetModel_Rod_Composite(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA)
        {
            Random rand = StaticRandom.GetRandomForThread();

            finalDNA.MaterialsForCustomizable = WeaponHandleDNA.GetRandomMaterials_Composite(dna.MaterialsForCustomizable);
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            double halfLength = dna.Length / 2d;
            double halfBeamThickness = dna.Radius / 8d;
            double halfCoreThickness = (dna.Radius * .66d) / 2d;
            double washerRadius = dna.Radius * 1.1;

            double washerThickness1 = GetKeyValue("washerThickness1", from, to, dna.Length * rand.NextPercent(.015d, .5d));
            double washerThickness2 = GetKeyValue("washerThickness2", from, to, dna.Length * rand.NextPercent(.15d, .5d));
            double washerOffset = GetKeyValue("washerOffset", from, to, rand.NextPercent(.05d, .5d));

            var material = GetModel_Rod_CompositeSprtMaterial(finalDNA.MaterialsForCustomizable[0]);

            //NOTE: The beam/core dimensions shouldn't be randomized.  This should look like a manufactured, almost mass produced product
            #region Beams

            // Beam1
            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfLength, -dna.Radius, -halfBeamThickness), new Point3D(halfLength, dna.Radius, halfBeamThickness));

            geometries.Children.Add(geometry);

            // Beam2
            geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfLength, -halfBeamThickness, -dna.Radius), new Point3D(halfLength, halfBeamThickness, dna.Radius));

            geometries.Children.Add(geometry);

            #endregion
            #region Core

            material = GetModel_Rod_CompositeSprtMaterial(finalDNA.MaterialsForCustomizable[1]);

            geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfLength, -halfCoreThickness, -halfCoreThickness), new Point3D(halfLength, halfCoreThickness, halfCoreThickness));

            geometry.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 45));

            geometries.Children.Add(geometry);

            #endregion
            #region Washers

            material = GetModel_Rod_CompositeSprtMaterial(finalDNA.MaterialsForCustomizable[2]);

            var locations = new Tuple<double, double>[] 
            {
                Tuple.Create(0d, washerThickness1),
                Tuple.Create(washerOffset, washerThickness1),
                Tuple.Create(.5d, washerThickness2),
                Tuple.Create(1d - washerOffset, washerThickness1),
                Tuple.Create(1d, washerThickness1)
            };

            foreach (var loc in locations)
            {
                geometry = new GeometryModel3D();

                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = UtilityWPF.GetCylinder_AlongX(8, washerRadius, loc.Item2);

                geometry.Transform = new TranslateTransform3D(-halfLength + (dna.Length * loc.Item1), 0, 0);

                geometries.Children.Add(geometry);
            }

            #endregion
        }
        private static System.Windows.Media.Media3D.Material GetModel_Rod_CompositeSprtMaterial(MaterialDefinition definition)
        {
            MaterialGroup retVal = new MaterialGroup();

            Color color1 = UtilityWPF.ColorFromHex(definition.DiffuseColor);

            retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(color1.R, color1.G, color1.B))));     // making sure there is no semitransparency
            retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 50));

            return retVal;
        }

        private void GetModel_Rod_Klinth(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA)
        {
            GeometryModel3D geometry = new GeometryModel3D();

            MaterialGroup material = new MaterialGroup();

            // Ensure enough colors
            finalDNA.MaterialsForCustomizable = WeaponHandleDNA.GetRandomMaterials_Klinth(dna.MaterialsForCustomizable);

            Color color2 = UtilityWPF.ColorFromHex(finalDNA.MaterialsForCustomizable[0].DiffuseColor);
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, color2.R, color2.G, color2.B))));

            color2 = UtilityWPF.ColorFromHex(finalDNA.MaterialsForCustomizable[0].SpecularColor);
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, color2.R, color2.G, color2.B)), 75));

            color2 = UtilityWPF.ColorFromHex(finalDNA.MaterialsForCustomizable[0].EmissiveColor);
            material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(20, color2.R, color2.G, color2.B))));

            geometry.Material = material;
            geometry.BackMaterial = material;

            //NOTE: The dimensions shouldn't be randomized.  This should look like a manufactured, almost mass produced product.
            // Also, being a crystal, it needs to appear solid

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingPoint(0, false));
            rings.Add(new TubeRingRegularPolygon(.2, false, .75, .75, false));
            rings.Add(new TubeRingRegularPolygon(.3, false, 1, 1, false));
            rings.Add(new TubeRingRegularPolygon(.5, false, .9, .9, false));
            rings.Add(new TubeRingRegularPolygon(1, false, .8, .8, false));
            rings.Add(new TubeRingRegularPolygon(15, false, .8, .8, false));
            rings.Add(new TubeRingRegularPolygon(1, false, .9, .9, false));
            rings.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
            rings.Add(new TubeRingRegularPolygon(.3, false, .75, .75, false));
            rings.Add(new TubeRingPoint(.2, false));

            rings = TubeRingBase.FitNewSize(rings, dna.Radius, dna.Radius, dna.Length);

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(5, rings, false, true, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x

            geometries.Children.Add(geometry);
        }

        private void GetModel_Rod_Moon(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA)
        {
            const double PERCENT = 1;

            Random rand = StaticRandom.GetRandomForThread();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            #region Shaft

            GeometryModel3D shaft = new GeometryModel3D();

            shaft.Material = this.Materials.Handle_Moon;
            shaft.BackMaterial = shaft.Material;

            double maxRad1 = GetKeyValue("maxRad1", from, to, rand.NextDouble(.7, 1.02));
            double maxRad2 = GetKeyValue("maxRad2", from, to, rand.NextDouble(.7, 1.02));
            double maxRad12 = Math.Max(maxRad1, maxRad2);       // this is used in several places

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingRegularPolygon(0, false, maxRad1 * .4, maxRad1 * .4, true));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("tube1", from, to, rand.NextPercent(.25, PERCENT)), false, maxRad1 * .8, maxRad1 * .8, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("tube2", from, to, rand.NextPercent(.3, PERCENT)), false, maxRad1 * .85, maxRad1 * .85, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("tube3", from, to, rand.NextPercent(.75, PERCENT)), false, maxRad1 * .6, maxRad1 * .6, false));

            rings.Add(new TubeRingRegularPolygon(GetKeyValue("tube4", from, to, rand.NextPercent(20, PERCENT)), false, maxRad2 * .8, maxRad2 * .8, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("tube5", from, to, rand.NextPercent(1, PERCENT)), false, maxRad2 * .9, maxRad2 * .9, false));
            rings.Add(new TubeRingRegularPolygon(GetKeyValue("tube6", from, to, rand.NextPercent(1, PERCENT)), false, maxRad2 * 1, maxRad2 * 1, false));
            rings.Add(new TubeRingDome(GetKeyValue("tube7", from, to, rand.NextPercent(2.5, PERCENT)), false, 4));

            rings = TubeRingBase.FitNewSize(rings, maxRad12 * dna.Radius, maxRad12 * dna.Radius, dna.Length);

            shaft.Geometry = UtilityWPF.GetMultiRingedTube(10, rings, true, true, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));      // the tube builds along z, but this class wants along x

            #endregion

            // Number of gems
            int numIfNew = 0;
            if (rand.NextDouble() > .66d)       // only 33% will get gems
            {
                // Of the handles with gems, only 5% will get 2
                numIfNew = rand.NextDouble() > .95 ? 2 : 1;
            }

            int numGems = Convert.ToInt32(GetKeyValue("numGems", from, to, numIfNew));

            if (numGems == 0)
            {
                geometries.Children.Add(shaft);
                return;
            }

            #region Gems

            List<double> percents = new List<double>();

            for (int cntr = 0; cntr < numGems; cntr++)
            {
                string keyPrefix = "gem" + cntr.ToString();

                // Get a placement for this gem
                double percentIfNew = 0;
                do
                {
                    percentIfNew = rand.NextDouble(.15, .85);

                    if (percents.Count == 0)
                    {
                        break;
                    }
                } while (percents.Any(o => Math.Abs(percentIfNew - o) < .15));

                double percent = GetKeyValue(keyPrefix + "Percent", from, to, percentIfNew);

                percents.Add(percent);

                // Gem
                GeometryModel3D gem = new GeometryModel3D();

                gem.Material = this.Materials.Handle_MoonGem;
                gem.BackMaterial = gem.Material;

                double width = GetKeyValue(keyPrefix + "Width", from, to, rand.NextDouble(maxRad12 * 1d, maxRad12 * 1.4d));

                gem.Geometry = UtilityWPF.GetSphere(5, dna.Radius * width);
                Point3D position = new Point3D((dna.Length * percent) - (dna.Length / 2d), 0, 0);
                gem.Transform = new TranslateTransform3D(position.ToVector());

                // Light
                PointLight pointLight = new PointLight(this.Materials.Handle_MoonGemLight, position);
                UtilityWPF.SetAttenuation(pointLight, dna.Radius * 120d, .1d);

                geometries.Children.Add(pointLight);
                geometries.Children.Add(gem);
            }

            // Adding this after so that you don't see the shaft through the gems
            geometries.Children.Add(shaft);

            #endregion
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

        private static double GetDensity(WeaponHandleMaterial handle)
        {
            switch (handle)
            {
                // http://www.engineeringtoolbox.com/wood-density-d_40.html
                case WeaponHandleMaterial.Soft_Wood:
                    //return .4d;       //accurate, but too wildly low
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

        /// <summary>
        /// Some types are only valid for certain materials.  So rather than throw an error, just change the type
        /// </summary>
        private static WeaponHandleType GetHandleType(WeaponHandleMaterial material, WeaponHandleType requestedType)
        {
            switch (material)
            {
                case WeaponHandleMaterial.Soft_Wood:
                case WeaponHandleMaterial.Hard_Wood:
                    return WeaponHandleType.Rod;

                //case WeaponHandleMaterial.CheapRope:
                //case WeaponHandleMaterial.HempRope:
                //case WeaponHandleMaterial.Wraith:
                //    return WeaponHandleType.Rope;

                default:
                    return requestedType;
            }
        }

        /// <summary>
        /// This tries to get the value from the from list.  If it doesn't exist, it uses the valueIfNew.
        /// This always stores the return value in the to list
        /// </summary>
        private static T GetKeyValue<T>(string key, SortedList<string, T> from, SortedList<string, T> to, T valueIfNew)
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

        #endregion
    }

    #region Class: WeaponDamage

    public class WeaponDamage
    {
        #region Constructor

        public WeaponDamage(Point3D position = new Point3D(), double? kinetic = null, double? pierce = null, double? slash = null/*, double? flame = null, double? freeze = null, double? electric = null, double? poison = null*/)
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
        public readonly Point3D Position;

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

    #region Enum: WeaponHandleMaterial

    public enum WeaponHandleMaterial
    {
        // Wood - only for rods
        Soft_Wood,
        Hard_Wood,

        // Rope - only for ropes
        //CheapRope,
        //HempRope,

        // Metal - these are durable, but very dense
        Bronze,
        Iron,
        Steel,

        // The user can customize the colors of these two.  They are a step up from wood and metal
        /// <summary>
        /// Density and durability are between wood and metal
        /// </summary>
        Composite,
        /// <summary>
        /// A bit less dense than metal, same durability as composite (but is self repairing) - crystal appearance
        /// </summary>
        Klinth,

        // Special
        //      These will be rare.  not available when creating a weapon, must be picked up/purchased as is.
        //      They will have certain magic traits that come on randomly.
        //      Socketed gems won't change traits, just enhance
        //      Come up with heads that are more/less effective with each type of handle
        /// <summary>
        /// Boost strength - maybe some knockback
        /// Weapon self repairs
        /// </summary>
        /// <remarks>
        /// Blugeon heads work best
        /// 
        /// If they do so many kills using a moon with no gems, attach point at the end, and a proportional size, then give
        /// them the title of "Deadly Lolipop", and maybe an extra ability
        /// </remarks>
        Moon,
        /// <summary>
        /// Slow time
        /// a bit extra damage
        /// (tricky types of effects, maybe some random ranged damage)
        /// confusion/invisibility
        /// TODO: Read about wraiths, what are they really?
        /// </summary>
        /// <remarks>
        /// Extra effective when combined with a sword or spike
        /// </remarks>
        //Wraith,     //TODO: instead of a cylinder, this needs to be 2 to 5 shards.  put a point light in each.  maybe some random sparks appear and disappear.  this will always be rope, but limit the angles
        /// <summary>
        /// Life steal?  Reduce some abilites if used too much
        /// </summary>
        /// <remarks>
        /// If combined with an axe head, it gets extra enraged, reduces damage overall
        /// </remarks>
        //Enttrail,       // made from a dead ent.  regenerative abilities, but also angry
        /// <summary>
        /// Possessed by a demon.  Not moody/angry like the ent, but powerful
        /// Heavy damage
        /// </summary>
        /// <remarks>
        /// Maybe some kind of tractor beam - or pull the enemy bot closer, but repel the weapon (would need to repel it
        /// orth to direction so that double sided weapons aren't an issue)
        /// 
        /// have it help maintain swing speed (the equivalent of a small thruster attached to it, not always on, but will sometimes
        /// decide to get the weapon spun up quickly)
        /// 
        /// Thrives on chain kills...faster, faster.  Gets more and more powerful
        /// </remarks>
        //Demon,
    }

    #endregion
    #region Enum: WeaponHandleType

    public enum WeaponHandleType
    {
        Rope,
        Rod,

        // Overly complex.  Just allow for being extendable
        ///// <summary>
        ///// A rod that can extend (Make an option for spring loaded or just inertia)
        ///// </summary>
        //Telescoping,
        ///// <summary>
        ///// A rod that connects to a rope that can be released, retracted
        ///// </summary>
        //Harpoon,
    }

    #endregion
    #region Class: WeaponHandleDNA

    // Multiple handles could be attached together
    //TODO:
    //      Durability, % damaged
    //      The details of the graphic are randomly generated at build time.  Need to store those details so that a particular handle can be perfectly recreated (useful for saving/loading, moving to/from world/inventory)
    public class WeaponHandleDNA
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
        /// This doesn't have to be unique, just giving the user a chance to name it
        /// </summary>
        public string Name { get; set; }

        public WeaponHandleMaterial HandleMaterial { get; set; }

        public WeaponHandleType HandleType { get; set; }

        /// <summary>
        /// This is the intended attach point.  A weapon can have multiple handles, so this is more of a suggestion than a hard rule
        /// </summary>
        public double AttachPointPercent { get; set; }

        //public bool IsExtendable { get; set; }

        public double Length { get; set; }
        //public double MaxLength { get; set; }

        public double Radius { get; set; }

        // These these store custom settings
        public SortedList<string, double> KeyValues { get; set; }
        public MaterialDefinition[] MaterialsForCustomizable { get; set; }

        #region Public Methods

        public static WeaponHandleDNA GetRandomDNA(WeaponHandleMaterial? material = null, WeaponHandleType? type = null, bool? isDouble = null, double? length = null, double? radius = null)
        {
            const double DOUBLE_END = .2d;
            const double DOUBLE_CENTER = .05d;       // this is only half of the center

            WeaponHandleDNA retVal = new WeaponHandleDNA();

            retVal.UniqueID = Guid.NewGuid();

            Random rand = StaticRandom.GetRandomForThread();

            bool isDoubleActual = isDouble ?? (rand.Next(2) == 0);

            #region Material

            if (material == null)
            {
                retVal.HandleMaterial = UtilityHelper.GetRandomEnum<WeaponHandleMaterial>();
            }
            else
            {
                retVal.HandleMaterial = material.Value;
            }

            #endregion

            //TODO: Support rope
            retVal.HandleType = WeaponHandleType.Rod;

            #region Length

            if (length == null)
            {
                double lengthActual = rand.NextDouble(1.5d, 3.5d);
                if (isDoubleActual)
                {
                    lengthActual *= 2d;
                }

                retVal.Length = lengthActual;
            }
            else
            {
                retVal.Length = length.Value;
            }

            #endregion
            #region Radius

            if (radius == null)
            {
                double radiusActual = rand.NextDouble(.03d, .17d);
                if (isDoubleActual)
                {
                    radiusActual *= 2d;
                }

                retVal.Radius = radiusActual;
            }
            else
            {
                retVal.Radius = radius.Value;
            }

            #endregion
            #region AttachPoint

            if (isDoubleActual)
            {
                // Since the center of mass is in the middle, the attach point can't be in the middle.
                // Both ends are out as well.
                // So, choose one of the areas that are stars
                //  |----|********|--------|********|----|

                double randMaxValue = .5d - DOUBLE_END - DOUBLE_CENTER;
                double randValue = rand.NextDouble(randMaxValue);
                double half = DOUBLE_CENTER + randValue;

                if (rand.Next(2) == 0)
                {
                    // left side
                    retVal.AttachPointPercent = .5d - half;
                }
                else
                {
                    // right side
                    retVal.AttachPointPercent = .5d + half;
                }
            }
            else
            {
                // Choose one of the ends
                retVal.AttachPointPercent = rand.Next(2) == 0 ? 0d : 1d;
            }

            #endregion

            switch (retVal.HandleMaterial)
            {
                case WeaponHandleMaterial.Composite:
                    break;
                case WeaponHandleMaterial.Klinth:
                    break;
            }

            return retVal;
        }

        public static MaterialDefinition[] GetRandomMaterials_Composite(MaterialDefinition[] existing = null)
        {
            List<MaterialDefinition> retVal = new List<MaterialDefinition>();

            Random rand = StaticRandom.GetRandomForThread();

            if (existing != null)
            {
                retVal.AddRange(existing);
            }

            if (retVal.Count < 1)
            {
                // For the first one, just pick any random color
                retVal.Add(new MaterialDefinition() { DiffuseColor = UtilityWPF.GetRandomColor(255, 0, 255).ToHex() });
            }

            ColorHSV first = UtilityWPF.ColorFromHex(retVal[0].DiffuseColor).ToHSV();

            if (retVal.Count < 2)
            {
                retVal.Add(new MaterialDefinition() { DiffuseColor = UtilityWPF.HSVtoRGB(GetRandomHue(first.H), rand.NextDouble(100), rand.NextDouble(100)).ToHex() });
            }

            if (retVal.Count < 3)
            {
                retVal.Add(new MaterialDefinition() { DiffuseColor = UtilityWPF.HSVtoRGB(GetRandomHue(first.H), rand.NextDouble(100), rand.NextDouble(100)).ToHex() });
            }

            return retVal.ToArray();
        }
        public static MaterialDefinition[] GetRandomMaterials_Klinth(MaterialDefinition[] existing = null)
        {
            MaterialDefinition[] retVal = existing;

            if (retVal == null || retVal.Length < 1)
            {
                retVal = Enumerable.Range(0, 1).
                    Select(o =>
                    {
                        if (retVal != null && retVal.Length > o)
                        {
                            return retVal[o];
                        }
                        else
                        {
                            return new MaterialDefinition()
                            {
                                DiffuseColor = UtilityWPF.HSVtoRGB(StaticRandom.NextDouble(0, 360), StaticRandom.NextDouble(50, 80), StaticRandom.NextDouble(40, 90)).ToHex(),
                                SpecularColor = UtilityWPF.HSVtoRGB(StaticRandom.NextDouble(0, 360), StaticRandom.NextDouble(50, 80), StaticRandom.NextDouble(40, 90)).ToHex(),
                                EmissiveColor = UtilityWPF.GetRandomColor(255, 0, 255).ToHex()
                            };
                        }
                    }).
                    ToArray();
            }

            return retVal;
        }

        //TODO: Take in previous hue's and have a slight chance of 120 or 90 degree offset instead of always 0 and 180
        private static double GetRandomHue(double hue)
        {
            // Randomly choose a new hue that is similar to the one passed in
            double retVal = hue + StaticRandom.NextPow(3d, 18d, true);

            if (retVal < 0d)
            {
                retVal += 360d;
            }
            else if (retVal > 360d)
            {
                retVal -= 360d;
            }

            // Maybe rotate 180 degrees
            if (StaticRandom.Next(2) == 0)
            {
                retVal += 180d;

                if (retVal < 0d)
                {
                    retVal += 360d;
                }
                else if (retVal > 360d)
                {
                    retVal -= 360d;
                }
            }

            return retVal;
        }

        #endregion
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
        Shield      // a flat plate, or maybe a ball
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
