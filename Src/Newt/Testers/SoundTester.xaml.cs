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
using System.Windows.Shapes;

using Game.HelperClassesWPF;

namespace Game.Newt.Testers
{
	public partial class SoundTester : Window
	{
		#region Declaration Section

		private const string WAVFOLDER = @"c:\temp";

		private SoundPool _repeatSound = null;

		#endregion

		#region Constructor

		public SoundTester()
		{
			InitializeComponent();

			this.Background = SystemColors.ControlBrush;
		}

		#endregion

		#region Event Listeners

		private void btnRepeatPlay_Click(object sender, RoutedEventArgs e)
		{

			if (_repeatSound == null)
			{
				_repeatSound = new SoundPool(System.IO.Path.Combine(WAVFOLDER, "engine.wav"));
			}

			_repeatSound.TestPlayRepeat();

		}
		private void btnRepeatPause_Click(object sender, RoutedEventArgs e)
		{
			if (_repeatSound != null)
			{
				_repeatSound.TestPlayPause();
			}
		}

		#endregion
	}
}
