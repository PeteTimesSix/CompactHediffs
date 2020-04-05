using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.CompactHediffs.Rimworld
{
    [StaticConstructorOnStartup]
    public static class Textures
    {
        public static readonly Texture2D translucentWhite = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.5f));
        public static readonly Texture2D mostlyTransparentWhite = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.05f));

        public static readonly Texture2D CategorizedResourceReadout = ContentFinder<Texture2D>.Get("UI/Buttons/ResourceReadoutCategorized", true);

        public static readonly Texture2D TestTexture = ContentFinder<Texture2D>.Get("testTexture", true);
         
        public static readonly Texture2D InfoButtonNarrow = ContentFinder<Texture2D>.Get("infoNarrow", true);

        public static readonly Texture2D Vanilla_BleedingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Medical/Bleeding", true);
        public static readonly Texture2D Vanilla_TendedIcon_Need_General = ContentFinder<Texture2D>.Get("UI/Icons/Medical/TendedNeed", true);
        public static readonly Texture2D Vanilla_TendedIcon_Well_General = ContentFinder<Texture2D>.Get("UI/Icons/Medical/TendedWell", true);
        public static readonly Texture2D Vanilla_TendedIcon_Well_Injury = ContentFinder<Texture2D>.Get("UI/Icons/Medical/BandageWell", true);


        public static readonly Texture2D BleedingIcon_0 = ContentFinder<Texture2D>.Get("bleeding_0", true);
        public static readonly Texture2D BleedingIcon_1 = ContentFinder<Texture2D>.Get("bleeding_1", true);
        public static readonly Texture2D BleedingIcon_2 = ContentFinder<Texture2D>.Get("bleeding_2", true);
        public static readonly Texture2D BleedingIcon_3 = ContentFinder<Texture2D>.Get("bleeding_3", true);
        public static readonly Texture2D BleedingIcon_4 = ContentFinder<Texture2D>.Get("bleeding_4", true);
        public static readonly Texture2D BleedingIcon_5 = ContentFinder<Texture2D>.Get("bleeding_5", true);

        public static readonly Texture2D TendingIcon_0 = ContentFinder<Texture2D>.Get("tending_0", true);
        public static readonly Texture2D TendingIcon_1 = ContentFinder<Texture2D>.Get("tending_1", true);
        public static readonly Texture2D TendingIcon_2 = ContentFinder<Texture2D>.Get("tending_2", true);
        public static readonly Texture2D TendingIcon_3 = ContentFinder<Texture2D>.Get("tending_3", true);
        public static readonly Texture2D TendingIcon_4 = ContentFinder<Texture2D>.Get("tending_4", true);
        public static readonly Texture2D TendingIcon_5 = ContentFinder<Texture2D>.Get("tending_5", true);
    }
}
