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

namespace Game.Newt.v2.GameItems.ShipEditor
{
	public partial class InfoWindow : Window
	{
		#region Constructor

		public InfoWindow()
		{
			InitializeComponent();
		}

		#endregion

		#region Public Properties

		public string Text
		{
			get
			{
				return lblText.Text;
			}
			set
			{
				lblText.Text = value;
			}
		}

		public Brush InnerBorderBrush
		{
			get
			{
				return border1.BorderBrush;
			}
			set
			{
				border1.BorderBrush = value;
			}
		}

		public Brush TextColor
		{
			get
			{
				return lblText.Foreground;
			}
			set
			{
				lblText.Foreground = value;
			}
		}

		#endregion
	}
}
