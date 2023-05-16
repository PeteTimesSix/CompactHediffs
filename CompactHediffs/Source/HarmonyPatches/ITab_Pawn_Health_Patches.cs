using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace PeteTimesSix.CompactHediffs.HarmonyPatches
{
    [HarmonyPatch(typeof(ITab_Pawn_Health),MethodType.Constructor)]
    public static class ITab_Pawn_Health_Patches
    {
        public static ITab_Pawn_Health singletonRef;
        public static bool hasInitialSize = false;
        public static Vector2 initialSize;
        private static FieldRef<ITab_Pawn_Health, Vector2> size;

        [HarmonyPrepare]
        public static void Init() 
        {
            size = AccessTools.FieldRefAccess<ITab_Pawn_Health, Vector2>(AccessTools.Field(typeof(ITab_Pawn_Health), "size"));
        }

        [HarmonyPostfix]
        public static void ITab_Pawn_Health_Patches_ctor_Postifx(ITab_Pawn_Health __instance)
        {
            singletonRef = __instance;
            //field_size = Traverse.Create(singletonRef).Field<Vector2>("size");
            setNewExtraSize(CompactHediffsMod.Settings.extraTabWidth, CompactHediffsMod.Settings.extraTabHeight);
        }

        public static void setNewExtraSize(float newExtraWidth, float newExtraHeight)
        {
            if (!hasInitialSize)
            {
                initialSize = size(singletonRef);
                hasInitialSize = true;
            }
            size(singletonRef) = new Vector2(initialSize.x + newExtraWidth, initialSize.y + newExtraHeight);
        }

        public static Vector2 GetSize()
        {
            return size(singletonRef);
        }
    }
}
