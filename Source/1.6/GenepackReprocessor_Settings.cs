/*
 * User: Anonemous2
 * Date: 13-06-2024
 */
using System;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using UnityEngine;
using Verse;
using static GenepackReprocessor.GenepackReprocessorSettings;

namespace GenepackReprocessor;

public class GenepackImprovMod : Mod
{
    GenepackReprocessorSettings settings;

    // Var for controlling the display of settings, so it's a little nicer to navagate.
    private enum CurWindow : int
    {
        Main        = -1,
        Building    = 0,
        Work        = 1,
    }
    private CurWindow curWindow = CurWindow.Main;
    private CurWindow preWindow = CurWindow.Main;

    // An intermediate int value we'll round.
    int roundedFactor;

    // Magic numbers, for editing the layout from one spot.
    private const float labelPart = 0.7f;
    private const float fieldOffs = 0.1f;
    private const float floatOffset = 30f;
    private const float layoutRowHeight = 1.1f;           //1.1 times the size of text
    private const float layoutRowsTextBoxes2 = 2.133f;    //2 rows of text boxes, but not 1.1 * 2 :-/
    private const float layoutEnabledWork = 3.7f;         //3 rows of enabled settings values -- checkbox, slider, radio button

    // Buffers for text fields!
    string bufSteel = string.Empty;
    string bufPlast = string.Empty;
    string bufGold  = string.Empty;
    string bufCompo = string.Empty;
    string bufAdvCo = string.Empty;

    string bufHP           = string.Empty;
    string bufBuildWork    = string.Empty;
    string bufMass         = string.Empty;
    string bufFlammability = string.Empty;
    string bufSkillNeeded  = string.Empty;

    string bufPowerIdle = string.Empty;
    string bufPowerUsin = string.Empty;

    string bufSeparateBaseNeutroamine = string.Empty;
    string bufSeparateComplexityNeutroamine = string.Empty;

    string bufDuplicateBaseNeutroamine = string.Empty;
    string bufDuplicateComplexityNeutroamine = string.Empty;

    string bufRecycleBaseNeutroamine = string.Empty;
    string bufRecycleComplexityNeutroamine = string.Empty;

    string bufMergeBaseNeutroamine = string.Empty;
    string bufMergeComplexityNeutroamine = string.Empty;

    string bufArchitePen = string.Empty;

    private Vector2 _scrollPosition = new(0f, 0f);
    private float _totalContentHeight = 700f;
    private const float SCROLL_BAR_WIDTH_MARGIN = 20f;

    /// <summary>
    /// A mandatory constructor which resolves the reference to our settings.
    /// </summary>
    /// <param name="content"></param>
    public GenepackImprovMod(ModContentPack content) : base(content)
    {
        this.settings = GetSettings<GenepackReprocessorSettings>();

        // Initalize the buffers
        bufSteel = settings.costSteel.ToString();
        bufPlast = settings.costPlast.ToString();
        bufGold  = settings.costGold.ToString();
        bufCompo = settings.costCompo.ToString();
        bufAdvCo = settings.costAdvCo.ToString();

        bufHP           = settings.hp.ToString();
        bufBuildWork    = settings.buildWork.ToString();
        bufMass         = settings.mass.ToString();
        bufFlammability = settings.flammability.ToString();
        bufSkillNeeded  = settings.skillNeeded.ToString();

        bufPowerIdle = settings.powerIdle.ToString();
        bufPowerUsin = settings.powerUsin.ToString();

        bufSeparateBaseNeutroamine       = settings.separateBaseNeutroamine.ToString();
        bufSeparateComplexityNeutroamine = settings.separateComplexityNeutroamine.ToString();

        bufDuplicateBaseNeutroamine       = settings.duplicateBaseNeutroamine.ToString();
        bufDuplicateComplexityNeutroamine = settings.duplicateComplexityNeutroamine.ToString();

        bufMergeBaseNeutroamine = settings.mergeBaseNeutroamine.ToString();
        bufMergeComplexityNeutroamine = settings.mergeComplexityNeutroamine.ToString();

        bufRecycleBaseNeutroamine = settings.recycleBaseNeutroamine.ToString();
        bufRecycleComplexityNeutroamine = settings.recycleComplexityNeutroamine.ToString();

        bufArchitePen = settings.architePen.ToString();
    }

    /// <summary>
    /// Default and main menu when entering our mod's settings. Should allow you to
    /// navigate to the other menus. TODO: Might not need to use it.
    /// </summary>
    /// <param name="listing">Context window to add stuff into.</param>
    public void WindowContentsMain(ref Listing_Custom listing)
    {

    }

