using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class MinimapHelper
    {
        #region Class: MineralBlipProps

        private class MineralBlipProps
        {
            public MineralBlipProps(Color diffuseColor, Color specularColor, double size)
            {
                this.DiffuseColor = diffuseColor;
                this.SpecularColor = specularColor;
                this.Size = size;
            }

            public readonly Color DiffuseColor;
            public readonly Color SpecularColor;
            public readonly double Size;
        }

        #endregion
        #region Class: Blip

        private class Blip
        {
            public Blip(IMapObject item, BlipVisual[] visuals)
            {
                this.Item = item;
                this.Visuals = visuals;

                this.HasUprightVisual = visuals.Any(o => o.IsVisualUpright);
            }

            public readonly IMapObject Item;
            public readonly BlipVisual[] Visuals;

            public readonly bool HasUprightVisual;
        }

        #endregion
        #region Class: BlipVisual

        private class BlipVisual
        {
            public BlipVisual(Visual3D visual, bool isVisualUpright, TranslateTransform3D translate, AxisAngleRotation3D rotate)
            {
                this.Visual = visual;
                this.IsVisualUpright = isVisualUpright;
                this.Translate = translate;
                this.Rotate = rotate;
            }

            public readonly Visual3D Visual;
            public readonly bool IsVisualUpright;

            public readonly TranslateTransform3D Translate;
            public readonly AxisAngleRotation3D Rotate;
        }

        #endregion

        #region Declaration Section

        private readonly Map _map;
        private readonly Viewport3D _viewport;

        private List<Blip> _blips = new List<Blip>();

        /// <summary>
        /// The same geometry is used for all the circular blips
        /// </summary>
        private Lazy<Geometry3D> _blipGeometry_Circle = new Lazy<Geometry3D>(() => UtilityWPF.GetSphere_LatLon(4, 1, 1, 1));

        private SortedList<MineralType, MineralBlipProps> _mineralColors = null;

        #endregion

        #region Constructor

        public MinimapHelper(Map map, Viewport3D viewport)
        {
            _map = map;
            _viewport = viewport;

            _map.ItemAdded += new EventHandler<MapItemArgs>(Map_ItemAdded);
            _map.ItemRemoved += new EventHandler<MapItemArgs>(Map_ItemRemoved);
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            foreach (Blip blip in _blips)
            {
                Point3D position = blip.Item.PositionWorld;

                foreach (BlipVisual visual in blip.Visuals)
                {
                    visual.Translate.OffsetX = position.X;
                    visual.Translate.OffsetY = position.Y;
                    visual.Translate.OffsetZ = position.Z;
                }
            }
        }

        public void OrientUprightBlips(Vector3D desired, Vector3D current)
        {
            double angle = Vector3D.AngleBetween(desired, current);

            if (Vector3D.DotProduct(Vector3D.CrossProduct(desired, current), new Vector3D(0, 0, 1)) < 0)
            {
                angle = 360 - angle;
            }

            IEnumerable<BlipVisual> uprightVisuals = _blips.
                Where(o => o.HasUprightVisual).
                SelectMany(o => o.Visuals).
                Where(o => o.IsVisualUpright);

            foreach (BlipVisual visual in uprightVisuals)
            {
                visual.Rotate.Angle = angle;
            }
        }

        #endregion

        #region Event Listeners

        private void Map_ItemAdded(object sender, MapItemArgs e)
        {
            #region Create blip

            Visual3D blip;
            List<Tuple<Visual3D, bool>> blips = new List<Tuple<Visual3D, bool>>(); ;

            if (e.Item is Asteroid)
            {
                blip = GetAsteroidBlip((Asteroid)e.Item);
                blips.Add(Tuple.Create(blip, false));
            }
            else if (e.Item is Mineral)
            {
                blip = GetMineralBlip((Mineral)e.Item);
                blips.Add(Tuple.Create(blip, false));
            }
            else if (e.Item is SpaceStation2D)
            {
                blip = GetStationBlip((SpaceStation2D)e.Item);
                blips.Add(Tuple.Create(blip, true));
            }
            else if (e.Item is Ship)
            {
                blip = GetShipBlip((Ship)e.Item);
                blips.Add(Tuple.Create(blip, true));

                blip = GetShipCompassBlip((Ship)e.Item);
                blips.Add(Tuple.Create(blip, false));
            }

            #endregion

            // Remove nulls (if the item isn't very significant, then no blip is made)
            blips = blips.Where(o => o.Item1 != null).ToList();
            if (blips.Count == 0)
            {
                return;
            }

            #region Create transforms

            List<BlipVisual> blipVisuals = new List<BlipVisual>();

            foreach (var tuple in blips)
            {
                // Transform
                Transform3DGroup transform = new Transform3DGroup();
                AxisAngleRotation3D rotate = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
                transform.Children.Add(new RotateTransform3D(rotate));
                TranslateTransform3D translate = new TranslateTransform3D(e.Item.PositionWorld.ToVector());
                transform.Children.Add(translate);

                tuple.Item1.Transform = transform;

                blipVisuals.Add(new BlipVisual(tuple.Item1, tuple.Item2, translate, rotate));
            }

            #endregion

            // Store it
            _blips.Add(new Blip(e.Item, blipVisuals.ToArray()));
            _viewport.Children.AddRange(blipVisuals.Select(o => o.Visual));
        }
        private void Map_ItemRemoved(object sender, MapItemArgs e)
        {
            for (int cntr = 0; cntr < _blips.Count; cntr++)
            {
                if (_blips[cntr].Item.Token == e.Item.Token)
                {
                    _viewport.Children.RemoveAll(_blips[cntr].Visuals.Select(o => o.Visual));
                    _blips.RemoveAt(cntr);
                    return;
                }
            }
        }

        #endregion

        #region Private Methods

        private Visual3D GetAsteroidBlip(Asteroid asteroid)
        {
            if (asteroid.Radius < 2)
            {
                // No need to flood the map with tiny asteroids
                return null;
            }

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.DimGray));
            //materials.Children.Add(new SpecularMaterial(Brushes.White, 20d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = _blipGeometry_Circle.Value;

            double[] astRad = new[] { asteroid.RadiusVect.X, asteroid.RadiusVect.Y, asteroid.RadiusVect.Z }.OrderByDescending(o => o).ToArray();
            double avgRad = Math3D.Avg(asteroid.RadiusVect.X, asteroid.RadiusVect.Y, asteroid.RadiusVect.Z);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(astRad[0] * 2.2, astRad[2] * 2.2, astRad[1] * .5));
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), StaticRandom.NextDouble(360))));
            geometry.Transform = transform;

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            // Exit Function
            return retVal;
        }

        private Visual3D GetMineralBlip(Mineral mineral)
        {
            //if (mineral.Radius < 2)    // limit by value instead of size
            //{
            //    // No need to flood the map with tiny minerals
            //    return null;
            //}

            // Using the mineral's color makes the map look very busy.  Instead, set an intensity of a solid color based on its relative value
            // in relation to the other minerals
            if (_mineralColors == null)
            {
                GetMineralBlipSprtCacheProps();
            }
            MineralBlipProps blipProps = _mineralColors[mineral.MineralType];
            double size = blipProps.Size * mineral.VolumeInCubicMeters;     // blipProps.Size is for a volume of 1, so adjust by volume

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(blipProps.DiffuseColor)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(blipProps.SpecularColor), 100d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = _blipGeometry_Circle.Value;
            geometry.Transform = new ScaleTransform3D(size, size, size * .25d);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            // Exit Function
            return retVal;
        }
        private void GetMineralBlipSprtCacheProps()
        {
            // Get the min/max values
            double minDollars = double.MaxValue;
            double maxDollars = double.MinValue;

            SortedList<MineralType, double> dollars = new SortedList<MineralType, double>();
            foreach (MineralType mineralType in Enum.GetValues(typeof(MineralType)))
            {
                //decimal suggestedDollars = Mineral.GetSuggestedCredits(mineralType);
                decimal suggestedDollars = ItemOptionsAstMin2D.GetCredits_Mineral(mineralType);
                double suggestedDollarsScaled = Math.Sqrt(Convert.ToDouble(suggestedDollars));    // taking the square root, because the values are exponential, and I want the color scale more linear
                dollars.Add(mineralType, suggestedDollarsScaled);

                if (suggestedDollarsScaled < minDollars)
                {
                    minDollars = suggestedDollarsScaled;
                }

                if (suggestedDollarsScaled > maxDollars)
                {
                    maxDollars = suggestedDollarsScaled;
                }
            }

            // Store color based on those values
            _mineralColors = new SortedList<MineralType, MineralBlipProps>();
            Color maxColor = Colors.Chartreuse;
            foreach (MineralType mineralType in Enum.GetValues(typeof(MineralType)))
            {
                Color diffuse, specular;
                double size;

                double dollar = dollars[mineralType];

                double colorPercent = UtilityCore.GetScaledValue_Capped(.33d, 1d, minDollars, maxDollars, dollar);
                diffuse = UtilityWPF.AlphaBlend(maxColor, Colors.Transparent, colorPercent);
                specular = diffuse;

                //size = UtilityCore.GetScaledValue_Capped(2d, 5d, minDollars, maxDollars, dollar);
                size = UtilityCore.GetScaledValue_Capped(4d, 10d, minDollars, maxDollars, dollar);

                _mineralColors.Add(mineralType, new MineralBlipProps(diffuse, specular, size));
            }
        }

        private static Visual3D GetStationBlip(SpaceStation2D station)
        {
            const double SIZE = 35;

            FrameworkElement flag = station.Flag;
            BitmapSource flagImage = UtilityWPF.RenderControl(flag, Convert.ToInt32(SpaceStation2D.FLAGWIDTH), Convert.ToInt32(SpaceStation2D.FLAGHEIGHT), false);

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.White));
            materials.Children.Add(new DiffuseMaterial(new ImageBrush(flagImage)));
            //materials.Children.Add(new SpecularMaterial(Brushes.White, 20d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            double avg = (SpaceStation2D.FLAGWIDTH + SpaceStation2D.FLAGHEIGHT) / 2;

            double width = SpaceStation2D.FLAGWIDTH * (SIZE / avg);
            double height = SpaceStation2D.FLAGHEIGHT * (SIZE / avg);

            Point max = new Point(width / 2, height / 2);
            Point min = new Point(-max.X, -max.Y);

            geometry.Geometry = UtilityWPF.GetSquare2D(min, max);

            // The flag was upside down
            geometry.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180));

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            // Exit Function
            return retVal;
        }

        private static Visual3D GetShipBlip(Ship ship)
        {
            //TODO:  This makes an arrow that always points north, which is just annoying.  Instead, make the ship a dot, but put the arrow along the edge
            // of the minimap (just translate the arrow by a certain Y)

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.DodgerBlue)));
            materials.Children.Add(new SpecularMaterial(Brushes.RoyalBlue, 20d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;

            List<TubeRingDefinition_ORIG> rings = new List<TubeRingDefinition_ORIG>();
            rings.Add(new TubeRingDefinition_ORIG(30, 18, 0, true, false));
            rings.Add(new TubeRingDefinition_ORIG(5, false));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(3, rings, false, true);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new TranslateTransform3D(0, 0, 20));
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            geometry.Transform = transform;

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;

            // Exit Function
            return retVal;
        }
        private static Visual3D GetShipCompassBlip(Ship ship)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D(true);
            retVal.Thickness = 2d;
            retVal.Color = Colors.DodgerBlue;
            retVal.AddLine(new Point3D(0, 170, 20), new Point3D(0, 1000, 20));

            // Exit Function
            return retVal;
        }

        #endregion
    }
}
