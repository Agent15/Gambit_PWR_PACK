using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gambonanza.GameUI
{
    internal static class Log
    {
        public static void Line(string s)
        {
            try { Debug.Log("[GameUI] " + s); } catch { }
        }
    }

    internal static class Strip
    {
        /// <summary>
        /// Remove every component on <paramref name="root"/> (and children) that would
        /// route input back into the original game object: anything in the Blukulele
        /// namespace, Selectable subclasses, EventTriggers, and custom MonoBehaviours
        /// whose type name hints at button/feedback/rewired wiring. Returns the count.
        /// </summary>
        public static int Interactives(GameObject root)
        {
            if (root == null) return 0;
            int n = 0;
            foreach (var s in root.GetComponentsInChildren<Selectable>(true).ToArray())
            { if (s != null) { UnityEngine.Object.DestroyImmediate(s); n++; } }

            foreach (var et in root.GetComponentsInChildren<EventTrigger>(true).ToArray())
            { if (et != null) { UnityEngine.Object.DestroyImmediate(et); n++; } }

            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true).ToArray())
            {
                if (mb == null) continue;
                var fullName = mb.GetType().FullName ?? "";
                var typeName = mb.GetType().Name;
                if (fullName.StartsWith("Blukulele")
                    || typeName.Contains("Button")
                    || typeName.Contains("Feedback")
                    || typeName.Contains("Selectable")
                    || typeName.Contains("Rewired"))
                {
                    UnityEngine.Object.DestroyImmediate(mb);
                    n++;
                }
            }
            return n;
        }

        /// <summary>
        /// Reset every Image's color to white (and enable raycastTarget). The
        /// ColorTint transition on the original Selectable left whatever tint was
        /// last applied — usually a faded "normal" — so without this clones look
        /// disabled even though they're interactive.
        /// </summary>
        public static void ResetImageColors(GameObject root)
        {
            if (root == null) return;
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                img.color = Color.white;
                img.raycastTarget = true;
            }
        }
    }

    internal static class ButtonStyle
    {
        public static void ApplyDefaultColors(Button btn)
        {
            if (btn == null) return;
            btn.transition = Selectable.Transition.ColorTint;
            var c = btn.colors;
            c.normalColor      = Color.white;
            c.highlightedColor = new Color(1f,    0.78f, 0.55f, 1f);
            c.pressedColor     = new Color(0.65f, 0.45f, 0.30f, 1f);
            c.selectedColor    = new Color(1f,    0.85f, 0.65f, 1f);
            c.disabledColor    = new Color(0.5f,  0.5f,  0.5f,  0.5f);
            c.colorMultiplier  = 1f;
            c.fadeDuration     = 0.08f;
            btn.colors = c;
        }
    }

    internal static class Bucket
    {
        private static GameObject _root;

        /// <summary>
        /// Hidden DontDestroyOnLoad container that holds inert template clones.
        /// Created on demand. Templates parented under here survive scene loads
        /// and don't show up in the active scene.
        /// </summary>
        public static GameObject Root()
        {
            if (_root != null) return _root;
            _root = new GameObject("__GameUI_TemplateBucket");
            _root.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _root.hideFlags = HideFlags.HideAndDontSave;
            return _root;
        }
    }

    internal static class Safe
    {
        public static void Invoke(Action a)
        {
            if (a == null) return;
            try { a(); } catch (Exception ex) { Log.Line("callback threw: " + ex); }
        }
    }
}
