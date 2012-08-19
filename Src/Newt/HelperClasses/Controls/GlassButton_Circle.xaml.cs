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

using System.Windows.Media.Animation;

namespace Game.Newt.HelperClasses.Controls
{
	/// <summary>
	/// This is a user control painted up to look like a glass button
	/// </summary>
	/// <remarks>
	/// To play with the background color, there are 3 properties (all brushes):
	///		ButtonBackground
	///		ButtonHoverBackground
	///		ButtonClickBackground
	/// 
	/// Then the button's content is set with this:
	///		ButtonContent
	/// 
	/// Finally, listen to this event:
	///		ButtonClicked
	/// </remarks>
	public partial class GlassButton_Circle : UserControl
	{
		#region Events

		public event RoutedEventHandler ButtonClicked = null;

		#endregion

		#region Declaration Section

		private bool _isHovering = false;
		private bool _isClicking = false;

		#endregion

		#region Constructor

		public GlassButton_Circle()
		{
			InitializeComponent();

			//	Without this, the xaml can't bind to the custom dependency properties
			this.DataContext = this;
		}

		#endregion

		#region Public Properties

		public object ButtonContent
		{
			get
			{
				//	Note that I don't set this usercontrol's content property, because that would overwrite the grid
				return buttonContent.Content;
			}
			set
			{
				buttonContent.Content = value;
			}
		}

		//	This is the background brush of the button
		public static readonly DependencyProperty ButtonBackgroundProperty = DependencyProperty.Register("ButtonBackground", typeof(Brush), typeof(GlassButton_Circle), new FrameworkPropertyMetadata(SystemColors.ControlBrush));
		public Brush ButtonBackground
		{
			get
			{
				return (Brush)GetValue(ButtonBackgroundProperty);
			}
			set
			{
				SetValue(ButtonBackgroundProperty, value);
			}
		}

		//	This is the background brush of the button when the mouse is hovering
		public static readonly DependencyProperty ButtonHoverBackgroundProperty = DependencyProperty.Register("ButtonHoverBackground", typeof(Brush), typeof(GlassButton_Circle), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(48, 255, 255, 255))));
		public Brush ButtonHoverBackground
		{
			get
			{
				return (Brush)GetValue(ButtonHoverBackgroundProperty);
			}
			set
			{
				SetValue(ButtonHoverBackgroundProperty, value);
			}
		}

		//	This is the background brush of the button when the mouse is clicking
		public static readonly DependencyProperty ButtonClickBackgroundProperty = DependencyProperty.Register("ButtonClickBackground", typeof(Brush), typeof(GlassButton_Circle), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(32, 0, 0, 0))));
		public Brush ButtonClickBackground
		{
			get
			{
				return (Brush)GetValue(ButtonClickBackgroundProperty);
			}
			set
			{
				SetValue(ButtonClickBackgroundProperty, value);
			}
		}

		#endregion

		#region Event Listeners

		private void Grid_MouseEnter(object sender, MouseEventArgs e)
		{
			if (this.IsEnabled)
			{
				ShowHover();
			}
		}
		private void Grid_MouseLeave(object sender, MouseEventArgs e)
		{
			if (this.IsEnabled)
			{
				HideHover();
				HideClick();
			}
		}

		private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed && this.IsEnabled)
			{
				ShowClick();
				(sender as UIElement).CaptureMouse();
			}
		}
		private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left || !this.IsEnabled)
			{
				return;
			}

			(sender as UIElement).ReleaseMouseCapture();

			//NOTE:  Calling release capture will cause the mouse leave to fire IF they have left the control.  So at this point, I should
			//	only raise the click if they are still over the control
			bool shouldRaiseEvent = false;
			if (_isClicking)
			{
				shouldRaiseEvent = true;
			}

			HideClick();

			//	Waiting till now to raise the event so the visuals don't get screwed up
			if (shouldRaiseEvent)
			{
				if (this.ButtonClicked != null)
				{
					this.ButtonClicked(this, new RoutedEventArgs());
				}
			}
		}

		private void Grid_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (this.IsEnabled)
			{
				//rctDiffuseBottom.Opacity = 1d;
				//rctLightCap.Opacity = 1d;
				rctDiffuseBottomDisabled.Visibility = Visibility.Collapsed;
				rctLightCapDisabled.Visibility = Visibility.Collapsed;
				rctDiffuseBottom.Visibility = Visibility.Visible;
				rctLightCap.Visibility = Visibility.Visible;
			}
			else
			{
				//	Make sure no activity plates are showing
				HideHover();
				HideClick();

				//rctDiffuseBottom.Opacity = .4d;		//	for some reason, this is ignored (it's locked at .85)
				//rctLightCap.Opacity = .4d;
				rctDiffuseBottom.Visibility = Visibility.Collapsed;
				rctLightCap.Visibility = Visibility.Collapsed;
				rctDiffuseBottomDisabled.Visibility = Visibility.Visible;
				rctLightCapDisabled.Visibility = Visibility.Visible;
			}

			//	Inform the content (give it a chance to change its apperance)
			if (this.ButtonContent != null && this.ButtonContent is UIElement)
			{
				((UIElement)this.ButtonContent).IsEnabled = this.IsEnabled;
			}
		}

		#endregion

		#region Private Methods

		private void ShowHover()
		{
			Storyboard animation = (Storyboard)FindResource("showHover");
			animation.Begin(this);

			_isHovering = true;
		}
		private void HideHover()
		{
			if (_isHovering)
			{
				Storyboard animation = (Storyboard)FindResource("hideHover");
				animation.Begin(this);
			}

			_isHovering = false;
		}

		private void ShowClick()
		{
			Storyboard animation = (Storyboard)FindResource("showClick");
			animation.Begin(this);

			_isClicking = true;
		}
		private void HideClick()
		{
			if (_isClicking)
			{
				Storyboard animation = (Storyboard)FindResource("hideClick");
				animation.Begin(this);
			}

			_isClicking = false;
		}

		#endregion
	}
}
