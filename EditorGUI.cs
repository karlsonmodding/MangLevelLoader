using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

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
            dg_action = () => action(dg_input);
            dg_enabled = true;
        }

        public static bool dg_screenshot = false;

        private static Rect object_browser = new Rect(Screen.width - 305, 30, 300, 500);
        private static Vector2 object_browser_scroll = new Vector2(0, 0);
        private static Rect object_manip = new Rect(Screen.width - 305, 540, 300, 500);

        public static void _OnGUI()
        {
            if (!Main.editMode) return;
            if (dg_screenshot)
            {
                GUI.Box(new Rect(Screen.width / 2 - 200, 10, 400, 25), "Point your camera to set the Thumbnail and press ENTER");
                return;
            }
            if (dg_enabled)
            {
                GUI.Window(10, new Rect(Screen.width / 2 - 100, Screen.height / 2 - 35, 200, 70), (windowId) =>
                {
                    dg_input = GUI.TextField(new Rect(5, 20, 190, 20), dg_input);
                    if (GUI.Button(new Rect(5, 45, 95, 20), "OK"))
                    {
                        dg_enabled = false;
                        dg_action();
                    }
                    if (GUI.Button(new Rect(100, 45, 95, 20), "Cancel")) dg_enabled = false;
                }, dg_caption);
                return;
            }

            GUI.Box(new Rect(0, 0, Screen.width, 20), "");
            if (GUI.Button(new Rect(5, 0, 100, 20), "File")) dd_file = !dd_file;
            if (GUI.Button(new Rect(105, 0, 100, 20), "Level")) dd_level = !dd_level;
            //if (GUI.Button(new Rect(205, 0, 100, 20), "Object Browser")) objbrowser = !objbrowser;
            GUI.Box(new Rect(205, 0, 100, 20), "Object Browser");
            GUI.Label(new Rect(315, 0, 1000, 20), "<b>MangLevelLoader v" + Main.version + "</b>. Object count: " + Main.Level.Count + ".     Hold <b>right click</b> down to move and look around.");

            if (dd_file)
            {
                GUI.Box(new Rect(5, 20, 150, 80), "");
                if (GUI.Button(new Rect(5, 20, 150, 20), "Load Level"))
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
                if (GUI.Button(new Rect(5, 40, 150, 20), "Save Level"))
                {
                    dg_caption = "Enter level name:";
                    dg_input = "";
                    dg_action = () => Editor.NewSave(dg_input);
                    dg_enabled = true;
                }
                if (GUI.Button(new Rect(5, 60, 150, 20), "Upload to Workshop"))
                {
                    dg_screenshot = true;
                }
                if (GUI.Button(new Rect(5, 80, 150, 20), "Exit Editor")) Game.Instance.MainMenu();
            }

            if (dd_level)
            {
                GUI.Box(new Rect(105, 20, 150, 60), "");
                if (GUI.Button(new Rect(105, 20, 150, 20), "Spawn Object"))
                {
                    dg_caption = "Enter prefab:";
                    dg_input = "";
                    dg_action = () =>
                    {
                        Main.LevelObject obj = Editor.Spawn(dg_input);
                        Main.movableObj = Main.IdToIndex(obj.Id);
                    };
                    dg_enabled = true;
                }
                if (GUI.Button(new Rect(105, 40, 150, 20), "Select Object"))
                {
                    dg_caption = "Enter object id:";
                    dg_input = "";
                    dg_action = () => Main.movableObj = Main.IdToIndex(int.Parse(dg_input));
                    dg_enabled = true;
                }
                if (GUI.Button(new Rect(105, 60, 150, 20), "Set Spawn"))
                {
                    foreach (Main.LevelObject lo in Main.Level)
                        lo.Object.transform.position -= PlayerMovement.Instance.transform.position;
                    PlayerMovement.Instance.transform.position = Vector3.zero;
                }
            }

            object_browser = GUI.Window(11, object_browser, (windowId) => {
                GUI.DragWindow(new Rect(0, 0, 1000, 20));
                object_browser_scroll = GUI.BeginScrollView(new Rect(0, 20, 300, 480), object_browser_scroll, new Rect(0, 0, 280, Main.Level.Count * 25));
                int i = 0;
                foreach (var obj in Main.Level)
                {
                    GUI.BeginGroup(new Rect(5, 25 * i, 270, 20));
                    if (GUI.Button(new Rect(0, 0, 20, 20), "S"))
                    {
                        Main.movableObj = Main.IdToIndex(obj.Id);
                        MelonCoroutines.Start(identifyObject(obj.Object));
                    }
                    if (GUI.Button(new Rect(25, 0, 20, 20), "^"))
                        PlayerMovement.Instance.gameObject.transform.position = obj.Object.transform.position + Camera.main.transform.forward * -1f;
                    GUI.Label(new Rect(50, 0, 200, 20), obj.Id + " | " + obj.prefab);
                    GUI.EndGroup();
                    i++;
                }
                GUI.EndScrollView();
            }, "Level Object Browser");

            object_manip = GUI.Window(12, object_manip, (windowId) => {
                GUI.DragWindow(new Rect(0, 0, 1000, 20));
                if (Main.movableObj == -1)
                {
                    GUI.Label(new Rect(5, 20, 300, 20), "No object selected");
                    return;
                }
                Main.LevelObject obj = Main.Level[Main.movableObj];
                GUI.Label(new Rect(5, 20, 290, 20), "Selected object: " + obj.Id + ", prefab: " + obj.prefab);
                if (GUI.Button(new Rect(5, 40, 75, 20), "Duplicate"))
                {
                    Main.LevelObject copy = Editor.Spawn(obj.prefab.ToString());
                    copy.Object.transform.rotation = obj.Object.transform.rotation;
                    copy.Object.transform.localScale = obj.Object.transform.localScale;
                    Main.movableObj = Main.IdToIndex(copy.Id);
                    return;
                }
                if (GUI.Button(new Rect(85, 40, 75, 20), "Delete"))
                {
                    UnityEngine.Object.Destroy(Main.Level[Main.movableObj].Object);
                    Main.Level.RemoveAt(Main.movableObj);
                    Main.movableObj = 0;
                    return;
                }
                if (GUI.Button(new Rect(165, 40, 75, 20), "Identify")) MelonCoroutines.Start(identifyObject(obj.Object));
                if (GUI.Button(new Rect(245, 40, 50, 20), "Find")) PlayerMovement.Instance.gameObject.transform.position = obj.Object.transform.position + Camera.main.transform.forward * -1f;

                GUI.BeginGroup(new Rect(0, 80, 300, 80));
                GUI.Label(new Rect(5, 0, 290, 20), "[Pos] X: " + obj.Object.transform.position.x + " Y: " + obj.Object.transform.position.y + " Z: " + obj.Object.transform.position.z);
                GUI.Label(new Rect(5, 20, 20, 20), "X:");
                if (GUI.Button(new Rect(20, 20, 20, 20), "-")) obj.Object.transform.position += new Vector3(-Editor.step, 0, 0);
                if (GUI.RepeatButton(new Rect(40, 20, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    obj.Object.transform.position += new Vector3(x / 450, 0, 0);
                }
                if (GUI.Button(new Rect(265, 20, 20, 20), "+")) obj.Object.transform.position += new Vector3(Editor.step, 0, 0);
                GUI.Label(new Rect(5, 40, 20, 20), "Y:");
                if (GUI.Button(new Rect(20, 40, 20, 20), "-")) obj.Object.transform.position += new Vector3(0, -Editor.step, 0);
                if (GUI.RepeatButton(new Rect(40, 40, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    obj.Object.transform.position += new Vector3(0, x / 450, 0);
                }
                if (GUI.Button(new Rect(265, 40, 20, 20), "+")) obj.Object.transform.position += new Vector3(0, Editor.step, 0);
                GUI.Label(new Rect(5, 60, 20, 20), "Z:");
                if (GUI.Button(new Rect(20, 60, 20, 20), "-")) obj.Object.transform.position += new Vector3(0, 0, -Editor.step);
                if (GUI.RepeatButton(new Rect(40, 60, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    obj.Object.transform.position += new Vector3(0, 0, x / 450);
                }
                if (GUI.Button(new Rect(265, 60, 20, 20), "+")) obj.Object.transform.position += new Vector3(0, 0, Editor.step);
                GUI.EndGroup();

                GUI.BeginGroup(new Rect(0, 170, 300, 80));
                GUI.Label(new Rect(5, 0, 290, 20), "[Rot] X: " + obj.Object.transform.rotation.eulerAngles.x + " Y: " + obj.Object.transform.rotation.eulerAngles.y + " Z: " + obj.Object.transform.rotation.eulerAngles.z);
                GUI.Label(new Rect(5, 20, 20, 20), "X:");
                if (GUI.Button(new Rect(20, 20, 20, 20), "-")) ShiftRotation(obj.Object.transform, -Editor.step, 0f, 0f);
                if (GUI.RepeatButton(new Rect(40, 20, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    ShiftRotation(obj.Object.transform, x / 450, 0f, 0f);
                }
                if (GUI.Button(new Rect(265, 20, 20, 20), "+")) ShiftRotation(obj.Object.transform, Editor.step, 0f, 0f);
                GUI.Label(new Rect(5, 40, 20, 20), "Y:");
                if (GUI.Button(new Rect(20, 40, 20, 20), "-")) ShiftRotation(obj.Object.transform, 0f, -Editor.step, 0f);
                if (GUI.RepeatButton(new Rect(40, 40, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    ShiftRotation(obj.Object.transform, 0f, x / 450, 0f);
                }
                if (GUI.Button(new Rect(265, 40, 20, 20), "+")) ShiftRotation(obj.Object.transform, 0f, Editor.step, 0f);
                GUI.Label(new Rect(5, 60, 20, 20), "Z:");
                if (GUI.Button(new Rect(20, 60, 20, 20), "-")) ShiftRotation(obj.Object.transform, 0f, 0f, -Editor.step);
                if (GUI.RepeatButton(new Rect(40, 60, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    ShiftRotation(obj.Object.transform, 0f, 0f, x / 450);
                }
                if (GUI.Button(new Rect(265, 60, 20, 20), "+")) ShiftRotation(obj.Object.transform, 0f, 0f, Editor.step);
                GUI.EndGroup();

                GUI.BeginGroup(new Rect(0, 260, 300, 80));
                GUI.Label(new Rect(5, 0, 290, 20), "[Scale] X: " + obj.Object.transform.localScale.x + " Y: " + obj.Object.transform.localScale.y + " Z: " + obj.Object.transform.localScale.z);
                GUI.Label(new Rect(5, 20, 20, 20), "X:");
                if (GUI.Button(new Rect(20, 20, 20, 20), "-")) ShiftScale(obj.Object.transform, -Editor.step, 0f, 0f);
                if (GUI.RepeatButton(new Rect(40, 20, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    ShiftScale(obj.Object.transform, x / 450, 0f, 0f);
                }
                if (GUI.Button(new Rect(265, 20, 20, 20), "+")) ShiftScale(obj.Object.transform, Editor.step, 0f, 0f);
                GUI.Label(new Rect(5, 40, 20, 20), "Y:");
                if (GUI.Button(new Rect(20, 40, 20, 20), "-")) ShiftScale(obj.Object.transform, 0f, -Editor.step, 0f);
                if (GUI.RepeatButton(new Rect(40, 40, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    ShiftScale(obj.Object.transform, 0f, x / 450, 0f);
                }
                if (GUI.Button(new Rect(265, 40, 20, 20), "+")) ShiftScale(obj.Object.transform, 0f, Editor.step, 0f);
                GUI.Label(new Rect(5, 60, 20, 20), "Z:");
                if (GUI.Button(new Rect(20, 60, 20, 20), "-")) ShiftScale(obj.Object.transform, 0f, 0f, -Editor.step);
                if (GUI.RepeatButton(new Rect(40, 60, 225, 20), "<----------------|---------------->"))
                {
                    float x = Input.mousePosition.x - object_manip.x - 152;
                    ShiftScale(obj.Object.transform, 0f, 0f, x / 450);
                }
                if (GUI.Button(new Rect(265, 60, 20, 20), "+")) ShiftScale(obj.Object.transform, 0f, 0f, Editor.step);
                GUI.EndGroup();

                GUI.BeginGroup(new Rect(5, 350, 300, 80));
                GUI.Label(new Rect(0, 0, 300, 20), "Numerical values:");
                GUI.Label(new Rect(0, 20, 50, 20), "Pos:");
                {
                    float x = obj.Object.transform.position.x, y = obj.Object.transform.position.y, z = obj.Object.transform.position.z;
                    x = float.Parse(GUI.TextField(new Rect(50, 20, 70, 20), x.ToString()));
                    y = float.Parse(GUI.TextField(new Rect(125, 20, 70, 20), y.ToString()));
                    z = float.Parse(GUI.TextField(new Rect(200, 20, 70, 20), z.ToString()));
                    obj.Object.transform.position = new Vector3(x, y, z);
                }

                GUI.Label(new Rect(0, 40, 50, 20), "Rot:");
                {
                    float x = obj.Object.transform.rotation.eulerAngles.x, y = obj.Object.transform.rotation.eulerAngles.y, z = obj.Object.transform.rotation.eulerAngles.z;
                    x = float.Parse(GUI.TextField(new Rect(50, 40, 70, 20), x.ToString()));
                    y = float.Parse(GUI.TextField(new Rect(125, 40, 70, 20), y.ToString()));
                    z = float.Parse(GUI.TextField(new Rect(200, 40, 70, 20), z.ToString()));
                    obj.Object.transform.rotation = Quaternion.Euler(x, y, z);
                }

                GUI.Label(new Rect(0, 60, 50, 20), "Scale:");
                {
                    float x = obj.Object.transform.localScale.x, y = obj.Object.transform.localScale.y, z = obj.Object.transform.localScale.z;
                    x = float.Parse(GUI.TextField(new Rect(50, 60, 70, 20), x.ToString()));
                    y = float.Parse(GUI.TextField(new Rect(125, 60, 70, 20), y.ToString()));
                    z = float.Parse(GUI.TextField(new Rect(200, 60, 70, 20), z.ToString()));
                    obj.Object.transform.localScale = new Vector3(x, y, z);
                }
                GUI.EndGroup();

                GUI.Label(new Rect(5, 450, 100, 20), "Editor Step: ");
                Editor.step = float.Parse(GUI.TextField(new Rect(80, 450, 70, 20), Editor.step.ToString()));
                if (GUI.Button(new Rect(155, 450, 50, 20), "Reset")) Editor.step = 0.05f;
            }, "Object Properties");
        }

        private static void ShiftRotation(Transform tr, float dx, float dy, float dz)
        {
            tr.rotation = Quaternion.Euler(tr.rotation.eulerAngles.x + dx, tr.rotation.eulerAngles.y + dy, tr.rotation.eulerAngles.z + dz);
        }
        private static void ShiftScale(Transform tr, float dx, float dy, float dz)
        {
            tr.localScale = new Vector3(tr.localScale.x + dx, tr.localScale.y + dy, tr.localScale.z + dz);
        }
        private static IEnumerator identifyObject(GameObject go)
        {
            go.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            go.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            go.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            go.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            go.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            go.SetActive(true);
        }
    }
}
