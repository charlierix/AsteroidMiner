using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.Testers.Controllers
{
    public static class ThrustControlUtil
    {
        #region Declaration Section

        private const double MAXERROR = 100000000000;       // need something excessively large, but less than double.max

        #endregion

        /// <summary>
        /// This is a wrapper of UtilityAI.DiscoverSolution
        /// </summary>
        public static Task<ThrusterMap> DiscoverSolutionAsync(ControlledThrustBot bot, Vector3D? idealLinear, Vector3D? idealRotation, CancellationToken? cancel = null, ThrustContributionModel model = null, Action<ThrusterMap> newBestFound = null)
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

            #endregion

            // Do it
            var task = Task.Run(() => UtilityAI.DiscoverSolution(delegates));

            // Normalize and convert return value
            return task.ContinueWith(o => new ThrusterMap(ThrustControlUtil.Normalize(o.Result.Item), allThrusters, token));
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
            error[2] = GetScore_Inneficient(map, model);

            // Total
            // It's important to include the maximize thrust, but just a tiny bit.  Without it, the ship will be happy to
            // fire thrusters in all directions.  But if maximize thrust is any stronger, the solution will have too much
            // unbalance
            double total = error[0] + (error[1] * .01) + (error[2] * .1);

            return new SolutionError(error, total);
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

            double dot = Vector3D.DotProduct(objective.Value.ToUnit(false), actual.ToUnit(false));

            // Ideal dot is 1, so 1-1 is 0.
            // 1 - 0 is 1
            // 1 - -1 is 2
            // So divide by 2 to get difference in a range of 0 to 1
            double difference = (1d - dot) / 2d;

            // Now that difference is scaled 0 to 1, use the length squared as max error
            double dotScale = actual.LengthSquared * difference;

            return dotScale;
        }
        private static double GetScore_UnderPowered(Vector3D? objective, Vector3D actual, double maxPossible)
        {
            if (objective == null || objective.Value.LengthSquared.IsNearZero())
            {
                // The underpowered check doesn't care about this condition
                return 0;
            }

            double lenSqr = actual.LengthSquared;
            double maxSqr = maxPossible * maxPossible;

            double dot = Vector3D.DotProduct(objective.Value.ToUnit(false), actual.ToUnit(false));

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
        private static double GetScore_Inneficient(ThrusterMap map, ThrustContributionModel model)
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

        #endregion
    }

    #region Class: ThrustContributionModel

    public class ThrustContributionModel
    {
        //TODO: Take in the mass matrix - this isn't necessary, but helps with fine tuning
        public ThrustContributionModel(Thruster[] thrusters, Point3D centerOfMass)
        {
            this.Contributions = GetThrusterContributions(thrusters, centerOfMass);
            this.CenterOfMass = centerOfMass;
        }

        public readonly Tuple<int, int, ThrustContribution>[] Contributions;

        /// <summary>
        /// This is in bot's model coords
        /// </summary>
        public readonly Point3D CenterOfMass;

        #region Private Methods

        private static Tuple<int, int, ThrustContribution>[] GetThrusterContributions(Thruster[] thrusters, Point3D centerOfMass)
        {
            //This method is copied from ShipPartTesterWindow (and ShipPlayer)

            var retVal = new List<Tuple<int, int, ThrustContribution>>();

            for (int outer = 0; outer < thrusters.Length; outer++)
            {
                for (int inner = 0; inner < thrusters[outer].ThrusterDirectionsShip.Length; inner++)
                {
                    // This is copied from Body.AddForceAtPoint

                    Vector3D offsetFromMass = thrusters[outer].Position - centerOfMass;		// this is ship's local coords
                    Vector3D force = thrusters[outer].ThrusterDirectionsShip[inner] * thrusters[outer].ForceAtMax;

                    Vector3D translationForce, torque;
                    Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out torque, offsetFromMass, force);

                    retVal.Add(Tuple.Create(outer, inner, new ThrustContribution(thrusters[outer], inner, translationForce, torque)));
                }
            }

            return retVal.ToArray();
        }

        #endregion
    }

    #endregion
    #region Class: ThrustContribution

    public class ThrustContribution
    {
        public ThrustContribution(Thruster thruster, int index, Vector3D translationForce, Vector3D torque)
        {
            this.Thruster = thruster;
            this.Index = index;

            this.TranslationForceLength = translationForce.Length;
            this.TranslationForce = translationForce;
            this.TranslationForceUnit = new Vector3D(translationForce.X / this.TranslationForceLength, translationForce.Y / this.TranslationForceLength, translationForce.Z / this.TranslationForceLength);

            this.TorqueLength = torque.Length;
            this.Torque = torque;
            this.TorqueUnit = new Vector3D(torque.X / this.TorqueLength, torque.Y / this.TorqueLength, torque.Z / this.TorqueLength);
        }

        public readonly Thruster Thruster;
        /// <summary>
        /// This is the sub index
        /// </summary>
        public readonly int Index;

        public readonly Vector3D TranslationForce;
        public readonly Vector3D TranslationForceUnit;
        public readonly double TranslationForceLength;

        public readonly Vector3D Torque;
        public readonly Vector3D TorqueUnit;
        public readonly double TorqueLength;
    }

    #endregion

    #region Class: ThrusterMap

    public class ThrusterMap
    {
        public ThrusterMap(Tuple<int, int, double>[] flattened, Thruster[] allThrusters, long botToken)
        {
            #region build used

            List<ThrusterSetting> used = new List<ThrusterSetting>();

            foreach (var item in flattened)
            {
                if (Math1D.IsNearZero(item.Item3))
                {
                    continue;
                }

                used.Add(new ThrusterSetting(allThrusters[item.Item1], item.Item1, item.Item2, item.Item3));
            }

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
    #region Class: ThrusterSetting

    public class ThrusterSetting
    {
        public ThrusterSetting(Thruster thruster, int thrusterIndex, int subIndex, double percent)
        {
            this.Thruster = thruster;
            this.ThrusterIndex = ThrusterIndex;
            this.SubIndex = subIndex;
            this.Percent = percent;
        }

        public readonly Thruster Thruster;
        public readonly int ThrusterIndex;
        public readonly int SubIndex;
        public readonly double Percent;
    }

    #endregion
}
