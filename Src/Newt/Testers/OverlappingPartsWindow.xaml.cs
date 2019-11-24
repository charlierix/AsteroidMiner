using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers
{
    public partial class OverlappingPartsWindow : Window
    {
        #region interface: IPartSolver

        private interface IPartSolver
        {
            bool IsFinished
            {
                get;
            }

            //Tuple<Point3D, Vector3D>[] IntersectionLines
            //{
            //    get;
            //}

            int StepCount
            {
                get;
            }

            void Update();

            CollisionHull[] GetCollisionHulls();
        }

        #endregion
        #region class: PartSolver1

        /// <summary>
        /// This one only does translation.  No rotations
        /// </summary>
        private class PartSolver1 : IPartSolver
        {
            #region Declaration Section

            private World _world = null;

            // All of these arrays are the same size
            private PartBase[] _parts = null;
            private bool[] _hasMoved = null;
            private CollisionHull[] _hulls = null;
            private Tuple<Point3D, Quaternion>[] _initialPositions = null;

            #endregion

            #region Constructor

            public PartSolver1(World world, PartBase[] parts)
            {
                _world = world;

                _parts = parts;
                _hasMoved = new bool[parts.Length];		// defaults to false
                _hulls = parts.Select(o => o.CreateCollisionHull(world)).ToArray();
                _initialPositions = parts.Select(o => Tuple.Create(o.Position, o.Orientation)).ToArray();
            }

            #endregion

            #region Public Properties

            private double _ignoreDepthPercent = .01d;
            public double IgnoreDepthPercent
            {
                get
                {
                    return _ignoreDepthPercent;
                }
                set
                {
                    _ignoreDepthPercent = value;
                }
            }

            private double _movePerStepPercent = 1d;
            public double MovePerStepPercent
            {
                get
                {
                    return _movePerStepPercent;
                }
                set
                {
                    _movePerStepPercent = value;
                }
            }

            private bool _isFinished = false;
            public bool IsFinished
            {
                get
                {
                    return _isFinished;
                }
            }

            /// <summary>
            /// This is only exposed for debugging.  It is a list of intersections during the last call to Update
            /// </summary>
            public Tuple<Point3D, Vector3D>[] IntersectionLines
            {
                get;
                private set;
            }

            private int _stepCount = 0;
            public int StepCount
            {
                get
                {
                    return _stepCount;
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// This jostles/separates the parts.  After calling this, IsFinished may become true
            /// </summary>
            public void Update()
            {
                if (_isFinished)
                {
                    return;
                }

                var intersections = GetIntersections();
                if (intersections.Length == 0)
                {
                    _isFinished = true;
                    this.IntersectionLines = null;
                    return;
                }

                this.IntersectionLines = intersections.SelectMany(o => o.Item3).Select(o => Tuple.Create(o.ContactPoint, o.Normal.ToUnit() * o.PenetrationDistance)).ToArray();

                DoStep(intersections);

                _stepCount++;
            }

            /// <summary>
            /// This returns collision hulls that are offset where the parts are (only recreates hulls for parts that have moved)
            /// </summary>
            /// <remarks>
            /// NOTE: This method wasn't optimized to be called multiple times
            /// </remarks>
            public CollisionHull[] GetCollisionHulls()
            {
                CollisionHull[] retVal = new CollisionHull[_parts.Length];

                for (int cntr = 0; cntr < _parts.Length; cntr++)
                {
                    if (_hasMoved[cntr])
                    {
                        retVal[cntr] = _parts[cntr].CreateCollisionHull(_world);
                    }
                    else
                    {
                        retVal[cntr] = _hulls[cntr];
                    }
                }

                return retVal;
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// This finds intersections between all the hulls
            /// </summary>
            private Tuple<int, int, CollisionHull.IntersectionPoint[]>[] GetIntersections()
            {
                List<Tuple<int, int, CollisionHull.IntersectionPoint[]>> retVal = new List<Tuple<int, int, CollisionHull.IntersectionPoint[]>>();

                SortedList<int, double> sizes = new SortedList<int, double>();

                // Compare each hull to the others
                for (int outer = 0; outer < _hulls.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < _hulls.Length; inner++)
                    {



                        CollisionHull.IntersectionPoint[] points = _hulls[outer].GetIntersectingPoints_HullToHull(100, _hulls[inner], 0, GetIntersections_Transform(outer), GetIntersections_Transform(inner));



                        //CollisionHull.IntersectionPoint[] points = _hulls[outer].GetIntersectingPoints_HullToHull(100, _hulls[inner], 0);
                        //CollisionHull.IntersectionPoint[] points = _hulls[inner].GetIntersectingPoints_HullToHull(100, _hulls[outer], 0);		// the normals seem to be the same when colliding from the other direction


                        if (points != null && points.Length > 0)
                        {
                            double sumSize = GetIntersections_Size(sizes, _hulls, outer) + GetIntersections_Size(sizes, _hulls, inner);
                            double minSize = sumSize * _ignoreDepthPercent;

                            // Filter out the shallow penetrations
                            //TODO: May need to add the lost distance to the remaining intersections
                            points = points.Where(o => o.PenetrationDistance > minSize).ToArray();

                            if (points != null && points.Length > 0)
                            {
                                retVal.Add(Tuple.Create(outer, inner, points));
                            }
                        }
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }
            private Transform3D GetIntersections_Transform(int index)
            {
                if (!_hasMoved[index])
                {
                    return null;
                }

                Transform3DGroup retVal = new Transform3DGroup();

                retVal.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(_initialPositions[index].Item2, _parts[index].Orientation))));
                retVal.Children.Add(new TranslateTransform3D(_parts[index].Position - _initialPositions[index].Item1));

                return retVal;
            }
            private static double GetIntersections_Size(SortedList<int, double> sizes, CollisionHull[] hulls, int index)
            {
                if (sizes.ContainsKey(index))
                {
                    return sizes[index];
                }

                //NOTE: The returned AABB will be bigger than the actual object
                Point3D min, max;
                hulls[index].CalculateAproximateAABB(out min, out max);

                // Just get the average size of this box
                double size = ((max.X - min.X) + (max.Y - min.Y) + (max.Z - min.Z)) / 3d;

                sizes.Add(index, size);

                return size;
            }

            private void DoStep(Tuple<int, int, CollisionHull.IntersectionPoint[]>[] intersections)
            {
                SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves = new SortedList<int, List<Tuple<Vector3D?, Quaternion?>>>();

                // Shoot through all the part pairs
                foreach (var intersection in intersections)
                {
                    double mass1 = _parts[intersection.Item1].TotalMass;
                    double mass2 = _parts[intersection.Item2].TotalMass;
                    double totalMass = mass1 + mass2;

                    double sumPenetration = intersection.Item3.Sum(o => o.PenetrationDistance);
                    double avgPenetration = sumPenetration / Convert.ToDouble(intersection.Item3.Length);

                    // Shoot through the intersecting points between these two parts
                    foreach (var intersectPoint in intersection.Item3)
                    {
                        // The sum of scaledDistance needs to add up to avgPenetration
                        double percentDistance = intersectPoint.PenetrationDistance / sumPenetration;
                        double scaledDistance = avgPenetration * percentDistance;

                        // May not want to move the full distance in one step
                        scaledDistance *= _movePerStepPercent;

                        double distance1 = scaledDistance * ((totalMass - mass1) / totalMass);
                        double distance2 = scaledDistance * ((totalMass - mass2) / totalMass);

                        // Normal is pointing away from the first and toward the second
                        //TODO: Maybe not always
                        Vector3D normalUnit = intersectPoint.Normal.ToUnit();

                        DoStep_AddForce(moves, intersection.Item1, normalUnit * (-1d * distance1), null);
                        DoStep_AddForce(moves, intersection.Item2, normalUnit * distance2, null);
                    }
                }

                // Apply the movements
                DoStep_Move(_parts, moves);

                // Remember which parts were modified
                foreach (int index in moves.Keys)
                {
                    _hasMoved[index] = true;
                }
            }

            private static void DoStep_AddForce(SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves, int index, Vector3D? translation, Quaternion? rotation)
            {
                if (!moves.ContainsKey(index))
                {
                    moves.Add(index, new List<Tuple<Vector3D?, Quaternion?>>());
                }

                moves[index].Add(Tuple.Create(translation, rotation));
            }
            private static void DoStep_Move(PartBase[] parts, SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves)
            {
                foreach (int partIndex in moves.Keys)
                {
                    foreach (var move in moves[partIndex])
                    {
                        if (move.Item1 != null)
                        {
                            parts[partIndex].Position += move.Item1.Value;
                        }

                        if (move.Item2 != null)
                        {
                            parts[partIndex].Orientation = parts[partIndex].Orientation.RotateBy(move.Item2.Value);
                        }
                    }
                }
            }

            #endregion
        }

        #endregion
        #region class: PartSolver2

        /// <summary>
        /// This one only does translation.  No rotations
        /// </summary>
        private class PartSolver2 : IPartSolver
        {
            #region class: DebugPoints

            public class DebugPoints
            {
                public DebugPoints(Point3D contact, Vector3D translation1, Vector3D torque1, Vector3D offset1, Vector3D translation2, Vector3D torque2, Vector3D offset2)
                {
                    this.Contact = contact;
                    this.Translation1 = translation1;
                    this.Torque1 = torque1;
                    this.Offset1 = offset1;
                    this.Translation2 = translation2;
                    this.Torque2 = torque2;
                    this.Offset2 = offset2;
                }

                public readonly Point3D Contact;

                public readonly Vector3D Translation1;
                public readonly Vector3D Torque1;
                public readonly Vector3D Offset1;

                public readonly Vector3D Translation2;
                public readonly Vector3D Torque2;
                public readonly Vector3D Offset2;
            }

            #endregion

            #region class: Intersection

            private class Intersection
            {
                public Intersection(int index1, int index2, double avgSize1, double avgSize2, CollisionHull.IntersectionPoint[] intersections)
                {
                    this.Index1 = index1;
                    this.Index2 = index2;
                    this.AvgSize1 = avgSize1;
                    this.AvgSize2 = avgSize2;
                    this.Intersections = intersections;
                }

                public readonly int Index1;
                public readonly int Index2;

                public readonly double AvgSize1;
                public readonly double AvgSize2;

                public readonly CollisionHull.IntersectionPoint[] Intersections;
            }

            #endregion

            #region Declaration Section

            private World _world = null;

            // All of these arrays are the same size
            private PartBase[] _parts = null;
            private bool[] _hasMoved = null;
            private CollisionHull[] _hulls = null;
            private Tuple<Point3D, Quaternion>[] _initialPositions = null;

            private readonly bool _useTransforms;

            #endregion

            #region Constructor

            public PartSolver2(World world, PartBase[] parts, bool useTransforms)
            {
                _world = world;
                _useTransforms = useTransforms;

                _parts = parts;
                _hasMoved = new bool[parts.Length];		// defaults to false
                _hulls = parts.Select(o => o.CreateCollisionHull(world)).ToArray();
                _initialPositions = parts.Select(o => Tuple.Create(o.Position, o.Orientation)).ToArray();
            }

            #endregion

            #region Public Properties

            private double _ignoreDepthPercent = .01d;
            public double IgnoreDepthPercent
            {
                get
                {
                    return _ignoreDepthPercent;
                }
                set
                {
                    _ignoreDepthPercent = value;
                }
            }

            private double _movePerStepPercent = 1d;
            public double MovePerStepPercent
            {
                get
                {
                    return _movePerStepPercent;
                }
                set
                {
                    _movePerStepPercent = value;
                }
            }

            private bool _doRotations = true;
            public bool DoRotations
            {
                get
                {
                    return _doRotations;
                }
                set
                {
                    _doRotations = value;
                }
            }

            private bool _isFinished = false;
            public bool IsFinished
            {
                get
                {
                    return _isFinished;
                }
            }

            /// <summary>
            /// This is only exposed for debugging.  It is a list of intersections during the last call to Update
            /// </summary>
            public DebugPoints[] IntersectionLines
            {
                get;
                private set;
            }

            private int _stepCount = 0;
            public int StepCount
            {
                get
                {
                    return _stepCount;
                }
            }

            private int _maxSteps = 50;
            public int MaxSteps
            {
                get
                {
                    return _maxSteps;
                }
                set
                {
                    _maxSteps = value;
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// This jostles/separates the parts.  After calling this, IsFinished may become true
            /// </summary>
            public void Update()
            {
                if (_isFinished)
                {
                    return;
                }

                Intersection[] intersections = GetIntersections();

                if (intersections.Length == 0)
                {
                    _isFinished = true;
                    this.IntersectionLines = null;
                    return;
                }

                this.IntersectionLines = DoStep(intersections);

                _stepCount++;

                if (_stepCount > _maxSteps)
                {
                    // Stop prematurely
                    _isFinished = true;
                }
            }

            /// <summary>
            /// This returns collision hulls that are offset where the parts are (only recreates hulls for parts that have moved)
            /// </summary>
            public CollisionHull[] GetCollisionHulls()
            {
                CollisionHull[] retVal = new CollisionHull[_parts.Length];

                for (int cntr = 0; cntr < _parts.Length; cntr++)
                {
                    if (_hasMoved[cntr])
                    {
                        retVal[cntr].Dispose();
                        retVal[cntr] = _parts[cntr].CreateCollisionHull(_world);
                        _hasMoved[cntr] = false;
                    }
                    else
                    {
                        retVal[cntr] = _hulls[cntr];
                    }
                }

                return retVal;
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// This finds intersections between all the hulls
            /// </summary>
            private Intersection[] GetIntersections()
            {
                List<Intersection> retVal = new List<Intersection>();

                // Compare each hull to the others
                for (int outer = 0; outer < _hulls.Length - 1; outer++)
                {
                    double? sizeOuter = null;

                    for (int inner = outer + 1; inner < _hulls.Length; inner++)
                    {
                        CollisionHull.IntersectionPoint[] points;
                        if (_useTransforms)
                        {
                            points = _hulls[outer].GetIntersectingPoints_HullToHull(100, _hulls[inner], 0, GetIntersections_Transform(outer), GetIntersections_Transform(inner));
                        }
                        else
                        {
                            if (_hasMoved[outer])
                            {
                                _hulls[outer].Dispose();
                                _hulls[outer] = _parts[outer].CreateCollisionHull(_world);
                                _hasMoved[outer] = false;
                            }

                            if (_hasMoved[inner])
                            {
                                _hulls[inner].Dispose();
                                _hulls[inner] = _parts[inner].CreateCollisionHull(_world);
                                _hasMoved[inner] = false;
                            }

                            points = _hulls[outer].GetIntersectingPoints_HullToHull(100, _hulls[inner], 0);
                        }

                        if (points != null && points.Length > 0)
                        {
                            sizeOuter = sizeOuter ?? (_parts[outer].ScaleActual.X + _parts[outer].ScaleActual.X + _parts[outer].ScaleActual.X) / 3d;
                            double sizeInner = (_parts[inner].ScaleActual.X + _parts[inner].ScaleActual.X + _parts[inner].ScaleActual.X) / 3d;

                            double sumSize = sizeOuter.Value + sizeInner;
                            double minSize = sumSize * _ignoreDepthPercent;

                            // Filter out the shallow penetrations
                            //TODO: May need to add the lost distance to the remaining intersections
                            points = points.Where(o => o.PenetrationDistance > minSize).ToArray();

                            if (points != null && points.Length > 0)
                            {
                                retVal.Add(new Intersection(outer, inner, sizeOuter.Value, sizeInner, points));
                            }
                        }
                    }
                }

                // Exit Function
                return retVal.ToArray();
            }
            private Transform3D GetIntersections_Transform(int index)
            {
                if (!_hasMoved[index])
                {
                    return null;
                }

                Transform3DGroup retVal = new Transform3DGroup();

                Quaternion delta = Math3D.GetRotation(_initialPositions[index].Item2, _parts[index].Orientation);
                delta.Invert();
                retVal.Children.Add(new RotateTransform3D(new QuaternionRotation3D(delta)));
                retVal.Children.Add(new TranslateTransform3D(_parts[index].Position - _initialPositions[index].Item1));

                return retVal;
            }

            private DebugPoints[] DoStep(Intersection[] intersections)
            {
                List<DebugPoints> retVal = new List<DebugPoints>();
                SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves = new SortedList<int, List<Tuple<Vector3D?, Quaternion?>>>();

                double crazyScale = 1d;
                if (intersections.Length > _parts.Length)
                {
                    // If there are lots of parts intersecting each other at once, this will move parts too far (because it does a scan of all intersections,
                    // then moves the parts using the sum of all intersections)
                    // This is a crude attempt to soften that effect
                    crazyScale = Convert.ToDouble(_parts.Length) / Convert.ToDouble(intersections.Length);
                }

                // Shoot through all the part pairs
                foreach (var intersection in intersections)
                {
                    double mass1 = _parts[intersection.Index1].TotalMass;
                    double mass2 = _parts[intersection.Index2].TotalMass;
                    double totalMass = mass1 + mass2;

                    double sumPenetration = intersection.Intersections.Sum(o => o.PenetrationDistance);
                    double avgPenetration = sumPenetration / Convert.ToDouble(intersection.Intersections.Length);

                    Vector3D direction = (_parts[intersection.Index2].Position - _parts[intersection.Index1].Position).ToUnit();

                    double sizeScale = _movePerStepPercent * (1d / Convert.ToDouble(intersection.Intersections.Length));

                    // Shoot through the intersecting points between these two parts
                    foreach (var intersectPoint in intersection.Intersections)
                    {
                        // The sum of scaledDistance needs to add up to avgPenetration
                        double percentDistance = intersectPoint.PenetrationDistance / sumPenetration;
                        double scaledDistance = avgPenetration * percentDistance;

                        // May not want to move the full distance in one step
                        scaledDistance *= _movePerStepPercent * crazyScale;

                        double distance1 = scaledDistance * ((totalMass - mass1) / totalMass);
                        double distance2 = scaledDistance * ((totalMass - mass2) / totalMass);

                        Vector3D translation1, torque1;
                        Vector3D offset1 = intersectPoint.ContactPoint - _parts[intersection.Index1].Position;
                        Math3D.SplitForceIntoTranslationAndTorque(out translation1, out torque1, offset1, direction * (-1d * distance1));
                        DoStep_AddForce(moves, intersection.Index1, translation1, this.DoRotations ? DoStep_Rotate(torque1, intersection.AvgSize1, sizeScale) : null);		// don't use the full size, or the rotation won't even be noticable

                        Vector3D translation2, torque2;
                        Vector3D offset2 = intersectPoint.ContactPoint - _parts[intersection.Index2].Position;
                        Math3D.SplitForceIntoTranslationAndTorque(out translation2, out torque2, offset2, direction * distance2);
                        DoStep_AddForce(moves, intersection.Index2, translation2, this.DoRotations ? DoStep_Rotate(torque2, intersection.AvgSize2, sizeScale) : null);

                        // Debug visuals
                        retVal.Add(new DebugPoints(intersectPoint.ContactPoint, translation1, torque1, offset1, translation2, torque2, offset2));
                    }
                }

                // Apply the movements
                DoStep_Move(_parts, moves);

                // Remember which parts were modified
                foreach (int index in moves.Keys)
                {
                    _hasMoved[index] = true;
                }

                // Exit Function
                return retVal.ToArray();
            }
            private DebugPoints[] DoStep_OLD(Intersection[] intersections)
            {
                //NOTE: If there are lots of parts intersecting each other at once, this will move parts too far (because it does a scan of all intersections,
                //then moves the parts using the sum of all intersections)
                //
                // A nicer, but more expensive approach would be

                List<DebugPoints> retVal = new List<DebugPoints>();
                SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves = new SortedList<int, List<Tuple<Vector3D?, Quaternion?>>>();

                // Shoot through all the part pairs
                foreach (var intersection in intersections)
                {
                    double mass1 = _parts[intersection.Index1].TotalMass;
                    double mass2 = _parts[intersection.Index2].TotalMass;
                    double totalMass = mass1 + mass2;

                    double sumPenetration = intersection.Intersections.Sum(o => o.PenetrationDistance);
                    double avgPenetration = sumPenetration / Convert.ToDouble(intersection.Intersections.Length);

                    Vector3D direction = (_parts[intersection.Index2].Position - _parts[intersection.Index1].Position).ToUnit();

                    double sizeScale = _movePerStepPercent * (1d / Convert.ToDouble(intersection.Intersections.Length));

                    // Shoot through the intersecting points between these two parts
                    foreach (var intersectPoint in intersection.Intersections)
                    {
                        // The sum of scaledDistance needs to add up to avgPenetration
                        double percentDistance = intersectPoint.PenetrationDistance / sumPenetration;
                        double scaledDistance = avgPenetration * percentDistance;

                        // May not want to move the full distance in one step
                        scaledDistance *= _movePerStepPercent;

                        double distance1 = scaledDistance * ((totalMass - mass1) / totalMass);
                        double distance2 = scaledDistance * ((totalMass - mass2) / totalMass);

                        Vector3D translation1, torque1;
                        Vector3D offset1 = intersectPoint.ContactPoint - _parts[intersection.Index1].Position;
                        Math3D.SplitForceIntoTranslationAndTorque(out translation1, out torque1, offset1, direction * (-1d * distance1));
                        DoStep_AddForce(moves, intersection.Index1, translation1, this.DoRotations ? DoStep_Rotate(torque1, intersection.AvgSize1, sizeScale) : null);		// don't use the full size, or the rotation won't even be noticable

                        Vector3D translation2, torque2;
                        Vector3D offset2 = intersectPoint.ContactPoint - _parts[intersection.Index2].Position;
                        Math3D.SplitForceIntoTranslationAndTorque(out translation2, out torque2, offset2, direction * distance2);
                        DoStep_AddForce(moves, intersection.Index2, translation2, this.DoRotations ? DoStep_Rotate(torque2, intersection.AvgSize2, sizeScale) : null);

                        // Debug visuals
                        retVal.Add(new DebugPoints(intersectPoint.ContactPoint, translation1, torque1, offset1, translation2, torque2, offset2));
                    }
                }

                // Apply the movements
                DoStep_Move(_parts, moves);

                // Remember which parts were modified
                foreach (int index in moves.Keys)
                {
                    _hasMoved[index] = true;
                }

                // Exit Function
                return retVal.ToArray();
            }

            private static Quaternion? DoStep_Rotate(Vector3D torque, double size, double penetrationScale)
            {
                const double MAXANGLE = 12d; //22.5d;

                if (Math3D.IsNearZero(torque))
                {
                    return null;
                }

                double length = torque.Length;
                Vector3D axis = torque / length;

                // Since the max torque will be a full penetration out at radius, that will be penetration cross radius.  So the length of that will be
                // roughly (size/2)^2
                //double maxExpected = Math.Pow(size * .33d, 2d);		// make the max size a bit smaller than half, since a max penetration would be really rare
                //double maxExpected = size * size;

                double maxExpected = (size * .5d) * (size * penetrationScale);

                // Make the angle to be some proportion between the torque's length and the average size of the part
                double angle = UtilityCore.GetScaledValue_Capped(0d, MAXANGLE, 0d, maxExpected, length);

                return new Quaternion(axis, angle);
            }

            private static void DoStep_AddForce(SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves, int index, Vector3D? translation, Quaternion? rotation)
            {
                if (!moves.ContainsKey(index))
                {
                    moves.Add(index, new List<Tuple<Vector3D?, Quaternion?>>());
                }

                moves[index].Add(Tuple.Create(translation, rotation));
            }
            private static void DoStep_Move(PartBase[] parts, SortedList<int, List<Tuple<Vector3D?, Quaternion?>>> moves)
            {
                foreach (int partIndex in moves.Keys)
                {
                    foreach (var move in moves[partIndex])
                    {
                        if (move.Item1 != null)
                        {
                            parts[partIndex].Position += move.Item1.Value;
                        }

                        if (move.Item2 != null)
                        {
                            parts[partIndex].Orientation = parts[partIndex].Orientation.RotateBy(move.Item2.Value);
                        }
                    }
                }
            }

            #endregion
        }

        #endregion

        #region class: ItemColors

        private class ItemColors
        {
            public Color BoundryLines = Colors.Silver;
        }

        #endregion

        #region Declaration Section

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = new ItemOptions();
        private ItemColors _colors = new ItemColors();

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private World _world = null;
        private Map _map = null;
        private RadiationField _radiation = null;
        private GravityFieldUniform _gravity = null;

        private MaterialManager _materialManager = null;
        private int _material_Part = -1;
        private int _material_Ship = -1;

        private ScreenSpaceLines3D _boundryLines = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private PartBase[] _parts = null;

        private IPartSolver _solver = null;

        private DispatcherTimer _solverTimer = null;		// using a separate timer than world update to get finer control over the interval

        private List<Visual3D> _initialVisuals = new List<Visual3D>();		// a snapshot of the parts before being pulled apart
        private List<Visual3D> _currentVisuals = new List<Visual3D>();		// the parts as they are being pulled apart
        private List<Visual3D> _debugVisuals = new List<Visual3D>();		// use this for temp lines, dots, etc

        private double _debugOffset = 7d;
        private int _tickCntr = 0;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public OverlappingPartsWindow()
        {
            InitializeComponent();

            _isInitialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.KeyPanScale = 1d;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;

                #region copied from MouseComplete_NoLeft - middle button changed

                TrackBallMapping complexMapping = null;

                // Middle Button
                complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                complexMapping.Add(MouseButton.Middle);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateAroundLookDirection, MouseButton.Middle, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                complexMapping.Add(MouseButton.Middle);
                complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Zoom, MouseButton.Middle, new Key[] { Key.LeftShift, Key.RightShift }));

                //retVal.Add(new TrackBallMapping(CameraMovement.Pan_AutoScroll, MouseButton.Middle));
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Pan, MouseButton.Middle));

                // Left+Right Buttons (emulate middle)
                complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection_AutoScroll);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);

                complexMapping = new TrackBallMapping(CameraMovement.RotateAroundLookDirection);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                _trackball.Mappings.Add(complexMapping);

                complexMapping = new TrackBallMapping(CameraMovement.Zoom_AutoScroll);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);

                complexMapping = new TrackBallMapping(CameraMovement.Zoom);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftShift, Key.RightShift });
                _trackball.Mappings.Add(complexMapping);

                //complexMapping = new TrackBallMapping(CameraMovement.Pan_AutoScroll);
                complexMapping = new TrackBallMapping(CameraMovement.Pan);
                complexMapping.Add(MouseButton.Left);
                complexMapping.Add(MouseButton.Right);
                _trackball.Mappings.Add(complexMapping);

                // Right Button
                complexMapping = new TrackBallMapping(CameraMovement.RotateInPlace_AutoScroll);
                complexMapping.Add(MouseButton.Right);
                complexMapping.Add(new Key[] { Key.LeftCtrl, Key.RightCtrl });
                complexMapping.Add(new Key[] { Key.LeftAlt, Key.RightAlt });
                _trackball.Mappings.Add(complexMapping);
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.RotateInPlace, MouseButton.Right, new Key[] { Key.LeftCtrl, Key.RightCtrl }));

                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit_AutoScroll, MouseButton.Right, new Key[] { Key.LeftAlt, Key.RightAlt }));
                _trackball.Mappings.Add(new TrackBallMapping(CameraMovement.Orbit, MouseButton.Right));

                #endregion

                //_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.Keyboard_ASDW_In));		// let the ship get asdw instead of the camera
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = true;

                #endregion

                #region Init World

                // Set the size of the world to something a bit random (gets boring when it's always the same size)
                double halfSize = 50d;
                _boundryMin = new Point3D(-halfSize, -halfSize, -halfSize);
                _boundryMax = new Point3D(halfSize, halfSize, halfSize);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                var boundryLines = _world.SetCollisionBoundry(_boundryMin, _boundryMax);

                // Draw the lines
                _boundryLines = new ScreenSpaceLines3D(true);
                _boundryLines.Thickness = 1d;
                _boundryLines.Color = _colors.BoundryLines;
                _viewport.Children.Add(_boundryLines);

                foreach (var line in boundryLines.innerLines)
                {
                    _boundryLines.AddLine(line.from, line.to);
                }

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Part
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Part = _materialManager.AddMaterial(material);

                // Ship
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Ship = _materialManager.AddMaterial(material);

                //_materialManager.RegisterCollisionEvent(_material_Ship, _material_Asteroid, Collision_Ship);

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world);
                //_map.SnapshotFequency_Milliseconds = 125;
                //_map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = false;
                //_map.ShouldShowSnapshotLines = true;
                //_map.ShouldSnapshotCentersDrift = false;

                #endregion
                #region Fields

                _radiation = new RadiationField();
                _radiation.AmbientRadiation = 1d;

                _gravity = new GravityFieldUniform();

                #endregion

                _world.UnPause();

                #region Solver Timer

                _solverTimer = new DispatcherTimer();
                _solverTimer.Interval = TimeSpan.FromMilliseconds(trkSimSpeed.Value);
                _solverTimer.Tick += new EventHandler(SolverTimer_Tick);
                _solverTimer.IsEnabled = true;

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
        }
        private void SolverTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_solver == null || _solver.IsFinished)
                {
                    return;
                }

                foreach (Visual3D visual in _currentVisuals)
                {
                    _viewport.Children.Remove(visual);
                }

                _solver.Update();

                DrawParts(_parts, _currentVisuals, new Vector3D(0, 0, 0));

                #region Debug Visuals

                if (_solver is PartSolver1)
                {
                    PartSolver1 solverCast1 = (PartSolver1)_solver;
                    if (solverCast1.IntersectionLines != null)
                    {
                        DrawLines(solverCast1.IntersectionLines, new Vector3D(0, -_debugOffset, 0), UtilityWPF.GetRandomColor(64, 192));
                    }
                }
                else if (_solver is PartSolver2)
                {
                    PartSolver2 solverCast2 = (PartSolver2)_solver;

                    if (solverCast2.IntersectionLines != null)
                    {
                        var lineSets = solverCast2.IntersectionLines.Select(o => new Tuple<Point3D, Vector3D>[] {
                            Tuple.Create(o.Contact, o.Translation1 * 10d),
                            Tuple.Create(o.Contact + (o.Translation1 * 10d), o.Torque1 * 10d),
							//Tuple.Create(o.Contact, o.Offset1 * -1d),
							Tuple.Create(o.Contact, o.Translation2 * 10d),
                            Tuple.Create(o.Contact + (o.Translation2 * 10d), o.Torque2 * 10d),
							//Tuple.Create(o.Contact, o.Offset2 * -1d)
						}).ToArray();

                        Color color = UtilityWPF.GetRandomColor(64, 192);
                        DrawLines(lineSets.SelectMany(o => o).ToArray(), new Vector3D(0, -_debugOffset, 0), color);
                        //DrawLines(lineSets.SelectMany(o => o).ToArray(), new Vector3D(0, 0, 0), color);

                        DrawDots(solverCast2.IntersectionLines.Select(o => o.Contact).ToArray(), new Vector3D(0, -_debugOffset, 0), .05d, color);
                    }
                }

                #endregion

                _tickCntr++;
                lblNumTicks.Text = _tickCntr.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Solver_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                ClearScene();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkSimSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (_solverTimer != null)
                {
                    _solverTimer.Interval = TimeSpan.FromMilliseconds(trkSimSpeed.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClearDebugVisuals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Visual3D visual in _debugVisuals)
                {
                    _viewport.Children.Remove(visual);
                }

                _debugVisuals.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnTwoCubes1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShipPartDNA dnaGrav = new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-.5, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                ShipPartDNA dnaSpin = new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(.5, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                StartScene(new PartBase[] { grav, spin }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTwoCubes2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShipPartDNA dnaGrav = new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-.6, -.1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                ShipPartDNA dnaSpin = new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(.6, .1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                StartScene(new PartBase[] { grav, spin }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTwoCubes3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //PartDNA dnaGrav = new PartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-1.01, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };
                //PartDNA dnaSpin = new PartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(1.01, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 30), Scale = new Vector3D(10, 10, 10) };
                ShipPartDNA dnaGrav = new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = new Point3D(-1.01, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, -1), 15), Scale = new Vector3D(10, 10, 10) };
                ShipPartDNA dnaSpin = new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(1.01, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 45), Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                StartScene(new PartBase[] { grav, spin }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTwoCubesRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShipPartDNA dnaGrav = new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = Math3D.GetRandomVector(2d).ToPoint(), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(10, 10, 10) };
                ShipPartDNA dnaSpin = new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = Math3D.GetRandomVector(2d).ToPoint(), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                StartScene(new PartBase[] { grav, spin }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTwoCubeCylinder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isEnergy = StaticRandom.Next(2) == 0;

                ShipPartDNA dna1 = new ShipPartDNA() { PartType = isEnergy ? EnergyTank.PARTTYPE : FuelTank.PARTTYPE, Position = new Point3D(-.5, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(4, 4, 3) };
                ShipPartDNA dnaSpin = new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = new Point3D(.5, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(10, 10, 10) };

                PartBase cylinder = null;
                if (isEnergy)
                {
                    cylinder = new EnergyTank(_editorOptions, _itemOptions, dna1);
                }
                else
                {
                    cylinder = new FuelTank(_editorOptions, _itemOptions, dna1);
                }
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                StartScene(new PartBase[] { cylinder, spin }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTwoCylinderCylinder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isEnergy1 = StaticRandom.Next(2) == 0;
                bool isEnergy2 = StaticRandom.Next(2) == 0;

                ShipPartDNA dna1 = new ShipPartDNA() { PartType = isEnergy1 ? EnergyTank.PARTTYPE : FuelTank.PARTTYPE, Position = new Point3D(-.5, -.1, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(4, 4, 3) };
                ShipPartDNA dna2 = new ShipPartDNA() { PartType = isEnergy2 ? EnergyTank.PARTTYPE : FuelTank.PARTTYPE, Position = new Point3D(.5, .1, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(4, 4, 3) };

                PartBase cylinder1 = null;
                if (isEnergy1)
                {
                    cylinder1 = new EnergyTank(_editorOptions, _itemOptions, dna1);
                }
                else
                {
                    cylinder1 = new FuelTank(_editorOptions, _itemOptions, dna1);
                }

                PartBase cylinder2 = null;
                if (isEnergy2)
                {
                    cylinder2 = new EnergyTank(_editorOptions, _itemOptions, dna2);
                }
                else
                {
                    cylinder2 = new FuelTank(_editorOptions, _itemOptions, dna2);
                }

                StartScene(new PartBase[] { cylinder1, cylinder2 }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTwoOdd1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //PartDNA dnaFuel = new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-1d, 0, 0), Orientation = new Quaternion(new Vector3D(1, 1, 0), 90d), Scale = new Vector3D(.25d, .25d, 4d) };
                //FuelTank fuel = new FuelTank(_editorOptions, _itemOptions, dnaFuel);
                //fuel.QuantityCurrent = fuel.QuantityMax;

                ShipPartDNA dnaEnergy = new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(-1d, 0, 0), Orientation = new Quaternion(new Vector3D(1, 1, 0), 90d), Scale = new Vector3D(.25d, .25d, 4d) };
                //PartDNA dnaEnergy = new PartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(-1d, 0, 0), Orientation = new Quaternion(new Vector3D(1, 1, 0), 10d), Scale = new Vector3D(4d, 4d, .25d) };
                EnergyTank energy = new EnergyTank(_editorOptions, _itemOptions, dnaEnergy);
                //energy.QuantityCurrent = fuel.QuantityMax;

                ShipPartDNA dnaBrain = new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(2d, 2d, 2d) };
                Brain brain = new Brain(_editorOptions, _itemOptions, dnaBrain, null);

                //StartScene(new PartBase[] { fuel, brain });
                StartScene(new PartBase[] { energy, brain }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnThreeCubesRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShipPartDNA dnaGrav = new ShipPartDNA() { PartType = SensorGravity.PARTTYPE, Position = Math3D.GetRandomVector(2d).ToPoint(), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(10, 10, 10) };
                ShipPartDNA dnaSpin = new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = Math3D.GetRandomVector(2d).ToPoint(), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(10, 10, 10) };
                ShipPartDNA dnaVel = new ShipPartDNA() { PartType = SensorVelocity.PARTTYPE, Position = Math3D.GetRandomVector(2d).ToPoint(), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(10, 10, 10) };

                SensorGravity grav = new SensorGravity(_editorOptions, _itemOptions, dnaGrav, null, null);
                SensorSpin spin = new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);
                SensorVelocity vel = new SensorVelocity(_editorOptions, _itemOptions, dnaVel, null);

                StartScene(new PartBase[] { grav, spin, vel }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnThreeAnyRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartScene(new PartBase[] { GetRandomPart(), GetRandomPart(), GetRandomPart() }, 7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnFiveAnyRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartScene(Enumerable.Range(0, 5).Select(o => GetRandomPart()).ToArray(), 10);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTenAnyRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartScene(Enumerable.Range(0, 10).Select(o => GetRandomPart()).ToArray(), 12);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTwentyAnyRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartScene(Enumerable.Range(0, 20).Select(o => GetRandomPart()).ToArray(), 15);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnFiftyAnyRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartScene(Enumerable.Range(0, 50).Select(o => GetRandomPart()).ToArray(), 20);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnHundredAnyRand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartScene(Enumerable.Range(0, 100).Select(o => GetRandomPart()).ToArray(), 25);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCollisionOffset_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                //TODO: Don't use parts, use custom semi transparent visuals


                // ------------------ v1

                // Create a cube

                // Come up with a random offset

                // Fire rays from the mouse, and show a line and the two points where newton says it intersects






                //-------------------- v2

                // Create two cubes and hulls


                // Come up with a random offset



                // Redraw, draw the collision points





            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnHullBodyDispose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Vector3D size = new Vector3D(1, 1, 1);
                CollisionHull hull1 = CollisionHull.CreateBox(_world, 0, size, null);
                CollisionHull hull2 = CollisionHull.CreateBox(_world, 0, size, null);

                Body body1 = new Body(hull1, Matrix3D.Identity, 1d, null);
                Body body2 = new Body(hull2, Matrix3D.Identity, 1d, null);

                body1.Dispose();
                body2.Dispose();

                hull1.Dispose();
                hull2.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnHullBodyDispose2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //DisposeTests.ShareHullsAcrossWorlds();
                DisposeTests.ShareHullsWithBodies();
                //DisposeTests.ShareHullsWithBodies_alt();
                //DisposeTests.CompundCollisionHull();





            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearScene()
        {
            foreach (Visual3D visual in UtilityCore.Iterate(_initialVisuals, _currentVisuals, _debugVisuals))
            {
                _viewport.Children.Remove(visual);
            }

            _initialVisuals.Clear();
            _currentVisuals.Clear();
            _debugVisuals.Clear();

            //TODO: Dispose something?
            _solver = null;
            _parts = null;

            lblNumTicks.Text = "";
        }
        private void StartScene(PartBase[] parts, double offset)
        {
            ClearScene();

            _parts = parts;
            _debugOffset = offset;

            _tickCntr = 0;
            lblNumTicks.Text = _tickCntr.ToString();

            // Draw their initial positions off to the side
            DrawParts(parts, _initialVisuals, new Vector3D(0, _debugOffset, 0));

            // Create the solver
            if (radSolver1.IsChecked.Value)
            {
                PartSolver1 solver1 = new PartSolver1(_world, parts);
                solver1.MovePerStepPercent = .5d;
                _solver = solver1;
            }
            else if (radSolver2.IsChecked.Value)
            {
                PartSolver2 solver2 = new PartSolver2(_world, parts, false);
                solver2.MovePerStepPercent = 1d; //.75d;
                solver2.DoRotations = true;
                _solver = solver2;
            }
            else
            {
                throw new ApplicationException("Unknown solver type");
            }
        }

        private void DrawParts(PartBase[] parts, List<Visual3D> visuals, Vector3D offset)
        {
            Model3DGroup models = new Model3DGroup();

            foreach (PartBase part in parts)
            {
                models.Children.Add(part.Model);
            }

            ModelVisual3D model = new ModelVisual3D();
            model.Content = models;
            model.Transform = new TranslateTransform3D(offset);

            visuals.Add(model);
            _viewport.Children.Add(model);
        }
        private void DrawLines(Tuple<Point3D, Vector3D>[] lines, Vector3D offset, Color color)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Color = color;
            lineVisual.Thickness = 2d;

            foreach (var line in lines)
            {
                lineVisual.AddLine(line.Item1 + offset, line.Item1 + line.Item2 + offset);
            }

            _debugVisuals.Add(lineVisual);
            _viewport.Children.Add(lineVisual);
        }
        private void DrawDots(Point3D[] positions, Vector3D offset, double radius, Color color)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

            Model3DGroup geometries = new Model3DGroup();

            foreach (Point3D position in positions)
            {
                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(3, radius, radius, radius);
                geometry.Transform = new TranslateTransform3D(position.ToVector() + offset);

                geometries.Children.Add(geometry);
            }

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometries;

            // Add to the viewport
            _debugVisuals.Add(model);
            _viewport.Children.Add(model);
        }

        private PartBase GetRandomPart()
        {
            Point3D position = Math3D.GetRandomVector(2d).ToPoint();
            Quaternion orientation = Math3D.GetRandomRotation();
            double radius = 1d + StaticRandom.NextDouble() * 4d;
            double height = 1d + StaticRandom.NextDouble() * 4d;

            switch (StaticRandom.Next(8))
            {
                case 0:
                    #region Spin

                    double spinSize = 5d + (StaticRandom.NextDouble() * 8d);
                    ShipPartDNA dnaSpin = new ShipPartDNA() { PartType = SensorSpin.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(spinSize, spinSize, spinSize) };
                    return new SensorSpin(_editorOptions, _itemOptions, dnaSpin, null);

                #endregion

                case 1:
                    #region Fuel

                    ShipPartDNA dnaFuel = new ShipPartDNA() { PartType = FuelTank.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    FuelTank fuel = new FuelTank(_editorOptions, _itemOptions, dnaFuel);
                    fuel.QuantityCurrent = fuel.QuantityMax;		// without this, the fuel tank gets tossed around because it's so light
                    return fuel;

                #endregion

                case 2:
                    #region Energy

                    ShipPartDNA dnaEnergy = new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    return new EnergyTank(_editorOptions, _itemOptions, dnaEnergy);

                #endregion

                case 3:
                    #region Brain

                    ShipPartDNA dnaBrain = new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, radius) };
                    return new Brain(_editorOptions, _itemOptions, dnaBrain, null);

                #endregion

                case 4:
                    #region Thruster

                    ThrusterDNA dnaThruster1 = new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(height, height, height), ThrusterType = UtilityCore.GetRandomEnum(ThrusterType.Custom) };
                    return new Thruster(_editorOptions, _itemOptions, dnaThruster1, null);

                #endregion

                case 5:
                    #region Solar

                    ConverterRadiationToEnergyDNA dnaSolar = new ConverterRadiationToEnergyDNA() { PartType = ConverterRadiationToEnergy.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(height, 1d + StaticRandom.NextDouble() * 4d, 1d), Shape = UtilityCore.GetRandomEnum<SolarPanelShape>() };
                    return new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dnaSolar, null, _radiation);

                #endregion

                case 6:
                    #region Fuel->Energy

                    ShipPartDNA dnaBurner = new ShipPartDNA() { PartType = ConverterFuelToEnergy.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    return new ConverterFuelToEnergy(_editorOptions, _itemOptions, dnaBurner, null, null);

                #endregion

                case 7:
                    #region Energy->Ammo

                    ShipPartDNA dnaReplicator = new ShipPartDNA() { PartType = ConverterEnergyToAmmo.PARTTYPE, Position = position, Orientation = orientation, Scale = new Vector3D(radius, radius, height) };
                    return new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dnaReplicator, null, null);

                #endregion

                default:
                    throw new ApplicationException("Unexpected integer");
            }
        }

        #endregion

        private void btnTwoOdd2_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Brain-Brain
            //TODO: Solar-Brain
        }
    }
}
