using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// In-game console overlay. Built programmatically — no vanilla cloning, so
    /// it's resilient to game updates and survives scene loads via
    /// DontDestroyOnLoad. Visual style is deliberately neutral / dev-tool-y so it
    /// doesn't compete with the cream chess chrome.
    ///
    /// Toggle: F1 by default, configurable via env var GAMBONANZA_CONSOLE_KEY.
    /// Pause: Time.timeScale = 0 while open (toggle via `pause-on-open off`).
    /// </summary>
    internal sealed class ModConsoleUI : MonoBehaviour
    {
        // ----- one-shot bootstrapping --------------------------------------

        private static ModConsoleUI _instance;
        private static readonly List<Action> s_perFrameSubscribers = new List<Action>();

        public static void SpawnOnce(ModConsole console)
        {
            if (_instance != null) return;
            var go = new GameObject("__ModHost_Console");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<ModConsoleUI>();
            _instance._console = console;
            _instance.Build();
            _instance.SetVisible(false);
            console.OnOpenRequested  += () => _instance.SetVisible(true);
            console.OnCloseRequested += () => _instance.SetVisible(false);
        }

        /// <summary>External Update subscribers (used by LogTail to pump queued
        /// lines on the main thread without spawning their own MonoBehaviour).</summary>
        public static void RegisterUpdate(Action a)   { if (a != null && !s_perFrameSubscribers.Contains(a)) s_perFrameSubscribers.Add(a); }
        public static void UnregisterUpdate(Action a) { if (a != null) s_perFrameSubscribers.Remove(a); }

        // ----- toggle key resolution ---------------------------------------

        private static KeyCode ResolveToggle(string envName, KeyCode fallback)
        {
            try
            {
                var v = Environment.GetEnvironmentVariable(envName);
                if (!string.IsNullOrEmpty(v) && Enum.TryParse<KeyCode>(v, true, out var k)) return k;
            }
            catch { }
            return fallback;
        }

        private KeyCode _toggleKey  = ResolveToggle("GAMBONANZA_CONSOLE_KEY",  KeyCode.F1);
        private KeyCode _toggleKey2 = ResolveToggle("GAMBONANZA_CONSOLE_KEY2", KeyCode.BackQuote);

        // ----- state -------------------------------------------------------

        private ModConsole _console;
        private Canvas _canvas;
        private GameObject _root;
        private TMP_Text _scroll;
        private ScrollRect _scrollRect;
        private TMP_InputField _input;
        private TMP_Text _suggestions;

        private bool _pauseOnOpen = true;
        private float _savedTimeScale = 1f;
        private int _lastRenderedRevision = -1;

        // Tab-cycle state. Reset whenever the user edits the input outside of Tab.
        private List<string> _tabCandidates;
        private int _tabIndex;
        private string _tabBaseText;     // input value snapshot when Tab cycle began
        private int _tabBaseCursor;

        // Command history.
        private readonly List<string> _history = new List<string>();
        private int _historyCursor = -1;     // -1 = "live" input (not navigating)
        private string _liveBuffer;          // saved when user starts navigating history

        // ----- visibility --------------------------------------------------

        private void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
            _console.IsOpen = visible;
            if (visible)
            {
                if (_pauseOnOpen)
                {
                    _savedTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }
                _input.text = "";
                _input.ActivateInputField();
                _input.Select();
                ResetTabCycle();
                _historyCursor = -1;
                Render();
            }
            else
            {
                if (_pauseOnOpen) Time.timeScale = _savedTimeScale;
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _input.gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void Update()
        {
            // Toggle. We check Input.GetKeyDown directly — even when the input
            // field has focus, Unity still surfaces these events to our Update.
            // The InputField's own char processing swallows the printable chars
            // for the toggle key, so we early-out if the key would otherwise
            // type into the field.
            bool toggle = Input.GetKeyDown(_toggleKey) || Input.GetKeyDown(_toggleKey2);
            if (toggle) _console.Toggle();

            if (_console.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) _console.Close();
                HandleAutocompleteKey();
                HandleHistoryKeys();
                if (_lastRenderedRevision != _console.Revision) Render();
            }

            // Pump per-frame subscribers (e.g. LogTail). Done unconditionally so
            // tail-on works whether the console is visible or not.
            for (int i = 0; i < s_perFrameSubscribers.Count; i++)
            {
                try { s_perFrameSubscribers[i]?.Invoke(); }
                catch (Exception ex) { Debug.LogError("[ModHost] console update subscriber threw: " + ex); }
            }
        }

        // ----- key handlers ------------------------------------------------

        private void HandleAutocompleteKey()
        {
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            // If we have a stable cycle going, advance it.
            if (_tabCandidates != null && _tabCandidates.Count > 0
                && string.Equals(_input.text, _lastAppliedText, StringComparison.Ordinal))
            {
                _tabIndex = (_tabIndex + 1) % _tabCandidates.Count;
                ApplyCandidate(_tabCandidates[_tabIndex]);
                RenderSuggestions();
                return;
            }

            // First Tab press: compute candidates from the current input.
            int caret = _input.caretPosition;
            var cands = _console.Complete(_input.text, caret);
            if (cands == null || cands.Count == 0)
            {
                _tabCandidates = null;
                _suggestions.text = "(no completions)";
                return;
            }

            _tabBaseText = _input.text;
            _tabBaseCursor = caret;

            if (cands.Count == 1)
            {
                ApplyCandidate(cands[0]);
                _tabCandidates = null;
                _suggestions.text = "";
                return;
            }

            // Multiple candidates: extend to longest common prefix, list them.
            string lcp = LongestCommonPrefix(cands);
            // Compute the partial we're completing so we know whether LCP is an
            // improvement worth applying before the cycle starts.
            string partial = ExtractCurrentToken(_input.text, caret);
            if (lcp.Length > partial.Length)
                ApplyCandidate(lcp);

            _tabCandidates = cands.ToList();
            _tabIndex = -1;     // first Enter-Tab press cycles to index 0
            RenderSuggestions();
        }

        private void HandleHistoryKeys()
        {
            // Up/Down navigate command history. We only act on KeyDown so caret
            // navigation inside the input still works (left/right arrows).
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (_history.Count == 0) return;
                if (_historyCursor < 0) _liveBuffer = _input.text;
                _historyCursor = Mathf.Min(_historyCursor + 1, _history.Count - 1);
                _input.text = _history[_history.Count - 1 - _historyCursor];
                _input.caretPosition = _input.text.Length;
                ResetTabCycle();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (_historyCursor < 0) return;
                _historyCursor--;
                _input.text = _historyCursor < 0 ? (_liveBuffer ?? "") : _history[_history.Count - 1 - _historyCursor];
                _input.caretPosition = _input.text.Length;
                ResetTabCycle();
            }
        }

        private void OnInputValueChanged(string _)
        {
            // The user typed something between Tab presses → cancel the cycle.
            if (_input.text != _lastAppliedText) ResetTabCycle();
        }

        private void OnInputSubmit(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                // Empty submit → clear and stay open.
                _input.text = "";
                _input.ActivateInputField();
                return;
            }
            _history.Add(text);
            // Cap history at 100 to keep memory steady.
            while (_history.Count > 100) _history.RemoveAt(0);
            _historyCursor = -1;
            _liveBuffer = null;
            _console.Submit(text);
            _input.text = "";
            _input.ActivateInputField();
            ResetTabCycle();
            Render();
        }

        // ----- autocomplete helpers ----------------------------------------

        private string _lastAppliedText;

        private void ApplyCandidate(string candidate)
        {
            var (text, cursor) = ModConsole.ApplyCompletion(_input.text, _input.caretPosition, candidate);
            _input.text = text;
            _input.caretPosition = cursor;
            _input.ActivateInputField();
            _lastAppliedText = text;
        }

        private void ResetTabCycle()
        {
            _tabCandidates = null;
            _tabIndex = -1;
            _tabBaseText = null;
            _suggestions.text = "";
            _lastAppliedText = _input.text;
        }

        private void RenderSuggestions()
        {
            if (_tabCandidates == null || _tabCandidates.Count == 0)
            {
                _suggestions.text = "";
                return;
            }
            // Highlight the selected candidate; show first 12.
            var sb = new System.Text.StringBuilder();
            int show = Math.Min(_tabCandidates.Count, 12);
            for (int i = 0; i < show; i++)
            {
                if (i == _tabIndex) sb.Append("<color=#ffd54f>[").Append(_tabCandidates[i]).Append("]</color>");
                else                sb.Append(_tabCandidates[i]);
                if (i < show - 1) sb.Append("  ");
            }
            if (_tabCandidates.Count > show) sb.Append($"  …(+{_tabCandidates.Count - show})");
            _suggestions.text = sb.ToString();
        }

        private static string LongestCommonPrefix(IReadOnlyList<string> ss)
        {
            if (ss.Count == 0) return "";
            string p = ss[0] ?? "";
            for (int i = 1; i < ss.Count; i++)
            {
                string s = ss[i] ?? "";
                int n = Math.Min(p.Length, s.Length);
                int k = 0;
                while (k < n && char.ToLowerInvariant(p[k]) == char.ToLowerInvariant(s[k])) k++;
                p = p.Substring(0, k);
                if (p.Length == 0) break;
            }
            return p;
        }

        private static string ExtractCurrentToken(string input, int cursor)
        {
            input ??= "";
            cursor = Math.Clamp(cursor, 0, input.Length);
            int start = cursor;
            while (start > 0 && !char.IsWhiteSpace(input[start - 1])) start--;
            int end = cursor;
            while (end < input.Length && !char.IsWhiteSpace(input[end])) end++;
            return input.Substring(start, end - start);
        }

        // ----- rendering ---------------------------------------------------

        private void Render()
        {
            _lastRenderedRevision = _console.Revision;
            var sb = new System.Text.StringBuilder();
            foreach (var line in _console.Lines)
            {
                sb.Append(ColorOpen(line.Color));
                sb.Append(EscapeRichText(line.Text));
                sb.Append(ColorClose(line.Color));
                sb.Append('\n');
            }
            _scroll.text = sb.ToString();
            // Scroll to bottom — content grows downwards in the scrollback.
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
        }

        private static string ColorOpen(ModSdk.ConsoleLineColor c)
        {
            switch (c)
            {
                case ModSdk.ConsoleLineColor.Info:  return "<color=#7ee0ff>";
                case ModSdk.ConsoleLineColor.Warn:  return "<color=#ffd54f>";
                case ModSdk.ConsoleLineColor.Error: return "<color=#ff7676>";
                case ModSdk.ConsoleLineColor.Echo:  return "<color=#aaaaaa>";
                default: return "";
            }
        }
        private static string ColorClose(ModSdk.ConsoleLineColor c)
            => c == ModSdk.ConsoleLineColor.Default ? "" : "</color>";

        private static string EscapeRichText(string s)
        {
            // The buffer holds raw user text — we don't want < to be parsed as a
            // TMP tag. Replace < with a non-breaking unicode entity that TMP
            // ignores. Right-angle bracket renders fine.
            return string.IsNullOrEmpty(s) ? "" : s.Replace("<", "<​");
        }

        // ----- build ------------------------------------------------------

        private void Build()
        {
            _root = new GameObject("Root", typeof(RectTransform));
            _root.transform.SetParent(transform, false);

            // Canvas above everything.
            var canvasGo = NewChild(_root.transform, "Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32760;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Top-half panel.
            var panel = NewChild(canvasGo.transform, "Panel", typeof(Image));
            var panelRT = (RectTransform)panel.transform;
            panelRT.anchorMin = new Vector2(0f, 0.5f);
            panelRT.anchorMax = new Vector2(1f, 1f);
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.92f);

            // Scrollback container (above the input bar).
            var scrollHost = NewChild(panel.transform, "ScrollHost", typeof(Image), typeof(ScrollRect));
            var scrollHostRT = (RectTransform)scrollHost.transform;
            scrollHostRT.anchorMin = new Vector2(0, 0); scrollHostRT.anchorMax = new Vector2(1, 1);
            scrollHostRT.offsetMin = new Vector2(12, 80);
            scrollHostRT.offsetMax = new Vector2(-12, -16);
            scrollHost.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // transparent
            _scrollRect = scrollHost.GetComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 32f;

            // Viewport (mask).
            var viewport = NewChild(scrollHost.transform, "Viewport", typeof(Image), typeof(Mask));
            var viewportRT = (RectTransform)viewport.transform;
            viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = viewportRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;
            _scrollRect.viewport = viewportRT;

            // Content (the TMP_Text itself sized to its content).
            var content = NewChild(viewport.transform, "Content",
                typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            var contentRT = (RectTransform)content.transform;
            contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0, 1);
            contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            _scrollRect.content = contentRT;

            _scroll = NewChild(content.transform, "Text", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            _scroll.fontSize = 16;
            _scroll.color = new Color(0.92f, 0.92f, 0.92f);
            _scroll.alignment = TextAlignmentOptions.TopLeft;
            _scroll.richText = true;
            _scroll.textWrappingMode = TextWrappingModes.Normal;
            _scroll.text = "";

            // Suggestions strip — sits between the scrollback and the input.
            var sugBar = NewChild(panel.transform, "Suggestions", typeof(Image));
            var sugRT = (RectTransform)sugBar.transform;
            sugRT.anchorMin = new Vector2(0, 0); sugRT.anchorMax = new Vector2(1, 0);
            sugRT.pivot = new Vector2(0.5f, 0);
            sugRT.anchoredPosition = new Vector2(0, 56);
            sugRT.sizeDelta = new Vector2(-24, 22);
            sugBar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            _suggestions = NewChild(sugBar.transform, "Text", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var sugTxtRT = (RectTransform)_suggestions.transform;
            sugTxtRT.anchorMin = Vector2.zero; sugTxtRT.anchorMax = Vector2.one;
            sugTxtRT.offsetMin = new Vector2(8, 0); sugTxtRT.offsetMax = Vector2.zero;
            _suggestions.fontSize = 14;
            _suggestions.color = new Color(0.85f, 0.85f, 0.85f);
            _suggestions.alignment = TextAlignmentOptions.MidlineLeft;
            _suggestions.richText = true;
            _suggestions.text = "";

            // Input bar (bottom of the panel).
            var inputBar = NewChild(panel.transform, "InputBar", typeof(Image));
            var ibRT = (RectTransform)inputBar.transform;
            ibRT.anchorMin = new Vector2(0, 0); ibRT.anchorMax = new Vector2(1, 0);
            ibRT.pivot = new Vector2(0.5f, 0);
            ibRT.anchoredPosition = new Vector2(0, 12);
            ibRT.sizeDelta = new Vector2(-24, 36);
            inputBar.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);

            var prompt = NewChild(inputBar.transform, "Prompt", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var promptRT = (RectTransform)prompt.transform;
            promptRT.anchorMin = new Vector2(0, 0.5f); promptRT.anchorMax = new Vector2(0, 0.5f);
            promptRT.pivot = new Vector2(0, 0.5f);
            promptRT.anchoredPosition = new Vector2(8, 0);
            promptRT.sizeDelta = new Vector2(28, 30);
            prompt.fontSize = 18;
            prompt.color = new Color(0.6f, 1f, 0.6f);
            prompt.alignment = TextAlignmentOptions.MidlineLeft;
            prompt.text = ">";

            // TMP InputField — built manually because we don't depend on the
            // game's pre-built input prefab.
            var inputGo = NewChild(inputBar.transform, "Input", typeof(Image), typeof(TMP_InputField));
            var inputRT = (RectTransform)inputGo.transform;
            inputRT.anchorMin = new Vector2(0, 0); inputRT.anchorMax = new Vector2(1, 1);
            inputRT.offsetMin = new Vector2(32, 4); inputRT.offsetMax = new Vector2(-8, -4);
            inputGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            _input = inputGo.GetComponent<TMP_InputField>();

            // TextArea + Text + Placeholder for TMP_InputField.
            var textArea = NewChild(inputGo.transform, "TextArea", typeof(RectMask2D));
            var taRT = (RectTransform)textArea.transform;
            taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(4, 0); taRT.offsetMax = Vector2.zero;

            var inputText = NewChild(textArea.transform, "Text", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var itRT = (RectTransform)inputText.transform;
            itRT.anchorMin = Vector2.zero; itRT.anchorMax = Vector2.one;
            itRT.offsetMin = itRT.offsetMax = Vector2.zero;
            inputText.fontSize = 18;
            inputText.color = new Color(0.95f, 0.95f, 0.95f);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            inputText.richText = false;     // user text is literal
            inputText.textWrappingMode = TextWrappingModes.NoWrap;

            var placeholder = NewChild(textArea.transform, "Placeholder", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var phRT = (RectTransform)placeholder.transform;
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = phRT.offsetMax = Vector2.zero;
            placeholder.fontSize = 18;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.text = "type a command — Tab to autocomplete, ↑/↓ for history";

            _input.textViewport   = (RectTransform)textArea.transform;
            _input.textComponent  = inputText;
            _input.placeholder    = placeholder;
            _input.lineType       = TMP_InputField.LineType.SingleLine;
            _input.restoreOriginalTextOnEscape = false;
            _input.onSubmit.AddListener(OnInputSubmit);
            _input.onValueChanged.AddListener(OnInputValueChanged);
            _input.text = "";
        }

        private static GameObject NewChild(Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            foreach (var c in components) go.AddComponent(c);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
