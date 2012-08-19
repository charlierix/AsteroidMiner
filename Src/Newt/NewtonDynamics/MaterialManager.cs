using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.NewtonDynamics.Import;

//NOTE:  These classes are a high level wrapper to newton's material.  My classes are named with the same words that newton uses, but the meanings are different
namespace Game.Newt.NewtonDynamics
{
	public class MaterialManager
	{
		#region Class: CollisionListener

		private class CollisionListener
		{
			public MaterialManager Sender = null;
			public int Material1 = -1;
			public int material2 = -1;
			public EventHandler<MaterialCollisionArgs> EventListener = null;

			public Newton.NewtonContactsProcess InvokeContactsProcessVariable = null;
			public void InvokeContactsProcess(IntPtr contact, float timestep, int threadIndex)
			{
				MaterialCollisionArgs e = new MaterialCollisionArgs(contact, timestep, threadIndex);
				this.EventListener(this.Sender, e);
			}
		}

		#endregion

		#region Declaration Section

		private WorldBase _world = null;

		/// <summary>
		/// Key=Newton's material group ID
		/// Value=My custom material class
		/// </summary>
		/// <remarks>
		/// I wanted to just make a simple List, but since newton returns an ID every time you call create group, I can't trust that the ID's
		/// returned are sequential (just unique)
		/// </remarks>
		private SortedList<int, Material> _materials = new SortedList<int, Material>();

		private List<CollisionListener> _collisionListeners = new List<CollisionListener>();

		#endregion

		#region Constructor

		public MaterialManager(WorldBase world)
		{
			_world = world;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This adds properties about a material, and tells newton how to collide this with all previous materials that were added
		/// </summary>
		/// <remarks>
		/// NOTE:  Don't change material's properties after this method is called, or corrupted behaviour will occur (some material combos will have the
		/// old values, new ones will have the new values)
		/// </remarks>
		public int AddMaterial(Material material)
		{
			//	Tell newton about this material group
			int retVal = Newton.NewtonMaterialCreateGroupID(_world.Handle);

			//	Add now so the loop will set up props between this and itself
			_materials.Add(retVal, material);

			// Newton needs the collision properties set across all permutations of materials, so add this to existing materials (even against itself)
			foreach (int prevMaterialID in _materials.Keys)
			{
				bool isCollidable = _materials[prevMaterialID].IsCollidable && material.IsCollidable;

				//	Tell newton about this material combination
				if (isCollidable)
				{
					double thickness = (_materials[prevMaterialID].SurfaceThickness + material.SurfaceThickness) / 2d;
					Newton.NewtonMaterialSetSurfaceThickness(_world.Handle, prevMaterialID, retVal, Convert.ToSingle(thickness));

					bool isContinuous = _materials[prevMaterialID].IsContinuousCollision || material.IsContinuousCollision;
					Newton.NewtonMaterialSetContinuousCollisionMode(_world.Handle, prevMaterialID, retVal, isContinuous ? 1 : 0);

					double softness = (_materials[prevMaterialID].Softness + material.Softness) / 2d;
					Newton.NewtonMaterialSetDefaultSoftness(_world.Handle, prevMaterialID, retVal, Convert.ToSingle(softness));

					double elasticity = (_materials[prevMaterialID].Elasticity + material.Elasticity) / 2d;
					Newton.NewtonMaterialSetDefaultElasticity(_world.Handle, prevMaterialID, retVal, Convert.ToSingle(elasticity));

					Newton.NewtonMaterialSetDefaultCollidable(_world.Handle, prevMaterialID, retVal, 1);

					double staticFriction = (_materials[prevMaterialID].StaticFriction + material.StaticFriction) / 2d;
					double kineticFriction = (_materials[prevMaterialID].KineticFriction + material.KineticFriction) / 2d;
					Newton.NewtonMaterialSetDefaultFriction(_world.Handle, prevMaterialID, retVal, Convert.ToSingle(staticFriction), Convert.ToSingle(kineticFriction));
				}
				else
				{
					Newton.NewtonMaterialSetDefaultCollidable(_world.Handle, prevMaterialID, retVal, 0);
				}
			}

			//	Exit Function
			return retVal;
		}

		public void Clear()
		{
			_materials.Clear();
			_collisionListeners.Clear();

			Newton.NewtonMaterialDestroyAllGroupID(_world.Handle);
		}

		/// <summary>
		/// This will register an event listener with a material pair collision.  The event will get called whenever any two bodies
		/// of those two ID's collide
		/// NOTE:  Not sure if it's limited to a single listener for a material ID pair
		/// </summary>
		/// <remarks>
		/// This is how you get notified of a collision, there is no event off of the body class, you need to get at it from the material
		/// and filter for the bodies you care about
		/// 
		/// NOTE:  I tested and don't seem to need to register an opposite handler (register with 1,2.  No need to also register with 2,1.
		/// any collision between a 1 and a 2 gets raised)
		/// </remarks>
		public void RegisterCollisionEvent(int material1, int material2, EventHandler<MaterialCollisionArgs> callback)
		{
			//	Create a class that will listen to the newton callback, and turn it into a more c# friendly event
			CollisionListener listener = new CollisionListener();
			listener.Sender = this;
			listener.Material1 = material1;
			listener.material2 = material2;
			listener.EventListener = callback;
			_collisionListeners.Add(listener);

			//	Register the callback
			listener.InvokeContactsProcessVariable = new Newton.NewtonContactsProcess(listener.InvokeContactsProcess);		//	there is a CallbackOnCollectedDelegate exception if newton isn't handed an explicit delegate variable (as opposed to just handing it the method)
			Newton.NewtonMaterialSetCollisionCallback(_world.Handle, material1, material2, IntPtr.Zero, null, listener.InvokeContactsProcessVariable);
		}

		#endregion

		//TODO:  Implement when I need it
		//	these define a material on material collision
		//internal static extern IntPtr NewtonMaterialGetUserData(IntPtr newtonWorld, int id0, int id1);

		//TODO:  Implement this if I need it.  The default material is a template for others, but I will force the caller to pass unique values all the time
		//internal static extern int NewtonMaterialGetDefaultGroupID(IntPtr newtonWorld);

		//TODO:  Implement these if the need arises (probably won't, because I store the objects in c# - maybe for debugging)
		//internal static extern IntPtr NewtonWorldGetFirstMaterial(IntPtr newtonWorld);
		//internal static extern IntPtr NewtonWorldGetNextMaterial(IntPtr newtonWorld, IntPtr material);

		//internal static extern IntPtr NewtonWorldGetFirstBody(IntPtr newtonWorld);
		//internal static extern IntPtr NewtonWorldGetNextBody(IntPtr newtonWorld, IntPtr curBody);
	}