    public void ContentsBuildingCost(Listing_Custom listing, Rect viewRect)
    {
        // Create a subsection for the costs.
        Listing_Custom sub = listing.BeginSection(Text.LineHeight * layoutRowsTextBoxes2, width: viewRect.width);

        Rect line = sub.GetRectLine();
        sub.ColLabel(line, 3, 0,
            "GeneR_Materials".Translate(), "GeneR_MaterialsHelp".Translate());
        sub.NGTextFieldNumericLabeled<int>(line, 3, 1,
            "GeneR_MatComp".Translate(), ref settings.costCompo, ref bufCompo, 0f, 75f, labelPart, fieldOffs, "GeneR_MatCompHelp".Translate());
        sub.NGTextFieldNumericLabeled<int>(line, 3, 2,
            "GeneR_MatAdvComp".Translate(), ref settings.costAdvCo, ref bufAdvCo, 0f, 75f, labelPart, fieldOffs, "GeneR_MatAdvCompHelp".Translate());
        sub.Gap();

        line = sub.GetRectLine();
        sub.NGTextFieldNumericLabeled<int>(line, 3, 0,
            "GeneR_MatSteel".Translate(), ref settings.costSteel, ref bufSteel, 0f, 500f, labelPart, fieldOffs, "GeneR_MatSteelHelp".Translate());
        sub.NGTextFieldNumericLabeled<int>(line, 3, 1,
            "GeneR_MatPlasteel".Translate(), ref settings.costPlast, ref bufPlast, 0f, 500f, labelPart, fieldOffs, "GeneR_MatPlasteelHelp".Translate());
        sub.NGTextFieldNumericLabeled<int>(line, 3, 2,
            "GeneR_MatGold".Translate(), ref settings.costGold, ref bufGold, 0f, 500f, labelPart, fieldOffs, "GeneR_MatGoldHelp".Translate());
        
        listing.EndSection(sub);
    }

    public void ContentsBuildingSettings(Listing_Custom listing, Rect inRect)
    {
        // Create a subsection for the stats.
        Listing_Custom sub = listing.BeginSection(Text.LineHeight * layoutRowsTextBoxes2, width: inRect.width);

        Rect line = sub.GetRectLine();
        sub.NGTextFieldNumericLabeled<int>(line, 3, 0,
            "GeneR_HP".Translate(), ref settings.hp, ref bufHP, 1f, 10000, labelPart, fieldOffs, "GeneR_HPHelp".Translate());
        sub.NGTextFieldNumericLabeled<int>(line, 3, 1,
            "GeneR_BuildWork".Translate(), ref settings.buildWork, ref bufBuildWork, 0f, 100000f, labelPart, fieldOffs, "GeneR_BuildWorkHelp".Translate());
        sub.NGTextFieldNumericLabeled<int>(line, 3, 2,
            "GeneR_Mass".Translate(), ref settings.mass, ref bufMass, 1f, 100f, labelPart, fieldOffs, "GeneR_MassHelp".Translate());
        sub.Gap();

        line = sub.GetRectLine();
        sub.NGTextFieldNumericLabeled<float>(line, 3, 0,
            "GeneR_Flam".Translate(), ref settings.flammability, ref bufFlammability, 0f, 1f, labelPart, fieldOffs, "GeneR_FlamHelp".Translate());
        sub.NGTextFieldNumericLabeled<int>(line, 3, 1,
            "GeneR_BuildSkill".Translate(), ref settings.skillNeeded, ref bufSkillNeeded, 0f, 20f, labelPart, fieldOffs, "GeneR_BuildSkillHelp".Translate());
        sub.NGCheckboxLabeled(line, 3, 2, "GeneR_Minify".Translate(), ref settings.movable, fieldOffs, "GeneR_MinifyHelp".Translate()); // TODO: Translate.

        listing.EndSection(sub);
    }

