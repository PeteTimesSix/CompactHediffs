using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static PeteTimesSix.CompactHediffs.ModCompat.EBF;

namespace PeteTimesSix.CompactHediffs.ModCompat
{
	[StaticConstructorOnStartup]
    public static class SmartMedicine
    {
        public static bool active = false;

		public static Texture2D[] careTextures;

		public delegate void _LabelButton(Rect rect, string text, Hediff hediff);
		public delegate Dictionary<Hediff, MedicalCareCategory> _PriorityCareCompGet();
		public delegate MedicalCareCategory _GetCare(Pawn pawn);
		public static _LabelButton LabelButton;
		public static _PriorityCareCompGet PriorityCareCompGet;
		public static _GetCare GetCare;

		static SmartMedicine()
		{
			active = ModLister.GetActiveModWithIdentifier("Uuugggg.SmartMedicine") != null;
			if (active)
			{
                try
                {
                    careTextures = AccessTools.StaticFieldRefAccess<Texture2D[]>(AccessTools.Field(typeof(MedicalCareUtility), "careTextures")).Invoke();

                    LabelButton = AccessTools.MethodDelegate<_LabelButton>(AccessTools.Method("SmartMedicine.HediffRowPriorityCare:LabelButton"));
                    PriorityCareCompGet = AccessTools.MethodDelegate<_PriorityCareCompGet>(AccessTools.Method("SmartMedicine.PriorityCareComp:Get"));
                    GetCare = AccessTools.MethodDelegate<_GetCare>(AccessTools.Method("SmartMedicine.GetPawnMedicalCareCategory:GetCare"));
                }
                catch
                {
                    Log.Error("Compact hediffs - SmartMedicine patch failed.");
                    active = false;
                }
			}
		}
    }
}
