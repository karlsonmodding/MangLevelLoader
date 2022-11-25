using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using MelonLoader;
using MonoMod.RuntimeDetour.Platforms;
using System.Runtime.InteropServices;

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
		public static bool globalMov;
		public static byte[] currentLevel = new byte[0];
		public static string currentLevelName = "";
		public const string version = "0.3.3";
		public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
			base.OnSceneWasInitialized(buildIndex, sceneName);
			editMode = false;
			if (buildIndex == 1 && !initialized)
			{
				initialized = true;
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Levels"));
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "Workshop"));
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


		public const long CLIENT_ID = 1045056967242170388;
		private static string temp_accessToken = "";
		public static Discord.Discord discord;
		public static Discord.User currentUser = new Discord.User
		{
			Id = -1
		};
        public static Discord.Activity activity;

		public static List<Action> runOnMain = new List<Action>();
        public override void OnUpdate()
        {
            discord.RunCallbacks();
			if(runOnMain.Count > 0)
			{ // once at a time, not to overload
				runOnMain[0]();
				runOnMain.RemoveAt(0);
			}
        }

        public override void OnGUI()
        {
            Workshop_API.WorkshopGUI._OnGUI();
        }

        public override void OnApplicationStart()
		{
			LevelTimeDB.Load();
            discord = new Discord.Discord(CLIENT_ID, (ulong)Discord.CreateFlags.Default);
            var applicationManager = discord.GetApplicationManager();
            applicationManager.GetOAuth2Token((Discord.Result result, ref Discord.OAuth2Token token) =>
            {
                if (result != Discord.Result.Ok)
				{
					// TODO: replace with dialog
					MelonLogger.Msg("Couldn't connect to discord, please try again later.");
					return;
				}
				temp_accessToken = token.AccessToken;
			});
			var activityManager = discord.GetActivityManager();
			activity = new Discord.Activity
			{
				ApplicationId = CLIENT_ID,
				Assets =
				{
					LargeImage = "mllw_pfp",
					LargeText = "MLL Version " + version
				},
				Details = "Made by devilExE and Mang",
				Timestamps =
				{
					Start = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds
				}
			};
			activityManager.UpdateActivity(activity, (result) =>
            {
                if (result != Discord.Result.Ok)
                    MelonLogger.Msg("Couldn't update discord RPC");
            });
			var userManager = discord.GetUserManager();
            userManager.OnCurrentUserUpdate += UserManager_OnCurrentUserUpdate;
        }

        public static string sessionToken = "";
		public static List<int> likedLevels = new List<int>();
        private static void UserManager_OnCurrentUserUpdate()
        {
			currentUser = discord.GetUserManager().GetCurrentUser();
			int[] liked;
            (sessionToken, liked) = Workshop_API.Core.Login(currentUser.Id, temp_accessToken);
			if(liked.Length > 0)
				likedLevels.AddRange(liked);
        }
    }
}
