using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics_153;

namespace Game.Newt.AsteroidMiner_153
{
	/// <summary>
	/// This is a reworking of SwarmBot1, but using the base class.  This isn't meant to be a true AI bot, but is used by
	/// the swarmbot tester
	/// </summary>
	public class SwarmBot2 : SwarmBotBase
	{
		#region Enum: BehaviorType

		public enum BehaviorType
		{
			/// <summary>
			/// This is the simplest.  It always goes straight for the chase point, full force, ignoring everything else
			/// </summary>
			StraightToTarget,
			/// <summary>
			/// This is like the other, but will try to adjust for the current velocity
			/// </summary>
			StraightToTarget_VelocityAware1,
			/// <summary>
			/// Like the previous, but limits the velocity (so that the velocity will be zero when near the chase point)
			/// </summary>
			StraightToTarget_VelocityAware2,

			/// <summary>
			/// This one ignores the chase point, and flies toward the center of the flock
			/// </summary>
			TowardCenterOfFlock,
			/// <summary>
			/// They go toward the center of the flock, and also keep a small distance from others
			/// </summary>
			CenterFlock_AvoidNeighbors,
			/// <summary>
			/// They also try to match the flock's cumulative velocity
			/// </summary>
			CenterFlock_AvoidNeighbors_FlockVelocity,

			/// <summary>
			/// This has the 3 flocking rules, and also chases the chase point
			/// </summary>
			Flocking_ChasePoint,
			Flocking_ChasePoint_AvoidKnownObsticles
		}

		#endregion

		#region Declaration Section

		private const double PERCENTSTANDARD = .5d;
		private const double PERCENTATTACKING = 1d;

		private double _maxAccel = 1d;

		#endregion

		#region Public Properties

		private BehaviorType _behavior = BehaviorType.StraightToTarget;
		public BehaviorType Behavior
		{
			get
			{
				return _behavior;
			}
			set
			{
				_behavior = value;
			}
		}

		private double _visionLimit = double.MaxValue;
		public double VisionLimit
		{
			get
			{
				return _visionLimit;
			}
			set
			{
				_visionLimit = value;
			}
		}

		private int _numClosestBotsToLookAt = int.MaxValue;
		public int NumClosestBotsToLookAt
		{
			get
			{
				return _numClosestBotsToLookAt;
			}
			set
			{
				_numClosestBotsToLookAt = value;
			}
		}

		// Exposing this so the form has something to manipulate
		public bool IsAttacking
		{
			get
			{
				return base.IsAttacking;
			}
			set
			{
				base.IsAttacking = value;
			}
		}

		/// <summary>
		/// This is the list of obsticles to avoid
		/// TODO:  Support an option to avoid all
		/// </summary>
		private List<ConvexBody3D> _obsticles = new List<ConvexBody3D>();
		public List<ConvexBody3D> Obsticles
		{
			get
			{
				return _obsticles;
			}
		}

		#endregion

		#region Public Methods

		public void CreateBot(Viewport3D viewport, SharedVisuals sharedVisuals, World world, Point3D worldPosition)
		{
			base.CreateBot(viewport, sharedVisuals, world, worldPosition);
		}

		public override void WorldUpdating()
		{
			// Figure out which behavior to use
			//NOTE:  These set this.ThrustTransform and this.ThrustPercent
			switch (_behavior)
			{
				case BehaviorType.StraightToTarget:
					Brain_StraightToTarget();
					break;
				case BehaviorType.StraightToTarget_VelocityAware1:
					Brain_StraightToTarget_VelocityAware1();
					break;
				case BehaviorType.StraightToTarget_VelocityAware2:
					Brain_StraightToTarget_VelocityAware2();
					break;


				case BehaviorType.TowardCenterOfFlock:
					Brain_TowardCenterOfFlock();
					break;
				case BehaviorType.CenterFlock_AvoidNeighbors:
					Brain_CenterFlock_AvoidNeighbors();
					break;
				case BehaviorType.CenterFlock_AvoidNeighbors_FlockVelocity:
					Brain_CenterFlock_AvoidNeighbors_FlockVelocity();
					break;


				case BehaviorType.Flocking_ChasePoint:
					Brain_Flocking_ChasePoint();
					break;
				case BehaviorType.Flocking_ChasePoint_AvoidKnownObsticles:
					Brain_Flocking_ChasePoint_AvoidKnownObsticles();
					break;

				default:
					throw new ApplicationException("Unknown BehaviorType: " + _behavior.ToString());
			}

			// Call the base class
			base.WorldUpdating();
		}

