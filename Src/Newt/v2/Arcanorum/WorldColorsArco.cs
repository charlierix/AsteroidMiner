using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.Arcanorum
{
    public static class WorldColorsArco
    {
        public static Color SensorVision_Any_Color = UtilityWPF.ColorFromHex("A0A0A0");
        public static ThreadLocal<DiffuseMaterial> SensorVision_Any_Diffuse = new ThreadLocal<DiffuseMaterial>(() => new DiffuseMaterial(new SolidColorBrush(SensorVision_Any_Color)));
        public static ThreadLocal<SpecularMaterial> SensorVision_Any_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 3));

        public static Color MotionController_Linear_Color = UtilityWPF.ColorFromHex("4851B5");
        public static ThreadLocal<DiffuseMaterial> MotionController_Linear_Diffuse = new ThreadLocal<DiffuseMaterial>(() => new DiffuseMaterial(new SolidColorBrush(MotionController_Linear_Color)));
        public static ThreadLocal<SpecularMaterial> MotionController_Linear_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0D3E0A3")), 40));

        public static Color MotionController2_Color = UtilityWPF.ColorFromHex("4851B5");
        public static ThreadLocal<DiffuseMaterial> MotionController2_Diffuse = new ThreadLocal<DiffuseMaterial>(() => new DiffuseMaterial(new SolidColorBrush(MotionController2_Color)));
        public static ThreadLocal<SpecularMaterial> MotionController2_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0D3E0A3")), 40));
    }
}
