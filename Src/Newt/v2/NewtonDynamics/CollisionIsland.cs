using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.v2.NewtonDynamics.Import;

namespace Game.Newt.v2.NewtonDynamics
{
	/// <summary>
	/// I believe an island only exists for a frame, and is a collection of bodies that will have collision calculations performed
	/// </summary>
	/// <remarks>
	/// From the .cpp:
	/// 
	/// Remarks: The application can set a function callback to be called just after the array of all bodies making an island of articulated and colliding bodies are collected for resolution. 
	/// This function will be called just before the array is accepted for solution and integration. 
	/// The function callback may return one to validate the array or zero to skip the resolution of this array of bodies on this frame only.
	/// This functionality can be used by the application to implement in game physics LOD. For example the application can determine the AABB of the 
	/// island and check against the view frustum, if the entire island AABB is invisible then the application can suspend simulation even if they are not in equilibrium.
	/// another functionality is the implementation of visual debuggers, and also the implementation of auto frozen bodies under arbitrary condition set by the logic of the application.
	///
	/// Remarks: The application should not modify the position, velocity, or it create or destroy any body or joint during this function call. Doing so will result in unpredictable malfunctions.
	/// </remarks>
	public class CollisionIsland
	{
		#region Constructor

		public CollisionIsland(IntPtr handle, int numBodies)
		{
			_handle = handle;
			_numBodies = numBodies;
		}

		#endregion

		#region Public Properties

		private IntPtr _handle;
		public IntPtr Handle
		{
			get
			{
				return _handle;
			}
		}

		private int _numBodies;
		public int NumBodies
		{
			get
			{
				return _numBodies;
			}
		}

		#endregion

		#region Public Methods

		public Body GetBody(int bodyIndex)
		{
			IntPtr bodyHandle = Newton.NewtonIslandGetBody(_handle, bodyIndex);
			return ObjectStorage.Instance.GetBody(bodyHandle);
		}

		public void GetBodyAABB(out Point3D minPoint, out Point3D maxPoint, int bodyIndex)
		{
			float[] p0 = new float[3];
			float[] p1 = new float[3];
			Newton.NewtonIslandGetBodyAABB(_handle, bodyIndex, p0, p1);

			minPoint = new NewtonVector3(p0).ToPointWPF();
			maxPoint = new NewtonVector3(p1).ToPointWPF();
		}

		#endregion
	}
}
