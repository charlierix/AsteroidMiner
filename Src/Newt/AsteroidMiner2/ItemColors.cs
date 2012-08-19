using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner2
{
	//	Armor should be silvers/blacks/whites
	//	Internal parts are colored based on function group
	public class WorldColors
	{
		//	******************** Environment

		public Color SpaceBackground = UtilityWPF.ColorFromHex("383838");		//	this is hardcoded in xaml, but I want it here for reference
		public Color BoundryLines = UtilityWPF.ColorFromHex("303030");

		public Color StarColor
		{
			get
			{
				if (StaticRandom.NextDouble() < .95d)
				{
					byte color = Convert.ToByte(StaticRandom.Next(192, 256));
					return Color.FromRgb(color, color, color);
				}
				else
				{
					return UtilityWPF.GetRandomColor(255, 160, 192);
				}
			}
		}
		public Color StarEmissive
		{
			get
			{
				Color color = Color.FromRgb(255, 255, 255);
				if (StaticRandom.NextDouble() > .8d)
				{
					color = UtilityWPF.GetRandomColor(255, 128, 192);
				}

				return Color.FromArgb(Convert.ToByte(StaticRandom.Next(32, 128)), color.R, color.G, color.B);
			}
		}

		public Color AsteroidColor
		{
			get
			{
				//byte rgb = Convert.ToByte(StaticRandom.Next(76, 104));
				byte rgb = Convert.ToByte(StaticRandom.Next(64, 89));

				if (StaticRandom.NextDouble() < .95d)
				{
					return Color.FromRgb(rgb, rgb, rgb);
				}
				else
				{
					//return UtilityWPF.GetRandomColor(255, 95, 99);
					return UtilityWPF.GetRandomColor(255, 75, 78);
				}
			}
		}
		public SpecularMaterial AsteroidSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0404040")), 5d);
			}
		}

		public Color SpaceStationHull
		{
			get
			{
				return UtilityWPF.AlphaBlend(UtilityWPF.GetRandomColor(255, 108, 148), Colors.Gray, .25);
			}
		}
		public SpecularMaterial SpaceStationHullSpecular
		{
			get
			{
				return new SpecularMaterial(Brushes.Silver, 75d);
			}
		}

		public Color SpaceStationGlass
		{
			get
			{
				return Color.FromArgb(25, 220, 240, 240);		// the skin is semitransparent, so you can see the components inside
			}
		}
		public SpecularMaterial SpaceStationGlassSpecular_Front
		{
			get
			{
				return new SpecularMaterial(Brushes.White, 85d);
			}
		}
		public SpecularMaterial SpaceStationGlassSpecular_Back
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)), 10d);
			}
		}

		public Color SpaceStationForceField
		{
			get
			{
				return UtilityWPF.ColorFromHex("#2086E7FF");
			}
		}
		public Color SpaceStationForceFieldEmissive_Front
		{
			get
			{
				return UtilityWPF.ColorFromHex("#0A89BBC7");
			}
		}
		public Color SpaceStationForceFieldEmissive_Back
		{
			get
			{
				return UtilityWPF.ColorFromHex("#0AFFC086");
			}
		}

		//	******************** Parts

		public Color CargoBay
		{
			get
			{
				return UtilityWPF.ColorFromHex("34543B");
			}
		}
		public SpecularMaterial CargoBaySpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("5E5448")), 80d);
			}
		}

		public Color ConverterBase
		{
			get
			{
				return UtilityWPF.ColorFromHex("27403B");
			}
		}
		public SpecularMaterial ConverterBaseSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("1F1F61")), 70d);
			}
		}

		public Color ConverterFuel
		{
			get
			{
				return UtilityWPF.AlphaBlend(this.FuelTank, this.ConverterBase, .75d);
			}
		}
		public SpecularMaterial ConverterFuelSpecular
		{
			get
			{
				return this.ConverterBaseSpecular;
			}
		}

		public Color ConverterEnergy
		{
			get
			{
				return UtilityWPF.AlphaBlend(this.EnergyTank, this.ConverterBase, .75d);
			}
		}
		public SpecularMaterial ConverterEnergySpecular
		{
			get
			{
				return this.ConverterBaseSpecular;
			}
		}

		public Color ConverterAmmo
		{
			get
			{
				return UtilityWPF.AlphaBlend(this.AmmoBox, this.ConverterBase, .75d);
			}
		}
		public SpecularMaterial ConverterAmmoSpecular
		{
			get
			{
				Color ammoColor = UtilityWPF.ColorFromHex("D95448");
				Color baseColor = UtilityWPF.ColorFromHex("1F1F61");
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(ammoColor, baseColor, .6d)), 70d);
			}
		}

		public Color HangarBay
		{
			get
			{
				return UtilityWPF.ColorFromHex("BDA88E");
			}
		}
		public SpecularMaterial HangarBaySpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("70615649")), 35d);
			}
		}
		public Color HangarBayTrim
		{
			get
			{
				return UtilityWPF.ColorFromHex("968671");
			}
		}
		public SpecularMaterial HangarBayTrimSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("704A443D")), 35d);
			}
		}

		public Color AmmoBox
		{
			get
			{
				//return UtilityWPF.ColorFromHex("666E7F");
				return UtilityWPF.ColorFromHex("4B515E");
			}
		}
		public SpecularMaterial AmmoBoxSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0D95448")), 65d);
			}
		}

		public Color GunBase
		{
			get
			{
				return UtilityWPF.ColorFromHex("4F5359");		//	flatter dark gray
			}
		}
		public SpecularMaterial GunBaseSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("546B6F78")), 35d);
			}
		}
		public Color GunBarrel
		{
			get
			{
				return UtilityWPF.ColorFromHex("3C424C");		//	gunmetal
			}
		}
		public SpecularMaterial GunBarrelSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D023355C")), 75d);
			}
		}
		public Color GunTrim
		{
			get
			{
				//	red would look tacky, use light gray
				//return UtilityWPF.ColorFromHex("4C1A1A");		//	dark red
				return UtilityWPF.ColorFromHex("5E6166");
			}
		}
		public SpecularMaterial GunTrimSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80D12A2A")), 50d);
			}
		}

		public Color GrapplePad
		{
			get
			{
				return UtilityWPF.ColorFromHex("573A3A");
			}
		}
		public SpecularMaterial GrapplePadSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80BF8E8E")), 25d);
			}
		}

		public Color BeamGunDish
		{
			get
			{
				return UtilityWPF.ColorFromHex("324669");
			}
		}
		public SpecularMaterial BeamGunDishSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A80F45A3")), 40d);
			}
		}
		public Color BeamGunCrystal
		{
			get
			{
				return UtilityWPF.ColorFromHex("3B5B94");
			}
		}
		public SpecularMaterial BeamGunCrystalSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0F45A3")), 90d);
			}
		}
		public EmissiveMaterial BeamGunCrystalEmissive
		{
			get
			{
				return new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30719BE3")));
			}
		}
		public Color BeamGunTrim
		{
			get
			{
				return UtilityWPF.ColorFromHex("5E6166");
			}
		}
		public SpecularMaterial BeamGunTrimSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("802A67D1")), 50d);
			}
		}

		public Color AmmoBoxPlate
		{
			get
			{
				return UtilityWPF.ColorFromHex("5E3131");
			}
		}
		public SpecularMaterial AmmoBoxPlateSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0DE2C2C")), 65d);
			}
		}

		public Color FuelTank
		{
			get
			{
				//return UtilityWPF.ColorFromHex("D49820");
				return UtilityWPF.ColorFromHex("A38521");
			}
		}
		public SpecularMaterial FuelTankSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80659C9E")), 40d);
			}
		}

		public Color EnergyTank
		{
			get
			{
				return UtilityWPF.ColorFromHex("507BC7");
			}
		}
		public SpecularMaterial EnergyTankSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D0551FB8")), 80d);
			}
		}
		public EmissiveMaterial EnergyTankEmissive
		{
			get
			{
				return new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203348D4")));
			}
		}

		public Color Thruster
		{
			get
			{
				return UtilityWPF.ColorFromHex("754F42");
			}
		}
		public SpecularMaterial ThrusterSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("906B7D5A")), 20d);
			}
		}
		public Color ThrusterBack
		{
			get
			{
				return UtilityWPF.ColorFromHex("4F403A");
			}
		}

		public Color TractorBeamBase
		{
			get
			{
				//return UtilityWPF.ColorFromHex("6599A3");
				return UtilityWPF.ColorFromHex("6F8185");
			}
		}
		public SpecularMaterial TractorBeamBaseSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF6599A3")), 60);
			}
		}
		public Color TractorBeamRod
		{
			get
			{
				//return UtilityWPF.ColorFromHex("8F7978");
				return UtilityWPF.ColorFromHex("8A788F");
			}
		}
		public SpecularMaterial TractorBeamRodSpecular
		{
			get
			{
				//return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A03530")), 100);
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("8A30A1")), 100);
			}
		}
		public EmissiveMaterial TractorBeamRodEmissive
		{
			get
			{
				//return new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("60DE9A97")));
				return new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40AE75BA")));
			}
		}

		public Color EyeLens
		{
			get
			{
				return UtilityWPF.ColorFromHex("3D1B16");
			}
		}
		public SpecularMaterial EyeLensSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80A00000")), 75d);
			}
		}
		public Color EyeBase
		{
			get
			{
				return Color.FromRgb(75, 75, 75);
			}
		}
		public SpecularMaterial EyeBaseSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)), 35d);
			}
		}

		public Color Brain
		{
			get
			{
				return UtilityWPF.ColorFromHex("FFE32078");
			}
		}
		public SpecularMaterial BrainSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0ED8EB9")), 35d);
			}
		}
		public Color BrainInsideStrand
		{
			get
			{
				//return UtilityWPF.ColorFromHex("E597BB");
				Color color1 = UtilityWPF.ColorFromHex("E58EB7");
				Color color2 = UtilityWPF.ColorFromHex("E5AEC8");
				return UtilityWPF.AlphaBlend(color1, color2, StaticRandom.NextDouble());
			}
		}
		public SpecularMaterial BrainInsideStrandSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0073")), 100d);
			}
		}

		public Color ShieldBase
		{
			get
			{
				return UtilityWPF.ColorFromHex("1D8F8D");
			}
		}
		public SpecularMaterial ShieldBaseSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("503FB2")), 100d);
			}
		}
		public EmissiveMaterial ShieldBaseEmissive
		{
			get
			{
				return new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("205FD4CE")));
			}
		}

		public Color ShieldEnergy
		{
			get
			{
				return UtilityWPF.AlphaBlend(this.EnergyTank, this.ShieldBase, .65d);
			}
		}
		public SpecularMaterial ShieldEnergySpecular
		{
			get
			{
				return ShieldBaseSpecular;
			}
		}

		public Color ShieldKinetic
		{
			get
			{
				return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("505663"), this.ShieldBase, .9d);
			}
		}
		public SpecularMaterial ShieldKineticSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C07D416D")), 70d);
			}
		}

		public Color ShieldTractor
		{
			get
			{
				return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("956CA1"), this.ShieldBase, .9d);
			}
		}
		public SpecularMaterial ShieldTractorSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0AE75BA")), 50d);
			}
		}

		public Color SelfRepairBase
		{
			get
			{
				return UtilityWPF.ColorFromHex("E8E0C1");
			}
		}
		public SpecularMaterial SelfRepairBaseSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0707691")), 60d);
			}
		}
		public Color SelfRepairCross
		{
			get
			{
				return UtilityWPF.ColorFromHex("43B23B");
			}
		}
		public SpecularMaterial SelfRepairCrossSpecular
		{
			get
			{
				return new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203DB2")), 85d);
			}
		}

		//	Light armor should be whitish (plastic)
		//	Heavy armor should be silverish (metal)
	}
}
