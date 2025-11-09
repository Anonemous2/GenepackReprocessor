/*
 * User: Anonemous2
 * Date: 13-06-2024
 */
using System;
using System.Collections.Generic;
using System.Linq;
using GenepackReprocessor.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GenepackReprocessor;

[StaticConstructorOnStartup]

/// <summary>
/// This is the Building that allows us to split, copy, and merge genepacks.
/// </summary>
public class Building_GeneSeparator : Building, IThingHolder
{
    // Settings for mod
    private static GenepackReprocessorSettings _settings;
    public static GenepackReprocessorSettings Settings => _settings ??= LoadedModManager.GetMod<GenepackImprovMod>().GetSettings<GenepackReprocessorSettings>();

    //TODO: make these vars one List<Genepack> of "genepacksBeingWorked"
    private Genepack genepackToWork;
    private List<Genepack> genepacksToMerge;

    // Work vars
    private bool doForever = false;     // If true, after splitting, it will see if there's more genes that can be isolated.

    private WorkJob workJob;
    private bool workingInt;
    private int lastWorkedTick = -999;
    private float workDone;
    private float totalWorkRequired;

    // Consumables used
    public ThingOwner innerContainer;
    private int architesInGenes;
    private int architesRequired;
    private int neutroamineRequired;

    [Unsaved(false)]
    private float lastWorkAmount = -1f;

    [Unsaved(false)]
    private CompPowerTrader cachedPowerComp;

    [Unsaved(false)]
    private List<Genepack> tmpGenepacks = new List<Genepack>();

    [Unsaved(false)]
    private HashSet<Thing> tmpUsedFacilities = new HashSet<Thing>();

    [Unsaved(false)]
    private int? cachedComplexity;

    private const int CheckContainersInterval = 180;

    private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

    private static readonly CachedTexture SeparateIcon = new CachedTexture("GeneSeparator/Split");
    private static readonly CachedTexture MergeIcon = new CachedTexture("GeneSeparator/Merge");
    private static readonly CachedTexture DuplicateIcon = new CachedTexture("GeneSeparator/Duplicate");
    private static readonly CachedTexture RecycleIcon = new CachedTexture("GeneSeparator/Recycle");
    private static readonly CachedTexture RepeatIcon = new CachedTexture("GeneSeparator/Forever");

    // Getter methods
    public float ProgressPercent => workDone / totalWorkRequired;
    public bool Working => workingInt;

    // TODO: how does it get power
    private CompPowerTrader PowerTraderComp => cachedPowerComp ?? (cachedPowerComp = this.TryGetComp<CompPowerTrader>());
    public bool PowerOn => PowerTraderComp.PowerOn;

    // TODO: not sure here, returns connected?
    public List<Thing> ConnectedFacilities => this.TryGetComp<CompAffectedByFacilities>().LinkedFacilitiesListForReading;


