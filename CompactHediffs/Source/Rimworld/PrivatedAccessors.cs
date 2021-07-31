using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PeteTimesSix.CompactHediffs.Rimworld
{
	public static class PrivatedAccessors
	{
		static PrivatedAccessors()
		{
		}

		public static float GetTotalTendQuality(this HediffComp_TendDuration instance)
		{
			return Traverse.Create(instance).Field<float>("totalTendQuality").Value;
		}

	}
}
