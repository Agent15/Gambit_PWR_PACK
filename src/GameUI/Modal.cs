using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gambonanza.GameUI
{
    /// <summary>
    /// A mod-owned modal that uses the game's Settings panel chrome when possible
    /// (cloned once and reused) and falls back to a programmatic cream-on-brown panel
    /// otherwise. Returned by <see cref="Pixel.CreateModal"/>.
    ///
    /// Lifecycle:
    ///   var modal = Pixel.CreateModal("MyMod_Modal", "MY MOD");
    ///   // populate modal.Content however you like
    ///   modal.AddToolbarButton("CLOSE", modal.Hide);
    ///   modal.Show();
    /// </summary>
    public sealed class Modal
    {
        /// <summary>The root GameObject (a Canvas if cloned from SettingsCanvas).</summary>
        public GameObject Root { get; }

        /// <summary>The empty area you should populate. Already has a VerticalLayoutGroup.</summary>
        public Transform Content { get; }

        /// <summary>The header label. Set <c>.text</c> to retitle.</summary>
        public TMP_Text Title { get; }

        /// <summary>Toolbar transform (under Root, anchored to bottom). Use <see cref="AddToolbarButton"/>.</summary>
        public Transform Toolbar { get; }

        /// <summary>Optional: status line shown above the toolbar. Set <c>.text</c> to display a message.</summary>
        public TMP_Text Status { get; }

        /// <summary>Fired after <see cref="Hide"/> runs. Useful for tearing down listeners.</summary>
        public event Action Hidden;

        internal Modal(GameObject root, Transform content, TMP_Text title, Transform toolbar, TMP_Text status)
        {
            Root = root; Content = content; Title = title; Toolbar = toolbar; Status = status;
        }

        public void Show() { if (Root != null) Root.SetActive(true); }

        public void Hide()
        {
            if (Root != null) Root.SetActive(false);
            try { Hidden?.Invoke(); } catch (Exception ex) { Debug.LogError("[GameUI] modal hidden handler: " + ex); }
        }

        /// <summary>Convenience: append a game-styled button to the toolbar.</summary>
        public Button AddToolbarButton(string label, Action onClick)
            => Pixel.CreateButton(Toolbar, label, onClick);
    }
}
