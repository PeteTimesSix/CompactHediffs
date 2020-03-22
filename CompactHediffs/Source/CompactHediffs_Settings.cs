using PeteTimesSix.CompactHediffs.HarmonyPatches;
using Verse;

namespace PeteTimesSix.CompactHediffs
{
    public class CompactHediffs_Settings : ModSettings
    {

        public bool _enabled = true;
        public bool Enabled { get { return _enabled; }
            set 
            {
                if(value)
                    ITab_Pawn_Health_Patches.setNewExtraWidth(extraTabWidth);
                else
                    ITab_Pawn_Health_Patches.setNewExtraWidth(0f);
                _enabled = value;
            }
        }

        public bool replacingPartInBodypart = true;
        public bool italicizeMissing = true;
        public bool evenOddHighlights = false;
        public bool verticalSeparator = true;
        public bool horizontalSeparator = true;
        public bool separatorNightMode = true;
        public bool tendPrioritySort = true;
        public bool bodypartHealthbars = true;
        public bool bleedingIcons = true;

        public float extraTabWidth = 100f;

        

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref _enabled, "isEnabled", true);

            Scribe_Values.Look<bool>(ref replacingPartInBodypart, "replacingPartInBodypart", true);
            Scribe_Values.Look<bool>(ref italicizeMissing, "italicizeMissing", true);
            Scribe_Values.Look<bool>(ref evenOddHighlights, "evenOddHighlights", false);
            Scribe_Values.Look<bool>(ref verticalSeparator, "verticalSeparator", true);
            Scribe_Values.Look<bool>(ref horizontalSeparator, "horizontalSeparator", true);
            Scribe_Values.Look<bool>(ref separatorNightMode, "separatorNightMode", true);
            Scribe_Values.Look<bool>(ref tendPrioritySort, "tendPrioritySort", true);
            Scribe_Values.Look<bool>(ref bodypartHealthbars, "bodypartHealthbars", true);
            Scribe_Values.Look<bool>(ref bleedingIcons, "bleedingIcons", true);

            Scribe_Values.Look<float>(ref extraTabWidth, "extraTabWidth", 100f);
        }
    }
}