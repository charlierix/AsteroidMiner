using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;
using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Primitives3D;
using Game.Newt.NewtonDynamics_153;

namespace Game.Newt.AsteroidMiner_153
{
    /// <summary>
    /// This is a wrapper to the wpf viewers and newtondynamics world.  This gets handed high level objects (ship, asteroid, etc),
    /// and will update the various objects
    /// </summary>
    public class Map
    {
        //TODO:  Make add/remove events

        #region Class: MineralBlipProps

        private class MineralBlipProps
        {
            public Color DiffuseColor = Colors.Transparent;
            public Color SpecularColor = Colors.Transparent;
            public double Size = 1d;
        }

        #endregion

        #region Declaration Section

        private Geometry3D _blipGeometry = null;

        // Store the objects in separate buckets, so that they are easier to get to
        private List<SpaceStation> _spaceStations = new List<SpaceStation>();
        private List<ModelVisual3D> _spaceStationBlips = new List<ModelVisual3D>();        // these two lists are kept the same size

        private List<Asteroid> _asteroids = new List<Asteroid>();
        private List<ModelVisual3D> _asteroidBlips = new List<ModelVisual3D>();

        private List<Mineral> _minerals = new List<Mineral>();
        private List<ModelVisual3D> _mineralBlips = new List<ModelVisual3D>();

        private Ship _ship = null;
        private ModelVisual3D _shipBlip = null;
        private ModelVisual3D _shipCompassBlip = null;

        private List<SwarmBot2> _swarmbots = new List<SwarmBot2>();		// I don't bother with blips for these

        private SortedList<MineralType, MineralBlipProps> _mineralColors = null;

        #endregion

        #region Constructor

        public Map(Viewport3D viewport, Viewport3D viewportMap, World world)
        {
            _viewport = viewport;
            _viewportMap = viewportMap;
            _world = world;
        }

        #endregion

        #region Public Properties

        private Viewport3D _viewport = null;
        /// <summary>
        /// This is the main game viewer
        /// </summary>
        public Viewport3D Viewport
        {
            get
            {
                return _viewport;
            }
        }

        private Viewport3D _viewportMap = null;
        /// <summary>
        /// This is the mini map in the corner
        /// </summary>
        public Viewport3D ViewportMap
        {
            get
            {
                return _viewportMap;
            }
        }

