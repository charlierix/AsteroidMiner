using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems
{
    //TODO: Need two more parts.  InvalidPart, UnknownPart (maybe just need the one)

    /// <summary>
    /// Instantiating a bot got complex, so this holds the logic that instantiates/links parts
    /// </summary>
    public static class BotConstructor
    {
        #region Declaration Section

        private static Lazy<BezierMesh> _linkEstimateMesh = new Lazy<BezierMesh>(() => CreateLinkEstimateMesh());

        private static Lazy<Type[]> _shipPartTypes = new Lazy<Type[]>(() => GetShipPartTypes());

        #endregion

        //TODO: Make two overloads.  One that returns a task, and one that does everything on one thread
        //It wouldn't do much good.  The two most expensive methods are InstantiateParts_Standard, SeparateParts - instantiate takes twice as long as separate).  Both must run in the calling thread
        public static BotConstruction_Result ConstructBot(IEnumerable<ShipPartDNA> dna, ShipCoreArgs core, ShipExtraArgs extra = null, BotConstructor_Events events = null)
        {
            ShipDNA shipDNA = ShipDNA.Create(dna);
            return ConstructBot(shipDNA, core, extra, events);
        }
        public static BotConstruction_Result ConstructBot(ShipDNA dna, ShipCoreArgs core, ShipExtraArgs extra = null, BotConstructor_Events events = null)
        {
            // Fix args
            FixArgs(ref extra, ref events);

            // Discard parts
            Tuple<ShipPartDNA[], ShipPartDNA[]> validatedParts = DiscardParts(dna, events.ShouldDiscardPart);

            // Instantiate Parts
            BotConstruction_Parts parts = new BotConstruction_Parts();

            IEnumerable<ShipPartDNA> untouchedParts = InstantiateParts_Containers(validatedParts.Item1, parts, core, extra, events.InstantiateUnknownPart_Container, events.InstantiateUnknownPart_ContainerGroups);

            untouchedParts = InstantiateParts_Standard(untouchedParts, parts, core, extra, events.InstantiateUnknownPart_Standard);

            InstantiateParts_Post(untouchedParts, parts, core, extra, events.InstantiateUnknownPart_Standard);

            //TODO: The discarded parts should still be part of the bot, just not active.  Make a special part that is an inactive lump,
            //and will hold the dna (future generations may mutate the dna enough to make it valid again)
            //InstantiateParts_Discarded(validatedParts.Item2);

            parts.AllPartsArray = parts.AllParts.Select(o => o.Item1).ToArray();

            // Separate parts (don't allow parts to intersect each other)
            //NOTE: This must be done after linking parts together (or the linker would get wild results)
            Tuple<ShipPartDNA[], CollisionHull[], bool> dna_hulls = SeparateParts(parts.AllPartsArray, parts.AllParts.Select(o => o.Item2).ToArray(), extra.RepairPartPositions, core.World);

            if (dna_hulls.Item3)
            {
                #region recurse

                foreach (CollisionHull hull in dna_hulls.Item2)
                {
                    hull.Dispose();
                }

                // Recurse, but don't pull apart
                extra.RepairPartPositions = false;      // I was going to clone the extra, but that's a lot of work

                BotConstruction_Result recursed;
                try
                {
                    recursed = ConstructBot(ShipDNA.Create(dna, dna_hulls.Item1), core, extra, events);
                }
                finally
                {
                    extra.RepairPartPositions = true;       // put it back (execution won't get here unless it was originally true)
                }

                #endregion
                return recursed;
            }

            // These are parts that need to have Update called each tick
            IPartUpdatable[] updatable = parts.AllPartsArray.Where(o => o is IPartUpdatable).Select(o => (IPartUpdatable)o).ToArray();
            IPartUpdatable[] updatableParts_MainThread = updatable.Where(o => o.IntervalSkips_MainThread != null).ToArray();
            IPartUpdatable[] updatableParts_AnyThread = updatable.Where(o => o.IntervalSkips_AnyThread != null).ToArray();

            // For now, just have one global watcher.  In the future, may want localized, or specialized
            parts.LifeEventWatcher = new LifeEventWatcher(parts.Containers);

            // Post instantiate link
            LinkParts_NonNeural(parts, core, extra, events.LinkParts_NonNeural);

            // Link Neural
            NeuralUtility.ContainerOutput[] links = LinkNeural(extra, parts);

            // Rebuild AllParts just in case something will use it later (the parts construction class will be stored in the Bot class)
            parts.AllParts = Enumerable.Range(0, parts.AllParts.Count).
                Select(o => Tuple.Create(parts.AllParts[o].Item1, dna_hulls.Item1[o])).
                ToList();

            // WPF
            Tuple<Model3DGroup, ModelVisual3D> visual = GetWPFMain(parts.AllPartsArray);
            Ship.VisualEffects visualEffects = GetWPFEffects(parts, extra);

            // Physics Body
            Body physicsBody = GetPhysicsBody(parts.AllPartsArray, dna_hulls.Item1, dna_hulls.Item2, visual.Item2, visualEffects, core, extra);

            // Calculate radius
            Point3D aabbMin, aabbMax;
            physicsBody.GetAABB(out aabbMin, out aabbMax);
            double radius = (aabbMax - aabbMin).Length / 2d;

            // Exit Function
            return new BotConstruction_Result()
            {
                ArgsCore = core,
                ArgsExtra = extra,

                Parts = parts.AllPartsArray,
                DNAParts = dna_hulls.Item1,
                DNA = ShipDNA.Create(dna, dna_hulls.Item1),

                PartConstruction = parts,

                UpdatableParts_MainThread = updatableParts_MainThread,
                UpdatableParts_AnyThread = updatableParts_AnyThread,

                Links = links,

                Model = visual.Item1,
                VisualEffects = visualEffects,

                PhysicsBody = physicsBody,

                Radius = radius,
            };
        }

        /// <summary>
        /// This is a helper method to convert dna into a design
        /// </summary>
        /// <remarks>
        /// This class may not be the best place for this method, but I wanted the switch statements near each other (easier to remember
        /// to update both)
        /// </remarks>
        public static PartDesignBase GetPartDesign(ShipPartDNA dna, EditorOptions options, bool isFinalModel)
        {
            PartDesignBase retVal;

            switch (dna.PartType)
            {
                case AmmoBox.PARTTYPE:
                    retVal = new AmmoBoxDesign(options, isFinalModel);
                    break;

                case FuelTank.PARTTYPE:
                    retVal = new FuelTankDesign(options, isFinalModel);
                    break;

                case EnergyTank.PARTTYPE:
                    retVal = new EnergyTankDesign(options, isFinalModel);
                    break;

                case PlasmaTank.PARTTYPE:
                    retVal = new PlasmaTankDesign(options, isFinalModel);
                    break;

                case CargoBay.PARTTYPE:
                    retVal = new CargoBayDesign(options, isFinalModel);
                    break;

                case ConverterMatterToFuel.PARTTYPE:
                    retVal = new ConverterMatterToFuelDesign(options, isFinalModel);
                    break;

                case ConverterMatterToEnergy.PARTTYPE:
                    retVal = new ConverterMatterToEnergyDesign(options, isFinalModel);
                    break;

                case ConverterMatterToPlasma.PARTTYPE:
                    retVal = new ConverterMatterToPlasmaDesign(options, isFinalModel);
                    break;

                case ConverterMatterToAmmo.PARTTYPE:
                    retVal = new ConverterMatterToAmmoDesign(options, isFinalModel);
                    break;

                case ConverterEnergyToAmmo.PARTTYPE:
                    retVal = new ConverterEnergyToAmmoDesign(options, isFinalModel);
                    break;

                case ConverterEnergyToFuel.PARTTYPE:
                    retVal = new ConverterEnergyToFuelDesign(options, isFinalModel);
                    break;

                case ConverterEnergyToPlasma.PARTTYPE:
                    retVal = new ConverterEnergyToPlasmaDesign(options, isFinalModel);
                    break;

                case ConverterFuelToEnergy.PARTTYPE:
                    retVal = new ConverterFuelToEnergyDesign(options, isFinalModel);
                    break;

                case ConverterRadiationToEnergy.PARTTYPE:
                    ConverterRadiationToEnergyDNA dnaCon = (ConverterRadiationToEnergyDNA)dna;
                    retVal = new ConverterRadiationToEnergyDesign(options, isFinalModel, dnaCon.Shape);
                    break;

                case Thruster.PARTTYPE:
                    ThrusterDNA dnaThrust = (ThrusterDNA)dna;
                    if (dnaThrust.ThrusterType == ThrusterType.Custom)
                        retVal = new ThrusterDesign(options, isFinalModel, dnaThrust.ThrusterDirections);
                    else
                        retVal = new ThrusterDesign(options, isFinalModel, dnaThrust.ThrusterType);
                    break;

                case TractorBeam.PARTTYPE:
                    retVal = new TractorBeamDesign(options, isFinalModel);
                    break;

                case ImpulseEngine.PARTTYPE:
                    ImpulseEngineDNA dnaImpulse = (ImpulseEngineDNA)dna;
                    retVal = new ImpulseEngineDesign(options, isFinalModel, dnaImpulse.ImpulseEngineType);
                    break;

                case Brain.PARTTYPE:
                    retVal = new BrainDesign(options, isFinalModel);
                    break;

                case BrainNEAT.PARTTYPE:
                    retVal = new BrainNEATDesign(options, isFinalModel);
                    break;

                case BrainRGBRecognizer.PARTTYPE:
                    retVal = new BrainRGBRecognizerDesign(options, isFinalModel);
                    break;

                case DirectionControllerRing.PARTTYPE:
                    retVal = new DirectionControllerRingDesign(options, isFinalModel);
                    break;

                case DirectionControllerSphere.PARTTYPE:
                    retVal = new DirectionControllerSphereDesign(options, isFinalModel);
                    break;

                case SensorGravity.PARTTYPE:
                    retVal = new SensorGravityDesign(options, isFinalModel);
                    break;

                case SensorSpin.PARTTYPE:
                    retVal = new SensorSpinDesign(options, isFinalModel);
                    break;

                case SensorVelocity.PARTTYPE:
                    retVal = new SensorVelocityDesign(options, isFinalModel);
                    break;

                case SensorRadiation.PARTTYPE:
                    retVal = new SensorRadiationDesign(options, isFinalModel);
                    break;

                case SensorTractor.PARTTYPE:
                    retVal = new SensorTractorDesign(options, isFinalModel);
                    break;

                case SensorCollision.PARTTYPE:
                    retVal = new SensorCollisionDesign(options, isFinalModel);
                    break;

                case SensorFluid.PARTTYPE:
                    retVal = new SensorFluidDesign(options, isFinalModel);
                    break;

                case SensorInternalForce.PARTTYPE:
                    retVal = new SensorInternalForceDesign(options, isFinalModel);
                    break;

                case SensorNetForce.PARTTYPE:
                    retVal = new SensorNetForceDesign(options, isFinalModel);
                    break;

                case SensorHoming.PARTTYPE:
                    retVal = new SensorHomingDesign(options, isFinalModel);
                    break;

                case CameraHardCoded.PARTTYPE:
                    retVal = new CameraHardCodedDesign(options, isFinalModel);
                    break;

                case CameraColorRGB.PARTTYPE:
                    retVal = new CameraColorRGBDesign(options, isFinalModel);
                    break;

                case ProjectileGun.PARTTYPE:
                    retVal = new ProjectileGunDesign(options, isFinalModel);
                    break;

                case BeamGun.PARTTYPE:
                    retVal = new BeamGunDesign(options, isFinalModel);
                    break;

                case GrappleGun.PARTTYPE:
                    retVal = new GrappleGunDesign(options, isFinalModel);
                    break;

                case ShieldEnergy.PARTTYPE:
                    retVal = new ShieldEnergyDesign(options, isFinalModel);
                    break;

                case ShieldKinetic.PARTTYPE:
                    retVal = new ShieldKineticDesign(options, isFinalModel);
                    break;

                case ShieldTractor.PARTTYPE:
                    retVal = new ShieldTractorDesign(options, isFinalModel);
                    break;

                case SwarmBay.PARTTYPE:
                    retVal = new SwarmBayDesign(options, isFinalModel);
                    break;

                case HangarBay.PARTTYPE:
                    retVal = new HangarBayDesign(options, isFinalModel);
                    break;

                case SelfRepair.PARTTYPE:
                    retVal = new SelfRepairDesign(options, isFinalModel);
                    break;

                default:
                    throw new ApplicationException("Unknown part type: " + dna.PartType);
            }

            retVal.SetDNA(dna);

            return retVal;
        }

        /// <summary>
        /// This is a convenience that uses reflection to get all ship parts
        /// </summary>
        /// <remarks>
        /// This is for cases like having price fluctuations of each part type
        /// </remarks>
        public static Lazy<string[]> AllPartTypes = new Lazy<string[]>(() => GetAllPartTypes());

        //public static Lazy<PartToolItemBase[]> AllPartToolItems = new Lazy<PartToolItemBase[]>(() => GetAllPartToolItems());

        #region profiled

        //public static Tuple<BotConstruction_Result, string> ConstructBot_Profile(ShipDNA dna, ShipCoreArgs core, ShipExtraArgs extra = null, BotConstructor_Events events = null)
        //{
        //    var stopwatch = new System.Diagnostics.Stopwatch();
        //    StringBuilder report = new StringBuilder();

        //    // Fix args
        //    stopwatch.Restart();
        //    FixArgs(ref extra, ref events);
        //    stopwatch.Stop();
        //    report.AppendLine("FixArgs\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    // Discard parts
        //    stopwatch.Restart();
        //    Tuple<ShipPartDNA[], ShipPartDNA[]> validatedParts = DiscardParts(dna, events.ShouldDiscardPart);
        //    stopwatch.Stop();
        //    report.AppendLine("DiscardParts\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    // Instantiate Parts
        //    BotConstruction_Parts parts = new BotConstruction_Parts();

        //    stopwatch.Restart();
        //    IEnumerable<ShipPartDNA> untouchedParts = InstantiateParts_Containers(validatedParts.Item1, parts, core, extra, events.InstantiateUnknownPart_Container, events.InstantiateUnknownPart_ContainerGroups);
        //    stopwatch.Stop();
        //    report.AppendLine("InstantiateParts_Containers\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    //TODO: May want an extra pass between containers and standard (parts that use containers, but need to be handed to child parts)
        //    //or if that gets out of hand, have a way to know dependencies --- maybe the get of all parts of type will ensure that all parts of that type are instantiated first

        //    stopwatch.Restart();
        //    InstantiateParts_Standard(untouchedParts, parts, core, extra, events.InstantiateUnknownPart_Standard);
        //    stopwatch.Stop();
        //    report.AppendLine("InstantiateParts_Standard\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    //TODO: The discarded parts should still be part of the bot, just not active.  Make a special part that is an inactive lump,
        //    //and will hold the dna (future generations may mutate the dna enough to make it valid again)
        //    //InstantiateParts_Discarded(validatedParts.Item2);

        //    parts.AllPartsArray = parts.AllParts.Select(o => o.Item1).ToArray();

        //    // These are parts that need to have Update called each tick
        //    IPartUpdatable[] updatable = parts.AllPartsArray.Where(o => o is IPartUpdatable).Select(o => (IPartUpdatable)o).ToArray();
        //    IPartUpdatable[] updatableParts_MainThread = updatable.Where(o => o.IntervalSkips_MainThread != null).ToArray();
        //    IPartUpdatable[] updatableParts_AnyThread = updatable.Where(o => o.IntervalSkips_AnyThread != null).ToArray();

        //    // Post instantiate link
        //    stopwatch.Restart();
        //    LinkParts_NonNeural(parts, core, extra, events.LinkParts_NonNeural);
        //    stopwatch.Stop();
        //    report.AppendLine("LinkParts_NonNeural\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    // Link Neural
        //    stopwatch.Restart();
        //    NeuralUtility.ContainerOutput[] links = LinkNeural(extra, parts);
        //    stopwatch.Stop();
        //    report.AppendLine("LinkNeural\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    // Separate parts (don't allow parts to intersect each other)
        //    //NOTE: This must be done after linking parts together (or the linker would get wild results)
        //    stopwatch.Restart();
        //    Tuple<ShipPartDNA[], CollisionHull[]> dna_hulls = SeparateParts(parts.AllPartsArray, parts.AllParts.Select(o => o.Item2).ToArray(), extra.RepairPartPositions, core.World);
        //    stopwatch.Stop();
        //    report.AppendLine("SeparateParts\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    // Rebuild AllParts just in case something will use it later (the parts construction class will be stored in the Bot class)
        //    parts.AllParts = Enumerable.Range(0, parts.AllParts.Count).
        //        Select(o => Tuple.Create(parts.AllParts[o].Item1, dna_hulls.Item1[o])).
        //        ToList();

        //    // WPF
        //    stopwatch.Restart();
        //    Tuple<Model3DGroup, ModelVisual3D> visual = GetWPFMain(parts.AllPartsArray);
        //    Ship.VisualEffects visualEffects = GetWPFEffects(parts, extra);
        //    stopwatch.Stop();
        //    report.AppendLine("GetWPFEffects\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    // Physics Body
        //    stopwatch.Restart();
        //    Body physicsBody = GetPhysicsBody(parts.AllPartsArray, dna_hulls.Item1, dna_hulls.Item2, visual.Item2, visualEffects, core, extra);
        //    stopwatch.Stop();
        //    report.AppendLine("GetPhysicsBody\t" + stopwatch.ElapsedMilliseconds.ToString("N0"));

        //    // Calculate radius
        //    Point3D aabbMin, aabbMax;
        //    physicsBody.GetAABB(out aabbMin, out aabbMax);
        //    double radius = (aabbMax - aabbMin).Length / 2d;

        //    // Exit Function
        //    var result = new BotConstruction_Result()
        //    {
        //        ArgsCore = core,
        //        ArgsExtra = extra,

        //        Parts = parts.AllPartsArray,
        //        DNAParts = dna_hulls.Item1,
        //        DNA = ShipDNA.Create(dna, dna_hulls.Item1),

        //        PartConstruction = parts,

        //        UpdatableParts_MainThread = updatableParts_MainThread,
        //        UpdatableParts_AnyThread = updatableParts_AnyThread,

        //        Links = links,

        //        Model = visual.Item1,
        //        VisualEffects = visualEffects,

        //        PhysicsBody = physicsBody,

        //        Radius = radius,
        //    };

        //    return Tuple.Create(result, report.ToString());
        //}

        #endregion

        #region Private Methods - instantiate

        /// <summary>
        /// Containers get built first.  Most of the standard parts need at least one container group to function
        /// </summary>
        private static IEnumerable<ShipPartDNA> InstantiateParts_Containers(ShipPartDNA[] parts, BotConstruction_Parts building, ShipCoreArgs core, ShipExtraArgs extra, Func<ShipPartDNA, ShipCoreArgs, ShipExtraArgs, PartBase> instantiateUnknown, Func<BotConstruction_Containers, object[]> instantiateUnknownGroups)
        {
            List<ShipPartDNA> untouchedParts = new List<ShipPartDNA>();

            BotConstruction_Containers containers = new BotConstruction_Containers();
            building.Containers = containers;

            foreach (ShipPartDNA dna in parts)
            {
                switch (dna.PartType)
                {
                    case AmmoBox.PARTTYPE:
                        AddPart(new AmmoBox(extra.Options, extra.ItemOptions, dna),
                            dna, containers.Ammos, building.AllParts);
                        break;

                    case FuelTank.PARTTYPE:
                        AddPart(new FuelTank(extra.Options, extra.ItemOptions, dna),
                            dna, containers.Fuels, building.AllParts);
                        break;

                    case EnergyTank.PARTTYPE:
                        AddPart(new EnergyTank(extra.Options, extra.ItemOptions, dna),
                            dna, containers.Energies, building.AllParts);
                        break;

                    case PlasmaTank.PARTTYPE:
                        AddPart(new PlasmaTank(extra.Options, extra.ItemOptions, dna),
                            dna, containers.Plasmas, building.AllParts);
                        break;

                    case CargoBay.PARTTYPE:
                        AddPart(new CargoBay(extra.Options, extra.ItemOptions, dna),
                            dna, containers.CargoBays, building.AllParts);
                        break;

                    default:
                        PartBase customPart = null;
                        if (instantiateUnknown != null)
                        {
                            // Call a delegate
                            customPart = instantiateUnknown(dna, core, extra);
                        }

                        if (customPart != null)
                        {
                            AddPart(customPart, dna, containers.CustomContainers, building.AllParts);
                        }
                        else
                        {
                            untouchedParts.Add(dna);
                        }
                        break;
                }
            }

            //NOTE: The parts can handle being handed a null container.  It doesn't add much value to have parts that are dead weight, but I don't want to
            //penalize a design for having thrusters, but no fuel tank.  Maybe descendants will develop fuel tanks and be a winning design

            // Build groups
            BuildContainerGroup(out containers.FuelGroup, containers.Fuels, Math3D.GetCenter(containers.Fuels.Select(o => o.Position).ToArray()));
            BuildContainerGroup(out containers.EnergyGroup, containers.Energies, Math3D.GetCenter(containers.Energies.Select(o => o.Position).ToArray()));
            BuildContainerGroup(out containers.PlasmaGroup, containers.Plasmas, Math3D.GetCenter(containers.Plasmas.Select(o => o.Position).ToArray()));
            BuildContainerGroup(out containers.AmmoGroup, containers.Ammos, Math3D.GetCenter(containers.Ammos.Select(o => o.Position).ToArray()), ContainerGroup.ContainerOwnershipType.QuantitiesCanChange);
            BuildContainerGroup(out containers.CargoBayGroup, containers.CargoBays);

            if (containers.CustomContainers.Count > 0)
            {
                // Call a delegate to create custom container groups
                containers.CustomContainerGroups = instantiateUnknownGroups(containers);
            }

            return untouchedParts;
        }
        /// <summary>
        /// Standard parts should be the majority of the parts.  They only rely on containers
        /// </summary>
        private static IEnumerable<ShipPartDNA> InstantiateParts_Standard(IEnumerable<ShipPartDNA> parts, BotConstruction_Parts building, ShipCoreArgs core, ShipExtraArgs extra, Func<ShipPartDNA, ShipCoreArgs, ShipExtraArgs, BotConstruction_Containers, PartBase> instantiateUnknown)
        {
            List<ShipPartDNA> untouchedParts = new List<ShipPartDNA>();

            EditorOptions options = extra.Options;
            ItemOptions itemOptions = extra.ItemOptions;
            BotConstruction_Containers containers = building.Containers;

            SortedList<string, List<PartBase>> standard = new SortedList<string, List<PartBase>>();
            building.StandardParts = standard;

            foreach (ShipPartDNA dna in parts)
            {
                switch (dna.PartType)
                {
                    case ConverterMatterToFuel.PARTTYPE:
                        AddPart(new ConverterMatterToFuel(options, itemOptions, dna, containers.FuelGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterMatterToEnergy.PARTTYPE:
                        AddPart(new ConverterMatterToEnergy(options, itemOptions, dna, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterMatterToPlasma.PARTTYPE:
                        AddPart(new ConverterMatterToPlasma(options, itemOptions, dna, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterMatterToAmmo.PARTTYPE:
                        AddPart(new ConverterMatterToAmmo(options, itemOptions, dna, containers.AmmoGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterEnergyToAmmo.PARTTYPE:
                        AddPart(new ConverterEnergyToAmmo(options, itemOptions, dna, containers.EnergyGroup, containers.AmmoGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterEnergyToFuel.PARTTYPE:
                        AddPart(new ConverterEnergyToFuel(options, itemOptions, dna, containers.EnergyGroup, containers.FuelGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterEnergyToPlasma.PARTTYPE:
                        AddPart(new ConverterEnergyToPlasma(options, itemOptions, dna, containers.EnergyGroup, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterFuelToEnergy.PARTTYPE:
                        AddPart(new ConverterFuelToEnergy(options, itemOptions, dna, containers.FuelGroup, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ConverterRadiationToEnergy.PARTTYPE:
                        AddPart(new ConverterRadiationToEnergy(options, itemOptions, (ConverterRadiationToEnergyDNA)dna, containers.EnergyGroup, extra.Radiation),
                            dna, standard, building.AllParts);
                        break;

                    case Thruster.PARTTYPE:
                        AddPart(new Thruster(options, itemOptions, (ThrusterDNA)dna, containers.FuelGroup),
                            dna, standard, building.AllParts);
                        break;

                    case TractorBeam.PARTTYPE:
                        AddPart(new TractorBeam(options, itemOptions, dna, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ImpulseEngine.PARTTYPE:
                        AddPart(new ImpulseEngine(options, itemOptions, (ImpulseEngineDNA)dna, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case Brain.PARTTYPE:
                        AddPart(new Brain(options, itemOptions, dna, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case BrainNEAT.PARTTYPE:
                        AddPart(new BrainNEAT(options, itemOptions, (BrainNEATDNA)dna, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case BrainRGBRecognizer.PARTTYPE:
                        AddPart(new BrainRGBRecognizer(options, itemOptions, (BrainRGBRecognizerDNA)dna, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case SensorGravity.PARTTYPE:
                        AddPart(new SensorGravity(options, itemOptions, dna, containers.EnergyGroup, extra.Gravity),
                            dna, standard, building.AllParts);
                        break;

                    case SensorSpin.PARTTYPE:
                        AddPart(new SensorSpin(options, itemOptions, dna, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case SensorVelocity.PARTTYPE:
                        AddPart(new SensorVelocity(options, itemOptions, dna, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case SensorHoming.PARTTYPE:
                        AddPart(new SensorHoming(options, itemOptions, dna, core.Map, containers.EnergyGroup),
                            dna, standard, building.AllParts);
                        break;

                    case CameraColorRGB.PARTTYPE:
                        AddPart(new CameraColorRGB(options, itemOptions, dna, containers.EnergyGroup, extra.CameraPool),
                            dna, standard, building.AllParts);
                        break;

                    case CameraHardCoded.PARTTYPE:
                        AddPart(new CameraHardCoded(options, itemOptions, dna, containers.EnergyGroup, core.Map),
                            dna, standard, building.AllParts);
                        break;

                    case ProjectileGun.PARTTYPE:
                        AddPart(new ProjectileGun(options, itemOptions, dna, core.Map, core.World, extra.Material_Projectile),
                            dna, standard, building.AllParts);
                        break;

                    case BeamGun.PARTTYPE:
                        AddPart(new BeamGun(options, itemOptions, dna, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ShieldEnergy.PARTTYPE:
                        AddPart(new ShieldEnergy(options, itemOptions, dna, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ShieldKinetic.PARTTYPE:
                        AddPart(new ShieldKinetic(options, itemOptions, dna, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case ShieldTractor.PARTTYPE:
                        AddPart(new ShieldTractor(options, itemOptions, dna, containers.PlasmaGroup),
                            dna, standard, building.AllParts);
                        break;

                    case SwarmBay.PARTTYPE:
                        AddPart(new SwarmBay(options, itemOptions, dna, core.Map, core.World, extra.Material_SwarmBot, containers.PlasmaGroup, extra.SwarmObjectiveStrokes),
                            dna, standard, building.AllParts);
                        break;

                    default:
                        PartBase customPart = null;
                        if (instantiateUnknown != null)
                        {
                            // Call a delegate
                            customPart = instantiateUnknown(dna, core, extra, containers);
                        }

                        if (customPart != null)
                        {
                            AddPart(customPart, dna, standard, building.AllParts);
                        }
                        else
                        {
                            untouchedParts.Add(dna);
                        }
                        break;
                }
            }

            return untouchedParts;
        }
        /// <summary>
        /// This is for parts that need access to some of the standard parts
        /// NOTE: These parts get added to the standard list
        /// </summary>
        private static void InstantiateParts_Post(IEnumerable<ShipPartDNA> parts, BotConstruction_Parts building, ShipCoreArgs core, ShipExtraArgs extra, Func<ShipPartDNA, ShipCoreArgs, ShipExtraArgs, BotConstruction_Containers, PartBase> instantiateUnknown)
        {
            EditorOptions options = extra.Options;
            ItemOptions itemOptions = extra.ItemOptions;
            BotConstruction_Containers containers = building.Containers;

            var standard = building.StandardParts;

            foreach (ShipPartDNA dna in parts)
            {
                switch (dna.PartType)
                {
                    case DirectionControllerRing.PARTTYPE:
                        AddPart(new DirectionControllerRing(options, itemOptions, dna, containers.EnergyGroup, FindParts<Thruster>(standard), FindParts<ImpulseEngine>(standard)),
                            dna, standard, building.AllParts);
                        break;

                    case DirectionControllerSphere.PARTTYPE:
                        AddPart(new DirectionControllerSphere(options, itemOptions, dna, containers.EnergyGroup, FindParts<Thruster>(standard), FindParts<ImpulseEngine>(standard)),
                            dna, standard, building.AllParts);
                        break;

                    default:
                        PartBase customPart = null;
                        if (instantiateUnknown != null)
                        {
                            // Call a delegate
                            customPart = instantiateUnknown(dna, core, extra, containers);
                        }

                        if (customPart != null)
                        {
                            AddPart(customPart, dna, standard, building.AllParts);
                        }
                        else
                        {
                            //TODO: Make an unknown part that is a small dense sphere.  This way a bot could still be used in environments
                            //that don't know about the custom parts
                            throw new ApplicationException("Unknown dna.PartType: " + dna.PartType);
                        }
                        break;
                }
            }
        }
        private static void InstantiateParts_Discarded(ShipPartDNA[] parts)
        {
        }

        private static T[] FindParts<T>(SortedList<string, List<PartBase>> parts) where T : PartBase
        {
            //NOTE: The sorted list has the desired type grouped up, but they would have to pass PARTTYPE as well.  This method isn't called very often, so just brute force it
            return parts.
                SelectMany(o => o.Value).
                Where(o => o is T).
                Select(o => o as T).
                ToArray();
        }

        private static void AddPart<T>(T item, ShipPartDNA dna, List<T> specificList, List<Tuple<PartBase, ShipPartDNA>> combinedList) where T : PartBase
        {
            // This is just a helper method so one call adds to two lists
            specificList.Add(item);
            combinedList.Add(new Tuple<PartBase, ShipPartDNA>(item, dna));
        }
        private static void AddPart(PartBase item, ShipPartDNA dna, SortedList<string, List<PartBase>> partsByType, List<Tuple<PartBase, ShipPartDNA>> combinedList)
        {
            // Parts by type
            List<PartBase> partsOfType;
            if (!partsByType.TryGetValue(dna.PartType, out partsOfType))
            {
                partsOfType = new List<PartBase>();
                partsByType.Add(dna.PartType, partsOfType);
            }

            partsOfType.Add(item);

            // Combined
            combinedList.Add(new Tuple<PartBase, ShipPartDNA>(item, dna));
        }

        private static void BuildContainerGroup(out IContainer containerGroup, IEnumerable<IContainer> containers, Point3D center, ContainerGroup.ContainerOwnershipType? ownerType = null)
        {
            containerGroup = null;

            int count = containers.Count();

            if (count == 1)
            {
                containerGroup = containers.First();
            }
            else if (count > 1)
            {
                ContainerGroup group = new ContainerGroup();
                group.Ownership = ownerType ?? ContainerGroup.ContainerOwnershipType.GroupIsSoleOwner;		// this is the most efficient option
                foreach (IContainer container in containers)
                {
                    group.AddContainer(container);
                }

                containerGroup = group;
            }
        }
        private static void BuildContainerGroup(out CargoBayGroup cargoBayGroup, IEnumerable<CargoBay> cargoBays)
        {
            if (cargoBays.Count() == 0)
            {
                cargoBayGroup = null;
            }
            else
            {
                // There's no interface for cargo bay, so the other parts are hard coded to the group - this could be
                // changed to an interface in the future and made more efficient
                cargoBayGroup = new CargoBayGroup(cargoBays.ToArray());
            }
        }

        #endregion
        #region Private Methods - link neural

        private static NeuralUtility.ContainerOutput[] LinkNeural(ShipExtraArgs extra, BotConstruction_Parts parts)
        {
            if (!extra.RunNeural)
            {
                return null;
            }

            //TODO: This is currently only used for new bots (existing just use the existing links).  May want it stored in the bot's dna, and used to
            //prune links --- that was the original intent, but I don't want to fully implement until needed
            BotConstruction_PartMap partMap = GetLinkMap(null, parts, extra.ItemOptions, extra.PartLink_Overflow, extra.PartLink_Extra);

            #region debug draw

            //const double DOT = .1;
            //const double THICKNESS = .01;

            //Debug3DWindow window = new Debug3DWindow() { Title = "Part Neural Map" };

            //foreach (var part in parts.AllPartsArray)
            //{
            //    Color partColor = Colors.Gray;
            //    if (part is INeuronContainer neuralPart)
            //    {
            //        switch (neuralPart.NeuronContainerType)
            //        {
            //            case NeuronContainerType.Sensor:
            //                partColor = Colors.OliveDrab;
            //                break;

            //            case NeuronContainerType.Brain:
            //                partColor = Colors.HotPink;
            //                break;

            //            case NeuronContainerType.Manipulator:
            //                partColor = Colors.SteelBlue;
            //                break;
            //        }
            //    }

            //    window.AddDot(part.Position, DOT, partColor);       //TODO: May want to multiply by part's scale
            //}

            //foreach (var link in partMap.Map_DNA)
            //{
            //    window.AddLine(link.From, link.To, THICKNESS * link.Weight, link.Weight > 0 ? UtilityWPF.ColorFromHex("008A2A") : UtilityWPF.ColorFromHex("A61514"));
            //}

            //window.Show();

            #endregion

            NeuralUtility.ContainerInput[] neuralContainers = BuildNeuralContainers(parts, extra.ItemOptions);

            NeuralUtility.ContainerOutput[] retVal = NeuralUtility.LinkNeurons(partMap, neuralContainers, extra.ItemOptions.NeuralLink_MaxWeight);

            if (retVal.Length == 0)
            {
                return null;
            }
            else
            {
                return retVal;
            }
        }

        private static BotConstruction_PartMap GetLinkMap(BotPartMapLinkDNA[] initial, BotConstruction_Parts parts, ItemOptions itemOptions, ItemLinker_OverflowArgs overflowArgs, ItemLinker_ExtraArgs extraArgs)
        {
            if (initial == null)
            {
                initial = CreatePartMap(parts, overflowArgs, extraArgs);
            }

            BotConstruction_PartMap map = AnalyzeMap(initial, parts, itemOptions, extraArgs);


            // After many generations, links could drift too much.  May want to try to correct that (but the act of correcting could get
            // heavy handed and ruin a trend?)


            //TODO: Make sure all the parts are linked
            //map = EnsureAllPartsMapped(map, parts);

            //TODO: Call a delegate here

            //TODO: Analyze burdens

            //TODO: Adjust links



            return new BotConstruction_PartMap()
            {
                Map_DNA = map.Map_DNA,
                Map_Actual = map.Map_DNA.
                    Select(o =>
                    (
                        parts.AllPartsArray.First(p => p.Position.IsNearValue(o.From)),
                        parts.AllPartsArray.First(p => p.Position.IsNearValue(o.To)),
                        o.Weight
                    )).
                    ToArray(),
            };
        }

        private static BotPartMapLinkDNA[] CreatePartMap_THOUGHTS(BotConstruction_Parts parts)
        {
            //NOTE: This method should ignore itemOptions.
            //      BrainLinksPerNeuron_External_FromSensor
            //      BrainLinksPerNeuron_External_FromBrain
            //      BrainLinksPerNeuron_External_FromManipulator
            //      ThrusterLinksPerNeuron_Sensor
            //      ThrusterLinksPerNeuron_Brain
            //
            //Instead, it should just link items, and the weight of these links could be multiplied by the above properties



            //TODO: Call a delegate that lets the caller pre link items:
            //      for example, cameras are normally inputs, recognizers are normally brains
            //      but let the caller link them.  Any linked camera should be none, and any linked recognizer should be input (or brain?)
            //      so if there are cameras and no recognizers, then they just default to input
            //      recognizers with no cameras default to none






            // this seems like overkill.  The property should just change if it knows it's been linked
            //
            //TODO: Categorize the parts.  Raise a delegate to allow an override
            //foreach(var part)
            //{
            //    var identity = calldelegate(part);        // allow none
            //    if(identity == null)
            //    {
            //        identity = identify(part);
            //    }
            //}




            //LinkItem[] brains = _brains3D.
            //    Select(o => new LinkItem(o.Position.Value, o.Size)).
            //    ToArray();

            //LinkItem[] io = _inputs3D.
            //    Concat(_outputs3D).
            //    Select(o => new LinkItem(o.Position.Value, o.Size)).
            //    ToArray();


            ////TODO: Take in some settings
            //ItemLinker_OverflowArgs overflowArgs = new ItemLinker_OverflowArgs();
            //ItemLinker_ExtraArgs extraArgs = new ItemLinker_ExtraArgs();

            //Tuple<int, int>[] links = ItemLinker.Link_1_2(brains, io, overflowArgs, extraArgs);


            return null;
        }
        private static BotPartMapLinkDNA[] CreatePartMap(BotConstruction_Parts parts, ItemLinker_OverflowArgs overflowArgs, ItemLinker_ExtraArgs extraArgs)
        {
            INeuronContainer[] neuralParts = parts.AllPartsArray.
                Where(o => o is INeuronContainer).
                Select(o => (INeuronContainer)o).
                ToArray();

            //NOTE: Some parts may have been set to none
            LinkItem[] brains = neuralParts.
                Where(o => o.NeuronContainerType == NeuronContainerType.Brain).
                Select(o => new LinkItem(o.Position, o.Radius)).
                ToArray();

            LinkItem[] io = neuralParts.
                Where(o => o.NeuronContainerType == NeuronContainerType.Sensor || o.NeuronContainerType == NeuronContainerType.Manipulator).
                Select(o => new LinkItem(o.Position, o.Radius)).
                ToArray();

            Tuple<int, int>[] links = ItemLinker.Link_1_2(brains, io, overflowArgs, extraArgs);

            return links.
                Select(o => new BotPartMapLinkDNA()
                {
                    From = brains[o.Item1].Position,
                    To = io[o.Item2].Position,
                    Weight = 1d,
                }).
                ToArray();
        }

        private static BotConstruction_PartMap AnalyzeMap(BotPartMapLinkDNA[] map, BotConstruction_Parts parts, ItemOptions itemOptions, ItemLinker_ExtraArgs extraLinks)
        {
            Point3D[] partPositions = parts.AllPartsArray.
                Select(o => o.Position).
                ToArray();

            var initialLinkPoints = map.
                Select(o => Tuple.Create(o.From, o.To, o.Weight)).
                ToArray();

            // Figure out the max allowed links
            int maxFinalLinkCount = (GetEstimatedLinkCount(parts.AllPartsArray, extraLinks) * itemOptions.PartMap_FuzzyLink_MaxLinkPercent).ToInt_Round();

            // If parts moved since the links were assigned, this will find the best matches for links
            var newLinks = ItemLinker.FuzzyLink(initialLinkPoints, partPositions, maxFinalLinkCount, itemOptions.PartMap_FuzzyLink_MaxIntermediateCount);

            return new BotConstruction_PartMap()
            {
                Map_DNA = newLinks.
                    Select(o => new BotPartMapLinkDNA()
                    {
                        From = partPositions[o.Item1],
                        To = partPositions[o.Item2],
                        Weight = o.Item3,
                    }).
                    ToArray(),
            };
        }

        private static double GetEstimatedLinkCount(PartBase[] parts, ItemLinker_ExtraArgs extraLinks)
        {
            // The ratio of brains to IO is a big influence on the estimate, so count the number of brains
            int numBrains = parts.Count(o => o is INeuronContainer && ((INeuronContainer)o).NeuronContainerType == NeuronContainerType.Brain);
            double ratio = numBrains.ToDouble() / parts.Length.ToDouble();

            // Use the mesh to do a bicubic interpolation
            double retVal = _linkEstimateMesh.Value.EstimateValue(ratio, parts.Length);
            retVal *= 1d + extraLinks.Percent;

            return retVal;
        }
        /// <summary>
        /// This was built from running this:
        /// MissileCommand0D.DelaunaySegmentEquation1b_Click()
        /// </summary>
        private static BezierMesh CreateLinkEstimateMesh()
        {
            double[] ratios = new double[] { 0, .1, .2, .3, .4, .5, .6, .7, .8, .9, 1 };        // x axis
            double[] pointCounts = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 20, 26, 35, 47, 61, 77, 96, 118, 142, 168, 197, 229, 263, 300 };       // y axis
            double[] linkCounts = new double[]      // z values
                {
                    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
                    0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,
                    0,  0,  2,  2,  2,  2,  2,  2,  2,  3,  3,
                    0,  0,  3,  3,  3,  3,  3,  4,  4,  6,  6,
                    0,  0,  4,  4,  4,  4,  5,  7,  7,  7,  9.65,
                    0,  5,  5,  5,  5,  6,  8,  8,  10.5875,    10.7125,    13.65,
                    0,  6,  6,  6,  7,  9,  9,  11.6,   14.875,     15,     18.35,
                    0,  7,  7,  7,  8,  10,     12.7,   15.8875,    15.8625,    19.3125,    22.9875,
                    0,  8,  8,  9,  11,     11,     13.6375,    16.925,     20.2625,    24.15,  28.3125,
                    0,  9,  9,  10,     12,     14.725,     17.85,  21.4375,    25.2125,    29.4625,    33.3125,
                    0,  10,     10,     11,     13,     18.7375,    22.175,     26.35,  30.1625,    34.4125,    38.5875,
                    0,  11,     11,     14,     16.7375,    19.8,   23.225,     27.3875,    35.8,   39.7375,    43.9125,
                    0,  12,     13,     15,     17.775,     20.8875,    28.2375,    32.6,   36.3125,    45.5,   49.7,
                    0,  13,     14,     16,     21.9125,    25.2375,    29.0625,    37.275,     41.55,  51.1,   55.6125,
                    0,  14,     15,     17,     22.8875,    30.0625,    33.9125,    38.4375,    47.4875,    56.9,   61.3,
                    0,  15,     16,     20.65,  23.9625,    31.2625,    39.4875,    43.5,   52.9625,    57.75,  67.4375,
                    0,  19,     22,     27.7125,    35.4125,    43.325,     52.575,     61.8875,    70.9625,    81.325,     92.025,
                    0,  26,     30.7125,    41.3875,    49.2375,    62.7875,    77.5875,    87.5875,    102.7875,   112.9625,   130.1625,
                    0,  37,     46.2,   58.05,  77.1875,    96.7,   111.2375,   127.1625,   149.225,    170.65,     187.2375,
                    0,  51.725,     66.25,  88.4875,    113.325,    139.7,  160.75,     188.2125,   216.375,    239.225,    268.4125,
                    0,  68.775,     93.7875,    122.475,    154.125,    185.6875,   224.4375,   259.775,    294.8,  330.8125,   366.6625,
                    0,  92.0875,    123.45,     163.65,     206.8625,   246.5875,   293.4875,   338.775,    386.3125,   428.5,  476.425,
                    0,  119.3875,   162.0875,   216.4,  266.2375,   324.325,    381.125,    435.9,  496.625,    550.9375,   613.95,
                    0,  150.3875,   210.2125,   269.775,    338.0125,   409.425,    482.075,    554.5125,   623.5125,   696.275,    769.2875,
                    0,  183.4875,   256.6125,   340.5875,   422.725,    507.1125,   591.3125,   675.8625,   768.6375,   856.35,     942.5375,
                    0,  223.725,    315.6625,   407.325,    507.8375,   609.775,    715.7,  819.425,    921.65,     1026.95,    1130.8125,
                    0,  268.2125,   373.125,    490.2375,   608.125,    726.0375,   847.1875,   972.9625,   1100.25,    1219.45,    1345.4875,
                    0,  316.35,     443.4625,   581.4625,   720.925,    854.7875,   1000.775,   1144.6375,  1290.3,     1434.2875,  1584.5125,
                    0,  365.9875,   520.325,    675.4625,   834.6,  1003.275,   1164.6,     1328.7,     1492.9625,  1665.35,    1830.9875,
                    0,  424.6375,   598.35,     779.7,  963.8,  1151.2375,  1340.875,   1530.5,     1722.0375,  1914.4625,  2106.5375
                };

            return new BezierMesh(ratios, pointCounts, linkCounts);
        }

        //TODO: May want a delegate called for each part
        private static NeuralUtility.ContainerInput[] BuildNeuralContainers(BotConstruction_Parts parts, ItemOptions itemOptions)
        {
            List<NeuralUtility.ContainerInput> retVal = new List<NeuralUtility.ContainerInput>();

            foreach (var part in parts.AllParts.Where(o => o.Item1 is INeuronContainer))
            {
                INeuronContainer container = (INeuronContainer)part.Item1;
                ShipPartDNA dna = part.Item2;
                NeuralLinkDNA[] internalLinks = dna == null ? null : dna.InternalLinks;
                NeuralLinkExternalDNA[] externalLinks = dna == null ? null : dna.ExternalLinks;

                switch (container.NeuronContainerType)
                {
                    case NeuronContainerType.Sensor:
                        #region Sensor

                        // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                        // neuron containers can hook to it
                        retVal.Add(new NeuralUtility.ContainerInput(part.Item1.Token, container, NeuronContainerType.Sensor, container.Position, container.Orientation, null, null, 0, null, null));

                        #endregion
                        break;

                    case NeuronContainerType.Brain:
                        #region Brain

                        int brainChemicalCount = 0;
                        if (part.Item1 is Brain castBrain)
                        {
                            brainChemicalCount = (castBrain.BrainChemicalCount * 1.33d).ToInt_Round();		// increasing so that there is a higher chance of listeners
                        }

                        retVal.Add(new NeuralUtility.ContainerInput(
                            part.Item1.Token,
                            container, NeuronContainerType.Brain,
                            container.Position, container.Orientation,

                            // there are other parts that call themselves brains, but their internal firings are custom logic.  So only the Brain class needs internal wiring at this level
                            //TODO: If more parts need internal wiring in the future, add another enum value Brain_Standard, Brain_Custom (or something) - or this method should call a delegate for each part that can apply custom logic/ratios
                            part.Item1 is Brain ? itemOptions.Brain_LinksPerNeuron_Internal : (double?)null,

                            new Tuple<NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
                            {
                                Tuple.Create(NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Smallest, itemOptions.Brain_LinksPerNeuron_External_FromSensor),
                                Tuple.Create(NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Average, itemOptions.Brain_LinksPerNeuron_External_FromBrain),
                                Tuple.Create(NeuronContainerType.Manipulator, NeuralUtility.ExternalLinkRatioCalcType.Smallest, itemOptions.Brain_LinksPerNeuron_External_FromManipulator)
                            },
                            brainChemicalCount,
                            internalLinks, externalLinks));

                        #endregion
                        break;

                    case NeuronContainerType.Manipulator:
                        #region Manipulator

                        retVal.Add(new NeuralUtility.ContainerInput(
                            part.Item1.Token,
                            container, NeuronContainerType.Manipulator,
                            container.Position, container.Orientation,
                            null,
                            new Tuple<NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
                            {
                                Tuple.Create(NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Destination, itemOptions.Thruster_LinksPerNeuron_Sensor),
                                Tuple.Create(NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Destination, itemOptions.Thruster_LinksPerNeuron_Brain),
                            },
                            0,
                            null, externalLinks));

                        #endregion
                        break;

                    case NeuronContainerType.None:
                        break;

                    default:
                        throw new ApplicationException("Unknown NeuronContainerType: " + container.NeuronContainerType.ToString());
                }
            }

            return retVal.ToArray();
        }

        #endregion
        #region Private Methods - moment of inertia

        /// <summary>
        /// Calculate the moment of inertia of the bot
        /// TODO: Account for the invisible structural filler between the parts (probably just do a convex hull and give the filler a uniform density)
        /// </summary>
        /// <returns>
        /// Item1=3D moment of inertia
        /// Item2=Center of mass (bot coords)
        /// </returns>
        internal static Tuple<MassMatrix, Point3D> GetInertiaTensorAndCenterOfMass_Points(PartBase[] parts, ShipPartDNA[] dna, double inertiaMultiplier)
        {
            #region Prep work

            // Break the mass of the parts into pieces
            double cellSize = dna.Select(o => Math1D.Max(o.Scale.X, o.Scale.Y, o.Scale.Z)).Max() * .2d;		// break the largest object up into roughly 5x5x5
            UtilityNewt.IObjectMassBreakdown[] massBreakdowns = parts.Select(o => o.GetMassBreakdown(cellSize)).ToArray();

            double cellSphereMultiplier = (cellSize * .5d) * (cellSize * .5d) * .4d;		// 2/5 * r^2

            double[] partMasses = parts.Select(o => o.TotalMass).ToArray();
            double totalMass = partMasses.Sum();
            double totalMassInverse = 1d / totalMass;

            Vector3D axisX = new Vector3D(1d, 0d, 0d);
            Vector3D axisY = new Vector3D(0d, 1d, 0d);
            Vector3D axisZ = new Vector3D(0d, 0d, 1d);

            #endregion

            #region Bot's center of mass

            // Calculate the ship's center of mass
            double centerX = 0d;
            double centerY = 0d;
            double centerZ = 0d;
            for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
            {
                // Shift the part into bot coords
                Point3D centerMass = parts[cntr].Position + parts[cntr].Orientation.GetRotatedVector(massBreakdowns[cntr].CenterMass.ToVector());

                centerX += centerMass.X * partMasses[cntr];
                centerY += centerMass.Y * partMasses[cntr];
                centerZ += centerMass.Z * partMasses[cntr];
            }

            Point3D center = new Point3D(centerX * totalMassInverse, centerY * totalMassInverse, centerZ * totalMassInverse);

            #endregion

            #region Local inertias

            // Get the local moment of inertia of each part for each of the three ship's axiis
            //TODO: If the number of cells is large, this would be a good candidate for running in parallel, but this method keeps cellSize pretty course
            Vector3D[] localInertias = new Vector3D[massBreakdowns.Length];
            for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
            {
                RotateTransform3D localRotation = new RotateTransform3D(new QuaternionRotation3D(parts[cntr].Orientation.ToReverse()));



                //TODO: Verify these results with the equation for the moment of inertia of a cylinder



                //NOTE: Each mass breakdown adds up to a mass of 1, so putting that mass back now (otherwise the ratios of masses between parts would be lost)
                localInertias[cntr] = new Vector3D(
                    GetInertia(massBreakdowns[cntr], localRotation.Transform(axisX), cellSphereMultiplier) * partMasses[cntr],
                    GetInertia(massBreakdowns[cntr], localRotation.Transform(axisY), cellSphereMultiplier) * partMasses[cntr],
                    GetInertia(massBreakdowns[cntr], localRotation.Transform(axisZ), cellSphereMultiplier) * partMasses[cntr]);
            }

            #endregion
            #region Global inertias

            // Apply the parallel axis theorem to each part
            double shipInertiaX = 0d;
            double shipInertiaY = 0d;
            double shipInertiaZ = 0d;
            for (int cntr = 0; cntr < massBreakdowns.Length; cntr++)
            {
                // Shift the part into ship coords
                Point3D partCenter = parts[cntr].Position + massBreakdowns[cntr].CenterMass.ToVector();

                shipInertiaX += GetInertia(partCenter, localInertias[cntr].X, partMasses[cntr], center, axisX);
                shipInertiaY += GetInertia(partCenter, localInertias[cntr].Y, partMasses[cntr], center, axisY);
                shipInertiaZ += GetInertia(partCenter, localInertias[cntr].Z, partMasses[cntr], center, axisZ);
            }

            #endregion

            // Newton wants the inertia vector to be one, so divide off the mass of all the parts <- not sure why I said they need to be one.  Here is a response from Julio Jerez himself:
            //this is the correct way
            //NewtonBodySetMassMatrix( priv->body, mass, mass * inertia[0], mass * inertia[1], mass * inertia[2] );
            //matrix = new MassMatrix(totalMass, new Vector3D(shipInertiaX * totalMassInverse, shipInertiaY * totalMassInverse, shipInertiaZ * totalMassInverse));
            MassMatrix matrix = new MassMatrix(totalMass, new Vector3D(shipInertiaX * inertiaMultiplier, shipInertiaY * inertiaMultiplier, shipInertiaZ * inertiaMultiplier));

            return Tuple.Create(matrix, center);
        }

        /// <summary>
        /// This calculates the moment of inertia of the body around the axis (the axis goes through the center of mass)
        /// </summary>
        /// <remarks>
        /// Inertia of a body is the sum of all the mr^2
        /// 
        /// Each cell of the mass breakdown needs to be thought of as a sphere.  If it were a point mass, then for a body with only one
        /// cell, the mass would be at the center, and it would have an inertia of zero.  So by using the parallel axis theorem on each cell,
        /// the returned inertia is accurate.  The reason they need to thought of as spheres instead of cubes, is because the inertia is the
        /// same through any axis of a sphere, but not for a cube.
        /// 
        /// So sphereMultiplier needs to be 2/5 * cellRadius^2
        /// </remarks>
        private static double GetInertia(UtilityNewt.IObjectMassBreakdown body, Vector3D axis, double sphereMultiplier)
        {
            double retVal = 0d;

            // Cache this point in case the property call is somewhat expensive
            Point3D center = body.CenterMass;

            foreach (var pointMass in body)
            {
                if (pointMass.Item2 == 0d)
                {
                    continue;
                }

                // Tack on the inertia of the cell sphere (2/5*mr^2)
                retVal += pointMass.Item2 * sphereMultiplier;

                // Get the distance between this point and the axis
                double distance = Math3D.GetClosestDistance_Line_Point(body.CenterMass, axis, pointMass.Item1);

                // Now tack on the md^2
                retVal += pointMass.Item2 * distance * distance;
            }

            return retVal;
        }
        /// <summary>
        /// This returns the inertia of the part relative to the bot's axis
        /// NOTE: The other overload takes a vector that was transformed into the part's model coords.  The vector passed to this overload is in bot's model coords
        /// </summary>
        private static double GetInertia(Point3D partCenter, double partInertia, double partMass, Point3D botCenterMass, Vector3D axis)
        {
            // Start with the inertia of the part around the axis passed in
            double retVal = partInertia;

            // Get the distance between the part and the axis
            double distance = Math3D.GetClosestDistance_Line_Point(botCenterMass, axis, partCenter);

            // Now tack on the md^2
            retVal += partMass * distance * distance;

            return retVal;
        }

        #endregion
        #region Private Methods

        /// <summary>
        /// This throws out parts that don't meet validation
        /// </summary>
        /// <returns>
        /// Item1=Parts to keep
        /// Item2=Parts to discard
        /// </returns>
        private static Tuple<ShipPartDNA[], ShipPartDNA[]> DiscardParts(ShipDNA dna, Func<ShipPartDNA, bool> shouldDiscardPart)
        {
            const double MINLENGTHSQUARED = .01 * .01;

            List<ShipPartDNA> keep = new List<ShipPartDNA>();
            List<ShipPartDNA> discard = new List<ShipPartDNA>();

            foreach (ShipPartDNA part in dna.PartsByLayer.SelectMany(o => o.Value))
            {
                // Size
                if (part.Scale.LengthSquared < MINLENGTHSQUARED)
                {
                    discard.Add(part);
                    continue;
                }

                //TODO: Validate the distance from other parts

                // Delegate for custom rules
                if (shouldDiscardPart != null && shouldDiscardPart(part))
                {
                    discard.Add(part);
                    continue;
                }

                keep.Add(part);
            }

            return Tuple.Create(keep.ToArray(), discard.ToArray());
        }

        private static void LinkParts_NonNeural(BotConstruction_Parts building, ShipCoreArgs core, ShipExtraArgs extra, Action<BotConstruction_Parts, ShipCoreArgs, ShipExtraArgs> linkParts_NonNeural)
        {
            #region CargoBays - Converters

            // Link cargo bays with converters
            if (building.Containers.CargoBays.Count > 0)
            {
                IConverterMatter[] converters = building.
                    GetStandardParts<IConverterMatter>(ConverterMatterToEnergy.PARTTYPE).
                    Concat(building.GetStandardParts<IConverterMatter>(ConverterMatterToFuel.PARTTYPE)).
                    Concat(building.GetStandardParts<IConverterMatter>(ConverterMatterToAmmo.PARTTYPE)).
                    Concat(building.GetStandardParts<IConverterMatter>(ConverterMatterToPlasma.PARTTYPE)).
                    ToArray();

                if (converters.Length > 0)
                {
                    building.Containers.ConvertMatterGroup = new ConverterMatterGroup(converters, building.Containers.CargoBayGroup);
                }
            }

            #endregion

            //TODO: This should be interface based
            #region LifeEventWatcher

            foreach (BrainRGBRecognizer recognizer in building.GetStandardParts<BrainRGBRecognizer>(BrainRGBRecognizer.PARTTYPE))
            {
                LifeEventToVector toVector = new LifeEventToVector(building.LifeEventWatcher, new[] { LifeEventType.AddedCargo, LifeEventType.LostPlasma });
                recognizer.AssignOutputs(toVector);
            }

            #endregion



            //TODO: These two need to be done in a different step, they need to store results into dna
            #region Guns - Ammo

            // Distribute ammo boxes to guns based on the gun's caliber

            ProjectileGun[] guns = building.GetStandardParts<ProjectileGun>(ProjectileGun.PARTTYPE).ToArray();

            if (guns.Length > 0 && building.Containers.Ammos.Count > 0)
            {
                ProjectileGun.AssignAmmoBoxes(guns, building.Containers.Ammos);
            }

            #endregion
            #region Cameras - Recognizers

            // Tie recognizers to cameras
            //TODO: This doesn't belong here.  It should be a special pass in the neural linker
            //It's also ignoring dna

            BrainRGBRecognizer[] recognizers = building.GetStandardParts<BrainRGBRecognizer>(BrainRGBRecognizer.PARTTYPE).ToArray();
            CameraColorRGB[] cameras = building.GetStandardParts<CameraColorRGB>(CameraColorRGB.PARTTYPE).ToArray();

            if (recognizers.Length > 0 && cameras.Length > 0)
            {
                BrainRGBRecognizer.AssignCameras(recognizers, cameras);
            }

            #endregion



            if (linkParts_NonNeural != null)
            {
                linkParts_NonNeural(building, core, extra);
            }
        }

        private static Tuple<ShipPartDNA[], CollisionHull[], bool> SeparateParts(PartBase[] parts, ShipPartDNA[] dna, bool repairPartPositions, World world)
        {
            CollisionHull[] hulls = null;
            bool changed = false;

            if (repairPartPositions)
            {
                bool changed1, changed2;

                PartSeparator_Part[] partWrappers = parts.
                    Select(o => new PartSeparator_Part(o)).
                    ToArray();

                //TODO: Make a better version.  The PartSeparator should just expose one method, and do both internally
                PartSeparator.PullInCrude(out changed1, partWrappers);

                // Separate intersecting parts
                hulls = PartSeparator.Separate(out changed2, partWrappers, world);

                changed = changed1 || changed2;

                if (changed)
                {
                    dna = parts.
                        Select(o => o.GetNewDNA()).
                        ToArray();
                }
            }
            else
            {
                hulls = parts.
                    Select(o => o.CreateCollisionHull(world)).
                    ToArray();
            }

            return Tuple.Create(dna, hulls, changed);
        }

        private static Tuple<Model3DGroup, ModelVisual3D> GetWPFMain(PartBase[] parts)
        {
            //TODO: Remember this so that flames and other visuals can be added/removed.  That way there will still only be one model visual
            //TODO: When joints are supported, some of the parts (or part groups) will move relative to the others.  There can still be a single model visual
            Model3DGroup models = new Model3DGroup();

            foreach (PartBase part in parts)
            {
                models.Children.Add(part.Model);
                part.EnsureCorrectGraphics_StandardDestroyed();
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = models;

            return Tuple.Create(models, visual);
        }
        private static Ship.VisualEffects GetWPFEffects(BotConstruction_Parts parts, ShipExtraArgs extra)
        {
            Thruster[] thrusters = parts.GetStandardParts<Thruster>(Thruster.PARTTYPE).ToArray();

            // These visuals will only show up in the main viewport (they won't be shared with the camera pool, so won't be visible to
            // other ships)
            return new Ship.VisualEffects(extra.ItemOptions, thrusters);
        }

        private static Body GetPhysicsBody(PartBase[] parts, ShipPartDNA[] dna, CollisionHull[] hulls, ModelVisual3D visual, Ship.VisualEffects visualEffects, ShipCoreArgs core, ShipExtraArgs extra)
        {
            // For now, just make a composite collision hull out of all the parts
            CollisionHull hull = CollisionHull.CreateCompoundCollision(core.World, 0, hulls);

            Body retVal = new Body(hull, Matrix3D.Identity, 1d, new Visual3D[] { visual, visualEffects.ThrustVisual });		// just passing a dummy value for mass, the real mass matrix is calculated later
            retVal.MaterialGroupID = core.Material_Ship;
            retVal.LinearDamping = .01d;
            retVal.AngularDamping = new Vector3D(.01d, .01d, .01d);

            if (extra.IsPhysicsStatic)
            {
                retVal.CenterOfMass = new Point3D(0, 0, 0);        // Part_RequestWorldLocation is wrong if this isn't zero as well
                retVal.Mass = 0;
            }
            else
            {
                Tuple<MassMatrix, Point3D> massBreakdown = GetInertiaTensorAndCenterOfMass_Points(parts, dna, extra.ItemOptions.MomentOfInertiaMultiplier);

                retVal.CenterOfMass = massBreakdown.Item2;
                retVal.MassMatrix = massBreakdown.Item1;
            }

            hull.Dispose();
            foreach (CollisionHull partHull in hulls)
            {
                partHull.Dispose();
            }

            return retVal;
        }

        private static void FixArgs(ref ShipExtraArgs extra, ref BotConstructor_Events events)
        {
            events = events ?? new BotConstructor_Events();

            extra = extra ?? new ShipExtraArgs();
            extra.Options = extra.Options ?? new EditorOptions();
            extra.ItemOptions = extra.ItemOptions ?? new ItemOptions();
            extra.PartLink_Overflow = extra.PartLink_Overflow ?? new ItemLinker_OverflowArgs();
            extra.PartLink_Extra = extra.PartLink_Extra ?? new ItemLinker_ExtraArgs();
        }

        private static Type[] GetShipPartTypes()
        {
            // All of the ship parts sit in their own assembly/namespace.  So just picking an arbitrary shippart so I can get at the rest
            Type fuelTankType = typeof(FuelTank);

            Assembly assembly = Assembly.GetAssembly(fuelTankType);

            string ns = fuelTankType.Namespace;

            return assembly.GetTypes().
                Where(o => String.Equals(o.Namespace, ns, StringComparison.Ordinal)).
                ToArray();
        }

        private static string[] GetAllPartTypes()
        {
            return _shipPartTypes.Value.
                SelectMany(o => o.GetFields()).
                Where(o => o.Name == "PARTTYPE").       // all of the ship parts have a string constant called PARTTYPE, to make methods like this easier :)
                Select(o => o.GetRawConstantValue().ToString()).
                OrderBy().
                ToArray();
        }

        // This is flawed (some constructors want an extra enum).  Went with design class returning a toolitem
        private static PartToolItemBase[] GetAllPartToolItems()
        {
            Type compare = typeof(PartToolItemBase);

            object[] constructorArgs = new object[] { new EditorOptions() };
            EditorOptions options = new EditorOptions();

            var test = _shipPartTypes.Value.
                Where(o => o.BaseType != null && o.BaseType == compare).
                ToArray();

            foreach (var test2 in test)
            {
                PartToolItemBase test3 = (PartToolItemBase)Activator.CreateInstance(test2, constructorArgs);
            }


            return null;
        }

        #endregion
    }

    #region class: BotConstructor_Events

    /// <summary>
    /// At various stages of instantiating parts/linking them together, these delegates will get called to give the
    /// caller a chance to create custom parts that are defined outside this dll
    /// </summary>
    public class BotConstructor_Events
    {
        /// <summary>
        /// This hook allows custom validation of parts
        /// </summary>
        /// <return>
        /// True: Discard the part
        /// False: Keep the part
        /// </return>
        public Func<ShipPartDNA, bool> ShouldDiscardPart { get; set; }

        // This class only knows about parts defined in this dll.  If a part is defined in a higher dll, the caller will need to instantiate it
        /// <summary>
        /// There is a pre pass through the parts that looks for containers.  Then the standard pass can hand containers to the other parts.
        /// If you have a custom container part, then hook to this delegate
        /// </summary>
        public Func<ShipPartDNA, ShipCoreArgs, ShipExtraArgs, PartBase> InstantiateUnknownPart_Container { get; set; }
        /// <summary>
        /// Once the container parts are instantiated, container groups get created
        /// NOTE: Passing BotConstruction_Containers, which will have everything populated except CustomContainerGroups (which is what this delegate populates)
        /// </summary>
        public Func<BotConstruction_Containers, object[]> InstantiateUnknownPart_ContainerGroups { get; set; }

        /// <summary>
        /// This pass goes through all the parts that haven't been created in previous passes
        /// </summary>
        public Func<ShipPartDNA, ShipCoreArgs, ShipExtraArgs, BotConstruction_Containers, PartBase> InstantiateUnknownPart_Standard { get; set; }


        //TODO: There may need to be two variants of the below delegates.  No dna, links stored in dna


        /// <summary>
        /// This gives a chance to wire up parts (like having all thrusters tied to all fuel tanks)
        /// </summary>
        public Action<BotConstruction_Parts, ShipCoreArgs, ShipExtraArgs> LinkParts_NonNeural { get; set; }

        /// <summary>
        /// This should get called first so the caller can specify certain parts that need to be neurally linked
        /// </summary>
        //public Func<PartBase[], object> GetLinkMap_Neural { get; set; }
    }

    #endregion

    #region class: BotConstruction_Containers

    public class BotConstruction_Containers
    {
        public List<AmmoBox> Ammos = new List<AmmoBox>();
        public IContainer AmmoGroup = null;		// this is used by the converters to fill up the ammo boxes.  The logic to match ammo boxes with guns is more complex than a single group

        public List<FuelTank> Fuels = new List<FuelTank>();
        public IContainer FuelGroup = null;		// this will either be null, ContainerGroup, or a single FuelTank

        public List<EnergyTank> Energies = new List<EnergyTank>();
        public IContainer EnergyGroup = null;		// this will either be null, ContainerGroup, or a single EnergyTank

        public List<PlasmaTank> Plasmas = new List<PlasmaTank>();
        public IContainer PlasmaGroup = null;     // this will either be null, ContainerGroup, or a single PlasmaTank

        public List<CargoBay> CargoBays = new List<CargoBay>();
        public CargoBayGroup CargoBayGroup = null;

        public List<PartBase> CustomContainers = new List<PartBase>();
        public object[] CustomContainerGroups = null;

        public ConverterMatterGroup ConvertMatterGroup = null;
    }

    #endregion
    #region class: BotConstruction_Parts

    public class BotConstruction_Parts
    {
        public BotConstruction_Containers Containers = null;

        public LifeEventWatcher LifeEventWatcher = null;

        public SortedList<string, List<PartBase>> StandardParts = null;
        public IEnumerable<PartBase> GetStandardParts(string partType)
        {
            List<PartBase> list;
            if (this.StandardParts.TryGetValue(partType, out list))
            {
                return list;
            }

            return new PartBase[0];
        }
        public IEnumerable<T> GetStandardParts<T>(string partType) where T : class
        {
            List<PartBase> list;
            if (this.StandardParts.TryGetValue(partType, out list))
            {
                foreach (PartBase item in list)
                {
                    T itemCast = item as T;
                    if (itemCast == null)
                    {
                        if (item == null)
                        {
                            throw new ApplicationException("Shouldn't see a null part");
                        }
                        else
                        {
                            throw new ApplicationException("Couldn't cast to " + typeof(T).ToString());
                        }
                    }

                    yield return itemCast;
                }
            }
        }

        public List<Tuple<PartBase, ShipPartDNA>> AllParts = new List<Tuple<PartBase, ShipPartDNA>>();
        public PartBase[] AllPartsArray = null;     // this gets populated from AllParts once the instantiations are finished
    }

    #endregion

    #region class: BotConstruction_PartMap

    public class BotConstruction_PartMap
    {
        public BotPartMapLinkDNA[] Map_DNA { get; set; }

        /// <summary>
        /// This is the same as what is in this.Map, but holds links to actual parts
        /// </summary>
        public (PartBase from, PartBase to, double weight)[] Map_Actual { get; set; }

        //TODO: Store burdens for the parts?
    }

    #endregion
    #region class: BotPartMapLinkDNA

    /// <summary>
    /// This stores a high level picture of how the parts should be connected
    /// </summary>
    /// <remarks>
    /// This class is named this way because the original intention was for the BotDNA to store it
    /// </remarks>
    public class BotPartMapLinkDNA
    {
        public Point3D From { get; set; }
        public Point3D To { get; set; }

        /// <summary>
        /// This weight should only be positive
        /// </summary>
        public double Weight { get; set; }
    }

    #endregion

    #region class: BotConstruction_Result

    /// <summary>
    /// This holds instantiated/linked parts
    /// NOTE: This can only be used in one bot
    /// </summary>
    public class BotConstruction_Result
    {
        public ShipCoreArgs ArgsCore = null;
        public ShipExtraArgs ArgsExtra = null;

        public PartBase[] Parts = null;
        public ShipPartDNA[] DNAParts = null;
        public ShipDNA DNA = null;

        public BotConstruction_Parts PartConstruction = null;

        public IPartUpdatable[] UpdatableParts_MainThread = null;
        public IPartUpdatable[] UpdatableParts_AnyThread = null;

        public NeuralUtility.ContainerOutput[] Links = null;

        public Model3D Model = null;
        public Ship.VisualEffects VisualEffects = null;

        public Body PhysicsBody = null;

        public double Radius = -1;
    }

    #endregion
}
