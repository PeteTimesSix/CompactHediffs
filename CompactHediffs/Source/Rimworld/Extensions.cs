using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.CompactHediffs.Rimworld
{
    public static class Extensions
	{

		public static Rect ContractedBy(this Rect rect, float marginX, float marginY) 
        {
            return new Rect(rect.x + marginX, rect.y + marginY, rect.width - marginX * 2, rect.height - marginY * 2);
        }

		public static string ApplyTag(this string s, string tag)
		{
			return string.Format("<{0}>{1}</{0}>", tag, s);
		}

		public static string InvalidateTags(this string str)
        {
            str = str.Replace("<", "&lt");
            str = str.Replace(">", "&gt");
            return str;
        }

		private static int FirstLetterBetweenTagsNestingAware(this string str)
		{
			int num = 0;
			while (num < str.Length - 1 && str[num] == '<' && str[num + 1] != '/')
			{
				num += str.Substring(num).IndexOf('>') + 1;
			}
			return num;
		}

		public static string CapitalizeFirstNestingAware(this string str)
		{
			if (str.NullOrEmpty())
			{
				return str;
			}
			if (char.IsUpper(str[0]))
			{
				return str;
			}
			if (str.Length == 1)
			{
				return str.ToUpper();
			}
			int num = str.FirstLetterBetweenTagsNestingAware();
			if (num == 0)
			{
				return char.ToUpper(str[num]).ToString() + str.Substring(num + 1);
			}
			if (num < str.Length)
			{
				return str.Substring(0, num) + char.ToUpper(str[num]).ToString() + str.Substring(num + 1);
			}
			return str;
		}
	}
}
