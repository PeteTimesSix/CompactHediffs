using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PeteTimesSix.CompactHediffs.Rimworld.UI
{
    public static class CustomInfoCardButtonWidget
    {
		public static bool CustomInfoCardButton(Rect rect, Hediff hediff)
		{
			if (CustomInfoCardButtonWorker(rect))
			{
				Find.WindowStack.Add(new Dialog_InfoCard(hediff.def));
				return true;
			}
			return false;
		}

		private static bool CustomInfoCardButtonWorker(Rect rect)
		{
			MouseoverSounds.DoRegion(rect);
			TooltipHandler.TipRegionByKey(rect, "DefInfoTip");
			bool result = Widgets.ButtonImage(rect, Textures.InfoButtonNarrow, GUI.color, true);
			UIHighlighter.HighlightOpportunity(rect, "InfoCard");
			return result;
		}
	}
}
