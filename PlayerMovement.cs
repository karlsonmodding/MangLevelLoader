using UnityEngine;
using HarmonyLib;
using JetBrains.Annotations;
using KarlsonLevels.Workshop_API;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(PlayerMovement), "FixedUpdate")]
    class PlayerMovement_FixedUpdate
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
                    float scale = 1f;
                    if (Input.GetKey(KeyCode.LeftShift)) scale = 2f;
                    __instance.gameObject.transform.position += (Camera.main.transform.forward * z + Camera.main.transform.right * x) * scale;
                    __instance.rb.velocity = Vector3.zero;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if(UIManger.Instance.gameUI.activeSelf) { UIManger.Instance.gameUI.SetActive(false); }
                GameObject ps = GameObject.Find("Camera/Main Camera/Particle System"); // you little fucker
                if(ps.activeSelf) ps.SetActive(false);
                if (EditorGUI.dg_screenshot && Input.GetKeyDown(KeyCode.Return))
                {
                    EditorGUI.dg_screenshot = false;
                    EditorGUI.Dialog("Enter level name: ", (name) =>
                    {
                        byte[] thumbnail = Editor.MakeScreenshot();
                        Core.UploadLevel(new WML_Convert.WML(name, thumbnail, Editor.SaveLevelBytes()));
                    }, "Untitled Level");
                }
                return false;
            }
            return true;
        }

    }

    [HarmonyPatch(typeof(PlayerMovement), "Pause")]
    public class PlayerMovement_Pause
    {
        public static bool Prefix() => !Main.editMode;
    }

    [HarmonyPatch(typeof(PlayerMovement), "Update")]
    public class PlayerMovement_Update
    {
        public static bool Prefix() => Main.editMode && Input.GetButton("Fire2") || !Main.editMode || Main.editMode && EditorGUI.dg_screenshot;
    }
}

