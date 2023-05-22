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
    public static class Pawnmorpher
    {
        public static bool active = false;

		//public static Type hediff_AddedMutation;
		//public delegate bool _IsCoreMutation(Hediff hediff);
		//public static _IsCoreMutation IsCoreMutation;

		public static HashSet<HediffDef> pawnmorpherHediffDefs;
		public static HashSet<HediffDef> coreMutationHediffDefs;

		public delegate string _GetTooltip(IEnumerable<Hediff> hediffs);
		public static _GetTooltip GetTooltip;

		static Pawnmorpher()
		{
			active = ModLister.GetActiveModWithIdentifier("tachyonite.pawnmorpher") != null | ModLister.GetActiveModWithIdentifier("tachyonite.pawnmorpherpublic") != null;
			if (active)
            {
                //hediff_AddedMutation = AccessTools.TypeByName("Pawnmorph.Hediff_AddedMutation");

                CachePawnmorpherHediffs();
                //Log.Message("The following hediffDefs have been marked as extra replacement parts: "+String.Join(", ", coreMutationHediffDefs));

                var hasTooltipMethod = AccessTools.GetMethodNames(AccessTools.TypeByName("Pawnmorph.PatchHealthCardUtilityDrawHediffRow")).Any(n => n == "Tooltip");
                if (hasTooltipMethod)
                    GetTooltip = AccessTools.MethodDelegate<_GetTooltip>("Pawnmorph.PatchHealthCardUtilityDrawHediffRow:Tooltip");
            }
        }

        private static void CachePawnmorpherHediffs()
        {
            var removeCompType = AccessTools.TypeByName("Pawnmorph.Hediffs.RemoveFromPartComp");
            var removeCompPropsType = AccessTools.TypeByName("Pawnmorph.Hediffs.RemoveFromPartCompProperties");
            var layerField = AccessTools.Field(removeCompPropsType, "layer");

            var hediffDefs = LoadedModManager.GetMod(AccessTools.TypeByName("Pawnmorph.PawnmorpherMod"));

            pawnmorpherHediffDefs = new HashSet<HediffDef>();
            coreMutationHediffDefs = new HashSet<HediffDef>();
            foreach (var hediffDef in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                if (hediffDef.GetType().Namespace.Contains("Pawnmorph"))
                {
                    pawnmorpherHediffDefs.Add(hediffDef);

                    if (hediffDef.HasComp(removeCompType))
                    {
                        var mutationLayerUntyped = layerField.GetValue(hediffDef.CompPropsFor(removeCompType));
                        var mutationLayerRaw = (int)(Convert.ChangeType(mutationLayerUntyped, Enum.GetUnderlyingType(mutationLayerUntyped.GetType())));
                        if (mutationLayerRaw == 1) // Core = 1
                        {
                            coreMutationHediffDefs.Add(hediffDef);
                        }
                    }
                }
            }
        }
    }
}
