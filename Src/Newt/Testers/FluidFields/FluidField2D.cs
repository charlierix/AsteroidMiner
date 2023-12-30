using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Game.HelperClassesCore;

namespace Game.Newt.Testers.FluidFields
{
    //TODO: Expose a property that will slowly fade _layers to zero (call it disipate)
    //TODO: Allow this.Update to be called from a separate thread (the problem is calls to SetInk, AddVel, etc need to get cached between updates)

    /// <remarks>
    /// Got this here:
    /// http://icosahedral.net/java/fluidsim.html
    /// 
    /// 
    /// * Fluidfield.java
    /// * 
    /// * Internal respresentation of the fluid, handles all the calculations, using a iterative linear solver.
    /// * 
    /// * Uses the method described by Jos Stam in http://www.dgp.toronto.edu/people/stam/reality/Research/pdf/GDC03.pdf,
    /// * with the addition of vorticity confinement and an overrelaxation factor on the linear solver to improve the
    /// * rate of convergence. Credits to Jos Stam for the fluid simulation algorithm.
    /// * 
    /// * @author David Wu
    /// * @version 1.0, November 5, 2007
    /// 
    /// </remarks>
    public class FluidField2D
    {
        #region enum: SetBoundsType

        private enum SetBoundsType
        {
            // These set the edges to zero, so should only be used for pressure/velocity arrays
            VelocityX,
            VelocityY,

            // This sets the edges to the neighbor
            Both_Layers,
            Both_Other
        }

        #endregion
        #region struct: IndexLERP

        private struct IndexLERP
        {
            public IndexLERP(int k, Tuple<int, double>[] neighbors)
            {
                this.K = k;

                this.Neighbors = neighbors;
            }

            public readonly int K;

            // If this is empty, just store zero
            public readonly Tuple<int, double>[] Neighbors;
        }

        #endregion

        #region Declaration Section

        private double[] _xVel;
        private double[] _yVel;
        private double[] _xVelTemp;
        private double[] _yVelTemp;

        /// <summary>
        /// I'm guessing layers can represent anything?
        /// The applet is using this to represent color
        /// NOTE: Layers don't affect the fluid, they are just along for the ride
        /// </summary>
        /// <remarks>
        /// First Dimension:
        ///     the applet has this sized to 3 (0=R, 1=G, 2=B)
        ///     the applet uses values between 0 and 1
        /// 
        /// Second Dimension:
        ///     represents a 2D array, but is flattened to 1D - set getK() for getting converting from 2 coords to the 1D index
        /// </remarks>
        private double[][] _layers;     //NOTE: These layers don't affect anything, they are just spread around through diffusion and the velocity array
        private double[][] _layersP;        // Is this density?  density particles?  (either way, I don't think it should be exposed publicly)

        // These are used by vorticityConfinement
        private double[] _curl;
        private double[] _curlAbs;

        // These are intermediate arrays to hold changes between calls to update
        private double[][] _layerSrc;       //NOTE: This doesn't hold an absolute to set layer to, it holds the amount to add to layer (see setInk and addStuff)
        private double[] _xVelSrc;
        private double[] _yVelSrc;

        private bool _isBlockCacheDirty = true;
        private bool _hasBlockedCells = false;

        // These lerp store which neighbors to average, and how much of each neighbor to contribute
        private IndexLERP[] _blocked_VelocityX = null;
        private IndexLERP[] _blocked_VelocityY = null;
        private IndexLERP[] _blocked_Other = null;

        // This one is full size, and tells whether the cell is completely blocked
        private bool[] _blocked_Total = null;

        #endregion

        #region Constructor

        public FluidField2D(int x, int y, int numLayers)
        {
            _xSize = x;
            _ySize = y;
            _kSize = x * y;
            _numLayers = numLayers;

            _layers = Enumerable.Range(0, numLayers).
                Select(o => new double[x * y]).
                ToArray();
            _xVel = new double[x * y];
            _yVel = new double[x * y];

            _xVelTemp = _xVel.ToArray();
            _yVelTemp = _yVel.ToArray();
            _layersP = Enumerable.Range(0, numLayers).
                Select(o => new double[x * y]).
                ToArray();

            _curl = new double[_kSize];
            _curlAbs = new double[_kSize];

            _layerSrc = Enumerable.Range(0, _numLayers).
                Select(o => new double[_kSize]).
                ToArray();

            _xVelSrc = new double[_kSize];
            _yVelSrc = new double[_kSize];

            _blocked = new bool[_kSize];
            _blocked_Total = new bool[_kSize];
        }

        #endregion

        #region Public Properties

        private int _xSize;
        public int XSize => _xSize;

        private int _ySize;
        public int YSize => _ySize;

        private int _kSize;
        /// <summary>
        /// This is the 1D size of the arrays.  It is just _xSize * _ySize.
        /// See GetK()
        /// </summary>
        public int KSize => _kSize;

        private int _numLayers;
        public int NumLayers => _numLayers;

        private double _wallReflectivity = .95d;
        public double WallReflectivity
        {
            get
            {
                return _wallReflectivity;
            }
            set
            {
                _wallReflectivity = value;

                _isBlockCacheDirty = true;
            }
        }

        // The properties below can be adjusted while this field is running (between calls to Update).  They tweak how the field acts
        private double _diffusion = .01;
        /// <summary>
        /// Rate at which the ink/velocity diffuses and spreads out in the fluid.
        /// 0 to 1 (though .1 is pretty extreme)
        /// </summary>
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

        private double _viscosity = .01;
        /// <summary>
        /// Rate at which fluid motions damp out over time.
        /// 0 to 1 (though .1 is pretty extreme)
        /// </summary>
        public double Viscosity
        {
            get
            {
                return _viscosity;
            }
            set
            {
                _viscosity = value;
            }
        }

