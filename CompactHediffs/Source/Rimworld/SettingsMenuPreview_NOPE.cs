using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeteTimesSix.CompactHediffs.Rimworld
{
    class SettingsMenuPreview_NOPE
    {



		/*
        var hediffBoxSize = new Vector2(630f, 430f);
        var tabRect = new Rect(0f, 20f, hediffBoxSize.x, hediffBoxSize.y - 20f);
        Rect rect = new Rect(tabRect.x, tabRect.y, tabRect.width * 0.375f, tabRect.height).Rounded();
        Rect rect2 = new Rect(rect.xMax, tabRect.y, tabRect.width - rect.width, tabRect.height);
        Rect heddifRect = rect2.ContractedBy(10f);

        Rect hediffRectActual = new Rect(inRect.RightHalf().x + ((inRect.RightHalf().width - heddifRect.width) / 2f), 80f, heddifRect.width, heddifRect.height);
        Widgets.DrawMenuSection(hediffRectActual);

            var fakeDiffs = genFakeHediffs();

        CustomHealthCardUtility.DrawFakeHediffListing(hediffRectActual, fakeDiffs, true);

        public IEnumerable<Hediff> genFakeHediffs()
        {
            var fakeDiffs = new List<Hediff>();

            Hediff hediff = (Hediff)Activator.CreateInstance(typeof(Hediff));
            hediff.def = HediffDefOf.AlcoholHigh;
            hediff.pawn = null;
            BodyPartRecord record = null;
            hediff.Part = record;
            fakeDiffs.Add(hediff);

            hediff = (Hediff)Activator.CreateInstance(typeof(Hediff_Injury));
            hediff.def = HediffDefOf.Cut;
            hediff.pawn = null;
            Traverse.Create(hediff).Field("severityInt").SetValue(2);
            record = new BodyPartRecord();
            record.customLabel = "Left arm";
            record.def = BodyPartDefOf.Arm;
            hediff.Part = record;
            fakeDiffs.Add(hediff);

            hediff = (Hediff)Activator.CreateInstance(typeof(Hediff_Injury));
            hediff.def = HediffDefOf.Cut;
            hediff.pawn = null;
            Traverse.Create(hediff).Field("severityInt").SetValue(30);
            record = new BodyPartRecord();
            record.customLabel = "Left arm";
            record.def = BodyPartDefOf.Arm;
            hediff.Part = record;
            fakeDiffs.Add(hediff);


            hediff = (Hediff)Activator.CreateInstance(typeof(Hediff_AddedPart));
            hediff.def = HediffDefOf.BionicLeg;
            hediff.pawn = null;
            record = new BodyPartRecord();
            record.customLabel = "Right leg";
            record.def = BodyPartDefOf.Leg;
            hediff.Part = record;
            fakeDiffs.Add(hediff);

            return fakeDiffs;
        }
       */

		/*private static IEnumerable<IGrouping<BodyPartRecord, Hediff>> FakeVisibleHediffGroupsInOrder(IEnumerable<Hediff> diffs, bool showBloodLoss)
		{
			Func<BodyPartRecord, float> getListPriority = (BodyPartRecord rec) => method_getListPriority.GetValue<float>(rec);

			foreach (IGrouping<BodyPartRecord, Hediff> grouping in from x in diffs
																   group x by x.Part into x
																   orderby getListPriority(x.First<Hediff>().Part) descending
																   select x)
			{
				yield return grouping;
			}
			yield break;
		}

		public static void DrawFakeHediffListing(Rect rect, IEnumerable<Hediff> fakeDiffs, bool showBloodLoss)
		{
			method_CanEntryBeClicked = Traverse.Create(typeof(HealthCardUtility)).Method("CanEntryBeClicked", new Type[] { typeof(IEnumerable<Hediff>), typeof(Pawn) });

			GUI.color = Color.white;
			GUI.BeginGroup(rect);
			float lineHeight = Text.LineHeight;
			Rect outRect = new Rect(0f, 0f, rect.width, rect.height - lineHeight);
			Rect viewRect = new Rect(0f, 0f, rect.width - 16f, field_scrollViewHeight.Value);
			Rect rect2 = rect;
			if (viewRect.height > outRect.height)
			{
				rect2.width -= 16f;
			}
			var scrollPosition = field_scrollPosition.Value;
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
			GUI.color = Color.white;
			float b = 0f;
			field_highlight.Value = true;
			bool flag = false;
			if (Event.current.type == EventType.Layout)
			{
				field_lastMaxIconsTotalWidth.Value = 0f;
			}
			foreach (IGrouping<BodyPartRecord, Hediff> diffs in FakeVisibleHediffGroupsInOrder(fakeDiffs, showBloodLoss))
			{
				flag = true;
				CustomHealthCardUtility.DrawHediffRow(rect2, null, diffs, ref b);
			}
			if (!flag)
			{
				Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, outRect.height), "(" + "NoHealthConditions".Translate() + ")");
				b = outRect.height - 1f;
			}
			if (Event.current.type == EventType.Repaint)
			{
				field_scrollViewHeight.Value = b;
			}
			else if (Event.current.type == EventType.Layout)
			{
				field_scrollViewHeight.Value = Mathf.Max(field_scrollViewHeight.Value, b);
			}
			Widgets.EndScrollView();
			float bleedRateTotal = 0f;

			foreach(Hediff hediff in fakeDiffs) 
			{
				if(hediff.Bleeding)
					bleedRateTotal += hediff.BleedRate;
			}

			if (bleedRateTotal > 0.01f)
			{
				Rect rect3 = new Rect(0f, rect.height - lineHeight, rect.width, lineHeight);
				string text = "BleedingRate".Translate() + ": " + bleedRateTotal.ToStringPercent() + "/" + "LetterDay".Translate();
				int num = 30000;
				if (num < 60000)
				{
					text += " (" + "TimeToDeath".Translate(num.ToStringTicksToPeriod(true, false, true, true)) + ")";
				}
				else
				{
					text += " (" + "WontBleedOutSoon".Translate() + ")";
				}
				Widgets.Label(rect3, text);
			}
			GUI.EndGroup();
			GUI.color = Color.white;
		}*/
	}
}
