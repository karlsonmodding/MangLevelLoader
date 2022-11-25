using Harmony;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonLevels.Workshop_API
{
    public static class WorkshopCache
    {
        public static void ClearCache()
        {
            mostLikedCache = new int[0];
            mostLikedCacheGen = false;
            mostDlCache = new int[0];
            mostDlCacheGen = false;
            mostRecCache = new int[0];
            mostRecCacheGen = false;
            levelCache.Clear();
        }

        public static void MakeCache()
        {
            GrabMostLiked();
            GrabMostDl();
            GrabMostRec();
        }

        public static void QueueDownload(int levelId)
        {
            if(downloadThread == null)
            {
                downloadThread = new Thread(() =>
                {
                    WebClient wc = new WebClient();
                    wc.DownloadProgressChanged += (sender, e) => downloadProgress = e.ProgressPercentage;
                    wc.DownloadFileCompleted += (sender, e) => downloadCurrent = "";
                    while(true)
                    {
                        Thread.Sleep(0);
                        if(DownloadList.Count > 0 && downloadCurrent == "")
                        {
                            downloadCurrent = DownloadList[0].Item1;
                            downloadProgress = 0;
                            wc.DownloadFileAsync(new Uri(DownloadList[0].Item2), DownloadList[0].Item3);
                            DownloadList.RemoveAt(0);
                        }
                    }
                });
                downloadThread.Start();
            }
            DownloadList.Add((levelId + " Level Data", Core.API_ENDPOINT + "/level/downloadlevel.php?id=" + levelId + "&token=" + Main.sessionToken, Path.Combine(Directory.GetCurrentDirectory(), "Levels", "Workshop", levelId + ".wml")));
        }
        // Display Name, Url, file location
        private static List<(string, string, string)> DownloadList = new List<(string, string, string)>();
        private static Thread downloadThread = null;
        public static int downloadProgress = 0;
        public static string downloadCurrent = "";
        public static int QueuedFiles() => DownloadList.Count;

        public static void GrabMostLiked()
        {
            if (mostLikedCache.Length != 0 || mostLikedCacheGen) return;
            mostLikedCacheGen = true;
            new Thread(() =>
            {
                mostLikedCache = Core.GetMostLiked();
                foreach (int id in mostLikedCache) GrabLevel(id);
                mostLikedCacheGen = false;
            }).Start();
        }
        public static void GrabMostDl()
        {
            if (mostDlCache.Length != 0 || mostDlCacheGen) return;
            mostDlCacheGen = true;
            new Thread(() =>
            {
                mostDlCache = Core.GetMostDl();
                foreach (int id in mostDlCache) GrabLevel(id);
                mostDlCacheGen = false;
            }).Start();
        }
        public static void GrabMostRec()
        {
            if (mostRecCache.Length != 0 || mostRecCacheGen) return;
            mostRecCacheGen = true;
            new Thread(() =>
            {
                mostRecCache = Core.GetMostRecent();
                foreach (int id in mostRecCache) GrabLevel(id);
                mostRecCacheGen = false;
            }).Start();
        }

        private static void GrabLevel(int id)
        {
            if (levelCache.ContainsKey(id)) return;
            Core.SmallLevelData ld = Core.GetLevelInfo(id);
            // todo: add liked list
            try {
                levelCache.Add(id,
                    new WorkshopLevel(id, ld.Name, Core.GetUserName(ld.Author), Texture2D.blackTexture, ld.Dl, ld.Likes, Main.likedLevels.Contains(id))
                );
            } catch { return; } // weird multi-thread

            Main.runOnMain.Add(() =>
            {
                Texture2D tex = new Texture2D(0, 0);
                tex.LoadImage(ld.Picture);
                levelCache[id].Thumbnail = tex;
            });
        }

        public static int[] mostLikedCache { get; private set; } = new int[0];
        public static bool mostLikedCacheGen { get; private set; } = false;
        public static int[] mostDlCache { get; private set; } = new int[0];
        public static bool mostDlCacheGen { get; private set; } = false;
        public static int[] mostRecCache { get; private set; } = new int[0];
        public static bool mostRecCacheGen { get; private set; } = false;
        public static Dictionary<int, WorkshopLevel> levelCache { get; private set; } = new Dictionary<int, WorkshopLevel>();
    }

    public class WorkshopLevel
    {
        public WorkshopLevel(int id, string name, string author, Texture2D thumbnail, int dl, int likes, bool liked)
        {
            Id = id;
            Name = name;
            Author = author;
            Thumbnail = thumbnail;
            Dl = dl;
            Likes = likes;
            Downloaded = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "Workshop", id + ".wml"));
            Liked = liked;
        }

        public int Id;
        public string Name;
        public string Author;
        public Texture2D Thumbnail;
        public int Dl;
        public int Likes;
        public bool Downloaded;
        public bool Liked;

        public void ToggleLike()
        {
            Liked = !Liked;
            if(Liked)
            {
                Core.LikeLevel(Id);
                Main.likedLevels.Add(Id);
                Likes++;
            }
            else
            {
                Core.UnlikeLevel(Id);
                Main.likedLevels.Remove(Id);
                Likes--;
            }
        }

        public void DownloadLevel()
        {
            if (Downloaded) return;
            Downloaded = true;
            Dl++;
            WorkshopCache.QueueDownload(Id);
        }
    }
}
