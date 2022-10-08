using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using System.Reflection;
using UnityEngine;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(PlayerMovement), "Movement")]
    class PlayerMovementPatch
    {
        static bool Prefix(PlayerMovement __instance) {
            if (Main.editMode)
            {
                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
                float y = __instance.gameObject.transform.position.y;
                if (Main.movableObj == 0)
                {
                    __instance.gameObject.transform.position += Camera.main.transform.forward * z + Camera.main.transform.right * x;
                    __instance.rb.velocity = Vector3.zero;
                }
                else
                {
                    Vector3 foo = Camera.main.transform.forward * z + Camera.main.transform.right * x;
                    Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.position += new Vector3(foo.x, 0, foo.z);
                }
                return false;
            }
            else return true;
        }
    }
}
