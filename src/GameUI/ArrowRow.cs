using System;
using TMPro;
using UnityEngine;

namespace Gambonanza.GameUI
{
    /// <summary>
    /// Handle returned by <see cref="Pixel.AddSettingsArrowRow"/>. Lets the caller
    /// retitle, push new values, or remove the row when the mod is disabled.
    /// </summary>
    public sealed class ArrowRow
    {
        public GameObject Root { get; }
        public TMP_Text Title { get; }
        public TMP_Text Value { get; }

        internal ArrowRow(GameObject root, TMP_Text title, TMP_Text value)
        {
            Root = root; Title = title; Value = value;
        }

        /// <summary>Convenience: set the title text (no-op if title TMP wasn't found).</summary>
        public void SetTitle(string s) { if (Title != null) try { Title.text = s; } catch { } }

        /// <summary>Convenience: set the value text (no-op if value TMP wasn't found).</summary>
        public void SetValue(string s) { if (Value != null) try { Value.text = s; } catch { } }

        /// <summary>
        /// Destroy the row's GameObject. Safe to call if it's already gone.
        /// Use from your mod's IModLifecycle.OnDisable so toggling-off cleans up.
        /// </summary>
        public void Remove()
        {
            if (Root == null) return;
            try { UnityEngine.Object.Destroy(Root); } catch { }
        }
    }
}
