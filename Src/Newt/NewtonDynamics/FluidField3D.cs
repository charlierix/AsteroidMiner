using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.Newt.HelperClasses;

namespace Game.Newt.NewtonDynamics
{
    //TODO: The final version used for aerodynamics should allow ink to be null, save some processing (or a boolean telling whether to calculate ink)
    //Actually, keeps a boolean for is ink populated.  If they never set ink to non zero, then don't update it

    //TODO: Implement viscocity (damping only slows the velocity).  When boundry type is set to open_slaved or shared, and in the presence of a fast moving viscous fluid, the fluid should resist change in velocity, not simply slow down

    /// <remarks>
    /// Got this here:
    /// http://mikeash.com/pyblog/fluid-simulation-for-dummies.html
    /// 
    /// Which is based on this:
    /// http://www.dgp.toronto.edu/people/stam/reality/Research/pdf/GDC03.pdf
    /// </remarks>
    public class FluidField3D
    {
        #region Enum: BoundryType

        private enum SetBoundsType
        {
            VelocityX,
            VelocityY,
            VelocityZ,
            Ink,
            Other,
        }

        #endregion
        #region Struct: IndexLERP

        private struct IndexLERP
        {
            public IndexLERP(int index1D, Tuple<int, double>[] neighbors)
            {
                this.Index1D = index1D;

                this.Neighbors = neighbors;
            }

            public readonly int Index1D;

            // If this is empty, just store zero
            public readonly Tuple<int, double>[] Neighbors;
        }

        #endregion
        #region Class: BoundrySettings

        private class BoundrySettings
        {
            public FluidFieldBoundryType3D BoundryType = FluidFieldBoundryType3D.Closed;

            public IFluidField OpenBoundryParent = null;

            // These stay null until Update is called when boundry type is Open_Slaved or Open_Shared.  They will get refreshed at the
            // beginning of the update.  These are full size, even though only the boundries will be populated.  This trades memory for
            // speed (otherwise sorted lists would be needed)
            //NOTE: Only velocity is shared, ink isn't
            public double[] OpenBorderVelocityX = null;
            public double[] OpenBorderVelocityY = null;
            public double[] OpenBorderVelocityZ = null;

            public Mapping_3D_1D[] OpenBorderCells = null;      // these are cached to cut down on replicating complex for loops (these only point to the border cells)

            public double WallReflectivity = .95d;

            public bool IsBlockCacheDirty = true;
            public bool HasBlockedCells = false;

            // These lerp store which neighbors to average, and how much of each neighbor to contribute
            public IndexLERP[] Blocked_VelocityX = null;
            public IndexLERP[] Blocked_VelocityY = null;
            public IndexLERP[] Blocked_VelocityZ = null;
            public IndexLERP[] Blocked_Other = null;

            /// <summary>
            /// This holds which cells are completely blocked
            /// </summary>
            public int[] Blocked_Total = null;
        }

        #endregion

        #region Events

        public event EventHandler BlockedCellsChanged = null;

        #endregion

        #region Declaration Section

        private const double ONETHIRD = 1d / 3d;
        private const double OPENINFLOWMULT = .75d;

        // These are used for calculating reflection of blocked cells
        private const double VEL_INSIDECORNER = .25d;
        private const double VEL_OUTSIDECORNER = .5d;
        private const double VEL_ORTH = 1d;
        private const double OTHER_ANY = 1d;

        private double[] _s;      // what is s?

        // Temp arrays
        private double[] _velXTemp;
        private double[] _velYTemp;
        private double[] _velZTemp;

        private BoundrySettings _boundrySettings = new BoundrySettings();

        #endregion

        #region Constructor

        public FluidField3D(int size)
        {
            _size = size;
            _size1D = size * size * size;

            _s = new double[_size1D];
            _ink = new double[_size1D];

            _velocityX = new double[_size1D];
            _velocityY = new double[_size1D];
            _velocityZ = new double[_size1D];

            _velXTemp = new double[_size1D];
            _velYTemp = new double[_size1D];
            _velZTemp = new double[_size1D];

            _blocked = new bool[_size1D];
        }

        #endregion

        #region Public Properties

        private int _size;
        /// <summary>
        /// Number of cells of each axis (only perfect cubes are currently supported)
        /// </summary>
        public int Size
        {
            get
            {
                return _size;
            }
        }

        private int _size1D;
        public int Size1D
        {
            get
            {
                return _size1D;
            }
        }

        /// <summary>
        /// Rate at which the ink/velocity diffuses and spreads out in the fluid.
        /// 0 to 1 (though .1 is pretty extreme)
        /// </summary>
        private double _diffusion = .01;
        public double Diffusion
        {
            get
            {
                return _diffusion;
            }
            set
            {
                _diffusion = value;
            }
        }

        /// <summary>
        /// Rate at which fluid motions damp out over time.
        /// 0 to 1 (though .1 is pretty extreme)
        /// </summary>
        private double _damping = .01;
        public double Damping
        {
            get
            {
                return _damping;
            }
            set
            {
                _damping = value;
            }
        }

        private double _timestep = 20;
        /// <summary>
        /// Controls the time-increment per frame of the simulation.
        /// 0 to 100
        /// </summary>
        /// <remarks>
        /// Note that large timesteps will lead to inaccurate fluid behavior, and large timesteps will also cause the simulation to blow up at high vorticities
        /// </remarks>
        public double TimeStep
        {
            get
            {
                return _timestep;
            }
            set
            {
                _timestep = value;
            }
        }

        private int _iterations = 6;
        /// <summary>
        /// Controls the number of iterations used in the iterative linear solver that calculates the fluid.
        /// 0 to 100
        /// </summary>
        /// <remarks>
        /// Low values will result in a fast simulation, but the fluid will behave inaccurately, whereas high values will give more accurate behavior, but
        /// slow down the simulation. The primary effect is to make fluids more incompressable (as they should be) to display more accurate swirling
        /// effects.
        /// </remarks>
        public int Iterations
        {
            get
            {
                return _iterations;
            }
            set
            {
                _iterations = value;
            }
        }

        public double WallReflectivity
        {
            get
            {
                return _boundrySettings.WallReflectivity;
            }
            set
            {
                _boundrySettings.WallReflectivity = value;

                _boundrySettings.IsBlockCacheDirty = true;
            }
        }

        public FluidFieldBoundryType3D BoundryType
        {
            get
            {
                return _boundrySettings.BoundryType;
            }
            set
            {
                _boundrySettings.BoundryType = value;
            }
        }

        /// <summary>
        /// If BoundryType is Open_Slaved or Open_Shared, this will need to be set to a field (if null, zero is assumed for all border cells)
        /// </summary>
        public IFluidField OpenBoundryParent
        {
            get
            {
                return _boundrySettings.OpenBoundryParent;
            }
            set
            {
                _boundrySettings.OpenBoundryParent = value;
            }
        }

        // These are used if BoundryType is Open_Slaved or Open_Shared.  They are used to request values at varous points in the world
        public Point3D PositionWorld
        {
            get;
            set;
        }
        public double SizeWorld
        {
            get;
            set;
        }
        public Quaternion RotationWorld
        {
            get;
            set;
        }
        //NOTE: I was debating whether I need to model angular velocity, but I don't think I do, but I could be wrong :)
        public Vector3D VelocityWorld
        {
            get;
            set;
        }

        //----------------------- Exposed for reading only -----------------------

        private double[] _ink;
        /// <summary>
        /// This doesn't affect the fluid, it is just carried around in the fluid (0 to 1)
        /// </summary>
        /// <remarks>
        /// FluidField2D has an arbitrary number of ink arrays (Layers) for R,G,B.  It is safe to add that kind of functionality to this class
        /// as well if needed
        /// </remarks>
        public double[] Ink
        {
            get
            {
                return _ink;
            }
        }

        // Velocity of the fluid
        private double[] _velocityX;
        public double[] VelocityX
        {
            get
            {
                return _velocityX;
            }
        }

        private double[] _velocityY;
        public double[] VelocityY
        {
            get
            {
                return _velocityY;
            }
        }

        private double[] _velocityZ;
        public double[] VelocityZ
        {
            get
            {
                return _velocityZ;
            }
        }

        //TODO: Be able to request the forces acting on a cell
        private bool[] _blocked;
        /// <summary>
        /// These are individual cells that are blocked (they represent internal walls)
        /// </summary>
        public bool[] Blocked
        {
            get
            {
                return _blocked;
            }
        }

        //---------------------------------------------------------------------------------

        #endregion

        #region Public Methods

        public void Update()
        {
            const bool DIFFUSESEPARATETHREAD = true;

            #region Prep

            IndexBlockedCells();
            PopulateOpenBorderVelocites();

            // Need to force totally bloced cell velocities to zero
            foreach (int index in _boundrySettings.Blocked_Total)
            {
                _velocityX[index] = 0;
                _velocityY[index] = 0;
                _velocityZ[index] = 0;
            }

            #endregion

            #region Velocity step

            if (_diffusion > 0 || _damping > 0)
            {
                // Diffuse all three velocity components
                if (_diffusion > 0)
                {
                    if (DIFFUSESEPARATETHREAD)
                    {
                        Task[] tasks = new Task[]
                        {
                            Task.Run(() => Diffuse(SetBoundsType.VelocityX, _velXTemp, _velocityX, _blocked, _timestep, _diffusion, _iterations, _size, _boundrySettings)),
                            Task.Run(() => Diffuse(SetBoundsType.VelocityY, _velYTemp, _velocityY, _blocked, _timestep, _diffusion, _iterations, _size, _boundrySettings)),
                            Task.Run(() => Diffuse(SetBoundsType.VelocityZ, _velZTemp, _velocityZ, _blocked, _timestep, _diffusion, _iterations, _size, _boundrySettings))
                        };
                        Task.WaitAll(tasks);
                    }
                    else
                    {
                        Diffuse(SetBoundsType.VelocityX, _velXTemp, _velocityX, _blocked, _timestep, _diffusion, _iterations, _size, _boundrySettings);
                        Diffuse(SetBoundsType.VelocityY, _velYTemp, _velocityY, _blocked, _timestep, _diffusion, _iterations, _size, _boundrySettings);
                        Diffuse(SetBoundsType.VelocityZ, _velZTemp, _velocityZ, _blocked, _timestep, _diffusion, _iterations, _size, _boundrySettings);
                    }
                }
                else
                {
                    Array.Copy(_velocityX, _velXTemp, _size1D);     //diffusion acts like a fuzzy array.copy.  So since it didn't run, copy into temp
                    Array.Copy(_velocityY, _velYTemp, _size1D);
                    Array.Copy(_velocityZ, _velZTemp, _size1D);
                }

                if (_damping > 0)
                {
                    Slow(_velXTemp, _damping, _timestep);
                    Slow(_velYTemp, _damping, _timestep);
                    Slow(_velZTemp, _damping, _timestep);
                }

                // Fix up velocities so they keep things incompressible
                Project(_velXTemp, _velYTemp, _velZTemp, _velocityX, _velocityY, _blocked, _iterations, _size, _boundrySettings);       //where is _velocityZ???????
            }
            else
            {
                Array.Copy(_velocityX, _velXTemp, _size1D);
                Array.Copy(_velocityY, _velYTemp, _size1D);
                Array.Copy(_velocityZ, _velZTemp, _size1D);
            }

            // Move the velocities around according to the velocities of the fluid (confused yet?)
            Advect(SetBoundsType.VelocityX, _velocityX, _velXTemp, _velXTemp, _velYTemp, _velZTemp, _blocked, _timestep, _size, _boundrySettings);
            Advect(SetBoundsType.VelocityY, _velocityY, _velYTemp, _velXTemp, _velYTemp, _velZTemp, _blocked, _timestep, _size, _boundrySettings);
            Advect(SetBoundsType.VelocityZ, _velocityZ, _velZTemp, _velXTemp, _velYTemp, _velZTemp, _blocked, _timestep, _size, _boundrySettings);

            // Fix up the velocities again
            Project(_velocityX, _velocityY, _velocityZ, _velXTemp, _velYTemp, _blocked, _iterations, _size, _boundrySettings);

            #endregion
            #region Density step

            // Diffuse the dye
            Diffuse(SetBoundsType.Ink, _s, _ink, _blocked, _timestep, _diffusion, _iterations, _size, _boundrySettings);
            // Move the dye around according to the velocities
            Advect(SetBoundsType.Ink, _ink, _s, _velocityX, _velocityY, _velocityZ, _blocked, _timestep, _size, _boundrySettings);

            #endregion
        }

