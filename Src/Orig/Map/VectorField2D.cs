using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Math3D;

namespace Game.Orig.Map
{
	/// <remarks>
	/// I don't know where the VectorField/ValueField classes should go.  They don't belong in Math3D, and this
	/// is the next highest level
	/// 
	/// Even though this says 2D, the height could be thought to be infinite, since Z is ignored (so an object at any Z
	/// would be affected)
	/// </remarks>
	public class VectorField2D
	{
		#region Declaration Section

		private VectorField2DMode _fieldMode = VectorField2DMode.None;

		/// <summary>
		/// This is the center of the field
		/// </summary>
		/// <remarks>
		/// By moving this position, you will move the entire field around
		/// 
		/// This stays null until the constructor is finished.  That keeps ResetField from thrashing unnessassarily
		/// </remarks>
		private MyVector _position = null;

		//	This is the span in world coords of the field (remember, position is in the center of this span)
		private double _sizeX = 0;
		private double _sizeY = 0;

		//	This is how many grid cells there are along the x and y
		private int _squaresPerSideX = 1;
		private int _squaresPerSideY = 1;

		//	These are derivitives of the other values, and are cached here so they don't have to be recalculated during
		//	each request
		private double _halfSizeX = 0;
		private double _halfSizeY = 0;
		private double _sizeOfSquareX = 0;
		private double _sizeOfSquareY = 0;

		//	This is the strength of the vectors
		private double _strength = 1;

		/// <summary>
		/// These are the field lines for each grid
		/// (row * _squaresPerSideX) + column
		/// </summary>
		/// <remarks>
		/// I store this as 1D, because 2D arrays in .net are completely unoptimized
		/// </remarks>
		private MyVector[] _grid = null;

		#endregion

		#region Constructor

		public VectorField2D()
		{
			_fieldMode = VectorField2DMode.None;

			_position = new MyVector();
		}

		public VectorField2D(VectorField2DMode fieldMode, double size, int squaresPerSide, double strength, MyVector position)
			: this(fieldMode, size, size, squaresPerSide, squaresPerSide, strength, position) { }

		public VectorField2D(VectorField2DMode fieldMode, double sizeX, double sizeY, int squaresPerSideX, int squaresPerSideY, double strength, MyVector position)
		{
			//	I use the property sets to enforce constraints
			this.FieldMode = fieldMode;

			this.SizeX = sizeX;
			this.SizeY = sizeY;

			this.SquaresPerSideX = squaresPerSideX;
			this.SquaresPerSideY = squaresPerSideY;

			this.Strength = strength;

			//	By waiting until now to set the position, I've kept ResetField from running (called by the property sets)
			_position = position;

			ResetField();
		}

		#endregion

		#region Public Properties

		public VectorField2DMode FieldMode
		{
			get
			{
				return _fieldMode;
			}
			set
			{
				if (value == VectorField2DMode.Custom)
				{
					throw new ArgumentOutOfRangeException("Cannot set to custom directly.  Call SetFieldLines instead");
				}

				_fieldMode = value;

				ResetField();
			}
		}

		public MyVector Position
		{
			get
			{
				return _position;
			}
		}

		public double SizeX
		{
			get
			{
				return _sizeX;
			}
			set
			{
				if (value <= 0d)
				{
					throw new ArgumentOutOfRangeException("SizeX", value, "SizeX must be greater than zero: " + value.ToString());
				}

				_sizeX = value;
				_halfSizeX = _sizeX / 2d;
				_sizeOfSquareX = _sizeX / _squaresPerSideX;

				//	I don't need to call reset
			}
		}
		public double SizeY
		{
			get
			{
				return _sizeY;
			}
			set
			{
				if (value <= 0d)
				{
					throw new ArgumentOutOfRangeException("SizeY", value, "SizeY must be greater than zero: " + value.ToString());
				}

				_sizeY = value;
				_halfSizeY = _sizeY / 2d;
				_sizeOfSquareY = _sizeY / _squaresPerSideY;

				//	I don't need to call reset
			}
		}

