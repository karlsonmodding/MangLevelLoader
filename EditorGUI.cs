using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace KarlsonLevels
{
    public static class EditorGUI
    {
        private static bool dd_file = true;
        private static bool dd_level = false;

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

        private static Rect level_object_browser = new Rect(Screen.width - 305, 30, 300, 500);
        private static Vector2 object_browser_scroll = new Vector2(0, 0);
        private static Rect object_manip = new Rect(Screen.width - 305, 540, 300, 500);
        private static Rect object_browser = new Rect(Screen.width / 2 - 580, Screen.height / 2 - 350, 1160, 705);
        private static bool object_browser_enabled = false;
        private static int object_browser_page = 0;
        private static bool object_browser_cheat_sheet = false;
        private static Vector2 object_browser_cheat_sheet_scroll = new Vector2(0, 0);
        private static Texture2D[] object_browser_textures = null;
        private static float[] object_browser_zooms = new float[50];
        private static Rect object_browser_cheat_sheet_window = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 250, 400, 500);

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
            if (GUI.Button(new Rect(205, 0, 100, 20), "Object Browser"))
            {
                object_browser_enabled = !object_browser_enabled;
                if(object_browser_enabled) object_browser_page = 0;
            }
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
                if (GUI.Button(new Rect(5, 80, 150, 20), "Exit Editor"))
                {
                    dd_file = true;
                    dd_level = false;
                    object_browser_enabled = false;
                    Game.Instance.MainMenu();
                }
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

            level_object_browser = GUI.Window(11, level_object_browser, (windowId) => {
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
                    if (GUI.Button(new Rect(25, 0, 20, 20), "^")) { PlayerMovement.Instance.gameObject.transform.position = obj.Object.transform.position + Camera.main.transform.forward * -5f; MelonCoroutines.Start(identifyObject(obj.Object)); }
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
                    Main.movableObj = -1;
                    return;
                }
                if (GUI.Button(new Rect(165, 40, 75, 20), "Identify")) MelonCoroutines.Start(identifyObject(obj.Object));
                if (GUI.Button(new Rect(245, 40, 50, 20), "Find")) { PlayerMovement.Instance.gameObject.transform.position = obj.Object.transform.position + Camera.main.transform.forward * -5f; MelonCoroutines.Start(identifyObject(obj.Object)); }

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
                    x = float.Parse(GUI.TextField(new Rect(50, 20, 70, 20), floatstr(x)));
                    y = float.Parse(GUI.TextField(new Rect(125, 20, 70, 20), floatstr(y)));
                    z = float.Parse(GUI.TextField(new Rect(200, 20, 70, 20), floatstr(z)));
                    obj.Object.transform.position = new Vector3(x, y, z);
                }

                GUI.Label(new Rect(0, 40, 50, 20), "Rot:");
                {
                    float x = obj.Object.transform.rotation.eulerAngles.x, y = obj.Object.transform.rotation.eulerAngles.y, z = obj.Object.transform.rotation.eulerAngles.z;
                    x = float.Parse(GUI.TextField(new Rect(50, 40, 70, 20), floatstr(x)));
                    y = float.Parse(GUI.TextField(new Rect(125, 40, 70, 20), floatstr(y)));
                    z = float.Parse(GUI.TextField(new Rect(200, 40, 70, 20), floatstr(z)));
                    obj.Object.transform.rotation = Quaternion.Euler(x, y, z);
                }

                GUI.Label(new Rect(0, 60, 50, 20), "Scale:");
                {
                    float x = obj.Object.transform.localScale.x, y = obj.Object.transform.localScale.y, z = obj.Object.transform.localScale.z;
                    x = float.Parse(GUI.TextField(new Rect(50, 60, 70, 20), floatstr(x)));
                    y = float.Parse(GUI.TextField(new Rect(125, 60, 70, 20), floatstr(y)));
                    z = float.Parse(GUI.TextField(new Rect(200, 60, 70, 20), floatstr(z)));
                    obj.Object.transform.localScale = new Vector3(x, y, z);
                }
                GUI.EndGroup();

                GUI.Label(new Rect(5, 450, 100, 20), "Editor Step: ");
                Editor.step = float.Parse(GUI.TextField(new Rect(80, 450, 70, 20), floatstr(Editor.step)));
                if (GUI.Button(new Rect(155, 450, 50, 20), "Reset")) Editor.step = 0.05f;
            }, "Object Properties");
            try
            {
                if (object_browser_enabled) object_browser = GUI.Window(13, object_browser, (windowId) =>
                {
                    GUI.DragWindow(new Rect(61, 0, 769, 20));
                    if (GUI.Button(new Rect(1110, 0, 50, 20), "Close")) object_browser_enabled = false;
                    if (GUI.Button(new Rect(1020, 0, 80, 20), "Next Page")) object_browser_page = Math.Min(33, object_browser_page + 1);
                    if (GUI.Button(new Rect(940, 0, 80, 20), "Prev Page")) object_browser_page = Math.Max(0, object_browser_page - 1);
                    if (GUI.Button(new Rect(830, 0, 100, 20), "Cheat Sheet")) object_browser_cheat_sheet = !object_browser_cheat_sheet;
                    GUI.Label(new Rect(5, 0, 40, 20), "Page:");
                    int val = object_browser_page + 1;
                    val = int.Parse(GUI.TextField(new Rect(40, 0, 21, 20), val.ToString()));
                    object_browser_page = Mathf.Clamp(val - 1, 0, 33);
                    for (int i = 0; i < 50 && object_browser_page * 50 + i < Main.Prefabs.Length; i++)
                    {
                        GUI.BeginGroup(new Rect(7 + 115 * (i % 10), 25 + 135 * (i / 10), 110, 130));
                        GUI.Box(new Rect(0, 0, 110, 130), "");
                        GUI.DrawTexture(new Rect(5, 5, 100, 100), object_browser_textures[i]);
                        GUI.Label(new Rect(5, 107, 200, 20), object_browser_page * 50 + i + " " + Main.Prefabs[object_browser_page * 50 + i].name);
                        if (GUI.Button(new Rect(80, 80, 20, 20), "+")) Editor.Spawn((object_browser_page * 50 + i).ToString());
                        object_browser_zooms[i] = GUIex.Slider(new Rect(5, 5, 100, 20), object_browser_zooms[i], 0.25f, 8f);
                        GUI.EndGroup();
                    }
                }, "Object Browser");
            } catch { }

            if (object_browser_enabled && object_browser_cheat_sheet) object_browser_cheat_sheet_window = GUI.Window(14, object_browser_cheat_sheet_window, (windowId) =>
            {
                GUI.DragWindow(new Rect(0, 0, 350, 20));
                if (GUI.Button(new Rect(350, 0, 50, 20), "Close")) object_browser_cheat_sheet = false;
                object_browser_cheat_sheet_scroll = GUI.BeginScrollView(new Rect(5, 20, 390, 480), object_browser_cheat_sheet_scroll, new Rect(0, 0, 360, 1500));
                GUI.TextArea(new Rect(0, 0, 360, 1500), cheatsheet);
                GUI.EndScrollView();
            }, "Cheat Sheet by sirty?#7676");



            if (PlayerMovement.Instance == null || Camera.main == null) return;
            GUI.DrawTexture(new Rect(5, Screen.height - 105, 100, 100), gizmoRender);
            GUI.Box(new Rect(110, Screen.height - 55, 340, 50), "");
            GUI.Label(new Rect(115, Screen.height - 50, 35, 20), "[Pos]");
            GUI.Label(new Rect(150, Screen.height - 50, 100, 20), "X: " + PlayerMovement.Instance.gameObject.transform.position.x);
            GUI.Label(new Rect(240, Screen.height - 50, 100, 20), "Y: " + PlayerMovement.Instance.gameObject.transform.position.y);
            GUI.Label(new Rect(330, Screen.height - 50, 100, 20), "Z: " + PlayerMovement.Instance.gameObject.transform.position.z);
            GUI.Label(new Rect(115, Screen.height - 30, 35, 20), "[Rot]");
            GUI.Label(new Rect(150, Screen.height - 30, 100, 20), "X: " + Camera.main.transform.rotation.eulerAngles.x);
            GUI.Label(new Rect(240, Screen.height - 30, 100, 20), "Y: " + Camera.main.transform.rotation.eulerAngles.y);
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
        private static string floatstr(float f)
        {
            if (f == Math.Floor(f)) return f + ".";
            return f.ToString();
        }

        private static Texture2D gizmoRender = new Texture2D(100, 100);

        public static void _OnUpdate()
        {
            if(!Main.editMode || Camera.main == null) return;
            GameObject GObg = GameObject.Find("/Gizmo Backplane");
            if(GObg == null) return;
            GameObject GOcam = new GameObject("Gizmo Camera");
            Camera cam = GOcam.AddComponent<Camera>();

            GOcam.transform.position = new Vector3(5000, 5000, 5000);
            GOcam.transform.rotation = Camera.main.transform.rotation;
            GOcam.transform.position -= GOcam.transform.forward * 5;

            GObg.transform.position = GOcam.transform.position + GOcam.transform.forward * 10;
            GObg.transform.LookAt(GOcam.transform);
            ShiftRotation(GObg.transform, 90, 0, 0);
            
            RenderTexture rt = new RenderTexture(100, 100, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            UnityEngine.Object.Destroy(gizmoRender);
            gizmoRender = new Texture2D(100, 100);
            gizmoRender.ReadPixels(new Rect(0, 0, 100, 100), 0, 0);
            gizmoRender.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(GOcam);
        }

        private static int oldWindow = -1;
        public static IEnumerator ObjectBrowserNewPage()
        {
            if(object_browser_page != oldWindow)
            {
                oldWindow = object_browser_page;
                for (int i = 0; i < 50; i++) object_browser_zooms[i] = 1f;
            }
            object_browser_textures = new Texture2D[50];
            for (int i = 0; i < 50; i++) object_browser_textures[i] = new Texture2D(100, 100);
            GameObject GObg = null, GOcam = null;
            Camera cam = null;
            RenderTexture rt = new RenderTexture(100, 100, 24);
            int rot = 0;
            while (true)
            {
                if(object_browser_enabled)
                {
                    if(GObg == null)
                    {
                        GObg = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        GObg.GetComponent<Renderer>().material.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1));
                        GObg.transform.localScale = new Vector3(500, 500, 500);
                        GOcam = new GameObject("Obj Browser Camera");
                        cam = GOcam.AddComponent<Camera>();
                        cam.targetTexture = rt;
                    }
                    rot += 10;
                    if (rot == 360) rot = 0;
                    for (int i = 0; i < 50 && object_browser_page * 50 + i < Main.Prefabs.Length; i++)
                    {
                        GameObject prefab = Main.Prefabs[i + object_browser_page * 50];
                        if (prefab.name == "Player" || prefab.name == "DetectWeapons")
                        {
                            for (int j = 0; j < 100; j++)
                                for (int k = 0; k < 100; k++)
                                    if (Math.Abs(j - k) < 5) object_browser_textures[i].SetPixel(j, k, new Color(1, 0, 0, 1));
                                    else object_browser_textures[i].SetPixel(j, k, new Color(0.5f, 0.5f, 0.5f, 1));
                            object_browser_textures[i].Apply();
                            continue;
                        }
                        prefab.SetActive(true);
                        GameObject render = Object.Instantiate(prefab, new Vector3(-5000, 5000, 5000), Quaternion.identity);
                        prefab.SetActive(false);
                        float size = maxSize3(render.transform.localScale);
                        GOcam.transform.position = new Vector3(-5000, 5000, 5000);
                        GOcam.transform.rotation = Quaternion.Euler(45, rot, 0);
                        GOcam.transform.position -= GOcam.transform.forward * 5 * object_browser_zooms[i];

                        GObg.transform.position = GOcam.transform.position + GOcam.transform.forward * 11 * object_browser_zooms[i];
                        GObg.transform.LookAt(GOcam.transform);
                        ShiftRotation(GObg.transform, 90, 0, 0);

                        cam.Render();
                        RenderTexture.active = rt;
                        object_browser_textures[i].ReadPixels(new Rect(0, 0, 100, 100), 0, 0);
                        object_browser_textures[i].Apply();
                        RenderTexture.active = null;

                        UnityEngine.Object.Destroy(render);
                        yield return new WaitForEndOfFrame();
                    }
                }
                else if(GObg != null)
                {
                    cam.targetTexture = null;
                    UnityEngine.Object.Destroy(GOcam);
                    UnityEngine.Object.Destroy(GObg);
                    GObg = null;
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        private static float maxSize3(Vector3 v)
        {
            float m = v.x;
            if(v.y > m) m = v.y;
            if(v.z > m) m = v.z;
            return m;
        }

        private static string cheatsheet = "Karlson Level Editor Prefab Cheat Sheet\n\nEach Level Ends with the Player and/or DetectWeapons prefabs, so use those to search around different levels\n\nTutorial 0-116\nSb0 118-243\nSb1 248-386\nSb2 388-487\nEsc0 493-644\nEsc1 660-871\nEsc2 873-954\nEsc3 965-1276\nSky0 1282-1385\nSky1 1393-1573\nSky2 1578-1689\n\n--- Interactables ---\n\nMilk - 65\nExplosive Barrel (\"default\") - 62\nCabinet (\"default\") - 63\nScreen - 28\nTable Top - 54\nJump Pad - 16, 873\nGlass - 520, 542\nFake Door - 820, 822\nLAVA HUGE ESC2 - 902\nRegular Lava Esc1 - 711\nDoor (not interactable yet) - 607, 608\n\n--- Weapons ---\n\nShotgun - 64\nUzi (\"AK47\") - 66\nPistol - 67\nGrapple - 68\nBoomer -27\n\n--- Important ---\n\nSmall Square Wall - 1580\nSquare Platform - 1582\nMonkey Bar - 18\nYellow Cube - 7\nWall - 8\nYellow Wall - 10\nThin Wall - 1578, 666\nTransparent Platform - 660\nTall Pole - 667\nTiny Platform - 669\nOrange Square - 894\nGiant Flooring/Ceiling Esc2 - 889\nGARGANTUAN Transparent Ceiling Esc2 - 891\nGARGANTUAN Floor Esc2 - 895\nBlue Pyramid - 249\nBlue Big Platform - 320\nBurnt Orange Pole - 252\nOrange Pole - 262\n\n\n\n*Tutorial*\n\nLong Platform - 25, 19\nYellow Cube - 7\nWall - 8\nYellow Wall - 10\nShort Wall - 11, 24\nLong Wall - 12\nSmall Platform - 13, 17\t\nSmall Red - 15\nJump Pad - 16\nMonkey Bar - 18\nLong Red Ramp - 22\nFloor - 23\n\n\n\n*Sb0*\n\nRed Pole - 122 , 127\t\nRed Box - 123\nRed Platform - 124\nRed Wall - 125\nRed Rotating Plank - 129\n\n\nFloor - 126\n\n\n*Sky2*\n\nSquare Platform - 1580\nThin Wall - 1578";
    }
}
