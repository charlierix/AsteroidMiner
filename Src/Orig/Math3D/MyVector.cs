using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Game.Orig.Math3D
{
    /// <summary>
    /// This is the legendary Vector.  The main intent of this class is to allow you to work with a set of coordinates as one entity.
    /// </summary>
    /// <remarks>
    /// This is equivelent to System.Windows.Media.Media3D.Vector3D
    /// </remarks>
    public class MyVector
    {
        #region Declaration Section

        // I decided to make these public, because the properties don't add any value, and those properties get hit
        // A LOT
        public double X;
        public double Y;
        public double Z;

        #endregion

        #region Constructor

        public MyVector()
        {
            this.X = 0d;
            this.Y = 0d;
            this.Z = 0d;
        }
        public MyVector(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
        public MyVector(Point point)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.Z = 0d;
        }
        public MyVector(PointF point)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.Z = 0d;
        }

        #endregion

        #region Public Properties

        public bool IsZero
        {
            get
            {
                return this.X == 0d && this.Y == 0d && this.Z == 0d;
            }
        }
        public bool IsNearZero
        {
            get
            {
                return Utility3D.IsNearZero(this);
            }
        }

        /// <summary>
        /// 0 is X, 1 is Y, 2 is Z
        /// </summary>
        /// <remarks>
        /// It's more efficient to hit the values directly, but I've seen algorighms that work best with an index
        /// </remarks>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.X;

                    case 1:
                        return this.Y;

                    case 2:
                        return this.Z;

                    default:
                        throw new ArgumentOutOfRangeException("index", index, "The index passed in must be 0 1 or 2: " + index.ToString());
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.X = value;
                        break;

                    case 1:
                        this.Y = value;
                        break;

                    case 2:
                        this.Z = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("index", index, "The index passed in must be 0 1 or 2: " + index.ToString());
                }
            }
        }

        #endregion

        #region Public Methods

        public MyVector Clone()
        {
            return new MyVector(this.X, this.Y, this.Z);
        }

        /// <summary>
        /// There are a lot of classes that expose vectors as read only properties (so the pointers can be shared).  I just made
        /// this function so it's easier to shuttle values around in one shot
        /// </summary>
        public void StoreNewValues(MyVector valuesToGrab)
        {
            this.X = valuesToGrab.X;
            this.Y = valuesToGrab.Y;
            this.Z = valuesToGrab.Z;
        }

        /// <summary>
        /// This will return my magnitude (length).  Square root is expensive, so try to use the Magnitude Squared function
        /// whenever you can.
        /// </summary>
        public double GetMagnitude()
        {
            return Math.Sqrt((this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z));
        }
        /// <summary>
        /// This is the square of my magnitude (faster than using the square root)
        /// </summary>
        public double GetMagnitudeSquared()
        {
            return (this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z);
        }

        /// <summary>
        /// This function will make me become a unit vector.  (A vector with a length of one)
        /// </summary>
        public void BecomeUnitVector()
        {
            // Get my length
            double length = this.GetMagnitude();

            // Divide each of my values by that length
            if (length != 0)
            {
                this.X /= length;
                this.Y /= length;
                this.Z /= length;
            }
        }
        /// <summary>
        /// This creates a new vector that is the vector with a length of one
        /// </summary>
        public static MyVector BecomeUnitVector(MyVector vector)
        {
            MyVector retVal = vector.Clone();
            retVal.BecomeUnitVector();

            return retVal;
        }

        /// <summary>
        /// This function takes in a destination vector, and I will tell you how much you need to rotate me in order for me to end up along
        /// that destination vector.
        /// </summary>
        /// <remarks>
        /// I gave up trying to return this in YawPitchRoll form.  I think it can be done, but there are all kinds of strange contridictions
        /// and order of operation that I've decided that is not the way things are done.  Use YawPitchRoll when receiving input from a
        /// joystick.  But when all you know is vectors, and you want to know how to rotate them, use this function.
        /// 
        /// If I am already aligned with the vector passed in, then I will return an arbitrary orthoganal, and an angle of zero.
        /// </remarks>
        /// <param name="destination">This is the vector you want me to align myself with</param>
        /// <param name="rotationAxis">This is a vector that is orthoganal to me and the vector passed in (cross product)</param>
        /// <param name="rotationRadians">This is the number of radians you must rotate me around the rotation axis in order to be aligned with the vector passed in</param>
        public void GetAngleAroundAxis(out MyVector rotationAxis, out double rotationRadians, MyVector destination)
        {
            // Grab the angle
            rotationRadians = GetAngleBetweenVectors(this, destination);
            if (Double.IsNaN(rotationRadians))
            {
                rotationRadians = 0;
            }

            // I need to pull the cross product from me to the vector passed in
            rotationAxis = Cross(this, destination);

            // If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
            if (rotationAxis.IsZero)
            {
                // If I am here, then the angle will either be 0 or PI.
                if (rotationRadians == 0)
                {
                    // The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
                    rotationAxis.X = 1;
                    rotationRadians = 0;
                }
                else
                {
                    // The vectors are pointing directly away from each other, so I will need to be more careful when I create my orthoganal.
                    rotationAxis = GetArbitraryOrhonganal(rotationAxis);
                }
            }

            //rotationAxis.BecomeUnitVector();		// It would be nice to be tidy, but not nessassary, and I don't want slow code
        }
        public void GetAngleAroundAxis(out MyQuaternion rotation, MyVector destination)
        {
            MyVector axis;
            double radians;
            GetAngleAroundAxis(out axis, out radians, destination);

            rotation = new MyQuaternion(axis, radians);
        }

        /// <summary>
        /// This function will rotate me around any arbitrary axis (no gimbal lock for me!!!!!!!)
        /// </summary>
        /// <remarks>
        /// From some web site talking about rotations (seems like good stuff, but I don't know how it's used):
        /// 
        /// // I'm not sure why this multiplication is important
        /// Quaternion totalQuat = Quaternion.MultiplyQuaternions(rotationQuat, new Quaternion(1, 0, 0, 0));      // order of multiplication is important
        /// 
        /// // Get a matrix telling me how to rotate
        /// Matrix rotationMatrix = totalQuat.ToMatrixFromUnitQuaternion();
        /// 
        /// // AND THEN??????????
        /// </remarks>
        /// <param name="rotateAround">Any vector to rotate around</param>
        /// <param name="radians">How far to rotate</param>
        public void RotateAroundAxis(MyVector rotateAround, double radians)
        {
            // Create a quaternion that represents the axis and angle passed in
            MyQuaternion rotationQuat = new MyQuaternion(rotateAround, radians);

            // Get a vector that represents me rotated by the quaternion
            MyVector newValue = rotationQuat.GetRotatedVector(this, true);

            // Store my new values
            this.X = newValue.X;
            this.Y = newValue.Y;
            this.Z = newValue.Z;
        }

        /// <summary>
        /// This just helps with quicky tester GUI's
        /// </summary>
        public Point ToPoint()
        {
            return new Point(Convert.ToInt32(this.X), Convert.ToInt32(this.Y));
        }
        public PointF ToPointF()
        {
            return new PointF(Convert.ToSingle(this.X), Convert.ToSingle(this.Y));
        }

        public override string ToString()
        {
            return this.X.ToString() + ", " + this.Y.ToString() + ", " + this.Z.ToString();
        }
        public string ToString(int significantDigits)
        {
            return this.X.ToString("N" + significantDigits.ToString()) + ", " + this.Y.ToString("N" + significantDigits.ToString()) + ", " + this.Z.ToString("N" + significantDigits.ToString());
        }

        #endregion

        #region Public Add, Subtract, Multiply, Divide

        /// <summary>
        /// This function will add the vector passed in to me.  This will change my values.
        /// </summary>
        public void Add(MyVector vector)
        {
            this.X += vector.X;
            this.Y += vector.Y;
            this.Z += vector.Z;
        }
        /// <summary>
        /// This function will add the vector passed in to me.  This will change my values.
        /// </summary>
        public void Add(double x, double y, double z)
        {
            this.X += x;
            this.Y += y;
            this.Z += z;
        }
        /// <summary>
        /// This returns a new vector that is the sum of the two passed in
        /// </summary>
        public static MyVector Add(MyVector v1, MyVector v2)
        {
            return new MyVector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        /// <summary>
        /// This function subtracts the vector passed in from me (updates this class)
        /// Ex: If I was (0,0,0), and the vector passed in is (1,1,1), my new value will be (-1,-1,-1)
        /// </summary>
        public void Subtract(MyVector vector)
        {
            this.X -= vector.X;
            this.Y -= vector.Y;
            this.Z -= vector.Z;
        }
        /// <summary>
        /// This function subtracts the vector passed in from me (updates this class)
        /// Ex: If I was (0,0,0), and the vector passed in is (1,1,1), my new value will be (-1,-1,-1)
        /// </summary>
        public void Subtract(double x, double y, double z)
        {
            this.X -= x;
            this.Y -= y;
            this.Z -= z;
        }
        /// <summary>
        /// This function subtracts me from the vector passed in (updates this class)
        /// Ex: If I was (2,2,2), and the vector passed in is (5,5,5), my new value will be (3,3,3)
        /// </summary>
        public void SubtractMeFromVector(MyVector vector)
        {
            this.X = vector.X - this.X;
            this.Y = vector.Y - this.Y;
            this.Z = vector.Z - this.Z;
        }
        /// <summary>
        /// This function subtracts me from the vector passed in (updates this class)
        /// Ex: If I was (2,2,2), and the vector passed in is (5,5,5), my new value will be (3,3,3)
        /// </summary>
        public void SubtractMeFromVector(double x, double y, double z)
        {
            this.X = x - this.X;
            this.Y = y - this.Y;
            this.Z = z - this.Z;
        }
        /// <summary>
        /// This function returns a new vector that is v1 - v2
        /// </summary>
        public static MyVector Subtract(MyVector v1, MyVector v2)
        {
            return new MyVector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        /// <summary>
        /// This function will multiply me times the constant passed in
        /// </summary>
        public void Multiply(double multiplyBy)
        {
            this.X *= multiplyBy;
            this.Y *= multiplyBy;
            this.Z *= multiplyBy;
        }
        /// <summary>
        /// This will return a new vector with a value of: vector * mult
        /// </summary>
        public static MyVector Multiply(MyVector vector, double multiplyBy)
        {
            return new MyVector(vector.X * multiplyBy, vector.Y * multiplyBy, vector.Z * multiplyBy);
        }

        /// <summary>
        /// This will divide me by the constant passed in
        /// </summary>
        public void Divide(double divideBy)
        {
            this.X /= divideBy;
            this.Y /= divideBy;
            this.Z /= divideBy;
        }
        /// <summary>
        /// This will create a new vector that is vector / const
        /// </summary>
        public static MyVector Divide(MyVector vector, double divideBy)
        {
            return new MyVector(vector.X / divideBy, vector.Y / divideBy, vector.Z / divideBy);
        }
        /// <summary>
        /// I'm not sure why you would need this, but I'll provide the function
        /// </summary>
        public void DivideMeFromConstant(double divideFrom)
        {
            this.X = divideFrom / this.X;
            this.Y = divideFrom / this.Y;
            this.Z = divideFrom / this.Z;
        }
        /// <summary>
        /// This will create a new vector that is const / vector
        /// </summary>
        public static MyVector DivideMeFromConstant(MyVector vector, double divideFrom)
        {
            return new MyVector(divideFrom / vector.X, divideFrom / vector.Y, divideFrom / vector.Z);
        }

        #endregion
        #region Operator Overloads

        /// <summary>
        /// This returns a new vector that is the sum of the two passed in
        /// </summary>
        public static MyVector operator +(MyVector v1, MyVector v2)
        {
            return new MyVector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }
        /// <summary>
        /// This function returns a new vector that is v1 - v2
        /// </summary>
        public static MyVector operator -(MyVector v1, MyVector v2)
        {
            return new MyVector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        /// <summary>
        /// This will return a new vector with a value of: vector * mult
        /// </summary>
        public static MyVector operator *(MyVector vector, double multiplyBy)
        {
            return new MyVector(vector.X * multiplyBy, vector.Y * multiplyBy, vector.Z * multiplyBy);
        }
        /// <summary>
        /// This will return a new vector with a value of: vector * mult
        /// </summary>
        public static MyVector operator *(double multiplyBy, MyVector vector)
        {
            return new MyVector(vector.X * multiplyBy, vector.Y * multiplyBy, vector.Z * multiplyBy);
        }

        /// <summary>
        /// This will create a new vector that is vector / const
        /// </summary>
        public static MyVector operator /(MyVector vector, double divideBy)
        {
            return new MyVector(vector.X / divideBy, vector.Y / divideBy, vector.Z / divideBy);
        }
        /// <summary>
        /// This will create a new vector that is const / vector
        /// </summary>
        public static MyVector operator /(double divideFrom, MyVector vector)
        {
            return new MyVector(divideFrom / vector.X, divideFrom / vector.Y, divideFrom / vector.Z);
        }

        #endregion

        #region Public Static Methods (that aren't overloads of instance methods)

        /// <summary>
        /// I will internally rotate the vectors around, get the Z component to drop out, and only return Theta.  I will not change the values
        /// of the vectors passed in
        /// </summary>
        public static double GetAngleBetweenVectors(MyVector v1, MyVector v2)
        {
            // Get the dot product of the two vectors (I use retVal, just because it makes a convenient temp variable)
            double retVal = Dot(MyVector.BecomeUnitVector(v1), MyVector.BecomeUnitVector(v2));

            // Now pull the arccos of the dot product
            retVal = Math.Acos(retVal);

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This gives the dot product between the two vectors passed in
        /// NOTE:  This overload does not normalize them first (a standard dot product)
        /// </summary>
        public static double Dot(MyVector v1, MyVector v2)
        {
            // For speed reasons, I want to rewrite the function, rather than call my overload with false
            return (v1.X * v2.X) +
                        (v1.Y * v2.Y) +
                        (v1.Z * v2.Z);
        }
        /// <summary>
        /// This gives the dot product between the two vectors passed in.  This overload allows for the
        /// vectors to be normalized first.
        /// </summary>
        /// <param name="becomeUnitVectors">
        /// This tells the funtion to make them unit vectors first (forces the output to -1 to 1)
        /// NOTE:  If you're going to pass false, then use the simpler overload.  It will avoid the if statement.
        /// </param>
        public static double Dot(MyVector v1, MyVector v2, bool becomeUnitVectors)
        {
            // See if they need to become unit vectors first
            if (becomeUnitVectors)
            {
                // Turn the vectors passed in into unit vectors first
                MyVector working1 = MyVector.BecomeUnitVector(v1);
                MyVector working2 = MyVector.BecomeUnitVector(v2);

                // Exit Function
                return (working1.X * working2.X) +
                            (working1.Y * working2.Y) +
                            (working1.Z * working2.Z);
            }
            else
            {
                return (v1.X * v2.X) +
                            (v1.Y * v2.Y) +
                            (v1.Z * v2.Z);
            }
        }

        /// <summary>
        /// This gives the cross product between the two vectors passed in
        /// </summary>
        /// <remarks>
        /// I think this is right handed
        /// </remarks>
        public static MyVector Cross(MyVector v1, MyVector v2)
        {
            return new MyVector(v1.Y * v2.Z - v1.Z * v2.Y,
                                            v1.Z * v2.X - v1.X * v2.Z,
                                            v1.X * v2.Y - v1.Y * v2.X);
        }

        /// <summary>
        /// This function will pick an arbitrary orthogonal to the vector passed in.  This will only be usefull if you are going
        /// to rotate 180
        /// </summary>
        public static MyVector GetArbitraryOrhonganal(MyVector vector)
        {
            // Clone the vector passed in
            MyVector retVal = vector.Clone();

            // Make sure that none of the values are equal to zero.
            if (retVal.X == 0) retVal.X = 0.000000001d;
            if (retVal.Y == 0) retVal.Y = 0.000000001d;
            if (retVal.Z == 0) retVal.Z = 0.000000001d;

            // Figure out the orthogonal X and Y slopes
            double orthM = (retVal.X * -1) / retVal.Y;
            double orthN = (retVal.Y * -1) / retVal.Z;

            // When calculating the new coords, I will default Y to 1, and find an X and Z that satisfy that.  I will go ahead and reuse the retVal
            retVal.Y = 1;
            retVal.X = 1 / orthM;
            retVal.Z = orthN;

            // Exit Function
            return retVal;
        }

        #endregion
    }
}