        public void AddInk(int x, int y, int z, double amount)
        {
            _ink[Get1DIndex(x, y, z)] += amount;
        }
        public void AddInk(int index1D, double amount)
        {
            _ink[index1D] += amount;
        }
        public void SetInk(int x, int y, int z, double amount)
        {
            _ink[Get1DIndex(x, y, z)] = amount;
        }
        public void SetInk(int index1D, double amount)
        {
            _ink[index1D] = amount;
        }

        public void AddVelocity(int x, int y, int z, double amountX, double amountY, double amountZ)
        {
            int index = Get1DIndex(x, y, z);

            _velocityX[index] += amountX;
            _velocityY[index] += amountY;
            _velocityZ[index] += amountZ;
        }
        public void AddVelocity(int index1D, double amountX, double amountY, double amountZ)
        {
            _velocityX[index1D] += amountX;
            _velocityY[index1D] += amountY;
            _velocityZ[index1D] += amountZ;
        }
        public void SetVelocity(int x, int y, int z, double amountX, double amountY, double amountZ)
        {
            int index = Get1DIndex(x, y, z);

            _velocityX[index] = amountX;
            _velocityY[index] = amountY;
            _velocityZ[index] = amountZ;
        }
        public void SetVelocity(int index1D, double amountX, double amountY, double amountZ)
        {
            _velocityX[index1D] = amountX;
            _velocityY[index1D] = amountY;
            _velocityZ[index1D] = amountZ;
        }

        public void SetBlockedCell(int x, int y, int z, bool isBlocked)
        {
            SetBlockedCells(new int[] { Get1DIndex(x, y, z) }, isBlocked);
        }
        public void SetBlockedCell(int index1D, bool isBlocked)
        {
            SetBlockedCells(new int[] { index1D }, isBlocked);
        }
        public void SetBlockedCells(bool isBlocked)
        {
            // Set all the cells to the same value
            SetBlockedCells(Enumerable.Range(0, _size1D), isBlocked);
        }
        public void SetBlockedCells(IEnumerable<int> indices1D, bool isBlocked)
        {
            foreach (int index in indices1D)
            {
                _blocked[index] = isBlocked;
            }

            _boundrySettings.IsBlockCacheDirty = true;

            if (this.BlockedCellsChanged != null)
            {
                this.BlockedCellsChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// All the arrrays are stored as 1D, but represent 3D.  This method returns the index into an array
        /// </summary>
        public int Get1DIndex(int x, int y, int z)
        {
            return x + y * _size + z * _size * _size;
        }
        public static int Get1DIndex(int x, int y, int z, int size)
        {
            return x + y * size + z * size * size;
        }
        /// <summary>
        /// Same method as Get1DIndex, just shortened name
        /// </summary>
        private int IX(int x, int y, int z)
        {
            return x + y * _size + z * _size * _size;
        }
        private static int IX(int x, int y, int z, int size)
        {
            return x + y * size + z * size * size;
        }
        public Tuple<int, int, int> Get3DIndex(int index1D)
        {
            //index1D = x + (y * _size) + (z * _size * _size);

            //NOTE: this is integer math

            int sizeSquared = _size * _size;
            int z = index1D / sizeSquared;

            int remainder = index1D - (z * sizeSquared);

            int y = remainder / _size;

            remainder = remainder - (y * _size);

            return Tuple.Create(remainder, y, z);
        }

        /// <summary>
        /// This returns all the cells in model coords or world coords
        /// </summary>
        /// <param name="shouldTransform">
        /// True: Rotate and Position transform are applied
        /// False: Rotate and Position aren't applied, but SizeWorld is still used
        /// </param>
        public Rectangle3DIndexedMapped[] GetCells(bool shouldTransform = false)
        {
            return GetCells(this.SizeWorld);
        }
        public Rectangle3DIndexedMapped[] GetCells(double rectangleSize, bool shouldTransform = false)
        {
            double cellSize = rectangleSize / _size;
            double halfSize = rectangleSize / 2d;

            Transform3DGroup transform = null;
            if (shouldTransform)
            {
                transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.RotationWorld)));
                transform.Children.Add(new TranslateTransform3D(this.PositionWorld.ToVector()));
            }

            // Corner Points
            int cornerSize = _size + 1;
            Point3D[] cornerPoints = new Point3D[cornerSize * cornerSize * cornerSize];

            for (int x = 0; x < cornerSize; x++)
            {
                for (int y = 0; y < cornerSize; y++)
                {
                    for (int z = 0; z < cornerSize; z++)
                    {
                        Point3D point = new Point3D(-halfSize + (x * cellSize), -halfSize + (y * cellSize), -halfSize + (z * cellSize));
                        cornerPoints[FluidField3D.Get1DIndex(x, y, z, cornerSize)] = shouldTransform ? transform.Transform(point) : point;
                    }
                }
            }

            // Cells
            Rectangle3DIndexedMapped[] retVal = new Rectangle3DIndexedMapped[_size1D];

            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    for (int z = 0; z < _size; z++)
                    {
                        int index = Get1DIndex(x, y, z);
                        retVal[index] = new Rectangle3DIndexedMapped(new Mapping_3D_1D(x, y, z, index), cornerPoints, new int[] 
                            {
                                FluidField3D.Get1DIndex(x, y, z, cornerSize),
                                FluidField3D.Get1DIndex(x, y, z + 1, cornerSize),
                                FluidField3D.Get1DIndex(x, y + 1, z, cornerSize),
                                FluidField3D.Get1DIndex(x, y + 1, z + 1, cornerSize),
                                FluidField3D.Get1DIndex(x + 1, y, z, cornerSize),
                                FluidField3D.Get1DIndex(x + 1, y, z + 1, cornerSize),
                                FluidField3D.Get1DIndex(x + 1, y + 1, z, cornerSize),
                                FluidField3D.Get1DIndex(x + 1, y + 1, z + 1, cornerSize)
                            });
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Takes the average velocity of the requested cells
        /// </summary>
        /// <remarks>
        /// This was written to assist with IFluidField.GetForce.  I decided to not use the interface to give the caller
        /// the option of model or world coords.  Also, there is more flexibility if multiple fields need to be stitched
        /// together.
        /// 
        /// TODO: When this class implements viscosity, also implement IFluidField directly (and assume world coords)
        /// </remarks>
        public Vector3D[] GetFlowAtLocations(Point3D[] points, double radius)
        {
            //TODO: look at radius

            double fullSize = this.SizeWorld;
            double halfSize = fullSize / 2d;
            double cellSize = fullSize / _size;

            // This is to transform the point from world to model coords
            Transform3DGroup transformPoint = new Transform3DGroup();
            transformPoint.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.RotationWorld.ToReverse())));
            transformPoint.Children.Add(new TranslateTransform3D(this.PositionWorld.ToVector() * -1d));
            transformPoint.Children.Add(new TranslateTransform3D(new Vector3D(halfSize, halfSize, halfSize)));       // The point passed in is from -half to +half, but the ints are 0 to size

            // This is to transform the return velocity back to world coords
            RotateTransform3D transformVect = new RotateTransform3D(new QuaternionRotation3D(this.RotationWorld));

            Vector3D[] retVal = new Vector3D[points.Length];

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                // Convert into int positions
                Point3D transformed = transformPoint.Transform(points[cntr]);

                int x = Convert.ToInt32(Math.Round(transformed.X / cellSize));
                int y = Convert.ToInt32(Math.Round(transformed.Y / cellSize));
                int z = Convert.ToInt32(Math.Round(transformed.Z / cellSize));

                int index = Get1DIndex(x, y, z);
                if (index < 0 || index >= _size1D)
                {
                    // The point is outside this field
                    retVal[cntr] = new Vector3D(0, 0, 0);
                }

                double negate = 1d;
                if (_blocked[index])
                {
                    // Blocked cells have the opposite velocity, so that they push back.  But when reporting, it needs to be flipped back
                    // so that it's more like the force felt at that point
                    negate = -1d;
                }

