using PeteTimesSix.CompactHediffs.HarmonyPatches;
using PeteTimesSix.CompactHediffs.Rimworld;
using PeteTimesSix.CompactHediffs.Rimworld.UI;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace PeteTimesSix.CompactHediffs
{
    public class CompactHediffs_Settings : ModSettings
    {
        public enum SeverityBarMode 
        {
            Off,
            LeftToRight,
            EdgeToMiddle,
            MiddleToEdge,
        }


        public bool _enabled = true;
        public bool Enabled { get { return _enabled; }
            set 
            {
                if(value)
                    ITab_Pawn_Health_Patches.setNewExtraWidth(extraTabWidth);
                else
                    ITab_Pawn_Health_Patches.setNewExtraWidth(0f);
                _enabled = value;
            }
        }

        public bool replacingPartInBodypart = true;
        public bool italicizeMissing = true;
        public bool verticalSeparator = true;
        public bool horizontalSeparator = true;
        public bool internalSeparator = true;
        public bool separatorNightMode = true;
        public bool tendPrioritySort = true;
        public bool evenOddHighlights = false;

        public bool bodypartHealthbars = true;
        public bool bleedingIcons = true;

        public SeverityBarMode severityBarMode = SeverityBarMode.LeftToRight;
        public bool severityBarHighContrast = true;
        public bool showHiddenProgressConditions = false;

        public float extraTabWidth = 100f;

        public float verticalSeparatorWidth = 2f;
        public float horizontalSeparatorHeight = 2f;
        public float internalSeparatorHeight = 2f;
        public float healthBarHeight = 3f;
        public float internalBarHeight = 3f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref _enabled, "isEnabled", true);

            Scribe_Values.Look<bool>(ref replacingPartInBodypart, "replacingPartInBodypart", true);
            Scribe_Values.Look<bool>(ref italicizeMissing, "italicizeMissing", true);
            Scribe_Values.Look<bool>(ref evenOddHighlights, "evenOddHighlights", false);
            Scribe_Values.Look<bool>(ref verticalSeparator, "verticalSeparator", true);
            Scribe_Values.Look<bool>(ref horizontalSeparator, "horizontalSeparator", true);
            Scribe_Values.Look<bool>(ref internalSeparator, "internalSeparator", true);
            Scribe_Values.Look<bool>(ref separatorNightMode, "separatorNightMode", true);
            Scribe_Values.Look<bool>(ref tendPrioritySort, "tendPrioritySort", true);
            Scribe_Values.Look<bool>(ref bodypartHealthbars, "bodypartHealthbars", true);
            Scribe_Values.Look<bool>(ref bleedingIcons, "bleedingIcons", true);

            Scribe_Values.Look<float>(ref extraTabWidth, "extraTabWidth", 100f);

            Scribe_Values.Look<SeverityBarMode>(ref severityBarMode, "severityBarMode", SeverityBarMode.LeftToRight);
            Scribe_Values.Look<bool>(ref severityBarHighContrast, "severityBarHighContrast", false);
            Scribe_Values.Look<bool>(ref showHiddenProgressConditions, "showHiddenProgressConditions", false);
        }

        internal void DoSettingsWindowContents(Rect inRect)
        {
            bool didDraw = false;

            if (Current.ProgramState == ProgramState.Playing)
            {
                var selector = Find.Selector;
                if (selector != null && selector.NumSelected > 0 && selector.SelectedObjects[0] is Pawn)
                {
                    Pawn pawn = selector.SelectedObjects[0] as Pawn;
                    
                    float width =  (1 - 0.375f) * (630f + extraTabWidth);
                    Rect rect = inRect.TopHalf().ContractedBy(10f);

                    if (rect.width > width) 
                    {
                        float diff = rect.width - width;
                        rect.x += diff / 2f;
                        rect.width = width;
                    }

                    HealthCardUtility.DrawHediffListing(rect, pawn, true);
                    didDraw = true;
                }
            }

            if (!didDraw)
            {
                Widgets.NoneLabelCenteredVertically(inRect.TopHalf().ContractedBy(10f), "settings_selectExamplePawnIngame".Translate());
            }

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect.BottomHalf());

            listingStandard.ColumnWidth = (inRect.width) / 3f;
            listingStandard.ColumnWidth -= 30f;

            bool localEnabled = Enabled;
            listingStandard.CheckboxLabeled("settings_IsEnabled".Translate(), ref localEnabled, "settings_IsEnabled_tooltip".Translate());
            Enabled = localEnabled;
            listingStandard.GapLine();

            listingStandard.CheckboxLabeled("settings_replacingPartInBodypart".Translate(), ref replacingPartInBodypart, "settings_replacingPartInBodypart_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_italicizeMissing".Translate(), ref italicizeMissing, "settings_italicizeMissing_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_evenOddHighlights".Translate(), ref evenOddHighlights, "settings_evenOddHighlights_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_verticalSeparator".Translate(), ref verticalSeparator, "settings_verticalSeparator_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_horizontalSeparator".Translate(), ref horizontalSeparator, "settings_horizontalSeparator_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_internalSeparator".Translate(), ref internalSeparator, "settings_internalSeparator_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_separatorNightMode".Translate(), ref separatorNightMode, "settings_separatorNightMode_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_tendPrioritySort".Translate(), ref tendPrioritySort, "settings_tendPrioritySort_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_bodypartHealthbars".Translate(), ref bodypartHealthbars, "settings_bodypartHealthbars_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_bleedingIcons".Translate(), ref bleedingIcons, "settings_bleedingIcons_tooltip".Translate());

            listingStandard.NewColumn();
            listingStandard.ColumnWidth = ((inRect.width) / 3f) * 2f;


            listingStandard.Label($"{"settings_extraTabWidth".Translate()}: {extraTabWidth.ToString("F0")}px");
            extraTabWidth = listingStandard.Slider(extraTabWidth, 0f, 400f);

            listingStandard.EnumSelector(ref severityBarMode, "settings_severityBars".Translate(), valueLabelPrefix: "SeverityBarMode_", tooltip: "settings_severityBars_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_severityBarHighContrast".Translate(), ref severityBarHighContrast, "settings_severityBarHighContrast_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_showHiddenProgressConditions".Translate(), ref showHiddenProgressConditions, "settings_showHiddenProgressConditions_tooltip".Translate());

            listingStandard.End();
        }
    }
}