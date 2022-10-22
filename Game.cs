using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using MelonLoader;
namespace KarlsonLevels
{
    [HarmonyPatch(typeof(Game), "RestartGame")]
    class RestartGame
    {
        static void Prefix() {
            MelonCoroutines.Start(Editor.NewLoad(null));
        }
    }
}
