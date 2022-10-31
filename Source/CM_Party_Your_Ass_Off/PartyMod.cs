using HarmonyLib;
using RimWorld;
using Verse;

namespace KB_Party_Your_Ass_Off
{
    public class PartyMod : Mod
    {
        private static PartyMod _instance;
        public static PartyMod Instance => _instance;

        public PartyMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("CM_Party_Your_Ass_Off");
            harmony.PatchAll();

            _instance = this;
        }
    }
}
