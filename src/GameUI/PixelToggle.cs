using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gambonanza.GameUI
{
    /// <summary>
    /// A two-state button styled to match the game's pixel art. Clicking flips the
    /// state and calls back. Use <see cref="Pixel.CreateToggle"/> to build one.
    ///
    /// Visual = "{label}: ON" / "{label}: OFF" if you pass a label, else just "ON" / "OFF".
    /// </summary>
    public sealed class PixelToggle
    {
        private readonly Button   _button;
        private readonly TMP_Text _labelText;
        private readonly string   _baseLabel;
        private readonly Action<bool> _onChange;
        private bool _isOn;

        public GameObject Root => _button != null ? _button.gameObject : null;
        public Button     Button => _button;
        public bool       IsOn => _isOn;

        internal PixelToggle(Button button, TMP_Text labelText, string baseLabel, bool initialOn, Action<bool> onChange)
        {
            _button     = button;
            _labelText  = labelText;
            _baseLabel  = baseLabel ?? "";
            _onChange   = onChange;
            _isOn       = initialOn;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => Set(!_isOn));
            }
            Repaint();
        }

        /// <summary>Set state. <paramref name="notify"/>=false suppresses the callback.</summary>
        public void Set(bool on, bool notify = true)
        {
            if (_isOn == on) { Repaint(); return; }
            _isOn = on;
            Repaint();
            if (notify) Safe.Invoke(() => _onChange?.Invoke(_isOn));
        }

        private void Repaint()
        {
            if (_labelText == null) return;
            _labelText.text = string.IsNullOrEmpty(_baseLabel)
                ? (_isOn ? "ON" : "OFF")
                : $"{_baseLabel}: {(_isOn ? "ON" : "OFF")}";
        }
    }
}
