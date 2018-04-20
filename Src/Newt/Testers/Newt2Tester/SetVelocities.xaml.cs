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

using Game.HelperClassesCore;

namespace Game.Newt.Testers.Newt2Tester
{
	public partial class SetVelocities : UserControl
	{
		#region Events

		public event EventHandler Stop = null;
		public event EventHandler<SetVelocityArgs> WhackEm = null;

		#endregion

		#region Constructor

		public SetVelocities()
		{
			InitializeComponent();
		}

		#endregion

		#region Event Listeners

		private void btnTranslation_Click(object sender, RoutedEventArgs e)
		{
			if (this.WhackEm != null)
			{
				SetVelocityArgs args = new SetVelocityArgs(GetDirection(), GetTranslationSpeed(), 0d, GetOverwriteSetting());
				this.WhackEm(this, args);
			}
		}
		private void btnRotation_Click(object sender, RoutedEventArgs e)
		{
			if (this.WhackEm != null)
			{
				SetVelocityArgs args = new SetVelocityArgs(GetDirection(), 0d, GetRotationSpeed(), GetOverwriteSetting());
				this.WhackEm(this, args);
			}
		}
		private void btnBoth_Click(object sender, RoutedEventArgs e)
		{
			if (this.WhackEm != null)
			{
				SetVelocityArgs args = new SetVelocityArgs(GetDirection(), GetTranslationSpeed(), GetRotationSpeed(), GetOverwriteSetting());
				this.WhackEm(this, args);
			}
		}

		private void btnStop_Click(object sender, RoutedEventArgs e)
		{
			if (this.Stop != null)
			{
				this.Stop(this, new EventArgs());
			}
		}

		private void btnVibrate_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("finish this");
		}

		#endregion

		#region Private Methods

		private SetVelocityDirection GetDirection()
		{
			if (radRandom.IsChecked.Value)
			{
				return SetVelocityDirection.Random;
			}
			else if (radTowardCenter.IsChecked.Value)
			{
				return SetVelocityDirection.TowardCenter;
			}
			else if (radFromCenter.IsChecked.Value)
			{
				return SetVelocityDirection.FromCenter;
			}
			else
			{
				throw new ApplicationException("Unknown direction");
			}
		}

		private double GetTranslationSpeed()
		{
			return UtilityCore.GetScaledValue_Capped(.1d, 50d, trkSpeed.Minimum, trkSpeed.Maximum, trkSpeed.Value);
		}
		private double GetRotationSpeed()
		{
			return UtilityCore.GetScaledValue_Capped(.1d, 20d, trkSpeed.Minimum, trkSpeed.Maximum, trkSpeed.Value);
		}

		private bool GetOverwriteSetting()
		{
			if (radOverwrite.IsChecked.Value)
			{
				return true;
			}
			else if (radAdd.IsChecked.Value)
			{
				return false;
			}
			else
			{
				throw new ApplicationException("Unknown overwrite setting");
			}
		}

		#endregion
	}

	#region Enum: SetVelocityDirection

	public enum SetVelocityDirection
	{
		Random,
		TowardCenter,
		FromCenter
	}

	#endregion

	#region Class: SetVelocityArgs

	public class SetVelocityArgs : EventArgs
	{
		public SetVelocityArgs(SetVelocityDirection direction, double translationSpeed, double rotationSpeed, bool overwriteCurrentVelocity)
		{
			this.Direction = direction;
			this.TranslationSpeed = translationSpeed;
			this.RotationSpeed = rotationSpeed;
			this.OverwriteCurrentVelocity = overwriteCurrentVelocity;
		}

		public SetVelocityDirection Direction
		{
			get;
			private set;
		}
		public double TranslationSpeed
		{
			get;
			private set;
		}
		public double RotationSpeed
		{
			get;
			private set;
		}
		public bool OverwriteCurrentVelocity
		{
			get;
			private set;
		}
	}

	#endregion
}
