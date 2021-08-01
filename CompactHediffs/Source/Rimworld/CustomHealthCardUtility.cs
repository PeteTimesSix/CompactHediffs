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
using PeteTimesSix.CompactHediffs.Rimworld.UI_compat;

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
		public static readonly int DefaultIconHeight = 18;
		public static readonly int BleedIconWidth = 15;
		public static readonly int TendIconWidth = 15;
		public static readonly int InfoIconWidth = 10;

		public static readonly int SmartMedicineIconWidth = 20;
		public static readonly int SmartMedicineIconHeight = 20;

		//public static Traverse<float> field_lastMaxIconsTotalWidth;
		//public static Traverse<float> field_scrollViewHeight;
		//public static Traverse<Vector2> field_scrollPosition;
		public static Traverse<bool> field_highlight;
		public static Traverse<bool> field_showHediffsDebugInfo;

		public static Traverse method_CanEntryBeClicked;
		public static Traverse method_EntryClicked;
		public static Traverse method_GetTooltip;
		public static Traverse method_GetListPriority;

		public static Traverse method_pawnmorpher_Tooltip;
		public static Traverse method_eliteBionics_GetMaxHealth;

		public static Texture2D[] value_smartMedicine_careTextures;
		public static Traverse method_smartMedicine_LabelButton;
		public static Traverse method_smartMedicine_PriorityCareComp_Get;
		public static Traverse method_smartMedicine_GetPawnMedicalCareCategory_GetCare;

		static CustomHealthCardUtility() 
		{
			//field_lastMaxIconsTotalWidth = Traverse.Create(typeof(HealthCardUtility)).Field<float>("lastMaxIconsTotalWidth");
			//field_scrollViewHeight = Traverse.Create(typeof(HealthCardUtility)).Field<float>("scrollViewHeight");
			//field_scrollPosition = Traverse.Create(typeof(HealthCardUtility)).Field<Vector2>("scrollPosition");
			field_highlight = Traverse.Create(typeof(HealthCardUtility)).Field<bool>("highlight");
			field_showHediffsDebugInfo = Traverse.Create(typeof(HealthCardUtility)).Field<bool>("showHediffsDebugInfo");

			method_CanEntryBeClicked = Traverse.Create(typeof(HealthCardUtility)).Method("CanEntryBeClicked", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn) });
			method_EntryClicked = Traverse.Create(typeof(HealthCardUtility)).Method("EntryClicked", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn) });
			method_GetTooltip = Traverse.Create(typeof(HealthCardUtility)).Method("GetTooltip", new Type[] { typeof(Pawn), typeof(BodyPartRecord) });
			//method_GetTooltip = Traverse.Create(typeof(HealthCardUtility)).Method("GetTooltip", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn), typeof(BodyPartRecord) });
			method_GetListPriority = Traverse.Create(typeof(HealthCardUtility)).Method("GetListPriority", new Type[] { typeof(BodyPartRecord) });

			if(!method_CanEntryBeClicked.MethodExists())
				Log.Warning("could not access HealthCardUtility.CanEntryBeClicked");
			if (!method_EntryClicked.MethodExists())
				Log.Warning("could not access HealthCardUtility.EntryClicked");
			if (!method_GetTooltip.MethodExists())
				Log.Warning("could not access HealthCardUtility.GetTooltip");
			if (!method_GetListPriority.MethodExists())
				Log.Warning("could not access HealthCardUtility.GetListPriority");

			if (CompactHediffsMod.pawnmorpherLoaded)
			{
				method_pawnmorpher_Tooltip = Traverse.CreateWithType("Pawnmorph.PatchHealthCardUtilityDrawHediffRow")?.Method("Tooltip", new Type[] { typeof(IEnumerable<Hediff>) });
				if (!method_pawnmorpher_Tooltip.MethodExists())
				{
					Log.Warning("could not access Pawnmorph.PatchHealthCardUtilityDrawHediffRow.Tooltip");
					method_pawnmorpher_Tooltip = null;
				}
			}
			if (CompactHediffsMod.eliteBionicsLoaded)
			{
				//for the record, Vectorial1024, this is really rather rude.
				method_eliteBionics_GetMaxHealth = Traverse.CreateWithType("EBF.VanillaExtender")?.Method("GetMaxHealth", new Type[] { typeof(BodyPartDef), typeof(Pawn), typeof(BodyPartRecord) });
				if (!method_eliteBionics_GetMaxHealth.MethodExists())
				{
					Log.Warning("could not access EBF.VanillaExtender.GetMaxHealth");
					method_eliteBionics_GetMaxHealth = null;
				}
			}
			if (CompactHediffsMod.smartMedicineLoaded) 
			{
				value_smartMedicine_careTextures = Traverse.Create(typeof(MedicalCareUtility)).Field<Texture2D[]>("careTextures").Value;

				method_smartMedicine_LabelButton = Traverse.CreateWithType("SmartMedicine.HediffRowPriorityCare")?.Method("LabelButton", new Type[] { typeof(Rect), typeof(string), typeof(Hediff) });
				if (!method_smartMedicine_LabelButton.MethodExists())
				{
					Log.Warning("could not access SmartMedicine.HediffRowPriorityCare.LabelButton");
					method_smartMedicine_LabelButton = null;
				}

				method_smartMedicine_PriorityCareComp_Get = Traverse.CreateWithType("SmartMedicine.PriorityCareComp")?.Method("Get", new Type[] { });
				if (!method_smartMedicine_PriorityCareComp_Get.MethodExists())
				{
					Log.Warning("could not access SmartMedicine.PriorityCareComp.Get");
					method_smartMedicine_PriorityCareComp_Get = null;
				}

				method_smartMedicine_GetPawnMedicalCareCategory_GetCare = Traverse.CreateWithType("SmartMedicine.GetPawnMedicalCareCategory")?.Method("GetCare", new Type[] { typeof(Pawn) });
				if (!method_smartMedicine_GetPawnMedicalCareCategory_GetCare.MethodExists())
				{
					Log.Warning("could not access SmartMedicine.GetPawnMedicalCareCategory.GetCare");
					method_smartMedicine_GetPawnMedicalCareCategory_GetCare = null;
				}
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

				int iconsWidth = CalcIconsWidthForGrouping(grouping, out int iconCount);
				float iconOverlap = 0;
				if (iconsWidth > (rowRect.width - column_bodypartWidth) / 2f)
				{
					iconOverlap = (((iconsWidth - (rowRect.width - column_bodypartWidth) / 2f)) / (float)iconCount);
					iconsWidth = (int)((rowRect.width - column_bodypartWidth) / 2f);
				}
				int hediffLabelWidth = (int)(rowRect.width - (column_bodypartWidth + iconsWidth));

				hediffTotalHeight += (int)(Text.CalcHeight(hediffLabel, hediffLabelWidth));

				if (settings.internalSeparator && i < groupings.Count - 1)
					hediffTotalHeight += settings.internalSeparatorHeight;

				if (settings.severityBarMode != CompactHediffs_Settings.SeverityBarMode.Off)
				{
					var hediff = grouping.First();

					float maxSeverity = GetMaxSeverityForHediff(hediff);
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
					if (settings.showCumulativeThreatment)
					{
						var tendQualityRequired = (tendDurationComp?.props as HediffCompProperties_TendDuration)?.disappearsAtTotalTendQuality;
						if (!(tendDurationComp == null || !tendQualityRequired.HasValue || tendQualityRequired.Value <= 0))
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

			//moved up here so the tooltip is first... although thats probably what the TooltipPriority is for
			int tooltipIDOffset = 0;
			if (pawn != null)
			{
				if (Mouse.IsOver(wholeEntryRect))
				{
					if (part != null)
					{
						TooltipHandler.TipRegion(wholeEntryRect, new TipSignal(() => method_GetTooltip.GetValue<string>(pawn, part), (int)currentY + 7857 + tooltipIDOffset, TooltipPriority.Pawn));
						tooltipIDOffset++;
					}
					if (CompactHediffsMod.pawnmorpherLoaded)
					{
						//copied from Pawnmorph.PatchHealthCardUtilityDrawHediffRow
						string tooltip = method_pawnmorpher_Tooltip.GetValue<string>(diffs);
						if (tooltip != "")
						{
							TooltipHandler.TipRegion(wholeEntryRect, new TipSignal(() => tooltip, (int)currentY + 117857 + tooltipIDOffset));
							tooltipIDOffset++;
						}
					}
				}
			}

			int innerY = 0;

			GUI.color = Color.white;

			for (int i = 0; i < groupings.Count; i++)
			{
				IGrouping<HediffDef, Hediff> grouping = groupings[i];
				string hediffLabel = GenLabelForHediffGroup(grouping);
				var hediffsByPriority = grouping.OrderByDescending(x => x.TendableNow(true) ? x.TendPriority : -1);

				Hediff representativeHediff = grouping.First();
				if (grouping.Count() > 1)
				{
					hediffLabel = hediffLabel + " x" + grouping.Count().ToString();
				}
				if (settings.italicizeMissing && representativeHediff.def == HediffDefOf.MissingBodyPart)
				{
					hediffLabel = hediffLabel.ApplyTag("i");
				}

				int iconsWidth = CalcIconsWidthForGrouping(grouping, out int iconCount);
				float iconOverlap = 1f;
				if (iconsWidth > (rowRect.width - column_bodypartWidth) / 2f)
				{
					iconOverlap = ((rowRect.width - column_bodypartWidth) / 2f) / (float)iconsWidth;
					iconsWidth = (int)((rowRect.width - column_bodypartWidth) / 2f);
				}
				int hediffLabelWidth = (int)((rowRect.width - column_bodypartWidth) - iconsWidth);

				int hediffTextHeight = (int)Text.CalcHeight(hediffLabel, hediffLabelWidth);


				int hediffColumnWidth = (int)(rowRect.width - column_bodypartWidth);

				if (settings.severityBarMode != CompactHediffs_Settings.SeverityBarMode.Off && settings.severityBarsPosition == CompactHediffs_Settings.BarPosition.Above)
				{
					foreach (Hediff hediff in grouping)
					{
						Rect barRect = new Rect(column_bodypartWidth, currentY + innerY, hediffColumnWidth, settings.internalBarHeight).Rounded();
						innerY += DrawSeverityBar(settings, barRect, hediff);
                        if (settings.showCumulativeThreatment) 
						{
							Rect cumulativeBarRect = new Rect(column_bodypartWidth, currentY + innerY, hediffColumnWidth, settings.internalBarHeight).Rounded();
							innerY += DrawCumulativeThreatmentBar(settings, cumulativeBarRect, hediff);
						}
					}
				}

				Rect fullHediffRect = new Rect(column_bodypartWidth, currentY + innerY, rowRect.width - column_bodypartWidth, hediffTextHeight).Rounded();
				Rect hediffLabelrect = new Rect(column_bodypartWidth, currentY + innerY, hediffLabelWidth, hediffTextHeight).Rounded();

				GUI.color = GetHediffColor(settings, representativeHediff);
				//this is where smartMedicine transpiles its float menu into, so lets follow suit
				Widgets.Label(hediffLabelrect, hediffLabel);
				GUI.color = Color.white;
				if (CompactHediffsMod.smartMedicineLoaded)
				{
					MedicalCareCategory defaultCare = method_smartMedicine_GetPawnMedicalCareCategory_GetCare.GetValue<MedicalCareCategory>(pawn);
					UI_SmartMedicine.AddSmartMedicineFloatMenuButton(fullHediffRect, hediffsByPriority, defaultCare);
				}


				float widthAccumulator = 0;

				int iconOffset = (int)Math.Max((fullHediffRect.height - IconHeight) / 2f, 0);

				//draw info button
				{
					Rect iconRect = new Rect(rowRect.width - (IconHeight / 2f), fullHediffRect.y + iconOffset, IconHeight / 2f, IconHeight).Rounded();
					CustomInfoCardButtonWidget.CustomInfoCardButton(iconRect, representativeHediff);
					widthAccumulator += iconRect.width;
				}

				var hediffsWithStateIcon = hediffsByPriority.Where(x => x.StateIcon.HasValue);
				//draw non-injury icons first
				foreach (Hediff localHediff in hediffsWithStateIcon.Where(x => x.StateIcon.Texture != Textures.Vanilla_TendedIcon_Well_Injury))
				{
					var hediffStateIcon = localHediff.StateIcon;
					GUI.color = hediffStateIcon.Color;
					int deafultIconOffset = (int)Math.Max((fullHediffRect.height - DefaultIconHeight) / 2f, 0);
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + DefaultIconHeight), fullHediffRect.y + deafultIconOffset, DefaultIconHeight, DefaultIconHeight).Rounded();
					GUI.DrawTexture(iconRect, hediffStateIcon.Texture);
					widthAccumulator += iconRect.width * iconOverlap;
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
					widthAccumulator += iconRect.width * iconOverlap;
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
						widthAccumulator += iconRect.width * iconOverlap;
					}
					else
					{
						break;
					}
				}

				//draw Smart Medicine icon
				if (CompactHediffsMod.smartMedicineLoaded)
				{
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + SmartMedicineIconWidth), fullHediffRect.y + iconOffset, SmartMedicineIconWidth, SmartMedicineIconHeight).Rounded();
					MedicalCareCategory defaultCare = method_smartMedicine_GetPawnMedicalCareCategory_GetCare.GetValue<MedicalCareCategory>(pawn);
					UI_SmartMedicine.DrawSmartMedicineIcon(iconRect, defaultCare, hediffsByPriority.ToList());
				}

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

				foreach(Hediff localHediff in hediffsByPriority) 
				{
					TooltipHandler.TipRegion(fullHediffRect, new TipSignal(() => localHediff.GetTooltip(pawn, field_showHediffsDebugInfo.Value), (int)currentY + 7857 + tooltipIDOffset, TooltipPriority.Default));
					tooltipIDOffset++;
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

		private static int CalcIconsWidthForGrouping(IGrouping<HediffDef, Hediff> grouping, out int count)
		{
			count = 0;
			int iconsWidth = InfoIconWidth;
			foreach (Hediff diff in grouping)
			{
				if (diff.StateIcon.HasValue)
				{
					if (CompactHediffsMod.settings.tendingIcons && diff.StateIcon.Texture == Textures.Vanilla_TendedIcon_Well_General)
						iconsWidth += TendIconWidth;
					else
						iconsWidth += IconHeight;
					count++;
				}
				if (diff.Bleeding)
				{
					if (CompactHediffsMod.settings.bleedingIcons)
						iconsWidth += BleedIconWidth;
					else
						iconsWidth += IconHeight;
					count++;
				}
				if (CompactHediffsMod.smartMedicineLoaded)
				{
					iconsWidth += SmartMedicineIconWidth;
					count++;
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

		private static Color GetHediffColor(CompactHediffs_Settings settings, Hediff hediff)
		{
			if (settings.bloodlossSpecialHandling && hediff.def == HediffDefOf.BloodLoss)
			{
				if (hediff.Severity < 0.65f)
					return new Color(1f, 1f - (hediff.Severity / 0.65f), 1f - (hediff.Severity / 0.65f));
				else
				{
					var factor = ((hediff.Severity - 0.65f) / 0.35f);
					return new Color(1f - (0.5f * factor), 0f, 0.25f * factor);
				}
			}
			return hediff.LabelColor;
		}


		private static int DrawSeverityBar(CompactHediffs_Settings settings, Rect barRect, Hediff hediff)
		{
			if (settings.severityBarMode == CompactHediffs_Settings.SeverityBarMode.Off)
				return 0;

			bool showsSeverity = hediff.SeverityLabel != null;
			if (!settings.showHiddenProgressConditions && !showsSeverity)
 				return 0;

			float maxSeverity = GetMaxSeverityForHediff(hediff);
			float severityFraction = (hediff.Severity / maxSeverity);
			if (severityFraction < 0)
				severityFraction = 0;

			var tendDurationComp = hediff.TryGetComp<HediffComp_TendDuration>();
			var immunizableComp = hediff.TryGetComp<HediffComp_Immunizable>();
			bool hasImmunity = immunizableComp != null && immunizableComp.Immunity > 0.001f;
			bool hasSeverity = severityFraction > 0.001f;
			float immunityFraction = hasImmunity ? immunizableComp.Immunity : 0;

			Color hediffColor = GetHediffColor(settings, hediff);

			if (hasImmunity || hasSeverity)
			{
				GUI.color = Color.black;
				GUI.DrawTexture(barRect, Textures.translucentWhite);

				Rect? commonRect = null;
				Rect? commonRectMirror = null;
				Color commonColor = settings.severityBarHighContrast ? Color.white : hediffColor;
				Rect? severityRect = null;
				Rect? severityRectMirror = null;
				Color severityColor = settings.severityBarHighContrast ? Color.red : hediffColor;
				Rect? immunityRect = null;
				Rect? immunityRectMirror = null;
				Color immunityColor = settings.severityBarHighContrast ? BluishGreen : CyanWhite;
				Texture2D commonTexture = TexUI.FastFillTex;
				Texture2D severityTexture = TexUI.FastFillTex;
				Texture2D immunityTexture = TexUI.FastFillTex;
				if (hediff.def == HediffDefOf.WoundInfection)
				{
					severityTexture = Textures.Bar_Infected;
					immunityTexture = Textures.Bar_Pill;
				}
				severityTexture = Textures.Bar_Infected;
				immunityTexture = Textures.Bar_Pill;


				switch (settings.severityBarMode) 
				{
					case CompactHediffs_Settings.SeverityBarMode.LeftToRight:
						if (immunityFraction > severityFraction)
						{
							severityRect = new Rect(barRect.x, barRect.y, barRect.width * severityFraction, settings.internalBarHeight).Rounded();
							immunityRect = new Rect(barRect.x + severityRect.Value.width, barRect.y, barRect.width * (immunityFraction - severityFraction), settings.internalBarHeight).Rounded();
						}
						else
						{
							if (hasImmunity)
							{
								immunityRect = new Rect(barRect.x, barRect.y, barRect.width * (severityFraction - (severityFraction - immunityFraction)), settings.internalBarHeight).Rounded();
								severityRect = new Rect(barRect.x + immunityRect.Value.width, barRect.y, barRect.width * (severityFraction - immunityFraction), settings.internalBarHeight).Rounded();
							}
							else
							{
								commonRect = new Rect(barRect.x, barRect.y, barRect.width * severityFraction, settings.internalBarHeight).Rounded();
							}
						}
						break;
					case CompactHediffs_Settings.SeverityBarMode.EdgeToMiddle:
						{
							var midRect = new Rect(barRect.x + ((barRect.width / 2f) - 1f), barRect.y, 2f, settings.internalBarHeight).Rounded();
							GUI.color = settings.separatorNightMode ? Color.black : Color.grey;
							GUI.DrawTexture(midRect, TexUI.FastFillTex);

							if (hasImmunity)
							{
								int severityWidth = (int) ((barRect.width / 2f) * severityFraction);
								severityRect = new Rect(barRect.x, barRect.y, severityWidth, settings.internalBarHeight).Rounded();
								int immunityWidth = (int) ((barRect.width / 2f) * immunityFraction);
								immunityRect = new Rect(barRect.x + (barRect.width - immunityWidth), barRect.y, immunityWidth, settings.internalBarHeight).Rounded();
							}
							else
							{
								int commonWidth = (int) ((barRect.width / 2f) * severityFraction);
								commonRect = new Rect(barRect.x, barRect.y, commonWidth, settings.internalBarHeight).Rounded();
								commonRectMirror = new Rect(barRect.x + (barRect.width - commonWidth), barRect.y, commonWidth, settings.internalBarHeight).Rounded();
							}
						}
						break;
					case CompactHediffs_Settings.SeverityBarMode.MiddleToEdge:
						{
							if (hasImmunity)
							{
								int severityWidth = (int) ((barRect.width / 2f) * severityFraction);
								severityRect = new Rect(barRect.x + ((barRect.width / 2f) - severityWidth), barRect.y, severityWidth, settings.internalBarHeight).Rounded();
								int immunityWidth = (int) ((barRect.width / 2f) * immunityFraction);
								immunityRect = new Rect(barRect.x + (barRect.width / 2f), barRect.y, immunityWidth, settings.internalBarHeight).Rounded();
							}
							else
							{
								int commonWidth = (int) ((barRect.width / 2f) * severityFraction);
								commonRect = new Rect(barRect.x + ((barRect.width / 2f) - commonWidth), barRect.y, commonWidth, settings.internalBarHeight).Rounded();
								commonRectMirror = new Rect(barRect.x + (barRect.width / 2f), barRect.y, commonWidth, settings.internalBarHeight).Rounded();
							}
						}
						break;
				}

				if (settings.bloodlossSpecialHandling && hediff.def == HediffDefOf.BloodLoss)
				{
					var bloodlossSpeed = Math.Min(hediff.pawn.health.hediffSet.BleedRateTotal, 4f);
					if (bloodlossSpeed >= 0.1f)
					{
						var alphaMult = Pulser.PulseBrightness(0.5f + bloodlossSpeed * 0.75f, 0.5f);
						commonColor.a *= alphaMult;
					}
					commonColor = hediffColor;
					commonTexture = Textures.Bar_Ragged;
				}
				else
				{
					bool canBeTendedNow = !hediff.IsPermanent() && !hediff.pawn.Dead && hediff.TendableNow(false);
					bool needsTendingNow = canBeTendedNow && tendDurationComp != null && tendDurationComp.tendTicksLeft <= 0;
					if (canBeTendedNow)
					{
						var alphaMult = needsTendingNow ? Pulser.PulseBrightness(2f, 0.5f) : Pulser.PulseBrightness(1f, 0.5f);
						commonColor.a *= alphaMult;
						severityColor.a *= alphaMult;
						immunityColor.a *= alphaMult;
					}
				}

				/*if (!showsSeverity) 
				{
					commonColor.a *= 0.65f;
					severityColor.a *= 0.65f;
					immunityColor.a *= 0.65f;
				}*/

				if (!settings.severityBarTextured)
				{
					commonTexture = TexUI.FastFillTex;
					severityTexture = TexUI.FastFillTex;
					immunityTexture = TexUI.FastFillTex;
				}
				else if(hediff.def == HediffDefOf.Malnutrition)
				{
					commonTexture = Textures.Bar_Malnutrition;
					severityTexture = Textures.Bar_Malnutrition;
				}
				else if (CompactHediffsMod.pawnmorpherLoaded && hediff.GetType().ToString().Contains("Pawnmorph"))
				{
					commonTexture = Textures.Bar_DNA;
					severityTexture = Textures.Bar_DNA;
				}


				if (!settings.severityBarHighContrast)
				{
					if(commonTexture == TexUI.FastFillTex)
						commonColor.a *= 0.75f;
					if (severityTexture == TexUI.FastFillTex)
						severityColor.a *= 0.75f;
					if (immunityTexture == TexUI.FastFillTex)
						immunityColor.a *= 0.75f;
				}


				if (commonRect.HasValue)
				{
					GUI.color = commonColor;
					var drawnRect = commonRect.Value;
					float texScale = commonTexture.height / (float)settings.internalBarHeight;
					GUI.DrawTextureWithTexCoords(drawnRect, commonTexture, new Rect(0,0, texScale * (drawnRect.width / commonTexture.width), texScale * (drawnRect.height / commonTexture.height)));
				}
				if (commonRectMirror.HasValue)
				{
					GUI.color = commonColor;
					var drawnRect = commonRectMirror.Value;
					float texScale = commonTexture.height / (float)settings.internalBarHeight;
					GUI.DrawTextureWithTexCoords(drawnRect, commonTexture, new Rect(0, 0, texScale * (drawnRect.width / commonTexture.width), texScale * (drawnRect.height / commonTexture.height)));
				}
				if (severityRect.HasValue)
				{
					GUI.color = severityColor;
					var drawnRect = severityRect.Value;
					float texScale = severityTexture.height / (float)settings.internalBarHeight;
					GUI.DrawTextureWithTexCoords(drawnRect, severityTexture, new Rect(0, 0, texScale * (drawnRect.width / severityTexture.width), texScale * (drawnRect.height / severityTexture.height)));
				}
				if (severityRectMirror.HasValue)
				{
					GUI.color = severityColor;
					var drawnRect = severityRectMirror.Value;
					float texScale = severityTexture.height / (float)settings.internalBarHeight;
					GUI.DrawTextureWithTexCoords(drawnRect, severityTexture, new Rect(0, 0, texScale * (drawnRect.width / severityTexture.width), texScale * (drawnRect.height / severityTexture.height)));
				}
				if (immunityRect.HasValue)
				{
					GUI.color = immunityColor;
					var drawnRect = immunityRect.Value;
					float texScale = immunityTexture.height / (float)settings.internalBarHeight;
					GUI.DrawTextureWithTexCoords(drawnRect, immunityTexture, new Rect(0, 0, texScale * (drawnRect.width / immunityTexture.width), texScale * (drawnRect.height / immunityTexture.height)));
				}
				if (immunityRectMirror.HasValue)
				{
					GUI.color = immunityColor;
					var drawnRect = immunityRectMirror.Value;
					float texScale = immunityTexture.height / (float)settings.internalBarHeight;
					GUI.DrawTextureWithTexCoords(drawnRect, immunityTexture, new Rect(0, 0, texScale * (drawnRect.width / immunityTexture.width), texScale * (drawnRect.height / immunityTexture.height)));
				}

				GUI.color = Color.white;
				return settings.internalBarHeight;
			}

			return 0;
		}

		private static int DrawCumulativeThreatmentBar(CompactHediffs_Settings settings, Rect barRect, Hediff hediff)
		{
			var tendDurationComp = hediff.TryGetComp<HediffComp_TendDuration>();
			var tendQualityRequired = (tendDurationComp?.props as HediffCompProperties_TendDuration)?.disappearsAtTotalTendQuality;
			if (tendDurationComp == null || !tendQualityRequired.HasValue || tendQualityRequired.Value <= 0)
				return 0;
			var tendedFraction = tendDurationComp.GetTotalTendQuality() / tendQualityRequired.Value;
			if (tendedFraction > 1)
				tendedFraction = 0;

			Color hediffColor = GetHediffColor(settings, hediff);

			GUI.color = Color.black;
			GUI.DrawTexture(barRect, Textures.translucentWhite);

			Rect? immunityRect = null;
			Rect? immunityRectMirror = null;
			Color immunityColor = settings.severityBarHighContrast ? BluishGreen : CyanWhite;
			Texture2D immunityTexture = Textures.Bar_PillBig;

			if (!settings.severityBarHighContrast)
			{
				immunityColor.a *= 0.75f;
			}

			switch (settings.severityBarMode)
			{
				case CompactHediffs_Settings.SeverityBarMode.LeftToRight:
					immunityRect = new Rect(barRect.x, barRect.y, barRect.width * tendedFraction, settings.internalBarHeight).Rounded();
					break;
				case CompactHediffs_Settings.SeverityBarMode.EdgeToMiddle:
					{
						var midRect = new Rect(barRect.x + ((barRect.width / 2f) - 1f), barRect.y, 2f, settings.internalBarHeight).Rounded();
						GUI.color = settings.separatorNightMode ? Color.black : Color.grey;
						GUI.DrawTexture(midRect, TexUI.FastFillTex);

						var width = barRect.width * (tendedFraction / 2f);
						immunityRect = new Rect(barRect.x, barRect.y, width, settings.internalBarHeight).Rounded();
						immunityRectMirror = new Rect(barRect.x + (barRect.width - width), barRect.y, width, settings.internalBarHeight).Rounded();
					}
					break;
				case CompactHediffs_Settings.SeverityBarMode.MiddleToEdge:
					{
						var half = (barRect.width / 2f);
						immunityRect = new Rect(barRect.x + half - (half * tendedFraction), barRect.y, barRect.width * tendedFraction, settings.internalBarHeight).Rounded();
					}
					break;
			}

			bool canBeTendedNow = !hediff.IsPermanent() && !hediff.pawn.Dead && hediff.TendableNow(false);
			bool needsTendingNow = canBeTendedNow && tendDurationComp != null && tendDurationComp.tendTicksLeft <= 0;
			if (canBeTendedNow)
			{
				var alphaMult = needsTendingNow ? Pulser.PulseBrightness(2f, 0.5f) : Pulser.PulseBrightness(1f, 0.5f);
				immunityColor.a *= alphaMult;
			}

			if (!settings.severityBarTextured)
			{
				immunityTexture = TexUI.FastFillTex;
			}
			GUI.color = immunityColor;
			if (immunityRect.HasValue)
			{
				var drawnRect = immunityRect.Value;
				float texScale = immunityTexture.height / (float)settings.internalBarHeight;

				GUI.DrawTextureWithTexCoords(drawnRect, immunityTexture, new Rect(0, 0, texScale * (drawnRect.width / immunityTexture.width), texScale * (drawnRect.height / immunityTexture.height)));
			}
			if (immunityRectMirror.HasValue)
			{
				var drawnRect = immunityRectMirror.Value;
				float texScale = immunityTexture.height / (float)settings.internalBarHeight;
				GUI.DrawTextureWithTexCoords(drawnRect, immunityTexture, new Rect(0, 0, texScale * (drawnRect.width / immunityTexture.width), texScale * (drawnRect.height / immunityTexture.height)));
			}

			GUI.color = Color.white;
			return settings.internalBarHeight;
		}

		private static float GetMaxSeverityForHediff(Hediff hediff)
		{
			float maxSeverity = -1;
			if (hediff.def.lethalSeverity > 0f)
			{
				maxSeverity = hediff.def.lethalSeverity;
			}
			else if (hediff.def.maxSeverity > 0f && hediff.def.maxSeverity < (float.MaxValue - float.Epsilon))
			{
				maxSeverity = hediff.def.maxSeverity;
			}
			else if (hediff is Hediff_Pregnant)
			{
				maxSeverity = 1f;
			}
			return maxSeverity;
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
