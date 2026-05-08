using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// Framework-shipped console commands. These exercise the same public API
    /// mods use, so if a built-in works the API works.
    ///
    /// Anything game-specific (gambits, save data) accesses vanilla types via
    /// reflection where possible — keeps the patcher's IL hooks unchanged when
    /// vanilla renames a field.
    /// </summary>
    internal static class BuiltinCommands
    {
        public static void Register(ModConsole c)
        {
            // ---- meta ---------------------------------------------------------

            c.RegisterCommand("help", "list every command", _ =>
            {
                int width = 0;
                foreach (var (name, _) in c.AllCommands()) if (name.Length > width) width = name.Length;
                foreach (var (name, help) in c.AllCommands())
                    c.Print($"  {name.PadRight(width)}   {help}");
            });
            c.RegisterCommand("?", "alias for 'help'", args => c.Submit("help"));

            c.RegisterCommand("clear", "wipe console scrollback", _ => c.Clear());
            c.RegisterCommand("cls", "alias for 'clear'", _ => c.Clear());

            c.RegisterCommand("echo", "print args back to the console", args =>
                c.Print(string.Join(" ", args)));

            c.RegisterCommand("version", "print framework + game version", _ =>
            {
                string game = "?";
                try { game = Application.version; } catch { }
                c.Print($"Gambonanza game version: {game}");
                c.Print("ModHost: 0.2.0");
            });

            c.RegisterCommand("quit", "quit the game (no confirm)", _ =>
            {
                c.PrintWarn("quitting…");
                try { Application.Quit(); } catch { }
            });

            // ---- mods ---------------------------------------------------------

            c.RegisterCommand("mods", "list every loaded mod", _ =>
            {
                var mods = ModHost.AllMods();
                if (mods.Count == 0) { c.Print("(no mods loaded)"); return; }
                int idW = 0, vW = 0;
                foreach (var m in mods)
                {
                    if (m.Manifest.id != null && m.Manifest.id.Length > idW) idW = m.Manifest.id.Length;
                    var v = m.Manifest.version ?? "";
                    if (v.Length > vW) vW = v.Length;
                }
                foreach (var m in mods)
                {
                    var state = m.IsActive ? "[on] " : "[off]";
                    var v = m.Manifest.version ?? "";
                    var author = string.IsNullOrEmpty(m.Manifest.author) ? "" : $" by {m.Manifest.author}";
                    c.Print($"  {state} {m.Manifest.id.PadRight(idW)}  v{v.PadRight(vW)}{author}");
                }
            });

            // 'mod <id>' — completer returns mod ids
            c.RegisterCommand("mod", "show one mod's manifest details", args =>
            {
                if (args.Length < 1) { c.PrintWarn("usage: mod <id>"); return; }
                var id = args[0];
                var m = ModHost.AllMods().FirstOrDefault(x => string.Equals(x.Manifest.id, id, StringComparison.OrdinalIgnoreCase));
                if (m == null) { c.PrintError($"no mod with id '{id}'"); return; }
                c.Print($"id:           {m.Manifest.id}");
                c.Print($"name:         {m.Manifest.name ?? "(unnamed)"}");
                c.Print($"version:      {m.Manifest.version ?? ""}");
                c.Print($"author:       {m.Manifest.author ?? ""}");
                c.Print($"entry:        {m.Manifest.entry ?? ""}");
                c.Print($"directory:    {m.Directory}");
                c.Print($"active:       {m.IsActive}");
                c.Print($"hot-toggle:   {(m.Lifecycle != null)}");
                if (m.Manifest.dependencies != null && m.Manifest.dependencies.Length > 0)
                    c.Print($"dependencies: {string.Join(", ", m.Manifest.dependencies)}");
            }, completer: (args, idx) => idx == 0 ? ModHost.AllMods().Select(m => m.Manifest.id) : null);

            // 'mod enable / disable / toggle <id>' — registry already supports
            // hot-toggle for mods that implement IModLifecycle; for the others
            // we still flip the manifest flag so the next launch picks it up.
            c.RegisterCommand("mod enable", "enable a mod (writes mod.json; hot if IModLifecycle)", args =>
            {
                if (args.Length < 1) { c.PrintWarn("usage: mod enable <id>"); return; }
                if (!ModHost.TryEnable(args[0], out var err)) c.PrintError(err);
                else if (!string.IsNullOrEmpty(err)) c.PrintWarn(err);
                else c.PrintInfo($"enabled '{args[0]}'.");
            }, completer: (args, idx) => idx == 0
                ? ModHost.AllMods().Where(m => !m.IsActive).Select(m => m.Manifest.id)
                : null);

            c.RegisterCommand("mod disable", "disable a mod (writes mod.json; hot if IModLifecycle)", args =>
            {
                if (args.Length < 1) { c.PrintWarn("usage: mod disable <id>"); return; }
                if (!ModHost.TryDisable(args[0], out var err)) c.PrintError(err);
                else if (!string.IsNullOrEmpty(err)) c.PrintWarn(err);
                else c.PrintInfo($"disabled '{args[0]}'.");
            }, completer: (args, idx) => idx == 0
                ? ModHost.AllMods().Where(m => m.IsActive).Select(m => m.Manifest.id)
                : null);

            c.RegisterCommand("mod toggle", "flip a mod's enabled state", args =>
            {
                if (args.Length < 1) { c.PrintWarn("usage: mod toggle <id>"); return; }
                var m = ModHost.AllMods().FirstOrDefault(x => string.Equals(x.Manifest.id, args[0], StringComparison.OrdinalIgnoreCase));
                if (m == null) { c.PrintError($"no mod with id '{args[0]}'"); return; }
                bool ok = m.IsActive ? ModHost.TryDisable(m.Manifest.id, out var err) : ModHost.TryEnable(m.Manifest.id, out err);
                if (!ok) c.PrintError(err);
                else if (!string.IsNullOrEmpty(err)) c.PrintWarn(err);
                else c.PrintInfo($"{(m.IsActive ? "enabled" : "disabled")} '{m.Manifest.id}'.");
            }, completer: (args, idx) => idx == 0 ? ModHost.AllMods().Select(m => m.Manifest.id) : null);

            // 'mod reload' — pick up newly-dropped mod folders without restarting.
            // Existing mods aren't re-instantiated; this is purely an additive
            // rescan of the Mods directory.
            c.RegisterCommand("mod reload", "rescan the Mods folder for newly-added mods", _ =>
            {
                int n = ModHost.Rescan();
                if (n == 0) c.PrintInfo("no new mods found.");
                else c.PrintInfo($"loaded {n} new mod(s).");
            });

            // 'mod restart' — for changes that can't take effect in-process
            // (mods without IModLifecycle, framework DLL changes). Just quits;
            // the user relaunches from Steam.
            c.RegisterCommand("mod restart", "quit the game so non-hot-reloadable mod changes take effect", _ =>
            {
                c.PrintWarn("quitting — relaunch from Steam to pick up the changes.");
                try { Application.Quit(); } catch { }
            });

            // ---- log tail -----------------------------------------------------

            c.RegisterCommand("tail", "mirror Player.log into the console (on|off)", args =>
            {
                if (args.Length < 1) { c.PrintWarn("usage: tail on|off"); return; }
                if (args[0].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    LogTail.Enable();
                    c.PrintInfo("tail: ON (mirroring [ModHost] / [<modid>] lines).");
                }
                else if (args[0].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    LogTail.Disable();
                    c.PrintInfo("tail: OFF.");
                }
                else c.PrintWarn("usage: tail on|off");
            }, completer: (args, idx) => idx == 0 ? new[] { "on", "off" } : null);

            // ---- timescale ----------------------------------------------------

            c.RegisterCommand("time-scale", "set Time.timeScale (1=normal, 0=pause)", args =>
            {
                if (args.Length < 1) { c.Print($"current Time.timeScale = {Time.timeScale}"); return; }
                if (!float.TryParse(args[0], out var v)) { c.PrintError("not a number: " + args[0]); return; }
                Time.timeScale = Mathf.Clamp(v, 0f, 100f);
                c.PrintInfo($"Time.timeScale = {Time.timeScale}");
            });

            // ---- gambit cheats ------------------------------------------------

            c.RegisterCommand("gambit list", "list every registered gambit id", _ =>
            {
                var ids = AllGambitIds();
                if (ids.Count == 0) { c.PrintWarn("GambitLibrary not initialised yet — start or resume a run first."); return; }
                foreach (var id in ids.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)) c.Print("  " + id);
                c.PrintInfo($"{ids.Count} gambit(s).");
            });

            c.RegisterCommand("gambit give", "spawn a gambit into a free slot of the current run", args =>
            {
                if (args.Length < 1) { c.PrintWarn("usage: gambit give <id>"); return; }
                if (!TryGiveGambit(args[0], out var err)) c.PrintError(err);
                else c.PrintInfo($"injected '{args[0]}' into a free slot.");
            }, completer: (args, idx) => idx == 0 ? AllGambitIds() : null);

            c.RegisterCommand("gambit clear", "remove every gambit from the current run", _ =>
            {
                if (!TryClearGambits(out var err)) c.PrintError(err);
                else c.PrintInfo("cleared all gambit slots.");
            });
        }

        // ----- gambit access (reflection-tolerant) ------------------------------

        private const string GambitFocusMapField = "m_FocusMap";

        private static List<string> AllGambitIds()
        {
            var result = new List<string>();
            if (!SingletonMonoBehaviour<GambitLibrary>.IsCreated()) return result;
            var lib = SingletonMonoBehaviour<GambitLibrary>.Instance;
            if (lib?.GambitsInfo == null) return result;
            foreach (var info in lib.GambitsInfo)
                if (info != null && !string.IsNullOrEmpty(info.ID)) result.Add(info.ID);
            return result;
        }

        // Mirror of ArbiterDebugHotkey.TryInject, framework-flavoured. Reflection
        // on private save-data fields keeps the build green when vanilla shuffles
        // them.
        private static bool TryGiveGambit(string id, out string err)
        {
            err = null;
            if (!SingletonMonoBehaviour<GambitLibrary>.IsCreated()) { err = "GambitLibrary not created."; return false; }
            var lib = SingletonMonoBehaviour<GambitLibrary>.Instance;
            var info = lib.GetGambitPerId(id);
            if (info == null) { err = $"unknown gambit id '{id}'."; return false; }
            int idx = lib.GambitsInfo.IndexOf(info);
            if (idx < 0 || idx >= lib.Gambits.Count) { err = "library prefab list out of sync with info list."; return false; }

            if (!SingletonMonoBehaviour<GambitManager>.IsCreated())
            { err = "GambitManager not created — start or resume a run first."; return false; }
            var mgr = SingletonMonoBehaviour<GambitManager>.Instance;
            if (mgr.GambitPlaces == null || mgr.GambitPlaces.Length == 0)
            { err = "GambitPlaces empty — run not initialised."; return false; }
            if (mgr.IsFull()) { err = "all gambit slots are full."; return false; }

            var place = mgr.GetGambitPlace();
            if (place == null) { err = "GetGambitPlace returned null."; return false; }

            var prefab = lib.Gambits[idx];
            var instance = UnityEngine.Object.Instantiate(prefab, place.GambitParent);
            place.CurrentGambit = instance;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale    = Vector3.one;

            // Persist into save data so a reload restores it. CurrentGambits stores
            // GambitName (the localization key vanilla matches on), not ID.
            try
            {
                var data = DataManager.Instance?.Data;
                if (data != null && data.CurrentGambits != null)
                {
                    for (int i = 0; i < data.CurrentGambits.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(data.CurrentGambits[i]))
                        {
                            data.CurrentGambits[i] = info.GambitName;
                            break;
                        }
                    }
                }
            }
            catch { /* save persistence is best-effort; runtime injection still worked. */ }

            return true;
        }

        private static bool TryClearGambits(out string err)
        {
            err = null;
            if (!SingletonMonoBehaviour<GambitManager>.IsCreated())
            { err = "GambitManager not created — start or resume a run first."; return false; }
            var mgr = SingletonMonoBehaviour<GambitManager>.Instance;
            if (mgr.GambitPlaces == null) return true;
            foreach (var place in mgr.GambitPlaces)
            {
                if (place == null || place.CurrentGambit == null) continue;
                UnityEngine.Object.Destroy(place.CurrentGambit.gameObject);
                place.CurrentGambit = null;
            }
            try
            {
                var data = DataManager.Instance?.Data;
                if (data != null && data.CurrentGambits != null)
                    for (int i = 0; i < data.CurrentGambits.Length; i++) data.CurrentGambits[i] = "";
            }
            catch { }
            return true;
        }
    }

    /// <summary>
    /// Subscribes to <see cref="Application.logMessageReceived"/> and forwards
    /// matching lines into the console. Off by default; toggled by `tail on/off`.
    /// </summary>
    internal static class LogTail
    {
        private static bool _on;
        private static readonly Queue<(string, ConsoleLineColor)> _pending = new Queue<(string, ConsoleLineColor)>();

        public static void Enable()
        {
            if (_on) return;
            _on = true;
            Application.logMessageReceived += OnLog;
            ModConsoleUI.RegisterUpdate(Pump);
        }

        public static void Disable()
        {
            if (!_on) return;
            _on = false;
            Application.logMessageReceived -= OnLog;
            ModConsoleUI.UnregisterUpdate(Pump);
        }

        // Called from any thread — we just enqueue. Pump runs on the main thread.
        private static void OnLog(string condition, string stack, LogType type)
        {
            if (string.IsNullOrEmpty(condition)) return;
            // Only mirror lines we likely produced. Vanilla floods Debug.Log; we
            // want signal, not the firehose.
            if (!(condition.StartsWith("[ModHost]", StringComparison.Ordinal)
                  || (condition.Length > 0 && condition[0] == '[' && !condition.StartsWith("[Unity"))))
                return;
            ConsoleLineColor col;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert: col = ConsoleLineColor.Error; break;
                case LogType.Warning: col = ConsoleLineColor.Warn; break;
                default: col = ConsoleLineColor.Default; break;
            }
            lock (_pending) _pending.Enqueue((condition, col));
        }

        private static void Pump()
        {
            if (!_on) return;
            lock (_pending)
                while (_pending.Count > 0)
                {
                    var (msg, col) = _pending.Dequeue();
                    ModConsole.Instance?.Print(msg, col);
                }
        }
    }
}
