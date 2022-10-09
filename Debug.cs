using Harmony;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static KarlsonLevels.Main;

namespace KarlsonLevels
{
	[HarmonyPatch(typeof(Debug), "RunCommand")]
	class Editor
	{
		const string magic = "MLL2\r\n";
		const byte fileVersion = 0;
		static bool Prefix(Debug __instance) {
			string[] command = __instance.console.text.Split(' ');
			try
			{
				switch (command[0].ToLower())
				{
					case "edit":
						MelonCoroutines.Start(StartEdit());
						break;
					case "list":
						for (int i = 0; i < Prefabs.Length; i++)
						{
							MelonLogger.Msg(i + " " + Prefabs[i].name);
						}
						break;
					case "spawn":
						Spawn(command[1], ref __instance);
						break;
					case "save":
						NewSave();
						break;
					case "load":
						string path = null;
						if (command.Length != 1)
						{
							path = $"{Directory.GetCurrentDirectory()}\\Levels\\{command[1]}.mll";
						}
						MelonCoroutines.Start(NewLoad(path));
						break;
					case "moveobj":
						movableObj = Convert.ToInt32(command[1]);
						MovementMode = MoveModeEnum.movement;
						break;
					case "rotate":
						movableObj = Convert.ToInt32(command[1]);
						MovementMode = MoveModeEnum.rotation;
						break;
					case "scale":
						movableObj = Convert.ToInt32(command[1]);
						MovementMode = MoveModeEnum.scale;
						break;
					default:
						return true;
				}
			}
			catch (Exception e)
			{
				MelonLogger.Error(e.Message);
				MelonLogger.Error(e.StackTrace);
			}
			__instance.console.text = "";
			__instance.console.Select();
			__instance.console.ActivateInputField();
			return false;
		}

		public static void NewSave() {
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
			File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\Levels\\level.mll", save.ToArray());
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
			File.WriteAllLines(Directory.GetCurrentDirectory() + "\\Levels\\level.mll", lines, Encoding.ASCII);
		}

		static void Spawn(string obj, ref Debug inst) {
			int index = Convert.ToInt32(obj);
			Prefabs[index].SetActive(true);
			GameObject foo = Object.Instantiate(Prefabs[index], PlayerMovement.Instance.gameObject.transform.position, Quaternion.identity);
			LevelObject bar;
			bar.Object = foo;
			bar.Id = GetId();
			bar.prefab = index;
			Level.Add(bar);
			Prefabs[index].SetActive(false);
			inst.consoleLog.text += "\nSpawned object with ID " + bar.Id;
		}

		static int GetId() {
		back:
			int foo = UnityEngine.Random.Range(1, 10000);
			foreach (LevelObject l in Level) if (l.Id == foo) goto back;
			return foo;
		}

		static IEnumerator NewLoad(string path) {
			if (path != null)
            {
				byte[] data = File.ReadAllBytes(path);
				if (!CheckHash(data)) yield break;
				// check magic number
				if (!(data[0] == 77 && data[1] == 76 && data[2] == 76 && data[3] == 50 && data[4] == 13 && data[5] == 10)) yield break; // not a good solution but it works and im lazy
				MelonLogger.Msg("lel");
				if (data[6] != fileVersion) yield break;
				ushort objsLength = BitConverter.ToUInt16(data, 7);
				LevelData = new LevelObjectData[objsLength];
				for (int i = 9, j = 0; j < objsLength; i += 40, j++) {
					LevelObjectData lod;
					lod.Id = BitConverter.ToUInt16(data, i);
					MelonLogger.Msg(lod.Id);
					lod.prefab = BitConverter.ToUInt16(data, i + 2) & 0x1FFF;
					MelonLogger.Msg(lod.prefab);
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
			yield return null;
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

		static IEnumerator StartEdit() {
			Level = new List<LevelObject>();
			SceneManager.LoadScene(6);
			yield return null;
			yield return null; //why this like srsly why it doesnt make any sense
			editMode = true;
			PlayerMovement.Instance.gameObject.GetComponent<Rigidbody>().isKinematic = false;
			PlayerMovement.Instance.gameObject.GetComponent<Rigidbody>().useGravity = false; //eh
			PlayerMovement.Instance.gameObject.GetComponent<Collider>().isTrigger = true;
			foreach (Collider c in Object.FindObjectsOfType<Collider>())
			{
				if (c.gameObject != PlayerMovement.Instance.gameObject) DestroyObject.Destroy(c.gameObject);
			}
		}
	}
	[HarmonyPatch(typeof(Debug), "Help")]
	class Help1
	{
		static void Postfix(Debug __instance) {
			__instance.consoleLog.text += $"\n\nMangLevelLoader {version}\n  edit - Enters edit mode\n  list - Lists all available prefab'd objects\n" +
				$"  spawn (prefab) - Spawns the associated prefab and displays it's ID\n  moveobj (ID) - Makes the object movable with WASD, set to 0\nto get control" +
				$" of the player back\n  save - Saves the level to level.mll\n  load (name) - Loads the level with the given name\n  scale (ID) - Control the scale with keyboard" +
				$"\n  rotate (ID) - Control the rotation with keyboard\nMade by Mang432";
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
}
