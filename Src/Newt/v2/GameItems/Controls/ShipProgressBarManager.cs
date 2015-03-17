using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Game.Newt.v2.GameItems.Controls
{
    /// <summary>
    /// This is a small helper class that will create/update progress bars for a ship
    /// </summary>
    /// <remarks>
    /// Give it a stackpanel, a ship, then call update on a regular basis
    /// </remarks>
    public class ShipProgressBarManager
    {
        #region Declaration Section

        protected readonly StackPanel _panel;

        private ProgressBarGame _energy = null;
        private ProgressBarGame _fuel = null;
        private ProgressBarGame _plasma = null;
        private ProgressBarGame _cargo = null;
        private ProgressBarGame _ammo = null;

        #endregion

        #region Constructor

        public ShipProgressBarManager(StackPanel panel)
        {
            // Do this so all the labels and bars will be aligned
            Grid.SetIsSharedSizeScope(panel, true);

            _panel = panel;
        }

        #endregion

        #region Public Properties

        //TODO: Have an option of being given a ship, or an array of parts
        private Ship _ship = null;
        public Ship Ship
        {
            get
            {
                return _ship;
            }
            set
            {
                ClearProgressBars();
                _ship = value;
            }
        }

        private Brush _foreground = Brushes.White;
        public Brush Foreground
        {
            get
            {
                return _foreground;
            }
            set
            {
                if (_foreground == value)
                {
                    return;
                }

                ClearProgressBars();
                _foreground = value;
            }
        }

        #endregion

        #region Public Methods

        public virtual void Update()
        {
            //NOTE: The ship's containers shouldn't switch to/from null during the lifetime of the ship, but this method is made to be robust

            if (_ship == null)
            {
                return;
            }

            // Energy
            if (_energy != null && _ship.Energy != null)
            {
                _energy.Maximum = _ship.Energy.QuantityMax;
                _energy.Value = _ship.Energy.QuantityCurrent;
            }
            else if (_energy == null && _ship.Energy != null)
            {
                _energy = CreateProgressBar(_ship.Energy.QuantityCurrent, _ship.Energy.QuantityMax, "energy", WorldColors.EnergyTank);
            }
            else if (_energy != null && _ship.Energy == null)
            {
                _panel.Children.Remove(_energy);
                _energy = null;
            }

            // Fuel
            if (_fuel != null && _ship.Fuel != null)
            {
                _fuel.Maximum = _ship.Fuel.QuantityMax;
                _fuel.Value = _ship.Fuel.QuantityCurrent;
            }
            else if (_fuel == null && _ship.Fuel != null)
            {
                _fuel = CreateProgressBar(_ship.Fuel.QuantityCurrent, _ship.Fuel.QuantityMax, "fuel", WorldColors.FuelTank);
            }
            else if (_fuel != null && _ship.Fuel == null)
            {
                _panel.Children.Remove(_fuel);
                _fuel = null;
            }

            // Plasma
            if (_plasma != null && _ship.Plasma != null)
            {
                _plasma.Maximum = _ship.Plasma.QuantityMax;
                _plasma.Value = _ship.Plasma.QuantityCurrent;
            }
            else if (_plasma == null && _ship.Plasma != null)
            {
                _plasma = CreateProgressBar(_ship.Plasma.QuantityCurrent, _ship.Plasma.QuantityMax, "plasma", WorldColors.PlasmaTank);
            }
            else if (_plasma != null && _ship.Plasma == null)
            {
                _panel.Children.Remove(_plasma);
                _plasma = null;
            }

            // Cargo
            Tuple<double, double> cargo = null;
            if (_ship.CargoBays != null)
            {
                cargo = _ship.CargoBays.CargoVolume;
            }

            if (cargo != null && cargo.Item2 > 0d)
            {
                if (_cargo != null)
                {
                    _cargo.Maximum = cargo.Item2;
                    _cargo.Value = cargo.Item1;
                }
                else
                {
                    _cargo = CreateProgressBar(cargo.Item1, cargo.Item2, "cargo", WorldColors.CargoBay);
                }
            }
            else if (_cargo != null)
            {
                _panel.Children.Remove(_cargo);
                _cargo = null;
            }

            // Ammo
            //TODO: Break this down by caliber
            if (_ammo != null && _ship.Ammo != null)
            {
                _ammo.Maximum = _ship.Ammo.QuantityMax;
                _ammo.Value = _ship.Ammo.QuantityCurrent;
            }
            else if (_ammo == null && _ship.Ammo != null)
            {
                _ammo = CreateProgressBar(_ship.Ammo.QuantityCurrent, _ship.Ammo.QuantityMax, "ammo", WorldColors.AmmoBox);
            }
            else if (_ammo != null && _ship.Ammo == null)
            {
                _panel.Children.Remove(_ammo);
                _ammo = null;
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void ClearProgressBars()
        {
            _energy = null;
            _fuel = null;
            _plasma = null;
            _cargo = null;
            _ammo = null;

            _panel.Children.Clear();
        }

        protected ProgressBarGame CreateProgressBar(double quantityCurrent, double quantityMax, string name, Color color)
        {
            ProgressBarGame retVal = new ProgressBarGame();
            retVal.RightLabelVisibility = Visibility.Visible;
            retVal.RightLabelText = name;
            retVal.Foreground = this.Foreground;
            retVal.ProgressColor = color;

            retVal.Minimum = 0;
            retVal.Maximum = quantityMax;
            retVal.Value = quantityCurrent;

            _panel.Children.Add(retVal);

            return retVal;
        }

        #endregion
    }
}
