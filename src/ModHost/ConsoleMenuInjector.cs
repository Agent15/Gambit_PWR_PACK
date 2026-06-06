using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gambonanza.ModHost
{
    /// <summary>Adds a visible home-screen entry point for the console.</summary>
    internal sealed class ConsoleMenuInjector
    {
        private const string InjectedName = "ModHost_OpenConsoleButton";
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public void InjectButton(MonoBehaviour canvasMenu)
        {
            if (canvasMenu == null) return;

            var t = canvasMenu.GetType();
            var report = t.GetField("m_ReportContainer", F)?.GetValue(canvasMenu) as Transform;
            if (report == null || report.parent == null) return;
            if (report.parent.Find(InjectedName) != null) return;

            var visible = t.GetField("m_ReportVisiblePlace", F)?.GetValue(canvasMenu) as Transform;
            var clone = UnityEngine.Object.Instantiate(report.gameObject, report.parent);
            clone.name = InjectedName;
            clone.SetActive(true);
            clone.transform.SetSiblingIndex(report.GetSiblingIndex() + 1);
            clone.transform.localPosition = (visible != null ? visible.localPosition : report.localPosition) + Vector3.down * GetOffset(report);
            clone.transform.localRotation = report.localRotation;
            clone.transform.localScale = report.localScale;

            foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true))
                text.text = "Console\nF10";

            RewireConsoleButton(clone);
            ModHost.LogLine($"Injected '{InjectedName}' console button from report-button template.");
        }

        private static float GetOffset(Transform report)
        {
            var rt = report as RectTransform;
            if (rt != null && rt.rect.height > 0f) return rt.rect.height * 1.85f;
            return 165f;
        }

        private static void RewireConsoleButton(GameObject root)
        {
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true).ToArray())
                UnityEngine.Object.DestroyImmediate(selectable);
            foreach (var trigger in root.GetComponentsInChildren<EventTrigger>(true).ToArray())
                UnityEngine.Object.DestroyImmediate(trigger);
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true).ToArray())
            {
                if (mb == null || mb is TMP_Text) continue;
                var n = mb.GetType().Name;
                if (n == "LinkOpener" || n == "ShadowButton" || n.Contains("Selectable") || n.Contains("Rewired"))
                    UnityEngine.Object.DestroyImmediate(mb);
            }

            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = true;

            var graphic = root.GetComponent<Graphic>() ?? root.GetComponentInChildren<Graphic>(true);
            var button = root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            if (graphic != null) button.targetGraphic = graphic;
            button.onClick.AddListener(() => ModConsole.Instance?.Open());
            button.interactable = true;

            var hover = root.AddComponent<ConsoleMenuButtonBehaviour>();
            hover.Bind(root.transform.localScale);
        }
    }

    internal sealed class ConsoleMenuButtonBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;

        public void Bind(Vector3 baseScale)
        {
            _baseScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
            _targetScale = _baseScale;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * 14f);
        }

        public void OnPointerEnter(PointerEventData eventData) => _targetScale = _baseScale * 1.1f;
        public void OnPointerExit(PointerEventData eventData) => _targetScale = _baseScale;
        public void OnPointerDown(PointerEventData eventData) => _targetScale = _baseScale * 1.16f;
        public void OnPointerUp(PointerEventData eventData) => _targetScale = _baseScale * 1.1f;
    }
}
