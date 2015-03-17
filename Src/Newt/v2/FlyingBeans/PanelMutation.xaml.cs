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

using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF.Controls2D;

namespace Game.Newt.v2.FlyingBeans
{
    public partial class PanelMutation : UserControl
    {
        #region Declaration Section

        private const string MSGBOXCAPTION = "PanelMutation";

        private List<SliderShowValues.PropSync> _propLinks = new List<SliderShowValues.PropSync>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public PanelMutation(FlyingBeanOptions options)
        {
            InitializeComponent();

            _options = options;

            PropertyInfo[] propsOptions = typeof(FlyingBeanOptions).GetProperties();

            _propLinks.Add(new SliderShowValues.PropSync(trkNumBody, propsOptions.Where(o => o.Name == "BodyNumToMutate").First(), _options, 1, 10));
            _propLinks.Add(new SliderShowValues.PropSync(trkBodyVect, propsOptions.Where(o => o.Name == "BodySizeChangePercent").First(), _options, 0, 2d));
            _propLinks.Add(new SliderShowValues.PropSync(trkBodyPos, propsOptions.Where(o => o.Name == "BodyMovementAmount").First(), _options, 0, 1d));
            _propLinks.Add(new SliderShowValues.PropSync(trkBodyOrient, propsOptions.Where(o => o.Name == "BodyOrientationChangePercent").First(), _options, 0, .2));

            _propLinks.Add(new SliderShowValues.PropSync(trkPercentNeurons, propsOptions.Where(o => o.Name == "NeuronPercentToMutate").First(), _options, 0, .08));		//TODO: Multiply by 100
            _propLinks.Add(new SliderShowValues.PropSync(trkNeuronDistance, propsOptions.Where(o => o.Name == "NeuronMovementAmount").First(), _options, 0, .2));

            _propLinks.Add(new SliderShowValues.PropSync(trkPercentLinks, propsOptions.Where(o => o.Name == "LinkPercentToMutate").First(), _options, 0, .08));		//TODO: Multiply by 100
            _propLinks.Add(new SliderShowValues.PropSync(trkLinkWeight, propsOptions.Where(o => o.Name == "LinkWeightAmount").First(), _options, 0, .5));
            _propLinks.Add(new SliderShowValues.PropSync(trkLinkDistance, propsOptions.Where(o => o.Name == "LinkMovementAmount").First(), _options, 0, .5));
            _propLinks.Add(new SliderShowValues.PropSync(trkLinkFromContainerDist, propsOptions.Where(o => o.Name == "LinkContainerMovementAmount").First(), _options, 0, 1));
            _propLinks.Add(new SliderShowValues.PropSync(trkLinkFromContainerRotate, propsOptions.Where(o => o.Name == "LinkContainerRotateAmount").First(), _options, 0, .3));


            //TODO: Figure out how to do this in xaml
            trkNumBody.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkBodyVect.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkBodyPos.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkBodyOrient.ValueChanged += new EventHandler(Slider_ValueChanged);

            trkPercentNeurons.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkNeuronDistance.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkPercentLinks.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkLinkWeight.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkLinkDistance.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkLinkFromContainerDist.ValueChanged += new EventHandler(Slider_ValueChanged);
            trkLinkFromContainerRotate.ValueChanged += new EventHandler(Slider_ValueChanged);


            chkBody.IsChecked = options.MutateChangeBody;
            chkNeural.IsChecked = options.MutateChangeNeural;


            //trkPercentNeurons.Value = _options.NeuronPercentToMutate * 100d;
            //trkNeuronDistance.Value = _options.NeuronMovementAmount;
            //trkPercentLinks.Value = _options.LinkPercentToMutate * 100d;
            //trkLinkWeight.Value = _options.LinkWeightAmount;
            //trkLinkDistance.Value = _options.LinkMovementAmount;
            //trkLinkFromContainerDist.Value = _options.LinkContainerMovementAmount;
            //trkLinkFromContainerRotate.Value = _options.LinkContainerRotateAmount;

            _isInitialized = true;
        }