        private double _vorticity = 10;
        /// <summary>
        /// Controls the fine-scale flows and vortices of the fluid.
        /// 0 to 100
        /// </summary>
        /// <remarks>
        /// To really see the effect of this property, set it to zero, then start swirling colors into the fluid, then set this to maximum
        /// and notice what happens
        /// </remarks>
        public double Vorticity
        {
            get
            {
                return _vorticity;
            }
            set
            {
                _vorticity = value;
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

        private int _iterations = 8;
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

        private bool _useCheapDiffusion;
        public bool UseCheapDiffusion
        {
            get
            {
                return _useCheapDiffusion;
            }
            set
            {
                _useCheapDiffusion = value;
            }
        }

        private FluidFieldBoundryType2D _boundryType = FluidFieldBoundryType2D.Closed;
        public FluidFieldBoundryType2D BoundryType
        {
            get
            {
                return _boundryType;
            }
            set
            {
                _boundryType = value;
            }
        }

        //----------------------- Exposed for reading only -----------------------
        public double[] XVel => _xVel;
        public double[] YVel => _yVel;

        public double[][] Layers => _layers;

        //TODO: Be able to request the forces acting on a cell
        private bool[] _blocked;
        /// <summary>
        /// These are individual cells that are blocked (they represent internal walls)
        /// </summary>
        public bool[] Blocked => _blocked;

        //---------------------------------------------------------------------------------

        #endregion

        #region Public Methods

        public void Update()
        {
            IndexBlockedCells();

            AddStuff();

            VelocityStep();
            DensityStep();
        }

        /// <summary>
        /// This returns the index for the x,y passed in (converts a 2D coord into a 1D index)
        /// </summary>
        public int GetK(int x, int y)
        {
            return (y * _xSize) + x;
        }
        public static int GetK(int xSize, int x, int y)
        {
            return (y * xSize) + x;
        }

        /// <summary>
        /// There are 3 layers that represent RGB (0=R, 1=G, 2=B)
        /// </summary>
        public double[] GetLayer(int i)
        { return _layers[i]; }

        /// <summary>
        /// This sets one part of a color
        /// </summary>
        /// <param name="layer">0=R, 1=G, 2=B</param>
        /// <param name="k">result of call to getK</param>
        /// <param name="value">0=black, 1=white</param>
        public void SetInk(int layer, int k, double value)
        {
            _layerSrc[layer][k] = value - _layers[layer][k];
        }

        /// <summary>
        /// This applies a velocity to the cell at k - see getK()
        /// </summary>
        public void AddVel(int k, double xValue, double yValue)
        {
            _xVelSrc[k] += xValue;
            _yVelSrc[k] += yValue;
        }
        public void SetVel(int k, double xValue, double yValue)
        {
            _xVelSrc[k] = xValue - _xVel[k];
            _yVelSrc[k] = yValue - _yVel[k];
        }

        public Vector GetVelocity(int k)
        {
            return new Vector(_xVel[k], _yVel[k]);
        }

        public void SetBlockedCell(int k, bool isBlocked)
        {
            _blocked[k] = isBlocked;
            _isBlockCacheDirty = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Add color/velocity that have been applied since the last call to update
        /// </summary>
        private void AddStuff()
        {
            // Add color
            for (int i = 0; i < _layers.Length; i++)
            {
                for (int k = 0; k < _layers[i].Length; k++)
                {
                    _layers[i][k] += _layerSrc[i][k];
                    _layerSrc[i][k] = 0;
                }
            }

            // Add velocity
            for (int k = 0; k < _kSize; k++)
            {
                if (_blocked_Total[k])
                {
                    _xVel[k] = 0;
                    _yVel[k] = 0;
                }
                else
                {
                    _xVel[k] += _xVelSrc[k];
                    _yVel[k] += _yVelSrc[k];
                }

                _xVelSrc[k] = 0;
                _yVelSrc[k] = 0;
            }
        }

        private void VelocityStep()
        {
            //// Vorticity
            //if (_vorticity > 0)
            //{
            //    VorticityConfinement(_xVelTemp, _yVelTemp);
            //    AddSource(_xVel, _xVelTemp);
            //    AddSource(_yVel, _yVelTemp);
            //}

            // Diffusion/Viscosity
            if (_diffusion > 0 || _viscosity > 0)
            {
                if (_diffusion > 0)
                {
                    Diffuse(_xVelTemp, _xVel, SetBoundsType.VelocityX, _diffusion, _timestep);
                    Diffuse(_yVelTemp, _yVel, SetBoundsType.VelocityY, _diffusion, _timestep);
                }
                else
                {
                    Array.Copy(_xVel, _xVelTemp, _kSize);       //diffusion acts like a fuzzy array.copy.  So since it didn't run, copy into temp
                    Array.Copy(_yVel, _yVelTemp, _kSize);
                }

                if (_viscosity > 0)
                {
                    Slow(_xVelTemp, _viscosity, _timestep);
                    Slow(_yVelTemp, _viscosity, _timestep);
                }

                Project(_xVelTemp, _yVelTemp, _xVel, _yVel);
            }
            else
            {
                Array.Copy(_xVel, _xVelTemp, _kSize);
                Array.Copy(_yVel, _yVelTemp, _kSize);
            }

            Advect(_xVel, _xVelTemp, _xVelTemp, _yVelTemp, SetBoundsType.VelocityX, _timestep);
            Advect(_yVel, _yVelTemp, _xVelTemp, _yVelTemp, SetBoundsType.VelocityY, _timestep);
            Project(_xVel, _yVel, _xVelTemp, _yVelTemp);
        }

        private void DensityStep()
        {
            if (_timestep <= 0)
            {
                return;
            }

            double[] temp;
            for (int i = 0; i < _layers.Length; i++)
            {
                if (_diffusion > 0)
                {
                    temp = _layersP[i];
                    _layersP[i] = _layers[i];
                    _layers[i] = temp;

                    Diffuse(_layers[i], _layersP[i], SetBoundsType.Both_Layers, _diffusion, _timestep);
                }

                temp = _layersP[i];
                _layersP[i] = _layers[i];
                _layers[i] = temp;

                Advect(_layers[i], _layersP[i], _xVel, _yVel, SetBoundsType.Both_Layers, _timestep);
            }
        }

        private static void Slow(double[] cells, double viscocity, double timestep)
        {
            double factor = 1d / (viscocity * timestep / 20d + 1d);

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] *= factor;
            }
        }

        private void Diffuse(double[] dest, double[] src, SetBoundsType boundType, double diff, double dt)
        {
            double a = dt * diff;
            if (_useCheapDiffusion)
            {
                StoopidSolve(dest, src, boundType, a);
            }
            else
            {
                LinearSolve(dest, src, boundType, a, 1 + 4 * a);
            }
        }

        private void StoopidSolve(double[] dest, double[] src, SetBoundsType boundType, double diffuseRate)
        {
            for (int y = 1; y < _ySize - 1; y++)
            {
                int yIndex = y * _xSize;
                for (int x = 1; x < _xSize - 1; x++)
                {
                    int k = x + yIndex;

                    if (_blocked[k])
                    {
                        continue;
                    }

                    dest[k] = (diffuseRate * (src[k - 1] + src[k + 1] + src[k - _xSize] + src[k + _xSize]) + src[k]) / (1 + 4 * diffuseRate);
                }
            }

            SetBounds(boundType, dest);
        }

        /// <summary>
        /// Improved gauss-siedel by adding relaxation factor. Overrelaxation at 1.5 seems strong, and higher values
        /// create small-scale instablity (mixing) but seem to produce reasonable incompressiblity even faster.
        /// 4-10 iterations is good for real-time, and not noticably inaccurate. For real accuracy, upwards of 20 is good.
        /// </summary>
        private void LinearSolve(double[] dest, double[] src, SetBoundsType boundType, double diffuseRate, double c)
        {
            double wMax = 1.9;
            double wMin = 1.5;
            for (int i = 0; i < _iterations; i++)
            {
                double w = Math.Max((wMin - wMax) * i / 60.0 + wMax, wMin);
                for (int y = 1; y < _ySize - 1; y++)
                {
                    int yIndex = y * _xSize;
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        int k = x + yIndex;

                        if (_blocked[k])
                        {
                            continue;
                        }

                        dest[k] = dest[k] + w * ((diffuseRate * (dest[k - 1] + dest[k + 1] + dest[k - _xSize] + dest[k + _xSize]) + src[k]) / c - dest[k]);
                    }
                }

                SetBounds(boundType, dest);
            }
        }

        private void Advect(double[] dest, double[] src, double[] xVelocity, double[] yVelocity, SetBoundsType boundType, double dt)
        {
            for (int y = 1; y < _ySize - 1; y++)
            {
                int yIndex = y * _xSize;
                for (int x = 1; x < _xSize - 1; x++)
                {
                    int k = x + yIndex;

                    if (_blocked[k])
                    {
                        continue;
                    }

                    //Reverse velocity, since we are interpolating backwards
                    //xSrc and ySrc is the position of the source density.
                    double xSrc = x - dt * xVelocity[k];
                    double ySrc = y - dt * yVelocity[k];

                    if (xSrc < 0.5) { xSrc = 0.5; }
                    if (xSrc > _xSize - 1.5) { xSrc = _xSize - 1.5; }
                    int xi0 = (int)xSrc;
                    int xi1 = xi0 + 1;

                    if (ySrc < 0.5) { ySrc = 0.5; }
                    if (ySrc > _ySize - 1.5) { ySrc = _ySize - 1.5; }
                    int yi0 = (int)ySrc;
                    int yi1 = yi0 + 1;

                    //Linear interpolation factors. Ex: 0.6 and 0.4
                    double xProp1 = xSrc - xi0;
                    double xProp0 = 1d - xProp1;
                    double yProp1 = ySrc - yi0;
                    double yProp0 = 1d - yProp1;

                    dest[k] =
                        xProp0 * (yProp0 * src[GetK(xi0, yi0)] + yProp1 * src[GetK(xi0, yi1)]) +
                        xProp1 * (yProp0 * src[GetK(xi1, yi0)] + yProp1 * src[GetK(xi1, yi1)]);
                }
            }

            SetBounds(boundType, dest);
        }

