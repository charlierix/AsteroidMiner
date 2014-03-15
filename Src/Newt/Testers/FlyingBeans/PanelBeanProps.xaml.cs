using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.HelperClasses.Controls2D;

namespace Game.Newt.Testers.FlyingBeans
{
    public partial class PanelBeanProps : UserControl
    {
        #region Declaration Section

        private const string MSGBOXCAPTION = "PanelBeanProps";

        private List<SliderShowValues.PropSync> _propLinks = new List<SliderShowValues.PropSync>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public PanelBeanProps(FlyingBeanOptions options, ItemOptions itemOptions)
        {
            InitializeComponent();

            _options = options;
            _itemOptions = itemOptions;

            //PropertyInfo[] propsOptions = typeof(FlyingBeanOptions).GetProperties();
            PropertyInfo[] propsItems = typeof(ItemOptions).GetProperties();

            // Consumption
            _propLinks.Add(new SliderShowValues.PropSync(trkThrustForce, propsItems.Where(o => o.Name == "ThrusterStrengthRatio").First(), _itemOptions, 5, 100));
            _propLinks.Add(new SliderShowValues.PropSync(trkFuelDraw, propsItems.Where(o => o.Name == "FuelToThrustRatio").First(), _itemOptions, .001d, 5));
            _propLinks.Add(new SliderShowValues.PropSync(trkGravitySensorEnergyDraw, propsItems.Where(o => o.Name == "GravitySensorAmountToDraw").First(), _itemOptions, .1, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkSpinSensorEnergyDraw, propsItems.Where(o => o.Name == "SpinSensorAmountToDraw").First(), _itemOptions, .1, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkBrainEnergyDraw, propsItems.Where(o => o.Name == "BrainAmountToDraw").First(), _itemOptions, .1, 10));

            // Neural
            _propLinks.Add(new SliderShowValues.PropSync(trkGravSensorNeuronDensity, propsItems.Where(o => o.Name == "GravitySensorNeuronDensity").First(), _itemOptions, 4, 60));
            _propLinks.Add(new SliderShowValues.PropSync(trkBrainNeuronDensity, propsItems.Where(o => o.Name == "BrainNeuronDensity").First(), _itemOptions, 4, 60));
            _propLinks.Add(new SliderShowValues.PropSync(trkBrainChemicalDensity, propsItems.Where(o => o.Name == "BrainChemicalDensity").First(), _itemOptions, 0, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkBrainInternalLinks, propsItems.Where(o => o.Name == "BrainLinksPerNeuron_Internal").First(), _itemOptions, .5, 8));
            _propLinks.Add(new SliderShowValues.PropSync(trkBrainExternalLinkSensor, propsItems.Where(o => o.Name == "BrainLinksPerNeuron_External_FromSensor").First(), _itemOptions, .1, 5));
            _propLinks.Add(new SliderShowValues.PropSync(trkBrainExternalLinkBrain, propsItems.Where(o => o.Name == "BrainLinksPerNeuron_External_FromBrain").First(), _itemOptions, .1, 5));
            _propLinks.Add(new SliderShowValues.PropSync(trkThrusterExternalLinkSensor, propsItems.Where(o => o.Name == "ThrusterLinksPerNeuron_Sensor").First(), _itemOptions, .1, 5));
            _propLinks.Add(new SliderShowValues.PropSync(trkThrusterExternalLinkBrain, propsItems.Where(o => o.Name == "ThrusterLinksPerNeuron_Brain").First(), _itemOptions, .1, 5));

            // Density
            _propLinks.Add(new SliderShowValues.PropSync(trkBrainDensity, propsItems.Where(o => o.Name == "BrainDensity").First(), _itemOptions, 1, 5000));
            _propLinks.Add(new SliderShowValues.PropSync(trkGravSensorDensity, propsItems.Where(o => o.Name == "SensorDensity").First(), _itemOptions, 1, 5000));
            _propLinks.Add(new SliderShowValues.PropSync(trkEnergyTankDensity, propsItems.Where(o => o.Name == "EnergyTankDensity").First(), _itemOptions, 1, 5000));
            _propLinks.Add(new SliderShowValues.PropSync(trkFuelTankDensity, propsItems.Where(o => o.Name == "FuelTankWallDensity").First(), _itemOptions, 1, 5000));
            _propLinks.Add(new SliderShowValues.PropSync(trkFuelDensity, propsItems.Where(o => o.Name == "FuelDensity").First(), _itemOptions, 1, 5000));
            _propLinks.Add(new SliderShowValues.PropSync(trkThrusterDensity, propsItems.Where(o => o.Name == "ThrusterDensity").First(), _itemOptions, 1, 5000));

            // TODO: this one should have a log scale
            //trkMomentOfInertia.Normalize_SliderToValue += //convert a linear to nonlinear
            //trkMomentOfInertia.Normalize_ValueToSlider += //convert a nonlinear to linear
            _propLinks.Add(new SliderShowValues.PropSync(trkMomentOfInertia, propsItems.Where(o => o.Name == "MomentOfInertiaMultiplier").First(), _itemOptions, .01, 10));



            //TODO: When I start saving the options to file, also need to set the min/max from options (in case the user changed the range)
            //TODO: This is a lot of hard coding.  Store the linkage between slider and property in a list, and just have one event listener for all sliders

            //trkThrustForce.Value = _itemOptions.ThrusterStrengthRatio;
            //trkFuelDraw.Minimum = .000000001;
            //trkFuelDraw.Maximum = .000005;
            //trkFuelDraw.Value = _itemOptions.FuelToThrustRatio;

            //trkSensorEnergyDraw.Value = _itemOptions.GravitySensorAmountToDraw;
            //trkBrainEnergyDraw.Value = _itemOptions.BrainAmountToDraw;

            //trkLifespan.Value = _options.MaxAgeSeconds;
            //trkAngularVelocity.Value = _options.AngularVelocityDeath;
            //trkGroundCollisions.Value = _options.MaxGroundCollisions;

            //trkGravSensorNeuronDensity.Value = _itemOptions.GravitySensorNeuronDensity;
            //trkBrainNeuronDensity.Value = _itemOptions.BrainNeuronDensity;
            //trkBrainChemicalDensity.Value = _itemOptions.BrainChemicalDensity;
            //trkBrainInternalLinks.Value = _itemOptions.BrainLinksPerNeuron_Internal;
            //trkBrainExternalLinkSensor.Value = _itemOptions.BrainLinksPerNeuron_External_FromSensor;
            //trkBrainExternalLinkBrain.Value = _itemOptions.BrainLinksPerNeuron_External_FromBrain;
            //trkThrusterExternalLinkSensor.Value = _itemOptions.ThrusterLinksPerNeuron_Sensor;
            //trkThrusterExternalLinkBrain.Value = _itemOptions.ThrusterLinksPerNeuron_Brain;

            //trkBrainDensity.Value = _itemOptions.BrainDensity;
            //trkGravSensorDensity.Value = _itemOptions.SensorDensity;
            //trkEnergyTankDensity.Value = _itemOptions.EnergyTankDensity;
            //trkFuelTankDensity.Value = _itemOptions.FuelTankWallDensity;
            //trkFuelDensity.Value = _itemOptions.FuelDensity;
            //trkThrusterDensity.Value = _itemOptions.ThrusterDensity;
            //trkMomentOfInertia.Value = _itemOptions.MomentOfInertiaMultiplier;

            _isInitialized = true;
        }

