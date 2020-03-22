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

        [HarmonyPostfix]
        public static void ITab_Pawn_Health_Patches_ctor_Postifx(ITab_Pawn_Health __instance)
        {
            singletonRef = __instance;
            field_size = Traverse.Create(singletonRef).Field<Vector2>("size");
            setNewExtraWidth(CompactHediffsMod.settings.extraTabWidth);
        }

        public static void setNewExtraWidth(float newExtraWidth)
        {
            if(field_size != null)
                field_size.Value = new Vector2(630f + newExtraWidth, 430f);
        }
    }
}
