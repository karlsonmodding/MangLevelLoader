using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using System.IO;
using MelonLoader;
using System.Runtime.CompilerServices;

namespace KarlsonLevels
{
    public static class LevelTimeDB
    {
        private static List<(string, float)> db = new List<(string, float)>();
        public static void Load()
        {
            if(!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "_timetable")))
            {
                MelonLogger.Msg("Creating time table");
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "_timetable"), "0");
            }
            string[] lines = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "_timetable"));
            int count = int.Parse(lines[0]);
            for(int i = 1; i <= count; i++)
                db.Add((lines[i].Split('|')[0], float.Parse(lines[i].Split('|')[1])));
        }

        public static void Save()
        {
            string buf = db.Count + "\n";
            foreach(var e in db)
                buf += e.Item1 + "|" + e.Item2 + "\n";
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "_timetable"), buf);
        }

        public static float getForLevel(string level)
        {
            var r = from x in db where x.Item1 == level select x;
            if (r.Count() == 0) return 0f;
            else return r.First().Item2;
        }

        public static void writeForLevel(string level, float time)
        {
            var r = from x in db where x.Item1 == level select x;
            if(r.Count() != 0) db.Remove(r.First());
            db.Add((level, time));
        }
    }

    [HarmonyPatch(typeof(Game), "Win")]
    public class Game_Win
    {
        public static bool Prefix(Game __instance)
        {
            if (Main.currentLevelName == "") return true;
            __instance.playing = false;
            Timer.Instance.Stop();
            Time.timeScale = 0.05f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UIManger.Instance.WinUI(true);
            float timer = Timer.Instance.GetTimer();
            float num3 = LevelTimeDB.getForLevel(Main.currentLevelName);
            if (timer < num3 || num3 == 0f)
            {
                LevelTimeDB.writeForLevel(Main.currentLevelName, timer);
                LevelTimeDB.Save();
            }
            MonoBehaviour.print("time has been saved as: " + Timer.Instance.GetFormattedTime(timer) + " on _timetable");
            __instance.done = true;
            return false;
        }
    }
}
