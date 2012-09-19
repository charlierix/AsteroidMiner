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

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.NewtonDynamics;
using Game.Newt.HelperClasses.Primitives3D;
using Game.Newt.HelperClasses;

namespace Game.Newt.Testers
{
	public partial class ShipPartTesterWindow : Window
	{
		#region Class: TempBody

		/// <summary>
		/// This is a body that shouldn't stay around forever
		/// </summary>
		private class TempBody
		{
			#region Constructor

			public TempBody(Body body, TimeSpan maxAge)
			{
				this.IsTimeLimited = true;

				this.PhysicsBody = body;

				this.CreateTime = DateTime.Now;
				this.MaxAge = maxAge;

				this.MaxDistance = 0d;
				this.MaxDistanceSquared = 0d;
			}
			public TempBody(Body body, double maxDistance)
			{
				this.IsTimeLimited = true;

				this.PhysicsBody = body;

				this.MaxDistance = maxDistance;
				this.MaxDistanceSquared = maxDistance * maxDistance;

				this.CreateTime = DateTime.MinValue;
				this.MaxAge = new TimeSpan();
			}

			#endregion

			#region Public Properties

			public readonly Body PhysicsBody;

			public readonly bool IsTimeLimited;

			//	These have meaning if IsTimeLimited is true
			public readonly DateTime CreateTime;
			public readonly TimeSpan MaxAge;

			//	This has meaning if IsTimeLimited is false
			public readonly double MaxDistance;
			public readonly double MaxDistanceSquared;

			#endregion

			#region Public Methods

			public bool ShouldDie()
			{
				if (this.IsTimeLimited)
				{
					return DateTime.Now - this.CreateTime > MaxAge;
				}
				else
				{
					return this.PhysicsBody.Position.ToVector().LengthSquared > this.MaxDistanceSquared;
				}
			}

			#endregion
		}

		#endregion
		#region Class: ItemColors

		private class ItemColors
		{
			//Color1
			//ctrllight: E5D5A4
			//ctrl: BFB289
			//ctrldark: 6B644C
			//brown: 473C33
			//red: 941300

			//Color2
			//ctrllight: F2EFDC
			//ctrl: D9D2B0
			//ctrldark: BFB28A
			//olive: 595139
			//burntorange: 732C03

			public Color BoundryLines = Colors.Silver;

			public Color MassBall = Colors.Orange;
			public Color MassBallReflect = Colors.Yellow;
			public double MassBallReflectIntensity = 50d;
		}

		#endregion
		#region Class: ThrustController

		/// <summary>
		/// This is a proof of concept class.  The final version will be owned by the ship
		/// </summary>
		private class ThrustController : IDisposable
		{
			#region Class: ThrustContribution

			private class ThrustContribution
			{
				#region Constructor

				public ThrustContribution(Thruster thruster, int index, Vector3D translationForce, Vector3D torque)
				{
					this.Thruster = thruster;
					this.Index = index;
					this.TranslationForce = translationForce;
					this.Torque = torque;
				}

				#endregion

				public readonly Thruster Thruster;
				public readonly int Index;

				public readonly Vector3D TranslationForce;
				public readonly Vector3D Torque;
			}

			#endregion
			#region Class: ThrustSetting

			private class ThrustSetting
			{
				#region Constructor

				public ThrustSetting(Thruster thruster, int index, Vector3D translationForceFull, Vector3D torqueFull, Vector3D translationForceUnit, Vector3D torqueUnit, Vector3D translationForce, Vector3D torque, double percent)
				{
					this.Thruster = thruster;
					this.Index = index;

					this.TranslationForceFull = translationForceFull;
					this.TorqueFull = torqueFull;

					this.TranslationForceUnit = translationForceUnit;
					this.TorqueUnit = torqueUnit;

					this.TranslationForce = translationForce;
					this.Torque = torque;

					this.Percent = percent;
				}

				#endregion

				public readonly Thruster Thruster;
				public readonly int Index;

				//	These are what the translation and torque would be if percent were 1
				public readonly Vector3D TranslationForceFull;
				public readonly Vector3D TorqueFull;

				//	These are the translation and torque as unit vectors
				public readonly Vector3D TranslationForceUnit;
				public readonly Vector3D TorqueUnit;

				//	These are what the translation and torque are for the stored percent
				public readonly Vector3D TranslationForce;
				public readonly Vector3D Torque;

				//	This is how much to fire this thruster
				public readonly double Percent;
			}

			#endregion
			#region Class: ThrustSet

			private class ThrustSet
			{
				#region Constructor

				public ThrustSet(ThrustSetting[] thrusters, Vector3D translation, Vector3D torque, double fuelUsage)
				{
					this.Thrusters = thrusters;

					this.Translation = translation;
					this.Torque = torque;

					this.TranslationLength = this.Translation.Length;
					this.TorqueLength = this.Torque.Length;

					//this.TranslationUnit = this.Translation.ToUnit();
					//this.TorqueUnit = this.Torque.ToUnit();

					this.FuelUsage = fuelUsage;
				}

				#endregion

				public readonly ThrustSetting[] Thrusters;

				public readonly Vector3D Translation;
				public readonly Vector3D Torque;

				public readonly double TranslationLength;
				public readonly double TorqueLength;

				//public readonly Vector3D TranslationUnit;
				//public readonly Vector3D TorqueUnit;

				public readonly double FuelUsage;
			}

			#endregion

			#region Declaration Section

			private Ship _ship = null;
			private List<Thruster> _thrusters = null;

			private Viewport3D _viewport = null;
			private ScreenSpaceLines3D _lines = null;
			private List<Visual3D> _debugVisuals = null;

			private ItemOptions _itemOptions = null;

			private bool _isUpPressed = false;
			private bool _isDownPressed = false;

			private DateTime? _lastTick = null;

			//	These are pre calculated to help with figuring out which thrusters to fire when a certain direction is requested
			private Point3D _shipCenterMass;
			private MassMatrix _shipMassMatrix;
			private ThrustContribution[] _contributions = null;
			private ThrustSet[] _zeroTranslationSets = null;
			private ThrustSet[] _zeroTorqueSets = null;

			#endregion

			#region Constructor

			public ThrustController(Ship ship, Viewport3D viewport, ItemOptions itemOptions)
			{
				_ship = ship;
				_thrusters = ship.Thrusters;
				_viewport = viewport;
				_itemOptions = itemOptions;

				_lines = new ScreenSpaceLines3D();
				_lines.Color = Colors.Orange;
				_lines.Thickness = 2d;
				_viewport.Children.Add(_lines);
			}

			public void Dispose()
			{
				if (_viewport != null && _lines != null)
				{
					_viewport.Children.Remove(_lines);
					_lines = null;
				}

				if (_viewport != null && _debugVisuals != null)
				{
					foreach (Visual3D model in _debugVisuals)
					{
						_viewport.Children.Remove(model);
					}

					_debugVisuals = null;
				}

				_viewport = null;
			}

			#endregion

			#region Public Methods

			/// <summary>
			/// Call this whenever the ship's mass matrix changes
			/// </summary>
			public void MassChanged()
			{
				_contributions = null;
				_zeroTranslationSets = null;
				_zeroTorqueSets = null;


				EnsureThrustSetsCalculated();


			}

			/// <summary>
			/// This fires every thruster at 100%
			/// </summary>
			public void ApplyForce1(BodyApplyForceAndTorqueArgs e)
			{
				_lines.Clear();

				if (!_isUpPressed && !_isDownPressed)
				{
					return;
				}

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;

				double elapsedTime = 1d;
				DateTime curTick = DateTime.Now;
				if (_lastTick != null)
				{
					elapsedTime = (curTick - _lastTick.Value).TotalSeconds;
				}

				_lastTick = curTick;

				foreach (Thruster thruster in _thrusters)
				{
					double percent = 1d;
					Vector3D? force = thruster.Fire(ref percent, 0, elapsedTime);
					if (force != null)
					{
						Vector3D bodyForce = e.Body.DirectionToWorld(force.Value);
						Point3D bodyPoint = e.Body.PositionToWorld(thruster.Position);
						e.Body.AddForceAtPoint(bodyForce, bodyPoint);

						_lines.AddLine(bodyPoint, bodyPoint - bodyForce);		//	subtracting, so the line looks like a flame
					}
					else
					{
						int seven = -2;
					}
				}
			}
			public void ApplyForce2(BodyApplyForceAndTorqueArgs e)
			{
				_lines.Clear();

				if (!_isUpPressed && !_isDownPressed)
				{
					return;
				}

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;

				double elapsedTime = 1d;
				DateTime curTick = DateTime.Now;
				if (_lastTick != null)
				{
					elapsedTime = (curTick - _lastTick.Value).TotalSeconds;
				}

				_lastTick = curTick;

				//	Keeping each button's contribution in a set so they are easier to normalize
				List<Tuple<Thruster, int, double>[]> thrusterSets = new List<Tuple<Thruster, int, double>[]>();

				if (_isUpPressed)
				{
					thrusterSets.Add(FireThrustLinear3(e, elapsedTime, new Vector3D(0, 0, 1)).ToArray());
				}

				if (_isDownPressed)
				{
					thrusterSets.Add(FireThrustLinear3(e, elapsedTime, new Vector3D(0, 0, -1)).ToArray());
				}

				//TODO: Normalize these so no thruster will fire above 100% (keep the ratios though)

				#region Fire them

				foreach (var thruster in thrusterSets.SelectMany(o => o))
				{
					double percent = thruster.Item3;
					Vector3D? force = thruster.Item1.Fire(ref percent, thruster.Item2, elapsedTime);
					if (force != null)
					{
						Vector3D bodyForce = e.Body.DirectionToWorld(force.Value);
						Point3D bodyPoint = e.Body.PositionToWorld(thruster.Item1.Position);
						e.Body.AddForceAtPoint(bodyForce, bodyPoint);

						Vector3D lineVect = GetThrustLine(bodyForce, _itemOptions.ThrusterStrengthRatio);		//	this returns a vector in the opposite direction, so the line looks like a flame
						Point3D lineStart = bodyPoint + (lineVect.ToUnit() * thruster.Item1.ThrustVisualStartRadius);
						_lines.AddLine(lineStart, lineStart + lineVect);
					}
					else
					{
						int seven = -2;
					}
				}

				#endregion
			}

