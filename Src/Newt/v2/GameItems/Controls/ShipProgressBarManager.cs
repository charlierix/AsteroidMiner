using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Game.Newt.v2.GameItems.ShipParts;

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
            UpdateStandardContainer(ref _energy, _bot.Energy, "energy", WorldColors.EnergyTank_Color);

            // Fuel
            UpdateStandardContainer(ref _fuel, _bot.Fuel, "fuel", WorldColors.FuelTank_Color);

            // Plasma
            UpdateStandardContainer(ref _plasma, _bot.Plasma, "plasma", WorldColors.PlasmaTank_Color);

            // Cargo
            UpdateCargoContainer(ref _cargo, _bot.CargoBays, "cargo", WorldColors.CargoBay_Color);

            // Ammo
            //TODO: Break this down by caliber
            UpdateStandardContainer(ref _ammo, _bot.Ammo, "ammo", WorldColors.AmmoBox_Color);
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

        protected ProgressBarGame CreateProgressBar(double quantityCurrent, double quantityMax, double damagePercent, string name, Color color)
        {
            ProgressBarGame retVal = new ProgressBarGame();
            retVal.RightLabelVisibility = Visibility.Visible;
            retVal.RightLabelText = name;
            retVal.Foreground = this.Foreground;
            retVal.ProgressColor = color;

            retVal.Minimum = 0;
            retVal.Maximum = quantityMax;
            retVal.Value = quantityCurrent;
            retVal.DamagedPercent = damagePercent;

            _panel.Children.Add(retVal);

            return retVal;
        }

        #endregion

        #region Private Methods

        private void UpdateStandardContainer(ref ProgressBarGame progressBar, IContainer container, string name, Color color)
        {
            #region damage %

            double damagePercent = 0d;
            if (container != null)
            {
                if (container is ContainerGroup)
                {
                    damagePercent = ((ContainerGroup)container).DamagePercent;
                }
                else if (container is ITakesDamage && ((ITakesDamage)container).IsDestroyed)
                {
                    damagePercent = 1d;
                }
            }

            #endregion

            if (progressBar != null && container != null)
            {
                progressBar.Maximum = container.QuantityMax;
                progressBar.Value = container.QuantityCurrent;
                progressBar.DamagedPercent = damagePercent;
            }
            else if (progressBar == null && container != null)
            {
                progressBar = CreateProgressBar(container.QuantityCurrent, container.QuantityMax, damagePercent, name, color);
            }
            else if (progressBar != null && container == null)
            {
                _panel.Children.Remove(progressBar);
                progressBar = null;
            }
        }
        private void UpdateCargoContainer(ref ProgressBarGame progressBar, CargoBayGroup cargoBays, string name, Color color)
        {
            double damagePercent = 0d;
            Tuple<double, double> cargo = null;
            if (cargoBays != null)
            {
                cargo = cargoBays.CargoVolume;
                damagePercent = cargoBays.DamagePercent;
            }

            if (cargo != null && cargo.Item2 > 0d)
            {
                if (progressBar != null)
                {
                    progressBar.Maximum = cargo.Item2;
                    progressBar.Value = cargo.Item1;
                    progressBar.DamagedPercent = damagePercent;
                }
                else
                {
                    progressBar = CreateProgressBar(cargo.Item1, cargo.Item2, damagePercent, name, color);
                }
            }
            else if (progressBar != null)
            {
                _panel.Children.Remove(progressBar);
                progressBar = null;
            }

        }

        #endregion
    }
}
