using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public static class DefaultShips
    {
        public static ShipDNA GetDNA(DefaultShipType type)
        {
            List<ShipPartDNA> parts = new List<ShipPartDNA>();

            // This is .7071.... (you see it a lot in quaternions)
            double sqrt2div2 = Math.Sqrt(2d) / 2d;

            switch (type)
            {
                case DefaultShipType.Basic:
                    #region Basic

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.351601369140306), Scale = new Vector3D(1.43344624886732, 1, 1) });

                    //parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -1.53919035872714), Scale = new Vector3D(2.39179746637604, 2.39179746637604, 1.84590950818591) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -1.55), Scale = new Vector3D(2.39179746637604, 2.39179746637604, 1.84590950818591) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(0, 0, 0.368123117732111), Scale = new Vector3D(0.824785836257515, 0.531437069753381, 0.726770338763181) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 1.33152230862174), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -1, 0, 0), Position = new Point3D(1.27190576467114, 0, -0.584241777880554), Scale = new Vector3D(1.94002791062303, 1.94002791062303, 1.94002791062303), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -1, 0, 0), Position = new Point3D(-1.27190576467114, 0, -0.584241777880554), Scale = new Vector3D(1.94002791062303, 1.94002791062303, 1.94002791062303), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, 0.689034166030726), Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    //parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -2.4), Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -2.5), Scale = new Vector3D(1, 1, 1), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    #endregion
                    break;

                case DefaultShipType.Mule:
                    #region Mule

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.1138112506836, 0, 0), Scale = new Vector3D(1.20893561744051, 1.49435720851294, 2.22589638019417) });
                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.1138112506836, 0, 0), Scale = new Vector3D(1.20893561744051, 1.49435720851294, 2.22589638019417) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.2335, -0.709, 0.029956340579129), Scale = new Vector3D(0.886552741156165, 0.886552741156165, 2.12441639345812) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.2335, -0.709, 0.029956340579129), Scale = new Vector3D(0.886552741156165, 0.886552741156165, 2.12441639345812) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.2335, 0.709, 0.029956340579129), Scale = new Vector3D(0.886552741156165, 0.886552741156165, 2.12441639345812) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.2335, 0.709, 0.029956340579129), Scale = new Vector3D(0.886552741156165, 0.886552741156165, 2.12441639345812) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.94451262451659, 0, -1.78148516678199), Scale = new Vector3D(3.03903351805189, 3.03903351805189, 1.16891960353551) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.94451262451659, 0, -1.78148516678199), Scale = new Vector3D(3.03903351805189, 3.03903351805189, 1.16891960353551) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.1, 0, 1.42601631334369), Scale = new Vector3D(1, 1.9332327468521, 0.705334254892751) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.1, 0, 1.42601631334369), Scale = new Vector3D(1, 1.9332327468521, 0.705334254892751) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(0, 0, 1, 0), Position = new Point3D(-0.394841115317897, 0, 1.59863858641419), Scale = new Vector3D(0.793600071807675, 0.793600071807675, 0.793600071807675) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(0, 0, 0, 1), Position = new Point3D(0.394841199625966, 0, 1.59863858641419), Scale = new Vector3D(0.793600071807675, 0.793600071807675, 0.793600071807675) });

                    // Big Thruster
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(2.46367573808076, 2.46367573808076, 2.46367573808076), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    // Top Thrusters
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0.729334005069577, -8.9744238735999E-05, 0.684157798982057, 8.41853258152711E-05), Position = new Point3D(-1.28096414995941, 0, 2.10547023636209), Scale = new Vector3D(1.50075309653899, 1.50075309653899, 1.50075309653899), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -0.729334010591072, 0, 0.684157804161541), Position = new Point3D(1.28096414995941, 0, 2.10547023636209), Scale = new Vector3D(1.50075309653899, 1.50075309653899, 1.50075309653899), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    // Bottom Thrusters
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(8.6267945584998E-06, -0.68401566032667, -9.14351796173045E-06, -0.729467323647773), Position = new Point3D(-0.726003601478887, 0, -2.68013152186471), Scale = new Vector3D(1.32356758692612, 1.32356758692612, 1.32356758692612), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-0.684015657014172, -6.78676916703537E-05, 0.729467320120058, -7.23208707233711E-05), Position = new Point3D(0.726003601478887, 0, -2.68013152186471), Scale = new Vector3D(1.32356758692612, 1.32356758692612, 1.32356758692612), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    #endregion
                    break;

                case DefaultShipType.Transport:
                    #region Transport

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 2.02269079096819), Scale = new Vector3D(1, 2.63600743619082, 4.50484102178382) });
                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.9166240073712, 0, 1.76724695453851), Scale = new Vector3D(2.29746564747429, 2.3715803995201, 3.94865967434108) });
                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.9166240073712, 0, 1.76724695453851), Scale = new Vector3D(2.29746564747429, 2.3715803995201, 3.94865967434108) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1.47963954177498), Scale = new Vector3D(3.13447669240769, 3.13447669240769, 2.22861073441357) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.28895753477268, 0, 3.98319895737387), Scale = new Vector3D(0.560611062308996, 1, 0.485998090881904) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.28895753477268, 0, 3.98319895737387), Scale = new Vector3D(0.560611062308996, 1, 0.485998090881904) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 5.22806674075844), Scale = new Vector3D(0.663983707416115, 0.663983707416115, 0.663983707416115) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.32219116521614, 0, -1.56070252073028), Scale = new Vector3D(2.08394407332299, 2.08394407332299, 2.08394407332299), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.32219116521614, 0, -1.56070252073028), Scale = new Vector3D(2.08394407332299, 2.08394407332299, 2.08394407332299), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.653210050681682, 0.957053806771705, -1.3407263651298), Scale = new Vector3D(1.67520655720119, 1.67520655720119, 1.67520655720119), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.653210050681682, -0.957053806771705, -1.3407263651298), Scale = new Vector3D(1.67520655720119, 1.67520655720119, 1.67520655720119), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.653210050681682, 0.957053806771705, -1.3407263651298), Scale = new Vector3D(1.67520655720119, 1.67520655720119, 1.67520655720119), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.653210050681682, -0.957053806771705, -1.3407263651298), Scale = new Vector3D(1.67520655720119, 1.67520655720119, 1.67520655720119), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, 4.60450114012191), Scale = new Vector3D(1.34615364634496, 1.34615364634496, 1.34615364634496), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -2.91014323818706), Scale = new Vector3D(1.34615364634496, 1.34615364634496, 1.34615364634496), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    #endregion
                    break;

                case DefaultShipType.Fighter:
                    #region Fighter

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1.15969308207482), Scale = new Vector3D(0.808747405553142, 0.855331230177944, 1.01832073761257) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.21675545237483, 0, -0.101844665695391), Scale = new Vector3D(2.26782236206537, 2.26782236206537, 1.92303346001871) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.21675545237483, 0, -0.101844665695391), Scale = new Vector3D(2.26782236206537, 2.26782236206537, 1.92303346001871) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.126261892759987), Scale = new Vector3D(0.712175575941888, 1, 1.21466136120512) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.757623879110037), Scale = new Vector3D(0.712175575941888, 1, 1) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 2.10389956504031), Scale = new Vector3D(1.8362140096987, 1.8362140096987, 1.8362140096987) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-0.012931531706368, 8.25323424338033E-05, 0.999896016572997, 0.00638159208915872), Position = new Point3D(0.894125918904454, 0, 1.55107138681859), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-5.94674294098809E-17, 0.0129317950749441, -2.3117660078756E-17, 0.999916380841988), Position = new Point3D(-0.894125918904454, 0, 1.55107138681859), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-0.0429122846180847, 0.000273877174733788, 0.999058458900017, 0.00637624658189419), Position = new Point3D(1.8, 0, 1.52582366126704), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(8.46199927216153E-17, 0.0429131585862698, -2.51913966236735E-17, 0.999078806110984), Position = new Point3D(-1.8, 0, 1.52582366126704), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, 0, 1, 0), Position = new Point3D(2.6, 0, -0.0109033700326891), Scale = new Vector3D(3.33834776233071, 3.33834776233071, 3.33834776233071), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-2.6, 0, -0.0109033700326891), Scale = new Vector3D(3.33834776233071, 3.33834776233071, 3.33834776233071), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });

                    #endregion
                    break;

                case DefaultShipType.Assault:
                    #region Assault

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.077024915051207), Scale = new Vector3D(1.31615779102172, 1.44497987694517, 1.31499646473755) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -2.58556144075749), Scale = new Vector3D(3.2321667298996, 3.2321667298996, 2.7938805480507) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.885562193131596), Scale = new Vector3D(1, 1.64210462059006, 0.741592101285269) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.938174991518528), Scale = new Vector3D(1, 1.64210462059006, 0.473086127511233) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 1.86048394097807), Scale = new Vector3D(1.45518251962155, 1.45518251962155, 1.45518251962155) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(3.80957186868835E-14, -1.02744126889082E-14, 0.256291229903911, -0.966599609701111), Position = new Point3D(0.6208266756, -0.39932019, 1.63757048812885), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(1.19672459534876E-14, -3.75983026566479E-14, 0.951598128569377, -0.307345085698893), Position = new Point3D(-0.6208266756, -0.39932019, 1.63757048812885), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-9.59867985237794E-15, -3.82715649466247E-14, 0.9709834714398, 0.239146603970693), Position = new Point3D(-0.6208266756, 0.39932019, 1.63757048812885), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-3.7380648773021E-14, -1.26306941883729E-14, 0.324135999240284, 0.946010493597456), Position = new Point3D(0.6208266756, 0.39932019, 1.63757048812885), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ShipPartDNA() { PartType = BeamGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.479027048702997, 0, 1.64740135798669), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = BeamGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.479027048702997, 0, 1.64740135798669), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ShipPartDNA() { PartType = TractorBeam.PARTTYPE, Orientation = new Quaternion(6.50574354810174E-15, 0.0416632352390286, 9.77838576029765E-16, 0.999131710451339), Position = new Point3D(1.09196538761987, 0, 1.18633612625865), Scale = new Vector3D(1.57980603896135, 1.57980603896135, 1.57980603896135) });
                    parts.Add(new ShipPartDNA() { PartType = TractorBeam.PARTTYPE, Orientation = new Quaternion(0.041663235053383, -3.93308952825476E-06, -0.999131705999346, -9.43199547187686E-05), Position = new Point3D(-1.09196538761987, 0, 1.18633612625865), Scale = new Vector3D(1.57980603896135, 1.57980603896135, 1.57980603896135) });

                    parts.Add(new ShipPartDNA() { PartType = PlasmaTank.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(1.21477551020204, 0, -1.07441879107477), Scale = new Vector3D(2.48145375613002, 2.48145375613002, 1.34025125533822) });
                    parts.Add(new ShipPartDNA() { PartType = PlasmaTank.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(-1.21477551020204, 0, -1.07441879107477), Scale = new Vector3D(2.48145375613002, 2.48145375613002, 1.34025125533822) });

                    parts.Add(new ShipPartDNA() { PartType = ShieldKinetic.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(1.2589962812182, 0.0204781279439623, 0.119158359454171), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ShieldEnergy.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(-1.2589962812182, 0.0204781279439623, 0.119158359454171), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-2.29215339225392, 0, 0.0677991493996509), Scale = new Vector3D(2.83609603739894, 2.83609603739894, 2.83609603739894), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, 0, 1, 0), Position = new Point3D(2.29215339225392, 0, 0.0677991493996509), Scale = new Vector3D(2.83609603739894, 2.83609603739894, 2.83609603739894), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.2, 0, -2.85124077528498), Scale = new Vector3D(1.95366858979375, 1.95366858979375, 1.95366858979375), ThrusterDirections = new[] { new Vector3D(0, 0, 1), new Vector3D(1, 0, 0) }, ThrusterType = ThrusterType.Custom });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.2, 0, -2.85124077528498), Scale = new Vector3D(1.95366858979375, 1.95366858979375, 1.95366858979375), ThrusterDirections = new[] { new Vector3D(0, 0, 1), new Vector3D(-1, 0, 0) }, ThrusterType = ThrusterType.Custom });

                    #endregion
                    break;

                case DefaultShipType.Horseshoe:
                    #region Horseshoe

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.91579347158044), Scale = new Vector3D(1.35642342337346, 1.36896611995553, 0.69794478820671) });
                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = new Quaternion(0.00047779182616264, 0.265457806417206, 0.00173530039675663, 0.964120798167776), Position = new Point3D(1.63742641796154, 0, 0.481138824923945), Scale = new Vector3D(1.19064713618044, 1, 0.69794478820671) });
                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = new Quaternion(-0.265457861222196, 0.000446305054388436, 0.96412058555631, -0.00184965772330746), Position = new Point3D(-1.63742641796154, 0, 0.481138824923945), Scale = new Vector3D(1.19064713618044, 1, 0.69794478820671) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(0, 0, -0.127363934606423), Scale = new Vector3D(1.54185002072198, 1.54185002072198, 1.33765558648748) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(3.66326758198724E-14, -0.447735146506699, 8.88203752836587E-14, 0.894166225364515), Position = new Point3D(0.747033647527572, 0, -0.896869170997692), Scale = new Vector3D(1.80730797644213, 1.80730797644213, 1.25773141061229) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0.447735143357092, 5.31072454356838E-05, 0.894166219074475, -0.000106059811404849), Position = new Point3D(-0.747033647527572, 0, -0.896869170997692), Scale = new Vector3D(1.80730797644213, 1.80730797644213, 1.25773141061229) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(0, 0.148521717175308, 0, 0.988909146245143), Position = new Point3D(0.632430070516491, 0, 0.26130307705331), Scale = new Vector3D(0.689284619490636, 1, 0.511728459914318) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(-0.148521716130528, -1.76166185359125E-05, 0.988909139288631, -0.000117297561107096), Position = new Point3D(-0.632430070516491, 0, 0.26130307705331), Scale = new Vector3D(0.689284619490636, 1, 0.511728459914318) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-0.01625686741119, -1.15198360855384E-06, -0.999867845887967, 7.08519876584482E-05), Position = new Point3D(-0.836870055381436, 0, 1.51323278949567), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(0, -0.0162568674520056, 0, 0.999867848398301), Position = new Point3D(0.836870055381436, 0, 1.51323278949567), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-0.34795494402255, -0.923352163053043, -0.00484000639641422, 0.162248926818064), Position = new Point3D(1.06144016501762, -0.36044859299609, 1.39134804670315), Scale = new Vector3D(1.14066154786467, 1.14066154786467, 1.14066154786467), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0.0083756237465087, -0.986702441981029, 0.0539863161161795, 0.153080428493439), Position = new Point3D(1.06144016501762, 0.36044859299609, 1.39134804670315), Scale = new Vector3D(1.14066154786467, 1.14066154786467, 1.14066154786467), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-0.986619408828828, -0.0152971306696259, -0.152697948136295, 0.0550588462510451), Position = new Point3D(-1.06144016501762, -0.36044859299609, 1.39134804670315), Scale = new Vector3D(1.14066154786467, 1.14066154786467, 1.14066154786467), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-0.925770345922437, 0.341469071359501, -0.162278887243383, -0.0037017118445911), Position = new Point3D(-1.06144016501762, 0.36044859299609, 1.39134804670315), Scale = new Vector3D(1.14066154786467, 1.14066154786467, 1.14066154786467), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -0.557984122750825, 0, 0.829851624543805), Position = new Point3D(1.67575867715362, 0, -0.389035535597188), Scale = new Vector3D(1.63020999627835, 1.63020999627835, 1.63020999627835), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0.557984118825669, 6.61842161977727E-05, 0.829851618706187, -9.84312583303764E-05), Position = new Point3D(-1.67575867715362, 0, -0.389035535597188), Scale = new Vector3D(1.63020999627835, 1.63020999627835, 1.63020999627835), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -2.4250191168898), Scale = new Vector3D(2.57595096771867, 2.57595096771867, 2.57595096771867), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    #endregion
                    break;

                case DefaultShipType.Star:
                    #region Star

                    //TODO: May want to make a second star that uses elbows instead (it would be more fuel efficient)

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(1, 1.60129365988516, 1) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1.05807573055252), Scale = new Vector3D(1.99993019975435, 1.99993019975435, 0.863351825016741) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0, 0.683098335564763, 0, 0.730326409181984), Position = new Point3D(-1.05807573055252, 0, 0), Scale = new Vector3D(1.99993019975435, 1.99993019975435, 0.863351825016741) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0, 0.703264706752846, 0, -0.710928092169548), Position = new Point3D(1.05807573055252, 0, 0), Scale = new Vector3D(1.99993019975435, 1.99993019975435, 0.863351825016741) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0, 1, 0, 0), Position = new Point3D(0, 0, 1.05807573055252), Scale = new Vector3D(1.99993019975435, 1.99993019975435, 0.863351825016741) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(0, 0.688417468445453, -0.79796951349749), Scale = new Vector3D(0.745650135839426, 0.56559268355117, 0.375659626855694) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(0, -0.688417468445453, -0.79796951349749), Scale = new Vector3D(0.745650135839426, 0.56559268355117, 0.375659626855694) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0.657464132956455, 1.03257821549063), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, -0.657464132956455, 1.03257821549063), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, 0.923186970492178, 0, -0.384351164319136), Position = new Point3D(1.26476767152616, 0, -1.28662344713811), Scale = new Vector3D(1.39780378270077, 1.39780378270077, 1.39780378270077), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -0.386018942782778, 0, 0.922490854053777), Position = new Point3D(-1.27284096169464, 0, -1.27863718227475), Scale = new Vector3D(1.39780378270077, 1.39780378270077, 1.39780378270077), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, 0.348232138619081, 0, 0.937408330255701), Position = new Point3D(-1.36075156370433, 0, 1.18464861437452), Scale = new Vector3D(1.39780378270077, 1.39780378270077, 1.39780378270077), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, 0.910954439078637, 0, 0.412506981665675), Position = new Point3D(1.19687216660849, 0, 1.35001265714851), Scale = new Vector3D(1.39780378270077, 1.39780378270077, 1.39780378270077), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });

                    #endregion
                    break;

                case DefaultShipType.Pusher:
                    #region Pusher

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-6.93889390390723E-18, 0, 0), Scale = new Vector3D(1.46154128068673, 1, 0.479152105873481) });

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = new Quaternion(-0.218986043794285, -2.23627610487731E-05, 0.975727985759823, -9.96409241866753E-05), Position = new Point3D(1.77624068304702, 0, 0.552006137462626), Scale = new Vector3D(1.83208586974987, 0.769314393721096, 0.192938667071176) });
                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = new Quaternion(-6.91632605526114E-15, 0.218986044936123, 1.1205900981921E-15, 0.975727990847467), Position = new Point3D(-1.77624068304702, 0, 0.552006137462626), Scale = new Vector3D(1.83208586974987, 0.769314393721096, 0.192938667071176) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(0, 0, -0.425283274017994), Scale = new Vector3D(1.27203211329411, 0.428525983233419, 1.24086611227738) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-0.00912840988219802, -9.32189306453168E-07, 0.999958329984113, -0.000102115316564305), Position = new Point3D(0.8524721685946, -0.14566397984532, -0.145079372003619), Scale = new Vector3D(0.708865290074796, 0.708865290074796, 0.708865290074796) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(-0.00912840988219802, -9.32189306453168E-07, 0.999958329984113, -0.000102115316564305), Position = new Point3D(0.8524721685946, 0.14566397984532, -0.145079372003619), Scale = new Vector3D(0.708865290074796, 0.708865290074796, 0.708865290074796) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(7.81827874367361E-07, 0.0091284098963145, -8.56441924652831E-05, 0.999958331530483), Position = new Point3D(-0.8524721685946, 0.14566397984532, -0.145079372003619), Scale = new Vector3D(0.708865290074796, 0.708865290074796, 0.708865290074796) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(7.81827874367361E-07, 0.0091284098963145, -8.56441924652831E-05, 0.999958331530483), Position = new Point3D(-0.8524721685946, -0.14566397984532, -0.145079372003619), Scale = new Vector3D(0.708865290074796, 0.708865290074796, 0.708865290074796) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -1.08085214890685), Scale = new Vector3D(1.92049927089236, 1.92049927089236, 2.39135066574421) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-2.40698320560254, 0, -0.237147155841337), Scale = new Vector3D(1.70191408656443, 1.70191408656443, 1.70191408656443), ThrusterDirections = new[] { new Vector3D(0.283542413776039, 0, -0.958959696541026), new Vector3D(0, 0, 1), new Vector3D(1, 0, 0) }, ThrusterType = ThrusterType.Custom });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(2.40698320560254, 0, -0.237147155841337), Scale = new Vector3D(1.70191408656443, 1.70191408656443, 1.70191408656443), ThrusterDirections = new[] { new Vector3D(-0.283542413776039, 0, -0.958959696541026), new Vector3D(0, 0, 1), new Vector3D(-1, 0, 0) }, ThrusterType = ThrusterType.Custom });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.58040084726707, 0, -0.710242664845455), Scale = new Vector3D(1.69927627192747, 1.69927627192747, 1.69927627192747), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.58040084726707, 0, -0.710242664845455), Scale = new Vector3D(1.69927627192747, 1.69927627192747, 1.69927627192747), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    #endregion
                    break;

                case DefaultShipType.Artillery:
                    #region Artillery

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 1.43204750548001), Scale = new Vector3D(4.21255010847876, 4.21255010847876, 4.21255010847876) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(-0.706737310432671, -2.79102806831798E-05, 0.707476057886624, -2.79394550685619E-05), Position = new Point3D(-1.06944281222852, 0, 0.304142324379067), Scale = new Vector3D(1.99260319499006, 1.53417387921141, 1.4583052766171) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(-5.83617160129272E-14, 0.706737310983783, 2.6874941375295E-14, 0.707476058438313), Position = new Point3D(1.06944281222852, 0, 0.304142324379067), Scale = new Vector3D(1.99260319499006, 1.53417387921141, 1.4583052766171) });

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1.22400386793819), Scale = new Vector3D(3.06308582402714, 1, 0.761928986810591) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-2.08502123852393, 0, -0.0757464655642288), Scale = new Vector3D(1.67375154608721, 1.67375154608721, 2.91813191443649) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(2.08502123852393, 0, -0.0757464655642288), Scale = new Vector3D(1.67375154608721, 1.67375154608721, 2.91813191443649) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(1, 0, 0, 0), Position = new Point3D(-1.03035945698428, 0, 1.91185748957738), Scale = new Vector3D(2.54418470999989, 2.54418470999989, 2.54418470999989), ThrusterDirections = new[] { new Vector3D(1, 0, 0), new Vector3D(0, 0, 1) }, ThrusterType = ThrusterType.Custom });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -1, 0, 0), Position = new Point3D(1.03035945698428, 0, 1.91185748957738), Scale = new Vector3D(2.54418470999989, 2.54418470999989, 2.54418470999989), ThrusterDirections = new[] { new Vector3D(1, 0, 0), new Vector3D(0, 0, 1) }, ThrusterType = ThrusterType.Custom });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(sqrt2div2, 0, sqrt2div2, 0), Position = new Point3D(-1.15629216725114, 0, -2.20740554195985), Scale = new Vector3D(2.54418470999989, 2.54418470999989, 2.54418470999989), ThrusterDirections = new[] { new Vector3D(1, 0, 0), new Vector3D(0, 0, 1) }, ThrusterType = ThrusterType.Custom });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(1.15629216725114, 0, -2.20740554195985), Scale = new Vector3D(2.54418470999989, 2.54418470999989, 2.54418470999989), ThrusterDirections = new[] { new Vector3D(1, 0, 0), new Vector3D(0, 0, 1) }, ThrusterType = ThrusterType.Custom });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -3.29564036315108), Scale = new Vector3D(2.98793169887143, 2.98793169887143, 2.98793169887143), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One), ThrusterType = ThrusterType.One });

                    #endregion
                    break;

                case DefaultShipType.Hive:
                    #region Hive

                    parts.Add(new ShipPartDNA() { PartType = CargoBay.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.566645574491999), Scale = new Vector3D(1.3146162844588, 1, 0.746001641471747) });

                    parts.Add(new ShipPartDNA() { PartType = ConverterMatterToPlasma.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.548364473683518, 0, -0.104397395), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ConverterMatterToAmmo.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.104397395), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ConverterMatterToFuel.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.548364473683518, 0, -0.104397395), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.621160029978706), Scale = new Vector3D(1.3945933720746, 1, 0.636972730498334) });

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.33754162418803, 0, -0.0277902602030434), Scale = new Vector3D(1.7519483608434, 1.7519483608434, 1.81851460066594) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.33754162418803, 0, -0.0277902602030434), Scale = new Vector3D(1.7519483608434, 1.7519483608434, 1.81851460066594) });

                    parts.Add(new ShipPartDNA() { PartType = PlasmaTank.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -1.16718087519328), Scale = new Vector3D(1, 1, 2.33574192401778) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 1.44783800163875), Scale = new Vector3D(0.852169384825627, 0.852169384825627, 0.852169384825627) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(0.913307043276022, 0.000159488879511325, -0.407271671255826, 7.11209915474368E-05), Position = new Point3D(-1.018774605, 0, -1.83078653111444), Scale = new Vector3D(0.852169384825627, 0.852169384825627, 0.852169384825627) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = new Quaternion(1.81325328303877E-15, 0.913307057201625, -9.20025667659273E-16, 0.40727167746568), Position = new Point3D(1.018774605, 0, -1.83078653111444), Scale = new Vector3D(0.852169384825627, 0.852169384825627, 0.852169384825627) });

                    parts.Add(new ShipPartDNA() { PartType = SwarmBay.PARTTYPE, Orientation = new Quaternion(-1, 0, 0, 0), Position = new Point3D(0, 0, -2.10586754213348), Scale = new Vector3D(1.22904745098348, 1.22904745098348, 1.22904745098348) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-0.983556706055346, -0.000171756429715009, 0.180599489142851, -3.15377072542308E-05), Position = new Point3D(-1.76039818, 0, 1.50802029698188), Scale = new Vector3D(1.96247952900617, 1.96247952900617, 1.96247952900617), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -0.983556721052077, 0, -0.180599491896533), Position = new Point3D(1.76039818, 0, 1.50802029698188), Scale = new Vector3D(1.96247952900617, 1.96247952900617, 1.96247952900617), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-0.986100951095401, -0.000172200725847872, -0.166147174980039, 2.90139301312891E-05), Position = new Point3D(-2.01363700443361, 0, -1.1470041346539), Scale = new Vector3D(1.96182335791366, 1.96182335791366, 1.96182335791366), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(-3.5422149461886E-15, -0.986100966130925, -3.26870111571977E-14, 0.166147177513359), Position = new Point3D(2.01363700443361, 0, -1.1470041346539), Scale = new Vector3D(1.96182335791366, 1.96182335791366, 1.96182335791366), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });

                    #endregion
                    break;

                case DefaultShipType.ThrusterPack1:
                    #region ThrusterPack1

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(1.21595934073765, 1.21595934073765, 1.60266885680206) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.608038627523538, 0, 0), Scale = new Vector3D(1.30241042620443, 1.30241042620443, 1.30241042620443), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.608035279806904, -0.00201768948252767, 0), Scale = new Vector3D(1.30241042620443, 1.30241042620443, 1.30241042620443), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.310467550112967, -0.522800987841033, 0), Scale = new Vector3D(1.30241042620443, 1.30241042620443, 1.30241042620443), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.322246664406133, -0.51562395196483, 0), Scale = new Vector3D(1.30241042620443, 1.30241042620443, 1.30241042620443), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.315515500980355, 0.519770084943163, 0), Scale = new Vector3D(1.30241042620443, 1.30241042620443, 1.30241042620443), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.31723854673733, 0.518720230012956, 0), Scale = new Vector3D(1.30241042620443, 1.30241042620443, 1.30241042620443), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    #endregion
                    break;
                case DefaultShipType.ThrusterPack2:
                    #region ThrusterPack2

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(2.06691438007685, 2.06691438007685, 1) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.06254051920115, 0, 0), Scale = new Vector3D(1.64718422786896, 1.64718422786896, 1.64718422786896), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.06254051920115, 0, 0), Scale = new Vector3D(1.64718422786896, 1.64718422786896, 1.64718422786896), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -1.13206280163906), Scale = new Vector3D(1.64718422786896, 1.64718422786896, 1.64718422786896), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, 1.13206280163906), Scale = new Vector3D(1.64718422786896, 1.64718422786896, 1.64718422786896), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two), ThrusterType = ThrusterType.Two });

                    #endregion
                    break;
                case DefaultShipType.ThrusterPack3:
                    #region ThrusterPack3

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, 0), Scale = new Vector3D(1, 1, 2.12288744363112) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.54582563958906, 0, 0), Scale = new Vector3D(1.86884958369789, 1.86884958369789, 1.86884958369789), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, 1, 0, 0), Position = new Point3D(1.54582563958906, 0, 0), Scale = new Vector3D(1.86884958369789, 1.86884958369789, 1.86884958369789), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_One), ThrusterType = ThrusterType.Two_One });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 1), Scale = new Vector3D(1.88817814825037, 1.88817814825037, 1.88817814825037), ThrusterDirections = new[] { new Vector3D(-1, 0, -1), new Vector3D(1, 0, -1) }, ThrusterType = ThrusterType.Custom });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1), Scale = new Vector3D(1.88817814825037, 1.88817814825037, 1.88817814825037), ThrusterDirections = new[] { new Vector3D(-1, 0, 1), new Vector3D(1, 0, 1) }, ThrusterType = ThrusterType.Custom });

                    #endregion
                    break;
                case DefaultShipType.ThrusterPack4:
                    #region ThrusterPack4

                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(-0.638616241, 0, -1.212044094), Scale = new Vector3D(1, 1, 1.47850516435542) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(0.638616241, 0, -1.212044094), Scale = new Vector3D(1, 1, 1.47850516435542) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(-0.638616241, 0, 1.212044094), Scale = new Vector3D(1, 1, 1.47850516435542) });
                    parts.Add(new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Orientation = new Quaternion(-sqrt2div2, 0, 0, sqrt2div2), Position = new Point3D(0.638616241, 0, 1.212044094), Scale = new Vector3D(1, 1, 1.47850516435542) });

                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2 / 2, 0, 1), Position = new Point3D(-1.50534398462239, 0, -1.2046880267736), Scale = new Vector3D(1.80360068144214, 1.80360068144214, 1.80360068144214), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two), ThrusterType = ThrusterType.Two_Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2 / 2, 0, 1), Position = new Point3D(1.50534398462239, 0, -1.2046880267736), Scale = new Vector3D(1.80360068144214, 1.80360068144214, 1.80360068144214), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two), ThrusterType = ThrusterType.Two_Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2 / 2, 0, 1), Position = new Point3D(-1.50534398462239, 0, 1.2046880267736), Scale = new Vector3D(1.80360068144214, 1.80360068144214, 1.80360068144214), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two), ThrusterType = ThrusterType.Two_Two });
                    parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2 / 2, 0, 1), Position = new Point3D(1.50534398462239, 0, 1.2046880267736), Scale = new Vector3D(1.80360068144214, 1.80360068144214, 1.80360068144214), ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.Two_Two), ThrusterType = ThrusterType.Two_Two });

                    #endregion
                    break;

                case DefaultShipType.GunPack1:
                    #region GunPack1

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.359488304064817), Scale = new Vector3D(0.740722286782034, 1, 0.447188240046024) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2, 0, sqrt2div2), Position = new Point3D(0, 0, -0.845037591643844), Scale = new Vector3D(0.486647531698294, 1, 1) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.600428061517778), Scale = new Vector3D(1.48921906025213, 1.48921906025213, 1.48921906025213) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.350479929104352, 0, 0.542018613802121), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.350479929104352, 0, 0.542018613802121), Scale = new Vector3D(1, 1, 1) });

                    #endregion
                    break;
                case DefaultShipType.GunPack2:
                    #region GunPack2

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.510426941375555), Scale = new Vector3D(1.3827605451072, 0.717210085171538, 0.357071853528449) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.155397015268484, 0, 0.188962868139779), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.440677674650502, 0, 0.188962868139779), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.155397015268484, 0, 0.188962868139779), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.440677674650502, 0, 0.188962868139779), Scale = new Vector3D(1, 1, 1) });

                    #endregion
                    break;
                case DefaultShipType.GunPack3:
                    #region GunPack3

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1.40253359942577), Scale = new Vector3D(1, 1.3849694218147, 1) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.241516303159814, 0, 0), Scale = new Vector3D(1.94636975940612, 1.94636975940612, 1.94636975940612) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.241516303159814, 0, 0), Scale = new Vector3D(1.94636975940612, 1.94636975940612, 1.94636975940612) });

                    #endregion
                    break;
                case DefaultShipType.GunPack4:
                    #region GunPack4

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -1.41809716103841), Scale = new Vector3D(0.693253778676797, 1, 1.3376841310155) });

                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.716722735172331, 0, -1.41192429975151), Scale = new Vector3D(0.552033986172998, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = AmmoBox.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.716722735172331, 0, -1.41192429975151), Scale = new Vector3D(0.552033986172998, 1, 1) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(1.77102999586209, 1.77102999586209, 1.77102999586209) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.43270286179556, 0, -0.1375361237872), Scale = new Vector3D(1.40476592424605, 1.40476592424605, 1.40476592424605) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.43270286179556, 0, -0.1375361237872), Scale = new Vector3D(1.40476592424605, 1.40476592424605, 1.40476592424605) });

                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.09697354483941, 0, -0.26), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.77489820441599, 0, -0.26), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.77489820441599, 0, -0.26), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = ProjectileGun.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.09697354483941, 0, -0.26), Scale = new Vector3D(1, 1, 1) });

                    #endregion
                    break;

                case DefaultShipType.SolarPack1:
                    #region SolarPack1

                    parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.839705260000125), Scale = new Vector3D(1.78236967813923, 1.78236967813923, 1.00599318771775) });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0.497616691156332, -0.498123278284321, -0.501865394679567, -0.502376307101586), Position = new Point3D(-1.10909536072, 0.00444429738647521, -1.06994696506863), Scale = new Vector3D(1, 1, 1), Shape = SolarPanelShape.Square });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0.387346563099166, 0.595158353476073, -0.384061748318819, 0.590123502176625), Position = new Point3D(-0.921315802678603, -1.05249304060646, -1.06343800724509), Scale = new Vector3D(1, 1, 1), Shape = SolarPanelShape.Right_Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0.00265941069493328, 0.00407751639853742, -0.545466510430476, 0.838118480522637), Position = new Point3D(-0.910573804138347, -1.07678673895002, 0.513541340293575), Scale = new Vector3D(1, 1, 2.06598515992626), Shape = SolarPanelShape.Right_Triangle });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0.413621103848679, -0.56979536163178, -0.417152644233965, -0.574660334217111), Position = new Point3D(-0.913749428695176, 1.0543961264291, -1.04024863680155), Scale = new Vector3D(1, 1, 1), Shape = SolarPanelShape.Right_Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(-0.00213456813488028, 0.00294053424986271, -0.587447180399454, -0.809254352548032), Position = new Point3D(-0.913828146920963, 1.05415453586618, 0.536954385105503), Scale = new Vector3D(1, 1, 2.06598515992626), Shape = SolarPanelShape.Right_Triangle });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0.500440349985147, 0.495286409860839, -0.504713162203721, 0.49951521719786), Position = new Point3D(1.14239605516126, -0.00625026792786753, -1.06994696506863), Scale = new Vector3D(1, 1, 1), Shape = SolarPanelShape.Square });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(-0.59334403143141, 0.390120091794577, 0.588324565992255, 0.386811813926279), Position = new Point3D(0.964490533295712, 1.05239382909038, -1.06343800724509), Scale = new Vector3D(1, 1, 1), Shape = SolarPanelShape.Right_Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(-0.0040650598768345, 0.00267841248175432, 0.835563528328906, 0.54937227043356), Position = new Point3D(0.953975769369413, 1.07678673895002, 0.513541340293574), Scale = new Vector3D(1, 1, 2.06598515992626), Shape = SolarPanelShape.Right_Triangle });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0.571719625049254, 0.410957226508587, -0.576601027197554, 0.414466022429626), Position = new Point3D(0.937257983711392, -1.05433292120977, -1.04024863680155), Scale = new Vector3D(1, 1, 1), Shape = SolarPanelShape.Right_Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(-0.00295046476679195, -0.00212082070363839, -0.811987296076357, 0.583663796965179), Position = new Point3D(0.937338953605967, -1.05409207595793, 0.536954385105502), Scale = new Vector3D(1, 1, 2.06598515992626), Shape = SolarPanelShape.Right_Triangle });

                    #endregion
                    break;
                case DefaultShipType.SolarPack2:
                    #region SolarPack2

                    parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, -0.73), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.73), Scale = new Vector3D(1, 1, 1) });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.09067399884812, 0, 0), Scale = new Vector3D(0.723431308988736, 1, 0.723431308988736), Shape = SolarPanelShape.Square });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(0.424338649252456, 0, 0), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2, 0, sqrt2div2), Position = new Point3D(1.7570107632149, 0, 0), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, -1, 0, 0), Position = new Point3D(1.08998941016696, 0, -0.66633428531784), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(1.08998941016696, 0, 0.66633428531784), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.09067399884812, 0, 0), Scale = new Vector3D(0.723431308988736, 1, 0.723431308988736), Shape = SolarPanelShape.Square });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(-1.7570107632149, 0, 0), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2, 0, sqrt2div2), Position = new Point3D(-0.424338649252456, 0, 0), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, -1, 0, 0), Position = new Point3D(-1.08998941016696, 0, -0.66633428531784), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-1.08998941016696, 0, 0.66633428531784), Scale = new Vector3D(0.723431308988736, 1, 0.485349726766778), Shape = SolarPanelShape.Trapazoid });

                    #endregion
                    break;
                case DefaultShipType.SolarPack3:
                    #region SolarPack3

                    parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2, 0, sqrt2div2), Position = new Point3D(0.241548942411659, 0, 0), Scale = new Vector3D(0.682246236956509, 0.682246236956509, 1.46020504286002) });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(-0.846397025609697, 0, 0), Scale = new Vector3D(0.462184256141049, 1, 2.39182659166201), Shape = SolarPanelShape.Square });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(-1, 0, 0, 0), Position = new Point3D(0.26059638765034, 0, -0.83736819913164), Scale = new Vector3D(1.62466201000318, 1, 0.703111361766014), Shape = SolarPanelShape.Right_Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.26059638765034, 0, 0.83736819913164), Scale = new Vector3D(1.62466201000318, 1, 0.703111361766014), Shape = SolarPanelShape.Right_Trapazoid });

                    #endregion
                    break;
                case DefaultShipType.SolarPack4:
                    #region SolarPack4

                    parts.Add(new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = new Quaternion(0, 0.218068992805724, 0, 0.975933355499594), Position = new Point3D(-0.591146181563435, 0, 0.723032883159889), Scale = new Vector3D(0.689484359512208, 0.689484359512208, 0.49700946105337) });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(1, 1, 0.315163112216065), Shape = SolarPanelShape.Square });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, -1, 0, 0), Position = new Point3D(0, 0, -0.5430805882683), Scale = new Vector3D(1, 1, 0.675765655943646), Shape = SolarPanelShape.Trapazoid });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0.5430805882683), Scale = new Vector3D(1, 1, 0.675765655943646), Shape = SolarPanelShape.Trapazoid });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, -1, 0, 0), Position = new Point3D(0, 0, -1.46215899363955), Scale = new Vector3D(0.313811027131742, 1, 1), Shape = SolarPanelShape.Triangle });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, sqrt2div2, 0, sqrt2div2), Position = new Point3D(1.08149144761653, 0, 0), Scale = new Vector3D(0.313811027131742, 1, 1), Shape = SolarPanelShape.Triangle });

                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, -sqrt2div2, 0, sqrt2div2), Position = new Point3D(-0.744195298710256, 0, 0), Scale = new Vector3D(0.314302141349811, 1, 0.365007504849372), Shape = SolarPanelShape.Triangle });
                    parts.Add(new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Orientation = new Quaternion(0, 0, 0, 1), Position = new Point3D(0, 0, 1.11889317105921), Scale = new Vector3D(0.314302141349811, 1, 0.365007504849372), Shape = SolarPanelShape.Triangle });

                    #endregion
                    break;

                case DefaultShipType.SwarmPack:
                    #region SwarmPack

                    parts.Add(new ShipPartDNA() { PartType = PlasmaTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0.978406524617677, 0, 1.02608961031058), Scale = new Vector3D(1.72623267950863, 1.72623267950863, 1) });


                    parts.Add(new ShipPartDNA() { PartType = SwarmBay.PARTTYPE, Orientation = new Quaternion(0, -0.997396141851085, 0, 0.0721175167387891), Position = new Point3D(-0.285014229114893, 0, 0.448171272551123), Scale = new Vector3D(1.83739979192166, 1.83739979192166, 1.83739979192166) });
                    parts.Add(new ShipPartDNA() { PartType = SwarmBay.PARTTYPE, Orientation = new Quaternion(0, -0.998950265135782, 0, -0.0458079445636934), Position = new Point3D(1.12169700087435, 0, -0.378091903215706), Scale = new Vector3D(1.83739979192166, 1.83739979192166, 1.83739979192166) });

                    parts.Add(new ShipPartDNA() { PartType = SwarmBay.PARTTYPE, Orientation = new Quaternion(6.09545244108796E-15, -0.999409421220556, -1.55540633928754E-13, 0.0343628982420524), Position = new Point3D(0.0310384069984295, 0, -1.02608961031058), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = SwarmBay.PARTTYPE, Orientation = new Quaternion(0, 0.875585109240185, 0, -0.483063884467523), Position = new Point3D(-1.54039689683519, 0, 0.630928824301011), Scale = new Vector3D(1, 1, 1) });
                    parts.Add(new ShipPartDNA() { PartType = SwarmBay.PARTTYPE, Orientation = new Quaternion(0, -0.979944542294803, 0, 0.199270404291829), Position = new Point3D(-1.45015457320283, 0, -0.551828638123439), Scale = new Vector3D(1, 1, 1) });

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown DefaultShipType: " + type.ToString());
            }

            ShipDNA retVal = ShipDNA.Create(parts);
            retVal.ShipName = type.ToString();
            retVal.ShipLineage = Guid.NewGuid().ToString();

            return retVal;
        }
    }

    #region Enum: DefaultShipType

    public enum DefaultShipType
    {
        Basic,
        Mule,
        Transport,
        Fighter,
        Assault,
        Horseshoe,
        Star,
        Pusher,
        Artillery,
        Hive,

        // ------ Below aren't complete ships, just part packs (they are still sold as ships, but they are meant to be scrapped) ------
        ThrusterPack1,
        ThrusterPack2,
        ThrusterPack3,
        ThrusterPack4,

        GunPack1,
        GunPack2,
        GunPack3,
        GunPack4,

        SolarPack1,
        SolarPack2,
        SolarPack3,
        SolarPack4,

        SwarmPack,
    }

    #endregion
}