        private void Project(double[] xV, double[] yV, double[] p, double[] div)
        {
            double h = 0.1;///(xSize-2);
            for (int y = 1; y < _ySize - 1; y++)
            {
                int yIndex = y * _xSize;
                for (int x = 1; x < _xSize - 1; x++)
                {
                    int k = x + yIndex;

                    if (_blocked[k])
                    {
                        continue;
                    }

                    //Negative divergence
                    div[k] = -0.5 * h * (xV[k + 1] - xV[k - 1] + yV[k + _xSize] - yV[k - _xSize]);
                    //Pressure field
                    p[k] = 0;
                }
            }
            SetBounds(SetBoundsType.Both_Other, div);
            SetBounds(SetBoundsType.Both_Other, p);

            LinearSolve(p, div, SetBoundsType.Both_Other, 1, 4);

            for (int y = 1; y < _ySize - 1; y++)
            {
                int yIndex = y * _xSize;
                for (int x = 1; x < _xSize - 1; x++)
                {
                    int k = x + yIndex;

                    if (_blocked[k])
                    {
                        continue;
                    }

                    xV[k] -= 0.5 * (p[k + 1] - p[k - 1]) / h;
                    yV[k] -= 0.5 * (p[k + _xSize] - p[k - _xSize]) / h;
                }
            }
            SetBounds(SetBoundsType.VelocityX, xV);
            SetBounds(SetBoundsType.VelocityY, yV);
        }

        private void SetBounds(SetBoundsType boundType, double[] cells)
        {
            // Outer Edges
            switch (_boundryType)
            {
                case FluidFieldBoundryType2D.Closed:
                    SetBounds_ClosedBox(boundType, cells);
                    break;

                case FluidFieldBoundryType2D.Open:
                    SetBounds_OpenBox(boundType, cells);
                    break;

                case FluidFieldBoundryType2D.WrapAround:
                    SetBounds_WrapAroundBox(boundType, cells);
                    break;

                default:
                    throw new ApplicationException("Unknown FluidFieldBoundryType2D: " + _boundryType.ToString());
            }

            // Blocked Cells
            if (_hasBlockedCells)
            {
                // This has similar logic to closed box, but applied facing outward around the blocked cells
                //SetBounds_BlockedCells(boundType, cells);
                SetBounds_BlockedCells(boundType, cells);
            }
        }

        private void SetBounds_ClosedBox(SetBoundsType boundType, double[] cells)
        {
            switch (boundType)
            {
                case SetBoundsType.VelocityX:
                    #region VelocityX

                    // For x velocity, reflect off the y walls, and slide along the x walls

                    // Reflect the left and right edges
                    for (int y = 0; y < _ySize; y++)
                    {
                        cells[GetK(0, y)] = cells[GetK(1, y)] * -_wallReflectivity;
                        cells[GetK(_xSize - 1, y)] = cells[GetK(_xSize - 2, y)] * -_wallReflectivity;
                    }

                    // Set top and bottom edges to their neighbors
                    for (int x = 1; x < _xSize - 1; x++)        //NOTE: the above loop is 0 to size, but this is 1 to size-1 so that the corners don't get double set
                    {
                        cells[GetK(x, 0)] = cells[GetK(x, 1)];
                        cells[GetK(x, _ySize - 1)] = cells[GetK(x, _ySize - 2)];
                    }

                    #endregion
                    break;

                case SetBoundsType.VelocityY:
                    #region VelocityY

                    // For y velocity, reflect off the x walls, and slide along the y walls

                    // Reflect the top and bottom edges
                    for (int x = 0; x < _xSize; x++)
                    {
                        cells[GetK(x, 0)] = cells[GetK(x, 1)] * -_wallReflectivity;
                        cells[GetK(x, _ySize - 1)] = cells[GetK(x, _ySize - 2)] * -_wallReflectivity;
                    }

                    // Set left and right edges to their neighbors
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        cells[GetK(0, y)] = cells[GetK(1, y)];
                        cells[GetK(_xSize - 1, y)] = cells[GetK(_xSize - 2, y)];
                    }

                    #endregion
                    break;

                case SetBoundsType.Both_Layers:
                case SetBoundsType.Both_Other:
                    #region Both

                    // Set top and bottom edges to their neighbors
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        cells[GetK(x, 0)] = cells[GetK(x, 1)];
                        cells[GetK(x, _ySize - 1)] = cells[GetK(x, _ySize - 2)];
                    }

                    // Set left and right edges to their neighbors
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        cells[GetK(0, y)] = cells[GetK(1, y)];
                        cells[GetK(_xSize - 1, y)] = cells[GetK(_xSize - 2, y)];
                    }

