using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(Milk), "OnTriggerEnter")]
    class Milk_OnTriggerEnter
    {
        static bool Prefix() {
            return !Main.editMode;
        }

        static void Postfix()
        {
            if (Main.editMode) return;
            if (Main.currentLevelName != "") UIManger.Instance.winUI.transform.Find("NextBtn").gameObject.SetActive(false);
            else UIManger.Instance.winUI.transform.Find("NextBtn").gameObject.SetActive(true);
        }
    }
}