                retVal[cntr] = transformVect.Transform(new Vector3D(_velocityX[index] * negate, _velocityY[index] * negate, _velocityZ[index] * negate));
            }

            // Exit Function
            return retVal;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Allows the fluid to spread out (blurs the fluid)
        /// </summary>
        /// <remarks>
        /// Put a drop of soy sauce in some water, and you'll notice that it doesn't stay still, but it spreads out.  This
        /// happens even if the water and sauce are both perfectly still.  This is called diffusion. We use diffusion both
        /// in the obvious case of making the dye spread out, and also in the less obvious case of making the velocities
        /// of the fluid spread out.
        /// 
        /// Diffuse is really simple; it just precalculates a value and passes everything off to lin_solve.  So that means,
        /// while I know what it does, I don't really know how, since all the work is in that mysterious function
        /// </remarks>
        private static void Diffuse(SetBoundsType whichSide, double[] destination, double[] source, bool[] blocked, double timestep, double diffusion, int iterations, int size, BoundrySettings boundrySettings)
        {
            double a = timestep * diffusion;

            //NOTE: The 2D uses 4.  If 4 is used here, the color just spreads out, so maybe it's 2xDimensions?
            LinearSolve(whichSide, destination, source, blocked, a, 1 + 6 * a, iterations, size, boundrySettings);
        }

        private static void Slow(double[] cells, double viscocity, double timestep)
        {
            double factor = 1d / (viscocity * timestep / 20d + 1d);

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] *= factor;
            }
        }

        /// <summary>
        /// Forces the velocity to be mass conserving
        /// </summary>
        /// <remarks>
        /// The fluid that this class models is incompressible  This means that the amount of fluid in each box has to stay constant.
        /// That means that the amount of fluid going in has to be exactly equal to the amount of fluid going out.  The other
        /// operations tend to screw things up so that you get some boxes with a net outflow, and some with a net inflow.  This
        /// operation runs through all the cells and fixes them up so everything is in equilibrium.
        /// 
        /// This function is also somewhat mysterious as to exactly how it works, but it does some more running through the data
        /// and setting values, with some calls to lin_solve thrown in for fun
        /// 
        /// --------------
        /// 
        /// Every velocity field is the sum of an incompressible field and a gradient field.  To obtain an incompressible field we
        /// simply subtract the gradient field from our current velocities.
        /// 
        /// The gradient field indicates the direction of steepest descent of some height function. Imagine a terrain with hills and
        /// valleys with an arrow at every point pointing in the direction of steepest descent
        /// </remarks>
        private static void Project(double[] velocX, double[] velocY, double[] velocZ, double[] p, double[] div, bool[] blocked, int iterations, int size, BoundrySettings boundrySettings)
        {
            int index;

            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        index = IX(x, y, z, size);

                        if (blocked[index])
                        {
                            continue;
                        }

                        //Negative divergence
                        div[index] = -0.5d * (
                                 velocX[IX(x + 1, y, z, size)]
                                - velocX[IX(x - 1, y, z, size)]
                                + velocY[IX(x, y + 1, z, size)]
                                - velocY[IX(x, y - 1, z, size)]
                                + velocZ[IX(x, y, z + 1, size)]
                                - velocZ[IX(x, y, z - 1, size)]
                            ) / size;

                        //Pressure field
                        p[index] = 0;
                    }
                }
            }
            SetBoundry(SetBoundsType.Other, div, blocked, size, boundrySettings);
            SetBoundry(SetBoundsType.Other, p, blocked, size, boundrySettings);
            LinearSolve(SetBoundsType.Other, p, div, blocked, 1, 6, iterations, size, boundrySettings);

            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        index = IX(x, y, z, size);

                        if (blocked[index])
                        {
                            continue;
                        }

                        velocX[index] -= 0.5d * (p[IX(x + 1, y, z, size)]
                                                        - p[IX(x - 1, y, z, size)]) * size;
                        velocY[index] -= 0.5d * (p[IX(x, y + 1, z, size)]
                                                        - p[IX(x, y - 1, z, size)]) * size;
                        velocZ[index] -= 0.5d * (p[IX(x, y, z + 1, size)]
                                                        - p[IX(x, y, z - 1, size)]) * size;
                    }
                }
            }
            SetBoundry(SetBoundsType.VelocityX, velocX, blocked, size, boundrySettings);
            SetBoundry(SetBoundsType.VelocityY, velocY, blocked, size, boundrySettings);
            SetBoundry(SetBoundsType.VelocityZ, velocZ, blocked, size, boundrySettings);
        }

        /// <remarks>
        /// Every cell has a set of velocities, and these velocities make things move. This is called advection.  As with diffusion, advection
        /// applies both to the dye and to the velocities themselves.
        /// 
        /// This function is responsible for actually moving things around.  To that end, it looks at each cell in turn.  In that cell, it grabs the
        /// velocity, follows that velocity back in time, and sees where it lands.  It then takes a weighted average of the cells around the spot
        /// where it lands, then applies that value to the current cell.
        /// </remarks>
        private static void Advect(SetBoundsType whichSide, double[] dest, double[] source, double[] velocX, double[] velocY, double[] velocZ, bool[] blocked, double timestep, int size, BoundrySettings boundrySettings)
        {
            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        int index1D = IX(x, y, z, size);

                        if (blocked[index1D])
                        {
                            continue;
                        }

                        // Reverse velocity, since we are interpolating backwards 
                        // xSrc and ySrc is the position of the source density.
                        double xSrc = x - (timestep * velocX[index1D]);
                        double ySrc = y - (timestep * velocY[index1D]);
                        double zSrc = z - (timestep * velocZ[index1D]);

                        if (xSrc < 0.5d) xSrc = 0.5d;
                        if (xSrc > size - 1.5d) xSrc = size - 1.5d;
                        int x0 = (int)xSrc;     // casting to int is also the equivalent of Math.Floor
                        int x1 = x0 + 1;

                        if (ySrc < 0.5d) ySrc = 0.5d;
                        if (ySrc > size - 1.5d) ySrc = size - 1.5d;
                        int y0 = (int)ySrc;
                        int y1 = y0 + 1;

                        if (zSrc < 0.5d) zSrc = 0.5d;
                        if (zSrc > size - 1.5d) zSrc = size - 1.5d;
                        int z0 = (int)zSrc;
                        int z1 = z0 + 1;

                        //Linear interpolation factors. Ex: 0.6 and 0.4
                        double xProp1 = xSrc - x0;
                        double xProp0 = 1d - xProp1;
                        double yProp1 = ySrc - y0;
                        double yProp0 = 1d - yProp1;
                        double zProp1 = zSrc - z0;
                        double zProp0 = 1d - zProp1;

                        // ugly no matter how you nest it
                        dest[index1D] =
                            xProp0 *
                                (yProp0 *
                                    (zProp0 * source[IX(x0, y0, z0, size)] +
                                    zProp1 * source[IX(x0, y0, z1, size)]) +
                                (yProp1 *
                                    (zProp0 * source[IX(x0, y1, z0, size)] +
                                    zProp1 * source[IX(x0, y1, z1, size)]))) +
                            xProp1 *
                                (yProp0 *
                                    (zProp0 * source[IX(x1, y0, z0, size)] +
                                    zProp1 * source[IX(x1, y0, z1, size)]) +
                                (yProp1 *
                                    (zProp0 * source[IX(x1, y1, z0, size)] +
                                    zProp1 * source[IX(x1, y1, z1, size)])));
                    }
                }
            }

            SetBoundry(whichSide, dest, blocked, size, boundrySettings);
        }

        /// <summary>
        /// Not exactly sure how this method works, it's solving a linear differential equation of some sort
        /// </summary>
        /// <remarks>
        ///  This runs through the whole array and sets each cell to a combination of its neighbors.  It does this several times; the more
        ///  iterations it does, the more accurate the results, but the slower things run.  (4 iterations is a good number to use).
        ///  
        /// After each iteration, it resets the boundaries so the calculations don't explode.
        /// </remarks>
        private static void LinearSolve(SetBoundsType whichSide, double[] dest, double[] source, bool[] blocked, double a, double c, int iterations, int size, BoundrySettings boundrySettings)
        {
            double cRecip = 1d / c;

            int index;

            for (int cntr = 0; cntr < iterations; cntr++)
            {
                for (int z = 1; z < size - 1; z++)
                {
                    for (int y = 1; y < size - 1; y++)
                    {
                        for (int x = 1; x < size - 1; x++)
                        {
                            index = IX(x, y, z, size);

                            if (blocked[index])
                            {
                                continue;
                            }

                            dest[index] =
                                    (source[IX(x, y, z, size)]
                                        + a * (dest[IX(x + 1, y, z, size)]
                                                + dest[IX(x - 1, y, z, size)]
                                                + dest[IX(x, y + 1, z, size)]
                                                + dest[IX(x, y - 1, z, size)]
                                                + dest[IX(x, y, z + 1, size)]
                                                + dest[IX(x, y, z - 1, size)]
                                       )) * cRecip;
                        }
                    }
                }

                SetBoundry(whichSide, dest, blocked, size, boundrySettings);
            }
        }

        /// <summary>
        /// Sets the boundary cells at the outer edges of the cube so they perfectly counteract their neighbor
        /// </summary>
        /// <remarks>
        /// This is a way to keep fluid from leaking out of the box.  (not having walls really screws up the simulation code).
        /// Walls are added by treating the outer layer of cells as the wall.  Basically, every velocity in the layer next to this
        /// outer layer is mirrored.  So when you have some velocity towards the wall in the next-to-outer layer, the wall
        /// gets a velocity that perfectly counters it.
        /// 
        /// 
        /// Say we're at the left edge of the cube. The left cell is the boundary cell that needs to counteract its neighbor, the
        /// right cell. The right cell has a velocity that's up and to the left.
        /// 
        /// The boundary cell's x velocity needs to be opposite its neighbor, but its y velocity needs to be equal to its neighbor.
        /// This will produce a result that counteracts the motion of the fluid which would take it through the wall, and preserves
        /// the rest of the motion.
        /// 
        /// So what action is taken depends on which array is passed in; if we're dealing with x velocities, then we have to set
        /// the boundary cell's value to the opposite of its neighbor, but for everything else we set it to be the same.
        ///
        /// 
        /// This function also sets corners. This is done very simply, by setting each corner cell equal to the average of its three
        /// neighbors.
        /// </remarks>
        private static void SetBoundry_ORIG(SetBoundsType whichSide, double[] array, int size, BoundrySettings boundrySettings)
        {
            const double ONETHIRD = 1d / 3d;

            // Reflect the walls
            //NOTE: These arrays go from 1 to length-1.  The corners are handled later

            // X wall
            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    array[IX(0, y, z, size)] = whichSide == SetBoundsType.VelocityX ? -array[IX(1, y, z, size)] : array[IX(1, y, z, size)];
                    array[IX(size - 1, y, z, size)] = whichSide == SetBoundsType.VelocityX ? -array[IX(size - 2, y, z, size)] : array[IX(size - 2, y, z, size)];
                }
            }

            // Y wall
            for (int z = 1; z < size - 1; z++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    array[IX(x, 0, z, size)] = whichSide == SetBoundsType.VelocityY ? -array[IX(x, 1, z, size)] : array[IX(x, 1, z, size)];
                    array[IX(x, size - 1, z, size)] = whichSide == SetBoundsType.VelocityY ? -array[IX(x, size - 2, z, size)] : array[IX(x, size - 2, z, size)];
                }
            }

            // Z wall
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    array[IX(x, y, 0, size)] = whichSide == SetBoundsType.VelocityZ ? -array[IX(x, y, 1, size)] : array[IX(x, y, 1, size)];
                    array[IX(x, y, size - 1, size)] = whichSide == SetBoundsType.VelocityZ ? -array[IX(x, y, size - 2, size)] : array[IX(x, y, size - 2, size)];
                }
            }


            // Corners take average of neighbors
            array[IX(0, 0, 0, size)] = ONETHIRD *
                                            (array[IX(1, 0, 0, size)]
                                          + array[IX(0, 1, 0, size)]
                                          + array[IX(0, 0, 1, size)]);

            array[IX(0, size - 1, 0, size)] = ONETHIRD *
                                            (array[IX(1, size - 1, 0, size)]
                                          + array[IX(0, size - 2, 0, size)]
                                          + array[IX(0, size - 1, 1, size)]);

            array[IX(0, 0, size - 1, size)] = ONETHIRD *
                                            (array[IX(1, 0, size - 1, size)]
                                          + array[IX(0, 1, size - 1, size)]
                                          + array[IX(0, 0, size - 2, size)]);

            array[IX(0, size - 1, size - 1, size)] = ONETHIRD *
                                            (array[IX(1, size - 1, size - 1, size)]
                                          + array[IX(0, size - 2, size - 1, size)]
                                          + array[IX(0, size - 1, size - 2, size)]);

            array[IX(size - 1, 0, 0, size)] = ONETHIRD *
                                            (array[IX(size - 2, 0, 0, size)]
                                          + array[IX(size - 1, 1, 0, size)]
                                          + array[IX(size - 1, 0, 1, size)]);

            array[IX(size - 1, size - 1, 0, size)] = ONETHIRD *
                                            (array[IX(size - 2, size - 1, 0, size)]
                                          + array[IX(size - 1, size - 2, 0, size)]
                                          + array[IX(size - 1, size - 1, 1, size)]);

            array[IX(size - 1, 0, size - 1, size)] = ONETHIRD *
                                            (array[IX(size - 2, 0, size - 1, size)]
                                          + array[IX(size - 1, 1, size - 1, size)]
                                          + array[IX(size - 1, 0, size - 2, size)]);

            array[IX(size - 1, size - 1, size - 1, size)] = ONETHIRD *
                                            (array[IX(size - 2, size - 1, size - 1, size)]
                                          + array[IX(size - 1, size - 2, size - 1, size)]
                                          + array[IX(size - 1, size - 1, size - 2, size)]);
        }

        private static void SetBoundry(SetBoundsType whichSide, double[] cells, bool[] blocked, int size, BoundrySettings boundrySettings)
        {
            // Outer Edges
            switch (boundrySettings.BoundryType)
            {
                case FluidFieldBoundryType3D.Closed:
                    SetBoundry_Closed(whichSide, cells, size, boundrySettings);
                    break;

                case FluidFieldBoundryType3D.Open:
                    SetBoundry_Open(whichSide, cells, size, boundrySettings);
                    break;

                case FluidFieldBoundryType3D.Open_Shared:
                    SetBoundry_OpenShared(whichSide, cells, size, boundrySettings);
                    break;

                case FluidFieldBoundryType3D.Open_Slaved:
                    SetBoundry_OpenSlaved(whichSide, cells, size, boundrySettings);
                    break;

                case FluidFieldBoundryType3D.WrapAround:
                    SetBoundry_ReachAround(whichSide, cells, size, boundrySettings);
                    break;

                default:
                    throw new ApplicationException("Unknown FluidFieldBoundryType3D: " + boundrySettings.BoundryType.ToString());
            }

            // Blocked Cells
            if (boundrySettings.HasBlockedCells)
            {
                // This has similar logic to closed box, but applied facing outward around the blocked cells
                SetBoundry_BlockedCells(whichSide, cells, boundrySettings);
            }
        }

        private static void SetBoundry_Closed(SetBoundsType whichSide, double[] cells, int size, BoundrySettings boundrySettings)
        {
            #region X wall

            // When it's the x velocity, it needs to reflect off of this wall.  All other cases slide along it (or in the case of non
            // velocities, just a copy)
            double reflectMult = whichSide == SetBoundsType.VelocityX ? -boundrySettings.WallReflectivity : 1d;

            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    cells[IX(0, y, z, size)] = reflectMult * cells[IX(1, y, z, size)];
                    cells[IX(size - 1, y, z, size)] = reflectMult * cells[IX(size - 2, y, z, size)];
                }
            }

            #endregion
            #region Y wall

            reflectMult = whichSide == SetBoundsType.VelocityY ? -boundrySettings.WallReflectivity : 1d;

            for (int z = 1; z < size - 1; z++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    cells[IX(x, 0, z, size)] = reflectMult * cells[IX(x, 1, z, size)];
                    cells[IX(x, size - 1, z, size)] = reflectMult * cells[IX(x, size - 2, z, size)];
                }
            }

            #endregion
            #region Z wall

            reflectMult = whichSide == SetBoundsType.VelocityZ ? -boundrySettings.WallReflectivity : 1d;

            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    cells[IX(x, y, 0, size)] = reflectMult * cells[IX(x, y, 1, size)];
                    cells[IX(x, y, size - 1, size)] = reflectMult * cells[IX(x, y, size - 2, size)];
                }
            }

            #endregion

            #region Edges

            for (int x = 1; x < size - 1; x++)
            {
                cells[IX(x, 0, 0, size)] = 0.5 * (cells[IX(x, 1, 0, size)] + cells[IX(x, 0, 1, size)]);
                cells[IX(x, size - 1, 0, size)] = 0.5 * (cells[IX(x, size - 2, 0, size)] + cells[IX(x, size - 1, 1, size)]);
                cells[IX(x, 0, size - 1, size)] = 0.5 * (cells[IX(x, 1, size - 1, size)] + cells[IX(x, 0, size - 2, size)]);
                cells[IX(x, size - 1, size - 1, size)] = 0.5 * (cells[IX(x, size - 2, size - 1, size)] + cells[IX(x, size - 1, size - 2, size)]);
            }

            for (int y = 1; y < size - 1; y++)
            {
                cells[IX(0, y, 0, size)] = 0.5 * (cells[IX(1, y, 0, size)] + cells[IX(0, y, 1, size)]);
                cells[IX(size - 1, y, 0, size)] = 0.5 * (cells[IX(size - 2, y, 0, size)] + cells[IX(size - 1, y, 1, size)]);
                cells[IX(0, y, size - 1, size)] = 0.5 * (cells[IX(1, y, size - 1, size)] + cells[IX(0, y, size - 2, size)]);
                cells[IX(size - 1, y, size - 1, size)] = 0.5 * (cells[IX(size - 2, y, size - 1, size)] + cells[IX(size - 1, y, size - 2, size)]);
            }

            for (int z = 0; z < size - 1; z++)
            {
                cells[IX(0, 0, z, size)] = 0.5 * (cells[IX(1, 0, z, size)] + cells[IX(0, 1, z, size)]);
                cells[IX(size - 1, 0, z, size)] = 0.5 * (cells[IX(size - 2, 0, z, size)] + cells[IX(size - 1, 1, z, size)]);
                cells[IX(0, size - 1, z, size)] = 0.5 * (cells[IX(1, size - 1, z, size)] + cells[IX(0, size - 2, z, size)]);
                cells[IX(size - 1, size - 1, z, size)] = 0.5 * (cells[IX(size - 2, size - 1, z, size)] + cells[IX(size - 1, size - 2, z, size)]);
            }

            #endregion

            #region Corners

            // Corners take average of neighbors
            cells[IX(0, 0, 0, size)] = ONETHIRD * (
                cells[IX(1, 0, 0, size)] +
                cells[IX(0, 1, 0, size)] +
                cells[IX(0, 0, 1, size)]);

            cells[IX(0, size - 1, 0, size)] = ONETHIRD * (
                cells[IX(1, size - 1, 0, size)] +
                cells[IX(0, size - 2, 0, size)] +
                cells[IX(0, size - 1, 1, size)]);

            cells[IX(0, 0, size - 1, size)] = ONETHIRD * (
                cells[IX(1, 0, size - 1, size)] +
                cells[IX(0, 1, size - 1, size)] +
                cells[IX(0, 0, size - 2, size)]);

            cells[IX(0, size - 1, size - 1, size)] = ONETHIRD * (
                cells[IX(1, size - 1, size - 1, size)] +
                cells[IX(0, size - 2, size - 1, size)] +
                cells[IX(0, size - 1, size - 2, size)]);

            cells[IX(size - 1, 0, 0, size)] = ONETHIRD * (
                cells[IX(size - 2, 0, 0, size)] +
                cells[IX(size - 1, 1, 0, size)] +
                cells[IX(size - 1, 0, 1, size)]);

            cells[IX(size - 1, size - 1, 0, size)] = ONETHIRD * (
                cells[IX(size - 2, size - 1, 0, size)] +
                cells[IX(size - 1, size - 2, 0, size)] +
                cells[IX(size - 1, size - 1, 1, size)]);

            cells[IX(size - 1, 0, size - 1, size)] = ONETHIRD * (
                cells[IX(size - 2, 0, size - 1, size)] +
                cells[IX(size - 1, 1, size - 1, size)] +
                cells[IX(size - 1, 0, size - 2, size)]);

            cells[IX(size - 1, size - 1, size - 1, size)] = ONETHIRD * (
                cells[IX(size - 2, size - 1, size - 1, size)] +
                cells[IX(size - 1, size - 2, size - 1, size)] +
                cells[IX(size - 1, size - 1, size - 2, size)]);

            #endregion
        }
        private static void SetBoundry_Open(SetBoundsType whichSide, double[] cells, int size, BoundrySettings boundrySettings)
        {
            // OpenBox needs to allow open flow for the velocity, but just copy cells for everything else
            //NOTE: Allow outflow, but not full inflow (the inflow becomes self reinforcing, and the whole field becomes a wall of wind)
            //NOTE: Inflow is only an issue along the wall where the corresponding velocity array is parallel (so the velocityX array only needs to worry about the x wall.  The y and z walls are safe to copy values, because the flow is perpendicular to them)

            #region X wall

            if (whichSide == SetBoundsType.VelocityX)
            {
                for (int z = 1; z < size - 1; z++)
                {
                    for (int y = 1; y < size - 1; y++)
                    {
                        cells[IX(0, y, z, size)] = SetBoundry_OpenSprtCap(cells[IX(1, y, z, size)], false);
                        cells[IX(size - 1, y, z, size)] = SetBoundry_OpenSprtCap(cells[IX(size - 2, y, z, size)], true);
                    }
                }
            }
            else
            {
                for (int z = 1; z < size - 1; z++)
                {
                    for (int y = 1; y < size - 1; y++)
                    {
                        cells[IX(0, y, z, size)] = cells[IX(1, y, z, size)];
                        cells[IX(size - 1, y, z, size)] = cells[IX(size - 2, y, z, size)];
                    }
                }
            }

            #endregion
            #region Y wall

            if (whichSide == SetBoundsType.VelocityY)
            {
                for (int z = 1; z < size - 1; z++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        cells[IX(x, 0, z, size)] = SetBoundry_OpenSprtCap(cells[IX(x, 1, z, size)], false);
                        cells[IX(x, size - 1, z, size)] = SetBoundry_OpenSprtCap(cells[IX(x, size - 2, z, size)], true);
                    }
                }
            }
            else
            {
                for (int z = 1; z < size - 1; z++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        cells[IX(x, 0, z, size)] = cells[IX(x, 1, z, size)];
                        cells[IX(x, size - 1, z, size)] = cells[IX(x, size - 2, z, size)];
                    }
                }
            }

            #endregion
            #region Z wall

            if (whichSide == SetBoundsType.VelocityZ)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        cells[IX(x, y, 0, size)] = SetBoundry_OpenSprtCap(cells[IX(x, y, 1, size)], false);
                        cells[IX(x, y, size - 1, size)] = SetBoundry_OpenSprtCap(cells[IX(x, y, size - 2, size)], true);
                    }
                }
            }
            else
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        cells[IX(x, y, 0, size)] = cells[IX(x, y, 1, size)];
                        cells[IX(x, y, size - 1, size)] = cells[IX(x, y, size - 2, size)];
                    }
                }
            }

            #endregion

            #region Edges

            for (int x = 1; x < size - 1; x++)
            {
                cells[IX(x, 0, 0, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, 1, 0, size)], false) : cells[IX(x, 1, 0, size)]) +      // restrict y travel when it's the y veloctiy array
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, 0, 1, size)], false) : cells[IX(x, 0, 1, size)])        // restrict z travel when it's the z velocity array
                    );

                cells[IX(x, size - 1, 0, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, size - 2, 0, size)], true) : cells[IX(x, size - 2, 0, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, size - 1, 1, size)], false) : cells[IX(x, size - 1, 1, size)])
                    );

                cells[IX(x, 0, size - 1, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, 1, size - 1, size)], false) : cells[IX(x, 1, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, 0, size - 2, size)], true) : cells[IX(x, 0, size - 2, size)])
                    );

                cells[IX(x, size - 1, size - 1, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, size - 2, size - 1, size)], true) : cells[IX(x, size - 2, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, size - 1, size - 2, size)], true) : cells[IX(x, size - 1, size - 2, size)])
                    );
            }

            for (int y = 1; y < size - 1; y++)
            {
                cells[IX(0, y, 0, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, y, 0, size)], false) : cells[IX(1, y, 0, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, y, 1, size)], false) : cells[IX(0, y, 1, size)])
                    );

                cells[IX(size - 1, y, 0, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, y, 0, size)], true) : cells[IX(size - 2, y, 0, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, y, 1, size)], false) : cells[IX(size - 1, y, 1, size)])
                    );

                cells[IX(0, y, size - 1, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, y, size - 1, size)], false) : cells[IX(1, y, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, y, size - 2, size)], true) : cells[IX(0, y, size - 2, size)])
                    );

                cells[IX(size - 1, y, size - 1, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, y, size - 1, size)], true) : cells[IX(size - 2, y, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, y, size - 2, size)], true) : cells[IX(size - 1, y, size - 2, size)])
                    );
            }

            for (int z = 0; z < size - 1; z++)
            {
                cells[IX(0, 0, z, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, 0, z, size)], false) : cells[IX(1, 0, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, 1, z, size)], false) : cells[IX(0, 1, z, size)])
                    );

                cells[IX(size - 1, 0, z, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, 0, z, size)], true) : cells[IX(size - 2, 0, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 1, z, size)], false) : cells[IX(size - 1, 1, z, size)])
                    );

                cells[IX(0, size - 1, z, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, size - 1, z, size)], false) : cells[IX(1, size - 1, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, size - 2, z, size)], true) : cells[IX(0, size - 2, z, size)])
                    );

                cells[IX(size - 1, size - 1, z, size)] = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, size - 1, z, size)], true) : cells[IX(size - 2, size - 1, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 2, z, size)], true) : cells[IX(size - 1, size - 2, z, size)])
                    );
            }

            #region FLAWED

            //double average1, average2, average3, average4;

            //for (int x = 1; x < size - 1; x++)
            //{
            //    average1 = 0.5 * (cells[IX(x, 1, 0, size)] + cells[IX(x, 0, 1, size)]);
            //    average2 = 0.5 * (cells[IX(x, size - 2, size - 1, size)] + cells[IX(x, size - 1, size - 2, size)]);
            //    average3 = 0.5 * (cells[IX(x, size - 2, 0, size)] + cells[IX(x, size - 1, 1, size)]);
            //    average4 = 0.5 * (cells[IX(x, 1, size - 1, size)] + cells[IX(x, 0, size - 2, size)]);

            //    if (whichSide == SetBoundsType.VelocityY || whichSide == SetBoundsType.VelocityZ)
            //    {
            //        average1 = SetBoundry_OpenBoxSprtCap(average1, false);
            //        average2 = SetBoundry_OpenBoxSprtCap(average2, true);
            //        average3 *= OPENINFLOWMULT;
            //        average4 *= OPENINFLOWMULT;
            //    }

            //    cells[IX(x, 0, 0, size)] = average1;
            //    cells[IX(x, size - 1, size - 1, size)] = average2;

            //    cells[IX(x, size - 1, 0, size)] = average3;
            //    cells[IX(x, 0, size - 1, size)] = average4;
            //}

            //for (int y = 1; y < size - 1; y++)
            //{
            //    average1 = 0.5 * (cells[IX(1, y, 0, size)] + cells[IX(0, y, 1, size)]);
            //    average2 = 0.5 * (cells[IX(size - 2, y, size - 1, size)] + cells[IX(size - 1, y, size - 2, size)]);
            //    average3 = 0.5 * (cells[IX(size - 2, y, 0, size)] + cells[IX(size - 1, y, 1, size)]);
            //    average4 = 0.5 * (cells[IX(1, y, size - 1, size)] + cells[IX(0, y, size - 2, size)]);

            //    if (whichSide == SetBoundsType.VelocityX || whichSide == SetBoundsType.VelocityZ)
            //    {
            //        average1 = SetBoundry_OpenBoxSprtCap(average1, false);
            //        average2 = SetBoundry_OpenBoxSprtCap(average2, true);
            //        average3 *= OPENINFLOWMULT;
            //        average4 *= OPENINFLOWMULT;
            //    }

            //    cells[IX(0, y, 0, size)] = average1;
            //    cells[IX(size - 1, y, size - 1, size)] = average2;

            //    cells[IX(size - 1, y, 0, size)] = average3;
            //    cells[IX(0, y, size - 1, size)] = average4;
            //}

            //for (int z = 0; z < size - 1; z++)
            //{
            //    average1 = 0.5 * (cells[IX(1, 0, z, size)] + cells[IX(0, 1, z, size)]);
            //    average2 = 0.5 * (cells[IX(size - 2, size - 1, z, size)] + cells[IX(size - 1, size - 2, z, size)]);
            //    average3 = 0.5 * (cells[IX(size - 2, 0, z, size)] + cells[IX(size - 1, 1, z, size)]);
            //    average4 = 0.5 * (cells[IX(1, size - 1, z, size)] + cells[IX(0, size - 2, z, size)]);

            //    if (whichSide == SetBoundsType.VelocityX || whichSide == SetBoundsType.VelocityY)
            //    {
            //        average1 = SetBoundry_OpenBoxSprtCap(average1, false);
            //        average2 = SetBoundry_OpenBoxSprtCap(average2, true);
            //        average3 *= OPENINFLOWMULT;
            //        average4 *= OPENINFLOWMULT;
            //    }

            //    cells[IX(0, 0, z, size)] = average1;
            //    cells[IX(size - 1, size - 1, z, size)] = average2;

            //    cells[IX(size - 1, 0, z, size)] = average3;
            //    cells[IX(0, size - 1, z, size)] = average4;
            //}

            #endregion

            #endregion

            #region Corners

            // Corners take average of neighbors

            cells[IX(0, 0, 0, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, 0, 0, size)], false) : cells[IX(1, 0, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, 1, 0, size)], false) : cells[IX(0, 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, 0, 1, size)], false) : cells[IX(0, 0, 1, size)])
                );

            cells[IX(0, size - 1, 0, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, size - 1, 0, size)], false) : cells[IX(1, size - 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, size - 2, 0, size)], true) : cells[IX(0, size - 2, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, size - 1, 1, size)], false) : cells[IX(0, size - 1, 1, size)])
                );

            cells[IX(0, 0, size - 1, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, 0, size - 1, size)], false) : cells[IX(1, 0, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, 1, size - 1, size)], false) : cells[IX(0, 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, 0, size - 2, size)], true) : cells[IX(0, 0, size - 2, size)])
                );

            cells[IX(0, size - 1, size - 1, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, size - 1, size - 1, size)], false) : cells[IX(1, size - 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, size - 2, size - 1, size)], true) : cells[IX(0, size - 2, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, size - 1, size - 2, size)], true) : cells[IX(0, size - 1, size - 2, size)])
                );

            cells[IX(size - 1, 0, 0, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, 0, 0, size)], true) : cells[IX(size - 2, 0, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 1, 0, size)], false) : cells[IX(size - 1, 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 0, 1, size)], false) : cells[IX(size - 1, 0, 1, size)])
                );

            cells[IX(size - 1, size - 1, 0, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, size - 1, 0, size)], true) : cells[IX(size - 2, size - 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 2, 0, size)], true) : cells[IX(size - 1, size - 2, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 1, 1, size)], false) : cells[IX(size - 1, size - 1, 1, size)])
                );

            cells[IX(size - 1, 0, size - 1, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, 0, size - 1, size)], true) : cells[IX(size - 2, 0, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 1, size - 1, size)], false) : cells[IX(size - 1, 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 0, size - 2, size)], true) : cells[IX(size - 1, 0, size - 2, size)])
                );

            cells[IX(size - 1, size - 1, size - 1, size)] = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, size - 1, size - 1, size)], true) : cells[IX(size - 2, size - 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 2, size - 1, size)], true) : cells[IX(size - 1, size - 2, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 1, size - 2, size)], true) : cells[IX(size - 1, size - 1, size - 2, size)])
                );


            #region FLAWED

            //double average;
            //bool isVelocity = whichSide == SetBoundsType.VelocityX || whichSide == SetBoundsType.VelocityY || whichSide == SetBoundsType.VelocityZ;

            //// 1
            //average = ONETHIRD * (
            //    cells[IX(1, 0, 0, size)] +
            //    cells[IX(0, 1, 0, size)] +
            //    cells[IX(0, 0, 1, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(0, 0, 0, size)] = average;

            //// 2
            //average = ONETHIRD * (
            //    cells[IX(1, size - 1, 0, size)] +
            //    cells[IX(0, size - 2, 0, size)] +
            //    cells[IX(0, size - 1, 1, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(0, size - 1, 0, size)] = average;

            //// 3
            //average = ONETHIRD * (
            //    cells[IX(1, 0, size - 1, size)] +
            //    cells[IX(0, 1, size - 1, size)] +
            //    cells[IX(0, 0, size - 2, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(0, 0, size - 1, size)] = average;

            //// 4
            //average = ONETHIRD * (
            //    cells[IX(1, size - 1, size - 1, size)] +
            //    cells[IX(0, size - 2, size - 1, size)] +
            //    cells[IX(0, size - 1, size - 2, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(0, size - 1, size - 1, size)] = average;

            //// 5
            //average = ONETHIRD * (
            //    cells[IX(size - 2, 0, 0, size)] +
            //    cells[IX(size - 1, 1, 0, size)] +
            //    cells[IX(size - 1, 0, 1, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(size - 1, 0, 0, size)] = average;

            //// 6
            //average = ONETHIRD * (
            //    cells[IX(size - 2, size - 1, 0, size)] +
            //    cells[IX(size - 1, size - 2, 0, size)] +
            //    cells[IX(size - 1, size - 1, 1, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(size - 1, size - 1, 0, size)] = average;

            //// 7
            //average = ONETHIRD * (
            //    cells[IX(size - 2, 0, size - 1, size)] +
            //    cells[IX(size - 1, 1, size - 1, size)] +
            //    cells[IX(size - 1, 0, size - 2, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(size - 1, 0, size - 1, size)] = average;

            //// 8
            //average = ONETHIRD * (
            //    cells[IX(size - 2, size - 1, size - 1, size)] +
            //    cells[IX(size - 1, size - 2, size - 1, size)] +
            //    cells[IX(size - 1, size - 1, size - 2, size)]);

            //if (isVelocity) average *= OPENINFLOWMULT;

            //cells[IX(size - 1, size - 1, size - 1, size)] = average;

            #endregion

            #endregion
        }
        private static double SetBoundry_OpenSprtCap(double newValue, bool allowPositive)
        {
            bool isPositive = newValue > 0;

            if (isPositive == allowPositive)
            {
                return newValue;
            }
            else
            {
                //return 0;     // too restrictive
                return newValue * OPENINFLOWMULT;
            }
        }
        private static void SetBoundry_OpenShared(SetBoundsType whichSide, double[] cells, int size, BoundrySettings boundrySettings)
        {
            // This method is sort of a combination of SetBoundry_Open and SetBoundry_OpenSlaved.  It stores the average of what
            // open would have stored with what slave would have stored: (open + slave) / 2

            double[] source;

            switch (whichSide)
            {
                case SetBoundsType.VelocityX:
                    source = boundrySettings.OpenBorderVelocityX;
                    break;

                case SetBoundsType.VelocityY:
                    source = boundrySettings.OpenBorderVelocityY;
                    break;

                case SetBoundsType.VelocityZ:
                    source = boundrySettings.OpenBorderVelocityZ;
                    break;

                default:
                    // Non velocity can use the standard open method
                    SetBoundry_Open(whichSide, cells, size, boundrySettings);
                    return;
            }

            #region X wall

            int index;

            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    index = IX(0, y, z, size);
                    cells[index] = .5d * (source[index] + cells[IX(1, y, z, size)]);

                    index = IX(size - 1, y, z, size);
                    cells[index] = .5d * (source[index] + cells[IX(size - 2, y, z, size)]);
                }
            }

            #endregion
            #region Y wall

            for (int z = 1; z < size - 1; z++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    index = IX(x, 0, z, size);
                    cells[index] = .5d * (source[index] + cells[IX(x, 1, z, size)]);

                    index = IX(x, size - 1, z, size);
                    cells[index] = .5d * (source[index] + cells[IX(x, size - 2, z, size)]);
                }
            }

            #endregion
            #region Z wall

            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    index = IX(x, y, 0, size);
                    cells[index] = .5d * (source[index] + cells[IX(x, y, 1, size)]);

                    index = IX(x, y, size - 1, size);
                    cells[index] = .5d * (source[index] + cells[IX(x, y, size - 2, size)]);
                }
            }

            #endregion

            #region Edges

            double thisValue;

            for (int x = 1; x < size - 1; x++)
            {
                index = IX(x, 0, 0, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, 1, 0, size)], false) : cells[IX(x, 1, 0, size)]) +      // restrict y travel when it's the y veloctiy array
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, 0, 1, size)], false) : cells[IX(x, 0, 1, size)])        // restrict z travel when it's the z velocity array
                    );
                cells[index] = .5d * (source[index] + thisValue);       // now take the average of this field's value with the source.  NOTE: Giving the two equal weight, that's why I don't just add the 3 and divide by 3.

                index = IX(x, size - 1, 0, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, size - 2, 0, size)], true) : cells[IX(x, size - 2, 0, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, size - 1, 1, size)], false) : cells[IX(x, size - 1, 1, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(x, 0, size - 1, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, 1, size - 1, size)], false) : cells[IX(x, 1, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, 0, size - 2, size)], true) : cells[IX(x, 0, size - 2, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(x, size - 1, size - 1, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(x, size - 2, size - 1, size)], true) : cells[IX(x, size - 2, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(x, size - 1, size - 2, size)], true) : cells[IX(x, size - 1, size - 2, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);
            }

            for (int y = 1; y < size - 1; y++)
            {
                index = IX(0, y, 0, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, y, 0, size)], false) : cells[IX(1, y, 0, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, y, 1, size)], false) : cells[IX(0, y, 1, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(size - 1, y, 0, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, y, 0, size)], true) : cells[IX(size - 2, y, 0, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, y, 1, size)], false) : cells[IX(size - 1, y, 1, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(0, y, size - 1, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, y, size - 1, size)], false) : cells[IX(1, y, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, y, size - 2, size)], true) : cells[IX(0, y, size - 2, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(size - 1, y, size - 1, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, y, size - 1, size)], true) : cells[IX(size - 2, y, size - 1, size)]) +
                    (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, y, size - 2, size)], true) : cells[IX(size - 1, y, size - 2, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);
            }

            for (int z = 0; z < size - 1; z++)
            {
                index = IX(0, 0, z, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, 0, z, size)], false) : cells[IX(1, 0, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, 1, z, size)], false) : cells[IX(0, 1, z, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(size - 1, 0, z, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, 0, z, size)], true) : cells[IX(size - 2, 0, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 1, z, size)], false) : cells[IX(size - 1, 1, z, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(0, size - 1, z, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, size - 1, z, size)], false) : cells[IX(1, size - 1, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, size - 2, z, size)], true) : cells[IX(0, size - 2, z, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);

                index = IX(size - 1, size - 1, z, size);
                thisValue = 0.5 * (
                    (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, size - 1, z, size)], true) : cells[IX(size - 2, size - 1, z, size)]) +
                    (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 2, z, size)], true) : cells[IX(size - 1, size - 2, z, size)])
                    );
                cells[index] = .5d * (source[index] + thisValue);
            }

            #endregion

            #region Corners

            // Corners take average of neighbors

            index = IX(0, 0, 0, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, 0, 0, size)], false) : cells[IX(1, 0, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, 1, 0, size)], false) : cells[IX(0, 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, 0, 1, size)], false) : cells[IX(0, 0, 1, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            index = IX(0, size - 1, 0, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, size - 1, 0, size)], false) : cells[IX(1, size - 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, size - 2, 0, size)], true) : cells[IX(0, size - 2, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, size - 1, 1, size)], false) : cells[IX(0, size - 1, 1, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            index = IX(0, 0, size - 1, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, 0, size - 1, size)], false) : cells[IX(1, 0, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, 1, size - 1, size)], false) : cells[IX(0, 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, 0, size - 2, size)], true) : cells[IX(0, 0, size - 2, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            index = IX(0, size - 1, size - 1, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(1, size - 1, size - 1, size)], false) : cells[IX(1, size - 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(0, size - 2, size - 1, size)], true) : cells[IX(0, size - 2, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(0, size - 1, size - 2, size)], true) : cells[IX(0, size - 1, size - 2, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            index = IX(size - 1, 0, 0, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, 0, 0, size)], true) : cells[IX(size - 2, 0, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 1, 0, size)], false) : cells[IX(size - 1, 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 0, 1, size)], false) : cells[IX(size - 1, 0, 1, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            index = IX(size - 1, size - 1, 0, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, size - 1, 0, size)], true) : cells[IX(size - 2, size - 1, 0, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 2, 0, size)], true) : cells[IX(size - 1, size - 2, 0, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 1, 1, size)], false) : cells[IX(size - 1, size - 1, 1, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            index = IX(size - 1, 0, size - 1, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, 0, size - 1, size)], true) : cells[IX(size - 2, 0, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 1, size - 1, size)], false) : cells[IX(size - 1, 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, 0, size - 2, size)], true) : cells[IX(size - 1, 0, size - 2, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            index = IX(size - 1, size - 1, size - 1, size);
            thisValue = ONETHIRD * (
                (whichSide == SetBoundsType.VelocityX ? SetBoundry_OpenSprtCap(cells[IX(size - 2, size - 1, size - 1, size)], true) : cells[IX(size - 2, size - 1, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityY ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 2, size - 1, size)], true) : cells[IX(size - 1, size - 2, size - 1, size)]) +
                (whichSide == SetBoundsType.VelocityZ ? SetBoundry_OpenSprtCap(cells[IX(size - 1, size - 1, size - 2, size)], true) : cells[IX(size - 1, size - 1, size - 2, size)])
                );
            cells[index] = .5d * (source[index] + thisValue);

            #endregion
        }
        private static void SetBoundry_OpenSlaved(SetBoundsType whichSide, double[] cells, int size, BoundrySettings boundrySettings)
        {
            double[] source;

            switch (whichSide)
            {
                case SetBoundsType.VelocityX:
                    source = boundrySettings.OpenBorderVelocityX;
                    break;

                case SetBoundsType.VelocityY:
                    source = boundrySettings.OpenBorderVelocityY;
                    break;

                case SetBoundsType.VelocityZ:
                    source = boundrySettings.OpenBorderVelocityZ;
                    break;

                default:
                    // Non velocity can use the standard open method
                    SetBoundry_Open(whichSide, cells, size, boundrySettings);
                    return;
            }

            // When slaved, this field's velocity is just a copy of the source's velocity
            foreach (var index in boundrySettings.OpenBorderCells)
            {
                cells[index.Offset1D] = source[index.Offset1D];
            }
        }
        private static void SetBoundry_ReachAround(SetBoundsType whichSide, double[] cells, int size, BoundrySettings boundrySettings)
        {
            double average, sum, count;

            bool wrapX = whichSide == SetBoundsType.VelocityX || whichSide == SetBoundsType.Ink;
            bool wrapY = whichSide == SetBoundsType.VelocityY || whichSide == SetBoundsType.Ink;
            bool wrapZ = whichSide == SetBoundsType.VelocityZ || whichSide == SetBoundsType.Ink;

            #region X wall

            if (wrapX)
            {
                // Copy of other side
                for (int z = 1; z < size - 1; z++)
                {
                    for (int y = 1; y < size - 1; y++)
                    {
                        cells[IX(0, y, z, size)] = cells[IX(size - 2, y, z, size)];
                        cells[IX(size - 1, y, z, size)] = cells[IX(1, y, z, size)];
                    }
                }
            }
            else
            {
                // Average
                for (int z = 1; z < size - 1; z++)
                {
                    for (int y = 1; y < size - 1; y++)
                    {
                        average = .5d * (cells[IX(1, y, z, size)] + cells[IX(size - 2, y, z, size)]);

                        cells[IX(0, y, z, size)] = average;
                        cells[IX(size - 1, y, z, size)] = average;
                    }
                }
            }

            #endregion
            #region Y wall

            if (wrapY)
            {
                // Copy of other side
                for (int z = 1; z < size - 1; z++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        cells[IX(x, 0, z, size)] = cells[IX(x, size - 2, z, size)];
                        cells[IX(x, size - 1, z, size)] = cells[IX(x, 1, z, size)];
                    }
                }
            }
            else
            {
                // Average
                for (int z = 1; z < size - 1; z++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        average = .5d * (cells[IX(x, 1, z, size)] + cells[IX(x, size - 2, z, size)]);

                        cells[IX(x, 0, z, size)] = average;
                        cells[IX(x, size - 1, z, size)] = average;
                    }
                }
            }

            #endregion
            #region Z wall

            if (wrapZ)
            {
                // Copy of other side
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        cells[IX(x, y, 0, size)] = cells[IX(x, y, size - 2, size)];
                        cells[IX(x, y, size - 1, size)] = cells[IX(x, y, 1, size)];
                    }
                }
            }
            else
            {
                // Average
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        average = .5d * (cells[IX(x, y, 1, size)] + cells[IX(x, y, size - 2, size)]);

                        cells[IX(x, y, 0, size)] = average;
                        cells[IX(x, y, size - 1, size)] = average;
                    }
                }
            }

            #endregion

            #region X edges

            for (int x = 1; x < size - 1; x++)
            {
                // 1
                sum = 0; count = 0;

                sum += cells[IX(x, size - 2, 0, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(x, 1, 0, size)]; count++;
                }

                sum += cells[IX(x, 0, size - 2, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(x, 0, 1, size)]; count++;
                }

                cells[IX(x, 0, 0, size)] = sum / count;

                // 2
                sum = 0; count = 0;

                sum += cells[IX(x, 1, 0, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(x, size - 2, 0, size)]; count++;
                }

                sum += cells[IX(x, size - 1, size - 2, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(x, size - 1, 1, size)]; count++;
                }

                cells[IX(x, size - 1, 0, size)] = sum / count;

                // 3
                sum = 0; count = 0;

                sum += cells[IX(x, size - 2, size - 1, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(x, 1, size - 1, size)]; count++;
                }

                sum += cells[IX(x, 0, 1, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(x, 0, size - 2, size)]; count++;
                }

                cells[IX(x, 0, size - 1, size)] = sum / count;

                // 4
                sum = 0; count = 0;

                sum += cells[IX(x, 1, size - 1, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(x, size - 2, size - 1, size)]; count++;
                }

                sum += cells[IX(x, size - 1, 1, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(x, size - 1, size - 2, size)]; count++;
                }

                cells[IX(x, size - 1, size - 1, size)] = sum / count;
            }

            #endregion
            #region Y edges

            for (int y = 1; y < size - 1; y++)
            {
                // 1
                sum = 0; count = 0;

                sum += cells[IX(size - 2, y, 0, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(1, y, 0, size)]; count++;
                }

                sum += cells[IX(0, y, size - 2, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(0, y, 1, size)]; count++;
                }

                cells[IX(0, y, 0, size)] = sum / count;

                // 2
                sum = 0; count = 0;

                sum += cells[IX(1, y, 0, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(size - 2, y, 0, size)]; count++;
                }

                sum += cells[IX(size - 1, y, size - 2, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(size - 1, y, 1, size)]; count++;
                }

                cells[IX(size - 1, y, 0, size)] = sum / count;

                // 3
                sum = 0; count = 0;

                sum += cells[IX(size - 2, y, size - 1, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(1, y, size - 1, size)]; count++;
                }

                sum += cells[IX(0, y, 1, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(0, y, size - 2, size)]; count++;
                }

                cells[IX(0, y, size - 1, size)] = sum / count;

                // 4
                sum = 0; count = 0;

                sum += cells[IX(1, y, size - 1, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(size - 2, y, size - 1, size)]; count++;
                }

                sum += cells[IX(size - 1, y, 1, size)]; count++;
                if (!wrapZ)
                {
                    sum += cells[IX(size - 1, y, size - 2, size)]; count++;
                }

                cells[IX(size - 1, y, size - 1, size)] = sum / count;
            }

            #endregion
            #region Z edges

            for (int z = 0; z < size - 1; z++)
            {
                // 1
                sum = 0; count = 0;

                sum += cells[IX(size - 2, 0, z, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(1, 0, z, size)]; count++;
                }

                sum += cells[IX(0, size - 2, z, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(0, 1, z, size)]; count++;
                }

                cells[IX(0, 0, z, size)] = sum / count;

                // 2
                sum = 0; count = 0;

                sum += cells[IX(1, 0, z, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(size - 2, 0, z, size)]; count++;
                }

                sum += cells[IX(size - 1, size - 2, z, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(size - 1, 1, z, size)]; count++;
                }

                cells[IX(size - 1, 0, z, size)] = sum / count;

                // 3
                sum = 0; count = 0;

                sum += cells[IX(size - 2, size - 1, z, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(1, size - 1, z, size)]; count++;
                }

                sum += cells[IX(0, 1, z, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(0, size - 2, z, size)]; count++;
                }

                cells[IX(0, size - 1, z, size)] = sum / count;

                // 4
                sum = 0; count = 0;

                sum += cells[IX(1, size - 1, z, size)]; count++;
                if (!wrapX)
                {
                    sum += cells[IX(size - 2, size - 1, z, size)]; count++;
                }

                sum += cells[IX(size - 1, 1, z, size)]; count++;
                if (!wrapY)
                {
                    sum += cells[IX(size - 1, size - 2, z, size)]; count++;
                }

                cells[IX(size - 1, size - 1, z, size)] = sum / count;
            }

            #endregion

            #region Corners

            // 1
            sum = 0; count = 0;

            sum += cells[IX(size - 2, 0, 0, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(1, 0, 0, size)]; count++;
            }

            sum += cells[IX(0, size - 2, 0, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(0, 1, 0, size)]; count++;
            }

            sum += cells[IX(0, 0, size - 2, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(0, 0, 1, size)]; count++;
            }

            cells[IX(0, 0, 0, size)] = sum / count;

            // 2
            sum = 0; count = 0;

            sum += cells[IX(size - 2, size - 1, 0, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(1, size - 1, 0, size)]; count++;
            }

            sum += cells[IX(0, 1, 0, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(0, size - 2, 0, size)]; count++;
            }

            sum += cells[IX(0, size - 1, size - 2, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(0, size - 1, 1, size)]; count++;
            }

            cells[IX(0, size - 1, 0, size)] = sum / count;

            // 3
            sum = 0; count = 0;

            sum += cells[IX(size - 2, 0, size - 1, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(1, 0, size - 1, size)]; count++;
            }

            sum += cells[IX(0, size - 2, size - 1, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(0, 1, size - 1, size)]; count++;
            }

            sum += cells[IX(0, 0, 1, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(0, 0, size - 2, size)]; count++;
            }

            cells[IX(0, 0, size - 1, size)] = sum / count;

            // 4
            sum = 0; count = 0;

            sum += cells[IX(size - 2, size - 1, size - 1, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(1, size - 1, size - 1, size)]; count++;
            }

            sum += cells[IX(0, 1, size - 1, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(0, size - 2, size - 1, size)]; count++;
            }

            sum += cells[IX(0, size - 1, 1, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(0, size - 1, size - 2, size)]; count++;
            }

            cells[IX(0, size - 1, size - 1, size)] = sum / count;

            // 5
            sum = 0; count = 0;

            sum += cells[IX(1, 0, 0, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(size - 2, 0, 0, size)]; count++;
            }

            sum += cells[IX(size - 1, size - 2, 0, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(size - 1, 1, 0, size)]; count++;
            }

            sum += cells[IX(size - 1, 0, size - 2, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(size - 1, 0, 1, size)]; count++;
            }

            cells[IX(size - 1, 0, 0, size)] = sum / count;

            // 6
            sum = 0; count = 0;

            sum += cells[IX(1, size - 1, 0, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(size - 2, size - 1, 0, size)]; count++;
            }

            sum += cells[IX(size - 1, 1, 0, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(size - 1, size - 2, 0, size)]; count++;
            }

            sum += cells[IX(size - 1, size - 1, size - 2, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(size - 1, size - 1, 1, size)]; count++;
            }

            cells[IX(size - 1, size - 1, 0, size)] = sum / count;

            // 7
            sum = 0; count = 0;

            sum += cells[IX(1, 0, size - 1, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(size - 2, 0, size - 1, size)]; count++;
            }

            sum += cells[IX(size - 1, size - 2, size - 1, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(size - 1, 1, size - 1, size)]; count++;
            }

            sum += cells[IX(size - 1, 0, 1, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(size - 1, 0, size - 2, size)]; count++;
            }

            cells[IX(size - 1, 0, size - 1, size)] = sum / count;

            // 8
            sum = 0; count = 0;

            sum += cells[IX(1, size - 1, size - 1, size)]; count++;
            if (!wrapX)
            {
                sum += cells[IX(size - 2, size - 1, size - 1, size)]; count++;
            }

            sum += cells[IX(size - 1, 1, size - 1, size)]; count++;
            if (!wrapY)
            {
                sum += cells[IX(size - 1, size - 2, size - 1, size)]; count++;
            }

            sum += cells[IX(size - 1, size - 1, 1, size)]; count++;
            if (!wrapZ)
            {
                sum += cells[IX(size - 1, size - 1, size - 2, size)]; count++;
            }

            cells[IX(size - 1, size - 1, size - 1, size)] = sum / count;

            #endregion
        }

        private static void SetBoundry_BlockedCells(SetBoundsType whichSide, double[] cells, BoundrySettings boundrySettings)
        {
            //NOTE: It is safe to ignore blocked cells that are interior (surrounded by other blocked cells)

            IndexLERP[] blocked = null;

            switch (whichSide)
            {
                case SetBoundsType.VelocityX:
                    blocked = boundrySettings.Blocked_VelocityX;
                    break;

                case SetBoundsType.VelocityY:
                    blocked = boundrySettings.Blocked_VelocityY;
                    break;

                case SetBoundsType.VelocityZ:
                    blocked = boundrySettings.Blocked_VelocityZ;
                    break;

                case SetBoundsType.Ink:
                case SetBoundsType.Other:
                    blocked = boundrySettings.Blocked_Other;
                    break;

                default:
                    throw new ApplicationException("Unknown SetBoundsType: " + whichSide.ToString());
            }

            foreach (IndexLERP index in blocked)
            {
                // Add up the neighbors
                double newValue = 0d;

                if (index.Neighbors != null)
                {
                    foreach (var neighbor in index.Neighbors)
                    {
                        newValue += cells[neighbor.Item1] * neighbor.Item2;
                    }
                }

                // Store the average
                cells[index.Index1D] = newValue;
            }
        }

        /// <summary>
        /// Blocked cells are just booleans.  But it is ineficient to scan for them multiple times each step.  So this is called at
        /// the beginning of the step to build indexed views of the blocked cells
        /// </summary>
        private void IndexBlockedCells()
        {
            if (!_boundrySettings.IsBlockCacheDirty)
            {
                return;
            }

            _boundrySettings.HasBlockedCells = false;

            List<IndexLERP> velocityX = new List<IndexLERP>();
            List<IndexLERP> velocityY = new List<IndexLERP>();
            List<IndexLERP> velocityZ = new List<IndexLERP>();
            List<IndexLERP> other = new List<IndexLERP>();

            List<int> totalBlock = new List<int>();

            List<int> open = new List<int>();

            // For now, ignore blocked cells that are on the edges of the field

            for (int x = 1; x < _size - 1; x++)
            {
                for (int y = 1; y < _size - 1; y++)
                {
                    for (int z = 1; z < _size - 1; z++)
                    {
                        int index1D = Get1DIndex(x, y, z);

                        if (!_blocked[index1D])
                        {
                            continue;
                        }

                        #region Detect total block

                        bool foundOpen = false;

                        for (int x0 = x - 1; x0 <= x + 1; x0++)
                        {
                            for (int y0 = y - 1; y0 <= y + 1; y0++)
                            {
                                for (int z0 = z - 1; z0 <= z + 1; z0++)
                                {
                                    if (!_blocked[Get1DIndex(x0, y0, z0)])     // not bothering to ignore the case where x0==x && y0==y && z0==z.  It wouldn't change the outcome of this scan, and would just be an unnecessary if statement
                                    {
                                        foundOpen = true;
                                        break;
                                    }
                                }

                                if (foundOpen)
                                {
                                    break;
                                }
                            }

                            if (foundOpen)
                            {
                                break;
                            }
                        }

                        if (!foundOpen)
                        {
                            // This cell is blocked by all sides.  The rest of this loop handles exposed blocked cells, so go to the next one
                            totalBlock.Add(index1D);
                            continue;
                        }

                        #endregion

                        // Plate - YZ - X free
                        if (IndexBlockedCells_Plate(Axis.Y, Axis.Z, Axis.X, velocityY, velocityZ, velocityX, other, y, z, x, index1D))
                        {
                            continue;
                        }

                        // Plate - XZ - Y free
                        if (IndexBlockedCells_Plate(Axis.X, Axis.Z, Axis.Y, velocityX, velocityZ, velocityY, other, x, z, y, index1D))
                        {
                            continue;
                        }

                        // Plate - XY - Z free
                        if (IndexBlockedCells_Plate(Axis.X, Axis.Y, Axis.Z, velocityX, velocityY, velocityZ, other, x, y, z, index1D))
                        {
                            continue;
                        }

                        // Inside Edge,Corner
                        if (IndexBlockedCells_Inside(velocityX, velocityY, velocityZ, other, x, y, z, index1D))
                        {
                            continue;
                        }

                        // Outside Edge,Corner
                        IndexBlockedCells_Outside(velocityX, velocityY, velocityZ, other, x, y, z, index1D);
                    }
                }
            }

            // Store the values
            _boundrySettings.Blocked_VelocityX = velocityX.ToArray();
            _boundrySettings.Blocked_VelocityY = velocityY.ToArray();
            _boundrySettings.Blocked_VelocityZ = velocityZ.ToArray();
            _boundrySettings.Blocked_Other = other.ToArray();

            _boundrySettings.Blocked_Total = totalBlock.ToArray();

            _boundrySettings.HasBlockedCells = velocityX.Count > 0;       // the three arrays are the same size
            _boundrySettings.IsBlockCacheDirty = false;
        }
        private bool IndexBlockedCells_Plate(Axis plate1, Axis plate2, Axis orth, List<IndexLERP> velocityPlate1, List<IndexLERP> velocityPlate2, List<IndexLERP> velocityOrth, List<IndexLERP> other, int indexPlate1, int indexPlate2, int indexOrth, int index1D)
        {
            // Just look left right up down, don't bother with diagonals

            int x = -1;
            int y = -1;
            int z = -1;

            #region See if is plate

            Set3DIndex(ref x, ref y, ref z, indexOrth, orth);

            // Left
            Set3DIndex(ref x, ref y, ref z, indexPlate1 - 1, plate1);
            Set3DIndex(ref x, ref y, ref z, indexPlate2, plate2);

            if (!_blocked[IX(x, y, z)])
            {
                return false;
            }

            // Right
            Set3DIndex(ref x, ref y, ref z, indexPlate1 + 1, plate1);
            Set3DIndex(ref x, ref y, ref z, indexPlate2, plate2);

            if (!_blocked[IX(x, y, z)])
            {
                return false;
            }

            // Up
            Set3DIndex(ref x, ref y, ref z, indexPlate1, plate1);
            Set3DIndex(ref x, ref y, ref z, indexPlate2 - 1, plate2);

            if (!_blocked[IX(x, y, z)])
            {
                return false;
            }

            // Down
            Set3DIndex(ref x, ref y, ref z, indexPlate1, plate1);
            Set3DIndex(ref x, ref y, ref z, indexPlate2 + 1, plate2);

            if (!_blocked[IX(x, y, z)])
            {
                return false;
            }

            #endregion

            Set3DIndex(ref x, ref y, ref z, indexPlate1, plate1);
            Set3DIndex(ref x, ref y, ref z, indexPlate2, plate2);

            List<int> open = new List<int>();

            // Above plate
            Set3DIndex(ref x, ref y, ref z, indexOrth - 1, orth);
            int index = IX(x, y, z);
            if (!_blocked[index])
            {
                open.Add(index);
            }

            // Below plate
            Set3DIndex(ref x, ref y, ref z, indexOrth + 1, orth);
            index = IX(x, y, z);
            if (!_blocked[index])
            {
                open.Add(index);
            }

            // This wall segment is perpendicular to velocity flow, so reflect back
            double mult = -_boundrySettings.WallReflectivity * VEL_ORTH / open.Count;        // they need to add up to VEL_ORTH * -reflectivity
            Tuple<int, double>[] lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
            velocityOrth.Add(new IndexLERP(index1D, lerp));

            // These are parallel, so slide along them
            mult = VEL_ORTH / open.Count;        // they need to add up to VEL_ORTH
            lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
            velocityPlate1.Add(new IndexLERP(index1D, lerp));
            velocityPlate2.Add(new IndexLERP(index1D, lerp));

            mult = OTHER_ANY / open.Count;        // they need to add up to OTHER_ANY
            lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
            other.Add(new IndexLERP(index1D, lerp));

            return true;
        }
        private bool IndexBlockedCells_Inside(List<IndexLERP> velocityX, List<IndexLERP> velocityY, List<IndexLERP> velocityZ, List<IndexLERP> other, int x, int y, int z, int index1D)
        {
            // If all the orth are blocked, then this is an inside edge or corner

            #region See if inside edge/corner

            if (!_blocked[IX(x - 1, y, z)])
            {
                return false;
            }

            if (!_blocked[IX(x + 1, y, z)])
            {
                return false;
            }

            if (!_blocked[IX(x, y - 1, z)])
            {
                return false;
            }

            if (!_blocked[IX(x, y + 1, z)])
            {
                return false;
            }

            if (!_blocked[IX(x, y, z - 1)])
            {
                return false;
            }

            if (!_blocked[IX(x, y, z + 1)])
            {
                return false;
            }

            #endregion

            List<int> open = new List<int>();

            for (int x0 = x - 1; x0 <= x + 1; x0++)
            {
                for (int y0 = y - 1; y0 <= y + 1; y0++)
                {
                    for (int z0 = z - 1; z0 <= z + 1; z0++)
                    {
                        int index = IX(x0, y0, z0);     // there are 7 unnecessary calls, but the amount of effort required if this is a corner vs orthogonal would be excessive
                        if (!_blocked[index])       // since it is known that the current cell is blocked, and the orthogonal cells are blocked, it's ok to check again (easier to check again than to have some kind of list of do not check)
                        {
                            open.Add(index);
                        }
                    }
                }
            }

            double mult = -_boundrySettings.WallReflectivity * VEL_INSIDECORNER / open.Count;        // they need to add up to VEL_INSIDECORNER * -reflectivity
            Tuple<int, double>[] lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();

            velocityX.Add(new IndexLERP(index1D, lerp));
            velocityY.Add(new IndexLERP(index1D, lerp));
            velocityZ.Add(new IndexLERP(index1D, lerp));

            mult = OTHER_ANY / open.Count;        // they need to add up to OTHER_ANY
            lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
            other.Add(new IndexLERP(index1D, lerp));

            return true;
        }
        private void IndexBlockedCells_Outside(List<IndexLERP> velocityX, List<IndexLERP> velocityY, List<IndexLERP> velocityZ, List<IndexLERP> other, int x, int y, int z, int index1D)
        {
            List<int> open = new List<int>();

            // Store the orth indices as compass directions
            int N = IX(x, y - 1, z);
            int E = IX(x + 1, y, z);
            int S = IX(x, y + 1, z);
            int W = IX(x - 1, y, z);
            int I = IX(x, y, z - 1);        // arbitrarily using I and U for z-+1
            int U = IX(x, y, z + 1);

            #region Orth

            if (!_blocked[N])
            {
                open.Add(N);
            }

            if (!_blocked[E])
            {
                open.Add(E);
            }

            if (!_blocked[S])
            {
                open.Add(S);
            }

            if (!_blocked[W])
            {
                open.Add(W);
            }

            if (!_blocked[I])
            {
                open.Add(I);
            }

            if (!_blocked[U])
            {
                open.Add(U);
            }

            #endregion

            int index = -1;

            #region 2D corners

            // Z plane (NSEW)
            if (!_blocked[N] && !_blocked[E])
            {
                index = IX(x + 1, y - 1, z);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[E])
            {
                index = IX(x + 1, y + 1, z);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[W])
            {
                index = IX(x - 1, y + 1, z);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[N] && !_blocked[W])
            {
                index = IX(x - 1, y - 1, z);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            // X plane (NSIU)
            if (!_blocked[N] && !_blocked[I])
            {
                index = IX(x, y - 1, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[N] && !_blocked[U])
            {
                index = IX(x, y - 1, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[U])
            {
                index = IX(x, y + 1, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[I])
            {
                index = IX(x, y + 1, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            // Y plane (EWIU)
            if (!_blocked[W] && !_blocked[I])
            {
                index = IX(x - 1, y, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[E] && !_blocked[I])
            {
                index = IX(x + 1, y, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[E] && !_blocked[U])
            {
                index = IX(x + 1, y, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[W] && !_blocked[U])
            {
                index = IX(x - 1, y, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            #endregion
            #region 3D corners

            // Front
            if (!_blocked[N] && !_blocked[E] && !_blocked[I])
            {
                index = IX(x + 1, y - 1, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[E] && !_blocked[I])
            {
                index = IX(x + 1, y + 1, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[W] && !_blocked[I])
            {
                index = IX(x - 1, y + 1, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[N] && !_blocked[W] && !_blocked[I])
            {
                index = IX(x - 1, y - 1, z - 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            // Rear
            if (!_blocked[N] && !_blocked[E] && !_blocked[U])
            {
                index = IX(x + 1, y - 1, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[E] && !_blocked[U])
            {
                index = IX(x + 1, y + 1, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[S] && !_blocked[W] && !_blocked[U])
            {
                index = IX(x - 1, y + 1, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            if (!_blocked[N] && !_blocked[W] && !_blocked[U])
            {
                index = IX(x - 1, y - 1, z + 1);
                if (!_blocked[index])
                {
                    open.Add(index);
                }
            }

            #endregion

            double mult = -_boundrySettings.WallReflectivity * VEL_OUTSIDECORNER / open.Count;        // they need to add up to VEL_OUTSIDECORNER * -reflectivity
            Tuple<int, double>[] lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();

            velocityX.Add(new IndexLERP(index1D, lerp));
            velocityY.Add(new IndexLERP(index1D, lerp));
            velocityZ.Add(new IndexLERP(index1D, lerp));

            mult = OTHER_ANY / open.Count;        // they need to add up to OTHER_ANY
            lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
            other.Add(new IndexLERP(index1D, lerp));
        }

        private void PopulateOpenBorderVelocites()
        {
            if (_boundrySettings.BoundryType != FluidFieldBoundryType3D.Open_Shared && _boundrySettings.BoundryType != FluidFieldBoundryType3D.Open_Slaved)
            {
                return;
            }

            if (_boundrySettings.OpenBorderVelocityX == null)
            {
                // First time, create arrays
                _boundrySettings.OpenBorderVelocityX = new double[_size1D];
                _boundrySettings.OpenBorderVelocityY = new double[_size1D];
                _boundrySettings.OpenBorderVelocityZ = new double[_size1D];

                _boundrySettings.OpenBorderCells = GetBorderCellIndices();
            }

            // Set the border cell values
            if (_boundrySettings.OpenBoundryParent == null)
            {
                #region Stamp Zero

                foreach (var index in _boundrySettings.OpenBorderCells)
                {
                    _boundrySettings.OpenBorderVelocityX[index.Offset1D] = 0d;
                    _boundrySettings.OpenBorderVelocityY[index.Offset1D] = 0d;
                    _boundrySettings.OpenBorderVelocityZ[index.Offset1D] = 0d;
                }

                #endregion
            }
            else
            {
                #region Get values from parent

                Transform3D transform = new RotateTransform3D(new QuaternionRotation3D(this.RotationWorld));
                Transform3D transformInv = new RotateTransform3D(new QuaternionRotation3D(this.RotationWorld.ToReverse()));

                double fullSize = this.SizeWorld;
                double halfSize = fullSize / 2d;
                double cellSize = fullSize / _size;
                double cellSizeHalf = cellSize / 2d;

                double axisOffset = -halfSize + cellSizeHalf;

                // If I just take fullSize / _size, that would be a sphere inscribed within the cell.  If I take that times sqrt(3), that
                // would be the cell inscribed in the sphere.  So I'm splitting the difference, and averaging those two extremes
                double cellSizeRequest = cellSize * 1.366d;

                Point3D[] positions = new Point3D[_boundrySettings.OpenBorderCells.Length];
                for (int cntr = 0; cntr < _boundrySettings.OpenBorderCells.Length; cntr++)
                {
                    Vector3D offset = new Vector3D(
                        axisOffset + (_boundrySettings.OpenBorderCells[cntr].X * cellSize),
                        axisOffset + (_boundrySettings.OpenBorderCells[cntr].Y * cellSize),
                        axisOffset + (_boundrySettings.OpenBorderCells[cntr].Z * cellSize));       // the index goes from 0 to x,y,z.  But the offset needs to go from -halfSize to halfSize

                    positions[cntr] = this.PositionWorld + transform.Transform(offset);
                }

                Tuple<Vector3D, double>[] flows = _boundrySettings.OpenBoundryParent.GetForce(positions, cellSizeRequest);

                for (int cntr = 0; cntr < _boundrySettings.OpenBorderCells.Length; cntr++)
                {
                    // Convert back into model coords
                    Vector3D flow = flows[cntr].Item1 - this.VelocityWorld;
                    flow = transformInv.Transform(flow);

                    // Store it
                    //TODO: may want to do something with the viscocity
                    _boundrySettings.OpenBorderVelocityX[_boundrySettings.OpenBorderCells[cntr].Offset1D] = flow.X;
                    _boundrySettings.OpenBorderVelocityY[_boundrySettings.OpenBorderCells[cntr].Offset1D] = flow.Y;
                    _boundrySettings.OpenBorderVelocityZ[_boundrySettings.OpenBorderCells[cntr].Offset1D] = flow.Z;
                }

                #endregion
            }
        }

        private Mapping_3D_1D[] GetBorderCellIndices()
        {
            List<Mapping_3D_1D> retVal = new List<Mapping_3D_1D>();

            // X wall
            for (int z = 1; z < _size - 1; z++)
            {
                for (int y = 1; y < _size - 1; y++)
                {
                    retVal.Add(new Mapping_3D_1D(0, y, z, IX(0, y, z)));
                    retVal.Add(new Mapping_3D_1D(_size - 1, y, z, IX(_size - 1, y, z)));
                }
            }

            // Y wall
            for (int z = 1; z < _size - 1; z++)
            {
                for (int x = 1; x < _size - 1; x++)
                {
                    retVal.Add(new Mapping_3D_1D(x, 0, z, IX(x, 0, z)));
                    retVal.Add(new Mapping_3D_1D(x, _size - 1, z, IX(x, _size - 1, z)));
                }
            }

            // Z wall
            for (int y = 1; y < _size - 1; y++)
            {
                for (int x = 1; x < _size - 1; x++)
                {
                    retVal.Add(new Mapping_3D_1D(x, y, 0, IX(x, y, 0)));
                    retVal.Add(new Mapping_3D_1D(x, y, _size - 1, IX(x, y, _size - 1)));
                }
            }

            // Edges
            for (int x = 1; x < _size - 1; x++)
            {
                retVal.Add(new Mapping_3D_1D(x, 0, 0, IX(x, 0, 0)));
                retVal.Add(new Mapping_3D_1D(x, _size - 1, 0, IX(x, _size - 1, 0)));
                retVal.Add(new Mapping_3D_1D(x, 0, _size - 1, IX(x, 0, _size - 1)));
                retVal.Add(new Mapping_3D_1D(x, _size - 1, _size - 1, IX(x, _size - 1, _size - 1)));
            }

            for (int y = 1; y < _size - 1; y++)
            {
                retVal.Add(new Mapping_3D_1D(0, y, 0, IX(0, y, 0)));
                retVal.Add(new Mapping_3D_1D(_size - 1, y, 0, IX(_size - 1, y, 0)));
                retVal.Add(new Mapping_3D_1D(0, y, _size - 1, IX(0, y, _size - 1)));
                retVal.Add(new Mapping_3D_1D(_size - 1, y, _size - 1, IX(_size - 1, y, _size - 1)));
            }

            for (int z = 0; z < _size - 1; z++)
            {
                retVal.Add(new Mapping_3D_1D(0, 0, z, IX(0, 0, z)));
                retVal.Add(new Mapping_3D_1D(_size - 1, 0, z, IX(_size - 1, 0, z)));
                retVal.Add(new Mapping_3D_1D(0, _size - 1, z, IX(0, _size - 1, z)));
                retVal.Add(new Mapping_3D_1D(_size - 1, _size - 1, z, IX(_size - 1, _size - 1, z)));
            }

            // Corners
            retVal.Add(new Mapping_3D_1D(0, 0, 0, IX(0, 0, 0)));
            retVal.Add(new Mapping_3D_1D(0, _size - 1, 0, IX(0, _size - 1, 0)));
            retVal.Add(new Mapping_3D_1D(0, 0, _size - 1, IX(0, 0, _size - 1)));
            retVal.Add(new Mapping_3D_1D(0, _size - 1, _size - 1, IX(0, _size - 1, _size - 1)));
            retVal.Add(new Mapping_3D_1D(_size - 1, 0, 0, IX(_size - 1, 0, 0)));
            retVal.Add(new Mapping_3D_1D(_size - 1, _size - 1, 0, IX(_size - 1, _size - 1, 0)));
            retVal.Add(new Mapping_3D_1D(_size - 1, 0, _size - 1, IX(_size - 1, 0, _size - 1)));
            retVal.Add(new Mapping_3D_1D(_size - 1, _size - 1, _size - 1, IX(_size - 1, _size - 1, _size - 1)));

            // Exit Function
            return retVal.ToArray();
        }

        private static void Set3DIndex(ref int x, ref int y, ref int z, int index, Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    x = index;
                    break;

                case Axis.Y:
                    y = index;
                    break;

                case Axis.Z:
                    z = index;
                    break;

                default:
                    throw new ApplicationException("Unknown Axis: " + axis.ToString());
            }
        }

        #endregion
    }

    #region Enum: FluidFieldBoundryType3D

    //TODO: May want combinations of these: Bottom is solid, Top is open, Left/Right and Front/Back are wrap around
    public enum FluidFieldBoundryType3D
    {
        /// <summary>
        /// Fluid reflects off the sides of the cube
        /// </summary>
        Closed,
        /// <summary>
        /// Fluid flows out of/into the cube
        /// </summary>
        /// <remarks>
        /// The border cells are a copy of their neighbors.  Inflow is reduced to avoid runaway feedback
        /// </remarks>
        Open,
        /// <summary>
        /// The border cells are copied from an outside field (through an interface)
        /// </summary>
        Open_Slaved,
        /// <summary>
        /// Sort of a combination of Open and Open_Slaved.  The border cells are an average of neighbor cells, and
        /// the outside field
        /// </summary>
        /// <remarks>
        /// This is useful when you have low resolution fields that occupy a large space, and small high resolution fields.
        /// 
        /// Imagine an airplane flying through the air:
        ///     The air can be modeled with a low resolution field that is the size of the map.
        ///     A high resolution field will be just larger than the airplane, and will travel with the plane.
        ///     A medium resolution field can surround the plane, and will model the wake.  (useful if there are a flock of airplanes)
        ///     
        /// Each field will transfer velocity at their borders
        /// TODO: As described, the medium field wouldn't work right.  Inner cells would need some transfer as well (maybe have something similar to blocked cells that take values from another field)
        /// </remarks>
        Open_Shared,
        WrapAround,

        // This isn't really needed.  It can be accomplished by setting blocked cells
        //ClosedCircle     // sphere in 3D
    }

    #endregion

    #region Class: FluidFieldUniform

    /// <summary>
    /// This is the simplest possible implementation.  The viscocity and flow are the same for all positions
    /// </summary>
    public class FluidFieldUniform : IFluidField
    {
        public double Viscosity = 0d;
        public Vector3D Flow = new Vector3D(0, 0, 0);

        //public void GetForce(out Vector3D flow, out double viscosity, Point3D point, double radius)
        //{
        //    viscosity = this.Viscosity;
        //    flow = this.Flow;
        //}
        public Tuple<Vector3D, double>[] GetForce(Point3D[] points, double radius)
        {
            Tuple<Vector3D, double>[] retVal = new Tuple<Vector3D, double>[points.Length];

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                retVal[cntr] = Tuple.Create(this.Flow, this.Viscosity);
            }

            return retVal;
        }
    }

    #endregion
    #region Class: FluidFieldField

    /// <summary>
    /// This is implementation is tied to a single field
    /// TODO: Come up with a better name
    /// TODO: When FluidField3D implements viscosity, there won't be any need for this class
    /// </summary>
    public class FluidFieldField : IFluidField
    {
        public FluidFieldField(FluidField3D field)
        {
            _field = field;
        }

        private readonly FluidField3D _field;

        public double Viscosity = 0d;

        //public void GetForce(out Vector3D flow, out double viscosity, Point3D point, double radius)
        //{
        //    viscosity = this.Viscosity;
        //    flow = _field.GetFlowAtLocation(point, radius);
        //}

        public Tuple<Vector3D, double>[] GetForce(Point3D[] points, double radius)
        {
            Vector3D[] forces = _field.GetFlowAtLocations(points, radius);

            Tuple<Vector3D, double>[] retVal = new Tuple<Vector3D, double>[forces.Length];

            for (int cntr = 0; cntr < forces.Length; cntr++)
            {
                retVal[cntr] = Tuple.Create(forces[cntr], this.Viscosity);
            }

            return retVal;
        }
    }

    #endregion
    #region Interface: IFluidField

    /// <remarks>
    /// There could be a few different ways to feed this:
    /// 
    /// There could be somewhat static gas clouds out in space, denser static atmospheres around larger planets, static jet streams, etc
    /// 
    /// This could also be the result of a fluid dynamics simulation where movement within the fluid creates high and low pressure spots,
    /// and the fluid keeps trying to equalize
    /// </remarks>
    public interface IFluidField
    {
        //void GetForce(out Vector3D flow, out double viscosity, Point3D point, double radius);

        /// <summary>
        /// This method returns the fluid flow at the locations requested (the average within the sphere)
        /// </summary>
        Tuple<Vector3D, double>[] GetForce(Point3D[] points, double radius);
    }

    #endregion
}
