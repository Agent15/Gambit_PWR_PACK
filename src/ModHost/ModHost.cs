using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// Single static entry point. The Cecil patcher injects:
    ///   - ModHost.LoadAll()                              at GameManager.Start
    ///   - ModHost.OnSettingsOpenedInvoke(this)           at SettingsCanvas.OnEnable
    /// </summary>
    public static class ModHost
    {
        private static bool _loaded;
        private static ModRegistry _registry;
        private static string _modsDirectory;
        private static ModConsole _console;
        private static ConsoleMenuInjector _consoleMenuInjector;

        public static void LoadAll()
        {
            if (_loaded) return;
            _loaded = true;
            _console = ModConsole.Ensure();
            _registry = new ModRegistry();
            _consoleMenuInjector = new ConsoleMenuInjector();

            try
            {
                _modsDirectory = ResolveModsDirectory();
                LogLine($"online. mods directory = {_modsDirectory}");

                if (!Directory.Exists(_modsDirectory))
                {
                    try { Directory.CreateDirectory(_modsDirectory); } catch { }
                    LogLine("Mods folder did not exist; created it (empty).");
                }
                else
                {
                    foreach (var modDir in OrderByDependencies(Directory.GetDirectories(_modsDirectory)))
                        LoadOne(modDir);
                }

                LogLine($"loaded {_registry.Count} mod(s).");
            }
            catch (Exception ex)
            {
                LogLine("LoadAll failed: " + ex);
            }

            int n = _registry.Count;
            _console?.PrintInfo($"ModHost v{ModUpdater.FrameworkVersion} online — {n} mod{(n == 1 ? "" : "s")} loaded. Press F10 or ` to toggle. Type 'help' for commands.");
            if (_console != null) ModUpdater.SpawnOnce(_console);
        }

        public static void OnSettingsOpenedInvoke(MonoBehaviour settingsCanvas)
        {
            if (!_loaded) LoadAll();
            try { _registry?.DispatchSettingsOpened(settingsCanvas); }
            catch (Exception ex) { LogLine("OnSettingsOpenedInvoke failed: " + ex); }
        }

        public static void OnHomeMenuOpenedInvoke(MonoBehaviour canvasMenu)
        {
            if (!_loaded) LoadAll();
            try { _consoleMenuInjector?.InjectButton(canvasMenu); }
            catch (Exception ex) { LogLine("console button injection failed: " + ex); }
        }

        internal static void OpenConsole()
        {
            if (!_loaded) LoadAll();
            (_console ?? ModConsole.Ensure()).Open();
        }

        internal static void OpenModsFolderInFinder()
        {
            try
            {
                if (string.IsNullOrEmpty(_modsDirectory)) return;
                if (!Directory.Exists(_modsDirectory)) Directory.CreateDirectory(_modsDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{_modsDirectory}\"",
                    UseShellExecute = false,
                });
            }
            catch (Exception ex) { LogLine("OpenModsFolder failed: " + ex.Message); }
        }

        // Exposed to console commands so they can introspect the registry.
        internal static System.Collections.Generic.IReadOnlyList<ModRegistry.LoadedMod> AllMods()
            => _registry?.Mods ?? (System.Collections.Generic.IReadOnlyList<ModRegistry.LoadedMod>)
                   System.Array.Empty<ModRegistry.LoadedMod>();

        internal static bool TryEnable(string id, out string error)  => _registry.TryEnable(id, out error);
        internal static bool TryDisable(string id, out string error) => _registry.TryDisable(id, out error);
        internal static int  Rescan() => _registry?.Rescan(_modsDirectory) ?? 0;

        internal static System.Collections.Generic.IEnumerable<ModRegistry.KeybindInfo> AllKeybinds(string modId = null)
            => _registry?.AllKeybinds(modId) ?? System.Linq.Enumerable.Empty<ModRegistry.KeybindInfo>();

        internal static bool TrySetKeybind(string modId, string name, string key, out string error)
        {
            error = null;
            return _registry != null && _registry.TrySetKeybind(modId, name, key, out error);
        }

        internal static string GetKeybind(string modId, string name)
            => _registry?.GetKeybind(modId, name) ?? ModKeybinds.Unset;

        internal static bool IsKeybindHeld(string modId, string name)
            => ModKeybinds.IsHeld(GetKeybind(modId, name));

        internal static bool WasKeybindPressed(string modId, string name)
            => ModKeybinds.WasPressed(GetKeybind(modId, name));

        internal static void LogLine(string s)
        {
            try { UnityEngine.Debug.Log("[ModHost] " + s); } catch { }
        }

        private static void LoadOne(string modDir)
        {
            var manifestPath = Path.Combine(modDir, "mod.json");
            if (!File.Exists(manifestPath))
            {
                LogLine($"skipped '{Path.GetFileName(modDir)}': no mod.json");
                return;
            }

            string json;
            try { json = File.ReadAllText(manifestPath); }
            catch (Exception ex) { LogLine($"could not read {manifestPath}: {ex.Message}"); return; }

            var manifest = ModManifest.TryParse(json, out var parseError);
            if (manifest == null)
            {
                LogLine($"invalid mod.json in '{modDir}': {parseError}");
                return;
            }
            if (!manifest.IsValid(out var validationError))
            {
                LogLine($"invalid manifest in '{modDir}': {validationError}");
                return;
            }

            try { _registry.LoadMod(modDir, manifest); }
            catch (Exception ex) { LogLine($"failed to load '{manifest.id}': {ex.Message}"); }
        }

        /// <summary>
        /// Topologically sorts mod directories so a mod's dependencies (declared in mod.json
        /// "dependencies") load before it does. Mods with unparseable manifests, missing
        /// dependencies, or that participate in a cycle are appended at the end so the rest
        /// can still load in a useful order.
        /// </summary>
        private static System.Collections.Generic.List<string> OrderByDependencies(string[] modDirs)
        {
            var manifestByDir = new System.Collections.Generic.Dictionary<string, ModManifest>();
            var dirById = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var unparsed = new System.Collections.Generic.List<string>();

            // Pass 1: parse every manifest. Anything that fails to parse goes to the tail —
            // LoadOne will re-emit the same diagnostic and skip it.
            Array.Sort(modDirs, StringComparer.Ordinal);
            foreach (var dir in modDirs)
            {
                var manifestPath = Path.Combine(dir, "mod.json");
                if (!File.Exists(manifestPath)) { unparsed.Add(dir); continue; }
                string json;
                try { json = File.ReadAllText(manifestPath); }
                catch { unparsed.Add(dir); continue; }
                var m = ModManifest.TryParse(json, out _);
                if (m == null || !m.IsValid(out _)) { unparsed.Add(dir); continue; }
                manifestByDir[dir] = m;
                if (!dirById.ContainsKey(m.id)) dirById[m.id] = dir;
            }

            // Pass 2: Kahn's algorithm. ready = mods whose unmet deps are zero.
            var unmet = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var dependents = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in manifestByDir)
            {
                var id = kv.Value.id;
                var deps = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (kv.Value.dependencies != null)
                {
                    foreach (var d in kv.Value.dependencies)
                    {
                        if (string.IsNullOrWhiteSpace(d)) continue;
                        // Drop deps we cannot satisfy from the discovered set; LoadOne will
                        // surface the actual failure when the mod tries to use them.
                        if (!dirById.ContainsKey(d))
                        {
                            LogLine($"'{id}' declares missing dependency '{d}'; loading anyway after the rest.");
                            continue;
                        }
                        deps.Add(d);
                        if (!dependents.TryGetValue(d, out var list))
                            dependents[d] = list = new System.Collections.Generic.List<string>();
                        list.Add(id);
                    }
                }
                unmet[id] = deps;
            }

            var ready = new System.Collections.Generic.SortedSet<string>(StringComparer.Ordinal);
            foreach (var kv in unmet) if (kv.Value.Count == 0) ready.Add(kv.Key);

            var ordered = new System.Collections.Generic.List<string>();
            while (ready.Count > 0)
            {
                var next = ready.Min;
                ready.Remove(next);
                ordered.Add(dirById[next]);
                if (dependents.TryGetValue(next, out var deps))
                {
                    foreach (var d in deps)
                    {
                        if (unmet[d].Remove(next) && unmet[d].Count == 0) ready.Add(d);
                    }
                }
            }

            // Anything still with unmet deps is in a cycle. Append in stable alphabetical order.
            if (ordered.Count < manifestByDir.Count)
            {
                LogLine("dependency cycle detected; cycle members will be loaded after the rest.");
                foreach (var kv in manifestByDir)
                {
                    if (unmet[kv.Value.id].Count > 0) ordered.Add(kv.Key);
                }
            }

            ordered.AddRange(unparsed);
            return ordered;
        }

        private static string ResolveModsDirectory()
        {
            var candidates = new System.Collections.Generic.List<string>();

            var env = Environment.GetEnvironmentVariable("GAMBONANZA_MODS_DIR");
            if (!string.IsNullOrEmpty(env)) candidates.Add(env);

            string dataPath = null;
            try { dataPath = Application.dataPath; } catch { }
            LogLine($"Application.dataPath = {dataPath ?? "<null>"}");

            if (!string.IsNullOrEmpty(dataPath))
            {
                for (int up = 1; up <= 6; up++)
                {
                    var sb = new System.Text.StringBuilder(dataPath);
                    for (int i = 0; i < up; i++) sb.Append("/..");
                    sb.Append("/Mods");
                    candidates.Add(Path.GetFullPath(sb.ToString()));
                }
            }

            // Extra safety for Windows/Git-Bash installs: derive candidates from
            // the actual loaded ModHost DLL location (Managed/) rather than only
            // Unity's Application.dataPath. This catches both
            // Gambonanza_Data/Managed and .app/.../Data/Managed layouts.
            string assemblyDir = null;
            try { assemblyDir = Path.GetDirectoryName(typeof(ModHost).Assembly.Location); } catch { }
            LogLine($"ModHost assembly dir = {assemblyDir ?? "<null>"}");
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                for (int up = 1; up <= 8; up++)
                {
                    var sb = new System.Text.StringBuilder(assemblyDir);
                    for (int i = 0; i < up; i++) sb.Append(Path.DirectorySeparatorChar).Append("..");
                    sb.Append(Path.DirectorySeparatorChar).Append("Mods");
                    candidates.Add(Path.GetFullPath(sb.ToString()));
                }
            }

            var home = Environment.GetEnvironmentVariable("HOME") ?? "";
            candidates.Add(Path.Combine(home, "Library", "Application Support", "Gambonanza", "Mods"));

            foreach (var c in candidates)
            {
                LogLine($"  candidate: {c} (exists={Directory.Exists(c)})");
                if (Directory.Exists(c)) return c;
            }
            return candidates.Count > 0 ? candidates[0] : Path.Combine(home, "Mods");
        }
    }
}
