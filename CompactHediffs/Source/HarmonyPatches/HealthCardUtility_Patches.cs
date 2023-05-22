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
        public static IEnumerable<IGrouping<BodyPartRecord, Hediff>> HealthCardUtility_VisibleHediffGroupsInOrder_Postfix(IEnumerable<IGrouping<BodyPartRecord, Hediff>> returned, Pawn pawn, bool showBloodLoss)
        {
            returned = CustomHealthCardUtility.ReorderHediffGroups(returned, pawn, showBloodLoss);
            foreach(var group in returned) 
            {
                yield return group;
            }
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
                    found = true;
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
            var settings = CompactHediffsMod.Settings;
            if (!settings.Enabled || settings.extraTabWidth < 1f || !ITab_Pawn_Health_Patches.hasInitialSize)
            {
                return 0.375f;
            }
            else
            {
                float width = ITab_Pawn_Health_Patches.initialSize.x;
                float curWidth = ITab_Pawn_Health_Patches.GetSize().x;
                return 0.375f * (width / curWidth);
            }
        }
    }

    [HarmonyPatch(typeof(HealthCardUtility), "DrawHediffRow")]
    public static class HealthCardUtility_DrawHediffRow
    {
        [HarmonyPrefix]
        public static bool HealthCardUtility_DrawHediffRow_DestructivePrefix(Rect rect, Pawn pawn, IEnumerable<Hediff> diffs, ref float curY)
        {
            if (CompactHediffsMod.Settings.Enabled)
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
