using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonLevels
{
    public static class GUIex
    {
        private static bool initialized = false;
        private static void _init()
        {
            if(initialized) return;
            initialized = true;
            transparent = new GUIStyle();
            transparent.normal.background = Texture2D.blackTexture;
        }

        private static GUIStyle transparent;

        private static bool newHold = false;
        private static float pressPos = 0;

        public static float Slider(Rect pos, float value, float minVal, float maxVal)
        {
            _init();
            newHold = GUI.RepeatButton(pos, "", transparent);
            GUI.Box(new Rect(pos.x, pos.y + pos.height / 4, pos.width, pos.height / 2), "");
            if (newHold) GUI.Box(new Rect(pos.x + (value - minVal) / (maxVal - minVal) * (pos.width - pos.height), pos.y, pos.height, pos.height), "");
            else GUI.Button(new Rect(pos.x + (value - minVal) / (maxVal - minVal) * (pos.width - pos.height), pos.y, pos.height, pos.height), "");
            int newHold1 = newHold ? 1 : 0;
            float val = Mathf.Clamp(value + (Input.mousePosition.x - pressPos) / (pos.width - pos.height) * (maxVal - minVal) * newHold1, minVal, maxVal);
            if (newHold) pressPos = Input.mousePosition.x;
            return val;
        }
    }
}
