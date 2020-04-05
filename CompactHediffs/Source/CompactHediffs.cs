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
        public static bool pawnmorpherLoaded = false;
        public static bool eliteBionicsLoaded = false;

        public CompactHediffsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<CompactHediffs_Settings>();

            var harmony = new Harmony("PeteTimesSix.CompactHediffs");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            pawnmorpherLoaded = ModLister.GetActiveModWithIdentifier("tachyonite.pawnmorpher") != null;
            eliteBionicsLoaded = ModLister.GetActiveModWithIdentifier("V1024.EBFramework") != null;
        }

        public override string SettingsCategory()
        {
            return "CompactHediffs_ModTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect) 
        {
            settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
	}
}
