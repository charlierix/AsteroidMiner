using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Newt.v2.GameItems;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class Player
    {
        #region Events

        public event EventHandler<ShipChangedArgs> ShipChanged = null;

        public event EventHandler CreditsChanged = null;

        #endregion

        #region Public Properties

        private ShipPlayer _ship = null;
        public ShipPlayer Ship
        {
            get
            {
                return _ship;
            }
            set
            {
                // If no change, do nothing
                if (_ship == null && value == null)
                {
                    return;
                }
                else if (_ship != null && value != null && _ship.Token == value.Token)
                {
                    return;
                }

                ShipPlayer prev = _ship;
                _ship = value;

                OnShipChanged(prev, _ship);
            }
        }

        private decimal _credits = 0m;
        public decimal Credits
        {
            get
            {
                return _credits;
            }
            set
            {
                if (value < -1m)
                {
                    //TODO: May want to throw an exception
                    throw new ArgumentException("Credits can't be negative");
                }
                else if (value < 0m)
                {
                    // Probably just a rounding error - tough with decimal, but give the benefit of doubt
                    _credits = 0m;
                }
                else
                {
                    _credits = value;
                }

                OnCreditsChanged();
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void OnShipChanged(ShipPlayer previousShip, ShipPlayer newShip)
        {
            if (this.ShipChanged != null)
            {
                this.ShipChanged(this, new ShipChangedArgs(previousShip, newShip));
            }
        }

        protected virtual void OnCreditsChanged()
        {
            if (this.CreditsChanged != null)
            {
                this.CreditsChanged(this, new EventArgs());
            }
        }

        #endregion
    }

    #region Class: ShipChangedArgs

    public class ShipChangedArgs : EventArgs
    {
        public ShipChangedArgs(ShipPlayer previousShip, ShipPlayer newShip)
        {
            this.PreviousShip = previousShip;
            this.NewShip = newShip;
        }

        public readonly ShipPlayer PreviousShip;
        public readonly ShipPlayer NewShip;
    }

    #endregion

    #region Class: PlayerDNA

    /// <summary>
    /// This is what gets serialized to file
    /// </summary>
    public class PlayerDNA
    {
        public ShipDNA Ship { get; set; }

        public double Fuel { get; set; }
        public double Energy { get; set; }
        public double Plasma { get; set; }
        public double Ammo { get; set; }

        public decimal Credits { get; set; }
    }

    #endregion
}
