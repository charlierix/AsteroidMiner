using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Primitives3D;

namespace Game.Newt.AsteroidMiner2.ShipEditor
{
	//TODO: Layer list needs a button to group by part type (this will wipe out existing layers)
	//TODO: Part design base should have a default scale property.  Each CreateGeometry method needs to create a part that has a size of one
	//TODO: Enable/Disable unusable buttons (delete layer, undo/redo, cut/copy/paste)
	//TODO: Cut/Copy/Paste Icons
	//TODO: Tool part icons
	//TODO: Other misc icons
	//TODO: Arrow Keys: Slowly move selected part
	//TODO: Add an extended description to the tool item base class
	//TODO: After paste, put them in auto moving the selection (only if pasting onto the same layer).  Have a private bool for isdraggingselection, and keep going until mouse down.  Move all the code from mouse up/down into privates, because their roles can reverse
	//TODO: Some parts should be allowed to be unique (only position/rotation change, no clone, scale, etc).  These would represent salvaged parts
	//TODO: Mouse Up/Down/Move have a lot of code in them, pull that out into classes?
	//TODO: Instead of storing an array of undos, store a class that has a list of change sets (remove parts, remove layer)
	//TODO: Snap to other parts (toggle: None | Ortho | Radial) (sensitivity)
	//TODO: More parts
	public partial class Editor : UserControl
	{
		#region Enum: DragHitShapeType

		private enum DragHitShapeType
		{
			Plane_Orth,
			Plane_Camera,
			Cylinder_X,
			Cylinder_Y,
			Cylinder_Z,
			Circle_X,
			Circle_Y,
			Circle_Z,
			Sphere
		}

		#endregion
		#region Class: MyHitTestResult

		private class MyHitTestResult
		{
			//	Only one of these will be populated
			public RayMeshGeometry3DHitTestResult ModelHit = null;
			public Point3D? DragShapeHit = null;

			/// <summary>
			/// This is a helper property that returns the hit point, regardless of what type of hit this class represents
			/// </summary>
			public Point3D Point
			{
				get
				{
					if (this.ModelHit != null)
					{
						if (this.ModelHit.VisualHit.Transform != null)
						{
							return this.ModelHit.VisualHit.Transform.Transform(this.ModelHit.PointHit);
						}
						else
						{
							return this.ModelHit.PointHit;
						}
					}
					else if (this.DragShapeHit != null)
					{
						return this.DragShapeHit.Value;
					}
					else
					{
						throw new InvalidOperationException("All of the properties are null");
					}
				}
			}

			public double GetDistanceFromPoint(Point3D point)
			{
				return (point - this.Point).Length;
			}
		}

		#endregion

		#region Declaration Section

		private const string DATAFORMAT_PART = "data format - part";

		private bool _isInitialized = false;

		private string _msgboxCaption = "Editor";

		/// <summary>
		/// This is another copy of the the editor colors, but is static so the dependency properties can directly reference it to get the
		/// default values
		/// </summary>
		private static EditorColors _dpColors = new EditorColors();

		private EditorOptions _options = new EditorOptions();

		/// <summary>
		/// This listens to the mouse/keyboard and controls the camera
		/// </summary>
		private TrackBallRoam _trackball = null;

		/// <summary>
		/// These are all the parts in the toolbox (tab control)
		/// </summary>
		private List<PartToolItemBase> _partsToolbox = new List<PartToolItemBase>();

		/// <summary>
		/// These are all the actual parts currently on the 3D surface
		/// </summary>
		/// <remarks>
		/// The outer list is each layer.  The inner list is the parts in that layer
		/// </remarks>
		private List<List<DesignPart>> _parts = new List<List<DesignPart>>();
		private int _currentLayerIndex = -1;
		private bool _isAllLayers = false;

		/// <summary>
		/// If they select a part in the 3D work area, this gets instantiated to tell what was clicked on, where, etc
		/// This goes back to null if they click in the dead space
		/// </summary>
		private SelectedParts _selectedParts = null;

		//	This is used when dragging a part from the toolbox onto the 3D surface
		private DraggingDropPart _draggingDropObject = null;

		/// <summary>
		/// This is what _dragHitShape is currently set up as
		/// </summary>
		private DragHitShapeType _dragHitShapeType;
		/// <summary>
		/// This defines the shape (plane, cylinder, etc) that the user's mouse rays will intersect (when they drag objects around, this
		/// is the shape that the objects will snap to)
		/// </summary>
		private DragHitShape _dragHitShape = null;

		//	When they start to drag _selectedParts around, this is where they initiated the drag (passed to _dragHitShape.CastRay to
		//	determine offset)
		private RayHitTestParameters _dragMouseDownClickRay = null;
		private RayHitTestParameters _dragMouseDownCenterRay = null;

		private Point3D _mouseDownDragPos;
		private Quaternion _mouseDownDragOrientation;

		private bool _isDraggingParts = false;

		private bool _isDraggingSelectionBox = false;
		private Point _selectionBoxMouseDownPos;

		/// <summary>
		/// Each item in this list is a group of parts that had a change (it's a list of arrays, because multiple parts can be selected
		/// and changed at once)
		/// </summary>
		private List<UndoRedoBase[]> _undoRedo = new List<UndoRedoBase[]>();
		/// <summary>
		/// This is where in the undo/redo list they currently are
		/// </summary>
		private int _undoRedoIndex = -1;

		/// <summary>
		/// Whenever they hit cut or copy, this holds a clone of the parts that were selected at the time
		/// </summary>
		private DesignPart[] _clipboard = null;

		/// <summary>
		/// When they drag a part, this goes true so an undo point can be added on mouseup
		/// </summary>
		private bool _isSelectionDragDirty = false;

		private bool _isShiftDown = false;
		private bool _isAltDown = false;
		private bool _isCtrlDown = false;
		private bool _isCapsLockDown = false;

		private List<ModelVisual3D> _debugVisuals = new List<ModelVisual3D>();

		private ModelVisual3D _compassRose = null;

		//	These are for manipulating capslock
		//	http://msdn.microsoft.com/en-us/library/windows/desktop/ms646304(v=vs.85).aspx
		//	http://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx
		[DllImport("user32.dll")]
		internal static extern short GetKeyState(int keyCode);
		[DllImport("user32.dll")]
		static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

		private bool _isProgramaticallyTogglingCapsLock = false;

		#endregion

		#region Constructor

		public Editor()
		{
			//TODO: Research loading/saving in xaml
			//System.Windows.Markup.XamlReader test2 = new System.Windows.Markup.XamlReader();
			//System.Windows.Markup.XamlWriter test3;
			//MeshGeometry3D test;

			InitializeComponent();

			//	Without this, the xaml can't bind to the custom dependency properties
			this.DataContext = this;

			#region Camera Trackball

			_trackball = new TrackBallRoam(_camera);
			_trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
			_trackball.AllowZoomOnMouseWheel = true;
			_trackball.ZoomScale *= .25d;
			_trackball.KeyPanScale *= .1d;

			//_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
			#region copied from MouseComplete_NoLeft - middle button changed

			TrackBallMapping complexMapping = null;

			//	Middle Button
			complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
			complexMapping.Add(MouseButton.Middle);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateAroundLookDirection, MouseButton.Middle, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

			complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
			complexMapping.Add(MouseButton.Middle);
			complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Zoom, MouseButton.Middle, new Key[] { Key.LeftShift, Key.RightShift }));

