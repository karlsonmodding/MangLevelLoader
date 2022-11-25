using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonLevels
{
    public static class EditorGUI
    {
        private static bool dd_file = false;
        private static bool dd_level = false;
        private static bool objbrowser = false;

        private static bool dg_enabled = false;
        private static string dg_caption = "";
        private static string dg_input = "";
        private static Action dg_action = null;
        public static void Dialog(string caption, Action<string> action, string input = "")
        {
            dg_caption = caption;
            dg_input = input;
            dg_action = ()=> action(dg_input);
            dg_enabled = true;
        }

        public static bool dg_screenshot = false;

        public static void _OnGUI()
        {
            if (!Main.editMode) return;
            if(dg_screenshot)
            {
                GUI.Box(new Rect(Screen.width / 2 - 150, 10, 300, 20), "Point your camera to set the Thumbnail and press ENTER");
                return;
            }
            if(dg_enabled)
            {
                GUI.Window(10, new Rect(Screen.width / 2 - 100, Screen.height / 2 - 35, 200, 70), (windowId) =>
                {
                    dg_input = GUI.TextField(new Rect(5, 20, 190, 20), dg_input);
                    if (GUI.Button(new Rect(5, 45, 95, 20), "OK"))
                    {
                        dg_enabled = false;
                        dg_action();
                    }
                    if(GUI.Button(new Rect(100, 45, 95, 20), "Cancel")) dg_enabled = false;
                }, dg_caption);
                return;
            }

            GUI.Box(new Rect(0, 0, Screen.width, 20), "");
            if (GUI.Button(new Rect(5, 0, 100, 20), "File")) dd_file = !dd_file;
            if (GUI.Button(new Rect(105, 0, 100, 20), "Level")) dd_level = !dd_level;
            if (GUI.Button(new Rect(205, 0, 100, 20), "Object Browser")) objbrowser = !objbrowser;

            if(dd_file)
            {
                GUI.Box(new Rect(5, 20, 150, 80), "");
                if(GUI.Button(new Rect(5, 20, 150, 20), "Load Level"))
                {
                    dg_caption = "Enter level name:";
                    dg_input = "";
                    dg_action = () =>
                    {
                        MelonCoroutines.Start(Editor.NewLoad(Path.Combine(Directory.GetCurrentDirectory(), "Levels", dg_input + ".mll")));
                        MelonCoroutines.Start(Editor.StartEdit(dg_input));
                    };
                    dg_enabled = true;
                }
                if(GUI.Button(new Rect(5, 40, 150, 20), "Save Level"))
                {
                    dg_caption = "Enter level name:";
                    dg_input = "";
                    dg_action = () => Editor.NewSave(dg_input);
                    dg_enabled = true;
                }
                if(GUI.Button(new Rect(5, 60, 150, 20), "Upload to Workshop"))
                {
                    dg_screenshot = true;
                }
                if (GUI.Button(new Rect(5, 80, 150, 20), "Exit Editor")) Game.Instance.MainMenu();
            }

            if(dd_level)
            {
                GUI.Box(new Rect(105, 20, 150, 60), "");
                if(GUI.Button(new Rect(105, 20, 150, 20), "Spawn Object"))
                {
                    dg_caption = "Enter prefab:";
                    dg_input = "";
                    dg_action = () => Editor.Spawn(dg_input);
                    dg_enabled = true;
                }
                if(GUI.Button(new Rect(105, 40, 150, 20), "Select Object"))
                {
                    dg_caption = "Enter object id:";
                    dg_input = "";
                    dg_action = () => Main.movableObj = int.Parse(dg_input);
                    dg_enabled = true;
                }
                if(GUI.Button(new Rect(105, 60, 150, 20), "Set Spawn"))
                {
                    foreach (Main.LevelObject lo in Main.Level)
                        lo.Object.transform.position -= PlayerMovement.Instance.transform.position;
                    PlayerMovement.Instance.transform.position = Vector3.zero;
                }
            }
        }
    }
}
