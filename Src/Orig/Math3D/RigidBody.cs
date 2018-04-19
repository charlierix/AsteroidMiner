using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
    /// <summary>
    /// This class is designed to emulate a rigid structure.  I have a base point and direction.  You then add mass points relative
    /// to that center (those points will probably represent something to the user - cargo tanks, weapons, whatever)
    /// 
    /// You can then hit me with forces along any vector relative to my center (of position), and I will turn those into translational
    /// and rotational momentum.
    /// </summary>
    /// <remarks>
    /// Each time a point mass is changed, I need to recalculate my center of mass, and inertia tensor
    /// 
    /// The postion and direction that my base ball represents is the center of position.  When you hit the base External and Internal
    /// force properties, those are translational only (never any angular force).  When you hit my force methods, I split it into rotation
    /// and translation
    /// </remarks>
    public class RigidBody : TorqueBall
    {
        #region Declaration Section

        private const double NEARZEROMASS = .000001d;

        /// <summary>
        /// These are all the point masses that stay in a fixed position relative to the center.
        /// </summary>
        private List<PointMass> _masses = new List<PointMass>();

        /// <summary>
        /// This is used so the mass set override doesn't throw an exception during the constructor
        /// </summary>
        private bool _constructorCalled = false;

        #endregion

        #region Constructor

        public RigidBody(MyVector position, DoubleVector origDirectionFacing, double radius)
            : base(position, origDirectionFacing, radius, 0)
        {
            _constructorCalled = true;
        }

        public RigidBody(MyVector position, DoubleVector origDirectionFacing, double radius, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, 0, 1, 1, 1, boundingBoxLower, boundingBoxUpper)
        {
            _constructorCalled = true;
        }

        /// <summary>
        /// This overload is used if you plan to do collisions
        /// </summary>
        public RigidBody(MyVector position, DoubleVector origDirectionFacing, double radius, double elasticity, double kineticFriction, double staticFriction, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius, 0, elasticity, kineticFriction, staticFriction, boundingBoxLower, boundingBoxUpper)
        {
            _constructorCalled = true;
        }

        /// <summary>
        /// This one is used to assist with the clone method (especially for my derived classes)
        /// </summary>
        /// <param name="usesBoundingBox">Just pass in what you have</param>
        /// <param name="boundingBoxLower">Set this to null if bounding box is false</param>
        /// <param name="boundingBoxUpper">Set this to null if bounding box is false</param>
        protected RigidBody(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, double radius, double elasticity, double kineticFriction, double staticFriction, bool usesBoundingBox, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, rotation, radius, 0, elasticity, kineticFriction, staticFriction, usesBoundingBox, boundingBoxLower, boundingBoxUpper)
        {
            _constructorCalled = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// WARNING:  Don't set this property directly.  Play with the point masses instead.
        /// </summary>
        public override double Mass
        {
            get
            {
                return base.Mass;

                /*
                // Since I don't allow the user to set mass directly, base.Mass will stay 0.  Instead, I need to add up
                // my point masses
                double retVal = 0;

                foreach (PointMass mass in _masses)
                {
                    retVal += mass.Mass;
                }

                return retVal;
                */
            }
            set
            {
                if (!_constructorCalled)
                {
                    if (value != 0)
                    {
                        throw new ArgumentOutOfRangeException("During the constructor, only a mass of zero can be handed in.  Add point masses instead");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Setting the mass directly is not allowed.  Add point masses instead");
                }

                // The only way execution can get here is zero is passed in on the constructor
                // The ball won't let me store zero, so I'll get close
                base.Mass = NEARZEROMASS;
            }
        }

        /// <summary>
        /// These are the point masses.  These are always relative to base.OriginalDirectionFacing (you would need to rotate
        /// a copy of them if you want them aligned to base.DirectionFacing)
        /// NOTE:  You can change the values of a point mass, and I will see that change (like the ice skater pulling her arms
        /// in to spin faster)
        /// </summary>
        public PointMass[] PointMasses
        {
            get
            {
                return _masses.ToArray();
            }
        }

        public int PointMassCount
        {
            get
            {
                return _masses.Count;
            }
        }

        #endregion

        #region Public Methods

        public override Sphere Clone()
        {
            // Make a shell
            // I want a copy of the bounding box, not a clone (everything else gets cloned)
            RigidBody retVal = new RigidBody(this.Position.Clone(), this.OriginalDirectionFacing.Clone(), this.Rotation.Clone(), this.Radius, this.Elasticity, this.KineticFriction, this.StaticFriction, this.UsesBoundingBox, this.BoundryLower, this.BoundryUpper);

            PointMass[] clonedMasses = _masses.ToArray();		// sounds like an army

            // Clone them (I don't want the events to go with)
            for (int massCntr = 0; massCntr < clonedMasses.Length; massCntr++)
            {
                clonedMasses[massCntr] = clonedMasses[massCntr].Clone();
            }

            // Now I can add the masses to my return
            retVal.AddRangePointMasses(clonedMasses);

            // Exit Function
            return retVal;
        }
        public RigidBody CloneRigidBody()
        {
            return this.Clone() as RigidBody;
        }

        public PointMass AddPointMass(double x, double y, double z, double mass)
        {
            // Make a new one
            PointMass retVal = new PointMass(x, y, z, mass);

            // Get set up to listen to its change event
            retVal.PropertyChanged += new EventHandler(PointMassChanged);

            // Add this to my list
            _masses.Add(retVal);

            // Tell myself to adjust for the added weight
            ResetInertiaTensorAndCenterOfMass();

            // Exit Function
            return retVal;
        }
        public void AddRangePointMasses(PointMass[] masses)
        {
            // Hook to these mass's change events
            for (int massCntr = 0; massCntr < masses.Length; massCntr++)
            {
                masses[massCntr].PropertyChanged += new EventHandler(PointMassChanged);
            }

            // Add them to my list
            _masses.AddRange(masses);

            // Tell myself to adjust for the added weight
            ResetInertiaTensorAndCenterOfMass();
        }

        #endregion
        #region Protected Methods

        protected override void ResetInertiaTensorAndCenterOfMass()
        {
            if (_masses.Count == 0)
            {
                // Since I have no mass or structure, I'll use default values (nothing better whack me in this state, I'm sure
                // the result would be near infinite velocity)
                base.CenterOfMass.X = 0;
                base.CenterOfMass.Y = 0;
                base.CenterOfMass.Z = 0;

                base.InertialTensorBody = MyMatrix3.IdentityMatrix;

                base.Mass = NEARZEROMASS;		// I don't want to call this.Mass, because it has extra checks

                return;
            }

            // Figure out the center of mass
            double totalMass;
            MyVector centerMass = GetCenterOfMass(out totalMass);

            // Get the locations of the point masses relative to the center of mass (instead of relative to base.Position)
            MyVector[] massLocations = GetRelativeMassPositions(centerMass);		// this array lines up with _masses

            // Figure out the inertia tensor
            MyMatrix3 inertiaTensor = new MyMatrix3();
            #region Calculate Tensor

            for (int massCntr = 0; massCntr < _masses.Count; massCntr++)
            {
                // M(Y^2 + Z^2)
                inertiaTensor.M11 += _masses[massCntr].Mass * ((massLocations[massCntr].Y * massLocations[massCntr].Y) + (massLocations[massCntr].Z * massLocations[massCntr].Z));

                // M(X^2 + Z^2)
                inertiaTensor.M22 += _masses[massCntr].Mass * ((massLocations[massCntr].X * massLocations[massCntr].X) + (massLocations[massCntr].Z * massLocations[massCntr].Z));

                // M(X^2 + Y^2)
                inertiaTensor.M33 += _masses[massCntr].Mass * ((massLocations[massCntr].X * massLocations[massCntr].X) + (massLocations[massCntr].Y * massLocations[massCntr].Y));

                // MXY
                inertiaTensor.M21 += _masses[massCntr].Mass * massLocations[massCntr].X * massLocations[massCntr].Y;

                // MXZ
                inertiaTensor.M31 += _masses[massCntr].Mass * massLocations[massCntr].X * massLocations[massCntr].Z;

                // MYZ
                inertiaTensor.M32 += _masses[massCntr].Mass * massLocations[massCntr].Y * massLocations[massCntr].Z;
            }

            // Finish up the non diagnals (it's actually the negative sum for them, and the transpose elements have
            // the same value)
            inertiaTensor.M21 *= -1;
            inertiaTensor.M12 = inertiaTensor.M21;

            inertiaTensor.M31 *= -1;
            inertiaTensor.M13 = inertiaTensor.M31;

            inertiaTensor.M32 *= -1;
            inertiaTensor.M23 = inertiaTensor.M32;

            #endregion

            // Store the values
            base.CenterOfMass.StoreNewValues(centerMass);
            base.InertialTensorBody = inertiaTensor;
            base.Mass = totalMass;		// I don't want to call this.Mass, because it has extra checks
        }

        #endregion

        #region Event Listeners

        private void PointMassChanged(object sender, EventArgs e)
        {
            ResetInertiaTensorAndCenterOfMass();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sum(m*r)/M
        /// </summary>
        private MyVector GetCenterOfMass(out double totalMass)
        {
            MyVector retVal = new MyVector(0, 0, 0);

            if (_masses.Count == 0)
            {
                totalMass = NEARZEROMASS;
                return retVal;
            }

            totalMass = 0;

            foreach (PointMass pointMass in _masses)
            {
                retVal.X += pointMass.X * pointMass.Mass;
                retVal.Y += pointMass.Y * pointMass.Mass;
                retVal.Z += pointMass.Z * pointMass.Mass;
                totalMass += pointMass.Mass;
            }

            retVal.X /= totalMass;
            retVal.Y /= totalMass;
            retVal.Z /= totalMass;

            // Exit Function
            return retVal;
        }

        private MyVector[] GetRelativeMassPositions(MyVector centerMass)
        {
            MyVector[] retVal = new MyVector[_masses.Count];

            for (int massCntr = 0; massCntr < _masses.Count; massCntr++)
            {
                retVal[massCntr] = _masses[massCntr].Position - centerMass;
            }

            return retVal;
        }

        #endregion
    }

    #region class: PointMass

    /// <summary>
    /// This class is meant to tie in with RigidBody.  Whenever any property gets changed, an event needs to be raised
    /// so that the rigid body can adjust itself (that's why I don't store the position as a vector, no change detection)
    /// </summary>
    public class PointMass
    {
        #region Events

        public event EventHandler PropertyChanged;

        #endregion
        #region Declaration Section

        private MyVector _position = null;
        private double _mass = 0;

        #endregion

        #region Constructor

        public PointMass(double x, double y, double z, double mass)
        {
            _position = new MyVector(x, y, z);
            _mass = mass;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// WARNING:  This is for viewing the data only.  If you set the values through the vector, the change will
        /// not be noticed (causing unpredictable results)
        /// </summary>
        public MyVector Position
        {
            get
            {
                return _position;
            }
        }

        /// <summary>
        /// If you are going to adjust more than one property at a time, call the more specific methods, since the rigid
        /// body readjusts itself whenever something in this class changes
        /// </summary>
        public double X
        {
            get
            {
                return _position.X;
            }
            set
            {
                _position.X = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// If you are going to adjust more than one property at a time, call the more specific methods, since the rigid
        /// body readjusts itself whenever something in this class changes
        /// </summary>
        public double Y
        {
            get
            {
                return _position.Y;
            }
            set
            {
                _position.Y = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// If you are going to adjust more than one property at a time, call the more specific methods, since the rigid
        /// body readjusts itself whenever something in this class changes
        /// </summary>
        public double Z
        {
            get
            {
                return _position.Z;
            }
            set
            {
                _position.Z = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// If you are going to adjust more than one property at a time, call the more specific methods, since the rigid
        /// body readjusts itself whenever something in this class changes
        /// </summary>
        public double Mass
        {
            get
            {
                return _mass;
            }
            set
            {
                _mass = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Public Methods

        public PointMass Clone()
        {
            return new PointMass(_position.X, _position.Y, _position.Z, _mass);
        }

        /// <summary>
        /// This method lets you set multiple properties, and the change event only fires once
        /// </summary>
        public void ChangePosition(double x, double y, double z)
        {
            _position.X = x;
            _position.Y = y;
            _position.Z = z;

            OnPropertyChanged();
        }

        /// <summary>
        /// This method lets you set multiple properties, and the change event only fires once
        /// </summary>
        public void ChangeEverything(double x, double y, double z, double mass)
        {
            _position.X = x;
            _position.Y = y;
            _position.Z = z;
            _mass = mass;

            OnPropertyChanged();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This raises the PropertyChanged event
        /// </summary>
        protected virtual void OnPropertyChanged()
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new EventArgs());
            }
        }

        #endregion
    }

    #endregion
}
