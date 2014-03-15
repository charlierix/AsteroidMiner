using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.Newt.HelperClasses;

namespace Game.Newt.Testers.Arcanorum
{
    public static class WorldColorsArco
    {

        public static Color SensorVision_Any_Color = UtilityWPF.ColorFromHex("");
        public static ThreadLocal<DiffuseMaterial> SensorVision_Any_Diffuse = new ThreadLocal<DiffuseMaterial>(() => new DiffuseMaterial(new SolidColorBrush(SensorVision_Any_Color)));
        public static ThreadLocal<SpecularMaterial> SensorVision_Any_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 35));


    }
}
