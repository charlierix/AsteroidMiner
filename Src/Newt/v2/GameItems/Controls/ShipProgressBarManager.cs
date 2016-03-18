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

        //TODO: Have an option of being given a bot, or an array of parts
        private Bot _bot = null;
        public Bot Bot
        {
            get
            {
                return _bot;
            }
            set
            {
                ClearProgressBars();
                _bot = value;
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

            if (_bot == null)
            {
                return;
            }

            // Energy
            if (_energy != null && _bot.Energy != null)
            {
                _energy.Maximum = _bot.Energy.QuantityMax;
                _energy.Value = _bot.Energy.QuantityCurrent;
            }
            else if (_energy == null && _bot.Energy != null)
            {
                _energy = CreateProgressBar(_bot.Energy.QuantityCurrent, _bot.Energy.QuantityMax, "energy", WorldColors.EnergyTank);
            }
            else if (_energy != null && _bot.Energy == null)
            {
                _panel.Children.Remove(_energy);
                _energy = null;
            }

            // Fuel
            if (_fuel != null && _bot.Fuel != null)
            {
                _fuel.Maximum = _bot.Fuel.QuantityMax;
                _fuel.Value = _bot.Fuel.QuantityCurrent;
            }
            else if (_fuel == null && _bot.Fuel != null)
            {
                _fuel = CreateProgressBar(_bot.Fuel.QuantityCurrent, _bot.Fuel.QuantityMax, "fuel", WorldColors.FuelTank);
            }
            else if (_fuel != null && _bot.Fuel == null)
            {
                _panel.Children.Remove(_fuel);
                _fuel = null;
            }

            // Plasma
            if (_plasma != null && _bot.Plasma != null)
            {
                _plasma.Maximum = _bot.Plasma.QuantityMax;
                _plasma.Value = _bot.Plasma.QuantityCurrent;
            }
            else if (_plasma == null && _bot.Plasma != null)
            {
                _plasma = CreateProgressBar(_bot.Plasma.QuantityCurrent, _bot.Plasma.QuantityMax, "plasma", WorldColors.PlasmaTank);
            }
            else if (_plasma != null && _bot.Plasma == null)
            {
                _panel.Children.Remove(_plasma);
                _plasma = null;
            }

            // Cargo
            Tuple<double, double> cargo = null;
            if (_bot.CargoBays != null)
            {
                cargo = _bot.CargoBays.CargoVolume;
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
            if (_ammo != null && _bot.Ammo != null)
            {
                _ammo.Maximum = _bot.Ammo.QuantityMax;
                _ammo.Value = _bot.Ammo.QuantityCurrent;
            }
            else if (_ammo == null && _bot.Ammo != null)
            {
                _ammo = CreateProgressBar(_bot.Ammo.QuantityCurrent, _bot.Ammo.QuantityMax, "ammo", WorldColors.AmmoBox);
            }
            else if (_ammo != null && _bot.Ammo == null)
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
