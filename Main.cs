using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


[assembly: MelonInfo(typeof(KarlsonLevels.Main), "MangLevelLoader", KarlsonLevels.Main.version + "-beta", "Mang432")]
[assembly: MelonGame("Dani", "Karlson")]
namespace KarlsonLevels
{
	public class Main : MelonMod
	{
		bool initialized;
		public static GameObject[] Prefabs;
		public static List<LevelObject> Level = new List<LevelObject>();
		public static LevelObjectData[] LevelData;
		public static bool editMode;
		public static MoveModeEnum MovementMode;
		public static int movableObj; // Id of the object that's currently in control, only applicable in edit mode
		public static string currentLevel;
		public const string version = "0.3.1";
		public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
			base.OnSceneWasInitialized(buildIndex, sceneName);
			editMode = false;
			if (buildIndex == 1 && !initialized)
			{
				initialized = true;
				Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Levels");
				MelonCoroutines.Start(PrefabInitializer());
			}
		}

		IEnumerator PrefabInitializer() {
			List<GameObject> objs = new List<GameObject>();
			for (int i = 2; i <= 12; i++)
			{
				yield return null;
				SceneManager.LoadScene(i);
				yield return null;
				foreach (Collider g in Object.FindObjectsOfType<Collider>())
				{
					objs.Add(g.gameObject);
					g.transform.parent = null;
					Object.DontDestroyOnLoad(g.gameObject);
					g.gameObject.SetActive(false);
				}
				MelonLogger.Msg("Level " + (i - 1) + " loaded");
			}
			yield return null;
			Prefabs = objs.ToArray();
			SceneManager.LoadScene(1);
			MelonLogger.Msg(Prefabs.Length + " objects loaded");
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
		}

		public static int IdToIndex(int Id) {
			for (int i = 0; i < Level.Count; i++)
			{
				if (Level[i].Id == Id) return i;
			}
			return -1;
		}

		public static Vector3 StringToVector3(string sVector) {
			// Remove the parentheses
			if (sVector.StartsWith("(") && sVector.EndsWith(")"))
			{
				sVector = sVector.Substring(1, sVector.Length - 2);
			}

			// split the items
			string[] sArray = sVector.Split(',');

			// store as a Vector3
			Vector3 result = new Vector3(
				float.Parse(sArray[0]),
				float.Parse(sArray[1]),
				float.Parse(sArray[2]));

			return result;
		}

		public static void ListAdd(ref List<byte> list, uint value) {
			byte[] valb = BitConverter.GetBytes(value);
			foreach(byte b in valb)
            {
				list.Add(b);
            }
        }

		public static void ListAdd(ref List<byte> list, ushort value) {
			byte[] valb = BitConverter.GetBytes(value);
			foreach (byte b in valb)
			{
				list.Add(b);
			}
		}

		public static void ListAdd(ref List<byte> list, float value) {
			byte[] valb = BitConverter.GetBytes(value);
			foreach (byte b in valb)
			{
				list.Add(b);
			}
		}

		public static void ListAdd(ref List<byte> list, Vector3 value) {
			ListAdd(ref list, value.x);
			ListAdd(ref list, value.y); 
			ListAdd(ref list, value.z);
		}

		public static uint Hash(byte[] data) {
			uint hash = 5381;
			for (int i = 0; i < data.Length; i++)
            {
				hash = (hash * 33) ^ data[i];
			}
			return hash;
        }

		public static bool CheckHash(byte[] data) {
			byte[] test = BitConverter.GetBytes(Hash(data.Take(data.Length - 4).ToArray()));
			return Enumerable.SequenceEqual(test, data.Skip(data.Length - 4).ToArray());
		}

		public enum MoveModeEnum : byte { movement, scale, rotation }

		public struct LevelObject
		{
			public GameObject Object;
			public int Id, prefab;
		}

		public struct LevelObjectData
		{
			public Vector3 scale, position, rotation;
			public int Id, prefab;
		}
	}
}
