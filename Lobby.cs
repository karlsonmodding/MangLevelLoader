using HarmonyLib;
using KarlsonLevels.Workshop_API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KarlsonLevels
{
    [HarmonyPatch(typeof(Lobby), "LoadMap")]
    class Lobby_LoadMap
    {
        static void Prefix() {
            Main.currentLevel = null;
        }
    }

    [HarmonyPatch(typeof(Lobby), "Start")]
    class Lobby_Start
    {
        static void Prefix()
        {
            Object.Destroy(GameObject.Find("/UI/Menu/Map"));
            GameObject.Find("/UI/Menu/About").transform.position = new Vector3(-6.6021f, 9.5527f, 185.61f);
            GameObject GO_Levels = UnityEngine.Object.Instantiate(GameObject.Find("/UI/Menu/Options"));
            GO_Levels.transform.parent = GameObject.Find("/UI/Menu").transform;
            GO_Levels.transform.position = new Vector3(-6.6021f, 10.9891f, 185.61f);
            GO_Levels.transform.localScale = new Vector3(1.1707f, 1.1707f, 1.1707f);
            GO_Levels.name = "Custom";
            ((TMP_Text)GO_Levels.GetComponent<Button>().targetGraphic).text = "Custom";
            

            GameObject GO_LevelsUI = UnityEngine.Object.Instantiate(GameObject.Find("/UI").transform.Find("Play").gameObject);
            GO_LevelsUI.transform.parent = GameObject.Find("/UI").transform; // don't find accidentaly other /UI -- it won't happen
            GO_LevelsUI.transform.position = new Vector3(-7.9997f, 14.4872f, 188.3855f);
            GO_LevelsUI.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            GO_LevelsUI.transform.localScale = new Vector3(0.7635f, 0.7635f, 0.7635f);
            GO_LevelsUI.name = "Custom";
            GO_LevelsUI.SetActive(false);
            // initialize ui
            levelTemplate = UnityEngine.Object.Instantiate(GO_LevelsUI.transform.Find("Escape0").gameObject);
            levelTemplate.name = "Level Template"; // no need to set any properties yet
            /**
             * Top left:
             * -7.9997f, 17.4876f, 176.4724f
             * Next in row:
             * -7.9997f, 17.4876f, 181.4724f
             * delta= 0f, 0f, 5f
             * Next in column:
             * -7.9997f, 14.1901f, 176.473f
             * delta= 0f, -3f, 0f
             */
            try
            {
                randomSprite.Clear();
                for (int i = 1; i <= GO_LevelsUI.transform.childCount; i++)
                {
                    randomSprite.Add(GO_LevelsUI.transform.GetChild(i).gameObject.GetComponent<Image>().sprite);
                    UnityEngine.Object.Destroy(GO_LevelsUI.transform.GetChild(i).gameObject);
                }
            } catch(Exception e)
            {
                MelonLoader.MelonLogger.Msg(e.ToString());
            }
            
            // initialize buttons
            GameObject nextPage = UnityEngine.Object.Instantiate(GO_LevelsUI.transform.Find("Back").gameObject);
            TextMeshProUGUI currentPage = UnityEngine.Object.Instantiate((TextMeshProUGUI)nextPage.GetComponent<Button>().targetGraphic); // initialize here text because we modify scale of text
            currentPage.transform.parent = GO_LevelsUI.transform; // hitbox layering
            nextPage.transform.parent = GO_LevelsUI.transform;
            ((TextMeshProUGUI)nextPage.GetComponent<Button>().targetGraphic).text = ">";
            nextPage.transform.position = new Vector3(-7.9997f, 20.3098f, 192.6089f);
            nextPage.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            // weird double scaling and resizing hitbox fix, idk how i even figured this out
            nextPage.transform.localScale = new Vector3(0.1932f, 1.1707f, 1.1707f);
            ((TextMeshProUGUI)nextPage.GetComponent<Button>().targetGraphic).rectTransform.localScale = new Vector3(5f, 0.8255f, 0.8255f);
            ((TextMeshProUGUI)nextPage.GetComponent<Button>().targetGraphic).rectTransform.sizeDelta = new Vector2(30, 30);
            nextPage.name = "NextPage";
            GameObject prevPage = UnityEngine.Object.Instantiate(GO_LevelsUI.transform.Find("Back").gameObject);
            prevPage.transform.parent = GO_LevelsUI.transform;
            ((TextMeshProUGUI)prevPage.GetComponent<Button>().targetGraphic).text = "<";
            prevPage.transform.position = new Vector3(-7.9997f, 20.3098f, 190.8047f);
            prevPage.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            // weird double scaling and resizing hitbox fix, idk how i even figured this out
            prevPage.transform.localScale = new Vector3(0.1932f, 1.1707f, 1.1707f);
            ((TextMeshProUGUI)prevPage.GetComponent<Button>().targetGraphic).rectTransform.localScale = new Vector3(5f, 0.8255f, 0.8255f);
            ((TextMeshProUGUI)prevPage.GetComponent<Button>().targetGraphic).rectTransform.sizeDelta = new Vector2(30, 30);
            prevPage.name = "PrevPage";
            GameObject workshop = UnityEngine.Object.Instantiate(GO_LevelsUI.transform.Find("Back").gameObject);
            workshop.transform.parent = GO_LevelsUI.transform;
            ((TextMeshProUGUI)workshop.GetComponent<Button>().targetGraphic).text = "Workshop";
            workshop.transform.position = new Vector3(-7.9997f, 20.3098f, 183.988f);
            workshop.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            // weird double scaling and resizing hitbox fix, idk how i even figured this out
            workshop.transform.localScale = new Vector3(0.1352f, 0.8195f, 0.8195f);
            ((TextMeshProUGUI)workshop.GetComponent<Button>().targetGraphic).rectTransform.localScale = new Vector3(5f, 0.8255f, 0.8255f);
            ((TextMeshProUGUI)workshop.GetComponent<Button>().targetGraphic).rectTransform.sizeDelta = new Vector2(300, 30);
            workshop.name = "Workshop";

            currentPage.transform.position = new Vector3(-7.9997f, 20.2474f, 191.691f);
            currentPage.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            currentPage.transform.localScale = new Vector3(0.8255f, 0.8255f, 0.8255f);
            currentPage.text = "-";
            currentPage.gameObject.name = "CurrentPage";


            InterceptButton(prevPage.GetComponent<Button>(), () => RenderMenuPage(menuPage - 1));
            InterceptButton(nextPage.GetComponent<Button>(), () => RenderMenuPage(menuPage + 1));
            InterceptButton(workshop.GetComponent<Button>(), () => Workshop_API.WorkshopGUI.OpenWorkshop());

            InterceptButton(GO_Levels.GetComponent<Button>(), () =>
            {
                MelonLoader.MelonLogger.Msg("Custom click");
                MenuCamera cam = UnityEngine.Object.FindObjectOfType<MenuCamera>();
                typeof(MenuCamera).GetField("desiredPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(cam, new Vector3(-1f, 15.1f, 184.06f));
                typeof(MenuCamera).GetField("desiredRot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(cam, Quaternion.Euler(0f, -90f, 0f));

                GameObject.Find("/UI/Menu").SetActive(false);
                GO_LevelsUI.SetActive(true);
                RenderMenuPage(1);
            });
        }

        private static void DisableButton(Button b) {
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                b.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }

        private static void InterceptButton(Button b, UnityAction onClick)
        {
            DisableButton(b);
            b.onClick.AddListener(onClick);
        }

        private static int menuPage = 0;
        private static GameObject levelTemplate;
        private static List<Sprite> randomSprite = new List<Sprite>();
        public static void RenderMenuPage(int page)
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
            menuPage = page;
            GameObject.Find("/UI/Custom/CurrentPage").GetComponent<TextMeshProUGUI>().text = page.ToString();

            // clear old levels
            for(int i = 5; i < GameObject.Find("/UI/Custom").transform.childCount; i++)
                UnityEngine.Object.Destroy(GameObject.Find("/UI/Custom").transform.GetChild(i).gameObject);

            List<string> l = new List<string>();
            l.AddRange(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Levels"), "*.mll"));
            l.AddRange(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Levels", "Workshop"), "*.wml"));
            string[] levelList = l.ToArray();

            for (int i = 0; i < 12 && i < levelList.Length - 12 * (page-1); i++)
            {
                int idx = 12 * (page - 1) + i;
                GameObject go = UnityEngine.Object.Instantiate(levelTemplate);
                go.transform.parent = GameObject.Find("/UI/Custom").transform;
                go.transform.position = new Vector3(-7.9997f, 17.4876f - 3 * (i / 4), 176.4724f + 5 * (i % 4));
                go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                go.transform.localScale = new Vector3(1f, 1f, 1f);
                if (Path.GetExtension(levelList[idx]) == ".mll")
                {
                    go.GetComponentsInChildren<TextMeshProUGUI>()[0].text = Path.GetFileNameWithoutExtension(levelList[idx]);
                    go.GetComponentsInChildren<TextMeshProUGUI>()[1].text = LevelTimeDB.getForLevel(Path.GetFileName(levelList[idx])) == 0 ? "[NO RECORD]" : Timer.Instance.GetFormattedTime(LevelTimeDB.getForLevel(Path.GetFileName(levelList[idx])));
                    go.GetComponent<Image>().sprite = randomSprite[UnityEngine.Random.Range(0, randomSprite.Count - 2)];
                    go.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);
                    GameObject text = new GameObject("mll text");
                    text.AddComponent<TextMeshProUGUI>();
                    text.GetComponent<TextMeshProUGUI>().text = ".mll level";
                    text.transform.parent = go.transform;
                    text.transform.localPosition = new Vector3(13.9504f, -17.8543f, 0f);
                    text.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                    text.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    InterceptButton(go.GetComponent<Button>(), () =>
                    {
                        MelonLoader.MelonLogger.Msg("Loading level idx: " + idx);
                        MelonLoader.MelonLogger.Msg("Loading level: " + levelList[idx]);
                        MelonLoader.MelonCoroutines.Start(Editor.NewLoad(levelList[idx]));
                    });
                    continue;
                }
                // read wml
                WML_Convert.WML level = WML_Convert.Decode(File.ReadAllBytes(levelList[idx]));
                go.GetComponentsInChildren<TextMeshProUGUI>()[0].text = level.Name;
                go.GetComponentsInChildren<TextMeshProUGUI>()[1].text = LevelTimeDB.getForLevel(Path.GetFileName(levelList[idx])) == 0 ? "[NO RECORD]" : Timer.Instance.GetFormattedTime(LevelTimeDB.getForLevel(Path.GetFileName(levelList[idx])));
                Texture2D tex = new Texture2D(1, 1);
                tex.LoadImage(level.Thumbnail);
                go.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
                InterceptButton(go.GetComponent<Button>(), () =>
                {
                    MelonLoader.MelonLogger.Msg("Loading level idx: " + idx);
                    MelonLoader.MelonLogger.Msg("[WML] Loading level: " + levelList[idx]);
                    MelonLoader.MelonCoroutines.Start(Editor.NewLoad(level.LevelData, Path.GetFileName(levelList[idx])));
                });
            }

            GameObject.Find("/UI/Custom/PrevPage").GetComponent<Button>().interactable = page != 1;
            GameObject.Find("/UI/Custom/NextPage").GetComponent<Button>().interactable = (levelList.Length - 1) / 12 >= page;
        }
    }
}
