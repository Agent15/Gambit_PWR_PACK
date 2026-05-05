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
    ///   - ModHost.OnHomeMenuOpenedInvoke(this)           at CanvasMenu.OnEnable
    /// </summary>
    public static class ModHost
    {
        private static bool _loaded;
        private static ModRegistry _registry;
        private static string _modsDirectory;
        private static HomeMenuInjector _menuInjector;

        public static void LoadAll()
        {
            if (_loaded) return;
            _loaded = true;
            _registry = new ModRegistry();
            _menuInjector = new HomeMenuInjector(() => OpenModManagerUI());

            try
            {
                _modsDirectory = ResolveModsDirectory();
                LogLine($"online. mods directory = {_modsDirectory}");

                if (!Directory.Exists(_modsDirectory))
                {
                    try { Directory.CreateDirectory(_modsDirectory); } catch { }
                    LogLine("Mods folder did not exist; created it (empty).");
                    return;
                }

                foreach (var modDir in Directory.GetDirectories(_modsDirectory))
                    LoadOne(modDir);

                LogLine($"loaded {_registry.Count} mod(s).");
            }
            catch (Exception ex)
            {
                LogLine("LoadAll failed: " + ex);
            }
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
            LogLine("OnHomeMenuOpenedInvoke fired.");
            try { _menuInjector?.InjectButton(canvasMenu); }
            catch (Exception ex) { LogLine("OnHomeMenuOpenedInvoke failed: " + ex); }
        }

        // ----- API exposed to the modal UI --------------------------------------

        internal static string ModsDirectory => _modsDirectory;

        internal static System.Collections.Generic.IReadOnlyList<ModRegistry.LoadedMod> AllMods()
            => _registry?.Mods ?? (System.Collections.Generic.IReadOnlyList<ModRegistry.LoadedMod>)
                   System.Array.Empty<ModRegistry.LoadedMod>();

        internal static bool TryEnable(string modId, out string error)
            => _registry.TryEnable(modId, out error);

        internal static bool TryDisable(string modId, out string error)
            => _registry.TryDisable(modId, out error);

        internal static int Rescan()
            => _registry?.Rescan(_modsDirectory) ?? 0;

        internal static void OpenModsFolderInFinder()
        {
            try
            {
                if (string.IsNullOrEmpty(_modsDirectory)) return;
                if (!Directory.Exists(_modsDirectory)) Directory.CreateDirectory(_modsDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "open",
                    Arguments       = $"\"{_modsDirectory}\"",
                    UseShellExecute = false,
                });
            }
            catch (Exception ex) { LogLine("OpenModsFolder failed: " + ex.Message); }
        }

        // -----------------------------------------------------------------------

        internal static void LogLine(string s)
        {
            try { UnityEngine.Debug.Log("[ModHost] " + s); } catch { }
        }

        private static void OpenModManagerUI()
        {
            LogLine("OpenModManagerUI: MODS button clicked");
            try { ModManagerUI.Show(); }
            catch (Exception ex) { LogLine("Show modal failed: " + ex); }
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
