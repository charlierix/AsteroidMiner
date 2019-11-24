using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems
{
    public static class ThrustControlUtil
    {
        #region Declaration Section

        private const double MAXERROR = 100000000000;       // need something excessively large, but less than double.max

        #endregion

        /// <summary>
        /// This is a wrapper of UtilityAI.DiscoverSolution
        /// </summary>
        public static void DiscoverSolutionAsync(Bot bot, Vector3D? idealLinear, Vector3D? idealRotation, CancellationToken? cancel = null, ThrustContributionModel model = null, Action<ThrusterMap> newBestFound = null, Action<ThrusterMap> finalFound = null, DiscoverSolutionOptions<Tuple<int, int, double>> options = null)
        {
            long token = bot.Token;

            // Cache Thrusters
            Thruster[] allThrusters = bot.Thrusters;
            if (allThrusters == null || allThrusters.Length == 0)
            {
                throw new ArgumentException("This bot has no thrusters");
            }

            // Ensure model is created (if the caller wants to find solutions for several directions at the same time, it would be
            // more efficient to calculate the model once, and pass to all the solution finders)
            model = model ?? new ThrustContributionModel(bot.Thrusters, bot.PhysicsBody.CenterOfMass);

            // Figure out how much force can be generated in the ideal directions
            double maxForceLinear = idealLinear == null ? 0d : ThrustControlUtil.GetMaximumPossible_Linear(model, idealLinear.Value);
            double maxForceRotate = idealRotation == null ? 0d : ThrustControlUtil.GetMaximumPossible_Rotation(model, idealRotation.Value);

            // Mutate 2% of the elements, with a 10% value drift
            MutateUtility.MuateArgs mutateArgs = new MutateUtility.MuateArgs(false, .02, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .1));

            #region delegates

            //NOTE: While breeding/mutating, they don't get normalized.  But error calculation and returned maps need them normalized

            var delegates = new DiscoverSolutionDelegates<Tuple<int, int, double>>()
            {
                GetNewSample = new Func<Tuple<int, int, double>[]>(() =>
                {
                    ThrusterMap map = ThrustControlUtil.GenerateRandomMap(allThrusters, token);
                    return map.Flattened;
                }),

                GetError = new Func<Tuple<int, int, double>[], SolutionError>(o =>
                {
                    ThrusterMap map = new ThrusterMap(ThrustControlUtil.Normalize(o), allThrusters, token);
                    return ThrustControlUtil.GetThrustMapScore(map, model, idealLinear, idealRotation, maxForceLinear, maxForceRotate);
                }),

                Mutate = new Func<Tuple<int, int, double>[], Tuple<int, int, double>[]>(o =>
                {
                    ThrusterMap map = new ThrusterMap(o, allThrusters, token);
                    return ThrustControlUtil.Mutate(map, mutateArgs, false).Flattened;
                }),
            };

            if (cancel != null)
            {
                delegates.Cancel = cancel.Value;
            }

            if (newBestFound != null)
            {
                delegates.NewBestFound = new Action<SolutionResult<Tuple<int, int, double>>>(o =>
                {
                    ThrusterMap map = new ThrusterMap(ThrustControlUtil.Normalize(o.Item), allThrusters, token);
                    newBestFound(map);
                });
            }

            if (finalFound != null)
            {
                delegates.FinalFound = new Action<SolutionResult<Tuple<int, int, double>>>(o =>
                {
                    ThrusterMap map = new ThrusterMap(ThrustControlUtil.Normalize(o.Item), allThrusters, token);
                    finalFound(map);
                });
            }

            #endregion

            // Do it
            //NOTE: If options.ThreadShare is set, then there's no reason to do this async, but there's no harm either
            Task.Run(() => UtilityAI.DiscoverSolution(delegates, options));
        }
        public static void DiscoverSolutionAsync2(Bot bot, Vector3D? idealLinear, Vector3D? idealRotation, CancellationToken? cancel = null, ThrustContributionModel model = null, Action<ThrusterMap> newBestFound = null, Action<ThrusterMap> finalFound = null, Action<Tuple<ThrusterMap, double[]>[]> logGeneration = null, DiscoverSolutionOptions2<Tuple<int, int, double>> options = null)
        {
            long token = bot.Token;

            // Cache Thrusters
            Thruster[] allThrusters = bot.Thrusters;
            if (allThrusters == null || allThrusters.Length == 0)
            {
                throw new ArgumentException("This bot has no thrusters");
            }

            // Ensure model is created (if the caller wants to find solutions for several directions at the same time, it would be
            // more efficient to calculate the model once, and pass to all the solution finders)
            model = model ?? new ThrustContributionModel(bot.Thrusters, bot.PhysicsBody.CenterOfMass);

            // Figure out how much force can be generated in the ideal directions
            double maxForceLinear = idealLinear == null ? 0d : ThrustControlUtil.GetMaximumPossible_Linear(model, idealLinear.Value);
            double maxForceRotate = idealRotation == null ? 0d : ThrustControlUtil.GetMaximumPossible_Rotation(model, idealRotation.Value);

            // Mutate 2% of the elements, with a 10% value drift
            MutateUtility.MuateArgs mutateArgs = new MutateUtility.MuateArgs(false, .02, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .1));

            #region delegates

            //NOTE: While breeding/mutating, they don't get normalized.  But error calculation and returned maps need them normalized

            var delegates = new DiscoverSolutionDelegates2<Tuple<int, int, double>>()
            {
                GetNewSample = new Func<Tuple<int, int, double>[]>(() =>
                {
                    ThrusterMap map = ThrustControlUtil.GenerateRandomMap(allThrusters, token);
                    return map.Flattened;
                }),

                GetScore = new Func<Tuple<int, int, double>[], double[]>(o =>
                {
                    ThrusterMap map = new ThrusterMap(ThrustControlUtil.Normalize(o), allThrusters, token);
                    return ThrustControlUtil.GetThrustMapScore3(map, model, idealLinear, idealRotation, maxForceLinear, maxForceRotate);
                }),

                Mutate = new Func<Tuple<int, int, double>[], Tuple<int, int, double>[]>(o =>
                {
                    ThrusterMap map = new ThrusterMap(o, allThrusters, token);
                    return ThrustControlUtil.Mutate(map, mutateArgs, false).Flattened;
                }),
            };

            if (cancel != null)
            {
                delegates.Cancel = cancel.Value;
            }

            if (newBestFound != null)
            {
                delegates.NewBestFound = new Action<SolutionResult2<Tuple<int, int, double>>>(o =>
                {
                    ThrusterMap map = new ThrusterMap(ThrustControlUtil.Normalize(o.Item), allThrusters, token);
                    newBestFound(map);
                });
            }

            if (finalFound != null)
            {
                delegates.FinalFound = new Action<SolutionResult2<Tuple<int, int, double>>>(o =>
                {
                    ThrusterMap map = new ThrusterMap(ThrustControlUtil.Normalize(o.Item), allThrusters, token);
                    finalFound(map);
                });
            }

            if (logGeneration != null)
            {
                delegates.LogGeneration = new Action<Tuple<Tuple<int, int, double>[], double[]>[]>(o =>
                {
                    Tuple<ThrusterMap, double[]>[] generation = o.
                        Select(p => Tuple.Create(new ThrusterMap(ThrustControlUtil.Normalize(p.Item1), allThrusters, token), p.Item2)).
                        ToArray();

                    logGeneration(generation);
                });
            }

            #endregion

            options = options ?? new DiscoverSolutionOptions2<Tuple<int, int, double>>();
            options.ScoreAscendDescend = new bool[] { false, false };

            // Do it
            //NOTE: If options.ThreadShare is set, then there's no reason to do this async, but there's no harm either
            Task.Run(() => UtilityAI.DiscoverSolution2(delegates, options));
        }

        //TODO: Come up with some overloads that have various constraints (only thrusters that contribute to a direction, x percent of the thrusters, etc)
        public static ThrusterMap GenerateRandomMap(Thruster[] allThrusters, long botToken)
        {
            Random rand = StaticRandom.GetRandomForThread();

            var flattened = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < allThrusters.Length; outer++)
            {
                for (int inner = 0; inner < allThrusters[outer].ThrusterDirectionsModel.Length; inner++)
                {
                    flattened.Add(Tuple.Create(outer, inner, rand.NextDouble()));
                }
            }

            return new ThrusterMap(Normalize(flattened.ToArray()), allThrusters, botToken);
        }

        public static ThrusterMap Mutate(ThrusterMap map, bool shouldNormalize = true)
        {
            // Modify 2% of the properties
            // Each property should drift up to 10% each direction (the values get capped between 0 and 100%)
            MutateUtility.MuateArgs args = new MutateUtility.MuateArgs(false, .02, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .1));

            return Mutate(map, args);
        }
        public static ThrusterMap Mutate(ThrusterMap map, MutateUtility.MuateArgs args, bool shouldNormalize = true)
        {
            var flattened = map.Flattened.ToArray();        // ensure it's a clone

            int changeCount = args.GetChangeCount(flattened.Length);
            if (changeCount == 0 && flattened.Length > 0)
            {
                changeCount = 1;
            }

            foreach (int index in UtilityCore.RandomRange(0, flattened.Length, changeCount))
            {
                // Mutate the value
                double newValue = MutateUtility.Mutate(flattened[index].Item3, args.DefaultFactor);

                // Cap it
                if (newValue < 0)
                {
                    newValue = 0;
                }
                else if (newValue > 1)
                {
                    newValue = 1;
                }

                // Store the mutated value
                flattened[index] = Tuple.Create(flattened[index].Item1, flattened[index].Item2, newValue);
            }

            if (shouldNormalize)
            {
                flattened = Normalize(flattened);
            }

            return new ThrusterMap(flattened, map.AllThrusters, map.BotToken);
        }

        /// <summary>
        /// This scales all thrusters so that they fire as strong as possible
        /// </summary>
        /// <remarks>
        /// When generating a random mapping, the strongest thruster may only be firing at 50%.  This method would
        /// make that thruster fire at 100% (and scale all other thrusters to compensate)
        /// </remarks>
        public static ThrusterMap Normalize(ThrusterMap map)
        {
            return new ThrusterMap(Normalize(map.Flattened), map.AllThrusters, map.BotToken);
        }
        public static Tuple<int, int, double>[] Normalize(Tuple<int, int, double>[] flattened)
        {
            if (flattened.Length == 0)
            {
                return flattened;
            }

            double maxPercent = flattened.Max(o => o.Item3);
            if (maxPercent.IsNearZero() || maxPercent.IsNearValue(1d))       // if it's zero, then no thrusters are firing.  If one, it's already normalized
            {
                return flattened;
            }

            double scale = 1d / maxPercent;

            return flattened.
                Select(o => Tuple.Create(o.Item1, o.Item2, o.Item3 * scale)).
                ToArray();
        }

        public static SolutionError GetThrustMapScore(ThrusterMap map, ThrustContributionModel model, Vector3D? linear, Vector3D? rotation, double maxPossibleLinear = 0, double maxPossibleRotate = 0)
        {
            if (linear == null && rotation == null)
            {
                throw new ApplicationException("Objective linear and rotate can't both be null");
            }

            Tuple<Vector3D, Vector3D> forces = ApplyThrust(map, model);

            double[] error = new double[3];

            if (forces.Item1.LengthSquared.IsNearZero() && forces.Item2.LengthSquared.IsNearZero())
            {
                // When there is zero contribution, give a large error.  Otherwise the winning strategy is not to play :)
                error[0] = MAXERROR;        // Balance
                error[1] = MAXERROR;        // UnderPowered
            }
            else
            {
                // Balance
                double linearScore = GetScore_Balance(linear, forces.Item1);
                double rotateScore = GetScore_Balance(rotation, forces.Item2);
                error[0] = linearScore + rotateScore;

                // UnderPowered
                linearScore = GetScore_UnderPowered(linear, forces.Item1, maxPossibleLinear);
                rotateScore = GetScore_UnderPowered(rotation, forces.Item2, maxPossibleRotate);
                error[1] = linearScore + rotateScore;
            }

            // Inneficient
            error[2] = GetScore_Inefficient(map, model);

            // Total
            // It's important to include the maximize thrust, but just a tiny bit.  Without it, the ship will be happy to
            // fire thrusters in all directions.  But if maximize thrust is any stronger, the solution will have too much
            // unbalance
            double total = error[0] + (error[1] * .01) + (error[2] * .1);

            return new SolutionError(error, total);
        }
        public static double[] GetThrustMapScore3(ThrusterMap map, ThrustContributionModel model, Vector3D? linear, Vector3D? rotation, double maxPossibleLinear = 0, double maxPossibleRotate = 0)
        {
            if (linear == null && rotation == null)
            {
                throw new ApplicationException("Objective linear and rotate can't both be null");
            }

            Tuple<Vector3D, Vector3D> forces = ApplyThrust(map, model);

            double[] retVal = new double[2];

            if (forces.Item1.LengthSquared.IsNearZero() && forces.Item2.LengthSquared.IsNearZero())
            {
                // When there is zero contribution, give a large error.  Otherwise the winning strategy is not to play :)
                retVal[0] = MAXERROR;        // Balance
                retVal[1] = MAXERROR;        // Inefficient
            }
            else
            {
                // Balance
                double linearScore = GetScore_Balance(linear, forces.Item1);
                double rotateScore = GetScore_Balance(rotation, forces.Item2);
                retVal[0] = linearScore + rotateScore;

                // Inefficient
                linearScore = linear == null ? 0 : GetScore_Inefficient4(map, model, linear.Value, true);
                rotateScore = rotation == null ? 0 : GetScore_Inefficient4(map, model, rotation.Value, false);
                retVal[1] = linearScore + rotateScore;
            }

            return retVal;
        }

        /// <summary>
        /// This looks that which thrusters can contribute, then adds up the sum of their contributions when firing
        /// at 100%
        /// </summary>
        /// <remarks>
        /// This is useful for calculating error.  Without knowing an upper performance limit, any thruster combos that balance
        /// would be considered ideal, even if they just cancel each other out, and there is almost no net force
        /// </remarks>
        public static double GetMaximumPossible_Linear(ThrustContributionModel model, Vector3D direction)
        {
            direction = direction.ToUnit();     // doing this so the dot product can be used as a percent

            double retVal = 0d;

            foreach (var contribution in model.Contributions)
            {
                retVal += contribution.Item3.TranslationForce.GetProjectedVector(direction, false).Length;
            }

            return retVal;
        }
        public static double GetMaximumPossible_Rotation(ThrustContributionModel model, Vector3D direction)
        {
            direction = direction.ToUnit();     // doing this so the dot product can be used as a percent

            double retVal = 0d;

            foreach (var contribution in model.Contributions)
            {
                retVal += contribution.Item3.Torque.GetProjectedVector(direction, false).Length;
            }

            return retVal;
        }

        public static Tuple<Vector3D, Vector3D> ApplyThrust(ThrusterMap map, ThrustContributionModel model)
        {
            if (map.Flattened.Length != model.Contributions.Length)
            {
                throw new ApplicationException("TODO: Handle mismatched map and model");
            }

            var combined = Enumerable.Range(0, map.Flattened.Length).
                Select(o =>
                {
                    if (map.Flattened[o].Item1 != model.Contributions[o].Item1 || map.Flattened[o].Item2 != model.Contributions[o].Item2)
                    {
                        throw new ApplicationException("TODO: Handle mismatched map and model");
                    }

                    return new
                    {
                        Index = map.Flattened[o].Item1,
                        SubIndex = map.Flattened[o].Item2,
                        Percent = map.Flattened[o].Item3,
                        Contribution = model.Contributions[o].Item3,
                    };
                }).
                ToArray();

            Vector3D linear = Math3D.GetSum(combined.Select(o => Tuple.Create(o.Contribution.TranslationForce, o.Percent)).ToArray());
            Vector3D rotation = Math3D.GetSum(combined.Select(o => Tuple.Create(o.Contribution.Torque, o.Percent)).ToArray());

            return Tuple.Create(linear, rotation);
        }

        #region Private Methods

        // These return error as a square (so the numbers can get big quick)
        private static double GetScore_Balance(Vector3D? objective, Vector3D actual)
        {
            if (objective == null || objective.Value.LengthSquared.IsNearZero())
            {
                // When null, the objective is zero.  So any length is an error
                return actual.LengthSquared;
            }

            double dot = Vector3D.DotProduct(objective.Value.ToUnit(), actual.ToUnit());

            // Ideal dot is 1, so 1-1 is 0.
            // 1 - 0 is 1
            // 1 - -1 is 2
            // So divide by 2 to get difference in a range of 0 to 1
            double difference = (1d - dot) / 2d;

            // Now that difference is scaled 0 to 1, use the length squared as max error
            double dotScale = actual.LengthSquared * difference;

            return dotScale;
        }

        //TODO: These two are aspects of the same thing.  Make a single method for this
        private static double GetScore_UnderPowered(Vector3D? objective, Vector3D actual, double maxPossible)
        {
            if (objective == null || objective.Value.LengthSquared.IsNearZero())
            {
                // The underpowered check doesn't care about this condition
                return 0;
            }

            double lenSqr = actual.LengthSquared;
            double maxSqr = maxPossible * maxPossible;

            double dot = Vector3D.DotProduct(objective.Value.ToUnit(), actual.ToUnit());

            // This makes sure it's actually pushing the bot, and not just balancing forces
            double underPower = 0;
            if (lenSqr * dot < maxSqr)
            {
                if (dot > 0)
                {
                    underPower = maxSqr - (lenSqr * dot);
                }
                else
                {
                    underPower = maxSqr;
                }
            }

            return underPower;
        }
        private static double GetScore_Inefficient(ThrusterMap map, ThrustContributionModel model)
        {
            // Only focus on linear balance.  Torque balance is desired

            //TODO: This first version only looks a single thrusters.  Make a future version that looks a more combinations

            double retVal = 0;

            var byThruster = model.Contributions.
                ToLookup(o => o.Item1).
                Where(o => o.Count() > 1);

            foreach (var thruster in byThruster)
            {
                var arr = thruster.ToArray();

                foreach (var pair in UtilityCore.GetPairs(arr.Length))
                {
                    double dot = Vector3D.DotProduct(arr[pair.Item1].Item3.TranslationForceUnit, arr[pair.Item2].Item3.TranslationForceUnit);

                    if (dot < -.95)
                    {
                        // They are directly opposing each other.  Figure out the force they are firing, based on the thruster percents
                        double force1 = arr[pair.Item1].Item3.TranslationForceLength * map.Flattened.First(o => o.Item1 == arr[pair.Item1].Item1 && o.Item2 == arr[pair.Item1].Item2).Item3;
                        double force2 = arr[pair.Item2].Item3.TranslationForceLength * map.Flattened.First(o => o.Item1 == arr[pair.Item2].Item1 && o.Item2 == arr[pair.Item2].Item2).Item3;

                        // The min represents waste (because if the weaker thruster were turned off, the net force won't change)
                        double min = Math.Min(force1, force2);

                        // Square it
                        retVal += min * min;
                    }
                }
            }

            return retVal;
        }

        private static double GetScore_Inefficient2(ThrusterMap map, ThrustContributionModel model, double maxPossibleLinear, double maxPossibleRotate)
        {
            if (map.Flattened.Length != model.Contributions.Length)
            {
                throw new ApplicationException("TODO: Handle mismatched map and model");
            }

            // Add up all of the thruster forces.  Don't worry about direction, just get the sum of the magnitude
            var forces = Enumerable.Range(0, map.Flattened.Length).
                Select(o =>
                {
                    if (map.Flattened[o].Item1 != model.Contributions[o].Item1 || map.Flattened[o].Item2 != model.Contributions[o].Item2)
                    {
                        throw new ApplicationException("TODO: Handle mismatched map and model");
                    }

                    return new
                    {
                        Linear = model.Contributions[o].Item3.TranslationForceLength * map.Flattened[o].Item3,
                        Rotate = model.Contributions[o].Item3.TorqueLength * map.Flattened[o].Item3,
                    };
                }).
                ToArray();

            double linear = forces.Sum(o => o.Linear);
            double rotate = forces.Sum(o => o.Rotate);

            // Subtract max possible
            linear -= maxPossibleLinear;
            rotate -= maxPossibleRotate;

            //linear = Math.Max(linear, 0);       // don't let scores go negative
            //rotate = Math.Max(rotate, 0);
            linear = Math.Abs(linear);       // negative scores are also bad
            rotate = Math.Abs(rotate);

            // Return remainder squared
            return (linear * linear) + (rotate * rotate);
        }

        private static double GetScore_Inefficient3(ThrusterMap map, ThrustContributionModel model)
        {
            if (map.Flattened.Length != model.Contributions.Length)
            {
                throw new ApplicationException("TODO: Handle mismatched map and model");
            }

            // Add up all the thruster forces, regardless of direction

            double retVal = Enumerable.Range(0, map.Flattened.Length).
                Select(o =>
                {
                    if (map.Flattened[o].Item1 != model.Contributions[o].Item1 || map.Flattened[o].Item2 != model.Contributions[o].Item2)
                    {
                        throw new ApplicationException("TODO: Handle mismatched map and model");
                    }

                    // The translation force is the same as the thrust produced by the thruster.  Don't want to include
                    // torque, because this method only cares about how much fuel is being used, and adding torque
                    // is an innacurate complication
                    return model.Contributions[o].Item3.TranslationForceLength * map.Flattened[o].Item3;
                }).
                Sum();

            return retVal;
        }

        // This should be called twice, once for linear and once for rotation.  It should add the force length if dot is close to zero.  May also want a smaller length if dot is close to -1
        private static double GetScore_Inefficient4(ThrusterMap map, ThrustContributionModel model, Vector3D objectiveUnit, bool isLinear)
        {
            const double DOT_MAX = .3;      // when dot is >= this, there is no error
            const double PERCENT_NEG = .5;      // when dot is -1, this is the returned percent error

            if (map.Flattened.Length != model.Contributions.Length)
            {
                throw new ApplicationException("TODO: Handle mismatched map and model");
            }

            double retVal = Enumerable.Range(0, map.Flattened.Length).
                Select(o =>
                {
                    if (map.Flattened[o].Item1 != model.Contributions[o].Item1 || map.Flattened[o].Item2 != model.Contributions[o].Item2)
                    {
                        throw new ApplicationException("TODO: Handle mismatched map and model");
                    }

                    // Get the actual vector
                    Vector3D actualVect;
                    double actualVal;
                    if (isLinear)
                    {
                        actualVect = model.Contributions[o].Item3.TranslationForceUnit;
                        actualVal = model.Contributions[o].Item3.TranslationForceLength;
                    }
                    else
                    {
                        actualVect = model.Contributions[o].Item3.TorqueUnit;
                        actualVal = model.Contributions[o].Item3.TorqueLength;
                    }

                    // Use dot to see how in line this actual vector is with the ideal
                    double dot = Vector3D.DotProduct(objectiveUnit, actualVect);

                    // Convert dot into a percent
                    //      - If dot is near zero, error should be maximum, because this doesn't contribute to ideal
                    //      - Can't penalize positive and negative dots equally, or it will try to find a minimum fuel useage
                    //        that is still balanced, instead of maximum thrust that is balanced (the goal is maximum thrust
                    //        without venting fuel out the sides)
                    double percent;
                    if (dot > 0)
                    {
                        if (dot > DOT_MAX)
                        {
                            percent = 0;
                        }
                        else
                        {
                            percent = UtilityCore.GetScaledValue(1, 0, 0, DOT_MAX, dot);
                        }
                    }
                    else
                    {
                        percent = UtilityCore.GetScaledValue(1, PERCENT_NEG, 0, 1, -dot);
                    }

                    return actualVal * map.Flattened[o].Item3 * percent;
                }).
                Sum();

            return retVal;
        }

        #endregion
    }

    #region class: ThrustContributionModel

    public class ThrustContributionModel
    {
        //TODO: Take in the mass matrix - this isn't necessary, but helps with fine tuning
        public ThrustContributionModel(Thruster[] thrusters, Point3D centerOfMass)
        {
            this.IsDestroyed = thrusters.       // caching this in case the property changes mid calculations
                Select(o => o.IsDestroyed).
                ToArray();

            this.CenterOfMass = centerOfMass;

            this.Contributions = GetThrusterContributions(thrusters, this.IsDestroyed, centerOfMass);
        }

        public readonly Tuple<int, int, ThrustContribution>[] Contributions;

        public readonly bool[] IsDestroyed;

        /// <summary>
        /// This is in bot's model coords
        /// </summary>
        public readonly Point3D CenterOfMass;

        #region Private Methods

        private static Tuple<int, int, ThrustContribution>[] GetThrusterContributions(Thruster[] thrusters, bool[] isDestroyed, Point3D centerOfMass)
        {
            //This method is copied from ShipPartTesterWindow (and ShipPlayer)

            var retVal = new List<Tuple<int, int, ThrustContribution>>();

            for (int outer = 0; outer < thrusters.Length; outer++)
            {
                if (isDestroyed[outer])
                {
                    // This thruster is destroyed, so it has no contribution (leaving it in the list of contributions so that thruster maps stay the same size as
                    // thrusters get destroyed and repaired
                    retVal.AddRange(
                        Enumerable.Range(0, thrusters[outer].ThrusterDirectionsShip.Length).
                            Select(o => Tuple.Create(outer, o, new ThrustContribution(thrusters[outer], o, true, new Vector3D(0, 0, 0), new Vector3D(0, 0, 0))))
                        );
                    continue;
                }

                for (int inner = 0; inner < thrusters[outer].ThrusterDirectionsShip.Length; inner++)
                {
                    // This is copied from Body.AddForceAtPoint

                    Vector3D offsetFromMass = thrusters[outer].Position - centerOfMass;		// this is ship's local coords
                    Vector3D force = thrusters[outer].ThrusterDirectionsShip[inner] * thrusters[outer].ForceAtMax;

                    Vector3D translationForce, torque;
                    Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out torque, offsetFromMass, force);

                    retVal.Add(Tuple.Create(outer, inner, new ThrustContribution(thrusters[outer], inner, false, translationForce, torque)));
                }
            }

            return retVal.ToArray();
        }

        #endregion
    }

    #endregion
    #region class: ThrustContribution

    public class ThrustContribution
    {
        public ThrustContribution(Thruster thruster, int index, bool isDestroyed, Vector3D translationForce, Vector3D torque)
        {
            this.Thruster = thruster;
            this.Index = index;

            this.IsDestroyed = isDestroyed;

            this.TranslationForceLength = translationForce.Length;
            this.TranslationForce = translationForce;
            if (this.TranslationForceLength.IsNearZero())
            {
                this.TranslationForceUnit = new Vector3D(0, 0, 0);      // this can happen with destroyed thrusters
            }
            else
            {
                this.TranslationForceUnit = new Vector3D(translationForce.X / this.TranslationForceLength, translationForce.Y / this.TranslationForceLength, translationForce.Z / this.TranslationForceLength);
            }

            this.TorqueLength = torque.Length;
            this.Torque = torque;
            if (this.TorqueLength.IsNearZero())
            {
                this.TorqueUnit = new Vector3D(0, 0, 0);
            }
            else
            {
                this.TorqueUnit = new Vector3D(torque.X / this.TorqueLength, torque.Y / this.TorqueLength, torque.Z / this.TorqueLength);
            }
        }

        public readonly Thruster Thruster;
        /// <summary>
        /// This is the sub index
        /// </summary>
        public readonly int Index;

        public readonly bool IsDestroyed;

        public readonly Vector3D TranslationForce;
        public readonly Vector3D TranslationForceUnit;
        public readonly double TranslationForceLength;

        public readonly Vector3D Torque;
        public readonly Vector3D TorqueUnit;
        public readonly double TorqueLength;
    }

    #endregion

    #region class: ThrusterMap

    public class ThrusterMap
    {
        public ThrusterMap(Tuple<int, int, double>[] flattened, Thruster[] allThrusters, long botToken)
        {
            #region build used

            ThrusterSetting[] used = flattened.
                Where(o => !o.Item3.IsNearZero()).
                Select(o => new ThrusterSetting(allThrusters[o.Item1], o.Item1, o.Item2, o.Item3)).
                ToArray();

            #endregion

            this.UsedThrusters = used.ToArray();
            this.Flattened = flattened;
            this.AllThrusters = allThrusters;
            this.BotToken = botToken;
        }
        public ThrusterMap(IEnumerable<ThrusterSetting> thrusterUsage, Thruster[] allThrusters, long botToken)
        {
            #region build flattened

            var flattened = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < allThrusters.Length; outer++)
            {
                ThrusterSetting[] matches = thrusterUsage.
                    Where(o => o.ThrusterIndex == outer).
                    ToArray();

                for (int inner = 0; inner < allThrusters[outer].ThrusterDirectionsModel.Length; inner++)
                {
                    ThrusterSetting match = matches.FirstOrDefault(o => o.SubIndex == inner);
                    double percent = match == null ? 0d : match.Percent;
                    flattened.Add(Tuple.Create(outer, inner, percent));
                }
            }

            #endregion

            this.UsedThrusters = thrusterUsage.ToArray();
            this.Flattened = flattened.ToArray();
            this.AllThrusters = allThrusters;
            this.BotToken = botToken;
        }

        /// <summary>
        /// This won't contain nulls.  Items might also be rearranged
        /// </summary>
        public readonly ThrusterSetting[] UsedThrusters;

        /// <summary>
        /// This contains one entry for each thruster, subthruster (in order)
        /// </summary>
        /// <remarks>
        /// This is for algorithms that want fixed length arrays (genetic algorithms)
        /// </remarks>
        public readonly Tuple<int, int, double>[] Flattened;

        public readonly Thruster[] AllThrusters;

        public readonly long BotToken;
    }

    #endregion
    #region class: ThrusterSetting

    public class ThrusterSetting
    {
        public ThrusterSetting(Thruster thruster, int thrusterIndex, int subIndex, double percent)
        {
            this.Thruster = thruster;
            this.ThrusterIndex = thrusterIndex;
            this.SubIndex = subIndex;
            this.Percent = percent;
        }

        public readonly Thruster Thruster;
        public readonly int ThrusterIndex;
        public readonly int SubIndex;
        public readonly double Percent;
    }

    #endregion

    #region class: ThrusterSolutionMap

    /// <summary>
    /// This holds a map and the resulting acceleration
    /// </summary>
    public class ThrusterSolutionMap
    {
        public ThrusterSolutionMap(ThrusterMap map, double linearAccel, double rotateAccel)
        {
            this.Map = map;
            this.LinearAccel = linearAccel;
            this.RotateAccel = rotateAccel;
        }

        public readonly ThrusterMap Map;

        public readonly double LinearAccel;
        public readonly double RotateAccel;
    }

    #endregion

    #region class: ThrustSolutionSolver

    /// <summary>
    /// This still needs quite a bit of work
    /// </summary>
    /// <remarks>
    /// It is given a bunch of linear+rotation requests to solve, which it solves using crossover+mutation -- this portion is working pretty well
    /// 
    /// The problem is when a linear+rotation is wanted that wasn't explicitly trained for.  I'm planning to combine solutions that surround
    /// the desired request.  Figuring out which and how much is almost as complex as the original problem
    /// 
    /// My first attempt is to take dot products, then choose the closest ones.  But that is flawed, because dot product is a scalar, and could
    /// choose solution requests that are on the wrong side:
    ///     1  2  |               3
    /// In the above example, some combination of 2 and 3 should be used, 1 should be thrown out - even though 1 is closer than 3
    /// 
    /// -----------------------
    /// 
    /// A different approach could be to train a single neural net.  NNs are a continuous transform from request space to solution space, so there
    /// wouldn't be any of this focus on specific independent solutions
    /// 
    /// Maybe use the crossover+mutation with specific vectors because of its speed of finding a solution, then use those solutions as training
    /// data for the final neural net
    /// 
    /// -----------------------
    /// 
    /// After thinking about this more, a neural net is overkill.  There are three types of request:
    ///     Linear
    ///     Rotation
    ///     Linear+Rotation
    ///     
    /// Each of those three is a bunch of unit vectors pointing in all directions.  So create 3 convex hulls (one for each scenario).  Then any
    /// arbitrary request will intersect one face of the hull, telling which vertices (solutions) to linearly interpolate
    /// 
    /// Note that Linear+Rotation would be 6D, so the convex hull of that wouldn't be made of triangles, but 5D objects - unless there's some
    /// way to simplify to 3D?
    /// </remarks>
    public class ThrustSolutionSolver
    {
        #region class: ThrustRequest

        private class ThrustRequest
        {
            public Vector3D? Linear { get; set; }
            public Vector3D? Rotate { get; set; }
        }

        #endregion
        #region class: ThrusterSolution

        private class ThrusterSolution
        {
            public ThrusterSolution(ThrustRequest request, ThrustContributionModel model, MassMatrix inertia, double mass)
            {
                this.Request = request;

                this.Model = model;
                this.Inertia = inertia;
                this.Mass = mass;
            }

            public readonly ThrustRequest Request;

            // These are used to figure out what percent of the map to use
            public readonly ThrustContributionModel Model;
            public readonly MassMatrix Inertia;
            public readonly double Mass;

            /// <summary>
            /// This holds a map of which thrusters to fire at what percent in order to go the requested direction
            /// </summary>
            public volatile ThrusterSolutionMap Map = null;
        }

        #endregion
        #region class: CurrentModel

        private class CurrentModel
        {
            public CurrentModel(CancellationTokenSource cancelCurrentBalancer, ThrusterSolution[] previousSolutions, ThrustContributionModel model, MassMatrix inertia, double mass)
            {
                this.CancelCurrentBalancer = cancelCurrentBalancer;
                this.PreviousSolutions = previousSolutions;
                this.Model = model;
                this.Inertia = inertia;
                this.Mass = mass;
            }

            public readonly CancellationTokenSource CancelCurrentBalancer;

            public readonly ThrusterSolution[] PreviousSolutions;

            public readonly ThrustContributionModel Model;
            public readonly MassMatrix Inertia;
            public readonly double Mass;
        }

        #endregion

        #region Declaration Section

        private readonly Bot _bot;
        private readonly Thruster[] _thrusters;

        private readonly RoundRobinManager _thrustWorkerThread;

        private readonly List<ThrusterSolution> _solutions = new List<ThrusterSolution>();

        private bool _isDirty = true;

        private CurrentModel _currentModel = null;

        private readonly DispatcherTimer _visualizationTimer;
        private Debug3DWindow _window = null;       // this can only be messed with from within the dispatch tick, because that's on the UI thread

        /// <summary>
        /// This is just a quick bool for testing
        /// </summary>
        /// <remarks>
        ///TODO: Pass in constraints:
        ///  2D plane
        ///  linear only
        ///  rotation only
        ///  only solve within cone
        ///  desired resolution (tells how many independent vectors to find solutions for)
        /// </remarks>
        private readonly bool _is2D;

        #endregion

        #region Constructor

        public ThrustSolutionSolver(Bot bot, Thruster[] thrusters, RoundRobinManager thrustWorkerThread, bool is2D)
        {
            _bot = bot;
            _thrusters = thrusters;     // don't use bot.Thrusters, because this class might only be working with a subset
            _thrustWorkerThread = thrustWorkerThread;

            _is2D = is2D;

            _bot.MassChanged += Bot_MassChanged;

            foreach (Thruster thruster in _thrusters)
            {
                thruster.Destroyed += Thruster_Destroyed;
                thruster.Resurrected += Thruster_Resurrected;
            }

            //_visualizationTimer = new DispatcherTimer(DispatcherPriority.Normal, Application.Current.Dispatcher);
            //_visualizationTimer.Interval = TimeSpan.FromSeconds(1);
            //_visualizationTimer.Tick += VisualizationTimer_Tick;
            //_visualizationTimer.IsEnabled = true;

            EnsureSolverIsCurrent();
        }

        #endregion

        #region Public Methods

        //TODO: This should return null if there's not a close enough match?
        public ThrusterSetting[] FindSolution(Vector3D? linear, Vector3D? rotation)
        {

            //GetSolutionLERP_Top4(linear, rotation, _solutions);

            GetSolutionLERP_PercentOfTop2(linear, rotation, _solutions);




            return new ThrusterSetting[0];
        }

        #endregion

        #region Event Listeners

        private void Bot_MassChanged(object sender, EventArgs e)
        {
            _isDirty = true;
        }

        private void Thruster_Resurrected(object sender, EventArgs e)
        {
            _isDirty = true;
        }
        private void Thruster_Destroyed(object sender, EventArgs e)
        {
            _isDirty = true;
        }

        private void VisualizationTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                const double AXISLEN = 1.3;
                const double LINETHICK = .01;
                const double DOTRAD = .05;

                #region init

                if (_bot == null || _bot.IsDisposed)
                {
                    _visualizationTimer.IsEnabled = false;
                    return;
                }

                if (_window == null)
                {
                    _window = new Debug3DWindow()
                    {
                        Title = "ThrustSolutionSolver",
                    };

                    _window.Show();
                }

                _window.Visuals3D.Clear();
                _window.Messages_Top.Clear();
                _window.Messages_Bottom.Clear();

                _window.AddAxisLines(AXISLEN, LINETHICK / 2);

                #endregion

                #region snapshots

                double count = _solutions.Count;
                var solutionSnapshot = _solutions.
                    Select((o, i) =>
                    {
                        var map = o.Map;        // this is volatile, so take a snapshot

                        double maxForceLinear = o.Request.Linear == null ? 0d : ThrustControlUtil.GetMaximumPossible_Linear(o.Model, o.Request.Linear.Value);
                        double maxForceRotate = o.Request.Rotate == null ? 0d : ThrustControlUtil.GetMaximumPossible_Rotation(o.Model, o.Request.Rotate.Value);

                        double hue = (i.ToDouble() / count) * 360d;

                        double[] score = null;
                        if (map != null)
                        {
                            score = ThrustControlUtil.GetThrustMapScore3(ThrustControlUtil.Normalize(map.Map), o.Model, o.Request.Linear, o.Request.Rotate, maxForceLinear, maxForceRotate);
                        }

                        return new
                        {
                            o = o,
                            Score = score,
                            Map = map,
                            Color = new ColorHSV(hue, 75, 75).ToRGB(),
                        };
                    }).
                    ToArray();

                if (solutionSnapshot.Length == 0)
                {
                    return;
                }

                if (solutionSnapshot.All(o => o.Score == null))
                {
                    return;
                }

                // Min/Max Scores
                var minMax = Enumerable.Range(0, solutionSnapshot[0].Score.Length).
                    Select(o => new
                    {
                        Min = solutionSnapshot.Where(p => p.Score != null).Min(p => p.Score[o]),
                        Max = solutionSnapshot.Where(p => p.Score != null).Max(p => p.Score[o]),
                    }).
                    ToArray();

                #endregion

                foreach (var solution in solutionSnapshot)
                {
                    Vector3D scorePosition = new Vector3D(0, 0, 0);

                    #region linear/rotation lines

                    if (solution.o.Request.Linear != null)
                    {
                        _window.AddLine(new Point3D(0, 0, 0), solution.o.Request.Linear.Value.ToPoint(), LINETHICK, solution.Color);
                        scorePosition += solution.o.Request.Linear.Value;
                    }

                    if (solution.o.Request.Rotate != null)
                    {
                        _window.AddLine(new Point3D(0, 0, 0), solution.o.Request.Rotate.Value.ToPoint(), LINETHICK, solution.Color);
                        scorePosition += solution.o.Request.Rotate.Value;
                    }

                    scorePosition = scorePosition.ToUnit() * 1.5;

                    #endregion

                    if (solution.Score != null)
                    {
                        #region % score dots

                        for (int cntr = 0; cntr < minMax.Length; cntr++)
                        {
                            double percentLength = UtilityCore.GetScaledValue(.66, .85, 0, minMax.Length - 1, cntr);
                            double percent = UtilityCore.GetScaledValue(0, 1, minMax[cntr].Min, minMax[cntr].Max, solution.Score[cntr]);

                            Color percentColor;
                            if (percent < .5)
                            {
                                percentColor = UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("B19719"), UtilityWPF.ColorFromHex("16B13D"), percent * 2);
                            }
                            else
                            {
                                percentColor = UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("B12B1D"), UtilityWPF.ColorFromHex("B19719"), (percent - .5) * 2);
                            }

                            _window.AddDot((scorePosition * percentLength).ToPoint(), DOTRAD, percentColor);
                        }

                        #endregion

                        #region score text

                        string scoreText = solution.Score.
                            Select(o => o.ToStringSignificantDigits(2)).
                            ToJoin(" | ");

                        _window.AddText3D(scoreText, scorePosition.ToPoint(), scorePosition, .12, solution.Color, false, new Vector3D(1, 0, 0));

                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ThrustSolutionSolver", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void EnsureSolverIsCurrent()
        {
            if (_isDirty)
            {
                ResetSolver();
                _isDirty = false;
            }
        }

        /// <summary>
        /// Call this if the bot's mass has changed, or thrusters get destroyed/repaired
        /// </summary>
        private void ResetSolver()
        {
            if (_thrusters == null || _thrusters.Length == 0 || _bot?.PhysicsBody == null)
            {
                return;
            }

            CurrentModel currentModel = _currentModel;
            _currentModel = null;

            if (currentModel?.CancelCurrentBalancer != null)
            {
                currentModel.CancelCurrentBalancer.Cancel();
            }

            // Remember the current solutions, so they can help get a good start on the new solver
            var previous = _solutions.ToArray();
            _solutions.Clear();

            MassMatrix inertia = _bot.PhysicsBody.MassMatrix;
            _currentModel = new CurrentModel(
                new CancellationTokenSource(),
                previous,
                new ThrustContributionModel(_thrusters, _bot.PhysicsBody.CenterOfMass),
                inertia,
                inertia.Mass);

            foreach (ThrustRequest request in GetRequests())
            {
                AddThrustRequest(request);
            }
        }

        /// <summary>
        /// This adds a request to the current solver (the current solver knows about the bot's current mass, moment of inertia)
        /// </summary>
        private void AddThrustRequest(ThrustRequest request)
        {
            CurrentModel model = _currentModel;
            if (model == null)
            {
                throw new InvalidOperationException("This must be called after ResetSolver");
            }

            ThrusterSolution solution = new ThrusterSolution(request, model.Model, model.Inertia, model.Mass);
            _solutions.Add(solution);

            // This delegate gets called when a better solution is found
            var newBestFound = new Action<ThrusterMap>(o =>
            {
                solution.Map = GetThrusterSolutionMap(o, model.Model, model.Inertia, model.Mass);
            });

            var options = new DiscoverSolutionOptions2<Tuple<int, int, double>>()
            {
                //MaxIterations = 2000,     //TODO: Find a reasonable stop condition
                ThreadShare = _thrustWorkerThread,
            };

            // Find the previous solution for this request
            var prevMatch = model.PreviousSolutions.FirstOrDefault(o => ThrustRequestComparer(request, o.Request));
            if (prevMatch != null && prevMatch.Map != null)
            {
                options.Predefined = new[] { prevMatch.Map.Map.Flattened };
            }
            else
            {
                //TODO: Find nearby solutions and combine them to get an educated guess of a best solution (it may not be a very
                //good guess, but it will save the solver from starting completely from scratch)
                //options.Predefined = FindSolution();
                //
                //Don't do this during an initial creation - it will be a lot of work for nothing
            }

            // Find the combination of thrusters that push in the requested direction
            ThrustControlUtil.DiscoverSolutionAsync2(_bot, request.Linear, request.Rotate, model.CancelCurrentBalancer.Token, model.Model, newBestFound, options: options);
        }

        //TODO: This class should start with a uniform set of requests (look at a constraints class), then maintain another set of other request based on usage
        //NOTE: Any non null vector needs to be a unit vector
        private ThrustRequest[] GetRequests()
        {
            if (_is2D)
            {
                return GetRequests_Random_2D();
            }
            else
            {
                return GetRequests_Random_3D();
            }
        }
        private ThrustRequest[] GetRequests_Uniform_2D()
        {
            const int COUNT = 8;

            List<ThrustRequest> retVal = new List<ThrustRequest>();

            Vector3D baseVect = new Vector3D(1, 0, 0);
            Vector3D axis = new Vector3D(0, 0, 1);

            for (int cntr = 0; cntr < COUNT; cntr++)
            {
                Vector3D linear = baseVect.GetRotatedVector(axis, (cntr.ToDouble() / COUNT.ToDouble()) * 360d);
                retVal.Add(new ThrustRequest() { Linear = linear.ToUnit() });
            }

            retVal.Add(new ThrustRequest() { Rotate = new Vector3D(0, 0, 1).ToUnit() });
            retVal.Add(new ThrustRequest() { Rotate = new Vector3D(0, 0, -1).ToUnit() });

            return retVal.ToArray();
        }
        private ThrustRequest[] GetRequests_Uniform_3D()
        {
            //TODO: Also have a set of rotations (not doing it now, because it will be distracting in the visualizer)

            return Math3D.GetRandomVectors_SphericalShell_EvenDist(20, 1).
                Select(o => new ThrustRequest() { Linear = o.ToUnit() }).
                ToArray();
        }
        private ThrustRequest[] GetRequests_Random_2D()
        {
            //TODO: Also have a set of rotations (not doing it now, because it will be distracting in the visualizer)

            return Enumerable.Range(0, 10).
                Select(o => new ThrustRequest() { Linear = Math3D.GetRandomVector_Circular_Shell(1).ToUnit() }).
                ToArray();
        }
        private ThrustRequest[] GetRequests_Random_3D()
        {
            //TODO: Also have a set of rotations (not doing it now, because it will be distracting in the visualizer)

            return Enumerable.Range(0, 25).
                Select(o => new ThrustRequest() { Linear = Math3D.GetRandomVector_Spherical_Shell(1).ToUnit() }).
                ToArray();
        }

        private static ThrusterSolutionMap GetThrusterSolutionMap(ThrusterMap map, ThrustContributionModel model, MassMatrix inertia, double mass)
        {
            // Add up the forces
            Vector3D sumLinearForce = new Vector3D();
            Vector3D sumTorque = new Vector3D();
            foreach (ThrusterSetting thruster in map.UsedThrusters)
            {
                var contribution = model.Contributions.FirstOrDefault(o => o.Item1 == thruster.ThrusterIndex && o.Item2 == thruster.SubIndex);
                if (contribution == null)
                {
                    throw new ApplicationException(string.Format("Didn't find contribution for thruster: {0}, {1}", thruster.ThrusterIndex, thruster.SubIndex));
                }

                sumLinearForce += contribution.Item3.TranslationForce * thruster.Percent;
                sumTorque += contribution.Item3.Torque * thruster.Percent;
            }

            // Divide by mass
            //F=MA, A=F/M
            double accel = sumLinearForce.Length / mass;

            Vector3D projected = inertia.Inertia.GetProjectedVector(sumTorque);
            double angAccel = sumTorque.Length / projected.Length;
            if (Math1D.IsInvalid(angAccel))
            {
                angAccel = 0;       // this happens when there is no net torque
            }

            return new ThrusterSolutionMap(map, accel, angAccel);
        }

        private static bool ThrustRequestComparer(ThrustRequest item1, ThrustRequest item2)
        {
            if (item1 == null && item2 == null)
            {
                return true;
            }
            else if (item1 == null || item2 == null)
            {
                return false;
            }

            return VectorComparer(item1.Linear, item2.Linear) && VectorComparer(item1.Rotate, item2.Rotate);
        }
        private static bool VectorComparer(Vector3D? item1, Vector3D? item2)
        {
            if (item1 == null && item2 == null)
            {
                return true;
            }
            else if (item1 == null || item2 == null)
            {
                return false;
            }

            return Math3D.IsNearValue(item1.Value, item2.Value);
        }

        #endregion

        private static void GetSolutionLERP_Top4(Vector3D? linear, Vector3D? rotation, IEnumerable<ThrusterSolution> solutions)
        {
            const double AXISLEN = 1.3;
            const double LINETHICK = .01;
            const double DOTRAD = .05;

            const double GRAPH_WIDTH = 350;
            const double GRAPH_HEIGHT = 200;

            if (linear == null)
            {
                throw new ApplicationException("TODO: Handle null linear");
            }
            else if (rotation != null)
            {
                throw new ApplicationException("TODO: Handle non null rotation");
            }

            Vector3D linearUnit = linear.Value.ToUnit(true);
            if (linearUnit.IsInvalid())
            {
                throw new ApplicationException("TOTO: Handle non unit linear");
            }

            var dots = solutions.
                Where(o => o.Request.Linear != null && o.Request.Rotate == null).
                Select(o => new
                {
                    o = o,
                    Dot = Vector3D.DotProduct(linearUnit, o.Request.Linear.Value),
                }).
                OrderByDescending(o => o.Dot).
                ToArray();

            double closestDistance = 1d - dots[0].Dot;



            Debug3DWindow window = new Debug3DWindow()
            {
                Title = "ThrustSolutionSolver.FindSolution",
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("887")),
            };

            #region lines

            // Current lines
            foreach (var line in dots)
            {
                Vector3D pos = line.o.Request.Linear.Value;

                window.AddLine(new Point3D(0, 0, 0), pos.ToPoint(), LINETHICK, UtilityWPF.ColorFromHex("888"));

                window.AddText3D(line.Dot.ToStringSignificantDigits(3), (pos * 1.2).ToPoint(), pos, .12, UtilityWPF.ColorFromHex("888"), false);
            }

            // Request line
            window.AddLine(new Point3D(0, 0, 0), linear.Value.ToPoint(), LINETHICK * 2, UtilityWPF.ColorFromHex("333"));

            #endregion

            #region bottom panel

            StackPanel panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };

            // Text
            StringBuilder dotsText = new StringBuilder();
            for (int cntr = 0; cntr < 6; cntr++)
            {
                //double distance = cntr == 0 ?
                //    closestDistance :
                //    dots[cntr - 1].Dot - dots[cntr].Dot;

                double distance = 1d - dots[cntr].Dot;
                //TODO: Detect zero distance
                double percentClosest = distance / closestDistance;


                //TODO: don't base percent on just the closest, but the ratio of the two closest

                dotsText.AppendLine(string.Format("{0}\t{1}\t{2}", dots[cntr].Dot.ToStringSignificantDigits(3), distance.ToStringSignificantDigits(3), percentClosest.ToStringSignificantDigits(3)));
            }

            panel.Children.Add(new TextBlock()
            {
                Text = dotsText.ToString(),
            });

            #region full graph

            Canvas canvasGraph = new Canvas()
            {
                Width = GRAPH_WIDTH,
                Height = GRAPH_HEIGHT,
                Margin = new Thickness(8, 0, 0, 0),
            };

            double xStep = GRAPH_WIDTH / (dots.Length - 1);

            Tuple<double, double>[] gradient = dots.
                Select((o, i) => Tuple.Create(xStep * i, o.Dot)).
                ToArray();

            double thresholdY = dots.Length >= 4 ? dots[3].Dot : dots[dots.Length - 1].Dot;
            canvasGraph.Children.AddRange(GetGradientGraph(GRAPH_WIDTH, GRAPH_HEIGHT, gradient, UtilityWPF.ColorFromHex("40000000"), Colors.Black, -1, 1, new[] { 0d, thresholdY }));

            panel.Children.Add(canvasGraph);

            #endregion
            #region top 4 graph

            canvasGraph = new Canvas()
            {
                Width = GRAPH_WIDTH,
                Height = GRAPH_HEIGHT,
                Margin = new Thickness(8, 0, 0, 0),
            };

            int numCropped = Math.Min(4, dots.Length);

            xStep = GRAPH_WIDTH / numCropped;

            gradient = dots.
                Take(numCropped).
                Select((o, i) => Tuple.Create(xStep * i, o.Dot)).
                ToArray();

            double[] yThresholds = dots.
                Take(numCropped).
                Select(o => o.Dot).
                Concat(new[] { 1d }).
                ToArray();

            canvasGraph.Children.AddRange(GetGradientGraph(GRAPH_WIDTH, GRAPH_HEIGHT, gradient, UtilityWPF.ColorFromHex("40000000"), Colors.Black, maxY: 1, yLines: yThresholds));

            panel.Children.Add(canvasGraph);

            #endregion

            window.Messages_Bottom.Add(panel);

            #endregion

            window.Show();
        }

        private void GetSolutionLERP_PercentOfTop2(Vector3D? linear, Vector3D? rotation, List<ThrusterSolution> solutions)
        {
            const double AXISLEN = 1.3;
            const double LINETHICK = .01;
            const double DOTRAD = .05;

            const double GRAPH_WIDTH = 350;
            const double GRAPH_HEIGHT = 200;

            #region calculate

            if (linear == null)
            {
                throw new ApplicationException("TODO: Handle null linear");
            }
            else if (rotation != null)
            {
                throw new ApplicationException("TODO: Handle non null rotation");
            }

            Vector3D linearUnit = linear.Value.ToUnit(true);
            if (linearUnit.IsInvalid())
            {
                throw new ApplicationException("TOTO: Handle non unit linear");
            }

            var dots = solutions.
                Where(o => o.Request.Linear != null && o.Request.Rotate == null).
                Select(o => new
                {
                    o = o,
                    Dot = Vector3D.DotProduct(linearUnit, o.Request.Linear.Value),
                }).
                OrderByDescending(o => o.Dot).
                ToArray();

            #endregion

            Debug3DWindow window = new Debug3DWindow()
            {
                Title = "ThrustSolutionSolver.FindSolution",
            };

            #region lines

            // Current lines
            foreach (var line in dots)
            {
                Vector3D pos = line.o.Request.Linear.Value;

                Color color = line.Dot > 0 ? UtilityWPF.ColorFromHex("888") : UtilityWPF.ColorFromHex("966");

                window.AddLine(new Point3D(0, 0, 0), pos.ToPoint(), LINETHICK, color);

                window.AddText3D(line.Dot.ToStringSignificantDigits(3), (pos * 1.2).ToPoint(), pos, .12, color, false);
            }

            // Request line
            window.AddLine(new Point3D(0, 0, 0), linear.Value.ToPoint(), LINETHICK * 2, UtilityWPF.ColorFromHex("333"));

            #endregion

            dots = dots.
                Where(o => o.Dot >= 0d).
                ToArray();

            if (dots.Length < 3)
            {
                //TODO: Special handling
                window.Show();
                return;
            }

            #region projected lines

            Vector3D projectOffset = linear.Value * 2;

            Vector3D lineToBest = dots[0].o.Request.Linear.Value - linear.Value;
            lineToBest = Math3D.GetOrthogonal(linear.Value, lineToBest);
            lineToBest = lineToBest.ToUnit();

            window.AddLine(projectOffset.ToPoint(), (lineToBest + projectOffset).ToPoint(), LINETHICK, UtilityWPF.ColorFromHex("16376B"));

            for (int cntr = 1; cntr < dots.Length; cntr++)
            {
                Vector3D secondaryLine = dots[cntr].o.Request.Linear.Value - linear.Value;
                secondaryLine = Math3D.GetOrthogonal(linear.Value, secondaryLine);
                secondaryLine = secondaryLine.ToUnit();

                double dot = Vector3D.DotProduct(lineToBest, secondaryLine);

                Color color = dot > .95 ? UtilityWPF.ColorFromHex("7D7025") : UtilityWPF.ColorFromHex("184CA1");

                window.AddLine(projectOffset.ToPoint(), (secondaryLine + projectOffset).ToPoint(), LINETHICK, color);
            }

            #endregion

            double threshold = (1d - dots[1].Dot) * 1.5d;

            Grid bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            #region bottom list

            StackPanel bottomListText = new StackPanel();

            for (int cntr = 0; cntr < Math.Min(6, dots.Length); cntr++)
            {
                double distance = 1d - dots[cntr].Dot;

                string text = string.Format("{0}\t{1}",
                    dots[cntr].Dot.ToStringSignificantDigits(3),
                    distance.ToStringSignificantDigits(3)
                    );

                bottomListText.Children.Add(new TextBlock()
                {
                    Text = text,
                    Foreground = distance <= threshold ? Brushes.Black : Brushes.Red,
                });
            }

            Grid.SetColumn(bottomListText, 0);
            bottomGrid.Children.Add(bottomListText);

            #endregion

            window.Show();
        }


        //TODO: Put this in debug 3D window
        /// <summary>
        /// This returns a visual of a graph that can be added to a canvas
        /// </summary>
        private static IEnumerable<UIElement> GetGradientGraph(double width, double height, Tuple<double, double>[] gradient, Color fill, Color stroke, double? minY = null, double? maxY = null, double[] yLines = null)
        {
            if (gradient == null || gradient.Length <= 1)       // need at least two for a gradient
            {
                return new UIElement[0];
            }
            else if (width.IsNearZero() || height.IsNearZero())
            {
                return new UIElement[0];
            }

            List<UIElement> retVal = new List<UIElement>();

            double minYFinal = minY ?? UtilityCore.Iterate(gradient.Select(o => o.Item2), yLines).Min();
            double maxYFinal = maxY ?? UtilityCore.Iterate(gradient.Select(o => o.Item2), yLines).Max();

            if (yLines != null)
            {
                #region Y Lines (dashed lines)

                foreach (double y in yLines)
                {
                    Color colorDash = UtilityWPF.AlphaBlend(UtilityWPF.AlphaBlend(stroke, Colors.Gray, .85), Colors.Transparent, .66);

                    double yDash = ((maxYFinal - y) / (maxYFinal - minYFinal)) * height;

                    retVal.Add(new Line()
                    {
                        Stroke = new SolidColorBrush(colorDash),
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection(new[] { 4d, 2d }),
                        X1 = 0,
                        X2 = width,
                        Y1 = yDash,
                        Y2 = yDash,
                    });
                }

                #endregion
            }

            double lastGradX = gradient[gradient.Length - 1].Item1;
            if (!Math1D.IsNearZero(lastGradX) && lastGradX > 0)
            {
                Polyline polyLine = new Polyline();
                Polygon polyFill = new Polygon();

                polyLine.Stroke = new SolidColorBrush(stroke);
                polyLine.StrokeThickness = 2;

                polyFill.Fill = new SolidColorBrush(fill);

                //NOTE: gradient must be sorted on Item1
                double xScale = width / lastGradX;

                for (int cntr = 0; cntr < gradient.Length; cntr++)
                {
                    double x = gradient[cntr].Item1 * xScale;
                    double y = ((maxYFinal - gradient[cntr].Item2) / (maxYFinal - minYFinal)) * height;

                    polyLine.Points.Add(new Point(x, y));
                    polyFill.Points.Add(new Point(x, y));
                }

                // Circle back fill to make a polygon
                polyFill.Points.Add(new Point(polyFill.Points[polyFill.Points.Count - 1].X, height));
                polyFill.Points.Add(new Point(polyFill.Points[0].X, height));

                retVal.Add(polyFill);
                retVal.Add(polyLine);
            }

            return retVal;
        }

    }

    #endregion
}