        private World _world = null;
        /// <summary>
        /// The Newton physics world
        /// </summary>
        public World World
        {
            get
            {
                return _world;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is needed to keep the blips synced with their real world objects
        /// </summary>
        public void WorldUpdating()
        {
            #region Ship

            //Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,1,0), 

            _shipBlip.Transform = new MatrixTransform3D(_ship.PhysicsBody.VisualMatrix);
            _shipCompassBlip.Transform = new TranslateTransform3D(_ship.PositionWorld.ToVector());  // keep this one pointed north

            #endregion

            // Space Stations
            for (int cntr = 0; cntr < _spaceStations.Count; cntr++)
            {
                _spaceStationBlips[cntr].Transform = new TranslateTransform3D(_spaceStations[cntr].PositionWorld.ToVector());
            }

            // Asteroids
            for (int cntr = 0; cntr < _asteroids.Count; cntr++)
            {
                if (_asteroidBlips[cntr] != null)
                {
                    _asteroidBlips[cntr].Transform = new TranslateTransform3D(_asteroids[cntr].PositionWorld.ToVector());
                }
            }

            // Minerals
            for (int cntr = 0; cntr < _minerals.Count; cntr++)
            {
                if (_mineralBlips[cntr] != null)
                {
                    _mineralBlips[cntr].Transform = new TranslateTransform3D(_minerals[cntr].PositionWorld.ToVector());
                }
            }
        }

        //NOTE: The map won't load ModelVisual3D's to the main viewport, because the physics engine needs them added in it's constructor
        public void AddItem(IMapObject item)
        {
            ModelVisual3D blip = null;

            // These if statements are in order of what is most likely to be added
            if (item is Mineral)
            {
                #region Mineral

                blip = GetMineralBlip((Mineral)item);

                _minerals.Add((Mineral)item);
                _mineralBlips.Add(blip);

                if (blip != null)        // I may not bother making a blip for the tiny minerals
                {
                    _viewportMap.Children.Add(blip);
                }

                #endregion
            }
            else if (item is Asteroid)
            {
                #region Asteroid

                blip = GetAsteroidBlip((Asteroid)item);

                _asteroids.Add((Asteroid)item);
                _asteroidBlips.Add(blip);

                if (blip != null)        // I may not bother making a blip for the tiny asteroids
                {
                    _viewportMap.Children.Add(blip);
                }

                #endregion
            }
            else if (item is SpaceStation)
            {
                #region Space Station

                blip = GetSpaceStationBlip((SpaceStation)item);

                _spaceStations.Add((SpaceStation)item);
                _spaceStationBlips.Add(blip);

                _viewportMap.Children.Add(blip);

                #endregion
            }
            else if (item is Ship)
            {
                #region Ship

                _ship = (Ship)item;

                _shipBlip = GetShipBlip(_ship);
                _shipCompassBlip = GetShipCompassBlip(_ship);
                _viewportMap.Children.Add(_shipBlip);
                _viewportMap.Children.Add(_shipCompassBlip);

                #endregion
            }
            else if (item is SwarmBot2)
            {
                #region SwarmBot2

                _swarmbots.Add((SwarmBot2)item);

                #endregion
            }
            else
            {
                throw new ApplicationException("Unknown item type: " + item.ToString());
            }
        }
        //NOTE: The map WILL remove the ModelVisual3D's from the main viewport
        public void RemoveItem(IMapObject item)
        {
            int index = -1;

            if (item is Mineral)
            {
                #region Mineral

                index = _minerals.IndexOf((Mineral)item);

                if (_mineralBlips[index] != null)
                {
                    _viewportMap.Children.Remove(_mineralBlips[index]);
                }

                foreach (ModelVisual3D model in item.Visuals3D)
                {
                    _viewport.Children.Remove(model);
                }

                _minerals.RemoveAt(index);
                _mineralBlips.RemoveAt(index);

                _world.RemoveBody(item.PhysicsBody);

                #endregion
            }
            else if (item is Asteroid)
            {
                #region Asteroid

                index = _asteroids.IndexOf((Asteroid)item);

                if (_asteroidBlips[index] != null)
                {
                    _viewportMap.Children.Remove(_asteroidBlips[index]);
                }

                foreach (ModelVisual3D model in item.Visuals3D)
                {
                    _viewport.Children.Remove(model);
                }

                _asteroids.RemoveAt(index);
                _asteroidBlips.RemoveAt(index);

                _world.RemoveBody(item.PhysicsBody);

                #endregion
            }
            else if (item is SpaceStation)
            {
                #region Space Station

                index = _spaceStations.IndexOf((SpaceStation)item);

                _viewportMap.Children.Remove(_spaceStationBlips[index]);

                foreach (ModelVisual3D model in item.Visuals3D)
                {
                    _viewport.Children.Remove(model);
                }

                _spaceStations.RemoveAt(index);
                _spaceStationBlips.RemoveAt(index);

                #endregion
            }
            else if (item is Ship)
            {
                #region Ship

                throw new ApplicationException("finish this");

                _ship = null;
                _viewportMap.Children.Remove(_shipBlip);
                _shipBlip = null;

                foreach (ModelVisual3D model in item.Visuals3D)
                {
                    _viewport.Children.Remove(model);
                }

                _world.RemoveBody(item.PhysicsBody);

                #endregion
            }
            else if (item is SwarmBot2)
            {
                #region SwarmBot2

                ((SwarmBot2)item).ShouldDrawThrustLine = false;		// it's leaving its thrust line on the viewport (thrustline isn't returned by Visuals3D)
                ((SwarmBot2)item).ShouldShowDebugVisuals = false;

                foreach (ModelVisual3D model in item.Visuals3D)
                {
                    _viewport.Children.Remove(model);
                }

                _swarmbots.Remove((SwarmBot2)item);

                _world.RemoveBody(item.PhysicsBody);

                #endregion
            }
            else
            {
                throw new ApplicationException("Unknown item type: " + item.ToString());
            }
        }

        public IEnumerable<ModelVisual3D> GetObjects()
        {
            throw new ApplicationException("finish this");
        }
        /// <summary>
        /// This one returns the objects that are within range of the search center (the object's radius is taken into account)
        /// </summary>
        public IEnumerable<ModelVisual3D> GetObjects(Point3D centerSearch, double radiusSearch)
        {
            throw new ApplicationException("finish this");
        }

        public IEnumerable<Mineral> GetMinerals()
        {
            return _minerals;
        }

        #endregion

        #region Private Methods

        private ModelVisual3D GetSpaceStationBlip(SpaceStation spaceStation)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(Brushes.White));
            materials.Children.Add(new SpecularMaterial(Brushes.White, 20d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = GetBlipGeometry();
            geometry.Transform = new ScaleTransform3D(7, 7, 2);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(spaceStation.PositionWorld.ToVector());

            // Exit Function
            return retVal;
        }
        private ModelVisual3D GetAsteroidBlip(Asteroid asteroid)
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
            geometry.Geometry = GetBlipGeometry();
            geometry.Transform = new ScaleTransform3D(asteroid.Radius * 2, asteroid.Radius * 2, asteroid.Radius * .5);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(asteroid.PositionWorld.ToVector());

            // Exit Function
            return retVal;
        }
        private ModelVisual3D GetMineralBlip(Mineral mineral)
        {
            //if (mineral.Radius < 2)    // limit by value instead of size
            //{
            //    // No need to flood the map with tiny minerals
            //    return null;
            //}

            // Using the mineral's color makes the map look very busy.  Instead I will set an intensity of a solid color based on its relative value
            // in relation to the other minerals
            if (_mineralColors == null)
            {
                GetMineralBlipSprtCacheProps();
            }
            MineralBlipProps blipProps = _mineralColors[mineral.MineralType];

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(blipProps.DiffuseColor)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(blipProps.SpecularColor), 100d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = GetBlipGeometry();
            geometry.Transform = new ScaleTransform3D(blipProps.Size, blipProps.Size, blipProps.Size * .25d);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(mineral.PositionWorld.ToVector());

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
                if (mineralType == MineralType.Custom)
                {
                    continue;
                }

                decimal suggestedDollars = Mineral.GetSuggestedValue(mineralType);
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
                MineralBlipProps props = new MineralBlipProps();

                if (mineralType == MineralType.Custom)
                {
                    props.DiffuseColor = Colors.HotPink;      // not sure what to do here.  maybe I'll know more if I ever use custom minerals  :)
                    props.SpecularColor = props.DiffuseColor;
                    props.Size = 4;
                }
                else
                {
                    double dollar = dollars[mineralType];

                    double colorPercent = UtilityHelper.GetScaledValue_Capped(.33d, 1d, minDollars, maxDollars, dollar);
                    props.DiffuseColor = UtilityWPF.AlphaBlend(maxColor, Colors.Transparent, colorPercent);
                    props.SpecularColor = props.DiffuseColor;

                    props.Size = UtilityHelper.GetScaledValue_Capped(2d, 5d, minDollars, maxDollars, dollar);
                }

                _mineralColors.Add(mineralType, props);
            }
        }
        private ModelVisual3D GetMineralBlip_OLD(Mineral mineral)
        {
            //if (mineral.Radius < 2)    // limit by value instead of size
            //{
            //    // No need to flood the map with tiny minerals
            //    return null;
            //}

            Color blipColor = Color.FromArgb(255, mineral.DiffuseColor.R, mineral.DiffuseColor.G, mineral.DiffuseColor.B);

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(blipColor)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(blipColor), 100d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = GetBlipGeometry();
            geometry.Transform = new ScaleTransform3D(4, 4, 1);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(mineral.PositionWorld.ToVector());

            // Exit Function
            return retVal;
        }
        private ModelVisual3D GetShipBlip(Ship ship)
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

            //geometry.Transform = new ScaleTransform3D(7, 7, 2);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(ship.PositionWorld.ToVector());

            // Exit Function
            return retVal;
        }
        private ModelVisual3D GetShipBlip_OLD(Ship ship)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(ship.HullColor)));
            materials.Children.Add(new SpecularMaterial(Brushes.White, 20d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = GetBlipGeometry();
            geometry.Transform = new ScaleTransform3D(7, 7, 2);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(ship.PositionWorld.ToVector());

            // Exit Function
            return retVal;
        }
        private ModelVisual3D GetShipCompassBlip(Ship ship)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D(true);
            retVal.Thickness = 2d;
            retVal.Color = Colors.DodgerBlue;
            retVal.AddLine(new Point3D(0, 170, 20), new Point3D(0, 1000, 20));

            retVal.Transform = new TranslateTransform3D(ship.PositionWorld.ToVector());

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// The same geometry is used for all the blips
        /// </summary>
        private Geometry3D GetBlipGeometry()
        {
            if (_blipGeometry == null)
            {
                _blipGeometry = UtilityWPF.GetSphere(4, 1, 1, 1);
            }

            return _blipGeometry;
        }

        #endregion
    }
}
