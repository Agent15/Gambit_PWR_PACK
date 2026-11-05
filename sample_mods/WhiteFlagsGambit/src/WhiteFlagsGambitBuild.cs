using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.WhiteFlagsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitWhiteFlag.cs.
    /// </summary>
    public sealed class WhiteFlagsGambitBuild : IMod
    {
        // This gambit should only be single use, so I'm declaring a bool variable
        // to determine if it was triggered
        public static bool IDidThat = false;
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[WhiteFlagsGambit] registering WhiteFlag's Gambit.");

            // Custom art: put `WhiteFlag.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "WhiteFlag.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit white-flag`, so keep it short and readable.
            var def = GambitBuilder.Create("white-flag")
                .WithName("White Flag's Gambit")
                .WithDescription("Sell this gambit to force a stalemate.")
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(6)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach WhiteFlagsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitWhiteFlag>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[WhiteFlagsGambit] registered '{def.Id}'.");
        }
    }
}
