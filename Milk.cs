using HarmonyLib;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(Milk), "OnTriggerEnter")]
    class Milk_OnTriggerEnter
    {
        static bool Prefix() {
            return !Main.editMode;
        }
    }
}
