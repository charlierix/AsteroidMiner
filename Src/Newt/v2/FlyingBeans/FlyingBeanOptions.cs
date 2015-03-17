using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Game.Newt.v2.GameItems;
using System.Windows.Media.Media3D;
using System.ComponentModel;

namespace Game.Newt.v2.FlyingBeans
{
	/// <summary>
	/// This is a set of properties that gets shared between the main window and options panels.  This can also be xaml serialized so
	/// that a session can be saved/loaded
	/// </summary>
	public class FlyingBeanOptions
	{
		/// <summary>
		/// Even though there's no threading, the user may try to manipulate values as the program is using them
		/// </summary>
		public readonly object Lock = new object();

		//-------------------------- Bean Types
		/// <summary>
		/// These are hard coded beans
		/// </summary>
		public SortedList<string, ShipDNA> DefaultBeanList
		{
			get;
			set;
		}
		/// <summary>
		/// These are the actual beans that will be used when creating a new random bean
		/// </summary>
		public SortedList<string, ShipDNA> NewBeanList
		{
			get;
			set;
		}

		//-------------------------- Bean Props

		//-------------------------- Mutation
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public MutateUtility.ShipMutateArgs MutateArgs
		{
			get;
			set;
		}

		private volatile bool _mutateChangeBody = false;
		public bool MutateChangeBody
		{
			get
			{
				return _mutateChangeBody;
			}
			set
			{
				_mutateChangeBody = value;
			}
		}

		private volatile int _bodyNumToMutate = 3;
		public int BodyNumToMutate
		{
			get
			{
				return _bodyNumToMutate;
			}
			set
			{
				_bodyNumToMutate = value;
			}
		}

		private volatile object _bodySizeChangePercent = .8d;
		public double BodySizeChangePercent
		{
			get
			{
				return (double)_bodySizeChangePercent;
			}
			set
			{
				_bodySizeChangePercent = value;
			}
		}

		private volatile object _bodyMovementAmount = .3d;
		public double BodyMovementAmount
		{
			get
			{
				return (double)_bodyMovementAmount;
			}
			set
			{
				_bodyMovementAmount = value;
			}
		}

		private volatile object _bodyOrientationChangePercent = .07d;
		public double BodyOrientationChangePercent
		{
			get
			{
				return (double)_bodyOrientationChangePercent;
			}
			set
			{
				_bodyOrientationChangePercent = value;
			}
		}

		private volatile bool _mutateChangeNeural = true;
		public bool MutateChangeNeural
		{
			get
			{
				return _mutateChangeNeural;
			}
			set
			{
				_mutateChangeNeural = value;
			}
		}

		private volatile object _neuronPercentToMutate = .02d;
		public double NeuronPercentToMutate
		{
			get
			{
				return (double)_neuronPercentToMutate;
			}
			set
			{
				_neuronPercentToMutate = value;
			}
		}

		private volatile object _neuronMovementAmount = .04d;
		public double NeuronMovementAmount
		{
			get
			{
				return (double)_neuronMovementAmount;
			}
			set
			{
				_neuronMovementAmount = value;
			}
		}

		private volatile object _linkPercentToMutate = .02d;
		public double LinkPercentToMutate
		{
			get
			{
				return (double)_linkPercentToMutate;
			}
			set
			{
				_linkPercentToMutate = value;
			}
		}

		private volatile object _linkWeightAmount = .1d;
		public double LinkWeightAmount
		{
			get
			{
				return (double)_linkWeightAmount;
			}
			set
			{
				_linkWeightAmount = value;
			}
		}

		private volatile object _linkMovementAmount = .2d;
		public double LinkMovementAmount
		{
			get
			{
				return (double)_linkMovementAmount;
			}
			set
			{
				_linkMovementAmount = value;
			}
		}

		private volatile object _linkContainerMovementAmount = .1d;
		public double LinkContainerMovementAmount
		{
			get
			{
				return (double)_linkContainerMovementAmount;
			}
			set
			{
				_linkContainerMovementAmount = value;
			}
		}

		private volatile object _linkContainerRotateAmount = .08d;
		public double LinkContainerRotateAmount
		{
			get
			{
				return (double)_linkContainerRotateAmount;
			}
			set
			{
				_linkContainerRotateAmount = value;
			}
		}

		//-------------------------- Simulation
		private volatile object _gravity = 1d;
		public double Gravity
		{
			get
			{
				return (double)_gravity;
			}
			set
			{
				_gravity = value;

				GravityFieldUniform gravField = this.GravityField;
				if (gravField != null)
				{
					gravField.Gravity = new Vector3D(0, 0, value * -1d);
				}
			}
		}

		public GravityFieldUniform GravityField
		{
			get;
			set;
		}

