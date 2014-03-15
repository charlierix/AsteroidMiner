using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Game.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.Newt2Tester
{
    public partial class AddSimple : UserControl
    {
        #region Events

        public event EventHandler<AddBodyArgs> AddBody = null;

        #endregion

        #region Declaration Section

        private const double MINRATIO = .25d;
        private const double MAXRATIO = 2d;
        private const double MINMASS = .9d;
        private const double MAXMASS = 5d;

        private Random _rand = new Random();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public AddSimple()
        {
            InitializeComponent();

            _isInitialized = true;

            // Make the form look right
            Radio_Checked(this, new RoutedEventArgs());
        }

        #endregion

        #region Event Listeners

        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                bool isAllThree = false;

                // Figure out what to present
                CollisionShapeType shape = GetSelectedShape();
                switch (shape)
                {
                    case CollisionShapeType.Box:
                    case CollisionShapeType.Sphere:
                        isAllThree = true;
                        break;

                    case CollisionShapeType.Cone:
                    case CollisionShapeType.Capsule:
                        lblRatioHint.Text = "height must be greater or equal to diameter";
                        lblRatioHint.Visibility = Visibility.Visible;
                        break;

                    case CollisionShapeType.Cylinder:
                    case CollisionShapeType.ChamferCylinder:
                        lblRatioHint.Visibility = Visibility.Collapsed;
                        break;

                    default:
                        throw new ApplicationException("Unknown CollisionShapeType: " + shape.ToString());
                }

                // Change labels/visibility
                if (isAllThree)
                {
                    #region X Y Z

                    lblX.Content = "X";
                    lblY.Content = "Y";
                    lblZ.Content = "Z";

                    lblZ.Visibility = Visibility.Visible;
                    trkZ.Visibility = Visibility.Visible;

                    lblRatioHint.Visibility = Visibility.Collapsed;

                    #endregion
                }
                else
                {
                    #region Diameter Height

                    // Going with diameter instead of radius because some of the constraints talk about diameter, so it's easier to see the ratio as diameter
                    lblX.Content = "Diameter";
                    lblY.Content = "Height";

                    lblZ.Visibility = Visibility.Collapsed;
                    trkZ.Visibility = Visibility.Collapsed;

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Hull Shape Radio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkRandomRatios_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            lblX.IsEnabled = !chkRandomRatios.IsChecked.Value;
            lblY.IsEnabled = !chkRandomRatios.IsChecked.Value;
            lblZ.IsEnabled = !chkRandomRatios.IsChecked.Value;

            trkX.IsEnabled = !chkRandomRatios.IsChecked.Value;
            trkY.IsEnabled = !chkRandomRatios.IsChecked.Value;
            trkZ.IsEnabled = !chkRandomRatios.IsChecked.Value;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.AddBody == null)
                {
                    MessageBox.Show("There are no event listeners for this button", "Clear Clicked", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddBodyArgs args = null;

                CollisionShapeType shape = GetSelectedShape();
                switch (shape)
                {
                    case CollisionShapeType.Box:
                    case CollisionShapeType.Sphere:
                        #region x y z

                        double x, y, z, mass1;
                        GetRatiosMass(out x, out y, out z, out mass1, shape);

                        args = new AddBodyArgs(shape, new Vector3D(x, y, z), mass1);

                        #endregion
                        break;

                    case CollisionShapeType.Capsule:
                    case CollisionShapeType.ChamferCylinder:
                    case CollisionShapeType.Cone:
                    case CollisionShapeType.Cylinder:
                        #region radius height

                        double radius, height, mass2;
                        GetRatiosMass(out radius, out height, out mass2, shape);

                        args = new AddBodyArgs(shape, radius, height, mass2);

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown CollisionShapeType: " + shape.ToString());
                }

                // Raise the event
                this.AddBody(this, args);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Add Clicked", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private CollisionShapeType GetSelectedShape()
        {
            if (radBox.IsChecked.Value)
            {
                return CollisionShapeType.Box;
            }
            else if (radSphere.IsChecked.Value)
            {
                return CollisionShapeType.Sphere;
            }
            else if (radCone.IsChecked.Value)
            {
                return CollisionShapeType.Cone;
            }
            else if (radCapsule.IsChecked.Value)
            {
                return CollisionShapeType.Capsule;
            }
            else if (radCylinder.IsChecked.Value)
            {
                return CollisionShapeType.Cylinder;
            }
            else if (radChamferCylinder.IsChecked.Value)
            {
                return CollisionShapeType.ChamferCylinder;
            }
            else
            {
                throw new ApplicationException("Unknown shape selection");
            }
        }

        // these were copied from the 1.53 collision shapes tester
        private void GetRatiosMass(out double x, out double y, out double z, out double mass, CollisionShapeType shape)
        {
            // Ratios
            if (chkRandomRatios.IsChecked.Value)
            {
                x = UtilityHelper.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, _rand.NextDouble());		// reused as radius
                y = UtilityHelper.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, _rand.NextDouble());		// reused as height
                z = UtilityHelper.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, _rand.NextDouble());
            }
            else
            {
                x = UtilityHelper.GetScaledValue(MINRATIO, MAXRATIO, trkX.Minimum, trkX.Maximum, trkX.Value);
                y = UtilityHelper.GetScaledValue(MINRATIO, MAXRATIO, trkY.Minimum, trkY.Maximum, trkY.Value);
                z = UtilityHelper.GetScaledValue(MINRATIO, MAXRATIO, trkZ.Minimum, trkZ.Maximum, trkZ.Value);
            }

            switch (shape)
            {
                case CollisionShapeType.Box:
                    x *= 2d;		// sphere treats x as a radius.  box thinks of it as width
                    y *= 2d;
                    z *= 2d;
                    mass = x * y * z;
                    break;

                case CollisionShapeType.Sphere:
                    mass = (4d / 3d) * Math.PI * x * y * z;
                    break;

                default:
                    throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
            }

            // If I try to be realistic, then it's boring, so I'll scale the result.  (density shrinks a bit as things get larger)
            mass = UtilityHelper.GetScaledValue(MINMASS, MAXMASS, Math.Pow(MINRATIO, 3), Math.Pow(MAXRATIO, 3), mass);
        }
        private void GetRatiosMass(out double radius, out double height, out double mass, CollisionShapeType shape)
        {
            #region Ratios

            if (chkRandomRatios.IsChecked.Value)
            {
                height = UtilityHelper.GetScaledValue(MINRATIO, MAXRATIO, 0d, 1d, _rand.NextDouble());

                switch (shape)
                {
                    case CollisionShapeType.Cone:
                    case CollisionShapeType.Capsule:
                        // height must be greater or equal to diameter
                        radius = UtilityHelper.GetScaledValue(MINRATIO * 2d, height, 0d, 1d, _rand.NextDouble());
                        break;

                    default:
                        radius = UtilityHelper.GetScaledValue(MINRATIO * 2d, MAXRATIO, 0d, 1d, _rand.NextDouble());
                        break;
                }
            }
            else
            {
                //NOTE:  I'm not going to error out if they have invalid values - this is a tester, and I want to test what happens
                radius = UtilityHelper.GetScaledValue(MINRATIO * 2d, MAXRATIO * 2d, trkX.Minimum, trkX.Maximum, trkX.Value);
                radius /= 2d;		// the slider is diameter
                height = UtilityHelper.GetScaledValue(MINRATIO * 2d, MAXRATIO * 2d, trkY.Minimum, trkY.Maximum, trkY.Value);
            }

            #endregion

            switch (shape)
            {
                case CollisionShapeType.Capsule:
                    // This looks like a pill.  I'm guessing since the height is capped to the diameter that the rounded parts cut into the height
                    // I'm also guessing that the rounded parts are spherical
                    double cylinderHeight = height - (2d * radius);
                    mass = Math.PI * radius * radius * cylinderHeight;		// cylinder portion
                    mass += (4d / 3d) * Math.PI * radius * radius * radius;		// end caps (adds up to a sphere)
                    break;

                case CollisionShapeType.Cylinder:
                    mass = Math.PI * radius * radius * height;
                    break;

                case CollisionShapeType.Cone:
                    mass = (1d / 3d) * Math.PI * radius * radius * height;
                    break;

                case CollisionShapeType.ChamferCylinder:
                    mass = Math.PI * radius * radius * height;		// I can't find any examples of what this looks like when the height is large.  It looks like it turns into a capsule if the height exceeds diameter?
                    break;

                default:
                    throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
            }

            // If I try to be realistic, then it's boring, so I'll scale the result.  (density shrinks a bit as things get larger)
            mass = UtilityHelper.GetScaledValue(MINMASS, MAXMASS, Math.Pow(MINRATIO, 3), Math.Pow(MAXRATIO, 3), mass);
        }

        #endregion
    }

    #region Class: AddBodyArgs

    public class AddBodyArgs : EventArgs
    {
        #region Constructor

        public AddBodyArgs(CollisionShapeType collisionShape, Vector3D size, double mass)
        {
            this.CollisionShape = collisionShape;
            _size = size;
            this.Mass = mass;
        }
        public AddBodyArgs(CollisionShapeType collisionShape, double radius, double height, double mass)
        {
            this.CollisionShape = collisionShape;
            _radius = radius;
            _height = height;
            this.Mass = mass;
        }

        #endregion

        #region Public Properties

        public CollisionShapeType CollisionShape
        {
            get;
            private set;
        }

        private Vector3D? _size = null;
        public Vector3D Size
        {
            get
            {
                if (_size == null)
                {
                    throw new InvalidOperationException("This property can't be used with the current shape");
                }

                return _size.Value;
            }
        }

        private double? _radius = null;
        public double Radius
        {
            get
            {
                if (_radius == null)
                {
                    throw new InvalidOperationException("This property can't be used with the current shape");
                }

                return _radius.Value;
            }
        }
        private double? _height = null;
        public double Height
        {
            get
            {
                if (_height == null)
                {
                    throw new InvalidOperationException("This property can't be used with the current shape");
                }

                return _height.Value;
            }
        }

        public double Mass
        {
            get;
            private set;
        }

        #endregion
    }

    #endregion
}
