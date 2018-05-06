using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.HelperClassesCore;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.Math3D;
using Game.Orig.Map;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    //TODO:  Put the majority of this class in Game.HelperClassesCore (as an abstract)
    public class ShipController
    {
        #region enum: ShipTypeQual

        public enum ShipTypeQual
        {
            None = 0,
            Ball,
            SolidBall
        }

        #endregion
        #region enum: AttatchementType

        private enum AttatchementType
        {
            Thruster = 0,
            Tractor,
            Cannon,
            MachineGun
        }

        #endregion

        #region Events

        public event EventHandler CreateNewTractorBeams = null;
        public event EventHandler RecalcTractorBeamOffsets = null;
        public event EventHandler ChangeTractorBeamPower = null;

        #endregion

        #region Declaration Section

        private const double TURNRADIANS = .12d;
        public const double STANDARDRADIUS = 300;
        //public const double THRUSTER_FORCE = 120d;
        public const double THRUSTER_FORCE = 20000000d;

        private Random _rand = new Random();

        private bool _isSetup = false;

        // Pointers passed in
        private LargeMapViewer2D _picturebox = null;
        private SimpleMap _map;
        private MyVector _boundryLower = null;
        private MyVector _boundryUpper = null;

        /// <summary>
        /// The ship type could get swapped around (causing new blips to be created), but this will be the only
        /// token I will use (there will only be at most one ship in existance)
        /// </summary>
        private long _blipToken = 0;
        private BallBlip _ship = null;

        // These properties are what the user plays with in the property panel
        private ShipTypeQual _type = ShipTypeQual.None;
        private double _shipSize = 0;
        private double _thrusterAngle = 0;
        private double _machineGunAngle = 0;
        private bool _isMachineGunCrossoverInfinity = true;
        private double _machineGunCrossoverDistance = 0;
        private bool _ignoreOtherProjectiles = true;

        // Keyboard
        private bool _isUpPressed = false;
        private bool _isDownPressed = false;
        private bool _isLeftPressed = false;
        private bool _isRightPressed = false;
        private bool _isAPressed = false;
        private bool _isSPressed = false;
        private bool _isDPressed = false;
        private bool _isWPressed = false;
        private bool _isQPressed = false;
        private bool _isEPressed = false;
        private bool _isShiftPressed = false;
        private bool _isCtrlPressed = false;

        private int _qDownTick = Environment.TickCount;
        private int _eDownTick = Environment.TickCount;
        private bool _isQLocked = false;
        private bool _isELocked = false;

        private int _powerLevel = 2;  // 1=.5x, 2=1x, 3=2x, 4=infinite

        // Tractor Beam
        private List<TractorBeamCone> _tractorBeams = new List<TractorBeamCone>();

        // Guns
        private ProjectileWeapon _cannon = null;
        private List<ProjectileWeapon> _machineGuns = new List<ProjectileWeapon>();

        // These thruster offsets are only used for the solid ball
        private MyVector _thrusterOffset_BottomRight = null;
        private MyVector _thrusterOffset_BottomLeft = null;
        private MyVector _thrusterOffset_TopRight = null;
        private MyVector _thrusterOffset_TopLeft = null;

        // These are lines to draw to show thrust
        private List<MyVector[]> _thrustLines = new List<MyVector[]>();

        private double _thrustForce = 0d;
        private double _torqueballLeftRightThrusterForce = 0d;

        #endregion

        #region Constructor

        public ShipController(LargeMapViewer2D picturebox, SimpleMap map, MyVector boundryLower, MyVector boundryUpper)
        {
            _picturebox = picturebox;
            _map = map;
            _boundryLower = boundryLower;
            _boundryUpper = boundryUpper;

            _blipToken = TokenGenerator.NextToken();

            _picturebox.KeyDown += new System.Windows.Forms.KeyEventHandler(Picturebox_KeyDown);
            _picturebox.KeyUp += new System.Windows.Forms.KeyEventHandler(Picturebox_KeyUp);
        }

        #endregion

        #region Public Properties

        public bool Active
        {
            get
            {
                return _type != ShipTypeQual.None;
            }
        }

        public long ShipToken
        {
            get
            {
                return _blipToken;
            }
        }

        /// <summary>
        /// NOTE:  This may be null
        /// </summary>
        public BallBlip Ship
        {
            get
            {
                return _ship;
            }
        }

        public ShipTypeQual ShipType
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;

                PropsChanged(true);
            }
        }
        public double ShipSize
        {
            get
            {
                return _shipSize;
            }
            set
            {
                _shipSize = value;

                PropsChanged(false);
            }
        }
        public double ThrusterOffset
        {
            get
            {
                return _thrusterAngle;
            }
            set
            {
                _thrusterAngle = value;

                PropsChanged(false);
            }
        }
        public double MachineGunOffset
        {
            get
            {
                return _machineGunAngle;
            }
            set
            {
                _machineGunAngle = value;

                PropsChanged(false);
            }
        }

        public bool MachineGunCrossoverDistanceIsInfinity
        {
            get
            {
                return _isMachineGunCrossoverInfinity;
            }
            set
            {
                _isMachineGunCrossoverInfinity = value;

                PropsChanged(false);
            }
        }
        public double MachineGunCrossoverDistance
        {
            get
            {
                return _machineGunCrossoverDistance;
            }
            set
            {
                _machineGunCrossoverDistance = value;

                PropsChanged(false);
            }
        }

        public bool IgnoreOtherProjectiles
        {
            get
            {
                return _ignoreOtherProjectiles;
            }
            set
            {
                _ignoreOtherProjectiles = value;

                PropsChanged(false);
            }
        }

        public List<TractorBeamCone> TractorBeams
        {
            get
            {
                return _tractorBeams;
            }
        }

        public int PowerLevel
        {
            get
            {
                return _powerLevel;
            }
        }

        #endregion

        #region Public Methods

        public void FinishedSetup()
        {
            _isSetup = true;

            PropsChanged(true);
        }

        public void StopShip()
        {
            if (_ship != null)
            {
                _ship.Ball.StopBall();
            }
        }

        public void Timer(double elapsedTime)
        {
            if (_type == ShipTypeQual.None)
            {
                return;
            }

            _thrustLines.Clear();

            if (_type == ShipTypeQual.Ball)
            {
                #region Ship Thrusters

                // I need to double the output of the thrusters because ball only has one firing instead of two

                if (_isUpPressed)
                {
                    ApplyThrust(new MyVector(0, 0, 0), new MyVector(0, 1, 0), _thrustForce * 2d);		// down
                }

                if (_isWPressed)
                {
                    ApplyThrust(new MyVector(0, 0, 0), new MyVector(0, 1, 0), _thrustForce * 20d);		// down
                }

                if (_isDownPressed)
                {
                    ApplyThrust(new MyVector(0, 0, 0), new MyVector(0, -1, 0), _thrustForce * 2d);		// up
                }

                if (_isSPressed)
                {
                    ApplyThrust(new MyVector(0, 0, 0), new MyVector(0, -1, 0), _thrustForce * 20d);		// up
                }

                if (_isLeftPressed)
                {
                    _ship.Ball.RotateAroundAxis(new MyVector(0, 0, 1), TURNRADIANS * -1);
                }

                if (_isRightPressed)
                {
                    _ship.Ball.RotateAroundAxis(new MyVector(0, 0, 1), TURNRADIANS);
                }

                if (_isAPressed)
                {
                    ApplyThrust(new MyVector(0, 0, 0), new MyVector(1, 0, 0), _thrustForce * 2d);		// right
                }

                if (_isDPressed)
                {
                    ApplyThrust(new MyVector(0, 0, 0), new MyVector(-1, 0, 0), _thrustForce * 2d);		// left
                }

                #endregion
            }
            else if (_type == ShipTypeQual.SolidBall)
            {
                #region Ship Thrusters

                if (_isUpPressed)
                {
                    ApplyThrust(_thrusterOffset_TopRight, new MyVector(0, 1, 0), _thrustForce);		// down
                    ApplyThrust(_thrusterOffset_TopLeft, new MyVector(0, 1, 0), _thrustForce);		// s
                }

                if (_isWPressed)
                {
                    ApplyThrust(_thrusterOffset_TopRight, new MyVector(0, 1, 0), _thrustForce * 10d);		// down
                    ApplyThrust(_thrusterOffset_TopLeft, new MyVector(0, 1, 0), _thrustForce * 10d);		// s
                }

                if (_isDownPressed)
                {
                    ApplyThrust(_thrusterOffset_BottomRight, new MyVector(0, -1, 0), _thrustForce);		// up
                    ApplyThrust(_thrusterOffset_BottomLeft, new MyVector(0, -1, 0), _thrustForce);		// w
                }

                if (_isSPressed)
                {
                    ApplyThrust(_thrusterOffset_BottomRight, new MyVector(0, -1, 0), _thrustForce * 10d);		// up
                    ApplyThrust(_thrusterOffset_BottomLeft, new MyVector(0, -1, 0), _thrustForce * 10d);		// w
                }

                if (_isLeftPressed)
                {
                    ApplyThrust(_thrusterOffset_BottomRight, new MyVector(0, -1, 0), _torqueballLeftRightThrusterForce);		// up
                    ApplyThrust(_thrusterOffset_TopLeft, new MyVector(0, 1, 0), _torqueballLeftRightThrusterForce);		// s
                }

                if (_isRightPressed)
                {
                    ApplyThrust(_thrusterOffset_TopRight, new MyVector(0, 1, 0), _torqueballLeftRightThrusterForce);		// down
                    ApplyThrust(_thrusterOffset_BottomLeft, new MyVector(0, -1, 0), _torqueballLeftRightThrusterForce);		// w
                }

                if (_isAPressed)
                {
                    ApplyThrust(_thrusterOffset_TopLeft, new MyVector(1, 0, 0), _thrustForce);		// right
                    ApplyThrust(_thrusterOffset_BottomLeft, new MyVector(1, 0, 0), _thrustForce);		// right
                }

                if (_isDPressed)
                {
                    ApplyThrust(_thrusterOffset_TopRight, new MyVector(-1, 0, 0), _thrustForce);		// left
                    ApplyThrust(_thrusterOffset_BottomRight, new MyVector(-1, 0, 0), _thrustForce);		// left
                }

                #endregion
            }

            #region Tractor Beams

            foreach (TractorBeamCone tractor in _tractorBeams)
            {
                if ((_isQPressed || _isQLocked) && (_isEPressed || _isELocked))
                {
                    tractor.TurnOnStatic();
                }
                else if (_isQPressed || _isQLocked)
                {
                    tractor.TurnOn(-1d);
                }
                else if (_isEPressed || _isELocked)
                {
                    tractor.TurnOn(1d);
                }
                else
                {
                    tractor.TurnOff();
                }

                tractor.Timer();
            }

            #endregion

            #region Guns

            _cannon.Timer(elapsedTime);

            foreach (ProjectileWeapon weapon in _machineGuns)
            {
                weapon.Timer(elapsedTime);
            }

            #endregion
        }

        public void Draw()
        {
            if (_type == ShipTypeQual.None)
            {
                return;
            }

            // Fill Circle
            _picturebox.FillCircle(Color.FromArgb(100, Color.LimeGreen), _ship.Sphere.Position, _ship.Sphere.Radius);

            // Draw direction facing
            MyVector dirFacing = _ship.Sphere.DirectionFacing.Standard.Clone();
            dirFacing.BecomeUnitVector();
            dirFacing.Multiply(_ship.Sphere.Radius);
            dirFacing.Add(_ship.Sphere.Position);

            _picturebox.DrawLine(Color.White, 4, _ship.Sphere.Position, dirFacing);

            // Draw an edge
            _picturebox.DrawCircle(Color.Black, 25d, _ship.Sphere.Position, _ship.Sphere.Radius);

            #region Thrust Lines

            foreach (MyVector[] thrustPair in _thrustLines)
            {
                MyVector thrustStart = _ship.Ball.Rotation.GetRotatedVector(thrustPair[0], true);
                thrustStart.Add(_ship.Ball.Position);

                MyVector thrustStop = thrustPair[1] * -250d;
                thrustStop.Add(thrustPair[0]);
                thrustStop = _ship.Ball.Rotation.GetRotatedVector(thrustStop, true);
                thrustStop.Add(_ship.Ball.Position);

                _picturebox.DrawLine(Color.Coral, 40d, thrustStart, thrustStop);
            }

            #endregion
            #region Tractor Effect

            if (_isQPressed || _isQLocked || _isEPressed || _isELocked)
            {
                foreach (TractorBeamCone tractor in _tractorBeams)
                {
                    // Figure out the cone tip location
                    MyVector tractorStart = _ship.Ball.Rotation.GetRotatedVector(tractor.Offset, true);
                    tractorStart.Add(_ship.Ball.Position);

                    // Figure out how bright to draw it
                    int alpha = 0;
                    if (_powerLevel == 1)
                    {
                        alpha = 15;
                    }
                    else if (_powerLevel == 2)
                    {
                        alpha = 30;
                    }
                    else if (_powerLevel == 3)
                    {
                        alpha = 60;
                    }
                    else //if (_powerLevel == 4)
                    {
                        alpha = 128;
                    }

                    // Draw Cone
                    if ((_isQPressed || _isQLocked) && (_isEPressed || _isELocked))
                    {
                        _picturebox.FillPie(Color.FromArgb(alpha, Color.White), tractorStart, tractor.MaxDistance, _ship.Ball.DirectionFacing.Standard, tractor.SweepAngle);
                    }
                    else if (_isQPressed || _isQLocked)
                    {
                        _picturebox.FillPie(Color.FromArgb(alpha, Color.Pink), tractorStart, tractor.MaxDistance, _ship.Ball.DirectionFacing.Standard, tractor.SweepAngle);
                    }
                    else if (_isEPressed || _isELocked)
                    {
                        _picturebox.FillPie(Color.FromArgb(alpha, Color.LightSkyBlue), tractorStart, tractor.MaxDistance, _ship.Ball.DirectionFacing.Standard, tractor.SweepAngle);
                    }
                }
            }

            #endregion
            #region Gun Effect



            #endregion

            #region Thrusters

            if (_type == ShipTypeQual.Ball)
            {
                DrawAttatchment(new MyVector(0, 0, 0), AttatchementType.Thruster);
            }
            else if (_type == ShipTypeQual.SolidBall)
            {
                DrawAttatchment(_thrusterOffset_BottomRight, AttatchementType.Thruster);
                DrawAttatchment(_thrusterOffset_BottomLeft, AttatchementType.Thruster);
                DrawAttatchment(_thrusterOffset_TopRight, AttatchementType.Thruster);
                DrawAttatchment(_thrusterOffset_TopLeft, AttatchementType.Thruster);
            }

            #endregion
            #region Gun

            DrawAttatchment(_cannon.Barrels[0].Offset, AttatchementType.Cannon);

            foreach (ProjectileWeapon weapon in _machineGuns)
            {
                DrawAttatchment(weapon.Barrels[0].Offset, AttatchementType.MachineGun);
            }

            #endregion
            #region Tractor Beam

            // This needs to go after thrusters and guns, because the tractor is smaller, and the ball has everything at zero
            foreach (TractorBeamCone tractor in _tractorBeams)
            {
                DrawAttatchment(tractor.Offset, AttatchementType.Tractor);
            }

            #endregion
        }

        #endregion

        #region Event Listeners

        private void Picturebox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            // Arrows
            if (e.KeyCode == Keys.Up)
            {
                _isUpPressed = true;
            }
            if (e.KeyCode == Keys.Down)
            {
                _isDownPressed = true;
            }
            if (e.KeyCode == Keys.Left)
            {
                _isLeftPressed = true;
            }
            if (e.KeyCode == Keys.Right)
            {
                _isRightPressed = true;
            }

            // ASDW
            if (e.KeyCode == Keys.A)
            {
                _isAPressed = true;
            }
            if (e.KeyCode == Keys.S)
            {
                _isSPressed = true;
            }
            if (e.KeyCode == Keys.D)
            {
                _isDPressed = true;
            }
            if (e.KeyCode == Keys.W)
            {
                _isWPressed = true;
            }

            // Q, E
            if (e.KeyCode == Keys.Q)
            {
                if (!_isQPressed)
                {
                    if (Environment.TickCount - _qDownTick <= SystemInformation.DoubleClickTime)
                    {
                        // This is a double tap
                        _isQLocked = true;
                    }
                    else
                    {
                        _isQLocked = false;
                    }

                    _qDownTick = Environment.TickCount;       // since this event repeats while they hold the key in, I only want to record the first time it was pressed
                }

                _isQPressed = true;
            }
            if (e.KeyCode == Keys.E)
            {
                if (!_isEPressed)
                {
                    if (Environment.TickCount - _eDownTick <= SystemInformation.DoubleClickTime)
                    {
                        // This is a double tap
                        _isELocked = true;
                    }
                    else
                    {
                        _isELocked = false;
                    }

                    _eDownTick = Environment.TickCount;       // since this event repeats while they hold the key in, I only want to record the first time it was pressed
                }

                _isEPressed = true;
            }

            // 1-4
            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
            {
                _powerLevel = 1;
                PropsChangedSprtTractorPower();
            }
            if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                _powerLevel = 2;
                PropsChangedSprtTractorPower();
            }
            if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
            {
                _powerLevel = 3;
                PropsChangedSprtTractorPower();
            }
            if (e.KeyCode == Keys.D4 || e.KeyCode == Keys.NumPad4)
            {
                _powerLevel = 4;
                PropsChangedSprtTractorPower();
            }

            if (e.KeyCode == Keys.ShiftKey)
            {
                _isShiftPressed = true;
                _cannon.StartFiring();
            }
            if (e.KeyCode == Keys.ControlKey)
            {
                _isCtrlPressed = true;
                foreach (ProjectileWeapon weapon in _machineGuns)
                {
                    weapon.StartFiring();
                }
            }
        }
        private void Picturebox_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            // Arrow Keys
            if (e.KeyCode == Keys.Up)
            {
                _isUpPressed = false;
            }
            if (e.KeyCode == Keys.Down)
            {
                _isDownPressed = false;
            }
            if (e.KeyCode == Keys.Left)
            {
                _isLeftPressed = false;
            }
            if (e.KeyCode == Keys.Right)
            {
                _isRightPressed = false;
            }

            // ASDW
            if (e.KeyCode == Keys.A)
            {
                _isAPressed = false;
            }
            if (e.KeyCode == Keys.S)
            {
                _isSPressed = false;
            }
            if (e.KeyCode == Keys.D)
            {
                _isDPressed = false;
            }
            if (e.KeyCode == Keys.W)
            {
                _isWPressed = false;
            }

            // Q, E
            if (e.KeyCode == Keys.Q)
            {
                _isQPressed = false;
            }
            if (e.KeyCode == Keys.E)
            {
                _isEPressed = false;
            }

            if (e.KeyCode == Keys.ShiftKey)
            {
                _isShiftPressed = false;
                _cannon.StopFiring();
            }
            if (e.KeyCode == Keys.ControlKey)
            {
                _isCtrlPressed = false;
                foreach (ProjectileWeapon weapon in _machineGuns)
                {
                    weapon.StopFiring();
                }
            }
        }

        #endregion

        #region Private Methods

        private void PropsChanged(bool typeChanged)
        {
            if (!_isSetup)
            {
                return;
            }

            if (typeChanged)
            {
                // Make a new ship (remove the old first)
                PropsChangedSprtNew();
            }
            else
            {
                // Change the current ship
                PropsChangedSprtExisting();
            }

            if (_type != ShipTypeQual.None)
            {
                // Figure out thruster placement
                PropsChangedSprtThrusters();

                // Figure out gun placement
                PropsChangedSprtGuns();

                if (this.RecalcTractorBeamOffsets != null)
                {
                    this.RecalcTractorBeamOffsets(this, new EventArgs());
                }

                // Figure out how much power the tractor beam has (based on 1-4 keys)
                PropsChangedSprtTractorPower();
            }
        }
        private void PropsChangedSprtNew()
        {
            MyVector position = null;

            // Kill Existing
            if (_ship != null)
            {
                position = _ship.Ball.Position.Clone();
                _map.Remove(_ship.Token);
                _ship = null;
            }
            else
            {
                position = Utility3D.GetRandomVector(_boundryLower, _boundryUpper);
            }

            _tractorBeams.Clear();
            _cannon = null;
            _machineGuns.Clear();

            #region New Ship

            //TODO:  Listen to global props
            double elasticity = .75d;
            double kineticFriction = .75d;
            double staticFriction = 1d;

            // Build New
            Ball newBall;
            RadarBlipQual blipQual;        // logic came from BallAdder.CommitObject

            switch (_type)
            {
                case ShipTypeQual.None:
                    return;

                case ShipTypeQual.Ball:
                    #region Ball

                    newBall = new Ball(position, new DoubleVector(0, 1, 0, 1, 0, 0), _shipSize, UtilityCore.GetMassForRadius(_shipSize, 1d), elasticity, kineticFriction, staticFriction, _boundryLower, _boundryUpper);

                    blipQual = RadarBlipQual.BallUserDefined00;

                    _thrustForce = GetThrustForce(newBall.Mass);
                    _torqueballLeftRightThrusterForce = _thrustForce;

                    #endregion
                    break;

                case ShipTypeQual.SolidBall:
                    #region Solid Ball

                    newBall = new SolidBall(position, new DoubleVector(0, 1, 0, 1, 0, 0), _shipSize, UtilityCore.GetMassForRadius(_shipSize, 1d), elasticity, kineticFriction, staticFriction, _boundryLower, _boundryUpper);

                    blipQual = RadarBlipQual.BallUserDefined01;

                    _thrustForce = GetThrustForce(newBall.Mass);
                    _torqueballLeftRightThrusterForce = GetLeftRightThrusterMagnitude(((SolidBall)newBall).InertialTensorBody);

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown ShipTypeQual: " + _type.ToString());
            }

            newBall.RotateAroundAxis(new MyVector(0, 0, 1), Math.PI);

            // Finish Building
            _ship = new BallBlip(newBall, CollisionStyle.Standard, blipQual, _blipToken);
            _map.Add(_ship);

            #endregion

            if (this.CreateNewTractorBeams != null)
            {
                this.CreateNewTractorBeams(this, new EventArgs());
            }

            #region Guns

            _cannon = new ProjectileWeapon(300, 150, UtilityCore.GetMassForRadius(150, 1d), 25, true, _ignoreOtherProjectiles, RadarBlipQual.Projectile, false, _map, _boundryLower, _boundryUpper);
            _cannon.AddBarrel(_ship.Ball.OriginalDirectionFacing.Standard.Clone());
            _cannon.AddFiringMode(20);
            _cannon.SetProjectileExplosion(450, 2, 10000);
            _cannon.SeProjectileFuse(500);
            _cannon.SetShip(_ship);

            for (int cntr = 0; cntr < 2; cntr++)
            {
                ProjectileWeapon weapon = new ProjectileWeapon(30, 20, UtilityCore.GetMassForRadius(20, 1d), 100, true, _ignoreOtherProjectiles, RadarBlipQual.Projectile, false, _map, _boundryLower, _boundryUpper);
                weapon.AddBarrel(new MyVector(), new MyQuaternion());
                weapon.AddFiringMode(2);
                weapon.SetProjectileExplosion(40, 2, 300);
                weapon.SeProjectileFuse(500);
                weapon.SetShip(_ship);

                _machineGuns.Add(weapon);
            }

            #endregion
        }
        private void PropsChangedSprtExisting()
        {
            switch (_type)
            {
                case ShipTypeQual.None:
                    break;

                case ShipTypeQual.Ball:
                    _ship.Ball.Radius = _shipSize;
                    _ship.Ball.Mass = UtilityCore.GetMassForRadius(_shipSize, 1d);

                    _thrustForce = GetThrustForce(_ship.Ball.Mass);
                    _torqueballLeftRightThrusterForce = _thrustForce;
                    break;

                case ShipTypeQual.SolidBall:
                    _ship.Ball.Radius = _shipSize;
                    _ship.Ball.Mass = UtilityCore.GetMassForRadius(_shipSize, 1d);

                    _thrustForce = GetThrustForce(_ship.Ball.Mass);
                    _torqueballLeftRightThrusterForce = GetLeftRightThrusterMagnitude(_ship.TorqueBall.InertialTensorBody);
                    break;

                default:
                    throw new ApplicationException("Unknown ShipTypeQual: " + _type.ToString());
            }
        }
        private void PropsChangedSprtThrusters()
        {
            if (_type != ShipTypeQual.SolidBall)
            {
                return;  // the ball just has the thruster in the center
            }

            MyVector thrusterSeed = new MyVector(0, _ship.Ball.Radius, 0);
            MyVector zAxis = new MyVector(0, 0, 1);

            // Bottom Thrusters
            _thrusterOffset_BottomRight = thrusterSeed.Clone();
            _thrusterOffset_BottomRight.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(_thrusterAngle * -1));

            _thrusterOffset_BottomLeft = thrusterSeed.Clone();
            _thrusterOffset_BottomLeft.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(_thrusterAngle));

            // Top Thrusters
            thrusterSeed = new MyVector(0, _ship.Ball.Radius * -1, 0);
            _thrusterOffset_TopRight = thrusterSeed.Clone();
            _thrusterOffset_TopRight.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(_thrusterAngle));

            _thrusterOffset_TopLeft = thrusterSeed.Clone();
            _thrusterOffset_TopLeft.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(_thrusterAngle * -1));
        }
        private void PropsChangedSprtGuns()
        {
            //TODO:  Change the size and pain of the projectiles based on the size of the ship

            // Cannon
            _cannon.Barrels[0].Offset.BecomeUnitVector();
            _cannon.Barrels[0].Offset.Multiply(_ship.Ball.Radius + (_cannon.ProjectileRadius * 1.5d));
            _cannon.IgnoreOtherProjectiles = _ignoreOtherProjectiles;

            // Machine Guns
            MyVector zAxis = new MyVector(0, 0, 1);
            MyVector gunSeed = new MyVector(0, _ship.Ball.Radius + (_machineGuns[0].ProjectileRadius * 1.5d), 0);
            bool isLeft = true;

            foreach (ProjectileWeapon weapon in _machineGuns)
            {
                // Misc
                weapon.IgnoreOtherProjectiles = _ignoreOtherProjectiles;

                double gunAngle = _machineGunAngle;
                if (isLeft)
                {
                    gunAngle *= -1;
                }

                // Offset
                weapon.Barrels[0].Offset.StoreNewValues(gunSeed);
                weapon.Barrels[0].Offset.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(gunAngle));

                // Rotation
                if (_isMachineGunCrossoverInfinity)
                {
                    weapon.Barrels[0].Rotation.StoreNewValues(new MyQuaternion(zAxis, 0));
                }
                else
                {
                    weapon.Barrels[0].Rotation.StoreNewValues(GetCrossoverRotation(weapon.Barrels[0].Offset, _machineGunCrossoverDistance, _ship.Ball.Radius, isLeft));
                }

                isLeft = false;
            }
        }
        private void PropsChangedSprtTractorPower()
        {
            if (_tractorBeams.Count == 0)
            {
                return;
            }

            if (this.ChangeTractorBeamPower != null)
            {
                this.ChangeTractorBeamPower(this, new EventArgs());
            }
        }

        private void ApplyThrust(MyVector offset, MyVector force, double forceMultiplier)
        {
            _thrustLines.Add(new MyVector[] { offset, force });

            switch (_type)
            {
                case ShipTypeQual.Ball:
                    _ship.Ball.InternalForce.Add(force * forceMultiplier);
                    break;

                case ShipTypeQual.SolidBall:
                    _ship.TorqueBall.ApplyInternalForce(offset, force * forceMultiplier);
                    break;

                default:
                    throw new ApplicationException("Unexpected ShipTypeQual: " + _type.ToString());
            }
        }

        private void DrawAttatchment(MyVector offset, AttatchementType attatchment)
        {
            MyVector worldAttatchment = _ship.Ball.Rotation.GetRotatedVector(offset, true);
            worldAttatchment.Add(_ship.Ball.Position);

            switch (attatchment)
            {
                case AttatchementType.Thruster:
                    _picturebox.FillCircle(Color.Silver, worldAttatchment, 60d);
                    _picturebox.DrawCircle(Color.Black, 1d, worldAttatchment, 60d);
                    break;

                case AttatchementType.Tractor:
                    _picturebox.FillCircle(Color.Olive, worldAttatchment, 40d);
                    _picturebox.DrawCircle(Color.Black, 1d, worldAttatchment, 40d);
                    break;

                case AttatchementType.Cannon:
                    _picturebox.FillCircle(Color.Brown, worldAttatchment, 30d);
                    _picturebox.DrawCircle(Color.Black, 1d, worldAttatchment, 30d);
                    break;

                case AttatchementType.MachineGun:
                    _picturebox.FillCircle(Color.Brown, worldAttatchment, 25d);
                    _picturebox.DrawCircle(Color.Black, 1d, worldAttatchment, 25d);
                    break;
            }
        }

        private static double GetThrustForce(double mass)
        {
            return THRUSTER_FORCE * (mass / UtilityCore.GetMassForRadius(STANDARDRADIUS, 1d));
        }
        private static double GetLeftRightThrusterMagnitude(MyMatrix3 inertialTensor)
        {
            // Create a standard sized solid ball, and use that as my baseline
            SolidBall standBall = new SolidBall(new MyVector(), new DoubleVector(1, 0, 0, 0, 1, 0), STANDARDRADIUS, UtilityCore.GetMassForRadius(STANDARDRADIUS, 1d));

            double averageStand = GetLeftRightThrusterMagnitudeSprtGetAvg(standBall.InertialTensorBody);
            double averageShip = GetLeftRightThrusterMagnitudeSprtGetAvg(inertialTensor);

            return THRUSTER_FORCE * (Math.Sqrt(averageShip) / Math.Sqrt(averageStand));     // I need sqrt, because the tensor's don't grow linearly
        }
        private static double GetLeftRightThrusterMagnitudeSprtGetAvg(MyMatrix3 inertialTensor)
        {
            double retVal = 0;

            // I don't include 2-2, because it represents the center, and doesn't really have an effect on spin?
            retVal += inertialTensor.M11;
            //retVal += inertialTensor.M12;
            //retVal += inertialTensor.M13;
            //retVal += inertialTensor.M21;
            //retVal += inertialTensor.M22;
            //retVal += inertialTensor.M23;
            //retVal += inertialTensor.M31;
            //retVal += inertialTensor.M32;
            retVal += inertialTensor.M33;

            return retVal / 2d;
        }

        private static MyQuaternion GetCrossoverRotation(MyVector offset, double distance, double radius, bool isLeft)
        {
            MyVector straight = new MyVector(0, -distance - radius, 0);
            MyVector angled = offset - new MyVector(0, distance + radius, 0);

            double angle = MyVector.GetAngleBetweenVectors(straight, angled);
            if (!isLeft)
            {
                angle = angle * -1d;
            }

            return new MyQuaternion(new MyVector(0, 0, 1), angle);
        }

        #endregion
    }
}
