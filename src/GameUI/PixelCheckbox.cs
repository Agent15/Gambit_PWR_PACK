using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gambonanza.GameUI
{
    /// <summary>
    /// A two-state row that matches the in-game Settings menu checkboxes (cream
    /// rectangle with a title and a dark checkmark). Created by
    /// <see cref="Pixel.CreateCheckbox"/>. Click the row to flip; the callback
    /// fires with the new value.
    /// </summary>
    public sealed class PixelCheckbox
    {
        public GameObject Root { get; }
        public TMP_Text Label { get; }
        public bool IsOn => _isOn;

        private readonly GameObject _checkmark;
        private readonly Action<bool> _onChange;
        private bool _isOn;

        internal PixelCheckbox(GameObject root, TMP_Text label, GameObject checkmark,
                               string text, bool initialOn, Action<bool> onChange)
        {
            Root = root;
            Label = label;
            _checkmark = checkmark;
            _onChange  = onChange;
            _isOn      = initialOn;
            if (Label != null) Label.text = text ?? "";
            ApplyVisual();
        }

        /// <summary>Set state. <paramref name="notify"/>=false suppresses the callback.</summary>
        public void Set(bool on, bool notify = true)
        {
            if (_isOn == on) { ApplyVisual(); return; }
            _isOn = on;
            ApplyVisual();
            if (notify) Safe.Invoke(() => _onChange?.Invoke(_isOn));
        }

        public void SetLabel(string text) { if (Label != null) try { Label.text = text ?? ""; } catch { } }

        private void ApplyVisual()
        {
            if (_checkmark != null) _checkmark.SetActive(_isOn);
        }
    }
}