    // TODO: Implement.
    public void ContentsBuildingPower(Listing_Custom listing, Rect inRect)
    {
        // Create a subsection for the power drain.
        Listing_Custom sub = listing.BeginSection(Text.LineHeight * layoutRowHeight, width: inRect.width);

        Rect line = sub.GetRectLine();
        sub.ColLabel(line, 3, 0,
            "Building Power Usage (W)", "How many Watts of power does this building consume while working.");
        sub.NGTextFieldNumericLabeled<int>(line, 3, 1,
            "Idle Drain:", ref settings.powerIdle, ref bufPowerIdle, 1f, 10000, labelPart, fieldOffs, "How many Watts of power does this building consume while idle.");
        sub.NGTextFieldNumericLabeled<int>(line, 3, 2,
            "Working Drain:", ref settings.powerUsin, ref bufPowerUsin, 0f, 1000f, labelPart, fieldOffs, "How many Watts of power does this building consume while working.");

        listing.EndSection(sub);
    }

    public void ContentsSeparating(Listing_Custom listing, Rect inRect)
    {
        DrawOptions_Work(listing, inRect, ref settings.separateEnabled, ref settings.workToSplit, ref settings.split,
            "GeneR_CanSeparate", "GeneR_CanSeparateHelp", "GeneR_SeparateMultiplierHelp",
            ref settings.separateBaseNeutroamine, ref bufSeparateBaseNeutroamine,
            ref settings.separateComplexityNeutroamine, ref bufSeparateComplexityNeutroamine,
            true, ref settings.separateNeedsArchites, true, ref settings.consumeOnSplit);
    }

    public void ContentsDuplicate(Listing_Custom parentSection, Rect inRect)
    {
        bool _ = default;   //never consumes genes. Discard.

        DrawOptions_Work(parentSection, inRect, ref settings.duplicateEnabled, ref settings.workToDupli, ref settings.dupli,
            "GeneR_CanDuplicate", "GeneR_CanDuplicateHelp", "GeneR_DuplicateMultiplierHelp",
            ref settings.duplicateBaseNeutroamine, ref bufDuplicateBaseNeutroamine,
            ref settings.duplicateComplexityNeutroamine, ref bufDuplicateComplexityNeutroamine,
            true, ref settings.duplicateNeedsArchites, false, ref _);
    }

    public void ContentsMerge(Listing_Custom parentSection, Rect inRect)
    {
        DrawOptions_Work(parentSection, inRect, ref settings.mergeEnabled, ref settings.workToMerge, ref settings.merge,
            "GeneR_CanMerge", "GeneR_CanMergeHelp", "GeneR_MergeMultiplierHelp",
            ref settings.mergeBaseNeutroamine, ref bufMergeBaseNeutroamine,
            ref settings.mergeComplexityNeutroamine, ref bufMergeComplexityNeutroamine,
            true, ref settings.mergeNeedsArchites, true, ref settings.consumeOnMerge);
    }

    public void ContentsRecycle(Listing_Custom parentSection, Rect inRect)
    {
        bool _ = default;   //never uses archite, creates it. Discard.

        DrawOptions_Work(parentSection, inRect, ref settings.recycleEnabled, ref settings.workToRecycle, ref settings.recycle,
            "GeneR_CanRecycle", "GeneR_CanRecycleHelp", "GeneR_RecycleMultiplierHelp",
            ref settings.recycleBaseNeutroamine, ref bufRecycleBaseNeutroamine,
            ref settings.recycleComplexityNeutroamine, ref bufRecycleComplexityNeutroamine,
            false, ref _, true, ref settings.consumeOnRecycle);
    }

    public void DrawOptions_Work(Listing_Custom parent, Rect inRect,
        ref bool enabled, ref float workRequired, ref CurveType curve,
        string jobName, string jobHelp, string jobMultiplierHelp,
        ref int neutroAmount, ref string bufferNeutroAmount,
        ref int neutroComplexity, ref string bufferNeutroComplexity,
        bool canRequireArchite, ref bool consumesArchite,
        bool canConsumePacks, ref bool consumesPacks)
    {
        // Create a subsection.
        var size = enabled ? layoutEnabledWork : layoutRowHeight;
        Listing_Custom subSection = parent.BeginSection(Text.LineHeight * size, width: inRect.width);

        DrawOptions_Enabled(subSection, ref enabled, jobName, jobHelp);
        if (!enabled)   //If it's not enabled, there's no reason to show them.
            parent.EndSection(subSection);
        else            // Else show all the settings.
        {
            // Work multiplier.
            DrawOptions_WorkMultiplier(subSection, ref workRequired, jobMultiplierHelp);

            // Work curve.
            DrawOptions_WorkCurve(parent, subSection, ref curve);
            parent.EndSection(subSection);

            // Consumption.
            DrawOptions_Consumption(parent, inRect, ref neutroAmount, ref bufferNeutroAmount,
                ref neutroComplexity, ref bufferNeutroComplexity,
                canRequireArchite, ref consumesArchite, canConsumePacks, ref consumesPacks);
        }
    }

