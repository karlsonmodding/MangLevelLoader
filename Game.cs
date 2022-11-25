using MelonLoader;
using HarmonyLib;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(Game), "RestartGame")]
    class Game_RestartGame
    {
        static void Prefix() {
            if (Main.currentLevelName == "") return;
            MelonCoroutines.Start(Editor.NewLoad(new byte[0], ""));
        }
    }

    [HarmonyPatch(typeof(Game), "MainMenu")]
    class Game_MainMenu
    {
        static void Postfix() {
            Main.currentLevelName = "";
            Main.currentLevel = new byte[0];
        }
    }
}
