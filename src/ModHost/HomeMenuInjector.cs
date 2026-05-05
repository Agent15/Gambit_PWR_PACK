using System;
using Gambonanza.GameUI;
using UnityEngine;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// Adds a "MODS" button next to Settings in the home menu by delegating to
    /// <see cref="Pixel.AddHomeMenuButton"/>. All clone/strip/rewire mechanics
    /// live in the GameUI library so any mod can do the same.
    /// </summary>
    internal sealed class HomeMenuInjector
    {
        private const string InjectedName = "ModHost_OpenModsButton";
        private readonly Action _onClick;

        public HomeMenuInjector(Action onClick) { _onClick = onClick; }

        public void InjectButton(MonoBehaviour canvasMenu)
        {
            if (canvasMenu == null) return;
            var btn = Pixel.AddHomeMenuButton(canvasMenu, "MODS", InjectedName, _onClick);
            if (btn != null)
                ModHost.LogLine($"Injected '{InjectedName}' next to Settings via GameUI.Pixel.");
        }
    }
}