    public int ArchitesCount
    {
        get
        {
            int num = 0;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (innerContainer[i].def == ThingDefOf.ArchiteCapsule)
                {
                    num += innerContainer[i].stackCount;
                }
            }
            return num;
        }
    }

    public int ArchitesRequiredNow => architesRequired - ArchitesCount;

    public int NeutroamineCount
    {
        get
        {
            int num = 0;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (innerContainer[i].def == GeneSeparator_DefOfs.Neutroamine)
                {
                    num += innerContainer[i].stackCount;
                }
            }
            return num;
        }
    }

    public int NeutroamineRequiredNow => neutroamineRequired - NeutroamineCount;


    /*
     Implement: list of used genebanks?
     private HashSet<Thing> UsedFacilities
     */
    private HashSet<Thing> UsedFacilities
    {
        get
        {
            // If splitting or copying a genepack
            tmpUsedFacilities.Clear();
            if (genepackToWork != null)
            {
                List<Thing> connectedFacilities = ConnectedFacilities;
                for (int j = 0; j < connectedFacilities.Count; j++)
                {
                    if (!tmpUsedFacilities.Contains(connectedFacilities[j]))
                    {
                        CompGenepackContainer compGenepackContainer = connectedFacilities[j].TryGetComp<CompGenepackContainer>();
                        if (compGenepackContainer != null && compGenepackContainer.ContainedGenepacks.Contains(genepackToWork))
                        {
                            tmpUsedFacilities.Add(connectedFacilities[j]);
                            return tmpUsedFacilities;
                        }
                    }
                }
            }
            // Else if Merging
            if (!genepacksToMerge.NullOrEmpty())
            {
                List<Thing> connectedFacilities = ConnectedFacilities;
                for (int i = 0; i < genepacksToMerge.Count; i++)
                {
                    for (int j = 0; j < connectedFacilities.Count; j++)
                    {
                        if (!tmpUsedFacilities.Contains(connectedFacilities[j]))
                        {
                            CompGenepackContainer compGenepackContainer = connectedFacilities[j].TryGetComp<CompGenepackContainer>();
                            if (compGenepackContainer != null && compGenepackContainer.ContainedGenepacks.Contains(genepacksToMerge[i]))
                            {
                                tmpUsedFacilities.Add(connectedFacilities[j]);
                                break;
                            }
                        }
                    }
                }
            }
            return tmpUsedFacilities;
        }
    }

    /*
     Implement: yeah, not entirely sure here. Maybe it runs the code whenver CanBeWorkedOnNow is being refed
     public AcceptanceReport CanBeWorkedOnNow
     */
    public AcceptanceReport CanBeWorkedOnNow
    {
        get
        {
            if (!Working)
            {
                return false;
            }
            if (ArchitesRequiredNow > 0)
            {
                return false;
            }
            if (NeutroamineRequiredNow > 0)
            {
                return false;
            }
            if (!PowerOn)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            foreach (Thing usedFacility in UsedFacilities)
            {
                CompPowerTrader compPowerTrader = usedFacility.TryGetComp<CompPowerTrader>();
                if (compPowerTrader != null && !compPowerTrader.PowerOn)
                {
                    return "GenebankUnpowered".Translate();
                }
            }
            return true;
        }
    }

    /*
     Implement: yeah, not entirely sure here
     private int TotalGCX
     */
    private int TotalGCX
    {
        get
        {
            if (!Working)
            {
                return 0;
            }
            if (!cachedComplexity.HasValue)
            {
                cachedComplexity = 0;
                // If Merge Job
                if (workJob == WorkJob.Merge)
                {
                    if (!genepacksToMerge.NullOrEmpty())
                    {
                        List<GeneDefWithType> list = new List<GeneDefWithType>();
                        for (int i = 0; i < genepacksToMerge.Count; i++)
                        {
                            if (genepacksToMerge[i].GeneSet != null)
                            {
                                for (int j = 0; j < genepacksToMerge[i].GeneSet.GenesListForReading.Count; j++)
                                {
                                    list.Add(new GeneDefWithType(genepacksToMerge[i].GeneSet.GenesListForReading[j], xenogene: true));
                                }
                            }
                        }
                        for (int k = 0; k < list.Count; k++)
                        {
                            cachedComplexity += list[k].geneDef.biostatCpx;   // Add complexity for each gene
                            // architesInGenes  += list[k].geneDef.biostatArc;   // Also add 4 complexity for each Archite Capsule too.
                        }
                        return cachedComplexity.Value;
                    }
                }
                if (genepackToWork != null)
                {
                    List<GeneDefWithType> list = new List<GeneDefWithType>();
                    if (genepackToWork.GeneSet != null)
                    {
                        for (int j = 0; j < genepackToWork.GeneSet.GenesListForReading.Count; j++)
                        {
                            // TODO: look here for the creating of a gen Def
                            // list.Add(new GeneDefWithType(genepackToSeparate.GeneSet.GenesListForReading[j], xenogene: true));
                            // list.Add(new GeneDefWithType(genepacksToRecombine[i].GeneSet.GenesListForReading[j], xenogene: true));
                            list.Add(new GeneDefWithType(genepackToWork.GeneSet.GenesListForReading[j], xenogene: true));
                        }
                    }
                    for (int k = 0; k < list.Count; k++)
                    {
                        cachedComplexity += list[k].geneDef.biostatCpx;       // Add complexity for each gene
                        // architesInGenes  += list[k].geneDef.biostatArc;        // Also add 4 complexity for each Archite Capsule too.
                    }
                }
            }
            return cachedComplexity.Value;
        }
    }

    /*
     * TODO: use mod settings to destroy this def if disabled
     Implement: Checks if this is still valid() every so often.
     public override void Tick()
     */
    public override void PostPostMake()
    {
        if (!ModLister.CheckBiotech("Gene assembler"))  // TODO: update with the corret check
        {
            Destroy();
            return;
        }
        base.PostPostMake();
        innerContainer = new ThingOwner<Thing>(this);
    }

    /*
     Implement: Checks if this is still valid() every so often.
     public override void Tick()
     */
    protected override void Tick()
    {
        base.Tick();
        innerContainer.DoTick();
        if (this.IsHashIntervalTick(250))
        {
            bool flag = lastWorkedTick + 250 + 2 >= Find.TickManager.TicksGame;
            PowerTraderComp.PowerOutput = (flag ? (0f - base.PowerComp.Props.PowerConsumption) : (0f - base.PowerComp.Props.idlePowerDraw));
        }
        if (Working && this.IsHashIntervalTick(180))
        {
            CheckContainerValid();
        }
    }

    /*
     Implement: sets up the work needed and all other vars.
     public void Start(List<Genepack> packs, int architesRequired, string xenotypeName, XenotypeIconDef iconDef)
     */
    public void StartSplit(Genepack pack, int architesRequired)
    {
        Action setWork = () =>
        {
            genepackToWork = pack;
            workJob = WorkJob.Split;
        };
        StartJob(Settings.split, setWork, Settings.separateNeedsArchites, architesRequired, Settings.separateBaseNeutroamine, Settings.separateComplexityNeutroamine, Settings.workToSplit);
    }

    public void StartDuplicate(Genepack pack, int architesRequired)
    {
        Action setWork = () =>
        {
            genepackToWork = pack;
            workJob = WorkJob.Copy;
        };
        StartJob(Settings.dupli, setWork, Settings.duplicateNeedsArchites, architesRequired, Settings.duplicateBaseNeutroamine, Settings.duplicateComplexityNeutroamine, Settings.workToDupli);
    }

    /*
     Implement: sets up the work needed and all other vars.
     public void Start(List<Genepack> packs, int architesRequired, string xenotypeName, XenotypeIconDef iconDef)
     */
    public void StartMerge(List<Genepack> packs, int architesRequired)
    {
        Action setWork = () =>
        {
            genepacksToMerge = packs;
            workJob = WorkJob.Merge;
        };
        StartJob(Settings.merge, setWork, Settings.mergeNeedsArchites, architesRequired, Settings.mergeBaseNeutroamine, Settings.mergeComplexityNeutroamine, Settings.workToMerge);
    }

    public void StartRecycle(Genepack pack, int architesInGenes)
    {
        Action setWork = () =>
        {
            genepackToWork = pack;
            workJob = WorkJob.Recycle;
        };
        StartJob(Settings.recycle, setWork, false, architesInGenes, Settings.recycleBaseNeutroamine, Settings.recycleComplexityNeutroamine, Settings.workToRecycle);
    }

    protected void StartJob(GenepackReprocessorSettings.CurveType curve, Action setWork, bool architesNeeded, int architesRequiredTotal, int baseNeutroamine, int complexityNeutroamine, float work)
    {
        Reset();
        this.architesInGenes = architesRequiredTotal;
        if (architesNeeded)
        {
            this.architesRequired = architesRequiredTotal;
        }
        else
        {
            this.architesRequired = 0;
        }
        setWork();

        //extracted the individual job set and merge/split genepack references from this location

        this.workingInt = true;

        switch (curve)
        {
            case GenepackReprocessorSettings.CurveType.Linear:
                totalWorkRequired = GenepackLinCurves.ComplexityToCreationHoursCurve.Evaluate(TotalGCX);
                break;
            case GenepackReprocessorSettings.CurveType.Exponetial:
                totalWorkRequired = GenepackExpCurves.ComplexityToCreationHoursCurve.Evaluate(TotalGCX);
                break;
            default:
                totalWorkRequired = GenepackLogCurve.ComplexityToCreationHoursCurve.Evaluate(TotalGCX);
                break;
        }

        neutroamineRequired = baseNeutroamine + (TotalGCX * complexityNeutroamine);
        totalWorkRequired *= 4000f * work;
        totalWorkRequired *= (1 + architesInGenes); // Penalty for archites in the genepack
    }

    /*
     Implement: updates the remaing work, workAmount fed in is probably based on another def
     public void DoWork(float workAmount)
     */
    public void DoWork(float workAmount)
    {
        workDone += workAmount;
        lastWorkAmount = workAmount;
        lastWorkedTick = Find.TickManager.TicksGame;
    }

    /*
     Implement: Called when a xenogerm is finished, spawns the xenogerm, deletes any archite capsoles, then resets()
     public void Finish()
     */
    public void Finish()
    {
        // Consume inputs
        if (architesRequired > 0)
        {
            for (int num = innerContainer.Count - 1; num >= 0; num--)
            {
                if (innerContainer[num].def == ThingDefOf.ArchiteCapsule)
                {
                    Thing thing = innerContainer[num].SplitOff(Mathf.Min(innerContainer[num].stackCount, architesRequired));
                    architesRequired -= thing.stackCount;
                    thing.Destroy();
                    if (architesRequired <= 0)
                    {
                        break;
                    }
                }
            }
        }
        if (neutroamineRequired > 0)
        {
            for (int num = innerContainer.Count - 1; num >= 0; num--)
            {
                if (innerContainer[num].def == GeneSeparator_DefOfs.Neutroamine)
                {
                    Thing thing = innerContainer[num].SplitOff(Mathf.Min(innerContainer[num].stackCount, neutroamineRequired));
                    neutroamineRequired -= thing.stackCount;
                    thing.Destroy();
                    if (neutroamineRequired <= 0)
                    {
                        break;
                    }
                }
            }
        }

        switch (workJob)
        {
            case WorkJob.Merge:
                FinishMerge();
                break;

            case WorkJob.Copy:
                FinishDuplicate();
                break;

            case WorkJob.Split:
                FinishSeparate();
                break;

            case WorkJob.Recycle:
                FinishRecycle();
                break;

            default:
                break;
        }

        Reset();
    }

    private void FinishMerge()
    {
        SoundDefOf.GeneAssembler_Complete.PlayOneShot(SoundInfo.InMap(this));
        if (!genepacksToMerge.NullOrEmpty())
        {
            List<GeneDef> genesToAdd = new List<GeneDef>();
            foreach (Genepack genep in genepacksToMerge)
            {
                foreach (GeneDef gened in genep.GeneSet.GenesListForReading)
                {
                    genesToAdd.Add(gened);
                }
            }

            Genepack genepack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);

            // Randomly reorder the genes in the new merged pack
            List<GeneDef> genesToAdd2 = new List<GeneDef>();

            int total = genesToAdd.Count;

            for (int i = 0; i < total; i++)
            {
                int index = Rand.Range(0, genesToAdd.Count);
                genesToAdd2.Add(genesToAdd[index]);
                genesToAdd.RemoveAt(index);
            }

            genepack.Initialize(genesToAdd2);

            if (GenPlace.TryPlaceThing(genepack, InteractionCell, base.Map, ThingPlaceMode.Near))
            {
                Messages.Message("GeneR_GenepackMergeFinished".Translate(), genepack, MessageTypeDefOf.PositiveEvent);
            }

            // TODO Settings
            // Lazy method, we could instead just search once, and destroy if the correct pack
            if (Settings.consumeOnMerge)
            {
                foreach (Genepack genep in genepacksToMerge)
                {
                    DestroyGeneBankHoldingPack(genep);
                }
            }
        }
    }

    private void FinishDuplicate()
    {
        if (genepackToWork != null)
        {
            Genepack genepack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);

            genepack.Initialize(genepackToWork.GeneSet.GenesListForReading);

            if (GenPlace.TryPlaceThing(genepack, InteractionCell, base.Map, ThingPlaceMode.Near))
            {
                Messages.Message("GeneR_GenepackCloneFinished".Translate(), genepack, MessageTypeDefOf.PositiveEvent);
            }
        }
    }

    private void FinishSeparate()
    {
        if (genepackToWork != null)
        {
            SoundDefOf.GeneAssembler_Complete.PlayOneShot(SoundInfo.InMap(this));
            // TODO: Create new genepacks here. Might be only a shallow copy, so if there's errors
            List<GeneDef> genesToAdd = new List<GeneDef>(genepackToWork.GeneSet.GenesListForReading);
            List<GeneDef> genesToAdd2 = new List<GeneDef>();

            Genepack genepack1 = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);

            // Check for a single gene, then just duplicate it.
            // NOTE: This code should never be accessable.
            if (genesToAdd.Count == 1)
            {
                Log.Warning("Genepack reprocessor: weird case of separating a 1 gene pack. Please report this as a bug and how you did it.");

                genepack1.Initialize(genesToAdd);

                if (GenPlace.TryPlaceThing(genepack1, InteractionCell, base.Map, ThingPlaceMode.Near))
                {
                    Messages.Message("GeneR_GenepackCloneFinished".Translate(), genepack1, MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                // Randomly pull out half of the genes, and assign them to genesToAdd2
                int half = genesToAdd.Count / 2;

                for (int i = 0; i < half; i++)
                {
                    int index = Rand.Range(0, genesToAdd.Count);
                    genesToAdd2.Add(genesToAdd[index]);
                    genesToAdd.RemoveAt(index);
                }

                Genepack genepack2 = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);

                genepack1.Initialize(genesToAdd);
                genepack2.Initialize(genesToAdd2);

                if (GenPlace.TryPlaceThing(genepack1, InteractionCell, base.Map, ThingPlaceMode.Near) &&
                    GenPlace.TryPlaceThing(genepack2, InteractionCell, base.Map, ThingPlaceMode.Near))
                {
                    Messages.Message("GeneR_GenepackSplitFinished".Translate(), genepack1, MessageTypeDefOf.PositiveEvent);
                }

                // TODO: If settings are destroy,
                if (Settings.consumeOnSplit) { DestroyGeneBankHoldingPack(genepackToWork); }

                // Lastly, check if we should queue another separate job.
                if (doForever)
                {
                    // Make sure we don't repeat separating the last job, as the packs won't have been loaded into banks yet.
                    List<GeneDef> justSplitGenes = new List<GeneDef>(genesToAdd);
                    justSplitGenes.Concat(genesToAdd2);


                    // NOTE: This code is awful! But works. If there's a noticeable lag ingame I might comeback to optimize this mess.

                    // Alright, now we create a list of genes already isolated, which we'll use to find the next job.
                    // new List<GeneDef>(genepackToSeparate.GeneSet.GenesListForReading);
                    List<GeneDef> doneGenes = new List<GeneDef>(justSplitGenes);
                    // Search through all nearby containers, and append that gene if it's isolated already.
                    List<Thing> connectedFacilities = ConnectedFacilities;
                    for (int i = 0; i < connectedFacilities.Count; i++)
                    {
                        // Check to see if the connected is a genepack container
                        CompGenepackContainer compGenepackContainer = connectedFacilities[i].TryGetComp<CompGenepackContainer>();
                        if (compGenepackContainer != null)
                        {
                            for (int j = 0; j < compGenepackContainer.ContainedGenepacks.Count; j++)
                            {
                                // Now for each pack, see if it's 1 gene.
                                Genepack temp = compGenepackContainer.ContainedGenepacks[j];
                                if (temp.GeneSet.GenesListForReading.Count == 1)
                                {
                                    // Add to done list if new.
                                    GeneDef geneTemp = temp.GeneSet.GenesListForReading[0];
                                    if (!doneGenes.Contains(geneTemp)) { doneGenes.Add(geneTemp); }
                                }
                            }
                        }
                    }
                    // Great, now do it again.
                    bool valid = false;
                    for (int i = 0; i < connectedFacilities.Count; i++)
                    {
                        // Check to see if the connected is a genepack container
                        CompGenepackContainer compGenepackContainer = connectedFacilities[i].TryGetComp<CompGenepackContainer>();
                        if (compGenepackContainer != null)
                        {
                            for (int j = 0; j < compGenepackContainer.ContainedGenepacks.Count; j++)
                            {
                                // Now for each pack, see if it's more than 1 gene.
                                Genepack temp = compGenepackContainer.ContainedGenepacks[j];
                                int arcs = 0;
                                if (temp.GeneSet.GenesListForReading.Count > 1)
                                {
                                    // Cool, it is. Now see if there's a unique gene. If so, we'll start a new separate job.
                                    for (int k = 0; k < temp.GeneSet.GenesListForReading.Count; k++)
                                    {
                                        GeneDef geneTemp = temp.GeneSet.GenesListForReading[k];
                                        arcs += geneTemp.biostatArc;
                                        if (!doneGenes.Contains(geneTemp)) { valid = true; }
                                    }
                                }
                                if (valid)
                                {
                                    // Job
                                    Reset();
                                    StartSplit(temp, arcs);
                                    return;
                                }
                            }
                        }
                    }
                    // If after all that no valid genepacks are found, then turn off forever
                    if (!valid)
                    {
                        doForever = false;
                    }
                }
            }
        }
    }

    private void FinishRecycle()
    {
        if (genepackToWork != null)
        {
            SoundDefOf.GeneAssembler_Complete.PlayOneShot(SoundInfo.InMap(this));

            var thing = ThingMaker.MakeThing(ThingDefOf.ArchiteCapsule);
            thing.stackCount = this.architesInGenes;

            if (GenPlace.TryPlaceThing(thing, InteractionCell, base.Map, ThingPlaceMode.Near))
            {
                Messages.Message("GeneR_GenepackRecycleFinished".Translate(), thing, MessageTypeDefOf.PositiveEvent);
            }

            if (Settings.consumeOnRecycle)
            {
                DestroyGeneBankHoldingPack(genepackToWork);
            }
        }
    }

    /*
    Implement: Returns the all genepacks that are valid given the flags below
    public List<Genepack> GetGenepacks(bool includePowered, bool includeUnpowered)
    */
    public List<Genepack> GetGenepacks(bool includePowered, bool includeUnpowered)
    {
        tmpGenepacks.Clear();
        List<Thing> connectedFacilities = ConnectedFacilities;
        if (connectedFacilities != null)
        {
            foreach (Thing item in connectedFacilities)
            {
                CompGenepackContainer compGenepackContainer = item.TryGetComp<CompGenepackContainer>();
                if (compGenepackContainer != null)
                {
                    bool flag = item.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;
                    if ((includePowered && flag) || (includeUnpowered && !flag))
                    {
                        tmpGenepacks.AddRange(compGenepackContainer.ContainedGenepacks);
                    }
                }
            }
        }
        return tmpGenepacks;
    }

    /*
     Implement: Returns the specific genebank with this pack
     public CompGenepackContainer GetGeneBankHoldingPack(Genepack pack)
     */
    public CompGenepackContainer GetGeneBankHoldingPack(Genepack pack)
    {
        List<Thing> connectedFacilities = ConnectedFacilities;
        if (connectedFacilities != null)
        {
            foreach (Thing item in connectedFacilities)
            {
                CompGenepackContainer compGenepackContainer = item.TryGetComp<CompGenepackContainer>();
                if (compGenepackContainer == null)
                {
                    continue;
                }
                foreach (Genepack containedGenepack in compGenepackContainer.ContainedGenepacks)
                {
                    if (containedGenepack == pack)
                    {
                        return compGenepackContainer;
                    }
                }
            }
        }
        return null;
    }

    public void DestroyGeneBankHoldingPack(Genepack pack)
    {
        List<Thing> connectedFacilities = ConnectedFacilities;
        if (connectedFacilities != null)
        {
            foreach (Thing item in connectedFacilities)
            {
                CompGenepackContainer compGenepackContainer = item.TryGetComp<CompGenepackContainer>();
                if (compGenepackContainer == null)
                {
                    continue;
                }
                foreach (Genepack containedGenepack in compGenepackContainer.ContainedGenepacks)
                {
                    if (containedGenepack == pack)
                    {
                        containedGenepack.Destroy();
                        return;
                    }
                }
            }
        }
    }

    /*
     Implement: Returns the max complexity avaliable from this + gene processors
     public int MaxComplexity()
     */

    /*
     Implement: Sets work vars to default (clears from the current job)
     private void Reset()
     */
    private void Reset()
    {
        workJob = WorkJob.None;
        workingInt = false;
        genepackToWork = null;
        genepacksToMerge = null;
        cachedComplexity = null;
        workDone = 0f;
        lastWorkedTick = -999;
        neutroamineRequired = 0;
        architesRequired = 0;
        innerContainer.TryDropAll(def.hasInteractionCell ? InteractionCell : base.Position, base.Map, ThingPlaceMode.Near);
    }

    private void Repeat()
    {
        doForever = !doForever;
    }

    /*
     Implement: Looks to see if the selected genes are in banks?
     private void CheckAllContainersValid()
     */
    private void CheckContainerValid()
    {
        if (workJob == WorkJob.Merge)
        {
            if (genepacksToMerge.NullOrEmpty())
            {
                return;
            }
            List<Thing> connectedFacilities = ConnectedFacilities;
            for (int i = 0; i < genepacksToMerge.Count; i++)
            {
                bool flag = false;
                for (int j = 0; j < connectedFacilities.Count; j++)
                {
                    CompGenepackContainer compGenepackContainer = connectedFacilities[j].TryGetComp<CompGenepackContainer>();
                    if (compGenepackContainer != null && compGenepackContainer.ContainedGenepacks.Contains(genepacksToMerge[i]))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    Messages.Message("GeneR_MessageMergeCancelledMissingPack".Translate(this), this, MessageTypeDefOf.NegativeEvent);
                    Reset();
                    break;
                }
            }
        }
        else
        {
            if (genepackToWork == null)
            {
                return;
            }
            List<Thing> connectedFacilities = ConnectedFacilities;
            bool flag = false;
            for (int j = 0; j < connectedFacilities.Count; j++)
            {
                CompGenepackContainer compGenepackContainer = connectedFacilities[j].TryGetComp<CompGenepackContainer>();
                if (compGenepackContainer != null && compGenepackContainer.ContainedGenepacks.Contains(genepackToWork))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                Messages.Message("GeneR_MessageGenepackCancelledMissingPack".Translate(this), this, MessageTypeDefOf.NegativeEvent);
                Reset();
            }
        }
    }

    /*
     Delegated commands, split here for Multiplayer compatablity.
     */
    public void SeparateGenepack()
    {
        Find.WindowStack.Add(new Dialog_SeparateGenepack(this));
    }

    public void DuplicateGenepack()
    {
        Find.WindowStack.Add(new Dialog_DuplicateGenepack(this));
    }

    public void MergeGenepack()
    {
        Find.WindowStack.Add(new Dialog_MergeGenepack(this));
    }

    public void RecycleGenepack()
    {
        Find.WindowStack.Add(new Dialog_RecycleArchiteGenepack(this));
    }

    private void DevFill()
    {
        if (NeutroamineRequiredNow > 0)
        {
            Thing neutro = ThingMaker.MakeThing(GeneSeparator_DefOfs.Neutroamine);
            neutro.stackCount = NeutroamineRequiredNow;

            innerContainer.TryAdd(neutro);
        }
        if (ArchitesRequiredNow > 0)
        {
            Thing archit = ThingMaker.MakeThing(ThingDefOf.ArchiteCapsule);
            archit.stackCount = ArchitesRequiredNow;

            innerContainer.TryAdd(archit);
        }
    }

    /*
     Implement: Critical, allows us to set up the split job.
     public override IEnumerable<Gizmo> GetGizmos() { }
     */
    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        // Split Genepack
        Command_Action command_Separate = new Command_Action();
        command_Separate.defaultLabel = "GeneR_SeparateGenepack".Translate() + "...";
        command_Separate.defaultDesc = "GeneR_SeparateDesc".Translate();
        command_Separate.icon = SeparateIcon.Texture;
        command_Separate.action = SeparateGenepack;

        // Duplicate Genepack
        Command_Action command_Duplicate = new Command_Action();
        command_Duplicate.defaultLabel = "GeneR_DublicGenepack".Translate() + "...";
        command_Duplicate.defaultDesc = "GeneR_DublicDesc".Translate();
        command_Duplicate.icon = DuplicateIcon.Texture;
        command_Duplicate.action = DuplicateGenepack;

        // Merge Genepacks
        Command_Action command_Merge = new Command_Action();
        command_Merge.defaultLabel = "GeneR_MergeGenepack".Translate() + "...";
        command_Merge.defaultDesc = "GeneR_MergeDesc".Translate();
        command_Merge.icon = MergeIcon.Texture;
        command_Merge.action = MergeGenepack;

        // Recycle Archite Genepack
        Command_Action command_Recycle = new Command_Action();
        command_Merge.defaultLabel = "GeneR_RecycleGenepack".Translate() + "...";
        command_Merge.defaultDesc = "GeneR_RecycleDesc".Translate();
        command_Merge.icon = RecycleIcon.Texture;
        command_Merge.action = RecycleGenepack;

        // TODO: see if we need to change this if the description is specific to the assembler
        if (!def.IsResearchFinished)
        {
            var disableReason = "MissingRequiredResearch".Translate()
                + ": "
                + (from x in def.researchPrerequisites
                   where !x.IsFinished
                   select x.label).ToCommaList(useAnd: true).CapitalizeFirst();

            command_Separate.Disable(disableReason);
            command_Merge.Disable(disableReason);
            command_Duplicate.Disable(disableReason);
            command_Recycle.Disable(disableReason);
        }
        else if (!PowerOn)
        {
            var cannotUseReason = "CannotUseNoPower".Translate();
            command_Separate.Disable(cannotUseReason);
            command_Duplicate.Disable(cannotUseReason);
            command_Merge.Disable(cannotUseReason);
            command_Recycle.Disable(cannotUseReason);
        }
        else if (!GetGenepacks(includePowered: true, includeUnpowered: false).Any())
        {
            var cannotUseReason = "CannotUseReason".Translate("NoGenepacksAvailable".Translate().CapitalizeFirst());
            command_Separate.Disable(cannotUseReason);
            command_Duplicate.Disable(cannotUseReason);
            command_Merge.Disable(cannotUseReason);
            command_Recycle.Disable(cannotUseReason);
        }

        //DEBUG which jobs are enabled
        DebugMessaging.DebugMessage_JobEnabled(nameof(Settings.separateEnabled), Settings.separateEnabled);
        DebugMessaging.DebugMessage_JobEnabled(nameof(Settings.duplicateEnabled), Settings.duplicateEnabled);
        DebugMessaging.DebugMessage_JobEnabled(nameof(Settings.mergeEnabled), Settings.mergeEnabled);
        DebugMessaging.DebugMessage_JobEnabled(nameof(Settings.recycleEnabled), Settings.recycleEnabled);

        // Hide the buttons if that command is disabled.
        if (Settings.separateEnabled) { yield return command_Separate; }
        if (Settings.duplicateEnabled) { yield return command_Duplicate; }
        if (Settings.mergeEnabled) { yield return command_Merge; }
        if (Settings.recycleEnabled) { yield return command_Recycle; }

        if (Working)
        {
            Command_Action command_Cancel = new Command_Action();
            command_Cancel.defaultLabel = "GeneR_CancelGenepack".Translate();
            command_Cancel.defaultDesc = "GeneR_CancelGenepackDesc".Translate();
            command_Cancel.action = Reset;
            command_Cancel.icon = CancelIcon;
            yield return command_Cancel;

            // Add repeat toggle
            if (workJob == WorkJob.Split)
            {
                Command_Toggle command_Repeat = new Command_Toggle();
                command_Repeat.defaultLabel = "GeneR_ToggleRepeat".Translate();
                command_Repeat.defaultDesc = "GeneR_ToggleRepeatDesc".Translate();
                command_Repeat.icon = RepeatIcon.Texture;
                command_Repeat.isActive = () => doForever;
                command_Repeat.toggleAction = Repeat;
                yield return command_Repeat;
            }

            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_DevInstantFinish = new Command_Action();
                command_DevInstantFinish.defaultLabel = "DEV: Finish Genepack";
                command_DevInstantFinish.action = Finish;
                yield return command_DevInstantFinish;

                if (NeutroamineRequiredNow > 0 || ArchitesRequiredNow > 0)
                {
                    Command_Action command_ActionFill = new Command_Action();
                    command_ActionFill.defaultLabel = "DEV: Fill Resources";
                    command_ActionFill.action = DevFill;
                    yield return command_ActionFill;
                }
            }
        }
    }

    /*
     Implement: The informational text provided while selected
     public override string GetInspectString() { }
     */
    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (Working)
        {
            AcceptanceReport canBeWorkedOnNow = CanBeWorkedOnNow;
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            DebugMessaging.DebugMessage_WorkJob(workJob);
            text += workJob switch
            {
                WorkJob.Merge => (string)"GeneR_MergeJob".Translate(),
                WorkJob.Split => (string)("GeneR_SeparateJob".Translate() + ": " + genepackToWork.LabelNoCount.CapitalizeFirst()),
                WorkJob.Copy => (string)("GeneR_CopyGenepack".Translate() + ": " + genepackToWork.LabelNoCount.CapitalizeFirst()),
                WorkJob.Recycle => (string)("GeneR_GenepackRecycle".Translate() + ": " + genepackToWork.LabelNoCount.CapitalizeFirst()),
                _ => string.Empty,
            };
            text += "\n" + "GeneR_ComplexityPenalty".Translate() + ": " + TotalGCX;
            text += workJob switch
            {
                WorkJob.Merge or WorkJob.Split or WorkJob.Copy =>
                    (architesInGenes > 0)
                        ? "\n" + "GeneR_ArchitePenalty".Translate() + ": " + (1 + architesInGenes).ToString() + "GeneR_X".Translate()
                        : string.Empty,
                _ => string.Empty,
            };

            text += "\n" + "Progress".Translate() + ": " + ProgressPercent.ToStringPercent();
            int numTicks = Mathf.RoundToInt((totalWorkRequired - workDone) / ((lastWorkAmount > 0f) ? lastWorkAmount : this.GetStatValue(StatDefOf.AssemblySpeedFactor)));
            text += " (" + "DurationLeft".Translate(numTicks.ToStringTicksToPeriod()).Resolve() + ")";

            if (architesRequired > 0)
            {
                text += ("\n" + "ArchitesRequired".Translate() + ": ") + ArchitesCount + " / " + architesRequired;
            }

            if (neutroamineRequired > 0)
            {
                text += ("\n" + "GeneR_NeutroamineRequired".Translate() + ": ") + NeutroamineCount + " / " + neutroamineRequired;
            }

            if (!canBeWorkedOnNow.Accepted && !canBeWorkedOnNow.Reason.NullOrEmpty())
            {
                text += "\n" + ("AssemblyPaused".Translate() + ": " + canBeWorkedOnNow.Reason).Colorize(ColorLibrary.RedReadable);
            }
        }

        return text;
    }

    /// <summary>
    /// I think this is what is saved by the game, say when we exit or load a save.
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref workJob, "workJob", WorkJob.None);

        //Backwards compatability -- keep the name the same
        Scribe_References.Look(ref genepackToWork, "genepacksToSeparate");  // What happens if there's a merge op?
        Scribe_Collections.Look(ref genepacksToMerge, "genepacksToMerge", LookMode.Reference);
        Scribe_Values.Look(ref workingInt, "workingInt", defaultValue: false);
        Scribe_Values.Look(ref workDone, "workDone", 0f);
        Scribe_Values.Look(ref totalWorkRequired, "totalWorkRequired", 0f);
        Scribe_Values.Look(ref lastWorkedTick, "lastWorkedTick", -999);
        Scribe_Values.Look(ref architesRequired, "architesRequired", 0);
        Scribe_Values.Look(ref neutroamineRequired, "neutroamineRequired", 0);
        Scribe_Values.Look(ref doForever, "doForever", false);
    }

    // TODO: Interface
    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }
}
