using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(Lobby), "LoadMap")]
    class LobbyPatch
    {
        static void Prefix() {
            Main.currentLevel = null;
        }
    }
}
