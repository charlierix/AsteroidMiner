using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xaml;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    /// <summary>
    /// This is used to maintain a viewport scene across several threads.  This allows ai bots to see
    /// wpf 3D scenes
    /// </summary>
    public class CameraPool : IDisposable
    {
        #region Class: TaskWrapper

        /// <summary>
        /// This is a single thread
        /// </summary>
        /// <remarks>
        /// NOTE: this is a copy of NeuralPool.TaskWrapper
        /// </remarks>
        private class TaskWrapper : IDisposable
        {
            #region Class: ViewportOffline

            /// <summary>
            /// This houses a viewport, camera, etc.
            /// This class is NOT threadsafe (all calls must be made from the same thread - and that thread must be STA)
            /// </summary>
            private class ViewportOffline
            {
                #region Declaration Section

                private readonly FrameworkElement _control;

                private readonly PerspectiveCamera _camera;

                private readonly Color _background;

                #endregion

                #region Constructor

                public ViewportOffline(Color background)
                {
                    this.Viewport = new Viewport3D();

                    //  The initial values don't really matter.  The camera will be moved before taking a picture
                    _camera = new PerspectiveCamera(new Point3D(0, 0, 25), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45d);
                    this.Viewport.Camera = _camera;

                    //  The lights need to be passed in as CameraPoolVisual objects
                    //Model3DGroup lightGroup = new Model3DGroup();
                    //lightGroup.Children.Add(new AmbientLight(UtilityWPF.ColorFromHex("808080")));
                    //lightGroup.Children.Add(new DirectionalLight(UtilityWPF.ColorFromHex("FFFFFF"), new Vector3D(1, -1, -1)));
                    //lightGroup.Children.Add(new DirectionalLight(UtilityWPF.ColorFromHex("303030"), new Vector3D(-1, 1, 1)));

                    //ModelVisual3D lightModel = new ModelVisual3D();
                    //lightModel.Content = lightGroup;

                    //_viewport.Children.Add(lightModel);

                    _background = background;

                    // Viewport3D won't render to a bitmap when it's not part of the visual tree, but a border containing a viewport will
                    Border border = new Border();
                    border.Background = new SolidColorBrush(_background);
                    border.Child = this.Viewport;
                    _control = border;
                }

                #endregion

                #region Public Properties

                public readonly SortedList<long, Tuple<CameraPoolVisual, Visual3D>> Visuals = new SortedList<long, Tuple<CameraPoolVisual, Visual3D>>();

                public readonly Viewport3D Viewport;

                #endregion

                #region Public Methods

                public IBitmapCustom GetSnapshot(Point3D position, Vector3D lookDirection, Vector3D upDirection, int widthHeight)
                {
                    //  Move the camera
                    _camera.Position = position;
                    _camera.LookDirection = lookDirection;
                    _camera.UpDirection = upDirection;
                    //_camera.FieldOfView = ;       //  just leave this fixed for now

                    return UtilityWPF.RenderControl(_control, widthHeight, widthHeight, true, _background, false);
                }

                #endregion
            }

            #endregion

            #region Declaration Section

            private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

            private Task _task = null;

            #endregion

            #region Constructor

            public TaskWrapper(StaTaskScheduler staScheduler, Color background)
            {
                // Create the task (it just runs forever until dispose is called)
                _task = Task.Factory.StartNew(() =>
                {
                    Run(this, _cancel.Token, background);
                }, _cancel.Token, TaskCreationOptions.LongRunning, staScheduler);
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
                    // Cancel
                    _cancel.Cancel();
                    try
                    {
                        _task.Wait();
                    }
                    catch (Exception) { }

                    // Clean up
                    _task.Dispose();
                    _task = null;
                }
            }

            #endregion

            #region Public Properties

            //Item1: CameraPoolVisual or ICameraPoolCamera
            //Item2: True=Add, False=Remove
            public readonly ConcurrentQueue<Tuple<object, bool>> AddRemoves = new ConcurrentQueue<Tuple<object, bool>>();       // I only want one queue to minimize the number of pop attempts per tick

            private volatile int _count = 0;
            /// <summary>
            /// This is how many cameras are contained in this worker (helpful for load balancing)
            /// </summary>
            public int Count
            {
                get
                {
                    return _count;
                }
            }

            #endregion

            #region Private Methods

            private static void Run(TaskWrapper parent, CancellationToken cancel, Color background)
            {
                //NOTE: This method is running on an arbitrary thread
                try
                {
                    //  Create viewport
                    ViewportOffline viewport = new ViewportOffline(background);

                    List<ICameraPoolCamera> cameras = new List<ICameraPoolCamera>();

                    while (!cancel.IsCancellationRequested)
                    {
                        // Add/Remove items
                        AddRemoveItems(parent, viewport, cameras);

                        if (cameras.Count == 0)
                        {
                            // Hang out for a bit, then try again.  No need to burn up the processor
                            Thread.Sleep(450 + StaticRandom.Next(100));
                            continue;
                        }

                        MoveVisuals(viewport);

                        #region Take pictures

                        //TODO: If it takes a long time to get through these cameras, then maybe the visuals should be moved after every couple cameras
                        foreach (ICameraPoolCamera camera in cameras)
                        {
                            if (cancel.IsCancellationRequested)
                            {
                                return;
                            }

                            if (camera.IsOn)
                            {
                                var location = camera.GetWorldLocation_Camera();

                                IBitmapCustom bitmap = viewport.GetSnapshot(location.Item1, location.Item2.Standard, location.Item2.Orth, camera.PixelWidthHeight);

                                camera.StoreSnapshot(bitmap);
                            }
                        }

                        #endregion

                        Thread.Sleep(0);		// not sure if this is useful or not
                        //Thread.Yield();
                    }
                }
                catch (Exception)
                {
                    // Don't leak errors, just go away
                }
            }

            //private static void AddRemoveVisuals(TaskWrapper parent, ViewportOffline viewport)
            //{
            //    #region Removes

            //    CameraPoolVisual visual;
            //    while (parent.RemoveVisuals.TryDequeue(out visual))
            //    {
            //        viewport.Viewport.Children.Remove(viewport.Visuals[visual.Token].Item2);
            //        viewport.Visuals.Remove(visual.Token);
            //    }

            //    #endregion
            //    #region Adds

            //    while (parent.AddVisuals.TryDequeue(out visual))
            //    {
            //        //  Deserialize into an instance that is specific to this thread
            //        Model3D model;
            //        using (MemoryStream stream = new MemoryStream(visual.Model3D))
            //        {
            //            model = XamlServices.Load(stream) as Model3D;
            //        }

            //        //  Create a visual to hold it (no need to set the transform here, that will be done each frame)
            //        ModelVisual3D modelVisual = new ModelVisual3D();
            //        modelVisual.Content = model;

            //        //  Store it
            //        viewport.Viewport.Children.Add(modelVisual);
            //        viewport.Visuals.Add(visual.Token, Tuple.Create(visual, (Visual3D)modelVisual));
            //    }

            //    #endregion
            //}
            //private static void AddRemoveCameras(TaskWrapper parent, List<ICameraPoolCamera> cameras)
            //{
            //    bool hadChange = false;

            //    //  Removes
            //    ICameraPoolCamera camera;
            //    while (parent.RemoveCameras.TryDequeue(out camera))
            //    {
            //        cameras.Remove(camera);
            //        hadChange = true;
            //    }

            //    //  Adds
            //    while (parent.AddCameras.TryDequeue(out camera))
            //    {
            //        cameras.Add(camera);
            //        hadChange = true;
            //    }

            //    // Store the new count
            //    if (hadChange)
            //    {
            //        parent._count = cameras.Count;
            //    }
            //}

            private static void AddRemoveItems(TaskWrapper parent, ViewportOffline viewport, List<ICameraPoolCamera> cameras)
            {
                bool numCamerasChanged = false;

                Tuple<object, bool> item;
                while (parent.AddRemoves.TryDequeue(out item))
                {
                    if (item.Item1 is CameraPoolVisual)
                    {
                        CameraPoolVisual visual = (CameraPoolVisual)item.Item1;

                        if (item.Item2)
                        {
                            #region Add Visual

                            //  Deserialize into an instance that is specific to this thread
                            Model3D model;
                            using (MemoryStream stream = new MemoryStream(visual.Model3D))
                            {
                                model = XamlServices.Load(stream) as Model3D;
                            }

                            //  Create a visual to hold it (no need to set the transform here, that will be done each frame)
                            ModelVisual3D modelVisual = new ModelVisual3D();
                            modelVisual.Content = model;

                            //  Store it
                            viewport.Viewport.Children.Add(modelVisual);
                            viewport.Visuals.Add(visual.Token, Tuple.Create(visual, (Visual3D)modelVisual));

                            #endregion
                        }
                        else
                        {
                            #region Remove Visual

                            viewport.Viewport.Children.Remove(viewport.Visuals[visual.Token].Item2);
                            viewport.Visuals.Remove(visual.Token);

                            #endregion
                        }
                    }
                    else if (item.Item1 is ICameraPoolCamera)
                    {
                        ICameraPoolCamera camera = (ICameraPoolCamera)item.Item1;
                        numCamerasChanged = true;

                        if (item.Item2)
                        {
                            #region Add Camera

                            cameras.Add(camera);

                            #endregion
                        }
                        else
                        {
                            #region Remove Camera

                            cameras.Remove(camera);

                            #endregion
                        }
                    }
                    else
                    {
                        throw new ApplicationException("Unknown type of item: " + item.Item1.GetType().ToString());
                    }
                }

                // Store the new count
                if (numCamerasChanged)
                {
                    parent._count = cameras.Count;
                }
            }

            private static void MoveVisuals(ViewportOffline viewport)
            {
                foreach (var visual in viewport.Visuals.Values)
                {
                    if (visual.Item1.Visual == null)
                    {
                        //  This is a statically placed object (light)
                        continue;
                    }

                    visual.Item2.Transform = new MatrixTransform3D(visual.Item1.Visual.OffsetMatrix);
                }
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private StaTaskScheduler _staScheduler = null;

        /// <summary>
        /// Each of these has its own set of visuals/cameras that it's calling in a tight loop
        /// </summary>
        private TaskWrapper[] _tasks;

        /// <summary>
        /// This tells which thread contains a camera's token
        /// </summary>
        private SortedList<long, TaskWrapper> _cameraPointers = new SortedList<long, TaskWrapper>();

        #endregion

        #region Constructor

        public CameraPool(int numThreads, Color background)
        {
            _staScheduler = new StaTaskScheduler(numThreads);

            _tasks = new TaskWrapper[numThreads];
            for (int cntr = 0; cntr < numThreads; cntr++)
            {
                _tasks[cntr] = new TaskWrapper(_staScheduler, background);
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
                if (_tasks != null)
                {
                    for (int cntr = 0; cntr < _tasks.Length; cntr++)
                    {
                        _tasks[cntr].Dispose();
                    }

                    _tasks = null;
                }

                if (_staScheduler != null)
                {
                    _staScheduler.Dispose();
                    _staScheduler = null;
                }
            }
        }

        #endregion

        #region Public Methods

        //NOTE: Lights can be added as well with this method
        public void Add(CameraPoolVisual visual)
        {
            //  A visual will be added to all the viewports
            foreach (TaskWrapper task in _tasks)
            {
                task.AddRemoves.Enqueue(new Tuple<object, bool>(visual, true));
            }
        }
        public void Remove(CameraPoolVisual visual)
        {
            //  The visual is in all the viewports
            foreach (TaskWrapper task in _tasks)
            {
                task.AddRemoves.Enqueue(new Tuple<object, bool>(visual, false));
            }
        }

        public void Add(ICameraPoolCamera camera)
        {
            if (_cameraPointers.ContainsKey(camera.Token))
            {
                throw new ArgumentException("This camera has already been added");
            }

            // Find the thread with the least to do
            TaskWrapper wrapper = _tasks.
                Select(o => new { Count = o.Count, Wrapper = o }).
                OrderBy(o => o.Count).
                First().Wrapper;

            // Add to the wrapper
            wrapper.AddRemoves.Enqueue(new Tuple<object, bool>(camera, true));

            // Remember where it is
            _cameraPointers.Add(camera.Token, wrapper);
        }
        public void Remove(ICameraPoolCamera camera)
        {
            if (!_cameraPointers.ContainsKey(camera.Token))
            {
                throw new ArgumentException("This camera was never added");
            }

            TaskWrapper wrapper = _cameraPointers[camera.Token];

            // Remove from the wrapper
            wrapper.AddRemoves.Enqueue(new Tuple<object, bool>(camera, false));

            // Forget where it was
            _cameraPointers.Remove(camera.Token);
        }

        #endregion
    }

    #region Class: CameraPoolVisual

    public class CameraPoolVisual
    {
        public CameraPoolVisual(long token, byte[] model3D, ICameraPoolVisual visual)
        {
            this.Token = token;
            this.Model3D = model3D;
            this.Visual = visual;
        }

        public readonly long Token;

        /// <summary>
        /// This is a Model3D serialized with XamlServices.Save
        /// </summary>
        public readonly byte[] Model3D;

        /// <summary>
        /// If this is null, then the visual won't be considered movable (fixed lights)
        /// </summary>
        public readonly ICameraPoolVisual Visual;
    }

    #endregion
    #region Interface: ICameraPoolVisual

    public interface ICameraPoolVisual
    {
        //Point3D PositionWorld { get; }
        //Quaternion OrientationWorld { get; }

        /// <summary>
        /// This is what Newton Body exposes, and can be converted directly into a transform:
        /// visual.Transform = new MatrixTransform3D(e.OffsetMatrix);
        /// </summary>
        /// <remarks>
        /// See Game.Newt.v2.NewtonDynamics.Body,OnBodyMoved
        /// </remarks>
        Matrix3D OffsetMatrix { get; }
    }

    #endregion

    #region Interface: ICameraPoolCamera

    public interface ICameraPoolCamera
    {
        long Token { get; }

        /// <summary>
        /// Only square bitmaps are supported
        /// </summary>
        int PixelWidthHeight { get; }

        /// <summary>
        /// This gets the position and look/up directions of the camera
        /// </summary>
        Tuple<Point3D, DoubleVector> GetWorldLocation_Camera();

        /// <summary>
        /// NOTE: INeuronContainer has this same property
        /// </summary>
        bool IsOn { get; }

        /// <summary>
        /// This gets called arbitrarily by the pool (but only if IsOn is true)
        /// </summary>
        void StoreSnapshot(IBitmapCustom bitmap);
    }

    #endregion
}