    private void DrawOptions_Enabled(Listing_Custom subSection, ref bool enabled, string jobName, string jobHelp)
    {
        var line = subSection.GetRectLine();
        subSection.NGCheckboxLabeled(line, 1, 0, jobName.Translate(), ref enabled, fieldOffs / 4.5f,
            jobHelp.Translate());
    }

    private void DrawOptions_WorkMultiplier(Listing_Custom subSection, ref float workRequired, string jobMultiplierHelp)
    {
        var sliderLabel = "GeneR_WorkMultiplier".Translate() + workRequired.ToString("0.0") + "GeneR_X".Translate();
        roundedFactor = (int)(10f * subSection.SliderLabeled(sliderLabel, workRequired, 0.1f, 5, 0.25f, jobMultiplierHelp.Translate()));
        workRequired = (float)roundedFactor * 0.1f;
    }

    private void DrawOptions_WorkCurve(Listing_Custom parentSection, Listing_Custom subSection, ref CurveType curve)
    {
        Rect line = subSection.GetRectLine();
        if (parentSection.NGRadioButton(line, 3, 0, "GeneR_Logarithmic".Translate(),
            curve == GenepackReprocessorSettings.CurveType.Log, fieldOffs, "GeneR_LogarithmicHelp".Translate()))
        {
            curve = CurveType.Log;
        }

        if (parentSection.NGRadioButton(line, 3, 1, "GeneR_Linear".Translate(),
            curve == GenepackReprocessorSettings.CurveType.Linear, fieldOffs, "GeneR_LinearHelp".Translate()))
        {
            curve = CurveType.Linear;
        }

        if (parentSection.NGRadioButton(line, 3, 2, "GeneR_Exponential".Translate(),
            curve == GenepackReprocessorSettings.CurveType.Exponetial, fieldOffs, "GeneR_ExponentialHelp".Translate()))
        { 
            curve = CurveType.Exponetial;
        }
    }

    private void DrawOptions_Consumption(Listing_Custom parent, Rect inRect,
        ref int neutroAmount, ref string bufferNeutroAmount,
        ref int neutroComplexity, ref string bufferNeutroComplexity,
        bool canRequireArchite, ref bool consumesArchite,
        bool canConsumePacks, ref bool consumesPacks)
    {
        var size = layoutRowHeight;
        if (canRequireArchite || consumesPacks)
        {
            size = layoutRowsTextBoxes2;
        }
        if (canRequireArchite && consumesPacks)
        {
            size = 3.3f;
        }
        var subSection = parent.BeginSection((Text.LineHeight) * size, width: inRect.width);
        Rect line = subSection.GetRectLine();

        //neutro settings
        subSection.NGTextFieldNumericLabeled<int>(line, 3, 0,
            "GeneR_NeutroamineBase".Translate(), ref neutroAmount, ref bufferNeutroAmount,
            0f, 150f, labelPart, fieldOffs, "GeneR_NeutroamineBaseHelp".Translate());

        subSection.NGTextFieldNumericLabeled<int>(line, 3, 1,
            "GeneR_NeutroamineComp".Translate(), ref neutroComplexity, ref bufferNeutroComplexity,
            0f, 150f, labelPart, fieldOffs, "GeneR_NeutroamineCompHelp".Translate());

        if (canRequireArchite)
        {
            subSection.Gap();
            line = subSection.GetRectLine();
            subSection.NGCheckboxLabeled(line, 3, 0,
                "GeneR_ArchiteCapsulesSet".Translate(), ref consumesArchite, fieldOffs, "GeneR_ArchiteCapsulesSetHelp".Translate());
        }

        if (canConsumePacks)
        {
            subSection.Gap();
            line = subSection.GetRectLine();
            subSection.NGCheckboxLabeled(line, 3, 0,
                "GeneR_GenepackConsume".Translate(), ref consumesPacks, fieldOffs, "GeneR_GenepackConsumeHelp".Translate());
        }
        parent.EndSection(subSection);
    }

