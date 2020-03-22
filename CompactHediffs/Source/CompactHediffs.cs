using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using RimWorld;
using PeteTimesSix.CompactHediffs.Rimworld;
using PeteTimesSix.CompactHediffs.HarmonyPatches;

namespace PeteTimesSix.CompactHediffs
{
    public class CompactHediffsMod : Mod
    {
        public static CompactHediffs_Settings settings;

        public CompactHediffsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<CompactHediffs_Settings>();

            var harmony = new Harmony("PeteTimesSix.CompactHediffs");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override string SettingsCategory()
        {
            return "CompactHediffs_ModTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect) 
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect.LeftHalf());
            //listingStandard.CheckboxLabeled("settings_".Translate(), ref settings., "settings__tooltip".Translate());

            bool localEnabled = settings.Enabled;
            listingStandard.CheckboxLabeled("settings_IsEnabled".Translate(), ref localEnabled, "settings_IsEnabled_tooltip".Translate());
            settings.Enabled = localEnabled;
            listingStandard.GapLine();

            listingStandard.CheckboxLabeled("settings_replacingPartInBodypart".Translate(), ref settings.replacingPartInBodypart, "settings_replacingPartInBodypart_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_italicizeMissing".Translate(), ref settings.italicizeMissing, "settings_italicizeMissing_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_evenOddHighlights".Translate(), ref settings.evenOddHighlights, "settings_evenOddHighlights_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_verticalSeparator".Translate(), ref settings.verticalSeparator, "settings_verticalSeparator_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_horizontalSeparator".Translate(), ref settings.horizontalSeparator, "settings_horizontalSeparator_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_separatorNightMode".Translate(), ref settings.separatorNightMode, "settings_separatorNightMode_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_tendPrioritySort".Translate(), ref settings.tendPrioritySort, "settings_tendPrioritySort_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_bodypartHealthbars".Translate(), ref settings.bodypartHealthbars, "settings_bodypartHealthbars_tooltip".Translate());
            listingStandard.CheckboxLabeled("settings_bleedingIcons".Translate(), ref settings.bleedingIcons, "settings_bleedingIcons_tooltip".Translate());

            listingStandard.Label($"{"settings_extraTabWidth".Translate()}: {settings.extraTabWidth.ToString("F0")}px");
            settings.extraTabWidth = listingStandard.Slider(settings.extraTabWidth, 0f, 400f);

            listingStandard.End();

            

            base.DoSettingsWindowContents(inRect);
        }
	}
}
