using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner2.ShipEditor
{
	public class EditorColors
	{
		public Color Background = UtilityWPF.ColorFromHex("363432");

		public Color PanelBackground = UtilityWPF.ColorFromHex("408A8274");
		public Color PanelBorder = UtilityWPF.ColorFromHex("A059544B");

		public Color TextStandard = UtilityWPF.ColorFromHex("999081");

		public Color PanelSelectedItemBackground = UtilityWPF.ColorFromHex("20CCBFAB");
		public Color PanelSelectedItemBorder = UtilityWPF.ColorFromHex("CFC0A9");

		public Color TabItemSelectedBackground = UtilityWPF.ColorFromHex("8A8274");
		public Color TabItemSelectedBorder = UtilityWPF.ColorFromHex("706A5E");
		public Color TabItemSelectedText = UtilityWPF.ColorFromHex("D7C2AB");

		public Color SideExpanderBackground
		{
			get
			{
				return UtilityWPF.AlphaBlend(this.PanelBackground, Colors.Transparent, .5d);
			}
		}
		public Color SideExpanderBorder
		{
			get
			{
				return this.PanelBorder;
			}
		}
		public Color SideExpanderText
		{
			get
			{
				return this.TextStandard;
			}
		}

		public Color PartVisualTextColor = UtilityWPF.ColorFromHex("D7C2AB");
		public Color PartVisualBorderColor = UtilityWPF.ColorFromHex("40D7C2AB");

		public Color SelectionRectangleBackground = UtilityWPF.ColorFromHex("20607B91");
		public Color SelectionRectangleBorder = UtilityWPF.ColorFromHex("80667A8A");

		public Color UndoRedoGradientFrom = UtilityWPF.ColorFromHex("617B91");
		public Color UndoRedoGradientTo = UtilityWPF.ColorFromHex("75808A");
		public Color UndoRedoBorder = UtilityWPF.ColorFromHex("C8556B7D");

		public Color CompassRoseColor = UtilityWPF.ColorFromHex("667A8A");
		private SpecularMaterial _compassRoseSpecular = null;
		public SpecularMaterial CompassRoseSpecular
		{
			get
			{
				if (_compassRoseSpecular == null)
				{
					_compassRoseSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("82BDB8")), 20d);
				}

				return _compassRoseSpecular;
			}
		}

		private Brush _selectedEmissiveBrush = null;
		public Brush SelectedEmissiveBrush
		{
			get
			{
				if (_selectedEmissiveBrush == null)
				{
					_selectedEmissiveBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("20699BC2"));
				}

				return _selectedEmissiveBrush;
			}
		}

		private Brush _selectedLockedEmissiveBrush = null;
		public Brush SelectedLockedEmissiveBrush
		{
			get
			{
				if (_selectedLockedEmissiveBrush == null)
				{
					_selectedLockedEmissiveBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("80C26969"));
				}

				return _selectedLockedEmissiveBrush;
			}
		}

		private Brush _inactiveEmissiveBrush = null;
		public Brush InactiveEmissiveBrush
		{
			get
			{
				if (_inactiveEmissiveBrush == null)
				{
					//_inactiveEmissiveBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("E0383838"));
					_inactiveEmissiveBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("FF808080"));
				}

				return _inactiveEmissiveBrush;
			}
		}

		public Color DraggableModifier = UtilityWPF.ColorFromHex("85939E");
		public Color DraggableModifierSpecularColor = UtilityWPF.ColorFromHex("415B70");
		public double DraggableModifierSpecularPower = 50d;

		public Color DraggableModifierHotTrack = UtilityWPF.ColorFromHex("A69F6F");
		public Color DraggableModifierHotTrackSpecularColor = UtilityWPF.ColorFromHex("BFB349");
		public double DraggableModifierHotTrackSpecularPower = 50d;

		public Color SelectionLightColor = UtilityWPF.ColorFromHex("58A0DB");
	}
}