    // TODO: Implement. Also add translations after.
    public void ContentsArchiteSetting(ref Listing_Custom listing, Rect inRect)
    {
        // Create a subsection for the costs
        Rect line = new Rect(inRect.xMin, inRect.yMin, (inRect.width) / 2f, Text.LineHeight);

        // Else show all the settings.
        Listing_Custom sub = listing.CBeginSection(line, (line.height) * layoutRowHeight);

        sub.NGTextFieldNumericLabeled<float>(line, 1, 0,
            "Archite Penalty Multiplier:", ref settings.architePen, ref bufArchitePen, 0f, 5f, labelPart, fieldOffs, "Multiples the penalty for Archite genes.");
        roundedFactor = (int)(100f * settings.architePen);
        settings.architePen = (float)roundedFactor * 0.01f;

        listing.EndSection(sub);
    }

    /// <summary>
    /// The (optional) GUI part to set your settings.
    /// </summary>
    /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
    public override void DoSettingsWindowContents(Rect inRect)
    {
#if DEBUG
        var debugMsg = $"{nameof(inRect)} {{ {nameof(inRect.yMin)}: {inRect.yMin}; {nameof(inRect.yMax)}: {inRect.yMax}; {nameof(inRect.xMin)}: {inRect.xMin}; {nameof(inRect.xMax)}: {inRect.xMax} }} ... {nameof(_scrollPosition)}: {{ {nameof(_scrollPosition.x)}: {_scrollPosition.x}; {nameof(_scrollPosition.y)}: {_scrollPosition.y} }} ...";
        Messages.Message(debugMsg, null, MessageTypeDefOf.TaskCompletion, historical: false);
#endif
        Rect outerRect = new Rect(inRect);
        Rect settingsArea = new Rect(outerRect.xMin, outerRect.yMin, outerRect.width, outerRect.height - 80f);
        Rect bottomButtons = new Rect(outerRect.xMin - 10f, outerRect.yMax - 80f, outerRect.width - 20f, 40f);

        // Create the generic listing, which we'll fill with our settings.
        Listing_Custom listing = new Listing_Custom();
        listing.Begin(outerRect);

        // TODO: Add Reset and Hard buttons to the top of the window
        DrawSettings_DefaultButtons(listing, bottomButtons);
#if DEBUG
        if (Mouse.IsOver(bottomButtons))
        {
            Widgets.DrawHighlight(bottomButtons);
        }
#endif
        listing.End();

        DrawSettings_Variables(settingsArea);

        base.DoSettingsWindowContents(inRect);
    }

    private void DrawSettings_Variables(Rect settingsArea)
    {
        bool scrollBarVisible = _totalContentHeight > settingsArea.height;

#if DEBUG
        var debugMsg = $"{nameof(settingsArea)} {{ {nameof(settingsArea.yMin)}: {settingsArea.yMin}; {nameof(settingsArea.yMax)}: {settingsArea.yMax}; {nameof(settingsArea.xMin)}: {settingsArea.xMin}; {nameof(settingsArea.xMax)}: {settingsArea.xMax} }} ... {nameof(scrollBarVisible)}:{scrollBarVisible} ... ";
        Messages.Message(debugMsg, null, MessageTypeDefOf.TaskCompletion, historical: false);
#endif

        Rect scrollViewTotal = new Rect(0f, 0f, settingsArea.width - (scrollBarVisible ? SCROLL_BAR_WIDTH_MARGIN : 0f), _totalContentHeight);
        Widgets.BeginScrollView(settingsArea, ref _scrollPosition, scrollViewTotal);

        Rect viewRect = new Rect(0f, 0f, scrollViewTotal.width, 9999f);

#if DEBUG
        debugMsg = $"{nameof(viewRect)} {{ {nameof(viewRect.yMin)}: {viewRect.yMin}; {nameof(viewRect.yMax)}: {viewRect.yMax}; {nameof(viewRect.xMin)}: {viewRect.xMin}; {nameof(viewRect.xMax)}: {viewRect.xMax} }} ... {nameof(scrollBarVisible)}:{scrollBarVisible} ... ";
        Messages.Message(debugMsg, null, MessageTypeDefOf.TaskCompletion, historical: false);
#endif

        // Create the generic listing, which we'll fill with our settings.
        Listing_Custom listing = new Listing_Custom();
        listing.Begin(viewRect);

        ContentsBuildingCost(listing, viewRect);
        ContentsBuildingSettings(listing, viewRect);
        // ContentsBuildingPower(listing, viewRect); TEMP: Not used.

        // Work modes.
        DrawGapBetweenSections(listing);
        ContentsSeparating(listing, viewRect);

        DrawGapBetweenSections(listing);
        ContentsDuplicate(listing, viewRect);

        DrawGapBetweenSections(listing);
        ContentsMerge(listing, viewRect);

        DrawGapBetweenSections(listing);
        ContentsRecycle(listing, viewRect);

        // DrawGapBetweenSections(listing);
        // ContentsArchiteSetting(listing, viewRect);

        listing.End();

        Widgets.EndScrollView();

#if DEBUG
        if (Mouse.IsOver(settingsArea))
        {
            Widgets.DrawHighlight(settingsArea);
        }
#endif
    }

