//#define SHOWMOUSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    /// <summary>
    /// This listens to the mouse, and looks for a straight swipe gesture along the velocity of the bot.  When
    /// that is detected, it starts ramming (drawing some graphics, speeding up the bot more than it could
    /// normally go).  When a collision occurs in a ramming state, the item collided with will take damage
    /// </summary>
    /// <remarks>
    /// This class could use some work.  It sort of works, but could use some polish
    /// 
    /// A cool effect would be to detect if they are dragging the mouse at constant speed, or under constant
    /// acceleration (either accelerating faster, or starting fast and getting slower).  The damage applied should
    /// be the same in those 3 cases, but the way the bot is sped up could change.  (but in reality, it would be
    /// very difficult to have that fine of control over the mouse.  Just keeping it in a straight line is difficult)
    /// 
    /// TODO: When calculating damage, acceleration, this class is too dependent on _ramDirHistory.Count.
    /// If Update gets called on a different interval, Count per second will change
    /// 
    /// TODO: When struck with a weapon, take less damage if ramming
    /// 
    /// TODO: During a collision, only ram if the front part of the bot has been struck
    /// </remarks>
    public class RamWeapon : IDisposable, IPartUpdatable, IGivesDamage
    {
        #region Declaration Section

        private const int MAXPROBATION = 2;

        private bool _isFullySetUp = false;

        private readonly Bot _bot;

        private readonly Viewport3D _viewport;

        private bool _isVisualAdded = false;
        private Visual3D _visual = null;
        private PointLight _light = null;
        private Model3DGroup _modelGroup = null;
        private List<Tuple<GeometryModel3D, AnimateRotation>> _sparklies = null;

        private ScaleTransform3D _scale = null;
        private TranslateTransform3D _translate = null;
        private QuaternionRotation3D _rotate = null;



        private bool _isPlayerRam = false;

        // This is set when the ram is tied to an npc bot
        private AIMousePlate _aiPlate = null;

        // This is set when the ram is tied to the player's bot
        private FrameworkElement _mouseSource = null;
        private Point? _currentMousePoint = null;



        private List<Point> _pointHistory = null;

        private Vector? _currentRamDirection = null;
        private List<Vector> _ramDirHistory = null;

        private int _probationCount = 0;

        private Lazy<MaterialGroup> _sparklyMaterial = new Lazy<MaterialGroup>(() => GetSparklyMaterial());

#if SHOWMOUSE
        private Canvas _canvas = null;

        private Brush _orthBrush = null;
        private List<Line[]> _lines = null;       // this is just for debug visuals

        private Line _curDirectionLine = null;

        private Line _velocityLine = null;
#endif

        #endregion

        #region Constructor

        public RamWeapon(RamWeaponDNA dna, Bot bot, Viewport3D viewport)
        {
            _bot = bot;
            _viewport = viewport;

            this.DNA = dna;
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
                _bot.PhysicsBody.BodyMoved -= PhysicsBody_BodyMoved;
                if (_viewport != null && _visual != null)
                {
                    _viewport.Children.Remove(_visual);
                }
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
            if (!_isFullySetUp)
            {
                return;
            }

            Update_Ram(elapsedTime);

            Update_SparklyCount();
            Update_SparklyAngles(elapsedTime);
        }
        public void Update_AnyThread(double elapsedTime)
        {
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return null;
            }
        }


        private void Update_Ram(double elapsedTime)
        {
            //TODO: Use elapsed time more

            #region Velocity Line
#if SHOWMOUSE
            if (_canvas != null)
            {
                if (_velocityLine != null)
                {
                    _canvas.Children.Remove(_velocityLine);
                    _velocityLine = null;
                }

                if (_currentPoint != null)
                {
                    Vector botVel = _bot.VelocityWorld.ToVector2D() * 10;
                    botVel = new Vector(botVel.X, -botVel.Y);

                    _velocityLine = new Line()
                    {
                        Stroke = Brushes.Orange,
                        StrokeThickness = 2d,
                        X1 = _currentPoint.Value.X,
                        Y1 = _currentPoint.Value.Y,
                        X2 = _currentPoint.Value.X + botVel.X,
                        Y2 = _currentPoint.Value.Y + botVel.Y
                    };

                    _canvas.Children.Add(_velocityLine);
                }
            }
#endif
            #endregion

            Point? curPoint;
            if (!Update_InitialChecks(out curPoint, elapsedTime))
            {
                return;
            }

            Point prevPoint = _pointHistory[_pointHistory.Count - 1];

            _pointHistory.Add(curPoint.Value);
            _ramDirHistory.Add(curPoint.Value - prevPoint);

#if SHOWMOUSE
            AddLineVisual(prevPoint, _currentPoint.Value);
#endif

            Vector? lastFramesDir = GetAvgDirection(_ramDirHistory);
            if (lastFramesDir == null)
            {
                // Not enough data to get an average
                return;
            }

            if (_currentRamDirection == null)
            {
                Update_StoreDirection(lastFramesDir.Value);
                return;
            }

            // Compare this with the current direction
            double angle = Math.Abs(Vector.AngleBetween(_currentRamDirection.Value, lastFramesDir.Value));

            if (angle > this.DNA.MaxAngle)
            {
                _probationCount++;
                if (_probationCount > MAXPROBATION)
                {
                    StopRamming();
                }
                else
                {
                    _pointHistory.RemoveAt(_pointHistory.Count - 1);
                    _ramDirHistory.RemoveAt(_ramDirHistory.Count - 1);
                }
                return;
            }

            double accelMult = GetAccelerationMultiplier();
            _bot.DraggingBot.MaxVelocity = _bot.DNAPartial.DraggingMaxVelocity.Value * accelMult;
            _bot.DraggingBot.Multiplier = _bot.DNAPartial.DraggingMultiplier.Value * accelMult;
        }
        private bool Update_InitialChecks(out Point? currentPoint, double elapsedTime)
        {
            // Get the current point from either the mouse if they are human, or from the plate if npc
            currentPoint = GetCurrentPoint();
            if (currentPoint == null)
            {
                StopRamming();
                return false;
            }

            if (_pointHistory.Count == 0)
            {
                StopRamming();      // this is probably unnecessary
                _pointHistory.Add(currentPoint.Value);
                return false;
            }

            // Compare this current point to the previous point
            Point prevPoint = _pointHistory[_pointHistory.Count - 1];

            if (Math2D.IsNearValue(prevPoint, currentPoint.Value))
            {
                _probationCount++;
                if (_probationCount > MAXPROBATION)
                {
                    // They are the same point.  Reset
                    StopRamming();      // this is probably unnecessary
                    _pointHistory.Add(currentPoint.Value);
                }
                return false;
            }

            return true;
        }
        private void Update_StoreDirection(Vector lastFramesDir)
        {
            Vector3D currentVelocity = _bot.VelocityWorld;
            if (!Math3D.IsNearZero(currentVelocity))
            {
                Vector currentVelocityFixed = new Vector(currentVelocity.X, -currentVelocity.Y);        // Y is backward

                double angleVel = Math.Abs(Vector.AngleBetween(currentVelocityFixed, lastFramesDir));
                if (angleVel > this.DNA.MaxAngle)
                {
                    // Don't want to start ramming unless it's along the current velocity
                    StopRamming();
                    return;
                }
            }

            _currentRamDirection = lastFramesDir;

            double accelMult = GetAccelerationMultiplier();
            _bot.DraggingBot.MaxVelocity = _bot.DNAPartial.DraggingMaxVelocity.Value * accelMult;
            _bot.DraggingBot.Multiplier = _bot.DNAPartial.DraggingMultiplier.Value * accelMult;

            if (!_isVisualAdded && _viewport != null)
            {
                _sparklies = new List<Tuple<GeometryModel3D, AnimateRotation>>();
                CreateVisual();
                _viewport.Children.Add(_visual);
                _isVisualAdded = true;
            }

            #region CurDirection visual
#if SHOWMOUSE
            _curDirectionLine = new Line()
            {
                Stroke = Brushes.Pink,
                StrokeThickness = 3d,
                X1 = _pointHistory[0].X,
                Y1 = _pointHistory[0].Y,
                X2 = _pointHistory[0].X + (_currentRamDirection.Value.X * 10),
                Y2 = _pointHistory[0].Y + (_currentRamDirection.Value.Y * 10)
            };

            _canvas.Children.Add(_curDirectionLine);
#endif
            #endregion
        }

        private void Update_SparklyCount()
        {
            if (!_isVisualAdded)
            {
                return;
            }

            int count = 0;

            if (_ramDirHistory == null || _ramDirHistory.Count == 0)
            {
                count = 0;
            }
            else
            {
                count = _ramDirHistory.Count / 4;
            }

            if (count == _sparklies.Count)
            {
                return;
            }
            else if (count < _sparklies.Count)
            {
                #region Too Many

                while (count < _sparklies.Count)
                {
                    int index = StaticRandom.Next(_sparklies.Count);

                    _modelGroup.Children.Remove(_sparklies[index].Item1);
                    _sparklies.RemoveAt(index);
                }

                #endregion
            }
            else
            {
                #region Too Few

                while (_sparklies.Count < count)
                {
                    Random rand = StaticRandom.GetRandomForThread();

                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = _sparklyMaterial.Value;
                    geometry.BackMaterial = _sparklyMaterial.Value;
                    geometry.Geometry = UtilityWPF.GetSphere_LatLon(1, rand.NextPercent(.09, .33));       // this radius will be affected by scale

                    Transform3DGroup transform = new Transform3DGroup();

                    // Translate
                    Vector3D translate = Math3D.GetRandomVector_Circular(.5, 1).ToVector2D().ToVector3D(Math3D.GetNearZeroValue(.375));
                    transform.Children.Add(new TranslateTransform3D(translate));

                    // Rotate from Z to X
                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));

                    // Animated rotate around X
                    AxisAngleRotation3D axisAngle = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 10);
                    double angle = rand.NextDouble(180, 360);
                    if (rand.Next(2) == 0)
                    {
                        angle *= -1;
                    }
                    AnimateRotation animate = AnimateRotation.Create_Constant(axisAngle, angle);
                    transform.Children.Add(new RotateTransform3D(axisAngle));

                    // Scale
                    transform.Children.Add(_scale);

                    geometry.Transform = transform;

                    _modelGroup.Children.Add(geometry);

                    _sparklies.Add(Tuple.Create(geometry, animate));
                }

                #endregion
            }
        }
        private void Update_SparklyAngles(double elapsedTime)
        {
            if (_sparklies != null)
            {
                foreach (var sparkly in _sparklies)
                {
                    sparkly.Item2.Tick(elapsedTime);
                }
            }
        }

        #endregion
        #region IGivesDamage Members

        public WeaponDamage CalculateDamage(MaterialCollision[] collisions)
        {
            if (!_isFullySetUp || collisions.Length == 0)
            {
                return null;
            }

            double damangeMult = GetDamageMultiplier();
            if (Math3D.IsNearZero(damangeMult))
            {
                return null;
            }

            var avgCollision = MaterialCollision.GetAverageCollision(collisions, _bot.PhysicsBody);

            //TODO: See if this position is along the ramming direction

            double speed = avgCollision.Item2 / 10;     // a speed of 10 is considered an average impact speed

            double damage = speed * damangeMult;

            WeaponDamage retVal = new WeaponDamage(avgCollision.Item1, damage);

            // The act of hitting something needs to stop the ram, otherwise, they just keep pushing against the item
            // and continue to do damage
            StopRamming();

            return retVal;
        }

        #endregion

        #region Public Properties

        public RamWeaponDNA DNA
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void SetWPFStuff(FrameworkElement mouseSource, Canvas canvas)
        {
            _isPlayerRam = true;

            _mouseSource = mouseSource;

#if SHOWMOUSE
            _canvas = canvas;

            _orthBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("50808080"));

            _lines = new List<Line[]>();
