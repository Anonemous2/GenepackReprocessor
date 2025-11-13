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

public class Dialog_DuplicateGenepack : DialogBase_GenepackReprocessor
{
    protected override string Header => "GeneR_DuplicateGenes".Translate();

    protected override bool SetGenepack => true;

    protected override IEnumerable<Genepack> FilteredLibraryGenepacks => this.libraryGenepacks;

    public Dialog_DuplicateGenepack(Building_GeneSeparator geneSeparator) : base(geneSeparator) { }

    // Remove the xenotype name requirement.
    protected override bool CanAccept()
    {
        List<GeneDef> selectedGenes = SelectedGenes;
        if (!selectedGenepacks.Any())
        {
            Messages.Message("GeneR_MessageNoSelectedGenepack".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        // foreach (GeneDef selectedGene in SelectedGenes)
        // {
        // if (selectedGene.prerequisite != null && !selectedGenes.Contains(selectedGene.prerequisite))
        // {
        // Messages.Message("MessageGeneMissingPrerequisite".Translate(selectedGene.label).CapitalizeFirst() + ": " + selectedGene.prerequisite.LabelCap, null, MessageTypeDefOf.RejectInput, historical: false);
        // return false;
        // }
        // }
        return true;
    }

    protected override void Accept()
    {
        if (geneSeparator.Working)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeneR_ConfirmGenepack".Translate(), StartDuplicate, destructive: true));
        }
        else
        {
            StartDuplicate();
        }
    }

    private void StartDuplicate()
    {
        geneSeparator.StartDuplicate(SelectedGenepack, arc);
        SoundDefOf.StartRecombining.PlayOneShotOnCamera();
        Close(doCloseSound: false);
    }

    protected override void DoBottomButtons(Rect rect)
    {
        this.DoBottomButtons(rect, Settings.dupli, Settings.workToDupli);
    }
}
