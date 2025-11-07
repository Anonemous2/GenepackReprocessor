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
public class Dialog_SeparateGenepack : DialogBase_GenepackReprocessor
{
    protected override string Header => "GeneR_IsolateGenes".Translate();

    protected override bool SetGenepack => true;

    public Dialog_SeparateGenepack(Building_GeneSeparator geneSeparator) : base(geneSeparator) { }

    // Remove the xenotype name requirement.
    protected override bool CanAccept()
    {
        List<GeneDef> selectedGenes = SelectedGenes;
        foreach (GeneDef selectedGene in SelectedGenes)
        {
            // if (selectedGene.prerequisite != null && !selectedGenes.Contains(selectedGene.prerequisite))
            // {
            //    Messages.Message("MessageGeneMissingPrerequisite".Translate(selectedGene.label).CapitalizeFirst() + ": " + selectedGene.prerequisite.LabelCap, null, MessageTypeDefOf.RejectInput, historical: false);
            //    return false;
            // }
            if (!selectedGenepacks.Any())
            {
                Messages.Message("MessageNoSelectedGenepack".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            if (selectedGenes.Count < 2)
            {
                Messages.Message("GeneR_MessageTooFewGenes".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
        }
        return true;
    }

    protected override void Accept()
    {
        if (geneSeparator.Working)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeneR_ConfirmGenepack".Translate(), StartSeparation, destructive: true));
        }
        else
        {
            StartSeparation();
        }
    }

    private void StartSeparation()
    {
        geneSeparator.StartSplit(SelectedGenepack, arc);
        SoundDefOf.StartRecombining.PlayOneShotOnCamera();
        Close(doCloseSound: false);
    }

    protected override void DoBottomButtons(Rect rect)
    {
        this.DoBottomButtons(rect, Settings.split, Settings.workToSplit);
    }
}
