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
		public static readonly Color CyanWhite = new Color(0.8f, 1.0f, 1.0f, 1f);
		public static readonly Color BluishGreen = new Color(0.0f, 1.0f, 0.4f, 1f);
		public static readonly Color MissingBodyPart = new Color(0.15f, 0.0f, 0.0f, 1f);

		public static readonly int IconHeight = 20;
		public static readonly int BleedIconWidth = 15;
		public static readonly int TendIconWidth = 15;
		public static readonly int InfoIconWidth = 10;

		//private static Traverse<float> field_lastMaxIconsTotalWidth;
		//private static Traverse<float> field_scrollViewHeight;
		//private static Traverse<Vector2> field_scrollPosition;
		private static Traverse<bool> field_highlight;

		private static Traverse method_CanEntryBeClicked;
		private static Traverse method_EntryClicked;
		private static Traverse method_GetTooltip;
		private static Traverse method_GetListPriority;

		private static Traverse method_pawnmorpher_Tooltip;
		private static Traverse method_eliteBionics_GetMaxHealth;

		static CustomHealthCardUtility() 
		{
			//field_lastMaxIconsTotalWidth = Traverse.Create(typeof(HealthCardUtility)).Field<float>("lastMaxIconsTotalWidth");
			//field_scrollViewHeight = Traverse.Create(typeof(HealthCardUtility)).Field<float>("scrollViewHeight");
			//field_scrollPosition = Traverse.Create(typeof(HealthCardUtility)).Field<Vector2>("scrollPosition");
			field_highlight = Traverse.Create(typeof(HealthCardUtility)).Field<bool>("highlight");


			method_CanEntryBeClicked = Traverse.Create(typeof(HealthCardUtility)).Method("CanEntryBeClicked", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn) });
			method_EntryClicked = Traverse.Create(typeof(HealthCardUtility)).Method("EntryClicked", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn) });
			method_GetTooltip = Traverse.Create(typeof(HealthCardUtility)).Method("GetTooltip", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn), typeof(BodyPartRecord) });
			method_GetListPriority = Traverse.Create(typeof(HealthCardUtility)).Method("GetListPriority", new Type[] { typeof(BodyPartRecord) });

			if (CompactHediffsMod.pawnmorpherLoaded)
			{
				method_pawnmorpher_Tooltip = Traverse.CreateWithType("Pawnmorph.PatchHealthCardUtilityDrawHediffRow")?.Method("Tooltip", new Type[] { typeof(IEnumerable<Hediff>) });
				if (!method_pawnmorpher_Tooltip.MethodExists())
					method_pawnmorpher_Tooltip = null;
			}
			if (CompactHediffsMod.eliteBionicsLoaded)
			{
				//for the record, Vectorial1024, this is really rather rude.
				method_eliteBionics_GetMaxHealth = Traverse.CreateWithType("EBF.VanillaExtender")?.Method("GetMaxHealth", new Type[] { typeof(BodyPartDef), typeof(Pawn), typeof(BodyPartRecord) });
				if (!method_eliteBionics_GetMaxHealth.MethodExists())
					method_eliteBionics_GetMaxHealth = null;
			}
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

					//var traverse = Traverse.Create(diff);
					var property_IsCoreMutation = Traverse.Create(diff).Property("IsCoreMutation");
					if (property_IsCoreMutation.PropertyExists())
					{
						if (property_IsCoreMutation.GetValue<bool>())
							return diff;
					}
					else
					{
						List<BodyPartDef> parts = Traverse.Create(def)?.Field<List<BodyPartDef>>("parts")?.Value;
						//TODO: Remove this when Pawnmorpher updates
						if (parts != null && parts.Count == 1 && parts[0] == part.def) //making an assumption here...
							return diff;
					}
				}
			}

			return null;
		}

		public static void DrawHediffRow(Rect rowRect, Pawn pawn, IEnumerable<Hediff> diffs, ref float curY)
		{
			rowRect = rowRect.Rounded();
			int currentY = (int)curY;

			var settings = CompactHediffsMod.settings;


			int column_bodypartWidth = (int)(rowRect.width * 0.375f);
			//int column_hediffLabelWidth = (int)(rowRect.width - column_bodypartWidth - field_lastMaxIconsTotalWidth.Value);

			BodyPartRecord part = diffs.First<Hediff>().Part;
			Hediff replacingPart = null;
			if (part != null && settings.replacingPartInBodypart)
				replacingPart = GetReplacingPart(diffs, part);

			string bodyPartText = MakeBodyPartText(pawn, part, replacingPart);

			int bodypartLabelWidth = (int)(column_bodypartWidth - IconHeight / 2f);
			int bodyPartLabelHeight = (int)(Text.CalcHeight(bodyPartText, bodypartLabelWidth));
			int hediffTotalHeight = 0;

			List<IGrouping<HediffDef, Hediff>> groupings;
			if (replacingPart == null)
			{
				if (settings.tendPrioritySort)
					groupings = diffs.OrderByDescending(i => i.TendableNow(true) ? i.TendPriority : -1).GroupBy(x => x.def).ToList();
				else
					groupings = diffs.GroupBy(x => x.def).ToList();
			}
			else
			{
				if (settings.tendPrioritySort)
					groupings = diffs.Where(x => x != replacingPart).OrderByDescending(i => i.TendableNow(true) ? i.TendPriority : -1).GroupBy(x => x.def).ToList();
				else
					groupings = diffs.Where(x => x != replacingPart).GroupBy(x => x.def).ToList();
			}

			for (int i = 0; i < groupings.Count; i++)
			{
				IGrouping<HediffDef, Hediff> grouping = groupings[i];
				string hediffLabel = GenLabelForHediffGroup(grouping);
				Hediff representativeHediff = grouping.First();
				//string hediffLabel = representativeHediff.LabelCap;
				if (grouping.Count() > 1)
				{
					hediffLabel = hediffLabel + " x" + grouping.Count().ToString();
				}
				if (settings.italicizeMissing && representativeHediff.def == HediffDefOf.MissingBodyPart)
				{
					hediffLabel = hediffLabel.ApplyTag("i");
				}

				int iconsWidth = CalcIconsWidthForGrouping(grouping);
				int hediffLabelWidth = (int)(rowRect.width - (column_bodypartWidth + iconsWidth));

				hediffTotalHeight += (int)(Text.CalcHeight(hediffLabel, hediffLabelWidth));

				if (settings.internalSeparator && i < groupings.Count - 1)
					hediffTotalHeight += settings.internalSeparatorHeight;

				if (settings.severityBarMode != CompactHediffs_Settings.SeverityBarMode.Off)
				{
					var hediff = grouping.First();

					float maxSeverity = -1;
					if (hediff.def.lethalSeverity > 0f)
						maxSeverity = hediff.def.lethalSeverity;
					else if (hediff.def.maxSeverity > 0f)
						maxSeverity = hediff.def.maxSeverity;
					float severityFraction = (hediff.Severity / maxSeverity);
					if (severityFraction < 0)
						severityFraction = 0;

					var tendDurationComp = hediff.TryGetComp<HediffComp_TendDuration>();
					var immunizableComp = hediff.TryGetComp<HediffComp_Immunizable>();
					bool hasImmunity = immunizableComp != null;
					bool hasSeverity = severityFraction > 0.001f;

					bool showsSeverity = hediff.SeverityLabel != null;
					if (settings.showHiddenProgressConditions || showsSeverity)
					{
						if (hasImmunity || hasSeverity)
							hediffTotalHeight += settings.internalBarHeight;
					}
				}
			}

			int totalHeight = Math.Max(bodyPartLabelHeight, hediffTotalHeight);
			if (settings.horizontalSeparator)
				totalHeight += settings.horizontalSeparatorHeight;
			Rect wholeEntryRect = new Rect(0f, currentY, rowRect.width, totalHeight).Rounded();

			if (settings.evenOddHighlights)
			{
				DoWholeRowHighlight(wholeEntryRect);
			}

			var separatorColor = settings.separatorNightMode ? Color.black : Color.grey;

			if (settings.verticalSeparator)
			{
				GUI.color = separatorColor;
				Rect verticalSeparatorRect = new Rect(column_bodypartWidth, currentY, settings.verticalSeparatorWidth, totalHeight).Rounded();
				GUI.DrawTexture(verticalSeparatorRect, TexUI.FastFillTex);
			}

			if (settings.horizontalSeparator)
			{
				GUI.color = separatorColor;
				Rect horizontalSeparatorRect = new Rect(0, currentY, rowRect.width, settings.horizontalSeparatorHeight).Rounded();
				GUI.DrawTexture(horizontalSeparatorRect, TexUI.FastFillTex);
				currentY += settings.horizontalSeparatorHeight;
			}


			if (settings.bodypartHealthbars)
			{
				Rect fullHealthPercentageRect = new Rect(0, currentY, column_bodypartWidth, settings.healthBarHeight).Rounded();

				if (part != null)
				{
					Color healthColor = GetHealthColorForBodypart(pawn, part);
					float partMaxHealth = getPartMaxHealth(pawn, part);
					float partHealthFraction = pawn.health.hediffSet.GetPartHealth(part) / partMaxHealth;

					if (partHealthFraction < 1f)
					{
						GUI.color = Color.gray;
						GUI.DrawTexture(fullHealthPercentageRect, Textures.translucentWhite);
						if (partHealthFraction == 0)
						{
							GUI.color = MissingBodyPart;
							//GUI.color = Color.black;
							GUI.DrawTexture(fullHealthPercentageRect, TexUI.FastFillTex);
						}
						else
						{
							GUI.color = healthColor;
							Rect healthPercentageRect = new Rect(0, currentY, column_bodypartWidth * partHealthFraction, settings.healthBarHeight);
							GUI.DrawTexture(healthPercentageRect, Textures.translucentWhite);
						}

					}
				}
			}

			if (settings.verticalSeparator)
			{
				column_bodypartWidth += settings.verticalSeparatorWidth;
			}

			if (part == null)
			{
				Widgets.Label(new Rect(0f, currentY, bodypartLabelWidth, 100f), bodyPartText);
			}
			else
			{
				Widgets.Label(new Rect(0f, currentY, bodypartLabelWidth, 100f), bodyPartText);
				if (replacingPart != null)
				{
					GUI.color = Color.white;
					int iconOffset = (int)Math.Max((bodyPartLabelHeight - IconHeight) / 2f, 0);
					Rect iconRect = new Rect(bodypartLabelWidth, currentY + iconOffset, IconHeight / 2f, IconHeight).Rounded();
					CustomInfoCardButtonWidget.CustomInfoCardButton(iconRect, replacingPart);
				}
			}

			int innerY = 0;

			GUI.color = Color.white;

			for (int i = 0; i < groupings.Count; i++)
			{
				IGrouping<HediffDef, Hediff> grouping = groupings[i];
				string hediffLabel = GenLabelForHediffGroup(grouping);
				var hediffsByPriority = grouping.OrderByDescending(x => x.TendableNow(true) ? x.TendPriority : -1);

				//TextureAndColor stateIcon = null;
				//float stateSeverity = 0f;
				//float totalBleedRate = 0f;
				/*foreach (Hediff heddif in hediffsByPriority)
				{
					stateIcon = heddif.StateIcon;
					if (heddif.def.lethalSeverity > 0f)
						stateSeverity = heddif.Severity / heddif.def.lethalSeverity;
					else
						stateSeverity = -1f;

					totalBleedRate += heddif.BleedRate;
				}*/

				Hediff representativeHediff = grouping.First();
				//string hediffLabel = representativeHediff.LabelCap;
				if (grouping.Count() > 1)
				{
					hediffLabel = hediffLabel + " x" + grouping.Count().ToString();
				}
				if (settings.italicizeMissing && representativeHediff.def == HediffDefOf.MissingBodyPart)
				{
					hediffLabel = hediffLabel.ApplyTag("i");
				}

				int iconsWidth = CalcIconsWidthForGrouping(grouping);
				int hediffLabelWidth = (int)(rowRect.width - (column_bodypartWidth + iconsWidth));

				int hediffTextHeight = (int)Text.CalcHeight(hediffLabel, hediffLabelWidth);


				int hediffColumnWidth = (int)(rowRect.width - column_bodypartWidth);

				if (settings.severityBarMode != CompactHediffs_Settings.SeverityBarMode.Off && settings.severityBarsPosition == CompactHediffs_Settings.BarPosition.Above)
				{
					foreach (Hediff hediff in grouping)
					{
						Rect barRect = new Rect(column_bodypartWidth, currentY + innerY, hediffColumnWidth, settings.internalBarHeight).Rounded();
						innerY += DrawSeverityBar(settings, barRect, hediff);
					}
				}

				Rect fullHediffRect = new Rect(column_bodypartWidth, currentY + innerY, rowRect.width - column_bodypartWidth, hediffTextHeight).Rounded();
				Rect hediffLabelrect = new Rect(column_bodypartWidth, currentY + innerY, hediffLabelWidth, hediffTextHeight).Rounded();

				/*if (tendDurationComp != null)
				{
					var props = (tendDurationComp.TProps as HediffCompProperties_TendDuration);
					if (!props.TendIsPermanent)
					{
						Log.Message(hediff.Label + " tend " + tendDurationComp.tendTicksLeft + " of " + props.TendTicksFull);
						int duration = tendDurationComp.tendTicksLeft;
						float durationFraction = (float)tendDurationComp.tendTicksLeft / (float)props.TendTicksFull;
						float tendableFraction = (float)props.TendTicksOverlap / (float)props.TendTicksFull;


						Rect tendableRectFull = new Rect(column_bodypartWidth, curY + innerY + fullHediffRect.height - settings.internalBarHeight, hediffColumnWidth, settings.internalBarHeight).Rounded();
						Rect tendableNowRect = new Rect(tendableRectFull.x, tendableRectFull.y + 1f, tendableRectFull.width * tendableFraction, settings.internalBarHeight - 2f).Rounded();
						Rect tendedRect = new Rect(tendableRectFull.x + tendableNowRect.width, tendableRectFull.y + 1f, tendableRectFull.width * (1f - tendableFraction), settings.internalBarHeight - 2f).Rounded();
						Rect tendDurationRect = new Rect(tendableRectFull.x, tendableRectFull.y, tendableRectFull.width * durationFraction, settings.internalBarHeight).Rounded();

						float brightnessPulse = Pulser.PulseBrightness(1f, 0.5f);

						GUI.color = duration > 0f ? new Color(0.6f, 0.5f, 0.5f, 1f) : new Color(0.6f, 0.5f, 0.5f, brightnessPulse);
						GUI.DrawTexture(tendableNowRect, Textures.translucentWhite);
						GUI.color = duration > 0f ? new Color(0.65f, 0.7f, 0.65f, 1f) : new Color(0.65f, 0.7f, 0.65f, brightnessPulse);
						GUI.DrawTexture(tendedRect, Textures.translucentWhite);

						GUI.color = stateIcon.Color;
						GUI.DrawTexture(tendDurationRect, Textures.translucentWhite);
					}
				}*/

				GUI.color = representativeHediff.LabelColor;
				Widgets.Label(hediffLabelrect, hediffLabel);
				GUI.color = Color.white;

				int widthAccumulator = 0;

				int iconOffset = (int)Math.Max((fullHediffRect.height - IconHeight) / 2f, 0);

				//foreach (Hediff localHediff in hediffsByPriority)
				{
					Rect iconRect = new Rect(rowRect.width - (IconHeight / 2f), fullHediffRect.y + iconOffset, IconHeight / 2f, IconHeight).Rounded();
					CustomInfoCardButtonWidget.CustomInfoCardButton(iconRect, representativeHediff);
					widthAccumulator += (int)iconRect.width;
				}

				/*if (stateIcon.HasValue)
				{
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + IconSize), fullHediffRect.y + iconOffset, IconSize, IconSize);
					GUI.color = stateIcon.Color;
					if (stateSeverity >= 0)
						GUI.DrawTexture(iconRect.ContractedBy(GenMath.LerpDouble(0f, 1f, IconSize / 6f, 0f, Mathf.Min(stateSeverity, 1f))), stateIcon.Texture);
					else
						GUI.DrawTexture(iconRect.ContractedBy(IconSize / 6f), stateIcon.Texture);

					widthAccumulator += iconRect.width;
				}*/

				var hediffsWithStateIcon = hediffsByPriority.Where(x => x.StateIcon.HasValue);
				//draw non-injury icons first
				foreach (Hediff localHediff in hediffsWithStateIcon.Where(x => x.StateIcon.Texture != Textures.Vanilla_TendedIcon_Well_Injury))
				{
					var hediffStateIcon = localHediff.StateIcon;
					GUI.color = hediffStateIcon.Color;
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + IconHeight), fullHediffRect.y + iconOffset, IconHeight, IconHeight).Rounded();
					GUI.DrawTexture(iconRect, hediffStateIcon.Texture);
					widthAccumulator += (int)iconRect.width;
				}
				//draw tended injuries
				foreach (Hediff localHediff in hediffsWithStateIcon.Where(x => x.StateIcon.Texture == Textures.Vanilla_TendedIcon_Well_Injury).OrderByDescending(x => x.TryGetComp<HediffComp_TendDuration>().tendQuality))
				{
					var hediffStateIcon = localHediff.StateIcon;
					GUI.color = hediffStateIcon.Color;
					Rect iconRect;
					if (settings.tendingIcons)
					{
						iconRect = new Rect(rowRect.width - (widthAccumulator + TendIconWidth), fullHediffRect.y + iconOffset, TendIconWidth, IconHeight).Rounded();
						DrawCustomTendingIcon(iconRect, localHediff);
					}
					else
					{
						iconRect = new Rect(rowRect.width - (widthAccumulator + IconHeight), fullHediffRect.y + iconOffset, IconHeight, IconHeight).Rounded();
						GUI.DrawTexture(iconRect, hediffStateIcon.Texture);
					}
					widthAccumulator += (int)iconRect.width;
				}
				//draw bleeding injuries
				GUI.color = Color.white;
				foreach (Hediff localHediff in hediffsByPriority.Where(x => x.Bleeding).OrderByDescending(x => x.BleedRate))
				{
					if (localHediff.Bleeding)
					{
						Rect iconRect;
						if (settings.bleedingIcons)
						{
							iconRect = new Rect(rowRect.width - (widthAccumulator + BleedIconWidth), fullHediffRect.y + iconOffset, BleedIconWidth, IconHeight).Rounded();
							DrawCustomBleedIcon(iconRect, localHediff);
						}
						else
						{
							iconRect = new Rect(rowRect.width - (widthAccumulator + IconHeight), fullHediffRect.y + iconOffset, IconHeight, IconHeight).Rounded();
							GUI.DrawTexture(iconRect.ContractedBy(GenMath.LerpDouble(0f, 0.6f, 5f, 0f, Mathf.Min(localHediff.BleedRate, 1f))), Textures.Vanilla_BleedingIcon);
						}
						widthAccumulator += (int)iconRect.width;
					}
					else
					{
						break;
					}
				}

				/*if (totalBleedRate > 0f)
				{
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + IconSize), fullHediffRect.y + iconOffset, IconSize, IconSize);

					DrawBleedIcon(iconRect, );

					widthAccumulator += iconRect.width;
				}*/
				//field_lastMaxIconsTotalWidth.Value = Math.Max(widthAccumulator, field_lastMaxIconsTotalWidth.Value); 
				innerY += hediffTextHeight;

				if (settings.severityBarMode != CompactHediffs_Settings.SeverityBarMode.Off && settings.severityBarsPosition == CompactHediffs_Settings.BarPosition.Below)
				{
					foreach (Hediff hediff in grouping)
					{
						Rect barRect = new Rect(column_bodypartWidth, currentY + innerY, hediffColumnWidth, settings.internalBarHeight).Rounded();
						int extraHeight = DrawSeverityBar(settings, barRect, hediff);
						innerY += extraHeight;
						fullHediffRect.height += extraHeight;
					}
				}

				if (settings.internalSeparator && i < groupings.Count - 1)
				{
					Rect internalSeparatorRect = new Rect(column_bodypartWidth, currentY + innerY, (rowRect.width - column_bodypartWidth), settings.internalSeparatorHeight).Rounded();
					GUI.color = separatorColor;
					GUI.DrawTexture(internalSeparatorRect, TexUI.FastFillTex);
					innerY += settings.internalSeparatorHeight;
					fullHediffRect.height += settings.internalSeparatorHeight;
				}

			}
			GUI.color = Color.white;
			currentY += Math.Max(bodyPartLabelHeight, hediffTotalHeight);


			if (pawn != null)
			{
				if (Widgets.ButtonInvisible(wholeEntryRect, method_CanEntryBeClicked.GetValue<bool>(diffs, pawn)))
				{
					method_EntryClicked.GetValue(diffs, pawn);
				}
				if (Mouse.IsOver(wholeEntryRect))
				{
					TooltipHandler.TipRegion(wholeEntryRect, new TipSignal(() => method_GetTooltip.GetValue<string>(diffs, pawn, part), (int)currentY + 7857));
					if (CompactHediffsMod.pawnmorpherLoaded)
					{
						//copied from Pawnmorph.PatchHealthCardUtilityDrawHediffRow
						string tooltip = method_pawnmorpher_Tooltip.GetValue<string>(diffs);
						if (tooltip != "")
							TooltipHandler.TipRegion(wholeEntryRect, new TipSignal(() => tooltip, (int)currentY + 117857));
					}
				}
			}

			curY = (float)currentY;
		}

		private static string MakeBodyPartText(Pawn pawn, BodyPartRecord part, Hediff replacingPart)
		{
			string bodyPartText;
			if (part == null)
			{
				bodyPartText = ColoredText.Colorize("WholeBody".Translate(), Color.grey);
			}
			else
			{
				Color healthColor = GetHealthColorForBodypart(pawn, part);
				if (replacingPart != null)
				{
					//diffs = diffs.Where(x => x != replacingPart); //do not list this hediff
					var replacingPartColorLabel = ColoredText.Colorize(replacingPart.Label, replacingPart.def.defaultLabelColor);
					var regex = new Regex(@"\b" + part.def.label + @"\b");
					if (regex.IsMatch(replacingPart.Label) && regex.IsMatch(part.Label))
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
			return bodyPartText;
		}

		private static float getPartMaxHealth(Pawn pawn, BodyPartRecord part)
		{
			if (!CompactHediffsMod.eliteBionicsLoaded)
				return part.def.GetMaxHealth(pawn);
			else
				return method_eliteBionics_GetMaxHealth.GetValue<float>(part.def, pawn, part);
		}

		private static int CalcIconsWidthForGrouping(IGrouping<HediffDef, Hediff> grouping)
		{
			int iconsWidth = InfoIconWidth;
			foreach (Hediff diff in grouping)
			{
				if (diff.StateIcon.HasValue)
				{
					if (CompactHediffsMod.settings.tendingIcons && diff.StateIcon.Texture == Textures.Vanilla_TendedIcon_Well_General)
						iconsWidth += TendIconWidth;
					else
						iconsWidth += IconHeight;
				}
				if (diff.Bleeding)
				{
					if (CompactHediffsMod.settings.bleedingIcons)
						iconsWidth += BleedIconWidth;
					else
						iconsWidth += IconHeight;
				}
			}
			return iconsWidth;
		}

		private static string GenLabelForHediffGroup(IGrouping<HediffDef, Hediff> grouping)
		{
			string fullLabel = grouping.First().LabelCap;
			bool mismatch = false;
			foreach(Hediff hediff in grouping) 
			{
				if(hediff.LabelCap != fullLabel)
				{
					mismatch = true;
					break;
				}
			}
			if (!mismatch)
				return fullLabel;
			else
				return grouping.First().LabelBaseCap;
		}

		private static void DrawCustomTendingIcon(Rect iconRect, Hediff hediff)
		{
			float quality = hediff.TryGetComp<HediffComp_TendDuration>().tendQuality;
			Texture tendIcon;
			if (quality <= 0.01)
			{
				tendIcon = Textures.TendingIcon_0;
				//GUI.color = Color.white;
			}
			else if (quality <= 0.25)
				tendIcon = Textures.TendingIcon_1;
			else if (quality < 0.5)
				tendIcon = Textures.TendingIcon_2;
			else if (quality < 0.75)
				tendIcon = Textures.TendingIcon_3;
			else if(quality < 1.0)
				tendIcon = Textures.TendingIcon_4;
			else
				tendIcon = Textures.TendingIcon_5;

			GUI.DrawTexture(iconRect, tendIcon);
		}



		private static void DrawCustomBleedIcon(Rect iconRect, Hediff hediff)
		{
			float bleedRate = hediff.BleedRate;
			float brightnessPulse = Pulser.PulseBrightness(bleedRate, 0.25f);
			GUI.color = new Color(1f, 1f, 1f, brightnessPulse);

			Texture bleedIcon;
			if (bleedRate <= 0.1)
				bleedIcon = Textures.BleedingIcon_0;
			else if (bleedRate < 0.25)
				bleedIcon = Textures.BleedingIcon_1;
			else if (bleedRate < 0.5)
				bleedIcon = Textures.BleedingIcon_2;
			else if (bleedRate < 1.25)
				bleedIcon = Textures.BleedingIcon_3;
			else if (bleedRate < 2.25)
				bleedIcon = Textures.BleedingIcon_4;
			else
				bleedIcon = Textures.BleedingIcon_5;

			GUI.DrawTexture(iconRect, bleedIcon);
		}

		private static int DrawSeverityBar(CompactHediffs_Settings settings, Rect barRect, Hediff hediff)
		{
			if (settings.severityBarMode == CompactHediffs_Settings.SeverityBarMode.Off)
				return 0;

			bool showsSeverity = hediff.SeverityLabel != null;
			if (!settings.showHiddenProgressConditions && !showsSeverity)
 				return 0;

			float maxSeverity = -1;
			if (hediff.def.lethalSeverity > 0f)
				maxSeverity = hediff.def.lethalSeverity;
			else if (hediff.def.maxSeverity > 0f)
				maxSeverity = hediff.def.maxSeverity;
			float severityFraction = (hediff.Severity / maxSeverity);
			if (severityFraction < 0)
				severityFraction = 0;

			var tendDurationComp = hediff.TryGetComp<HediffComp_TendDuration>();
			var immunizableComp = hediff.TryGetComp<HediffComp_Immunizable>();
			bool hasImmunity = immunizableComp != null && immunizableComp.Immunity > 0.001f;
			bool hasSeverity = severityFraction > 0.001f;
			float immunityFraction = hasImmunity ? immunizableComp.Immunity : 0;


			if (hasImmunity || hasSeverity)
			{
				GUI.color = Color.black;
				GUI.DrawTexture(barRect, Textures.translucentWhite);

				Rect? commonRect = null;
				Rect? commonRectMirror = null;
				Color commonColor = settings.severityBarHighContrast ? Color.white : hediff.LabelColor;
				Rect? severityRect = null;
				Rect? severityRectMirror = null;
				Color severityColor = settings.severityBarHighContrast ? Color.red : hediff.LabelColor;
				Rect? immunityRect = null;
				Rect? immunityRectMirror = null;
				Color immunityColor = settings.severityBarHighContrast ? BluishGreen : CyanWhite;

				switch (settings.severityBarMode) 
				{
					case CompactHediffs_Settings.SeverityBarMode.LeftToRight:
						commonColor.a = 0.5f;
						if (immunityFraction > severityFraction)
						{
							commonRect = new Rect(barRect.x, barRect.y, barRect.width * severityFraction, settings.internalBarHeight).Rounded();
							immunityRect = new Rect(barRect.x + commonRect.Value.width, barRect.y, barRect.width * (immunityFraction - severityFraction), settings.internalBarHeight).Rounded();
						}
						else
						{
							if (hasImmunity)
							{
								commonRect = new Rect(barRect.x, barRect.y, barRect.width * (severityFraction - (severityFraction - immunityFraction)), settings.internalBarHeight).Rounded();
								severityRect = new Rect(barRect.x + commonRect.Value.width, barRect.y, barRect.width * (severityFraction - immunityFraction), settings.internalBarHeight).Rounded();
							}
							else
							{
								commonRect = new Rect(barRect.x, barRect.y, barRect.width * severityFraction, settings.internalBarHeight).Rounded();
							}
						}
						break;
					case CompactHediffs_Settings.SeverityBarMode.EdgeToMiddle:
						{
							commonRect = new Rect(barRect.x + ((barRect.width / 2f) - 1f), barRect.y, 2f, settings.internalBarHeight).Rounded();
							if (!settings.severityBarHighContrast)
							{
								severityColor.a = 0.75f;
								immunityColor.a = 0.75f;
							}
							if (hasImmunity)
							{
								int severityWidth = (int) ((barRect.width / 2f) * severityFraction);
								severityRect = new Rect(barRect.x, barRect.y, severityWidth, settings.internalBarHeight).Rounded();
								int immunityWidth = (int) ((barRect.width / 2f) * immunityFraction);
								immunityRect = new Rect(barRect.x + (barRect.width - immunityWidth), barRect.y, immunityWidth, settings.internalBarHeight).Rounded();
							}
							else
							{
								int severityWidth = (int) ((barRect.width / 2f) * severityFraction);
								severityRect = new Rect(barRect.x, barRect.y, severityWidth, settings.internalBarHeight).Rounded();
								severityRectMirror = new Rect(barRect.x + (barRect.width - severityWidth), barRect.y, severityWidth, settings.internalBarHeight).Rounded();
							}
						}
						break;
					case CompactHediffs_Settings.SeverityBarMode.MiddleToEdge:
						{
							if (!settings.severityBarHighContrast)
							{
								severityColor.a = 0.75f;
								immunityColor.a = 0.75f;
							}
							if (hasImmunity)
							{
								int severityWidth = (int) ((barRect.width / 2f) * severityFraction);
								severityRect = new Rect(barRect.x + ((barRect.width / 2f) - severityWidth), barRect.y, severityWidth, settings.internalBarHeight).Rounded();
								int immunityWidth = (int) ((barRect.width / 2f) * immunityFraction);
								immunityRect = new Rect(barRect.x + (barRect.width / 2f), barRect.y, immunityWidth, settings.internalBarHeight).Rounded();
							}
							else
							{
								int severityWidth = (int) ((barRect.width / 2f) * severityFraction);
								severityRect = new Rect(barRect.x + ((barRect.width / 2f) - severityWidth), barRect.y, severityWidth, settings.internalBarHeight).Rounded();
								severityRectMirror = new Rect(barRect.x + (barRect.width / 2f), barRect.y, severityWidth, settings.internalBarHeight).Rounded();
							}
						}
						break;
				}

				bool canBeTendedNow = !hediff.IsPermanent() && !hediff.pawn.Dead && hediff.TendableNow(false);
				bool needsTendingNow = canBeTendedNow && tendDurationComp != null && tendDurationComp.tendTicksLeft <= 0;
				if (canBeTendedNow)
				{
					var alphaMult = needsTendingNow ? Pulser.PulseBrightness(2f, 0.5f) : Pulser.PulseBrightness(1f, 0.5f);
					commonColor.a *= alphaMult;
					severityColor.a *= alphaMult;
					immunityColor.a *= alphaMult;
				}
				if (!showsSeverity) 
				{
					commonColor.a *= 0.65f;
					severityColor.a *= 0.65f;
					immunityColor.a *= 0.65f;
				}

				if(commonRect.HasValue)
				{
					GUI.color = commonColor;
					GUI.DrawTexture(commonRect.Value, TexUI.FastFillTex);
				}
				if (commonRectMirror.HasValue)
				{
					GUI.color = commonColor;
					GUI.DrawTexture(commonRectMirror.Value, TexUI.FastFillTex);
				}
				if (severityRect.HasValue)
				{
					GUI.color = severityColor;
					GUI.DrawTexture(severityRect.Value, TexUI.FastFillTex);
				}
				if (severityRectMirror.HasValue)
				{
					GUI.color = severityColor;
					GUI.DrawTexture(severityRectMirror.Value, TexUI.FastFillTex);
				}
				if (immunityRect.HasValue)
				{
					GUI.color = immunityColor;
					GUI.DrawTexture(immunityRect.Value, TexUI.FastFillTex);
				}
				if (immunityRectMirror.HasValue)
				{
					GUI.color = immunityColor;
					GUI.DrawTexture(immunityRectMirror.Value, TexUI.FastFillTex);
				}

				GUI.color = Color.white;
				return settings.internalBarHeight;
			}

			return 0;
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
			Func<BodyPartRecord, float> getListPriority = (BodyPartRecord rec) => method_GetListPriority.GetValue<float>(rec);

			return returned.OrderByDescending(x => x.Max(i => i.TendableNow(true) ? i.TendPriority : -1)).ThenByDescending(i => getListPriority(i.First().Part));
		}

	}
}
