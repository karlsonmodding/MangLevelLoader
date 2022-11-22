using Harmony;
using UnityEngine;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(PlayerMovement), "Movement")]
    class PlayerMovementPatch
    {
        static byte stepRateCounter;
        static bool Prefix(PlayerMovement __instance) {
            if (Main.editMode)
            {
                if (Input.GetKey(KeyCode.Alpha1))
                {
                    Main.MovementMode = Main.MoveModeEnum.movement;
                }
                else if (Input.GetKey(KeyCode.Alpha2))
                {
                    Main.MovementMode = Main.MoveModeEnum.scale;
                }
                else if (Input.GetKey(KeyCode.Alpha3))
                {
                    Main.MovementMode = Main.MoveModeEnum.rotation;
                }
                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
                float y = 0;
                if (Input.GetButton("Pickup")) y = 1f;
                if (Input.GetButton("Drop")) y = -1f;
                if (Main.movableObj == 0)
                {
                    __instance.gameObject.transform.position += Camera.main.transform.forward * z + Camera.main.transform.right * x;
                    __instance.rb.velocity = Vector3.zero;
                }
                stepRateCounter++;
                if (stepRateCounter < Editor.stepRate)
                {
                    return false;
                }
                else
                {
                    stepRateCounter = 0;
                }
                if (Main.MovementMode == Main.MoveModeEnum.movement)
                {
                    Vector3 movReltiveToCam = Camera.main.transform.forward * z + Camera.main.transform.right * x;
                    if (!Main.globalMov) Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.position += new Vector3(movReltiveToCam.x, y, movReltiveToCam.z) * Editor.step;
                    else Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.position += new Vector3(x, y, z) * Editor.step;
                }
                else if (Main.MovementMode == Main.MoveModeEnum.scale)
                {
                    Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.localScale += new Vector3(x, y, z) * Editor.step;
                }
                else // rotation ofc
                {
                    Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.eulerAngles += new Vector3(x, y, z) * Editor.step;
                }
                return false;
            }
            else return true;
        }

    }
}

