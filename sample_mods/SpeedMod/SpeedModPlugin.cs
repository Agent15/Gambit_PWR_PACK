using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using Gambonanza.ModSdk;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gambonanza.SpeedMod
{
    /// <summary>
    /// Mod entry point. ModHost instantiates this and calls OnLoad once at game start.
    /// Implements IModLifecycle so it supports hot toggle from the in-game mod manager.
    /// </summary>
    public sealed class SpeedModMain : IMod, IModLifecycle
    {
        private Runner _runner;
        private IModContext _ctx;

        public void OnLoad(IModContext context)
        {
            _ctx = context;
            // Subscribe once. The handler short-circuits when the runner is null (i.e. disabled).
            context.OnSettingsOpened += OnSettingsOpened;
            context.LogLine("loaded (idle).");
        }

        public void OnEnable()
        {
            if (_runner != null) return;
            try
            {
                var go = new GameObject("__SpeedModRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                _runner = go.AddComponent<Runner>();
                _runner.Bind(_ctx);
                _ctx?.LogLine("online.");
            }
            catch (Exception ex) { _ctx?.LogLine("OnEnable failed: " + ex); }
        }

        public void OnDisable()
        {
            try
            {
                if (_runner != null)
                {
                    _runner.TearDown();
                    UnityEngine.Object.Destroy(_runner.gameObject);
                    _runner = null;
                }
                _ctx?.LogLine("disabled.");
            }
            catch (Exception ex) { _ctx?.LogLine("OnDisable failed: " + ex); }
        }

        private void OnSettingsOpened(MonoBehaviour settingsCanvas)
        {
            if (_runner == null) return; // disabled — nothing to do
            try { _runner.InjectSettingsRow(settingsCanvas); }
            catch (Exception ex) { _ctx?.LogLine("OnSettingsOpened failed: " + ex); }
        }
    }

    public static class Speed
    {
        public static readonly float[] Steps = { 1f, 1.5f, 2f, 3f, 4f };
        public static int Index;
        public static float Current => Steps[Mathf.Clamp(Index, 0, Steps.Length - 1)];
        public static string Label => $"{Current:0.##}x";

        public static void Next() { Index = (Index + 1) % Steps.Length; Save(); }
        public static void Prev() { Index = (Index - 1 + Steps.Length) % Steps.Length; Save(); }

        public static void Save()
        {
            try { PlayerPrefs.SetInt("SpeedMod.Index", Index); PlayerPrefs.Save(); } catch { }
        }

        public static void Load()
        {
            try { Index = Mathf.Clamp(PlayerPrefs.GetInt("SpeedMod.Index", 0), 0, Steps.Length - 1); } catch { }
        }
    }

    /// <summary>
    /// Persistent runner. Watches Time.timeScale every LateUpdate; when the game
    /// writes a new non-zero value, multiplies it by the user's chosen speed.
    /// Pause (0) and our own writes are passed through.
    /// </summary>
    [DefaultExecutionOrder(int.MaxValue)]
    internal sealed class Runner : MonoBehaviour
    {
        private float _lastWrittenByUs = -1f;

        // Cap the physics fixed-step rate to its original real-time frequency.
        // Without this, Time.timeScale=4 makes Unity fire ~4x more FixedUpdate /
        // Physics2D.Simulate ticks per real second, burning CPU on idle scenes.
        private float _baseFixedDelta = 0.02f;

        // Settings injection state
        private const string InjectedRowName = "SpeedMod_AnimationSpeedRow";
        private GameObject _injectedRow;
        private TMP_Text _injectedTitle;
        private TMP_Text _injectedValue;
        private IModContext _ctx;

        public void Bind(IModContext ctx) => _ctx = ctx;

        private void Log(string s) { try { _ctx?.LogLine(s); } catch { Debug.Log("[SpeedMod] " + s); } }

        /// <summary>Called from SpeedModMain.OnDisable before destroying this GameObject.</summary>
        public void TearDown()
        {
            try { Time.timeScale = 1f; DOTween.timeScale = 1f; Time.fixedDeltaTime = _baseFixedDelta; } catch { }
            _lastWrittenByUs = -1f;
            if (_injectedRow != null)
            {
                try { Destroy(_injectedRow); } catch { }
                _injectedRow = null;
                _injectedTitle = null;
                _injectedValue = null;
            }
        }

        private void Awake()
        {
            _baseFixedDelta = Time.fixedDeltaTime;
            Speed.Load();
            ApplyImmediate();
        }

        private void LateUpdate()
        {
            float t = Time.timeScale;
            if (t <= 0f) return;
            if (Mathf.Approximately(t, _lastWrittenByUs)) return;
            float target = t * Speed.Current;
            Time.timeScale = target;
            _lastWrittenByUs = target;
            DOTween.timeScale = Speed.Current;
        }

        private void ApplyImmediate()
        {
            if (Time.timeScale > 0f)
            {
                Time.timeScale = Speed.Current;
                _lastWrittenByUs = Speed.Current;
            }
            DOTween.timeScale = Speed.Current;
            // Rescale physics step so it ticks at the original real-time frequency.
            Time.fixedDeltaTime = _baseFixedDelta * Speed.Current;
        }

        // ---- Settings modal injection ---------------------------------------

        public void InjectSettingsRow(MonoBehaviour settingsCanvas)
        {
            if (settingsCanvas == null) return;
            var t = settingsCanvas.GetType();

            // 1. Reflect title + value text references — the only landmarks we trust.
            var titleField = t.GetField("m_ControlsTitle",   BindingFlags.NonPublic | BindingFlags.Instance);
            var valueField = t.GetField("m_CurrentControls", BindingFlags.NonPublic | BindingFlags.Instance);
            if (titleField == null || valueField == null)
            {
                Log("Controls fields missing on SettingsCanvas; abort inject.");
                return;
            }

            var titleText = titleField.GetValue(settingsCanvas) as TMP_Text;
            var valueText = valueField.GetValue(settingsCanvas) as TMP_Text;
            if (titleText == null || valueText == null)
            {
                Log("Controls title/value text references null; abort inject.");
                return;
            }

            // 2. Common ancestor of title + value = the inner cell GameObject.
            Transform innerCell = FindCommonAncestor(new[] { titleText.transform, valueText.transform });
            if (innerCell == null || innerCell.parent == null)
            {
                Log("Could not derive Controls cell from title/value common ancestor.");
                return;
            }
            if (innerCell == titleText.transform || innerCell == valueText.transform)
                innerCell = innerCell.parent;

            // The inner cell sits alone inside a wrapper that participates in the gameplay tab's
            // vertical layout. We must clone the wrapper so spacing/layout is preserved.
            Transform sourceWrapper = innerCell.parent;
            if (sourceWrapper == null || sourceWrapper.parent == null)
            {
                Log("Controls wrapper has no parent; abort.");
                return;
            }
            Transform parent = sourceWrapper.parent;

            // 3. Locate arrows by name within the inner cell. The decompiled prefab uses
            //    Left_Arrow / Right_Arrow GameObjects with custom Selectable subclasses
            //    (ShadowButton, RotationButton, RewiredSelectable) plus EventTrigger.
            Transform leftArrow  = FindChildByName(innerCell, "Left_Arrow");
            Transform rightArrow = FindChildByName(innerCell, "Right_Arrow");
            if (leftArrow == null || rightArrow == null)
            {
                Log($"Could not locate Left_Arrow/Right_Arrow under '{innerCell.name}'; dump:");
                DumpHierarchy(innerCell, "    cell> ", 0, 3);
                return;
            }

            // 4. Record paths from the WRAPPER (the thing we'll clone) down to each landmark.
            var titlePath = PathFromAncestor(sourceWrapper, titleText.transform);
            var valuePath = PathFromAncestor(sourceWrapper, valueText.transform);
            var leftPath  = PathFromAncestor(sourceWrapper, leftArrow);
            var rightPath = PathFromAncestor(sourceWrapper, rightArrow);

            // 5. Idempotency.
            var existing = parent.Find(InjectedRowName);
            if (existing != null)
            {
                _injectedRow   = existing.gameObject;
                _injectedTitle = NavigatePath(_injectedRow.transform, titlePath)?.GetComponent<TMP_Text>();
                _injectedValue = NavigatePath(_injectedRow.transform, valuePath)?.GetComponent<TMP_Text>();
                RefreshInjectedRow();
                return;
            }

            // 6. Clone the wrapper and place it immediately after the source wrapper.
            _injectedRow = UnityEngine.Object.Instantiate(sourceWrapper.gameObject, parent);
            _injectedRow.name = InjectedRowName;
            _injectedRow.transform.SetSiblingIndex(sourceWrapper.GetSiblingIndex() + 1);

            Transform clonedTitle = NavigatePath(_injectedRow.transform, titlePath);
            Transform clonedValue = NavigatePath(_injectedRow.transform, valuePath);
            Transform clonedLeft  = NavigatePath(_injectedRow.transform, leftPath);
            Transform clonedRight = NavigatePath(_injectedRow.transform, rightPath);

            _injectedTitle = clonedTitle?.GetComponent<TMP_Text>();
            _injectedValue = clonedValue?.GetComponent<TMP_Text>();

            // 7. Strip every interactive component on the clone. Inspector-persistent
            //    UnityEvent listeners, EventTriggers, and any Selectable subclass might
            //    still hold references back into game state on the original instance.
            int stripped = 0;
            foreach (var s in _injectedRow.GetComponentsInChildren<Selectable>(true).ToArray())
            { UnityEngine.Object.DestroyImmediate(s); stripped++; }
            foreach (var et in _injectedRow.GetComponentsInChildren<EventTrigger>(true).ToArray())
            { UnityEngine.Object.DestroyImmediate(et); stripped++; }
            // Catch custom MonoBehaviours by type-name keyword (ShadowButton, RotationButton, SelectFeedback, etc).
            foreach (var mb in _injectedRow.GetComponentsInChildren<MonoBehaviour>(true).ToArray())
            {
                if (mb == null) continue;
                var n = mb.GetType().Name;
                if (n.Contains("Button") || n.Contains("Feedback") || n.Contains("Selectable") || n.Contains("Rewired"))
                { UnityEngine.Object.DestroyImmediate(mb); stripped++; }
            }

            // 8. Reset arrow images to full brightness — destroying the Selectable left
            //    Image.color stuck on whatever tint was last applied (often a faded normal).
            foreach (var arrow in new[] { clonedLeft, clonedRight })
            {
                if (arrow == null) continue;
                var img = arrow.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }

            // 9. Attach fresh Buttons on the cloned arrow GameObjects.
            int wired = 0;
            if (clonedLeft  != null && AttachFreshButton(clonedLeft,  OnLeftPressed))  wired++;
            if (clonedRight != null && AttachFreshButton(clonedRight, OnRightPressed)) wired++;

            // 10. Shrink the injected row so the modal doesn't overflow into the bottom checkboxes.
            var srcRT = sourceWrapper.GetComponent<RectTransform>();
            var srcLE = sourceWrapper.GetComponent<LayoutElement>();
            float srcHeight = (srcLE != null && srcLE.preferredHeight > 0)
                ? srcLE.preferredHeight
                : (srcRT != null ? srcRT.rect.height : 60f);
            var injLE = _injectedRow.GetComponent<LayoutElement>() ?? _injectedRow.AddComponent<LayoutElement>();
            injLE.preferredHeight = srcHeight * 0.6f;
            injLE.minHeight       = srcHeight * 0.6f;

            RefreshInjectedRow();
            Log(
                $"Injected '{InjectedRowName}' under '{parent.name}' " +
                $"(after '{sourceWrapper.name}' at idx {sourceWrapper.GetSiblingIndex() + 1}); " +
                $"stripped {stripped} interactive comps, wired {wired}/2 arrows.");
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindChildByName(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        private void OnLeftPressed()
        {
            Speed.Prev();
            ApplyImmediate();
            RefreshInjectedRow();
            Log($"Speed -> {Speed.Label} (left arrow)");
        }

        private void OnRightPressed()
        {
            Speed.Next();
            ApplyImmediate();
            RefreshInjectedRow();
            Log($"Speed -> {Speed.Label} (right arrow)");
        }

        private static bool AttachFreshButton(Transform target, UnityEngine.Events.UnityAction onClick)
        {
            if (target == null) return false;
            var go = target.gameObject;

            // Need a Graphic with raycastTarget enabled to receive clicks.
            var graphic = go.GetComponent<Graphic>();
            if (graphic == null) graphic = go.GetComponentInChildren<Graphic>(true);
            if (graphic != null) graphic.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.interactable = true;
            if (graphic != null) btn.targetGraphic = graphic;
            // ColorTint gives clear hover/press feedback so the arrows feel reactive.
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
            btn.onClick.AddListener(onClick);
            return true;
        }

        private void RefreshInjectedRow()
        {
            if (_injectedRow == null) return;
            try
            {
                if (_injectedTitle != null) _injectedTitle.text = "Animation Speed";
                if (_injectedValue != null) _injectedValue.text = Speed.Label;
            }
            catch { /* row may have been destroyed; reinit on next OnEnable */ }
        }

        private void DumpHierarchy(Transform root, string prefix, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            var comps = root.GetComponents<Component>();
            var names = string.Join(",", comps.Where(c => c != null).Select(c => c.GetType().Name));
            Log($"{prefix}{new string(' ', depth * 2)}{root.name}  [{names}]");
            for (int i = 0; i < root.childCount; i++)
                DumpHierarchy(root.GetChild(i), prefix, depth + 1, maxDepth);
        }

        // ---- Hierarchy helpers ----------------------------------------------

        private static Transform FindCommonAncestor(IList<Transform> transforms)
        {
            if (transforms == null || transforms.Count == 0) return null;

            var ancestors = new HashSet<Transform>();
            for (var t = transforms[0]; t != null; t = t.parent) ancestors.Add(t);

            for (int i = 1; i < transforms.Count; i++)
            {
                var found = new HashSet<Transform>();
                for (var t = transforms[i]; t != null; t = t.parent)
                    if (ancestors.Contains(t)) found.Add(t);
                ancestors = found;
                if (ancestors.Count == 0) return null;
            }

            // Of the surviving ancestors, return the deepest one (longest path from root).
            return ancestors.OrderByDescending(a => DepthOf(a)).FirstOrDefault();
        }

        private static int DepthOf(Transform t)
        {
            int d = 0;
            while (t.parent != null) { t = t.parent; d++; }
            return d;
        }

        /// <summary>
        /// Returns the chain of child indices from `ancestor` to `descendant`.
        /// Empty list = same transform. Null = not actually an ancestor.
        /// </summary>
        private static List<int> PathFromAncestor(Transform ancestor, Transform descendant)
        {
            if (ancestor == null || descendant == null) return null;
            var stack = new Stack<int>();
            var cur = descendant;
            while (cur != null && cur != ancestor)
            {
                stack.Push(cur.GetSiblingIndex());
                cur = cur.parent;
            }
            return cur == ancestor ? stack.ToList() : null;
        }

        private static Transform NavigatePath(Transform start, List<int> path)
        {
            if (start == null || path == null) return null;
            var cur = start;
            foreach (var idx in path)
            {
                if (idx < 0 || idx >= cur.childCount) return null;
                cur = cur.GetChild(idx);
            }
            return cur;
        }
    }
}
