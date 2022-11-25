using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using MelonLoader;
using HarmonyLib;

using static KarlsonLevels.Main;
using KarlsonLevels.Workshop_API;

namespace KarlsonLevels
{
    class Editor
    {
        const string magic = "MLL2\r\n";
        const byte fileVersion = 0;
        public static float step = 0.05f;

        public static void NewSave(string name = "level") {
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Levels", name + ".mll"), SaveLevelBytes());
        }

        public static byte[] SaveLevelBytes()
        {
            MelonLogger.Msg(Level.Count);
            LevelData = new LevelObjectData[Level.Count];
            for (int i = 0; i < LevelData.Length; i++)
            {
                MelonLogger.Msg(Level[i].Object);
                LevelData[i].position = Level[i].Object.transform.position;
                LevelData[i].scale = Level[i].Object.transform.localScale;
                LevelData[i].rotation = Level[i].Object.transform.eulerAngles;
                LevelData[i].Id = Level[i].Id;
                LevelData[i].prefab = Level[i].prefab;
            }
            List<byte> save = new List<byte>();
            foreach (char c in magic)
            {
                save.Add(Convert.ToByte(c));
            }
            save.Add(fileVersion);
            ListAdd(ref save, (ushort)LevelData.Length);
            foreach (LevelObjectData lod in LevelData)
            {
                ListAdd(ref save, (ushort)lod.Id);
                ListAdd(ref save, (ushort)lod.prefab);
                ListAdd(ref save, lod.position);
                ListAdd(ref save, lod.scale);
                ListAdd(ref save, lod.rotation);
            }
            ListAdd(ref save, Hash(save.ToArray()));
            return save.ToArray();
        }

        public static byte[] MakeScreenshot()
        {
            GameObject GOcam = new GameObject("Screenshot Camera");
            Camera cam = GOcam.AddComponent<Camera>();
            cam.fieldOfView = Camera.main.fieldOfView;
            GOcam.transform.position = Camera.main.transform.position;
            GOcam.transform.rotation = Camera.main.transform.rotation;
            RenderTexture rt = new RenderTexture(177, 100, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(177, 100, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, 177, 100), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(GOcam);
            return screenShot.EncodeToPNG();
        }

        static void Save() {
            LevelData = new LevelObjectData[Level.Count];
            for (int i = 0; i < LevelData.Length; i++)
            {
                MelonLogger.Msg(Level[i].Object);
                LevelData[i].position = Level[i].Object.transform.position;
                LevelData[i].scale = Level[i].Object.transform.localScale;
                LevelData[i].rotation = Level[i].Object.transform.eulerAngles;
                LevelData[i].Id = Level[i].Id;
                LevelData[i].prefab = Level[i].prefab;
            }
            string[] lines = new string[LevelData.Length + 1];
            lines[0] = "MLL1";
            for (int i = 1; i < lines.Length; i++)
            {
                int j = i - 1;
                lines[i] = $"{LevelData[j].Id};{LevelData[j].prefab};{LevelData[j].position.ToString()};{LevelData[j].scale};{LevelData[j].rotation.ToString()};";
            }
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "level.mll"), lines, Encoding.ASCII);
        }

        public static LevelObject Spawn(string obj) {
            int index = Convert.ToInt32(obj);
            Prefabs[index].SetActive(true);
            GameObject foo = Object.Instantiate(Prefabs[index], PlayerMovement.Instance.gameObject.transform.position, Quaternion.identity);
            LevelObject bar;
            bar.Object = foo;
            bar.Id = GetId();
            bar.prefab = index;
            Level.Add(bar);
            if (index == 129) // Removes hinge joint because it glitches movement mode in editing
            {
                Object.Destroy(foo.GetComponent<HingeJoint>());
                Object.Destroy(foo.GetComponent<Rigidbody>());
            }
            Prefabs[index].SetActive(false);
            return bar;
        }

        static int GetId() {
        back:
            int foo = UnityEngine.Random.Range(1, 10000);
            foreach (LevelObject l in Level) if (l.Id == foo) goto back;
            return foo;
        }

        public static IEnumerator NewLoad(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            return NewLoad(data, Path.GetFileName(path));
        }