			public void KeyDown(KeyEventArgs e)
			{
				switch (e.Key)
				{
					case Key.Up:
						_isUpPressed = true;
						break;

					case Key.Down:
						_isDownPressed = true;
						break;
				}
			}
			public void KeyUp(KeyEventArgs e)
			{
				switch (e.Key)
				{
					case Key.Up:
						_isUpPressed = false;
						break;

					case Key.Down:
						_isDownPressed = false;
						break;
				}

				if (!_isDownPressed && !_isUpPressed)
				{
					_lastTick = null;
				}
			}

			public void DrawDebugVisuals()
			{
				EnsureThrustSetsCalculated();

				#region Clear existing

				if (_debugVisuals == null)
				{
					_debugVisuals = new List<Visual3D>();
				}
				else
				{
					foreach (Visual3D existing in _debugVisuals)
					{
						_viewport.Children.Remove(existing);
					}
					_debugVisuals.Clear();
				}

				#endregion

				#region Center Mass

				//	Material
				MaterialGroup materials = new MaterialGroup();
				materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("BA5E67"))));
				materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("CF6F68")), 75d));

				//	Geometry Model
				GeometryModel3D geometry = new GeometryModel3D();
				geometry.Material = materials;
				geometry.BackMaterial = materials;
				geometry.Geometry = UtilityWPF.GetSphere(5, .1d, .1d, .1d);

				ModelVisual3D model = new ModelVisual3D();
				model.Content = geometry;
				model.Transform = new TranslateTransform3D(_ship.PhysicsBody.PositionToWorld(_shipCenterMass).ToVector());

				_debugVisuals.Add(model);
				_viewport.Children.Add(model);

				#endregion
				#region Torques

				ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
				lines.Thickness = 3d;
				lines.Color = UtilityWPF.ColorFromHex("EB9B73");

				ScreenSpaceLines3D opposites = new ScreenSpaceLines3D();
				opposites.Thickness = 1d;
				opposites.Color = UtilityWPF.ColorFromHex("FFDAA5");

				foreach (var thruster in _contributions)
				{
					lines.AddLine(_ship.PhysicsBody.PositionToWorld(_shipCenterMass), _ship.PhysicsBody.PositionToWorld(_shipCenterMass) + _ship.PhysicsBody.DirectionToWorld(thruster.Torque));
					opposites.AddLine(_ship.PhysicsBody.PositionToWorld(_shipCenterMass), _ship.PhysicsBody.PositionToWorld(_shipCenterMass) - _ship.PhysicsBody.DirectionToWorld(thruster.Torque));
				}

				_debugVisuals.Add(lines);
				_debugVisuals.Add(opposites);
				_viewport.Children.Add(lines);
				_viewport.Children.Add(opposites);

				#endregion

				//	May want to do translations





			}

			#endregion

			#region Private Methods - fire

			//NOTE: direction must be a unit vector
			/// <summary>
			/// This fires any thruster that contributes to the direction at 100%
			/// </summary>
			private void FireThrustLinear1(BodyApplyForceAndTorqueArgs e, double elapsedTime, Vector3D direction)
			{
				#region Get contributing thrusters

				//	Get a list of thrusters that will contribute to the direction

				List<Tuple<Thruster, int, Vector3D, double>> contributing = new List<Tuple<Thruster, int, Vector3D, double>>();

				foreach (Thruster thruster in _thrusters)
				{
					for (int cntr = 0; cntr < thruster.ThrusterDirectionsShip.Length; cntr++)
					{
						Vector3D thrustDirUnit = thruster.ThrusterDirectionsShip[cntr].ToUnit();
						double dot = Vector3D.DotProduct(thrustDirUnit, direction);

						if (dot > 0d)
						{
							contributing.Add(new Tuple<Thruster, int, Vector3D, double>(thruster, cntr, thrustDirUnit, dot));
						}
					}
				}

				#endregion

				if (contributing.Count == 0)
				{
					return;
				}

				Point3D center = _ship.PhysicsBody.CenterOfMass;
				MassMatrix massMatrix = _ship.PhysicsBody.MassMatrix;

				//	Figure out the drift


				//	See which thrusters can most reduce that drift

				#region Fire them

				foreach (var contribute in contributing)
				{
					double percent = 1d;
					Vector3D? force = contribute.Item1.Fire(ref percent, contribute.Item2, elapsedTime);
					if (force != null)
					{
						Vector3D bodyForce = e.Body.DirectionToWorld(force.Value);
						Point3D bodyPoint = e.Body.PositionToWorld(contribute.Item1.Position);
						e.Body.AddForceAtPoint(bodyForce, bodyPoint);

						_lines.AddLine(bodyPoint, bodyPoint - bodyForce);		//	subtracting, so the line looks like a flame
					}
					else
					{
						int seven = -2;
					}
				}

				#endregion
			}
			/// <summary>
			/// This is an attempt to fix 1, but I didn't get very far
			/// </summary>
			private List<Tuple<Thruster, int, double>> FireThrustLinear2(BodyApplyForceAndTorqueArgs e, double elapsedTime, Vector3D direction)
			{
				Point3D center = _ship.PhysicsBody.CenterOfMass;
				MassMatrix massMatrix = _ship.PhysicsBody.MassMatrix;

				ThrustContribution[] contributions = GetThrusterContributions(center);

				#region Find along and against

				List<Tuple<ThrustContribution, double>> along = new List<Tuple<ThrustContribution, double>>();
				List<Tuple<ThrustContribution, double>> against = new List<Tuple<ThrustContribution, double>>();

				foreach (ThrustContribution contribution in contributions)
				{
					double dot = Vector3D.DotProduct(contribution.Thruster.ThrusterDirectionsShip[contribution.Index], direction);

					//	Don't include zero
					if (dot > 0d)
					{
						along.Add(new Tuple<ThrustContribution, double>(contribution, dot));
					}
					else if (dot < 0d)
					{
						against.Add(new Tuple<ThrustContribution, double>(contribution, dot));
					}
				}

				#endregion

				Tuple<Vector3D, Vector3D> alongCombinedForceAndTorque = GetCombinedForceAndTorque(along.Select(o => new Tuple<Vector3D, Vector3D>(o.Item1.TranslationForce, o.Item1.Torque)));


				//	I think that fixing torque drif is much more important than linear drift


				if (!Math3D.IsNearZero(alongCombinedForceAndTorque.Item2.LengthSquared))
				{
					//	There is a net torque




				}



				if (!Math3D.IsNearValue(Vector3D.DotProduct(alongCombinedForceAndTorque.Item1.ToUnit(), direction), 1d))
				{
					//	There is a linear drift



				}







				//	Exit Function
				return along.Select(o => new Tuple<Thruster, int, double>(o.Item1.Thruster, o.Item1.Index, 1d)).ToList();
			}
			private List<Tuple<Thruster, int, double>> FireThrustLinear3(BodyApplyForceAndTorqueArgs e, double elapsedTime, Vector3D direction)
			{
				//TODO: Expose these as properties of the class, or take as args to the method call
				const double PERFECTALIGN = .01;

				EnsureThrustSetsCalculated();

				//	Find the best thruster set for the requested direction, may need to combine sets

				var matches = _zeroTorqueSets.Select(o => new Tuple<ThrustSet, double>(o, Vector3D.DotProduct(o.Translation, direction))).OrderByDescending(o => o.Item2).ToArray();

				if (matches.Length > 0)
				{
					//	Find set that are perfectly aligned (direction is a unit vector, so if they are perfectly aligned, the dot product will be the translation's length)
					var perfectAlign = matches.Where(o => IsNearValue(o.Item2, o.Item1.TranslationLength, o.Item1.TranslationLength * PERFECTALIGN)).OrderByDescending(o => o.Item2).ToArray();

					if (perfectAlign.Length > 0)
					{
						ThrustSet set = GetBestPerfectAlign(perfectAlign);

						return set.Thrusters.Select(o => new Tuple<Thruster, int, double>(o.Thruster, o.Index, o.Percent)).ToList();
					}


					//TODO: Combine solutions to get the requested direction


				}

				return new List<Tuple<Thruster, int, double>>();
			}

			private static ThrustSet GetBestPerfectAlign(Tuple<ThrustSet, double>[] sets)
			{
				// Find the most fuel efficient set
				var setsWithFuelEfficiency = sets.Select(o => new Tuple<ThrustSet, double, double>(o.Item1, o.Item2, o.Item2 / o.Item1.FuelUsage)).OrderByDescending(o => o.Item3).ToArray();

				return setsWithFuelEfficiency[0].Item1;
			}

			private static Tuple<Vector3D, Vector3D> GetCombinedForceAndTorque(IEnumerable<Tuple<Vector3D, Vector3D>> forcesAndTorques)
			{
				Vector3D force = new Vector3D(0, 0, 0);
				Vector3D torque = new Vector3D(0, 0, 0);

				foreach (var item in forcesAndTorques)
				{
					force += item.Item1;
					torque += item.Item2;
				}

				return new Tuple<Vector3D, Vector3D>(force, torque);
			}

			private static Vector3D GetThrustLine(Vector3D force, double strengthRatio)
			{
				const double MULT = -1d;		//	the desired length when force.length / strengthRatio is one

				double ratio = MULT * force.Length / strengthRatio;

				return force.ToUnit() * MULT;
			}

			#endregion
			#region Private Methods - prep

			private void EnsureThrustSetsCalculated()
			{
				if (_contributions != null)
				{
					return;
				}

				_shipCenterMass = _ship.PhysicsBody.CenterOfMass;
				_shipMassMatrix = _ship.PhysicsBody.MassMatrix;

				_contributions = GetThrusterContributions(_shipCenterMass);

				List<ThrustSet> zeroTorques = new List<ThrustSet>();
				List<ThrustSet> zeroTranslations = new List<ThrustSet>();

				var illegalCombos = GetIllegalCombos(_contributions);

				double fuelToThrust = _itemOptions.FuelToThrustRatio;

				//var test = AllCombosEnumerator(_contributions.Length, illegalCombos).ToList();

				//NOTE: AllCombosEnumerator seems to only be called once, so I'm guessing AsParallel gets all enumerated values before
				//dividing up the work (I would hate for it to call AllCombosEnumerator many times - once for Count(), etc)
				//foreach (Tuple<ThrustSet[], ThrustSet[]> zeros in AllCombosEnumerator(_contributions.Length, illegalCombos).AsParallel().Select(o => GetZeros(_contributions, o, fuelToThrust)))
				foreach (Tuple<ThrustSet[], ThrustSet[]> zeros in AllCombosEnumerator(_contributions.Length, illegalCombos).Select(o => GetZeros(_contributions, o, fuelToThrust)))
				{
					if (zeros == null)
					{
						continue;
					}

					if (zeros.Item1 != null && zeros.Item1.Length > 0)
					{
						zeroTorques.AddRange(zeros.Item1);
					}

					if (zeros.Item2 != null && zeros.Item2.Length > 0)
					{
						zeroTranslations.AddRange(zeros.Item2);
					}
				}

				_zeroTorqueSets = zeroTorques.ToArray();
				_zeroTranslationSets = zeroTranslations.ToArray();
			}

			private ThrustContribution[] GetThrusterContributions(Point3D center)
			{
				List<ThrustContribution> retVal = new List<ThrustContribution>();

				foreach (Thruster thruster in _thrusters)
				{
					for (int cntr = 0; cntr < thruster.ThrusterDirectionsShip.Length; cntr++)
					{
						//	This is copied from Body.AddForceAtPoint

						Vector3D offsetFromMass = thruster.Position - center;		//	this is ship's local coords
						Vector3D force = thruster.ThrusterDirectionsShip[cntr] * thruster.ForceAtMax;

						Vector3D translationForce, torque;
						Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out torque, offsetFromMass, force);

						retVal.Add(new ThrustContribution(thruster, cntr, translationForce, torque));
					}
				}

				//	Exit Function
				return retVal.ToArray();
			}

			/// <summary>
			/// This taks a set of thrusters and tries to find combinations that will produce zero torque and combinations that will produce zero translation
			/// </summary>
			/// <returns>
			/// Item1=Torques
			/// Item2=Translations
			/// </returns>
			private static Tuple<ThrustSet[], ThrustSet[]> GetZeros(ThrustContribution[] all, int[] combo, double fuelToThrust)
			{
				//	Get an array of the referenced thrusters
				ThrustContribution[] used = new ThrustContribution[combo.Length];
				for (int cntr = 0; cntr < combo.Length; cntr++)
				{
					used[cntr] = all[combo[cntr]];
				}

				//This is now done up front (more efficient)
				//	Make sure this combo doesn't have the same thruster firing is opposite directions
				//if (!IsValidCombo(used))
				//{
				//    return null;
				//}

				//	Try to find combinations of these thrusters that will produce zero torque and zero translation
				List<ThrustSet> torques = GetZeroTorques(used, fuelToThrust);
				List<ThrustSet> translations = GetZeroTranslations(used, fuelToThrust);

				//	Exit Function
				if (torques.Count == 0 && translations.Count == 0)
				{
					return null;
				}
				else
				{
					return new Tuple<ThrustSet[], ThrustSet[]>(torques.ToArray(), translations.ToArray());
				}
			}

			private static Tuple<int, int>[] GetIllegalCombos(ThrustContribution[] thrusters)
			{
				List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

				for (int outer = 0; outer < thrusters.Length - 1; outer++)
				{
					for (int inner = outer + 1; inner < thrusters.Length; inner++)
					{
						if (thrusters[outer].Thruster == thrusters[inner].Thruster)
						{
							Vector3D dir1 = thrusters[outer].Thruster.ThrusterDirectionsModel[thrusters[outer].Index];
							Vector3D dir2 = thrusters[inner].Thruster.ThrusterDirectionsModel[thrusters[inner].Index];

							if (Math3D.IsNearValue(Vector3D.DotProduct(dir1, dir2), -1d))
							{
								retVal.Add(new Tuple<int, int>(outer, inner));
							}
						}
					}
				}

				return retVal.ToArray();
			}

			//TODO: Finish these
			private static List<ThrustSet> GetZeroTorques(ThrustContribution[] used, double fuelToThrust)
			{
				//	Split the list into inline thrusters, and thrusters that produce torque.  So there could be 5 total, but if only 2
				//	produce torque, only those 2 are used in the torque calculations
				List<ThrustContribution> noTorque = new List<ThrustContribution>();
				List<ThrustContribution> hasTorque = new List<ThrustContribution>();

				for (int cntr = 0; cntr < used.Length; cntr++)
				{
					if (IsNearZeroTorque(used[cntr].Torque))
					{
						noTorque.Add(used[cntr]);
					}
					else
					{
						hasTorque.Add(used[cntr]);
					}
				}

				List<ThrustSet> retVal = new List<ThrustSet>();

				if (hasTorque.Count == 0)
				{
					//	All zero torque
					retVal.AddRange(GetZeroTorquesSprtZeroTorque(noTorque, fuelToThrust));
				}
				else if (hasTorque.Count == 2)
				{
					//	Two
					retVal.AddRange(GetZeroTorquesSprtTwoTorque(noTorque, hasTorque, fuelToThrust));
				}
				else if (hasTorque.Count > 2)
				{
					//	Many
					retVal.AddRange(GetZeroTorquesSprtManyTorque(noTorque, hasTorque, fuelToThrust));
				}

				//	Throw out any that have zero translation
				retVal = retVal.Where(o => !IsNearZeroTranslation(o.TranslationLength)).ToList();

				//	Exit Function
				return retVal;
			}
			private static List<ThrustSet> GetZeroTorquesSprtZeroTorque(List<ThrustContribution> noTorque, double fuelToThrust)
			{
				Vector3D translation = new Vector3D(0, 0, 0);
				Vector3D torque = new Vector3D(0, 0, 0);
				double fuelUsed = 0d;

				ThrustSetting[] thrusters = new ThrustSetting[noTorque.Count];
				for (int cntr = 0; cntr < noTorque.Count; cntr++)
				{
					thrusters[cntr] = new ThrustSetting(noTorque[cntr].Thruster, noTorque[cntr].Index, noTorque[cntr].TranslationForce, noTorque[cntr].Torque, noTorque[cntr].TranslationForce.ToUnit(), noTorque[cntr].Torque.ToUnit(), noTorque[cntr].TranslationForce, noTorque[cntr].Torque, 1d);

					translation += noTorque[cntr].TranslationForce;
					torque += noTorque[cntr].Torque;
					fuelUsed += noTorque[cntr].Thruster.ForceAtMax * fuelToThrust;
				}

				List<ThrustSet> retVal = new List<ThrustSet>();
				retVal.Add(new ThrustSet(thrusters, translation, torque, fuelUsed));
				return retVal;
			}
			private static List<ThrustSet> GetZeroTorquesSprtTwoTorque(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust)
			{
				List<ThrustSet> retVal = new List<ThrustSet>();

				//	Both torques are non zero.  See if they're opposed
				Vector3D torque1Unit = hasTorque[0].Torque.ToUnit();
				Vector3D torque2Unit = hasTorque[1].Torque.ToUnit();

				double dot = Vector3D.DotProduct(torque1Unit, torque2Unit);

				if (!IsNearValueTorquesDot(dot, -1d))
				{
					//	Nothing to return
					return retVal;
				}

				//	They are opposed, now get the ratio between the two
				double[] lengths = new double[] { hasTorque[0].Torque.Length, hasTorque[1].Torque.Length };

				Vector3D translation = new Vector3D(0, 0, 0);
				Vector3D torque = new Vector3D(0, 0, 0);
				double fuelUsed = 0d;

				List<ThrustSetting> thrusters = new List<ThrustSetting>();

				//	Add the two torque producing thrusters
				for (int cntr = 0; cntr < hasTorque.Count; cntr++)
				{
					#region Add opposing torques

					double lengthThis = lengths[cntr];
					double lengthOther = cntr == 0 ? lengths[1] : lengths[0];

					double percent = 1d;

					//	Figure out the percent.  The smaller one fires at 100%.  The other is a fraction of that
					if (!Math3D.IsNearValue(lengthThis, lengthOther) && lengthThis > lengthOther)
					{
						percent = lengthOther / lengthThis;
					}

					thrusters.Add(new ThrustSetting(hasTorque[cntr].Thruster, hasTorque[cntr].Index, hasTorque[cntr].TranslationForce, hasTorque[cntr].Torque, hasTorque[cntr].TranslationForce.ToUnit(), hasTorque[cntr].Torque.ToUnit(), hasTorque[cntr].TranslationForce * percent, hasTorque[cntr].Torque * percent, percent));

					translation += hasTorque[cntr].TranslationForce * percent;
					torque += hasTorque[cntr].Torque * percent;
					fuelUsed += hasTorque[cntr].Thruster.ForceAtMax * fuelToThrust * percent;

					#endregion
				}

				//	Add any inline thrusters each at 100%
				foreach (ThrustContribution thruster in noTorque)
				{
					#region Add inline

					thrusters.Add(new ThrustSetting(thruster.Thruster, thruster.Index, thruster.TranslationForce, thruster.Torque, thruster.TranslationForce.ToUnit(), thruster.Torque.ToUnit(), thruster.TranslationForce, thruster.Torque, 1d));

					translation += thruster.TranslationForce;
					torque += thruster.Torque;
					fuelUsed += thruster.Thruster.ForceAtMax * fuelToThrust;

					#endregion
				}

				retVal.Add(new ThrustSet(thrusters.ToArray(), translation, torque, fuelUsed));

				//	Exit Function
				return retVal;
			}
			private static List<ThrustSet> GetZeroTorquesSprtManyTorque(List<ThrustContribution> noTorque, List<ThrustContribution> hasTorque, double fuelToThrust)
			{

				#region See if adds to zero

				bool[] foundPlusMinus = new bool[6];
				foreach (ThrustContribution thrust in hasTorque)
				{
					if (thrust.Torque.X > 0)
					{
						foundPlusMinus[0] = true;
					}
					else if (thrust.Torque.X < 0)
					{
						foundPlusMinus[1] = true;
					}

					if (thrust.Torque.Y > 0)
					{
						foundPlusMinus[2] = true;
					}
					else if (thrust.Torque.Y < 0)
					{
						foundPlusMinus[3] = true;
					}

					if (thrust.Torque.Z > 0)
					{
						foundPlusMinus[4] = true;
					}
					else if (thrust.Torque.Z < 0)
					{
						foundPlusMinus[5] = true;
					}
				}

				List<ThrustSet> retVal = new List<ThrustSet>();

				if (foundPlusMinus[0] != foundPlusMinus[1] || foundPlusMinus[2] != foundPlusMinus[3] || foundPlusMinus[4] != foundPlusMinus[5])
				{
					//	Nothing to return
					return retVal;
				}

				if (!foundPlusMinus.All(o => o))
				{
					//	Nothing to return
					return retVal;
				}

				#endregion

				//	Not exactly sure how to figure this out, but I'm guessing it's some combination of cross and dot products

				//	May need lots of visualizations of thruster setup, and the debug lines

				//	There could be stable sets within what was passed in, for example:
				//		4 thrusters in a cross pointing down.  Each opposite pair perfectly oppose each other, but all 4 can be fired at 100%
				//		

				//	If the torque is along or against the translation, that should also be added, but marked (this would cause the
				//	ship to spin like a football but at least it won't wobble)



				return new List<ThrustSet>();
			}

			private static List<ThrustSet> GetZeroTranslations(ThrustContribution[] used, double fuelToThrust)
			{
				return new List<ThrustSet>();
			}

			private static bool IsNearZeroTorque(Vector3D testVect)
			{
				const double NEARZERO = .05d;

				return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
			}
			private static bool IsNearValueTorquesDot(double testValue, double compareTo)
			{
				const double NEARZERO = .0005d;

				return testValue >= compareTo - NEARZERO && testValue <= compareTo + NEARZERO;
			}

			private static bool IsNearZeroTranslation(Vector3D testVect)
			{
				const double NEARZERO = .05d;

				return Math.Abs(testVect.X) <= NEARZERO && Math.Abs(testVect.Y) <= NEARZERO && Math.Abs(testVect.Z) <= NEARZERO;
			}
			private static bool IsNearZeroTranslation(double length)
			{
				const double NEARZERO = .05d;

				return Math.Abs(length) <= NEARZERO;
			}

			private static bool IsNearValue(double testValue, double compareTo, double threshold)
			{
				return testValue >= compareTo - threshold && testValue <= compareTo + threshold;
			}

			/// <summary>
			/// This iterates over all combinations of a set of numbers
			/// NOTE: The number of iterations is (2^inputSize) - 1, so be careful with input sizes over 10 to 15
			/// </summary>
			/// <remarks>
			/// For example, if you pass in 4, you will get:
			///		0,1,2,3
			///		0,1,2
			///		0,1,3
			///		0,2,3
			///		1,2,3
			///		0,1
			///		0,2
			///		0,3
			///		1,2
			///		1,3
			///		2,3
			///		0
			///		1
			///		2
			///		3
			/// </remarks>
			private static IEnumerable<int[]> AllCombosEnumerator(int inputSize)
			{
				int inputMax = inputSize - 1;		//	save me from subtracting one all the time

				for (int numUsed = inputSize; numUsed >= 1; numUsed--)
				{
					int usedMax = numUsed - 1;		//	save me from subtracting one all the time

					//	Seed the return with everything at the left
					int[] retVal = Enumerable.Range(0, numUsed).ToArray();
					yield return (int[])retVal.Clone();		//	if this isn't cloned here, then the consumer needs to do it

					while (true)
					{
						//	Try to bump the last item
						if (retVal[usedMax] < inputMax)
						{
							retVal[usedMax]++;
							yield return (int[])retVal.Clone();
							continue;
						}

						//	The last item is as far as it will go, find an item to the left of it to bump
						bool foundOne = false;

						for (int cntr = usedMax - 1; cntr >= 0; cntr--)
						{
							if (retVal[cntr] < retVal[cntr + 1] - 1)
							{
								//	This one has room to bump
								retVal[cntr]++;

								//	Reset everything to the right of this spot
								for (int resetCntr = cntr + 1; resetCntr < numUsed; resetCntr++)
								{
									retVal[resetCntr] = retVal[cntr] + (resetCntr - cntr);
								}

								foundOne = true;
								yield return (int[])retVal.Clone();
								break;
							}
						}

						if (!foundOne)
						{
							//	This input size is exhausted (everything is as far right as they can go)
							break;
						}
					}
				}
			}
			private static IEnumerable<int[]> AllCombosEnumerator(int inputSize, Tuple<int, int>[] illegalPairs)
			{
				foreach (int[] retVal in AllCombosEnumerator(inputSize))
				{
					bool isValid = true;

					for (int illegalCntr = 0; illegalCntr < illegalPairs.Length; illegalCntr++)
					{
						bool found1 = false;
						bool found2 = false;

						for (int returnCntr = 0; returnCntr < retVal.Length; returnCntr++)
						{
							if (retVal[returnCntr] == illegalPairs[illegalCntr].Item1)
							{
								found1 = true;
							}
							else if (retVal[returnCntr] == illegalPairs[illegalCntr].Item2)
							{
								found2 = true;
							}
						}

						if (found1 && found2)
						{
							isValid = false;
							break;
						}
					}

					if (isValid)
					{
						yield return retVal;
					}
				}
			}

			#endregion
		}

		#endregion

		#region Declaration Section

		private Random _rand = new Random();

		private bool _isInitialized = false;

		private EditorOptions _editorOptions = new EditorOptions();
		private ItemOptions _itemOptions = new ItemOptions();
		private ItemColors _colors = new ItemColors();

		private Point3D _boundryMin;
		private Point3D _boundryMax;

		private World _world = null;
		private Map _map = null;
		private RadiationField _radiation = null;

		private MaterialManager _materialManager = null;
		private int _material_Asteroid = -1;
		private int _material_Ship = -1;
		private int _material_Sand = -1;

		private ScreenSpaceLines3D _boundryLines = null;

		/// <summary>
		/// This listens to the mouse/keyboard and controls the camera
		/// </summary>
		/// <remarks>
		/// I'll only create this when debuging
		/// </remarks>
		private TrackBallRoam _trackball = null;

		private List<Visual3D> _currentVisuals = new List<Visual3D>();
		private Body _currentBody = null;
		private Ship _ship = null;
		private ThrustController _thrustController = null;

		private List<TempBody> _tempBodies = new List<TempBody>();
		private DateTime _lastSandAdd = DateTime.MinValue;

		/// <summary>
		/// When a model is created, this is the direction it starts out as
		/// </summary>
		private DoubleVector _defaultDirectionFacing = new DoubleVector(1, 0, 0, 0, 0, 1);

		#endregion

		#region Constructor

		public ShipPartTesterWindow()
		{
			InitializeComponent();

			_itemOptions.ThrusterStrengthRatio = 100000d;
			//_itemOptions.ThrusterStrengthRatio = 500d;
			_itemOptions.FuelToThrustRatio /= _itemOptions.ThrusterStrengthRatio;

			_isInitialized = true;
		}

		#endregion

		#region Event Listeners

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			#region Trackball

			//	Trackball
			_trackball = new TrackBallRoam(_camera);
			_trackball.KeyPanScale = 1d;
			_trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
			_trackball.AllowZoomOnMouseWheel = true;

			#region copied from MouseComplete_NoLeft - middle button changed

			TrackBallMapping complexMapping = null;

			//	Middle Button
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

			//	Left+Right Buttons (emulate middle)
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

			//	Right Button
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
		}
		private void Window_Closed(object sender, EventArgs e)
		{
			try
			{
				//	Temp Bodies
				foreach (TempBody body in _tempBodies)
				{
					body.PhysicsBody.Dispose();
				}
				_tempBodies.Clear();

				//	Current Body
				if (_currentBody != null)
				{
					_currentBody.Dispose();
				}
				_currentBody = null;

				//TODO: Ship

				//	Map
				if (_map != null)
				{
					_map.Dispose();		//	this will dispose the physics bodies
					_map = null;
				}

				//	World
				if (_world != null)
				{
					_world.Pause();
					_world.Dispose();
					_world = null;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void World_Updating(object sender, WorldUpdatingArgs e)
		{
			try
			{
				#region Remove temp bodies

				int index = 0;
				while (index < _tempBodies.Count)
				{
					if (_tempBodies[index].ShouldDie())
					{
						foreach (Visual3D visual in _tempBodies[index].PhysicsBody.Visuals)
						{
							_viewport.Children.Remove(visual);
						}

						_tempBodies[index].PhysicsBody.Dispose();

						_tempBodies.RemoveAt(index);
					}
					else
					{
						index++;
					}
				}

				#endregion

				if (radFallingSand.IsChecked.Value || (radFallingSandPlates.IsChecked.Value && (DateTime.Now - _lastSandAdd).TotalMilliseconds > 5000d))
				{
					#region Falling Sand

					bool isPlate = radFallingSandPlates.IsChecked.Value;
					Color color = UtilityWPF.ColorFromHex("FFF5EE95");
					Vector3D size = new Vector3D(.05, .05, .05);
					double maxDist = 1.1 * size.LengthSquared;
					double mass = trkMass.Value;
					double radius = 1d;
					double startHeight = 1d;
					TimeSpan lifespan = TimeSpan.FromSeconds(15d);
					Vector3D velocity = new Vector3D(0, 0, -trkSpeed.Value);
					int numCubes = isPlate ? 150 : 1;

					//	Calculate positions (they must all be calculated up front, or some sand will be built later)
					List<Vector3D> positions = new List<Vector3D>();
					for (int cntr = 0; cntr < numCubes; cntr++)
					{
						Vector3D startPos;
						while (true)
						{
							startPos = Math3D.GetRandomVectorSpherical2D(radius);

							if (!positions.Any(o => (o - startPos).LengthSquared < maxDist))
							{
								break;
							}
						}

						positions.Add(startPos);
					}

					//	Place the sand
					for (int cntr = 0; cntr < numCubes; cntr++)
					{
						Vector3D startPos = new Vector3D(positions[cntr].X, positions[cntr].Y, startHeight);
						Quaternion orientation = isPlate ? Quaternion.Identity : Math3D.GetRandomRotation();

						Body body = GetNewBody(CollisionShapeType.Box, color, size, mass, startPos.ToPoint(), orientation, _material_Sand);
						body.Velocity = velocity;
						body.LinearDamping = 0d;
						body.AngularDamping = new Vector3D(0d, 0d, 0d);

						_tempBodies.Add(new TempBody(body, lifespan));
					}

					//	Remember when this was done
					_lastSandAdd = DateTime.Now;

					#endregion
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Ship_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
		{
			if (_thrustController != null)
			{
				//_thrustController.ApplyForce1(e);
				_thrustController.ApplyForce2(e);
			}
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (_thrustController != null)
			{
				_thrustController.KeyDown(e);
			}
		}
		private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (_thrustController != null)
			{
				_thrustController.KeyUp(e);
			}
		}

		private void Collision_Ship(object sender, MaterialCollisionArgs e)
		{
		}
		private void Collision_Sand(object sender, MaterialCollisionArgs e)
		{
			//	When the sand hits the ship, it is staying alive too long and messing up other sand
			Body body = e.GetBody(_material_Sand);

			TempBody tempBody = _tempBodies.FirstOrDefault(o => o.PhysicsBody == body);
			if (tempBody != null)
			{
				_tempBodies.Remove(tempBody);
				_tempBodies.Add(new TempBody(tempBody.PhysicsBody, TimeSpan.FromSeconds(1)));
			}
		}

		private void btnAmmoBox_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
				ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;

				double mass1 = ammoBox.TotalMass;

				double remainder = ammoBox.AddQuantity(.05d, false);
				double mass2 = ammoBox.TotalMass;

				remainder = ammoBox.AddQuantity(500d, false);
				double mass3 = ammoBox.TotalMass;

				double output = ammoBox.RemoveQuantity(.1d, false);
				double mass4 = ammoBox.TotalMass;

				output = ammoBox.RemoveQuantity(ammoBox.RemovalMultiple * 2d, false);
				double mass5 = ammoBox.TotalMass;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSingleFuel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				double mass1 = fuelTank.TotalMass;

				double remainder = fuelTank.AddQuantity(.05d, false);
				double mass2 = fuelTank.TotalMass;

				remainder = fuelTank.AddQuantity(500d, false);
				double mass3 = fuelTank.TotalMass;

				double output = fuelTank.RemoveQuantity(.1d, false);
				double mass4 = fuelTank.TotalMass;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSingleEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				double mass1 = energyTank.TotalMass;

				double remainder = energyTank.AddQuantity(.05d, false);
				double mass2 = energyTank.TotalMass;

				remainder = energyTank.AddQuantity(500d, false);
				double mass3 = energyTank.TotalMass;

				double output = energyTank.RemoveQuantity(.1d, false);
				double mass4 = energyTank.TotalMass;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnMultiEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				List<EnergyTank> tanks = new List<EnergyTank>();
				ContainerGroup group = new ContainerGroup();
				group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;

				for (int cntr = 0; cntr < 3; cntr++)
				{
					PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
					double xy = _rand.NextDouble() * 3d;
					double z = _rand.NextDouble() * 3d;
					dna.Scale = new Vector3D(xy, xy, z);

					EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

					tanks.Add(energyTank);
					group.AddContainer(energyTank);
				}

				#region Test1

				double max = group.QuantityMax;

				double remainder1 = group.AddQuantity(max * .5d, false);
				double remainder2 = group.RemoveQuantity(max * .25d, false);
				double remainder3 = group.RemoveQuantity(max * .33d, true);		//	should fail
				double remainder4 = group.AddQuantity(max, false);		//	partial add

				group.QuantityCurrent *= .5d;

				group.RemoveContainer(tanks[0], false);
				group.AddContainer(tanks[0]);

				group.RemoveContainer(tanks[0], true);
				group.AddContainer(tanks[0]);

				#endregion

				//TODO: Finish these
				//TODO: Test setting max (with just regular containers)

				#region Test2

				group.QuantityCurrent = 0d;

				group.Ownership = ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;
				//group.Ownership = ContainerGroup.ContainerOwnershipType.QuantitiesCanChange;

				tanks[0].QuantityCurrent *= .5d;		//	this will make sole owner fail, but the second enum will handle it

				group.QuantityCurrent = max * .75d;

				group.RemoveQuantity(max * .33d, false);
				group.AddQuantity(max * .5d, false);

				group.RemoveContainer(tanks[0], true);
				group.AddContainer(tanks[0]);

				#endregion

				#region Test3

				group.OnlyRemoveMultiples = true;
				group.RemovalMultiple = max * .25d;

				group.QuantityCurrent = group.QuantityMax;

				double rem1 = group.RemoveQuantity(max * .1d, false);
				double rem2 = group.RemoveQuantity(max * .1d, true);

				group.QuantityCurrent = group.QuantityMax;

				double rem3 = group.RemoveQuantity(max * .3d, false);
				double rem4 = group.RemoveQuantity(max * .3d, true);

				#endregion
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnEnergyToAmmo_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(ConverterEnergyToAmmo.PARTTYPE);
				ConverterEnergyToAmmo converter = new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dna, energyTank, ammoBox);

				energyTank.QuantityCurrent = energyTank.QuantityMax;

				double mass = converter.DryMass;
				mass = converter.TotalMass;

				converter.Transfer(1d, .5d);
				converter.Transfer(1d, 1d);
				converter.Transfer(1d, .1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnEnergyToFuel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(ConverterEnergyToFuel.PARTTYPE);
				ConverterEnergyToFuel converter = new ConverterEnergyToFuel(_editorOptions, _itemOptions, dna, energyTank, fuelTank);

				energyTank.QuantityCurrent = energyTank.QuantityMax;

				double mass = converter.DryMass;
				mass = converter.TotalMass;

				converter.Transfer(1d, .5d);
				converter.Transfer(1d, 1d);
				converter.Transfer(1d, .1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnFuelToEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				dna = GetDefaultDNA(ConverterFuelToEnergy.PARTTYPE);
				ConverterFuelToEnergy converter = new ConverterFuelToEnergy(_editorOptions, _itemOptions, dna, fuelTank, energyTank);

				fuelTank.QuantityCurrent = fuelTank.QuantityMax;

				double mass = converter.DryMass;
				mass = converter.TotalMass;

				converter.Transfer(1d, .5d);
				converter.Transfer(1d, 1d);
				converter.Transfer(1d, .1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSolarPanel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				RadiationField radiation = new RadiationField();
				radiation.AmbientRadiation = 1d;

				ConverterRadiationToEnergyDNA dna2 = new ConverterRadiationToEnergyDNA()
				{
					PartType = ConverterRadiationToEnergy.PARTTYPE,
					Shape = GetRandomEnum<SolarPanelShape>(),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				ConverterRadiationToEnergy solar = new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dna2, energyTank, radiation);

				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
				solar.Transfer(1d, Transform3D.Identity);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThruster_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				ThrusterDNA dna2 = new ThrusterDNA()
				{
					PartType = Thruster.PARTTYPE,
					ThrusterType = GetRandomEnum<ThrusterType>(),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				Thruster thruster = new Thruster(_editorOptions, _itemOptions, dna2, fuelTank);

				fuelTank.QuantityCurrent = fuelTank.QuantityMax;

				double percent;
				Vector3D? thrust;

				do
				{
					percent = 1d;
					thrust = thruster.Fire(ref percent, 0, 1d);
				} while (fuelTank.QuantityCurrent > 0d);

				percent = 1d;
				thrust = thruster.Fire(ref percent, 0, 1d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnStandaloneAmmoEmpty_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
				ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;

				BuildStandalonePart(ammoBox);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(ammoBox.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneAmmoFull_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(AmmoBox.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

				AmmoBox ammoBox = new AmmoBox(_editorOptions, _itemOptions, dna);
				ammoBox.RemovalMultiple = ammoBox.QuantityMax * .1d;
				ammoBox.QuantityCurrent = ammoBox.QuantityMax;

				BuildStandalonePart(ammoBox);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(ammoBox.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneFuelEmpty_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
				double radius = (dna.Scale.X + dna.Scale.Y) * .5d;
				dna.Scale = new Vector3D(radius, radius, dna.Scale.Z);

				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);

				BuildStandalonePart(fuelTank);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(fuelTank.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneFuelFull_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(FuelTank.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
				double radius = (dna.Scale.X + dna.Scale.Y) * .5d;
				dna.Scale = new Vector3D(radius, radius, dna.Scale.Z);

				//dna.Scale = new Vector3D(2, 2, .9);
				//dna.Scale = new Vector3D(1.15, 1.15, 4);

				FuelTank fuelTank = new FuelTank(_editorOptions, _itemOptions, dna);
				fuelTank.QuantityCurrent = fuelTank.QuantityMax;

				BuildStandalonePart(fuelTank);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(fuelTank.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(EnergyTank.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
				double radius = (dna.Scale.X + dna.Scale.Y) * .5d;
				dna.Scale = new Vector3D(radius, radius, dna.Scale.Z);

				EnergyTank energyTank = new EnergyTank(_editorOptions, _itemOptions, dna);

				BuildStandalonePart(energyTank);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(energyTank.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneEnergyToAmmo_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(ConverterEnergyToAmmo.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
				double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
				dna.Scale = new Vector3D(size, size, size);

				ConverterEnergyToAmmo converter = new ConverterEnergyToAmmo(_editorOptions, _itemOptions, dna, null, null);

				BuildStandalonePart(converter);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(converter.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneEnergyToFuel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(ConverterEnergyToFuel.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
				double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
				dna.Scale = new Vector3D(size, size, size);

				ConverterEnergyToFuel converter = new ConverterEnergyToFuel(_editorOptions, _itemOptions, dna, null, null);

				BuildStandalonePart(converter);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(converter.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneFuelToEnergy_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PartDNA dna = GetDefaultDNA(ConverterFuelToEnergy.PARTTYPE);
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

				//	It's inacurate to comment this out, but it tests the collision hull better
				double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
				dna.Scale = new Vector3D(size, size, size);

				ConverterFuelToEnergy converter = new ConverterFuelToEnergy(_editorOptions, _itemOptions, dna, null, null);

				BuildStandalonePart(converter);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(converter.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneSolarPanel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				RadiationField radiation = new RadiationField();
				radiation.AmbientRadiation = 1d;

				ConverterRadiationToEnergyDNA dna = new ConverterRadiationToEnergyDNA()
				{
					PartType = ConverterRadiationToEnergy.PARTTYPE,
					Shape = GetRandomEnum<SolarPanelShape>(),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);

				ConverterRadiationToEnergy solar = new ConverterRadiationToEnergy(_editorOptions, _itemOptions, dna, null, radiation);

				BuildStandalonePart(solar);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(solar.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnStandaloneThruster_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ThrusterDNA dna = new ThrusterDNA()
				{
					PartType = Thruster.PARTTYPE,
					ThrusterType = GetRandomEnum<ThrusterType>(ThrusterType.Custom),
					Position = new Point3D(0, 0, 0),
					Orientation = Quaternion.Identity,
					Scale = new Vector3D(1, 1, 1)
				};
				ModifyDNA(dna, chkStandaloneRandSize.IsChecked.Value, chkStandaloneRandOrientation.IsChecked.Value);
				double size = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;
				dna.Scale = new Vector3D(size, size, size);

				Thruster thruster = new Thruster(_editorOptions, _itemOptions, dna, null);

				BuildStandalonePart(thruster);

				if (chkStandaloneShowMassBreakdown.IsChecked.Value)
				{
					double cellSize = Math3D.Max(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) * UtilityHelper.GetScaledValue_Capped(.1d, .3d, 0d, 1d, _rand.NextDouble());
					DrawMassBreakdown(thruster.GetMassBreakdown(cellSize), cellSize);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnShipBasic_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(-.75, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });
				parts.Add(new PartDNA() { PartType = EnergyTank.PARTTYPE, Position = new Point3D(.75, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);

				_map.AddItem(_ship);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSimplestFlyer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, .5), Orientation = Quaternion.Identity, Scale = new Vector3D(2, 2, .9) });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 0, -.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);
				_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

				_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
				_ship.RecalculateMass();
				_thrustController.MassChanged();

				_map.AddItem(_ship);

				grdViewPort.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnShip3Thrust_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -1), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, .65) });

				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1.3, 0, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, 1.125833025, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-0.65, -1.125833025, 2), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);
				_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

				_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
				_ship.RecalculateMass();
				_thrustController.MassChanged();

				//TODO: May want to make a checkbox
				_thrustController.DrawDebugVisuals();

				_map.AddItem(_ship);

				grdViewPort.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnShip3ThrustRand_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, -1), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, .65) });

				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVectorSphericalShell2D(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVectorSphericalShell2D(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = Math3D.GetRandomVectorSphericalShell2D(1.3).ToPoint(), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1), ThrusterType = ThrusterType.One });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);
				_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

				_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
				_ship.RecalculateMass();
				_thrustController.MassChanged();

				//TODO: May want to make a checkbox
				_thrustController.DrawDebugVisuals();

				_map.AddItem(_ship);

				grdViewPort.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnShipSimpleFlyer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(3, 3, 1) });

				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two });

				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.One });

				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 90d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two_Two_One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 270d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two_Two_One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 180d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two_Two_One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 0d), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = ThrusterType.Two_Two_One });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);
				_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

				_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

				//double mass = _ship.PhysicsBody.Mass;

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
				_ship.RecalculateMass();
				_thrustController.MassChanged();

				//mass = _ship.PhysicsBody.Mass;

				_map.AddItem(_ship);

				grdViewPort.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnShipWackyFlyer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(3, 3, 1) });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(1, 0, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = GetRandomEnum<ThrusterType>(ThrusterType.Custom) });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1, 0, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = GetRandomEnum<ThrusterType>(ThrusterType.Custom) });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, 1, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = GetRandomEnum<ThrusterType>(ThrusterType.Custom) });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(0, -1, 0), Orientation = Math3D.GetRandomRotation(), Scale = new Vector3D(.5d, .5d, .5d), ThrusterType = GetRandomEnum<ThrusterType>(ThrusterType.Custom) });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);
				_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

				_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

				//double mass = _ship.PhysicsBody.Mass;

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
				_ship.RecalculateMass();
				_thrustController.MassChanged();

				//mass = _ship.PhysicsBody.Mass;

				_map.AddItem(_ship);

				grdViewPort.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnShipChallenge1_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.611527511599856, 0, 0), Orientation = new Quaternion(0, -0.706493084706277, 0, 0.707719945502605), Scale = new Vector3D(4.70545346938791, 4.70545346938791, 1.04748080326409) });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1.48216852903668, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1.65021551755816, 1.65021551755816, 1.65021551755816), ThrusterType = ThrusterType.Two });
				//parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-2.60730396412872, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 270), Scale = new Vector3D(0.71390056433019, 0.71390056433019, 0.71390056433019), ThrusterType = ThrusterType.Two_Two_One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1.48216852903668, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1.65021551755816, 1.65021551755816, 1.65021551755816), ThrusterType = ThrusterType.Two });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-2.60730396412872, 0, 0), Orientation = new Quaternion(new Vector3D(0, 0, 1), 270), Scale = new Vector3D(0.71390056433019, 0.71390056433019, 0.71390056433019), ThrusterType = ThrusterType.Two });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);
				_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

				_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

				//double mass = _ship.PhysicsBody.Mass;

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
				_ship.RecalculateMass();
				_thrustController.MassChanged();

				//mass = _ship.PhysicsBody.Mass;

				_map.AddItem(_ship);

				grdViewPort.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnShipChallenge2_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				List<PartDNA> parts = new List<PartDNA>();
				parts.Add(new PartDNA() { PartType = FuelTank.PARTTYPE, Position = new Point3D(0.611527511599856, 0, 0.0153375982352619), Orientation = new Quaternion(0, -0.706493084706277, 0, 0.707719945502605), Scale = new Vector3D(4.70545346938791, 4.70545346938791, 1.04748080326409) });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-1.48216852903668, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1.65021551755816, 1.65021551755816, 1.65021551755816), ThrusterType = ThrusterType.Two });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-2.60730396412872, 1.18811621237628, -0.0147591913688635), Orientation = new Quaternion(0, 0, -0.846976393198269, 0.531630500784946), Scale = new Vector3D(0.71390056433019, 0.71390056433019, 0.71390056433019), ThrusterType = ThrusterType.Two_Two_One });
				parts.Add(new ThrusterDNA() { PartType = Thruster.PARTTYPE, Position = new Point3D(-2.60721450068017, -1.18828382189838, -0.0147591913688617), Orientation = new Quaternion(0, 0, -0.496864090131338, 0.867828367788216), Scale = new Vector3D(0.71390056433019, 0.71390056433019, 0.71390056433019), ThrusterType = ThrusterType.Two_Two_One });

				ShipDNA shipDNA = ShipDNA.Create(parts);

				_ship = new Ship(_editorOptions, _itemOptions, shipDNA, _world, _material_Ship, _radiation);
				_ship.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Ship_ApplyForceAndTorque);

				_thrustController = new ThrustController(_ship, _viewport, _itemOptions);

				//double mass = _ship.PhysicsBody.Mass;

				_ship.Fuel.QuantityCurrent = _ship.Fuel.QuantityMax;
				_ship.RecalculateMass();
				_thrustController.MassChanged();

				//mass = _ship.PhysicsBody.Mass;

				_map.AddItem(_ship);

				grdViewPort.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnBoxBreakdown_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearCurrent();
				Random rand = StaticRandom.GetRandomForThread();

				#region Unit Tests

				//var cube1 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .3);
				//var cube2 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .065);
				//var cube3 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(1, 1, 1), .5);

				//var rect1 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .3);
				//var rect2 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .065);
				//var rect3 = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(2, 1, 1), .5);

				//for (int cntr = 0; cntr < 1000; cntr++)
				//{
				//    Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
				//    double cellSize = .1 + (rand.NextDouble() * 1.2);
				//    var rect = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);
				//}

				#endregion

				//	Visual
				Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
				double cellSize = .1 + (rand.NextDouble() * 1.2);
				var rect = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);

				DrawMassBreakdown(rect, cellSize);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCylinderBreakdown_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearCurrent();
				Random rand = StaticRandom.GetRandomForThread();



				//	Break it down
				Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
				double cellSize = .1 + (rand.NextDouble() * 1.2);

				//Vector3D size = new Vector3D(1, 1, 1);
				//double cellSize = .3d;

				//Vector3D size = new Vector3D(1, 1, 1);
				//double cellSize = .17d;

				//Vector3D size = new Vector3D(3, .2, .2);
				//double cellSize = .3d;

				var cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

				DrawMassBreakdown(cylinder, cellSize);

				//	Draw Cylinder
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCapsuleBreakdown_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearCurrent();
				Random rand = StaticRandom.GetRandomForThread();


				Vector3D size = new Vector3D(1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2), 1 + (rand.NextDouble() * 2));
				size.Z = size.Y;
				double cellSize = .1 + (rand.NextDouble() * 1.2);

				//Vector3D size = new Vector3D(3, .75, .75);
				//double cellSize = .25;


				Vector3D mainSize = new Vector3D(size.X * .75d, size.Y, size.Z);
				double mainVolume = Math.Pow((mainSize.Y + mainSize.Z) / 4d, 2d) * Math.PI * mainSize.X;
				var mainCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, mainSize, cellSize);

				Vector3D capSize = new Vector3D(size.X * .125d, size.Y * .5d, size.Z * .5d);
				double capVolume = Math.Pow((capSize.Y + capSize.Z) / 4d, 2d) * Math.PI * capSize.X;
				var capCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, capSize, cellSize);

				double offsetX = (mainSize.X * .5d) + (capSize.X * .5d);

				var objects = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>[3];
				objects[0] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(mainCylinder, new Point3D(0, 0, 0), Quaternion.Identity, mainVolume);
				objects[1] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(offsetX, 0, 0), Quaternion.Identity, capVolume);
				objects[2] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(-offsetX, 0, 0), Quaternion.Identity, capVolume);
				var combined = UtilityNewt.Combine(objects);

				DrawMassBreakdown(combined, cellSize);

				#region Draw Capsule

				//	Material
				MaterialGroup materials = new MaterialGroup();
				materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("402A4A52"))));
				materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("602C8564")), .25d));

				//	Geometry Model
				GeometryModel3D geometry = new GeometryModel3D();
				geometry.Material = materials;
				geometry.BackMaterial = materials;

				//NOTE: This capsule will become a sphere if the height isn't enough to overcome twice the radius.  But the fuel tank will start off
				//as a capsule, then the scale along height will deform the caps
				geometry.Geometry = UtilityWPF.GetCapsule_AlongZ(20, 20, size.Y * .5d, size.X);

				geometry.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));

				ModelVisual3D visual = new ModelVisual3D();
				visual.Content = geometry;
				_viewport.Children.Add(visual);
				_currentVisuals.Add(visual);

				#endregion
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		/// <remarks>
		/// Good links:
		/// http://newtondynamics.com/wiki/index.php5?title=NewtonBodySetMassMatrix
		/// 
		/// http://en.wikipedia.org/wiki/List_of_moments_of_inertia
		/// 
		/// http://en.wikipedia.org/wiki/Moment_of_inertia
		/// 
		/// 
		/// Say there are 3 solid spheres, each with a different mass and radius.  Each individual sphere's moment of inertia is 2/5*m*r^2 for each x,y,z
		/// axis, because they are perfectly symetrical.  So now that each individual sphere inertia is known, the 3 as a rigid body can use the parallel axis
		/// theorem.
		/// 
		/// ------------
		/// 
		/// However, say there are 3 solid cylinders, each with a different mass height radius, and each with a random orientation.  Each individual cylinder's
		/// moment of inertia in its local coords is (mr^2)/2 along the axis, and (3mr^2 + 3mh^2)/12 along two poles perp to the axis.
		/// 
		/// BUT the combined body can't use those simplified equations because each cylinder as an arbitrary orienation relative to the whole body's axis, so
		/// those simplifications for each individual body along the whole's axiis can't be used.  The only way is to evenly divide the parts into many uniform
		/// pieces and add up all the mr^2 of each piece (r being the distance from the axis)
		/// 
		/// In other words:
		///		Ix = sum(m* (dist from x)^2) 
		///		Iy = sum(m* (dist from y)^2) 
		///		Iz = sum(m* (dist from z)^2) 
		/// 
		/// Then use parallel axis theorem for each part because each part's center of mass is not along the whole's axiis (they may be in rare cases, but not likely)
		/// 
		/// -------------
		/// 
		/// Newt seems to want the mass separate from the vector, so I guess the vector is just all the r^2's
		/// 
		/// -------------
		/// 
		/// Rather than the ship knowing how to divide up each part, the part should have abstract methods:
		///		Point3D GetCenterMass()
		///		Vector3D GetInertia(Tuple[Vector3D,Vector3D,Vector3D] rays)		//	each of the 3 rays is through the object's center of mass.  The rays are assumed to all be perp to each other
		///		
		/// This way, if the part knows the object is a sphere, it can just return (2mr^2)/5, regardless of the vector passed in.
		/// Otherwise, it would need to break down the object however it decides to
		/// 
		/// As a side note, if those methods could be made static, and pass in a dna, then you could wrap them in a task, and spin
		/// up 3 tasks for each part (one for each axis).  Then await all the tasks. -- actually, keep the method an instance method,
		/// but make a static utility class for various solids.  Then the part can cache previous results for those rays.
		/// 
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnMassMatrix_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				EnsureWorldStarted();
				ClearCurrent();

				Body body = GetNewBody(CollisionShapeType.Box, Colors.Orange, new Vector3D(1, 1, 1), 1, new Point3D(0, 0, 0), Quaternion.Identity, _material_Ship);
				body.LinearDamping = .01f;
				body.AngularDamping = new Vector3D(.01f, .01f, .01f);

				var prevMatrix = body.MassMatrix;


				double x = 1;
				double y = 1;
				double z = 1;


				body.MassMatrix = new MassMatrix(10, new Vector3D(x, y, z));


				//	Remember them
				_currentBody = body;
				_currentVisuals.AddRange(body.Visuals);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void radFallingSand_Checked(object sender, RoutedEventArgs e)
		{
			if (!_isInitialized)
			{
				return;
			}

			try
			{
				EnsureWorldStarted();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private void ClearCurrent()
		{
			foreach (Visual3D visual in _currentVisuals)
			{
				_viewport.Children.Remove(visual);
			}
			_currentVisuals.Clear();

			if (_currentBody != null)
			{
				_currentBody.Dispose();
				_currentBody = null;
			}

			if (_ship != null)
			{
				_map.RemoveItem(_ship);
				_ship.PhysicsBody.Dispose();
				_ship = null;
			}

			if (_thrustController != null)
			{
				_thrustController.Dispose();
				_thrustController = null;
			}
		}

		private void EnsureWorldStarted()
		{
			if (_world != null)
			{
				return;
			}

			#region Init World

			//	Set the size of the world to something a bit random (gets boring when it's always the same size)
			double halfSize = 50d;
			_boundryMin = new Point3D(-halfSize, -halfSize, -halfSize);
			_boundryMax = new Point3D(halfSize, halfSize, halfSize);

			_world = new World();
			_world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

			List<Point3D[]> innerLines, outerLines;
			_world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

			//	Draw the lines
			_boundryLines = new ScreenSpaceLines3D(true);
			_boundryLines.Thickness = 1d;
			_boundryLines.Color = _colors.BoundryLines;
			_viewport.Children.Add(_boundryLines);

			foreach (Point3D[] line in innerLines)
			{
				_boundryLines.AddLine(line[0], line[1]);
			}

			#endregion
			#region Materials

			_materialManager = new MaterialManager(_world);

			//	Asteroid
			Game.Newt.NewtonDynamics.Material material = new Game.Newt.NewtonDynamics.Material();
			material.Elasticity = .25d;
			material.StaticFriction = .9d;
			material.KineticFriction = .75d;
			_material_Asteroid = _materialManager.AddMaterial(material);

			//	Ship
			material = new Game.Newt.NewtonDynamics.Material();
			material.Elasticity = .75d;
			material.StaticFriction = .5d;
			material.KineticFriction = .2d;
			_material_Ship = _materialManager.AddMaterial(material);

			//	Sand
			material = new Game.Newt.NewtonDynamics.Material();
			material.Elasticity = .5d;
			material.StaticFriction = .5d;
			material.KineticFriction = .33d;
			_material_Sand = _materialManager.AddMaterial(material);

			_materialManager.RegisterCollisionEvent(_material_Ship, _material_Asteroid, Collision_Ship);
			_materialManager.RegisterCollisionEvent(_material_Ship, _material_Sand, Collision_Sand);

			#endregion

			_map = new Map(_viewport, null, _world);
			//_map.SnapshotFequency_Milliseconds = 125;
			//_map.SnapshotMaxItemsPerNode = 10;
			_map.ShouldBuildSnapshots = false;
			//_map.ShouldShowSnapshotLines = true;
			//_map.ShouldSnapshotCentersDrift = false;

			_radiation = new RadiationField();
			_radiation.AmbientRadiation = 1d;

			_world.UnPause();
		}

		private void BuildStandalonePart(PartBase part)
		{
			EnsureWorldStarted();
			ClearCurrent();

			//	WPF
			ModelVisual3D model = new ModelVisual3D();
			model.Content = part.Model;

			_viewport.Children.Add(model);
			_currentVisuals.Add(model);

			//	Physics
			CollisionHull hull = part.CreateCollisionHull(_world);
			Body body = new Body(hull, Matrix3D.Identity, part.TotalMass, new Visual3D[] { model });
			body.MaterialGroupID = _material_Ship;
			body.LinearDamping = .01f;
			body.AngularDamping = new Vector3D(.01f, .01f, .01f);

			_currentBody = body;
		}

		private Body GetNewBody(CollisionShapeType shape, Color color, Vector3D size, double mass, Point3D position, Quaternion orientation, int materialID)
		{
			DoubleVector dirFacing = _defaultDirectionFacing.GetRotatedVector(orientation.Axis, orientation.Angle);

			//	Get the wpf model
			CollisionHull hull;
			Transform3DGroup transform;
			Quaternion rotation;
			DiffuseMaterial bodyMaterial;
			ModelVisual3D model = GetWPFModel(out hull, out transform, out rotation, out bodyMaterial, shape, color, Colors.White, 100d, size, position, dirFacing, true);

			//	Add to the viewport
			_viewport.Children.Add(model);

			//	Make a physics body that represents this shape
			Body retVal = new Body(hull, transform.Value, mass, new Visual3D[] { model });
			retVal.AutoSleep = false;		//	the falling sand was falling asleep, even though the velocity was non zero (the sand would suddenly stop)
			retVal.MaterialGroupID = materialID;

			return retVal;
		}
		private Model3D GetWPFGeometry(out CollisionHull hull, out Transform3DGroup transform, out Quaternion rotation, out DiffuseMaterial bodyMaterial, CollisionShapeType shape, Color color, Color reflectionColor, double reflectionIntensity, Vector3D size, Point3D position, DoubleVector directionFacing, bool createHull)
		{
			//	Material
			MaterialGroup materials = new MaterialGroup();
			bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
			materials.Children.Add(bodyMaterial);
			materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;

			hull = null;

			switch (shape)
			{
				case CollisionShapeType.Box:
					Vector3D halfSize = size / 2d;
					geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize.X, -halfSize.Y, -halfSize.Z), new Point3D(halfSize.X, halfSize.Y, halfSize.Z));
					if (createHull)
					{
						hull = CollisionHull.CreateBox(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Sphere:
					geometry.Geometry = UtilityWPF.GetSphere(5, size.X, size.Y, size.Z);
					if (createHull)
					{
						hull = CollisionHull.CreateSphere(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Cylinder:
					geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCylinder(_world, 0, size.X, size.Y, null);
					}
					break;

				case CollisionShapeType.Cone:
					geometry.Geometry = UtilityWPF.GetCone_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCone(_world, 0, size.X, size.Y, null);
					}
					break;

				default:
					throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
			}

			//	Transform
			transform = new Transform3DGroup();		//	rotate needs to be added before translate



			//rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);		//	can't use double vector, it over rotates (not anymore, but this is still isn't rotating correctly)

			rotation = Math3D.GetRotation(_defaultDirectionFacing.Standard, directionFacing.Standard);



			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
			transform.Children.Add(new TranslateTransform3D(position.ToVector()));

			geometry.Transform = transform;

			////	Model Visual
			//ModelVisual3D retVal = new ModelVisual3D();
			//retVal.Content = geometry;
			//retVal.Transform = transform;

			//	Exit Function
			//return retVal;
			return geometry;
		}
		private ModelVisual3D GetWPFModel(out CollisionHull hull, out Transform3DGroup transform, out Quaternion rotation, out DiffuseMaterial bodyMaterial, CollisionShapeType shape, Color color, Color reflectionColor, double reflectionIntensity, Vector3D size, Point3D position, DoubleVector directionFacing, bool createHull)
		{
			//	Material
			MaterialGroup materials = new MaterialGroup();
			bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
			materials.Children.Add(bodyMaterial);
			materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;

			hull = null;

			switch (shape)
			{
				case CollisionShapeType.Box:
					Vector3D halfSize = size / 2d;
					geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize.X, -halfSize.Y, -halfSize.Z), new Point3D(halfSize.X, halfSize.Y, halfSize.Z));
					if (createHull)
					{
						hull = CollisionHull.CreateBox(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Sphere:
					geometry.Geometry = UtilityWPF.GetSphere(5, size.X, size.Y, size.Z);
					if (createHull)
					{
						hull = CollisionHull.CreateSphere(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Cylinder:
					geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCylinder(_world, 0, size.X, size.Y, null);
					}
					break;

				case CollisionShapeType.Cone:
					geometry.Geometry = UtilityWPF.GetCone_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCone(_world, 0, size.X, size.Y, null);
					}
					break;

				default:
					throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
			}

			//	Transform
			transform = new Transform3DGroup();		//	rotate needs to be added before translate



			//rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);		//	can't use double vector, it over rotates (not anymore, but this is still isn't rotating correctly)

			rotation = Math3D.GetRotation(_defaultDirectionFacing.Standard, directionFacing.Standard);



			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
			transform.Children.Add(new TranslateTransform3D(position.ToVector()));

			//	Model Visual
			ModelVisual3D retVal = new ModelVisual3D();
			retVal.Content = geometry;
			retVal.Transform = transform;

			//	Exit Function
			return retVal;
		}

		private static PartDNA GetDefaultDNA(string partType)
		{
			PartDNA retVal = new PartDNA();

			retVal.PartType = partType;

			retVal.Position = new Point3D(0, 0, 0);
			retVal.Orientation = Quaternion.Identity;
			retVal.Scale = new Vector3D(1, 1, 1);

			return retVal;
		}
		private static void ModifyDNA(PartDNA dna, bool randSize, bool randOrientation)
		{
			if (randSize)
			{
				dna.Scale = Math3D.GetRandomVector(new Vector3D(.25, .25, .25), new Vector3D(2.5, 2.5, 2.5));
			}

			if (randOrientation)
			{
				dna.Orientation = Math3D.GetRandomRotation();
			}
		}

		private void DrawMassBreakdown(UtilityNewt.IObjectMassBreakdown breakdown, double cellSize)
		{
			#region Draw masses as cubes

			double radMult = (cellSize * .75d) / breakdown.Max(o => o.Item2);
			DoubleVector dirFacing = new DoubleVector(1, 0, 0, 0, 1, 0);

			Model3DGroup geometries = new Model3DGroup();

			foreach (var pointMass in breakdown)
			{
				double radius = pointMass.Item2 * radMult;

				CollisionHull dummy1; Transform3DGroup dummy2; Quaternion dummy3; DiffuseMaterial dummy4;
				geometries.Children.Add(GetWPFGeometry(out dummy1, out dummy2, out dummy3, out dummy4, CollisionShapeType.Box, _colors.MassBall, _colors.MassBallReflect, _colors.MassBallReflectIntensity, new Vector3D(radius, radius, radius), pointMass.Item1, dirFacing, false));
			}

			ModelVisual3D visual = new ModelVisual3D();
			visual.Content = geometries;
			_viewport.Children.Add(visual);
			_currentVisuals.Add(visual);

			#endregion
			#region Draw Axiis

			ScreenSpaceLines3D line = new ScreenSpaceLines3D();
			line.Color = Colors.DimGray;
			line.Thickness = 2d;
			line.AddLine(new Point3D(0, 0, 0), new Point3D(10, 0, 0));
			_viewport.Children.Add(line);
			_currentVisuals.Add(line);

			line = new ScreenSpaceLines3D();
			line.Color = Colors.Silver;
			line.Thickness = 2d;
			line.AddLine(new Point3D(0, 0, 0), new Point3D(0, 10, 0));
			_viewport.Children.Add(line);
			_currentVisuals.Add(line);

			line = new ScreenSpaceLines3D();
			line.Color = Colors.White;
			line.Thickness = 2d;
			line.AddLine(new Point3D(0, 0, 0), new Point3D(0, 0, 10));
			_viewport.Children.Add(line);
			_currentVisuals.Add(line);

			#endregion
		}

		private T GetRandomEnum<T>(T excluding)
		{
			return GetRandomEnum<T>(new T[] { excluding });
		}
		private T GetRandomEnum<T>(IEnumerable<T> excluding)
		{
			while (true)
			{
				T retVal = GetRandomEnum<T>();
				if (!excluding.Contains(retVal))
				{
					return retVal;
				}
			}
		}
		private T GetRandomEnum<T>()
		{
			Array allValues = Enum.GetValues(typeof(T));
			if (allValues.Length == 0)
			{
				throw new ArgumentException("This enum has no values");
			}

			return (T)allValues.GetValue(_rand.Next(allValues.Length));
		}

		#endregion
	}
}
