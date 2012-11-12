using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xaml;

namespace Game.Newt.AsteroidMiner2.View
{
	public partial class OptionsPanel : UserControl
	{
		#region Events

		public event EventHandler OKClicked = null;

		#endregion

		#region Declaration Section

		private const string FOLDER = "Asteroid Miner";
		private const string FILE = "AsteroidMinder3D Options.xml";

		private const string MSGBOXCAPTION = "Asteroid Miner Options";

		private bool _isInitialized = false;

		#endregion

		#region Constructor

		public OptionsPanel()
		{
			InitializeComponent();

			LoadFromFile();

			_isInitialized = true;
		}

		#endregion

		#region Public Properties

		public NumberOfStartingObjects NumStartingObjects
		{
			get
			{
				if (radStartVeryFew.IsChecked.Value)
				{
					return NumberOfStartingObjects.VeryFew;
				}
				else if (radStartFew.IsChecked.Value)
				{
					return NumberOfStartingObjects.Few;
				}
				else if (radStartNormal.IsChecked.Value)
				{
					return NumberOfStartingObjects.Normal;
				}
				else if (radStartMany.IsChecked.Value)
				{
					return NumberOfStartingObjects.Many;
				}
				else
				{
					MessageBox.Show("Unknown number of starting items", MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
					return NumberOfStartingObjects.Normal;
				}
			}
			private set
			{
				switch (value)
				{
					case NumberOfStartingObjects.VeryFew:
						radStartVeryFew.IsChecked = true;
						break;

					case NumberOfStartingObjects.Few:
						radStartFew.IsChecked = true;
						break;

					case NumberOfStartingObjects.Normal:
						radStartNormal.IsChecked = true;
						break;

					case NumberOfStartingObjects.Many:
						radStartMany.IsChecked = true;
						break;

					default:
						throw new ApplicationException("Unknown NumberOfStartingObjects: " + value.ToString());
				}
			}
		}

		public bool OctreeShowLines
		{
			get
			{
				return chkOctreeShowLines.IsChecked.Value;
			}
			private set
			{
				chkOctreeShowLines.IsChecked = value;
			}
		}
		public bool OctreeCentersDrift
		{
			get
			{
				return chkOctreeCentersDrift.IsChecked.Value;
			}
			private set
			{
				chkOctreeCentersDrift.IsChecked = value;
			}
		}

		public bool ShowStars
		{
			get
			{
				return chkShowStars.IsChecked.Value;
			}
			private set
			{
				chkShowStars.IsChecked = value;
			}
		}

		#endregion

		#region Event Listeners

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SaveToFile();

				if (this.OKClicked != null)
				{
					this.OKClicked(this, new EventArgs());
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private void LoadFromFile()
		{
			AsteroidMiner3DOptions options = ReadOptions() ?? new AsteroidMiner3DOptions();

			this.NumStartingObjects = options.NumStaringObjects ?? NumberOfStartingObjects.Normal;
			this.OctreeShowLines = options.OctreeShowLines ?? false;
			this.OctreeCentersDrift = options.OctreeCentersDrift ?? true;
			this.ShowStars = options.ShowStars ?? true;
		}
		private void SaveToFile()
		{
			AsteroidMiner3DOptions options = new AsteroidMiner3DOptions();

			options.NumStaringObjects = this.NumStartingObjects;
			options.OctreeShowLines = this.OctreeShowLines;
			options.OctreeCentersDrift = this.OctreeCentersDrift;
			options.ShowStars = this.ShowStars;

			SaveOptions(options);
		}

		/// <summary>
		/// This deserializes the options class from a previous call in appdata
		/// </summary>
		private static AsteroidMiner3DOptions ReadOptions()
		{
			string filename = GetOptionsFilename();
			if (!File.Exists(filename))
			{
				return null;
			}

			AsteroidMiner3DOptions retVal = null;
			using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				//deserialized = XamlReader.Load(file);		//	this is the old way, it doesn't like generic lists
				retVal = XamlServices.Load(file) as AsteroidMiner3DOptions;
			}

			return retVal;
		}
		private static void SaveOptions(AsteroidMiner3DOptions options)
		{
			string filename = GetOptionsFilename();

			//string xamlText = XamlWriter.Save(options);		//	this is the old one, it doesn't like generic lists
			string xamlText = XamlServices.Save(options);

			using (StreamWriter writer = new StreamWriter(filename, false))
			{
				writer.Write(xamlText);
			}
		}
		private static string GetOptionsFilename()
		{
			string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			foldername = System.IO.Path.Combine(foldername, FOLDER);

			//	Make sure the folder exists
			if (!Directory.Exists(foldername))
			{
				Directory.CreateDirectory(foldername);
			}

			return System.IO.Path.Combine(foldername, FILE);
		}

		#endregion
	}

	#region Enum: NumberOfStartingObjects

	public enum NumberOfStartingObjects
	{
		VeryFew,
		Few,
		Normal,
		Many
	}

	#endregion
	#region Class: AsteroidMiner3DOptions

	/// <summary>
	/// This class gets saved to xaml in their appdata folder
	/// NOTE: All properties are nullable so that new ones can be added, and an old xml will still load
	/// NOTE: Once a property is added, it can never be removed (or an old config will bomb the deserialize)
	/// </summary>
	/// <remarks>
	/// I didn't want this class to be public, but XamlServices fails otherwise
	/// </remarks>
	public class AsteroidMiner3DOptions
	{
		public NumberOfStartingObjects? NumStaringObjects
		{
			get;
			set;
		}

		public bool? OctreeShowLines
		{
			get;
			set;
		}
		public bool? OctreeCentersDrift
		{
			get;
			set;
		}

		public bool? ShowStars
		{
			get;
			set;
		}
	}

	#endregion
}
