using System;
using UnityEngine;

namespace Gambonanza.ModSdk
{
    /// <summary>
    /// Per-mod runtime context, supplied by ModHost during OnLoad.
    /// Use the events to subscribe to game lifecycle hooks the patcher routes through ModHost.
    /// </summary>
    public interface IModContext
    {
        /// <summary>The mod's id from mod.json.</summary>
        string ModId { get; }

        /// <summary>Absolute path to the mod's folder under Mods/.</summary>
        string ModDirectory { get; }

        /// <summary>Logs to Unity's Debug.Log with a [ModId] prefix.</summary>
        void LogLine(string message);

        /// <summary>
        /// Fires every time SettingsCanvas.OnEnable runs. Argument is the SettingsCanvas instance.
        /// Subscribers should be idempotent — the modal may be opened many times in one session.
        /// </summary>
        event Action<MonoBehaviour> OnSettingsOpened;
    }
}
