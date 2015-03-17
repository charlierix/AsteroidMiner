using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Newt.v1.AsteroidMiner1.ShipAddons
{
    public class Container : IContainer
    {
        #region IContainer Members

        private double _quantityCurrent = 0d;
        public double QuantityCurrent
        {
            get
            {
                return _quantityCurrent;
            }
            set
            {
                if (value < 0d)
                {
                    throw new ArgumentException("Quantity can't be negative: " + value.ToString());
                }

                _quantityCurrent = value;

                // Cap it
                if (_quantityCurrent > _quantityMax)
                {
                    _quantityCurrent = _quantityMax;
                }
            }
        }

        private double _quantityMax = 1d;
        public double QuantityMax
        {
            get
            {
                return _quantityMax;
            }
            set
            {
                if (value < 0d)
                {
                    throw new ArgumentException("Max Quantity can't be negative: " + value.ToString());
                }

                _quantityMax = value;

                // Cap it
                if (_quantityCurrent > _quantityMax)
                {
                    _quantityCurrent = _quantityMax;
                }
            }
        }

        public double QuantityMaxMinusCurrent
        {
            get
            {
                return _quantityMax - _quantityCurrent;
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            double actualAmout = amount;

            //   Figure out what should actually be stored
            if (_quantityCurrent + actualAmout > _quantityMax)
            {
                actualAmout = _quantityMax - _quantityCurrent;
            }

            if (exactAmountOnly && actualAmout != amount)
            {
                actualAmout = 0d;
            }

            //   Add the value
            _quantityCurrent += actualAmout;

            //   Exit function
            return amount - actualAmout;
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            double actualAmount = amount;

            // See if I can handle that much
            if (_quantityCurrent + actualAmount > _quantityMax)
            {
                actualAmount = _quantityMax - _quantityCurrent;
            }

            if (exactAmountOnly && actualAmount != amount)
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

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            double actualAmout = amount;

            // See if I need to restrict the outgoing flow
            if (actualAmout > _quantityCurrent)
            {
                actualAmout = _quantityCurrent;
            }

            if (_onlyRemoveMultiples && actualAmout % _removalMultiple != 0)
            {
                actualAmout = 0d;
            }

            if (exactAmountOnly && actualAmout != amount)
            {
                actualAmout = 0d;
            }

            // Remove the value
            _quantityCurrent -= actualAmout;

            // Exit function
            return amount - actualAmout;
        }

        #endregion

        #region Public Properties

        //  This can be set up so that during a remove statement, you can only remove exact multiples of RemoveMultiple.  The
        //  most obvious choice for RemoveMultiple is the integer value of 1
        private bool _onlyRemoveMultiples = false;
        public bool OnlyRemoveMultiples
        {
            get
            {
                return _onlyRemoveMultiples;
            }
            set
            {
                _onlyRemoveMultiples = value;
            }
        }

        private double _removalMultiple = 1d;
        public double RemovalMultiple
        {
            get
            {
                return _removalMultiple;
            }
            set
            {
                _removalMultiple = value;
            }
        }

        #endregion
    }
}
