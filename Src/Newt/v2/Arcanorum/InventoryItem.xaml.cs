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
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.Arcanorum
{
    public partial class InventoryItem : UserControl
    {
        #region Declaration Section

        private Visual3D _visual = null;

        #endregion

        #region Constructor

        public InventoryItem()
        {
            InitializeComponent();

            this.LightColor = _lightColor;
        }

        #endregion

        #region Public Properties

        private IMapObject _item = null;
        public IMapObject Item
        {
            get
            {
                return _item;
            }
        }

        private Color _lightColor = UtilityWPF.ColorFromHex("FFFFFF");
        /// <summary>
        /// This is the color of the 3D view's light
        /// </summary>
        public Color LightColor
        {
            get
            {
                return _lightColor;
            }
            set
            {
                _lightColor = value;

                Color ambient, front, back;
                GetLightColors(out ambient, out front, out back, _lightColor);

                ambientLight.Color = ambient;
                frontDirectional.Color = front;
                backDirectional.Color = back;
            }
        }

        private QuaternionRotation3D _rotateTransform = null;
        public QuaternionRotation3D RotateTransform
        {
            get
            {
                return _rotateTransform;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Makes this class show the item passed in
        /// </summary>
        /// <param name="sizePercent">
        /// If several items are shown next to each other, the camera of each viewport needs to be zoomed so that
        /// their relative sizes are the same, so this is this item's radius relative to the largest item's radius
        /// </param>
        public void SetItem(IMapObject item, double sizePercent)
        {
            // Remove previous item
            if (_item != null)
            {
                RemoveVisual();
            }

            // Store it
            _item = item;

            // Display new item
            if (_item != null)
            {
                CreateVisual(sizePercent);
            }
        }

        /// <summary>
        /// This is a helper method that sets the light's colors based on the color passed in
        /// </summary>
        public static void GetLightColors(out Color ambient, out Color front, out Color back, Color color)
        {
            ColorHSV hsv = color.ToHSV();

            //<AmbientLight x:Name="ambientLight" Color="#696969" />
            //<DirectionalLight x:Name="frontDirectional" Color="#FFFFFF" Direction="-1,-1,-1" />
            //<DirectionalLight x:Name="backDirectional" Color="#303030" Direction="1,1,1" />

            ambient = UtilityWPF.HSVtoRGB(color.A, hsv.H, hsv.S, hsv.V * .41d);
            front = color;
            back = UtilityWPF.HSVtoRGB(color.A, hsv.H, hsv.S, hsv.V * .19d);
        }

        #endregion

        #region Private Methods

        private void RemoveVisual()
        {
            if (_visual != null)
            {
                _viewport.Children.Remove(_visual);
            }

            _visual = null;
            _rotateTransform = null;
        }
        private void CreateVisual(double sizePercent)
        {
            const double ZOOMFULL = 2.7d;

            if (_item == null)
            {
                return;
            }

            Model3D model = UtilityCore.Clone(_item.Model);
            _rotateTransform = new QuaternionRotation3D(Quaternion.Identity);
            model.Transform = new RotateTransform3D(_rotateTransform);

            _visual = new ModelVisual3D() { Content = model };

            _camera.Position = (_camera.Position.ToVector().ToUnit() * (_item.Radius * ZOOMFULL / sizePercent)).ToPoint();
            _viewport.Children.Add(_visual);
        }

        #endregion
    }
}
