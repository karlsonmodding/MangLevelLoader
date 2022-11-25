using UnityEngine;
using HarmonyLib;
using JetBrains.Annotations;
using KarlsonLevels.Workshop_API;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(PlayerMovement), "Movement")]
    class PlayerMovement_Movement
    {
        static bool Prefix(PlayerMovement __instance) {
            if (Main.editMode)
            {
                if (Input.GetButton("Fire2") || EditorGUI.dg_screenshot)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    float x = Input.GetAxisRaw("Horizontal");
                    float z = Input.GetAxisRaw("Vertical");
                    __instance.gameObject.transform.position += Camera.main.transform.forward * z + Camera.main.transform.right * x;
                    __instance.rb.velocity = Vector3.zero;

                    if(EditorGUI.dg_screenshot && Input.GetKeyDown(KeyCode.Return))
                    {
                        EditorGUI.dg_screenshot = false;
                        EditorGUI.Dialog("Enter level name: ", (name) =>
                        {
                            byte[] thumbnail = Editor.MakeScreenshot();
                            Core.UploadLevel(new WML_Convert.WML(name, thumbnail, Editor.SaveLevelBytes()));
                        }, "Untitled Level");
                    }
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if(UIManger.Instance.gameUI.activeSelf) { UIManger.Instance.gameUI.SetActive(false); }
                return false;
            }
            else return true;
        }

    }

    [HarmonyPatch(typeof(PlayerMovement), "Pause")]
    public class PlayerMovement_Pause
    {
        public static bool Prefix() => !Main.editMode;
    }

    [HarmonyPatch(typeof(PlayerMovement), "Look")]
    public class PlayerMovement_Look
    {
        public static bool Prefix() => Main.editMode && Input.GetButton("Fire2") || !Main.editMode;
    }
}