	#region Class: Material

	/// <summary>
	/// This is just a data class that gets handed to MaterialManager.Add
	/// </summary>
	public class Material
	{
		/// <summary>
		/// Set to false if you want ghost materials
		/// </summary>
		public bool IsCollidable = true;

		/// <summary>
		/// Acts like a margin between two bodies.  Default is zero, rolling bodies may want a small thickness
		/// </summary>
		public double SurfaceThickness = 0d;

		/// <summary>
		/// The last line of the remarks sums it up, leave it off for everything but projectiles
		/// </summary>
		/// <remarks>
		/// From the wiki:
		/// continue collision mode enable allow the engine to predict colliding contact on rigid bodies Moving at high speed of subject to strong forces.
		/// continue collision mode does not prevent rigid bodies from interpenetration instead it prevent bodies from passing trough each others by extrapolating contact points when the bodies normal contact calculation determine the bodies are not colliding.
		/// for performance reason the bodies angular velocities is only use on the broad face of the collision, but not on the contact calculation.
		/// continue collision does not perform back tracking to determine time of contact, instead it extrapolate contact by incrementally extruding the collision geometries of the two colliding bodies along the linear velocity of the bodies during the time step, if during the extrusion colliding contact are found, a collision is declared and the normal contact resolution is called.
		/// for continue collision to be active the continue collision mode must on the material pair of the colliding bodies as well as on at least one of the two colliding bodies.
		/// Because there is penalty of about 40% to 80% depending of the shape complexity of the collision geometry, this feature is set off by default. It is the job of the application to determine what bodies need this feature on. Good guidelines are: very small objects, and bodies that move with high speed.
		/// </remarks>
		public bool IsContinuousCollision = false;

		/// <summary>
		/// softnessCoef must be a positive value. It is recommended that softnessCoef be set to value lower or equal to 1.0
		/// A low value for softnessCoef will make the material soft. A typical value for softnessCoef is 0.15, default value is 0.1
		/// </summary>
		/// <remarks>
		/// So a typical value is harder than the default?  Wouldn't 1 be better?  Anyway, I'm splitting the difference till I know better
		/// </remarks>
		public double Softness = .125;

		/// <summary>
		/// From 0 to 1, default is .4
		/// </summary>
		/// <remarks>
		/// I believe that zero is wet towel, and one is a steel ball.  Anything higher is flubber?
		/// </remarks>
		public double Elasticity = .4d;

		/// <summary>
		/// From 0 to 1 (default is .9)
		/// </summary>
		public double StaticFriction = .9d;
		/// <summary>
		/// From 0 to 1 (default is .5)
		/// </summary>
		public double KineticFriction = .5d;
	}

	#endregion
	#region Class: MaterialCollision