		private volatile int _numBeansAtATime = 15;
		public int NumBeansAtATime
		{
			get
			{
				return _numBeansAtATime;
			}
			set
			{
				_numBeansAtATime = value;
			}
		}

		private volatile object _newBeanProbOfWinner = .95d;
		public double NewBeanProbOfWinner
		{
			get
			{
				return (double)_newBeanProbOfWinner;
			}
			set
			{
				_newBeanProbOfWinner = value;
			}
		}

		private volatile bool _newBeanRandomOrientation = false;
		public bool NewBeanRandomOrientation
		{
			get
			{
				return _newBeanRandomOrientation;
			}
			set
			{
				_newBeanRandomOrientation = value;
			}
		}

		private volatile bool _newBeanRandomSpin = false;
		public bool NewBeanRandomSpin
		{
			get
			{
				return _newBeanRandomSpin;
			}
			set
			{
				_newBeanRandomSpin = value;
			}
		}

		private volatile object _angularVelocityDeath = 28d;
		public double AngularVelocityDeath
		{
			get
			{
				return (double)_angularVelocityDeath;
			}
			set
			{
				_angularVelocityDeath = value;
			}
		}

		private volatile object _maxAgeSeconds = 40d;
		public double MaxAgeSeconds
		{
			get
			{
				return (double)_maxAgeSeconds;
			}
			set
			{
				_maxAgeSeconds = value;
			}
		}

		private volatile int _maxGroundCollisions = 1;
		public int MaxGroundCollisions
		{
			get
			{
				return _maxGroundCollisions;
			}
			set
			{
				_maxGroundCollisions = value;
			}
		}

		private volatile bool _showExplosions = true;
		public bool ShowExplosions
		{
			get
			{
				return _showExplosions;
			}
			set
			{
				_showExplosions = value;
			}
		}

		//-------------------------- Tracking
		/// <summary>
		/// This holds beans that average a high enough score across several attempts
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public WinnerList WinnersFinal
		{
			get;
			set;
		}

		/// <summary>
		/// This keeps track of the beans that are currently alive (but not spawns from candidate, they are tracked separately)
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public WinnerList WinnersLive
		{
			get;
			set;
		}

		/// <summary>
		/// When a bean from the winner list dies, it gets added to this candidates list
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CandidateWinners WinnerCandidates
		{
			get;
			set;
		}

		private volatile int _finalistCount = 3;
		/// <summary>
		/// When a ship gets high enough score during its initial lifetime, it ends up in the candidate list.  Then
		/// some copies will be spawned, and their average high score will be considered for the final list.  This is
		/// is how many to spawn
		/// </summary>
		public int FinalistCount
		{
			get
			{
				return _finalistCount;
			}
			set
			{
				_finalistCount = value;
			}
		}

		//NOTE:	This is just for serializing to file (each bean is saved to a separate file to make it easier to see what's in a folder)
		public SortedList<string, double> WinningScores
		{
			get;
			set;
		}

		public volatile int _trackingMaxLineagesFinal = 3;
		public int TrackingMaxLineagesFinal
		{
			get
			{
				return _trackingMaxLineagesFinal;
			}
			set
			{
				_trackingMaxLineagesFinal = value;

				if (this.WinnersFinal != null)
				{
					this.WinnersFinal.MaxLineages = value;
				}
			}
		}

		public volatile int _trackingMaxPerLineageFinal = 3;
		public int TrackingMaxPerLineageFinal
		{
			get
			{
				return _trackingMaxPerLineageFinal;
			}
			set
			{
				_trackingMaxPerLineageFinal = value;

				if (this.WinnersFinal != null)
				{
					this.WinnersFinal.MaxPerLineage = value;
				}
			}
		}

		public volatile int _trackingMaxLineagesLive = 5;
		public int TrackingMaxLineagesLive
		{
			get
			{
				return _trackingMaxLineagesLive;
			}
			set
			{
				_trackingMaxLineagesLive = value;

				if (this.WinnersLive != null)
				{
					this.WinnersLive.MaxLineages = value;
				}
			}
		}

		public volatile int _trackingMaxPerLineageLive = 3;
		public int TrackingMaxPerLineageLive
		{
			get
			{
				return _trackingMaxPerLineageLive;
			}
			set
			{
				_trackingMaxPerLineageLive = value;

				if (this.WinnersLive != null)
				{
					this.WinnersLive.MaxPerLineage = value;
				}
			}
		}

		public volatile object _trackingScanFrequencySeconds = 1d;
		public double TrackingScanFrequencySeconds
		{
			get
			{
				return (double)_trackingScanFrequencySeconds;
			}
			set
			{
				_trackingScanFrequencySeconds = value;
			}
		}

		public long TotalBeans
		{
			get;
			set;
		}
	}
}
