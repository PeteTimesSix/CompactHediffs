using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PeteTimesSix.CompactHediffs.ModCompat
{
	[StaticConstructorOnStartup]
    public static class EBF
    {
        public static bool active = false;

		public delegate float _GetMaxHealth_Cached(BodyPartDef def, Pawn pawn, BodyPartRecord record);
		public static _GetMaxHealth_Cached GetMaxHealth_Cached;

        static EBF()
		{
			active = ModLister.GetActiveModWithIdentifier("V1024.EBFramework") != null;
			if (active)
			{
				//for the record, Vectorial1024, this is really rather rude.
				try
				{
					GetMaxHealth_Cached = AccessTools.MethodDelegate<_GetMaxHealth_Cached>("EBF.VanillaExtender:GetMaxHealth_Cached");
				}
				catch 
				{
					Log.Error("Compact hediffs - EBF patch failed.");
					active = false;
				}
			}
		}
    }
}
