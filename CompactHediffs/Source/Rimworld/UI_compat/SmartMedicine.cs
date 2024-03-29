﻿using PeteTimesSix.CompactHediffs.ModCompat;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.CompactHediffs.Rimworld.UI_compat
{
    public static class UI_SmartMedicine
	{

		public static void DrawSmartMedicineIcon(Rect iconRect, MedicalCareCategory defaultCare, List<Hediff> hediffs)
		{
			MedicalCareCategory maxCare;
			Hediff maxCareHediff = HighestCarePriority(hediffs, out maxCare);

			if (maxCareHediff == null)
			{
				return;
			}
			Texture2D tex = SmartMedicine.careTextures[(int)maxCare];
			var iconWidth = CustomHealthCardUtility.SmartMedicineIconWidth;
			var iconHeight = CustomHealthCardUtility.SmartMedicineIconHeight;
			Rect rect = new Rect(2 * iconRect.x + iconRect.width - iconRect.x - iconWidth, iconRect.y, iconWidth, iconHeight);
			GUI.DrawTexture(rect, tex);
		}


		public static void AddSmartMedicineFloatMenuButton(Rect buttonRect, IEnumerable<Hediff> hediffs, MedicalCareCategory defaultCare)
		{
			if (Event.current.button == 1 && Widgets.ButtonInvisible(buttonRect) && hediffs.Any(h => h.TendableNow(true)))
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				var hediffCares = SmartMedicine.PriorityCareCompGet();
				//Default care
				list.Add(new FloatMenuOption("TD.DefaultCare".Translate(), delegate
				{
					foreach(Hediff hediff in hediffs)
					{
						hediffCares.Remove(hediff);
					}
				}, SmartMedicine.careTextures[(int)defaultCare], Color.white));

				for (int i = 0; i < 5; i++)
				{
					MedicalCareCategory mc = (MedicalCareCategory)i;
					list.Add(new FloatMenuOption(mc.GetLabel(), () =>
					{
						foreach (Hediff hediff in hediffs)
						{
							hediffCares[hediff] = mc;
						}
					}, SmartMedicine.careTextures[(int)mc], Color.white));
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}

		public static Hediff HighestCarePriority(List<Hediff> hediffs, out MedicalCareCategory care) //heck if I know how to get an out parameter through traverse
		{
			care = MedicalCareCategory.NoCare;
			Hediff maxCareHediff = null;
			var hediffCares =  SmartMedicine.PriorityCareCompGet();
			foreach (Hediff h in hediffs)
			{
				if (h.TendableNow(true) && hediffCares.TryGetValue(h, out MedicalCareCategory heCare))
				{
					care = heCare > care ? heCare : care;
					maxCareHediff = h;
				}
			}
			return maxCareHediff;
		}
	}
}

