using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace Game.Newt.Testers.ChaseForces
{
    public partial class ForceCollection : UserControl
    {
        #region Events

        public event EventHandler ValueChanged = null;

        #endregion

        #region Declaration Section

        private const string TITLE = "ForceCollection";

        private readonly bool _isLinear;

        #endregion

        #region Constructor

        public ForceCollection(bool isLinear)
        {
            InitializeComponent();

            _isLinear = isLinear;

            this.DataContext = this;

            // Adding initial values, because these are pretty much the minimum required
            if (_isLinear)
            {
                ForceEntry entry = ForceEntry.GetNewEntry_Linear(ChaseDirectionType.Direction, 80);        // 80 attract
                entry.ValueChanged += new EventHandler(Entry_ValueChanged);
                pnlForces.Children.Add(entry);

                entry = ForceEntry.GetNewEntry_Linear(ChaseDirectionType.Velocity_Orth, 8);        // 8 drag orth
                entry.ValueChanged += new EventHandler(Entry_ValueChanged);
                pnlForces.Children.Add(entry);

                entry = ForceEntry.GetNewEntry_Linear(ChaseDirectionType.Velocity_AlongIfVelocityAway, 25);        // 25 drag when away
                entry.ValueChanged += new EventHandler(Entry_ValueChanged);
                pnlForces.Children.Add(entry);
            }
            else
            {
                ForceEntry entry = ForceEntry.GetNewEntry_Orientation(ChaseDirectionType.Direction, .1, gradient: new[] { Tuple.Create(0d, 0d), Tuple.Create(10d, 1d) });        // toward .1, gradient {0,0} {10,1}
                entry.ValueChanged += new EventHandler(Entry_ValueChanged);
                pnlForces.Children.Add(entry);

                entry = ForceEntry.GetNewEntry_Orientation(ChaseDirectionType.Velocity_Any, .03);        // drag .03
                entry.ValueChanged += new EventHandler(Entry_ValueChanged);
                pnlForces.Children.Add(entry);
            }

            txtInsertIndex.Text = pnlForces.Children.Count.ToString();
        }

        #endregion

        #region Public Methods

        public MapObject_ChasePoint_Forces GetChaseObject_Linear(IMapObject item)
        {
            if (!_isLinear)
            {
                throw new InvalidOperationException("This method can only be called when the control represents linear");
            }

            MapObject_ChasePoint_Forces retVal = new MapObject_ChasePoint_Forces(item, false);

            //TODO: May want to expose these.  I think they're unnecessary, and the result of overdesign
            //retVal.MaxAcceleration = 
            //retVal.MaxForce = 

            List<ChasePoint_Force> forces = new List<ChasePoint_Force>();

            foreach (UIElement entry in pnlForces.Children)
            {
                ChasePoint_Force chaseObject = null;
                if (entry is ForceEntry)
                {
                    chaseObject = ((ForceEntry)entry).GetChaseObject_Linear();
                }
                else
                {
                    throw new ApplicationException("Unknown type of entry: " + entry.ToString());
                }

                //NOTE: Doing a null check, because if they uncheck enabled, it will come back null
                if (chaseObject != null)
                {
                    forces.Add(chaseObject);
                }
            }

            if (forces.Count > 0)
            {
                retVal.Forces = forces.ToArray();
                return retVal;
            }
            else
            {
                // Don't bother returning something that will fail on update
                return null;
            }
        }
        public MapObject_ChaseOrientation_Torques GetChaseObject_Orientation(IMapObject item)
        {
            if (_isLinear)
            {
                throw new InvalidOperationException("This method can only be called when the control represents orientation");
            }

            MapObject_ChaseOrientation_Torques retVal = new MapObject_ChaseOrientation_Torques(item);

            //TODO: May want to expose these.  I think they're unnecessary, and the result of overdesign
            //retVal.MaxAcceleration = 
            //retVal.MaxForce = 

            List<ChaseOrientation_Torque> torques = new List<ChaseOrientation_Torque>();

            foreach (UIElement entry in pnlForces.Children)
            {
                ChaseOrientation_Torque chaseObject = null;
                if (entry is ForceEntry)
                {
                    chaseObject = ((ForceEntry)entry).GetChaseObject_Orientation();
                }
                else
                {
                    throw new ApplicationException("Unknown type of entry: " + entry.ToString());
                }

                //NOTE: Doing a null check, because if they uncheck enabled, it will come back null
                if (chaseObject != null)
                {
                    torques.Add(chaseObject);
                }
            }

            if (torques.Count > 0)
            {
                retVal.Torques = torques.ToArray();
                return retVal;
            }
            else
            {
                // Don't bother returning something that will fail on update
                return null;
            }
        }

        #endregion
        #region Protected Methods

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Event Listeners

        private void Entry_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = 0;
                if (!int.TryParse(txtInsertIndex.Text, out index))
                {
                    MessageBox.Show("Couldn't parse insert index as an integer", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (index < 0 || index > pnlForces.Children.Count)
                {
                    MessageBox.Show("Insert index is out of range", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ForceEntry entry = new ForceEntry(_isLinear);
                entry.ValueChanged += new EventHandler(Entry_ValueChanged);
                pnlForces.Children.Insert(index, entry);

                txtInsertIndex.Text = pnlForces.Children.Count.ToString();

                OnValueChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = 0;
                if (!int.TryParse(txtDeleteIndex.Text, out index))
                {
                    MessageBox.Show("Couldn't parse delete index as an integer", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (index < 0 || index >= pnlForces.Children.Count)
                {
                    MessageBox.Show("Delete index is out of range", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                pnlForces.Children.RemoveAt(index);

                OnValueChanged();

                txtInsertIndex.Text = pnlForces.Children.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pnlForces.Children.Clear();

                OnValueChanged();

                txtInsertIndex.Text = pnlForces.Children.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
