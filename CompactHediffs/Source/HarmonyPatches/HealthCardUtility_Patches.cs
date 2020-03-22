using HarmonyLib;
using PeteTimesSix.CompactHediffs.Rimworld;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.CompactHediffs.HarmonyPatches
{
    [HarmonyPatch(typeof(HealthCardUtility), "VisibleHediffGroupsInOrder")]
    public static class HealthCardUtility_VisibleHediffGroupsInOrder
    {
        [HarmonyPostfix]
        static IEnumerable<IGrouping<BodyPartRecord, Hediff>> HealthCardUtility_VisibleHediffGroupsInOrder_Postfix(IEnumerable<IGrouping<BodyPartRecord, Hediff>> returned, Pawn pawn, bool showBloodLoss)
        {
            returned = CustomHealthCardUtility.ReorderHediffGroups(returned, pawn, showBloodLoss);
            foreach(var group in returned) 
            {
                yield return group;
            }

			/*var method_getListPriority = Traverse.Create(typeof(HealthCardUtility)).Method("GetListPriority", new Type[] { typeof(BodyPartRecord) });
			Func<BodyPartRecord, float> getListPriority = (BodyPartRecord rec) => method_getListPriority.GetValue<float>(rec);

			var method_visibleHediffs = Traverse.Create(typeof(HealthCardUtility)).Method("VisibleHediffs", new Type[] { typeof(Pawn), typeof(bool) });
			Func<Pawn, bool, IEnumerable<Hediff>> visibleHediffs = (Pawn p, bool s) => method_visibleHediffs.GetValue<IEnumerable<Hediff>>(p, s);

			var grouping = visibleHediffs(pawn, showBloodLoss).GroupBy(x => x.Part).OrderByDescending(x => getListPriority(x.First().Part));
			foreach (var group in grouping)
			{
				yield return group;
			}*/
		}
    }

    /*[HarmonyPatch(typeof(HealthCardUtility), "DrawHediffListing")]
    public static class HealthCardUtility_DrawHediffListing
    {
        [HarmonyPrefix]
        static void HealthCardUtility_DrawHediffListing_Prefix()
        {
            CustomHealthCardUtility.drawingFirstRow = true; 
        }
    }*/

    [HarmonyPatch(typeof(HealthCardUtility), "DrawPawnHealthCard")]
    public static class HealthCardUtility_DrawPawnHealthCard
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HealthCardUtility_DrawPawnHealthCard_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float && (float)instruction.operand == 0.375f)
                {
                    MethodInfo method_GetAlteredMult = typeof(HealthCardUtility_DrawPawnHealthCard).GetMethod("GetAlteredMult");
                    yield return new CodeInstruction(OpCodes.Call, method_GetAlteredMult);
                }
                else 
                {
                    yield return instruction;
                }
            }
            if (!found) 
            {
                Log.Warning("Failed to find rect size constant to transpile.");
            }
        }

        public static float GetAlteredMult() 
        {
            var settings = CompactHediffsMod.settings;
            if (!settings.Enabled || settings.extraTabWidth < 1f)
            {
                return 0.375f;
            }
            else
            {
                return 0.375f * (630f / (630f + settings.extraTabWidth));
            }
        }
    }

    [HarmonyPatch(typeof(HealthCardUtility), "DrawHediffRow")]
    public static class HealthCardUtility_DrawHediffRow
    {
        [HarmonyPrefix]
        static bool HealthCardUtility_DrawHediffRow_DestructivePrefix(Rect rect, Pawn pawn, IEnumerable<Hediff> diffs, ref float curY)
        {
            if (CompactHediffsMod.settings.Enabled)
            {
                CustomHealthCardUtility.DrawHediffRow(rect, pawn, diffs, ref curY);
                return false;
            }
            else 
            {
                return true;
            }
        }
    }
}
