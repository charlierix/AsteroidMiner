using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Math3D;

namespace Game.Orig.Map
{
    #region enum: CollisionDepth

    public enum CollisionDepth
    {
        NotColliding = 0,
        Touching,
        Penetrating
    }

    #endregion

    /// <summary>
    /// This class knows how to collide radar blips (as well as detect collisions)
    /// </summary>
    /// <remarks>
    /// I intend the base class to know about singular items (spheres, polygons).  If you have a more complex object (ragdolls),
    /// derive this class.
    /// 
    /// I'm designing this class so that all the real work is done by protected methods.  That way a derived class can fairly easily
    /// redo the base class
    /// </remarks>
    public class CollisionHandler
    {
        #region Declaration Section

        /// <summary>
        /// This is the percent of the body's size that has to be penetrated before it is considered a penetration (as opposed to just
        /// touching)
        /// </summary>
        private double _penetrationThresholdPercent = .02d;

        private double _pullApartInstantPercent = 1d / 1.03d;		// pulls them 3% farther away from each other than the distance from each other (I store the divided value, so I don't have to do the division on every collision)
        private double _pullApartSpringVelocity = 1d;

        #endregion

        #region Properties

        /// <summary>
        /// The lower the threshold, the more exact the collision, but the longer it will take the map to find the time of
        /// collision (0% is edges touching, 100% is the centers touching)
        /// </summary>
        public double PenetrationThresholdPercent
        {
            get
            {
                return _penetrationThresholdPercent;
            }
            set
            {
                if (value < .001d)
                {
                    // In normal cases, this might be fine.  But if you are colliding a ship with a planet, .1% is still pretty
                    // far.  In that case, this class should be derived, and a more speciallized threshold should be used for
                    // that case, and this base class should handle the rest of the cases.
                    throw new ArgumentOutOfRangeException("Value cannot be less than .1%");
                }
                else if (value >= 1d)
                {
                    throw new ArgumentOutOfRangeException("Value must be less than 100% (otherwise, is it on its way in or out?)");
                }

                _penetrationThresholdPercent = value;
            }
        }

