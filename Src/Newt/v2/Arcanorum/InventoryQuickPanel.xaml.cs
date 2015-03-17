using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;

namespace Game.Newt.v2.Arcanorum
{
    public partial class InventoryQuickPanel : UserControl
    {
        #region Class: SlotItem

        private class SlotItem
        {
            public Border Border = null;
            public OutlinedTextBlock Text = null;

            public Viewport3D Viewport = null;
            public PerspectiveCamera Camera = null;
            //public PointLight Light = null;

            public long? WeaponToken = null;

            public AnimateRotation WeaponRotateAnimation = null;

            public Visual3D WeaponVisual = null;
            public AmbientLight WeaponAmbientLight = null;
            public DirectionalLight WeaponFrontLight = null;
            public DirectionalLight WeaponBackLight = null;
        }

        #endregion

        #region Declaration Section

        private List<SlotItem> _slots = new List<SlotItem>();

        private Lazy<FontFamily> _font = new Lazy<FontFamily>(() => GetBestBackgroundFont());

        //private SolidColorBrush _emptyTextFill = new SolidColorBrush(UtilityWPF.ColorFromHex("#10000000"));
        //private SolidColorBrush _emptyTextStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("#50FFFFFF"));
        private SolidColorBrush _emptyTextFill = new SolidColorBrush(UtilityWPF.ColorFromHex("#08000000"));
        private SolidColorBrush _emptyTextStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("#28FFFFFF"));
        private SolidColorBrush _filledTextFill = new SolidColorBrush(UtilityWPF.ColorFromHex("#30000000"));
        private SolidColorBrush _filledTextStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("#88FFFFFF"));

        private DispatcherTimer _timer = null;
        private DateTime _lastTick = DateTime.Now;

        #endregion

        #region Constructor

        public InventoryQuickPanel()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;
            _timer.IsEnabled = true;
        }

        #endregion

        #region Public Properties

        private int _slotCount = 4;
        public int SlotCount
        {
            get
            {
                return _slotCount;
            }
            set
            {
                if (_slotCount == value)
                {
                    return;
                }

                _slotCount = value;

                RefreshSlots();
            }
        }

        private ObservableCollection<Weapon> _weapons = null;
        public ObservableCollection<Weapon> Weapons
        {
            get
            {
                return _weapons;
            }
            set
            {
                if (_weapons != null)
                {
                    _weapons.CollectionChanged -= Weapons_CollectionChanged;
                }

                _weapons = value;

                _weapons.CollectionChanged += Weapons_CollectionChanged;

                RefreshSlots();
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

                ColorHSV hsv = _lightColor.ToHSV();

                foreach (SlotItem item in _slots.Where(o => o.WeaponToken != null))
                {
                    Color ambient, front, back;
                    InventoryItem.GetLightColors(out ambient, out front, out back, _lightColor);

                    item.WeaponAmbientLight.Color = ambient;
                    item.WeaponFrontLight.Color = front;
                    item.WeaponBackLight.Color = back;
                }
            }
        }

        #endregion

        #region Public Methods

        public Weapon GetSlot(int index)
        {
            return null;
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            DateTime newTime = DateTime.Now;
            double elapsedTime = (newTime - _lastTick).TotalSeconds;
            _lastTick = newTime;

            foreach (SlotItem slot in _slots)
            {
                if (slot.WeaponRotateAnimation != null)
                {
                    slot.WeaponRotateAnimation.Tick(elapsedTime);
                }
            }
        }

        private void Weapons_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshSlots();
        }

        #endregion

        #region Private Methods

        private void RefreshSlots()
        {
            // Add/Remove slots
            while (_slots.Count < _slotCount)
            {
                AddSlot();
            }

            while (_slots.Count > _slotCount)
            {
                RemoveSlot();
            }

            // Rebuild visuals
            for (int cntr = 0; cntr < _slotCount; cntr++)
            {
                UpdateSlot(cntr);
            }

            // Place all the cameras at the same location so it's easier to tell relative weapon sizes
            SlotItem[] filledSlots = _slots.Where(o => o.WeaponToken != null).ToArray();

            if (filledSlots.Length > 1)     // if there's only one, then it's already zoomed properly
            {
                double maxLength = filledSlots.Max(o => o.Camera.Position.ToVector().Length);

                foreach (SlotItem slot in filledSlots)
                {
                    slot.Camera.Position = (slot.Camera.Position.ToVector().ToUnit() * maxLength).ToPoint();
                }
            }
        }

        private void AddSlot()
        {
            if (_slots.Count != pnlSlots.Children.Count)
            {
                throw new ApplicationException(string.Format("The list and panel are out of sync.  list={0}, panel={1}", _slots.Count.ToString(), pnlSlots.Children.Count.ToString()));
            }

            SlotItem slot = new SlotItem();

            // Border
            slot.Border = new Border()
            {
                Width = 120,
                Height = 120,
                Margin = new Thickness(2)
            };

            // Text
            slot.Text = new OutlinedTextBlock()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = "",
                StrokeThickness = 2,
                FontFamily = _font.Value,
                FontSize = 60,
                FontWeight = FontWeights.Bold,
                Effect = new BlurEffect() { Radius = 3 }        // this gets changed based on whether the slot is empty or not
            };

            // Viewport Camera
            slot.Camera = new PerspectiveCamera()
            {
                Position = new Point3D(0, 0, 1),
                LookDirection = new Vector3D(0, 0, -1),
                UpDirection = new Vector3D(0, 1, 0),
                FieldOfView = 45d
            };

            // Viewport Lights
            Color ambient, front, back;
            InventoryItem.GetLightColors(out ambient, out front, out back, _lightColor);

            slot.WeaponAmbientLight = new AmbientLight(ambient);
            slot.WeaponFrontLight = new DirectionalLight(front, new Vector3D(1, -1, -1));
            slot.WeaponBackLight = new DirectionalLight(back, new Vector3D(-1, 1, 1));

            Model3DGroup lightsGeometry = new Model3DGroup();
            lightsGeometry.Children.Add(slot.WeaponAmbientLight);
            lightsGeometry.Children.Add(slot.WeaponFrontLight);
            lightsGeometry.Children.Add(slot.WeaponBackLight);

            //slot.Light = new PointLight()
            //{
            //    Color = Colors.Transparent,
            //    Position = new Point3D(0,0,0),
            //    Range = 0
            //};
            //lightsGeometry.Children.Add(slot.Light);

            // Viewport
            slot.Viewport = new Viewport3D();
            slot.Viewport.Camera = slot.Camera;
            slot.Viewport.Children.Add(new ModelVisual3D() { Content = lightsGeometry });

            Grid grid = new Grid();
            grid.Children.Add(slot.Text);
            grid.Children.Add(slot.Viewport);

            slot.Border.Child = grid;

            _slots.Add(slot);
            pnlSlots.Children.Add(slot.Border);
        }

        private void RemoveSlot()
        {
            if (_slots.Count != pnlSlots.Children.Count)
            {
                throw new ApplicationException(string.Format("The list and panel are out of sync.  list={0}, panel={1}", _slots.Count.ToString(), pnlSlots.Children.Count.ToString()));
            }

            pnlSlots.Children.RemoveAt(pnlSlots.Children.Count - 1);
            _slots.RemoveAt(_slots.Count - 1);
        }

        private void UpdateSlot(int index)
        {
            if (_slots.Count != pnlSlots.Children.Count)
            {
                throw new ApplicationException(string.Format("The list and panel are out of sync.  list={0}, panel={1}", _slots.Count.ToString(), pnlSlots.Children.Count.ToString()));
            }
            else if (_slots.Count < index)
            {
                throw new ArgumentException(string.Format("Invalid slot index: {0}, count={1}", index.ToString(), _slots.Count.ToString()));
            }

            Weapon weapon = null;
            if (_weapons != null && _weapons.Count > index)
            {
                weapon = _weapons[index];
            }

            SlotItem slot = _slots[index];

            slot.Text.Text = (index + 1).ToString();

            if (weapon == null)
            {
                #region Empty

                slot.Text.Fill = _emptyTextFill;
                slot.Text.Stroke = _emptyTextStroke;
                ((BlurEffect)slot.Text.Effect).Radius = 8;

                if (slot.WeaponVisual != null)
                {
                    slot.Viewport.Children.Remove(slot.WeaponVisual);
                }

                slot.WeaponToken = null;
                slot.WeaponRotateAnimation = null;
                slot.WeaponVisual = null;

                #endregion
            }
            else
            {
                #region Filled

                // Darken the background text
                slot.Text.Fill = _filledTextFill;
                slot.Text.Stroke = _filledTextStroke;
                ((BlurEffect)slot.Text.Effect).Radius = 3;

                // Remove visual if wrong weapon
                if (slot.WeaponToken != null && slot.WeaponToken.Value != weapon.Token)
                {
                    slot.Viewport.Children.Remove(slot.WeaponVisual);
                    slot.WeaponToken = null;
                    slot.WeaponVisual = null;
                }

                // Add weapon visual
                if (slot.WeaponToken == null)
                {
                    Model3D model = UtilityCore.Clone(weapon.Model);
                    QuaternionRotation3D quatTransform = new QuaternionRotation3D(Math3D.GetRandomRotation());
                    model.Transform = new RotateTransform3D(quatTransform);

                    slot.WeaponRotateAnimation = AnimateRotation.Create_AnyOrientation(quatTransform, 3d);

                    slot.WeaponVisual = new ModelVisual3D()
                    {
                        Content = model
                    };

                    // Pull the camera back to a good distance
                    //NOTE: The positions are nomalized by the caller after this method has finished
                    slot.Camera.Position = (slot.Camera.Position.ToVector().ToUnit() * (weapon.Radius * 2.7d)).ToPoint();

                    slot.Viewport.Children.Add(slot.WeaponVisual);
                    slot.WeaponToken = weapon.Token;
                }

                #endregion
            }
        }

        private static FontFamily GetBestBackgroundFont()
        {
            return UtilityWPF.GetFont(new string[] { "Lucida Console", "Verdana", "Microsoft Sans Serif", "Arial" });
        }

        #endregion
    }
}