#endif

            FinishSettingUp();

            _mouseSource.MouseMove += MouseSource_MouseMove;
        }
        public void SetAIStuff(AIMousePlate plate)
        {
            _isPlayerRam = false;

            _aiPlate = plate;

            FinishSettingUp();
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_BodyMoved(object sender, EventArgs e)
        {
            if (!_isVisualAdded)
            {
                return;
            }

            // Graphics need to be updated here.  If I wait until world update, this graphic will be one frame
            // behind, which is very noticable when the bot has a high velocity

            double damageMult = GetDamageMultiplier();

            Vector3D velocity = _bot.VelocityWorld;
            var dna = this.DNA;

            // Light size
            double lightSize = UtilityCore.GetScaledValue_Capped(0, _bot.Radius * 4, 0, dna.DamageMult * 20, damageMult);
            UtilityWPF.SetAttenuation(_light, lightSize, .1d);
            _light.Color = Color.FromArgb(Convert.ToByte(UtilityCore.GetScaledValue_Capped(0, 192, 0, dna.DamageMult * 10, damageMult)), 255, 0, 0);

            // Scale
            double scale;
            if (damageMult < dna.DamageMult * 4)
            {
                scale = 0;
            }
            else
            {
                scale = UtilityCore.GetScaledValue_Capped(0, _bot.Radius, dna.DamageMult * 4, dna.DamageMult * 25, damageMult);
            }
            _scale.ScaleX = scale;
            _scale.ScaleY = scale;
            _scale.ScaleZ = scale;

            // Translate
            Point3D position = _bot.PositionWorld + (velocity.ToUnit(false) * (_bot.Radius * UtilityCore.GetScaledValue_Capped(1.05, 1.3, 0, dna.DamageMult * 25, damageMult)));

            _translate.OffsetX = position.X;
            _translate.OffsetY = position.Y;
            _translate.OffsetZ = position.Z;

            // Rotate
            Vector3D xAxis = new Vector3D(1, 0, 0);

            Vector3D axis = Vector3D.CrossProduct(xAxis, velocity);
            double angle = Vector3D.AngleBetween(xAxis, velocity);

            if (Math3D.IsNearZero(axis) || Math3D.IsNearZero(angle))
            {
                _rotate.Quaternion = Quaternion.Identity;
            }
            else
            {
                _rotate.Quaternion = new Quaternion(axis, angle);
            }
        }

        private void MouseSource_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _currentMousePoint = e.GetPosition(_mouseSource);
        }

        #endregion

        #region Private Methods

        private void FinishSettingUp()
        {
            _pointHistory = new List<Point>();
            _currentMousePoint = null;

            _ramDirHistory = new List<Vector>();

            _isFullySetUp = true;
        }

        private void StopRamming()
        {
            _currentRamDirection = null;

            _pointHistory.Clear();
            _ramDirHistory.Clear();

            if (_isVisualAdded)
            {
                foreach (var sparkly in _sparklies)
                {
                    _modelGroup.Children.Remove(sparkly.Item1);
                }
                _sparklies.Clear();

                if (_viewport != null)
                {
                    _viewport.Children.Remove(_visual);
                }
                _isVisualAdded = false;
            }

            _bot.DraggingBot.MaxVelocity = _bot.DNAPartial.DraggingMaxVelocity.Value;
            _bot.DraggingBot.Multiplier = _bot.DNAPartial.DraggingMultiplier.Value;

#if SHOWMOUSE
            foreach (Line line in _lines.SelectMany(o => o))
            {
                _canvas.Children.Remove(line);
            }
            _lines.Clear();

            if (_curDirectionLine != null)
            {
                _canvas.Children.Remove(_curDirectionLine);
                _curDirectionLine = null;
            }
#endif
        }

        private static Vector? GetAvgDirection(List<Vector> directions)
        {
            const int SAMPLESIZE = 3;

            if (directions.Count < SAMPLESIZE)
            {
                return null;
            }

            double x = 0, y = 0;

            for (int cntr = directions.Count - SAMPLESIZE; cntr < directions.Count; cntr++)
            {
                x += directions[cntr].X;
                y += directions[cntr].Y;
            }

            return new Vector(x / SAMPLESIZE, y / SAMPLESIZE);
        }

        private void CreateVisual()
        {
            _modelGroup = new Model3DGroup();

            #region Light

            Color lightColor = UtilityWPF.ColorFromHex("FF0000");
            _light = new PointLight(lightColor, new Point3D(0, 0, 0));
            UtilityWPF.SetAttenuation(_light, _bot.Radius * .01, .1d);

            _modelGroup.Children.Add(_light);

            #endregion

            ////TODO: Don't just make one graphic.  Make sprites, a light - maybe like a fountain
            //DiffuseMaterial material = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40808080")));

            //GeometryModel3D geometry = new GeometryModel3D();
            //geometry.Material = material;
            //geometry.BackMaterial = material;
            ////geometry.Geometry = UtilityWPF.GetSphere(2, 1);
            //geometry.Geometry = UtilityWPF.GetCylinder_AlongX(10, 1, .75);

            _scale = new ScaleTransform3D(1, 1, 1);
            //geometry.Transform = _scale;

            //_modelGroup.Children.Add(geometry);

            Transform3DGroup transform = new Transform3DGroup();

            _rotate = new QuaternionRotation3D(Quaternion.Identity);
            transform.Children.Add(new RotateTransform3D(_rotate));

            _translate = new TranslateTransform3D(0, 0, 0);
            transform.Children.Add(_translate);

            _modelGroup.Transform = transform;

            // Visual
            _visual = new ModelVisual3D()
            {
                Content = _modelGroup
            };

            _bot.PhysicsBody.BodyMoved += PhysicsBody_BodyMoved;
        }

        private static MaterialGroup GetSparklyMaterial()
        {
            MaterialGroup retVal = new MaterialGroup();

            retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0FF6060"))));
            retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80FFFFFF")), 3));
            retVal.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40004000"))));

            return retVal;
        }

        private double GetDamageMultiplier()
        {
            if (_ramDirHistory == null)
            {
                return 0;
            }
            else
            {
                //TODO: Use something more complex than just the amount of time charging
                return _ramDirHistory.Count * this.DNA.DamageMult;
            }
        }
        private double GetAccelerationMultiplier()
        {
            if (_ramDirHistory == null)
            {
                return 1d;
            }
            else
            {
                //TODO: Use something more complex than just the amount of time charging
                return 1d + ((_ramDirHistory.Count * .04d) * this.DNA.AccelerationMult);        // increase acceleration by 4% per count
            }
        }

        private Point? GetCurrentPoint()
        {
            Point? retVal = null;

            if (_isPlayerRam)
            {
                // Grab the point that was set in the latest mouse move event
                retVal = _currentMousePoint;
            }
            else
            {
                if (_aiPlate != null)
                {
                    // Grab the point from the ai plate
                    retVal = _aiPlate.CurrentPoint2D;
                }
            }

            return retVal;
        }

