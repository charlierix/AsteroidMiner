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

namespace Game.Newt.AsteroidMiner2.ShipEditor
{
	public partial class LayerRow : UserControl
	{
		#region Events

		public event EventHandler LayerVisibilityChanged = null;

		#endregion

		#region Declaration Section

		private EditorColors _colors = null;

		#endregion

		#region Constructor

		public LayerRow(EditorColors colors)
		{
			InitializeComponent();

			_colors = colors;

			//	The textbox's cursor can't be set directly, but is the opposite of background.  If I just use null or transparent, the cursor is still
			//	black, so I'm making the background transparent black, which makes the cursor white;
			//txtName.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
			txtName.Background = new SolidColorBrush(Color.FromArgb(0, colors.Background.R, colors.Background.G, colors.Background.B));

			txtName.Foreground = new SolidColorBrush(colors.PartVisualTextColor);
		}

		#endregion

		#region Public Properties

		private bool _shouldShowVisibility = true;
		public bool ShouldShowVisibility
		{
			get
			{
				return _shouldShowVisibility;
			}
			set
			{
				_shouldShowVisibility = value;

				if (_shouldShowVisibility)
				{
					chkVisible.Visibility = Visibility.Visible;
				}
				else
				{
					chkVisible.Visibility = Visibility.Collapsed;
				}
			}
		}

		private bool _canEditName = true;
		public bool CanEditName
		{
			get
			{
				return _canEditName;
			}
			set
			{
				_canEditName = value;

				txtName.IsReadOnly = !_canEditName;
			}
		}

		public string LayerName
		{
			get
			{
				return txtName.Text;
			}
			set
			{
				txtName.Text = value;
			}
		}

		public bool IsLayerVisible
		{
			get
			{
				return chkVisible.IsChecked.Value;
			}
			set
			{
				if (chkVisible.IsChecked.Value == value)
				{
					return;
				}

				chkVisible.IsChecked = value;

				OnLayerVisibilityChanged();
			}
		}

		private bool _isSelected = false;
		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				_isSelected = value;

				if (_isSelected)
				{
					selectBorder.BorderBrush = new SolidColorBrush(_colors.PanelSelectedItemBorder);
					selectBorder.Background = new SolidColorBrush(_colors.PanelSelectedItemBackground);
				}
				else
				{
					selectBorder.BorderBrush = Brushes.Transparent;
					selectBorder.Background = Brushes.Transparent;
				}
			}
		}

		#endregion

		#region Protected Methods

		protected virtual void OnLayerVisibilityChanged()
		{
			if (this.LayerVisibilityChanged != null)
			{
				this.LayerVisibilityChanged(this, new EventArgs());
			}
		}

		#endregion

		#region Event Listeners

		private void chkVisible_Checked(object sender, RoutedEventArgs e)
		{
			OnLayerVisibilityChanged();
		}

		#endregion
	}
}