	/// <summary>
	/// This is a collision between two bodies (of a material type).  Call MaterialManager.RegisterCollisionEvent, and the event raised will
	/// have a bunch of instances of this for a given collision
	/// </summary>
	public class MaterialCollision
	{
		#region Declaration Section

		private IntPtr _handle;

		#endregion

		#region Constructor

		public MaterialCollision(IntPtr collisionHandle)
		{
			_handle = collisionHandle;
		}

		#endregion

		#region Public Properties

		public double ContactNormalSpeed
		{
			get
			{
				return Newton.NewtonMaterialGetContactNormalSpeed(_handle);
			}
		}

		public uint ContactFaceAttribute
		{
			get
			{
				return Newton.NewtonMaterialGetContactFaceAttribute(_handle);
			}
		}

		/// <summary>
		/// I don't think I've exposed any way to set user data, so this property is kind of pointless
		/// TODO:  If this is useful, then build a hook in ObjectStorage to store/retrieve objects based on intptr
		/// </summary>
		public IntPtr MaterialPairUserData
		{
			get
			{
				return Newton.NewtonMaterialGetMaterialPairUserData(_handle);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Vector is in world coords
		/// </summary>
		public Vector3D GetContactForceWorld(Body body)
		{
			NewtonVector3 retVal = new NewtonVector3();

			Newton.NewtonMaterialGetContactForce(_handle, body.Handle, retVal.Vector);

			return retVal.ToVectorWPF();
		}

		/// <summary>
		/// Vectors are in world coords
		/// </summary>
		public void GetContactPositionAndNormalWorld(out Point3D position, out Vector3D normal, out double normalSpeed, Body body)
		{
			NewtonVector3 newtPos = new NewtonVector3();
			NewtonVector3 newtNormal = new NewtonVector3();

			Newton.NewtonMaterialGetContactPositionAndNormal(_handle, body.Handle, newtPos.Vector, newtNormal.Vector);

			position = newtPos.ToPointWPF();

			normal = newtNormal.ToVectorWPF();
			normal.Normalize();
			normalSpeed = this.ContactNormalSpeed;
			normal *= normalSpeed;
		}

		/// <summary>
		/// The vectors are sized according to their speed (returning the speeds explicitely so the caller doesn't have to go to the expense of
		/// calling .Length)
		/// </summary>
		/// <remarks>
		/// Not sure if the vectors are local or world coords.  Documentation is nearly non existent for this one.  I'm guessing it's world if
		/// the contact normal is in world coords
		/// 
		/// The contact normal is perpendicular to the face (the particular triangle of the collision hull that is currently colliding)
		/// 
		/// The primary and secondary tangents are parallel to that face?  I know the primary and secondary are perpendicular to each other,
		/// but not sure what drives which is which - is it just the order of the verticies when the triangle was created?  Maybe they should just
		/// be considered arbitraray until you call this.RotateTangentDirections (which you would probably only do if you were then going to
		/// overide accel/friction)
		/// </remarks>
		public void GetContactTangentDirections(out Vector3D primaryTangent, out double primaryTangentSpeed, out Vector3D secondaryTangent, out double secondaryTangentSpeed, Body body)
		{
			NewtonVector3 newtPrim = new NewtonVector3();
			NewtonVector3 newtSec = new NewtonVector3();

			Newton.NewtonMaterialGetContactTangentDirections(_handle, body.Handle, newtPrim.Vector, newtSec.Vector);

			primaryTangent = newtPrim.ToVectorWPF();
			primaryTangent.Normalize();
			primaryTangentSpeed = Newton.NewtonMaterialGetContactTangentSpeed(_handle, 0);
			primaryTangent *= primaryTangentSpeed;

			secondaryTangent = newtSec.ToVectorWPF();
			secondaryTangent.Normalize();
			secondaryTangentSpeed = Newton.NewtonMaterialGetContactTangentSpeed(_handle, 1);
			secondaryTangent *= secondaryTangentSpeed;
		}

		/// <summary>
		/// This is the custom shapeID that was handed in when creating the collision hull
		/// NOTE:  The create methods take int, but this returns uint.  I'm hesitant to change the interface, so use small positive numbers, and you should be fine  :)
		/// </summary>
		public uint GetCollisionHullShapeID(Body body)
		{
			return Newton.NewtonMaterialGetBodyCollisionID(_handle, body.Handle);
		}

		//	Manipulation methods.  Read the wiki for more info, but the need for calling these should be rare

		/// <summary>
		/// I assume this is in world coords?
		/// </summary>
		public void SetContactNormalDirection(Vector3D newNormal)
		{
			Newton.NewtonMaterialSetContactNormalDirection(_handle, new NewtonVector3(newNormal).Vector);
		}
		public void SetContactNormalAcceleration(double acceleration)
		{
			Newton.NewtonMaterialSetContactNormalAcceleration(_handle, Convert.ToSingle(acceleration));
		}

		/// <summary>
		/// Is this in world coords?
		/// </summary>
		public void RotateTangentDirections(Vector3D newPrimaryTangent)
		{
			Newton.NewtonMaterialContactRotateTangentDirections(_handle, new NewtonVector3(newPrimaryTangent).Vector);
		}
		public void SetContactTangentAcceleration(double acceleration, bool isPrimaryTangent)
		{
			Newton.NewtonMaterialSetContactTangentAcceleration(_handle, Convert.ToSingle(acceleration), isPrimaryTangent ? 0 : 1);
		}
		public void SetContactFrictionState(bool hasFriction, bool isPrimaryTangent)
		{
			Newton.NewtonMaterialSetContactFrictionState(_handle, hasFriction ? 1 : 0, isPrimaryTangent ? 0 : 1);		//	note that primary is zero, not one
		}
		public void SetContactFriction(double staticFriction, double kineticFriction, bool isPrimaryTangent)
		{
			Newton.NewtonMaterialSetContactFrictionCoef(_handle, Convert.ToSingle(staticFriction), Convert.ToSingle(kineticFriction), isPrimaryTangent ? 0 : 1);
		}

		public void SetContactSoftness(double softness)
		{
			Newton.NewtonMaterialSetContactSoftness(_handle, Convert.ToSingle(softness));
		}
		public void SetContactElasticity(double elasticity)
		{
			Newton.NewtonMaterialSetContactElasticity(_handle, Convert.ToSingle(elasticity));
		}

		#endregion
	}

	#endregion

	#region Class: MaterialCollisionArgs

	public class MaterialCollisionArgs : EventArgs
	{
		#region Declaration Section

		//	Storing these as privates, and only return bodies, etc when requested.  This should be more efficient than getting everything every time (certain cases may just play
		//	sound effects, regardless of the details)
		private IntPtr _contactHandle;
		private float _timestep;
		private int _threadIndex;

		#endregion

		#region Constructor

		public MaterialCollisionArgs(IntPtr contactHandle, float timestep, int threadIndex)
		{
			_contactHandle = contactHandle;
			_timestep = timestep;
			_threadIndex = threadIndex;
		}

		#endregion

		#region Public Properties

		private Body _body0 = null;
		public Body Body0
		{
			get
			{
				if (_body0 == null)
				{
					IntPtr handle = Newton.NewtonJointGetBody0(_contactHandle);
					_body0 = ObjectStorage.Instance.GetBody(handle);
				}

				return _body0;
			}
		}

		private Body _body1 = null;
		public Body Body1
		{
			get
			{
				if (_body1 == null)
				{
					IntPtr handle = Newton.NewtonJointGetBody1(_contactHandle);
					_body1 = ObjectStorage.Instance.GetBody(handle);
				}

				return _body1;
			}
		}

		private MaterialCollision[] _collisions = null;
		public MaterialCollision[] Collisions
		{
			get
			{
				if (_collisions == null)
				{
					List<MaterialCollision> collisions = new List<MaterialCollision>();

					IntPtr collisionHandle = Newton.NewtonContactJointGetFirstContact(_contactHandle);
					while (collisionHandle != IntPtr.Zero)
					{
						//	collisionHandle seems to give higher level access (counts, remove, next).  The material handle seems to be a material on material collision for this contact?
						IntPtr realCollisionHandle = Newton.NewtonContactGetMaterial(collisionHandle);

						collisions.Add(new MaterialCollision(realCollisionHandle));
						collisionHandle = Newton.NewtonContactJointGetNextContact(_contactHandle, collisionHandle);
					}

					_collisions = collisions.ToArray();
				}

				return _collisions;
			}
		}

		public double Timestep
		{
			get
			{
				return _timestep;
			}
		}
		public int ThreadIndex
		{
			get
			{
				return _threadIndex;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This returns either Body0 or Body1, whichever is the requested materialID (this only makes sense for collisions
		/// between two different material types)
		/// </summary>
		public Body GetBody(int materialID)
		{
			if (this.Body0.MaterialGroupID == materialID)
			{
				return this.Body0;
			}
			else if (this.Body1.MaterialGroupID == materialID)
			{
				return this.Body1;
			}
			else
			{
				throw new ArgumentException(string.Format("Neither body is the requested material: requested={0}, body0={1}, body1={2}", materialID.ToString(), this.Body0.MaterialGroupID.ToString(), this.Body1.MaterialGroupID.ToString()));
			}
		}

		#endregion

		//	probably don't need this
		//internal static extern void NewtonContactJointRemoveContact(IntPtr contactJoint, IntPtr contact);
	}

	#endregion
}
