using System;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// Default IModContext implementation handed to each mod during OnLoad.
    /// </summary>
    internal sealed class ModContext : IModContext
    {
        public string ModId        { get; }
        public string ModDirectory { get; }
        public IConsoleApi Console { get; }

        public event Action<MonoBehaviour> OnSettingsOpened;

        public ModContext(string modId, string modDirectory, IConsoleApi console)
        {
            ModId = modId;
            ModDirectory = modDirectory;
            Console = console;
        }

        public void LogLine(string message)
        {
            try { Debug.Log($"[{ModId}] {message}"); } catch { }
        }

        public bool IsKeybindHeld(string name) => ModHost.IsKeybindHeld(ModId, name);

        public bool WasKeybindPressed(string name) => ModHost.WasKeybindPressed(ModId, name);

        public string GetKeybind(string name) => ModHost.GetKeybind(ModId, name);

        internal void RaiseSettingsOpened(MonoBehaviour settingsCanvas)
        {
            var handler = OnSettingsOpened;
            if (handler == null) return;
            try { handler(settingsCanvas); }
            catch (Exception ex) { LogLine("OnSettingsOpened handler threw: " + ex); }
        }
    }
}
