using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gambonanza.ModHost
{
    internal static class ModKeybinds
    {
        public const string Unset = "unset";

        private static readonly KeyCode[] ModifierKeys =
        {
            KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftControl, KeyCode.RightControl,
            KeyCode.LeftAlt, KeyCode.RightAlt,
            KeyCode.LeftCommand, KeyCode.RightCommand,
            KeyCode.LeftApple, KeyCode.RightApple,
        };

        private static readonly KeyCode[] CandidateKeys = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .Where(k => k != KeyCode.None && !ModifierKeys.Contains(k))
            .ToArray();

        public static bool IsHeld(string spec)
        {
            if (!TryParse(spec, out var binding)) return false;
            return ModifiersSatisfied(binding) && Input.GetKey(binding.Key);
        }

        public static bool WasPressed(string spec)
        {
            if (!TryParse(spec, out var binding)) return false;
            return ModifiersSatisfied(binding) && Input.GetKeyDown(binding.Key);
        }

        public static KeyCode FirstNonModifierKeyDown()
        {
            foreach (var key in CandidateKeys)
                if (Input.GetKeyDown(key)) return key;
            return KeyCode.None;
        }

        public static string CaptureSpec(KeyCode key)
        {
            var parts = new List<string>();
            if (ShiftDown()) parts.Add("Shift");
            if (CtrlDown()) parts.Add("Ctrl");
            if (AltDown()) parts.Add("Alt");
            if (CmdDown()) parts.Add("Cmd");
            parts.Add(NormalizeKeyName(key));
            return string.Join("+", parts.ToArray());
        }

        public static bool IsUnset(string spec)
        {
            return string.IsNullOrWhiteSpace(spec) || string.Equals(spec.Trim(), Unset, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParse(string spec, out Binding binding)
        {
            binding = default;
            if (IsUnset(spec)) return false;
            var parts = spec.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
            if (parts.Length == 0) return false;

            var keyToken = parts[parts.Length - 1];
            if (!TryParseKey(keyToken, out var key)) return false;

            binding = new Binding { Key = key };
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var mod = parts[i].ToLowerInvariant();
                if (mod == "shift") binding.Shift = true;
                else if (mod == "ctrl" || mod == "control") binding.Ctrl = true;
                else if (mod == "alt" || mod == "option") binding.Alt = true;
                else if (mod == "cmd" || mod == "command" || mod == "meta" || mod == "super") binding.Cmd = true;
            }
            return true;
        }

        private static bool ModifiersSatisfied(Binding b)
        {
            if (b.Shift && !ShiftDown()) return false;
            if (b.Ctrl && !CtrlDown()) return false;
            if (b.Alt && !AltDown()) return false;
            if (b.Cmd && !CmdDown()) return false;
            return true;
        }

        private static bool TryParseKey(string token, out KeyCode key)
        {
            key = KeyCode.None;
            if (string.IsNullOrWhiteSpace(token)) return false;
            var t = token.Trim();
            switch (t.ToLowerInvariant())
            {
                case "space": key = KeyCode.Space; return true;
                case "esc": case "escape": key = KeyCode.Escape; return true;
                case "backtick": case "grave": case "`": key = KeyCode.BackQuote; return true;
                case "mouse0": case "leftmouse": key = KeyCode.Mouse0; return true;
                case "mouse1": case "rightmouse": key = KeyCode.Mouse1; return true;
                case "mouse2": case "middlemouse": case "middlemousebutton": key = KeyCode.Mouse2; return true;
            }
            return Enum.TryParse(t, ignoreCase: true, out key);
        }

        private static string NormalizeKeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.BackQuote: return "BackQuote";
                case KeyCode.Mouse0: return "Mouse0";
                case KeyCode.Mouse1: return "Mouse1";
                case KeyCode.Mouse2: return "Mouse2";
                default: return key.ToString();
            }
        }

        private static bool ShiftDown() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        private static bool CtrlDown() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        private static bool AltDown() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        private static bool CmdDown() => Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple);

        private struct Binding
        {
            public KeyCode Key;
            public bool Shift;
            public bool Ctrl;
            public bool Alt;
            public bool Cmd;
        }
    }
}
