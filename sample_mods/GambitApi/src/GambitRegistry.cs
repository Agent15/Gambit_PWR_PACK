using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gambonanza.GambitApi
{
    public static class GambitRegistry
    {
        private static readonly List<GambitDefinition> _pending = new();
        private static bool _processing;
        private static readonly List<string> _unlockQueue = new();
        private static readonly Dictionary<string, (string name, string description)> _localizationEntries = new();
        // Inactive container that holds prefab templates. Prefabs themselves stay activeSelf=true,
        // so Instantiate(prefab) yields an active instance — but the template doesn't tick because
        // its parent is inactive (activeInHierarchy=false).
        private static GameObject _prefabRegistry;

        public static void Register(GambitDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (string.IsNullOrWhiteSpace(def.Id))
                throw new ArgumentException("Gambit ID cannot be empty.", nameof(def));

            Debug.Log($"[GambitApi] Register() called for '{def.Id}'.");

            // Cache localization entries for injection
            _localizationEntries[def.Id] = (def.Name, def.Description);

            if (CanRegisterImmediately())
            {
                DoRegister(def);
            }
            else
            {
                _pending.Add(def);
                TryStartProcessing();
            }
        }

        public static void RegisterAll(params GambitDefinition[] defs)
        {
            foreach (var def in defs)
                Register(def);
        }

        public static void ProcessPending()
        {
            if (_processing || _pending.Count == 0) return;
            _processing = true;

            var library = SingletonMonoBehaviour<GambitLibrary>.Instance;
            if (library == null)
            {
                Debug.LogError("[GambitApi] ProcessPending: GambitLibrary is null!");
                _processing = false;
                return;
            }

            var toProcess = new List<GambitDefinition>(_pending);
            _pending.Clear();

            foreach (var def in toProcess)
            {
                try { DoRegister(def); }
                catch (Exception ex) { Debug.LogError($"[GambitApi] Failed to register '{def.Id}': {ex}"); }
            }

            _processing = false;
        }

        private static bool CanRegisterImmediately()
        {
            var library = SingletonMonoBehaviour<GambitLibrary>.Instance;
            if (library == null) return false;
            var focusMapField = typeof(GambitLibrary).GetField("m_FocusMap", BindingFlags.NonPublic | BindingFlags.Instance);
            return library.GambitsInfo != null && library.GambitsInfo.Count > 0 && focusMapField?.GetValue(library) != null;
        }

        private static void DoRegister(GambitDefinition def)
        {
            var library = SingletonMonoBehaviour<GambitLibrary>.Instance;
            if (library == null) throw new InvalidOperationException("GambitLibrary is not available.");

            if (library.GambitsInfo.Any(g => g.ID == def.Id))
            {
                Debug.LogWarning($"[GambitApi] Gambit '{def.Id}' already registered. Skipping.");
                return;
            }

            // 1. Create ScriptableObject
            var soGambit = ScriptableObject.CreateInstance<SO_Gambit>();
            soGambit.ID = def.Id;
            soGambit.GambitName = $"{def.Id}_name";
            soGambit.GambitDescription = $"{def.Id}_description";
            soGambit.GambitVisual = def.Visual;
            soGambit.PriceCost = def.PriceCost;
            soGambit.Rarity = def.Rarity;
            soGambit.Focus = def.Focus ?? new[] { Gambit_Focus.UTILITY };
            soGambit.UnlockInfos = def.UnlockInfo;
            soGambit.GambitToUnlockToHaveAHint = def.GambitToUnlockToHaveAHint;

            soGambit.ShowPromotion = def.ShowPromotion;
            soGambit.ShowBless = def.ShowBless;
            soGambit.ShowGolden = def.ShowGolden;
            soGambit.ShowProtect = def.ShowProtect;
            soGambit.ShowTrap = def.ShowTrap;
            soGambit.ShowPhantom = def.ShowPhantom;
            soGambit.ShowWait = def.ShowWait;
            soGambit.ShowGoldenTile = def.ShowGoldenTile;
            soGambit.ShowBlessedTile = def.ShowBlessedTile;
            soGambit.ShowProtectedTile = def.ShowProtectedTile;
            soGambit.ShowTrapTile = def.ShowTrapTile;
            soGambit.ShowPhantomTile = def.ShowPhantomTile;
            soGambit.ShowLanding = def.ShowLanding;
            soGambit.ShowConsideredAs = def.ShowConsideredAs;

            // 2. Build prefab
            GambitBehaviour prefab = BuildPrefab(def, soGambit, library);

            // 3. Add to library
            int index = library.GambitsInfo.Count;
            soGambit.Gambit_Library_Index = index;
            library.GambitsInfo.Add(soGambit);
            library.Gambits.Add(prefab);

            // 4. Reinitialize sorted lists
            ReinitializeLibrary(library);

            // 5. Inject localization
            InjectLocalization(def);

            // 6. Queue unlock
            if (def.AutoUnlock)
                QueueUnlock(def.Id);

            // 7. Invalidate collection cache
            InvalidateCollectionCache();

            Debug.Log($"[GambitApi] Registered '{def.Id}' at index {index}.");
        }

        private static void InjectLocalization(GambitDefinition def)
        {
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
            {
                Debug.LogWarning("[GambitApi] LocalizationManager not found, tooltip text will be empty.");
                return;
            }

            // Force load if not cached
            var traduction = locManager.GetTraduction();
            if (traduction == null)
            {
                Debug.LogWarning("[GambitApi] GetTraduction() returned null.");
                return;
            }

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
            {
                Debug.LogWarning("[GambitApi] traduction['gambit'] node not found.");
                return;
            }

            string nameKey = $"{def.Id}_name";
            string descKey = $"{def.Id}_description";

            // The JSON implementation uses custom setters via indexer
            gambitNode[nameKey] = def.Name;
            gambitNode[descKey] = def.Description;

            Debug.Log($"[GambitApi] Injected localization: '{nameKey}' = '{def.Name}', '{descKey}' = '{def.Description}'");
        }

        private static void ReinitializeLibrary(GambitLibrary library)
        {
            library.Gambits_Common.Clear();
            library.Gambits_Rare.Clear();
            library.Gambits_Epic.Clear();
            library.Gambits_Legendary.Clear();
            library.Gambit_PAWN.Clear();
            library.Gambit_ROOK.Clear();
            library.Gambit_KNIGHT.Clear();
            library.Gambit_BISHOP.Clear();
            library.Gambit_QUEEN.Clear();
            library.Gambit_KING.Clear();
            library.Gambit_MONEY.Clear();
            library.Gambit_OTHER.Clear();
            library.Gambit_PROMOTION.Clear();
            library.Gambit_WAIT.Clear();
            library.Gambit_PHANTOM.Clear();
            library.Gambit_BLESS.Clear();
            library.Gambit_PROTECTIVE.Clear();
            library.Gambit_TRAP.Clear();
            library.Gambit_GOLDEN.Clear();
            library.Gambit_LAND.Clear();
            library.Gambit_SACRIFICE.Clear();
            library.Gambit_PIECE_SELLER.Clear();
            library.Gambit_GAMBIT_SELLER.Clear();
            library.Gambit_CRUMBLE.Clear();

            var initMethod = typeof(GambitLibrary).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (initMethod != null)
            {
                initMethod.Invoke(library, null);
                Debug.Log("[GambitApi] Reinitialized GambitLibrary.");
            }
        }

        private static GambitBehaviour BuildPrefab(GambitDefinition def, SO_Gambit soGambit, GambitLibrary library)
        {
            string templateId = def.TemplateGambitId;
            GambitBehaviour templatePrefab = null;

            if (!string.IsNullOrEmpty(templateId))
            {
                templatePrefab = FindPrefabById(library, templateId);
                if (templatePrefab == null)
                    Debug.LogWarning($"[GambitApi] Template '{templateId}' not found, using fallback.");
            }

            if (templatePrefab == null && library.Gambits.Count > 0)
                templatePrefab = library.Gambits[0];

            if (templatePrefab == null)
                throw new InvalidOperationException("No template prefab found.");

            // Park the template under an inactive registry GameObject. The clone itself stays
            // activeSelf=true so Object.Instantiate(prefab) yields an active instance, but the
            // template doesn't tick because activeInHierarchy=false through the inactive parent.
            var registry = GetOrCreatePrefabRegistry();
            var clone = UnityEngine.Object.Instantiate(templatePrefab, registry.transform);

            var oldBase = clone.GetComponent<BaseGambit>();
            if (oldBase != null)
                UnityEngine.Object.DestroyImmediate(oldBase);

            Type gambitType = def.BaseGambitType ?? typeof(SimpleGambit);
            var newBase = (BaseGambit)clone.gameObject.AddComponent(gambitType);
            if (newBase is SimpleGambit simple && def.TriggerAction != null)
                simple.OnTriggerAction = def.TriggerAction;

            clone.Info = soGambit;

            // Override the cloned template's in-game sprite with the modded visual.
            // The collection UI reads SO_Gambit.GambitVisual (already set), but the
            // in-game piece reads GambitBehaviour.m_Sprite. Custom mod sprites are usually
            // small (e.g. 17x26) and would render tiny at the default PPU; rebuild the sprite
            // with a PPU computed from the template's world height so the modded gambit
            // matches vanilla on-board size.
            if (def.Visual != null)
            {
                var spriteField = typeof(GambitBehaviour).GetField("m_Sprite", BindingFlags.NonPublic | BindingFlags.Instance);
                var highlightField = typeof(GambitBehaviour).GetField("m_SpriteHighlight", BindingFlags.NonPublic | BindingFlags.Instance);
                var templateSr = spriteField?.GetValue(clone) as SpriteRenderer;
                Sprite inGameSprite = def.Visual;
                if (templateSr != null && templateSr.sprite != null && def.Visual.texture != null)
                {
                    var tex = def.Visual.texture;
                    // Pixel-art sprites need point filtering (bilinear bleeds edge pixels into a
                    // visible halo) and clamp wrapping (repeat sampling pulls from the opposite
                    // edge — that's where the green stripe was coming from).
                    try { tex.filterMode = FilterMode.Point; } catch { /* read-only texture */ }
                    try { tex.wrapMode = TextureWrapMode.Clamp; } catch { /* read-only texture */ }

                    var templateSprite = templateSr.sprite;
                    float templateWorldH = templateSprite.bounds.size.y;
                    float ourPixelH = tex.height;
                    if (templateWorldH > 0.0001f && ourPixelH > 0)
                    {
                        float ppu = ourPixelH / templateWorldH;
                        // Match the template sprite's pivot so the in-game piece sits on the same
                        // anchor (vanilla pieces are typically bottom-pivoted so they stand on the
                        // sell UI base; using center-pivot offsets the sprite vertically).
                        Vector2 pivot = new Vector2(0.5f, 0.5f);
                        var tRect = templateSprite.rect;
                        if (tRect.width > 0 && tRect.height > 0)
                        {
                            var tp = templateSprite.pivot;
                            pivot = new Vector2(tp.x / tRect.width, tp.y / tRect.height);
                        }
                        inGameSprite = Sprite.Create(
                            tex,
                            new Rect(0, 0, tex.width, tex.height),
                            pivot,
                            ppu);
                        inGameSprite.name = def.Id + "_ingame";
                    }
                }
                if (templateSr != null) templateSr.sprite = inGameSprite;
                if (highlightField?.GetValue(clone) is SpriteRenderer shr) shr.sprite = inGameSprite;
            }

            return clone;
        }

        private static GameObject GetOrCreatePrefabRegistry()
        {
            if (_prefabRegistry != null) return _prefabRegistry;
            _prefabRegistry = new GameObject("[GambitApi] PrefabRegistry");
            _prefabRegistry.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_prefabRegistry);
            return _prefabRegistry;
        }

        private static GambitBehaviour FindPrefabById(GambitLibrary library, string id)
        {
            var data = library.GetGambitPerId(id);
            if (data == null) return null;
            int idx = library.GambitsInfo.IndexOf(data);
            if (idx < 0 || idx >= library.Gambits.Count) return null;
            return library.Gambits[idx];
        }

        private static void InvalidateCollectionCache()
        {
            // Find ALL GambitCollectionSlide instances, including inactive ones
            var slides = Resources.FindObjectsOfTypeAll<GambitCollectionSlide>();
            if (slides != null && slides.Length > 0)
            {
                var initField = typeof(GambitCollectionSlide).GetField("m_Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var slide in slides)
                {
                    initField?.SetValue(slide, false);
                    if (slide.GetComponent<CollectionPaginationPatch>() == null)
                    {
                        slide.gameObject.AddComponent<CollectionPaginationPatch>();
                        Debug.Log($"[GambitApi] Attached CollectionPaginationPatch to '{slide.gameObject.name}'.");
                    }
                }
                Debug.Log($"[GambitApi] Invalidated and patched {slides.Length} collection slide(s).");
            }
            else
            {
                Debug.Log("[GambitApi] No collection slides found (active or inactive).");
            }

            // Patch every RunInfoCanvas (and CollectionCanvas which subclasses it) so the
            // hardcoded "X/200" denominator stays accurate against the modded library count.
            var canvases = Resources.FindObjectsOfTypeAll<RunInfoCanvas>();
            if (canvases != null)
            {
                foreach (var canvas in canvases)
                {
                    if (canvas.GetComponent<GambitCountPatch>() == null)
                    {
                        canvas.gameObject.AddComponent<GambitCountPatch>();
                        Debug.Log($"[GambitApi] Attached GambitCountPatch to '{canvas.gameObject.name}'.");
                    }
                }
            }
        }

        private static void QueueUnlock(string id)
        {
            if (!_unlockQueue.Contains(id))
                _unlockQueue.Add(id);

#pragma warning disable CS0618
            var host = UnityEngine.Object.FindObjectOfType<GambitApiHost>();
#pragma warning restore CS0618
            if (host != null)
                host.StartCoroutine(UnlockMonitorRoutine());
        }

        private static IEnumerator UnlockMonitorRoutine()
        {
            float elapsed = 0f;
            while (_unlockQueue.Count > 0 && elapsed < 10f)
            {
                var um = SingletonMonoBehaviour<GambitUnlockManager>.Instance;
                if (um != null && um.UnlockedGambits != null && um.UnlockedGambits.Count > 0)
                {
                    var toUnlock = new List<string>(_unlockQueue);
                    _unlockQueue.Clear();
                    foreach (var id in toUnlock)
                    {
                        try { um.UnlockGambit(id); }
                        catch (Exception ex) { Debug.LogError($"[GambitApi] Unlock failed '{id}': {ex}"); }
                    }
                    yield break;
                }
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        private static void TryStartProcessing()
        {
            if (_processing) return;
#pragma warning disable CS0618
            var host = UnityEngine.Object.FindObjectOfType<GambitApiHost>();
#pragma warning restore CS0618
            if (host != null)
                host.StartCoroutine(WaitAndProcess());
        }

        private static IEnumerator WaitAndProcess()
        {
            yield return null;
            ProcessPending();
        }
    }
}
