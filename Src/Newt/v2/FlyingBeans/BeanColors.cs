using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Game.HelperClassesWPF;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.FlyingBeans
{
    public class BeanColors
    {
        // These are from the inpiration kuler theme:
        // Light Green (sky): 8DA893
        // Light Light Brown: E0D0AA
        // Light Brown: C18E44
        // Brown: 493227
        // Black: 1D1E24

        public Color Sky = UtilityWPF.ColorFromHex("8DA893");
        public Color BoundryLines = UtilityWPF.ColorFromHex("748A78");

        public Color Terrain = UtilityWPF.ColorFromHex("917161");
        public SpecularMaterial TerrainSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C18E44")), 30d);

        //Standard: 20000000
        //Hottrack: 70B0B0B0
        public SolidColorBrush PanelButtonPressedBackground = new SolidColorBrush(UtilityWPF.ColorFromHex("70404040"));
    }
}
