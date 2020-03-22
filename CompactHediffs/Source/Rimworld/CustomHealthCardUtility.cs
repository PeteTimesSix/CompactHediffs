using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using PeteTimesSix.CompactHediffs.Rimworld.UI;

namespace PeteTimesSix.CompactHediffs.Rimworld
{
	[StaticConstructorOnStartup]
	public static class CustomHealthCardUtility
	{
		//public static bool drawingFirstRow = true;

		//public static readonly Texture2D BleedingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Medical/Bleeding", true);

		public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		public static readonly Color StaticHighlightColor = new Color(0.75f, 0.75f, 0.85f, 1f);

		public static readonly float IconSize = 20f;

		private static Traverse<float> field_lastMaxIconsTotalWidth;
		private static Traverse<float> field_scrollViewHeight;
		private static Traverse<Vector2> field_scrollPosition;
		private static Traverse<bool> field_highlight;

		private static Traverse method_CanEntryBeClicked;
		private static Traverse method_EntryClicked;
		private static Traverse method_GetTooltip;
		private static Traverse method_getListPriority;

		static CustomHealthCardUtility() 
		{
			field_lastMaxIconsTotalWidth = Traverse.Create(typeof(HealthCardUtility)).Field<float>("lastMaxIconsTotalWidth");
			field_scrollViewHeight = Traverse.Create(typeof(HealthCardUtility)).Field<float>("scrollViewHeight");
			field_scrollPosition = Traverse.Create(typeof(HealthCardUtility)).Field<Vector2>("scrollPosition");
			field_highlight = Traverse.Create(typeof(HealthCardUtility)).Field<bool>("highlight");


			method_CanEntryBeClicked = Traverse.Create(typeof(HealthCardUtility)).Method("CanEntryBeClicked", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn) });
			method_EntryClicked = Traverse.Create(typeof(HealthCardUtility)).Method("EntryClicked", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn) });
			method_GetTooltip = Traverse.Create(typeof(HealthCardUtility)).Method("GetTooltip", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn), typeof(BodyPartRecord) });
			method_getListPriority = Traverse.Create(typeof(HealthCardUtility)).Method("GetListPriority", new Type[] { typeof(BodyPartRecord) });
		}

		public static Hediff GetReplacingPart(IEnumerable<Hediff> diffs, BodyPartRecord part) 
		{
			var replacingPart = diffs.Where(x => x is Hediff_AddedPart).FirstOrDefault();
			if (replacingPart != null)
				return replacingPart;

			foreach (var diff in diffs)
			{
				if (diff.GetType().ToString() == "Pawnmorph.Hediff_AddedMutation")
				{
					Def def = diff.def;

					var traverse = Traverse.Create(def).Property("IsCoreMutation");
					if (traverse.PropertyExists())
					{
						if(traverse.GetValue<bool>())
							return diff;
					}
					else
					{
						//TODO: Remove this when Pawnmorpher updates
						List<BodyPartDef> parts = Traverse.Create(def)?.Field<List<BodyPartDef>>("parts")?.Value;
						if (parts != null && parts.Count == 1 && parts[0] == part.def) //making an assumption here...
							return diff;
					}
				}
			}

			return null;
		}

		public static void DrawHediffRow(Rect rowRect, Pawn pawn, IEnumerable<Hediff> diffs, ref float curY)
        {
			var settings = CompactHediffsMod.settings;


			float column_bodypartWidth = (rowRect.width * 0.375f);
			float column_hediffLabelWidth = rowRect.width - column_bodypartWidth - field_lastMaxIconsTotalWidth.Value;

			BodyPartRecord part = diffs.First<Hediff>().Part;
			Hediff replacingPart = null;

			string bodyPartText;

			if (part == null)
			{
				bodyPartText = ColoredText.Colorize("WholeBody".Translate(), Color.grey);
			}
			else 
			{
				if (settings.replacingPartInBodypart)
					replacingPart = GetReplacingPart(diffs, part);

				Color healthColor = GetHealthColorForBodypart(pawn, part);
				if (replacingPart != null)
				{
					//diffs = diffs.Where(x => x != replacingPart); //do not list this hediff
					var replacingPartColorLabel = ColoredText.Colorize(replacingPart.Label, replacingPart.def.defaultLabelColor);
					var regex = new Regex(@"\b" + part.def.label + @"\b");
					if (regex.IsMatch(replacingPart.Label))
					{
						replacingPartColorLabel = replacingPartColorLabel.Replace(part.def.label, ColoredText.Colorize(part.def.label, healthColor));
						bodyPartText = ColoredText.Colorize(part.Label.Replace(part.def.label, replacingPartColorLabel), healthColor);
					}
					else
					{
						bodyPartText = ColoredText.Colorize(part.Label + ", ", healthColor) + replacingPartColorLabel;
					}
				}
				else
				{
					bodyPartText = ColoredText.Colorize(part.Label, healthColor);
				}
			}

			bodyPartText = bodyPartText.CapitalizeFirstNestingAware();

			float bodypartLabelWidth = column_bodypartWidth - IconSize / 2f;
			float bodyPartLabelHeight = Text.CalcHeight(bodyPartText, bodypartLabelWidth);
			float hediffTextsHeight = 0f;

			IEnumerable<IGrouping<int, Hediff>> groupings;
			if (replacingPart == null)
			{
				if(settings.tendPrioritySort)
					groupings = diffs.OrderByDescending(i => i.TendableNow(true) ? i.TendPriority : -1).GroupBy(x => x.UIGroupKey);
				else
					groupings = diffs.GroupBy(x => x.UIGroupKey);
			}
			else
			{
				if (settings.tendPrioritySort)
					groupings = diffs.Where(x => x != replacingPart).OrderByDescending(i => i.TendableNow(true) ? i.TendPriority : -1).GroupBy(x => x.UIGroupKey);
				else 
					groupings = diffs.Where(x => x != replacingPart).GroupBy(x => x.UIGroupKey);
			}

			foreach (IGrouping<int, Hediff> grouping in groupings)
			{
				string text = grouping.First<Hediff>().LabelCap;
				if (grouping.Count<Hediff>() != 1)
				{
					text = text + " x" + grouping.Count<Hediff>().ToString();
				}
				hediffTextsHeight += Text.CalcHeight(text, column_hediffLabelWidth);
			}

			Rect wholeEntryRect = new Rect(0f, curY, rowRect.width, Mathf.Max(bodyPartLabelHeight, hediffTextsHeight));

			if (settings.evenOddHighlights)
			{
				DoWholeRowHighlight(wholeEntryRect);
			}

			if (settings.bodypartHealthbars) 
			{
				Rect fullHealthPercentageRect = new Rect(0, curY, column_bodypartWidth, 3f);

				if (part != null)
				{
					Color healthColor = GetHealthColorForBodypart(pawn, part);
					float partHealthFraction;
					partHealthFraction = pawn.health.hediffSet.GetPartHealth(part) / part.def.GetMaxHealth(pawn);

					if (partHealthFraction < 1f)
					{
						GUI.color = Color.gray;
						GUI.DrawTexture(fullHealthPercentageRect, Textures.translucentWhite);
						if (partHealthFraction == 0)
						{
							GUI.color = Color.black;
							GUI.DrawTexture(fullHealthPercentageRect, TexUI.FastFillTex);
						}
						else
						{
							GUI.color = healthColor;
							Rect healthPercentageRect = new Rect(0, curY, column_bodypartWidth * partHealthFraction, 3f);
							//Rect healthPercentageRect = new Rect(0 + ((column_bodypartWidth * (1 - partHealthFraction)) / 2f), curY, column_bodypartWidth * partHealthFraction, 2);
							GUI.DrawTexture(healthPercentageRect, Textures.translucentWhite);
						}

					}
				}
			}

			if (settings.separatorNightMode)
				GUI.color = Color.black;
			else
				GUI.color = Color.grey;

			if (settings.verticalSeparator)
			{
				Rect verticalSeparatorRect = new Rect(column_bodypartWidth - 1, curY, 2, Mathf.Max(bodyPartLabelHeight, hediffTextsHeight));
				GUI.DrawTexture(verticalSeparatorRect, TexUI.FastFillTex);
			}

			if (settings.horizontalSeparator)
			{
				/*if (drawingFirstRow)
				{
					drawingFirstRow = false;
				}
				else*/
				{
					Rect horizontalSeparatorRect = new Rect(0, curY, rowRect.width, 1);
					GUI.DrawTexture(horizontalSeparatorRect, TexUI.FastFillTex);
				}
			}

			if (part == null)
			{
				Widgets.Label(new Rect(0f, curY, bodypartLabelWidth, 100f), bodyPartText);
			}
			else
			{
				Widgets.Label(new Rect(0f, curY, bodypartLabelWidth, 100f), bodyPartText);
				if(replacingPart != null) 
				{
					GUI.color = Color.white;
					float iconOffset = Math.Max((bodyPartLabelHeight - IconSize) / 2f, 0);
					Rect iconRect = new Rect(bodypartLabelWidth, curY + iconOffset, IconSize / 2f, IconSize);
					CustomInfoCardButtonWidget.CustomInfoCardButton(iconRect, replacingPart.def);
				}
			}

			float innerY = 0;

			GUI.color = Color.white;

			foreach (IGrouping<int, Hediff> grouping in groupings)
			{
				var hediffsByPriority = grouping.OrderByDescending(i => i.TendableNow(true) ? i.TendPriority : -1);

				int postGroupingCount = 0;
				Hediff hediff = null;
				TextureAndColor stateIcon = null;
				float stateSeverity = 0f;
				float totalBleedRate = 0f;
				foreach (Hediff hediff2 in hediffsByPriority)
				{
					if (postGroupingCount == 0)
					{
						hediff = hediff2;
					}

					stateIcon = hediff2.StateIcon;
					if (hediff2.def.lethalSeverity > 0f)
						stateSeverity = hediff2.Severity / hediff2.def.lethalSeverity;
					else
						stateSeverity = -1f;

					totalBleedRate += hediff2.BleedRate;
					postGroupingCount++;
				}
				string heddifLabel = hediff.LabelCap;
				if(settings.italicizeMissing && hediff is Hediff_MissingPart) 
				{
					heddifLabel = heddifLabel.ApplyTag("i");
				}


				if (postGroupingCount != 1)
				{
					heddifLabel = heddifLabel + " x" + postGroupingCount.ToStringCached();
				}
				GUI.color = hediff.LabelColor;
				float hediffTextHeight = Text.CalcHeight(heddifLabel, column_hediffLabelWidth);

				Rect hediffLabelrect = new Rect(column_bodypartWidth + 2f, curY + innerY, column_hediffLabelWidth - 2f, hediffTextHeight);
				Rect fullHediffRect = new Rect(column_bodypartWidth, curY + innerY, rowRect.width - column_bodypartWidth, hediffTextHeight);

				//GUI.DrawTexture(fullHediffRect.ContractedBy(1f), Textures.mostlyTransparentWhite);


				Widgets.Label(hediffLabelrect, heddifLabel);
				GUI.color = Color.white;

				float widthAccumulator = 0;

				float iconOffset = Math.Max((fullHediffRect.height - IconSize) / 2f, 0);
				foreach (HediffDef localHediffDef in hediffsByPriority.Select((Hediff h) => h.def).Distinct<HediffDef>())
				{
					Rect iconRect = new Rect(rowRect.width - (IconSize / 2f), fullHediffRect.y + iconOffset, IconSize / 2f, IconSize);
					CustomInfoCardButtonWidget.CustomInfoCardButton(iconRect, localHediffDef);
					widthAccumulator += iconRect.width;
				}
				if (totalBleedRate > 0f)
				{
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + IconSize), fullHediffRect.y + iconOffset, IconSize, IconSize);

					if (settings.bleedingIcons)
					{
						float brightnessPulse = Pulser.PulseBrightness(totalBleedRate, 0.25f);
						GUI.color = new Color(1f, 1f, 1f, brightnessPulse);

						Texture bleedIcon;
						if (totalBleedRate <= 0.1)
							bleedIcon = Textures.BleedingIcon_0;
						else if (totalBleedRate < 0.25)
							bleedIcon = Textures.BleedingIcon_1;
						else if (totalBleedRate < 0.5)
							bleedIcon = Textures.BleedingIcon_2;
						else if (totalBleedRate < 1.5)
							bleedIcon = Textures.BleedingIcon_3;
						else if (totalBleedRate < 2.5)
							bleedIcon = Textures.BleedingIcon_4;
						else
							bleedIcon = Textures.BleedingIcon_5;

						GUI.DrawTexture(iconRect, bleedIcon);
					}
					else
					{
						GUI.DrawTexture(iconRect.ContractedBy(GenMath.LerpDouble(0f, 0.6f, 5f, 0f, Mathf.Min(totalBleedRate, 1f))), Textures.BleedingIcon_Vanilla);
					}

					widthAccumulator += iconRect.width;
				}
				if (stateIcon.HasValue)
				{
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + IconSize), fullHediffRect.y + iconOffset, IconSize, IconSize);
					GUI.color = stateIcon.Color;
					if(stateSeverity >= 0)
						GUI.DrawTexture(iconRect.ContractedBy(GenMath.LerpDouble(0f, 1f, IconSize / 6f, 0f, Mathf.Min(totalBleedRate, 1f))), stateIcon.Texture);
					else
						GUI.DrawTexture(iconRect.ContractedBy(IconSize / 6f), stateIcon.Texture);

					widthAccumulator += iconRect.width;
				}
				/*GenUI.DrawElementStack<GenUI.AnonymousStackElement>(iconsRect, hediffTextHeight, list, delegate (Rect r, GenUI.AnonymousStackElement obj)
				{
					obj.drawer(r);
				}, (GenUI.AnonymousStackElement obj) => obj.width, 4f, 5f, true);*/
				field_lastMaxIconsTotalWidth.Value = widthAccumulator; //Mathf.Max(field_lastMaxIconsTotalWidth.Value, list.Sum((GenUI.AnonymousStackElement x) => x.width + 5f) - 5f);
				innerY += hediffTextHeight;
			}
			GUI.color = Color.white;
			curY += Mathf.Max(bodyPartLabelHeight, hediffTextsHeight);


			if (pawn != null)
			{
				if (Widgets.ButtonInvisible(wholeEntryRect, method_CanEntryBeClicked.GetValue<bool>(diffs, pawn)))
				{
					method_EntryClicked.GetValue(diffs, pawn);
				}
				if (Mouse.IsOver(wholeEntryRect))
				{
					TooltipHandler.TipRegion(wholeEntryRect, new TipSignal(() => method_GetTooltip.GetValue<string>(diffs, pawn, part), (int)curY + 7857));
				}
			}
		}

		private static Color GetHealthColorForBodypart(Pawn pawn, BodyPartRecord part)
		{
			if (pawn != null)
				return HealthUtility.GetPartConditionLabel(pawn, part).Second;
			else
				return Color.white;
		}

		public static void DoWholeRowHighlight(Rect rowRect)
		{
			if (field_highlight.Value)
			{
				GUI.color = StaticHighlightColor;
				GUI.DrawTexture(rowRect, TexUI.HighlightTex);
			}
			field_highlight.Value = !field_highlight.Value;
			if (Mouse.IsOver(rowRect))
			{
				GUI.color = HighlightColor;
				GUI.DrawTexture(rowRect, TexUI.HighlightTex);
			}
		}

		public static IEnumerable<IGrouping<BodyPartRecord, Hediff>> ReorderHediffGroups(IEnumerable<IGrouping<BodyPartRecord, Hediff>> returned, Pawn pawn, bool showBloodLoss)
		{
			Func<BodyPartRecord, float> getListPriority = (BodyPartRecord rec) => method_getListPriority.GetValue<float>(rec);

			return returned.OrderByDescending(x => x.Max(i => i.TendableNow(true) ? i.TendPriority : -1)).ThenByDescending(i => getListPriority(i.First().Part));
		}

	}
}
