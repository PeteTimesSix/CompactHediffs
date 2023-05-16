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
using PeteTimesSix.CompactHediffs.ModCompat;

namespace PeteTimesSix.CompactHediffs
{
    public class CompactHediffsMod : Mod
    {
        public static CompactHediffs_Settings Settings { get; set; }
        public static Harmony Harmony { get; set; }

        public CompactHediffsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CompactHediffs_Settings>();

            Harmony = new Harmony("PeteTimesSix.CompactHediffs");
            Harmony.PatchAll();

        }

        public override string SettingsCategory()
        {
            return "CompactHediffs_ModTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoSettingsWindowContents(inRect);
        }
	}

    [StaticConstructorOnStartup]
    public static class CompactHediffs_PostInit
    {

        static CompactHediffs_PostInit()
        {
        }
    }
}
