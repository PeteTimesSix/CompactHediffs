using HarmonyLib;
using PeteTimesSix.CompactHediffs.Rimworld;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.CompactHediffs.HarmonyPatches
{
    /*
    [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
    public static class PlaySettings_DoPlaySettingsGlobalControls
    {

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || (row == null)) { return; }

            var isEnabled = CompactHediffsMod.settings.Enabled;
            row.ToggleableIcon(ref isEnabled, Textures.CategorizedResourceReadout, "Toggle Hediff compact list", SoundDefOf.Mouseover_ButtonToggle);
            CompactHediffsMod.settings.Enabled = isEnabled;
        }
    }*/
}
