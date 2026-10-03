using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.NegativeGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitNegative.cs.
    /// </summary>
    public sealed class NegativeGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[NegativeGambit] registering Negative Gambit.");

            // Custom art: put `Negative.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Negative.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit negative`, so keep it short and readable.
            var def = GambitBuilder.Create("negative")
                .WithName("Negative Gambit")
                .WithDescription("Other gambits can be placed on top of this.")
                .WithRarity(Rarity.LEGENDARY)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(15)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach NegativeGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitNegative>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[NegativeGambit] registered '{def.Id}'.");
        }
    }
}
