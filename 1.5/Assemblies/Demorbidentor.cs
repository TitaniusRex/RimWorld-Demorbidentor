using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;
using Verse.AI;


namespace Demorbidentor
{

  public class CompAbilityEffect_DemorbidentorCost : CompAbilityEffect
	{
		public new CompProperties_AbilityDemorbidentorCost Props => (CompProperties_AbilityDemorbidentorCost)props;
		

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			GeneUtility.OffsetDemorbidentor(parent.pawn, 0f - Props.demorbidentorCost);
		}

		public override bool GizmoDisabled(out string reason)
		{
			Gene_Demorbidentor gene_Demorbidentor = parent.pawn.genes?.GetFirstGeneOfType<Gene_Demorbidentor>();
			if (gene_Demorbidentor == null)
			{
				reason = "NoDemorbidentorGene".Translate(parent.pawn);
				return true;
			}
			if (gene_Demorbidentor.Value < Props.demorbidentorCost)
			{
				reason = "FewDemorbidentor".Translate(parent.pawn);
				return true;
			}
			float num = Props.demorbidentorCost;
			if (Props.demorbidentorCost > float.Epsilon && num > gene_Demorbidentor.Value)
			{
				reason = "FewDemorbidentor".Translate(parent.pawn);
				return true;
			}
			reason = null;
			return false;
		}
	}
  
}
