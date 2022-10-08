using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;


namespace KarlsonLevels
{
    [HarmonyPatch(typeof(Milk), "OnTriggerEnter")]
    class MilkPatch
    {
        static bool Prefix() {
            return !Main.editMode;
        }
    }
}
