using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    public class EditorColors
    {
        public Color Background = UtilityWPF.ColorFromHex("363432");

        public Color Panel_Background = UtilityWPF.ColorFromHex("408A8274");
        public Color Panel_Border = UtilityWPF.ColorFromHex("A059544B");

        public Color TextStandard = UtilityWPF.ColorFromHex("999081");

        public Color PanelSelectedItem_Background = UtilityWPF.ColorFromHex("20CCBFAB");
        public Color PanelSelectedItem_Border = UtilityWPF.ColorFromHex("CFC0A9");

        public Color TabItemSelected_Background = UtilityWPF.ColorFromHex("8A8274");
        public Color TabItemSelected_Border = UtilityWPF.ColorFromHex("706A5E");
        public Color TabItemSelected_Text = UtilityWPF.ColorFromHex("D7C2AB");

        public Color TabItemHovered_Background = UtilityWPF.ColorFromHex("808A8274");
        public Color TabItemHovered_Border = UtilityWPF.ColorFromHex("706A5E");
        public Color TabItemHovered_Text = UtilityWPF.ColorFromHex("C2AF9A");

        public Color TabIcon_Primary = UtilityWPF.ColorFromHex("F0E2D3");
        public Color TabIcon_Secondary = UtilityWPF.ColorFromHex("B4C6D6");

        public Color SideExpander_Background
        {
            get
            {
                return UtilityWPF.AlphaBlend(this.Panel_Background, Colors.Transparent, .5d);
            }
        }
        public Color SideExpander_Border
        {
            get
            {
                return this.Panel_Border;
            }
        }
        public Color SideExpander_Text
        {
            get
            {
                return this.TextStandard;
            }
        }

        public Color PartVisual_TextColor = UtilityWPF.ColorFromHex("D7C2AB");
        public Color PartVisual_BorderColor = UtilityWPF.ColorFromHex("40D7C2AB");
        public Color PartVisual_BackgroundColor = UtilityWPF.ColorFromHex("59544E");
        public Color PartVisual_BackgroundColor_Hover = UtilityWPF.ColorFromHex("666059");

        public Color SelectionRectangle_Background = UtilityWPF.ColorFromHex("20607B91");
        public Color SelectionRectangle_Border = UtilityWPF.ColorFromHex("80667A8A");

        public Color UndoRedo_GradientFrom = UtilityWPF.ColorFromHex("617B91");
        public Color UndoRedo_GradientTo = UtilityWPF.ColorFromHex("75808A");
        public Color UndoRedo_Border = UtilityWPF.ColorFromHex("C8556B7D");

        public Color CompassRose_Color = UtilityWPF.ColorFromHex("667A8A");
        private SpecularMaterial _compassRoseSpecular = null;
        public SpecularMaterial CompassRose_Specular
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
        public Brush Selected_EmissiveBrush
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
        public Brush SelectedLocked_EmissiveBrush
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
        public Brush Inactive_EmissiveBrush
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
        public Color DraggableModifier_SpecularColor = UtilityWPF.ColorFromHex("415B70");
        public double DraggableModifier_SpecularPower = 50d;

        public Color DraggableModifierHotTrack = UtilityWPF.ColorFromHex("A69F6F");
        public Color DraggableModifierHotTrack_SpecularColor = UtilityWPF.ColorFromHex("BFB349");
        public double DraggableModifierHotTrack_SpecularPower = 50d;

        public Color SelectionLightColor = UtilityWPF.ColorFromHex("58A0DB");

        public Color ErorMessage = UtilityWPF.ColorFromHex("D4C3A7");
    }
}
