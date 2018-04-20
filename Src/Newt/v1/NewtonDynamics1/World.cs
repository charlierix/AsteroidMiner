using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
{
    [ContentProperty("Content")]
    public class World : FrameworkElement, IAddChild, IDisposable
    {
        #region Enum: BodyFilterType

        public enum BodyFilterType
        {
            IncludeBodies,
            ExcludeBodies
        }

        #endregion

        #region Events

        public event WorldUpdatingHandler Updating;

        public event BodyTransformEventHandler BodyTransforming;

        #endregion

        #region Declaration Section

        public const double EarthGravity = -9.8;

        private CWorld _world;
        private readonly Collection<Body> _removed = new Collection<Body>();
        private int _frame;
        private DispatcherTimer _timer;
        private bool _isInitialised;
        private bool _inWorldUpdate;
        private bool _mustDispose;

        private double _simulationSpeed = 1d;

        private bool _shouldForce2D = false;

        /// <summary>
        /// This defines a rectangular cavity that objects collide against (keeps things inside, rather than just stopping
        /// them)
        /// </summary>
        private List<TerrianBody3D> _boundry = null;
        private Vector3D? _boundryMin = null;     // these are stored explicitly so I can force objects to get back inside (the terrains are to let the engine do the collisions, but fast moving objects still get through)
        private Vector3D? _boundryMax = null;

        #endregion

        #region Constructor

        public World()
        {
        }

        #endregion

        #region IAddChild Members

        void IAddChild.AddChild(Object value)
        {
            // check against null
            if (value == null)
                throw new ArgumentNullException("value");

            // we only can have one child
            if (this.Content != null)
                throw new ArgumentException("World can only have one child");

            // now we can actually set the content
            Content = (Viewport3D)value;
        }

        void IAddChild.AddText(string text)
        {
            // The only text we accept is whitespace, which we ignore.
            for (int i = 0; i < text.Length; i++)
            {
                if (!Char.IsWhiteSpace(text[i]))
                {
                    throw new ArgumentException("Non whitespace in added text", text);
                }
            }
        }

        #endregion
        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_inWorldUpdate)
                {
                    _mustDispose = true;
                }
                else if (_world != null)
                {
                    Pause(); // stop the body events

                    if (_boundry != null)
                    {
                        _boundry = null;
                    }

                    _world.Dispose();
                    _world = null;
                }
            }
        }

        #endregion

        #region Public Properties

        public bool InWorldUpdate
        {
            get { return _inWorldUpdate; }
        }

        public CWorld NewtonWorld
        {
            get { return _world; }
        }

        /// <summary>
        /// This is a multiplier telling how fast the simulation should run (multiplied against number of seconds
        /// since last frame)
        /// </summary>
        /// <remarks>
        /// NOTE:  Newton won't allow an elapsed time greater than 50 milliseconds in any one cycle, so if the
        /// calculated elapsed time is greater than this, I cheat and update the world multiple times.  If this takes
        /// longer than the timer's interval, then the simulation speed becomes limited by the hardware.
        /// </remarks>
        public double SimulationSpeed
        {
            get
            {
                return _simulationSpeed;
            }
            set
            {
                if (value <= 0d)
                {
                    throw new ArgumentException("SimulationSpeed must be greater than zero: " + value.ToString());
                }

                _simulationSpeed = value;
            }
        }

        /// <summary>
        /// If this is true, then all objects will stay coerced to a Z of zero (and any rotations along the X or Y axis will
        /// be zeroed)
        /// </summary>
        public bool ShouldForce2D
        {
            get
            {
                return _shouldForce2D;
            }
            set
            {
                _shouldForce2D = value;
            }
        }

        public double TimeStep
        {
            get
            {
                return _world.TimeStep;
            }
        }

        #region DependencyProperty: MinimumFrameRate

        public static readonly DependencyProperty MinimumFrameRateProperty = DependencyProperty.Register("MinimumFrameRate", typeof(double), typeof(World), new PropertyMetadata(60.0, MinimumFrameRateChanged, CoerceMinimumFrameRate));

        private static void MinimumFrameRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            double value = (double)e.NewValue;

            World world = (World)d;

            world.NewtonWorld.MinimumFrameRate = (float)value;
        }

        private static object CoerceMinimumFrameRate(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;

            // clamp bewteen 60 & 1000 fps
            return Math.Max(60, Math.Min(value, 1000));
        }

        public double MinimumFrameRate
        {
            get { return (double)GetValue(MinimumFrameRateProperty); }
            set { SetValue(MinimumFrameRateProperty, value); }
        }

        #endregion
        #region DependencyProperty: Gravity

        public static readonly DependencyProperty GravityProperty = DependencyProperty.Register("Gravity", typeof(double), typeof(World), new FrameworkPropertyMetadata(EarthGravity, FrameworkPropertyMetadataOptions.None));

        public double Gravity
        {
            get { return (double)GetValue(GravityProperty); }
            set { SetValue(GravityProperty, value); }
        }

        #endregion
        #region DependencyProperty: Content

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(Viewport3D), typeof(World), new PropertyMetadata(null, OnContentChanged));

        private static void OnContentChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            World world = (World)sender;

            if (!(e.NewValue is Viewport3D))
                throw new ArgumentException("The World.Content has to be a Viewport3D.", "Content");

            // check to make sure we're attempting to set something new
            if (e.NewValue != e.OldValue)
            {
                Viewport3D oldContent = (Viewport3D)e.OldValue;
                Viewport3D newContent = (Viewport3D)e.NewValue;

                if (oldContent != null)
                {
                    // remove the previous child
                    world.RemoveVisualChild(oldContent);
                    world.RemoveLogicalChild(oldContent);
                    oldContent.Initialized -= world.content_Initialized;

                    World.SetWorld(oldContent, null);
                }

                DependencyObject parent = VisualTreeHelper.GetParent(newContent);
                if (parent == null)
                {
                    // link in the new child
                    world.AddLogicalChild(newContent);
                    world.AddVisualChild(newContent);
                }

                // let anyone know that derives from us that there was a change
                world.OnViewportContentChange(oldContent, newContent);

                // data bind to what is below us so that we have the same width/height
                // as the Viewport3D being enhanced
                // create the bindings now for use later
                world.BindToContentsWidthHeight(newContent);

                // Invalidate measure to indicate a layout update may be necessary
                world.InvalidateMeasure();

                newContent.Initialized += world.content_Initialized;

                World.SetWorld(newContent, world);
            }
        }

        public Viewport3D Content
        {
            get { return (Viewport3D)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        #endregion
        #region DependencyProperty: IsPaused

        public static readonly DependencyProperty IsPausedProperty = DependencyProperty.Register("IsPaused", typeof(bool), typeof(World), new PropertyMetadata(true, OnIsPausedChanged));

        internal static void OnIsPausedChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            World world = (World)sender;
            bool paused = (bool)e.NewValue;
            if (world._isInitialised)
            {
                if (paused)
                    world.Pause();
                else
                    world.UnPause();
            }
        }

        public bool IsPaused
        {
            get { return (bool)GetValue(IsPausedProperty); }
            set { SetValue(IsPausedProperty, value); }
        }

        #endregion
        #region DependencyProperty: Viewport

        // Using a DependencyProperty as the backing store for Viewport.  This enables animation, styling, binding, etc...
        protected static readonly DependencyPropertyKey ViewportPropertyKey = DependencyProperty.RegisterReadOnly("Viewport", typeof(Viewport3D), typeof(World), null);// new PropertyMetadata(ViewportPropertyChanged));

        public static readonly DependencyProperty ViewportProperty = ViewportPropertyKey.DependencyProperty;

        //private static void ViewportPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        // {
        // }

        public Viewport3D Viewport
        {
            get { return (Viewport3D)GetValue(ViewportProperty); }
        }

        #endregion
        #region DependencyProperty: SolverModel

        public static readonly DependencyProperty SolverModelProperty = DependencyProperty.Register("SolverModel", typeof(SolverModel), typeof(World), new PropertyMetadata(SolverModel.ExactMode, SolverModelPropertyChanged));

        private static void SolverModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var world = (World)d;

            if (world._isInitialised)
                world._world.SetSolverModel((SolverModel)e.NewValue);
        }

        public SolverModel SolverModel
        {
            get { return (SolverModel)GetValue(SolverModelProperty); }
            set { SetValue(SolverModelProperty, value); }
        }

        #endregion
        #region DependencyProperty: FrictionModel

        public static readonly DependencyProperty FrictionModelProperty = DependencyProperty.Register("FrictionModel", typeof(FrictionModel), typeof(World), new PropertyMetadata(FrictionModel.ExactModel, FrictionModelPropertyChanged));

        private static void FrictionModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var world = (World)d;

            if (world._isInitialised)
                world.FrictionModel = (FrictionModel)e.NewValue;
        }

        public FrictionModel FrictionModel
        {
            get { return (FrictionModel)GetValue(FrictionModelProperty); }
            set { SetValue(FrictionModelProperty, value); }
        }

        #endregion

        #region AttatchedProperty: Body

        public static readonly DependencyProperty BodyProperty = DependencyProperty.RegisterAttached("Body", typeof(Body), typeof(World), new PropertyMetadata(BodyPropertyChanged));

        public static void SetBody(ModelVisual3D visual, Body value)
        {
            if (visual == null) throw new ArgumentNullException("visual");

            visual.SetValue(BodyProperty, value);
        }

        public static Body GetBody(ModelVisual3D visual)
        {
            if (visual == null) throw new ArgumentNullException("visual");

            return (Body)visual.GetValue(BodyProperty);
        }

        public static Body GetBodyInherited(ModelVisual3D visual)
        {
            while (visual != null)
            {
                Body body = GetBody(visual);
                if (body == null)
                    visual = (VisualTreeHelper.GetParent(visual) as ModelVisual3D);
                else
                    return body;
            }

            return null;
        }

        private static void BodyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) throw new ArgumentNullException("d");

            ModelVisual3D visual = (d as ModelVisual3D);
            if (visual == null)
                throw new InvalidOperationException("A Body can only be attached to a ModelVisual3D.");

            if (e.OldValue != null)
            {
                Visual3DBodyBase body = (e.OldValue as Visual3DBodyBase);
                if (body != null)
                    body.Visual = null;
            }

            if (e.NewValue != null)
            {
                Visual3DBodyBase body = (e.NewValue as Visual3DBodyBase);
                if (body != null)
                    body.Visual = visual;
            }
        }

        #endregion
        #region AttatchedProperty: Collision

        public static readonly DependencyProperty CollisionMaskProperty = DependencyProperty.RegisterAttached("CollisionMask", typeof(CollisionMask), typeof(World), new PropertyMetadata(CollisionPropertyChanged));

        public static void SetCollisionMask(Geometry3D geometry, CollisionMask value)
        {
            if (geometry == null) throw new ArgumentNullException("geometry");

            geometry.SetValue(CollisionMaskProperty, value);
        }

        public static CollisionMask GetCollisionMask(Geometry3D geometry)
        {
            if (geometry == null) throw new ArgumentNullException("geometry");

            return (CollisionMask)geometry.GetValue(CollisionMaskProperty);
        }

        private static void CollisionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) throw new ArgumentNullException("d");

            if (e.NewValue != null)
            {
                GeometrylBasedCollisionMask collisionMask = (e.NewValue as GeometrylBasedCollisionMask);
                if (collisionMask != null)
                {
                    Geometry3D geometry = (d as Geometry3D);
                    if (geometry == null)
                    {
                        ModelVisual3D visual = (d as ModelVisual3D);
                        if (visual == null)
                            throw new InvalidOperationException("A Collision Mask can only be attached to a ModelVisual3D or Geometry3D.");

                        if (visual.Content == null)
                            throw new InvalidOperationException("The ModelVisual3D does not contain a Model.");

                        if (!(visual.Content is GeometryModel3D))
                            throw new InvalidOperationException("Only GeometryModel3D supported.");

                        geometry = ((GeometryModel3D)visual.Content).Geometry;
                    }

                    collisionMask.Geometry = geometry;
                }
            }
        }

        #endregion
        #region AttatchedProperty: World

        public static readonly DependencyProperty WorldProperty = DependencyProperty.RegisterAttached("World", typeof(World), typeof(World), new PropertyMetadata(WorldPropertyChanged));

        public static World GetWorld(Viewport3D viewport)
        {
            return (World)viewport.GetValue(WorldProperty);
        }

        public static void SetWorld(Viewport3D viewport, World value)
        {
            viewport.SetValue(WorldProperty, value);
        }

        private static void WorldPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var world = (World)e.OldValue;
            if (world != null)
                world.SetValue(World.ViewportPropertyKey, null);

            world = (World)e.NewValue;
            if (world != null)
                world.SetValue(World.ViewportPropertyKey, d);
        }

        #endregion

        #endregion
        #region Internal Properties

        internal bool CanRaiseBodyTransforming
        {
            get { return (BodyTransforming != null); }
        }

        #endregion

        #region Public Methods

        // TODO: uninitialise the old bodys
        public void InitialiseBodies()
        {
            Initialise();

            Viewport3D viewport = Viewport;
            if (viewport != null)
            {
                InitialiseBodies(null, viewport.Children);

                _isInitialised = true;

                if (!this.IsPaused)
                    UnPause();
            }
        }

        /// <summary>
        /// This lets you define a cube that everything is inside of, and will collide against
        /// </summary>
        public void SetCollisionBoundry(Viewport3D viewport, Vector3D min, Vector3D max)
        {
            List<Point3D[]> dummy1, dummy2;
            SetCollisionBoundry(out dummy1, out dummy2, viewport, min, max);
        }
        /// <summary>
        /// This lets you define a cube that everything is inside of, and will collide against.
        /// This overload returns lines so they could be drawn
        /// </summary>
        /// <param name="innerLines">element0 - from, element1 - to</param>
        /// <param name="outerLines">element0 - from, element1 - to</param>
        public void SetCollisionBoundry(out List<Point3D[]> innerLines, out List<Point3D[]> outerLines, Viewport3D viewport, Vector3D min, Vector3D max)
        {
            const double MAXDEPTH = 1000d;

            if (_boundry != null)
            {
                foreach (TerrianBody3D prev in _boundry)
                {
                    this.RemoveBody(prev);
                }
                _boundry.Clear();
                _boundry = null;
            }

            _boundry = new List<TerrianBody3D>();

            #region Calculate Depths

            // If the boundry is just plates, then fast moving objects will pass thru
            double depthX = ((max.Y - min.Y) + (max.Z - min.Z)) / 2d;
            double depthY = ((max.X - min.X) + (max.Z - min.Z)) / 2d;
            double depthZ = ((max.X - min.X) + (max.Y - min.Y)) / 2d;

            if (depthX > MAXDEPTH)
            {
                depthX = MAXDEPTH;
            }
            if (depthY > MAXDEPTH)
            {
                depthY = MAXDEPTH;
            }
            if (depthZ > MAXDEPTH)
            {
                depthZ = MAXDEPTH;
            }

            #endregion

            #region Terrains

            // Left
            ModelVisual3D model = GetWPFCube(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X, max.Y + depthY, max.Z));
            viewport.Children.Add(model);
            _boundry.Add(new TerrianBody3D(this, model));		// there's no need to call this.Add(), it's already added

            // Right
            model = GetWPFCube(new Point3D(max.X, min.Y - depthY, min.Z), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));
            viewport.Children.Add(model);
            _boundry.Add(new TerrianBody3D(this, model));


            // Top
            model = GetWPFCube(new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y + depthY, max.Z));
            viewport.Children.Add(model);
            _boundry.Add(new TerrianBody3D(this, model));

            // Bottom
            model = GetWPFCube(new Point3D(min.X, min.Y - depthY, min.Z), new Point3D(max.X, min.Y, max.Z));
            viewport.Children.Add(model);
            _boundry.Add(new TerrianBody3D(this, model));


            // Far
            model = GetWPFCube(new Point3D(min.X, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z));
            viewport.Children.Add(model);
            _boundry.Add(new TerrianBody3D(this, model));

            // Near
            model = GetWPFCube(new Point3D(min.X - depthX, min.Y - depthY, max.Z), new Point3D(max.X, max.Y + depthY, max.Z + depthZ));
            viewport.Children.Add(model);
            _boundry.Add(new TerrianBody3D(this, model));

            #endregion

            #region Inner Lines

            innerLines = new List<Point3D[]>();

            // Far
            innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, min.Z), new Point3D(max.X, min.Y, min.Z) });

            innerLines.Add(new Point3D[] { new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y, min.Z) });

            innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, min.Z), new Point3D(min.X, max.Y, min.Z) });

            innerLines.Add(new Point3D[] { new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, max.Y, min.Z) });

            // Near
            innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, min.Y, max.Z) });

            innerLines.Add(new Point3D[] { new Point3D(min.X, max.Y, max.Z), new Point3D(max.X, max.Y, max.Z) });

            innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, max.Z), new Point3D(min.X, max.Y, max.Z) });

            innerLines.Add(new Point3D[] { new Point3D(max.X, min.Y, max.Z), new Point3D(max.X, max.Y, max.Z) });

            // Connecting Z's
            innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, min.Z), new Point3D(min.X, min.Y, max.Z) });

            innerLines.Add(new Point3D[] { new Point3D(min.X, max.Y, min.Z), new Point3D(min.X, max.Y, max.Z) });

            innerLines.Add(new Point3D[] { new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, min.Y, max.Z) });

            innerLines.Add(new Point3D[] { new Point3D(max.X, max.Y, min.Z), new Point3D(max.X, max.Y, max.Z) });

            #endregion
            #region Outer Lines

            outerLines = new List<Point3D[]>();

            // Far
            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ) });

            // Near
            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ), new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ), new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ) });

            // Connecting Z's
            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ), new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ) });

            outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ) });

            #endregion

            // Nothing should be placed outside these boundries, so let the engine optimize
            this.SetSize(new Vector3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Vector3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));
            _boundryMin = min;  // storing the more constrained boundry, because if anything gets outside the actual boundry, it's frozen
            _boundryMax = max;
        }
        /// <summary>
        /// Anything that tries to go beyond this, seems to just get stuck
        /// </summary>
        public void SetSize(Vector3D min, Vector3D max)
        {
            _world.SetSize(min, max);
            _boundryMin = min;
            _boundryMax = max;
        }

        public void Pause()
        {
            this.IsPaused = true;

            if (_world != null)
            {
                if (!_world.Pause)
                {
                    if (_timer != null)
                    {
                        _timer.IsEnabled = false;
                        _timer.Tick -= _timer_Tick;
                        _timer = null;
                    }

                    _world.Pause = true;
                }
            }
        }
        public void UnPause()
        {
            this.IsPaused = false;

            if (_world != null)
                if (_world.Pause)
                {
                    // I need to call this, or when the timer first fires, the clock will see a large time gap, and newton will take a long time
                    // to calculate new positions, then everything will jump when it finally does start running again.  Just bad
                    RealtimeClock.Update();

                    _world.Pause = false;

                    _timer = new DispatcherTimer();
                    _timer.Interval = new TimeSpan(0, 0, 0, 0, 25);
                    _timer.Tick += _timer_Tick;
                    _timer.IsEnabled = true;
                }
        }

        public void Update()
        {
            // Elapsed time is in seconds
            const double MAXELAPSEDTIME = 1d / 20d;		// newton won't allow > 20 fps during a single update

            if (_world != null && _timer == null)
            {
                return;
            }

            // Figure out how much time has elapsed since the last update
            RealtimeClock.Update();
            double elapsedTime = RealtimeClock.TimeStep;
            elapsedTime *= _simulationSpeed;

            if (elapsedTime > MAXELAPSEDTIME)
            {
                #region Call multiple times

                double remaining = elapsedTime;

                while (remaining > 0d)
                {
                    if (remaining > MAXELAPSEDTIME)
                    {
                        Update(MAXELAPSEDTIME);
                        remaining -= MAXELAPSEDTIME;
                    }
                    else
                    {
                        // To be consistent with the way the original update method was designed, I need to update the
                        // tracker clock now
                        RealtimeClock.Update();

                        Update(remaining);
                        break;
                    }
                }

                #endregion
            }
            else
            {
                Update(elapsedTime);
            }
        }
        private void Update(double elapsedTime)
        {
            if (_world != null && _timer == null)
            {
                return;
            }

            // Tell the world to do its thing
            _inWorldUpdate = true;
            _world.Update(Convert.ToSingle(elapsedTime));
            _inWorldUpdate = false;

            if (_mustDispose)
            {
                Dispose(true);
            }
            else
            {
                // Remove anything that needs to be removed
                if (_removed.Count > 0)
                {
                    foreach (Body body in _removed)
                    {
                        body.Dispose();
                    }

                    _removed.Clear();
                }

                _frame++;

                // Raise an event
                if (Updating != null)
                {
                    Updating(this, new WorldUpdatingArgs(elapsedTime));
                }
            }
        }

        public void AddBody(Body body)
        {
            body.NewtonBody.ApplyForceAndTorque += body_ApplyForceAndTorque;
        }
        public void RemoveBody(Body body)
        {
            body.NewtonBody.ApplyForceAndTorque -= body_ApplyForceAndTorque;

            _removed.Add(body);
        }

        public static void DisposeBody(ModelVisual3D visual)
        {
            Body body = GetBody(visual);
            if (body != null)
            {
                body.Dispose();
                World.SetBody(visual, null);
            }
        }

        /// <summary>
        /// NOTE: The ray must be added to the viewport, or this overload won't work
        /// NOTE: Taking in a ray instead of simply taking two Point3D's seems overly complex.  Why the need for the ray to be added to the viewport?  Newton doesn't need that...
        /// </summary>
        public RayCastResult CastRay(Ray ray, BodyFilterType filterType, params Body[] bodies)
        {
            Matrix3D localToWorld = MathUtils.GetTransformToWorld(ray);

            Vector3D direction;
            if (ray.DirectionOrigin == ObjectOrigin.Local)
            {
                direction = localToWorld.Transform(ray.Direction);
            }
            else
            {
                direction = ray.Direction;
            }

            Point3D position = localToWorld.Transform(new Point3D());

            // Call my overload
            return CastRay(position, direction, ray.RayLength, filterType, bodies);
        }
        public RayCastResult CastRay(Point3D position, Vector3D direction, double rayLength, BodyFilterType filterType, params Body[] bodies)
        {

            //TODO:  If the ray starts outside the boundries, then this method will fail.  Fix that here, so the user doesn't
            // have to worry about it.  Also, if the ray length would cause an endpoint outside the bounds
            //
            // Also, if I fixed the start distance, then I need to add that back into the result, so it's transparent to the caller


            CBody[] bodyHandles = null;
            if (bodies.Length > 0)
            {
                // make a list of newton handles
                bodyHandles = new CBody[bodies.Length];
                for (int i = 0; i < bodies.Length; i++)
                {
                    bodyHandles[i] = bodies[i].NewtonBody;
                }
            }

            //TODO:  Always skip the boundry terrains

            EventHandler<CWorldRayPreFilterEventArgs> preFilterHandler = null;
            if (bodyHandles != null)
            {
                // This artificial method gets called from within NewtonWorld.WorldRayCast
                preFilterHandler = delegate (object sender, CWorldRayPreFilterEventArgs preFilterArgs)
                {
                    switch (filterType)
                    {
                        case BodyFilterType.ExcludeBodies:
                            preFilterArgs.Skip = (Array.IndexOf(bodyHandles, preFilterArgs.Body)) >= 0;
                            break;
                        case BodyFilterType.IncludeBodies:
                            preFilterArgs.Skip = (Array.IndexOf(bodyHandles, preFilterArgs.Body)) < 0;
                            break;
                    }
                };
            }

            List<CWorldRayFilterEventArgs> hitTestResults = new List<CWorldRayFilterEventArgs>();

            Vector3D posAsVector = position.ToVector();		// newt wants a vector instead of a point3d

            // Ask newton to do the hit test (it will invoke the filter delegate, whose implementation is above)
            this.NewtonWorld.WorldRayCast(posAsVector, posAsVector + (direction * rayLength),
                delegate (object sender, CWorldRayFilterEventArgs filterArgs)
                {
                    hitTestResults.Add(filterArgs);
                },
                null, preFilterHandler);

            if (hitTestResults.Count > 0)
            {
                // Find the closest one
                CWorldRayFilterEventArgs hitTestResult = null;
                double distance = double.MaxValue;
                for (int i = 0; i < hitTestResults.Count; i++)
                {
                    double d = (hitTestResults[i].IntersetParam * rayLength);
                    if (d < distance)
                    {
                        distance = d;
                        hitTestResult = hitTestResults[i];
                    }
                }

                Body resultBody = UtilityNewt.GetBodyFromUserData(hitTestResult.Body);
                if (resultBody == null)
                {
                    throw new ApplicationException("Couldn't get the Body from the CBody.UserData");
                }

                // Exit Function
                return new RayCastResult(resultBody, distance, hitTestResult.HitNormal, hitTestResult.IntersetParam);
            }
            else
            {
                // Nothing found
                return null;
            }
        }

        /*
        public RayCastResult CastRay_ORIG(Ray ray, BodyFilterType filterType, params Body[] bodies)
        {
            CBody[] bodyHandles = null;
            if (bodies.Length > 0)
            {
                // make a list of newton handles
                bodyHandles = new CBody[bodies.Length];
                for (int i = 0; i < bodies.Length; i++)
                    bodyHandles[i] = bodies[i].NewtonBody;
            }

            List<CWorldRayFilterEventArgs> _hitTestResults = new List<CWorldRayFilterEventArgs>();

            Matrix3D localToWorld = MathUtils.GetTransformToWorld(ray);

            Vector3D direction =
                (ray.DirectionOrigin == ObjectOrigin.Local)
                ?
                localToWorld.Transform(ray.Direction)
                :
                ray.Direction;

            var origin = (Vector3D)localToWorld.Transform(new Point3D());

            EventHandler<CWorldRayPreFilterEventArgs> preFilterHandler = null;
            if (bodyHandles != null)
                preFilterHandler = delegate(object sender, CWorldRayPreFilterEventArgs preFilterArgs)
                {
                    switch (filterType)
                    {
                        case BodyFilterType.ExcludeBodies:
                            preFilterArgs.Skip = (Array.IndexOf(bodyHandles, preFilterArgs.Body)) >= 0;
                            break;
                        case BodyFilterType.IncludeBodies:
                            preFilterArgs.Skip = (Array.IndexOf(bodyHandles, preFilterArgs.Body)) < 0;
                            break;
                    }
                };

            this.NewtonWorld.WorldRayCast(
                origin,
                origin + (direction * ray.RayLength),
                delegate(object sender, CWorldRayFilterEventArgs filterArgs)
                {
                    _hitTestResults.Add(filterArgs);
                },
                null,
                preFilterHandler);

            if (_hitTestResults.Count > 0)
            {
                CWorldRayFilterEventArgs _hitTestResult = null;
                double distance = double.MaxValue;
                for (int i = 0; i < _hitTestResults.Count; i++)
                {
                    double d = (_hitTestResults[i].IntersetParam * ray.RayLength);
                    if (d < distance)
                    {
                        distance = d;
                        _hitTestResult = _hitTestResults[i];
                    }
                }

                return new RayCastResult((Body)_hitTestResult.Body.UserData, distance, _hitTestResult.HitNormal, _hitTestResult.IntersetParam);
            }
            else
            {
                return null;
            }
        }
        */

        //TODO:  Make a separate method that returns the terrain
        public List<ConvexBody3D> GetBodies()
        {
            List<ConvexBody3D> retVal = new List<ConvexBody3D>();

            foreach (CBody body in _world.Bodies)
            {
                ConvexBody3D bodyCast = UtilityNewt.GetConvexBodyFromUserData(body);
                if (bodyCast == null)
                {
                    continue;
                }

                retVal.Add(bodyCast);
            }

            return retVal;
        }

        #endregion
        #region Internal Methods

        /// <summary>
        /// Extenders of Viewport3DDecorator can override this function to be notified
        /// when the Content property changes
        /// </summary>
        /// <param name="oldContent">The old value of the Content property</param>
        /// <param name="newContent">The new value of the Content property</param>
        protected internal virtual void OnViewportContentChange(UIElement oldContent, UIElement newContent)
        {
        }

        internal void RaiseBodyTransforming(Body body, BodyTransformEventArgs e)
        {
            if (BodyTransforming != null)
                BodyTransforming(body, e);
        }

        /*
        internal void InitialiseCollisionMask(CollisionMask collisionMask)
        {
            if (collisionMask == null) throw new ArgumentNullException("collisionMask");

            collisionMask.Initialise(this);
        }
        */

        #endregion
        #region Protected Methods

        protected void Initialise()
        {
            if (_world == null)
            {
                _world = new CWorld();
                _world.Pause = this.IsPaused;
                _world.MinimumFrameRate = (float)this.MinimumFrameRate;
                _world.SetSolverModel(this.SolverModel);
                _world.SetFrictionModel(this.FrictionModel);
            }
        }

        #endregion

        #region Event Listeners

        private void _timer_Tick(object sender, EventArgs e)
        {
            if (_simulationSpeed > 1d)
            {
            }

            Update();
        }

        private void body_ApplyForceAndTorque(object sender, CApplyForceAndTorqueEventArgs e)
        {
            CBody newtonBody = (CBody)sender;

            Body body = UtilityNewt.GetBodyFromUserData(newtonBody);

            bool apply = (body == null) || body.ApplyGravity;

            if (apply)
            {
                newtonBody.AddForce(new Vector3D(0, this.Gravity * newtonBody.MassMatrix.m_Mass, 0));
            }

            if (body != null)
            {
                FixPositionsAndVelocities(body, e);
            }
        }

        private void content_Initialized(object sender, EventArgs e)
        {
            InitialiseBodies();
        }

        #endregion
        #region Overrides

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }

        /// <summary>
        /// Returns the number of Visual children this element has.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return (Content == null ? 0 : 1); }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            int orginalIndex = index;

            // see if it's the content
            if (Content != null && index == 0)
            {
                return Content;
            }

            // if we didn't return then the index is out of range - throw an error
            throw new ArgumentOutOfRangeException("index", orginalIndex, "Out of range visual requested");
        }

        /// <summary> 
        /// Returns an enumertor to this element's logical children
        /// </summary>
        protected override IEnumerator LogicalChildren
        {
            get
            {
                Visual[] logicalChildren = new Visual[VisualChildrenCount];

                for (int i = 0; i < VisualChildrenCount; i++)
                {
                    logicalChildren[i] = GetVisualChild(i);
                }

                // return an enumerator to the ArrayList
                return logicalChildren.GetEnumerator();
            }
        }

        /// <summary>
        /// Updates the DesiredSize of the Viewport3DDecorator
        /// </summary>
        /// <param name="constraint">The "upper limit" that the return value should not exceed</param>
        /// <returns>The desired size of the Viewport3DDecorator</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size finalSize = new Size();

            // measure our Viewport3D(Enhancer)
            if (Content != null)
            {
                Content.Measure(constraint);
                finalSize = Content.DesiredSize;
            }

            return finalSize;
        }

        /// <summary>
        /// Arranges the Pre and Post Viewport children, and arranges itself
        /// </summary>
        /// <param name="arrangeSize">The final size to use to arrange itself and its children</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // arrange our Viewport3D(Enhancer)
            if (Content != null)
            {
                Content.Arrange(new Rect(arrangeSize));
            }

            return arrangeSize;
        }

        #endregion

        #region Private Methods

        private void InitialiseBodies(Body parent, ICollection<Visual3D> collection)
        {
            if (collection.Count > 0)
            {
                Visual3D[] items = new Visual3D[collection.Count];
                collection.CopyTo(items, 0);

                foreach (Visual3D visual3D in items)
                {
                    InitialiseBody(parent, visual3D);
                }
            }
        }

        private void InitialiseBody(Body parent, Visual3D visual3D)
        {
            ModelVisual3D model = (visual3D as ModelVisual3D);
            if (model != null)
            {
                Body body = GetBody(model);
                if (body != null)
                {
                    body.Initialise(this);

                    if (body.Joint != null)
                    {
                        if (!body.Joint.IsInitialised)
                        {
                            if (parent == null)
                                parent = new NullBody(this);

                            body.Joint.Initialise(this, parent, body);
                        }
                    }
                }

                // pass the last visual that had a body attached (if body is null then return the parent (which should not be null)
                InitialiseBodies(body ?? parent, model.Children);
            }
        }

        /// <summary>
        /// Data binds the (Max/Min)Width and (Max/Min)Height properties to the same
        /// ones as the content.  This will make it so we end up being sized to be
        /// exactly the same ActualWidth and ActualHeight as what is below us.
        /// </summary>
        /// <param name="newContent">What to bind to</param>
        private void BindToContentsWidthHeight(UIElement newContent)
        {
            // bind to width height
            Binding _widthBinding = new Binding("Width");
            _widthBinding.Mode = BindingMode.OneWay;
            Binding _heightBinding = new Binding("Height");
            _heightBinding.Mode = BindingMode.OneWay;

            _widthBinding.Source = newContent;
            _heightBinding.Source = newContent;

            BindingOperations.SetBinding(this, WidthProperty, _widthBinding);
            BindingOperations.SetBinding(this, HeightProperty, _heightBinding);

            // bind to max width and max height
            Binding _maxWidthBinding = new Binding("MaxWidth");
            _maxWidthBinding.Mode = BindingMode.OneWay;
            Binding _maxHeightBinding = new Binding("MaxHeight");
            _maxHeightBinding.Mode = BindingMode.OneWay;

            _maxWidthBinding.Source = newContent;
            _maxHeightBinding.Source = newContent;

            BindingOperations.SetBinding(this, MaxWidthProperty, _maxWidthBinding);
            BindingOperations.SetBinding(this, MaxHeightProperty, _maxHeightBinding);

            // bind to min width and min height
            Binding _minWidthBinding = new Binding("MinWidth");
            _minWidthBinding.Mode = BindingMode.OneWay;
            Binding _minHeightBinding = new Binding("MinHeight");
            _minHeightBinding.Mode = BindingMode.OneWay;

            _minWidthBinding.Source = newContent;
            _minHeightBinding.Source = newContent;

            BindingOperations.SetBinding(this, MinWidthProperty, _minWidthBinding);
            BindingOperations.SetBinding(this, MinHeightProperty, _minHeightBinding);
        }

        private ModelVisual3D GetWPFCube(Point3D min, Point3D max)
        {
            // Material
            //MaterialGroup materials = new MaterialGroup();
            //if (useColor)
            //{
            //Color color = Color.FromArgb(64, 128, 128, 128);
            //materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            //}

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            //geometry.Material = materials;
            //geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetCube(min, max);

            // Transform
            //Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d))));
            //transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVector(_rand, _boundryMin, _boundryMax)));

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            //retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This is a method I threw in that will enforce that bodies don't fly out farther than they are supposed to
        /// NOTE: This must be called from within the ApplyForceAndTorque callback
        /// </summary>
        private void FixPositionsAndVelocities(Body body, CApplyForceAndTorqueEventArgs e)
        {
            const double RETURNVELOCITY = 0d;
            double ZACCEL = 10;

            if (body == null)
            {
                return;
            }
            else if (body is TerrianBody3D)
            {
                return;
            }

            // Get the center of mass in world coords
            Point3D centerMassWorld = body.PositionToWorld(body.CenterOfMass);

            // These are all in world coords
            //body.NewtonBody.AddForce(force);
            //body.NewtonBody.AddTorque(torque);
            //body.NewtonBody.AddImpulse(deltaVelocity, positionOnBody);

            if (_boundryMin != null)        // if min is non null, _boundryMax is also non null.  I don't want to waste the processor checking that each frame
            {
                #region Stay inside bounding box

                // Set the velocity going away to zero, apply a force

                //Vector3D velocityWorld = body.DirectionToWorld(body.Velocity);
                Vector3D velocityWorld = body.Velocity;    // already in world coords
                bool modifiedVelocity = false;

                #region X

                if (centerMassWorld.X < _boundryMin.Value.X)
                {
                    if (velocityWorld.X < 0)
                    {
                        velocityWorld.X = RETURNVELOCITY;
                        modifiedVelocity = true;
                    }

                    body.NewtonBody.AddForce(new Vector3D(body.Mass * ZACCEL, 0, 0));       // Apply a constant acceleration until it hits zero
                }
                else if (centerMassWorld.X > _boundryMax.Value.X)
                {
                    if (velocityWorld.X > 0)
                    {
                        velocityWorld.X = -RETURNVELOCITY;
                        modifiedVelocity = true;
                    }

                    body.NewtonBody.AddForce(new Vector3D(body.Mass * ZACCEL * -1, 0, 0));       // Apply a constant acceleration until it hits zero
                }

                #endregion
                #region Y

                if (centerMassWorld.Y < _boundryMin.Value.Y)
                {
                    if (velocityWorld.Y < 0)
                    {
                        velocityWorld.Y = RETURNVELOCITY;
                        modifiedVelocity = true;
                    }

                    body.NewtonBody.AddForce(new Vector3D(0, body.Mass * ZACCEL, 0));       // Apply a constant acceleration until it hits zero
                }
                else if (centerMassWorld.Y > _boundryMax.Value.Y)
                {
                    if (velocityWorld.Y > 0)
                    {
                        velocityWorld.Y = -RETURNVELOCITY;
                        modifiedVelocity = true;
                    }

                    body.NewtonBody.AddForce(new Vector3D(0, body.Mass * ZACCEL * -1, 0));       // Apply a constant acceleration until it hits zero
                }

                #endregion
                #region Z

                if (centerMassWorld.Z < _boundryMin.Value.Z)
                {
                    if (velocityWorld.Z < 0)
                    {
                        velocityWorld.Z = RETURNVELOCITY;
                        modifiedVelocity = true;
                    }

                    body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL));       // Apply a constant acceleration until it hits zero
                }
                else if (centerMassWorld.Z > _boundryMax.Value.Z)
                {
                    if (velocityWorld.Z > 0)
                    {
                        velocityWorld.Z = -RETURNVELOCITY;
                        modifiedVelocity = true;
                    }

                    body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL * -1));       // Apply a constant acceleration until it hits zero
                }

                #endregion

                if (modifiedVelocity)
                {
                    //body.Velocity = body.DirectionFromWorld(velocityWorld);
                    body.Velocity = velocityWorld;        // already in world coords
                }

                #endregion
            }

            if (_shouldForce2D)
            {
                if (!body.Override2DEnforcement_Rotation)
                {
                    #region Angular Pos/Vel

                    //body.NewtonBody.AddTorque(new Vector3D(.01, 0, 0));       // pulls back (front comes up, rear goes down)
                    //body.NewtonBody.AddTorque(new Vector3D(0, .1, 0));    // creates a roll to the right (left side goes up, right side goes down)


                    Vector3D angularVelocity = body.Omega;        // Omega seems to be angular velocity
                    bool modifiedAngularVelocity = false;

                    //const double RESTORETORQUE = 15d;     // TODO:  look at the inertia tensor for the axis I want to apply torque to, and do and calculate to make a constant accel
                    const double RESTORETORQUE = .25d;     // TODO:  look at the inertia tensor for the axis I want to apply torque to, and do and calculate to make a constant accel

                    double massMatrixLength = body.MassMatrix.m_I.Length;
                    double restoreTorqueX = RESTORETORQUE * body.MassMatrix.m_Mass * (1 / (body.MassMatrix.m_I.X / massMatrixLength));
                    double restoreTorqueY = RESTORETORQUE * body.MassMatrix.m_Mass * (1 / (body.MassMatrix.m_I.Y / massMatrixLength));

                    //double restoreTorqueX = RESTORETORQUE;     // pulling values from the mass matrix seemed to cause the engine to corrupt.  See if there's a problem casting that structure
                    //double restoreTorqueY = RESTORETORQUE;

                    //TODO:  Dampen the angluar velocidy if the object is very close to zero and the angular speed is small.  Currently, the object has
                    // a very slight wobble indefinately

                    #region X

                    Vector3D fromVect = new Vector3D(1, 0, 0);
                    Vector3D toVect = body.DirectionToWorld(fromVect);
                    Quaternion rotation = Math3D.GetRotation(fromVect, toVect);
                    Vector3D axis = rotation.Axis;
                    double radians = Math1D.DegreesToRadians(rotation.Angle);

                    if ((axis.Y > 0 && radians > 0) || (axis.Y < 0 && radians < 0))
                    {
                        if (angularVelocity.Y > 0)
                        {
                            angularVelocity.Y = 0;
                            modifiedAngularVelocity = true;
                        }

                        //body.NewtonBody.AddTorque(new Vector3D(0, -RESTORETORQUE, 0));
                        //body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, -RESTORETORQUE, 0)));     // apply torque seems to want world coords
                        body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, -restoreTorqueY * Math.Abs(radians), 0)));
                    }
                    else if ((axis.Y > 0 && radians < 0) || (axis.Y < 0 && radians > 0))
                    {
                        if (angularVelocity.Y < 0)
                        {
                            angularVelocity.Y = 0;
                            modifiedAngularVelocity = true;
                        }

                        //body.NewtonBody.AddTorque(new Vector3D(0, RESTORETORQUE, 0));
                        body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, restoreTorqueY * Math.Abs(radians), 0)));
                    }

                    #endregion
                    #region Y

                    fromVect = new Vector3D(0, 1, 0);
                    toVect = body.DirectionToWorld(fromVect);
                    rotation = Math3D.GetRotation(fromVect, toVect);
                    axis = rotation.Axis;
                    radians = Math1D.DegreesToRadians(rotation.Angle);


                    if ((axis.X > 0 && radians > 0) || (axis.X < 0 && radians < 0))
                    {
                        if (angularVelocity.X > 0)
                        {
                            angularVelocity.X = 0;
                            modifiedAngularVelocity = true;
                        }

                        //body.NewtonBody.AddTorque(new Vector3D(-RESTORETORQUE, 0, 0));
                        body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(-restoreTorqueX * Math.Abs(radians), 0, 0)));
                    }
                    else if ((axis.X > 0 && radians < 0) || (axis.X < 0 && radians > 0))
                    {
                        if (angularVelocity.X < 0)
                        {
                            angularVelocity.X = 0;
                            modifiedAngularVelocity = true;
                        }

                        //body.NewtonBody.AddTorque(new Vector3D(RESTORETORQUE, 0, 0));
                        body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(restoreTorqueX * Math.Abs(radians), 0, 0)));
                    }

                    #endregion

                    if (modifiedAngularVelocity)
                    {
                        body.Omega = angularVelocity;
                    }

                    #endregion
                }

                if (!body.Override2DEnforcement_Translation)
                {
                    #region Z Pos/Vel

                    //Vector3D velocityWorld = body.DirectionToWorld(body.Velocity);
                    Vector3D velocityWorld = body.Velocity;     // already in world coords
                    bool modifiedVelocity = false;

                    if (centerMassWorld.Z < 0)
                    {
                        if (velocityWorld.Z < 0)
                        {
                            velocityWorld.Z = 0;
                            modifiedVelocity = true;
                        }

                        body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL));       // Apply a constant acceleration until it hits zero
                    }
                    else if (centerMassWorld.Z > 0)
                    {
                        if (velocityWorld.Z > 0)
                        {
                            velocityWorld.Z = 0;
                            modifiedVelocity = true;
                        }

                        body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL * -1d));      // Apply a constant acceleration until it hits zero
                    }

                    if (modifiedVelocity)
                    {
                        //body.Velocity = body.DirectionFromWorld(velocityWorld);
                        body.Velocity = velocityWorld;
                    }

                    #endregion
                }
            }
        }

        #endregion
    }

    #region WorldUpdating Delegate/Args

    public delegate void WorldUpdatingHandler(object sender, WorldUpdatingArgs e);

    public class WorldUpdatingArgs : EventArgs
    {
        public WorldUpdatingArgs(double elapsedTime)
        {
            this.ElapsedTime = elapsedTime;
        }

        public double ElapsedTime
        {
            get;
            private set;
        }
    }

    #endregion
}
