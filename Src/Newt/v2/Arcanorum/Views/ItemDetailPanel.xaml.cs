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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;

namespace Game.Newt.v2.Arcanorum.Views
{
    public partial class ItemDetailPanel : UserControl
    {
        #region Events

        public event EventHandler ItemChanged = null;

        #endregion

        #region Declaration Section

        private Brush _textBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("B0FFFFFF"));

        #endregion

        #region Constructor

        public ItemDetailPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        private object _item = null;
        public object Item
        {
            get
            {
                return _item;
            }
            set
            {
                // Clear existing
                pnlContent.Children.Clear();
                _model = null;
                _modelRotate = Quaternion.Identity;

                // Store the new value
                _item = value;

                // Display the value
                if (_item != null)
                {
                    if (_item is Weapon)
                    {
                        WeaponSelected(((Weapon)_item));
                    }
                    else
                    {
                        throw new ApplicationException("Unknown item type: " + _item.GetType().ToString());
                    }
                }

                // Inform the world
                if (this.ItemChanged != null)
                {
                    this.ItemChanged(this, new EventArgs());
                }
            }
        }

        private Model3D _model = null;
        public Model3D Model
        {
            get
            {
                return _model;
            }
        }

        private Quaternion _modelRotate = Quaternion.Identity;
        /// <summary>
        /// This is how the model should be rotated so that it looks best in the backdrop panel
        /// </summary>
        public Quaternion ModelRotate
        {
            get
            {
                return _modelRotate;
            }
        }

        #endregion

        #region Private Methods

        private void WeaponSelected(Weapon weapon)
        {
            //TODO: When there are more than just weapon handles, have a major section, then describe each part in sub sections

            //TODO: Density, Mass, Durability, Cost, Damage, Required experience
            //TODO: A small graphic next to mass showing how difficult it would be for the player to yield

            //TODO: A small paragraph describing the weapon in an expander

            pnlContent.Children.Add(GetTextblock_MajorHeader("Weapon Handle"));
            pnlContent.Children.Add(GetTextblock_MinorHeader(weapon.DNA.Handle.HandleMaterial.ToString().Replace('_', ' ')));
            //pnlContent.Children.Add(GetTextblock_Standard(string.Format("Mass: {0}", GetRounded(weapon.m))));

            _model = UtilityCore.Clone(weapon.Model);
            _modelRotate = new Quaternion(new Vector3D(0, 0, 1), 90);
        }

        private TextBlock GetTextblock_MajorHeader(string text)
        {
            TextBlock retVal = new TextBlock()
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = _textBrush,
                Margin = new Thickness(0, 0, 0, 6),
                Text = text
            };

            return retVal;
        }
        private TextBlock GetTextblock_MinorHeader(string text)
        {
            TextBlock retVal = new TextBlock()
            {
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                Foreground = _textBrush,
                Margin = new Thickness(0, 0, 0, 4),
                Text = text
            };

            return retVal;
        }
        private TextBlock GetTextblock_Standard(string text)
        {
            TextBlock retVal = new TextBlock()
            {
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                Foreground = _textBrush,
                Margin = new Thickness(0, 0, 0, 2),
                Text = text
            };

            return retVal;
        }

        private string GetRounded(double value)
        {
            //TODO: Only show about 3 significant digits
            string retVal = Math.Round(value, 2).ToString();
            return retVal;
        }

        #endregion
    }
}