                    // Set the corners to the average of their two neighbors
                    cells[GetK(0, 0)] = 0.5 * (cells[GetK(0, 1)] + cells[GetK(1, 0)]);
                    cells[GetK(0, _ySize - 1)] = 0.5 * (cells[GetK(1, _ySize - 1)] + cells[GetK(0, _ySize - 2)]);
                    cells[GetK(_xSize - 1, 0)] = 0.5 * (cells[GetK(_xSize - 1, 1)] + cells[GetK(_xSize - 2, 0)]);
                    cells[GetK(_xSize - 1, _ySize - 1)] = 0.5 * (cells[GetK(_xSize - 1, _ySize - 2)] + cells[GetK(_xSize - 2, _ySize - 1)]);

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown SetBoundsType: " + boundType.ToString());
            }
        }
        private void SetBounds_OpenBox(SetBoundsType boundType, double[] cells)
        {
            switch (boundType)
            {
                case SetBoundsType.VelocityX:
                    #region VelocityX

                    // Set left and right edges to their neighbors
                    //NOTE: Allow outflow, but not full inflow (the inflow becomes self reinforcing, and the whole field becomes a wall of wind)
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        cells[GetK(0, y)] = SetBounds_OpenBoxSprtCap(cells[GetK(1, y)], false);
                        cells[GetK(_xSize - 1, y)] = SetBounds_OpenBoxSprtCap(cells[GetK(_xSize - 2, y)], true);
                    }

                    // Set top and bottom edges to their neighbors
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        cells[GetK(x, 0)] = cells[GetK(x, 1)];
                        cells[GetK(x, _ySize - 1)] = cells[GetK(x, _ySize - 2)];
                    }

                    #endregion
                    break;

                case SetBoundsType.VelocityY:
                    #region VelocityY

                    // Set top and bottom edges to their neighbors
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        cells[GetK(x, 0)] = SetBounds_OpenBoxSprtCap(cells[GetK(x, 1)], false);
                        cells[GetK(x, _ySize - 1)] = SetBounds_OpenBoxSprtCap(cells[GetK(x, _ySize - 2)], true);
                    }

                    // Set left and right edges to their neighbors
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        cells[GetK(0, y)] = cells[GetK(1, y)];
                        cells[GetK(_xSize - 1, y)] = cells[GetK(_xSize - 2, y)];
                    }

                    #endregion
                    break;

                case SetBoundsType.Both_Layers:
                case SetBoundsType.Both_Other:
                    #region Both

                    // Set top and bottom edges to their neighbors
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        cells[GetK(x, 0)] = cells[GetK(x, 1)];
                        cells[GetK(x, _ySize - 1)] = cells[GetK(x, _ySize - 2)];
                    }

                    // Set left and right edges to their neighbors
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        cells[GetK(0, y)] = cells[GetK(1, y)];
                        cells[GetK(_xSize - 1, y)] = cells[GetK(_xSize - 2, y)];
                    }

                    // Set the corners to the average of their two neighbors
                    cells[GetK(0, 0)] = 0.5 * (cells[GetK(0, 1)] + cells[GetK(1, 0)]);
                    cells[GetK(0, _ySize - 1)] = 0.5 * (cells[GetK(1, _ySize - 1)] + cells[GetK(0, _ySize - 2)]);
                    cells[GetK(_xSize - 1, 0)] = 0.5 * (cells[GetK(_xSize - 1, 1)] + cells[GetK(_xSize - 2, 0)]);
                    cells[GetK(_xSize - 1, _ySize - 1)] = 0.5 * (cells[GetK(_xSize - 1, _ySize - 2)] + cells[GetK(_xSize - 2, _ySize - 1)]);

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown SetBoundsType: " + boundType.ToString());
            }
        }
        private static double SetBounds_OpenBoxSprtCap(double newValue, bool allowPositive)
        {
            bool isPositive = newValue > 0;

            if (isPositive == allowPositive)
            {
                return newValue;
            }
            else
            {
                //return 0;     // too restrictive
                return newValue * .75d;
            }
        }
        private void SetBounds_WrapAroundBox(SetBoundsType boundType, double[] cells)
        {
            switch (boundType)
            {
                case SetBoundsType.VelocityX:
                    #region VelocityX

                    // Set left and right edges to the opposite edge neighbors
                    for (int y = 0; y < _ySize; y++)
                    {
                        cells[GetK(0, y)] = cells[GetK(_xSize - 2, y)];
                        cells[GetK(_xSize - 1, y)] = cells[GetK(1, y)];
                    }

                    // Set top and bottom edges to the average of their above/below neighbors
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        double average = .5d * (cells[GetK(x, 1)] + cells[GetK(x, _ySize - 2)]);

                        cells[GetK(x, 0)] = average;
                        cells[GetK(x, _ySize - 1)] = average;
                    }

                    #endregion
                    break;

                case SetBoundsType.VelocityY:
                    #region VelocityY

                    // Set top and bottom edges to the opposite edge neighbors
                    for (int x = 0; x < _xSize; x++)
                    {
                        cells[GetK(x, 0)] = cells[GetK(x, _ySize - 2)];
                        cells[GetK(x, _ySize - 1)] = cells[GetK(x, 1)];
                    }

                    // Set left and right edges to the average of their left/right neighbors
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        double average = .5d * (cells[GetK(1, y)] + cells[GetK(_xSize - 2, y)]);

                        cells[GetK(0, y)] = average;
                        cells[GetK(_xSize - 1, y)] = average;
                    }

                    #endregion
                    break;

                case SetBoundsType.Both_Layers:
                    #region Both_Layers (wrap layers)

                    // Set top and bottom edges to their neighbors
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        cells[GetK(x, 0)] = cells[GetK(x, _ySize - 2)];
                        cells[GetK(x, _ySize - 1)] = cells[GetK(x, 1)];
                    }

                    // Set left and right edges to their neighbors
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        cells[GetK(0, y)] = cells[GetK(_xSize - 2, y)];
                        cells[GetK(_xSize - 1, y)] = cells[GetK(1, y)];
                    }

                    //TODO: Wrap this around as well (or just consider more cells when averaging)
                    // Set the corners to the average of their two neighbors
                    cells[GetK(0, 0)] = 0.5 * (cells[GetK(0, 1)] + cells[GetK(1, 0)]);
                    cells[GetK(0, _ySize - 1)] = 0.5 * (cells[GetK(1, _ySize - 1)] + cells[GetK(0, _ySize - 2)]);
                    cells[GetK(_xSize - 1, 0)] = 0.5 * (cells[GetK(_xSize - 1, 1)] + cells[GetK(_xSize - 2, 0)]);
                    cells[GetK(_xSize - 1, _ySize - 1)] = 0.5 * (cells[GetK(_xSize - 1, _ySize - 2)] + cells[GetK(_xSize - 2, _ySize - 1)]);

                    #endregion
                    break;

                case SetBoundsType.Both_Other:
                    #region Both_Other (standard)

                    // Set top and bottom edges to their neighbors
                    for (int x = 1; x < _xSize - 1; x++)
                    {
                        double average = .5d * (cells[GetK(x, 1)] + cells[GetK(x, _ySize - 2)]);

                        cells[GetK(x, 0)] = average;
                        cells[GetK(x, _ySize - 1)] = average;
                    }

                    // Set left and right edges to their neighbors
                    for (int y = 1; y < _ySize - 1; y++)
                    {
                        double average = .5d * (cells[GetK(1, y)] + cells[GetK(_xSize - 2, y)]);

                        cells[GetK(0, y)] = average;
                        cells[GetK(_xSize - 1, y)] = average;
                    }

                    // Set the corners to the average of their two neighbors
                    cells[GetK(0, 0)] = 0.5 * (cells[GetK(0, 1)] + cells[GetK(1, 0)]);
                    cells[GetK(0, _ySize - 1)] = 0.5 * (cells[GetK(1, _ySize - 1)] + cells[GetK(0, _ySize - 2)]);
                    cells[GetK(_xSize - 1, 0)] = 0.5 * (cells[GetK(_xSize - 1, 1)] + cells[GetK(_xSize - 2, 0)]);
                    cells[GetK(_xSize - 1, _ySize - 1)] = 0.5 * (cells[GetK(_xSize - 1, _ySize - 2)] + cells[GetK(_xSize - 2, _ySize - 1)]);

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown SetBoundsType: " + boundType.ToString());
            }
        }

        private void SetBounds_BlockedCells(SetBoundsType boundType, double[] cells)
        {
            //NOTE: It is safe to ignore blocked cells that are interior (surrounded by other blocked cells)

            IndexLERP[] blocked = null;

            switch (boundType)
            {
                case SetBoundsType.VelocityX:
                    blocked = _blocked_VelocityX;
                    break;

                case SetBoundsType.VelocityY:
                    blocked = _blocked_VelocityY;
                    break;

                case SetBoundsType.Both_Layers:
                case SetBoundsType.Both_Other:
                    blocked = _blocked_Other;
                    break;

                default:
                    throw new ApplicationException("Unknown SetBoundsType: " + boundType.ToString());
            }

            foreach (IndexLERP index in blocked)
            {
                // Add up the neighbors (could be 1 to 8 neighbors)
                double newValue = 0d;

                if (index.Neighbors != null)
                {
                    foreach (var neighbor in index.Neighbors)
                    {
                        newValue += cells[neighbor.Item1] * neighbor.Item2;
                    }
                }

                // Store the average
                cells[index.K] = newValue;
            }
        }

        /// <summary>
        /// Adds the contents of x0 into x
        /// </summary>
        private void AddSource(double[] x, double[] x0)
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] += _timestep * x0[i];
            }
        }

        /// <summary>
        /// Blocked cells are just booleans.  But it is ineficient to scan for them multiple times each step.  So this is called at
        /// the beginning of the step to build indexed views of the blocked cells
        /// </summary>
        private void IndexBlockedCells()
        {
            const double VEL_INSIDECORNER = .25d;
            const double VEL_OUTSIDECORNER = .5d;
            const double VEL_ORTH = 1d;
            const double OTHER_ANY = 1d;

            if (!_isBlockCacheDirty)
            {
                return;
            }

            _hasBlockedCells = false;

            List<IndexLERP> velocityX = new List<IndexLERP>();
            List<IndexLERP> velocityY = new List<IndexLERP>();
            List<IndexLERP> other = new List<IndexLERP>();

            List<int> open = new List<int>();

            // For now, ignore blocked cells that are on the edges of the field

            for (int x = 1; x < _xSize - 1; x++)
            {
                for (int y = 1; y < _ySize - 1; y++)
                {
                    int k = GetK(x, y);

                    _blocked_Total[k] = false;

                    if (!_blocked[k])
                    {
                        continue;
                    }

                    #region Detect total block

                    bool foundOpen = false;

                    for (int x0 = x - 1; x0 <= x + 1; x0++)
                    {
                        for (int y0 = y - 1; y0 <= y + 1; y0++)
                        {
                            if (!_blocked[GetK(x0, y0)])     // not bothering to ignore the case where x0==x && y0==y.  It wouldn't change the outcome of this scan, and would just be an unnecessary if statement
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

                    if (!foundOpen)
                    {
                        // This cell is blocked by all sides.  The rest of this loop handles exposed blocked cells, so go to the next one
                        _blocked_Total[k] = true;
                        continue;
                    }

                    #endregion

                    #region Prep

                    // Get neighbor k's (compass notation)
                    int N = GetK(x, y - 1);
                    int NE = GetK(x + 1, y - 1);
                    int E = GetK(x + 1, y);
                    int SE = GetK(x + 1, y + 1);
                    int S = GetK(x, y + 1);
                    int SW = GetK(x - 1, y + 1);
                    int W = GetK(x - 1, y);
                    int NW = GetK(x - 1, y - 1);

                    // Rules for velocity (X and Y calculated separately)
                    //      If orth (both left and right neighbors are blocked): standard 0 or 1 mult logic
                    //
                    //      If outside corner (top and left are blocked, bottom and right are open): 50% x and y
                    //
                    //      If inside corner (everything blocked except one corner): 25% x and y

                    // Rules for coloring:
                    //      If blocked left and right: only do top/bottom orth
                    //
                    //      If is corner (open bottom, open right): do orths and corner
                    //
                    //      If only corner is free: just take the corner

                    open.Clear();
                    Tuple<int, double>[] lerp = null;

                    #endregion

                    if (_blocked[N] && _blocked[S] && (!_blocked[W] || !_blocked[E]))
                    {
                        #region Vertical Wall

                        // The Y velocity will run parallel to this wall, so it is just a copy of the neighbors
                        //NOTE: Only considering orth neighbors.  Only corner walls need to worry about diagonal neighbors (otherwise the contributions
                        //of the diagonal neighbors would blur things up too much)
                        if (!_blocked[W])
                        {
                            open.Add(W);
                        }

                        if (!_blocked[E])
                        {
                            open.Add(E);
                        }

                        // This wall segment is perpendicular to velX flow, so reflect back
                        double mult = -_wallReflectivity * VEL_ORTH / open.Count;        // they need to add up to VEL_ORTH * -reflectivity
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        velocityX.Add(new IndexLERP(k, lerp));

                        // This is parallel, so slide along it
                        mult = VEL_ORTH / open.Count;        // they need to add up to VEL_ORTH
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        velocityY.Add(new IndexLERP(k, lerp));

                        mult = OTHER_ANY / open.Count;        // they need to add up to OTHER_ANY
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        other.Add(new IndexLERP(k, lerp));

                        #endregion
                    }
                    else if (_blocked[W] && _blocked[E] && (!_blocked[N] || !_blocked[S]))
                    {
                        #region Horizontal Wall

                        // The X velocity will run parallel to this wall, so it is just a copy of the neighbors
                        if (!_blocked[N])
                        {
                            open.Add(N);
                        }

                        if (!_blocked[S])
                        {
                            open.Add(S);
                        }

                        // This wall segment is perpendicular to velY flow, always store zero
                        double mult = -_wallReflectivity * VEL_ORTH / open.Count;        // they need to add up to VEL_ORTH * -reflectivity
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        velocityY.Add(new IndexLERP(k, null));

                        // This is parallel, so slide along it
                        mult = VEL_ORTH / open.Count;        // they need to add up to VEL_ORTH
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        velocityX.Add(new IndexLERP(k, lerp));

                        mult = OTHER_ANY / open.Count;        // they need to add up to OTHER_ANY
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        other.Add(new IndexLERP(k, lerp));

                        #endregion
                    }
                    else if (_blocked[N] && _blocked[S] && _blocked[W] && _blocked[E])
                    {
                        #region Inside corner

                        if (!_blocked[NW])
                        {
                            open.Add(NW);
                        }

                        if (!_blocked[NE])
                        {
                            open.Add(NE);
                        }

                        if (!_blocked[SE])
                        {
                            open.Add(SE);
                        }

                        if (!_blocked[SW])
                        {
                            open.Add(SW);
                        }

                        double mult = -_wallReflectivity * VEL_INSIDECORNER / open.Count;        // they need to add up to VEL_INSIDECORNER * -reflectivity
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();

                        velocityX.Add(new IndexLERP(k, lerp));
                        velocityY.Add(new IndexLERP(k, lerp));

                        mult = OTHER_ANY / open.Count;        // they need to add up to OTHER_ANY
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        other.Add(new IndexLERP(k, lerp));

                        #endregion
                    }
                    else
                    {
                        #region Outside corner

                        // Orth
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

                        // Corner
                        //NOTE: Not allowing corner neighbors that are in front of orth walls (because the orth walls are already using them)
                        if (!_blocked[NE] && !_blocked[N] && !_blocked[E])
                        {
                            open.Add(NE);
                        }

                        if (!_blocked[SE] && !_blocked[S] && !_blocked[E])
                        {
                            open.Add(SE);
                        }

                        if (!_blocked[SW] && !_blocked[S] && !_blocked[W])
                        {
                            open.Add(SW);
                        }

                        if (!_blocked[NW] && !_blocked[N] && !_blocked[W])
                        {
                            open.Add(NW);
                        }

                        double mult = -_wallReflectivity * VEL_OUTSIDECORNER / open.Count;        // they need to add up to VEL_OUTSIDECORNER * -reflectivity
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();

                        velocityX.Add(new IndexLERP(k, lerp));
                        velocityY.Add(new IndexLERP(k, lerp));

                        mult = OTHER_ANY / open.Count;        // they need to add up to OTHER_ANY
                        lerp = open.Select(o => Tuple.Create(o, mult)).ToArray();
                        other.Add(new IndexLERP(k, lerp));

                        #endregion
                    }
                }
            }

            // Store the values
            _blocked_VelocityX = velocityX.ToArray();
            _blocked_VelocityY = velocityY.ToArray();
            _blocked_Other = other.ToArray();

            _hasBlockedCells = _blocked_VelocityX.Length > 0;       // the three arrays are the same size
            _isBlockCacheDirty = false;
        }

        #region OLD blocked cell handling

        //#region struct: Index12D

        ///// <summary>
        ///// This is just a convenient way to store an index.  Both the 2D version and the 1D version
        ///// </summary>
        //private struct Index12D
        //{
        //    public Index12D(int k, int x, int y)
        //    {
        //        this.K = k;
        //        this.X = x;
        //        this.Y = y;
        //    }

        //    public readonly int K;

        //    public readonly int X;
        //    public readonly int Y;
        //}

        //#endregion

        //private struct IndexLERP
        //{
        //    public IndexLERP(int k, int[] neighborKs)
        //    {
        //        this.K = k;

        //        this.NeighborKs = neighborKs;
        //        this.NeighborCountInverse = 1d / Convert.ToDouble(neighborKs.Length);
        //    }

        //    public readonly int K;

        //    public readonly int[] NeighborKs;
        //    public readonly double NeighborCountInverse;        // storing the inverse, so it can be multiplied
        //}

        ////TODO: Velocity is still eaten by angled walls.  Consider unblocked corners

        //// These are the indexes into blocked cells that have a non blocked cell to their left/right/top/bottom
        ////NOTE: left/right indexes are populated independantly of top/bottom indexes (a blocked cell can be in both)
        //private Index12D[] _blockedLeftEdges = null;
        //private Index12D[] _blockedRightEdges = null;
        //private Index12D[] _blockedLeftAndRightEdges = null;

        //private Index12D[] _blockedTopEdges = null;
        //private Index12D[] _blockedBottomEdges = null;
        //private Index12D[] _blockedTopAndBottomEdges = null;

        //private IndexLERP[] _blockedLERP = null;
        //private IndexLERP2[] _blockedLERP2 = null;

        //private void IndexBlockedCells()
        //{
        //    if (!_isBlockCacheDirty)
        //    {
        //        return;
        //    }

        //    _hasBlockedCells = false;

        //    List<Index12D> leftEdges = new List<Index12D>();
        //    List<Index12D> rightEdges = new List<Index12D>();
        //    List<Index12D> leftAndRightEdges = new List<Index12D>();

        //    List<Index12D> topEdges = new List<Index12D>();
        //    List<Index12D> bottomEdges = new List<Index12D>();
        //    List<Index12D> topAndBottomEdges = new List<Index12D>();

        //    List<IndexLERP> lerp = new List<IndexLERP>();
        //    List<IndexLERP2> lerp2 = new List<IndexLERP2>();
        //    List<int> lerpNeighbors = new List<int>();
        //    List<int> lerpCornerNeighbors = new List<int>();

        //    // For now, ignore blocked cells that are on the edges of the field

        //    for (int x = 1; x < _xSize - 1; x++)
        //    {
        //        for (int y = 1; y < _ySize - 1; y++)
        //        {
        //            int k = GetK(x, y);

        //            if (!_blocked[k])
        //            {
        //                continue;
        //            }

        //            int leftK = GetK(x - 1, y);
        //            int rightK = GetK(x + 1, y);
        //            int topK = GetK(x, y - 1);
        //            int bottomK = GetK(x, y + 1);

        //            lerpNeighbors.Clear();
        //            lerpCornerNeighbors.Clear();

        //            #region Left and Right

        //            bool isLeft = false;
        //            bool isRight = false;

        //            if (!_blocked[leftK])
        //            {
        //                isLeft = true;
        //                lerpNeighbors.Add(leftK);
        //            }

        //            if (!_blocked[rightK])
        //            {
        //                isRight = true;
        //                lerpNeighbors.Add(rightK);
        //            }

        //            if (isLeft && isRight)
        //            {
        //                leftAndRightEdges.Add(new Index12D(k, x, y));
        //            }
        //            else if (isLeft)
        //            {
        //                leftEdges.Add(new Index12D(k, x, y));
        //            }
        //            else if (isRight)
        //            {
        //                rightEdges.Add(new Index12D(k, x, y));
        //            }

        //            #endregion
        //            #region Top and Bottom

        //            bool isTop = false;
        //            bool isBottom = false;

        //            if (!_blocked[topK])
        //            {
        //                isTop = true;
        //                lerpNeighbors.Add(topK);
        //            }

        //            if (!_blocked[bottomK])
        //            {
        //                isBottom = true;
        //                lerpNeighbors.Add(bottomK);
        //            }

        //            if (isTop && isBottom)
        //            {
        //                topAndBottomEdges.Add(new Index12D(k, x, y));
        //            }
        //            else if (isTop)
        //            {
        //                topEdges.Add(new Index12D(k, x, y));
        //            }
        //            else if (isBottom)
        //            {
        //                bottomEdges.Add(new Index12D(k, x, y));
        //            }

        //            #endregion

        //            #region LERP - 1

        //            if (lerpNeighbors.Count > 0)
        //            {
        //                lerp.Add(new IndexLERP(k, lerpNeighbors.ToArray()));
        //            }

        //            #endregion
        //            #region LERP - 2

        //            int cornerK;

        //            // Add open corner neighbors, only if they have both orhogonal neighbors
        //            if (isLeft && isTop)
        //            {
        //                cornerK = GetK(x - 1, y - 1);
        //                if (!_blocked[cornerK])
        //                {
        //                    lerpCornerNeighbors.Add(cornerK);
        //                }
        //            }

        //            if (isRight && isTop)
        //            {
        //                cornerK = GetK(x + 1, y - 1);
        //                if (!_blocked[cornerK])
        //                {
        //                    lerpCornerNeighbors.Add(cornerK);
        //                }
        //            }

        //            if (isLeft && isBottom)
        //            {
        //                cornerK = GetK(x - 1, y + 1);
        //                if (!_blocked[cornerK])
        //                {
        //                    lerpCornerNeighbors.Add(cornerK);
        //                }
        //            }

        //            if (isRight && isBottom)
        //            {
        //                cornerK = GetK(x + 1, y + 1);
        //                if (!_blocked[cornerK])
        //                {
        //                    lerpCornerNeighbors.Add(cornerK);
        //                }
        //            }

        //            if (lerpNeighbors.Count > 0 || lerpCornerNeighbors.Count > 0)
        //            {
        //                const double CORNERMULT = .5d;

        //                double total = lerpNeighbors.Count + (CORNERMULT * lerpCornerNeighbors.Count);

        //                double orthMult = 1d / total;
        //                double cornerMult = CORNERMULT / total;

        //                // Store each neighbor with its own multiplier to make it easier during setbounds
        //                lerp2.Add(new IndexLERP2(
        //                    k,
        //                    UtilityHelper.Iterate(
        //                        lerpNeighbors.Select(o => Tuple.Create(o, orthMult)),
        //                        lerpCornerNeighbors.Select(o => Tuple.Create(o, cornerMult))
        //                        ).ToArray()
        //                    ));
        //            }

        //            #endregion
        //        }
        //    }

        //    // Store the values
        //    _blockedLeftEdges = leftEdges.ToArray();
        //    _blockedRightEdges = rightEdges.ToArray();
        //    _blockedLeftAndRightEdges = leftAndRightEdges.ToArray();

        //    _blockedTopEdges = topEdges.ToArray();
        //    _blockedBottomEdges = bottomEdges.ToArray();
        //    _blockedTopAndBottomEdges = topAndBottomEdges.ToArray();

        //    _blockedLERP = lerp.ToArray();
        //    _blockedLERP2 = lerp2.ToArray();

        //    _hasBlockedCells = _blockedLeftEdges.Length > 0 || _blockedRightEdges.Length > 0 || _blockedLeftAndRightEdges.Length > 0 || _blockedTopEdges.Length > 0 || _blockedTopEdges.Length > 0 || _blockedBottomEdges.Length > 0 || _blockedTopAndBottomEdges.Length > 0 || _blockedLERP.Length > 0 || _blockedLERP2.Length > 0;
        //    _isBlockCacheDirty = false;
        //}

        //private void SetBounds_BlockedCells(SetBoundsType boundType, double[] cells)
        //{
        //    //NOTE: It is safe to ignore blocked cells that are interior (surrounded by other blocked cells)

        //    switch (boundType)
        //    {
        //        case SetBoundsType.XArray:
        //            #region XArray

        //            // Blocked cells that are on the left and right edges of a wall of blocked cells need to be set to zero
        //            foreach (Index12D index in UtilityHelper.Iterate(_blockedLeftEdges, _blockedRightEdges, _blockedLeftAndRightEdges))
        //            {
        //                cells[index.K] = 0;
        //            }

        //            // The tops and bottoms need to be a copy of their neighbor
        //            // Top
        //            foreach (Index12D index in _blockedTopEdges)
        //            {
        //                cells[index.K] = cells[GetK(index.X, index.Y - 1)];
        //            }

        //            // Bottom
        //            foreach (Index12D index in _blockedBottomEdges)
        //            {
        //                cells[index.K] = cells[GetK(index.X, index.Y + 1)];
        //            }

        //            // Top and Bottom
        //            foreach (Index12D index in _blockedTopAndBottomEdges)
        //            {
        //                cells[index.K] = .5d * (cells[GetK(index.X, index.Y - 1)] + cells[GetK(index.X, index.Y + 1)]);
        //            }

        //            #endregion
        //            break;

        //        case SetBoundsType.YArray:
        //            #region YArray

        //            // Topmost and bottommost become zero
        //            foreach (Index12D index in UtilityHelper.Iterate(_blockedTopEdges, _blockedBottomEdges, _blockedTopAndBottomEdges))
        //            {
        //                cells[index.K] = 0;
        //            }

        //            // The left and right need to be a copy of their neighbor
        //            // Left
        //            foreach (Index12D index in _blockedLeftEdges)
        //            {
        //                cells[index.K] = cells[GetK(index.X - 1, index.Y)];
        //            }

        //            // Right
        //            foreach (Index12D index in _blockedRightEdges)
        //            {
        //                cells[index.K] = cells[GetK(index.X + 1, index.Y)];
        //            }

        //            // Left and Right
        //            foreach (Index12D index in _blockedLeftAndRightEdges)
        //            {
        //                cells[index.K] = .5d * (cells[GetK(index.X - 1, index.Y)] + cells[GetK(index.X + 1, index.Y)]);
        //            }

        //            #endregion
        //            break;

        //        case SetBoundsType.Both_Layers:
        //        case SetBoundsType.Both_Other:
        //            #region Both

        //            ////Attempt1 - doesn't consider corner neighbors - still eats velocity/ink when the wall is at an angle

        //            //// Each blocked cell that has neighboring non blocked cells needs to get the average value of those neighbors
        //            //foreach (IndexLERP index in _blockedLERP)
        //            //{
        //            //    // Add up the neighbors (could be 1 to 4 neighbors)
        //            //    double newValue = 0d;
        //            //    foreach (int neighbor in index.NeighborKs)
        //            //    {
        //            //        newValue += cells[neighbor];
        //            //    }

        //            //    // Store the average
        //            //    cells[index.K] = index.NeighborCountInverse * newValue;
        //            //}

        //            // Attempt2
        //            foreach (IndexLERP2 index in _blockedLERP2)
        //            {
        //                // Add up the neighbors (could be 1 to 4 neighbors)
        //                double newValue = 0d;
        //                foreach (var neighbor in index.Neighbors)
        //                {
        //                    newValue += cells[neighbor.Item1] * neighbor.Item2;
        //                }

        //                // Store the average
        //                cells[index.K] = newValue;
        //            }

        //            #endregion
        //            break;

        //        default:
        //            throw new ApplicationException("Unknown SetBoundsType: " + boundType.ToString());
        //    }
        //}

        #endregion
        #region ORIG

        //private void SetBounds(SetBoundsType boundType, double[] cells)
        //{
        //    switch (boundType)
        //    {
        //        case SetBoundsType.XArray:
        //            #region XArray

        //            // Set left and right edges to zero
        //            for (int y = 0; y < _ySize; y++)
        //            {
        //                cells[GetK(0, y)] = 0;
        //                cells[GetK(_xSize - 1, y)] = 0;
        //            }

        //            // Set top and bottom edges to their neighbors
        //            for (int x = 1; x < _xSize - 1; x++)
        //            {
        //                cells[GetK(x, 0)] = cells[GetK(x, 1)];
        //                cells[GetK(x, _ySize - 1)] = cells[GetK(x, _ySize - 2)];
        //            }

        //            #endregion
        //            break;

        //        case SetBoundsType.YArray:
        //            #region YArray

        //            // Set top and bottom edges to zero
        //            for (int x = 0; x < _xSize; x++)
        //            {
        //                cells[GetK(x, 0)] = 0;
        //                cells[GetK(x, _ySize - 1)] = 0;
        //            }

        //            // Set left and right edges to their neighbors
        //            for (int y = 1; y < _ySize - 1; y++)
        //            {
        //                cells[GetK(0, y)] = cells[GetK(1, y)];
        //                cells[GetK(_xSize - 1, y)] = cells[GetK(_xSize - 2, y)];
        //            }

        //            #endregion
        //            break;

        //        case SetBoundsType.Both_Layers:
        //        case SetBoundsType.Both_Other:
        //            #region Both

        //            // Set top and bottom edges to their neighbors
        //            for (int x = 1; x < _xSize - 1; x++)
        //            {
        //                cells[GetK(x, 0)] = cells[GetK(x, 1)];
        //                cells[GetK(x, _ySize - 1)] = cells[GetK(x, _ySize - 2)];
        //            }

        //            // Set left and right edges to their neighbors
        //            for (int y = 1; y < _ySize - 1; y++)
        //            {
        //                cells[GetK(0, y)] = cells[GetK(1, y)];
        //                cells[GetK(_xSize - 1, y)] = cells[GetK(_xSize - 2, y)];
        //            }

        //            // Set the corners to the average of their two neighbors
        //            cells[GetK(0, 0)] = 0.5 * (cells[GetK(0, 1)] + cells[GetK(1, 0)]);
        //            cells[GetK(0, _ySize - 1)] = 0.5 * (cells[GetK(1, _ySize - 1)] + cells[GetK(0, _ySize - 2)]);
        //            cells[GetK(_xSize - 1, 0)] = 0.5 * (cells[GetK(_xSize - 1, 1)] + cells[GetK(_xSize - 2, 0)]);
        //            cells[GetK(_xSize - 1, _ySize - 1)] = 0.5 * (cells[GetK(_xSize - 1, _ySize - 2)] + cells[GetK(_xSize - 2, _ySize - 1)]);

        //            #endregion
        //            break;

        //        default:
        //            throw new ApplicationException("Unknown SetBoundsType: " + boundType.ToString());
        //    }
        //}

        ///// <remarks>
        ///// From the pdf:
        /////     The current solver suffers from what is called “numerical dissipation”: the fluids dampen faster 
        /////     than they should in reality. This is in essence what makes the algorithms stable. Recently 
        /////     Fedkiw et al. [Fedkiw01] propose a technique called “vorticity confinement” which re-injects the 
        /////     lost energy due to dissipation back into the fluid, through a force which encourages the flow to 
        /////     exhibit small scale vorticity. This technique works well for the simulation of smoke for example.
        ///// </remarks>
        //private void VorticityConfinement(double[] xForce, double[] yForce)
        //{
        //    //Calculate magnitude of curl(u,v) for each cell. (|w|)
        //    for (int y = 1; y < _ySize - 1; y++)
        //    {
        //        int yIndex = y * _xSize;
        //        for (int x = 1; x < _xSize - 1; x++)
        //        {
        //            int k = x + yIndex;
        //            double du_dy = (_xVel[k + _xSize] - _xVel[k - _xSize]) * 0.5d;
        //            double dv_dx = (_yVel[k + 1] - _yVel[k - 1]) * 0.5d;
        //            //double du_dy = (xVel[getK(x, y + 1)] - xVel[getK(x, y - 1)]) * 0.5d;
        //            //double dv_dx = (yVel[getK(x + 1, y)] - yVel[getK(x - 1, y)]) * 0.5d;

        //            // curl =  du_dy - dv_dx;
        //            _curl[k] = du_dy - dv_dx;
        //            _curlAbs[k] = Math.Abs(_curl[k]);
        //        }
        //    }

        //    for (int y = 2; y < _ySize - 2; y++)
        //    {
        //        int yIndex = y * _xSize;
        //        for (int x = 2; x < _xSize - 2; x++)
        //        {
        //            int k = x + yIndex;
        //            // Find derivative of the magnitude (n = del |w|)
        //            double dw_dx = (_curlAbs[k + 1] - _curlAbs[k - 1]) * 0.5d;
        //            double dw_dy = (_curlAbs[k + _xSize] - _curlAbs[k - _xSize]) * 0.5d;
        //            //double dw_dx = (curlAbs[getK(x + 1, y)] - curlAbs[getK(x - 1, y)]) * 0.5d;
        //            //double dw_dy = (curlAbs[getK(x, y + 1)] - curlAbs[getK(x, y - 1)]) * 0.5d;

        //            // Calculate vector length. (|n|)
        //            // Add small factor to prevent divide by zeros.
        //            double length = Math.Sqrt(dw_dx * dw_dx + dw_dy * dw_dy) + 0.000001;

        //            // N = ( n/|n| )
        //            dw_dx /= length;
        //            dw_dy /= length;

        //            double v = _curl[k];

        //            // N x w
        //            xForce[k] = dw_dy * -v * _vorticity;
        //            yForce[k] = dw_dx * v * _vorticity;
        //        }
        //    }
        //}

        //private void StupidSolve(double[] dest, double[] src, SetBoundsType boundType, double a)
        //{
        //    for (int y = 1; y < _ySize - 1; y++)
        //    {
        //        int yIndex = y * _xSize;
        //        for (int x = 1; x < _xSize - 1; x++)
        //        {
        //            int k = x + yIndex;
        //            dest[k] = (a * (src[k - 1] + src[k + 1] + src[k - _xSize] + src[k + _xSize]) + src[k]) / (1 + 4 * a);
        //        }
        //    }
        //    SetBounds(boundType, dest);
        //}

        ///// <summary>
        ///// Improved gauss-siedel by adding relaxation factor. Overrelaxation at 1.5 seems strong, and higher values
        ///// create small-scale instablity (mixing) but seem to produce reasonable incompressiblity even faster.
        ///// 4-10 iterations is good for real-time, and not noticably inaccurate. For real accuracy, upwards of 20 is good.
        ///// </summary>
        //private void LinearSolve(double[] dest, double[] src, SetBoundsType boundType, double a, double c)
        //{
        //    double wMax = 1.9;
        //    double wMin = 1.5;
        //    for (int i = 0; i < _iterations; i++)
        //    {
        //        double w = Math.Max((wMin - wMax) * i / 60.0 + wMax, wMin);
        //        for (int y = 1; y < _ySize - 1; y++)
        //        {
        //            int yIndex = y * _xSize;
        //            for (int x = 1; x < _xSize - 1; x++)
        //            {
        //                int k = x + yIndex;
        //                dest[k] = dest[k] + w * ((a * (dest[k - 1] + dest[k + 1] + dest[k - _xSize] + dest[k + _xSize]) + src[k]) / c - dest[k]);
        //                //dest[getK(x, y)] = (a * (dest[getK(x-1,y)] + dest[getK(x+1,y)] + dest[getK(x,y-1)] + dest[getK(x,y+1)]) + src[getK(x,y)]) / c;
        //            }
        //        }

        //        SetBounds(boundType, dest);
        //    }
        //}

        //private void Advect(double[] dest, double[] src, double[] xVelocity, double[] yVelocity, SetBoundsType boundType, double dt)
        //{
        //    for (int y = 1; y < _ySize - 1; y++)
        //    {
        //        int yIndex = y * _xSize;
        //        for (int x = 1; x < _xSize - 1; x++)
        //        {
        //            int k = x + yIndex;
        //            //Reverse velocity, since we are interpolating backwards
        //            //xSrc and ySrc is the position of the source density.
        //            double xSrc = x - dt * xVelocity[k];
        //            double ySrc = y - dt * yVelocity[k];

        //            if (xSrc < 0.5) { xSrc = 0.5; }
        //            if (xSrc > _xSize - 1.5) { xSrc = _xSize - 1.5; }
        //            int xi0 = (int)xSrc;
        //            int xi1 = xi0 + 1;

        //            if (ySrc < 0.5) { ySrc = 0.5; }
        //            if (ySrc > _ySize - 1.5) { ySrc = _ySize - 1.5; }
        //            int yi0 = (int)ySrc;
        //            int yi1 = yi0 + 1;

        //            //Linear interpolation factors. Ex: 0.6 and 0.4
        //            double xProp1 = xSrc - xi0;
        //            double xProp0 = 1.0 - xProp1;
        //            double yProp1 = ySrc - yi0;
        //            double yProp0 = 1.0 - yProp1;

        //            dest[k] =
        //                xProp0 * (yProp0 * src[GetK(xi0, yi0)] + yProp1 * src[GetK(xi0, yi1)]) +
        //                xProp1 * (yProp0 * src[GetK(xi1, yi0)] + yProp1 * src[GetK(xi1, yi1)]);
        //        }
        //    }

        //    SetBounds(boundType, dest);
        //}

        //private void Project(double[] xV, double[] yV, double[] p, double[] div)
        //{
        //    double h = 0.1;///(xSize-2);
        //    for (int y = 1; y < _ySize - 1; y++)
        //    {
        //        int yIndex = y * _xSize;
        //        for (int x = 1; x < _xSize - 1; x++)
        //        {
        //            int k = x + yIndex;
        //            //Negative divergence
        //            div[k] = -0.5 * h * (xV[k + 1] - xV[k - 1] + yV[k + _xSize] - yV[k - _xSize]);
        //            //Pressure field
        //            p[k] = 0;
        //        }
        //    }
        //    SetBounds(SetBoundsType.Both_Other, div);
        //    SetBounds(SetBoundsType.Both_Other, p);

        //    LinearSolve(p, div, SetBoundsType.Both_Other, 1, 4);

        //    for (int y = 1; y < _ySize - 1; y++)
        //    {
        //        int yIndex = y * _xSize;
        //        for (int x = 1; x < _xSize - 1; x++)
        //        {
        //            int k = x + yIndex;
        //            xV[k] -= 0.5 * (p[k + 1] - p[k - 1]) / h;
        //            yV[k] -= 0.5 * (p[k + _xSize] - p[k - _xSize]) / h;
        //        }
        //    }
        //    SetBounds(SetBoundsType.XArray, xV);
        //    SetBounds(SetBoundsType.YArray, yV);
        //}

        #endregion

        #endregion
    }

    #region enum: FluidFieldBoundryType2D

    //TODO: May want combinations of these: Bottom is solid, Top is open, Left/Right and Front/Back are wrap around
    public enum FluidFieldBoundryType2D
    {
        Closed,
        Open,
        WrapAround,

        // This isn't really needed.  It can be accomplished by setting blocked cells
        //ClosedCircle     // sphere in 3D
    }

    #endregion
}
