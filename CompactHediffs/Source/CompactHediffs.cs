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
            settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
	}
}