		#endregion

		#region Private Methods

		private void Brain_StraightToTarget()
		{
			// Figure out what direction to go to get to the chase point (in local coords)
			Vector3D directionToGo = GetDirection_StraightToTarget(this.ChasePoint);

			// Set thruster settings
			AimThruster(directionToGo);

			#region Attempt to world

			// This fails when converting everything to world coords

			//Point3D position = this.PhysicsBody.PositionToWorld(new Point3D(0, 0, 0));

			//Vector3D directionToGo = _chasePoint.ToVector() - position.ToVector();

			//Vector3D origThrustDirWorld = this.PhysicsBody.DirectionToWorld(_origThrustDirection);

			//Vector3D axis;
			//double radians;
			//Math3D.GetRotation(out axis, out radians, origThrustDirWorld, directionToGo);

			//_thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math3D.RadiansToDegrees(radians)));

			#endregion
		}
		private void Brain_StraightToTarget_VelocityAware1()
		{
			// Figure out what direction to go in local coords
			Vector3D directionToGo = GetDirection_StraightToTarget_VelocityAware1(this.ChasePoint);

			// Set thruster settings
			AimThruster(directionToGo);
		}
		private void Brain_StraightToTarget_VelocityAware2()
		{
			// Figure out what direction to go in local coords
			Vector3D directionToGo = GetDirection_StraightToTarget_VelocityAware2(this.ChasePoint, GetMaxAcceleration());

			// Set thruster settings
			AimThruster(directionToGo);
		}
		private void Brain_TowardCenterOfFlock()
		{
			// Get the known bots
			List<SwarmBotBase> otherBots = GetKnownOtherBots();

			// Calculate the center of position of the flock
			Vector3D centerPosition = GetCenterOfFLock(otherBots);

			// Figure out what direction to go in local coords
			Vector3D flockCenterDirection = new Vector3D();
			if (this.IsAttacking)
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware1(centerPosition.ToPoint());
			}
			else
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware2(centerPosition.ToPoint(), GetMaxAcceleration());
			}

