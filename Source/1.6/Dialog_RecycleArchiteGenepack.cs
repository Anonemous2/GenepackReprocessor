using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GenepackReprocessor;

/// <summary>Renders a UI dialog in which user will select archite genes for recycling</summary>
public class Dialog_RecycleArchiteGenepack : DialogBase_GenepackReprocessor
{
    protected override string Header => "GeneR_RecycleGenes".Translate();

    protected override bool SetGenepack => true;

    protected override IEnumerable<Genepack> FilteredLibraryGenepacks => this.libraryGenepacks
        .Where(gene => gene.GeneSet.ArchitesTotal > 0);

    /// <summary>Constructor</summary>
    /// <param name="geneSeparator">The specific <see cref="Building_GeneSeparator" /> that the dialog is rendering for</param>
    public Dialog_RecycleArchiteGenepack(Building_GeneSeparator geneSeparator) : base(geneSeparator) { }

    protected override bool CanAccept()
    {
        List<GeneDef> selectedGenes = SelectedGenes;
        if (!selectedGenepacks.Any())
        {
            Messages.Message("GeneR_MessageNoSelectedGenepack".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        foreach (GeneDef selectedGene in SelectedGenes)
        {
            if (selectedGenepacks.Sum(gene => gene.GeneSet.ArchitesTotal) < 1)
            {
                Messages.Message("GeneR_MessageNoArchiteGenes".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
        }
        return true;
    }

    protected override void Accept()
    {
        if (geneSeparator.Working)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("GeneR_ConfirmGenepack".Translate(), StartRecycle, destructive: true));
        }
        else
        {
            StartRecycle();
        }
    }

    private void StartRecycle()
    {
        geneSeparator.StartRecycle(SelectedGenepack, arc);
        SoundDefOf.StartRecombining.PlayOneShotOnCamera();
        Close(doCloseSound: false);
    }

    protected override void DoBottomButtons(Rect rect)
    {
        this.DoBottomButtons(rect, Settings.recycle, Settings.workToRecycle);
    }
}
