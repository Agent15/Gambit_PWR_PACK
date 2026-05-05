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

        public event Action<MonoBehaviour> OnSettingsOpened;

        public ModContext(string modId, string modDirectory)
        {
            ModId = modId;
            ModDirectory = modDirectory;
        }

        public void LogLine(string message)
        {
            try { Debug.Log($"[{ModId}] {message}"); } catch { }
        }

        internal void RaiseSettingsOpened(MonoBehaviour settingsCanvas)
        {
            var handler = OnSettingsOpened;
            if (handler == null) return;
            try { handler(settingsCanvas); }
            catch (Exception ex) { LogLine("OnSettingsOpened handler threw: " + ex); }
        }
    }
}
