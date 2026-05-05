using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.ModHost
{
    internal sealed class ModRegistry
    {
        private readonly List<LoadedMod> _mods = new List<LoadedMod>();
        private readonly Dictionary<string, LoadedMod> _byId = new Dictionary<string, LoadedMod>();
        private readonly HashSet<string> _seenDirs = new HashSet<string>();

        public IReadOnlyList<LoadedMod> Mods => _mods;
        public int Count => _mods.Count;

        // ----- Initial load -------------------------------------------------

        public void LoadMod(string modDirectory, ModManifest manifest)
        {
            _seenDirs.Add(modDirectory);
            var loaded = LoadAndConstruct(modDirectory, manifest);
            _mods.Add(loaded);
            _byId[loaded.Manifest.id] = loaded;
            ModHost.LogLine($"loaded '{manifest.id}' v{manifest.version} (entry={manifest.entry})");

            if (loaded.Manifest.enabled)
            {
                loaded.IsActive = true;
                if (loaded.Lifecycle != null)
                {
                    try { loaded.Lifecycle.OnEnable(); }
                    catch (Exception ex) { loaded.Context.LogLine("OnEnable threw: " + ex); }
                }
            }
        }

        // ----- Hot toggle ---------------------------------------------------

        public bool TryDisable(string modId, out string error)
        {
            error = null;
            if (!_byId.TryGetValue(modId, out var mod)) { error = "mod not loaded"; return false; }
            if (!mod.IsActive) return true;
            mod.IsActive = false;
            mod.Manifest.enabled = false;
            WriteManifest(mod);
            if (mod.Lifecycle == null) { error = "mod has no IModLifecycle; restart required to fully disable"; return true; }
            try { mod.Lifecycle.OnDisable(); }
            catch (Exception ex) { error = "OnDisable threw: " + ex.Message; mod.Context.LogLine(error); }
            return true;
        }

        public bool TryEnable(string modId, out string error)
        {
            error = null;
            if (!_byId.TryGetValue(modId, out var mod)) { error = "mod not loaded"; return false; }
            if (mod.IsActive) return true;
            mod.IsActive = true;
            mod.Manifest.enabled = true;
            WriteManifest(mod);
            if (mod.Lifecycle == null) { error = "mod has no IModLifecycle; restart required to fully enable"; return true; }
            try { mod.Lifecycle.OnEnable(); }
            catch (Exception ex) { error = "OnEnable threw: " + ex.Message; mod.Context.LogLine(error); }
            return true;
        }

        // ----- Rescan for newly-added mods ----------------------------------

        public int Rescan(string modsDir)
        {
            if (!Directory.Exists(modsDir)) return 0;
            int newlyLoaded = 0;
            foreach (var dir in Directory.GetDirectories(modsDir))
            {
                if (_seenDirs.Contains(dir)) continue;
                _seenDirs.Add(dir);
                var manifestPath = Path.Combine(dir, "mod.json");
                if (!File.Exists(manifestPath)) continue;
                try
                {
                    var json = File.ReadAllText(manifestPath);
                    var manifest = ModManifest.TryParse(json, out var parseErr);
                    if (manifest == null) { ModHost.LogLine($"rescan: invalid mod.json in {dir}: {parseErr}"); continue; }
                    if (!manifest.IsValid(out var validErr)) { ModHost.LogLine($"rescan: invalid manifest in {dir}: {validErr}"); continue; }
                    LoadMod(dir, manifest);
                    newlyLoaded++;
                }
                catch (Exception ex) { ModHost.LogLine($"rescan: failed to load {dir}: {ex.Message}"); }
            }
            return newlyLoaded;
        }

        // ----- Event dispatch -----------------------------------------------

        public void DispatchSettingsOpened(MonoBehaviour settingsCanvas)
        {
            for (int i = 0; i < _mods.Count; i++)
            {
                var m = _mods[i];
                if (!m.IsActive) continue;
                m.Context.RaiseSettingsOpened(settingsCanvas);
            }
        }

        // ----- Internals ----------------------------------------------------

        private LoadedMod LoadAndConstruct(string modDirectory, ModManifest manifest)
        {
            var dlls = Directory.GetFiles(modDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            if (dlls.Length == 0)
                throw new FileNotFoundException($"No .dll files in {modDirectory}");

            Type entryType = null;
            Assembly loadedAsm = null;
            foreach (var dll in dlls)
            {
                Assembly asm;
                try { asm = Assembly.LoadFrom(dll); }
                catch (Exception ex)
                {
                    ModHost.LogLine($"[{manifest.id}] could not load '{Path.GetFileName(dll)}': {ex.Message}");
                    continue;
                }
                var t = asm.GetType(manifest.entry, throwOnError: false);
                if (t != null) { entryType = t; loadedAsm = asm; break; }
            }

            if (entryType == null)
                throw new TypeLoadException($"Entry type '{manifest.entry}' not found in any DLL under {modDirectory}");
            if (!typeof(IMod).IsAssignableFrom(entryType))
                throw new InvalidCastException($"{manifest.entry} does not implement Gambonanza.ModSdk.IMod");

            var instance = (IMod)Activator.CreateInstance(entryType);
            var ctx = new ModContext(manifest.id, modDirectory);
            try { instance.OnLoad(ctx); }
            catch (Exception ex) { ctx.LogLine("OnLoad threw: " + ex); }

            return new LoadedMod
            {
                Manifest      = manifest,
                ManifestPath  = Path.Combine(modDirectory, "mod.json"),
                Directory     = modDirectory,
                Instance      = instance,
                Lifecycle     = instance as IModLifecycle,
                Context       = ctx,
                Assembly      = loadedAsm,
                IsActive      = false,
            };
        }

        private static void WriteManifest(LoadedMod mod)
        {
            try
            {
                var json = JsonUtility.ToJson(mod.Manifest, prettyPrint: true);
                File.WriteAllText(mod.ManifestPath, json);
            }
            catch (Exception ex) { ModHost.LogLine($"failed to write {mod.ManifestPath}: {ex.Message}"); }
        }

        // ----- LoadedMod ----------------------------------------------------

        internal sealed class LoadedMod
        {
            public ModManifest    Manifest;
            public string         ManifestPath;
            public string         Directory;
            public IMod           Instance;
            public IModLifecycle  Lifecycle;
            public ModContext     Context;
            public Assembly       Assembly;
            public bool           IsActive;
        }
    }
}
