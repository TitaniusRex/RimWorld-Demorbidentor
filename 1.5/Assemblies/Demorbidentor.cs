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

	public class Gene_DemorbidentorDrain : Gene, IGeneResourceDrain
	{
		[Unsaved(false)]
		private Gene_Demorbidentor cachedDemorbidentorGene;

		private const float MinAgeForDrain = 13f;

		public Gene_Resource Resource
		{
			get
			{
				if (cachedDemorbidentorGene == null || !cachedDemorbidentorGene.Active)
				{
					cachedDemorbidentorGene = pawn.genes.GetFirstGeneOfType<Gene_Demorbidentor>();
				}
				return cachedDemorbidentorGene;
			}
		}


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

		public float ResourceLossPerDay => def.resourceLossPerDay;

		public Pawn Pawn => pawn;

		public string DisplayLabel => Label + " (" + "Gene".Translate() + ")";

		public override void Tick()
		{
			base.Tick();
			GeneResourceDrainDemorbidentorUtility.TickResourceDrain(this);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo resourceDrainGizmo in GeneResourceDrainDemorbidentorUtility.GetResourceDrainGizmos(this))
			{
				yield return resourceDrainGizmo;
			}
		}
	}

	[StaticConstructorOnStartup]
	public class GeneGizmo_ResourceDemorbidentor : GeneGizmo_Resource
	{
		private static readonly Texture2D DemorbidentorCostTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.78f, 0.72f, 0.66f));

		private const float TotalPulsateTime = 0.85f;

		private List<Pair<IGeneResourceDrain, float>> tmpDrainGenes = new List<Pair<IGeneResourceDrain, float>>();

		public GeneGizmo_ResourceDemorbidentor(Gene_Resource gene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barhighlightColor)
			: base(gene, drainGenes, barColor, barhighlightColor)
		{
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
			float num = Mathf.Repeat(Time.time, 0.85f);
			float num2 = 1f;
			if (num < 0.1f)
			{
				num2 = num / 0.1f;
			}
			else if (num >= 0.25f)
			{
				num2 = 1f - (num - 0.25f) / 0.6f;
			}
			if (((MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow)?.LastMouseoverGizmo is Command_Ability command_Ability && gene.Max != 0f)
			{
				foreach (CompAbilityEffect effectComp in command_Ability.Ability.EffectComps)
				{
					if (effectComp is CompAbilityEffect_DemorbidentorCost compAbilityEffect_DemorbidentorCost && compAbilityEffect_DemorbidentorCost.Props.demorbidentorCost > float.Epsilon)
					{
						Rect rect = barRect.ContractedBy(3f);
						float width = rect.width;
						float num3 = gene.Value / gene.Max;
						rect.xMax = rect.xMin + width * num3;
						float num4 = Mathf.Min(compAbilityEffect_DemorbidentorCost.Props.demorbidentorCost / gene.Max, 1f);
						rect.xMin = Mathf.Max(rect.xMin, rect.xMax - width * num4);
						GenUI.DrawTextureWithMaterial(rect, DemorbidentorCostTex, null);
						return result;
					}
				}
				return result;
			}
			return result;
		}
		protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
		{
			Gene_Demorbidentor demorbidentorGene;
			if (IsDraggable && (demorbidentorGene = gene as Gene_Demorbidentor) != null)
			{
				headerRect.xMax -= 24f;
				Rect rect = new Rect(headerRect.xMax, headerRect.y, 24f, 24f);
				Widgets.DefIcon(rect, ThingDefOf.DemorbidentorPack);
				if (Widgets.ButtonInvisible(rect))
				{
					demorbidentorGene.demorbidentorPacksAllowed = !demorbidentorGene.demorbidentorPacksAllowed;
					if (demorbidentorGene.demorbidentorPacksAllowed)
					{
						SoundDefOf.Tick_High.PlayOneShotOnCamera();
					}
					else
					{
						SoundDefOf.Tick_Low.PlayOneShotOnCamera();
					}
				}
			}
			base.DrawHeader(headerRect, ref mouseOverElement);
		}

  		protected override string GetTooltip()
		{
			tmpDrainGenes.Clear();
			string text = $"{gene.ResourceLabel.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor)}: {gene.ValueForDisplay} / {gene.MaxForDisplay}\n";
			
			if (!drainGenes.NullOrEmpty())
			{
				float num = 0f;
				foreach (IGeneResourceDrain drainGene in drainGenes)
				{
					if (drainGene.CanOffset)
					{
						tmpDrainGenes.Add(new Pair<IGeneResourceDrain, float>(drainGene, drainGene.ResourceLossPerDay));
						num += drainGene.ResourceLossPerDay;
					}
				}
				if (num != 0f)
				{
					string text2 = ((num < 0f) ? "RegenerationRate".Translate() : "DrainRate".Translate());
					text = text + "\n\n" + text2 + ": " + "PerDay".Translate(Mathf.Abs(gene.PostProcessValue(num))).Resolve();
					foreach (Pair<IGeneResourceDrain, float> tmpDrainGene in tmpDrainGenes)
					{
						text = text + "\n  - " + tmpDrainGene.First.DisplayLabel.CapitalizeFirst() + ": " + "PerDay".Translate(gene.PostProcessValue(0f - tmpDrainGene.Second).ToStringWithSign()).Resolve();
					}
				}
			}
			if (!gene.def.resourceDescription.NullOrEmpty())
			{
				text = text + "\n\n" + gene.def.resourceDescription.Formatted(gene.pawn.Named("PAWN")).Resolve();
			}
			return text;
		}
	}

	public class IngestionOutcomeDoer_OffsetDemorbidentor : IngestionOutcomeDoer
	{
		public float offset;
		protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
		{
			GeneUtility.OffsetDemorbidentor(pawn, offset * (float)ingestedCount);
		}
		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
		{
			if (ModsConfig.BiotechActive)
			{
				string text = ((offset >= 0f) ? "+" : string.Empty);
				yield return new StatDrawEntry(StatCategoryDefOf.BasicsNonPawnImportant, "Demorbidentor".Translate().CapitalizeFirst(), text + Mathf.RoundToInt(offset * 100f), "DemorbidentorDesc".Translate(), 1000);
			}
		}
	}
 
	public class JobGiver_GetDemorbidentor : ThinkNode_JobGiver
	{
		private static float? cachedDemorbidentorPackDemorbidentorGain;

		public static float DemorbidentorPackDemorbidentorGain
		{
			get
			{
				if (!cachedDemorbidentorPackDemorbidentorGain.HasValue)
				{
					if (!ModsConfig.BiotechActive)
					{
						cachedDemorbidentorPackDemorbidentorGain = 0f;
					}
					else if (!(ThingDefOf.DemorbidentorPack.ingestible?.outcomeDoers?.FirstOrDefault((IngestionOutcomeDoer x) => x is IngestionOutcomeDoer_OffsetDemorbidentor) is IngestionOutcomeDoer_OffsetDemorbidentor ingestionOutcomeDoer_OffsetDemorbidentor))
					{
						cachedDemorbidentorPackDemorbidentorGain = 0f;
					}
					else
					{
						cachedDemorbidentorPackDemorbidentorGain = ingestionOutcomeDoer_OffsetDemorbidentor.offset;
					}
				}
				return cachedDemorbidentorPackDemorbidentorGain.Value;
			}
		}

		public static void ResetStaticData()
		{
			cachedDemorbidentorPackDemorbidentorGain = null;
		}

		public override float GetPriority(Pawn pawn)
		{
			if (!ModsConfig.BiotechActive)
			{
				return 0f;
			}
			if (pawn.genes?.GetFirstGeneOfType<Gene_Demorbidentor>() == null)
			{
				return 0f;
			}
			return 9.1f;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!ModsConfig.BiotechActive)
			{
				return null;
			}
			Gene_Demorbidentor gene_Demorbidentor = pawn.genes?.GetFirstGeneOfType<Gene_Demorbidentor>();
			if (gene_Demorbidentor == null)
			{
				return null;
			}
			if (!gene_Demorbidentor.ShouldConsumeDemorbidentorNow())
			{
				return null;
			}
			if (gene_Demorbidentor.demorbidentorPacksAllowed)
			{
				int num = Mathf.FloorToInt((gene_Demorbidentor.Max - gene_Demorbidentor.Value) / DemorbidentorPackDemorbidentorGain);
				if (num > 0)
				{
					Thing demorbidentorPack = GetDemorbidentorPack(pawn);
					if (demorbidentorPack != null)
					{
						Job job = JobMaker.MakeJob(JobDefOf.Ingest, demorbidentorPack);
						job.count = Mathf.Min(demorbidentorPack.stackCount, num);
						job.ingestTotalCount = true;
						return job;
					}
				}
			}
			return null;
		}

		private Thing GetDemorbidentorPack(Pawn pawn)
		{
			Thing carriedThing = pawn.carryTracker.CarriedThing;
			if (carriedThing != null && carriedThing.def == ThingDefOf.DemorbidentorPack)
			{
				return carriedThing;
			}
			for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
			{
				if (pawn.inventory.innerContainer[i].def == ThingDefOf.DemorbidentorPack)
				{
					return pawn.inventory.innerContainer[i];
				}
			}
			return GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerThings.ThingsOfDef(ThingDefOf.DemorbidentorPack), PathEndMode.OnCell, TraverseParms.For(pawn), 9999f, (Thing t) => pawn.CanReserve(t) && !t.IsForbidden(pawn));
		}
	}

	[DefOf]
	public static class ThingDefOf
	{
		public static ThingDef DemorbidentorPack;
	}

	[DefOf]
	public static class GeneDefOf
	{
		[MayRequireBiotech]
		public static GeneDef Demorbidentor;

	}

	public static class GeneResourceDrainDemorbidentorUtility
	{
		public static void TickResourceDrain(IGeneResourceDrain drain)
		{
			if (drain.CanOffset && drain.Resource != null)
			{
				OffsetResource(drain, (0f - drain.ResourceLossPerDay) / 60000f);
			}
		}

		public static void PostResourceOffset(IGeneResourceDrain drain, float oldValue)
		{
			if (oldValue > 0f && drain.Resource.Value <= 0f)
			{
				Pawn pawn = drain.Pawn;
				if (!pawn.health.hediffSet.HasHediff(HediffDefOf.DemorbidentorCraving))
				{
					pawn.health.AddHediff(HediffDefOf.DemorbidentorCraving);
				}
			}
		}

		public static void OffsetResource(IGeneResourceDrain drain, float amnt)
		{
			float value = drain.Resource.Value;
			drain.Resource.Value += amnt;
			PostResourceOffset(drain, value);
		}

		public static IEnumerable<Gizmo> GetResourceDrainGizmos(IGeneResourceDrain drain)
		{
			if (DebugSettings.ShowDevGizmos && drain.Resource != null)
			{
				Gene_Resource resource = drain.Resource;
				Command_Action command_Action = new Command_Action();
				command_Action.defaultLabel = "" + resource.ResourceLabel + " -20";
				command_Action.action = delegate
				{
					OffsetResource(drain, -0.1f);
				};
				yield return command_Action;
				Command_Action command_Action2 = new Command_Action();
				command_Action2.defaultLabel = "" + resource.ResourceLabel + " +20";
				command_Action2.action = delegate
				{
					OffsetResource(drain, 0.1f);
				};
				yield return command_Action2;
			}
		}
	}

	[DefOf]
	public static class HediffDefOf
    {
		public static HediffDef DemorbidentorCraving;
	}

	public class HediffCompProperties_SeverityFromDemorbidentor : HediffCompProperties
	{
		public float severityPerHourEmpty;

		public float severityPerHourDemorbidentor;

		public HediffCompProperties_SeverityFromDemorbidentor()
		{
			compClass = typeof(HediffComp_SeverityFromDemorbidentor);
		}
	}


        public class HediffComp_SeverityFromDemorbidentor : HediffComp
	{
		private Gene_Demorbidentor cachedDemorbidentorGene;

		public HediffCompProperties_SeverityFromDemorbidentor Props => (HediffCompProperties_SeverityFromDemorbidentor)props;

		public override bool CompShouldRemove => base.Pawn.genes?.GetFirstGeneOfType<Gene_Demorbidentor>() == null;

		private Gene_Demorbidentor Demorbidentor
		{
			get
			{
				if (cachedDemorbidentorGene == null)
				{
					cachedDemorbidentorGene = base.Pawn.genes.GetFirstGeneOfType<Gene_Demorbidentor>();
				}
				return cachedDemorbidentorGene;
			}
		}

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			severityAdjustment += ((Demorbidentor.Value > 0f) ? Props.severityPerHourDemorbidentor : Props.severityPerHourEmpty) / 2500f;
		}
	}

	
	
}