        #endregion

        #region Public Properties

        private FlyingBeanOptions _options;
        public FlyingBeanOptions Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;

                foreach (var link in _propLinks)
                {
                    if (link.Item is FlyingBeanOptions)
                    {
                        link.Item = value;
                    }
                }
            }
        }

        private ItemOptions _itemOptions;
        public ItemOptions ItemOptions
        {
            get
            {
                return _itemOptions;
            }
            set
            {
                _itemOptions = value;

                foreach (var link in _propLinks)
                {
                    if (link.Item is ItemOptions)
                    {
                        link.Item = value;
                    }
                }
            }
        }

        #endregion

        #region Event Listeners

        //private void trkThrustForce_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.ThrusterStrengthRatio = trkThrustForce.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkFuelDraw_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        //_itemOptions.FuelToThrustRatio = ScaleFuelTo(trkFuelDraw.Value, _itemOptions.ThrusterStrengthRatio);
        //        _itemOptions.FuelToThrustRatio = trkFuelDraw.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkSensorEnergyDraw_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.GravitySensorAmountToDraw = trkSensorEnergyDraw.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkBrainEnergyDraw_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.BrainAmountToDraw = trkBrainEnergyDraw.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private void trkLifespan_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.MaxAgeSeconds = trkLifespan.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkAngularVelocity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.AngularVelocityDeath = trkAngularVelocity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkGroundCollisions_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.MaxGroundCollisions = Convert.ToInt32(trkGroundCollisions.Value);		// the slider was told to be an integer
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private void trkGravSensorNeuronDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.GravitySensorNeuronDensity = trkGravSensorNeuronDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkBrainNeuronDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.BrainNeuronDensity = trkBrainNeuronDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkBrainChemicalDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.BrainChemicalDensity = trkBrainChemicalDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkBrainInternalLinks_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.BrainLinksPerNeuron_Internal = trkBrainInternalLinks.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkBrainExternalLinkSensor_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.BrainLinksPerNeuron_External_FromSensor = trkBrainExternalLinkSensor.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkBrainExternalLinkBrain_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.BrainLinksPerNeuron_External_FromBrain = trkBrainExternalLinkBrain.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkThrusterExternalLinkSensor_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.ThrusterLinksPerNeuron_Sensor = trkThrusterExternalLinkSensor.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkThrusterExternalLinkBrain_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.ThrusterLinksPerNeuron_Brain = trkThrusterExternalLinkBrain.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private void trkBrainDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.BrainDensity = trkBrainDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkGravSensorDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.SensorDensity = trkGravSensorDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkEnergyTankDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.EnergyTankDensity = trkEnergyTankDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkFuelTankDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.FuelTankWallDensity = trkFuelTankDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkFuelDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.FuelDensity = trkFuelDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkThrusterDensity_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.ThrusterDensity = trkThrusterDensity.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkMomentOfInertia_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _itemOptions.MomentOfInertiaMultiplier = trkMomentOfInertia.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        #endregion
    }
}