        /// <summary>
        /// This is how far to pull the objects apart (1 is 100%, and that is what it would be if I didn't have this property)
        /// NOTE:  This is only used when Instantanious PullApart is called
        /// </summary>
        /// <remarks>
        /// I store it as 1 / value, because it's cheaper for the collide function (then the collide function can do multiplication)
        /// </remarks>
        public double PullApartInstantPercent
        {
            get
            {
                return 1d / _pullApartInstantPercent;
            }
            set
            {
                if (value < 1d)
                {
                    throw new ArgumentOutOfRangeException("Using a value less than 100% will result in objects that stay stuck together: " + value.ToString());
                }
                else if (value > 1.25d)
                {
                    throw new ArgumentOutOfRangeException("Using a value greater than 125% will result in objects that fly apart very unnaturally: " + value.ToString());
                }

                _pullApartInstantPercent = 1d / value;
            }
        }
        /// <summary>
        /// This is how much velocity to give the balls
        /// NOTE:  This is only used when Spring PullApart is called
        /// </summary>
        public double PullApartSpringVelocity
        {
            get
            {
                return _pullApartSpringVelocity;
            }
            set
            {
                if (value <= 0d)
                {
                    throw new ArgumentOutOfRangeException("The spring acceleration must be greater than zero: " + value.ToString());
                }

                _pullApartSpringVelocity = value;
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// This overload simply returns a boolean (less expensive)
        /// </summary>
        public static bool IsIntersecting_SphereSphere_Bool(Sphere sphere1, Sphere sphere2)
        {
            MyVector lineBetween = sphere2.Position - sphere1.Position;

            double sumRadii = sphere1.Radius + sphere2.Radius;

            return lineBetween.GetMagnitudeSquared() <= sumRadii * sumRadii;
        }
        /// <summary>
        /// This static method just returns the point of intersection (or null).
        /// </summary>
        public static MyVector IsIntersecting_SphereSphere(Sphere sphere1, Sphere sphere2)
        {
            MyVector lineBetween = sphere2.Position - sphere1.Position;

            double distanceSquared = lineBetween.GetMagnitudeSquared();
            double sumRadii = sphere1.Radius + sphere2.Radius;

            // See if they missed each other
            if (distanceSquared > sumRadii * sumRadii)
            {
                return null;
            }

            // Figure out the percent of penetration
            double distance = Math.Sqrt(distanceSquared);
            double percentPenetrating = (sumRadii - distance) / sumRadii;

            // Turn lineBetween into a vector the length of ball1's radius
            lineBetween.BecomeUnitVector();
            lineBetween.Multiply(sphere1.Radius);
            lineBetween.Multiply(1 - percentPenetrating);		// shorten the line by the amount penetrating

            // Exit Function
            return sphere1.Position + lineBetween;
        }

        /// <summary>
        /// If intersecting, this will return the point of intersection.  Otherwise null
        /// </summary>
        /// <param name="lineStartPoint">Starting point of the line</param>
        /// <param name="lineDirection">direction and length of the line</param>
        /// <param name="limitToLineSegment">If false, the line passed in is thought of as infinitely long</param>
        /// <returns>point of intersection or null</returns>
        public static MyVector IsIntersecting_LineTriangle(MyVector lineStartPoint, MyVector lineDirection, Triangle triangle, bool limitToLineSegment)
        {
            // Get triangle edge vectors and plane normal
            MyVector edge21 = triangle.Vertex2 - triangle.Vertex1;
            MyVector edge31 = triangle.Vertex3 - triangle.Vertex1;
            MyVector normal = MyVector.Cross(edge21, edge31);
            if (normal.IsZero)
            {
                // Triangle is degenerate
                return null;
            }

            double a = MyVector.Dot(normal, lineStartPoint - triangle.Vertex1) * -1d;
            double b = MyVector.Dot(normal, lineDirection);
            if (Utility3D.IsNearZero(b))
            {
                // Ray is parallel to the triangle
                return null;
            }

            // Get the intersect point of the ray with the triangle's plane
            double percent = a / b;
            if (limitToLineSegment && (percent < 0d || percent > 1d))
            {
                // The ray intersects the plane, but the intersect point doesn't lie on the line segment
                return null;
            }

            // Calculate the intersect point
            MyVector retVal = lineStartPoint + (percent * lineDirection);

            // Check to see if the intersectPoint inside the triangle
            double uu = MyVector.Dot(edge21, edge21);
            double uv = MyVector.Dot(edge21, edge31);
            double vv = MyVector.Dot(edge31, edge31);
            MyVector w = retVal - triangle.Vertex1;
            double wu = MyVector.Dot(w, edge21);
            double wv = MyVector.Dot(w, edge31);
            double D = (uv * uv) - (uu * vv);

            // get and test parametric coords
            double s = ((uv * wv) - (vv * wu)) / D;
            if (s < 0d || s > 1d)
            {
                if (!(Utility3D.IsNearZero(s) || Utility3D.IsNearValue(s, 1d)))		// I was getting cases where s is VEEEEERY slightly negative, but the line was clearly intersecting
                {
                    // Intersection point is outside the triangle
                    return null;
                }
            }

            double t = ((uv * wu) - (uu * wv)) / D;
            if (t < 0d || (s + t) > 1d)
            {
                if (!(Utility3D.IsNearZero(t) || Utility3D.IsNearValue(s + t, 1d)))		// I was getting cases where s+t is VEEEEERY slightly over 1, but the line was clearly intersecting
                {
                    // Intersection point is outside the triangle
                    return null;
                }
            }

            // Exit Function
            return retVal;		// intersectPoint is in Triangle
        }

        /// <summary>
        /// If intersecting, this will return the point of intersection.  Otherwise null
        /// </summary>
        /// <param name="lineStartPoint">Starting point of the line</param>
        /// <param name="lineDirection">direction and length of the line</param>
        /// <param name="triangle">This represents the plane (3 arbitrary points on the plane)</param>
        /// <param name="limitToLineSegment">If false, the line passed in is thought of as infinitely long</param>
        /// <returns>point of intersection or null</returns>
        public static MyVector IsIntersecting_LinePlane(MyVector lineStartPoint, MyVector lineDirection, Triangle triangle, bool limitToLineSegment)
        {
            MyVector normal = triangle.Normal;

            double denominator = MyVector.Dot(normal, lineDirection);
            if (Utility3D.IsNearZero(denominator))		// parallel to the triangle's plane
            {
                return null;
            }

            double percentAlongLine = (triangle.DistanceFromOriginAlongNormal - MyVector.Dot(normal, lineStartPoint)) / denominator;

            if (limitToLineSegment && (percentAlongLine < 0d || percentAlongLine > 1d))
            {
                // The ray is intersecting, but they want the line segment only
                return null;
            }

            // Calculate the point along the line
            MyVector retVal = lineDirection.Clone();
            double length = retVal.GetMagnitude();
            retVal.Divide(length);
            retVal.Multiply(percentAlongLine * length);
            retVal.Add(lineStartPoint);

            // Exit Function
            return retVal;
        }

        public static MyVector IsIntersecting_SphereTriangle(Sphere sphere, Triangle triangle)
        {
            // Find the point on the triangle closest to the sphere's center
            MyVector retVal = triangle.GetClosestPointOnTriangle(sphere.Position);

            // Sphere and triangle intersect if the (squared) distance from sphere
            // center to point is less than the (squared) sphere radius
            MyVector v = retVal - sphere.Position;
            if (MyVector.Dot(v, v) <= sphere.Radius * sphere.Radius)
            {
                return retVal;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// This overload will always return null if the triangles are coplanar
        /// </summary>
        /// <returns>
        /// Null: no intersection
        /// 2 Points: the two points of intersection (it was always be two points, even if they are at the same position)
        /// </returns>
        public static MyVector[] IsIntersecting_TriangleTriangle(Triangle triangle1, Triangle triangle2)
        {
            MyVector[] retVal;
            bool dummy1;

            TriTriIntersectWithIsectline(out dummy1, out retVal, triangle1, triangle2, false);

            return retVal;
        }
        /// <summary>
        /// This overload returns two different levels of info (coplanar and 3D)
        /// </summary>
        /// <param name="isCoplanar">
        /// Whether the two triangles lie on the same plane or not (if the function returns true, and it's coplanar, you don't
        /// get any intersect points.  Just the knowledge that there is an intersection
        /// </param>
        /// <param name="intersectPoints">This is only populated if the function returns true, and coplanar is false</param>
        /// <returns>
        /// True:  Intersection occurred
        /// False:  No intersection occurred
        /// </returns>
        public static bool IsIntersecting_TriangleTriangle(out bool isCoplanar, out MyVector[] intersectPoints, Triangle triangle1, Triangle triangle2)
        {
            return TriTriIntersectWithIsectline(out isCoplanar, out intersectPoints, triangle1, triangle2, true);
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// This function will compare the two blips to see if they're touching or not
        /// NOTE:  This function DOES NOT check CollisionStyles of the blips.  So if you can't gracefully handle
        /// stationary penetrating blips, then don't call this function in that case.
        /// </summary>
        /// <remarks>
        /// I will examine the types of blips to see what type of check to do (sphere on sphere, sphere on poly, poly on poly)
        /// </remarks>
        public virtual CollisionDepth IsColliding(RadarBlip blip1, RadarBlip blip2)
        {
            // For now, I will just assume they are spheres
            return IsColliding_SphereSphere(blip1, blip2);
        }

        /// <summary>
        /// This function pulls the two blips straight apart from each other (no physics involved, just displacement)
        /// </summary>
        public virtual void PullItemsApartInstant(RadarBlip blip1, RadarBlip blip2)
        {
            if (blip1 is BallBlip && blip2 is BallBlip)
            {
                // They're both at least balls, use mass and radius in the calculation
                PullItemsApartInstant_BallBall((BallBlip)blip1, (BallBlip)blip2);
            }
            else
            {
                // At least one is a sphere.  Use radius in the calculation
                PullItemsApartInstant_SphereSphere(blip1, blip2);
            }
        }

        public virtual void PullItemsApartSpring(RadarBlip blip1, RadarBlip blip2)
        {
            if (blip1 is BallBlip && blip2 is BallBlip)
            {
                PullItemsApartSpring_BallBall((BallBlip)blip1, (BallBlip)blip2);
            }
            else if (blip1 is BallBlip)
            {
                PullItemsApartSpring_BallSphere((BallBlip)blip1, blip2);
            }
            else if (blip2 is BallBlip)
            {
                PullItemsApartSpring_BallSphere((BallBlip)blip2, blip1);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Can't apply a spring force to non-balls");
            }
        }

        public virtual bool Collide(RadarBlip blip1, RadarBlip blip2)
        {
            if (blip1.CollisionStyle == CollisionStyle.Ghost || blip2.CollisionStyle == CollisionStyle.Ghost)
            {
                // I won't even bother trying to collide ghosts
                return false;
            }

            if (!(blip1 is BallBlip))
            {
                return false;
            }

            if (!(blip2 is BallBlip))
            {
                return false;
            }

            // If I am here, they are both balls.
            BallBlip ballBlip1 = blip1 as BallBlip;
            BallBlip ballBlip2 = blip2 as BallBlip;

            if (ballBlip1.TorqueBall != null && ballBlip2.TorqueBall != null)
            {
                #region Treat them like solid balls

                if (ballBlip1.CollisionStyle == CollisionStyle.Standard)
                {
                    if (ballBlip2.CollisionStyle == CollisionStyle.Standard)
                    {
                        // Two standard balls
                        Collide_TorqueBallTorqueBall(ballBlip1, ballBlip2);
                    }
                    else
                    {
                        // One is standard, two is stationary
                        Collide_TorqueBallTorqueBall_2IsStationary(ballBlip1, ballBlip2);
                    }
                }
                else if (ballBlip2.CollisionStyle == CollisionStyle.Standard)
                {
                    // One is stationary, two is standard
                    Collide_TorqueBallTorqueBall_2IsStationary(ballBlip2, ballBlip1);
                }

                // No need for an else.  There is nothing to do (both are stationary)

                #endregion
            }
            else if (ballBlip1.TorqueBall == null && ballBlip2.TorqueBall == null)
            {
                #region Treat them like regular balls

                //TODO:  Handle a ball colliding with a torqueball

                if (ballBlip1.CollisionStyle == CollisionStyle.Standard)
                {
                    if (ballBlip2.CollisionStyle == CollisionStyle.Standard)
                    {
                        // Two standard balls
                        Collide_BallBall(ballBlip1, ballBlip2);
                    }
                    else
                    {
                        // One is standard, two is stationary
                        Collide_BallBall_2IsStationary(ballBlip1, ballBlip2);
                    }
                }
                else if (ballBlip2.CollisionStyle == CollisionStyle.Standard)
                {
                    // One is stationary, two is standard
                    Collide_BallBall_2IsStationary(ballBlip2, ballBlip1);
                }

                // No need for an else.  There is nothing to do (both are stationary)

                #endregion
            }
            else if (ballBlip1.TorqueBall != null)
            {
                // 1 is torqueball, 2 is regular ball
                CollideSprtBallTorqueBall(ballBlip2, ballBlip1);
            }
            else //if (ballBlip2.TorqueBall != null)
            {
                // 1 is regular ball, 2 is torqueball
                CollideSprtBallTorqueBall(ballBlip1, ballBlip2);
            }

            return true;
        }

        #endregion

        #region Protected Methods

        protected CollisionDepth IsColliding_SphereSphere(RadarBlip sphere1, RadarBlip sphere2)
        {
            MyVector lineBetween = sphere2.Sphere.Position - sphere1.Sphere.Position;

            double distanceSquared = lineBetween.GetMagnitudeSquared();
            double sumRadii = sphere1.Sphere.Radius + sphere2.Sphere.Radius;

            // See if they missed each other
            if (distanceSquared > sumRadii * sumRadii)
            {
                return CollisionDepth.NotColliding;
            }

            double distance = Math.Sqrt(distanceSquared);
            double percentPenetrating = (sumRadii - distance) / sumRadii;

            if (percentPenetrating <= _penetrationThresholdPercent)
            {
                return CollisionDepth.Touching;
            }
            else
            {
                return CollisionDepth.Penetrating;
            }
        }

        /// <summary>
        /// This function pulls the items directly apart based on the ratio of their radii
        /// </summary>
        protected void PullItemsApartInstant_SphereSphere(RadarBlip sphere1, RadarBlip sphere2)
        {
            // Get the vector from 2 to 1
            MyVector move1From2 = sphere1.Sphere.Position - sphere2.Sphere.Position;
            double totalDistance = move1From2.GetMagnitude() * _pullApartInstantPercent;		// distance from centers (exagerated by the percent passed in)
            double actualDistance = (sphere1.Sphere.Radius + sphere2.Sphere.Radius) - totalDistance;		// distance from edges

            // Make this a unit vector
            move1From2.Divide(totalDistance);

            double ratio1 = sphere1.Sphere.Radius / (sphere1.Sphere.Radius + sphere2.Sphere.Radius);

            // Set their positions
            if (sphere1.CollisionStyle == CollisionStyle.Standard)
            {
                sphere1.Sphere.Position.Add(move1From2 * (actualDistance * ratio1));
            }
            if (sphere2.CollisionStyle == CollisionStyle.Standard)
            {
                sphere2.Sphere.Position.Add(move1From2 * (actualDistance * (1d - ratio1) * -1d));
            }
        }
        /// <summary>
        /// This function pulls the items directly apart based on the ratio of their radii * mass
        /// </summary>
        protected void PullItemsApartInstant_BallBall(BallBlip ball1, BallBlip ball2)
        {
            // Get the vector from 2 to 1
            MyVector move1From2 = ball1.Ball.Position - ball2.Ball.Position;
            double totalDistance = move1From2.GetMagnitude() * _pullApartInstantPercent;		// distance from centers (exagerated by the percent passed in)
            double actualDistance = (ball1.Ball.Radius + ball2.Ball.Radius) - totalDistance;		// distance from edges

            // Make this a unit vector
            move1From2.Divide(totalDistance);

            double ratio1 = (ball1.Ball.Radius * ball1.Ball.Mass) / ((ball1.Ball.Radius * ball1.Ball.Mass) + (ball2.Ball.Radius * ball2.Ball.Mass));

            // Set their positions
            if (ball1.CollisionStyle == CollisionStyle.Standard)
            {
                ball1.Ball.Position.Add(move1From2 * (actualDistance * ratio1));
            }
            if (ball2.CollisionStyle == CollisionStyle.Standard)
            {
                ball2.Ball.Position.Add(move1From2 * (actualDistance * (1d - ratio1) * -1d));
            }
        }

        protected void PullItemsApartSpring_BallBall(BallBlip ball1, BallBlip ball2)
        {
            double sumRadii = ball1.Sphere.Radius + ball2.Sphere.Radius;

            // Get the vector from 2 to 1
            MyVector move1From2 = ball1.Sphere.Position - ball2.Sphere.Position;
            double totalDistance = move1From2.GetMagnitude();		// distance from centers

            if (totalDistance > sumRadii)
            {
                return;
            }

            // Make this a unit vector
            move1From2.Divide(totalDistance);

            // Normally, it's f=kx, but I want x to always be from zero to one.  So I'm really just finding the percent of
            // total velocity.
            double percent = 1d - (totalDistance / sumRadii);

            //TODO:  Don't pull them both away at the same velocity, use the ratio of the masses

            move1From2.Multiply(_pullApartSpringVelocity * percent);

            //TODO:  Instead of just adding directly to velocity, set it (but leave some of it there?  dot product?)
            if (ball1.CollisionStyle == CollisionStyle.Standard)
            {
                ball1.Ball.Velocity.Add(move1From2);
            }
            if (ball2.CollisionStyle == CollisionStyle.Standard)
            {
                move1From2.Multiply(-1d);
                ball2.Ball.Velocity.Add(move1From2);
            }
        }
        protected void PullItemsApartSpring_BallSphere(BallBlip ball, RadarBlip blip)
        {
        }

        protected void Collide_BallBall(BallBlip ball1, BallBlip ball2)
        {
            // See if they're sitting directly on top of each other
            if (Utility3D.IsNearValue(ball1.Sphere.Position.X, ball2.Sphere.Position.X) && Utility3D.IsNearValue(ball1.Sphere.Position.Y, ball2.Sphere.Position.Y) && Utility3D.IsNearValue(ball1.Sphere.Position.Z, ball2.Sphere.Position.Z))
            {
                // They're sitting directly on top of each other.  Move them slightly away from each other
                ball2.Sphere.Position.X += .001;
                ball2.Sphere.Position.Y += .001;
            }

            // Vector that is perpendicular to the tangent of the collision, and it points in the direction of object 1.  Real
            // easy when dealing with spheres     :)
            MyVector normal = ball2.Ball.Position - ball1.Ball.Position;

            // ------------------------------ Calculate the impulse
            // ------------------------------ impulse = ( -(1+elasticity)(Va - Vb) dot Normal ) / ( Normal dot ((1/Ma) + (1/Mb))Normal)
            // Numerator
            MyVector temp = ball1.Ball.Velocity - ball2.Ball.Velocity;
            temp.Multiply(-1d * (1d + ((ball1.Ball.Elasticity + ball2.Ball.Elasticity) / 2d)));
            double numerator = MyVector.Dot(temp, normal);

            if (numerator > 0d)
            {
                // Technically, there's no collision
                return;
            }

            // Divisor
            temp = normal.Clone();
            temp.Multiply((1d / ball1.Ball.Mass) + (1d / ball2.Ball.Mass));
            double divisor = MyVector.Dot(normal, temp);

            // Impulse
            double impulse = numerator / divisor;

            // ------------------------------ Calculate the first object's new velocity
            // ------------------------------ velocity = Va + ( impulse / Ma )Normal
            ball1.Ball.Velocity.Add(normal * (impulse / ball1.Ball.Mass));

            // ------------------------------ Calculate the second object's new velocity
            // ------------------------------ velocity = Vb - ( impulse / Mb )Normal
            ball2.Ball.Velocity.Subtract(normal * (impulse / ball2.Ball.Mass));
        }
        protected void Collide_BallBall_2IsStationary(BallBlip ball1, BallBlip ball2)
        {
            // See if they're sitting directly on top of each other
            if (Utility3D.IsNearValue(ball1.Sphere.Position.X, ball2.Sphere.Position.X) && Utility3D.IsNearValue(ball1.Sphere.Position.Y, ball2.Sphere.Position.Y) && Utility3D.IsNearValue(ball1.Sphere.Position.Z, ball2.Sphere.Position.Z))
            {
                // They're sitting directly on top of each other.  Move them slightly away from each other
                ball2.Sphere.Position.X += .001;
                ball2.Sphere.Position.Y += .001;
            }

            // Vector that is perpendicular to the tangent of the collision, and it points in the direction of object 1.  Real
            // easy when dealing with spheres     :)
            MyVector normal = ball2.Ball.Position - ball1.Ball.Position;

            // ------------------------------ Calculate the impulse
            // ------------------------------ impulse = ( -(1+elasticity)(Va - Vb) dot Normal ) / ( Normal dot ((1/Ma) + (1/Mb))Normal)
            // Numerator
            MyVector temp = ball1.Ball.Velocity - ball2.Ball.Velocity;
            temp.Multiply(-1d * (1d + ((ball1.Ball.Elasticity + ball2.Ball.Elasticity) / 2d)));
            double numerator = MyVector.Dot(temp, normal);

            if (numerator > 0d)
            {
                // Technically, there's no collision
                return;
            }

            // Divisor
            temp = normal.Clone();
            //temp.Multiply((1d / ball1.Ball.Mass) + (1d / ball2.Ball.Mass));
            temp.Multiply(1d / ball1.Ball.Mass);		// pretend ball2 has infinite mass, so 1 over infinity is zero
            double divisor = MyVector.Dot(normal, temp);

            // Impulse
            double impulse = numerator / divisor;

            // ------------------------------ Calculate the first object's new velocity
            // ------------------------------ velocity = Va + ( impulse / Ma )Normal
            ball1.Ball.Velocity.Add(normal * (impulse / ball1.Ball.Mass));

            // ------------------------------ Calculate the second object's new velocity
            // ------------------------------ velocity = Vb - ( impulse / Mb )Normal
            //ball2.Ball.Velocity.Subtract(normal * (impulse / ball2.Ball.Mass));		// ball2 is stationary
        }
        protected void Collide_TorqueBallTorqueBall(BallBlip ball1, BallBlip ball2)
        {
            // See if they're sitting directly on top of each other
            if (Utility3D.IsNearValue(ball1.Sphere.Position.X, ball2.Sphere.Position.X) && Utility3D.IsNearValue(ball1.Sphere.Position.Y, ball2.Sphere.Position.Y) && Utility3D.IsNearValue(ball1.Sphere.Position.Z, ball2.Sphere.Position.Z))
            {
                // They're sitting directly on top of each other.  Move them slightly away from each other
                ball2.Sphere.Position.X += .001;
                ball2.Sphere.Position.Y += .001;
            }

            // Get the collision normal and points of contact relative to the center of mass (assume they are spheres)
            MyVector normal, pointOfContact1, pointOfContact2;
            double normalMagnitude;
            GetCollisionNormalAndPointsOfContact_SphereSphere(out normal, out normalMagnitude, out pointOfContact1, out pointOfContact2, ball1, ball2);

            // ------------------------------ Calculate the impulse
            // ------------------------------ impulse = ( -(1+elasticity)(Va - Vb) dot Normal ) / ( Normal dot ((1/Ma) + (1/Mb))Normal + [a lot of rotation calculation])
            #region Numerator

            // Figure out the velocities at the point of contact, which is linear + (angular x pointofcontact)
            MyVector velocity1 = MyVector.Cross(ball1.TorqueBall.AngularVelocity, pointOfContact1);
            velocity1.Add(ball1.Ball.Velocity);

            MyVector velocity2 = MyVector.Cross(ball2.TorqueBall.AngularVelocity, pointOfContact2);
            velocity2.Add(ball2.Ball.Velocity);

            MyVector contactVelocity = velocity1 - velocity2;

            double numerator = MyVector.Dot(contactVelocity, normal);		// get the relative velocity dot normal
            numerator *= -1d * (1d + ((ball1.Ball.Elasticity + ball2.Ball.Elasticity) / 2d));

            #endregion

            if (numerator > 0d)
            {
                // Technically, there's no collision
                return;
            }

            #region Denominator

            double sumOf1OverMass = (1d / ball1.Ball.Mass) + (1d / ball2.Ball.Mass);

            // (inverse inertia tensor * (pointofcontact x normal)) x pointofcontact
            MyVector i1 = MyVector.Cross(pointOfContact1, normal);
            i1 = MyMatrix3.Multiply(ball1.TorqueBall.InertialTensorBodyInverse, i1);
            i1 = MyVector.Cross(i1, pointOfContact1);

            // (inverse inertia tensor * (pointofcontact x normal)) x pointofcontact
            MyVector i2 = MyVector.Cross(pointOfContact2, normal);
            i2 = MyMatrix3.Multiply(ball2.TorqueBall.InertialTensorBodyInverse, i2);
            i2 = MyVector.Cross(i2, pointOfContact2);

            double denominator = sumOf1OverMass + MyVector.Dot(i1, normal) + MyVector.Dot(i2, normal);

            #endregion

            double impulse = numerator / denominator;

            // ------------------------------ Calculate Linear Velocity
            // ------------------------------ velocity = Va + ( impulse / Ma )Normal
            // ------------------------------ velocity = Vb - ( impulse / Mb )Normal
            ball1.Ball.Velocity.Add(normal * (impulse / ball1.Ball.Mass));
            ball2.Ball.Velocity.Subtract(normal * (impulse / ball2.Ball.Mass));

            // ------------------------------ Calculate Angular Velocity (collision)
            // For spheres with mass in the center, there will be no spin as a result of the collision (because the collision
            // normal points directly to the center).  But if the center of mass is elsewhere, there will be a spin.
            MyVector deltaAngularMomentum = normal * impulse;
            deltaAngularMomentum = MyVector.Cross(pointOfContact1, deltaAngularMomentum);
            ball1.TorqueBall.AngularMomentum.Add(deltaAngularMomentum);

            deltaAngularMomentum = normal * (impulse * -1d);
            deltaAngularMomentum = MyVector.Cross(pointOfContact2, deltaAngularMomentum);
            ball2.TorqueBall.AngularMomentum.Add(deltaAngularMomentum);

            #region Friction

            // The direction of friction must always be tangent to the collision normal, and in reverse of the sliding velocity.
            // It might be easier to think of a block sitting on a ramp.  The normal of the collision with the ramp points toward
            // the block.  The tangent of that is the surface of the ramp.  If friction pointed anywhere but along the surface of
            // the ramp, the block would go airborne.

            // Figure out the direction of friction
            MyVector tangentDirection = contactVelocity - (normal * MyVector.Dot(normal, contactVelocity));
            tangentDirection.BecomeUnitVector();
            MyVector frictionDirection = tangentDirection * -1.0d;

            // See how much of the contact velocity is along the tangent
            double tangentSpeed = MyVector.Dot(tangentDirection, contactVelocity);

            // Figure out how much of an impulse it would take to exactly stop the objects
            double impulseToStop = GetImpulseForceMagnitudeFromDeltaVelocity(tangentSpeed, pointOfContact1, pointOfContact2, frictionDirection, sumOf1OverMass, ball1.TorqueBall.InertialTensorBodyInverse, ball2.TorqueBall.InertialTensorBodyInverse);

            // Take the average of the two object's static friction, and multiply by size of the normal
            double maxStaticFrictionForce = normalMagnitude * ((ball1.Ball.StaticFriction + ball2.Ball.StaticFriction) * .5d);

            MyVector frictionImpulse;

            // Figure out which friction coeficient to use
            if (maxStaticFrictionForce < impulseToStop)
            {
                // Use kinetic friction (slipping).  Kinetic friction is always less than static.
                double kineticFrictionForce = normalMagnitude * ((ball1.Ball.KineticFriction + ball2.Ball.KineticFriction) * .5d);
                frictionImpulse = frictionDirection * kineticFrictionForce;
            }
            else
            {
                // Use static friction (not slipping).  I don't want to multiply by maxStaticFrictionForce, because
                // that is too large.  I want to use the value that will perfectly stop the objects
                frictionImpulse = frictionDirection * impulseToStop;
            }

            // ------------------------------ Apply Linear Velocity
            ball1.Ball.Velocity.Add(frictionImpulse * (1d / ball1.Ball.Mass));
            ball2.Ball.Velocity.Subtract(frictionImpulse * (1d / ball2.Ball.Mass));

            // ------------------------------ Apply Angular Momentum
            ball1.TorqueBall.AngularMomentum.Add(MyVector.Cross(pointOfContact1, frictionImpulse));
            ball2.TorqueBall.AngularMomentum.Subtract(MyVector.Cross(pointOfContact2, frictionImpulse));

            #endregion
        }
        protected void Collide_TorqueBallTorqueBall_2IsStationary(BallBlip ball1, BallBlip ball2)
        {
            // See if they're sitting directly on top of each other
            if (Utility3D.IsNearValue(ball1.Sphere.Position.X, ball2.Sphere.Position.X) && Utility3D.IsNearValue(ball1.Sphere.Position.Y, ball2.Sphere.Position.Y) && Utility3D.IsNearValue(ball1.Sphere.Position.Z, ball2.Sphere.Position.Z))
            {
                // They're sitting directly on top of each other.  Move them slightly away from each other
                ball2.Sphere.Position.X += .001;
                ball2.Sphere.Position.Y += .001;
            }

            // Get the collision normal and points of contact relative to the center of mass (assume they are spheres)
            MyVector normal, pointOfContact1, pointOfContact2;
            double normalMagnitude;
            GetCollisionNormalAndPointsOfContact_SphereSphere(out normal, out normalMagnitude, out pointOfContact1, out pointOfContact2, ball1, ball2);

            // ------------------------------ Calculate the impulse
            // ------------------------------ impulse = ( -(1+elasticity)(Va - Vb) dot Normal ) / ( Normal dot ((1/Ma) + (1/Mb))Normal + [a lot of rotation calculation])
            #region Numerator

            // Figure out the velocities at the point of contact, which is linear + (angular x pointofcontact)
            MyVector velocity1 = MyVector.Cross(ball1.TorqueBall.AngularVelocity, pointOfContact1);
            velocity1.Add(ball1.Ball.Velocity);

            MyVector velocity2 = MyVector.Cross(ball2.TorqueBall.AngularVelocity, pointOfContact2);
            velocity2.Add(ball2.Ball.Velocity);

            MyVector contactVelocity = velocity1 - velocity2;

            double numerator = MyVector.Dot(contactVelocity, normal);		// get the relative velocity dot normal
            numerator *= -1d * (1d + ((ball1.Ball.Elasticity + ball2.Ball.Elasticity) / 2d));

            #endregion

            if (numerator > 0d)
            {
                // Technically, there's no collision
                return;
            }

            #region Denominator

            double sumOf1OverMass = (1d / ball1.Ball.Mass);		// pretend ball2 has infinite mass, so 1 over infinity is zero

            // (inverse inertia tensor * (pointofcontact x normal)) x pointofcontact
            MyVector i1 = MyVector.Cross(pointOfContact1, normal);
            i1 = MyMatrix3.Multiply(ball1.TorqueBall.InertialTensorBodyInverse, i1);
            i1 = MyVector.Cross(i1, pointOfContact1);


            //TODO:  may need to skip i2 if stationary (not rotatable)

            // (inverse inertia tensor * (pointofcontact x normal)) x pointofcontact
            MyVector i2 = MyVector.Cross(pointOfContact2, normal);
            i2 = MyMatrix3.Multiply(ball2.TorqueBall.InertialTensorBodyInverse, i2);
            i2 = MyVector.Cross(i2, pointOfContact2);

            double denominator = sumOf1OverMass + MyVector.Dot(i1, normal) + MyVector.Dot(i2, normal);

            #endregion

            double impulse = numerator / denominator;

            // ------------------------------ Calculate Linear Velocity
            // ------------------------------ velocity = Va + ( impulse / Ma )Normal
            // ------------------------------ velocity = Vb - ( impulse / Mb )Normal
            ball1.Ball.Velocity.Add(normal * (impulse / ball1.Ball.Mass));
            //ball2.Ball.Velocity.Subtract(normal * (impulse / ball2.Ball.Mass));

            // ------------------------------ Calculate Angular Velocity (collision)
            // For spheres with mass in the center, there will be no spin as a result of the collision (because the collision
            // normal points directly to the center).  But if the center of mass is elsewhere, there will be a spin.
            MyVector deltaAngularMomentum = normal * impulse;
            deltaAngularMomentum = MyVector.Cross(pointOfContact1, deltaAngularMomentum);
            ball1.TorqueBall.AngularMomentum.Add(deltaAngularMomentum);

            if (ball2.CollisionStyle == CollisionStyle.StationaryRotatable)
            {
                deltaAngularMomentum = normal * (impulse * -1d);
                deltaAngularMomentum = MyVector.Cross(pointOfContact2, deltaAngularMomentum);
                ball2.TorqueBall.AngularMomentum.Add(deltaAngularMomentum);
            }

            #region Friction

            // The direction of friction must always be tangent to the collision normal, and in reverse of the sliding velocity.
            // It might be easier to think of a block sitting on a ramp.  The normal of the collision with the ramp points toward
            // the block.  The tangent of that is the surface of the ramp.  If friction pointed anywhere but along the surface of
            // the ramp, the block would go airborne.

            // Figure out the direction of friction
            MyVector tangentDirection = contactVelocity - (normal * MyVector.Dot(normal, contactVelocity));
            tangentDirection.BecomeUnitVector();
            MyVector frictionDirection = tangentDirection * -1.0d;

            // See how much of the contact velocity is along the tangent
            double tangentSpeed = MyVector.Dot(tangentDirection, contactVelocity);

            // Figure out how much of an impulse it would take to exactly stop the objects
            double impulseToStop = GetImpulseForceMagnitudeFromDeltaVelocity(tangentSpeed, pointOfContact1, pointOfContact2, frictionDirection, sumOf1OverMass, ball1.TorqueBall.InertialTensorBodyInverse, ball2.TorqueBall.InertialTensorBodyInverse);

            // Take the average of the two object's static friction, and multiply by size of the normal
            double maxStaticFrictionForce = normalMagnitude * ((ball1.Ball.StaticFriction + ball2.Ball.StaticFriction) * .5d);

            MyVector frictionImpulse;

            // Figure out which friction coeficient to use
            if (maxStaticFrictionForce < impulseToStop)
            {
                // Use kinetic friction (slipping).  Kinetic friction is always less than static.
                double kineticFrictionForce = normalMagnitude * ((ball1.Ball.KineticFriction + ball2.Ball.KineticFriction) * .5d);
                frictionImpulse = frictionDirection * kineticFrictionForce;
            }
            else
            {
                // Use static friction (not slipping).  I don't want to multiply by maxStaticFrictionForce, because
                // that is too large.  I want to use the value that will perfectly stop the objects
                frictionImpulse = frictionDirection * impulseToStop;
            }

            // ------------------------------ Apply Linear Velocity
            ball1.Ball.Velocity.Add(frictionImpulse * (1d / ball1.Ball.Mass));
            //ball2.Ball.Velocity.Subtract(frictionImpulse * (1d / ball2.Ball.Mass));

            // ------------------------------ Apply Angular Momentum
            ball1.TorqueBall.AngularMomentum.Add(MyVector.Cross(pointOfContact1, frictionImpulse));
            if (ball2.CollisionStyle == CollisionStyle.StationaryRotatable)
            {
                ball2.TorqueBall.AngularMomentum.Subtract(MyVector.Cross(pointOfContact2, frictionImpulse));
            }

            #endregion
        }

        protected void GetCollisionNormalAndPointsOfContact_SphereSphere(out MyVector normal, out double normalMagnitude, out MyVector pointOfContact1, out MyVector pointOfContact2, BallBlip ball1, BallBlip ball2)
        {
            // Vector that is perpendicular to the tangent of the collision, and it points in the direction of object 1.  Real
            // easy when dealing with spheres     :)
            normal = ball2.Ball.Position - ball1.Ball.Position;

            // Remember this length
            normalMagnitude = normal.GetMagnitude();

            // This needs to be returned as a unit vector
            normal.Divide(normalMagnitude);

            // Start them off as unit vectors
            pointOfContact1 = normal.Clone();
            pointOfContact2 = normal.Clone();

            // Finish (use the ratio of their radii)
            pointOfContact1.Multiply((ball1.Ball.Radius / (ball1.Ball.Radius + ball2.Ball.Radius)) * normalMagnitude);
            pointOfContact2.Multiply((ball2.Ball.Radius / (ball1.Ball.Radius + ball2.Ball.Radius)) * normalMagnitude * -1);		// I want this one pointing the other direction

            // Now that I have the points of contact relative to the centers of position, I need to make them
            // relative to the centers of mass
            if (ball1.TorqueBall != null)
            {
                pointOfContact1.Subtract(ball1.TorqueBall.CenterOfMass);
            }

            if (ball2.TorqueBall != null)
            {
                pointOfContact2.Subtract(ball2.TorqueBall.CenterOfMass);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Impulse force to change the velocity at a certain point
        /// </summary>
        /// <remarks>
        /// R1  =   Contact point on body 1
        /// 
        /// J	=	DeltaVelocity / [(1/m1 + 1/m2 + N dot (((R1 x N) * inverseTensor1) x R1) +  N dot (((R2 x N) * inverseTensor2) x R2))]
        /// 
        /// Denominator should never be negative, but due to floating point inaccuracies this seems to sometimes happen!
        /// </remarks>
        private double GetImpulseForceMagnitudeFromDeltaVelocity(double deltaVelocity, MyVector pointOfContact1, MyVector pointOfContact2, MyVector lineOfAction, double sumOf1OverMass, MyMatrix3 inverseTensor1, MyMatrix3 inverseTensor2)
        {
            MyVector R1crossNtimesJ1 = MyMatrix3.Multiply(inverseTensor1, MyVector.Cross(pointOfContact1, lineOfAction));
            MyVector R2CrossNtimesJ2 = MyMatrix3.Multiply(inverseTensor2, MyVector.Cross(pointOfContact2, lineOfAction));

            double denominator = sumOf1OverMass +
                MyVector.Dot(lineOfAction, MyVector.Cross(R1crossNtimesJ1, pointOfContact1)) +
                MyVector.Dot(lineOfAction, MyVector.Cross(R2CrossNtimesJ2, pointOfContact2));

            return (deltaVelocity / denominator);
        }

        #region Triangle-Triangle collision functions

        /// <summary>
        /// This version computes the line of intersection as well (if they are not coplanar):
        /// coplanar returns whether the tris are coplanar
        /// isectpt1, isectpt2 are the endpoints of the line of intersection
        /// </summary>
        /// <remarks>
        /// by Tomas Moller, 1997.
        /// See article "A Fast Triangle-Triangle Intersection Test",
        /// Journal of Graphics Tools, 2(2), 1997
        /// updated: 2001-06-20 (added line of intersection)
        /// </remarks>
        private static bool TriTriIntersectWithIsectline(out bool isCoplanar, out MyVector[] intersectPoints, Triangle triangle1, Triangle triangle2, bool doTestIfCoplanar)
        {
            isCoplanar = false;
            intersectPoints = null;

            #region Part 1a

            // compute plane equation of triangle 1
            MyVector edge1 = triangle1.Vertex2 - triangle1.Vertex1;
            MyVector edge2 = triangle1.Vertex3 - triangle1.Vertex1;
            MyVector normal1 = MyVector.Cross(edge1, edge2);
            double d1 = MyVector.Dot(normal1, triangle1.Vertex1) * -1d;
            // plane equation 1: N1.X+d1=0

            // put triangle 2 into plane equation 1 to compute signed distances to the plane
            double du0 = MyVector.Dot(normal1, triangle2.Vertex1) + d1;
            double du1 = MyVector.Dot(normal1, triangle2.Vertex2) + d1;
            double du2 = MyVector.Dot(normal1, triangle2.Vertex3) + d1;

            // coplanarity robustness check
            if (Utility3D.IsNearZero(du0)) du0 = 0d;
            if (Utility3D.IsNearZero(du1)) du1 = 0d;
            if (Utility3D.IsNearZero(du2)) du2 = 0d;

            double du0du1 = du0 * du1;
            double du0du2 = du0 * du2;

            if (du0du1 > 0d && du0du2 > 0d)		// same sign on all of them + not equal 0 ?
            {
                return false;		// no intersection occurs
            }

            #endregion
            #region Part 1b

            // compute plane of triangle 2
            edge1 = triangle2.Vertex2 - triangle2.Vertex1;
            edge2 = triangle2.Vertex3 - triangle2.Vertex1;
            MyVector normal2 = MyVector.Cross(edge1, edge2);
            double d2 = MyVector.Dot(normal2, triangle2.Vertex1) * -1d;
            // plane equation 2: N2.X+d2=0

            // put V0,V1,V2 into plane equation 2
            double dv0 = MyVector.Dot(normal2, triangle1.Vertex1) + d2;
            double dv1 = MyVector.Dot(normal2, triangle1.Vertex2) + d2;
            double dv2 = MyVector.Dot(normal2, triangle1.Vertex3) + d2;

            // Check for near zero
            if (Utility3D.IsNearZero(dv0)) dv0 = 0d;
            if (Utility3D.IsNearZero(dv1)) dv1 = 0d;
            if (Utility3D.IsNearZero(dv2)) dv2 = 0d;

            double dv0dv1 = dv0 * dv1;
            double dv0dv2 = dv0 * dv2;

            if (dv0dv1 > 0d && dv0dv2 > 0d)		// same sign on all of them + not equal 0 ?
            {
                return false;		// no intersection occurs
            }

            #endregion

            // compute direction of intersection line
            MyVector intersectLineDirection = MyVector.Cross(normal1, normal2);

            #region Part 2

            // compute and index to the largest component of intersectLineDirection
            double max = Math.Abs(intersectLineDirection.X);
            int index = 0;
            double b = Math.Abs(intersectLineDirection.Y);
            double c = Math.Abs(intersectLineDirection.Z);
            if (b > max) { max = b; index = 1; }
            if (c > max) { max = c; index = 2; }

            // this is the simplified projection onto L
            double vp0 = triangle1.Vertex1[index];
            double vp1 = triangle1.Vertex2[index];
            double vp2 = triangle1.Vertex3[index];

            double up0 = triangle2.Vertex1[index];
            double up1 = triangle2.Vertex2[index];
            double up2 = triangle2.Vertex3[index];

            #endregion

            #region Part 3a

            double[] isect1 = new double[2];
            MyVector isectpointA1, isectpointA2;

            // compute interval for triangle 1
            isCoplanar = ComputeIntervalsIsEctLine(triangle1, vp0, vp1, vp2, dv0, dv1, dv2,
                                 dv0dv1, dv0dv2, out isect1[0], out isect1[1], out isectpointA1, out isectpointA2);
            if (isCoplanar)
            {
                if (doTestIfCoplanar)
                {
                    return IsCoplanarTriTri(normal1, triangle1, triangle2);
                }
                else
                {
                    return false;		// They're not going to look at the result anyway
                }
            }

            #endregion
            #region Part 3b

            double[] isect2 = new double[2];
            MyVector isectpointB1, isectpointB2;

            // compute interval for triangle 2
            ComputeIntervalsIsEctLine(triangle2, up0, up1, up2, du0, du1, du2,
                            du0du1, du0du2, out isect2[0], out isect2[1], out isectpointB1, out isectpointB2);


            int smallest1 = Sort(ref isect1[0], ref isect1[1]);
            int smallest2 = Sort(ref isect2[0], ref isect2[1]);

            if (isect1[1] < isect2[0] || isect2[1] < isect1[0])
            {
                return false;
            }

            #endregion

            // at this point, we know that the triangles intersect

            intersectPoints = new MyVector[2];

            #region Part 4 (edge clipping)

            if (isect2[0] < isect1[0])
            {
                if (smallest1 == 0) { intersectPoints[0] = isectpointA1.Clone(); }
                else { intersectPoints[0] = isectpointA2.Clone(); }

                if (isect2[1] < isect1[1])
                {
                    if (smallest2 == 0) { intersectPoints[1] = isectpointB2.Clone(); }
                    else { intersectPoints[1] = isectpointB1.Clone(); }
                }
                else
                {
                    if (smallest1 == 0) { intersectPoints[1] = isectpointA2.Clone(); }
                    else { intersectPoints[1] = isectpointA1.Clone(); }
                }
            }
            else
            {
                if (smallest2 == 0) { intersectPoints[0] = isectpointB1.Clone(); }
                else { intersectPoints[0] = isectpointB2.Clone(); }

                if (isect2[1] > isect1[1])
                {
                    if (smallest1 == 0) { intersectPoints[1] = isectpointA2.Clone(); }
                    else { intersectPoints[1] = isectpointA1.Clone(); }
                }
                else
                {
                    if (smallest2 == 0) { intersectPoints[1] = isectpointB2.Clone(); }
                    else { intersectPoints[1] = isectpointB1.Clone(); }
                }
            }

            #endregion

            return true;
        }

        /// <summary>
        /// This function checks if the point passed in is inside the triangle
        /// </summary>
        private static bool IsPointInTriangle(MyVector testPoint, MyVector vertex1, MyVector vertex2, MyVector vertex3, int index1, int index2)
        {
            double a, b, c, d0, d1, d2;

            a = vertex2[index2] - vertex1[index2];
            b = -(vertex2[index1] - vertex1[index1]);
            c = -a * vertex1[index1] - b * vertex1[index2];
            d0 = a * testPoint[index1] + b * testPoint[index2] + c;

            a = vertex3[index2] - vertex2[index2];
            b = -(vertex3[index1] - vertex2[index1]);
            c = -a * vertex2[index1] - b * vertex2[index2];
            d1 = a * testPoint[index1] + b * testPoint[index2] + c;

            a = vertex1[index2] - vertex3[index2];
            b = -(vertex1[index1] - vertex3[index1]);
            c = -a * vertex3[index1] - b * vertex3[index2];
            d2 = a * testPoint[index1] + b * testPoint[index2] + c;

            if (d0 * d1 > 0d && d0 * d2 > 0d)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// this edge to edge test is based on Franlin Antonio's gem: 
        /// "Faster Line Segment Intersection", in Graphics Gems III, pp. 199-202
        /// </summary>
        private static bool EdgeEdgeTest(MyVector V0, MyVector U0, MyVector U1, int index1, int index2, ref double Ax, ref double Ay, ref double Bx, ref double By, ref double Cx, ref double Cy, ref double d, ref double e, ref double f)
        {
            Bx = U0[index1] - U1[index1];
            By = U0[index2] - U1[index2];
            Cx = V0[index1] - U0[index1];
            Cy = V0[index2] - U0[index2];
            f = Ay * Bx - Ax * By;
            d = By * Cx - Bx * Cy;

            if ((f > 0d && d >= 0d && d <= f) || (f < 0d && d <= 0d && d >= f))
            {
                e = Ax * Cy - Ay * Cx;
                if (f > 0d)
                {
                    if (e >= 0d && e <= f) return true;
                }
                else
                {
                    if (e <= 0d && e >= f) return true;
                }
            }

            return false;
        }

        private static bool IsEdgeAgainstTriangleEdges(MyVector edgeStart, MyVector edgeStop, MyVector vertex1, MyVector vertex2, MyVector vertex3, int index1, int index2)
        {
            double Ax, Ay, Bx, By, Cx, Cy, e, d, f;
            Ax = edgeStop[index1] - edgeStart[index1];
            Ay = edgeStop[index2] - edgeStart[index2];
            Bx = By = Cx = Cy = e = d = f = 0;

            // test edge vertex1,vertex2 against edgeStart,edgeStop
            if (EdgeEdgeTest(edgeStart, vertex1, vertex2, index1, index2, ref Ax, ref Ay, ref Bx, ref By, ref Cx, ref Cy, ref d, ref e, ref f)) return true;
            // test edge vertex2,vertex3 against edgeStart,edgeStop
            if (EdgeEdgeTest(edgeStart, vertex2, vertex3, index1, index2, ref Ax, ref Ay, ref Bx, ref By, ref Cx, ref Cy, ref d, ref e, ref f)) return true;
            // test edge vertex3,vertex2 against edgeStart,edgeStop
            if (EdgeEdgeTest(edgeStart, vertex3, vertex1, index1, index2, ref Ax, ref Ay, ref Bx, ref By, ref Cx, ref Cy, ref d, ref e, ref f)) return true;

            return false;
        }

        private static bool IsCoplanarTriTri(MyVector N, Triangle triangle1, Triangle triangle2)
        {
            int index1, index2;

            // first project onto an axis-aligned plane, that maximizes the area
            // of the triangles, compute indices: index1, index2.
            MyVector A = new MyVector(Math.Abs(N.X), Math.Abs(N.Y), Math.Abs(N.Z));

            if (A.X > A.Y)
            {
                if (A.X > A.Z)
                {
                    index1 = 1;		// A.X is greatest
                    index2 = 2;
                }
                else
                {
                    index1 = 0;		// A.Z is greatest
                    index2 = 1;
                }
            }
            else		// A.X<=A.Y
            {
                if (A.Z > A.Y)
                {
                    index1 = 0;		// A.Z is greatest
                    index2 = 1;
                }
                else
                {
                    index1 = 0;		// A.Y is greatest
                    index2 = 2;
                }
            }

            // test all edges of triangle 1 against the edges of triangle 2
            if (IsEdgeAgainstTriangleEdges(triangle1.Vertex1, triangle1.Vertex2, triangle2.Vertex1, triangle2.Vertex2, triangle2.Vertex3, index1, index2)) return true;
            if (IsEdgeAgainstTriangleEdges(triangle1.Vertex2, triangle1.Vertex3, triangle2.Vertex1, triangle2.Vertex2, triangle2.Vertex3, index1, index2)) return true;
            if (IsEdgeAgainstTriangleEdges(triangle1.Vertex3, triangle1.Vertex1, triangle2.Vertex1, triangle2.Vertex2, triangle2.Vertex3, index1, index2)) return true;

            // finally, test if tri1 is totally contained in tri2 or vice versa
            if (IsPointInTriangle(triangle1.Vertex1, triangle2.Vertex1, triangle2.Vertex2, triangle2.Vertex3, index1, index2)) return true;
            if (IsPointInTriangle(triangle2.Vertex1, triangle1.Vertex1, triangle1.Vertex2, triangle1.Vertex3, index1, index2)) return true;

            return false;
        }

        /// <summary>
        /// sort so that a LessThan or EqualTo b
        /// </summary>
        private static int Sort(ref double a, ref double b)
        {
            if (a > b)
            {
                double c = a;
                a = b;
                b = c;
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private static void Intersect2(MyVector VTX0, MyVector VTX1, MyVector VTX2, double VV0, double VV1, double VV2,
                double D0, double D1, double D2, out double isect0, out double isect1, out MyVector isectpoint0, out MyVector isectpoint1)
        {
            double tmp = D0 / (D0 - D1);
            MyVector diff;

            isect0 = VV0 + (VV1 - VV0) * tmp;
            diff = VTX1 - VTX0;
            diff.Multiply(tmp);
            isectpoint0 = diff + VTX0;
            tmp = D0 / (D0 - D2);

            isect1 = VV0 + (VV2 - VV0) * tmp;
            diff = VTX2 - VTX0;
            diff.Multiply(tmp);
            isectpoint1 = VTX0 + diff;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// True: Coplanar
        /// False: Not Coplanar
        /// </returns>
        private static bool ComputeIntervalsIsEctLine(Triangle triangle,
                               double VV0, double VV1, double VV2, double D0, double D1, double D2,
                               double D0D1, double D0D2, out double isect0, out double isect1,
                               out MyVector isectpoint0, out MyVector isectpoint1)
        {
            // Init the return values.  I'm still not sure what these mean
            isect0 = isect1 = 0d;
            isectpoint0 = isectpoint1 = null;

            if (D0D1 > 0d)
            {
                // here we know that D0D2<=0.0
                // that is D0, D1 are on the same side, D2 on the other or on the plane
                Intersect2(triangle.Vertex3, triangle.Vertex1, triangle.Vertex2, VV2, VV0, VV1, D2, D0, D1, out isect0, out isect1, out isectpoint0, out isectpoint1);
            }
            else if (D0D2 > 0d)
            {
                // here we know that d0d1<=0.0
                Intersect2(triangle.Vertex2, triangle.Vertex1, triangle.Vertex3, VV1, VV0, VV2, D1, D0, D2, out isect0, out isect1, out isectpoint0, out isectpoint1);
            }
            else if (D1 * D2 > 0d || D0 != 0d)
            {
                // here we know that d0d1<=0.0 or that D0!=0.0
                Intersect2(triangle.Vertex1, triangle.Vertex2, triangle.Vertex3, VV0, VV1, VV2, D0, D1, D2, out isect0, out isect1, out isectpoint0, out isectpoint1);
            }
            else if (D1 != 0d)
            {
                Intersect2(triangle.Vertex2, triangle.Vertex1, triangle.Vertex3, VV1, VV0, VV2, D1, D0, D2, out isect0, out isect1, out isectpoint0, out isectpoint1);
            }
            else if (D2 != 0d)
            {
                Intersect2(triangle.Vertex3, triangle.Vertex1, triangle.Vertex2, VV2, VV0, VV1, D2, D0, D1, out isect0, out isect1, out isectpoint0, out isectpoint1);
            }
            else
            {
                // triangles are coplanar
                return true;
            }

            return false;
        }

        #endregion

        private void CollideSprtBallTorqueBall(BallBlip ballBlip1, BallBlip ballBlip2)
        {
            // Make a temp torqueball to use in place of ball1
            BallBlip tempTorqueball = new BallBlip(new SolidBall(ballBlip1.Ball.Position, ballBlip1.Ball.OriginalDirectionFacing, ballBlip1.Ball.Radius, ballBlip1.Ball.Mass, ballBlip1.Ball.Elasticity, ballBlip1.Ball.KineticFriction, ballBlip1.Ball.StaticFriction, ballBlip1.Ball.BoundryLower, ballBlip1.Ball.BoundryUpper), ballBlip1.CollisionStyle, ballBlip1.Qual, ballBlip1.Token);
            tempTorqueball.Ball.Velocity.StoreNewValues(ballBlip1.Ball.Velocity);

            #region Treat them like solid balls

            if (ballBlip1.CollisionStyle == CollisionStyle.Standard)
            {
                if (ballBlip2.CollisionStyle == CollisionStyle.Standard)
                {
                    // Two standard balls
                    Collide_TorqueBallTorqueBall(tempTorqueball, ballBlip2);
                }
                else
                {
                    // One is standard, two is stationary
                    Collide_TorqueBallTorqueBall_2IsStationary(tempTorqueball, ballBlip2);
                }
            }
            else if (ballBlip2.CollisionStyle == CollisionStyle.Standard)
            {
                // One is stationary, two is standard
                Collide_TorqueBallTorqueBall_2IsStationary(ballBlip2, tempTorqueball);
            }

            // No need for an else.  There is nothing to do (both are stationary)

            #endregion

            // Apply the changes that were made to the temp torqueball onto the ball1
            ballBlip1.Ball.Velocity.StoreNewValues(tempTorqueball.Ball.Velocity);
        }

        #endregion
    }
}