			//retVal.Add(new TrackBallMapping(CameraMovement.Pan_AutoScroll, MouseButton.Middle));
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan, MouseButton.Middle));

			//	Left+Right Buttons (emulate middle)
			complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);

			complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			_trackball.Mappings.Add(complexMapping);

			complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);

			complexMapping = new TrackBallMapping(CameraMovement.Zoom);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
			_trackball.Mappings.Add(complexMapping);

			//complexMapping = new TrackBallMapping(CameraMovement.Pan_AutoScroll);
			complexMapping = new TrackBallMapping(CameraMovement.Pan);
			complexMapping.Add(MouseButton.Left);
			complexMapping.Add(MouseButton.Right);
			_trackball.Mappings.Add(complexMapping);

			//	Right Button
			complexMapping = new TrackBallMapping(CameraMovement.RotateInPlace_AutoScroll);
			complexMapping.Add(MouseButton.Right);
			complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
			complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
			_trackball.Mappings.Add(complexMapping);
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateInPlace, MouseButton.Right, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit_AutoScroll, MouseButton.Right, new Key[] { Key.LeftAlt, Key.RightAlt }));
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));

			#endregion

			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_In, Key.OemPlus));
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_In, Key.Add));
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Out, Key.OemMinus));
			_trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan_Keyboard_Out, Key.Subtract));

			_trackball.ShouldHitTestOnOrbit = true;

			_trackball.UserMovedCamera += new EventHandler<UserMovedCameraArgs>(Trackball_UserMovedCamera);

			#endregion

			_dragHitShape = new DragHitShape();
			//TODO: Add this to the options (the visual should let the user drag a cone)
			//_dragHitShape.ConstrainMinDotProduct = _options.DragHitShape_ConstrainMinDotProduct;
			ChangeDragHitShape();

			//	Compass Rose
			_compassRose = GetCompassRose(_options.EditorColors);
			_viewport.Children.Add(_compassRose);

			#region Ship name textbox

			//	The textbox's cursor can't be set directly, but is the opposite of background.  If I just use null or transparent, the cursor is still
			//	black, so I'm making the background transparent black, which makes the cursor white;
			//txtName.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
			//txtShipName.Background = new SolidColorBrush(Color.FromArgb(0, _options.EditorColors.Background.R, _options.EditorColors.Background.G, _options.EditorColors.Background.B));
			txtDesignName.Background = new SolidColorBrush(Color.FromArgb(25, _options.EditorColors.PanelBackground.R, _options.EditorColors.PanelBackground.G, _options.EditorColors.PanelBackground.B));

			txtDesignName.Foreground = new SolidColorBrush(_options.EditorColors.PartVisualTextColor);

			#endregion

			_isInitialized = true;

			btnNewLayer_Click(this, new RoutedEventArgs());		//	all layers
			btnNewLayer_Click(this, new RoutedEventArgs());		//	first real layer
		}

		#endregion

		#region Public Properties

		public EditorOptions Options
		{
			get
			{
				return _options;
			}
		}

		#endregion
		#region Private Properties

		//	The main background color
		private Brush GridBackground
		{
			get { return (Brush)GetValue(GridBackgroundProperty); }
			set { SetValue(GridBackgroundProperty, value); }
		}
		private static readonly DependencyProperty GridBackgroundProperty = DependencyProperty.Register("GridBackground", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.Background)));

		//	A standard panel
		private Brush PanelBackground
		{
			get { return (Brush)GetValue(PanelBackgroundProperty); }
			set { SetValue(PanelBackgroundProperty, value); }
		}
		private static readonly DependencyProperty PanelBackgroundProperty = DependencyProperty.Register("PanelBackground", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.PanelBackground)));
		private Brush PanelBorder
		{
			get { return (Brush)GetValue(PanelBorderProperty); }
			set { SetValue(PanelBorderProperty, value); }
		}
		private static readonly DependencyProperty PanelBorderProperty = DependencyProperty.Register("PanelBorder", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.PanelBorder)));

		private Brush TextStandard
		{
			get { return (Brush)GetValue(TextStandardProperty); }
			set { SetValue(TextStandardProperty, value); }
		}
		private static readonly DependencyProperty TextStandardProperty = DependencyProperty.Register("TextStandard", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.TextStandard)));

		//	The only current use is the selected layer item
		private Brush PanelSelectedItemBackground
		{
			get { return (Brush)GetValue(PanelSelectedItemBackgroundProperty); }
			set { SetValue(PanelSelectedItemBackgroundProperty, value); }
		}
		private static readonly DependencyProperty PanelSelectedItemBackgroundProperty = DependencyProperty.Register("PanelSelectedItemBackground", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.PanelSelectedItemBackground)));
		private Brush PanelSelectedItemBorder
		{
			get { return (Brush)GetValue(PanelSelectedItemBorderProperty); }
			set { SetValue(PanelSelectedItemBorderProperty, value); }
		}
		private static readonly DependencyProperty PanelSelectedItemBorderProperty = DependencyProperty.Register("PanelSelectedItemBorder", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.PanelSelectedItemBorder)));

		//	The color of the current tab page's header
		private Brush TabItemSelectedBackground
		{
			get { return (Brush)GetValue(TabItemSelectedBackgroundProperty); }
			set { SetValue(TabItemSelectedBackgroundProperty, value); }
		}
		private static readonly DependencyProperty TabItemSelectedBackgroundProperty = DependencyProperty.Register("TabItemSelectedBackground", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.TabItemSelectedBackground)));
		private Brush TabItemSelectedBorder
		{
			get { return (Brush)GetValue(TabItemSelectedBorderProperty); }
			set { SetValue(TabItemSelectedBorderProperty, value); }
		}
		private static readonly DependencyProperty TabItemSelectedBorderProperty = DependencyProperty.Register("TabItemSelectedBorder", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.TabItemSelectedBorder)));
		private Brush TabItemSelectedText
		{
			get { return (Brush)GetValue(TabItemSelectedTextProperty); }
			set { SetValue(TabItemSelectedTextProperty, value); }
		}
		private static readonly DependencyProperty TabItemSelectedTextProperty = DependencyProperty.Register("TabItemSelectedText", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.TabItemSelectedText)));

		//	The expander panels on the side of the window
		private Brush SideExpanderBackground
		{
			get { return (Brush)GetValue(SideExpanderBackgroundProperty); }
			set { SetValue(SideExpanderBackgroundProperty, value); }
		}
		private static readonly DependencyProperty SideExpanderBackgroundProperty = DependencyProperty.Register("SideExpanderBackground", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.SideExpanderBackground)));
		private Brush SideExpanderBorder
		{
			get { return (Brush)GetValue(SideExpanderBorderProperty); }
			set { SetValue(SideExpanderBorderProperty, value); }
		}
		private static readonly DependencyProperty SideExpanderBorderProperty = DependencyProperty.Register("SideExpanderBorder", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.SideExpanderBorder)));
		private Brush SideExpanderText
		{
			get { return (Brush)GetValue(SideExpanderTextProperty); }
			set { SetValue(SideExpanderTextProperty, value); }
		}
		private static readonly DependencyProperty SideExpanderTextProperty = DependencyProperty.Register("SideExpanderText", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.SideExpanderText)));

		//	The rectangle they drag to select multiple parts
		private Brush SelectionRectangleBorder
		{
			get { return (Brush)GetValue(SelectionRectangleBorderProperty); }
			set { SetValue(SelectionRectangleBorderProperty, value); }
		}
		private static readonly DependencyProperty SelectionRectangleBorderProperty = DependencyProperty.Register("SelectionRectangleBorder", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.SelectionRectangleBorder)));
		private Brush SelectionRectangleBackground
		{
			get { return (Brush)GetValue(SelectionRectangleBackgroundProperty); }
			set { SetValue(SelectionRectangleBackgroundProperty, value); }
		}
		private static readonly DependencyProperty SelectionRectangleBackgroundProperty = DependencyProperty.Register("SelectionRectangleBackground", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.SelectionRectangleBackground)));

		//	The undo/redo button colors
		private Brush UndoRedoBorder
		{
			get { return (Brush)GetValue(UndoRedoBorderProperty); }
			set { SetValue(UndoRedoBorderProperty, value); }
		}
		private static readonly DependencyProperty UndoRedoBorderProperty = DependencyProperty.Register("UndoRedoBorder", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.UndoRedoBorder)));
		private Brush UndoRedoGradientFrom
		{
			get { return (Brush)GetValue(UndoRedoGradientFromProperty); }
			set { SetValue(UndoRedoGradientFromProperty, value); }
		}
		private static readonly DependencyProperty UndoRedoGradientFromProperty = DependencyProperty.Register("UndoRedoGradientFrom", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.UndoRedoGradientFrom)));
		private Brush UndoRedoGradientTo
		{
			get { return (Brush)GetValue(UndoRedoGradientToProperty); }
			set { SetValue(UndoRedoGradientToProperty, value); }
		}
		private static readonly DependencyProperty UndoRedoGradientToProperty = DependencyProperty.Register("UndoRedoGradientTo", typeof(Brush), typeof(Editor), new UIPropertyMetadata(new SolidColorBrush(_dpColors.UndoRedoGradientTo)));

		#endregion

		#region Public Methods

		public void SetupEditor(string msgboxCaption, IEnumerable<PartToolItemBase> parts, UIElement managementControl)
		{
			//TODO: Take in some other settings, like max size

			_msgboxCaption = msgboxCaption;

			#region Toolbox

			//	Remove existing tabs (except test)
			int index = 0;
			while (index < tabControl.Items.Count)
			{
				//if (((TabItem)tabControl.Items[index]).Header == "Test")
				if (string.Equals(((TabItem)tabControl.Items[index]).Header, "Test"))		//	can't use ==, because header is an object
				{
					index++;
				}
				else
				{
					tabControl.Items.RemoveAt(index);
				}
			}

			TabItem defaultTab = null;

			//	Make the tabs
			foreach (string tabName in parts.Select(o => o.TabName).Distinct())
			{
				TabItem tab = new TabItem();
				//TODO: Use icons instead of text
				tab.Header = tabName;
				//tab.ToolTip = tabName;		//TODO: Uncomment this when icons are used

				ScrollViewer scroll = new ScrollViewer();
				scroll.Style = (Style)FindResource("tabScrollViewer");

				WrapPanel wrap = new WrapPanel();
				scroll.Content = wrap;

				tab.Content = scroll;

				//	Add the parts that belong in this tab
				//TODO: Make some kind of sub control for each category (an expander is a bit extreme, but something like that)
				foreach (PartToolItemBase part in parts.Where(o => o.TabName == tabName).OrderBy(o => o.Category))
				{
					_partsToolbox.Add(part);
					wrap.Children.Add(part.Visual2D);
				}

				tabControl.Items.Add(tab);

				if (defaultTab == null)
				{
					defaultTab = tab;
				}
			}

			//	Set the default tab
			tabControl.SelectedItem = defaultTab;

			#endregion

			pnlManagement.Child = managementControl;
		}

		public void GetDesign(out string name, out List<string> layerNames, out SortedList<int, List<DesignPart>> partsByLayer)
		{
			//	Name
			name = txtDesignName.Text.Trim();

			//	Layer Names
			layerNames = new List<string>();
			for (int cntr = 1; cntr < pnlLayers.Children.Count; cntr++)
			{
				LayerRow row = (LayerRow)pnlLayers.Children[cntr];
				layerNames.Add(row.LayerName);
			}

			//	Parts by Layer
			partsByLayer = new SortedList<int, List<DesignPart>>();
			for (int cntr = 0; cntr < _parts.Count; cntr++)
			{
				if (_parts[cntr].Count == 0)
				{
					continue;
				}

				partsByLayer.Add(cntr, _parts[cntr]);
			}
		}
		public void SetDesign(string name, List<string> layerNames, SortedList<int, List<DesignPart>> partsByLayer)
		{
			#region Wipe existing parts

			List<DesignPart> existingParts = _parts.SelectMany(o => o).ToList();
			if (existingParts.Count > 0)
			{
				ClearSelectedParts();
				DeleteParts(existingParts);
			}

			_parts.Clear();		//	This will get rebuilt in two passes.  First is layers, second is parts per layer

			#endregion

			SwapLayers(layerNames);

			#region OLD

			//List<UndoRedoLayerAddRemove> undoLayers = new List<UndoRedoLayerAddRemove>();

			////	Remove existing layers
			//for (int cntr = pnlLayers.Children.Count - 2; cntr >= 0; cntr--)
			//{
			//    LayerRow layer = (LayerRow)pnlLayers.Children[cntr + 1];		//	adding one, because AllLayers is at zero
			//    layer.LayerVisibilityChanged -= new EventHandler(Layer_LayerVisibilityChanged);
			//    layer.GotFocus -= new RoutedEventHandler(LayerRow_GotFocus);

			//    pnlLayers.Children.RemoveAt(cntr + 1);
			//    undoLayers.Add(new UndoRedoLayerAddRemove(cntr, false, layer.LayerName));
			//}

			////	Add new layers
			//for (int cntr = 0; cntr < layerNames.Count; cntr++)
			//{
			//    LayerRow layer = new LayerRow(this.Options.EditorColors);
			//    layer.LayerName = layerNames[cntr];
			//    layer.IsLayerVisible = true;
			//    layer.LayerVisibilityChanged += new EventHandler(Layer_LayerVisibilityChanged);
			//    layer.GotFocus += new RoutedEventHandler(LayerRow_GotFocus);

			//    pnlLayers.Children.Add(layer);

			//    undoLayers.Add(new UndoRedoLayerAddRemove(cntr, true, layer.LayerName));

			//    _parts.Add(new List<DesignPart>());
			//}

			//AddNewUndoRedoItem(undoLayers.ToArray());

			#endregion

			//	Design Name
			txtDesignName.Text = name;		//	I'm not going to bother with storing an undo/redo for the name

			#region Add new parts

			List<UndoRedoAddRemove> undoParts = new List<UndoRedoAddRemove>();

			foreach (int layerIndex in partsByLayer.Keys)
			{
				_parts.Add(new List<DesignPart>());

				foreach (DesignPart part in partsByLayer[layerIndex])
				{
					_viewport.Children.Add(part.Model);
					_parts[layerIndex].Add(part);

					undoParts.Add(new UndoRedoAddRemove(true, part, layerIndex));
				}
			}

			AddNewUndoRedoItem(undoParts.ToArray());

			#endregion
		}

		#endregion

		#region Event Listeners

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			//	This must be done during load to get the key events to fire
			grdViewPort.Focus();

			#region Custom Button Colors

			//	Setting the style manually doesn't work either
			//Style undoStyle = (Style)FindResource("undoButton");

			//	The style is ignored, so these need to be set manually (I think it's because the undo graphic control has private properties, and swaps out when enabled/disabled)
			Game.Newt.HelperClasses.Controls.UndoRedo undoGraphic = (Game.Newt.HelperClasses.Controls.UndoRedo)btnUndo.ButtonContent;
			//undoGraphic.Style = undoStyle;
			undoGraphic.ArrowBorder = _options.EditorColors.UndoRedoBorder;
			undoGraphic.ArrowBackgroundFrom = _options.EditorColors.UndoRedoGradientFrom;
			undoGraphic.ArrowBackgroundTo = _options.EditorColors.UndoRedoGradientTo;

			undoGraphic = (Game.Newt.HelperClasses.Controls.UndoRedo)btnRedo.ButtonContent;
			undoGraphic.ArrowBorder = _options.EditorColors.UndoRedoBorder;
			undoGraphic.ArrowBackgroundFrom = _options.EditorColors.UndoRedoGradientFrom;
			undoGraphic.ArrowBackgroundTo = _options.EditorColors.UndoRedoGradientTo;

			#endregion
		}

		private void UserControl_GotFocus(object sender, RoutedEventArgs e)
		{
			//	There usually aren't any focusable controls, so set focus to this (so the keyboard events will work)
			if (!(e.OriginalSource is TextBox))
			{
				grdViewPort.Focus();
			}
		}
		private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
		{
			//	When the are editing a textbox and click in the editor area, focus doesn't change without some assistance
			grdViewPort.Focus();
		}

		private void Tool_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				_draggingDropObject = null;

				//	Find the tool item that started this
				PartToolItemBase part = FindPart(e.Source as DependencyObject);
				if (part == null)
				{
					return;
				}

				//	Since they are dragging a new part, make sure nothing is selected
				ClearSelectedParts();

				//	Reset the snap plane
				//ChangeDragPlane(_isVertical);
				ChangeDragHitShape();

				// Store the mouse position and tool that is dragging
				_draggingDropObject = new DraggingDropPart();
				_draggingDropObject.DragStart = e.GetPosition(null);
				_draggingDropObject.Part2D = part;		//	don't build a 3D object yet, they may quit dragging before they get to the viewport
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Tool_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (_draggingDropObject == null)
				{
					return;
				}

				// Get the current mouse position
				Point mousePos = e.GetPosition(null);
				Vector diff = _draggingDropObject.DragStart - mousePos;

				if (e.LeftButton == MouseButtonState.Pressed && (
					Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
				{
					//	Store this so mouse move can drag this around
					_draggingDropObject.Part3D = _draggingDropObject.Part2D.GetNewDesignPart();

					ModelVisual3D model = new ModelVisual3D();
					model.Content = _draggingDropObject.Part3D.Model;
					_draggingDropObject.Model = model;

					// Initialize the drag & drop operation
					DataObject dragData = new DataObject(DATAFORMAT_PART, _partsToolbox[0]);
					if (DragDrop.DoDragDrop(_draggingDropObject.Part2D.Visual2D, dragData, DragDropEffects.Move) == DragDropEffects.None)
					{
						//	If they drag around, but not in a place where Drop gets fired, then execution reenters here.
						//	Get rid of the dragging part
						if (_draggingDropObject != null && _draggingDropObject.HasAddedToViewport)
						{
							_viewport.Children.Remove(_draggingDropObject.Model);
						}

						_draggingDropObject = null;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void grdViewPort_PreviewDragOver(object sender, DragEventArgs e)
		{
			try
			{
				if (e.Data.GetDataPresent(DATAFORMAT_PART))
				{
					if (_draggingDropObject != null)
					{
						//	Move the object to where the mouse is pointing
						RayHitTestParameters clickRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, e.GetPosition(grdViewPort));
						Point3D? dragPoint = _dragHitShape.CastRay(clickRay, clickRay, clickRay, _camera, _viewport);

						if (dragPoint == null)
						{
							_draggingDropObject.Part3D.Position = new Point3D(0, 0, 0);
						}
						else
						{
							_draggingDropObject.Part3D.Position = dragPoint.Value;
						}

						//	Since this model is in the active portion of the 3D surface, make sure the model is visible
						if (!_draggingDropObject.HasAddedToViewport)
						{
							_viewport.Children.Add(_draggingDropObject.Model);
							_draggingDropObject.HasAddedToViewport = true;
						}
					}
				}
				else
				{
					e.Effects = DragDropEffects.None;
				}

				e.Handled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void grdViewPort_Drop(object sender, DragEventArgs e)
		{
			//TODO:  Remove it if it's too far away from the origin

			try
			{
				//	Mousemove already moved it, just leave it committed
				if (_draggingDropObject != null && _draggingDropObject.HasAddedToViewport)		//	just making sure
				{
					#region Store Part

					DesignPart design = new DesignPart(_options)
					{
						Model = _draggingDropObject.Model,
						Part2D = _draggingDropObject.Part2D,
						Part3D = _draggingDropObject.Part3D
					};

					_parts[_currentLayerIndex].Add(design);

					#endregion

					//	Store in undo/redo
					AddNewUndoRedoItem(new UndoRedoAddRemove[] { new UndoRedoAddRemove(true, design, _currentLayerIndex) });
				}

				_draggingDropObject = null;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void grdViewPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				_isDraggingParts = false;
				_isDraggingSelectionBox = false;

				//	Fire a ray at the mouse point
				Point clickPoint = e.GetPosition(grdViewPort);
				RayHitTestParameters clickRay;
				List<MyHitTestResult> hits = CastRay(out clickRay, clickPoint, null, true);

				//	See if they clicked on an existing part
				DesignPart clickedItem = null;
				foreach (MyHitTestResult hit in hits.Where(o => o.ModelHit != null))		//	skipping the dragshape hit
				{
					#region Detect model hit

					if (_selectedParts != null && !_selectedParts.IsLocked)
					{
						if (_selectedParts.IsModifierHit(hit.ModelHit.VisualHit))
						{
							//	This is a modifier of the selected item (scale arrow or rotate ring)
							_selectedParts.StartDraggingModifier(hit.ModelHit.VisualHit, clickPoint);		//	if alt is pressed, I don't want to copy this, only when they drag the part around
							_isDraggingParts = true;
							return;
						}
					}

					IEnumerable<DesignPart> candidates = null;
					if (_isAllLayers)
					{
						candidates = _parts.SelectMany(o => o);
					}
					else
					{
						candidates = _parts[_currentLayerIndex];
					}

					var matches = candidates.Where(o => o.Model == hit.ModelHit.VisualHit);
					if (matches.Count() == 1)
					{
						clickedItem = matches.First();
						break;
					}

					#endregion
				}

				#region Update selected item

				//	Update the currently selected item
				if (clickedItem != null && _selectedParts != null && _selectedParts.GetParts().Any(o => o == clickedItem))
				{
					if (_isCtrlDown)
					{
						#region Deselect from selected parts

						//	Ctrl clicking a selected part will deselect it
						_selectedParts.Remove(clickedItem);

						if (_selectedParts.Count == 0)
						{
							ClearSelectedParts();
						}

						#endregion
					}
					else
					{
						#region Update click point

						//	They clicked on the same part, just update the points
						_dragMouseDownClickRay = clickRay;
						_dragMouseDownCenterRay = new RayHitTestParameters(_selectedParts.Position, clickRay.Direction);		//TODO: the ray through the center of the part really isn't parallel to the click ray (since the perspective camera sees in a cone)

						#endregion
					}
				}
				else if (clickedItem != null && _selectedParts != null && _isCtrlDown)
				{
					#region Add to current selection

					_selectedParts.Add(clickedItem);

					//	Remember the click location in case they start dragging
					_dragMouseDownClickRay = clickRay;
					_dragMouseDownCenterRay = new RayHitTestParameters(_selectedParts.Position, clickRay.Direction);		//TODO: the ray through the center of the part really isn't parallel to the click ray (since the perspective camera sees in a cone)

					#endregion
				}
				else if (clickedItem == null && _isCtrlDown)
				{
					//	They missed any parts, but are making a ctrl+selectionbox.  Do nothing here and let mouseup take care of things
				}
				else
				{
					#region Changing selection, or unselecting everything

					ClearSelectedParts();

					if (clickedItem != null)
					{
						//	Select this part
						_selectedParts = new SelectedParts(_viewport, _camera, _options);
						_selectedParts.Add(clickedItem);

						//	Remember where they clicked
						_dragMouseDownClickRay = clickRay;
						_dragMouseDownCenterRay = new RayHitTestParameters(_selectedParts.Position, clickRay.Direction);		//TODO: the ray through the center of the part really isn't parallel to the click ray (since the perspective camera sees in a cone)

						if (chkShowGuideLines.IsChecked.Value)
						{
							ShowGuideLines(chkShowGuideLinesAllLayers.IsChecked.Value);
						}
					}

					#endregion
				}

				#endregion

				if (_isAltDown && _selectedParts != null)
				{
					#region Clone part

					List<UndoRedoAddRemove> undoItems = new List<UndoRedoAddRemove>();

					//	Make a copies of the selected parts
					int partIndex = -1;
					foreach (DesignPart clonedPart in _selectedParts.CloneParts())
					{
						partIndex++;

						_viewport.Children.Add(clonedPart.Model);

						if (clonedPart.GuideLines != null)
						{
							_viewport.Children.Add(clonedPart.GuideLines);
						}

						int layerIndex = _currentLayerIndex;
						if (_isAllLayers)
						{
							List<DesignPart> origParts = _selectedParts.GetParts().ToList();
							for (int cntr = 0; cntr < _parts.Count; cntr++)
							{
								if (_parts[cntr].Contains(origParts[partIndex]))
								{
									layerIndex = cntr;
									break;
								}
							}
						}

						_parts[layerIndex].Add(clonedPart);

						//	Build undo item
						undoItems.Add(new UndoRedoAddRemove(true, clonedPart, layerIndex));
					}

					//	Store the undo items
					AddNewUndoRedoItem(undoItems.ToArray());

					#endregion
				}

				if ((_selectedParts == null) || (clickedItem == null && _isCtrlDown))
				{
					#region Start selection rectangle

					if (!_isDraggingSelectionBox)
					{
						_isDraggingSelectionBox = true;
						_selectionBoxMouseDownPos = e.GetPosition(grdViewPort);
						grdViewPort.CaptureMouse();

						//	Initial placement of the drag selection box
						Canvas.SetLeft(selectionBox, _selectionBoxMouseDownPos.X);
						Canvas.SetTop(selectionBox, _selectionBoxMouseDownPos.Y);
						selectionBox.Width = 0;
						selectionBox.Height = 0;

						//	Make the drag selection box visible.
						selectionBox.Visibility = Visibility.Visible;
					}

					#endregion
				}
				else if (clickedItem != null && _isCtrlDown)
				{
					//	They just deselected a part.  Don't drag, don't make a selection box
				}
				else
				{
					_isDraggingParts = true;
					_mouseDownDragPos = _selectedParts.Position;
					_mouseDownDragOrientation = _selectedParts.Orientation.ToUnit();
					_isSelectionDragDirty = false;
				}

				//	Make sure the drag shape is current
				ChangeDragHitShape();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void grdViewPort_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (_isDraggingSelectionBox)
				{
					#region Selection Box

					// Release the mouse capture and stop tracking it.
					_isDraggingSelectionBox = false;
					grdViewPort.ReleaseMouseCapture();

					// Hide the drag selection box.
					selectionBox.Visibility = Visibility.Collapsed;

					if (Math3D.IsNearZero(selectionBox.Width) || Math3D.IsNearZero(selectionBox.Height))
					{
						//	This is a miss, because the only way to start a selction box is to not click on anything
						return;
					}

					Point mouseUpPos = e.GetPosition(grdViewPort);

					//	See if the part's centers are inside the rectangular solid.  This way they are required to drag the box at least half way thru the part, but minor
					//	touching will be ignored

					//	Cast 4 rays
					Point topLeft = new Point(Canvas.GetLeft(selectionBox), Canvas.GetTop(selectionBox));
					//Point bottomRight = new Point(Canvas.GetRight(selectionBox), Canvas.GetBottom(selectionBox));		//	this is just returning NANs for bottom and right
					Point bottomRight = new Point(topLeft.X + selectionBox.Width, topLeft.Y + selectionBox.Height);

					var rayTopLeft = UtilityWPF.RayFromViewportPoint(_camera, _viewport, topLeft);
					var rayTopRight = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(bottomRight.X, topLeft.Y));
					var rayBottomLeft = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(topLeft.X, bottomRight.Y));
					var rayBottomRight = UtilityWPF.RayFromViewportPoint(_camera, _viewport, bottomRight);

					List<ITriangle> planes = new List<ITriangle>();		//	visualizing this in sketchup is a life saver
					planes.Add(new Triangle(rayTopLeft.Origin, rayTopRight.Origin, rayTopLeft.Origin + rayTopLeft.Direction));		//	top
					planes.Add(new Triangle(rayTopRight.Origin, rayBottomRight.Origin, rayTopRight.Origin + rayTopRight.Direction));		//	right
					planes.Add(new Triangle(rayBottomRight.Origin, rayBottomLeft.Origin, rayBottomRight.Origin + rayBottomRight.Direction));		//	bottom
					planes.Add(new Triangle(rayBottomLeft.Origin, rayTopLeft.Origin, rayBottomLeft.Origin + rayBottomLeft.Direction));		//	left
					planes.Add(new Triangle(rayTopLeft.Origin, rayBottomRight.Origin, rayTopRight.Origin));		//	selection box

					//	Figure out which layers to look at
					IEnumerable<DesignPart> candidates = null;
					if (_isAllLayers)
					{
						candidates = _parts.SelectMany(o => o);
					}
					else
					{
						candidates = _parts[_currentLayerIndex];
					}

					//	Find all the parts inside this rectangular cone
					List<DesignPart> hitParts = candidates.Where(o => Math3D.IsInside(planes, o.Part3D.Position)).ToList();

					if (_isCtrlDown && _selectedParts != null)
					{
						#region Ctrl+Select

						if (hitParts.Count > 0)
						{
							//	Any of the hit parts that are currently in the selection should be removed.  Any that aren't should be added
							List<DesignPart> origSelectedParts = _selectedParts.GetParts().ToList();

							foreach (DesignPart hitPart in hitParts)
							{
								if (origSelectedParts.Contains(hitPart))
								{
									_selectedParts.Remove(hitPart);
								}
								else
								{
									_selectedParts.Add(hitPart);
								}
							}

							if (_selectedParts.Count == 0)
							{
								ClearSelectedParts();
							}
						}

						#endregion
					}
					else
					{
						#region Standard Select

						ClearSelectedParts();		//	doesn't matter if there was a selection or not.  A new one is needed

						if (hitParts.Count > 0)
						{
							//	Create a selection, and put the hit parts in it
							_selectedParts = new SelectedParts(_viewport, _camera, _options);
							_selectedParts.AddRange(hitParts);
						}

						#endregion
					}

					//	Update guidelines
					if (_selectedParts != null && chkShowGuideLines.IsChecked.Value)
					{
						ShowGuideLines(chkShowGuideLinesAllLayers.IsChecked.Value);
					}

					#endregion
				}
				else if (_isDraggingParts)
				{
					#region Finish moving selected parts

					_isDraggingParts = false;

					if (_selectedParts != null && _selectedParts.IsDraggingModifier)
					{
						_selectedParts.StopDraggingModifier();
					}

					if (_selectedParts != null && _isSelectionDragDirty)
					{
						//	They moved the selection, so store it in the undo list

						//	Group the parts by layer
						SortedList<int, List<DesignPart>> selectedPartsByLayer = GetPartsByLayer(_selectedParts.GetParts().ToList());

						//	Build the undo items
						List<UndoRedoTransformChange> undoItems = new List<UndoRedoTransformChange>();
						foreach (int layerIndex in selectedPartsByLayer.Keys)
						{
							undoItems.AddRange(selectedPartsByLayer[layerIndex].Select(o => new UndoRedoTransformChange(o.Part3D.Token, layerIndex)
								{
									Orientation = o.Part3D.Orientation,
									Position = o.Part3D.Position,
									Scale = o.Part3D.Scale
								}));
						}

						AddNewUndoRedoItem(undoItems.ToArray());
					}

					#endregion
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (_isDraggingSelectionBox)
				{
					#region Selection Box

					Point mousePos = e.GetPosition(grdViewPort);

					if (_selectionBoxMouseDownPos.X < mousePos.X)
					{
						Canvas.SetLeft(selectionBox, _selectionBoxMouseDownPos.X);
						selectionBox.Width = mousePos.X - _selectionBoxMouseDownPos.X;
					}
					else
					{
						Canvas.SetLeft(selectionBox, mousePos.X);
						selectionBox.Width = _selectionBoxMouseDownPos.X - mousePos.X;
					}

					if (_selectionBoxMouseDownPos.Y < mousePos.Y)
					{
						Canvas.SetTop(selectionBox, _selectionBoxMouseDownPos.Y);
						selectionBox.Height = mousePos.Y - _selectionBoxMouseDownPos.Y;
					}
					else
					{
						Canvas.SetTop(selectionBox, mousePos.Y);
						selectionBox.Height = _selectionBoxMouseDownPos.Y - mousePos.Y;
					}

					#endregion
				}
				else if (_isDraggingParts)
				{
					#region Dragging selected parts

					if (_selectedParts == null || _selectedParts.IsLocked || e.LeftButton != MouseButtonState.Pressed)
					{
						return;
					}

					_isSelectionDragDirty = true;

					if (_selectedParts.IsDraggingModifier)
					{
						//	Move the object to where the mouse is pointing
						_selectedParts.DragModifier(e.GetPosition(grdViewPort));
					}
					else
					{
						//	Move the object to where the mouse is pointing
						//_selectedParts.Position = DragItem(_selectedParts.Position, _selectedParts.GetParts().Select(o => o.Model), e.GetPosition(grdViewPort), _selectedParts.GetModifierVisuals());
						DragItem(e.GetPosition(grdViewPort));
					}

					#endregion
				}
				else
				{
					#region Hot Track

					//	For now, just hottrack the drag modifiers
					if (_selectedParts != null && !_selectedParts.IsLocked)
					{
						Point mousePos = e.GetPosition(grdViewPort);
						RayHitTestParameters mouseRay;
						List<MyHitTestResult> hits = CastRay(out mouseRay, mousePos, null, true);

						bool foundOne = false;
						foreach (MyHitTestResult hit in hits.Where(o => o.ModelHit != null))		//	skipping the dragshape hit
						{
							if (_selectedParts.IsModifierHit(hit.ModelHit.VisualHit))
							{
								foundOne = true;
								_selectedParts.HotTrackModifier(hit.ModelHit.VisualHit);
								break;
							}
						}

						if (!foundOne)
						{
							_selectedParts.StopHotTrack();
						}
					}

					#endregion
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void grdViewPort_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (_isProgramaticallyTogglingCapsLock)
			{
				return;
			}

			try
			{
				#region Repeatable Keystrokes

				switch (e.Key)
				{
					case Key.Z:
						if (_isCtrlDown && !(_isShiftDown || _isAltDown))
						{
							Undo();
						}
						break;

					case Key.Y:
						if (_isCtrlDown && !(_isShiftDown || _isAltDown))
						{
							Redo();
						}
						break;
				}

				#endregion

				if (e.IsRepeat)
				{
					return;
				}

				#region Nonrepeatable Keystrokes

				Key key = (e.Key == Key.System ? e.SystemKey : e.Key);		//	windows treats alt special, but I just care if alt is pressed

				switch (key)
				{
					case Key.LeftShift:
					case Key.RightShift:
						_isShiftDown = true;
						ChangeDragHitShape();
						break;

					case Key.LeftAlt:
					case Key.RightAlt:
						_isAltDown = true;
						this.Cursor = Cursors.Hand;
						break;

					case Key.LeftCtrl:
					case Key.RightCtrl:
						_isCtrlDown = true;
						break;

					case Key.CapsLock:
						_isCapsLockDown = true;
						ChangeDragHitShape();
						break;

					case Key.Delete:
						if (_isShiftDown && !(_isCtrlDown || _isAltDown))
						{
							Cut();		//	shift+insert is down below
						}
						else
						{
							DeleteSelectedParts();
						}
						break;

					case Key.Back:
						DeleteSelectedParts();
						break;

					case Key.Space:
						LockUnlockSelectedParts();
						break;

					case Key.X:
						if (_isCtrlDown && !(_isShiftDown || _isAltDown))
						{
							Cut();
						}
						break;

					case Key.C:
						if (_isCtrlDown && !(_isShiftDown || _isAltDown))
						{
							Copy();
						}
						break;

					case Key.V:
						if (_isCtrlDown && !(_isShiftDown || _isAltDown))
						{
							Paste();
						}
						break;

					case Key.Insert:		//	shift+delete is higher up the switch statement (part of delete selection)
						if (_isCtrlDown && !(_isShiftDown || _isAltDown))
						{
							Copy();
						}
						else if (_isShiftDown && !(_isCtrlDown || _isAltDown))
						{
							Paste();
						}
						break;

					case Key.A:
						if (_isCtrlDown)
						{
							SelectAllParts();
						}
						break;
				}

				#endregion
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void grdViewPort_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (_isProgramaticallyTogglingCapsLock)
			{
				return;
			}

			try
			{
				Key key = (e.Key == Key.System ? e.SystemKey : e.Key);		//	windows treats alt special, but I just care if alt is pressed

				switch (key)
				{
					case Key.LeftShift:
					case Key.RightShift:
						_isShiftDown = false;
						ChangeDragHitShape();
						break;

					case Key.LeftAlt:
					case Key.RightAlt:
						_isAltDown = false;
						this.Cursor = Cursors.Arrow;
						break;

					case Key.LeftCtrl:
					case Key.RightCtrl:
						_isCtrlDown = false;
						break;

					case Key.CapsLock:
						_isCapsLockDown = false;
						ChangeDragHitShape();

						#region Force Capslock Off

						//NOTE: I could look at the capslock state during the control's get focus, and force it back to that value whenever they get here, but the
						// odds of someone wanting to leave capslock on is rare, so I'll just keep turning it back off

						const int KEYEVENTF_EXTENDEDKEY = 0x1;
						const int KEYEVENTF_KEYUP = 0x2;
						const byte VK_CAPITAL = 0x14;

						int isCapsDown = GetKeyState(VK_CAPITAL);

						if (isCapsDown == 1)		//	if it's 0, then it's already off, so nothing to do
						{
							_isProgramaticallyTogglingCapsLock = true;

							keybd_event(VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
							keybd_event(VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);

							_isProgramaticallyTogglingCapsLock = false;
						}

						#endregion
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Trackball_UserMovedCamera(object sender, UserMovedCameraArgs e)
		{
			try
			{
				if (_dragHitShapeType == DragHitShapeType.Plane_Camera && e.IsRotate)
				{
					ChangeDragHitShape();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void chkShowGuideLines_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isInitialized)
				{
					return;
				}

				if (chkShowGuideLines.IsChecked.Value)
				{
					chkShowGuideLinesAllLayers.Visibility = Visibility.Visible;
				}
				else
				{
					chkShowGuideLinesAllLayers.Visibility = Visibility.Collapsed;
				}

				HideGuideLines();

				if (_selectedParts != null && chkShowGuideLines.IsChecked.Value)		//	it doesn't matter if all layers is checked or not
				{
					ShowGuideLines(chkShowGuideLinesAllLayers.IsChecked.Value);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void DragSnapShape_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isInitialized)
				{
					return;
				}

				ChangeDragHitShape();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Layer_LayerVisibilityChanged(object sender, EventArgs e)
		{
			try
			{
				if (!(sender is LayerRow))
				{
					throw new ApplicationException("Unknown Sender");
				}

				ClearSelectedParts();

				LayerRow senderCast = (LayerRow)sender;

				int index = pnlLayers.Children.IndexOf(senderCast);
				index--;		//	the panel has one extra "layer", the real ones are shifted by one

				foreach (DesignPart part in _parts[index])
				{
					if (senderCast.IsLayerVisible)
					{
						if (!_viewport.Children.Contains(part.Model))
						{
							_viewport.Children.Add(part.Model);
						}
					}
					else
					{
						if (_viewport.Children.Contains(part.Model))
						{
							_viewport.Children.Remove(part.Model);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void LayerRow_GotFocus(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(sender is LayerRow))
				{
					return;
				}

				LayerRow senderCast = (LayerRow)sender;

				int index = pnlLayers.Children.IndexOf(senderCast);
				index--;

				if ((index < 0 && !_isAllLayers) || (index >= 0 && _isAllLayers) || index != _currentLayerIndex)
				{
					ChangeLayer(index, false);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnNewLayer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				LayerRow layer = new LayerRow(_options.EditorColors);

				if (pnlLayers.Children.Count == 0)
				{
					layer.LayerName = "All Layers";
					layer.IsLayerVisible = true;
					layer.CanEditName = false;
					layer.ShouldShowVisibility = false;
				}
				else
				{
					layer.LayerName = "Layer " + (pnlLayers.Children.Count).ToString();		//	using count instead of count + 1, because 0 is all layers
					layer.IsLayerVisible = true;
					layer.LayerVisibilityChanged += new EventHandler(Layer_LayerVisibilityChanged);
				}

				layer.GotFocus += new RoutedEventHandler(LayerRow_GotFocus);

				pnlLayers.Children.Add(layer);

				if (pnlLayers.Children.Count > 1)		//	AllLayers doesn't count as an actual layer, it just lets you manipulate parts regardless what layer they are on
				{
					_parts.Add(new List<DesignPart>());
					ChangeLayer(_parts.Count - 1, false);
				}

				if (pnlLayers.Children.Count > 2)		//	don't let them undo the first layer
				{
					AddNewUndoRedoItem(new UndoRedoLayerAddRemove[] { new UndoRedoLayerAddRemove(_currentLayerIndex, true, layer.LayerName) });
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnDeleteLayer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (pnlLayers.Children.Count <= 2)		//	two, because there is the all layers and first layer
				{
					MessageBox.Show("There must be at least one layer", _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				//	Make sure nothing is selected
				ClearSelectedParts();

				//	Remove everything on the current layer
				if (_parts[_currentLayerIndex].Count > 0)
				{
					DeleteParts(_parts[_currentLayerIndex].ToList());		//	need to send it a copy of the list
				}

				//	Remember the removal
				LayerRow layer = (LayerRow)pnlLayers.Children[_currentLayerIndex + 1];		//	adding one, because AllLayers is at zero
				AddNewUndoRedoItem(new UndoRedoLayerAddRemove[] { new UndoRedoLayerAddRemove(_currentLayerIndex, false, layer.LayerName) });

				//	Remove it
				layer.LayerVisibilityChanged -= new EventHandler(Layer_LayerVisibilityChanged);
				layer.GotFocus -= new RoutedEventHandler(LayerRow_GotFocus);

				pnlLayers.Children.RemoveAt(_currentLayerIndex + 1);		//	adding one, because AllLayers is at zero
				_parts.RemoveAt(_currentLayerIndex);

				//	Change layers
				int newIndex = _currentLayerIndex;
				if (newIndex >= _parts.Count)
				{
					newIndex = _parts.Count - 1;
				}

				ChangeLayer(newIndex, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnLayersByType_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				List<DesignPart> existingParts = _parts.SelectMany(o => o).ToList();

				if (existingParts.Count == 0)
				{
					MessageBox.Show("There are currently no parts, so there is nothing to categorize", _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
				else if (_parts.Count > 1)		//	if there's only one layer, there's no need to ask for permission
				{
					if (MessageBox.Show("The current layers will be removed, and the parts will be placed in new layers\r\n\r\nContinue?", _msgboxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
					{
						return;
					}
				}

				//	Group by category
				var partsByCategory = existingParts.GroupBy(o => o.Part2D.Category).ToList();		//	linq is just too damn cool

				//	Remove existing parts.  I'll call delete, then reinsert them so that the undo/redo will work
				ClearSelectedParts();
				DeleteParts(existingParts);
				_parts.Clear();

				SwapLayers(partsByCategory.Select(o => o.Key).ToList());
				#region OLD

				//List<UndoRedoLayerAddRemove> undoLayers = new List<UndoRedoLayerAddRemove>();

				////	Remove existing layers
				//for (int cntr = pnlLayers.Children.Count - 2; cntr >= 0; cntr--)
				//{
				//    LayerRow layer = (LayerRow)pnlLayers.Children[cntr + 1];		//	adding one, because AllLayers is at zero
				//    layer.LayerVisibilityChanged -= new EventHandler(Layer_LayerVisibilityChanged);
				//    layer.GotFocus -= new RoutedEventHandler(LayerRow_GotFocus);

				//    pnlLayers.Children.RemoveAt(cntr + 1);
				//    undoLayers.Add(new UndoRedoLayerAddRemove(cntr, false, layer.LayerName));
				//}

				////	Add new layers
				//for (int cntr = 0; cntr < partsByCategory.Count; cntr++)
				//{
				//    LayerRow layer = new LayerRow(this.Options.EditorColors);
				//    layer.LayerName = partsByCategory[cntr].Key;
				//    layer.IsLayerVisible = true;
				//    layer.LayerVisibilityChanged += new EventHandler(Layer_LayerVisibilityChanged);
				//    layer.GotFocus += new RoutedEventHandler(LayerRow_GotFocus);

				//    pnlLayers.Children.Add(layer);

				//    undoLayers.Add(new UndoRedoLayerAddRemove(cntr, true, layer.LayerName));
				//}

				//AddNewUndoRedoItem(undoLayers.ToArray());

				#endregion

				#region Add new parts

				List<UndoRedoAddRemove> undoParts = new List<UndoRedoAddRemove>();

				for (int cntr = 0; cntr < partsByCategory.Count; cntr++)
				{
					_parts.Add(new List<DesignPart>());

					foreach (DesignPart part in partsByCategory[cntr])
					{
						_viewport.Children.Add(part.Model);
						_parts[cntr].Add(part);

						undoParts.Add(new UndoRedoAddRemove(true, part, cntr));
					}
				}

				AddNewUndoRedoItem(undoParts.ToArray());

				#endregion

				ChangeLayer(-1, true);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnUndo_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Undo();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnRedo_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Redo();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnCut_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Cut();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCopy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Copy();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnPaste_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Paste();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnResetCamera_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_camera.Position = new Point3D(0, 35, 5);
				_camera.LookDirection = new Vector3D(0, -35, -5);
				_camera.UpDirection = new Vector3D(0, -1, 0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCenterDrawing_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//if (_parts.SelectMany(o => o).Count() == 0)
				if (!_parts.Any(o => o.Count > 0))
				{
					//	There are no actual parts (doesn't matter how many layers there are)
					return;
				}

				ClearSelectedParts();

				//	Get the AABB of all the parts put together - yay linq!!!  :)
				Vector3D min = new Vector3D(
					_parts.Min(o => o.Min(p => p.Part3D.Position.X - (p.Part3D.Scale.X * .5d))),
					_parts.Min(o => o.Min(p => p.Part3D.Position.Y - (p.Part3D.Scale.Y * .5d))),
					_parts.Min(o => o.Min(p => p.Part3D.Position.Z - (p.Part3D.Scale.Z * .5d))));

				Vector3D max = new Vector3D(
					_parts.Max(o => o.Max(p => p.Part3D.Position.X + (p.Part3D.Scale.X * .5d))),
					_parts.Max(o => o.Max(p => p.Part3D.Position.Y + (p.Part3D.Scale.Y * .5d))),
					_parts.Max(o => o.Max(p => p.Part3D.Position.Z + (p.Part3D.Scale.Z * .5d))));

				//	Find the center point
				Vector3D size = max - min;
				Vector3D center = min + size * .5d;
				if (Math3D.IsNearZero(center))
				{
					return;
				}

				//	Shift all the positions
				foreach (DesignPart part in _parts.SelectMany(o => o))
				{
					part.Part3D.Position -= center;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnShowMappings_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				InfoWindow window = new InfoWindow();
				window.Width = 400;
				window.Height = 400;
				window.Title = "Keyboard/Mouse Mappings";

				window.Text = _trackball.GetMappingReport();

				window.Background = new SolidColorBrush(_options.EditorColors.Background);
				window.InnerBorderBrush = new SolidColorBrush(_options.EditorColors.PartVisualBorderColor);
				window.TextColor = new SolidColorBrush(_options.EditorColors.PartVisualTextColor);
				window.Show();

				MessageBox.Show("TODO: Show part manipulation keys");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		//TODO: Move these to the calling window
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			//try
			//{
			//    if (txtDesignName.Text.Trim() == "")
			//    {
			//        MessageBox.Show("Please give this a name first", _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
			//        return;
			//    }

			//    //	Get the definition of the ship
			//    ShipDNA ship = new ShipDNA();
			//    ship.ShipName = txtDesignName.Text.Trim();
			//    ship.PartsByLayer = new SortedList<int, List<PartDNA>>();
			//    for (int cntr = 0; cntr < _parts.Count; cntr++)
			//    {
			//        if (_parts[cntr].Count == 0)
			//        {
			//            continue;
			//        }

			//        ship.PartsByLayer.Add(cntr, _parts[cntr].Select(o => o.Part3D.GetDNA()).ToList());
			//    }
			//    //_parts.SelectMany(o => o).Select(o => o.Part3D.GetDNA()).ToList();


			//    //TODO: Store these in a database (RavenDB), fail over to storing on file if can't connect to DB


			//    //	Make sure the folder exists
			//    string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			//    foldername = System.IO.Path.Combine(foldername, "Asteroid Miner\\Ships");
			//    if (!Directory.Exists(foldername))
			//    {
			//        Directory.CreateDirectory(foldername);
			//    }

			//    //string xamlText = XamlWriter.Save(ship);
			//    //XamlServices.Load/Save in system.xaml.dll
			//    string xamlText = XamlServices.Save(ship);

			//    int infiniteLoopDetector = 0;
			//    while (true)
			//    {
			//        char[] illegalChars = System.IO.Path.GetInvalidFileNameChars();
			//        string filename = new string(ship.ShipName.Select(o => illegalChars.Contains(o) ? '_' : o).ToArray());
			//        filename = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff") + " - " + filename + ".xml";
			//        filename = System.IO.Path.Combine(foldername, filename);

			//        try
			//        {
			//            using (StreamWriter writer = new StreamWriter(new FileStream(filename, FileMode.CreateNew)))
			//            {
			//                writer.Write(xamlText);
			//                break;
			//            }
			//        }
			//        catch (IOException ex)
			//        {
			//            infiniteLoopDetector++;
			//            if (infiniteLoopDetector > 100)
			//            {
			//                throw new ApplicationException("Couldn't create the file\r\n" + filename, ex);
			//            }
			//        }
			//    }
			//}
			//catch (Exception ex)
			//{
			//    MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			//}
		}
		private void btnLoad_Click(object sender, RoutedEventArgs e)
		{
			//try
			//{
			//    //TODO: If there is a current scene, question if they want to save first (save would need to be moved into a method)

			//    string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			//    foldername = System.IO.Path.Combine(foldername, "Asteroid Miner\\Ships");

			//    if (!Directory.Exists(foldername) || Directory.GetFiles(foldername).Length == 0)
			//    {
			//        MessageBox.Show("No existing ships were found", _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
			//        return;
			//    }

			//    OpenFileDialog dialog = new OpenFileDialog();
			//    dialog.InitialDirectory = foldername;
			//    dialog.Multiselect = false;
			//    dialog.Title = "Please select a ship";
			//    bool? result = dialog.ShowDialog();
			//    if (result == null || !result.Value)
			//    {
			//        return;
			//    }

			//    //	Load the file
			//    object deserialized = null;
			//    using (FileStream file = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			//    {
			//        //deserialized = XamlReader.Load(file);
			//        deserialized = XamlServices.Load(file);
			//    }

			//    ShipDNA ship = deserialized as ShipDNA;

			//    if (ship == null)
			//    {
			//        MessageBox.Show("nooooooo");
			//    }
			//    else
			//    {
			//        MessageBox.Show("yes");
			//    }






			//    //TODO: Finish this

			//    ////	Wipe the current scene
			//    //DeleteParts(_parts.SelectMany(o => o).ToList());


			//    //	Rebuild the parts (need some kind of factory class that instantiates the right derived design class for each dna)


			//}
			//catch (Exception ex)
			//{
			//    MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			//}
		}

		#region TESTS

		private void btnSkewTest_Click(object sender, RoutedEventArgs e)
		{
			const double RANGE = 5d;

			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				Point3D point1 = new Point3D(-100, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
				Vector3D dir1 = new Vector3D(200, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
				Point3D point2 = new Point3D(-100, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
				Vector3D dir2 = new Vector3D(200, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));

				ScreenSpaceLines3D lines = new ScreenSpaceLines3D(true);
				lines.Color = Colors.Red;
				lines.Thickness = 2;
				lines.AddLine(point1, point1 + dir1);
				lines.AddLine(point2, point2 + dir2);

				//	Add debug visuals
				_debugVisuals.Add(lines);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


				double minDistance = Math3D.GetClosestDistanceBetweenLines(point1, dir1, point2, dir2);

				Point3D? resultSegmentPoint1, resultSegmentPoint2;
				if (Math3D.GetClosestPointsBetweenLines(out resultSegmentPoint1, out resultSegmentPoint2, point1, point1 + dir1, point2, point2 + dir2))
				{
					ScreenSpaceLines3D gapLine = new ScreenSpaceLines3D(true);
					gapLine.Color = Colors.Pink;
					gapLine.Thickness = 2;
					gapLine.AddLine(resultSegmentPoint1.Value, resultSegmentPoint2.Value);

					_debugVisuals.Add(gapLine);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnPlanes_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				TriangleIndexed plane1 = new TriangleIndexed(0, 1, 2, new Point3D[] {
					Math3D.GetRandomVectorSpherical(10).ToPoint(),
					Math3D.GetRandomVectorSpherical(10).ToPoint(),
					Math3D.GetRandomVectorSpherical(10).ToPoint() });

				TriangleIndexed plane2 = new TriangleIndexed(0, 1, 2, new Point3D[] {
					Math3D.GetRandomVectorSpherical(10).ToPoint(),
					Math3D.GetRandomVectorSpherical(10).ToPoint(),
					Math3D.GetRandomVectorSpherical(10).ToPoint() });

				//	Intersect Line
				Point3D resultPoint;
				Vector3D resultDirection;
				if (Math3D.GetIntersectingLine(out resultPoint, out resultDirection, plane1, plane2))
				{
					ScreenSpaceLines3D gapLine = new ScreenSpaceLines3D(true);
					gapLine.Color = Colors.White;
					gapLine.Thickness = 3;
					gapLine.AddLine(resultPoint, resultPoint + resultDirection * 100);
					gapLine.AddLine(resultPoint, resultPoint + resultDirection * -100);

					_debugVisuals.Add(gapLine);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
				}

				MaterialGroup material1 = new MaterialGroup();
				material1.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("18FF0000"))));
				material1.Children.Add(new SpecularMaterial(Brushes.Red, 85));

				MaterialGroup material2 = new MaterialGroup();
				material2.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("18FFFF00"))));
				material2.Children.Add(new SpecularMaterial(Brushes.Yellow, 85));

				Model3DGroup geometries = new Model3DGroup();

				//	Plane 1
				GeometryModel3D geometry = new GeometryModel3D();
				geometry.Material = material1;
				geometry.BackMaterial = material1;
				geometry.Geometry = UtilityWPF.GetSquare2D(100);

				Transform3DGroup transform = new Transform3DGroup();
				transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), plane1.Normal))));
				transform.Children.Add(new TranslateTransform3D(plane1.Point0.ToVector()));
				geometry.Transform = transform;

				geometries.Children.Add(geometry);

				//	Plane 2
				geometry = new GeometryModel3D();
				geometry.Material = material1;
				geometry.BackMaterial = material1;
				geometry.Geometry = UtilityWPF.GetSquare2D(100);

				transform = new Transform3DGroup();
				transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), plane2.Normal))));
				transform.Children.Add(new TranslateTransform3D(plane2.Point0.ToVector()));
				geometry.Transform = transform;

				geometries.Children.Add(geometry);

				////	Triangle 1
				//geometry = new GeometryModel3D();
				//geometry.Material = material2;
				//geometry.BackMaterial = material2;
				//geometry.Geometry = UtilityWPF.GetMeshFromTriangles(new TriangleIndexed[] { plane1 });

				//geometries.Children.Add(geometry);

				////	Triangle 2
				//geometry = new GeometryModel3D();
				//geometry.Material = material2;
				//geometry.BackMaterial = material2;
				//geometry.Geometry = UtilityWPF.GetMeshFromTriangles(new TriangleIndexed[] { plane2 });

				//geometries.Children.Add(geometry);

				//	Visual
				ModelVisual3D model = new ModelVisual3D();
				model.Content = geometries;

				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCircleLineTest_Click(object sender, RoutedEventArgs e)
		{
			const double RANGE = 5d;

			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				//	Line
				Point3D linePoint;
				Vector3D lineDir;
				if (StaticRandom.Next(2) == 0)
				{
					linePoint = new Point3D(-100, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
					lineDir = new Vector3D(200, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
				}
				else
				{
					linePoint = new Point3D(Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE), -100);
					lineDir = new Vector3D(Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE), 200);
				}
				//linePoint = new Point3D(0, 0, -100);
				//linePoint = new Point3D(Math3D.GetNearZeroValue(_rand, RANGE), Math3D.GetNearZeroValue(_rand, RANGE), -100);
				//lineDir = new Vector3D(0, 0, 200);
				//linePoint = new Point3D(-100, 0, Math3D.GetNearZeroValue(_rand, RANGE));
				//lineDir = new Vector3D(200, 0, 0);

				ScreenSpaceLines3D line = new ScreenSpaceLines3D(true);
				line.Color = Colors.Red;
				line.Thickness = 2;
				line.AddLine(linePoint, linePoint + lineDir);

				_debugVisuals.Add(line);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);

				//	Circle
				Point3D centerPoint = Math3D.GetRandomVectorSpherical(3d).ToPoint();
				Triangle circlePlane = new Triangle(centerPoint, Math3D.GetRandomVectorSpherical(3d).ToPoint(), Math3D.GetRandomVectorSpherical(3d).ToPoint());
				//Point3D centerPoint = new Point3D(0, 0, 0);
				//Triangle circlePlane = new Triangle(centerPoint, new Point3D(1, 0, 0), new Point3D(0, 1, 0));
				double radius = 2.1d + Math3D.GetNearZeroValue(2d);

				GeometryModel3D geometry = new GeometryModel3D();
				geometry.Material = new DiffuseMaterial(Brushes.Red);
				geometry.BackMaterial = new DiffuseMaterial(Brushes.Red);
				geometry.Geometry = UtilityWPF.GetRing(50, radius - .05, radius + .05, .1, null);

				Transform3DGroup transform = new Transform3DGroup();
				transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), circlePlane.Normal))));
				transform.Children.Add(new TranslateTransform3D(circlePlane.Point0.ToVector()));
				geometry.Transform = transform;

				ModelVisual3D model = new ModelVisual3D();
				model.Content = geometry;

				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);

				//	Closest points
				Point3D[] circlePoints, linePoints;
				if (Math3D.GetClosestPointsBetweenLineCircle(out circlePoints, out linePoints, circlePlane, centerPoint, radius, linePoint, lineDir, Math3D.RayCastReturn.AllPoints))
				{
					ScreenSpaceLines3D gapLine = new ScreenSpaceLines3D(true);
					gapLine.Color = Colors.Pink;
					gapLine.Thickness = 2;

					for (int cntr = 0; cntr < circlePoints.Length; cntr++)
					{
						gapLine.AddLine(circlePoints[cntr], linePoints[cntr]);
					}

					_debugVisuals.Add(gapLine);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCylinderLineTest_Click(object sender, RoutedEventArgs e)
		{
			const double RANGE = 5d;

			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				//	Line
				Point3D linePoint;
				Vector3D lineDir;
				if (StaticRandom.Next(2) == 0)
				{
					linePoint = new Point3D(-100, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
					lineDir = new Vector3D(200, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
				}
				else
				{
					linePoint = new Point3D(Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE), -100);
					lineDir = new Vector3D(Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE), 200);
				}

				ScreenSpaceLines3D line = new ScreenSpaceLines3D(true);
				line.Color = Colors.Red;
				line.Thickness = 2;
				line.AddLine(linePoint, linePoint + lineDir);

				_debugVisuals.Add(line);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);

				//	Cylinder
				Point3D axisPoint = Math3D.GetRandomVectorSpherical(3d).ToPoint();
				Vector3D axisDirection = Math3D.GetRandomVectorSpherical(3d);
				double radius = 2.1d + Math3D.GetNearZeroValue(2d);

				GeometryModel3D geometry = new GeometryModel3D();
				Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 255, 0, 0)));
				geometry.Material = material;
				geometry.BackMaterial = material;

				List<TubeRingBase> tubes = new List<TubeRingBase>();
				tubes.Add(new TubeRingRegularPolygon(0, false, radius, radius, false));
				tubes.Add(new TubeRingRegularPolygon(200, false, radius, radius, false));
				geometry.Geometry = UtilityWPF.GetMultiRingedTube(50, tubes, true, true);
				//geometry.Geometry = UtilityWPF.GetCylinder_AlongX(50, radius, 200);

				Transform3DGroup transform = new Transform3DGroup();
				//transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(1, 0, 0), axisDirection))));
				transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), axisDirection))));
				transform.Children.Add(new TranslateTransform3D(axisPoint.ToVector()));
				geometry.Transform = transform;

				ModelVisual3D cylinderModel = new ModelVisual3D();
				cylinderModel.Content = geometry;

				//	Add a few rings
				Model3DGroup ringModels = new Model3DGroup();
				for (double cntr = -100d; cntr <= 100d; cntr += 5)
				{
					geometry = new GeometryModel3D();
					geometry.Material = new DiffuseMaterial(Brushes.Maroon);
					geometry.BackMaterial = new DiffuseMaterial(Brushes.Maroon);
					geometry.Geometry = UtilityWPF.GetRing(50, radius - .005, radius + .005, .01, null);

					transform = new Transform3DGroup();
					transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, cntr)));
					transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), axisDirection))));
					transform.Children.Add(new TranslateTransform3D(axisPoint.ToVector()));
					geometry.Transform = transform;

					ringModels.Children.Add(geometry);
				}

				ModelVisual3D ringModel = new ModelVisual3D();
				ringModel.Content = ringModels;

				_debugVisuals.Add(ringModel);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);

				//	Closest points
				Point3D[] cylinderPoints, linePoints;
				//if (GetClosestPointsBetweenLineCylinder(out cylinderPoints, out linePoints, axisPoint, axisDirection, radius, linePoint, lineDir, false))
				if (Math3D.GetClosestPointsBetweenLineCylinder(out cylinderPoints, out linePoints, axisPoint, axisDirection, radius, linePoint, lineDir, Math3D.RayCastReturn.AllPoints))
				{
					ScreenSpaceLines3D gapLine = new ScreenSpaceLines3D(true);
					gapLine.Color = Colors.Pink;
					gapLine.Thickness = 2;

					for (int cntr = 0; cntr < cylinderPoints.Length; cntr++)
					{
						gapLine.AddLine(cylinderPoints[cntr], linePoints[cntr]);

						AddDebugDot(cylinderPoints[cntr], .05, Colors.Pink);
					}

					_debugVisuals.Add(gapLine);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
				}

				//	Add the cylinder last, since it's semitransparent
				_debugVisuals.Add(cylinderModel);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSphereLineTest_Click(object sender, RoutedEventArgs e)
		{
			const double RANGE = 5d;

			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				//	Line
				Point3D linePoint;
				Vector3D lineDir;
				if (StaticRandom.Next(2) == 0)
				{
					linePoint = new Point3D(-100, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
					lineDir = new Vector3D(200, Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE));
				}
				else
				{
					linePoint = new Point3D(Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE), -100);
					lineDir = new Vector3D(Math3D.GetNearZeroValue(RANGE), Math3D.GetNearZeroValue(RANGE), 200);
				}

				ScreenSpaceLines3D line = new ScreenSpaceLines3D(true);
				line.Color = Colors.Red;
				line.Thickness = 2;
				line.AddLine(linePoint, linePoint + lineDir);

				_debugVisuals.Add(line);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);

				//	Sphere
				Point3D centerPoint = Math3D.GetRandomVectorSpherical(3d).ToPoint();
				double radius = 2.1d + Math3D.GetNearZeroValue(2d);

				GeometryModel3D geometry = new GeometryModel3D();
				Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(32, 255, 0, 0)));
				geometry.Material = material;
				geometry.BackMaterial = material;
				geometry.Geometry = UtilityWPF.GetSphere(30, radius);

				Transform3DGroup transform = new Transform3DGroup();
				transform.Children.Add(new TranslateTransform3D(centerPoint.ToVector()));
				geometry.Transform = transform;

				ModelVisual3D sphereModel = new ModelVisual3D();
				sphereModel.Content = geometry;

				//	Closest points
				Point3D[] spherePoints, linePoints;
				Math3D.GetClosestPointsBetweenLineSphere(out spherePoints, out linePoints, centerPoint, radius, linePoint, lineDir, Math3D.RayCastReturn.AllPoints);

				ScreenSpaceLines3D gapLine = new ScreenSpaceLines3D(true);
				gapLine.Color = Colors.Pink;
				gapLine.Thickness = 2;

				for (int cntr = 0; cntr < spherePoints.Length; cntr++)
				{
					gapLine.AddLine(spherePoints[cntr], linePoints[cntr]);

					AddDebugDot(spherePoints[cntr], .05, Colors.Pink);
				}

				_debugVisuals.Add(gapLine);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


				//	Add the sphere last, since it's semitransparent
				_debugVisuals.Add(sphereModel);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnCylinderTest_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				for (int cntr = 3; cntr < 25; cntr++)
				{

					//	Material
					MaterialGroup material = new MaterialGroup();
					material.Children.Add(new DiffuseMaterial(Brushes.Yellow));
					material.Children.Add(new SpecularMaterial(Brushes.Maroon, 50d));

					//	Geometry Model
					GeometryModel3D geometry = new GeometryModel3D();
					geometry.Material = material;
					geometry.BackMaterial = material;
					//geometry.Geometry = UtilityWPF.GetCylinder_AlongX(cntr, 1, .75);
					geometry.Geometry = UtilityWPF.GetCone_AlongX(cntr, 1, 1);

					//	Model
					ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
					model.Content = geometry;
					model.Transform = new TranslateTransform3D((cntr * 10) - 120, 0, 0);

					//	Add debug visuals
					_debugVisuals.Add(model);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnHalfDomeTest_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				for (int cntr = 3; cntr < 20; cntr++)
				{
					//	Material
					MaterialGroup material = new MaterialGroup();
					material.Children.Add(new DiffuseMaterial(Brushes.Yellow));
					material.Children.Add(new SpecularMaterial(Brushes.Maroon, 50d));

					//	Geometry Model
					GeometryModel3D geometry = new GeometryModel3D();
					geometry.Material = material;
					geometry.BackMaterial = material;
					//geometry.Geometry = UtilityWPF.GetHalfDome1(cntr, 5, 5, 5);
					geometry.Geometry = UtilityWPF.GetCapsule_AlongZ(cntr, cntr, 4, 20);

					//	Model
					ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
					model.Content = geometry;
					model.Transform = new TranslateTransform3D((cntr * 15) - 150, 0, 0);

					//	Add debug visuals
					_debugVisuals.Add(model);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


					//	Lines
					//ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
					//lines.Color = UtilityWPF.AlphaBlend(Colors.Chocolate, Colors.Black, .4d);
					//lines.Thickness = 2;
					//foreach (var triangle in UtilityWPF.GetTrianglesFromMesh((MeshGeometry3D)geometry.Geometry))
					//{
					//    lines.AddLine(triangle.Point0, triangle.Point1);
					//    lines.AddLine(triangle.Point1, triangle.Point2);
					//    lines.AddLine(triangle.Point2, triangle.Point0);
					//}
					//lines.Transform = model.Transform;
					//_debugVisuals.Add(lines);
					//_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThrusterTest_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				for (int cntr = 3; cntr < 20; cntr++)
				{
					//	Material
					MaterialGroup material = new MaterialGroup();
					material.Children.Add(new DiffuseMaterial(Brushes.Yellow));
					material.Children.Add(new SpecularMaterial(Brushes.Maroon, 50d));

					//	Geometry Model
					GeometryModel3D geometry = new GeometryModel3D();
					geometry.Material = material;
					geometry.BackMaterial = material;


					List<TubeRingBase> rings = new List<TubeRingBase>();
					//rings.Add(new TubeRingDome(-1, false, 10));
					//rings.Add(new TubeRingPoint(-1, false));
					rings.Add(new TubeRingRegularPolygon(-1, false, 1, 1, false));
					rings.Add(new TubeRingRegularPolygon(2, false, 3, 3, false));
					rings.Add(new TubeRingRegularPolygon(3, false, 5, 5, false));
					rings.Add(new TubeRingRegularPolygon(1, false, 6, 6, false));
					rings.Add(new TubeRingRegularPolygon(2, false, 5.5, 5.5, false));
					//rings.Add(new TubeRingDome(4, false, 10));
					//rings.Add(new TubeRingPoint(4, false));
					rings.Add(new TubeRingRegularPolygon(4, false, 2, 2, false));

					geometry.Geometry = UtilityWPF.GetMultiRingedTube(cntr, rings, true, true);



					//	Model
					ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
					model.Content = geometry;
					model.Transform = new TranslateTransform3D((cntr * 15) - 150, 0, 0);

					//	Add debug visuals
					_debugVisuals.Add(model);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


					//	Lines
					ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
					lines.Color = UtilityWPF.AlphaBlend(Colors.Chocolate, Colors.Black, .4d);
					lines.Thickness = 2;
					foreach (var triangle in UtilityWPF.GetTrianglesFromMesh((MeshGeometry3D)geometry.Geometry))
					{
						lines.AddLine(triangle.Point0, triangle.Point1);
						lines.AddLine(triangle.Point1, triangle.Point2);
						lines.AddLine(triangle.Point2, triangle.Point0);
					}
					lines.Transform = model.Transform;
					_debugVisuals.Add(lines);
					_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Thruster)));
				//material.Children.Add(_colors.ThrusterSpecular);
				//geometry.Material = material;

				//material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.ThrusterBack)));
				//geometry.BackMaterial = material;

				//var rings = GetThrusterRings1Full();
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				#endregion

				ThrusterDesign thruster = new ThrusterDesign(_options, ThrusterType.One);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = thruster.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


				////	Lines
				//ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
				//lines.Color = UtilityWPF.AlphaBlend(Colors.Chocolate, Colors.Black, .4d);
				//lines.Thickness = 2;
				//foreach (var triangle in UtilityWPF.GetTrianglesFromMesh((MeshGeometry3D)geometry.Geometry))
				//{
				//    lines.AddLine(triangle.Point0, triangle.Point1);
				//    lines.AddLine(triangle.Point1, triangle.Point2);
				//    lines.AddLine(triangle.Point2, triangle.Point0);
				//}
				//lines.Transform = model.Transform;
				//_debugVisuals.Add(lines);
				//_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster2_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Thruster)));
				//material.Children.Add(_colors.ThrusterSpecular);
				//geometry.Material = material;

				//material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.ThrusterBack)));
				//geometry.BackMaterial = material;

				//List<TubeRingBase> rings = new List<TubeRingBase>();
				//rings.Add(new TubeRingRegularPolygon(-1, false, .25, .25, false));
				//rings.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
				//rings.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
				//rings.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
				//rings.Add(new TubeRingRegularPolygon(1.5, false, 1, 1, false));
				//rings.Add(new TubeRingRegularPolygon(.5, false, .9, .9, false));
				//rings.Add(new TubeRingRegularPolygon(1.2, false, .25, .25, false));

				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				#endregion

				ThrusterDesign thruster = new ThrusterDesign(_options, ThrusterType.Two);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = thruster.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


				////	Lines
				//ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
				//lines.Color = UtilityWPF.AlphaBlend(Colors.Chocolate, Colors.Black, .4d);
				//lines.Thickness = 2;
				//foreach (var triangle in UtilityWPF.GetTrianglesFromMesh((MeshGeometry3D)geometry.Geometry))
				//{
				//    lines.AddLine(triangle.Point0, triangle.Point1);
				//    lines.AddLine(triangle.Point1, triangle.Point2);
				//    lines.AddLine(triangle.Point2, triangle.Point0);
				//}
				//lines.Transform = model.Transform;
				//_debugVisuals.Add(lines);
				//_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster2_1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				//Model3DGroup geometries = new Model3DGroup();

				////	Geometry Model1
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Thruster)));
				//material.Children.Add(_colors.ThrusterSpecular);
				//geometry.Material = material;
				//var material1 = material;

				//material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.ThrusterBack)));
				//geometry.BackMaterial = material;
				//var material2 = material;

				//var rings = GetThrusterRings2Full();
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				//geometries.Children.Add(geometry);

				////	Geometry Model2
				//geometry = new GeometryModel3D();
				//geometry.Material = material1;
				//geometry.BackMaterial = material2;

				//rings = GetThrusterRings1Half(false);
				//Transform3DGroup transform = new Transform3DGroup();
				//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
				//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
				//transform.Children.Add(new TranslateTransform3D(0, -1 * rings.Sum(o => o.DistFromPrevRing), 0));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, false, transform);

				//geometries.Children.Add(geometry);

				#endregion

				ThrusterDesign thruster = new ThrusterDesign(_options, ThrusterType.Two_One);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = thruster.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


				////	Lines
				//ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
				//lines.Color = UtilityWPF.AlphaBlend(Colors.Chocolate, Colors.Black, .4d);
				//lines.Thickness = 2;
				//foreach (var triangle in UtilityWPF.GetTrianglesFromMesh((MeshGeometry3D)geometry.Geometry))
				//{
				//    lines.AddLine(triangle.Point0, triangle.Point1);
				//    lines.AddLine(triangle.Point1, triangle.Point2);
				//    lines.AddLine(triangle.Point2, triangle.Point0);
				//}
				//lines.Transform = model.Transform;
				//_debugVisuals.Add(lines);
				//_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster2_2_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				//Model3DGroup geometries = new Model3DGroup();

				////	Geometry Model1
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Thruster)));
				//material.Children.Add(_colors.ThrusterSpecular);
				//geometry.Material = material;
				//var material1 = material;

				//material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.ThrusterBack)));
				//geometry.BackMaterial = material;
				//var material2 = material;

				//var rings = GetThrusterRings2Full();
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				//geometries.Children.Add(geometry);

				////	Geometry Model2
				//geometry = new GeometryModel3D();
				//geometry.Material = material1;
				//geometry.BackMaterial = material2;

				//Transform3DGroup transform = new Transform3DGroup();
				//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true, transform);

				//geometries.Children.Add(geometry);

				#endregion

				ThrusterDesign thruster = new ThrusterDesign(_options, ThrusterType.Two_Two);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = thruster.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster2_2_1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				//Model3DGroup geometries = new Model3DGroup();

				////	Geometry Model1
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Thruster)));
				//material.Children.Add(_colors.ThrusterSpecular);
				//geometry.Material = material;
				//var material1 = material;

				//material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.ThrusterBack)));
				//geometry.BackMaterial = material;
				//var material2 = material;

				//var rings = GetThrusterRings2Full();
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				//geometries.Children.Add(geometry);

				////	Geometry Model2
				//geometry = new GeometryModel3D();
				//geometry.Material = material1;
				//geometry.BackMaterial = material2;

				//Transform3DGroup transform = new Transform3DGroup();
				//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true, transform);

				//geometries.Children.Add(geometry);

				////	Geometry Model3
				//geometry = new GeometryModel3D();
				//geometry.Material = material1;
				//geometry.BackMaterial = material2;

				//rings = GetThrusterRings1Half(false);
				//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
				//transform.Children.Add(new TranslateTransform3D(0, -1 * rings.Sum(o => o.DistFromPrevRing), 0));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, false, transform);

				//geometries.Children.Add(geometry);

				#endregion

				ThrusterDesign thruster = new ThrusterDesign(_options, ThrusterType.Two_Two_One);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = thruster.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


				////	Lines
				//ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
				//lines.Color = UtilityWPF.AlphaBlend(Colors.Chocolate, Colors.Black, .4d);
				//lines.Thickness = 2;
				//foreach (var triangle in UtilityWPF.GetTrianglesFromMesh((MeshGeometry3D)geometry.Geometry))
				//{
				//    lines.AddLine(triangle.Point0, triangle.Point1);
				//    lines.AddLine(triangle.Point1, triangle.Point2);
				//    lines.AddLine(triangle.Point2, triangle.Point0);
				//}
				//lines.Transform = model.Transform;
				//_debugVisuals.Add(lines);
				//_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster2_2_2_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				//Model3DGroup geometries = new Model3DGroup();

				////	Geometry Model1
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.Thruster)));
				//material.Children.Add(_colors.ThrusterSpecular);
				//geometry.Material = material;
				//var material1 = material;

				//material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.ThrusterBack)));
				//geometry.BackMaterial = material;
				//var material2 = material;

				//var rings = GetThrusterRings2Full();
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				//geometries.Children.Add(geometry);

				////	Geometry Model2
				//geometry = new GeometryModel3D();
				//geometry.Material = material1;
				//geometry.BackMaterial = material2;

				//Transform3DGroup transform = new Transform3DGroup();
				//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true, transform);

				//geometries.Children.Add(geometry);

				////	Geometry Model3
				//geometry = new GeometryModel3D();
				//geometry.Material = material1;
				//geometry.BackMaterial = material2;

				//transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true, transform);

				//geometries.Children.Add(geometry);

				#endregion

				ThrusterDesign thruster = new ThrusterDesign(_options, ThrusterType.Two_Two_Two);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = thruster.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);


				////	Lines
				//ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
				//lines.Color = UtilityWPF.AlphaBlend(Colors.Chocolate, Colors.Black, .4d);
				//lines.Thickness = 2;
				//foreach (var triangle in UtilityWPF.GetTrianglesFromMesh((MeshGeometry3D)geometry.Geometry))
				//{
				//    lines.AddLine(triangle.Point0, triangle.Point1);
				//    lines.AddLine(triangle.Point1, triangle.Point2);
				//    lines.AddLine(triangle.Point2, triangle.Point0);
				//}
				//lines.Transform = model.Transform;
				//_debugVisuals.Add(lines);
				//_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnFuelTank_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Material
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.FuelTank)));
				//material.Children.Add(_colors.FuelTankSpecular);

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//geometry.Material = material;
				//geometry.BackMaterial = material;
				//geometry.Geometry = UtilityWPF.GetCapsule_AlongZ(20, 6, 1, 4);

				#endregion

				FuelTankDesign fuelTank = new FuelTankDesign(_options);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = fuelTank.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnEnergyTank_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.EnergyTank)));
				//material.Children.Add(_colors.EnergyTankSpecular);
				//material.Children.Add(_colors.EnergyTankEmissive);
				//geometry.Material = material;
				//geometry.BackMaterial = material;

				//List<TubeRingBase> rings = new List<TubeRingBase>();
				//rings.Add(new TubeRingDome(0, false, 5));
				//rings.Add(new TubeRingRegularPolygon(.4, false, 1, 1, false));
				//rings.Add(new TubeRingRegularPolygon(2.5, false, 1, 1, false));
				//rings.Add(new TubeRingDome(.4, false, 5));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				#endregion

				EnergyTankDesign energyTank = new EnergyTankDesign(_options);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = energyTank.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCargoBay1_1_1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Material
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.CargoBay)));
				//material.Children.Add(_colors.CargoBaySpecular);

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//geometry.Material = material;
				//geometry.BackMaterial = material;
				//geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-.5, -.5, -.5), new Point3D(.5, .5, .5));

				#endregion

				CargoBayDesign cargoBay = new CargoBayDesign(_options);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = cargoBay.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCargoBay2_1_1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Material
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.CargoBay)));
				//material.Children.Add(_colors.CargoBaySpecular);

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//geometry.Material = material;
				//geometry.BackMaterial = material;
				//geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-1, -.5, -.5), new Point3D(1, .5, .5));

				#endregion

				CargoBayDesign cargoBay = new CargoBayDesign(_options);
				cargoBay.Scale = new Vector3D(2, 1, 1);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = cargoBay.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCargoBay2_2_1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Material
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.CargoBay)));
				//material.Children.Add(_colors.CargoBaySpecular);

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//geometry.Material = material;
				//geometry.BackMaterial = material;
				//geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-1, -1, -.5), new Point3D(1, 1, .5));

				#endregion

				CargoBayDesign cargoBay = new CargoBayDesign(_options);
				cargoBay.Scale = new Vector3D(2, 2, 1);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = cargoBay.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCargoBay3_2_1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Material
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.CargoBay)));
				//material.Children.Add(_colors.CargoBaySpecular);

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//geometry.Material = material;
				//geometry.BackMaterial = material;
				//geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-1.5, -1, -.5), new Point3D(1.5, 1, .5));

				#endregion

				CargoBayDesign cargoBay = new CargoBayDesign(_options);
				cargoBay.Scale = new Vector3D(3, 2, 1);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = cargoBay.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCargoBay3_3_1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				////	Material
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.CargoBay)));
				//material.Children.Add(_colors.CargoBaySpecular);

				////	Geometry Model
				//GeometryModel3D geometry = new GeometryModel3D();
				//geometry.Material = material;
				//geometry.BackMaterial = material;
				//geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-1.5, -1.5, -.5), new Point3D(1.5, 1.5, .5));

				#endregion

				CargoBayDesign cargoBay = new CargoBayDesign(_options);
				cargoBay.Scale = new Vector3D(3, 3, 1);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = cargoBay.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnTractorBeam_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Clear debug visuals
				foreach (var visual in _debugVisuals)
				{
					_viewport.Children.Remove(visual);
				}
				_debugVisuals.Clear();

				#region OLD

				//Model3DGroup geometries = new Model3DGroup();

				////	Geometry Model1
				//GeometryModel3D geometry = new GeometryModel3D();
				//MaterialGroup material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.TractorBeamBase)));
				////material.Children.Add(new SpecularMaterial(Brushes.Silver, 75));
				//geometry.Material = material;
				//geometry.BackMaterial = material;

				//List<TubeRingBase> rings = new List<TubeRingBase>();
				//rings.Add(new TubeRingPoint(0, false));
				//rings.Add(new TubeRingRegularPolygon(.3, false, .5, .5, false));
				//rings.Add(new TubeRingRegularPolygon(2, false, 1, 1, false));
				//rings.Add(new TubeRingDome(.66, false, 5));
				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true);

				//geometries.Children.Add(geometry);

				////	Geometry Model2
				//geometry = new GeometryModel3D();
				//material = new MaterialGroup();
				//material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_colors.TractorBeamRod)));
				//material.Children.Add(_colors.TractorBeamRodSpecular);
				//material.Children.Add(_colors.TractorBeamRodEmissive);
				//geometry.Material = material;
				//geometry.BackMaterial = material;

				//rings = new List<TubeRingBase>();
				//rings.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
				//rings.Add(new TubeRingRegularPolygon(1.5, false, .25, .25, false));
				//rings.Add(new TubeRingDome(1, false, 4));

				//geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, false);

				//geometries.Children.Add(geometry);

				#endregion

				TractorBeamDesign tractor = new TractorBeamDesign(_options);

				//	Model
				ModelVisual3D model = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
				model.Content = tractor.Model;

				//	Add debug visuals
				_debugVisuals.Add(model);
				_viewport.Children.Add(_debugVisuals[_debugVisuals.Count - 1]);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), _msgboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#region OLD

		//private void btnDragTest_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		//{
		//    // Store the mouse position
		//    _toolDragStart = e.GetPosition(null);
		//}
		//private void btnDragTest_PreviewMouseMove(object sender, MouseEventArgs e)
		//{
		//    // Get the current mouse position
		//    Point mousePos = e.GetPosition(null);
		//    Vector diff = _toolDragStart - mousePos;

		//    if (e.LeftButton == MouseButtonState.Pressed && (
		//        Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
		//        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
		//    {

		//        //TODO:  Get the part and its corresponding model

		//        #region WPF Object

		//        //	Material
		//        MaterialGroup material = new MaterialGroup();
		//        material.Children.Add(new DiffuseMaterial(Brushes.Red));
		//        //material.Children.Add(_colors.CompassRoseSpecular);

		//        //	Geometry Model
		//        GeometryModel3D geometry = new GeometryModel3D();
		//        geometry.Material = material;
		//        geometry.BackMaterial = material;
		//        geometry.Geometry = UtilityWPF.GetSphere(10, 1, 1, 1);

		//        //transforms = new Transform3DGroup();
		//        //transforms.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, -8)));
		//        //geometry.Transform = transforms;

		//        ModelVisual3D model = new ModelVisual3D();
		//        model.Content = geometry;

		//        #endregion

		//        //	Store this so mouse move can drag this around
		//        _draggingDropObject = new DraggingDropObject();
		//        _draggingDropObject.Model = model;

		//        // Initialize the drag & drop operation
		//        DataObject dragData = new DataObject(DATAFORMAT_PART, _partsToolbox[0]);
		//        if (DragDrop.DoDragDrop(btnDragTest, dragData, DragDropEffects.Move) == DragDropEffects.None)
		//        {
		//            //	If they drag around, but not in a place where Drop gets fired, then execution reenters here.
		//            //	Get rid of the dragging part
		//            if (_draggingDropObject != null && _draggingDropObject.HasAddedToViewport)
		//            {
		//                _viewport.Children.Remove(_draggingDropObject.Model);
		//            }

		//            _draggingDropObject = null;
		//        }
		//    }
		//}
		//private void btnDragTest_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		//{
		//    //http://blogs.msdn.com/b/jaimer/archive/2007/07/12/drag-drop-in-wpf-explained-end-to-end.aspx

		//    //	There's no need to change the icon, it looks fine as is
		//}

		#endregion
		#region OLD (line/cylinder intersect)

		//private bool GetClosestPointsBetweenLineCylinder(out Point3D[] cylinderPoints, out Point3D[] linePoints, Point3D pointOnAxis, Vector3D axisDirection, double radius, Point3D pointOnLine, Vector3D lineDirection, bool onlyReturnSinglePoint)
		//{
		//    //	Get the shortest point between the cylinder's axis and the line
		//    Point3D? nearestAxisPoint, nearestLinePoint;
		//    if (!Math3D.GetClosestPointsBetweenLines(out nearestAxisPoint, out nearestLinePoint, pointOnAxis, axisDirection, pointOnLine, lineDirection))
		//    {
		//        //	The axis and line are parallel
		//        cylinderPoints = null;
		//        linePoints = null;
		//        return false;
		//    }

		//    Vector3D nearestLine = nearestLinePoint.Value - nearestAxisPoint.Value;
		//    double nearestDistance = nearestLine.Length;

		//    if (nearestDistance < radius)
		//    {
		//        ////TODO: Finish this
		//        //cylinderPoints = null;
		//        //linePoints = null;
		//        //return false;

		//        CylinderLineArgs args = new CylinderLineArgs()
		//        {
		//            PointOnAxis = pointOnAxis,
		//            AxisDirection = axisDirection,
		//            Radius = radius,
		//            PointOnLine = pointOnLine,
		//            LineDirection = lineDirection
		//        };

		//        return GetClosestPointsBetweenLineCylinderSprtInside3(out cylinderPoints, out linePoints, args, nearestAxisPoint.Value, nearestLinePoint.Value, nearestLine, nearestDistance);
		//    }
		//    else
		//    {
		//        //	Sitting outside the cylinder, so just project the line to the cylinder wall
		//        cylinderPoints = new Point3D[] { nearestAxisPoint.Value + (nearestLine.ToUnit() * radius) };
		//        linePoints = new Point3D[] { nearestLinePoint.Value };
		//        return true;
		//    }
		//}

		//private struct CylinderLineArgs
		//{
		//    public Point3D PointOnAxis;
		//    public Vector3D AxisDirection;
		//    public double Radius;
		//    public Point3D PointOnLine;
		//    public Vector3D LineDirection;
		//}

		//private struct CylinderCirclePlaneIntersectProps
		//{
		//    //	These are the planes that intersected
		//    public ITriangle SlicePlane;		//	this one doesn't need to be here, it can stay local to the method it's created in
		//    public ITriangle CirclePlane;

		//    //	This is the line that the planes intersect along
		//    public Point3D PointOnLine;
		//    public Vector3D LineDirection;

		//    //	This is a line from the circle's center to the intersect line
		//    public Point3D NearestToCenter;
		//    public Vector3D CenterToNearest;
		//    public double CenterToNearestLength;
		//}

		//private bool GetClosestPointsBetweenLineCylinderSprtInside1(out Point3D[] cylinderPoints, out Point3D[] linePoints, Point3D pointOnAxis, Vector3D axisDirection, double radius, Point3D pointOnLine, Vector3D lineDirection, bool onlyReturnSinglePoint)
		//{
		//    //	The slice plane runs parallel to the cylinder's axis
		//    Triangle slicePlane = new Triangle(pointOnLine, pointOnLine + lineDirection, pointOnLine + axisDirection);

		//    Vector3D circlePlaneLine1 = Math3D.GetArbitraryOrhonganal(axisDirection);
		//    Vector3D circlePlaneLine2 = Vector3D.CrossProduct(axisDirection, circlePlaneLine1);
		//    Triangle circlePlane = new Triangle(pointOnAxis, pointOnAxis + circlePlaneLine1, pointOnAxis + circlePlaneLine2);

		//    //	Use that slice plane to project the line onto the circle's plane
		//    Point3D intersectPoint;
		//    Vector3D intersectDir;
		//    if (!Math3D.GetIntersectingLine(out intersectPoint, out intersectDir, circlePlane, slicePlane))
		//    {
		//        throw new ApplicationException("The slice plane should never be parallel to the circle's plane");		//	it was defined as perpendicular
		//    }


		//    throw new ApplicationException("ummmmmmmmm.....");



		//}
		//private bool GetClosestPointsBetweenLineCylinderSprtInside2(out Point3D[] cylinderPoints, out Point3D[] linePoints, Point3D pointOnAxis, Vector3D axisDirection, double radius, Point3D pointOnLine, Vector3D lineDirection)
		//{
		//    //	Make a transform to project the cylinder axis along z, centered at x,y = 0
		//    Transform3DGroup transform = new Transform3DGroup();
		//    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(axisDirection, new Vector3D(0, 0, 1)))));
		//    transform.Children.Add(new TranslateTransform3D(-pointOnAxis.ToVector()));

		//    //	Transform the line into cylinder coords
		//    Point3D pointOnLineLocal = transform.Transform(pointOnLine);
		//    Vector3D lineDirectionLocal = transform.Transform(lineDirection);

		//    //	Project this line onto the circle's plane
		//    Triangle slicePlane = new Triangle(pointOnLineLocal, pointOnLineLocal + lineDirectionLocal, pointOnLineLocal + new Vector3D(0, 0, 1));		//	this plane runs in the direction of the line, and parallel to the cylinder axis (in cylinder coords)
		//    Triangle circlePlane = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0));		//	this plane is the xy plane through the origin

		//    Point3D intersectPoint;
		//    Vector3D intersectDir;
		//    if (!Math3D.GetIntersectingLine(out intersectPoint, out intersectDir, circlePlane, slicePlane))
		//    {
		//        throw new ApplicationException("The slice plane should never be parallel to the circle's plane");		//	it was defined as perpendicular
		//    }


		//    throw new ApplicationException("stuck again......");


		//}

		//private bool GetClosestPointsBetweenLineCylinderSprtInside3(out Point3D[] cylinderPoints, out Point3D[] linePoints, CylinderLineArgs args, Point3D nearestAxisPoint, Point3D nearestLinePoint, Vector3D nearestLine, double nearestLineDistance)
		//{
		//    //	Create new args so that the plane goes through the closest points
		//    CylinderLineArgs newArgs = new CylinderLineArgs()
		//    {
		//        PointOnAxis = nearestAxisPoint,
		//        AxisDirection = args.AxisDirection,
		//        Radius = args.Radius,
		//        PointOnLine = nearestLinePoint,
		//        LineDirection = args.LineDirection
		//    };

		//    //	Bundle up some more loose variables into a structure
		//    CylinderCirclePlaneIntersectProps intersectArgs = Inside3SprtIntersectArgs(newArgs, nearestAxisPoint, nearestLinePoint, nearestLine, nearestLineDistance);

		//    //	Now get the circle intersects
		//    Point3D[] circlePoints2D, linePoints2D;
		//    Inside3SprtInsidePerps(out circlePoints2D, out linePoints2D, newArgs, intersectArgs);

		//    //	Project the circle hits onto the original line
		//    Point3D? p1, p2, p3, p4;
		//    Math3D.GetClosestPointsBetweenLines(out p1, out p2, args.PointOnLine, args.LineDirection, circlePoints2D[0], args.AxisDirection);
		//    Math3D.GetClosestPointsBetweenLines(out p3, out p4, args.PointOnLine, args.LineDirection, circlePoints2D[1], args.AxisDirection);

		//    //	p1 and p2 are the same, p3 and p4 are the same
		//    cylinderPoints = new Point3D[] { p1.Value, p3.Value };
		//    linePoints = cylinderPoints;
		//    return true;
		//}
		//private static CylinderCirclePlaneIntersectProps Inside3SprtIntersectArgs(CylinderLineArgs args, Point3D nearestAxisPoint, Point3D nearestLinePoint, Vector3D nearestLine, double nearestLineDistance)
		//{
		//    CylinderCirclePlaneIntersectProps retVal;

		//    //	The slice plane runs perpendicular to the circle's plane
		//    retVal.SlicePlane = new Triangle(args.PointOnLine, args.PointOnLine + args.LineDirection, args.PointOnLine + args.AxisDirection);

		//    //	Make a plane that the circle sits in
		//    Vector3D circlePlaneLine1 = Math3D.GetArbitraryOrhonganal(args.AxisDirection);
		//    Vector3D circlePlaneLine2 = Vector3D.CrossProduct(args.AxisDirection, circlePlaneLine1);
		//    retVal.CirclePlane = new Triangle(args.PointOnAxis, args.PointOnAxis + circlePlaneLine1, args.PointOnAxis + circlePlaneLine2);

		//    //	Use that slice plane to project the line onto the circle's plane
		//    if (!Math3D.GetIntersectingLine(out retVal.PointOnLine, out retVal.LineDirection, retVal.CirclePlane, retVal.SlicePlane))
		//    {
		//        throw new ApplicationException("The slice plane should never be parallel to the circle's plane");		//	it was defined as perpendicular
		//    }

		//    retVal.NearestToCenter = nearestLinePoint;
		//    retVal.CenterToNearest = nearestLine;
		//    retVal.CenterToNearestLength = nearestLineDistance;

		//    return retVal;
		//}

		//private static void Inside3SprtInsidePerps(out Point3D[] circlePoints, out Point3D[] linePoints, CylinderLineArgs args, CylinderCirclePlaneIntersectProps planeIntersect)
		//{
		//    //	See if the line passes through the center
		//    if (Math3D.IsNearZero(planeIntersect.CenterToNearestLength))
		//    {
		//        //Vector3D lineDirUnit = args.LineDirection.ToUnit();
		//        Vector3D lineDirUnit = planeIntersect.LineDirection.ToUnit();

		//        //	The line passes through the circle's center, so the nearest points will shoot straight from the center in the direction of the line
		//        circlePoints = new Point3D[2];
		//        circlePoints[0] = args.PointOnAxis + (lineDirUnit * args.Radius);
		//        circlePoints[1] = args.PointOnAxis - (lineDirUnit * args.Radius);
		//    }
		//    else
		//    {
		//        //	The two points are perpendicular to this line.  Use A^2 + B^2 = C^2 to get the length of the perpendiculars
		//        double perpLength = Math.Sqrt((args.Radius * args.Radius) - (planeIntersect.CenterToNearestLength * planeIntersect.CenterToNearestLength));
		//        Vector3D perpDirection = Vector3D.CrossProduct(planeIntersect.CenterToNearest, args.AxisDirection).ToUnit();

		//        circlePoints = new Point3D[2];
		//        circlePoints[0] = planeIntersect.NearestToCenter + (perpDirection * perpLength);
		//        circlePoints[1] = planeIntersect.NearestToCenter - (perpDirection * perpLength);
		//    }

		//    //	Get corresponding points along the line
		//    linePoints = new Point3D[2];
		//    linePoints[0] = Math3D.GetNearestPointAlongLine(args.PointOnLine, args.LineDirection, circlePoints[0]);
		//    linePoints[1] = Math3D.GetNearestPointAlongLine(args.PointOnLine, args.LineDirection, circlePoints[1]);
		//}

		#endregion

		#endregion

		#endregion

		#region Private Methods

		private void ClearSelectedParts()
		{
			HideGuideLines();

			if (_selectedParts != null)
			{
				//	Wipe out any selection visuals
				_selectedParts.Clear();
			}

			_selectedParts = null;
		}
		private void DeleteSelectedParts()
		{
			if (_selectedParts == null)
			{
				return;
			}

			//	Remember the parts
			List<DesignPart> parts = new List<DesignPart>(_selectedParts.GetParts());

			//	Let the clear method do its thing
			ClearSelectedParts();

			//	Now delete the parts
			DeleteParts(parts);
		}
		private void LockUnlockSelectedParts()
		{
			if (_selectedParts == null)
			{
				return;
			}

			//	Flip their state
			_selectedParts.IsLocked = !_selectedParts.IsLocked;

			//	Store this in the undo list (they need to be broken out by layer)
			SortedList<int, List<DesignPart>> partsByLayer = GetPartsByLayer(_selectedParts.GetParts().ToList());

			List<UndoRedoLockUnlock> undoRedos = new List<UndoRedoLockUnlock>();
			foreach (int layerIndex in partsByLayer.Keys)
			{
				undoRedos.AddRange(partsByLayer[layerIndex].Select(o => new UndoRedoLockUnlock(o.Part3D.Token, _selectedParts.IsLocked, layerIndex)));
			}

			AddNewUndoRedoItem(undoRedos.ToArray());
		}

		private void ShowGuideLines(bool allLayers)
		{
			for (int cntr = 0; cntr < _parts.Count; cntr++)
			{
				if (allLayers || cntr == _currentLayerIndex)
				{
					foreach (DesignPart part in _parts[cntr])
					{
						//	Wipe out the existing guidelines, because they may now be a different color
						if (part.GuideLines != null && _viewport.Children.Contains(part.GuideLines))
						{
							_viewport.Children.Remove(part.GuideLines);
						}
						part.GuideLines = null;

						//	Make new guidelines
						part.CreateGuildLines();
						_viewport.Children.Add(part.GuideLines);
					}
				}
			}
		}
		private void HideGuideLines()
		{
			foreach (DesignPart part in _parts.SelectMany(o => o))
			{
				if (part.GuideLines != null)
				{
					_viewport.Children.Remove(part.GuideLines);
					part.GuideLines = null;
				}
			}
		}

		private void DeleteParts(List<DesignPart> parts)
		{
			List<UndoRedoAddRemove> undoItems = new List<UndoRedoAddRemove>();

			SortedList<int, List<DesignPart>> partsByLayer = GetPartsByLayer(parts);

			foreach (int layerIndex in partsByLayer.Keys)
			{
				foreach (DesignPart part in partsByLayer[layerIndex])
				{
					//	Make sure no extra visuals are showing
					if (part.GuideLines != null)
					{
						_viewport.Children.Remove(part.GuideLines);
						part.GuideLines = null;
					}

					//	Remove the part
					if (_viewport.Children.Contains(part.Model))		//	if the layer is invisible, the model will already be removed
					{
						_viewport.Children.Remove(part.Model);
					}

					_parts[layerIndex].Remove(part);

					//	Build undo item
					undoItems.Add(new UndoRedoAddRemove(false, part, layerIndex));
				}
			}

			//	Store the undo items
			AddNewUndoRedoItem(undoItems.ToArray());
		}

		private void SelectAllParts()
		{
			ClearSelectedParts();		//	doesn't matter if there was a selection or not.  A new one is needed

			if (!_isAllLayers && (_parts[_currentLayerIndex].Count == 0 || !((LayerRow)pnlLayers.Children[_currentLayerIndex + 1]).IsLayerVisible))
			{
				//	There are no parts in this layer
				return;
			}

			//	Create a selection, and put the hit parts in it
			_selectedParts = new SelectedParts(_viewport, _camera, _options);

			if (_isAllLayers)
			{
				_selectedParts.AddRange(_parts.SelectMany(o => o));
			}
			else
			{
				_selectedParts.AddRange(_parts[_currentLayerIndex]);
			}
		}

		private SortedList<int, List<DesignPart>> GetPartsByLayer(List<DesignPart> parts)
		{
			SortedList<int, List<DesignPart>> retVal = new SortedList<int, List<DesignPart>>();

			for (int cntr = 0; cntr < _parts.Count; cntr++)
			{
				var matches = _parts[cntr].Where(o => parts.Contains(o));
				if (matches.Count() > 0)
				{
					retVal.Add(cntr, matches.ToList());
				}
			}

			return retVal;
		}

		private void SwapLayers(List<string> newLayerNames)
		{
			List<UndoRedoLayerAddRemove> undoLayers = new List<UndoRedoLayerAddRemove>();

			//	Remove existing layers
			for (int cntr = pnlLayers.Children.Count - 2; cntr >= 0; cntr--)
			{
				LayerRow layer = (LayerRow)pnlLayers.Children[cntr + 1];		//	adding one, because AllLayers is at zero
				layer.LayerVisibilityChanged -= new EventHandler(Layer_LayerVisibilityChanged);
				layer.GotFocus -= new RoutedEventHandler(LayerRow_GotFocus);

				pnlLayers.Children.RemoveAt(cntr + 1);
				undoLayers.Add(new UndoRedoLayerAddRemove(cntr, false, layer.LayerName));
			}

			//	Add new layers
			for (int cntr = 0; cntr < newLayerNames.Count; cntr++)
			{
				LayerRow layer = new LayerRow(this.Options.EditorColors);
				layer.LayerName = newLayerNames[cntr];
				layer.IsLayerVisible = true;
				layer.LayerVisibilityChanged += new EventHandler(Layer_LayerVisibilityChanged);
				layer.GotFocus += new RoutedEventHandler(LayerRow_GotFocus);

				pnlLayers.Children.Add(layer);

				undoLayers.Add(new UndoRedoLayerAddRemove(cntr, true, layer.LayerName));
			}

			undoLayers.Reverse();		//	they need to be undone in opossite order
			AddNewUndoRedoItem(undoLayers.ToArray());
		}

		private void ChangeLayer(int index, bool forceVisible)
		{
			ClearSelectedParts();

			if (index < 0)		//	-1 is all layers, in that case, leave the current layer alone (so if they create a new part, it will go to that layer)
			{
				_isAllLayers = true;
			}
			else
			{
				_isAllLayers = false;
				_currentLayerIndex = index;
			}

			for (int cntr = 0; cntr < pnlLayers.Children.Count; cntr++)
			{
				LayerRow row = (LayerRow)pnlLayers.Children[cntr];

				bool isActiveLayer = cntr == index + 1;

				row.IsSelected = isActiveLayer;

				if (cntr > 0)
				{
					foreach (DesignPart part in _parts[cntr - 1])
					{
						part.Part3D.IsActiveLayer = _isAllLayers || isActiveLayer;
					}
				}

				if (_isAllLayers || (isActiveLayer && forceVisible))
				{
					row.IsLayerVisible = true;
				}
			}
		}

		private void DragItem(Point clickPoint)
		{
			if (chkDragParts.IsChecked.Value)
			{
				//TODO: Call CastRay to see where they clicked, then either use the existing _dragHitShape, or make a new local one for the part
				//	they are currently over
				throw new ApplicationException("finish this");
			}
			else
			{
				RayHitTestParameters mouseRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);
				Point3D? hitPoint = _dragHitShape.CastRay(_dragMouseDownClickRay, _dragMouseDownCenterRay, mouseRay, _camera, _viewport);

				if (hitPoint != null)
				{
					_selectedParts.Position = hitPoint.Value;

					Quaternion? rotationDelta = _dragHitShape.GetRotation(_mouseDownDragPos, hitPoint.Value);
					if (rotationDelta != null)
					{
						//Quaternion newRotation = Quaternion.Multiply(_mouseDownDragOrientation, rotationDelta.Value.ToUnit());
						Quaternion newRotation = Quaternion.Multiply(rotationDelta.Value.ToUnit(), _mouseDownDragOrientation);
						_selectedParts.Orientation = newRotation;
					}
				}
			}
		}

		private void ChangeDragHitShape()
		{
			Point3D point;
			if (_selectedParts == null)
			{
				point = new Point3D(0, 0, 0);
			}
			else
			{
				point = _selectedParts.Position;
			}

			if (radDragPlaneOrth.IsChecked.Value)
			{
				#region Orth Plane

				_dragHitShapeType = DragHitShapeType.Plane_Orth;

				if (_isShiftDown)
				{
					if (_isCapsLockDown)
					{
						RayHitTestParameters[] lines = new RayHitTestParameters[2];
						lines[0] = new RayHitTestParameters(point, new Vector3D(1, 0, 0));
						lines[1] = new RayHitTestParameters(point, new Vector3D(0, 1, 0));
						_dragHitShape.SetShape_Lines(lines);
					}
					else
					{
						_dragHitShape.SetShape_Plane(new Triangle(point, new Point3D(point.X + 1d, point.Y, point.Z), new Point3D(point.X, point.Y + 1d, point.Z)));
					}
				}
				else
				{
					if (_isCapsLockDown)
					{
						RayHitTestParameters[] lines = new RayHitTestParameters[2];
						lines[0] = new RayHitTestParameters(point, new Vector3D(1, 0, 0));
						lines[1] = new RayHitTestParameters(point, new Vector3D(0, 0, 1));
						_dragHitShape.SetShape_Lines(lines);
					}
					else
					{
						_dragHitShape.SetShape_Plane(new Triangle(point, new Point3D(point.X + 1d, point.Y, point.Z), new Point3D(point.X, point.Y, point.Z + 1d)));
					}
				}

				#endregion
			}
			else if (radDragPlaneCamera.IsChecked.Value)
			{
				#region Camera Plane

				_dragHitShapeType = DragHitShapeType.Plane_Camera;

				RayHitTestParameters cameraLookCenter = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth * .5d, _viewport.ActualHeight * .5d));

				//	Come up with the right plane
				Vector3D standard = Math3D.GetArbitraryOrhonganal(cameraLookCenter.Direction);
				Vector3D orth = Vector3D.CrossProduct(standard, cameraLookCenter.Direction);
				ITriangle plane = new Triangle(point, point + standard, point + orth);

				if (_isCapsLockDown)
				{
					//	Since they are locked onto lines, choose lines that go up/down, and left/right based on how they are looking
					RayHitTestParameters cameraLookUp = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth * .5d, _viewport.ActualHeight * .25d));
					RayHitTestParameters cameraLookRight = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth * .75d, _viewport.ActualHeight * .5d));

					Point3D? cameraCenterPoint = Math3D.GetIntersectionOfPlaneAndLine(plane, cameraLookCenter.Origin, cameraLookCenter.Direction);
					Point3D? cameraUpPoint = Math3D.GetIntersectionOfPlaneAndLine(plane, cameraLookUp.Origin, cameraLookUp.Direction);
					Point3D? cameraRightPoint = Math3D.GetIntersectionOfPlaneAndLine(plane, cameraLookRight.Origin, cameraLookRight.Direction);

					if (cameraCenterPoint == null || cameraUpPoint == null || cameraRightPoint == null)
					{
						_dragHitShape.SetShape_None();		//	this should never happen, since the rays are perpendicular to the plane
					}
					else
					{
						RayHitTestParameters[] lines = new RayHitTestParameters[2];
						lines[0] = new RayHitTestParameters(point, cameraUpPoint.Value - cameraCenterPoint.Value);
						lines[1] = new RayHitTestParameters(point, cameraRightPoint.Value - cameraCenterPoint.Value);
						_dragHitShape.SetShape_Lines(lines);
					}
				}
				else
				{
					_dragHitShape.SetShape_Plane(plane);
				}

				#endregion
			}
			else if (radDragCylinderX.IsChecked.Value || radDragCylinderY.IsChecked.Value || radDragCylinderZ.IsChecked.Value)
			{
				#region Cylinder

				//	Figure out what direction the cylinder should be
				Vector3D direction;
				if (radDragCylinderX.IsChecked.Value)
				{
					_dragHitShapeType = DragHitShapeType.Cylinder_X;
					direction = new Vector3D(1, 0, 0);
				}
				else if (radDragCylinderY.IsChecked.Value)
				{
					_dragHitShapeType = DragHitShapeType.Cylinder_Y;
					direction = new Vector3D(0, 1, 0);
				}
				else if (radDragCylinderZ.IsChecked.Value)
				{
					_dragHitShapeType = DragHitShapeType.Cylinder_Z;
					direction = new Vector3D(0, 0, 1);
				}
				else
				{
					throw new ApplicationException();
				}

				if (_selectedParts == null)
				{
					//	There is no offset from the center axis, so just drag along the axis
					_dragHitShape.SetShape_Line(new RayHitTestParameters(new Point3D(0, 0, 0), direction));
				}
				else
				{
					//	Project the click point
					Point3D axisIntersect = Math3D.GetNearestPointAlongLine(new Point3D(0, 0, 0), direction, point);

					if (Math3D.IsNearZero(point - axisIntersect))
					{
						//	The object is on the axis line, so just drag along that line (cylinder has no radius, and plane only has one dimension defined)
						_dragHitShape.SetShape_Line(new RayHitTestParameters(point, direction));
					}
					else
					{
						if (_isShiftDown)
						{
							#region Plane/Line

							if (_isCapsLockDown)
							{
								RayHitTestParameters[] lines = new RayHitTestParameters[2];
								lines[0] = new RayHitTestParameters(point, direction);
								lines[1] = new RayHitTestParameters(point, point - axisIntersect);
								_dragHitShape.SetShape_Lines(lines);
							}
							else
							{
								_dragHitShape.SetShape_Plane(new Triangle(axisIntersect, axisIntersect + direction, point));
							}

							#endregion
						}
						else
						{
							#region Cylinder/Circle,Line

							double radius = (point - axisIntersect).Length;

							if (_isCapsLockDown)
							{
								Triangle circlePlane = new Triangle(axisIntersect, point, axisIntersect + Vector3D.CrossProduct(direction, point - axisIntersect));
								DragHitShape.CircleDefinition circle = new DragHitShape.CircleDefinition(circlePlane, axisIntersect, radius);

								RayHitTestParameters line = new RayHitTestParameters(point, direction);

								_dragHitShape.SetShape_LinesCircles(new RayHitTestParameters[] { line }, new DragHitShape.CircleDefinition[] { circle });
							}
							else
							{
								_dragHitShape.SetShape_Cylinder(new RayHitTestParameters(axisIntersect, direction), radius);		//	need to use axisIntersect, because if the user is looking along the direction of the axis, then _dragHitShape will constrain to a circle around that point
							}

							#endregion
						}
					}
				}

				#endregion
			}
			else if (radDragSphere.IsChecked.Value)
			{
				#region Sphere

				_dragHitShapeType = DragHitShapeType.Sphere;

				if (_selectedParts == null || Math3D.IsNearZero(point))
				{
					_dragHitShape.SetShape_None();
				}
				else
				{
					if (_isShiftDown || _isCapsLockDown)		//	since it's a single line anyway, just make both do the same thing
					{
						_dragHitShape.SetShape_Line(new RayHitTestParameters(point, point.ToVector()));
					}
					else
					{
						_dragHitShape.SetShape_Sphere(new Point3D(0, 0, 0), point.ToVector().Length);
					}
				}

				#endregion
			}
			else if (radDragCircleX.IsChecked.Value || radDragCircleY.IsChecked.Value || radDragCircleZ.IsChecked.Value)
			{
				#region Circle

				//	Figure out what direction the cylinder should be
				Vector3D direction;
				if (radDragCircleX.IsChecked.Value)
				{
					_dragHitShapeType = DragHitShapeType.Circle_X;
					direction = new Vector3D(1, 0, 0);
				}
				else if (radDragCircleY.IsChecked.Value)
				{
					_dragHitShapeType = DragHitShapeType.Circle_Y;
					direction = new Vector3D(0, 1, 0);
				}
				else if (radDragCircleZ.IsChecked.Value)
				{
					_dragHitShapeType = DragHitShapeType.Circle_Z;
					direction = new Vector3D(0, 0, 1);
				}
				else
				{
					throw new ApplicationException();
				}

				if (_selectedParts == null)
				{
					//	There is no offset from the center axis, so do nothing
					_dragHitShape.SetShape_None();
				}
				else
				{
					//	Project the click point
					Point3D axisIntersect = Math3D.GetNearestPointAlongLine(new Point3D(0, 0, 0), direction, point);

					if (Math3D.IsNearZero(point - axisIntersect))
					{
						//	The object is on the axis line, so can't do anything
						_dragHitShape.SetShape_None();
					}
					else
					{
						#region Circle

						double radius = (point - axisIntersect).Length;
						Triangle circlePlane = new Triangle(axisIntersect, point, axisIntersect + Vector3D.CrossProduct(direction, point - axisIntersect));
						_dragHitShape.SetShape_Circle(circlePlane, axisIntersect, radius);

						#endregion
					}
				}

				#endregion
			}
			else
			{
				//	This should never happen
				_dragHitShape.SetShape_None();
			}
		}

		/// <summary>
		/// This will cast a ray from the point (on _viewport) along the direction that the camera is looking, and returns hits
		/// against wpf visuals and the drag shape (sorted by distance from camera)
		/// </summary>
		/// <remarks>
		/// This only looks at the click ray, and won't do anything with offsets (that's what the DragHitShape class is for).  This
		/// method is useful for seeing where they clicked, then take further action from there
		/// </remarks>
		private List<MyHitTestResult> CastRay(out RayHitTestParameters clickRay, Point clickPoint, IEnumerable<Visual3D> ignoreVisuals, bool returnAllHits)
		{
			List<MyHitTestResult> retVal = new List<MyHitTestResult>();

			//	This gets called every time there is a hit
			HitTestResultCallback resultCallback = delegate(HitTestResult result)
				{
					if (result is RayMeshGeometry3DHitTestResult)		//	It could also be a RayHitTestResult, which isn't as exact as RayMeshGeometry3DHitTestResult
					{
						RayMeshGeometry3DHitTestResult resultCast = (RayMeshGeometry3DHitTestResult)result;
						if (ignoreVisuals == null || !ignoreVisuals.Any(o => o == resultCast.VisualHit))
						{
							retVal.Add(new MyHitTestResult() { ModelHit = resultCast });

							if (!returnAllHits)
							{
								return HitTestResultBehavior.Stop;
							}
						}
					}

					return HitTestResultBehavior.Continue;
				};

			//	Get hits against existing models
			VisualTreeHelper.HitTest(grdViewPort, null, resultCallback, new PointHitTestParameters(clickPoint));

			//	Get a hit against the drag shape
			clickRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);

			Point3D? shapeHit = _dragHitShape.CastRay(clickRay);
			if (shapeHit != null)
			{
				retVal.Add(new MyHitTestResult() { DragShapeHit = shapeHit.Value });
			}

			//	Sort by distance
			if (retVal.Count > 1)
			{
				Point3D clickRayOrigin = clickRay.Origin;		//	the compiler complains about anonymous methods using out params
				retVal = retVal.OrderBy(o => o.GetDistanceFromPoint(clickRayOrigin)).ToList();
			}

			//	Exit Function
			return retVal;
		}

		private void AddNewUndoRedoItem(UndoRedoBase[] items)
		{
			//	Remove everything in the list above the current point
			if (_undoRedoIndex < _undoRedo.Count - 1)
			{
				_undoRedo.RemoveRange(_undoRedoIndex + 1, _undoRedo.Count - _undoRedoIndex - 1);
			}

			_undoRedo.Add(items);
			_undoRedoIndex = _undoRedo.Count - 1;

			btnUndo.IsEnabled = true;
			btnRedo.IsEnabled = false;
		}
		private void Undo()
		{
			if (_undoRedoIndex < 0)
			{
				//	There is nothing left to undo
				return;
			}

			//	Make sure there is nothing selected
			ClearSelectedParts();

			bool isAllLayers = _undoRedo[_undoRedoIndex].GroupBy(o => o.LayerIndex).Count() > 1;

			//	Figure out what changed
			//	The undo/redo item at _undoRedoIndex holds what change was made to get to this point
			if (_undoRedo[_undoRedoIndex] is UndoRedoTransformChange[])
			{
				#region Parts Moved

				if (isAllLayers)
				{
					ChangeLayer(-1, true);
				}

				foreach (UndoRedoTransformChange change in _undoRedo[_undoRedoIndex])
				{
					if (!isAllLayers)
					{
						ChangeLayer(change.LayerIndex, true);
					}

					//	Search backward through the undo/redo list for the last position
					UndoRedoTransformChange prevPos = null;
					for (int cntr = _undoRedoIndex - 1; cntr >= 0; cntr--)
					{
						var matches = _undoRedo[cntr].Where(o => o.Token == change.Token);
						if (matches.Count() > 0)
						{
							UndoRedoBase match = matches.First();		//	there should never be more than one

							if (match is UndoRedoTransformChange)
							{
								prevPos = (UndoRedoTransformChange)match;
							}
							else if (match is UndoRedoAddRemove)
							{
								UndoRedoAddRemove matchCast = (UndoRedoAddRemove)match;
								prevPos = new UndoRedoTransformChange(match.Token, matchCast.LayerIndex)
									{
										Orientation = matchCast.Part.Part3D.Orientation,
										Position = matchCast.Part.Part3D.Position,
										Scale = matchCast.Part.Part3D.Scale
									};
							}

							//NOTE: There is no else here, because there are other types of undo objects, but they aren't related to movement

							break;
						}
					}

					if (prevPos == null)
					{
						//TODO: Support undo/redo of insert/delete
						throw new ApplicationException("Couldn't find previous position for token: " + change.Token.ToString());
					}

					//	Find the part
					DesignPart part = _parts.SelectMany(o => o.Where(p => p.Part3D.Token == change.Token)).FirstOrDefault();
					if (part == null)
					{
						throw new ApplicationException("Should have found the part: " + change.Token.ToString());
					}

					//	Set the new position
					part.Part3D.SetTransform(prevPos.Scale, prevPos.Position, prevPos.Orientation);
				}

				#endregion
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoAddRemove[] && ((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex])[0].IsAdd)
			{
				//	Parts Added
				UndoRedoSprtRemove((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex], isAllLayers);		//	undoing an add is a remove
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoAddRemove[] && !((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex])[0].IsAdd)
			{
				//	Parts Removed
				UndoRedoSprtAdd((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex], isAllLayers);		//	undoing a remove is an add
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoLockUnlock[])
			{
				#region Lock/Unlock

				if (isAllLayers)
				{
					ChangeLayer(-1, true);
				}

				foreach (UndoRedoLockUnlock change in _undoRedo[_undoRedoIndex])
				{
					if (!isAllLayers)
					{
						ChangeLayer(change.LayerIndex, true);
					}

					DesignPart part = _parts.SelectMany(o => o.Where(p => p.Part3D.Token == change.Token)).FirstOrDefault();
					if (part == null)
					{
						throw new ApplicationException("Couldn't find part: " + change.Token.ToString());
					}
					part.Part3D.IsLocked = !change.IsLock;
				}

				#endregion
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoLayerAddRemove[])
			{
				#region Layers

				foreach (UndoRedoLayerAddRemove undoLayer in _undoRedo[_undoRedoIndex])
				{
					if (undoLayer.IsAdd)
					{
						//	Layer Added
						UndoRedoSprtLayerRemove(new UndoRedoLayerAddRemove[] { undoLayer });		//	undoing an add is a remove
					}
					else
					{
						//	Layer Removed
						UndoRedoSprtLayerAdd(new UndoRedoLayerAddRemove[] { undoLayer });		//	undoing a remove is an add
					}
				}

				#endregion
			}
			else
			{
				throw new ApplicationException("Unknown type of undo/redo item");
			}

			_undoRedoIndex--;

			btnUndo.IsEnabled = _undoRedoIndex >= 0;
			btnRedo.IsEnabled = true;
		}
		private void Redo()
		{
			if (_undoRedoIndex >= _undoRedo.Count - 1)
			{
				//	There is nothing left to redo
				return;
			}

			//	Make sure there is nothing selected
			ClearSelectedParts();

			_undoRedoIndex++;

			bool isAllLayers = _undoRedo[_undoRedoIndex].GroupBy(o => o.LayerIndex).Count() > 1;

			//	Figure out what changed
			if (_undoRedo[_undoRedoIndex] is UndoRedoTransformChange[])
			{
				#region Parts Moved

				if (isAllLayers)
				{
					ChangeLayer(-1, true);
				}

				foreach (UndoRedoTransformChange change in (UndoRedoTransformChange[])_undoRedo[_undoRedoIndex])
				{
					if (!isAllLayers)
					{
						ChangeLayer(change.LayerIndex, true);
					}

					DesignPart part = _parts.SelectMany(o => o.Where(p => p.Part3D.Token == change.Token)).FirstOrDefault();
					if (part == null)
					{
						throw new ApplicationException("Couldn't find part: " + change.Token.ToString());
					}

					part.Part3D.SetTransform(change.Scale, change.Position, change.Orientation);
				}

				#endregion
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoAddRemove[] && ((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex])[0].IsAdd)
			{
				//	Parts Added
				UndoRedoSprtAdd((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex], isAllLayers);
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoAddRemove[] && !((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex])[0].IsAdd)
			{
				//	Parts Removed
				UndoRedoSprtRemove((UndoRedoAddRemove[])_undoRedo[_undoRedoIndex], isAllLayers);
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoLockUnlock[])
			{
				#region Lock/Unlock

				if (isAllLayers)
				{
					ChangeLayer(-1, true);
				}

				foreach (UndoRedoLockUnlock change in _undoRedo[_undoRedoIndex])
				{
					if (!isAllLayers)
					{
						ChangeLayer(change.LayerIndex, true);
					}

					DesignPart part = _parts.SelectMany(o => o.Where(p => p.Part3D.Token == change.Token)).FirstOrDefault();
					if (part == null)
					{
						throw new ApplicationException("Couldn't find part: " + change.Token.ToString());
					}
					part.Part3D.IsLocked = change.IsLock;
				}

				#endregion
			}
			else if (_undoRedo[_undoRedoIndex] is UndoRedoLayerAddRemove[])
			{
				#region Layers

				foreach (UndoRedoLayerAddRemove undoLayer in _undoRedo[_undoRedoIndex].Reverse())
				{
					if (undoLayer.IsAdd)
					{
						//	Layer Added
						UndoRedoSprtLayerAdd(new UndoRedoLayerAddRemove[] { undoLayer });
					}
					else
					{
						//	Layer Removed
						UndoRedoSprtLayerRemove(new UndoRedoLayerAddRemove[] { undoLayer });
					}
				}

				#endregion
			}
			else
			{
				throw new ApplicationException("Unknown type of undo/redo item");
			}

			btnUndo.IsEnabled = true;
			btnRedo.IsEnabled = _undoRedoIndex < _undoRedo.Count - 1;
		}
		private void UndoRedoSprtAdd(UndoRedoAddRemove[] items, bool isAllLayers)
		{
			if (isAllLayers)
			{
				ChangeLayer(-1, true);
			}

			foreach (UndoRedoAddRemove item in items)
			{
				if (!isAllLayers)
				{
					ChangeLayer(item.LayerIndex, true);
				}

				DesignPart clonedPart = item.Part.Clone();
				clonedPart.Part3D.Token = item.Token;

				_viewport.Children.Add(clonedPart.Model);
				_parts[item.LayerIndex].Add(clonedPart);
			}
		}
		private void UndoRedoSprtRemove(UndoRedoAddRemove[] items, bool isAllLayers)
		{
			if (isAllLayers)
			{
				ChangeLayer(-1, true);
			}

			foreach (UndoRedoAddRemove item in items)
			{
				if (!isAllLayers)
				{
					ChangeLayer(item.LayerIndex, true);
				}

				//	Find the part
				DesignPart actualPart = _parts.SelectMany(o => o.Where(p => p.Part3D.Token == item.Token)).FirstOrDefault();
				if (actualPart == null)
				{
					throw new ApplicationException("Couldn't find part: " + actualPart.ToString());
				}

				//	Remove the part
				if (_viewport.Children.Contains(actualPart.Model))		//	the layer could be invisible, so the model may not be there
				{
					_viewport.Children.Remove(actualPart.Model);		//	I can't remove change.Part.Model, because that is a clone
				}
				_parts.Single(o => o.Contains(actualPart)).Remove(actualPart);
			}
		}
		private void UndoRedoSprtLayerAdd(UndoRedoLayerAddRemove[] items)
		{
			foreach (UndoRedoLayerAddRemove item in items)
			{
				LayerRow layer = new LayerRow(_options.EditorColors);
				//layer.LayerName = "Layer " + (item.LayerIndex + 1).ToString();
				layer.LayerName = item.Name;
				layer.IsLayerVisible = true;

				layer.LayerVisibilityChanged += new EventHandler(Layer_LayerVisibilityChanged);
				layer.GotFocus += new RoutedEventHandler(LayerRow_GotFocus);

				pnlLayers.Children.Insert(item.LayerIndex + 1, layer);		//	row at zero is AllLayers, so it's shifted by one
				_parts.Insert(item.LayerIndex, new List<DesignPart>());

				ChangeLayer(item.LayerIndex, true);
			}
		}
		private void UndoRedoSprtLayerRemove(UndoRedoLayerAddRemove[] items)
		{
			foreach (UndoRedoLayerAddRemove item in items)
			{
				if (_parts[item.LayerIndex].Count > 0)
				{
					throw new ApplicationException("There shouldn't be any items on the current layer: " + item.LayerIndex.ToString());
				}

				//	Remove it
				LayerRow layer = (LayerRow)pnlLayers.Children[item.LayerIndex];
				layer.LayerVisibilityChanged -= new EventHandler(Layer_LayerVisibilityChanged);
				layer.GotFocus -= new RoutedEventHandler(LayerRow_GotFocus);

				pnlLayers.Children.RemoveAt(item.LayerIndex + 1);		//	row at zero is AllLayers, so it's shifted by one
				_parts.RemoveAt(item.LayerIndex);

				//	Change layers
				int newIndex = item.LayerIndex;
				if (newIndex >= _parts.Count)
				{
					newIndex = _parts.Count - 1;
				}

				ChangeLayer(newIndex, false);
			}
		}

		private void Cut()
		{
			if (_selectedParts == null)
			{
				return;
			}

			_clipboard = _selectedParts.CloneParts().ToArray();
			DeleteSelectedParts();
		}
		private void Copy()
		{
			if (_selectedParts == null)
			{
				return;
			}

			_clipboard = _selectedParts.CloneParts().ToArray();
		}
		private void Paste()
		{
			if (_clipboard == null)
			{
				return;
			}

			ClearSelectedParts();

			List<DesignPart> pastedParts = new List<DesignPart>();
			List<UndoRedoAddRemove> undoItems = new List<UndoRedoAddRemove>();

			//	Paste the parts
			foreach (DesignPart clipPart in _clipboard)
			{
				//	Clone it
				DesignPart part = clipPart.Clone();

				//	Add it
				_viewport.Children.Add(part.Model);
				_parts[_currentLayerIndex].Add(part);

				//	Remember this part
				undoItems.Add(new UndoRedoAddRemove(true, part, _currentLayerIndex));
				pastedParts.Add(part);
			}

			//	Store the undo items
			AddNewUndoRedoItem(undoItems.ToArray());

			//	Select the newly pasted parts
			_selectedParts = new SelectedParts(_viewport, _camera, _options);
			_selectedParts.AddRange(pastedParts);

			if (chkShowGuideLines.IsChecked.Value)
			{
				ShowGuideLines(chkShowGuideLinesAllLayers.IsChecked.Value);
			}
		}

		private ModelVisual3D GetCompassRose(EditorColors colors)
		{
			Model3DGroup models = new Model3DGroup();

			#region Ring

			//	Material
			MaterialGroup material = new MaterialGroup();
			material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_options.EditorColors.CompassRoseColor)));
			material.Children.Add(_options.EditorColors.CompassRoseSpecular);

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = material;
			geometry.BackMaterial = material;
			geometry.Geometry = UtilityWPF.GetTorus(100, 20, .12, 10);

			Transform3DGroup transforms = new Transform3DGroup();
			transforms.Children.Add(new ScaleTransform3D(1d, 1d, .16d));
			transforms.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, -8)));
			geometry.Transform = transforms;

			models.Children.Add(geometry);

			#endregion
			#region Arrow 1

			//	Material
			material = new MaterialGroup();
			material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_options.EditorColors.CompassRoseColor)));
			material.Children.Add(_options.EditorColors.CompassRoseSpecular);

			//	Geometry Model
			geometry = new GeometryModel3D();
			geometry.Material = material;
			geometry.BackMaterial = material;
			geometry.Geometry = UtilityWPF.GetCone_AlongX(20, .96, 2d);

			transforms = new Transform3DGroup();
			transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
			transforms.Children.Add(new ScaleTransform3D(1d, 1d, .02d));
			transforms.Children.Add(new TranslateTransform3D(new Vector3D(-11, 0, -8)));
			geometry.Transform = transforms;

			models.Children.Add(geometry);

			#endregion
			#region Arrow 2

			//	Material
			material = new MaterialGroup();
			material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_options.EditorColors.CompassRoseColor)));
			material.Children.Add(_options.EditorColors.CompassRoseSpecular);

			//	Geometry Model
			geometry = new GeometryModel3D();
			geometry.Material = material;
			geometry.BackMaterial = material;
			geometry.Geometry = UtilityWPF.GetCone_AlongX(20, .48, 1.5d);

			transforms = new Transform3DGroup();
			transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
			transforms.Children.Add(new ScaleTransform3D(1d, 1d, .04d));
			transforms.Children.Add(new TranslateTransform3D(new Vector3D(9.25, 0, -8)));
			geometry.Transform = transforms;

			models.Children.Add(geometry);

			#endregion
			#region Origin Dot

			//	Material
			material = new MaterialGroup();
			material.Children.Add(new DiffuseMaterial(new SolidColorBrush(_options.EditorColors.CompassRoseColor)));
			material.Children.Add(_options.EditorColors.CompassRoseSpecular);

			//	Geometry Model
			geometry = new GeometryModel3D();
			geometry.Material = material;
			geometry.BackMaterial = material;
			geometry.Geometry = UtilityWPF.GetSphere(20, .08, .08, .08);

			models.Children.Add(geometry);

			#endregion

			ModelVisual3D retVal = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
			retVal.Content = models;

			//	Exit Function
			return retVal;
		}

		private ModelVisual3D GetDebugVisual(MyHitTestResult modelHit)
		{
			//	Material
			MaterialGroup material = new MaterialGroup();
			material.Children.Add(new DiffuseMaterial(Brushes.Yellow));
			material.Children.Add(new SpecularMaterial(Brushes.Maroon, 50d));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = material;
			geometry.BackMaterial = material;
			geometry.Geometry = UtilityWPF.GetSphere(5, .15, .15, .15);

			//	Model
			ModelVisual3D retVal = new ModelVisual3D();        // this is the expensive one, so as few of these should be made as possible
			retVal.Content = geometry;
			retVal.Transform = new TranslateTransform3D(modelHit.Point.ToVector());

			//	Exit Function
			return retVal;
		}

		private PartToolItemBase FindPart(DependencyObject visual)
		{
			if (visual == null || visual is TabItem || visual is TabControl)
			{
				return null;
			}

			//	See if this is one of the toolbox item's visuals
			var matches = _partsToolbox.Where(o => o.Visual2D == visual);
			if (matches.Count() == 1)		//	the count should never be greater than one
			{
				return matches.First();
			}

			//	Recurse
			return FindPart(VisualTreeHelper.GetParent(visual));
		}

		private void AddDebugDot(Point3D position, double radius, Color color)
		{
			//	Material
			MaterialGroup materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
			materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;
			geometry.Geometry = UtilityWPF.GetSphere(3, radius, radius, radius);

			//	Model Visual
			ModelVisual3D model = new ModelVisual3D();
			model.Content = geometry;
			model.Transform = new TranslateTransform3D(position.ToVector());

			//	Add to the viewport
			_debugVisuals.Add(model);
			_viewport.Children.Add(model);
		}

		#region DRAGITEM_OLD

		///// <summary>
		///// This is called when the user is dragging a part around (either during initial drag/drop, or dragging a selected part)
		///// </summary>
		//private Point3D DragItem(Point3D position, IEnumerable<ModelVisual3D> models, Point clickPoint, IEnumerable<Visual3D> ignoreVisuals)
		//{
		//    #region OLD

		//    ////TODO:  Cast ray should only do a plane check if there isn't a model hit
		//    ////TODO:  Only need to return the first hit 

		//    //List<Visual3D> allIgnores = new List<Visual3D>();
		//    //if (ignoreVisuals != null)
		//    //{
		//    //    allIgnores.AddRange(ignoreVisuals);
		//    //}
		//    //allIgnores.Add(model);

		//    ////	Do a hit test for 3D objects under the mouse
		//    //List<MyHitTestResult> hits = CastRay(clickPoint, allIgnores, true);

		//    //Point3D planeHitPoint = hits.Where(o => o.PlaneHit != null).First().Point;

		//    ////NOTE: _camera.LookDirection and _camera.UpDirection are really screwed up (I think that trackball messed them up), so fire a ray instead
		//    ////	I'm not using the mouse click point, because that can change as they drag, and the inconsistency would be jarring
		//    //RayHitTestParameters cameraLook = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth / 2d, _viewport.ActualHeight / 2d));

		//    //double dot = Vector3D.DotProduct(_plane.Normal.ToUnit(), cameraLook.Direction.ToUnit());
		//    //if (Math.Abs(dot) < .1)
		//    //{
		//    //    //	They are looking along the plane, so snap to a line instead of a plane
		//    //    Vector3D? snapLine = DragItemSprtGetSnapLine(cameraLook.Direction);		//	the returned vector is used like 3 bools, with only one axis set to true

		//    //    if (snapLine != null)
		//    //    {
		//    //        //	I can't use the plane hit results (because the farther the mouse is from the plane, the farther the ray will travel before it hits the plane, so the
		//    //        //	object still drifts funny).  Instead, do a line-line compare
		//    //        //part.Position = new Point3D(
		//    //        //    snapLine.Value.X == 0 ? part.Position.X : planeHitPoint.X,
		//    //        //    snapLine.Value.Y == 0 ? part.Position.Y : planeHitPoint.Y,
		//    //        //    snapLine.Value.Z == 0 ? part.Position.Z : planeHitPoint.Z);

		//    //        RayHitTestParameters mouseRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);

		//    //        Point3D? lineHitPoint, dummy1;
		//    //        if (Math3D.GetClosestPointsBetweenLines(out lineHitPoint, out dummy1, part.Position, snapLine.Value, mouseRay.Origin, mouseRay.Direction))
		//    //        {
		//    //            part.Position = lineHitPoint.Value;
		//    //        }
		//    //    }
		//    //}
		//    //else
		//    //{
		//    //    part.Position = planeHitPoint;
		//    //}


		//    ////part.Position = hits.Where(o => o.PlaneHit != null).First().Point;		//TODO: Give the user the option to always snap to the plane
		//    ////part.Position = hits[0].Point - clickOffset;		//TODO:  Depending on what the first hit is, translate to the dragging object's center or edge



		//    ////	I can't get the projection logic to work, and am tired of fighting it
		//    ////	After some thought, the reason it's failing, is because I'm storing the offset from the hull perimiter to the center of position.  The offset should have
		//    ////	been from the plane to the center.  This will be difficult, because the part won't always be sliding against a plane (or the same plane)
		//    ////part.Position = hits[0].Point;




		//    ////if (clickOffset.IsNearZero())
		//    ////{
		//    ////    part.Position = hits[0].Point;		//	the projection fails if there is no offset vector
		//    ////}
		//    ////else
		//    ////{
		//    ////    //	When they first clicked on the object, the hit was somewhere on its perimiter.
		//    ////    //	When they drag around, the hit is on the plane.
		//    ////    //	The object's position needs to stay on the plane, but keep the part of the offset that is along the plane
		//    ////    Vector3D offsetAlongPlane = clickOffset.GetProjectedVector(_plane);

		//    ////    part.Position = hits[0].Point - offsetAlongPlane;
		//    ////}

		//    #endregion

		//    List<Visual3D> allIgnores = new List<Visual3D>();
		//    if (ignoreVisuals != null)
		//    {
		//        allIgnores.AddRange(ignoreVisuals);
		//    }
		//    allIgnores.AddRange(models.Cast<Visual3D>());

		//    //	Do a hit test for 3D objects under the mouse
		//    RayHitTestParameters clickRay;
		//    List<MyHitTestResult> hits = CastRay(out clickRay, clickPoint, allIgnores, true);

		//    Point3D planeHitPoint = hits.Where(o => o.DragShapeHit != null).First().Point;

		//    //NOTE: _camera.LookDirection and _camera.UpDirection are really screwed up (I think that the trackball messed them up), so fire a ray instead
		//    //	I'm not using the mouse click point, because that can change as they drag, and the inconsistency would be jarring
		//    RayHitTestParameters cameraLook = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth / 2d, _viewport.ActualHeight / 2d));

		//    double dot = Vector3D.DotProduct(_plane.Normal.ToUnit(), cameraLook.Direction.ToUnit());

		//    Point3D retVal = position;

		//    if (Math.Abs(dot) < .15)
		//    {
		//        //	They are looking along the plane, so snap to a line instead of a plane
		//        Vector3D? snapLine = DragItemSprtGetSnapLine(cameraLook.Direction);		//	the returned vector is used like 3 bools, with only one axis set to true

		//        if (snapLine != null)
		//        {
		//            //	I can't use the plane hit results (because the farther the mouse is from the plane, the farther the ray will travel before it hits the plane, so the
		//            //	object still drifts funny).  Instead, do a line-line compare
		//            //part.Position = new Point3D(
		//            //    snapLine.Value.X == 0 ? part.Position.X : planeHitPoint.X,
		//            //    snapLine.Value.Y == 0 ? part.Position.Y : planeHitPoint.Y,
		//            //    snapLine.Value.Z == 0 ? part.Position.Z : planeHitPoint.Z);

		//            RayHitTestParameters mouseRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);

		//            Point3D? lineHitPoint, dummy1;
		//            if (Math3D.GetClosestPointsBetweenLines(out lineHitPoint, out dummy1, position, snapLine.Value, mouseRay.Origin, mouseRay.Direction))
		//            {
		//                retVal = lineHitPoint.Value;
		//            }
		//        }
		//    }
		//    else
		//    {
		//        retVal = planeHitPoint;
		//    }

		//    //	Exit Function
		//    return retVal;
		//}
		//private Vector3D? DragItemSprtGetSnapLine(Vector3D lookDirection)
		//{
		//    const double THRESHOLD = .33d;

		//    DoubleVector testVectors;

		//    if (_isVertical)
		//    {
		//        //	XZ plane
		//        testVectors = new DoubleVector(1, 0, 0, 0, 0, 1);
		//    }
		//    else
		//    {
		//        //	XY plane
		//        testVectors = new DoubleVector(1, 0, 0, 0, 1, 0);
		//    }

		//    //Quaternion cameraOrientation = testVectors.GetAngleAroundAxis(new DoubleVector(_camera.LookDirection.ToUnit(), _camera.UpDirection.ToUnit()));
		//    //RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(cameraOrientation));
		//    //DoubleVector testVectorsTransformed = new DoubleVector(transform.Transform(testVectors.Standard), transform.Transform(testVectors.Orth));

		//    double dot = Vector3D.DotProduct(testVectors.Standard, lookDirection.ToUnit());
		//    if (Math.Abs(dot) < THRESHOLD)
		//    {
		//        return testVectors.Standard;		//	standard is fairly orhogonal to the camera's look direction, so only allow the part to slide along this axis
		//    }

		//    dot = Vector3D.DotProduct(testVectors.Orth, lookDirection.ToUnit());
		//    if (Math.Abs(dot) < THRESHOLD)
		//    {
		//        return testVectors.Orth;
		//    }

		//    return null;
		//}

		//private void ChangeDragPlane(bool isVertical)
		//{
		//    if (_selectedParts == null)
		//    {
		//        if (isVertical)
		//        {
		//            _plane = new Triangle(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 0, 1));
		//        }
		//        else
		//        {
		//            _plane = new Triangle(new Point3D(1, 0, 0), new Point3D(0, 1, 0), new Point3D(0, 0, 0));
		//        }
		//    }
		//    else
		//    {
		//        Point3D point = _selectedParts.Position;

		//        if (isVertical)
		//        {
		//            _plane = new Triangle(new Point3D(0, point.Y, 0), new Point3D(1, point.Y, 0), new Point3D(0, point.Y, 1));
		//        }
		//        else
		//        {
		//            _plane = new Triangle(new Point3D(1, 0, point.Z), new Point3D(0, 1, point.Z), new Point3D(0, 0, point.Z));
		//        }
		//    }

		//    _isVertical = isVertical;
		//}

		#endregion
		#region OLD

		//private void Undo_OLD()
		//{
		//    if (_undoRedoIndex <= 0)
		//    {
		//        //	There is nothing left to undo
		//        return;
		//    }

		//    //	Make sure there is nothing selected
		//    ClearSelectedParts();

		//    _undoRedoIndex--;

		//    //	Figure out what changed
		//    if (_undoRedo[_undoRedoIndex] is UndoRedoTransformChange[])
		//    {
		//        //	Parts Moved
		//        UndoRedoSprtMoveParts_OLD((UndoRedoTransformChange[])_undoRedo[_undoRedoIndex]);
		//    }
		//    else
		//    {
		//        throw new ApplicationException("Unknown type of undo/redo item");
		//    }
		//}
		//private void Redo_OLD()
		//{
		//    if (_undoRedoIndex >= _undoRedo.Count - 1)
		//    {
		//        //	There is nothing left to redo
		//        return;
		//    }

		//    //	Make sure there is nothing selected
		//    ClearSelectedParts();

		//    _undoRedoIndex++;

		//    //	Figure out what changed
		//    if (_undoRedo[_undoRedoIndex] is UndoRedoTransformChange[])
		//    {
		//        //	Parts Moved
		//        UndoRedoSprtMoveParts_OLD((UndoRedoTransformChange[])_undoRedo[_undoRedoIndex]);
		//    }
		//    else
		//    {
		//        throw new ApplicationException("Unknown type of undo/redo item");
		//    }
		//}
		//private void UndoRedoSprtMoveParts_OLD(UndoRedoTransformChange[] changes)
		//{
		//    foreach (UndoRedoTransformChange change in changes)
		//    {
		//        //DesignPart part = _parts.Where(o => o.Part3D.Token == change.Token).First();
		//        DesignPart part = _parts.Single(o => o.Part3D.Token == change.Token);

		//        part.Part3D.SetTransform(change.Scale, change.Position, change.Orientation);
		//    }
		//}

		#endregion
		#region OLD

		//private static List<TubeRingBase> GetThrusterRings1Full()
		//{
		//    List<TubeRingBase> retVal = new List<TubeRingBase>();

		//    retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
		//    retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.05, 1.05, false));
		//    retVal.Add(new TubeRingDome(.66, false, 4));

		//    return retVal;
		//}
		//private static List<TubeRingBase> GetThrusterRings1Half(bool includeDome)
		//{
		//    List<TubeRingBase> retVal = new List<TubeRingBase>();

		//    retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
		//    retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
		//    if (includeDome)
		//    {
		//        retVal.Add(new TubeRingDome(.55, false, 4));
		//    }

		//    return retVal;
		//}
		//private static List<TubeRingBase> GetThrusterRings2Full()
		//{
		//    List<TubeRingBase> retVal = new List<TubeRingBase>();

		//    retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
		//    retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.5, false, 1, 1, false));
		//    retVal.Add(new TubeRingRegularPolygon(.5, false, .9, .9, false));
		//    retVal.Add(new TubeRingRegularPolygon(1.2, false, .25, .25, false));

		//    return retVal;
		//}

		#endregion

		#endregion
	}
}
