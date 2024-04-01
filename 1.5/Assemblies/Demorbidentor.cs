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

	public class CompProperties_AbilityDemorbidentorCost : CompProperties_AbilityEffect
	{
		public float demorbidentorCost;

		public CompProperties_AbilityDemorbidentorCost()
		{
			compClass = typeof(CompAbilityEffect_DemorbidentorCost);
		}

		public override IEnumerable<string> ExtraStatSummary()
		{
			yield return (string)("DemorbidentorCost".Translate() + ": ") + Mathf.RoundToInt(demorbidentorCost * 99f);/*100*/
		}
	}

	public class Gene_Demorbidentor : Gene_Resource, IGeneResourceDrain
	{
		public bool demorbidentorPacksAllowed = true;

		public Gene_Resource Resource => this;

		public Pawn Pawn => pawn;

		public bool CanOffset
		{
			get
			{
				if (Active)
				{
					return !pawn.Deathresting;
				}
				return false;
			}
		}

		public string DisplayLabel => Label + " (" + "Gene".Translate() + ")";

		public float ResourceLossPerDay => def.resourceLossPerDay;

		public override float InitialResourceMax => 3f;

		public override float MinLevelForAlert => 0.15f;

		public override float MaxLevelOffset => 0.1f;

		protected override Color BarColor => new ColorInt(3, 138, 3).ToColor;

		protected override Color BarHighlightColor => new ColorInt(45, 142, 42).ToColor;

		public override void PostAdd()
		{
			if (ModLister.CheckBiotech("Demorbidentor"))
			{
				base.PostAdd();
				Reset();
			}
		}

		public override void Notify_IngestedThing(Thing thing, int numTaken)
		{
			if (thing.def.IsMeat)
			{
				IngestibleProperties ingestible = thing.def.ingestible;
				if (ingestible != null && ingestible.sourceDef?.race?.Humanlike == true)
				{
					GeneUtility.OffsetDemorbidentor(pawn, 0.0375f * thing.GetStatValue(StatDefOf.Nutrition) *  (float)numTaken);
				}
			}
		}

		public override void Tick()
		{
			base.Tick();
			GeneResourceDrainDemorbidentorUtility.TickResourceDrain(this);
		}

		public override void SetTargetValuePct(float val)
		{
			targetValue = Mathf.Clamp(val * Max, 0f, Max - MaxLevelOffset);
		}

		public bool ShouldConsumeDemorbidentorNow()
		{
			return Value < targetValue;
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			foreach (Gizmo resourceDrainGizmo in GeneResourceDrainDemorbidentorUtility.GetResourceDrainGizmos(this))
			{
				yield return resourceDrainGizmo;
			}
		}

	}

	[StaticConstructorOnStartup]
	public static class GeneUtility
	{
		public static void OffsetDemorbidentor(Pawn pawn, float offset, bool applyStatFactor = true)
		{
			if (!ModsConfig.BiotechActive)
			{
				return;
			}
			if (offset > 0f && applyStatFactor)
			{
				offset *= pawn.GetStatValue(StatDefOf.DemorbidentorGainFactor);
			}
			Gene_DemorbidentorDrain gene_DemorbidentorDrain = pawn.genes?.GetFirstGeneOfType<Gene_DemorbidentorDrain>();
			if (gene_DemorbidentorDrain != null)
			{
				GeneResourceDrainDemorbidentorUtility.OffsetResource(gene_DemorbidentorDrain, offset);
				return;
			}
			Gene_Demorbidentor gene_Demorbidentor = pawn.genes?.GetFirstGeneOfType<Gene_Demorbidentor>();
			if (gene_Demorbidentor != null)
			{
				gene_Demorbidentor.Value += offset;
			}
		}
	}


	[DefOf]
	public static class StatDefOf
	{
		[MayRequireBiotech]
		public static StatDef HemogenGainFactor;
		public static StatDef MedicalPotency;
		public static StatDef DemorbidentorGainFactor;
		public static StatDef Nutrition;
	}

	
  
}
