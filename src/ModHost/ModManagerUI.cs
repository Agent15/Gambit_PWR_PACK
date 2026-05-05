using System;
using System.Collections.Generic;
using Gambonanza.GameUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// In-game mod manager modal. All chrome + button styling is delegated to
    /// Gambonanza.GameUI.Pixel so the modal uses the game's actual UI
    /// components (cloned, stripped, rewired) instead of hand-rolled approximations.
    /// </summary>
    internal sealed class ModManagerUI : MonoBehaviour
    {
        private static ModManagerUI _instance;

        private Modal _modal;
        private readonly Dictionary<string, PixelCheckbox> _checkboxes = new Dictionary<string, PixelCheckbox>();

        // Row uses a slightly darker / more saturated cream than the modal
        // background so each row reads as a distinct cell instead of melting
        // into the panel.
        private static readonly Color RowBg      = new Color(0.86f, 0.78f, 0.55f, 1f);
        private static readonly Color RowShadow  = new Color(0.30f, 0.13f, 0.10f, 0.55f);
        private static readonly Color DarkText   = new Color(0.36f, 0.13f, 0.11f, 1f);
        private static readonly Color SubtleText = new Color(0.36f, 0.13f, 0.11f, 0.7f);

        private void Update()
        {
            // Escape hatch: pressing Escape closes the modal so the user is never
            // trapped if cloned chrome or input routing misbehaves.
            if (_modal != null && _modal.Root != null && _modal.Root.activeSelf
                && Input.GetKeyDown(KeyCode.Escape))
            {
                ModHost.LogLine("ModManagerUI: Escape pressed, hiding modal");
                _modal.Hide();
            }
        }

        public static void Show()
        {
            ModHost.LogLine("ModManagerUI.Show: entry");
            try
            {
                if (_instance == null)
                {
                    ModHost.LogLine("ModManagerUI.Show: building first instance");
                    var go = new GameObject("__ModHostManagerUI");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideAndDontSave;
                    var inst = go.AddComponent<ModManagerUI>();
                    try
                    {
                        inst.Build();
                    }
                    catch (Exception ex)
                    {
                        ModHost.LogLine("ModManagerUI.Build failed; tearing down half-init: " + ex);
                        UnityEngine.Object.Destroy(go);
                        return;
                    }
                    if (inst._modal == null)
                    {
                        ModHost.LogLine("ModManagerUI.Build returned null modal; aborting.");
                        UnityEngine.Object.Destroy(go);
                        return;
                    }
                    _instance = inst;
                }

                ModHost.LogLine("ModManagerUI.Show: calling modal.Show()");
                _instance._modal.Show();
                _instance.Refresh(rescan: false);
                ModHost.LogLine("ModManagerUI.Show: done");
            }
            catch (Exception ex)
            {
                ModHost.LogLine("ModManagerUI.Show: unexpected exception: " + ex);
            }
        }

        private void Build()
        {
            ModHost.LogLine("ModManagerUI.Build: creating modal");
            _modal = Pixel.CreateModal("ModHost_Manager", "MODS");
            if (_modal == null) { ModHost.LogLine("ModManagerUI.Build: Pixel.CreateModal returned null!"); return; }

            ModHost.LogLine("ModManagerUI.Build: wiring toolbar");
            _modal.AddToolbarButton("REFRESH",     () => Refresh(rescan: true));
            _modal.AddToolbarButton("OPEN FOLDER", ModHost.OpenModsFolderInFinder);
            _modal.AddToolbarButton("CLOSE",       _modal.Hide);
            ModHost.LogLine("ModManagerUI.Build: done");
        }

        private void Refresh(bool rescan)
        {
            string note = null;
            if (rescan)
            {
                int newlyLoaded = ModHost.Rescan();
                note = newlyLoaded > 0 ? $"Loaded {newlyLoaded} new mod(s)." : "No new mods found.";
            }

            // Wipe and rebuild rows.
            for (int i = _modal.Content.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_modal.Content.GetChild(i).gameObject);
            _checkboxes.Clear();

            var mods = ModHost.AllMods();
            if (mods.Count == 0)
            {
                var empty = Pixel.CreateLabel(_modal.Content,
                    "No mods installed.\nDrop a folder containing mod.json + .dll into the Mods folder, then click Refresh.",
                    18, SubtleText, TextAlignmentOptions.Center);
                SetHeight(empty.gameObject, 80);
            }
            else
            {
                for (int i = 0; i < mods.Count; i++) AddModRow(mods[i]);
            }

            if (_modal.Status != null)
                _modal.Status.text = note ?? $"{mods.Count} mod(s) loaded.";
        }

        private void AddModRow(ModRegistry.LoadedMod mod)
        {
            var row = NewChild(_modal.Content, $"Row_{mod.Manifest.id}",
                typeof(Image), typeof(HorizontalLayoutGroup));
            row.GetComponent<Image>().color = RowBg;
            // Drop-shadow under the cream row, mirroring the Settings panel
            // rows' raised look. Shadow renders the duplicated mesh BEHIND the
            // graphic, so the cream cell still sits on top.
            var rowShadow = row.AddComponent<Shadow>();
            rowShadow.effectColor    = RowShadow;
            rowShadow.effectDistance = new Vector2(0f, -4f);
            rowShadow.useGraphicAlpha = true;
            SetHeight(row, 64);

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 6, 6);
            hlg.spacing = 12;
            hlg.childAlignment      = TextAnchor.MiddleLeft;
            hlg.childControlWidth   = true;
            hlg.childControlHeight  = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // Left column: name + meta
            var col = NewChild(row.transform, "Info",
                typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var colVlg = col.GetComponent<VerticalLayoutGroup>();
            colVlg.spacing = 2;
            colVlg.childControlWidth = true; colVlg.childControlHeight = false;
            colVlg.childForceExpandWidth = true; colVlg.childForceExpandHeight = false;
            col.GetComponent<LayoutElement>().flexibleWidth = 1;

            var nameLabel = Pixel.CreateLabel(col.transform,
                string.IsNullOrEmpty(mod.Manifest.name) ? mod.Manifest.id : mod.Manifest.name,
                22, DarkText, TextAlignmentOptions.MidlineLeft);
            SetHeight(nameLabel.gameObject, 26);

            var metaParts = new List<string>();
            if (!string.IsNullOrEmpty(mod.Manifest.version)) metaParts.Add("v" + mod.Manifest.version);
            if (!string.IsNullOrEmpty(mod.Manifest.author))  metaParts.Add("by " + mod.Manifest.author);
            if (mod.Lifecycle == null)                       metaParts.Add("(restart needed to toggle)");

            var meta = Pixel.CreateLabel(col.transform,
                string.Join("   •   ", metaParts), 14, SubtleText, TextAlignmentOptions.MidlineLeft);
            SetHeight(meta.gameObject, 18);

            // Right: small game-styled checkbox (cloned from Settings menu
            // Toggle). The widget self-sizes to the captured box dimensions —
            // do NOT override its LayoutElement here, or the checkmark child
            // will stretch and render as a solid filled rectangle when on.
            var modCapture = mod;
            var checkbox = Pixel.CreateCheckbox(row.transform, "", mod.IsActive,
                isOn => OnTogglePressed(modCapture, isOn));
            _checkboxes[mod.Manifest.id] = checkbox;
        }

        private void OnTogglePressed(ModRegistry.LoadedMod mod, bool wantOn)
        {
            string error;
            bool ok = wantOn
                ? ModHost.TryEnable(mod.Manifest.id, out error)
                : ModHost.TryDisable(mod.Manifest.id, out error);

            if (!ok)
            {
                if (_modal.Status != null) _modal.Status.text = "Error: " + (error ?? "unknown");
                // Snap checkbox back to actual state.
                if (_checkboxes.TryGetValue(mod.Manifest.id, out var c)) c.Set(mod.IsActive, notify: false);
                return;
            }

            // Successfully toggled — rebuild rows so meta lines reflect new state cleanly.
            Refresh(rescan: false);
            if (!string.IsNullOrEmpty(error) && _modal.Status != null) _modal.Status.text = error;
        }

        // ----- tiny shared primitives -------------------------------------

        private static GameObject NewChild(Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            foreach (var c in components) go.AddComponent(c);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetHeight(GameObject go, float h)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = h; le.minHeight = h;
        }
    }
}