			// Set thruster settings
			AimThruster(flockCenterDirection);
		}
		private void Brain_CenterFlock_AvoidNeighbors()
		{
			// Get the known bots
			List<SwarmBotBase> otherBots = GetKnownOtherBots();

			// Calculate the center of position of the flock
			Vector3D centerPosition = GetCenterOfFLock(otherBots);

			// Figure out what direction to go in local coords
			//Vector3D flockCenterDirection = GetDirection_StraightToTarget(centerPosition.ToPoint());
			Vector3D flockCenterDirection = new Vector3D();
			if (this.IsAttacking)
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware1(centerPosition.ToPoint());
			}
			else
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware2(centerPosition.ToPoint(), GetMaxAcceleration());
			}

			// Get a cumulative vector that tells how to avoid others
			//TODO: Give this params for the avoidance strength
			Vector3D avoidDirection = GetDirection_AvoidBots(otherBots, 8d);		//	passing 8, because this is currently always called by the swarmbot tester




			// For now, I will just normalize the two and add them together with equal weight
			//NOTE:  Normalizing gives bad results.  The avoid already has a priority magnitude, normalizing wipes it
			//flockCenterDirection.Normalize();
			//avoidDirection.Normalize();

			flockCenterDirection /= 2;

			Vector3D directionToGo = flockCenterDirection + avoidDirection;




			// Set thruster settings
			AimThruster(directionToGo);
		}
		private void Brain_CenterFlock_AvoidNeighbors_FlockVelocity()
		{
			// Get the known bots
			List<SwarmBotBase> otherBots = GetKnownOtherBots();

			// Calculate the center of position of the flock
			Vector3D centerPosition = GetCenterOfFLock(otherBots);

			// Figure out what direction to go in local coords
			//Vector3D flockCenterDirection = GetDirection_StraightToTarget(centerPosition.ToPoint());
			Vector3D flockCenterDirection = new Vector3D();
			if (this.IsAttacking)
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware1(centerPosition.ToPoint());
			}
			else
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware2(centerPosition.ToPoint(), GetMaxAcceleration());
			}

			// Get a cumulative vector that tells how to avoid others
			//TODO: Give this params for the avoidance strength
			Vector3D avoidDirection = GetDirection_AvoidBots(otherBots, 8d);		//	passing 8, because this is currently always called by the swarmbot tester

			// Get the average flock velocity
			Vector3D commonVelocityDirection = GetSwarmVelocity(otherBots);



			commonVelocityDirection.Normalize();
			commonVelocityDirection /= 1.5;


			Vector3D directionToGo = flockCenterDirection + avoidDirection + commonVelocityDirection;




			// Set thruster settings
			AimThruster(directionToGo);
		}
		private void Brain_Flocking_ChasePoint()
		{
			double maxAccel = GetMaxAcceleration();

			// Get the known bots
			List<SwarmBotBase> otherBots = GetKnownOtherBots();

			// Calculate the center of position of the flock
			Vector3D centerPosition = GetCenterOfFLock(otherBots);

			// Figure out what direction to go in local coords
			//Vector3D flockCenterDirection = GetDirection_StraightToTarget(centerPosition.ToPoint());
			Vector3D flockCenterDirection = new Vector3D();
			if (this.IsAttacking)
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware1(centerPosition.ToPoint());
			}
			else
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware2(centerPosition.ToPoint(), maxAccel);
			}

			// Get a cumulative vector that tells how to avoid others
			//TODO: Give this params for the avoidance strength
			Vector3D avoidDirection = GetDirection_AvoidBots(otherBots, 8d);		//	passing 8, because this is currently always called by the swarmbot tester

			// Get the average flock velocity
			Vector3D commonVelocityDirection = GetSwarmVelocity(otherBots);

			// Go toward the chase point
			Vector3D chasePointDirection = new Vector3D();
			if (this.IsAttacking)
			{
				chasePointDirection = GetDirection_StraightToTarget_VelocityAware1(this.ChasePoint);
			}
			else
			{
				chasePointDirection = GetDirection_StraightToTarget_VelocityAware2(this.ChasePoint, maxAccel);
			}





			// When attacking, other bot avoidence goes down, chase point goes up, use velocityaware1 instead of 2 for chase point

			commonVelocityDirection.Normalize();
			commonVelocityDirection /= 2;

			//chasePointDirection *= 2;      // the chasepoint needs a stronger pull then the center flock.  Otherwise, far away bots have as much influence as the chase point

			Vector3D directionToGo = flockCenterDirection + avoidDirection + commonVelocityDirection + chasePointDirection;




			// Set thruster settings
			AimThruster(directionToGo);
		}
		private void Brain_Flocking_ChasePoint_AvoidKnownObsticles()
		{
			//NOTE:  Since this is the one being used by asteroid miner (and not currently used in the swarmbot tester), the values are tweaked for that

			double maxAccel = GetMaxAcceleration();

			// Get the known bots
			List<SwarmBotBase> otherBots = GetKnownOtherBots();

			// Calculate the center of position of the flock
			Vector3D centerPosition = GetCenterOfFLock(otherBots);

			// Figure out what direction to go in local coords
			//Vector3D flockCenterDirection = GetDirection_StraightToTarget(centerPosition.ToPoint());
			Vector3D flockCenterDirection = new Vector3D();
			if (this.IsAttacking)
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware1(centerPosition.ToPoint());
			}
			else
			{
				flockCenterDirection = GetDirection_StraightToTarget_VelocityAware2(centerPosition.ToPoint(), maxAccel);
			}

			// Get a cumulative vector that tells how to avoid others
			//TODO: Give this params for the avoidance strength
			Vector3D avoidBotDirection = GetDirection_AvoidBots(otherBots, 2d);

			// Get the average flock velocity
			Vector3D commonVelocityDirection = GetSwarmVelocity(otherBots);

			// Go toward the chase point
			//TODO:  The velocity aware methods are still too twitchy
			//TODO:  The velocity aware methods can't just go to the point, they have to try to match the point's velocity
			//Vector3D chasePointDirection = GetDirection_StraightToTarget(this.ChasePoint);
			Vector3D chasePointDirection = new Vector3D();
			if (this.IsAttacking)
			{
				chasePointDirection = GetDirection_StraightToTarget_VelocityAware1(this.ChasePoint);
			}
			else
			{
				//chasePointDirection = GetDirection_StraightToTarget_VelocityAware2_DebugVisuals(this.ChasePoint, maxAccel);
				chasePointDirection = GetDirection_StraightToTarget_VelocityAware2(this.ChasePoint, this.ChasePointVelocity, maxAccel);
				//chasePointDirection = GetDirection_InterceptTarget_VelocityAware(this.ChasePoint, this.ChasePointVelocity, maxAccel);
			}

			Vector3D avoidBodiesDirection = GetDirection_AvoidBodies(_obsticles, 66d, false);





			// When attacking, other bot avoidence should go down, chase point goes up, use velocityaware1 instead of 2 for chase point


			if (!commonVelocityDirection.IsZero())		//	when you normalize a zero vector, it becomes NaN
			{
				commonVelocityDirection.Normalize();
				commonVelocityDirection /= 2;
			}



			//Vector3D directionToGo = flockCenterDirection + avoidBotDirection + commonVelocityDirection + chasePointDirection + avoidBodiesDirection;
			Vector3D directionToGo = chasePointDirection;



			// Set thruster settings
			AimThruster(directionToGo);
		}

		/// <summary>
		/// This returns this.OtherBots, but potentially with various vision constraints
		/// </summary>
		private List<SwarmBotBase> GetKnownOtherBots()
		{
			List<SwarmBotBase> retVal = null;

			if (_visionLimit == double.MaxValue && _numClosestBotsToLookAt == int.MaxValue)
			{
				retVal = this.OtherBots;
			}
			else
			{
				retVal = GetOtherBots(_visionLimit, _numClosestBotsToLookAt < int.MaxValue);

				if (_numClosestBotsToLookAt < int.MaxValue && retVal.Count > _numClosestBotsToLookAt)
				{
					// Limit the number of bots returned
					retVal.RemoveRange(_numClosestBotsToLookAt, retVal.Count - _numClosestBotsToLookAt);
				}
			}

			return retVal;
		}

		private double GetMaxAcceleration()
		{
			double retVal = Math.Abs(base.Thruster.ForceStrength);
			if (!this.IsAttacking)
			{
				retVal *= .5d;
			}

			retVal /= this.Mass;


			// Max accel should be force/mass, but the thruster is currently directly changing velocity
			//retVal *= 250;        //I have to multipy by 50, because that's the rough frames per second

			return retVal;
		}

		private void AimThruster(Vector3D directionToGo)
		{
			// Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
			Vector3D axis;
			double radians;
			Math3D.GetRotation(out axis, out radians, this.OrigThrustDirection, directionToGo);

			// Thrust Direction
			this.ThrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math3D.RadiansToDegrees(radians)));

			// Thrust Strength (I'll set this with a standard algorithm.  The caller can always set it however they want)
			double maxStrength = PERCENTSTANDARD;
			if (this.IsAttacking)
			{
				maxStrength = PERCENTATTACKING;
			}

			//double forcePercent = directionToGo.Length / maxStrength;
			//if (forcePercent > 1d)
			//{
			//    forcePercent = 1d;
			//}

			double forcePercent = directionToGo.Length;
			if (forcePercent > maxStrength)
			{
				forcePercent = maxStrength;
			}

			this.ThrustPercent = forcePercent;
		}

		#endregion
	}
}