    private void DrawSettings_DefaultButtons(Listing_Custom listing, Rect bottom)
    {
#if DEBUG
        var debugMsg = $"{nameof(bottom)} {{ {nameof(bottom.yMin)}: {bottom.yMin}; {nameof(bottom.yMax)}: {bottom.yMax}; {nameof(bottom.xMin)}: {bottom.xMin}; {nameof(bottom.xMax)}: {bottom.xMax} }} ... ";
        Messages.Message(debugMsg, null, MessageTypeDefOf.TaskCompletion, historical: false);
#endif

        //did this "Default Settings" button just get pressed?
        if (listing.CButtonText(bottom, 6, 4, "GeneR_SetDefault".Translate(), null, "GeneR_SetDefaultHelp".Translate()))
        {
            ResetToDefaults();
            Messages.Message("GeneR_SetDefaultMes".Translate(), null, MessageTypeDefOf.TaskCompletion, historical: false);
        }

        //did this "Simple Settings" button just get pressed?
        if (listing.CButtonText(bottom, 6, 5, "GeneR_SetSimple".Translate(), null, "GeneR_SetSimpleHelp".Translate()))
        {
            ResetToSimple();
            Messages.Message("GeneR_SetSimpleMes".Translate(), null, MessageTypeDefOf.TaskCompletion, historical: false);
        }
    }

    public void DrawGapBetweenSections(Listing_Custom listing)
    {
        listing.Gap(20f);
    }

    // Clear buffers.
    public void ClearBuffers()
    {
        // Reset Buffers.
        bufSteel = settings.costSteel.ToString();
        bufPlast = settings.costPlast.ToString();
        bufGold = settings.costGold.ToString();
        bufCompo = settings.costCompo.ToString();
        bufAdvCo = settings.costAdvCo.ToString();

        bufHP = settings.hp.ToString();
        bufBuildWork = settings.buildWork.ToString();
        bufMass = settings.mass.ToString();
        bufFlammability = settings.flammability.ToString();
        bufSkillNeeded = settings.skillNeeded.ToString();

        bufPowerIdle = settings.powerIdle.ToString();
        bufPowerUsin = settings.powerUsin.ToString();

        bufSeparateBaseNeutroamine = settings.separateBaseNeutroamine.ToString();
        bufSeparateComplexityNeutroamine = settings.separateComplexityNeutroamine.ToString();

        bufDuplicateBaseNeutroamine = settings.duplicateBaseNeutroamine.ToString();
        bufDuplicateComplexityNeutroamine = settings.duplicateComplexityNeutroamine.ToString();

        bufMergeBaseNeutroamine = settings.mergeBaseNeutroamine.ToString();
        bufMergeComplexityNeutroamine = settings.mergeComplexityNeutroamine.ToString();

        bufRecycleBaseNeutroamine = settings.recycleBaseNeutroamine.ToString();
        bufRecycleComplexityNeutroamine = settings.recycleComplexityNeutroamine.ToString();

        bufArchitePen = settings.architePen.ToString();
    }