		public int SquaresPerSideX
		{
			get
			{
				return _squaresPerSideX;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("SquaresPerSideX", value, "Squares Per Side (X) can't be less than 1: " + value.ToString());
				}

				_squaresPerSideX = value;
				_sizeOfSquareX = _sizeX / _squaresPerSideX;

				ResetField();
			}
		}
		public int SquaresPerSideY
		{
			get
			{
				return _squaresPerSideY;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("SquaresPerSideY", value, "Squares Per Side (Y) can't be less than 1: " + value.ToString());
				}

				_squaresPerSideY = value;
				_sizeOfSquareY = _sizeY / _squaresPerSideY;

				ResetField();
			}
		}

		public double Strength
		{
			get
			{
				return _strength;
			}
			set
			{
				if (value <= 0d)
				{
					throw new ArgumentOutOfRangeException("Strength", value, "Strength must be positive: " + value.ToString());
				}

				_strength = value;

				ResetField();
			}
		}

		#endregion

		#region Public Methods

		public MyVector GetFieldStrengthWorld(MyVector atPoint)
		{
			return GetFieldStrengthLocal(atPoint - this.Position);
		}
		public MyVector GetFieldStrengthLocal(MyVector atPoint)
		{
			int index = GetIndexForLocalPosition(atPoint);

			if (index < 0)
			{
				//	Return a strength of zero
				return new MyVector();
			}
			else
			{
				return _grid[index];
			}
		}

		/// <summary>
		/// This function helps tester forms draw the field.  All positions are in world coords.
		/// </summary>
		/// <param name="gridLines">
		/// A list of from/to linepoints
		/// line[0] is from point.  line[1] is to point.
		/// </param>
		/// <param name="fieldLines">
		/// A list of positions and field strength (the positions are in the center of each grid)
		/// line[0] is position.  line[1] is field strength.
		/// </param>
		/// <returns>
		/// True:  The grid is set up, and the out lists are filled
		/// False:  The grid is NOT set up, and the list counts are zero
		/// </returns>
		public bool GetDrawLines(out List<MyVector[]> gridLines, out List<MyVector[]> fieldLines)
		{
			if (_position == null || _grid == null)
			{
				gridLines = null;
				fieldLines = null;
				return false;
			}

			#region Build Grid Lines

			gridLines = new List<MyVector[]>();

			MyVector topLeftPos = new MyVector(_position.X - _halfSizeX, _position.Y - _halfSizeY, 0);

			//	Add vertical lines
			for (int xCntr = 0; xCntr <= _squaresPerSideX; xCntr++)		//	I say == because I need to draw the very bottom/right border
			{
				MyVector[] curLine = new MyVector[2];

				curLine[0] = new MyVector(topLeftPos.X + (xCntr * _sizeOfSquareX), topLeftPos.Y, 0);
				curLine[1] = new MyVector(curLine[0].X, topLeftPos.Y + _sizeY, 0);

				gridLines.Add(curLine);
			}

			//	Add horizontal lines
			for (int yCntr = 0; yCntr <= _squaresPerSideY; yCntr++)
			{
				MyVector[] curLine = new MyVector[2];

				curLine[0] = new MyVector(topLeftPos.X, topLeftPos.Y + (yCntr * _sizeOfSquareY), 0);
				curLine[1] = new MyVector(topLeftPos.X + _sizeX, curLine[0].Y, 0);

				gridLines.Add(curLine);
			}

			#endregion

			#region Build Field Lines

			fieldLines = new List<MyVector[]>();

			foreach(MyVector center in GetGridCenters())
			{
				MyVector[] curLine = new MyVector[2];

				curLine[0] = center;
				curLine[1] = GetFieldStrengthWorld(center);

				fieldLines.Add(curLine);
			}

			#endregion

			//	Exit Function
			return true;
		}

		/// <summary>
		/// This function returns a set of positions that are the center of each grid.  This can be used for populating the grid
		/// with custom values based on some function.
		/// </summary>
		public List<MyVector> GetGridCenters()
		{
			if(_position == null || _grid == null)
			{
				return null;
			}

			List<MyVector> retVal = new List<MyVector>();

			//	Figure out the middle of the top/left square
			MyVector posTopLeft = new MyVector(_position.X - _halfSizeX + (_sizeOfSquareX / 2d), _position.Y - _halfSizeY + (_sizeOfSquareY / 2d), 0);

			//	Shoot through all the grids
			for (int xCntr = 0; xCntr < _squaresPerSideX; xCntr++)
			{
				for (int yCntr = 0; yCntr < _squaresPerSideY; yCntr++)
				{
					retVal.Add(new MyVector(posTopLeft.X + (xCntr * _sizeOfSquareX), posTopLeft.Y + (yCntr * _sizeOfSquareY), 0));
				}
			}

			//	Exit Function
			return retVal;
		}

		/// <summary>
		/// This function stores the field lines passed in
		/// </summary>
		/// <remarks>
		/// 2D arrays are not optimized, but in this case, it's easier for the outside user to work with.
		/// </remarks>
		/// <param name="fieldLines">fieldLines[X, Y] (x and y go from 0 to SquaresPerSide - 1)</param>
		public void SetFieldLines(MyVector[,] fieldLines)
		{
			//	Check out the array
			if (fieldLines == null)
			{
				throw new ArgumentNullException("fieldLines", "fieldLines cannot be null");
			}

			if (fieldLines.GetUpperBound(0) != _squaresPerSideX - 1)
			{
				throw new ArgumentOutOfRangeException("fieldLines.GetUpperBound(0)", fieldLines.GetUpperBound(0), "fieldLines.GetUpperBound(0) must be the same as _squaresPerSideX - 1\nUBound: " + fieldLines.GetUpperBound(0).ToString() + ", _squaresPerSideX - 1: " + ((int)(_squaresPerSideX - 1)).ToString());
			}

			if (fieldLines.GetUpperBound(1) != _squaresPerSideY - 1)
			{
				throw new ArgumentOutOfRangeException("fieldLines.GetUpperBound(1)", fieldLines.GetUpperBound(1), "fieldLines.GetUpperBound(1) must be the same as _squaresPerSideY - 1\nUBound: " + fieldLines.GetUpperBound(1).ToString() + ", _squaresPerSideY - 1: " + ((int)(_squaresPerSideY - 1)).ToString());
			}

			//	Init my grid
			_grid = new MyVector[fieldLines.Length];

			//	Store the lines
			for (int xCntr = 0; xCntr <= fieldLines.GetUpperBound(0); xCntr++)
			{
				for (int yCntr = 0; yCntr <= fieldLines.GetUpperBound(1); yCntr++)
				{
					_grid[GetIndex(xCntr, yCntr)] = fieldLines[xCntr, yCntr];
				}
			}

			//	Put myself into a custom state
			_fieldMode = VectorField2DMode.Custom;

			//	No need to call ResetField()
		}

		#endregion

		#region Private Methods

		private void ResetField()
		{
			if (this.Position == null)
			{
				//	The constructor isn't finished
				return;
			}

			switch (_fieldMode)
			{
				case VectorField2DMode.None:
					_grid = null;
					break;

				case VectorField2DMode.Custom:
					if (_grid != null)
					{
						//	Fix myself
						_grid = null;
						_fieldMode = VectorField2DMode.None;

						throw new InvalidOperationException("_fieldMode should only become Custom within this.SetFieldLines, and only after _grid is set up");
					}
					break;

				case VectorField2DMode.Inward:
					ResetFieldSprtInOut(-1);
					break;
				case VectorField2DMode.Outward:
					ResetFieldSprtInOut(1);
					break;

				case VectorField2DMode.SwirlLeft:
					ResetFieldSprtSwirl(true);
					break;
				case VectorField2DMode.SwirlRight:
					ResetFieldSprtSwirl(false);
					break;

				default:
					throw new ApplicationException("Unknown VectorField2DMode: " + _fieldMode.ToString());
			}
		}
		private void ResetFieldSprtInOut(double direction)
		{
			//	Init the grid
			_grid = new MyVector[_squaresPerSideX * _squaresPerSideY];

			foreach(MyVector center in GetGridCenters())
			{
				//	Get the local position
				MyVector localPosition = center - _position;

				//	Turn that into a constant length, pointing in or out
				MyVector fieldLine = localPosition.Clone();
				fieldLine.BecomeUnitVector();
				fieldLine.Multiply(_strength * direction);

				//	Store it
				_grid[GetIndexForLocalPosition(localPosition)] = fieldLine;
			}
		}
		private void ResetFieldSprtSwirl(bool goLeft)
		{
			//	Init the grid
			_grid = new MyVector[_squaresPerSideX * _squaresPerSideY];

			//	I'm going to rotate everything 90 degrees
			MyVector rotateAxis = new MyVector(0, 0, 1);
			double radians = Math.PI / 2d;
			if(goLeft)
			{
				radians *= -1d;
			}

			foreach (MyVector center in GetGridCenters())
			{
				//	Get the local position
				MyVector localPosition = center - _position;

				//	Turn that into a constant length, pointing in or out
				MyVector fieldLine = localPosition.Clone();
				fieldLine.BecomeUnitVector();
				fieldLine.Multiply(_strength);
				fieldLine.RotateAroundAxis(rotateAxis, radians);

				//	Store it
				_grid[GetIndexForLocalPosition(localPosition)] = fieldLine;
			}
		}
		
		/// <summary>
		/// This function figures out which grid the position passed in is sitting on, and converts that into
		/// the 1D index (-1 if outside the grid)
		/// </summary>
		/// <returns>
		/// -1:  position is outside the grid
		/// 0 to _grid.Length - 1:  index into _grid
		/// </returns>
		private int GetIndexForLocalPosition(MyVector position)
		{
			if (_grid == null)
			{
				return -1;
			}

			//	Turn the position into a position relative to the top/left point
			double topleftOffsetX = position.X + _halfSizeX;		//	should this be minus?
			double topleftOffsetY = position.Y + _halfSizeY;

			if (topleftOffsetX < 0d || topleftOffsetY < 0d || topleftOffsetX > _sizeX || topleftOffsetY > _sizeY)
			{
				//	They're off the grid
				return -1;
			}

			int xIndex = Convert.ToInt32(Math.Floor(topleftOffsetX / _sizeOfSquareX));
			int yIndex = Convert.ToInt32(Math.Floor(topleftOffsetY / _sizeOfSquareY));

			//	Exit Function
			return GetIndex(xIndex, yIndex);
		}

		private int GetIndex(int xIndex, int yIndex)
		{
			return (yIndex * _squaresPerSideX) + xIndex;
		}

		#endregion
	}

	#region Enum: VectorField2DMode

	public enum VectorField2DMode
	{
		None = 0,
		SwirlLeft,
		SwirlRight,
		Inward,
		Outward,
		Custom
	}

	#endregion
}
