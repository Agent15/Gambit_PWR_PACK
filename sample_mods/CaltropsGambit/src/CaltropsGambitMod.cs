using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.CaltropsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitCaltrops.cs.
    /// </summary>
    public sealed class CaltropsGambitMod : IMod
    {
        public void OnLoad(IModContext context)
        {
            context.LogLine("[CaltropsGambit] registering Caltrops' Gambit.");

            // Optional custom art: put `caltrops.png` next to mod.json.
            // In source form that means:
            //   CaltropsGambit/caltrops.png
            // After building/installing, sample_mods/build.sh copies it beside the DLL:
            //   Mods/CaltropsGambit/caltrops.png
            // If the file is missing, we generate a tiny placeholder so the mod still works.
            var spritePath = Path.Combine(context.ModDirectory, "caltrops.png");
            var sprite = File.Exists(spritePath)
                ? ModGambitApi.LoadSprite(spritePath)
                : GenerateFallbackSprite();

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit caltrops`, so keep it short and readable.
            var def = GambitBuilder.Create("caltrops")
                .WithName("Caltrops' Gambit")
                .WithDescription("Enemy pieces that step on <color=◊>TRAP</color> tiles are captured instead of trapped.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.TRAP, Gambit_Focus.SACRIFICE)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach GambitCaltrops to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitCaltrops>()
                // Show the vanilla trap-tile explanation icon/text in the tooltip,
                // because Caltrops builds on vanilla TRAP tiles.
                .ShowTrapTile()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[CaltropsGambit] registered '{def.Id}'.");
        }

        private static Sprite GenerateFallbackSprite()
        {
            const int w = 17;
            const int h = 26;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var c = Color.clear;

                    // Three little spike clusters, readable at gambit-card size.
                    var spikeA = y >= 5 && y <= 14 && x >= 2 && x <= 7 && (x + y) % 3 != 0;
                    var spikeB = y >= 9 && y <= 19 && x >= 6 && x <= 12 && (x + 2 * y) % 4 != 0;
                    var spikeC = y >= 3 && y <= 12 && x >= 10 && x <= 15 && (2 * x + y) % 3 != 0;
                    var stem = (y == 15 && x >= 2 && x <= 13) || (y == 20 && x >= 5 && x <= 13);

                    if (spikeA || spikeB || spikeC || stem)
                    {
                        var edge = x <= 2 || x >= 15 || y <= 4 || y >= 20;
                        c = edge ? new Color(0.18f, 0.12f, 0.08f, 1f) : new Color(0.70f, 0.55f, 0.30f, 1f);
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
