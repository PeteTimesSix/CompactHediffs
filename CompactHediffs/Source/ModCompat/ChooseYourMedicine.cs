using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.CompactHediffs.ModCompat
{
    [StaticConstructorOnStartup]
    public static class ChooseYourMedicine
    {
        public static bool active = false;

        public static readonly int IconWidth = 16;
        public static readonly int IconHeight = 16;

        public delegate float _DrawButtonToAssignMedManually(Rect rect, Pawn pawn, IGrouping<HediffDef, Hediff> grouping);
        public static _DrawButtonToAssignMedManually DrawButtonToAssignMedManually;

        static ChooseYourMedicine()
        {
            active = ModLister.GetActiveModWithIdentifier("Kopp.ChooseYourMedicine") != null;
            if (active)
            {
                var hasDrawMethod = AccessTools.GetMethodNames(AccessTools.TypeByName("ChooseYourMedicine.CompatibilityCompactHediffs")).Any(n => n == "DrawButtonToAssignMedManually");
                if(hasDrawMethod)
                {
                    DrawButtonToAssignMedManually = AccessTools.MethodDelegate<_DrawButtonToAssignMedManually>("ChooseYourMedicine.CompatibilityCompactHediffs:DrawButtonToAssignMedManually");
                }
                else 
                {
                    //patch not live yet so this is to be expected
                    //Log.Warning("Compact hediffs - Choose your Medicine interop method not found.");
                    active = false;
                }
            }
        }
    }
}
