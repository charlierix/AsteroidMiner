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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Game.Newt.Testers.Newt2Tester
{
    public partial class AddJoinedBodies : UserControl
    {
        #region Events

        public event EventHandler<AddJoinedBodiesArgs> AddJoint = null;

        #endregion

        #region Declaration Section

        private const string BODY_BOX_FACE = "Box: Face";
        private const string BODY_BOX_EDGE = "Box: Edge";
        private const string BODY_BOX_CORNER = "Box: Corner";
        private const string BODY_SPHERE = "Sphere";
        private const string BODY_CYLINDER_CAP = "Cylinder: Cap";
        private const string BODY_CYLINDER_EDGE = "Cylinder: Edge";
        private const string BODY_CONE_BASE = "Cone: Base";
        private const string BODY_CONE_TIP = "Cone: Tip";

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public AddJoinedBodies()
        {
            InitializeComponent();

            InitializeBodyTypeComboBox(cboBody1);
            InitializeBodyTypeComboBox(cboBody2);

            _isInitialized = true;
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

                // Combobox visibility
                AddJointType jointType = GetJointType();
                switch (jointType)
                {
                    case AddJointType.BallAndSocket:
                    case AddJointType.Hinge:
                    case AddJointType.Slider:
                    case AddJointType.Corkscrew:
                    case AddJointType.UniversalJoint:
                    case AddJointType.Multi_BallAndChain:
                        lblBody1.Content = "Body 1 Type";

                        lblBody1.Visibility = Visibility.Visible;
                        cboBody1.Visibility = Visibility.Visible;

                        lblBody2.Visibility = Visibility.Visible;
                        cboBody2.Visibility = Visibility.Visible;
                        break;

                    case AddJointType.UpVector:
                    case AddJointType.Multi_Tetrahedron:
                        lblBody1.Content = "Body Type";

                        lblBody1.Visibility = Visibility.Visible;
                        cboBody1.Visibility = Visibility.Visible;

                        lblBody2.Visibility = Visibility.Collapsed;
                        cboBody2.Visibility = Visibility.Collapsed;
                        break;

                    default:
                        throw new ApplicationException("Unknown AddJointType: " + jointType.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Hull Shape Radio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                AddJoinedBodiesArgs args = new AddJoinedBodiesArgs(GetJointType(), GetBodyType(cboBody1.SelectedValue.ToString()), GetBodyType(cboBody2.SelectedValue.ToString()), trkDistance.Value);

                if (this.AddJoint != null)
                {
                    this.AddJoint(this, args);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Add Body/Joint", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private AddJointType GetJointType()
        {
            if (radBallAndSocket.IsChecked.Value)
            {
                return AddJointType.BallAndSocket;
            }
            else if (radHinge.IsChecked.Value)
            {
                return AddJointType.Hinge;
            }
            else if (radSlider.IsChecked.Value)
            {
                return AddJointType.Slider;
            }
            else if (radCorkscrew.IsChecked.Value)
            {
                return AddJointType.Corkscrew;
            }
            else if (radUJoint.IsChecked.Value)
            {
                return AddJointType.UniversalJoint;
            }
            else if (radUpVector.IsChecked.Value)
            {
                return AddJointType.UpVector;
            }
            else if (radBallAndChain.IsChecked.Value)
            {
                return AddJointType.Multi_BallAndChain;
            }
            else if (radTetrahedron.IsChecked.Value)
            {
                return AddJointType.Multi_Tetrahedron;
            }
            else
            {
                throw new ApplicationException("Unknown joint type");
            }
        }
        private static AddJointBodyType GetBodyType(string bodyType)
        {
            switch (bodyType)
            {
                case BODY_BOX_FACE:
                    return AddJointBodyType.Box_Face;

                case BODY_BOX_EDGE:
                    return AddJointBodyType.Box_Edge;

                case BODY_BOX_CORNER:
                    return AddJointBodyType.Box_Corner;

                case BODY_SPHERE:
                    return AddJointBodyType.Sphere;

                case BODY_CYLINDER_CAP:
                    return AddJointBodyType.Cylinder_Cap;

                case BODY_CYLINDER_EDGE:
                    return AddJointBodyType.Cylinder_Edge;

                case BODY_CONE_BASE:
                    return AddJointBodyType.Cone_Base;

                case BODY_CONE_TIP:
                    return AddJointBodyType.Cone_Tip;

                default:
                    throw new ApplicationException("Unknown body type: " + bodyType);
            }
        }
        private static void InitializeBodyTypeComboBox(ComboBox combo)
        {
            combo.Items.Add(BODY_BOX_FACE);
            combo.Items.Add(BODY_BOX_EDGE);
            combo.Items.Add(BODY_BOX_CORNER);
            combo.Items.Add(BODY_SPHERE);
            combo.Items.Add(BODY_CYLINDER_CAP);
            combo.Items.Add(BODY_CYLINDER_EDGE);
            combo.Items.Add(BODY_CONE_BASE);
            combo.Items.Add(BODY_CONE_TIP);

            combo.SelectedValue = BODY_BOX_FACE;
        }

        #endregion
    }

    #region enum: AddJointType

    public enum AddJointType
    {
        BallAndSocket,
        Hinge,
        Slider,
        Corkscrew,
        UniversalJoint,
        UpVector,
        Multi_BallAndChain,
        Multi_Tetrahedron
    }

    #endregion
    #region enum: AddJointBodyType

    public enum AddJointBodyType
    {
        Box_Face,
        Box_Edge,
        Box_Corner,
        Sphere,
        Cylinder_Cap,
        Cylinder_Edge,
        Cone_Base,
        Cone_Tip
    }

    #endregion

    #region class: AddJoinedBodiesArgs

    public class AddJoinedBodiesArgs : EventArgs
    {
        public AddJoinedBodiesArgs(AddJointType jointType, AddJointBodyType body1Type, AddJointBodyType body2Type, double separationDistance)
        {
            this.JointType = jointType;
            this.Body1Type = body1Type;
            this.Body2Type = body2Type;
            this.SeparationDistance = separationDistance;
        }

        public AddJointType JointType
        {
            get;
            private set;
        }
        public AddJointBodyType Body1Type
        {
            get;
            private set;
        }
        public AddJointBodyType Body2Type
        {
            get;
            private set;
        }
        public double SeparationDistance
        {
            get;
            private set;
        }
    }

    #endregion
}