    // Defaults reset.
    public void ResetToDefaults()
    {
        settings.hp = 600;
        settings.buildWork = 24000;
        settings.movable = false;
        settings.mass = 30;
        settings.flammability = 0.5f;
        settings.skillNeeded = 6;
        // Repocessor materials cost.
        settings.costSteel = 200;
        settings.costPlast = 50;
        settings.costGold = 0;
        settings.costCompo = 6;
        settings.costAdvCo = 1;
        // Power drain.
        settings.powerIdle = 25;
        settings.powerUsin = 200;

        // Separate settings.
        settings.split = CurveType.Exponetial; // Work needed for x complexity.
        settings.separateEnabled = true; // Is this work mode usable ingame?
        settings.consumeOnSplit = true; // Destroy original genepacks?
        settings.workToSplit = 1.0f; // Work needed multiplier for each task type.
                                     // Separate materials cost.
        settings.separateBaseNeutroamine = 4;
        settings.separateComplexityNeutroamine = 2;
        settings.separateNeedsArchites = false;

        // Duplicate settings
        settings.dupli = CurveType.Linear;
        settings.duplicateEnabled = true;
        settings.workToDupli = 1.0f;
        // Duplicate materials cost.
        settings.duplicateBaseNeutroamine = 8;
        settings.duplicateComplexityNeutroamine = 4;
        settings.duplicateNeedsArchites = true;

        // Merge settings
        settings.merge = CurveType.Log;
        settings.mergeEnabled = true;
        settings.consumeOnMerge = false;
        settings.workToMerge = 1.0f;
        settings.genepackMergeMax = 9;
        // Merge materials cost.
        settings.mergeBaseNeutroamine = 6;
        settings.mergeComplexityNeutroamine = 3;
        settings.mergeNeedsArchites = true;

        // Recycle settings
        settings.recycle = CurveType.Exponetial;
        settings.recycleEnabled = true;
        settings.consumeOnRecycle = true;
        settings.workToRecycle = 3.0f;
        // Recycle materials cost.
        settings.recycleBaseNeutroamine = 12;
        settings.recycleComplexityNeutroamine = 6;

        settings.architePen = 1f;

        // TODO:
        // Worker settings
        settings.skillImportance = 1f;   // Multiplier on the skill's benefit/harm to work speed.
        settings.skillGain = 1f;   // Multiplier on the skill gain from creating genepacks.

        // Reset Buffers
        ClearBuffers();
    }

    // Simple settings reset
    public void ResetToSimple()
    {
        // Reset everything to defaults settings, then only update simple mode settings.
        ResetToDefaults();

        // Separate settings.
        settings.separateBaseNeutroamine = 0;
        settings.separateComplexityNeutroamine = 0;

        // Duplicate settings
        settings.duplicateEnabled = false;

        // Merge settings
        settings.mergeEnabled = false;

        // Separate settings
        settings.separateEnabled = false;

        // Recycle settings
        settings.recycleEnabled = false;

        // Reset Buffers
        ClearBuffers();
    }

    public override void WriteSettings()
    {
        curWindow = CurWindow.Main;         // Reset the settings window to main.
        base.WriteSettings();
        Messages.Message("GeneR_SetsSaved".Translate(), null, MessageTypeDefOf.TaskCompletion, historical: false);
        Log.Warning("Genepack reprocessor settings saved.");
        GenepackReprocessor_OnDefsLoaded.ApplySettingsToDefs();
    }

    /// <summary>
    /// Override SettingsCategory to show up in the list of settings.
    /// Using .Translate() is optional, but does allow for localisation.
    /// </summary>
    /// <returns>The (translated) mod name.</returns>
    public override string SettingsCategory()
    {
        return "GeneR_GR".Translate();
    }
}

[StaticConstructorOnStartup] // this makes the static constructor get called AFTER defs are loaded.
public class GenepackReprocessor_OnDefsLoaded
{
    // Settings for mod
    private static GenepackReprocessorSettings _settings;

    public static GenepackReprocessorSettings Settings => _settings ??= LoadedModManager.GetMod<GenepackImprovMod>().GetSettings<GenepackReprocessorSettings>();

