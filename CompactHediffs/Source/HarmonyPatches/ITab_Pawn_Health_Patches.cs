using HarmonyLib;
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
    [HarmonyPatch(typeof(ITab_Pawn_Health),MethodType.Constructor)]
    public static class ITab_Pawn_Health_Patches
    {
        public static ITab_Pawn_Health singletonRef;
        public static Traverse<Vector2> field_size;

        public static Vector2? initialSize;

        [HarmonyPostfix]
        public static void ITab_Pawn_Health_Patches_ctor_Postifx(ITab_Pawn_Health __instance)
        {
            singletonRef = __instance;
            field_size = Traverse.Create(singletonRef).Field<Vector2>("size");
            setNewExtraSize(CompactHediffsMod.settings.extraTabWidth, CompactHediffsMod.settings.extraTabHeight);
        }

        public static void setNewExtraSize(float newExtraWidth, float newExtraHeight)
        {
            if(field_size != null)
            {
                if (!initialSize.HasValue)
                    initialSize = field_size.Value;
                field_size.Value = new Vector2(initialSize.Value.x + newExtraWidth, initialSize.Value.y + newExtraHeight);
            }
        }
    }
}
