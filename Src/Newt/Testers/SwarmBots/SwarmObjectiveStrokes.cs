using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.SwarmBots
{
    //TODO: Make a class that looks at strokes and available bots.  Then assign strokes to bots.  Otherwise the bots will just
    //mob objectives, and leave others untouched, or go out of their way when other bots are closer
    //
    //It would be cool if this intermediate class isn't some hardcoded controller, but a way for them to communicate with each
    //other.  Somehow grow a group brain
    //
    //The bots talk to the local group, and that group votes on how desirable the objecitve is.  Set up a feedback so an upvoted
    //target will be less desirable.  Also have some persistance so a group commits to an objective and doesn't flake between two
    //
    //Have a group NN that considers current position, velocity, distance from stroke, shape of stroke, items near stroke, etc

    public class SwarmObjectiveStrokes
    {
        #region Class: Stroke

        public class Stroke
        {
            public Stroke(Tuple<Point3D, Vector3D>[] points, double deathTime)
            {
                this.Token = TokenGenerator.NextToken();
                this.Points = points;
                this.DeathTime = deathTime;
            }

            public readonly long Token;
            /// <summary>
            /// Points and Velocities
            /// </summary>
            public readonly Tuple<Point3D, Vector3D>[] Points;

            //TODO: If a swarm has locked on to this stroke, then be lenient with the death time.  Also, if a swarm has gone
            //through the stroke, remove this early
            /// <summary>
            /// How long this stroke should stay around
            /// </summary>
            public readonly double DeathTime;
        }

        #endregion
        #region Class: PointsChanged

        public class PointsChangedArgs
        {
            public PointsChangedArgs(PointChangeType changeType, Tuple<Point3D, Vector3D>[] points, long? strokeToken = null)
            {
                this.ChangeType = changeType;
                this.Points = points;
                this.StrokeToken = strokeToken;
            }

            public readonly PointChangeType ChangeType;
            public readonly Tuple<Point3D, Vector3D>[] Points;
            public readonly long? StrokeToken;
        }

        #endregion
        #region Enum: PointChangeType

        public enum PointChangeType
        {
            AddNewStroke,
            AddingPointsToStroke,
            ConvertStroke_Add,
            ConvertStroke_Remove,
            RemoveStroke_Timeout,
            RemoveStroke_Remove,
            RemoveStroke_Clear,
        }

        #endregion

        #region Events

        public event EventHandler<PointsChangedArgs> PointsChanged = null;

        #endregion

        #region Declaration Section

        private readonly object _lock = new object();

        private readonly WorldClock _clock;
        private readonly double _strokeLife_Seconds;
        private readonly double _smallestSubstrokeSize;

        private readonly List<Stroke> _strokes = new List<Stroke>();

        //TODO: Also store the time of each point.  Speed of line segments should influence velocity
        private readonly List<Tuple<Point3D, Vector3D?>> _addingPoints = new List<Tuple<Point3D, Vector3D?>>();

        #endregion

        #region Constructor

        public SwarmObjectiveStrokes(WorldClock clock, double smallestSubstrokeSize, double strokeLife_Seconds = 4)
        {
            _clock = clock;
            _strokeLife_Seconds = strokeLife_Seconds;
            _smallestSubstrokeSize = smallestSubstrokeSize;
        }

        #endregion

        #region Public Methods

        public void AddPointToStroke(Point3D point, Vector3D? velocity = null)
        {
            // Add point
            lock (_lock)
            {
                _addingPoints.Add(Tuple.Create(point, velocity));
            }

            // Raise event
            if (this.PointsChanged != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.AddingPointsToStroke, new[] { Tuple.Create(point, velocity ?? new Vector3D(0, 0, 0)) });
                this.PointsChanged(this, args);
            }
        }
        public void StopStroke()
        {
            Tuple<Point3D, Vector3D?>[] fromPoints;
            Tuple<Point3D, Vector3D>[] toPoints;
            long token;

            lock (_lock)
            {
                if (_addingPoints.Count == 0)
                {
                    return;
                }

                // Build refined stroke
                fromPoints = _addingPoints.ToArray();
                toPoints = BuildStroke(fromPoints, _smallestSubstrokeSize);

                _addingPoints.Clear();
                _strokes.Add(new Stroke(toPoints, GetStopTime()));
                token = _strokes[_strokes.Count - 1].Token;
            }

            // Raise events
            if (this.PointsChanged != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.ConvertStroke_Remove, fromPoints.Select(o => Tuple.Create(o.Item1, o.Item2 ?? new Vector3D(0, 0, 0))).ToArray());
                this.PointsChanged(this, args);

                args = new PointsChangedArgs(PointChangeType.ConvertStroke_Add, toPoints, token);
                this.PointsChanged(this, args);
            }
        }

        public void AddStroke(IEnumerable<Point3D> points)
        {
            Tuple<Point3D, Vector3D>[] eventPoints;
            long token;

            // Add it
            lock (_lock)
            {
                eventPoints = BuildStroke(points.ToArray(), _smallestSubstrokeSize);
                _strokes.Add(new Stroke(eventPoints, GetStopTime()));

                token = _strokes[_strokes.Count - 1].Token;
            }

            // Raise Event
            if (this.PointsChanged != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.AddNewStroke, eventPoints, token);
                this.PointsChanged(this, args);
            }
        }

        public void RemoveStroke(long token)
        {
            Tuple<Point3D, Vector3D>[] eventPoints = null;

            lock (_lock)
            {
                // Find it
                for (int cntr = 0; cntr < _strokes.Count; cntr++)
                {
                    if (_strokes[cntr].Token == token)
                    {
                        // Remove it
                        eventPoints = _strokes[cntr].Points;
                        _strokes.RemoveAt(cntr);
                        break;
                    }
                }
            }

            // Raise Event
            if (this.PointsChanged != null && eventPoints != null)
            {
                PointsChangedArgs args = new PointsChangedArgs(PointChangeType.RemoveStroke_Remove, eventPoints, token);
                this.PointsChanged(this, args);
            }
        }

        public void Clear()
        {
            var eventPoints = new List<Tuple<Tuple<Point3D, Vector3D>[], long?>>();

            lock (_lock)
            {
                // Remove pre points
                if (_addingPoints.Count > 0)
                {
                    eventPoints.Add(Tuple.Create(_addingPoints.Select(o => Tuple.Create(o.Item1, o.Item2 ?? new Vector3D(0, 0, 0))).ToArray(), (long?)null));
                    _addingPoints.Clear();
                }

                // Remove strokes
                while (_strokes.Count > 0)
                {
                    eventPoints.Add(Tuple.Create(_strokes[0].Points, (long?)_strokes[0].Token));
                    _strokes.RemoveAt(0);
                }
            }

            // Raise Event
            if (this.PointsChanged != null && eventPoints.Count > 0)
            {
                foreach (var points in eventPoints)
                {
                    PointsChangedArgs args = new PointsChangedArgs(PointChangeType.RemoveStroke_Clear, points.Item1, points.Item2);
                    this.PointsChanged(this, args);
                }
            }
        }

        public void Tick()
        {
            var eventPoints = new List<Tuple<Tuple<Point3D, Vector3D>[], long>>();

            lock (_lock)
            {
                double currentTime = _clock.CurrentTime;

                int index = 0;
                while (index < _strokes.Count)
                {
                    if (currentTime > _strokes[index].DeathTime)
                    {
                        // Remove old stroke
                        eventPoints.Add(Tuple.Create(_strokes[index].Points, _strokes[index].Token));
                        _strokes.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            // Raise Events
            if (this.PointsChanged != null && eventPoints.Count > 0)
            {
                foreach (var points in eventPoints)
                {
                    PointsChangedArgs args = new PointsChangedArgs(PointChangeType.RemoveStroke_Timeout, points.Item1, points.Item2);
                    this.PointsChanged(this, args);
                }
            }
        }

        /// <summary>
        /// This returns the nearest stroke, and the distance from it
        /// </summary>
        public Tuple<Stroke, double> GetNearestStroke(Point3D point, double searchRadius)
        {
            // Only need to look for the first point
            lock (_lock)
            {
                double searchSquared = searchRadius * searchRadius;

                Stroke retVal = null;
                double distanceSquared = double.MaxValue;

                foreach (Stroke stroke in _strokes)
                {
                    double dist = (stroke.Points[0].Item1 - point).LengthSquared;
                    if (dist <= searchSquared && dist < distanceSquared)
                    {
                        retVal = stroke;
                        distanceSquared = dist;
                    }
                }

                if (retVal == null)
                {
                    return null;
                }
                else
                {
                    return Tuple.Create(retVal, Math.Sqrt(distanceSquared));
                }
            }
        }

        public static Tuple<Point3D, Vector3D>[] BuildStroke(Point3D[] points, double smallestSubstrokeSize)
        {
            return BuildStroke(points.
                Select(o => new Tuple<Point3D, Vector3D?>(o, null)).ToArray(),
                smallestSubstrokeSize);
        }
        public static Tuple<Point3D, Vector3D>[] BuildStroke_ORIG(Tuple<Point3D, Vector3D?>[] points, double smallestSubstrokeSize)
        {
            //TODO: Apply a bezier.  Try to reduce the number of points (only need points in direction changes)
            //If nulls are passed in, figure out a good velocity

            return points.
                Select(o => Tuple.Create(o.Item1, o.Item2 ?? new Vector3D(0, 0, 0))).
                ToArray();
        }
        public static Tuple<Point3D, Vector3D>[] BuildStroke(Tuple<Point3D, Vector3D?>[] points, double smallestSubstrokeSize)
        {
            //TODO: Use the velocities that are passed in
            //TODO: Look for the defining points of the curve (points where radius of curve is smallest, also inflection points)
            //TODO: Try to always include the final point (unless it's too close to the beginning)

            if (points == null || points.Length == 0)
            {
                return new Tuple<Point3D, Vector3D>[0];
            }

            double smallestSqr = smallestSubstrokeSize * smallestSubstrokeSize;

            #region determine points

            List<Point3D> finalPoints = new List<Point3D>();

            finalPoints.Add(points[0].Item1);

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                if ((points[cntr].Item1 - finalPoints[finalPoints.Count - 1]).LengthSquared > smallestSqr)
                {
                    finalPoints.Add(points[cntr].Item1);
                }
            }

            #endregion
            #region determine velocities

            Tuple<Point3D, Vector3D>[] retVal = new Tuple<Point3D, Vector3D>[finalPoints.Count];

            for (int cntr = 0; cntr < finalPoints.Count - 1; cntr++)
            {
                retVal[cntr] = Tuple.Create(finalPoints[cntr], finalPoints[cntr + 1] - finalPoints[cntr]);
            }

            if (retVal.Length == 1)
            {
                if (points.Length == 1)
                {
                    // Only one point, so don't bother with a velocity 
                    retVal[0] = Tuple.Create(finalPoints[0], new Vector3D(0, 0, 0));
                }
                else
                {
                    // The last point didn't make it to the final list, but it can still be used for velocity
                    retVal[0] = Tuple.Create(finalPoints[0], points[points.Length - 1].Item1 - finalPoints[0]);
                }
            }
            else
            {
                // The last point should just continue the velocity
                //TODO: May want to try harder to calculate the radius of the curve, and let that influence the velocity (or just
                //average the previous couple points)
                retVal[retVal.Length - 1] = Tuple.Create(finalPoints[retVal.Length - 1], retVal[retVal.Length - 2].Item2);
            }

            #endregion

            return retVal;
        }

        #endregion

        #region Private Methods

        private double GetStopTime()
        {
            //TODO: May want a longer life if it's an elaborate stroke, or far from others, or if there are a lot of existing strokes
            return _clock.CurrentTime + _strokeLife_Seconds;
        }

        #endregion
    }
}