        public static IEnumerator NewLoad(byte[] data, string levelName) {
            try
            {
                if (data != null && data.Length > 0)
                {
                    currentLevel = data;
                    currentLevelName = levelName;
                    // check magic number
                    if (!(data[0] == 77 && data[1] == 76 && data[2] == 76 && data[3] == 50 && data[4] == 13 && data[5] == 10))
                    { // not a good solution but it works and im lazy
                        MelonLogger.Error("Load error: bad magic number; this might not be a level file");
                        yield break;
                    }
                    else if (data[6] > fileVersion)
                    {
                        MelonLogger.Error($"Load error: file version is newer than {fileVersion:00}, please update MLL to load this level!");
                        yield break;
                    }
                    else if (!CheckHash(data))
                    {
                        MelonLogger.Error("Load error: level data is corrupted");
                        yield break;
                    }
                    ushort objsLength = BitConverter.ToUInt16(data, 7);
                    LevelData = new LevelObjectData[objsLength];
                    for (int j = 0, i = 9; j < objsLength; i += 40, j++)
                    {
                        LevelObjectData lod;
                        lod.Id = BitConverter.ToUInt16(data, i);
                        lod.prefab = BitConverter.ToUInt16(data, i + 2) & 0x1FFF;
                        float x = BitConverter.ToSingle(data, i + 4);
                        float y = BitConverter.ToSingle(data, i + 8);
                        float z = BitConverter.ToSingle(data, i + 12);
                        lod.position = new Vector3(x, y, z);
                        MelonLogger.Msg(lod.position);
                        x = BitConverter.ToSingle(data, i + 16);
                        y = BitConverter.ToSingle(data, i + 20);
                        z = BitConverter.ToSingle(data, i + 24);
                        lod.scale = new Vector3(x, y, z);
                        x = BitConverter.ToSingle(data, i + 28);
                        y = BitConverter.ToSingle(data, i + 32);
                        z = BitConverter.ToSingle(data, i + 36);
                        lod.rotation = new Vector3(x, y, z);
                        LevelData[j] = lod;
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.StackTrace);
                MelonLogger.Error(e.Message);
            }
            yield return null;
            SceneManager.LoadScene(6);
            yield return null;
            foreach (Collider c in Object.FindObjectsOfType<Collider>())
            {
                if (c.gameObject != PlayerMovement.Instance.gameObject & c.gameObject.GetComponent<DetectWeapons>() == null) DestroyObject.Destroy(c.gameObject);
            }
            Level = new List<LevelObject>();
            for (int i = 0; i < LevelData.Length; i++)
            {
                Quaternion q = new Quaternion();
                q.eulerAngles = LevelData[i].rotation;
                GameObject g = Object.Instantiate(Prefabs[LevelData[i].prefab], LevelData[i].position, q);
                if ((LevelData[i].prefab ^ 129) == 0) {
                    g.GetComponent<HingeJoint>().connectedBody = null;
                }
                g.transform.localScale = LevelData[i].scale;
                g.SetActive(true);
                LevelObject lo;
                lo.Object = g;
                lo.prefab = LevelData[i].prefab;
                lo.Id = LevelData[i].Id;
                Level.Add(lo);
            }
            Time.timeScale = 1f;
            Game.Instance.StartGame();
        }

        static IEnumerator LoadLevel(string path) {
            if (path != null)
            {
                string[] lines = File.ReadAllLines(path, Encoding.ASCII);
                if (lines[0] != "MLL1") yield break;
                LevelData = new LevelObjectData[lines.Length - 1];
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] line = lines[i + 1].Split(';');
                    LevelData[i].Id = Convert.ToInt32(line[0]);
                    LevelData[i].prefab = Convert.ToInt32(line[1]);
                    LevelData[i].position = StringToVector3(line[2]);
                    LevelData[i].scale = StringToVector3(line[3]);
                    LevelData[i].rotation = StringToVector3(line[4]);
                }
                yield return null;
            }
            SceneManager.LoadScene(6);
            yield return null;
            foreach (Collider c in Object.FindObjectsOfType<Collider>())
            {
                if (c.gameObject != PlayerMovement.Instance.gameObject) DestroyObject.Destroy(c.gameObject);
            }
            for (int i = 0; i < LevelData.Length; i++)
            {
                Quaternion q = new Quaternion();
                q.eulerAngles = LevelData[i].rotation;
                GameObject g = Object.Instantiate(Prefabs[LevelData[i].prefab], LevelData[i].position, q);
                g.SetActive(true);
                LevelObject lo;
                lo.Object = g;
                lo.prefab = LevelData[i].prefab;
                lo.Id = LevelData[i].Id;
                Level.Add(lo);
            }
        }

        public static IEnumerator StartEdit(string path) {
            if (path == null)
            {
                Level = new List<LevelObject>();
                SceneManager.LoadScene(6);
            }
            yield return null;
            yield return null;
            yield return null;
            try
            {
                editMode = true;
                Main.movableObj = -1;
                PlayerMovement.Instance.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                PlayerMovement.Instance.gameObject.GetComponent<Rigidbody>().useGravity = false; //eh
                PlayerMovement.Instance.gameObject.GetComponent<Collider>().enabled = false;
                if (path == null)
                    foreach (Collider c in Object.FindObjectsOfType<Collider>()) 
                        if (c.gameObject != PlayerMovement.Instance.gameObject) DestroyObject.Destroy(c.gameObject);
            }
            catch (Exception e)
            {
                MelonLogger.Error("Player instance error! " + e.StackTrace);
            }
        }
    }

    public static class Extensions
    {
        public static byte[] SubArray(this byte[] array, int offset, int length) {
            return array.Skip(offset)
                        .Take(length)
                        .ToArray();
        }
    }

    [HarmonyPatch(typeof(Debug), "OpenConsole")]
    public class Debug_OpenConsole
    {
        public static bool Prefix() => !editMode;
    }
}
