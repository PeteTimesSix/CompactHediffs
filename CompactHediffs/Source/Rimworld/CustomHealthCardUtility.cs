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
using static HarmonyLib.AccessTools;
using PeteTimesSix.CompactHediffs.ModCompat;

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
		public static readonly int DevRemoveButtonWidth = 20;

		public static readonly int SmartMedicineIconWidth = 20;
		public static readonly int SmartMedicineIconHeight = 20;

		//public static Traverse<float> field_lastMaxIconsTotalWidth;
		//public static Traverse<float> field_scrollViewHeight;
		//public static Traverse<Vector2> field_scrollPosition;

		public static FieldRef<bool> highlight;
		public static FieldRef<bool> showHediffsDebugInfo;

		public delegate bool _canEntryBeClicked(IEnumerable<Hediff> diffs, Pawn pawn);
		public delegate void _entryClicked(IEnumerable<Hediff> diffs, Pawn pawn);
		public delegate string _getTooltip(Pawn pawn, BodyPartRecord part);
		public delegate float _getListPriority(BodyPartRecord rec);
		public static _canEntryBeClicked CanEntryBeClicked;
		public static _entryClicked EntryClicked;
		public static _getTooltip GetTooltip;
		public static _getListPriority GetListPriority;



		static CustomHealthCardUtility() 
		{
			var healthCardType = typeof(HealthCardUtility);
			highlight = StaticFieldRefAccess<bool>(AccessTools.Field(healthCardType, "highlight"));
			showHediffsDebugInfo = StaticFieldRefAccess<bool>(AccessTools.Field(healthCardType, "showHediffsDebugInfo"));

			CanEntryBeClicked = AccessTools.MethodDelegate<_canEntryBeClicked>(AccessTools.Method(healthCardType, "CanEntryBeClicked"));
			EntryClicked = AccessTools.MethodDelegate<_entryClicked>(AccessTools.Method(healthCardType, "EntryClicked"));
			GetTooltip = AccessTools.MethodDelegate<_getTooltip>(AccessTools.Method(healthCardType, "GetTooltip"));
			GetListPriority = AccessTools.MethodDelegate<_getListPriority>(AccessTools.Method(healthCardType, "GetListPriority"));
		}

		public static void DrawHediffRow(Rect rowRect, Pawn pawn, IEnumerable<Hediff> diffs, ref float curY)
		{
			rowRect = rowRect.Rounded();
			int currentY = (int)curY;

			var settings = CompactHediffsMod.Settings;


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

            int tooltipIDOffset = 0;
            if (part == null)
			{
				Widgets.Label(new Rect(0f, currentY, bodypartLabelWidth, 100f), bodyPartText);
			}
			else
			{
				Widgets.Label(new Rect(0f, currentY, bodypartLabelWidth, 100f), bodyPartText);

                TooltipHandler.TipRegion(wholeEntryRect, new TipSignal(() => GetTooltip(pawn, part), (int)currentY + 7857 + tooltipIDOffset, TooltipPriority.Pawn));
                tooltipIDOffset++;

                if (replacingPart != null)
				{
					GUI.color = Color.white;
					int iconOffset = (int)Math.Max((bodyPartLabelHeight - IconHeight) / 2f, 0);
					Rect iconRect = new Rect(bodypartLabelWidth, currentY + iconOffset, IconHeight / 2f, IconHeight).Rounded();
					CustomInfoCardButtonWidget.CustomInfoCardButton(iconRect, replacingPart);

					var justColumnRect = wholeEntryRect.LeftPartPixels(column_bodypartWidth);
                    if (Mouse.IsOver(justColumnRect))
                    {
                        if (replacingPart != null)
                        {
                            TooltipHandler.TipRegion(justColumnRect, new TipSignal(() => replacingPart.GetTooltip(pawn, showHediffsDebugInfo.Invoke()), (int)currentY + 7857 + tooltipIDOffset, TooltipPriority.Pawn));
                            tooltipIDOffset++;

                            if (Pawnmorpher.active && Pawnmorpher.GetTooltip != null)
                            {
                                //copied from Pawnmorph.PatchHealthCardUtilityDrawHediffRow
                                string tooltip = Pawnmorpher.GetTooltip(new List<Hediff>() { replacingPart });
                                if (!string.IsNullOrWhiteSpace(tooltip))
                                {
                                    TooltipHandler.TipRegion(justColumnRect, new TipSignal(() => tooltip, (int)currentY + 117857 + tooltipIDOffset));
                                    tooltipIDOffset++;
                                }
                            }
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

				//do tooltips
                if (Mouse.IsOver(fullHediffRect))
                {
					List<string> uniqueTooltips = new List<string>();
					foreach(var individualHediff in grouping)
					{
                        string tooltip = individualHediff.GetTooltip(pawn, showHediffsDebugInfo.Invoke());
						if(!string.IsNullOrWhiteSpace(tooltip))
							uniqueTooltips.Add(tooltip);

                        if (Pawnmorpher.active && Pawnmorpher.GetTooltip != null)
                        {
                            //copied from Pawnmorph.PatchHealthCardUtilityDrawHediffRow
                            tooltip = Pawnmorpher.GetTooltip(new List<Hediff>() { individualHediff });
                            if (!string.IsNullOrWhiteSpace(tooltip))
								uniqueTooltips.Add(tooltip);
                        }
                    }

					foreach(var tooltip in uniqueTooltips)
					{
                        TooltipHandler.TipRegion(fullHediffRect, new TipSignal(() => tooltip, (int)currentY + 117857 + tooltipIDOffset));
                        tooltipIDOffset++;
                    }
                }

                GUI.color = GetHediffColor(settings, representativeHediff);
				//this is where smartMedicine transpiles its float menu into, so lets follow suit
				Widgets.Label(hediffLabelrect, hediffLabel);
				GUI.color = Color.white;
				if (SmartMedicine.active)
				{
					MedicalCareCategory defaultCare = SmartMedicine.GetCare(pawn);
					UI_SmartMedicine.AddSmartMedicineFloatMenuButton(fullHediffRect, hediffsByPriority, defaultCare);
				}


				float widthAccumulator = 0;

				int iconOffset = (int)Math.Max((fullHediffRect.height - IconHeight) / 2f, 0);

				//draw dev buttons
				if (DebugSettings.godMode && Current.ProgramState == ProgramState.Playing)
				{
                    {
                        Rect iconRect = new Rect(rowRect.width - DevRemoveButtonWidth, fullHediffRect.y + iconOffset, DevRemoveButtonWidth, DevRemoveButtonWidth).Rounded();
                        TooltipHandler.TipRegion(iconRect, () => "DEV: Remove hediff", 1071045645);
                        GUI.color = Color.red;
                        if (GUI.Button(iconRect, TexButton.Delete))
                        {
                            foreach (var hediff in grouping)
                                pawn.health.RemoveHediff(hediff);
                        }
                        widthAccumulator += iconRect.width;
                    }
					if(grouping.Count() == 1)
					{
                        var singleHediff = grouping.First();
                        if (singleHediff.def.maxSeverity < float.MaxValue || singleHediff.def.lethalSeverity > 0f)
                        {
                            Rect iconRect = new Rect(rowRect.width - (widthAccumulator + DevRemoveButtonWidth), fullHediffRect.y + iconOffset, DevRemoveButtonWidth, DevRemoveButtonWidth).Rounded();
                            GUI.color = Color.cyan;
                            TooltipHandler.TipRegion(iconRect, () => "DEV: Set severity", 2131648723);
                            if (GUI.Button(iconRect, TexButton.Save))
                            {
                                Find.WindowStack.Add(new Dialog_DebugSetSeverity(singleHediff));
                            }
                            widthAccumulator += iconRect.width;
                        }
                        if (singleHediff.TryGetComp<HediffComp_Disappears>() != null)
                        {
                            Rect iconRect = new Rect(rowRect.width - (widthAccumulator + DevRemoveButtonWidth), fullHediffRect.y + iconOffset, DevRemoveButtonWidth, DevRemoveButtonWidth).Rounded();
                            GUI.color = Color.yellow;
                            TooltipHandler.TipRegion(iconRect, () => "DEV: Set remaining time", 6234623);
                            if (GUI.Button(iconRect, TexButton.Save))
                            {
                                Find.WindowStack.Add(new Dialog_DebugSetHediffRemaining(singleHediff));
                            }
                            widthAccumulator += iconRect.width;
                        }
                    }
                }

                //draw info button
                {
                    GUI.color = Color.white;
                    Rect iconRect = new Rect(rowRect.width - (widthAccumulator + (IconHeight / 2f)), fullHediffRect.y + iconOffset, IconHeight / 2f, IconHeight).Rounded();
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

				//draw other mod icon
				if (SmartMedicine.active)
				{
					Rect iconRect = new Rect(rowRect.width - (widthAccumulator + SmartMedicineIconWidth), fullHediffRect.y + iconOffset, SmartMedicineIconWidth, SmartMedicineIconHeight).Rounded();
					MedicalCareCategory defaultCare = SmartMedicine.GetCare(pawn);
					UI_SmartMedicine.DrawSmartMedicineIcon(iconRect, defaultCare, hediffsByPriority.ToList());
				}
                if (ChooseYourMedicine.active)
                {
                    Rect iconRect = new Rect(rowRect.width - (widthAccumulator + ChooseYourMedicine.IconWidth), fullHediffRect.y + iconOffset, ChooseYourMedicine.IconWidth, ChooseYourMedicine.IconHeight).Rounded();
                    ChooseYourMedicine.DrawButtonToAssignMedManually(iconRect, pawn, grouping);
                }

                innerY += hediffTextHeight;

				if (settings.severityBarMode != CompactHediffs_Settings.SeverityBarMode.Off && settings.severityBarsPosition == CompactHediffs_Settings.BarPosition.Below)
				{
					foreach (Hediff hediff in grouping)
					{
						Rect barRect = new Rect(column_bodypartWidth, currentY + innerY, hediffColumnWidth, settings.internalBarHeight).Rounded();
						int extraHeight = DrawSeverityBar(settings, barRect, hediff);
						innerY += extraHeight;
						if (settings.showCumulativeThreatment)
						{
							Rect cumulativeBarRect = new Rect(column_bodypartWidth, currentY + innerY, hediffColumnWidth, settings.internalBarHeight).Rounded();
							innerY += DrawCumulativeThreatmentBar(settings, cumulativeBarRect, hediff);
						}
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
				if (Widgets.ButtonInvisible(wholeEntryRect, CanEntryBeClicked(diffs, pawn)))
				{
					EntryClicked(diffs, pawn);
				}
			}

			curY = (float)currentY;
		}

		public static Hediff GetReplacingPart(IEnumerable<Hediff> hediffs, BodyPartRecord part)
		{
			var replacingPart = hediffs.Where(x => x is Hediff_AddedPart).FirstOrDefault();
			if (replacingPart != null)
				return replacingPart;

			if (Pawnmorpher.active)
			{
				foreach (var hediff in hediffs)
				{
					if (Pawnmorpher.coreMutationHediffDefs.Contains(hediff.def))
						return hediff;
				}
			}

			return null;
		}
		
		public static string MakeBodyPartText(Pawn pawn, BodyPartRecord part, Hediff replacingPart)
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


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Harmony patches targeting this name")]
        private static float getPartMaxHealth(Pawn pawn, BodyPartRecord part)
		{
			if (!EBF.active)
				return part.def.GetMaxHealth(pawn);
			else
				return EBF.GetMaxHealth_Cached(part.def, pawn, part);
		}

		private static int CalcIconsWidthForGrouping(IGrouping<HediffDef, Hediff> grouping, out int count)
		{
			count = 0;
			int iconsWidth = InfoIconWidth;
			if (Prefs.DevMode && Current.ProgramState == ProgramState.Playing)
				iconsWidth += DevRemoveButtonWidth;
			foreach (Hediff diff in grouping)
			{
				if (diff.StateIcon.HasValue)
				{
					if (CompactHediffsMod.Settings.tendingIcons && diff.StateIcon.Texture == Textures.Vanilla_TendedIcon_Well_General)
						iconsWidth += TendIconWidth;
					else
						iconsWidth += IconHeight;
					count++;
				}
				if (diff.Bleeding)
				{
					if (CompactHediffsMod.Settings.bleedingIcons)
						iconsWidth += BleedIconWidth;
					else
						iconsWidth += IconHeight;
					count++;
				}
				if (SmartMedicine.active)
				{
					iconsWidth += SmartMedicineIconWidth;
					count++;
                }
                if (ChooseYourMedicine.active)
                {
                    iconsWidth += ChooseYourMedicine.IconWidth;
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
				else if(ModsConfig.AnomalyActive && (hediff.def == HediffDefOf.CubeComa || hediff.def == HediffDefOf.CubeInterest || hediff.def == HediffDefOf.CubeRage || hediff.def == HediffDefOf.CubeWithdrawal))
				{
                    commonTexture = Textures.Bar_Cubes;
                    severityTexture = Textures.Bar_Cubes;
                }
				else if (Pawnmorpher.active && Pawnmorpher.pawnmorpherHediffDefs.Contains(hediff.def))
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
			if (highlight.Invoke())
			{
				GUI.color = StaticHighlightColor;
				GUI.DrawTexture(rowRect, TexUI.HighlightTex);
			}
			highlight.Invoke() = !highlight.Invoke();
			if (Mouse.IsOver(rowRect))
			{
				GUI.color = HighlightColor;
				GUI.DrawTexture(rowRect, TexUI.HighlightTex);
			}
		}

		public static IEnumerable<IGrouping<BodyPartRecord, Hediff>> ReorderHediffGroups(IEnumerable<IGrouping<BodyPartRecord, Hediff>> returned, Pawn pawn, bool showBloodLoss)
		{
			Func<BodyPartRecord, float> getListPriority = (BodyPartRecord rec) => GetListPriority(rec);

			return returned.OrderByDescending(x => x.Max(i => i.TendableNow(true) ? i.TendPriority : -1)).ThenByDescending(i => getListPriority(i.First().Part));
		}

	}
}
