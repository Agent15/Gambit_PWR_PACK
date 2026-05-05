using System;
using Blukulele.CHE;
using UnityEngine;

namespace Gambonanza.GambitApi
{
    /// <summary>
    /// Static convenience entry point for the Gambit Creation API.
    /// </summary>
    public static class ModGambitApi
    {
        /// <summary>
        /// Start defining a new gambit.
        /// </summary>
        public static GambitBuilder CreateGambit(string id) => GambitBuilder.Create(id);

        /// <summary>
        /// Register a pre-built gambit definition.
        /// </summary>
        public static void Register(GambitDefinition def) => GambitRegistry.Register(def);

        /// <summary>
        /// Register multiple gambit definitions.
        /// </summary>
        public static void RegisterAll(params GambitDefinition[] defs) => GambitRegistry.RegisterAll(defs);

        /// <summary>
        /// Force-unlock a gambit by ID. Useful if you disabled AutoUnlock during registration.
        /// </summary>
        public static void Unlock(string gambitId)
        {
            var um = Blukulele.Core.SingletonMonoBehaviour<GambitUnlockManager>.Instance;
            um?.UnlockGambit(gambitId);
        }

        /// <summary>
        /// Load a sprite from a file path inside your mod folder.
        /// </summary>
        public static Sprite LoadSprite(string filePath, Vector2? pivot = null)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError($"[GambitApi] Sprite file not found: {filePath}");
                return null;
            }

            var bytes = System.IO.File.ReadAllBytes(filePath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot ?? new Vector2(0.5f, 0.5f), 100f);
            return sprite;
        }
    }
}