    static GenepackReprocessor_OnDefsLoaded()
    {
        // Apply settings to defs now that defs are loaded:
        ApplySettingsToDefs();

        // Sticking MP compat here!
        try
        {
            ((Action)(() =>
            {
                if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Multiplayer"))
                {
                    // It's loading resources, must patch later.
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        var type = AccessTools.TypeByName("GenepackReprocessor.Building_GeneSeparator");
                        // Dialog separate
                        MP.RegisterSyncMethod(type, "StartSplit");
                        // Dialog duplicate
                        MP.RegisterSyncMethod(type, "StartDuplicate");
                        // Dialog merge
                        MP.RegisterSyncMethod(type, "StartMerge");
                        // Command finish, debug
                        MP.RegisterSyncMethod(type, "Finish");
                        // Command cancel
                        MP.RegisterSyncMethod(type, "Reset");
                        // Command repeat toggle, debug
                        MP.RegisterSyncMethod(type, "Repeat");
                        // Command fill, debug
                        MP.RegisterSyncMethod(type, "DevFill");

                        // TODO: See if we can sync some of the settings as well.
                    });
                }
            }))();
        }
        catch (TypeLoadException ex)
        {
            Log.Warning("Genepack multiplayer patch exception.");
        }
    }

    public static void ApplySettingsToDefs()
    {
        // ThingDef that we might want to change, best to took them up once.
        // It might be worth taking a note of what defs got changed, then only look them up if there's
        // a performance hit.
        ThingDef reprocessor = DefDatabase<ThingDef>.GetNamed("GeneSeparator");
        if (reprocessor != null)
        {
            // Update costList.
            reprocessor.costList.Clear();
            if (Settings.costSteel > 0)
            {
                reprocessor.costList.Add(new ThingDefCountClass(DefDatabase<ThingDef>.GetNamed("Steel"), Settings.costSteel));
            }
            if (Settings.costPlast > 0)
            {
                reprocessor.costList.Add(new ThingDefCountClass(DefDatabase<ThingDef>.GetNamed("Plasteel"), Settings.costPlast));
            }
            if (Settings.costGold > 0)
            {
                reprocessor.costList.Add(new ThingDefCountClass(DefDatabase<ThingDef>.GetNamed("Gold"), Settings.costGold));
            }
            if (Settings.costCompo > 0)
            {
                reprocessor.costList.Add(new ThingDefCountClass(DefDatabase<ThingDef>.GetNamed("ComponentIndustrial"), Settings.costCompo));
            }
            if (Settings.costAdvCo > 0)
            {
                reprocessor.costList.Add(new ThingDefCountClass(DefDatabase<ThingDef>.GetNamed("ComponentSpacer"), Settings.costAdvCo));
                // Add the help message about getting advanced components
                ResearchProjectDef process = DefDatabase<ResearchProjectDef>.GetNamed("GeneProcessor");
                process.discoveredLetterTitle = "GeneR_ResearchMes".Translate();
                process.discoveredLetterText = "GeneR_ResearchMesDes".Translate();
            }
            // Update Stats.
            reprocessor.statBases.Clear();

            StatModifier hp = new StatModifier();
            hp.stat = DefDatabase<StatDef>.GetNamed("MaxHitPoints");
            hp.value = Settings.hp;
            reprocessor.statBases.Add(hp);

            StatModifier work = new StatModifier();
            work.stat = DefDatabase<StatDef>.GetNamed("WorkToBuild");
            work.value = Settings.buildWork;
            reprocessor.statBases.Add(work);

            StatModifier mass = new StatModifier();
            mass.stat = DefDatabase<StatDef>.GetNamed("Mass");
            mass.value = Settings.mass;
            reprocessor.statBases.Add(mass);

            StatModifier flammability = new StatModifier();
            flammability.stat = DefDatabase<StatDef>.GetNamed("Flammability");
            flammability.value = Settings.flammability;
            reprocessor.statBases.Add(flammability);

            reprocessor.constructionSkillPrerequisite = Settings.skillNeeded;

            if (!Settings.movable)
            {
                reprocessor.minifiedDef = null;
                reprocessor.thingCategories.Clear();
            }
        }

        /* TEMP: Code that doesn't work yet. Don't worry about it.
        // Modify Stats for gene creation
        GeneSeparator_DefOfs.GenepackCreationSpeed.skillNeedFactors.Clear();
        GeneSeparator_DefOfs.GenepackCreationSpeed.skillNeedFactors.Add(new SkillNeed_BaseBonus() { skill = DefDatabase<SkillDef>.GetNamed("Intellectual"),
            baseValue = ,
            bonusPerLevel =
        });*/


        /* TODO: Figure out comps
        // Genebank
        foreach (CompProperties comp in reprocessor.comps)
        {
            if (comp is CompProperties_Power)
            {
                reprocessor.comps.Remove(comp);
                flag = true;
                break;
            }
        }
        if (flag)
        {
            // CompProperties =
            CompPowerTrader tempIn = new CompPowerTrader();
            tempIn

            CompProperties_Power tempC = new CompProperties_Power();
            tempC.compClass = tempIn;
        }

        if (flag) {
        CompProperties_GenepackContainer tempC = new CompProperties_GenepackContainer();
        tempC.maxCapacity = Settings.genebankCapacity;
            genebank.comps.Add(tempC);
        }
        flag = false;

        // Genepack
        StatModifier stat = new StatModifier();
        stat.stat = StatDef.Named("SellPriceFactor");
        stat.value = (float)Settings.geneSellFactor / 100f;
        foreach (StatModifier sta in genepack.statBases)
        {
            if (sta.GetHashCode == stat.GetHashCode)
            {
                genepack.statBases.Remove(sta);
                break;
            }
        }
        genepack.statBases.Add(stat);
        */
    }
}
