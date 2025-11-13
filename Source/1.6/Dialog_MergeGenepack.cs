/*
 * User: Anonemous2
 * Date: 13-06-2024
 */
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GenepackReprocessor;
public class Dialog_MergeGenepack : DialogBase_GenepackReprocessor
{
    protected override string Header => "GeneR_MergeGenes".Translate();

    protected override bool SetGenepack => false;

    protected override IEnumerable<Genepack> FilteredLibraryGenepacks => this.libraryGenepacks;

    public Dialog_MergeGenepack(Building_GeneSeparator geneSeparator) : base(geneSeparator) { }

    // Remove the xenotype name requirement.
    protected override bool CanAccept()
    {
        List<GeneDef> selectedGenes = SelectedGenes;
        int mergeLimit = Building_GeneSeparator.Settings.genepackMergeMax;
        if (selectedGenes.Count > mergeLimit) { // TODO: make this value dependant on settings
            Messages.Message("GeneR_MessageTooManyGenes".Translate(mergeLimit + 1).CapitalizeFirst(), null, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        // foreach (GeneDef selectedGene in SelectedGenes)
        // {
        //    if (selectedGene.prerequisite != null && !selectedGenes.Contains(selectedGene.prerequisite))
        //    {
        //        Messages.Message("MessageGeneMissingPrerequisite".Translate(selectedGene.label).CapitalizeFirst() + ": " + selectedGene.prerequisite.LabelCap, null, MessageTypeDefOf.RejectInput, historical: false);
        //        return false;
        //    }
        // }
        if (selectedGenepacks.Count < 2)
        {
            Messages.Message("GeneR_MessageSelectMoreGenepacks".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        return true;
    }

    protected override void Accept()
    {
        if (geneSeparator.Working)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeneR_ConfirmGenepack".Translate(), StartMerge, destructive: true));
        }
        else
        {
            StartMerge();
        }
    }

    private void StartMerge()
    {
        geneSeparator.StartMerge(selectedGenepacks, arc);
        SoundDefOf.StartRecombining.PlayOneShotOnCamera();
        Close(doCloseSound: false);
    }

    protected override void DoBottomButtons(Rect rect)
    {
        this.DoBottomButtons(rect, Settings.merge, Settings.workToMerge);
    }
}
