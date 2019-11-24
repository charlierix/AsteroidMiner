using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipParts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.GameItems
{
    //TODO: Build overloads that combine multiple ships into one - that should be a separate genetic algorithms class
    //TODO: Mutate maps
    //TODO: Factor is too simple.  Make an args class
    public static class MutateUtility
    {
        #region Thoughts

        // Should mutate all the properties, or only some of the properties?
        //		I read an article that said only mutating one random property produces good results in a lot of cases
        //
        // Or mutate a few random properties with very low mutation rates, and a couple others with a bit higher rates?

        // Should some properties have a min/max cap, so that the mutation rate acts more like a spring force when too close to the caps?
        //		Why should the organism's design be pre decided like this?
        //		I'm not sure I like this idea

        // Intead of the args dictating global general terms, it might be useful to store a history of previous mutations:
        //		Use reflection to get each absolute property name
        //		Store those property names with a history of the last X mutations:
        //			Dictionary<string, MutateStep[]>
        //
        // But this may be overkill to do for each property
        //
        // A simpler solution would be for the class being mutated to hold the current mutation rate

        // When mutating a ship, there should probably be some independent passes
        //		Part divide/create/merge:
        //			If a part is too small, may want to delete it
        //			May want to insert a small random part
        //			May want to divide a large part into two smaller ones
        //			May want to merge two smaller parts into a larger one
        //			Analyze the layout of the ship to find limbs, and copy an entire limb (the concept of a limb should probably be part of the ship):
        //				A ship could be made of free floating parts, and limbs that contain attached parts or more limbs
        //				Limbs would be more handy when joints are supported
        //
        //		Individual properties:
        //			Mutate individual properties of a few randomly selected parts
        //
        //		Neural:
        //
        //		Validation steps:
        //			Without an energy tank, the ship will fail
        //			Don't allow parts to intersect


        // For asexual reproduction, simple mutation is probably best.  But when there are multiple parents, may want some crossover algorithms

        #endregion

        #region enum: FactorType

        public enum FactorType
        {
            /// <summary>
            /// Factor represents a percent of an item's current value (.1 would be plus or minus 10%)
            /// </summary>
            /// <remarks>
            /// Factor is a value greater than 0:
            ///		0 means no change (no point in calling mutate)
            ///		.01 means +- 1%
            ///		.1 means +- 10%
            ///		1 means +- 100%
            ///		2 means +- 200%
            ///		
            /// A good value to use is probably around .01 to .0001
            /// </remarks>
            Percent,
            /// <summary>
            /// Factor represents a fixed amout to add remove from item's current value
            /// </summary>
            /// <remarks>
            /// Percent will never cross past 0 (0 is an asymptote, and the value will creep slower to zero the closer it gets)
            /// 
            /// But distance will just hop a fixed amount up or down
            /// </remarks>
            Distance
        }

        #endregion
        #region class: MuateArgs

        public class MuateArgs
        {
            #region Constructor

            // These three only populate DefaultFactor
            public MuateArgs(double factor)
            {
                this.NumProperties = null;
                this.PercentProperties = null;

                this.FactorByPropName = null;
                this.FactorByDataType = null;
                this.DefaultFactor = new MuateFactorArgs(FactorType.Percent, factor);
            }
            public MuateArgs(bool TrueIsNumProps_FalseIsPercentProps, double props, double factor)
            {
                if (TrueIsNumProps_FalseIsPercentProps)
                {
                    this.NumProperties = props;
                    this.PercentProperties = null;
                }
                else
                {
                    this.PercentProperties = props;
                    this.NumProperties = null;
                }

                this.FactorByPropName = null;
                this.FactorByDataType = null;
                this.DefaultFactor = new MuateFactorArgs(FactorType.Percent, factor);
            }
            public MuateArgs(bool TrueIsNumProps_FalseIsPercentProps, double props, MuateFactorArgs factor)
            {
                if (TrueIsNumProps_FalseIsPercentProps)
                {
                    this.NumProperties = props;
                    this.PercentProperties = null;
                }
                else
                {
                    this.PercentProperties = props;
                    this.NumProperties = null;
                }

                this.FactorByPropName = null;
                this.FactorByDataType = null;
                this.DefaultFactor = factor;
            }

            // These two will populate all three (it's safe to pass null for some of the factor types)
            public MuateArgs(Tuple<string, MuateFactorArgs>[] factorByPropName, Tuple<PropsByPercent.DataType, MuateFactorArgs>[] factorByDataType, MuateFactorArgs defaultFactor)
            {
                this.NumProperties = null;
                this.PercentProperties = null;

                this.FactorByPropName = factorByPropName;
                this.FactorByDataType = factorByDataType;
                this.DefaultFactor = defaultFactor;
            }
            public MuateArgs(bool TrueIsNumProps_FalseIsPercentProps, double props, Tuple<string, MuateFactorArgs>[] factorByPropName, Tuple<PropsByPercent.DataType, MuateFactorArgs>[] factorByDataType, MuateFactorArgs defaultFactor)
            {
                if (TrueIsNumProps_FalseIsPercentProps)
                {
                    this.NumProperties = props;
                    this.PercentProperties = null;
                }
                else
                {
                    this.PercentProperties = props;
                    this.NumProperties = null;
                }

                this.FactorByPropName = factorByPropName;
                this.FactorByDataType = factorByDataType;
                this.DefaultFactor = defaultFactor;
            }

            #endregion

            /// <summary>
            /// This is how many properties to mutate.  If this is null (and %props is null), then all properties will be mutated
            /// </summary>
            /// <remarks>
            /// The reason this is a double and not an integer, is because you may want to mutate 1.5 properties.  That means that one property
            /// would get the full amount of factor, and one property would get half of factor.
            /// </remarks>
            public readonly double? NumProperties;
            /// <summary>
            /// Instead of a hard number of properties, this is what percent of properties should change (useful when you don't know up
            /// front how many properties there are)
            /// </summary>
            public readonly double? PercentProperties;

            // Any of these three properties could be null.  When a property is going to be mutated, these factors are scanned in the order:
            //		Name, else DataType, else Default
            // If there is no match, and exception is thrown
            public readonly Tuple<string, MuateFactorArgs>[] FactorByPropName;		// name scan is case sensitive
            public readonly Tuple<PropsByPercent.DataType, MuateFactorArgs>[] FactorByDataType;
            public readonly MuateFactorArgs DefaultFactor;		//NOTE: It's probably safest for default to be a percent

            #region TODO

            //TODO: Figure out how to make this work.  A larger standard deviation would mutate more properties, but less per prop (the sum of the mutation would still add up to NumProperties * Factor)
            //so lots of props by a little, or few props by a lot
            //public readonly double? PropertyStandardDeviation;

            //TODO: Implement this
            //public readonly bool UseAllOfFactor;

            #endregion

            public int GetChangeCount(int arrayLength)
            {
                int retVal;
                if (this.NumProperties != null)
                {
                    retVal = this.NumProperties.Value.ToInt_Round();
                }
                else if (this.PercentProperties != null)
                {
                    retVal = (this.PercentProperties.Value * arrayLength).ToInt_Round();
                }
                else
                {
                    retVal = arrayLength;
                }

                if (retVal < 0)
                {
                    retVal = 0;
                }
                else if (retVal > arrayLength)
                {
                    retVal = arrayLength;
                }

                return retVal;
            }
        }

        #endregion
        #region class: MuateFactorArgs

        public class MuateFactorArgs
        {
            public MuateFactorArgs(FactorType factorType, double factor)
                : this(factorType, factor, true) { }
            public MuateFactorArgs(FactorType factorType, double factor, bool isRandom)
            {
                this.FactorType = factorType;
                this.Factor = factor;
                this.IsRandom = isRandom;
            }

            public readonly FactorType FactorType;
            /// <summary>
            /// True: A random value from 0 to Factor is used.
            /// False: Exactly Factor is used.
            /// </summary>
            /// <remarks>
            /// It's a bit odd for a mutator to not be random, but the false case is easy enough to implement
            /// </remarks>
            public readonly bool IsRandom;

            public readonly double Factor;
        }

        #endregion
        #region class: ShipPartAddRemoveArgs

        public class ShipPartAddRemoveArgs
        {
            public ShipPartAddRemoveArgs()
            {
                // See the thoughts section at the top of this file for ideas
                throw new ApplicationException("finish this");
            }
        }

        #endregion
        #region class: NeuronMutateArgs

        public class NeuronMutateArgs
        {
            public NeuronMutateArgs(MuateArgs neuronMovement, MuateArgs neuronAddRemove, MuateArgs linkMovement, MuateArgs linkAddRemove)
            {
                this.NeuronMovement = neuronMovement;
                this.NeuronAddRemove = neuronAddRemove;
                this.LinkMovement = linkMovement;
                this.LinkAddRemove = linkAddRemove;
            }

            //NOTE: Add/Remove neurons and links can have an odd consequence when instantiating that actual part:
            //If there are too many, some will be randomly removed, and if there are too few, some will be randomly created
            //So these probabilities should be very low (just wait for the part's size property to change)

            public readonly MuateArgs NeuronMovement;
            public readonly MuateArgs NeuronAddRemove;

            public readonly MuateArgs LinkMovement;
            public readonly MuateArgs LinkAddRemove;
        }

        #endregion
        #region class: ShipMutateArgs

        public class ShipMutateArgs
        {
            public ShipMutateArgs(ShipPartAddRemoveArgs partAddRemove, MuateArgs partChanges, NeuronMutateArgs neuronChanges)
            {
                this.PartAddRemove = partAddRemove;
                this.PartChanges = partChanges;
                this.NeuronChanges = neuronChanges;
            }

            //NOTE: It's ok for some of these to be null.  That type of mutation will be skipped
            public readonly ShipPartAddRemoveArgs PartAddRemove;
            public readonly MuateArgs PartChanges;
            public readonly NeuronMutateArgs NeuronChanges;
        }

        #endregion

        #region Public Methods - big objects

        public static T[] MutateList<T>(T[] list, MuateArgs args)
        {
            T[] cloned = UtilityCore.Clone(list);

            PropsByPercent props = new PropsByPercent(cloned.Select(o => (object)o).ToArray(), new PropsByPercent.FilterArgs() { IgnoreTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String } });

            MutateProps(props, args);

            // Ran into a problem returning cloned.  The list passed in was a bunch of Vector3D and the mutated values were only
            // in props._list, because structs are copies.  So need to return that list back to the caller
            return props.List.
                Select(o => (T)o).
                ToArray();
        }
        public static T MutateSettingsObject<T>(T settings, MuateArgs args)
        {
            T retVal = UtilityCore.Clone(settings);

            PropsByPercent props = new PropsByPercent(new object[] { retVal }, new PropsByPercent.FilterArgs() { IgnoreTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String } });

            MutateProps(props, args);

            return retVal;
        }

        public static ShipDNA Mutate(ShipDNA dna, ShipMutateArgs args)
        {
            // Start off with a clone of what was passed in
            ShipDNA retVal = UtilityCore.Clone(dna);
            retVal.Generation++;

            if (args.PartAddRemove != null)
            {
                throw new ApplicationException("finish this");
            }

            if (args.PartChanges != null)
            {
                PropsByPercent props = new PropsByPercent(retVal.PartsByLayer.SelectMany(o => o.Value),
                    new PropsByPercent.FilterArgs() { IgnoreNames = new string[] { "Neurons", "AltNeurons", "InternalLinks", "ExternalLinks" }, IgnoreTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String } });

                //string[] propNames = props.Select(o => o.PropertyName).Distinct().OrderBy(o => o).ToArray();		// this is just for debugging

                MutateProps(props, args.PartChanges);
            }

            if (args.NeuronChanges != null)
            {
                // Get all the parts that are dna containers
                ShipPartDNA[] parts = retVal.PartsByLayer.SelectMany(o => o.Value).ToArray();

                // Call the neural specific overload
                Mutate(parts, args.NeuronChanges);
            }

            return retVal;
        }

        public static void Mutate(ShipPartDNA[] parts, NeuronMutateArgs args)
        {
            if (args.NeuronMovement != null)
            {
                PropsByPercent props = new PropsByPercent(parts, new PropsByPercent.FilterArgs() { OnlyUseNames = new string[] { "Neurons", "AltNeurons" } });
                MutateProps(props, args.NeuronMovement);
            }

            if (args.LinkMovement != null)
            {
                PropsByPercent props = new PropsByPercent(parts.SelectMany(o => UtilityCore.Iterate(o.InternalLinks, o.ExternalLinks)));		// internal and external links are complex types, so make the list iterate over each of them to get at the primitive props inside
                MutateProps(props, args.LinkMovement);
            }
        }

        #endregion
        #region Public Methods - primitives

        // Double
        public static double Mutate(double value, MuateFactorArgs args)
        {
            switch (args.FactorType)
            {
                case FactorType.Distance:
                    if (args.IsRandom)
                    {
                        return Mutate_RandDistance(value, args.Factor);
                    }
                    else
                    {
                        return Mutate_FixedDistance(value, args.Factor);
                    }

                case FactorType.Percent:
                    if (args.IsRandom)
                    {
                        return Mutate_RandPercent(value, args.Factor);
                    }
                    else
                    {
                        return Mutate_FixedPercent(value, args.Factor);
                    }

                default:
                    throw new ApplicationException("Unknown FactorType: " + args.FactorType.ToString());
            }
        }

        /// <summary>
        /// This mutates the value by a fixed factor.  It is still random whether that mutation is up or down
        /// </summary>
        /// <param name="factor">0=0%, .1=10%</param>
        public static double Mutate_FixedPercent(double value, double factor)
        {
            if (StaticRandom.Next(2) == 0)
            {
                // Add
                return value * (1d + factor);
            }
            else
            {
                // Remove
                return value / (1d + factor);
            }
        }
        /// <summary>
        /// This mutates the value a random amount up to the factor
        /// </summary>
        /// <param name="factor">0=0%, .1=10%</param>
        public static double Mutate_RandPercent(double value, double factor)
        {
            //NOTE: This is a copy of the overload without the remainer variable (I'm prioritizing on speed at the expense of code duplication)

            Random rand = StaticRandom.GetRandomForThread();

            if (rand.Next(2) == 0)
            {
                // Add
                return value * (1d + (rand.NextDouble() * factor));
            }
            else
            {
                // Remove
                return value / (1d + (rand.NextDouble() * factor));
            }
        }
        /// <summary>
        /// This mutates the value a random amount up to the factor
        /// </summary>
        /// <param name="factor">0=0%, .1=10%</param>
        /// <param name="remainder">
        /// This is how much of factor wasn't used.  This is useful if you want all of factor used up across multiple properties.
        /// </param>
        public static double Mutate_RandPercent(out double remainder, double value, double factor)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double randFactor = rand.NextDouble() * factor;
            remainder = factor - randFactor;

            if (rand.Next(2) == 0)
            {
                // Add
                return value * (1d + randFactor);
            }
            else
            {
                // Remove
                return value / (1d + randFactor);
            }
        }

        public static double Mutate_FixedDistance(double value, double factor)
        {
            if (StaticRandom.Next(2) == 0)
            {
                // Add
                return value + factor;
            }
            else
            {
                // Remove
                return value - factor;
            }
        }
        public static double Mutate_RandDistance(double value, double factor)
        {
            //NOTE: This is a copy of the overload without the remainer variable (I'm prioritizing on speed at the expense of code duplication)

            Random rand = StaticRandom.GetRandomForThread();

            if (rand.Next(2) == 0)
            {
                // Add
                return value + (rand.NextDouble() * factor);
            }
            else
            {
                // Remove
                return value - (rand.NextDouble() * factor);
            }
        }
        public static double Mutate_RandDistance(out double remainder, double value, double factor)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double randFactor = rand.NextDouble() * factor;
            remainder = factor - randFactor;

            if (rand.Next(2) == 0)
            {
                // Add
                return value + randFactor;
            }
            else
            {
                // Remove
                return value - randFactor;
            }
        }

        // Vector
        public static Vector3D Mutate(Vector3D value, MuateFactorArgs args)
        {
            switch (args.FactorType)
            {
                case FactorType.Distance:
                    if (args.IsRandom)
                    {
                        return Mutate_RandDistance(value, args.Factor);
                    }
                    else
                    {
                        return Mutate_FixedDistance(value, args.Factor);
                    }

                case FactorType.Percent:
                    if (args.IsRandom)
                    {
                        return Mutate_RandPercent(value, args.Factor);
                    }
                    else
                    {
                        return Mutate_FixedPercent(value, args.Factor);
                    }

                default:
                    throw new ApplicationException("Unknown FactorType: " + args.FactorType.ToString());
            }
        }

        public static Vector3D Mutate_FixedPercent(Vector3D value, double factor)
        {
            return value + Math3D.GetRandomVector_Spherical_Shell(value.Length * factor);
        }
        public static Vector3D Mutate_RandPercent(Vector3D value, double factor)
        {
            return value + Math3D.GetRandomVector_Spherical(value.Length * factor);
        }
        public static Vector3D Mutate_RandPercent(out double remainder, Vector3D value, double factor)
        {
            double length = value.Length;

            Vector3D offset = Math3D.GetRandomVector_Spherical(length * factor);
            remainder = factor - (offset.Length / length);

            return value + offset;
        }

        public static Vector3D Mutate_FixedDistance(Vector3D value, double factor)
        {
            return value + Math3D.GetRandomVector_Spherical_Shell(factor);
        }
        public static Vector3D Mutate_RandDistance(Vector3D value, double factor)
        {
            return value + Math3D.GetRandomVector_Spherical(factor);
        }
        public static Vector3D Mutate_RandDistance(out double remainder, Vector3D value, double factor)
        {
            Vector3D offset = Math3D.GetRandomVector_Spherical(factor);
            remainder = factor - offset.Length;

            return value + offset;
        }

        // Quaternion
        public static Quaternion Mutate(Quaternion value, MuateFactorArgs args)
        {
            switch (args.FactorType)
            {
                case FactorType.Distance:
                    if (args.IsRandom)
                    {
                        return Mutate_RandDistance(value, args.Factor);
                    }
                    else
                    {
                        return Mutate_FixedDistance(value, args.Factor);
                    }

                case FactorType.Percent:
                    if (args.IsRandom)
                    {
                        return Mutate_RandPercent(value, args.Factor);
                    }
                    else
                    {
                        return Mutate_FixedPercent(value, args.Factor);
                    }

                default:
                    throw new ApplicationException("Unknown FactorType: " + args.FactorType.ToString());
            }
        }

        public static Quaternion Mutate_FixedPercent(Quaternion value, double factor)
        {
            if (StaticRandom.Next(2) == 0)
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), factor * 360d));
            }
            else
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), -factor * 360d));
            }
        }
        public static Quaternion Mutate_RandPercent(Quaternion value, double factor)
        {
            return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), Math1D.GetNearZeroValue(factor * 360d)));
        }
        public static Quaternion Mutate_RandPercent(out double remainder, Quaternion value, double factor)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double randFactor = rand.NextDouble() * factor;
            remainder = factor - randFactor;

            if (StaticRandom.Next(2) == 0)
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), randFactor * 360d));
            }
            else
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), -randFactor * 360d));
            }
        }

        public static Quaternion Mutate_FixedDistance(Quaternion value, double factor)
        {
            if (StaticRandom.Next(2) == 0)
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), factor));
            }
            else
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), -factor));
            }
        }
        public static Quaternion Mutate_RandDistance(Quaternion value, double factor)
        {
            return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), Math1D.GetNearZeroValue(factor)));
        }
        public static Quaternion Mutate_RandDistance(out double remainder, Quaternion value, double factor)
        {
            Random rand = StaticRandom.GetRandomForThread();

            double randFactor = rand.NextDouble() * factor;
            remainder = factor - randFactor;

            if (StaticRandom.Next(2) == 0)
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), randFactor));
            }
            else
            {
                return value.RotateBy(new Quaternion(Math3D.GetRandomVector_Spherical_Shell(1d), -randFactor));
            }
        }

        #endregion

        #region Private Methods

        private static void MutateProps(PropsByPercent props, MuateArgs args)
        {
            if ((args.NumProperties == null && args.PercentProperties == null) ||		// nothing specified, so all
                (args.NumProperties != null && args.NumProperties.Value > props.Count) ||		// they want to change more than exists
                (args.PercentProperties != null && args.PercentProperties.Value >= 1d))		// they want to change more than 100%
            {
                #region All of them

                foreach (var prop in props)
                {
                    MutateProp(prop, FindFactor(prop, args));
                }

                #endregion
                return;
            }

            // Figure out how many properties to touch
            double count;
            if (args.NumProperties != null)
            {
                count = args.NumProperties.Value;
            }
            else
            {
                count = props.Count * args.PercentProperties.Value;
            }

            Random rand = StaticRandom.GetRandomForThread();

            List<PropsByPercent.PropWrapper> used = new List<PropsByPercent.PropWrapper>();

            // Modify that many properties
            for (int cntr = 0; cntr < count; cntr++)		// comparing an int with a double.  If there's a fraction, it's like using ceiling
            {
                while (true)
                {
                    // Come up with a random percentage into the list of properties
                    double percent = rand.NextDouble();

                    // Get the property at that percent
                    PropsByPercent.PropWrapper prop = props.GetProperty(percent);
                    if (prop == null)
                    {
                        throw new ApplicationException("Didn't find property at percent: " + percent.ToString());
                    }

                    if (!used.Contains(prop))
                    {
                        var factor = FindFactor(prop, args);
                        if (factor != null)
                        {
                            // This property hasn't been modified yet.  Figure out how much to modify it
                            double factorValue = factor.Factor;
                            if (cntr + 1 > count)
                            {
                                factorValue *= (count - cntr);		// this last one is only a fraction
                                factor = new MuateFactorArgs(factor.FactorType, factorValue, factor.IsRandom);
                            }

                            // Mutate it
                            MutateProp(prop, factor);
                        }

                        // Remember that this property was modified (even if the factor is null)
                        used.Add(prop);
                        break;
                    }
                }
            }
        }
        private static void MutateProp(PropsByPercent.PropWrapper prop, MuateFactorArgs args)
        {
            switch (prop.DataType)
            {
                case PropsByPercent.DataType.Double:
                    prop.SetValue(Mutate((double)prop.GetValue(), args));
                    break;

                case PropsByPercent.DataType.Vector3D:
                    #region Vector3D

                    switch (prop.PropertyName)
                    {
                        case "Scale":
                            if (prop.Item is ShipPartDNA)
                            {
                                MutateProp_Scale(prop, args);
                                return;
                            }
                            break;

                        case "ThrusterDirections":
                            if (prop.Item is ThrusterDNA)
                            {
                                MutateProp_ThrusterDirection(prop, args);
                                return;
                            }
                            break;
                    }

                    prop.SetValue(Mutate((Vector3D)prop.GetValue(), args));

                    #endregion
                    break;

                case PropsByPercent.DataType.Point3D:
                    prop.SetValue(Mutate(((Point3D)prop.GetValue()).ToVector(), args).ToPoint());
                    break;

                case PropsByPercent.DataType.Quaternion:
                    prop.SetValue(Mutate((Quaternion)prop.GetValue(), args));
                    break;

                default:
                    throw new ApplicationException("Unexpected PropsByPercent.DataType: " + prop.DataType.ToString());
            }
        }
        private static void MutateProp_Scale(PropsByPercent.PropWrapper prop, MuateFactorArgs args)
        {
            // Get the constraint for this
            ShipPartDNA item = (ShipPartDNA)prop.Item;
            PartDesignAllowedScale allowed = PartAllowedScale.GetForPart(item.PartType, true);

            // Cast the current value
            Vector3D currentValue = (Vector3D)prop.GetValue();
            Vector3D newValue = currentValue;

            double newValue1;

            // Mutate one of the axiis
            switch (allowed)
            {
                case PartDesignAllowedScale.X_Y_Z:
                    #region X_Y_Z

                    switch (StaticRandom.Next(3))
                    {
                        case 0:
                            //X
                            newValue1 = Mutate(currentValue.X, args);
                            newValue = new Vector3D(newValue1, currentValue.Y, currentValue.Z);
                            break;

                        case 1:
                            //Y
                            newValue1 = Mutate(currentValue.Y, args);
                            newValue = new Vector3D(currentValue.X, newValue1, currentValue.Z);
                            break;

                        default:
                            //Z
                            newValue1 = Mutate(currentValue.Z, args);
                            newValue = new Vector3D(currentValue.X, currentValue.Y, newValue1);
                            break;
                    }

                    #endregion
                    break;

                case PartDesignAllowedScale.XY_Z:
                    #region XY_Z

                    if (StaticRandom.Next(2) == 0)
                    {
                        //XY
                        newValue1 = Mutate(currentValue.X, args);		// X and Y should be the same
                        newValue = new Vector3D(newValue1, newValue1, currentValue.Z);
                    }
                    else
                    {
                        //Z
                        newValue1 = Mutate(currentValue.Z, args);
                        newValue = new Vector3D(currentValue.X, currentValue.Y, newValue1);
                    }

                    #endregion
                    break;

                case PartDesignAllowedScale.XYZ:
                    #region XYZ

                    newValue1 = Mutate(currentValue.X, args);		// they should all be the same anyway, so just grab X
                    newValue = new Vector3D(newValue1, newValue1, newValue1);

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown PartDesignAllowedScale: " + allowed.ToString());
            }

            // Store the new value
            prop.SetValue(newValue);
        }
        private static void MutateProp_ThrusterDirection(PropsByPercent.PropWrapper prop, MuateFactorArgs args)
        {
            ThrusterDNA item = (ThrusterDNA)prop.Item;
            item.ThrusterType = ThrusterType.Custom;		// any change to the thrust direction needs to make this custom

            Vector3D current = ((Vector3D)prop.GetValue()).ToUnit();

            Quaternion delta = Mutate(Quaternion.Identity, args);

            current = delta.GetRotatedVector(current);

            prop.SetValue(current);
        }

        private static MuateFactorArgs FindFactor(PropsByPercent.PropWrapper prop, MuateArgs args)
        {
            if (args.FactorByPropName != null)
            {
                string name = prop.PropertyName;

                for (int cntr = 0; cntr < args.FactorByPropName.Length; cntr++)
                {
                    if (args.FactorByPropName[cntr].Item1 == name)
                    {
                        // Found a match against property name
                        return args.FactorByPropName[cntr].Item2;
                    }
                }
            }

            if (args.FactorByDataType != null)
            {
                PropsByPercent.DataType dataType = prop.DataType;

                for (int cntr = 0; cntr < args.FactorByDataType.Length; cntr++)
                {
                    if (args.FactorByDataType[cntr].Item1 == dataType)
                    {
                        // Found a match against datatype
                        return args.FactorByDataType[cntr].Item2;
                    }
                }
            }

            // No special cases found, return the default (could be null)
            return args.DefaultFactor;
        }

        #region OLD

        ///// <summary>
        ///// This one mutates every property.  Not very practical, only a couple should be mutated at a time
        ///// </summary>
        //private static void MutateProperties<T>(T from, T to, double factor)
        //{
        //    PropertyInfo[] fromInfo = typeof(T).GetProperties();
        //    PropertyInfo[] toInfo = typeof(T).GetProperties();

        //    foreach (PropertyInfo fromProp in fromInfo)
        //    {
        //        MutateProperty(from, to, fromProp, toInfo, factor);
        //    }
        //}
        //private static void MutateProperties<T>(T from, T to, double numProperties, double factor)
        //{
        //    PropertyInfo[] fromInfo = typeof(T).GetProperties();
        //    if (numProperties > fromInfo.Length)
        //    {
        //        // Use the simpler overload
        //        MutateProperties(from, to, factor);
        //        return;
        //    }

        //    PropertyInfo[] toInfo = typeof(T).GetProperties();

        //    int numPropsInt = Convert.ToInt32(Math.Ceiling(numProperties));
        //    double remaining = numProperties;

        //    foreach (PropertyInfo fromProp in UtilityHelper.RandomRange(0, fromInfo.Length, numPropsInt).Select(o => fromInfo[o]))
        //    {
        //        // Figure out how much to mutate
        //        double newFactor = factor;
        //        if (remaining < 1d)
        //        {
        //            newFactor = factor * remaining;		// when remaining is less than one, it acts like a percent (ex: if they pass in 2.33, 2 props get full factor, 1 prop gets 33% factor)
        //        }

        //        // Mutate it
        //        if (MutateProperty(from, to, fromProp, toInfo, factor))
        //        {
        //            remaining -= 1d;		// only decrement when a property was actually mutated
        //        }
        //    }
        //}
        //private static bool MutateProperty<T>(T from, T to, PropertyInfo fromProp, PropertyInfo[] toInfo, double factor)
        //{
        //    object val = null;

        //    string name = fromProp.PropertyType.FullName.ToLower();
        //    switch (name)
        //    {
        //        case "system.double":
        //            #region Double

        //            val = fromProp.GetValue(from, null);
        //            val = Mutate_RandPercent((double)val, factor);

        //            #endregion
        //            break;

        //        //TODO: Support int, vector3d, point3d, quaternion

        //        default:
        //            return false;
        //    }

        //    // Store this new value in the output class
        //    PropertyInfo toProp = toInfo.Where(o => o.Name == fromProp.Name).FirstOrDefault();
        //    if (toProp == null)
        //    {
        //        throw new ApplicationException("The properties should be the same between types: " + fromProp.Name);
        //    }

        //    toProp.SetValue(to, val, null);

        //    // Exit Function
        //    return true;
        //}

        #endregion

        #endregion
    }

    #region class: PropsByPercent

    /// <summary>
    /// This exposes properties of items in a list
    /// </summary>
    /// <remarks>
    /// This was written so the mutator class could hand a list of dna instances to this.  Then all the properties of all the dna classes
    /// could be accessed using rand.NextDouble(), which can be thought of as a percent.  Each property will have an equal chance
    /// of being accessed.
    /// 
    /// For example, one dna instance could be basic, like:
    ///		Position, Orientation, Scale
    /// 
    /// Another dna instance could have those properties, as well as an array of doubles:
    ///		Position, Orientation, Scale, double[0], double[1], double[2], double[3], double[4]
    /// 
    /// If a dna is randomly chosen, then a property is randomly chosen, the first dna's 3 properties will mutate more often than the
    /// second dna's 8 properties.  This class gives each of the items in that 2nd dna's double array the same chance of being hit as
    /// anything else (it would simply see 11 properties in the list, so if you request the property that is 50% into the list, you will
    /// get item[floor(11*.5)] (the 6th item: dna2's scale)
    /// 
    /// Also, since the mutator may mutate a few properties in one shot, it may not want to mutate the same property twice.  So
    /// PropWrapper implements IEquatable to allow for dupe checking
    /// 
    /// Also, the mutator will do several different types of passes against specific properties.  So this can be set up with filters so only
    /// certain properties are exposed
    /// </remarks>
    public class PropsByPercent : IEnumerable<PropsByPercent.PropWrapper>
    {
        #region enum: DataType

        public enum DataType
        {
            String,
            Double,
            Vector3D,
            Point3D,
            Quaternion,
            Unknown
        }

        #endregion

        #region class: PropWrapper

        /// <summary>
        /// This is a wrapper to one of the items in PropsByPercent._list and an individual property off of that item
        /// </summary>
        public class PropWrapper : IEquatable<PropWrapper>
        {
            #region Declaration Section

            private readonly int _outerIndex;
            private readonly int _propIndex;
            private readonly int _subIndex;

            private readonly PropertyInfo _prop;
            private readonly object _item;

            #endregion

            #region Constructor

            public PropWrapper(int outerIndex, int propIndex, int subIndex, PropertyInfo prop, object item)
            {
                _outerIndex = outerIndex;
                _propIndex = propIndex;
                _subIndex = subIndex;
                _prop = prop;
                _item = item;
            }

            #endregion

            #region IEquatable<PropWrapper> Members

            public bool Equals(PropWrapper other)
            {
                if (_outerIndex != other._outerIndex)
                {
                    return false;
                }
                else if (_propIndex != other._propIndex)
                {
                    return false;
                }
                else if (_subIndex != other._subIndex)
                {
                    return false;
                }

                // No need to compare _item and _prop, those are for get/set.  The three ints are enough to uniquely identify a property

                return true;
            }

            #endregion

            #region Public Properties

            public string DataTypeRaw
            {
                get
                {
                    return _prop.PropertyType.FullName.ToLower();
                }
            }
            public DataType DataType
            {
                get
                {
                    return PropsByPercent.GetDatatype(this.DataTypeRaw);
                }
            }
            public string PropertyName
            {
                get
                {
                    return _prop.Name;
                }
            }

            public object Item
            {
                get
                {
                    return _item;
                }
            }

            #endregion

            #region Public Methods

            public object GetValue()
            {
                //TODO: If PropsByPercent.GetValue needs to raise an event, then it should no longer be a static method, and this wrapper will need to store a reference to PropsByPercent
                return PropsByPercent.GetValue(_prop, _item, _subIndex);
            }
            public void SetValue(object value)
            {
                PropsByPercent.SetValue(_prop, _item, _subIndex, value);
            }

            #endregion
        }

        #endregion
        #region class: FilterArgs

        public class FilterArgs
        {
            #region Public Properties

            // Names
            public string[] IgnoreNames = null;
            public string[] OnlyUseNames = null;

            // Datatype Raw
            public string[] IgnoreTypesRaw = null;
            public string[] OnlyUseTypesRaw = null;

            // DataType
            public DataType[] IgnoreTypes = null;
            public DataType[] OnlyUseTypes = null;

            #endregion

            #region Public Methods

            public Tuple<bool, string> IsValid()
            {
                if (IsPopulated(this.IgnoreNames) && IsPopulated(this.OnlyUseNames))
                {
                    return new Tuple<bool, string>(false, "IgnoreNames and OnlyUseNames can't both be used");
                }

                if (IsPopulated(this.IgnoreTypesRaw) && IsPopulated(this.OnlyUseTypesRaw))
                {
                    return new Tuple<bool, string>(false, "IgnoreTypesRaw and OnlyUseTypesRaw can't both be used");
                }

                if (IsPopulated(this.IgnoreTypes) && IsPopulated(this.OnlyUseTypes))
                {
                    return new Tuple<bool, string>(false, "IgnoreTypes and OnlyUseTypes can't both be used");
                }

                if (IsPopulated(this.OnlyUseTypesRaw) && IsPopulated(this.OnlyUseTypes))
                {
                    return new Tuple<bool, string>(false, "OnlyUseTypesRaw and OnlyUseTypes can't both be used");
                }

                // It's valid
                return new Tuple<bool, string>(true, "");
            }

            public bool IsMatch(PropertyInfo prop)
            {
                //NOTE: This assumes that IsValid has already been called.  I don't want to take the expense of calling it for every property

                // Name
                if (this.IgnoreNames != null && this.IgnoreNames.Contains(prop.Name))		// case sensitive
                {
                    return false;
                }

                if (this.OnlyUseNames != null && !this.OnlyUseNames.Contains(prop.Name))		// case sensitive
                {
                    return false;
                }

                // Datatype Raw
                if (this.IgnoreTypesRaw != null)
                {
                    string type = prop.PropertyType.FullName;
                    if (this.IgnoreTypesRaw.Any(o => o.Equals(type, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                if (this.OnlyUseTypesRaw != null)
                {
                    string type = prop.PropertyType.FullName;
                    if (!this.OnlyUseTypesRaw.Any(o => o.Equals(type, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                // DataType
                if (this.IgnoreTypes != null)
                {
                    DataType type = PropsByPercent.GetDatatype(prop);
                    if (this.IgnoreTypes.Any(o => o == type))
                    {
                        return false;
                    }
                }

                if (this.OnlyUseTypes != null)
                {
                    DataType type = PropsByPercent.GetDatatype(prop);
                    if (!this.OnlyUseTypes.Any(o => o == type))
                    {
                        return false;
                    }
                }

                // All filters passed
                return true;
            }

            #endregion

            #region Private Methods

            private static bool IsPopulated(Array array)
            {
                return array != null && array.Length > 0;
            }

            #endregion
        }

        #endregion

        #region class: ClassTracker

        /// <summary>
        /// This represents a single item in _list
        /// </summary>
        private class ClassTracker
        {
            public ClassTracker(PercentTracker percentage, PropTracker[] props)
            {
                this.Percentage = percentage;
                this.Props = props;
            }

            /// <summary>
            /// This is what percent of the list this class takes up
            /// </summary>
            public readonly PercentTracker Percentage;
            /// <summary>
            /// These are the properties within this class
            /// </summary>
            /// <remarks>
            /// NOTE: This only holds the properties that are cared about (this won't hold properties that are supposed to be ignored)
            /// </remarks>
            public readonly PropTracker[] Props;
        }

        #endregion
        #region class: PropTracker

        private class PropTracker
        {
            public PropTracker(PercentTracker percentage, PropertyInfo property)
            {
                this.Percentage = percentage;
                this.Property = property;
            }

            /// <summary>
            /// This is what percent of the parent class this property takes up
            /// </summary>
            public readonly PercentTracker Percentage;
            /// <summary>
            /// This allows get/set access to the property (as well as name, datatype)
            /// </summary>
            public readonly PropertyInfo Property;
        }

        #endregion
        #region class: PercentTracker

        /// <summary>
        /// This represents a chunk of memory in terms of percentage of its parent.
        /// </summary>
        /// <remarks>
        /// The sum of the percent of each instance of this class at a certain level should always add up to exactly one, ex:
        ///		Tier1 30%
        ///			Tier2 10%
        ///			Tier2 70%
        ///			Tier2 20%
        ///		Tier1 50%
        ///			Tier2 25%
        ///				Tier3 50%
        ///				Tier3 50%
        ///			Tier2 75%
        ///		Tier1 20%
        /// </remarks>
        private class PercentTracker
        {
            public PercentTracker(double from, double size, int count)
            {
                this.From = from;
                this.To = from + size;
                this.Size = size;
                this.Count = count;
            }

            /// <summary>
            /// This is the percent of the containing list that this instance starts at
            /// </summary>
            public readonly double From;
            /// <summary>
            /// This is the percent of the containing list that this instance ends at
            /// NOTE: This instance goes up to, but does not include To (in other words this represents percents that are: From lteq % lt To)
            /// </summary>
            public readonly double To;

            /// <summary>
            /// This is the percent of the size of the parent list that this instance represents
            /// </summary>
            /// <remarks>
            /// For example, if the list has 8 items, and this.Count is 2, then this.Size is .25
            /// </remarks>
            public readonly double Size;
            /// <summary>
            /// This is how many items this instance represents
            /// </summary>
            /// <remarks>
            /// For example, if you want to treat each item in an array as equal weight to a standalone variable, and the array has 5 items,
            /// and this instance represents that array, then Count should be 5, not 1 (the PercentTracker instance that represents that standalone
            /// variable will have a count of 1)
            /// </remarks>
            public readonly int Count;
        }

        #endregion

        #region Events

        //TODO: Expose events that allow a calling class to handle datatypes that this class doesn't know what to do with.  These events
        //would only be raised in the switch default section of the similarly named methods

        //public event CanHandleProp
        //public event GetPropSize
        //public event GetValue
        //public event SetValue

        #endregion

        #region Declaration Section

        public const string PROP_STRING = "system.string";
        public const string PROP_STRINGARRAY = "system.string[]";
        public const string PROP_STRINGJAGGED = "system.string[][]";

        public const string PROP_DOUBLE = "system.double";
        public const string PROP_DOUBLEARRAY = "system.double[]";
        public const string PROP_DOUBLEJAGGED = "system.double[][]";

        public const string PROP_VECTOR3D = "system.windows.media.media3d.vector3d";
        public const string PROP_VECTOR3DARRAY = "system.windows.media.media3d.vector3d[]";
        public const string PROP_VECTOR3DJAGGED = "system.windows.media.media3d.vector3d[][]";

        public const string PROP_POINT3D = "system.windows.media.media3d.point3d";
        public const string PROP_POINT3DARRAY = "system.windows.media.media3d.point3d[]";
        public const string PROP_POINT3DJAGGED = "system.windows.media.media3d.point3d[][]";

        public const string PROP_QUATERNION = "system.windows.media.media3d.quaternion";
        public const string PROP_QUATERNIONARRAY = "system.windows.media.media3d.quaternion[]";
        public const string PROP_QUATERNIONJAGGED = "system.windows.media.media3d.quaternion[][]";

        // These two arrays are the same size
        private readonly object[] _list;		// this is a copy of what was passed in
        private readonly ClassTracker[] _itemsByPercent;		// this gives a breakdown of percents/properties for each item in _list

        #endregion

        #region Constructor

        public PropsByPercent(IEnumerable<object> list)
            : this(list, null) { }
        public PropsByPercent(IEnumerable<object> list, FilterArgs filter)
        {
            // Validate the filter
            if (filter != null && !filter.IsValid().Item1)
            {
                throw new ArgumentException(filter.IsValid().Item2, "filter");		// just call isvalid a 2nd time rather than store the tuple, it's cheap
            }

            // Store the list
            _list = list.ToArray();		// the list will need to have random access, so enumerable is too loose (plus, it could be some lambda that flattens a tree, so make sure it only executes once)

            // Now parse it
            _itemsByPercent = ParseList(_list, filter);
            this.Count = _itemsByPercent.Sum(o => o.Percentage.Count);
        }

        #endregion

        #region IEnumerable Members

        //NOTE: The enumerator only returns the properties that met the filter

        public IEnumerator<PropsByPercent.PropWrapper> GetEnumerator()
        {
            return GetEnumeratorWorker();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumeratorWorker();
        }

        private IEnumerator<PropsByPercent.PropWrapper> GetEnumeratorWorker()
        {
            for (int cntr1 = 0; cntr1 < _list.Length; cntr1++)
            {
                for (int cntr2 = 0; cntr2 < _itemsByPercent[cntr1].Props.Length; cntr2++)
                {
                    for (int cntr3 = 0; cntr3 < _itemsByPercent[cntr1].Props[cntr2].Percentage.Count; cntr3++)
                    {
                        yield return new PropWrapper(cntr1, cntr2, cntr3, _itemsByPercent[cntr1].Props[cntr2].Property, _list[cntr1]);
                    }
                }
            }
        }

        #endregion

        #region Public Properties

        public int Count
        {
            get;
            private set;
        }

        public object[] List => _list;

        #endregion

        #region Public Method

        public PropWrapper GetProperty(double percent)
        {
            if (this.Count == 0)
            {
                return null;
            }

            #region Cleanup %

            if (percent < -.01d)
            {
                return null;
            }
            else if (percent < 0d)
            {
                percent = 0d;
            }

            if (percent > 1d && percent < 1.01d)		// if it's off by more than that, then just let the exception hit
            {
                percent = 1d;
            }

            if (percent == 1d)
            {
                percent -= .000001d;		// can't let percent be exactly one, because the check is always < To
            }

            #endregion

            // Find the item in _list that straddles this percent
            for (int cntr = 0; cntr < _itemsByPercent.Length; cntr++)
            {
                if (_itemsByPercent[cntr].Percentage.Count > 0 && percent < _itemsByPercent[cntr].Percentage.To)
                {
                    // Percent falls somewhere in this item.  Get the percentage into this item
                    double innerPercent = (percent - _itemsByPercent[cntr].Percentage.From) / _itemsByPercent[cntr].Percentage.Size;

                    // Now find the individual property within this item
                    var propIndex = GetPropertySprtInner(_itemsByPercent[cntr].Props, innerPercent);

                    // Exit Function
                    return new PropWrapper(cntr, propIndex.Item1, propIndex.Item2, _itemsByPercent[cntr].Props[propIndex.Item1].Property, _list[cntr]);
                }
            }

            //throw new ApplicationException("Didn't find index: " + percent.ToString());
            return null;
        }

        #endregion

        #region Private Methods

        private static ClassTracker[] ParseList(object[] list, FilterArgs filter)
        {
            Dictionary<Type, PropertyInfo[]> props = new Dictionary<Type, PropertyInfo[]>();

            #region Get the counts

            var counts = new List<Tuple<int, PercentTracker[], Type>>();
            foreach (object item in list)
            {
                Type type = item.GetType();
                if (!props.ContainsKey(type))
                {
                    // Store the properties that should be tracked
                    props.Add
                    (
                        type,
                        type.GetProperties().
                            Where(o => o.CanRead && o.CanWrite && GetDatatype(o) != DataType.Unknown && (filter == null || filter.IsMatch(o))).
                            ToArray()
                    );
                }

                int[] countBreakdown = props[type].
                    Select(o => GetPropSize(o, item)).
                    ToArray();

                counts.Add(new Tuple<int, PercentTracker[], Type>(countBreakdown.Sum(), GetPercents(countBreakdown), type));
            }

            int totalInt = counts.Sum(o => o.Item1);
            bool isTotalZero = totalInt == 0;
            double total = totalInt;

            #endregion

            List<ClassTracker> retVal = new List<ClassTracker>();

            double offset = 0;

            foreach (var count in counts)
            {
                // Join the two arrays together
                PropTracker[] propsCounts = new PropTracker[count.Item2.Length];
                for (int cntr = 0; cntr < count.Item2.Length; cntr++)
                {
                    propsCounts[cntr] = new PropTracker(count.Item2[cntr], props[count.Item3][cntr]);
                }

                double size = isTotalZero ? 0d : count.Item1 / total;

                retVal.Add(new ClassTracker(new PercentTracker(offset, size, count.Item1), propsCounts));

                offset += size;
            }

            return retVal.ToArray();
        }

        private static PercentTracker[] GetPercents(IEnumerable<int> list)
        {
            List<PercentTracker> retVal = new List<PercentTracker>();

            int totalInt = list.Sum();
            bool isTotalZero = totalInt == 0;
            double total = totalInt;

            double offset = 0;

            foreach (int count in list)
            {
                double size = isTotalZero ? 0d : count / total;
                retVal.Add(new PercentTracker(offset, size, count));

                offset += size;
            }

            return retVal.ToArray();
        }

        private static Tuple<int, int> GetPropertySprtInner(PropTracker[] props, double percent)
        {
            for (int cntr = 0; cntr < props.Length; cntr++)
            {
                if (props[cntr].Percentage.Count > 0 && percent < props[cntr].Percentage.To)
                {
                    // Percent falls somewhere in this item.  Get the percentage into this item
                    double innerPercent = (percent - props[cntr].Percentage.From) / props[cntr].Percentage.Size;

                    // Convert that into an index
                    int subIndex = Convert.ToInt32(Math.Floor(props[cntr].Percentage.Count * innerPercent));
                    if (subIndex == props[cntr].Percentage.Count)
                    {
                        subIndex--;		// this should never happen, only if there's a rounding error
                    }

                    // Exit Function
                    return new Tuple<int, int>(cntr, subIndex);
                }
            }

            throw new ApplicationException("Didn't find inner index: " + percent.ToString());
        }

        private static DataType GetDatatype(PropertyInfo prop)
        {
            return GetDatatype(prop.PropertyType.FullName);
        }
        private static DataType GetDatatype(string datatype)
        {
            switch (datatype.ToLower())
            {
                case PROP_STRING:
                case PROP_STRINGARRAY:
                case PROP_STRINGJAGGED:
                    return DataType.String;

                case PROP_DOUBLE:
                case PROP_DOUBLEARRAY:
                case PROP_DOUBLEJAGGED:
                    return DataType.Double;

                case PROP_VECTOR3D:
                case PROP_VECTOR3DARRAY:
                case PROP_VECTOR3DJAGGED:
                    return DataType.Vector3D;

                case PROP_POINT3D:
                case PROP_POINT3DARRAY:
                case PROP_POINT3DJAGGED:
                    return DataType.Point3D;

                case PROP_QUATERNION:
                case PROP_QUATERNIONARRAY:
                case PROP_QUATERNIONJAGGED:
                    return DataType.Quaternion;

                default:
                    return DataType.Unknown;
            }
        }
        private static int GetPropSize(PropertyInfo prop, object item)
        {
            object val = null;

            string name = prop.PropertyType.FullName.ToLower();
            switch (name)
            {
                case PROP_STRING:
                case PROP_DOUBLE:
                case PROP_VECTOR3D:
                case PROP_POINT3D:
                case PROP_QUATERNION:
                    return 1;

                case PROP_STRINGARRAY:
                case PROP_DOUBLEARRAY:
                case PROP_VECTOR3DARRAY:
                case PROP_POINT3DARRAY:
                case PROP_QUATERNIONARRAY:
                    val = prop.GetValue(item, null);
                    return val == null ? 0 : ((Array)val).Length;

                case PROP_STRINGJAGGED:
                case PROP_DOUBLEJAGGED:
                case PROP_VECTOR3DJAGGED:
                case PROP_POINT3DJAGGED:
                case PROP_QUATERNIONJAGGED:
                    #region jagged

                    val = prop.GetValue(item, null);

                    int retVal = 0;
                    if (val != null)
                    {
                        Array[] valCast = (Array[])val;
                        for (int cntr = 0; cntr < valCast.Length; cntr++)
                        {
                            retVal += valCast.GetValue(cntr) == null ? 0 : ((Array)valCast.GetValue(cntr)).Length;
                        }
                    }

                    return retVal;

                #endregion

                default:
                    return 0;
            }
        }
        private static object GetValue(PropertyInfo prop, object item, int subIndex)
        {
            string name = prop.PropertyType.FullName.ToLower();
            switch (name)
            {
                case PROP_STRING:
                case PROP_DOUBLE:
                case PROP_VECTOR3D:
                case PROP_POINT3D:
                case PROP_QUATERNION:
                    return prop.GetValue(item, null);

                case PROP_STRINGARRAY:
                case PROP_DOUBLEARRAY:
                case PROP_VECTOR3DARRAY:
                case PROP_POINT3DARRAY:
                case PROP_QUATERNIONARRAY:
                    Array strArr = (Array)prop.GetValue(item, null);
                    return strArr.GetValue(subIndex);

                case PROP_STRINGJAGGED:
                case PROP_DOUBLEJAGGED:
                case PROP_VECTOR3DJAGGED:
                case PROP_POINT3DJAGGED:
                case PROP_QUATERNIONJAGGED:
                    #region jagged

                    Array[] jaggedArr = (Array[])prop.GetValue(item, null);

                    Tuple<int, int> jIndex = GetJaggedIndex(jaggedArr, subIndex);
                    return ((Array)jaggedArr.GetValue(jIndex.Item1)).GetValue(jIndex.Item2);

                #endregion

                default:
                    throw new ApplicationException("Unexpected property: " + name);
            }
        }
        private static void SetValue(PropertyInfo prop, object item, int subIndex, object value)
        {
            string name = prop.PropertyType.FullName.ToLower();
            switch (name)
            {
                case PROP_STRING:
                case PROP_DOUBLE:
                case PROP_VECTOR3D:
                case PROP_POINT3D:
                case PROP_QUATERNION:
                    prop.SetValue(item, value, null);
                    break;

                case PROP_STRINGARRAY:
                case PROP_DOUBLEARRAY:
                case PROP_VECTOR3DARRAY:
                case PROP_POINT3DARRAY:
                case PROP_QUATERNIONARRAY:
                    #region array

                    Array strArr = (Array)prop.GetValue(item, null);
                    strArr.SetValue(value, subIndex);
                    prop.SetValue(item, strArr, null);		//NOTE: Technically, the array is now already modified, so there is no reason to store the array back into the class.  But it feels cleaner to do this (and will throw an exception if that property is readonly)

                    #endregion
                    break;

                case PROP_STRINGJAGGED:
                case PROP_DOUBLEJAGGED:
                case PROP_VECTOR3DJAGGED:
                case PROP_POINT3DJAGGED:
                case PROP_QUATERNIONJAGGED:
                    #region jagged

                    Array[] jaggedArr = (Array[])prop.GetValue(item, null);

                    Tuple<int, int> jIndex = GetJaggedIndex(jaggedArr, subIndex);
                    ((Array)jaggedArr.GetValue(jIndex.Item1)).SetValue(value, jIndex.Item2);

                    prop.SetValue(item, jaggedArr, null);

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unexpected property: " + name);
            }
        }

        private static Tuple<int, int> GetJaggedIndex(IEnumerable<Array> jagged, int index)
        {
            int used = 0;
            int outer = -1;

            foreach (Array arr in jagged)
            {
                outer++;

                if (arr == null || arr.Length == 0)
                {
                    continue;
                }

                if (used + arr.Length > index)
                {
                    return new Tuple<int, int>(outer, index - used);
                }

                used += arr.Length;
            }

            throw new ApplicationException("The index passed in is larger than the jagged array");
        }

        #endregion
    }

    #endregion
}