#if SHOWMOUSE
        private void AddLineVisual(Point from, Point to)
        {
        #region Line

            Line line1 = new Line()
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2d,
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y
            };

        #endregion
        #region Orth1

            Vector orth = to - from;
            orth = new Vector(-orth.Y, orth.X).ToUnit(false) * 10;

            Point orthFrom = from - orth;
            Point orthTo = from + orth;

            Line line2a = new Line()
            {
                Stroke = _orthBrush,
                StrokeThickness = 1d,
                X1 = orthFrom.X,
                Y1 = orthFrom.Y,
                X2 = orthTo.X,
                Y2 = orthTo.Y
            };

        #endregion
        #region Orth2

            orthFrom = to - orth;
            orthTo = to + orth;

            Line line2b = new Line()
            {
                Stroke = _orthBrush,
                StrokeThickness = 1d,
                X1 = orthFrom.X,
                Y1 = orthFrom.Y,
                X2 = orthTo.X,
                Y2 = orthTo.Y
            };

        #endregion

            _canvas.Children.Add(line2a);
            _canvas.Children.Add(line2b);
            _canvas.Children.Add(line1);

            _lines.Add(new Line[] { line1, line2a, line2b });
        }
#endif

        #endregion
    }

    #region Class: RamWeaponDNA

    public class RamWeaponDNA
    {
        /// <summary>
        /// When they drag the mouse in a straight line, this is the most they can deviate, and remain in ramming mode
        /// </summary>
        public double MaxAngle;
        /// <summary>
        /// A value of 1 would be a standard amount of damage (it's multiplied times the amount of ramming charge
        /// that they have built up)
        /// </summary>
        public double DamageMult;
        public double AccelerationMult;

        public static RamWeaponDNA Fix(RamWeaponDNA dna)
        {
            if (dna != null)
            {
                return dna;
            }

            RamWeaponDNA retVal = new RamWeaponDNA()
            {
                MaxAngle = StaticRandom.NextPercent(20, .33),
                DamageMult = StaticRandom.NextPercent(1.5, .33),
                AccelerationMult = StaticRandom.NextPercent(1, .25)
            };

            return retVal;
        }
    }

    #endregion
}
