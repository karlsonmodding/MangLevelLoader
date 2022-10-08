using Harmony;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using static KarlsonLevels.Main;

namespace KarlsonLevels
{
	[HarmonyPatch(typeof(Debug), "RunCommand")]
	class Editor
	{
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
						Save();
						break;
					case "load":
						string path = null;
						if (command.Length != 1)
						{
							path = $"{Directory.GetCurrentDirectory()}\\Levels\\{command[1]}.mll";
						}
						MelonCoroutines.Start(LoadLevel(path));
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
				Object.Instantiate(Prefabs[LevelData[i].prefab], LevelData[i].position, q).SetActive(true);
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
}
