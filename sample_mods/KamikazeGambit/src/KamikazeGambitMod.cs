using System.IO;
using Blukulele.CHE;
using Gambonanza.ModSdk;
using Gambonanza.GambitApi;
using UnityEngine;

namespace Gambonanza.KamikazeGambit
{
    /// <summary>
    /// Entry point for the Kamikaze Gambit. Loads the bundled sprite (or generates a
    /// placeholder), then asks <c>GambitBuilder</c> from GambitApi to register a new
    /// EPIC-rarity gambit whose runtime behaviour is implemented by <see cref="GambitKamikaze"/>.
    /// Also wires up an F8/F9 debug hotkey so the gambit can be injected into a live run
    /// without going through the shop.
    /// </summary>
    public class KamikazeGambitMod : IMod
    {
        public void OnLoad(IModContext context)
        {
            context.LogLine("[KamikazeGambit] OnLoad called.");
            Debug.Log("[KamikazeGambit] OnLoad called.");

            var spritePath = Path.Combine(context.ModDirectory, "kamikaze.png");
            Debug.Log($"[KamikazeGambit] Looking for sprite at: {spritePath}");

            Sprite sprite = ModGambitApi.LoadSprite(spritePath);

            if (sprite == null)
            {
                Debug.Log("[KamikazeGambit] No custom sprite found, generating fallback...");
                sprite = GenerateFallbackSprite();
                context.LogLine("[KamikazeGambit] Using fallback sprite. Drop a kamikaze.png in the mod folder for the real one (any size; the API rescales to match the vanilla template).");
            }
            else
            {
                Debug.Log("[KamikazeGambit] Custom sprite loaded successfully.");
            }

            Debug.Log("[KamikazeGambit] Building gambit definition...");

            var builder = GambitBuilder.Create("KamikazeGambit_Kamikaze")
                .WithName("Kamikaze's Gambit")
                .WithDescription("<color=©>LANDING</color> on an enemy piece destroys both pieces.<br><i>(Once per game)</i>")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.SACRIFICE, Gambit_Focus.LANDING)
                .WithPrice(8)
                .WithVisual(sprite)
                // Our art is more tightly cropped than vanilla cards, so matching the
                // template's world height 1:1 makes it look slightly oversized in-game.
                // 0.85 brings it back in line.
                .WithVisualScale(0.85f)
                // No .CloneFrom() - let the API use the first available vanilla prefab as template
                .WithBaseGambit<GambitKamikaze>()
                .ShowLanding()
                .AutoUnlock(true);

            Debug.Log("[KamikazeGambit] Registering gambit...");
            var def = builder.Register();

            if (def != null)
            {
                context.LogLine($"[KamikazeGambit] Registered '{def.Id}' successfully!");
                Debug.Log($"[KamikazeGambit] Registered '{def.Id}' successfully!");
            }
            else
            {
                Debug.LogError("[KamikazeGambit] Register() returned null!");
            }

            // Debug-only hotkey: F9 injects the kamikaze gambit into a free slot of the current run.
            var hotkeyHost = new GameObject("KamikazeDebugHost");
            UnityEngine.Object.DontDestroyOnLoad(hotkeyHost);
            hotkeyHost.AddComponent<KamikazeDebugHotkey>();
        }

        private static Sprite GenerateFallbackSprite()
        {
            Debug.Log("[KamikazeGambit] Generating fallback sprite...");
            int w = 17, h = 26;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool fill = false;
                    if (x >= 5 && x <= 11 && y >= 6 && y <= 18) fill = true;
                    if (x >= 7 && x <= 9 && y >= 19 && y <= 22) fill = true;
                    if (y >= 10 && y <= 13 && x >= 1 && x <= 15) fill = true;
                    if (x >= 7 && x <= 9 && y >= 2 && y <= 5) fill = true;
                    if (x >= 7 && x <= 9 && y >= 0 && y <= 1) fill = true;

                    if (fill)
                    {
                        if (x == 5 || x == 11 || y == 6 || y == 18)
                            pixels[y * w + x] = new Color(0.6f, 0.1f, 0f, 1f);
                        else if (y >= 19)
                            pixels[y * w + x] = new Color(0.9f, 0.9f, 0.3f, 1f);
                        else if (y <= 1)
                            pixels[y * w + x] = new Color(1f, 0.5f, 0f, 1f);
                        else
                            pixels[y * w + x] = new Color(0.9f, 0.2f, 0.05f, 1f);
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            Debug.Log("[KamikazeGambit] Fallback sprite generated.");
            return sprite;
        }
    }
}
