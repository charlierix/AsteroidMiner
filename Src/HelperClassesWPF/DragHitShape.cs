using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// This defines a shape that a ray cast will hit, and you can do various hit tests against it
    /// </summary>
    /// <remarks>
    /// This ends up being a bit of a thin wrapper to Math3D and UtilityWPF's methods, so in some cases, this may be overkill,
    /// but I'm trying to pull some of the logic out of the main editor's code
    /// </remarks>
    public class DragHitShape
    {
        #region enum: ShapeType

        private enum ShapeType
        {
            None,
            Line,
            Lines,
            Circle,
            Circles,
            LinesCircles,
            Plane,
            Cylinder,
            Sphere,
            Mesh
        }

        #endregion

        #region class: CircleDefinition

        public class CircleDefinition
        {
            public CircleDefinition(ITriangle plane, Point3D center, double radius)
            {
                this.Plane = plane;
                this.Center = center;
                this.Radius = radius;
            }

            public ITriangle Plane
            {
                get;
                private set;
            }
            public Point3D Center
            {
                get;
                private set;
            }
            public double Radius
            {
                get;
                private set;
            }
        }

        #endregion

        #region Declaration Section

        private ShapeType _shape = ShapeType.None;

        private Point3D _point;
        private Vector3D _direction;
        private RayHitTestParameters[] _lines = null;
        private ITriangle _plane;
        private double _radius;
        private ITriangle[] _triangles = null;
        private CircleDefinition[] _circles = null;

        #endregion

        #region Public Properties

        private double _constrainMaxDotProduct = .85d;
        /// <summary>
        /// Any dot product higher than this value will constrain the drag motion to a single line/circle instead of the
        /// entire plane/cylinder
        /// </summary>
        /// <remarks>
        /// NOTE: The absolute value of the dot product is used
        /// </remarks>
        public double ConstrainMinDotProduct
        {
            get
            {
                return _constrainMaxDotProduct;
            }
            set
            {
                _constrainMaxDotProduct = value;
            }
        }

        #endregion

        #region Public Methods

        public void GetShape_Line(out RayHitTestParameters line)
        {
            if (_shape != ShapeType.Line)
            {
                throw new InvalidOperationException("This class isn't set up for a line: " + _shape.ToString());
            }

            line = new RayHitTestParameters(_point, _direction);
        }
        public void GetShape_Lines(out IEnumerable<RayHitTestParameters> lines)
        {
            if (_shape != ShapeType.Lines)
            {
                throw new InvalidOperationException("This class isn't set up for lines: " + _shape.ToString());
            }

            lines = _lines;
        }
        public void GetShape_Plane(out ITriangle plane)
        {
            if (_shape != ShapeType.Plane)
            {
                throw new InvalidOperationException("This class isn't set up for a plane: " + _shape.ToString());
            }

            plane = _plane;
        }
        public void GetShape_Circle(out ITriangle plane, out Point3D center, out double radius)
        {
            if (_shape != ShapeType.Circle)
            {
                throw new InvalidOperationException("This class isn't set up for a circle: " + _shape.ToString());
            }

            plane = _plane;
            center = _point;
            radius = _radius;
        }
        public void GetShape_Circles(out IEnumerable<CircleDefinition> circles)
        {
            if (_shape != ShapeType.Circles)
            {
                throw new InvalidOperationException("This class isn't set up for circles: " + _shape.ToString());
            }

            circles = _circles;
        }
        public void GetShape_LinesCircles(out IEnumerable<RayHitTestParameters> lines, out IEnumerable<CircleDefinition> circles)
        {
            if (_shape != ShapeType.LinesCircles)
            {
                throw new InvalidOperationException("This class isn't set up for lines and circles: " + _shape.ToString());
            }

            lines = _lines;
            circles = _circles;
        }
        public void GetShape_Cylinder(out RayHitTestParameters axis, out double radius)
        {
            if (_shape != ShapeType.Cylinder)
            {
                throw new InvalidOperationException("This class isn't set up for a cylinder: " + _shape.ToString());
            }

            axis = new RayHitTestParameters(_point, _direction);
            radius = _radius;
        }
        public void GetShape_Sphere(out Point3D center, out double radius)
        {
            if (_shape != ShapeType.Sphere)
            {
                throw new InvalidOperationException("This class isn't set up for a sphere: " + _shape.ToString());
            }

            center = _point;
            radius = _radius;
        }
        public void GetShape_Mesh(out IEnumerable<ITriangle> triangles)
        {
            if (_shape != ShapeType.Mesh)
            {
                throw new InvalidOperationException("This class isn't set up for a mesh: " + _shape.ToString());
            }

            triangles = _triangles;
        }

        public void SetShape_None()
        {
            _shape = ShapeType.None;
        }
        public void SetShape_Line(RayHitTestParameters line)
        {
            _shape = ShapeType.Line;
            _point = line.Origin;
            _direction = line.Direction;
        }
        public void SetShape_Lines(IEnumerable<RayHitTestParameters> lines)
        {
            _shape = ShapeType.Lines;
            _lines = lines.ToArray();
        }
        public void SetShape_Plane(ITriangle plane)
        {
            _shape = ShapeType.Plane;
            _plane = plane;
        }
        public void SetShape_Circle(ITriangle plane, Point3D center, double radius)
        {
            _shape = ShapeType.Circle;
            _plane = plane;
            _point = center;
            _radius = radius;
        }
        public void SetShape_Circles(IEnumerable<CircleDefinition> circles)
        {
            _shape = ShapeType.Circles;
            _circles = circles.ToArray();
        }
        public void SetShape_LinesCircles(IEnumerable<RayHitTestParameters> lines, IEnumerable<CircleDefinition> circles)
        {
            _shape = ShapeType.LinesCircles;
            _lines = lines.ToArray();
            _circles = circles.ToArray();
        }
        public void SetShape_Cylinder(RayHitTestParameters axis, double radius)
        {
            _shape = ShapeType.Cylinder;
            _point = axis.Origin;
            _direction = axis.Direction;
            _radius = radius;
        }
        public void SetShape_Sphere(Point3D center, double radius)
        {
            _shape = ShapeType.Sphere;
            _point = center;
            _radius = radius;
        }
        public void SetShape_Mesh(IEnumerable<ITriangle> triangles)
        {
            _shape = ShapeType.Mesh;
            _triangles = triangles.ToArray();
        }

        /// <summary>
        /// This overload returns a point on the shape that is closest to the point passed in
        /// </summary>
        public Point3D? CastRay(Point3D point)
        {
            Point3D? retVal = null;

            switch (_shape)
            {
                case ShapeType.Line:
                    #region Line

                    retVal = Math3D.GetClosestPoint_Line_Point(_point, _direction, point);

                    #endregion
                    break;

                case ShapeType.Lines:
                    #region Lines

                    retVal = CastRay_LinesCircles(_lines, null, point);

                    #endregion
                    break;

                case ShapeType.Plane:
                    #region Plane

                    retVal = Math3D.GetClosestPoint_Plane_Point(_plane, point);

                    #endregion
                    break;

                case ShapeType.Circle:
                    #region Circle

                    retVal = Math3D.GetClosestPoint_Circle_Point(_plane, _point, _radius, point);

                    #endregion
                    break;

                case ShapeType.Circles:
                    #region Circles

                    retVal = CastRay_LinesCircles(null, _circles, point);

                    #endregion
                    break;

                case ShapeType.LinesCircles:
                    #region LinesCircles

                    retVal = CastRay_LinesCircles(_lines, _circles, point);

                    #endregion
                    break;

                case ShapeType.Cylinder:
                    #region Cylinder

                    retVal = Math3D.GetClosestPoint_Cylinder_Point(_point, _direction, _radius, point);

                    #endregion
                    break;

                case ShapeType.Sphere:
                    #region Sphere

                    retVal = Math3D.GetClosestPoint_Sphere_Point(_point, _radius, point);

                    #endregion
                    break;

                case ShapeType.Mesh:
                    throw new ApplicationException("finish this");

                case ShapeType.None:
                    retVal = null;
                    break;

                default:
                    throw new ApplicationException("Unknown ShapeType: " + _shape.ToString());
            }

            return retVal;
        }

        /// <summary>
        /// This overload is a straight ray cast, nothing extra
        /// </summary>
        public Point3D? CastRay(RayHitTestParameters ray)
        {
            Point3D? retVal = null;

            switch (_shape)
            {
                case ShapeType.Line:
                    #region Line

                    Point3D? point1, point2;
                    if (Math3D.GetClosestPoints_Line_Line(out point1, out point2, _point, _direction, ray.Origin, ray.Direction))
                    {
                        retVal = point1.Value;
                    }

                    #endregion
                    break;

                case ShapeType.Lines:
                    #region Lines

                    retVal = CastRay_LinesCircles(_lines, null, ray);

                    #endregion
                    break;

                case ShapeType.Plane:
                    #region Plane

                    retVal = Math3D.GetIntersection_Plane_Line(_plane, ray.Origin, ray.Direction);

                    #endregion
                    break;

                case ShapeType.Circle:
                    #region Circle

                    Point3D[] nearestCirclePoints, nearestLinePoints;
                    if (Math3D.GetClosestPoints_Circle_Line(out nearestCirclePoints, out nearestLinePoints, _plane, _point, _radius, ray.Origin, ray.Direction, Math3D.RayCastReturn.ClosestToRay))
                    {
                        retVal = nearestCirclePoints[0];
                    }

                    #endregion
                    break;

                case ShapeType.Circles:
                    #region Circles

                    retVal = CastRay_LinesCircles(null, _circles, ray);

                    #endregion
                    break;

                case ShapeType.LinesCircles:
                    #region LinesCircles

                    retVal = CastRay_LinesCircles(_lines, _circles, ray);

                    #endregion
                    break;

                case ShapeType.Cylinder:
                    #region Cylinder

                    Point3D[] nearestCylinderPoints, nearestLinePoints2;
                    if (Math3D.GetClosestPoints_Cylinder_Line(out nearestCylinderPoints, out nearestLinePoints2, _point, _direction, _radius, ray.Origin, ray.Direction, Math3D.RayCastReturn.ClosestToRay))
                    {
                        retVal = nearestCylinderPoints[0];
                    }

                    #endregion
                    break;

                case ShapeType.Sphere:
                    #region Sphere

                    Point3D[] nearestSpherePoints, nearestLinePoints3;
                    Math3D.GetClosestPoints_Sphere_Line(out nearestSpherePoints, out nearestLinePoints3, _point, _radius, ray.Origin, ray.Direction, Math3D.RayCastReturn.ClosestToRay);
                    {
                        retVal = nearestSpherePoints[0];
                    }

                    #endregion
                    break;

                case ShapeType.Mesh:
                    throw new ApplicationException("finish this");

                case ShapeType.None:
                    retVal = null;
                    break;

                default:
                    throw new ApplicationException("Unknown ShapeType: " + _shape.ToString());
            }

            return retVal;
        }

        /// <summary>
        /// This overload is for actually dragging a part around
        /// </summary>
        /// <remarks>
        /// The centerRay is the ray that goes through the middle of the part being dragged around.  The return point is where
        /// the center of the part will go, and is calculated as an offset from the click ray
        /// </remarks>
        public Point3D? CastRay(RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        {
            Point3D? retVal = null;

            switch (_shape)
            {
                case ShapeType.Line:
                    double dummy1;
                    retVal = CastRay_Line(out dummy1, _point, _direction, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.Lines:
                    retVal = CastRay_LinesCircles(_lines, null, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.Plane:
                    retVal = CastRay_Plane(_plane, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.Circle:
                    double dummy2;
                    retVal = CastRay_Circle(out dummy2, _plane, _point, _radius, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.Circles:
                    retVal = CastRay_LinesCircles(null, _circles, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.LinesCircles:
                    retVal = CastRay_LinesCircles(_lines, _circles, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.Cylinder:
                    retVal = CastRay_Cylinder(_point, _direction, _radius, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.Sphere:
                    retVal = CastRay_Sphere(_point, _radius, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    break;

                case ShapeType.Mesh:
                    throw new ApplicationException("finish this");

                case ShapeType.None:
                    retVal = null;
                    break;

                default:
                    throw new ApplicationException("Unknown ShapeType: " + _shape.ToString());
            }

            return retVal;
        }

        /// <summary>
        /// This overload is the same as the previous, but if the camera is looking along the plane/cylinder wall, then the output will be
        /// constrained to a line/circle
        /// NOTE: Sphere and circle will never be constrained, the lesser overload will be used instead
        /// </summary>
        public Point3D? CastRay(RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay, PerspectiveCamera camera, Viewport3D viewport)
        {
            if (_shape == ShapeType.None)
            {
                return null;
            }
            else if (_shape == ShapeType.Sphere || _shape == ShapeType.Circle || _shape == ShapeType.Circles)
            {
                return CastRay(mouseDownClickRay, mouseDownCenterRay, currentClickRay);
            }

            #region Get dot product

            //NOTE: _camera.LookDirection and _camera.UpDirection are really screwed up (I think that the trackball messed them up), so fire a ray instead
            // I'm not using the mouse click point, because that can change as they drag, and the inconsistency would be jarring
            RayHitTestParameters cameraLook = UtilityWPF.RayFromViewportPoint(camera, viewport, new Point(viewport.ActualWidth / 2d, viewport.ActualHeight / 2d));

            double dot = 0;
            double[] dots = null;

            Vector3D cameraLookUnit = cameraLook.Direction.ToUnit();

            switch (_shape)
            {
                case ShapeType.Plane:
                    dot = Vector3D.DotProduct(_plane.NormalUnit, cameraLookUnit);		// the dot is against the normal
                    break;

                case ShapeType.Line:
                case ShapeType.Cylinder:
                    //NOTE: They cylinder only limits movement if they are looking along the line.
                    dot = Vector3D.DotProduct(_direction.ToUnit(), cameraLookUnit);		// the dot is along the drag line
                    break;

                case ShapeType.Lines:
                case ShapeType.LinesCircles:		// I don't care about the dot product for the circles, they aren't limited
                    dots = _lines.Select(o => Vector3D.DotProduct(o.Direction.ToUnit(), cameraLookUnit)).ToArray();
                    break;

                default:
                    throw new ApplicationException("finish this");
            }

            #endregion

            Point3D? retVal = null;

            switch (_shape)
            {
                case ShapeType.Line:
                    #region Line

                    if (Math.Abs(dot) > _constrainMaxDotProduct)
                    {
                        retVal = null;
                    }
                    else
                    {
                        double dummy1;
                        retVal = CastRay_Line(out dummy1, _point, _direction, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    }

                    #endregion
                    break;

                case ShapeType.Lines:
                case ShapeType.Circles:
                case ShapeType.LinesCircles:
                    #region Lines/Circles

                    //NOTE: These have to look at _shape instead of null checks, because the values may be non null from a previous use

                    List<RayHitTestParameters> usableLines = new List<RayHitTestParameters>();
                    if (_shape == ShapeType.Lines || _shape == ShapeType.LinesCircles)
                    {
                        for (int cntr = 0; cntr < _lines.Length; cntr++)
                        {
                            if (Math.Abs(dots[cntr]) <= _constrainMaxDotProduct)
                            {
                                usableLines.Add(_lines[cntr]);
                            }
                        }
                    }

                    CircleDefinition[] usableCircles = null;
                    if (_shape == ShapeType.Circles || _shape == ShapeType.LinesCircles)
                    {
                        usableCircles = _circles;		// all circles are always used
                    }

                    retVal = CastRay_LinesCircles(usableLines, usableCircles, mouseDownClickRay, mouseDownCenterRay, currentClickRay);

                    #endregion
                    break;

                case ShapeType.Plane:
                    #region Plane

                    if (Math.Abs(dot) < 1d - _constrainMaxDotProduct)
                    {
                        retVal = CastRay_PlaneLimited(_plane, mouseDownClickRay, mouseDownCenterRay, currentClickRay, cameraLook);
                    }
                    else
                    {
                        retVal = CastRay_Plane(_plane, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    }

                    #endregion
                    break;

                case ShapeType.Cylinder:
                    #region Cylinder

                    if (Math.Abs(dot) > _constrainMaxDotProduct)
                    {
                        // Constrain to a circle
                        Vector3D circleVector1 = Math3D.GetArbitraryOrhonganal(_direction);
                        Vector3D circleVector2 = Vector3D.CrossProduct(circleVector1, _direction);
                        Triangle plane = new Triangle(_point, _point + circleVector1, _point + circleVector2);

                        double dummy1;
                        retVal = CastRay_Circle(out dummy1, plane, _point, _radius, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    }
                    else
                    {
                        retVal = CastRay_Cylinder(_point, _direction, _radius, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    }

                    #endregion
                    break;

                case ShapeType.Mesh:
                    throw new ApplicationException("finish this");

                case ShapeType.None:
                    retVal = null;
                    break;

                default:
                    throw new ApplicationException("Unknown ShapeType: " + _shape.ToString());
            }

            return retVal;
        }

        /// <summary>
        /// This returns the normal of the drag shape relative to the point passed in
        /// NOTE: I'll wait till this method is needed before finishing it
        /// </summary>
        /// <remarks>
        /// This is important for cases where they are dragging an object across a cylinder or sphere.  You would want that object's orientation
        /// to change so it appears to be stuck to the side of the shape
        /// 
        /// Drag shape of line(s) just returns null
        /// </remarks>
        public Vector3D? GetNormal(Point3D point)
        {
            // Can't do this, because if the point goes under the plane, the normal gets reversed
            //// This should handle most cases
            //Point3D? pointOnShape = CastRay(point);
            //if (pointOnShape != null && !Math3D.IsNearValue(point, pointOnShape.Value))
            //{
            //    return point - pointOnShape.Value;
            //}

            Vector3D? retVal = null;

            // The request point is sitting on the shape.  Some of the shapes can get more generic

            switch (_shape)
            {
                case ShapeType.Plane:
                    #region Plane

                    retVal = _plane.NormalUnit;

                    // No need to do all this.  Execution only gets here when the request point is sitting on the plane
                    //Point3D pointOnPlane = Math3D.GetClosestPoint_Point_Plane(_plane, point);

                    //if (!Math3D.IsNearValue(point, pointOnPlane) && Vector3D.DotProduct(point - pointOnPlane, retVal.Value) < 0)
                    //{
                    //    // The test point is away from the normal, reverse it
                    //    retVal = retVal.Value * -1;
                    //}

                    #endregion
                    break;

                case ShapeType.Cylinder:
                    #region Cylinder

                    // Execution gets here when the request point is sitting on the surface of the cylinder.  Treat it like a line and try again

                    Point3D nearestAxisPoint = Math3D.GetClosestPoint_Line_Point(_point, _direction, point);

                    if (!Math3D.IsNearValue(nearestAxisPoint, point))
                    {
                        retVal = point - nearestAxisPoint;
                    }

                    #endregion
                    break;

                case ShapeType.Sphere:
                    #region Sphere

                    if (!Math3D.IsNearValue(_point, point))
                    {
                        retVal = point - _point;
                    }

                    #endregion
                    break;
            }

            return retVal;
        }

        public Quaternion? GetRotation(Point3D from, Point3D to)
        {
            switch (_shape)
            {
                case ShapeType.Circle:
                    return GetRotation_Circle(_point, _plane.Normal, from, to);

                case ShapeType.Cylinder:
                    return GetRotation_Circle(_point, _direction, from, to);

                case ShapeType.Sphere:
                    return GetRotation_Sphere(_point, from, to);

                case ShapeType.Circles:
                case ShapeType.LinesCircles:
                    // Need to know what circle was used
                    //
                    // This public method shouldn't be separate from the castray methods.  The rotation should be passed as an out param if the user passes a
                    // bool to get it
                    //throw new ApplicationException("finish this");
                    return null;

                case ShapeType.Mesh:
                    // Project a ray to see which triange each point is on, then get the angle between the normals
                    // This could get difficult if the point isn't very close to any of the triangles (this method is inteded to be called using the result of CastRay)
                    throw new ApplicationException("finish this");

                default:
                    return null;
            }
        }

        #endregion

        #region Private Methods

        private static Point3D? CastRay_Line(out double clickDistanceSquared, Point3D point, Vector3D direction, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        {
            // This is the offset along the drag line from the center to mouse down click
            Vector3D offset;

            Point3D? point1, point2, point3, point4;
            if (Math3D.GetClosestPoints_Line_Line(out point1, out point2, point, direction, mouseDownClickRay.Origin, mouseDownClickRay.Direction) &&
                Math3D.GetClosestPoints_Line_Line(out point3, out point4, point, direction, mouseDownCenterRay.Origin, mouseDownCenterRay.Direction))
            {
                offset = point3.Value - point1.Value;		// clickpoint on drag line minus centerpoint on drag line
            }
            else
            {
                // The click ray is parallel to the drag axis.  This should be extremely rare
                offset = new Vector3D(0, 0, 0);
            }

            // Now that the offset is known, project the current click ray onto the drag line
            if (Math3D.GetClosestPoints_Line_Line(out point1, out point2, point, direction, currentClickRay.Origin, currentClickRay.Direction))
            {
                clickDistanceSquared = (point2.Value - point1.Value).LengthSquared;
                return point1.Value + offset;
            }
            else
            {
                clickDistanceSquared = 0;
                return null;
            }
        }
        private static Point3D? CastRay_Plane(ITriangle plane, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        {
            // This is the offset along the drag plane from the center to mouse down click
            Vector3D offset;

            Point3D? mouseDownClickPoint = Math3D.GetIntersection_Plane_Line(plane, mouseDownClickRay.Origin, mouseDownClickRay.Direction);
            Point3D? mouseDownCenterPoint = Math3D.GetIntersection_Plane_Line(plane, mouseDownCenterRay.Origin, mouseDownCenterRay.Direction);

            if (mouseDownClickPoint != null && mouseDownCenterPoint != null)
            {
                offset = mouseDownCenterPoint.Value - mouseDownClickPoint.Value;
            }
            else
            {
                // The click ray is parallel to the drag plane.  This should be extremely rare
                offset = new Vector3D(0, 0, 0);
            }

            // Now that the offset is known, project the current click ray onto the drag plane
            Point3D? retVal = Math3D.GetIntersection_Plane_Line(plane, currentClickRay.Origin, currentClickRay.Direction);

            if (retVal != null)
            {
                return retVal.Value + offset;
            }
            else
            {
                return null;
            }
        }
        private static Point3D? CastRay_Circle(out double clickDistanceSquared, ITriangle plane, Point3D center, double radius, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        {
            clickDistanceSquared = 0d;		// this will get overwritten, but without setting it globally, the compiler will complain

            // Get points on the circle
            Point3D? mouseDownClickPoint = null;
            Point3D? mouseDownCenterPoint = null;
            Point3D? currentClickPoint = null;

            Point3D[] nearestCirclePoints, nearestLinePoints;

            if (Math3D.GetClosestPoints_Circle_Line(out nearestCirclePoints, out nearestLinePoints, plane, center, radius, currentClickRay.Origin, currentClickRay.Direction, Math3D.RayCastReturn.ClosestToRay))
            {
                clickDistanceSquared = (nearestLinePoints[0] - nearestCirclePoints[0]).LengthSquared;
                currentClickPoint = nearestCirclePoints[0];

                if (Math3D.GetClosestPoints_Circle_Line(out nearestCirclePoints, out nearestLinePoints, plane, center, radius, mouseDownClickRay.Origin, mouseDownClickRay.Direction, Math3D.RayCastReturn.ClosestToRay))
                {
                    mouseDownClickPoint = nearestCirclePoints[0];

                    if (Math3D.GetClosestPoints_Circle_Line(out nearestCirclePoints, out nearestLinePoints, plane, center, radius, mouseDownCenterRay.Origin, mouseDownCenterRay.Direction, Math3D.RayCastReturn.ClosestToRay))
                    {
                        mouseDownCenterPoint = nearestCirclePoints[0];
                    }
                }
            }

            if (mouseDownCenterPoint == null || mouseDownClickPoint == null || currentClickPoint == null)
            {
                clickDistanceSquared = 0d;
                return currentClickPoint;		// it doesn't matter if this one is null or not, the offset can't be determined, so just return the raw click value
            }

            // Get the offset
            Vector3D mouseDownClickLine = mouseDownClickPoint.Value - center;
            Vector3D mouseDownCenterLine = mouseDownCenterPoint.Value - center;

            Quaternion offset = Math3D.GetRotation(mouseDownClickLine, mouseDownCenterLine);

            // Convert to local, and rotate by offset
            Vector3D currentClickLine = currentClickPoint.Value - center;
            currentClickLine = new RotateTransform3D(new QuaternionRotation3D(offset)).Transform(currentClickLine);

            // Now convert back to world coords
            return center + currentClickLine;
        }
        private static Point3D? CastRay_Cylinder(Point3D point, Vector3D direction, double radius, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        {
            // Get points on the cylinder
            Point3D? mouseDownClickPoint = null;
            Point3D? mouseDownCenterPoint = null;
            Point3D? currentClickPoint = null;

            Point3D[] nearestCylinderPoints, nearestLinePoints;
            if (Math3D.GetClosestPoints_Cylinder_Line(out nearestCylinderPoints, out nearestLinePoints, point, direction, radius, currentClickRay.Origin, currentClickRay.Direction, Math3D.RayCastReturn.ClosestToRay))
            {
                currentClickPoint = nearestCylinderPoints[0];

                if (Math3D.GetClosestPoints_Cylinder_Line(out nearestCylinderPoints, out nearestLinePoints, point, direction, radius, mouseDownClickRay.Origin, mouseDownClickRay.Direction, Math3D.RayCastReturn.ClosestToRay))
                {
                    mouseDownClickPoint = nearestCylinderPoints[0];

                    if (Math3D.GetClosestPoints_Cylinder_Line(out nearestCylinderPoints, out nearestLinePoints, point, direction, radius, mouseDownCenterRay.Origin, mouseDownCenterRay.Direction, Math3D.RayCastReturn.ClosestToRay))
                    {
                        mouseDownCenterPoint = nearestCylinderPoints[0];
                    }
                }
            }

            if (mouseDownCenterPoint == null || mouseDownClickPoint == null || currentClickPoint == null)
            {
                return currentClickPoint;		// it doesn't matter if this one is null or not, the offset can't be determined, so just return the raw click value
            }

            // Circle only cared about an offset angle, but cylinder needs two things:
            //		Offset line (the part of the offset that is parallel to the cylinder's axis)
            //		Offset angle (the part of the offset that is perpendicular to the cylinder's axis)
            Vector3D offsetLinear = (mouseDownCenterPoint.Value - mouseDownClickPoint.Value).GetProjectedVector(direction);
            Quaternion offsetRadial = GetRotation_Circle(point, direction, mouseDownClickPoint.Value, mouseDownCenterPoint.Value);





            //TODO: Get the radial offset working as well (sphere is also messed up, the same fix should work for both)







            //TODO: See if this is the most effiecient way or not
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(offsetRadial)));
            transform.Children.Add(new TranslateTransform3D(offsetLinear));

            //TODO: Bring currentClickPoint into local coords, do the transform, then put it back into global coords

            // Find the point along the cylinder's axis that is nearest to the current click.  This will become the center of the model coords
            Point3D modelCenter = Math3D.GetClosestPoint_Line_Point(point, direction, currentClickPoint.Value);

            // Shift the click point into model coords
            Vector3D modelClick = currentClickPoint.Value - modelCenter;

            // Adjust by the offset transform (needed to put into model coords, because there is a rotation)
            modelClick = transform.Transform(modelClick);

            // Now put back into world coords
            return modelCenter + modelClick;
        }
        private static Point3D? CastRay_Sphere(Point3D center, double radius, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        {
            // Get points on the sphere
            Point3D[] nearestSpherePoints, nearestLinePoints;
            Math3D.GetClosestPoints_Sphere_Line(out nearestSpherePoints, out nearestLinePoints, center, radius, currentClickRay.Origin, currentClickRay.Direction, Math3D.RayCastReturn.ClosestToRay);
            Point3D currentClickPoint = nearestSpherePoints[0];




            //TODO: The rest of this method is flawed, it should be fixed (I don't think it's as simplistic as I'm making it - or there is just a flaw in the math)
            // Actually, I think the flaw is taking it to world coords, rotating, and putting back?
            return currentClickPoint;




            //Math3D.GetClosestPointsBetweenLineSphere(out nearestSpherePoints, out nearestLinePoints, center, radius, mouseDownClickRay.Origin, mouseDownClickRay.Direction, Math3D.RayCastReturn.ClosestToRay);
            //Point3D mouseDownClickPoint = nearestSpherePoints[0];

            //Math3D.GetClosestPointsBetweenLineSphere(out nearestSpherePoints, out nearestLinePoints, center, radius, mouseDownCenterRay.Origin, mouseDownCenterRay.Direction, Math3D.RayCastReturn.ClosestToRay);
            //Point3D mouseDownCenterPoint = nearestSpherePoints[0];

            //// Get the offset from mouse down click to center
            //Vector3D mouseDownClickLine = mouseDownClickPoint - center;
            //Vector3D mouseDownCenterLine = mouseDownCenterPoint - center;

            //Quaternion offset = Math3D.GetRotation(mouseDownClickLine, mouseDownCenterLine);

            //// Convert to local coords, rotate
            //Vector3D currentClickLine = currentClickPoint - center;
            //currentClickLine = new RotateTransform3D(new QuaternionRotation3D(offset)).Transform(currentClickLine);

            //// Now convert back to world coords
            //return center + currentClickLine;
        }

        private static Point3D? CastRay_PlaneLimited(ITriangle plane, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay, RayHitTestParameters cameraLook)
        {
            // They are looking along the plane, so snap to a line instead of a plane
            RayHitTestParameters snapLine = CastRay_PlaneLimitedSprtGetSnapLine(plane, cameraLook.Direction);		// the returned vector is used like 3 bools, with only one axis set to true
            if (snapLine == null)
            {
                return null;
            }

            double dummy1;
            return CastRay_Line(out dummy1, snapLine.Origin, snapLine.Direction, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
        }
        private static RayHitTestParameters CastRay_PlaneLimitedSprtGetSnapLine(ITriangle plane, Vector3D lookDirection)
        {
            const double THRESHOLD = .33d;

            #region Get orthogonal vectors

            Point3D centerPoint = new Point3D();
            DoubleVector testVectors = new DoubleVector();

            //This assumes that the plane was set up with orthogonal vectors, and that these are the ones to use in this case

            for (int cntr = 0; cntr < 3; cntr++)
            {
                switch (cntr)
                {
                    case 0:
                        centerPoint = plane.Point0;
                        testVectors = new DoubleVector((plane.Point1 - plane.Point0).ToUnit(), (plane.Point2 - plane.Point0).ToUnit());
                        break;

                    case 1:
                        centerPoint = plane.Point1;
                        testVectors = new DoubleVector((plane.Point2 - plane.Point1).ToUnit(), (plane.Point0 - plane.Point1).ToUnit());
                        break;

                    case 2:
                        centerPoint = plane.Point2;
                        testVectors = new DoubleVector((plane.Point0 - plane.Point2).ToUnit(), (plane.Point1 - plane.Point2).ToUnit());
                        break;

                    default:
                        throw new ApplicationException("Unexpected cntr: " + cntr.ToString());
                }

                if (Math1D.IsNearZero(Vector3D.DotProduct(testVectors.Standard, testVectors.Orth)))
                {
                    break;
                }
            }

            #endregion

            double dot = Vector3D.DotProduct(testVectors.Standard, lookDirection.ToUnit());
            if (Math.Abs(dot) < THRESHOLD)
            {
                return new RayHitTestParameters(centerPoint, testVectors.Standard);		// standard is fairly orhogonal to the camera's look direction, so only allow the part to slide along this axis
            }

            dot = Vector3D.DotProduct(testVectors.Orth, lookDirection.ToUnit());
            if (Math.Abs(dot) < THRESHOLD)
            {
                return new RayHitTestParameters(centerPoint, testVectors.Orth);
            }

            return null;
        }

        private static Point3D? CastRay_LinesCircles(IEnumerable<RayHitTestParameters> lines, IEnumerable<CircleDefinition> circles, Point3D testPoint)
        {
            Point3D? retVal = null;
            double distance = double.MaxValue;

            if (lines != null)
            {
                // Cast onto each line, and return the point that's closest to the test point
                foreach (RayHitTestParameters line in lines)
                {
                    Point3D point = Math3D.GetClosestPoint_Line_Point(line.Origin, line.Direction, testPoint);

                    double localDistance = (point - testPoint).LengthSquared;
                    if (retVal == null || localDistance < distance)
                    {
                        retVal = point;
                        distance = localDistance;
                    }
                }
            }

            if (circles != null)
            {
                // Cast onto each circle, and return the point that's closest to the test point
                foreach (CircleDefinition circle in circles)
                {
                    Point3D? point = Math3D.GetClosestPoint_Circle_Point(circle.Plane, circle.Center, circle.Radius, testPoint);
                    if (point != null)
                    {
                        double localDistance = (point.Value - testPoint).LengthSquared;
                        if (retVal == null || localDistance < distance)
                        {
                            retVal = point.Value;
                            distance = localDistance;
                        }
                    }
                }
            }

            return retVal;
        }
        private static Point3D? CastRay_LinesCircles(IEnumerable<RayHitTestParameters> lines, IEnumerable<CircleDefinition> circles, RayHitTestParameters ray)
        {
            Point3D? retVal = null;
            double distance = double.MaxValue;

            if (lines != null)
            {
                // Cast onto each line, and return the point that's closest to the ray's origin
                foreach (RayHitTestParameters line in lines)
                {
                    Point3D? point1, point2;
                    if (Math3D.GetClosestPoints_Line_Line(out point1, out point2, line.Origin, line.Direction, ray.Origin, ray.Direction))
                    {
                        double localDistance = (point2.Value - point1.Value).LengthSquared;
                        if (retVal == null || localDistance < distance)
                        {
                            retVal = point1.Value;
                            distance = localDistance;
                        }
                    }
                }
            }

            if (circles != null)
            {
                // Cast onto each circle, and return the point that's closest to the ray's origin
                foreach (CircleDefinition circle in circles)
                {
                    Point3D[] points1, points2;
                    if (Math3D.GetClosestPoints_Circle_Line(out points1, out points2, circle.Plane, circle.Center, circle.Radius, ray.Origin, ray.Direction, Math3D.RayCastReturn.ClosestToRay))
                    {
                        double localDistance = (points2[0] - points1[0]).LengthSquared;
                        if (retVal == null || localDistance < distance)
                        {
                            retVal = points1[0];
                            distance = localDistance;
                        }
                    }
                }
            }

            return retVal;
        }
        private static Point3D? CastRay_LinesCircles(IEnumerable<RayHitTestParameters> lines, IEnumerable<CircleDefinition> circles, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        {
            Point3D? retVal = null;
            double distance = double.MaxValue;

            if (lines != null)
            {
                // Cast onto each line, and return the point that's closest to the ray's origin
                foreach (RayHitTestParameters line in lines)
                {
                    double localDistance;
                    Point3D? localPoint = CastRay_Line(out localDistance, line.Origin, line.Direction, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    if (localPoint != null)
                    {
                        if (retVal == null || localDistance < distance)
                        {
                            retVal = localPoint.Value;
                            distance = localDistance;
                        }
                    }
                }
            }

            if (circles != null)
            {
                // Cast onto each circle, and return the point that's closest to the ray's origin
                foreach (CircleDefinition circle in circles)
                {
                    double localDistance;
                    Point3D? localPoint = CastRay_Circle(out localDistance, circle.Plane, circle.Center, circle.Radius, mouseDownClickRay, mouseDownCenterRay, currentClickRay);
                    if (localPoint != null)
                    {
                        if (retVal == null || localDistance < distance)
                        {
                            retVal = localPoint.Value;
                            distance = localDistance;
                        }
                    }
                }
            }

            return retVal;
        }

        private static Quaternion GetRotation_Circle(Point3D center, Vector3D normal, Point3D from, Point3D to)
        {
            // Get the two angles, it doesn't matter where they are along that normal
            Vector3D fromLine = from - Math3D.GetClosestPoint_Line_Point(center, normal, from);
            Vector3D toLine = to - Math3D.GetClosestPoint_Line_Point(center, normal, to);

            return Math3D.GetRotation(fromLine, toLine);		// I was about to just take the angle, since I already have the normal, but the angle could be greater than 180, so the axis would flip, and the math3d method already accounts for that, so I'm just using it
        }
        private static Quaternion GetRotation_Sphere(Point3D center, Point3D from, Point3D to)
        {
            Vector3D fromLine = from - center;
            Vector3D toLine = to - center;

            return Math3D.GetRotation(fromLine, toLine);
        }

        #region MAYBE USEFUL

        //private static Point3D? CastRay_Circle_OVERKILL(ITriangle plane, Point3D center, double radius, RayHitTestParameters mouseDownClickRay, RayHitTestParameters mouseDownCenterRay, RayHitTestParameters currentClickRay)
        //{
        //    //TODO: This method may be a bit overkill, since all three rays are projecting to the same cicle

        //    // Get angle for all three rays
        //    Quaternion angleMouseDownClick = GetAngle_Circle(plane, center, radius, mouseDownClickRay);
        //    Quaternion angleMouseDownCenter = GetAngle_Circle(plane, center, radius, mouseDownCenterRay);
        //    Quaternion angleCurrentClick = GetAngle_Circle(plane, center, radius, currentClickRay);

        //    // Take mouse down click minus center
        //    Quaternion centerNegated = new Quaternion(angleMouseDownCenter.Axis, angleMouseDownCenter.Angle * -1d);
        //    Quaternion offset = Quaternion.Multiply(angleMouseDownClick.ToUnit(), centerNegated.ToUnit());

        //    // That difference is how much to subtract the mouse down by
        //    Quaternion offsetNegated = new Quaternion(offset.Axis, offset.Angle * -1d);
        //    Quaternion final = Quaternion.Multiply(angleCurrentClick.ToUnit(), offsetNegated.ToUnit());






        //}
        //private static Quaternion GetAngle_Circle(ITriangle plane, Point3D center, double radius, RayHitTestParameters ray)
        //{
        //    // Get a point on the circle
        //    Point3D[] nearestCirclePoints, nearestLinePoints;
        //    if (!Math3D.GetClosestPointsBetweenLineCircle(out nearestCirclePoints, out nearestLinePoints, plane, center, radius, ray.Origin, ray.Direction, true))
        //    {
        //        return Quaternion.Identity;
        //    }

        //    // Choose an arbitrary vector in this plane to be the axis where angle=0 (shouldn't matter what I use, as long as only this method
        //    // is used for getting a circle's angle)
        //    Vector3D axisX = plane.Point1 - plane.Point0;

        //    Vector3D requestAxis = nearestCirclePoints[0] - center;
        //    double angle = Vector3D.AngleBetween(axisX, requestAxis);

        //    Vector3D axis = Vector3D.CrossProduct(axisX, requestAxis);

        //    return new Quaternion(axis, angle);
        //}

        #endregion

        #endregion
    }
}
