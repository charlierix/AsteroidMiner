using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    #region interface: IContainer

    /// <summary>
    /// This defines a class that is designed to hang on to quantities of stuff
    /// </summary>
    /// <remarks>
    /// You would use a container for all kinds of purposes (Ammo Clip, Money, Fuel Tank).  Basically anything that needs to be 
    /// packaged and transported around.
    /// 
    /// I was batting around the idea of allowing flow restrictions.  Think of a senario with an energy tank, a fuel tank, and an 
    /// energy-matter converter sitting between them.  But I decided that the restriction is in the converter, not the tanks.  The 
    /// biggest problem with having flow restrictions at the container level is how to enforce it.  If the max flow is ten, and there 
    /// were 100 requests for 10, then the container just let out 1000 fluid.  Did this occur over a second, or an hour?  Now every 
    /// container needs to get updates on the game's time.  While this is a technically accurate model, it is over complex, because
    /// it is most likely the user of my contents that has the real bottleneck (a machine gun has a very predictable rate of fire, etc.)
    /// </remarks>
    public interface IContainer
    {
        //TODO: This would be nice to have in the future
        //public event ValueMilestone(enum whichMileStone?, bool isUpStroke, double currentValue);

        double QuantityCurrent { get; set; }
        double QuantityMax { get; set; }
        double QuantityMax_Usable { get; }      // if the container is destroyed, or partially destroyed, then this will be less than QuantityMax

        // These are for cases that need the value, and don't want to use two gets every time they need it calculated
        double QuantityMaxMinusCurrent { get; }
        double QuantityMaxMinusCurrent_Usable { get; }

        /// <summary>
        /// This can be set up so that during a remove statement, you can only remove exact multiples of RemoveMultiple.  The
        /// most obvious choice for RemoveMultiple is the integer value of 1
        /// TODO: May want to constrain adds as well (do that with a separate property)
        /// </summary>
        bool OnlyRemoveMultiples { get; set; }
        /// <summary>
        /// NOTE: Only removes are enforced, partial adds are allowed.  This is for cases like ammo that is being slowly regenerated.  In reality, you can't make
        /// half a bullet, but it makes it much easier to implement this way (the bullets are stored here as simple numerical values, but you can't fire one until a
        /// complete bullet has been made.
        /// 
        /// If you really want adds enforced, make an AdditionMultiple property
        /// </summary>
        double RemovalMultiple { get; set; }

        /// <summary>
        /// This will try to add to the current quantity.  It will return the amount left over.  The only time the amount left over will
        /// be greater than zero is if the capacity is exceeded
        /// </summary>
        double AddQuantity(double amount, bool exactAmountOnly);

        /// <summary>
        /// This function will transfer stuff from the container passed in into this container.  returns the amount that COULDN'T be transfered
        /// </summary>
        /// <remarks>
        /// There are a couple situations that could happen.  It could be too full to hold all that was requested.  The source container may not have had
        /// enough to fulfill the request.
        /// </remarks>
        double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly);

        /// <summary>
        /// This will suck everything it possibly can from the container passed in, and add that to this class.
        /// The return variable will hold the amount that COULDN'T be transfered
        /// </summary>
        double AddQuantity(IContainer pullFrom, bool exactAmountOnly);

        /// <summary>
        /// This will try to deplete from this container.  It will return the amount left over from the request
        /// </summary>
        double RemoveQuantity(double amount, bool exactAmountOnly);
    }

    #endregion

    #region class: Container

    public class Container : IContainer
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Public Properties

        private double _quantityCurrent = 0d;
        public double QuantityCurrent
        {
            get
            {
                lock (_lock)
                {
                    return _quantityCurrent;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value < 0d)
                    {
                        if (Math1D.IsNearZero(value))
                        {
                            value = 0d;
                        }
                        else
                        {
                            throw new ArgumentException("Quantity can't be negative: " + value.ToString());
                        }
                    }

                    _quantityCurrent = value;

                    // Cap it
                    double max = _quantityMax_Usable ?? _quantityMax;
                    if (_quantityCurrent > max)
                    {
                        _quantityCurrent = max;
                    }
                }
            }
        }

        private double _quantityMax = 1d;
        public double QuantityMax
        {
            get
            {
                lock (_lock)
                {
                    return _quantityMax;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value < 0d)
                    {
                        if (Math1D.IsNearZero(value))
                        {
                            value = 0d;
                        }
                        else
                        {
                            throw new ArgumentException("Max Quantity can't be negative: " + value.ToString());
                        }
                    }

                    _quantityMax = value;

                    // Cap values
                    if (_quantityMax_Usable != null && _quantityMax_Usable.Value > _quantityMax)
                    {
                        _quantityMax_Usable = _quantityMax;
                    }

                    double max = _quantityMax_Usable ?? _quantityMax;
                    if (_quantityCurrent > max)
                    {
                        _quantityCurrent = max;
                    }
                }
            }
        }

        /// <summary>
        /// The interface only requires a get.  Implementers may have different ways of coming to need a less than max (like being destroyed, or partially damaged).
        /// So this class has a nullable property that can be set or left alone
        /// </summary>
        public double QuantityMax_Usable
        {
            get
            {
                lock (_lock)
                {
                    return _quantityMax_Usable ?? _quantityMax;
                }
            }
        }

        private double? _quantityMax_Usable = null;
        public double? QuantityMax_Usable_Nullable
        {
            get
            {
                lock (_lock)
                {
                    return _quantityMax_Usable;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value != null && value.Value < 0d)
                    {
                        if (Math1D.IsNearZero(value.Value))
                        {
                            value = 0d;
                        }
                        else
                        {
                            throw new ArgumentException("Max Quantity (usable) can't be negative: " + value.Value.ToString());
                        }
                    }

                    _quantityMax_Usable = value;

                    // Cap values
                    if (_quantityMax_Usable != null)
                    {
                        if (_quantityCurrent > _quantityMax_Usable.Value)
                        {
                            _quantityCurrent = _quantityMax_Usable.Value;
                        }

                        if (_quantityMax < _quantityMax_Usable.Value)
                        {
                            _quantityMax = _quantityMax_Usable.Value;
                        }
                    }
                }
            }
        }

        public double QuantityMaxMinusCurrent
        {
            get
            {
                lock (_lock)
                {
                    return _quantityMax - _quantityCurrent;
                }
            }
        }
        public double QuantityMaxMinusCurrent_Usable
        {
            get
            {
                lock (_lock)
                {
                    double max = _quantityMax_Usable ?? _quantityMax;
                    return max - _quantityCurrent;
                }
            }
        }

        private bool _onlyRemoveMultiples = false;
        public bool OnlyRemoveMultiples
        {
            get
            {
                lock (_lock)
                {
                    return _onlyRemoveMultiples;
                }
            }
            set
            {
                lock (_lock)
                {
                    _onlyRemoveMultiples = value;
                }
            }
        }

        private double _removalMultiple = 1d;
        public double RemovalMultiple
        {
            get
            {
                lock (_lock)
                {
                    return _removalMultiple;
                }
            }
            set
            {
                lock (_lock)
                {
                    _removalMultiple = value;
                }
            }
        }

        #endregion

        #region Public Methods

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                double actualAmount = amount;

                //   Figure out what should actually be stored
                double max = _quantityMax_Usable ?? _quantityMax;
                if (_quantityCurrent + actualAmount > max)
                {
                    actualAmount = max - _quantityCurrent;
                }

                //if (exactAmountOnly && actualAmount != amount)
                if (exactAmountOnly && !Math1D.IsNearValue(actualAmount, amount))
                {
                    actualAmount = 0d;
                }

                //   Add the value
                _quantityCurrent += actualAmount;

                //   Exit function
                return amount - actualAmount;
            }
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                double actualAmount = amount;

                // See if I can handle that much
                double max = _quantityMax_Usable ?? _quantityMax;
                if (_quantityCurrent + actualAmount > max)
                {
                    actualAmount = max - _quantityCurrent;
                }

                if (exactAmountOnly && !Math1D.IsNearValue(actualAmount, amount))
                {
                    actualAmount = 0d;
                }

                if (actualAmount != 0d)
                {
                    // Now try to pull that much out of the source container
                    actualAmount -= pullFrom.RemoveQuantity(actualAmount, exactAmountOnly);

                    // Add the value
                    _quantityCurrent += actualAmount;
                }

                // Exit function
                return amount - actualAmount;
            }
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            // Run contraints on the requested amount to remove
            double actualAmount = GetRemoveAmount(this, amount, exactAmountOnly);

            lock (_lock)
            {
                // Remove the value
                _quantityCurrent -= actualAmount;
            }

            // Exit function
            return amount - actualAmount;
        }

        /// <summary>
        /// This is a helper method to know what RemoveQuantity will take (between RemovalMultiple and exactAmountOnly, it
        /// gets a little complex)
        /// </summary>
        public static double GetRemoveAmount(IContainer container, double amount, bool exactAmountOnly)
        {
            //NOTE: I couldn't think of a way to make this totally threadsafe without adding more to the IContainer interface that returns all the needed
            //variables in one shot.  But in reality, OnlyRemoveMultiples and RemovalMultiple should change very infrequently if ever

            double retVal = amount;

            double quantityCurrent = container.QuantityCurrent;

            // See if the outgoing flow needs to be restricted
            if (retVal > quantityCurrent)
            {
                retVal = quantityCurrent;
            }

            // See if it wants even multiples
            if (container.OnlyRemoveMultiples)
            {
                double removalMultiple = container.RemovalMultiple;

                if (!Math1D.IsDivisible(retVal, removalMultiple))
                {
                    // Remove as many multiples of the requested amount as possible
                    retVal = Math.Floor(retVal / removalMultiple) * removalMultiple;
                }
            }

            // Exact amount
            if (exactAmountOnly && !Math1D.IsNearValue(retVal, amount))
            {
                retVal = 0d;
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion

    //TODO: Make a version that doesn't know about ITakesDamage (it should be the destroyed container's job to populate QuantityMax_Usable)
    #region class: ContainerGroup

    //WARNING: When this group isn't the sole owner of the containers, then this isn't completely threadsafe - at least not atomic.  Some containers could
    //get more filled than others
    /// <summary>
    /// This represents a set of containers that share their contents evenly amongst themselves (instant transfer).  So from
    /// the outside, they can be treated like a single container.
    /// NOTE: This is also aware of ITakesDamage
    /// </summary>
    /// <remarks>
    /// A ship can have multiple energy tanks, and multiple fuel tanks.  But all the energy tanks will be treated like a single
    /// tank (same with all the fuel tanks).  The advantage of multple tanks vs a large single tank would be better mass
    /// distribution, and redundency.
    /// </remarks>
    public class ContainerGroup : IContainer
    {
        #region enum: ContainerOwnershipType

        public enum ContainerOwnershipType
        {
            /// <summary>
            /// Neither quanities or maxes can change outside the knowledge of this group class (this allows for optimizations)
            /// </summary>
            GroupIsSoleOwner,
            /// <summary>
            /// Quantities can change outside this group class (but not maxes).  Whenever an add or remove is performed by this
            /// class, all the containers will be equalized first.
            /// </summary>
            QuantitiesCanChange,
            /// <summary>
            /// Both quanities and maxes could change outside this class.  Whenever an add or remove is performed by this class,
            /// the ratios will be recalculated, and the quantities will be equalized first.
            /// </summary>
            QuantitiesMaxesCanChange
        }

        #endregion

        #region Declaration Section

        private readonly object _lock = new object();

        // ITakesDamage is the same object as container (could be null)
        private List<Tuple<IContainer, ITakesDamage>> _containers = new List<Tuple<IContainer, ITakesDamage>>();

        // IsDestoyed is cached instead of relying on the live properties.  This way the bool is controlled within a lock and is synced with _ratios and _current
        private List<bool> _destroyed = new List<bool>();

        /// <summary>
        /// This is the ratio of each tank's max quantity compared to the sum of all the tanks
        /// </summary>
        /// <remarks>
        /// Item1=Considers whether the container is destroyed
        /// Item2=Ignores whether the container is destroyed
        /// </remarks>
        private List<Tuple<double, double>> _ratios = new List<Tuple<double, double>>();

        // These are only used if ownership is full or partial (as an optimization)
        private double _current = 0d;
        private double _max = 0d;
        private double _max_IncludesDestroyed = 0;

        #endregion

        #region IContainer Members

        public double QuantityCurrent
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityCurrent();
                }
            }
            set
            {
                lock (_lock)
                {
                    SetQuantityCurrent(value);
                }
            }
        }
        public double QuantityMax
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityMax().Item2;      // ignore sub part destruction
                }
            }
            set
            {
                lock (_lock)
                {
                    SetQuantityMax(value);
                }
            }
        }
        public double QuantityMax_Usable
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityMax().Item1;      // include sub part destruction
                }
            }
        }

        public double QuantityMaxMinusCurrent
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityMax().Item2 - GetQuantityCurrent();
                }
            }
        }
        public double QuantityMaxMinusCurrent_Usable
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityMax().Item1 - GetQuantityCurrent();
                }
            }
        }

        //NOTE: This class will still pull fractions across all containers, but will enforce even amounts from the global remove method.  This isn't technically correct, but is much easier :)
        private bool _onlyRemoveMultiples = false;
        public bool OnlyRemoveMultiples
        {
            get
            {
                lock (_lock)
                {
                    return _onlyRemoveMultiples;
                }
            }
            set
            {
                lock (_lock)
                {
                    _onlyRemoveMultiples = value;
                }
            }
        }

        private double _removalMultiple = 1d;
        public double RemovalMultiple
        {
            get
            {
                lock (_lock)
                {
                    return _removalMultiple;
                }
            }
            set
            {
                lock (_lock)
                {
                    _removalMultiple = value;
                }
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                return AddQuantity_priv(amount, exactAmountOnly);
            }
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity_priv(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                return AddQuantity_priv(pullFrom, amount, exactAmountOnly);
            }
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                double current = GetQuantityCurrent();
                double max = GetQuantityMax().Item1;        // using the destroyed aware max

                double actualAmount = amount;

                // See if I need to restrict the outgoing flow
                if (actualAmount > current)
                {
                    actualAmount = current;
                }

                //NOTE: Only looking at the whole, not each individual container
                if (_onlyRemoveMultiples && !Math1D.IsDivisible(actualAmount, _removalMultiple))
                {
                    // Remove as many multiples of the requested amount as possible
                    actualAmount = Math.Floor(actualAmount / _removalMultiple) * _removalMultiple;
                }

                if (exactAmountOnly && !Math1D.IsNearValue(actualAmount, amount))
                {
                    actualAmount = 0d;
                }

                if (actualAmount != 0d)
                {
                    #region Remove it

                    // Ensure that the containers are equalized
                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            // Nothing to do
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            EqualizeContainers(false);
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            EqualizeContainers(true);
                            break;

                        default:
                            throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }

                    // Remove the value evenly
                    for (int cntr = 0; cntr < _containers.Count; cntr++)
                    {
                        _containers[cntr].Item1.QuantityCurrent -= actualAmount * _ratios[cntr].Item1;      // using the destroyed aware ratio
                    }

                    // Cache the new value (this is used if sole owner)
                    _current = current - actualAmount;

                    #endregion
                }

                // Exit function
                return amount - actualAmount;
            }
        }

        #endregion

        #region Public Properties

        private ContainerOwnershipType _ownership = ContainerOwnershipType.QuantitiesMaxesCanChange;		// defaulting to the safest but most expensive setting
        public ContainerOwnershipType Ownership
        {
            get
            {
                lock (_lock)
                {
                    return _ownership;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_ownership == value)
                    {
                        return;
                    }

                    _ownership = value;

                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            EqualizeContainers(true);		// this should be the last time this needs to be called (unless containers are added/removed)
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            RecalcRatios();		// no need to call equalize now.  It will be called whenever any action against this group is requested
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            // no need to call anything right now
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }
                }
            }
        }

        public double DamagePercent
        {
            get
            {
                lock (_lock)
                {
                    double retVal = 0d;

                    for (int cntr = 0; cntr < _containers.Count; cntr++)
                    {
                        if (_destroyed[cntr])
                        {
                            retVal += _ratios[cntr].Item2;
                        }
                    }

                    return retVal;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Once this is added, the quantity is equalized across all containers
        /// </summary>
        public void AddContainer(IContainer container)
        {
            lock (_lock)
            {
                if (_containers.Any(o => o.Item1 == container))
                {
                    throw new ArgumentException("The container was already added to this group");
                }

                ITakesDamage takesDamage = container as ITakesDamage;

                _containers.Add(Tuple.Create(container, takesDamage));
                _destroyed.Add(takesDamage == null ? false : takesDamage.IsDestroyed);
                _ratios.Add(Tuple.Create(0d, 0d));

                if (takesDamage != null)
                {
                    takesDamage.Resurrected += ContainerCast_ResurrectedDestroyed;
                    takesDamage.Destroyed += ContainerCast_ResurrectedDestroyed;
                }

                // Cache the new values
                SetCurrentMax();

                EqualizeContainers(true);		// this needs to be done after setting _current
            }
        }

        /// <summary>
        /// This will equalize the containers before removing it (if deplete is false)
        /// </summary>
        /// <remarks>
        /// If the container is destroyed/switched off, that would be a case for not depleting it first
        /// </remarks>
        public void RemoveContainer(IContainer container, bool depleteFirst)
        {
            lock (_lock)
            {
                int index = _containers.IndexOf(new Tuple<IContainer, ITakesDamage>(container, null), (i1, i2) => i1.Item1 == i2.Item1);

                if (index < 0)
                {
                    throw new ArgumentException("The container wasn't in the group");
                }

                if (!depleteFirst)
                {
                    // Since they are removing the container, and don't want it emptied, make sure that it has an even share of the quantity
                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            // The containers are already equalized
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            EqualizeContainers(false);
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            EqualizeContainers(true);
                            break;

                        default:
                            throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }
                }

                // Pull it out
                _containers.RemoveAt(index);
                _ratios.RemoveAt(index);
                _destroyed.RemoveAt(index);

                // Cache the new values (must be done before calling other instance methods, since they may pull from these cached values)
                SetCurrentMax();

                EqualizeContainers(true);		// probably only need to call RecalcRatios, but this feels safer

                if (depleteFirst)
                {
                    AddQuantity_priv(container, false);		// ignoring this.OnlyRemoveMultiples, since partial adds are supported.  So the container will be fully depleted into the remaining containers, but any external call to get quantity will still have that enforced
                }
            }
        }

        #endregion

        #region Event Listeners

        private void ContainerCast_ResurrectedDestroyed(object sender, EventArgs e)
        {
            lock (_lock)
            {
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    _destroyed[cntr] = _containers[cntr].Item2 == null ? false : _containers[cntr].Item2.IsDestroyed;
                }

                SetCurrentMax();

                EqualizeContainers(true);		// this needs to be done after setting _current
            }
        }

        #endregion

        #region Private Methods

        // ---------------- These are private implementations of the publics.  Made them private methods so they can be called from within a lock
        private double GetQuantityCurrent()
        {
            if (_ownership == ContainerOwnershipType.GroupIsSoleOwner)
            {
                return _current;
            }
            else
            {
                //return _containers.Sum(o => o.QuantityCurrent);		// not using linq because this property will be called a lot, and I want it as fast as possible

                double retVal = 0;
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    if (!_destroyed[cntr])
                    {
                        retVal += _containers[cntr].Item1.QuantityCurrent;
                    }
                }

                return retVal;
            }
        }
        private void SetQuantityCurrent(double value)
        {
            if (value < 0d)
            {
                throw new ArgumentException("Quantity can't be negative: " + value.ToString());
            }

            double maxQuantity = GetQuantityMax().Item1;        // only need the one that cares if it's destroyed

            if (value > maxQuantity)
            {
                #region too much, top them off

                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    if (!_destroyed[cntr])
                    {
                        _containers[cntr].Item1.QuantityCurrent = _containers[cntr].Item1.QuantityMax;
                    }
                }

                _current = maxQuantity;     // no need to look at ownership, just set it

                #endregion
            }
            else
            {
                #region there is room, equalize

                if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
                {
                    RecalcRatios();
                }

                // Distribute the values evenly (it doesn't matter what was there before)
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    if (!_destroyed[cntr])
                    {
                        _containers[cntr].Item1.QuantityCurrent = value * _ratios[cntr].Item1;
                    }
                }

                _current = value;

                #endregion
            }
        }

        private Tuple<double, double> GetQuantityMax()
        {
            if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
            {
                double max = 0;
                double maxIncludesDestroyed = 0;
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    maxIncludesDestroyed += _containers[cntr].Item1.QuantityMax;
                    if (!_destroyed[cntr])
                    {
                        max += _containers[cntr].Item1.QuantityMax;
                    }
                }

                return Tuple.Create(max, maxIncludesDestroyed);
            }
            else
            {
                return Tuple.Create(_max, _max_IncludesDestroyed);
            }
        }
        private void SetQuantityMax(double value)
        {
            if (value < 0d)
            {
                throw new ArgumentException("Max Quantity can't be negative: " + value.ToString());
            }

            if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
            {
                RecalcRatios();
            }

            // Distribute the values evenly (let each container handle what happens if there is now overflow)
            //NOTE: _ratios is based on each container's max relative to the sum of their maxes.  So the max will change, but the ratios will stay the same
            //NOTE: when setting max, this uses the ratio that ignores whether the contrainer is destroyed
            for (int cntr = 0; cntr < _containers.Count; cntr++)
            {
                _containers[cntr].Item1.QuantityMax = value * _ratios[cntr].Item2;
            }

            _max = value;

            // Cap it
            if (GetQuantityCurrent() > _max)
            {
                SetQuantityCurrent(_max);
            }
        }

        private double AddQuantity_priv(double amount, bool exactAmountOnly)
        {
            double current = GetQuantityCurrent();
            double max = GetQuantityMax().Item1;        // using the destroyed aware max

            double actualAmount = amount;

            //   Figure out what should actually be stored
            if (current + actualAmount > max)
            {
                actualAmount = max - current;
            }

            if (exactAmountOnly && !Math1D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                #region Add it

                // Ensure that the containers are equalized
                switch (_ownership)
                {
                    case ContainerOwnershipType.GroupIsSoleOwner:
                        // Nothing to do
                        break;

                    case ContainerOwnershipType.QuantitiesCanChange:
                        EqualizeContainers(false);
                        break;

                    case ContainerOwnershipType.QuantitiesMaxesCanChange:
                        EqualizeContainers(true);
                        break;

                    default:
                        throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                }

                // Add the value evenly
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    if (!_destroyed[cntr])
                    {
                        _containers[cntr].Item1.QuantityCurrent += actualAmount * _ratios[cntr].Item1;      // using the destroyed aware ratio
                    }
                }

                // Cache the new value (this is used if sole owner)
                _current = current + actualAmount;

                #endregion
            }

            //   Exit function
            return amount - actualAmount;
        }
        private double AddQuantity_priv(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity_priv(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        private double AddQuantity_priv(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            if (pullFrom is ITakesDamage && ((ITakesDamage)pullFrom).IsDestroyed)
            {
                return 0d;
            }

            double current = GetQuantityCurrent();
            double max = GetQuantityMax().Item1;        // using the destroyed aware max

            double actualAmount = amount;

            // See if I can handle that much
            if (current + actualAmount > max)
            {
                actualAmount = max - current;
            }

            if (exactAmountOnly && !Math1D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                // Now try to pull that much out of the source container
                actualAmount -= pullFrom.RemoveQuantity(actualAmount, exactAmountOnly);

                if (actualAmount != 0d)
                {
                    #region Add it

                    // Ensure that the containers are equalized
                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            // Nothing to do
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            EqualizeContainers(false);
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            EqualizeContainers(true);
                            break;

                        default:
                            throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }

                    // Add the value evenly
                    for (int cntr = 0; cntr < _containers.Count; cntr++)
                    {
                        if (!_destroyed[cntr])
                        {
                            _containers[cntr].Item1.QuantityCurrent += actualAmount * _ratios[cntr].Item1;        // using the destroyed aware ratio
                        }
                    }

                    // Cache the new value (this is used if sole owner)
                    _current = current + actualAmount;

                    #endregion
                }
            }

            // Exit function
            return amount - actualAmount;
        }
        // ------------------------------------------------

        private void SetCurrentMax()
        {
            _current = 0;
            _max = 0;
            _max_IncludesDestroyed = 0;

            for (int cntr = 0; cntr < _containers.Count; cntr++)
            {
                _max_IncludesDestroyed += _containers[cntr].Item1.QuantityMax;

                if (!_destroyed[cntr])
                {
                    _current += _containers[cntr].Item1.QuantityCurrent;
                    _max += _containers[cntr].Item1.QuantityMax;
                }
            }
        }

        /// <summary>
        /// This compares the size of each container to the sum of all the container's sizes, and stores that percent in _ratios
        /// </summary>
        private void RecalcRatios()
        {
            Tuple<double, double> max = GetQuantityMax();

            bool isZero1 = max.Item1.IsNearZero();
            bool isZero2 = max.Item2.IsNearZero();

            _ratios.Clear();

            for (int cntr = 0; cntr < _containers.Count; cntr++)
            {
                // 1: Skips destroyed
                double ratio1;
                if (isZero1 || _destroyed[cntr])
                {
                    ratio1 = 0d;
                }
                else
                {
                    ratio1 = _containers[cntr].Item1.QuantityMax / max.Item1;
                }

                // 2: Includes destroyed
                double ratio2;
                if (isZero2)
                {
                    ratio2 = 0d;
                }
                else
                {
                    ratio2 = _containers[cntr].Item1.QuantityMax / max.Item2;
                }

                _ratios.Add(Tuple.Create(ratio1, ratio2));
            }
        }

        /// <summary>
        /// This adds up all the quantity across the containers, and makes sure that each container holds the same percent of that quantity
        /// </summary>
        private void EqualizeContainers(bool recalcRatios)
        {
            if (recalcRatios)
            {
                RecalcRatios();
            }

            double current = GetQuantityCurrent();

            for (int cntr = 0; cntr < _containers.Count; cntr++)
            {
                //NOTE: ratio when destroyed is zero, so destroyed containers will get zero
                _containers[cntr].Item1.QuantityCurrent = current * _ratios[cntr].Item1;      // using the destroyed aware ratio
            }
        }

        #endregion
    }

    #endregion

    #region class: ContainerInfinite

    /// <summary>
    /// This is used when you don't care about finite resources.  It pretends it's a container by always reporting that it's full.  None of the methods
    /// or property sets actually do anything, they just silently return
    /// </summary>
    public class ContainerInfinite : IContainer
    {
        private readonly double _maxQuantity;

        public ContainerInfinite(double maxQuantity = 1000000)
        {
            _maxQuantity = maxQuantity;
        }

        public double QuantityCurrent
        {
            get
            {
                return _maxQuantity;
            }
            set
            {
                // do nothing
            }
        }
        public double QuantityMax
        {
            get
            {
                return _maxQuantity;
            }
            set
            {
                // do nothing
            }
        }

        public double QuantityMax_Usable => _maxQuantity;

        public double QuantityMaxMinusCurrent => 0;
        public double QuantityMaxMinusCurrent_Usable => 0;

        public bool OnlyRemoveMultiples
        {
            get
            {
                return false;
            }
            set
            {
                // do nothing
            }
        }
        public double RemovalMultiple
        {
            get
            {
                return 1;       // the value doesn't matter because OnlyRemoveMultiples returns false
            }
            set
            {
                // do nothing
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            return 0;
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            double pulled = pullFrom.RemoveQuantity(amount, exactAmountOnly);

            return amount - pulled;
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            return 0;
        }
    }

    #endregion
}