        #endregion

        #region Public Methods

        public static MutateUtility.ShipMutateArgs BuildMutateArgs(FlyingBeanOptions options)
        {
            #region Neural

            MutateUtility.NeuronMutateArgs neuralArgs = null;

            if (options.MutateChangeNeural)
            {
                MutateUtility.MuateArgs neuronMovement = new MutateUtility.MuateArgs(false, options.NeuronPercentToMutate, null, null,
                    new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.NeuronMovementAmount));		// neurons are all point3D (positions need to drift around freely.  percent doesn't make much sense)

                MutateUtility.MuateArgs linkMovement = new MutateUtility.MuateArgs(false, options.LinkPercentToMutate,
                    new Tuple<string, MutateUtility.MuateFactorArgs>[]
					{
						Tuple.Create("FromContainerPosition", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkContainerMovementAmount)),
						Tuple.Create("FromContainerOrientation", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.LinkContainerRotateAmount))
					},
                    new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
					{
						Tuple.Create(PropsByPercent.DataType.Double, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkWeightAmount)),		// all the doubles are weights, which need to be able to cross over zero (percents can't go + to -)
						Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkMovementAmount)),		// using a larger value for the links
					},
                    null);

                neuralArgs = new MutateUtility.NeuronMutateArgs(neuronMovement, null, linkMovement, null);
            }

            #endregion
            #region Body

            MutateUtility.MuateArgs bodyArgs = null;

            if (options.MutateChangeBody)
            {
                var mutate_Vector3D = Tuple.Create(PropsByPercent.DataType.Vector3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.BodySizeChangePercent));
                var mutate_Point3D = Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.BodyMovementAmount));		// positions need to drift around freely.  percent doesn't make much sense
                var mutate_Quaternion = Tuple.Create(PropsByPercent.DataType.Quaternion, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.BodyOrientationChangePercent));

                //NOTE: The mutate class has special logic for Scale and ThrusterDirections
                bodyArgs = new MutateUtility.MuateArgs(true, options.BodyNumToMutate,
                    null,
                    new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
						{
							mutate_Vector3D,
							mutate_Point3D,
							mutate_Quaternion,
						},
                    new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .01d));		// this is just other (currently there aren't any others - just being safe)
            }

            #endregion

            return new MutateUtility.ShipMutateArgs(null, bodyArgs, neuralArgs);
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
                    //if (link.Item is FlyingBeanOptions)
                    //{
                    link.Item = value;		// they're all the same type
                    //}
                }

                chkBody.IsChecked = _options.MutateChangeBody;
                chkNeural.IsChecked = _options.MutateChangeNeural;
            }
        }

        #endregion

        #region Event Listeners

        private void Slider_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                _options.MutateArgs = BuildMutateArgs(_options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                _options.MutateChangeBody = chkBody.IsChecked.Value;
                _options.MutateChangeNeural = chkNeural.IsChecked.Value;

                _options.MutateArgs = BuildMutateArgs(_options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private void trkPercentNeurons_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.NeuronPercentToMutate = trkPercentNeurons.Value * .01d;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkNeuronDistance_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.NeuronMovementAmount = trkNeuronDistance.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkPercentLinks_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.LinkPercentToMutate = trkPercentLinks.Value * .01d;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkLinkWeight_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.LinkWeightAmount = trkLinkWeight.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkLinkDistance_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.LinkMovementAmount = trkLinkDistance.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkLinkFromContainerDist_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.LinkContainerMovementAmount = trkLinkFromContainerDist.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}
        //private void trkLinkFromContainerRotate_ValueChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!_isInitialized)
        //        {
        //            return;
        //        }

        //        _options.LinkContainerRotateAmount = trkLinkFromContainerRotate.Value;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        #endregion

        #region Private Methods

        //private void RebuildArgs()
        //{
        //    _options.MutateArgs = BuildMutateArgs(_options);
        //}

        #endregion
    }
}
