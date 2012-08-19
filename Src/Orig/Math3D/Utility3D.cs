using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Game.Orig.Math3D
{
	#region Class: DoubleVector

	/// <summary>
	/// This is useful for holding pairs of vectors (a vector, and some orthoganal to that vector).  There is nothing in this class
	/// that enforces that, it's just a simple container
	/// </summary>
	public class DoubleVector
	{
		public MyVector Standard;
		public MyVector Orth;

		public DoubleVector()
		{
			this.Standard = null;
			this.Orth = null;
		}
		public DoubleVector(MyVector standard, MyVector orthogonalToStandard)
		{
			this.Standard = standard;
			this.Orth = orthogonalToStandard;
		}
		public DoubleVector(double standardX, double standardY, double standardZ, double orthogonalX, double orthogonalY, double orthogonalZ)
		{
			this.Standard = new MyVector(standardX, standardY, standardZ);
			this.Orth = new MyVector(orthogonalX, orthogonalY, orthogonalZ);
		}

		public DoubleVector Clone()
		{
			return new DoubleVector(this.Standard.Clone(), this.Orth.Clone());
		}

        /// <summary>
        /// This function takes in a destination double vector, and I will tell you how much you need to rotate me in order for me to end up
        /// along that destination double vector.
        /// </summary>
        /// <remarks>
        /// This function is a mutated copy of MyVector.GetAngleBetweenVectors.  It is almost identical, but slightly more complex  :)
        /// 
        /// If I am already aligned with the vector passed in, then I will return an arbitrary orthoganal, and an angle of zero.
        /// </remarks>
        /// <param name="destination">This is the double vector you want me to align myself with</param>
        public MyQuaternion GetAngleAroundAxis(DoubleVector destination)
        {
            #region Standard

            //	Get the angle
            double rotationRadians = MyVector.GetAngleBetweenVectors(this.Standard, destination.Standard);
            if (Double.IsNaN(rotationRadians))
            {
                rotationRadians = 0d;
            }

            //	I need to pull the cross product from me to the vector passed in
            MyVector rotationAxis = MyVector.Cross(this.Standard, destination.Standard);

            //	If the cross product is zero, then there are two possibilities.  The vectors point in the same direction, or opposite directions.
            if (rotationAxis.IsNearZero)
            {
                //	If I am here, then the angle will either be 0 or PI.
                if (Utility3D.IsNearZero(rotationRadians))
                {
                    //	The vectors sit on top of each other.  I will set the orthoganal to an arbitrary value, and return zero for the radians
                    rotationAxis.X = 1d;
                    rotationAxis.Y = 0d;
                    rotationAxis.Z = 0d;
                    rotationRadians = 0d;
                }
                else
                {
                    //	The vectors are pointing directly away from each other, because this is a double vector, I must rotate along an axis that
                    // is orthogonal to my standard and orth
                    rotationAxis = this.Orth; //MyVector.Cross(this.Standard, this.Orth);
                }
            }

            MyQuaternion quatStandard = new MyQuaternion(rotationAxis, rotationRadians);



            //return quatStandard;




            #endregion

            // I only need to rotate the orth, because I already know where the standard will be
            MyVector rotatedOrth = quatStandard.GetRotatedVector(this.Orth, true);

            #region Orthogonal

            //	Grab the angle
            rotationRadians = MyVector.GetAngleBetweenVectors(rotatedOrth, destination.Orth);
            if (Double.IsNaN(rotationRadians))
            {
                rotationRadians = 0d;
            }

            // Since I've rotated the standards onto each other, the rotation axis of the orth is the standard (asumming it was truely orthogonal
            // to begin with)
            rotationAxis = destination.Standard.Clone();

            MyQuaternion quatOrth = new MyQuaternion(rotationAxis, rotationRadians);

            #endregion

            // Exit Function
            //return MyQuaternion.Multiply(quatOrth, quatStandard);
            return MyQuaternion.Multiply(quatStandard, quatOrth);
        }
	}

	#endregion
	#region Class: Triangle

	/// <summary>
	/// The basic building block of polygons.  Use the right hand rule.
	/// </summary>
	/// <remarks>
	/// I hate using double the memory for each triangle when people are only going to use one or the other
	/// set of properties (vectors or uniquevalues[pointer]), but I figure it's easier to code against.  Besides, these
	/// triangles should only be used for physics, not graphics, so the total number of triangles should be fewer.
	/// (use DirectX's, or OpenGL's, or XAML3D's, or whoever's primitives for graphics)
	/// </remarks>
	public class Triangle
	{
		#region Declaration Section

		public MyVector Vertex1;
		public MyVector Vertex2;
		public MyVector Vertex3;

		//	These pointers are used to point to a list of vectors that are shared across many triangles
		//	Otherwise, they are -1
		public int Pointer1;
		public int Pointer2;
		public int Pointer3;

		#endregion

		#region Constructor

		public Triangle()
		{
			this.Vertex1 = null;
			this.Vertex2 = null;
			this.Vertex3 = null;

			this.Pointer1 = -1;
			this.Pointer2 = -1;
			this.Pointer3 = -1;
		}
		public Triangle(MyVector vertex1, MyVector vertex2, MyVector vertex3)
		{
			this.Vertex1 = vertex1;
			this.Vertex2 = vertex2;
			this.Vertex3 = vertex3;

			this.Pointer1 = -1;
			this.Pointer2 = -1;
			this.Pointer3 = -1;
		}
		public Triangle(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3)
		{
			this.Vertex1 = new MyVector(x1, y1, z1);
			this.Vertex2 = new MyVector(x2, y2, z2);
			this.Vertex3 = new MyVector(x3, y3, z3);

			this.Pointer1 = -1;
			this.Pointer2 = -1;
			this.Pointer3 = -1;
		}
		public Triangle(MyVector[] uniquePoints, int pointer1, int pointer2, int pointer3)
		{
			this.Vertex1 = uniquePoints[pointer1];
			this.Vertex2 = uniquePoints[pointer2];
			this.Vertex3 = uniquePoints[pointer3];

			this.Pointer1 = pointer1;
			this.Pointer2 = pointer2;
			this.Pointer3 = pointer3;
		}

		#endregion

		#region Public Properties

		public MyVector Normal
		{
			get
			{
				MyVector retVal = MyVector.Cross(this.Vertex3 - this.Vertex2, this.Vertex1 - this.Vertex2);
				retVal.BecomeUnitVector();

				return retVal;
			}
		}
		public double DistanceFromOriginAlongNormal
		{
			get
			{
				return MyVector.Dot(this.Normal, this.Vertex1);
			}
		}

		#endregion

		#region Public Methods

		public Triangle Clone()
		{
			return new Triangle(this.Vertex1.Clone(), this.Vertex2.Clone(), this.Vertex3.Clone());
		}
		public Triangle Clone(MyVector[] clonedUniquePoints)
		{
			return new Triangle(clonedUniquePoints, this.Pointer1, this.Pointer2, this.Pointer3);
		}

		public PointF[] ToPointF()
		{
			PointF[] retVal = new PointF[3];

			retVal[0] = this.Vertex1.ToPointF();
			retVal[1] = this.Vertex2.ToPointF();
			retVal[2] = this.Vertex3.ToPointF();

			return retVal;
		}

		public MyVector GetClosestPointOnTriangle(MyVector point)
		{

			MyVector ab = this.Vertex2 - this.Vertex1;
			MyVector ac = this.Vertex3 - this.Vertex1;
			MyVector bc = this.Vertex3 - this.Vertex2;

			MyVector ap = point - this.Vertex1;
			MyVector bp = point - this.Vertex2;
			MyVector cp = point - this.Vertex3;

			// Compute parametric position s for projection P' of P on AB,
			// P' = A + s*AB, s = snom/(snom+sdenom)
			double snom = MyVector.Dot(ap, ab);
			double sdenom = MyVector.Dot(bp, this.Vertex1 - this.Vertex2);

			// Compute parametric position t for projection P' of P on AC,
			// P' = A + t*AC, s = tnom/(tnom+tdenom)
			double tnom = MyVector.Dot(ap, ac);
			double tdenom = MyVector.Dot(cp, this.Vertex1 - this.Vertex3);

			if (snom <= 0d && tnom <= 0d)
			{
				return this.Vertex1; // Vertex region early out
			}

			// Compute parametric position u for projection P' of P on BC,
			// P' = B + u*BC, u = unom/(unom+udenom)
			double unom = MyVector.Dot(bp, bc);
			double udenom = MyVector.Dot(cp, this.Vertex2 - this.Vertex3);

			if (sdenom <= 0d && unom <= 0d)
			{
				return this.Vertex2; // Vertex region early out
			}
			if (tdenom <= 0d && udenom <= 0d)
			{
				return this.Vertex3; // Vertex region early out
			}

			MyVector pa = this.Vertex1 - point;
			MyVector pb = this.Vertex2 - point;

			// P is outside (or on) AB if the triple scalar product [N PA PB] <= 0
			MyVector n = MyVector.Cross(ab, ac);
			double vc = MyVector.Dot(n, MyVector.Cross(pa, pb));
			// If P outside AB and within feature region of AB,
			// return projection of P onto AB
			if (vc <= 0d && snom >= 0d && sdenom >= 0d)
			{
				return this.Vertex1 + snom / (snom + sdenom) * ab;
			}

			MyVector pc = this.Vertex3 - point;

			// P is outside (or on) BC if the triple scalar product [N PB PC] <= 0
			double va = MyVector.Dot(n, MyVector.Cross(pb, pc));
			// If P outside BC and within feature region of BC,
			// return projection of P onto BC
			if (va <= 0d && unom >= 0d && udenom >= 0d)
			{
				return this.Vertex2 + unom / (unom + udenom) * bc;
			}

			// P is outside (or on) CA if the triple scalar product [N PC PA] <= 0
			double vb = MyVector.Dot(n, MyVector.Cross(pc, pa));
			// If P outside CA and within feature region of CA,
			// return projection of P onto CA
			if (vb <= 0d && tnom >= 0d && tdenom >= 0d)
			{
				return this.Vertex1 + tnom / (tnom + tdenom) * ac;
			}

			// P must project inside face region. Compute Q using barycentric coordinates
			double u = va / (va + vb + vc);
			double v = vb / (va + vb + vc);
			double w = 1d - u - v; // = vc / (va + vb + vc)
			return u * this.Vertex1 + v * this.Vertex2 + w * this.Vertex3;
		}

		#endregion

		#region Operator Overloads

		public static Triangle operator +(MyVector vector, Triangle triangle)
		{
			return new Triangle(triangle.Vertex1 + vector, triangle.Vertex2 + vector, triangle.Vertex3 + vector);
		}
		public static Triangle operator +(Triangle triangle, MyVector vector)
		{
			return new Triangle(triangle.Vertex1 + vector, triangle.Vertex2 + vector, triangle.Vertex3 + vector);
		}

		#endregion
	}

	#endregion

	public static class Utility3D
	{
		private const double NEARZERO = .0001d;

		/// <summary>
		/// Converts degrees into radians
		/// </summary>
		public static double GetDegreesToRadians(double theta)
		{
			return (theta * Math.PI) / 180d;
		}
		/// <summary>
		/// Converts radians to degrees
		/// </summary>
		public static double GetRadiansToDegrees(double theta)
		{
			return (theta * 180) / Math.PI;
		}

		/// <summary>
		/// Get a random vector between boundry lower and boundry upper
		/// </summary>
		public static MyVector GetRandomVector(MyVector boundryLower, MyVector boundryUpper)
		{
			return GetRandomVector(new Random(), boundryLower, boundryUpper);
		}
		/// <summary>
		/// Get a random vector between boundry lower and boundry upper
		/// </summary>
		public static MyVector GetRandomVector(Random rand, MyVector boundryLower, MyVector boundryUpper)
		{
			MyVector retVal = new MyVector();

			retVal.X = boundryLower.X + (rand.NextDouble() * (boundryUpper.X - boundryLower.X));
			retVal.Y = boundryLower.Y + (rand.NextDouble() * (boundryUpper.Y - boundryLower.Y));
			retVal.Z = boundryLower.Z + (rand.NextDouble() * (boundryUpper.Z - boundryLower.Z));

			return retVal;
		}
		/// <summary>
		/// Get a random vector between maxValue*-1 and maxValue
		/// </summary>
		public static MyVector GetRandomVector(double maxValue)
		{
			return GetRandomVector(new Random(), maxValue);
		}
		/// <summary>
		/// Get a random vector between maxValue*-1 and maxValue
		/// </summary>
		public static MyVector GetRandomVector(Random rand, double maxValue)
		{
			MyVector retVal = new MyVector();

			retVal.X = GetNearZeroValue(rand, maxValue);
			retVal.Y = GetNearZeroValue(rand, maxValue);
			retVal.Z = GetNearZeroValue(rand, maxValue);

			return retVal;
		}
		/// <summary>
		/// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
		/// rather than cube)
		/// </summary>
		public static MyVector GetRandomVectorSpherical(double maxRadius)
		{
			return GetRandomVectorSpherical(new Random(), maxRadius);
		}
		/// <summary>
		/// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
		/// rather than cube)
		/// </summary>
		public static MyVector GetRandomVectorSpherical(Random rand, double maxRadius)
		{
			MyVector retVal = new MyVector(GetNearZeroValue(rand, maxRadius), 0, 0);

			MyVector rotateAxis = GetRandomVector(rand, 5d);
			double radians = GetNearZeroValue(rand, 2d * Math.PI);

			retVal.RotateAroundAxis(rotateAxis, radians);

			return retVal;
		}
		public static MyVector GetRandomVectorSpherical2D(double maxRadius)
		{
			return GetRandomVectorSpherical2D(new Random(), maxRadius);
		}
		/// <summary>
		/// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
		/// rather than cube).  Z will always be zero.
		/// </summary>
		public static MyVector GetRandomVectorSpherical2D(Random rand, double maxRadius)
		{
			MyVector retVal = new MyVector(GetNearZeroValue(rand, maxRadius), 0, 0);

			MyVector rotateAxis = new MyVector(0, 0, 1);
			double radians = GetNearZeroValue(rand, 2d * Math.PI);

			retVal.RotateAroundAxis(rotateAxis, radians);

			return retVal;
		}

		/// <summary>
		/// Gets a value between -maxValue and maxValue
		/// </summary>
		public static double GetNearZeroValue(Random rand, double maxValue)
		{
			double retVal = rand.NextDouble() * maxValue;

			if (rand.Next(0, 2) == 1)
			{
				retVal *= -1;
			}

			return retVal;
		}

		public static bool IsNearZero(double testValue)
		{
			return Math.Abs(testValue) <= NEARZERO;
		}
        public static bool IsNearZero(MyVector testVect)
        {
            return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
        }
		public static bool IsNearValue(double testValue, double compareTo)
		{
			return testValue >= compareTo - NEARZERO && testValue <= compareTo + NEARZERO;
		}

		public static double GetDistance3D(MyVector position1, MyVector position2)
		{
			return MyVector.Subtract(position2, position1).GetMagnitude();
		}

		/// <summary>
		/// This function returns the vector from returnList that is closest to testVect
		/// </summary>
		public static MyVector GetNearestVector(MyVector testVect, MyVector[] returnList)
		{
			//	Find the closest point
			double minDist = double.MaxValue;
			int minDistIndex = -1;

			double x, y, z, curDist;

			for (int returnCntr = 0; returnCntr < returnList.Length; returnCntr++)
			{
				//	Get dist squared
				x = returnList[returnCntr].X - testVect.X;
				y = returnList[returnCntr].Y - testVect.Y;
				z = returnList[returnCntr].Z - testVect.Z;

				curDist = (x * x) + (y * y) + (z * z);		//	no need to use sqrt

				//	See if this is nearer
				if (curDist < minDist)
				{
					minDist = curDist;
					minDistIndex = returnCntr;
				}
			}

			//	Exit Function
			return returnList[minDistIndex];
		}

		/// <summary>
		/// This does a deep clone
		/// </summary>
		public static MyVector[] GetClonedArray(MyVector[] vectors)
		{
			MyVector[] retVal = new MyVector[vectors.Length];

			for (int cntr = 0; cntr < vectors.Length; cntr++)
			{
				retVal[cntr] = vectors[cntr].Clone();
			}

			return retVal;
		}
		/// <summary>
		/// This does a deep clone
		/// </summary>
		public static DoubleVector[] GetClonedArray(DoubleVector[] vectorPairs)
		{
			DoubleVector[] retVal = new DoubleVector[vectorPairs.Length];

			for (int cntr = 0; cntr < vectorPairs.Length; cntr++)
			{
				retVal[cntr] = vectorPairs[cntr].Clone();
			}

			return retVal;
		}
		/// <summary>
		/// This does a deep clone
		/// </summary>
		public static Triangle[] GetClonedArray(Triangle[] triangles)
		{
			Triangle[] retVal = new Triangle[triangles.Length];

			for (int cntr = 0; cntr < triangles.Length; cntr++)
			{
				retVal[cntr] = triangles[cntr].Clone();
			}

			return retVal;
		}
		/// <summary>
		/// This does a deep clone
		/// </summary>
		public static Triangle[] GetClonedArray(MyVector[] clonedUniquePoints, Triangle[] triangles)
		{
			Triangle[] retVal = new Triangle[triangles.Length];

			for (int cntr = 0; cntr < triangles.Length; cntr++)
			{
				retVal[cntr] = triangles[cntr].Clone(clonedUniquePoints);
			}

			return retVal;
		}
	}
}
