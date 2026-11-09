using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.PitStopsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitPitStop.cs.
    /// </summary>
    public sealed class PitStopsGambitBuild : IMod
    {
        // Declare a bool variable to determine if a stalemate occurred
        public static bool stalemateHappened = false;
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[PitStopsGambit] registering PitStop's Gambit.");

            // Custom art: put `PitStop.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "PitStop.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit pit-stop`, so keep it short and readable.
            var def = GambitBuilder.Create("pit-stop")
                .WithName("Pit Stop's Gambit")
                .WithDescription("If a game ends in stalemate, earn <color=*>$10</color> and return to the shop.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.MONEY, Gambit_Focus.UTILITY)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach PitStopsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitPitStop>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[PitStopsGambit] registered '{def.Id}'.");
        }
    }
}
